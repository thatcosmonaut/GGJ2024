using System;
using MoonWorks.Math.Float;
using MoonTools.ECS;
using MoonWorks.Graphics;
using GGJ2024.Utility;

namespace GGJ2024.Systems;

public class Motion : MoonTools.ECS.System
{
    MoonTools.ECS.Filter VelocityFilter;
    MoonTools.ECS.Filter RectFilter;

    public Motion(World world) : base(world)
    {
        VelocityFilter = FilterBuilder.Include<Position>().Include<Velocity>().Build();
        RectFilter = FilterBuilder.Include<Position>().Include<Rectangle>().Build();
    }

    Rectangle GetWorldRect(Position p, Rectangle r)
    {
        return new Rectangle(p.X + r.X, p.Y + r.Y, r.Width, r.Height);
    }

    bool CheckCollision(Entity e, Rectangle rect)
    {
        foreach (var other in RectFilter.Entities)
        {
            if (other != e)
            {
                var otherR = Get<Rectangle>(other);
                var otherP = Get<Position>(other);
                var otherRect = GetWorldRect(otherP, otherR);

                if (rect.Intersects(otherRect))
                {
                    return true;
                }

            }
        }

        return false;
    }

    (bool hit, Position pos) SweepTest(Entity e)
    {
        var v = Get<Velocity>(e);
        var p = Get<Position>(e);
        var r = Get<Rectangle>(e);

        var targetX = p.X + v.X;
        var targetY = p.Y + v.Y;

        var outX = p.X;
        var outY = p.Y;

        var xEnum = new IntegerEnumerator(p.X, (int)targetX);
        var yEnum = new IntegerEnumerator(p.Y, (int)targetY);

        bool xHit = false;
        bool yHit = false;

        foreach (var x in xEnum)
        {
            var newPos = new Position(x, p.Y);
            var rect = GetWorldRect(newPos, r);

            xHit = CheckCollision(e, rect);

            if (xHit) break;

            outX = x;
        }

        foreach (var y in yEnum)
        {
            var newPos = new Position(p.X, y);
            var rect = GetWorldRect(newPos, r);

            yHit = CheckCollision(e, rect);

            if (yHit) break;

            outY = y;
        }

        return (xHit || yHit, new Position(outX, outY));
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in VelocityFilter.Entities)
        {
            var pos = Get<Position>(entity);
            var vel = (Vector2)Get<Velocity>(entity);

            if (Has<Rectangle>(entity))
            {
                var result = SweepTest(entity);
                Set(entity, result.pos);
            }
            else
                Set(entity, pos + vel);
        }
    }
}