using System;
using System.IO;
using System.Text;
using MoonTools.ECS;
using MoonWorks.Graphics;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Data;
using RollAndCash.Messages;
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
    const float TalkTime = 5.0f;
    const int MaxTextWidth = 160;
    const int MaxNPCs = 4;

    string[] Dialogue;
    int DialogueIndex = 0;

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

    StaticSoundID[] BizassSoundz =
    [
        StaticAudio.Bizass_NPC1,
        StaticAudio.Bizass_NPC2,
        StaticAudio.Bizass_NPC3,
        StaticAudio.Bizass_NPC4,
        StaticAudio.Bizass_NPC5,
        StaticAudio.Bizass_NPC6,
        StaticAudio.Bizass_NPC7,
        StaticAudio.Bizass_NPC8
    ];

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

        var dialogueFilePath = Path.Combine(
            System.AppContext.BaseDirectory,
            "Content",
            "Data",
            "dialogue"
        );

        Dialogue = File.ReadAllLines(dialogueFilePath);
        Dialogue.Shuffle();
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
        Set(NPC, new AdjustFramerateToSpeed());
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

    public void Talk(Entity entity, Entity player)
    {
        if (HasOutRelation<CantTalk>(entity))
            return;

        var playerIndex = Get<Player>(player).Index;
        Send(new PlayStaticSoundMessage(Rando.GetRandomItem(BizassSoundz)));

        var index = 0;
        if (Some<IsPopupBox>())
        {
            // jank to push old boxes farther back
            foreach (var (_, uiElement) in Relations<ShowingPopup>())
            {
                if (Has<IsPopupBox>(uiElement))
                {
                    Set(uiElement, new Depth(8));
                }
                else
                {
                    Set(uiElement, new Depth(6));
                }
            }

            // newly created popups will draw on top of older ones
            index = 1;
        }

        var font = Fonts.FromID(Fonts.KosugiID);
        var position = Get<Position>(entity);

        var xOffset = position.X < Dimensions.GAME_W * 3 / 4 ? 10 : -100;
        var yOffset = position.Y > Dimensions.GAME_H * 3 / 4 ? -100 : -30;

        var dialogue = Dialogue[DialogueIndex].Split(' ');
        var builder = new StringBuilder();

        foreach (var word in dialogue)
        {
            builder.Append(word);
            builder.Append(" ");
            font.TextBounds(
                builder.ToString(),
                FontSizes.DIALOGUE,
                MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                MoonWorks.Graphics.Font.VerticalAlignment.Top,
                out var testBounds
            );
            if (testBounds.W > MaxTextWidth)
            {
                var builderIndex = builder.Length - (word.Length + 1);
                builder.Remove(builderIndex, word.Length + 1);

                var text = CreateEntity();
                Set(text, position + new Position(xOffset, yOffset));
                Set(text, new Text(Fonts.KosugiID, FontSizes.DIALOGUE, TextStorage.GetID(builder.ToString()), MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
                Set(text, new TextDropShadow(1, 1));
                Set(text, new Depth(6 - index * 4));
                Set(text, new Timer(TalkTime));
                Relate(entity, text, new ShowingPopup());
                yOffset += 15;
                builder.Clear();
                builder.Append(word);
                builder.Append(" ");
            }
        }

        if (builder.Length > 0)
        {
            var text = CreateEntity();
            Set(text, position + new Position(xOffset, yOffset));
            Set(text, new Text(Fonts.KosugiID, FontSizes.DIALOGUE, TextStorage.GetID(builder.ToString()), MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
            Set(text, new TextDropShadow(1, 1));
            Set(text, new Depth(6 - index * 4));
            Set(text, new Timer(TalkTime));
            Relate(entity, text, new ShowingPopup());
        }


        DialogueIndex++;
        DialogueIndex = DialogueIndex % Dialogue.Length;

        var timer = CreateEntity();
        Set(timer, new Timer(TalkTime));
        Relate(entity, timer, new CantTalk());
    }

    private static Rectangle TextRectangle(WellspringCS.Wellspring.Rectangle textBounds, Position position)
    {
        return new Rectangle((int)textBounds.X + position.X, (int)textBounds.Y + position.Y, (int)textBounds.W, (int)textBounds.H);
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

            if (HasOutRelation<TouchingSolid>(entity) || HasInRelation<TouchingSolid>(entity))
            {
                direction = Vector2.Normalize(Directions.GetRandomItem());

                foreach (var other in OutRelations<TouchingSolid>(entity))
                {
                    if (Has<Player>(other))
                    {
                        Talk(entity, other);
                    }
                }
                foreach (var other in InRelations<TouchingSolid>(entity))
                {
                    if (Has<Player>(other))
                    {
                        Talk(entity, other);
                    }
                }
            }

            if (!HasOutRelation<Colliding>(entity))
            {
                UnrelateAll<ConsideredProduct>(entity);
            }

            if (HasOutRelation<ShowingPopup>(entity))
            {
                foreach (var popup in OutRelations<ShowingPopup>(entity))
                {
                    var timer = Get<Timer>(popup);
                    float t = Easing.OutExpo(timer.Remaining);
                    Set(popup, new Color(t, t, t, t));
                }
            }

            bool destroyed = false;

            foreach (var other in OutRelations<Colliding>(entity))
            {
                if (!HasOutRelation<Holding>(entity))
                {
                    if (Has<CanBeHeld>(other) && !Related<ConsideredProduct>(entity, other))
                    {
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
                Set(entity, new Velocity(direction * NPCSpeed));
                Set(entity, new LastDirection(direction));

                var depth = MathHelper.Lerp(100, 10, Get<Position>(entity).Y / (float)Dimensions.GAME_H);
                Set(entity, new Depth(depth));
            }
        }
    }
}
