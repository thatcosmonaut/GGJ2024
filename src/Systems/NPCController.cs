

using System;
using MoonTools.ECS;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using RollAndCash.Components;

namespace RollAndCash.Systems;

public class NPCController : MoonTools.ECS.System
{
    MoonTools.ECS.Filter NPCFilter;

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
        World.Set(NPC, new Position(Dimensions.GAME_W * 0.5f, Dimensions.GAME_H * 0.33f));
        World.Set(NPC, new SpriteAnimation(Content.SpriteAnimations.NPC_Bizazss_Walk_Down, 0));
        World.Set(NPC, new Rectangle(-8, -8, 16, 16));
        World.Set(NPC, new CanInteract());
        World.Set(NPC, new CanInspect());
        World.Set(NPC, new CanHold());
        World.Set(NPC, new Solid());
        World.Set(NPC, new Depth(5));
        World.Set(NPC, new MaxSpeed(128));
        World.Set(NPC, new Velocity(Vector2.Zero));
        World.Set(NPC, new LastDirection(Vector2.Zero));
        World.Set(NPC, new CanTalk());
        World.Set(NPC, new DirectionalSprites(
            Content.SpriteAnimations.NPC_Bizazss_Walk_Up.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_UpRight.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_Right.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_DownRight.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_Down.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_DownLeft.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_Left.ID,
            Content.SpriteAnimations.NPC_Bizazss_Walk_UpLeft.ID
        ));

        return NPC;
    }

    public override void Update(TimeSpan delta)
    {

        foreach (var entity in NPCFilter.Entities)
        {
            Set(entity, new LastDirection(-Vector2.UnitY));
            var depth = MathHelper.Lerp(100, 10, Get<Position>(entity).Y / (float)Dimensions.GAME_H);
            Set(entity, new Depth(depth));
        }
    }
}