using System;
using System.IO;
using MoonWorks;
using MoonWorks.Audio;
using MoonWorks.Graphics;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using RollAndCash.Content;

namespace RollAndCash.GameStates;

public class LogoState : GameState
{
    RollAndCashGame Game;
    GraphicsDevice GraphicsDevice;
    AudioDevice AudioDevice;
    GameState TransitionState;

	GraphicsPipeline HiResPipeline;

	SpriteBatch HiResSpriteBatch;
	Sampler LinearSampler;

    float Fade = 0;
    float FadeTimer = 0;

    float FadeInDuration = 2f;
    float FadeHoldDuration = 1f;
    float FadeOutDuration = 2f;

    bool SoundPlayed = false;

    public LogoState(RollAndCashGame game, GameState transitionState)
    {
        Game = game;
        GraphicsDevice = game.GraphicsDevice;
        AudioDevice = game.AudioDevice;
        TransitionState = transitionState;

		var baseContentPath = Path.Combine(
			System.AppContext.BaseDirectory,
			"Content"
		);

		var shaderContentPath = Path.Combine(
			baseContentPath,
			"Shaders"
		);

		var vertShaderModule = new ShaderModule(GraphicsDevice, Path.Combine(shaderContentPath, "InstancedSpriteBatch.vert.refresh"));
		var fragShaderModule = new ShaderModule(GraphicsDevice, Path.Combine(shaderContentPath, "InstancedSpriteBatch.frag.refresh"));

        HiResPipeline = new GraphicsPipeline(
			GraphicsDevice,
            new GraphicsPipelineCreateInfo
            {
                AttachmentInfo = new GraphicsPipelineAttachmentInfo(
                    new ColorAttachmentDescription(
                        game.MainWindow.SwapchainFormat,
                        ColorAttachmentBlendState.NonPremultiplied
                    )
                ),
                DepthStencilState = DepthStencilState.Disable,
                MultisampleState = MultisampleState.None,
                PrimitiveType = PrimitiveType.TriangleList,
                RasterizerState = RasterizerState.CCW_CullNone,
                VertexInputState = new VertexInputState([
                    VertexBindingAndAttributes.Create<PositionVertex>(0),
                    VertexBindingAndAttributes.Create<SpriteInstanceData>(1, 1, VertexInputRate.Instance)
                ]),
                VertexShaderInfo = GraphicsShaderInfo.Create<ViewProjectionMatrices>(vertShaderModule, "main", 0),
                FragmentShaderInfo = GraphicsShaderInfo.Create(fragShaderModule, "main", 1)
            }
        );

        LinearSampler = new Sampler(GraphicsDevice, SamplerCreateInfo.LinearClamp);
        HiResSpriteBatch = new SpriteBatch(GraphicsDevice);
    }

    public override void Start()
    {

    }

    public override void Update(TimeSpan delta)
    {
        Fade = Easing.AttackHoldRelease(
            0,
            1,
            0,
            FadeTimer,
            FadeInDuration,
            Easing.Function.Float.InQuart,
            FadeHoldDuration,
            FadeOutDuration,
            Easing.Function.Float.OutQuart
        );

        if (!SoundPlayed && Fade == 1)
        {
            var sound = StaticAudio.Lookup(StaticAudio.MoonWorksChime);
            var voice = AudioDevice.Obtain<TransientVoice>(sound.Format);
            voice.Submit(sound);
            voice.Play();

            SoundPlayed = true;
        }

        FadeTimer += (float) delta.TotalSeconds;

        if (FadeTimer > FadeInDuration + FadeHoldDuration + FadeOutDuration)
        {
            Game.SetState(TransitionState);
        }
    }

    public override void Draw(Window window, double alpha)
    {
        var logoPosition = new Position(window.Width / 2 + 20, window.Height / 2 - 100);

        var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);

        if (swapchainTexture != null)
        {
            HiResSpriteBatch.Reset();

            var logoAnimation = SpriteAnimations.Logo_MoonWorks;
            var sprite = logoAnimation.Frames[0];
            HiResSpriteBatch.Add(
                new Vector3(logoPosition.X, logoPosition.Y, -1f),
                0,
                new Vector2(sprite.SliceRect.W, sprite.SliceRect.H),
                Color.Lerp(Color.White, Color.Black, 1 - Fade),
                sprite.UV.LeftTop, sprite.UV.Dimensions
            );

            HiResSpriteBatch.Upload(commandBuffer);

            commandBuffer.BeginRenderPass(new ColorAttachmentInfo(swapchainTexture, Color.Black));

            var hiResViewProjectionMatrices = new ViewProjectionMatrices(Matrix4x4.Identity, GetHiResProjectionMatrix());

            HiResSpriteBatch.Render(
                commandBuffer,
                HiResPipeline,
                TextureAtlases.TP_HiRes.Texture,
                LinearSampler,
                hiResViewProjectionMatrices
            );

            commandBuffer.EndRenderPass();
        }

        GraphicsDevice.Submit(commandBuffer);
    }

    public override void End()
    {

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
