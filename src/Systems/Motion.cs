using System;
using MoonWorks.Math.Float;
using MoonTools.ECS;

namespace GGJ2024.Systems;

public class Motion : MoonTools.ECS.System
{
    Filter VelocityFilter;

    public Motion(World world) : base(world)
    {
        VelocityFilter = FilterBuilder.Include<Position>().Include<Velocity>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in VelocityFilter.Entities)
        {
            var pos = Get<Position>(entity);
            var vel = (Vector2)Get<Velocity>(entity);
            Set(entity, pos + vel);
        }
    }
}