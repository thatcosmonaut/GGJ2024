
using System;
using MoonWorks.Math.Float;
using MoonTools.ECS;
using RollAndCash.Components;
using RollAndCash.Data;
using RollAndCash.Messages;

namespace RollAndCash.Systems;

public class DirectionalAnimation : MoonTools.ECS.System
{
    MoonTools.ECS.Filter DirectionFilter;
    public DirectionalAnimation(World world) : base(world)
    {
        DirectionFilter = FilterBuilder
        .Include<LastDirection>()
        .Include<DirectionalSprites>()
        .Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in DirectionFilter.Entities)
        {
            var direction = Get<LastDirection>(entity).Direction;
            var animations = Get<DirectionalSprites>(entity);

            SpriteAnimationInfo animation;

            if (direction.X > 0)
            {
                if (direction.Y > 0)
                {
                    animation = SpriteAnimationInfo.FromID(animations.DownRight);
                }
                else if (direction.Y < 0)
                {
                    animation = SpriteAnimationInfo.FromID(animations.UpRight);
                }
                else
                {
                    animation = SpriteAnimationInfo.FromID(animations.Right);
                }
            }
            else if (direction.X < 0)
            {
                if (direction.Y > 0)
                {
                    animation = SpriteAnimationInfo.FromID(animations.DownLeft);
                }
                else if (direction.Y < 0)
                {
                    animation = SpriteAnimationInfo.FromID(animations.UpLeft);
                }
                else
                {
                    animation = SpriteAnimationInfo.FromID(animations.Left);
                }
            }
            else
            {
                if (direction.Y > 0)
                {
                    animation = SpriteAnimationInfo.FromID(animations.Down);
                }
                else if (direction.Y < 0)
                {
                    animation = SpriteAnimationInfo.FromID(animations.Up);
                }
                else
                {
                    animation = Get<SpriteAnimation>(entity).SpriteAnimationInfo;
                }
            }

            var velocity = Has<Velocity>(entity) ? (Vector2)Get<Velocity>(entity) : Vector2.Zero;

            int framerate = Get<SpriteAnimation>(entity).FrameRate;

            if (Has<AdjustFramerateToSpeed>(entity))
            {
                framerate = (int)(velocity.Length() / 20f);
                if (Has<FunnyRunTimer>(entity))
                {
                    framerate = 25;
                }
            }

            if (direction.LengthSquared() > 0)
            {
                Send(new SetAnimationMessage(
                    entity,
                    new SpriteAnimation(animation, framerate, true)
                ));
            }
            else
            {
                framerate = 0;
                Send(new SetAnimationMessage(
                    entity,
                    new SpriteAnimation(animation, framerate, true, 0),
                    true
                ));
            }
        }
    }

}
