using System;
using MoonTools.ECS;
using MoonWorks.Math.Float;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Messages;
using RollAndCash.Relations;
using RollAndCash.Systems;
using RollAndCash.Utility;

namespace RollAndCash.Systems;

public class DroneController : MoonTools.ECS.System
{
    float RestockDroneSpeed = 80;
    float EvilDroneSpeed = 200;

    Filter TargeterFilter;
    Filter ProductSpawnerFilter;
    Filter StealerFilter;

    DroneSpawner DroneSpawner;

    public DroneController(World world) : base(world)
    {
        TargeterFilter = FilterBuilder.Include<CanTargetProductSpawner>().Build();
        ProductSpawnerFilter = FilterBuilder.Include<Position>().Include<CanSpawn>().Build();
        StealerFilter = FilterBuilder.Include<CanStealProducts>().Build();

        DroneSpawner = new DroneSpawner(world);
    }

    public override void Update(TimeSpan delta)
    {
        // set restock timer on empty product spawners
        foreach (var productSpawner in ProductSpawnerFilter.Entities)
        {
            if (!HasInRelation<BelongsToProductSpawner>(productSpawner) && !Has<WaitingForProductRestock>(productSpawner))
            {
                Set(productSpawner, new WaitingForProductRestock());

                var restockTimerEntity = CreateEntity();
                Set(restockTimerEntity, new Timer(Rando.Range(0.5f, 2.5f)));
                Relate(productSpawner, restockTimerEntity, new RestockTimer());
            }
        }

        // check restock timer
        foreach (var productSpawner in ProductSpawnerFilter.Entities)
        {
            if (Has<WaitingForProductRestock>(productSpawner) && !HasOutRelation<RestockTimer>(productSpawner))
            {
                Remove<WaitingForProductRestock>(productSpawner);
                DroneSpawner.SpawnRestockDrone(productSpawner);
            }
        }

        // restock drone target procedure
        foreach (var targeter in TargeterFilter.Entities)
        {
            if (HasOutRelation<Holding>(targeter) && HasOutRelation<Targeting>(targeter))
            {
                var targetedSpawner = OutRelationSingleton<Targeting>(targeter);
                var product = OutRelationSingleton<Holding>(targeter);

                var targeterPosition = Get<Position>(targeter);
                var targetedPosition = Get<Position>(targetedSpawner);

                var vectorToTarget = targetedPosition + new Vector2(0, -15) - targeterPosition;
                var direction = Vector2.Normalize(vectorToTarget);
                var distance = MathF.Min(vectorToTarget.Length(), RestockDroneSpeed);

                Set(targeter, new Velocity(direction * distance));
                Set(targeter, new LastDirection(direction));

                if (distance < 5)
                {
                    // dont use hold so we drop in precise location
                    Remove<Velocity>(product);
                    Unrelate<Holding>(targeter, product);
                    Send(new PlayStaticSoundMessage(StaticAudio.PutDown));
                    Set(product, targetedPosition);

                    // fly off in a random direction
                    var randomDirection = Vector2.Rotate(Vector2.UnitX, Rando.Range(0, 2 * MathF.PI));
                    Set(targeter, new Velocity(randomDirection * RestockDroneSpeed));
                }
            }
        }

        foreach (var stealer in StealerFilter.Entities)
        {
            if (HasOutRelation<Targeting>(stealer))
            {
                if (!HasOutRelation<Holding>(stealer))
                {
                    // approach target
                    var target = OutRelationSingleton<Targeting>(stealer);

                    var stealerPosition = Get<Position>(stealer);
                    var targetPosition = Get<Position>(target);

                    var vectorToTarget = targetPosition + new Vector2(0, -15) - stealerPosition;
                    var distanceSquared = vectorToTarget.LengthSquared();

                    var direction = Vector2.Normalize(vectorToTarget);
                    Set(stealer, new Velocity(direction * EvilDroneSpeed));
                    Set(stealer, new LastDirection(direction));

                    // try to steal
                    if (distanceSquared < 9)
                    {
                        Set(stealer, new TryHold());
                        Send(new PlayStaticSoundMessage(StaticAudio.EvilDroneLaugh));
                    }
                }
            }
        }
    }
}
