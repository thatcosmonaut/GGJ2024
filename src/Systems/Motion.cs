using System;
using MoonWorks.Math.Float;
using MoonTools.ECS;
using MoonWorks.Graphics;
using GGJ2024.Utility;
using System.Collections.Generic;

namespace GGJ2024.Systems;

public class Motion : MoonTools.ECS.System
{
    MoonTools.ECS.Filter VelocityFilter;
    MoonTools.ECS.Filter RectFilter;

    Dictionary<(int, int), HashSet<Entity>> SpatialHash = new();
    const int CellSize = 32;

    public Motion(World world) : base(world)
    {
        VelocityFilter = FilterBuilder.Include<Position>().Include<Velocity>().Build();
        RectFilter = FilterBuilder.Include<Position>().Include<Rectangle>().Build();
    }

    void ClearSpatialHash()
    {
        //don't remove the hashsets/clear the dict, we'll reuse them so we don't have to pressure the GC
        foreach (var (k, v) in SpatialHash)
        {
            SpatialHash[k].Clear();
        }
    }

    (int, int) GetHashKey(int x, int y)
    {
        return (x / CellSize, y / CellSize);
    }

    void AddToHash(Entity e)
    {
        var pos = Get<Position>(e);
        var rect = Get<Rectangle>(e);
        var worldRect = GetWorldRect(pos, rect);

        for (var x = worldRect.X; x < worldRect.X + worldRect.Width; x += CellSize)
        {
            for (var y = worldRect.Y; y < worldRect.Y + worldRect.Height; y += CellSize)
            {
                var key = GetHashKey(x, y);
                if (!SpatialHash.ContainsKey(key))
                    SpatialHash.Add(key, new HashSet<Entity>());

                SpatialHash[key].Add(e);
            }
        }
    }

    IEnumerable<Entity> RetrieveFromHash(Rectangle rect)
    {
        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            for (var y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                var key = GetHashKey(x, y);
                if (SpatialHash.ContainsKey(key))
                {
                    foreach (var e in SpatialHash[key])
                        yield return e;
                }
            }
        }
    }

    Rectangle GetWorldRect(Position p, Rectangle r)
    {
        return new Rectangle(p.X + r.X, p.Y + r.Y, r.Width, r.Height);
    }

    bool CheckCollision(Entity e, Rectangle rect)
    {
        foreach (var other in RetrieveFromHash(rect))
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

        var dx = v.X > 0 ? MathF.Ceiling(v.X) : MathF.Floor(v.X);
        var dy = v.Y > 0 ? MathF.Ceiling(v.Y) : MathF.Floor(v.Y);

        var targetX = p.X + dx;
        var targetY = p.Y + dy;

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
        ClearSpatialHash();

        foreach (var entity in RectFilter.Entities)
            AddToHash(entity);

        foreach (var entity in VelocityFilter.Entities)
        {
            var pos = Get<Position>(entity);
            var vel = (Vector2)Get<Velocity>(entity);

            if (Has<Rectangle>(entity) && vel.LengthSquared() > 0)
            {
                var result = SweepTest(entity);
                Set(entity, result.pos);
            }
            else
                Set(entity, pos + vel);
        }
    }
}