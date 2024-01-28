using System;
using MoonWorks.Math.Float;
using MoonTools.ECS;
using MoonWorks.Graphics;
using GGJ2024.Utility;
using System.Collections.Generic;
using System.Linq;
using GGJ2024.Components;

namespace GGJ2024.Systems;

public class Motion : MoonTools.ECS.System
{
    MoonTools.ECS.Filter VelocityFilter;
    MoonTools.ECS.Filter RectFilter;

    Dictionary<(int, int), HashSet<Entity>> SpatialHash = new();
    HashSet<Entity> Results = new();
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

        for (var x = worldRect.X; x < worldRect.X + worldRect.Width; x++)
        {
            for (var y = worldRect.Y; y < worldRect.Y + worldRect.Height; y++)
            {
                var key = GetHashKey(x, y);
                if (!SpatialHash.ContainsKey(key))
                    SpatialHash.Add(key, new HashSet<Entity>());

                SpatialHash[key].Add(e);
            }
        }
    }

    void RetrieveFromHash(Rectangle rect)
    {
        Results.Clear();

        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            for (var y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                var key = GetHashKey(x, y);
                if (SpatialHash.ContainsKey(key))
                {
                    foreach (var e in SpatialHash[key])
                        Results.Add(e);
                }
            }
        }
    }

    Rectangle GetWorldRect(Position p, Rectangle r)
    {
        return new Rectangle(p.X + r.X, p.Y + r.Y, r.Width, r.Height);
    }

    (Entity other, bool hit) CheckCollision(Entity e, Rectangle rect)
    {
        RetrieveFromHash(rect);

        foreach (var other in Results)
        {
            if (other != e)
            {
                var otherR = Get<Rectangle>(other);
                var otherP = Get<Position>(other);
                var otherRect = GetWorldRect(otherP, otherR);

                if (rect.Intersects(otherRect))
                {
                    return (other, true);
                }

            }
        }

        return (default, false);
    }

    Position SweepTest(Entity e)
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

        var (staticOther, staticHit) = CheckCollision(e, GetWorldRect(p, r));
        if (staticHit && !Related<Colliding>(e, staticOther) && !Related<Colliding>(staticOther, e))
            Relate(e, staticOther, new Colliding());

        foreach (var x in xEnum)
        {
            var newPos = new Position(x, p.Y);
            var rect = GetWorldRect(newPos, r);

            (var other, var hit) = CheckCollision(e, rect);
            if (!Related<Colliding>(e, other) && !Related<Colliding>(other, e))
                Relate(e, other, new Colliding());

            xHit = hit;

            if (xHit && Has<Solid>(other) && Has<Solid>(e)) break;

            outX = x;
        }

        foreach (var y in yEnum)
        {
            var newPos = new Position(p.X, y);
            var rect = GetWorldRect(newPos, r);

            (var other, var hit) = CheckCollision(e, rect);
            yHit = hit;

            if (!Related<Colliding>(e, other) && !Related<Colliding>(other, e))
                Relate(e, other, new Colliding());

            if (yHit && Has<Solid>(other) && Has<Solid>(e)) break;

            outY = y;
        }

        return new Position(outX, outY);
    }

    public override void Update(TimeSpan delta)
    {
        ClearSpatialHash();

        foreach (var entity in RectFilter.Entities)
        {
            if (HasOutRelation<Colliding>(entity))
            {
                foreach (var o in OutRelations<Colliding>(entity))
                    Unrelate<Colliding>(entity, o);
            }

            if (HasInRelation<Holding>(entity))
                continue;

            AddToHash(entity);
        }

        foreach (var entity in VelocityFilter.Entities)
        {
            var pos = Get<Position>(entity);
            var vel = (Vector2)Get<Velocity>(entity);

            if (Has<Rectangle>(entity))
            {
                var result = SweepTest(entity);
                Set(entity, result);
            }
            else
                Set(entity, pos + vel * (float)delta.TotalSeconds);
        }
    }
}