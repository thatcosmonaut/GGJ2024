using System;
using MoonWorks.Math.Float;
using MoonTools.ECS;
using MoonWorks.Graphics;
using RollAndCash.Utility;
using System.Collections.Generic;
using RollAndCash.Components;
using MoonWorks;
using RollAndCash.Relations;

namespace RollAndCash.Systems;

public class Motion : MoonTools.ECS.System
{
    MoonTools.ECS.Filter VelocityFilter;
    MoonTools.ECS.Filter CanHoldFilter;
    MoonTools.ECS.Filter CanBeHeldFilter;
    MoonTools.ECS.Filter InteractFilter;
    MoonTools.ECS.Filter SolidFilter;

    Dictionary<(int, int), HashSet<Entity>> InteractSpatialHash = new();
    HashSet<Entity> InteractResults = new();

    Dictionary<(int, int), HashSet<Entity>> SolidSpatialHash = new();
    HashSet<Entity> SolidResults = new HashSet<Entity>();

    const int CellSize = 32;

    public Motion(World world) : base(world)
    {
        VelocityFilter = FilterBuilder.Include<Position>().Include<Velocity>().Build();
        CanHoldFilter = FilterBuilder.Include<Position>().Include<CanHold>().Build();
        CanBeHeldFilter = FilterBuilder.Include<Position>().Include<Rectangle>().Include<CanBeHeld>().Build();
        InteractFilter = FilterBuilder.Include<Position>().Include<Rectangle>().Include<CanInteract>().Build();
        SolidFilter = FilterBuilder.Include<Position>().Include<Rectangle>().Include<Solid>().Build();
    }

    void ClearCanBeHeldSpatialHash()
    {
        //don't remove the hashsets/clear the dict, we'll reuse them so we don't have to pressure the GC
        foreach (var (k, v) in InteractSpatialHash)
        {
            v.Clear();
        }
    }

    void ClearSolidSpatialHash()
    {
        foreach (var (k, v) in SolidSpatialHash)
        {
            v.Clear();
        }
    }

    (int, int) GetHashKey(int x, int y)
    {
        return (x / CellSize, y / CellSize);
    }

    void AddToHash(Dictionary<(int, int), HashSet<Entity>> hash, Entity e)
    {
        var pos = Get<Position>(e);
        var rect = Get<Rectangle>(e);
        var worldRect = GetWorldRect(pos, rect);

        for (var x = worldRect.X; x < worldRect.X + worldRect.Width; x++)
        {
            for (var y = worldRect.Y; y < worldRect.Y + worldRect.Height; y++)
            {
                var key = GetHashKey(x, y);
                if (!hash.ContainsKey(key))
                    hash.Add(key, new HashSet<Entity>());

                hash[key].Add(e);
            }
        }
    }

