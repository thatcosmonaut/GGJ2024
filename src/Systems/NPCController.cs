using System;
using MoonTools.ECS;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using RollAndCash.Components;
using RollAndCash.Data;
using RollAndCash.Relations;
using RollAndCash.Utility;

namespace RollAndCash.Systems;

public class NPCController : MoonTools.ECS.System
{
    MoonTools.ECS.Filter NPCFilter;
    const float NPCSpeed = 64.0f;
    const float PickUpChance = 0.5f;
    const float MinSpawnTime = 3.0f;
    const float MaxSpawnTime = 10.0f;
    const float MinTimeInStore = 5.0f;
    const float LeaveStoreChance = 0.66f;
    const int MaxNPCs = 4;

    Vector2[] Directions = new[]
    {
        Vector2.UnitX,
        Vector2.UnitY,
        -Vector2.UnitX,
        -Vector2.UnitY,
        Vector2.UnitX + Vector2.UnitY,
        Vector2.UnitX - Vector2.UnitY,
        -Vector2.UnitX + Vector2.UnitY,
        -Vector2.UnitX - Vector2.UnitY
    };

    public NPCController(World world) : base(world)
    {
        NPCFilter =
            FilterBuilder
            .Include<Position>()
            .Include<SpriteAnimation>()
            .Include<Rectangle>()
            .Include<Solid>()
            .Include<CanTalk>()
            .Exclude<Player>()
            .Include<DirectionalSprites>()
            .Build();
    }
    public Entity SpawnNPC()
    {
        var NPC = World.CreateEntity();
        Set(NPC, new Position(Dimensions.GAME_W * 0.5f, Dimensions.GAME_H * 0.25f));
        Set(NPC, new SpriteAnimation(Content.SpriteAnimations.NPC_Bizazss_Walk_Down, 0));
        Set(NPC, new Rectangle(-8, -8, 16, 16));
        Set(NPC, new CanInteract());
        Set(NPC, new CanHold());
        Set(NPC, new Solid());
        Set(NPC, new Depth(5));
        Set(NPC, new MaxSpeed(128));
        Set(NPC, new Velocity(Vector2.Zero));
        Set(NPC, new LastDirection(Vector2.UnitY));
        Set(NPC, new CanTalk());
        Set(NPC, new DirectionalSprites(
            Content.SpriteAnimations.NPC_Bizazss_Walk_Up.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_UpRight.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_Right.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_DownRight.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_Down.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_DownLeft.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_Left.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_UpLeft.ID
        ));

        var timer = CreateEntity();
        Set(timer, new Timer(MinTimeInStore));
        Relate(NPC, timer, new CantLeaveStore());

        return NPC;
    }

    public override void Update(TimeSpan delta)
    {
        if (Some<IsTitleScreen>())
        {
            foreach (var npc in NPCFilter.Entities)
            {
                Destroy(npc);
            }
            return;
        }

        if (!Some<DontSpawnNPCs>() && NPCFilter.Count < MaxNPCs)
        {
            SpawnNPC();

            var time = Rando.Range(MinSpawnTime, MaxSpawnTime);
            var timer = CreateEntity();
            Set(timer, new DontSpawnNPCs());
            Set(timer, new Timer(time));
        }

        float deltaTime = (float)delta.TotalSeconds;

        foreach (var entity in NPCFilter.Entities)
        {
            var direction = Get<LastDirection>(entity).Direction;
            var position = Get<Position>(entity);

            if (Has<TryHold>(entity))
                Remove<TryHold>(entity);

            if (Has<TouchingSolid>(entity))
            {
                direction = Vector2.Normalize(Directions.GetRandomItem());
            }

            if (!HasOutRelation<Colliding>(entity))
            {
                UnrelateAll<ConsideredProduct>(entity);
            }

            bool destroyed = false;

            foreach (var other in OutRelations<Colliding>(entity))
            {
                if (!HasOutRelation<Holding>(entity))
                {
                    if (Has<CanBeHeld>(other) && !Related<ConsideredProduct>(entity, other))
                    {
                        System.Console.WriteLine(TextStorage.GetString(Get<Name>(other).TextID));
                        if (Rando.Value <= PickUpChance)
                        {
                            Set(entity, new TryHold());
                        }

                        Relate(entity, other, new ConsideredProduct());
                    }
                }

                if (Has<StoreExit>(other) && !HasOutRelation<CantLeaveStore>(entity) && Rando.Value <= LeaveStoreChance)
                {
                    destroyed = true;
                    if (HasOutRelation<Holding>(entity))
                        Destroy(OutRelationSingleton<Holding>(entity));

                    Destroy(entity);
                }
            }

            if (!destroyed)
            {
                if (HasOutRelation<Holding>(entity))
                {
                    UnrelateAll<BelongsToProductSpawner>(OutRelationSingleton<Holding>(entity));
                }

                Set(entity, new Velocity(direction * NPCSpeed));
                Set(entity, new LastDirection(direction));

                var depth = MathHelper.Lerp(100, 10, Get<Position>(entity).Y / (float)Dimensions.GAME_H);
                Set(entity, new Depth(depth));
            }
        }
    }
}