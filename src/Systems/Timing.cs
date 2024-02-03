using System;
using MoonTools.ECS;
using RollAndCash.Relations;
using Timer = RollAndCash.Components.Timer;

namespace RollAndCash.Systems;

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
                if (HasOutRelation<TeleportToAtTimerEnd>(entity))
                {
                    var outEntity = OutRelationSingleton<TeleportToAtTimerEnd>(entity);
                    var data = World.GetRelationData<TeleportToAtTimerEnd>(entity, outEntity);
                    var entityToTeleportTo = data.TeleportTo;
                    var position = Get<Position>(entityToTeleportTo);
                    Set(outEntity, position);
                }

                Destroy(entity);
                return;
            }

            Set(entity, timer with { Time = time });
        }
    }
}
