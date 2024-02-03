using System;
using MoonWorks.Math.Float;
using MoonTools.ECS;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Data;
using RollAndCash.Messages;
using MoonWorks.Graphics;

namespace RollAndCash.Systems;

public class SplashScreenSystem : MoonTools.ECS.System
{
    GameLoopManipulator GameLoopManipulator;

    public SplashScreenSystem(World world) : base(world)
    {
        GameLoopManipulator = new GameLoopManipulator(world);
    }

    public override void Update(TimeSpan delta)
    {
        var gameState = GetSingleton<GameState>();

        if (gameState != GameState.Splash) { return; }

        if (SomeMessage<Startup>())
        {
            var entity = CreateEntity();
            Set(entity, new Position(0, 0));
            Set(entity, new Rectangle(0, 0, 1920, 1080));
            Set(entity, new ColorBlend(Color.Black));
            Set(entity, new DrawAsRectangle());
            Set(entity, new SpriteAnimation(SpriteAnimations.Logo_MoonWorks));
            Set(entity, new Depth(1));
            Set(entity, new Timer(3));
            Set(entity, new HiResArt());

            Send(new PlayStaticSoundMessage(StaticAudio.MoonWorksChime));
        }

        if (!Some<Timer>())
        {
            Set(GetSingletonEntity<GameState>(), GameState.Game);
            GameLoopManipulator.ShowTitleScreen();
        }
    }
}
