using System;
using System.IO;
using MoonWorks;
using MoonWorks.Audio;
using MoonWorks.Graphics;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using RollAndCash.Content;
using RollAndCash.Utility;

namespace RollAndCash.GameStates;

public class TitleState : GameState
{
    RollAndCashGame Game;
    GraphicsDevice GraphicsDevice;
    AudioDevice AudioDevice;
    GameState TransitionStateA;
    GameState TransitionStateB;

    GraphicsPipeline HiResPipeline;

    SpriteBatch HiResSpriteBatch;
    Texture RenderTexture;

    Sampler LinearSampler;
    StreamingVoice Voice;
    float Time = 30.0f;
    private float Timer = 0.0f;

    public TitleState(RollAndCashGame game, GameState transitionStateA, GameState transitionStateB)
    {
        Game = game;
        GraphicsDevice = game.GraphicsDevice;
        AudioDevice = game.AudioDevice;
        TransitionStateA = transitionStateA;
        TransitionStateB = transitionStateB;

        var sound = StreamingAudio.Lookup(StreamingAudio.roll_n_cash_grocery_lords);
        Voice = AudioDevice.Obtain<StreamingVoice>(sound.Format);

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

        RenderTexture = Texture.CreateTexture2D(GraphicsDevice, Dimensions.GAME_W, Dimensions.GAME_H, game.MainWindow.SwapchainFormat, TextureUsageFlags.ColorTarget);
    }

    public override void Start()
    {
		var sound = StreamingAudio.Lookup(StreamingAudio.roll_n_cash_grocery_lords);
        Voice.Load(sound);
        Voice.Play();

        var announcerSound = StaticAudio.Lookup(StaticAudio.RollAndCash);
        var announcerVoice = AudioDevice.Obtain<TransientVoice>(announcerSound.Format);
        announcerVoice.Submit(announcerSound);
        announcerVoice.SetVolume(1.8f);
        announcerVoice.Play();
    }

    public override void Update(TimeSpan delta)
    {
        Timer += (float)delta.TotalSeconds;

        Voice.Update();

        if (Game.Inputs.AnyPressed)
        {
            Game.SetState(TransitionStateB);
        }
        else if (Timer >= Time)
        {
            Timer = 0.0f;
            Game.SetState(TransitionStateA);
        }
    }

    public void SetTransitionStateB(GameState state)
    {
        TransitionStateB = state;
    }

    public override void Draw(Window window, double alpha)
    {
        var logoPosition = new Position(Rando.Range(-1, 1), Rando.Range(-1, 1));

        var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);

        if (swapchainTexture != null)
        {
            HiResSpriteBatch.Reset();

            var logoAnimation = SpriteAnimations.Screen_Title;
            var sprite = logoAnimation.Frames[0];
            HiResSpriteBatch.Add(
                new Vector3(logoPosition.X, logoPosition.Y, -1f),
                0,
                new Vector2(sprite.SliceRect.W, sprite.SliceRect.H),
                Color.White,
                sprite.UV.LeftTop, sprite.UV.Dimensions
            );

            HiResSpriteBatch.Upload(commandBuffer);

            commandBuffer.BeginRenderPass(new ColorAttachmentInfo(RenderTexture, Color.Black));

            var hiResViewProjectionMatrices = new ViewProjectionMatrices(Matrix4x4.Identity, GetHiResProjectionMatrix());

            HiResSpriteBatch.Render(
                commandBuffer,
                HiResPipeline,
                TextureAtlases.TP_HiRes.Texture,
                LinearSampler,
                hiResViewProjectionMatrices
            );

            commandBuffer.EndRenderPass();

            commandBuffer.CopyTextureToTexture(RenderTexture, swapchainTexture, MoonWorks.Graphics.Filter.Nearest);
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
            Dimensions.GAME_W,
            Dimensions.GAME_H,
            0,
            0.01f,
            1000
        );
    }
}
