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

    Vector2[] Directions = new[]
    {
        Vector2.UnitX,
        Vector2.UnitY,
        -Vector2.UnitX,
        -Vector2.UnitY,
        Vector2.UnitX + Vector2.UnitY,
        Vector2.UnitX - Vector2.UnitY,
        -Vector2.UnitX + Vector2.UnitY,
        -Vector2.UnitX - Vector2.UnitY,
        Vector2.Zero
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
        Set(NPC, new Position(Dimensions.GAME_W * 0.5f, Dimensions.GAME_H * 0.33f));
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

        var wallDetector = World.CreateEntity();
        Set(wallDetector, Get<Position>(NPC) + Get<LastDirection>(NPC).Direction * NPCSpeed * 0.5f);
        Set(wallDetector, new Rectangle(0, 0, 1, 1));
        Set(wallDetector, new CanInteract());

        Relate(NPC, wallDetector, new HasWallDetector());

        return NPC;
    }

    public override void Update(TimeSpan delta)
    {
        if (Some<IsTitleScreen>())
            return;

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

            if (!HasOutRelation<Holding>(entity))
            {
                foreach (var other in OutRelations<Colliding>(entity))
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
            }
            else
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