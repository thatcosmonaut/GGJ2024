using System;
using RollAndCash.Components;
using RollAndCash.Messages;
using MoonTools.ECS;

namespace RollAndCash.Systems;

public class SetSpriteAnimationSystem : MoonTools.ECS.System
{

	public SetSpriteAnimationSystem(World world) : base(world)
	{
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
	}
}
