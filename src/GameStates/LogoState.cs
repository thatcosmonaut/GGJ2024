using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.Audio;
using MoonWorks.Graphics;
using MoonWorks.Math;
using RollAndCash.Content;

namespace RollAndCash.GameStates;

public class LogoState : GameState
{
    RollAndCashGame Game;
    GraphicsDevice GraphicsDevice;
    AudioDevice AudioDevice;
    GameState TransitionStateA;
    GameState TransitionStateB;

    SpriteBatch HiResSpriteBatch;
    Sampler LinearSampler;
    PersistentVoice Voice;

    float Fade = 0;
    float FadeTimer = 0;

    float FadeInDuration = 0.2f;
    float FadeHoldDuration = 2f;
    float FadeOutDuration = 2f;

    bool SoundPlayed = false;

    public LogoState(RollAndCashGame game, GameState transitionStateA, GameState transitionStateB)
    {
        Game = game;
        GraphicsDevice = game.GraphicsDevice;
        AudioDevice = game.AudioDevice;
        TransitionStateA = transitionStateA;
        TransitionStateB = transitionStateA;

        LinearSampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearClamp);
        HiResSpriteBatch = new SpriteBatch(GraphicsDevice, game.MainWindow.SwapchainFormat);
    }

    public override void Start()
    {
		Fade = 0;
    	FadeTimer = 0;
        SoundPlayed = false;

        if (Voice == null)
        {
            var sound = StaticAudio.Lookup(StaticAudio.MoonWorksChime);
            Voice = AudioDevice.Obtain<PersistentVoice>(sound.Format);
        }
    }

    public override void Update(TimeSpan delta)
    {
        Fade = Easing.AttackHoldRelease(
            0,
            1,
            0,
            FadeTimer,
            FadeInDuration,
            Easing.Function.Float.InQuad,
            FadeHoldDuration,
            FadeOutDuration,
            Easing.Function.Float.InQuad
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
        else if (FadeTimer > FadeInDuration + FadeHoldDuration + FadeOutDuration + 0.5f)
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
            HiResSpriteBatch.Start();

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

            var renderPass = commandBuffer.BeginRenderPass(new ColorTargetInfo(swapchainTexture, Color.Black));

            var hiResViewProjectionMatrices = new ViewProjectionMatrices(Matrix4x4.Identity, GetHiResProjectionMatrix());

            HiResSpriteBatch.Render(
                renderPass,
                TextureAtlases.TP_HiRes.Texture,
                LinearSampler,
                hiResViewProjectionMatrices
            );

            commandBuffer.EndRenderPass(renderPass);
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
