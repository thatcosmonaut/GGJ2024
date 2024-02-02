using System;
using System.Security.Cryptography.X509Certificates;
using RollAndCash.Components;
using RollAndCash.Messages;
using MoonTools.ECS;

namespace RollAndCash.Systems;

public class SetSpriteAnimationSystem : MoonTools.ECS.System
{

	MoonTools.ECS.Filter SlowDownAnimationFilter;

	public SetSpriteAnimationSystem(World world) : base(world)
	{
		SlowDownAnimationFilter = FilterBuilder.Include<SlowDownAnimation>().Include<Position>().Build();
	}

	public override void Update(TimeSpan delta)
	{
		foreach (var message in ReadMessages<SetAnimationMessage>())
		{
			if (Has<SpriteAnimation>(message.Entity))
			{
				var currentAnimation = Get<SpriteAnimation>(message.Entity);

				if (currentAnimation.SpriteAnimationInfoID ==
					message.Animation.SpriteAnimationInfoID)
				{
					if (currentAnimation.FrameRate != message.Animation.FrameRate)
					{
						Set(message.Entity, currentAnimation.ChangeFramerate(message.Animation.FrameRate));
					}
					else if (message.ForceUpdate)
					{
						Set(message.Entity, message.Animation);
					}
				}
				else
				{
					Set(message.Entity, message.Animation);
				}
			}
			else
			{
				Set(message.Entity, message.Animation);
			}
		}

		// Slows down item animation
		foreach (var entity in SlowDownAnimationFilter.Entities)
		{
			var c = Get<SlowDownAnimation>(entity);
			var goal = c.BaseSpeed;
			var step = c.step;
			var currentAnimation = Get<SpriteAnimation>(entity);
			var frameRate = currentAnimation.FrameRate;
			frameRate = Math.Max(frameRate - step, goal);
			Set(entity, currentAnimation.ChangeFramerate(frameRate));
		}
	}
}
