using System;
using System.IO;
using MoonWorks;
using MoonWorks.Audio;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using RollAndCash.Content;
using RollAndCash.Systems;

namespace RollAndCash.GameStates;

public class LogoState : GameState
{
    RollAndCashGame Game;
    GraphicsDevice GraphicsDevice;
    AudioDevice AudioDevice;
    GameState TransitionStateA;
    GameState TransitionStateB;

    GraphicsPipeline HiResPipeline;

    SpriteBatch HiResSpriteBatch;
    Sampler LinearSampler;
    PersistentVoice Voice;

    float Fade = 0;
    float FadeTimer = 0;

    float FadeInDuration = 2f;
    float FadeHoldDuration = 1f;
    float FadeOutDuration = 2f;

    bool SoundPlayed = false;

    public LogoState(RollAndCashGame game, GameState transitionStateA, GameState transitionStateB)
    {
        Game = game;
        GraphicsDevice = game.GraphicsDevice;
        AudioDevice = game.AudioDevice;
        TransitionStateA = transitionStateA;
        TransitionStateB = transitionStateA;

        var sound = StaticAudio.Lookup(StaticAudio.MoonWorksChime);
        Voice = AudioDevice.Obtain<PersistentVoice>(sound.Format);
        Voice.Submit(sound);

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
        SoundPlayed = false;
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
        	Voice.Submit(sound);
            Voice.Play();

            SoundPlayed = true;
        }

        FadeTimer += (float)delta.TotalSeconds;
        if (Game.Inputs.AnyPressed)
        {
            Game.SetState(TransitionStateB);
        }
        else if (FadeTimer > FadeInDuration + FadeHoldDuration + FadeOutDuration)
        {
            Game.SetState(TransitionStateA);
        }

    }

    public override void Draw(Window window, double alpha)
    {
        var logoPosition = new Position(680, 250);

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
}
