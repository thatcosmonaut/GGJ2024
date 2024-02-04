using System;
using System.IO;
using MoonWorks;
using MoonWorks.Audio;
using MoonWorks.Graphics;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using RollAndCash.Content;

namespace RollAndCash.GameStates;

public class HowToPlayState : GameState
{
    RollAndCashGame Game;
    GraphicsDevice GraphicsDevice;
    GameState TransitionState;

    GraphicsPipeline HiResPipeline;

    SpriteBatch HiResSpriteBatch;
    Sampler LinearSampler;

    StreamingVoice Voice;
    Texture RenderTexture;
    AudioDevice AudioDevice;


    public HowToPlayState(RollAndCashGame game, GameState transitionState)
    {
        AudioDevice = game.AudioDevice;
        Game = game;
        GraphicsDevice = game.GraphicsDevice;
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

        RenderTexture = Texture.CreateTexture2D(GraphicsDevice, Dimensions.GAME_W, Dimensions.GAME_H, game.MainWindow.SwapchainFormat, TextureUsageFlags.ColorTarget);
    }

    public override void Start()
    {
        var sound = StreamingAudio.Lookup(StreamingAudio.tutorial_type_beat);
        Voice = AudioDevice.Obtain<StreamingVoice>(sound.Format);
        Voice.Load(sound);
        Voice.Play();
    }

    public override void Update(TimeSpan delta)
    {
        if (Game.Inputs.AnyPressed)
        {
            Game.SetState(TransitionState);
        }
    }

    public override void Draw(Window window, double alpha)
    {
        var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);

        if (swapchainTexture != null)
        {
            HiResSpriteBatch.Reset();

            var logoAnimation = SpriteAnimations.Screen_HowToPlay;
            var sprite = logoAnimation.Frames[0];
            HiResSpriteBatch.Add(
                new Vector3(0, 0, -1f),
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
        Voice.Unload();
        Voice.Dispose();
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
