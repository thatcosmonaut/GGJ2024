using System;
using MoonWorks;
using MoonWorks.Audio;
using MoonWorks.Graphics;
using System.Numerics;
using RollAndCash.Content;

namespace RollAndCash.GameStates;

public class HowToPlayState : GameState
{
    RollAndCashGame Game;
    GraphicsDevice GraphicsDevice;
    GameState TransitionState;

    SpriteBatch HiResSpriteBatch;
    Sampler LinearSampler;

    Texture RenderTexture;
    AudioDevice AudioDevice;
    PersistentVoice MusicVoice;
    AudioDataQoa Music;

    float ForceTimer = 0;
    float MinTime = 2f;

    public HowToPlayState(RollAndCashGame game, GameState transitionState)
    {
        AudioDevice = game.AudioDevice;
        Game = game;
        GraphicsDevice = game.GraphicsDevice;
        TransitionState = transitionState;

        LinearSampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearClamp);
        HiResSpriteBatch = new SpriteBatch(GraphicsDevice, Game.RootTitleStorage, game.MainWindow.SwapchainFormat);

        RenderTexture = Texture.Create2D(GraphicsDevice, Dimensions.GAME_W, Dimensions.GAME_H, game.MainWindow.SwapchainFormat, TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler);
    }

    public override void Start()
    {
        if (MusicVoice == null)
        {
            Music = StreamingAudio.Lookup(StreamingAudio.tutorial_type_beat);
            Music.Loop = true;
            MusicVoice = AudioDevice.Obtain<PersistentVoice>(Music.Format);
        }

        Music.Seek(0);
        Music.SendTo(MusicVoice);
        MusicVoice.Play();
    }

    public override void Update(TimeSpan delta)
    {
        if (ForceTimer >= MinTime && Game.Inputs.AnyPressed)
        {
            Game.SetState(TransitionState);
        }

        ForceTimer += (float)delta.TotalSeconds;
    }

    public override void Draw(Window window, double alpha)
    {
        var commandBuffer = GraphicsDevice.AcquireCommandBuffer();

        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);

        if (swapchainTexture != null)
        {
            HiResSpriteBatch.Start();

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

            var renderPass = commandBuffer.BeginRenderPass(new ColorTargetInfo(RenderTexture, Color.Black));

            var hiResViewProjectionMatrices = new ViewProjectionMatrices(Matrix4x4.Identity, GetHiResProjectionMatrix());

            HiResSpriteBatch.Render(
                renderPass,
                TextureAtlases.TP_HiRes.Texture,
                LinearSampler,
                hiResViewProjectionMatrices
            );

            commandBuffer.EndRenderPass(renderPass);

            commandBuffer.Blit(RenderTexture, swapchainTexture, Filter.Nearest);

        }

        GraphicsDevice.Submit(commandBuffer);
    }

    public override void End()
    {
        Music.Disconnect();
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
