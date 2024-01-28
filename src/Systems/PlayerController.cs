using GGJ2024.Components;
using GGJ2024.Data;
using GGJ2024.Messages;
using MoonTools.ECS;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace GGJ2024.Systems;

public class PlayerController : MoonTools.ECS.System
{
    MoonTools.ECS.Filter PlayerFilter;
    float Speed = 32f;

    public PlayerController(World world) : base(world)
    {
        PlayerFilter =
        FilterBuilder
        .Include<Player>()
        .Include<Position>()
        .Build();
    }

    public void SpawnPlayer(int index)
    {
        var player = World.CreateEntity();
        World.Set(player, new Position(Dimensions.GAME_W * 0.5f, Dimensions.GAME_H * 0.5f + index * 32.0f));
        World.Set(player, new SpriteAnimation(index == 0 ? Content.SpriteAnimations.Char_Walk_Down : Content.SpriteAnimations.Char2_Walk_Down, 0));
        World.Set(player, new Player(index, 0));
		World.Set(player, new Rectangle(-8, -8, 16, 16));
        World.Set(player, new CanHold());
        World.Set(player, new Solid());
        World.Set(player, index == 0 ? Color.Green : Color.Blue);
    }

    public override void Update(System.TimeSpan delta)
    {
        if (!Some<Player>())
        {
            SpawnPlayer(0);
            SpawnPlayer(1);
        }

        foreach (var entity in PlayerFilter.Entities)
        {
            var playerIndex = Get<Player>(entity).Index;
            var direction = Vector2.Zero;
            if (Has<TryHold>(entity))
                Remove<TryHold>(entity);

            foreach (var action in ReadMessages<Action>())
            {
                if (action.Index == playerIndex)
                {
                    if (action.ActionType == Actions.MoveX)
                    {
                        direction.X += action.Value;
                    }
                    else if (action.ActionType == Actions.MoveY)
                    {
                        direction.Y += action.Value;
                    }

                    if (action.ActionType == Actions.Interact && action.ActionState == ActionState.Pressed)
                    {
                        Set(entity, new TryHold());
                    }
                }
            }

			if (direction.LengthSquared() > 0)
			{
				direction = Vector2.Normalize(direction);
			}

			SpriteAnimationInfo animation;

			if (direction.X > 0)
			{
				if (direction.Y > 0)
				{
					animation = playerIndex == 0 ? Content.SpriteAnimations.Char_Walk_DownRight : Content.SpriteAnimations.Char2_Walk_DownRight;
				}
				else if (direction.Y < 0)
				{
					animation = playerIndex == 0 ? Content.SpriteAnimations.Char_Walk_UpRight : Content.SpriteAnimations.Char2_Walk_UpRight;
				}
				else
				{
					animation = playerIndex == 0 ? Content.SpriteAnimations.Char_Walk_Right : Content.SpriteAnimations.Char2_Walk_Right;
				}
			}
			else if (direction.X < 0)
			{
				if (direction.Y > 0)
				{
					animation = playerIndex == 0 ? Content.SpriteAnimations.Char_Walk_DownLeft : Content.SpriteAnimations.Char2_Walk_DownLeft;
				}
				else if (direction.Y < 0)
				{
					animation = playerIndex == 0 ? Content.SpriteAnimations.Char_Walk_UpLeft : Content.SpriteAnimations.Char2_Walk_UpLeft;
				}
				else
				{
					animation = playerIndex == 0 ? Content.SpriteAnimations.Char_Walk_Left : Content.SpriteAnimations.Char2_Walk_Left;
				}
			}
			else
			{
				if (direction.Y > 0)
				{
					animation = playerIndex == 0 ? Content.SpriteAnimations.Char_Walk_Down : Content.SpriteAnimations.Char2_Walk_Down;
				}
				else if (direction.Y < 0)
				{
					animation = playerIndex == 0 ? Content.SpriteAnimations.Char_Walk_Up : Content.SpriteAnimations.Char2_Walk_Down;
				}
				else
				{
					animation = Get<SpriteAnimation>(entity).SpriteAnimationInfo;
				}
			}

			var velocity = direction * Speed;

			int framerate = (int) (velocity.LengthSquared() / 200f);

			Send(new SetAnimationMessage(
				entity,
				new SpriteAnimation(animation, framerate)
			));

            Set(entity, new Velocity(velocity));
        }
    }
}