using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using MoonWorks;
using MoonWorks.Audio;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using RollAndCash.Content;
using RollAndCash.Utility;

namespace RollAndCash.GameStates;

public class CreditsState : GameState
{
    RollAndCashGame Game;
    GameState TransitionState;

    GraphicsDevice GraphicsDevice;

    GraphicsPipeline TextPipeline;
    PersistentVoice Voice;

    SpriteBatch HiResSpriteBatch;
    Sampler LinearSampler;

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

        var sound = StaticAudio.Lookup(StaticAudio.CreditsLaugh);
        Voice = game.AudioDevice.Obtain<PersistentVoice>(sound.Format);

        GraphicsDevice = Game.GraphicsDevice;

		TextPipeline = GraphicsPipeline.Create(
			GraphicsDevice,
			new GraphicsPipelineCreateInfo
			{
				TargetInfo = new GraphicsPipelineTargetInfo
				{
					DepthStencilFormat = TextureFormat.D16Unorm,
					HasDepthStencilTarget = true,
					ColorTargetDescriptions =
					[
						new ColorTargetDescription
						{
							Format = game.MainWindow.SwapchainFormat,
							BlendState = ColorTargetBlendState.PremultipliedAlphaBlend
						}
					]
				},
				DepthStencilState = new DepthStencilState
				{
					EnableDepthTest = true,
					EnableDepthWrite = true,
					CompareOp = CompareOp.LessOrEqual
				},
				VertexShader = GraphicsDevice.TextVertexShader,
				FragmentShader = GraphicsDevice.TextFragmentShader,
				VertexInputState = GraphicsDevice.TextVertexInputState,
				RasterizerState = RasterizerState.CCW_CullNone,
				PrimitiveType = PrimitiveType.TriangleList,
				MultisampleState = MultisampleState.None
			}
		);

        LinearSampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearClamp);
        HiResSpriteBatch = new SpriteBatch(GraphicsDevice, game.MainWindow.SwapchainFormat);

        Rando.Shuffle(Names);
    }

    public override void Start()
    {
        Rando.Shuffle(Names);
        CreditsTime = 0;

        var sound = StaticAudio.Lookup(StaticAudio.CreditsLaugh);
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


            HiResSpriteBatch.Start();

            var logoAnimation = SpriteAnimations.Logo_JerryCrew;
            var sprite = logoAnimation.Frames[0];
            var logoPosition = new Position(665, 80);

            HiResSpriteBatch.Add(
                new Vector3(logoPosition.X, logoPosition.Y, -1f),
                0,
                new Vector2(sprite.SliceRect.W, sprite.SliceRect.H),
                Color.White,
                sprite.UV.LeftTop, sprite.UV.Dimensions
            );

            HiResSpriteBatch.Upload(commandBuffer);

            AddString("is", 50, Color.White, new Position(960, 350));

            var y = 470;
            foreach (var name in Names)
            {
                AddString(name, 70, Color.White, new Position(960, y));
                y += 100;
            }

            foreach (var (batch, _) in ActiveBatchTransforms)
            {
                batch.UploadBufferData(commandBuffer);
            }

            var renderPass = commandBuffer.BeginRenderPass(
                new ColorTargetInfo(swapchainTexture, Color.Black)
            );

            var hiResViewProjectionMatrices = new ViewProjectionMatrices(Matrix4x4.Identity, GetHiResProjectionMatrix());

            HiResSpriteBatch.Render(
                renderPass,
                TextureAtlases.TP_HiRes.Texture,
                LinearSampler,
                hiResViewProjectionMatrices
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