    void RetrieveFromHash(Dictionary<(int, int), HashSet<Entity>> hash, HashSet<Entity> results, Rectangle rect)
    {
        results.Clear();

        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            for (var y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                var key = GetHashKey(x, y);
                if (hash.ContainsKey(key))
                {
                    foreach (var e in hash[key])
                        results.Add(e);
                }
            }
        }
    }

    Rectangle GetWorldRect(Position p, Rectangle r)
    {
        return new Rectangle(p.X + r.X, p.Y + r.Y, r.Width, r.Height);
    }

    (Entity other, bool hit) CheckSolidCollision(Entity e, Rectangle rect)
    {
        RetrieveFromHash(SolidSpatialHash, SolidResults, rect);

        foreach (var other in SolidResults)
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

    Position SweepTest(Entity e, float dt)
    {
        var velocity = Get<Velocity>(e);
        var position = Get<Position>(e);
        var r = Get<Rectangle>(e);

        var movement = new Vector2(velocity.X, velocity.Y) * dt;
        var targetPosition = position + movement;

        var xEnum = new IntegerEnumerator(position.X, targetPosition.X);
        var yEnum = new IntegerEnumerator(position.Y, targetPosition.Y);

        int mostRecentValidXPosition = position.X;
        int mostRecentValidYPosition = position.Y;

        bool xHit = false;
        bool yHit = false;

        foreach (var x in xEnum)
        {
            var newPos = new Position(x, position.Y);
            var rect = GetWorldRect(newPos, r);

            (var other, var hit) = CheckSolidCollision(e, rect);

            xHit = hit;

            if (xHit && Has<Solid>(other) && Has<Solid>(e))
            {
                movement.X = mostRecentValidXPosition - position.X;
                position = position.SetX(position.X); // truncates x coord
                break;
            }

            mostRecentValidXPosition = x;
        }

        foreach (var y in yEnum)
        {
            var newPos = new Position(mostRecentValidXPosition, y);
            var rect = GetWorldRect(newPos, r);

            (var other, var hit) = CheckSolidCollision(e, rect);
            yHit = hit;

            if (yHit && Has<Solid>(other) && Has<Solid>(e))
            {
                movement.Y = mostRecentValidYPosition - position.Y;
                position = position.SetY(position.Y); // truncates y coord
                break;
            }

            mostRecentValidYPosition = y;
        }

        return position + movement;
    }

    public override void Update(TimeSpan delta)
    {
        ClearCanBeHeldSpatialHash();
        ClearSolidSpatialHash();

        foreach (var entity in InteractFilter.Entities)
        {
            if (HasInRelation<Holding>(entity))
                continue;

            AddToHash(InteractSpatialHash, entity);
        }

        foreach (var entity in InteractFilter.Entities)
        {
            foreach (var other in OutRelations<Colliding>(entity))
            {
                Unrelate<Colliding>(entity, other);
            }

            var position = Get<Position>(entity);
            var rect = GetWorldRect(position, Get<Rectangle>(entity));

            RetrieveFromHash(InteractSpatialHash, InteractResults, rect);

            foreach (var other in InteractResults)
            {
                if (other != entity)
                {
                    var otherR = Get<Rectangle>(other);
                    var otherP = Get<Position>(other);
                    var otherRect = GetWorldRect(otherP, otherR);

                    if (rect.Intersects(otherRect))
                    {
                        Relate(entity, other, new Colliding());
                    }
                }
            }
        }

        foreach (var entity in SolidFilter.Entities)
        {
            AddToHash(SolidSpatialHash, entity);
        }

        foreach (var entity in VelocityFilter.Entities)
        {
            var pos = Get<Position>(entity);
            var vel = (Vector2)Get<Velocity>(entity);

            if (Has<Rectangle>(entity))
            {
                var result = SweepTest(entity, (float)delta.TotalSeconds);
                Set(entity, result);
            }
            else
            {
                var scaledVelocity = vel * (float)delta.TotalSeconds;
                if (Has<ForceIntegerMovement>(entity))
                {
                    scaledVelocity = new Vector2((int)scaledVelocity.X, (int)scaledVelocity.Y);
                }
                Set(entity, pos + scaledVelocity);
            }

            if (Has<FallSpeed>(entity))
            {
                var fallspeed = Get<FallSpeed>(entity).Speed;
                Set(entity, new Velocity(vel + Vector2.UnitY * fallspeed));
            }

            if (Has<DestroyAtScreenBottom>(entity) && pos.Y > 500)
            {
                Destroy(entity);
            }
        }

        foreach (var entity in SolidFilter.Entities)
        {
            var position = Get<Position>(entity);
            var rectangle = Get<Rectangle>(entity);

            var leftPos = new Position(position.X - 1, position.Y);
            var rightPos = new Position(position.X + 1, position.Y);
            var upPos = new Position(position.X, position.Y - 1);
            var downPos = new Position(position.X, position.Y + 1);

            var leftRectangle = GetWorldRect(leftPos, rectangle);
            var rightRectangle = GetWorldRect(rightPos, rectangle);
            var upRectangle = GetWorldRect(upPos, rectangle);
            var downRectangle = GetWorldRect(downPos, rectangle);

            var (_, leftCollided) = CheckSolidCollision(entity, leftRectangle);
            var (_, rightCollided) = CheckSolidCollision(entity, rightRectangle);
            var (_, upCollided) = CheckSolidCollision(entity, upRectangle);
            var (_, downCollided) = CheckSolidCollision(entity, downRectangle);

            if (leftCollided || rightCollided || upCollided || downCollided)
            {
                Set(entity, new TouchingSolid());
            }
            else
            {
                Remove<TouchingSolid>(entity);
            }
        }
    }
}
