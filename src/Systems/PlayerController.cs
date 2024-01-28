using System;
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
	float MaxSpeedBase = 128f;

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
		World.Set(player, new Depth(5));
		World.Set(player, new MaxSpeed(128));
		World.Set(player, new Velocity(Vector2.Zero));
	}

	public override void Update(System.TimeSpan delta)
	{
		var deltaTime = delta.Milliseconds / 10f;

		foreach (var entity in PlayerFilter.Entities)
		{
			var playerIndex = Get<Player>(entity).Index;
			var direction = Vector2.Zero;
			if (Has<TryHold>(entity))
				Remove<TryHold>(entity);

			#region Input
			var inputState = Get<InputState>(entity);

			if (inputState.Left.IsDown)
			{
				direction.X = -1;
			}
			else if (inputState.Right.IsDown)
			{
				direction.X = 1;
			}

			if (inputState.Up.IsDown)
			{
				direction.Y = -1;
			}
			else if (inputState.Down.IsDown)
			{
				direction.Y = 1;
			}
			#endregion

			if (inputState.Interact.IsPressed)
			{
				Set(entity, new TryHold());
			}

			// Movement
			var velocity = Get<Velocity>(entity).Value;
			velocity += direction * 64f * deltaTime;
			var maxSpeed = Get<MaxSpeed>(entity).Value;
			if (direction.LengthSquared() > 0)
			{
				direction = Vector2.Normalize(direction);
				maxSpeed = Math.Min(maxSpeed + (deltaTime), 300);
				Set(entity, new MaxSpeed(maxSpeed));
			}
			else
			{
				var speed = Get<Velocity>(entity).Value.Length();
				speed = Math.Max(speed - (10 * deltaTime), 0);
				velocity = Vector2.Normalize(velocity) * speed;
				Set(entity, new MaxSpeed(MaxSpeedBase));
			}

			#region Animation
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
					animation = playerIndex == 0 ? Content.SpriteAnimations.Char_Walk_Up : Content.SpriteAnimations.Char2_Walk_Up;
				}
				else
				{
					animation = Get<SpriteAnimation>(entity).SpriteAnimationInfo;
				}
			}
			#endregion

			// limit max speed
			if (velocity.Length() > maxSpeed)
			{
				velocity = Vector2.Normalize(velocity) * maxSpeed;
			}

			int framerate = (int)(velocity.Length() / 20f);

			if (direction.LengthSquared() > 0)
			{
				Send(new SetAnimationMessage(
					entity,
					new SpriteAnimation(animation, framerate, true)
				));
			}
			else
			{
				Send(new SetAnimationMessage(
					entity,
					new SpriteAnimation(animation, 0, true, 0),
					true
				));
			}

			Set(entity, new Velocity(velocity));
		}
	}
}
