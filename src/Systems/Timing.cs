using System;
using MoonTools.ECS;
using Timer = GGJ2024.Components.Timer;

namespace LD54.Systems;

public class Timing : MoonTools.ECS.System
{
    private Filter TimerFilter;

    public Timing(World world) : base(world)
    {
        TimerFilter = FilterBuilder
            .Include<Timer>()
            .Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in TimerFilter.Entities)
        {
            var timer = Get<Timer>(entity);
            var time = timer.Time - (float)delta.TotalSeconds;

            if (time <= 0)
            {
                Destroy(entity);
                return;
            }

            Set(entity, timer with { Time = time });
        }
    }
}
