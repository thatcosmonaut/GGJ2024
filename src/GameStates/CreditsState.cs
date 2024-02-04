using System;
using System.Collections.Generic;
using MoonWorks;
using MoonWorks.Audio;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Math.Float;
using RollAndCash.Content;
using RollAndCash.Utility;

namespace RollAndCash.GameStates;

public class CreditsState : GameState
{
    RollAndCashGame Game;
    GameState TransitionState;

    GraphicsDevice GraphicsDevice;
    AudioDevice AudioDevice;

    GraphicsPipeline TextPipeline;
    TransientVoice Voice;

    Queue<TextBatch> BatchPool = new Queue<TextBatch>();
    List<(TextBatch, Matrix4x4)> ActiveBatchTransforms = new List<(TextBatch, Matrix4x4)>();

    private string[] Names = [
        "BEAU BLYTH",
        "COLIN JACKSON",
        "CASSANDRA LUGO",
        "EVAN HEMSLEY",
        "LAURA MICHET"
    ];

    float CreditsTime = 0;
    float CreditsDuration = 4;

    public CreditsState(RollAndCashGame game, GameState transitionStateA)
    {
        Game = game;
        TransitionState = transitionStateA;

        GraphicsDevice = Game.GraphicsDevice;
        AudioDevice = Game.AudioDevice;

        TextPipeline = new GraphicsPipeline(
            Game.GraphicsDevice,
            new GraphicsPipelineCreateInfo
            {
                AttachmentInfo = new GraphicsPipelineAttachmentInfo(
                    new ColorAttachmentDescription(
                        Game.MainWindow.SwapchainFormat,
                        ColorAttachmentBlendState.AlphaBlend
                    )
                ),
                DepthStencilState = DepthStencilState.DepthReadWrite,
                VertexShaderInfo = GraphicsDevice.TextVertexShaderInfo,
                FragmentShaderInfo = GraphicsDevice.TextFragmentShaderInfo,
                VertexInputState = GraphicsDevice.TextVertexInputState,
                RasterizerState = RasterizerState.CCW_CullNone,
                PrimitiveType = PrimitiveType.TriangleList,
                MultisampleState = MultisampleState.None
            }
        );

        Rando.Shuffle(Names);
    }

    public override void Start()
    {
        var sound = StaticAudio.Lookup(StaticAudio.CreditsLaugh);
        Voice = AudioDevice.Obtain<TransientVoice>(sound.Format);
        Voice.Submit(sound);
        Voice.Play();
    }

    public override void Update(TimeSpan delta)
    {
        CreditsTime += (float)delta.TotalSeconds;

        if (Game.Inputs.AnyPressed || CreditsTime >= CreditsDuration)
        {
            Game.SetState(TransitionState);
        }
    }

    public void SetTransitionState(GameState state)
    {
        TransitionState = state;
    }


    public override void Draw(Window window, double alpha)
    {
        var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);
        if (swapchainTexture != null)
        {
            foreach (var (batch, _) in ActiveBatchTransforms)
            {
                FreeTextBatch(batch);
            }
            ActiveBatchTransforms.Clear();

            AddString("JERRY CREW", 100, Color.White, new Position(960, 180));
            AddString("is", 50, Color.White, new Position(960, 300));

            var y = 420;
            foreach (var name in Names)
            {
                AddString(name, 70, Color.White, new Position(960, y));
                y += 100;
            }

            foreach (var (batch, _) in ActiveBatchTransforms)
            {
                batch.UploadBufferData(commandBuffer);
            }

            commandBuffer.BeginRenderPass(
                new ColorAttachmentInfo(swapchainTexture, Color.Black)
            );

            if (ActiveBatchTransforms.Count > 0)
            {
                var hiResProjectionMatrix = GetHiResProjectionMatrix();

                commandBuffer.BindGraphicsPipeline(TextPipeline);
                foreach (var (batch, transform) in ActiveBatchTransforms)
                {
                    batch.Render(commandBuffer, transform * hiResProjectionMatrix);
                }
            }

            commandBuffer.EndRenderPass();
        }

        GraphicsDevice.Submit(commandBuffer);
    }

    public override void End()
    {
        if (Voice != null)
            Voice.Stop();
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

    private void AddString(string text, int pixelSize, Color color, Position position)
    {
        var batch = AcquireTextBatch();

        batch.Start(Fonts.FromID(Fonts.KosugiID));
        batch.Add(
            text,
            pixelSize,
            color,
            HorizontalAlignment.Center,
            VerticalAlignment.Middle
        );

        ActiveBatchTransforms.Add((batch, Matrix4x4.CreateTranslation(position.X, position.Y, -1)));
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
