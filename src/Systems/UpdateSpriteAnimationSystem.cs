using System;
using GGJ2024.Components;
using MoonTools.ECS;

namespace GGJ2024.Systems;

public class UpdateSpriteAnimationSystem : MoonTools.ECS.System
{
	Filter SpriteAnimationFilter;

	public UpdateSpriteAnimationSystem(World world) : base(world)
	{
		SpriteAnimationFilter = FilterBuilder
			.Include<SpriteAnimation>()
			.Build();
	}

	public override void Update(TimeSpan delta)
	{
		foreach (var entity in SpriteAnimationFilter.Entities)
		{
			UpdateSpriteAnimation(entity, (float)delta.TotalSeconds);
		}
	}

	public void UpdateSpriteAnimation(Entity entity, float dt)
	{
		var spriteAnimation = Get<SpriteAnimation>(entity).Update(dt);
		Set(entity, spriteAnimation);

		if (spriteAnimation.Finished)
		{
			/*
			if (Has<DestroyOnAnimationFinish>(entity))
			{
				Destroy(entity);
			}
			*/
		}
	}
}
