using System;
using MoonTools.ECS;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using RollAndCash;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Messages;
using RollAndCash.Relations;
using RollAndCash.Systems;
using RollAndCash.Utility;

namespace GGJ2024.Systems;

public class DroneController : MoonTools.ECS.System
{
    float DroneSpeed = 80;

    ProductSpawner Product;

    Filter TargeterFilter;
    Filter ProductSpawnerFilter;

    StaticSoundID[] DroneSounds =
    [
        StaticAudio.Drone1,
        StaticAudio.Drone2,
        StaticAudio.Drone3,
        StaticAudio.Drone4,
        StaticAudio.Drone5,
        StaticAudio.Drone6,
        StaticAudio.Drone7,
        StaticAudio.Drone8,
        StaticAudio.Drone9,
        StaticAudio.Drone10,
        StaticAudio.Drone11,
        StaticAudio.Drone12,
        StaticAudio.Drone13,
        StaticAudio.Drone14,
        StaticAudio.Drone15,
        StaticAudio.Drone16
    ];

    public DroneController(World world) : base(world)
    {
        TargeterFilter = FilterBuilder.Include<CanTargetProductSpawner>().Build();
        ProductSpawnerFilter = FilterBuilder.Include<Position>().Include<CanSpawn>().Build();

        Product = new ProductSpawner(world);
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
                SpawnDrone(productSpawner);
            }
        }

        // drone target procedure
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
                var distance = MathF.Min(vectorToTarget.Length(), DroneSpeed);

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
                    Set(targeter, new Velocity(randomDirection * DroneSpeed));
                }
            }
        }
    }

    public void SpawnDrone(Entity emptyProductSpawner)
    {
        // spawn in random border position
        var xPosition = Rando.IntInclusive(0, 1) == 0 ? Rando.IntInclusive(-75, -25) : Rando.IntInclusive(Dimensions.GAME_W + 25, Dimensions.GAME_W + 75);
        var yPosition = Rando.IntInclusive(-25, Dimensions.GAME_H - 50);
        var position = new Position(xPosition, yPosition);

        var drone = World.CreateEntity();
        Set(drone, position);
        Set(drone, new SpriteAnimation(SpriteAnimations.NPC_Drone_Fly_Down, 60));
        Set(drone, new Rectangle(-8, -8, 16, 16));
        Set(drone, new CanInteract());
        Set(drone, new CanHold());
        Set(drone, new Depth(5));
        Set(drone, new DirectionalSprites(
            SpriteAnimations.NPC_Drone_Fly_Up.ID,
            SpriteAnimations.NPC_Drone_Fly_UpRight.ID,
            SpriteAnimations.NPC_Drone_Fly_Right.ID,
            SpriteAnimations.NPC_Drone_Fly_DownRight.ID,
            SpriteAnimations.NPC_Drone_Fly_Down.ID,
            SpriteAnimations.NPC_Drone_Fly_DownLeft.ID,
            SpriteAnimations.NPC_Drone_Fly_Left.ID,
            SpriteAnimations.NPC_Drone_Fly_UpLeft.ID
        ));
        Set(drone, new CanTargetProductSpawner());
        Set(drone, new Velocity(Vector2.Zero));
        Set(drone, new DestroyWhenOutOfBounds());

        // spawn product related to spawner
        Entity product;
        if (Has<SpawnCategory>(emptyProductSpawner))
        {
            product = Product.SpawnProduct(position, Get<SpawnCategory>(emptyProductSpawner).Category);
        }
        else
        {
            product = Product.SpawnRandomProduct(position);
        }

        Relate(drone, product, new Holding());
        Relate(drone, emptyProductSpawner, new Targeting());
        Relate(product, emptyProductSpawner, new BelongsToProductSpawner());

        Send(new PlayStaticSoundMessage(Rando.GetRandomItem(DroneSounds), Data.SoundCategory.Drone));
    }
}
