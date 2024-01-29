using System;
using RollAndCash.Components;
using MoonTools.ECS;

namespace RollAndCash.Systems;

public class ColorAnimation : MoonTools.ECS.System
{
	MoonTools.ECS.Filter ColorAnimationFilter;

	public ColorAnimation(World world) : base(world)
	{
		ColorAnimationFilter = FilterBuilder.Include<ColorBlend>().Include<ColorSpeed>().Build();
	}

	public override void Update(TimeSpan delta)
	{
		var dt = (float)delta.TotalSeconds;

		foreach (var colorAnimationEntity in ColorAnimationFilter.Entities)
		{
			var color = Get<ColorBlend>(colorAnimationEntity).Color;
			var colorSpeed = Get<ColorSpeed>(colorAnimationEntity);

			var newColor = new MoonWorks.Graphics.Color(
				((color.R / 255f) + colorSpeed.RedSpeed * dt) % 1f,
				((color.G / 255f) + colorSpeed.GreenSpeed * dt) % 1f,
				((color.B / 255f) + colorSpeed.BlueSpeed * dt) % 1f
			);

			Set(colorAnimationEntity, new ColorBlend(newColor));
		}
	}
}
