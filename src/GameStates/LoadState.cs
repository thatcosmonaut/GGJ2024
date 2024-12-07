using System;
using System.Collections.Generic;
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
    AsyncFileLoader AsyncIOLoader;
    GameState TransitionState;

    GraphicsPipeline TextPipeline;

    Queue<TextBatch> BatchPool = new Queue<TextBatch>();
    List<(TextBatch, Matrix4x4)> ActiveBatchTransforms = new List<(TextBatch, Matrix4x4)>();

    System.Diagnostics.Stopwatch Timer = new System.Diagnostics.Stopwatch();
    System.Diagnostics.Stopwatch LoadTimer = new System.Diagnostics.Stopwatch();

    public LoadState(RollAndCashGame game, GameState transitionState)
    {
        Game = game;
        GraphicsDevice = Game.GraphicsDevice;
        AsyncIOLoader = new AsyncFileLoader(GraphicsDevice);
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
    }

    public override void Start()
    {
        LoadTimer.Start();
        TextureAtlases.EnqueueLoadAllImages(AsyncIOLoader);
        StaticAudioPacks.pack_0.LoadAsync(AsyncIOLoader);
        Timer.Start();
    }

    public override void Update(TimeSpan delta)
    {
        if (LoadTimer.IsRunning && AsyncIOLoader.Idle)
        {
            LoadTimer.Stop();
            Logger.LogInfo($"Load finished in {LoadTimer.Elapsed.TotalMilliseconds}ms");
        }

        if (Timer.Elapsed.TotalSeconds > 3 && AsyncIOLoader.Idle)
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
            foreach (var (batch, _) in ActiveBatchTransforms)
            {
                FreeTextBatch(batch);
            }
            ActiveBatchTransforms.Clear();

            AddString("L", 60, new Position(1640, 1020), 1.2f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("O", 60, new Position(1680, 1020), 1.0f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("A", 60, new Position(1720, 1020), 0.8f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("D", 60, new Position(1760, 1020), 0.6f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("I", 60, new Position(1782, 1020), 0.4f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("N", 60, new Position(1820, 1020), 0.2f + 4 * (float)Timer.Elapsed.TotalSeconds);
            AddString("G", 60, new Position(1860, 1020), 0.0f + 4 * (float)Timer.Elapsed.TotalSeconds);

            foreach (var (batch, _) in ActiveBatchTransforms)
            {
                batch.UploadBufferData(commandBuffer);
            }

            var renderPass = commandBuffer.BeginRenderPass(
                new ColorTargetInfo(swapchainTexture, Color.Black)
            );

            if (ActiveBatchTransforms.Count > 0)
            {
                var hiResProjectionMatrix = GetHiResProjectionMatrix();

                renderPass.BindGraphicsPipeline(TextPipeline);
                foreach (var (batch, transform) in ActiveBatchTransforms)
                {
                    batch.Render(renderPass, transform * hiResProjectionMatrix);
                }
            }

            commandBuffer.EndRenderPass(renderPass);
        }

        GraphicsDevice.Submit(commandBuffer);
    }

    public override void End()
    {
        AsyncIOLoader.Dispose();
        AsyncIOLoader = null;
        StaticAudioPacks.pack_0.SliceBuffers();
        StaticAudio.LoadAll();
        SpriteAnimations.LoadAll();
        ProductLoader.Load();
    }

    // In case we quit during load
    public void Destroy()
    {
        AsyncIOLoader?.Dispose();
        AsyncIOLoader = null;
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
        var batch = AcquireTextBatch();

        batch.Start(Fonts.FromID(Fonts.KosugiID));
        batch.Add(
            text,
            pixelSize,
            Color.White,
            HorizontalAlignment.Center,
            VerticalAlignment.Middle
        );

        ActiveBatchTransforms.Add((batch, Matrix4x4.CreateRotationX(-rotation) * Matrix4x4.CreateTranslation(position.X, position.Y, -1)));
    }

    private TextBatch AcquireTextBatch()
    {
        if (BatchPool.Count > 0)
        {
            return BatchPool.Dequeue();
        }
        else
        {
            return new TextBatch(GraphicsDevice);
        }
    }

    private void FreeTextBatch(TextBatch batch)
    {
        BatchPool.Enqueue(batch);
    }
}
