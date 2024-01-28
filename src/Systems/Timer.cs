using System;
using GGJ2024.Components;
using GGJ2024.Content;
using MoonTools.ECS;

namespace GGJ2024.Systems;

public class Timer : MoonTools.ECS.System
{
	public Timer(World world) : base(world)
	{
	}

	public override void Update(TimeSpan delta)
	{
		var timerEntity = GetSingletonEntity<GameTimer>();
		var time = Get<GameTimer>(timerEntity).Time;

		time -= (float) delta.TotalSeconds;

		Set(timerEntity, new GameTimer(time));

		var timeSpan = new TimeSpan(0, 0, (int) time);
		var timeString = timeSpan.ToString(@"m\:ss"); // this is really bad for string memory usage but whatevsies lol -evan

		Set(timerEntity, new Text(Fonts.KosugiID, 24, timeString, MoonWorks.Graphics.Font.HorizontalAlignment.Center, MoonWorks.Graphics.Font.VerticalAlignment.Middle));
	}
}
