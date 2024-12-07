using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.AsyncIO;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using RollAndCash.Content;
using RollAndCash.Systems;

namespace RollAndCash.GameStates;

public class LoadState : GameState
{
    RollAndCashGame Game;
    GraphicsDevice GraphicsDevice;
    AsyncIOLoader AsyncIOLoader;
    GameState TransitionState;

    TextBatch TextBatch;
    GraphicsPipeline TextPipeline;

    System.Diagnostics.Stopwatch Timer = new System.Diagnostics.Stopwatch();

    public LoadState(RollAndCashGame game, GameState transitionState)
    {
        Game = game;
        GraphicsDevice = Game.GraphicsDevice;
        AsyncIOLoader = Game.AsyncIOLoader;
        TransitionState = transitionState;

        TextBatch = new TextBatch(GraphicsDevice);
        TextPipeline = GraphicsPipeline.Create(
			GraphicsDevice,
			new GraphicsPipelineCreateInfo
			{
				TargetInfo = new GraphicsPipelineTargetInfo
				{
					ColorTargetDescriptions =
					[
						new ColorTargetDescription
						{
							Format = game.MainWindow.SwapchainFormat,
							BlendState = ColorTargetBlendState.PremultipliedAlphaBlend
						}
					]
				},
				DepthStencilState = DepthStencilState.Disable,
				VertexShader = GraphicsDevice.TextVertexShader,
				FragmentShader = GraphicsDevice.TextFragmentShader,
				VertexInputState = GraphicsDevice.TextVertexInputState,
				RasterizerState = RasterizerState.CCW_CullNone,
				PrimitiveType = PrimitiveType.TriangleList,
				MultisampleState = MultisampleState.None
			}
		);
    }

    public override void Start()
    {
        TextureAtlases.EnqueueLoadAllImages(AsyncIOLoader);
        StaticAudioPacks.pack_0.LoadAsync(AsyncIOLoader);
        Timer.Start();
    }

    public override void Update(TimeSpan delta)
    {
        if (Timer.Elapsed.TotalSeconds > 2 && AsyncIOLoader.Idle)
        {
            Timer.Stop();
            StaticAudioPacks.pack_0.SliceBuffers();
            StaticAudio.LoadAll();
            Game.SetState(TransitionState);
        }
    }

    public override void Draw(Window window, double alpha)
    {
        var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(Game.MainWindow);
        if (swapchainTexture != null)
        {
            TextBatch.Start(Fonts.FromID(Fonts.KosugiID));
            TextBatch.Add(
                "LOADING",
                60,
                Color.White,
                HorizontalAlignment.Center,
                VerticalAlignment.Middle
            );
            TextBatch.UploadBufferData(commandBuffer);

            var renderPass = commandBuffer.BeginRenderPass(
                new ColorTargetInfo(swapchainTexture, Color.Black)
            );

            renderPass.BindGraphicsPipeline(TextPipeline);

            var modelMatrix = Matrix4x4.CreateRotationX(10 * (float)Timer.Elapsed.TotalSeconds) * Matrix4x4.CreateTranslation(1720, 1020, -1);
            TextBatch.Render(renderPass, modelMatrix * GetHiResProjectionMatrix());

            commandBuffer.EndRenderPass(renderPass);
        }

        GraphicsDevice.Submit(commandBuffer);
    }

    public override void End()
    {
        SpriteAnimations.LoadAll();
        ProductLoader.Load();
    }

    private Matrix4x4 GetHiResProjectionMatrix()
    {
        return Matrix4x4.CreateOrthographicOffCenter(
            0,
            1920,
            1080,
            0,
            0.01f,
            1000
        );
    }
}
