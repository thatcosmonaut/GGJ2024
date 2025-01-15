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
    AsyncFileLoader AsyncFileLoader;
    GameState TransitionState;

    GraphicsPipeline TextPipeline;
    TextBatch TextBatch;

    System.Diagnostics.Stopwatch Timer = new System.Diagnostics.Stopwatch();
    System.Diagnostics.Stopwatch LoadTimer = new System.Diagnostics.Stopwatch();

    public LoadState(RollAndCashGame game, GameState transitionState)
    {
        Game = game;
        GraphicsDevice = Game.GraphicsDevice;
        AsyncFileLoader = new AsyncFileLoader(GraphicsDevice);
        TransitionState = transitionState;

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
        TextBatch = new TextBatch(GraphicsDevice);
    }

    public override void Start()
    {
        LoadTimer.Start();
        TextureAtlases.EnqueueLoadAllImages(AsyncFileLoader);
        StaticAudioPacks.LoadAsync(AsyncFileLoader);
        StreamingAudio.LoadAsync(AsyncFileLoader);
        AsyncFileLoader.Submit();
        Timer.Start();
    }

    public override void Update(TimeSpan delta)
    {
        if (AsyncFileLoader.Status == AsyncFileLoaderStatus.Failed)
        {
            // Uh oh, time to bail!
            throw new ApplicationException("Game assets could not be loaded!");
        }

        if (LoadTimer.IsRunning && AsyncFileLoader.Status == AsyncFileLoaderStatus.Complete)
        {
            LoadTimer.Stop();
            Logger.LogInfo($"Load finished in {LoadTimer.Elapsed.TotalMilliseconds}ms");
        }

        // "loading screens are why you have loading times" -Ethan Lee
        if (Timer.Elapsed.TotalSeconds > 3 && AsyncFileLoader.Status == AsyncFileLoaderStatus.Complete)
        {
            Timer.Stop();
            Game.SetState(TransitionState);
        }
    }

    public override void Draw(Window window, double alpha)
    {
        var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(Game.MainWindow);
        if (swapchainTexture != null)
        {
            TextBatch.Start();
            AddString("L", 60, new Position(1640, 1020), 1.2f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("O", 60, new Position(1680, 1020), 1.0f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("A", 60, new Position(1720, 1020), 0.8f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("D", 60, new Position(1760, 1020), 0.6f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("I", 60, new Position(1782, 1020), 0.4f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("N", 60, new Position(1820, 1020), 0.2f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("G", 60, new Position(1860, 1020), 0.0f + 4 * (float)Timer.Elapsed.TotalSeconds);
            TextBatch.UploadBufferData(commandBuffer);

            var renderPass = commandBuffer.BeginRenderPass(
                new ColorTargetInfo(swapchainTexture, Color.Black)
            );

            renderPass.BindGraphicsPipeline(TextPipeline);
            TextBatch.Render(renderPass, GetHiResProjectionMatrix());

            commandBuffer.EndRenderPass(renderPass);
        }

        GraphicsDevice.Submit(commandBuffer);
    }

    public override void End()
    {
        AsyncFileLoader.Dispose();
        AsyncFileLoader = null;
        StaticAudioPacks.pack_0.SliceBuffers();
        StaticAudio.LoadAll();
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

    private void AddString(string text, int pixelSize, Position position, float rotation)
    {
        TextBatch.Add(
            Fonts.FromID(Fonts.KosugiID),
            text,
            pixelSize,
            Matrix4x4.CreateRotationX(-rotation) * Matrix4x4.CreateTranslation(position.X, position.Y, -1),
            Color.White,
            HorizontalAlignment.Center,
            VerticalAlignment.Middle
        );
    }
}
