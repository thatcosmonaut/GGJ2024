using System;
using System.IO.IsolatedStorage;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Messages;
using RollAndCash.Relations;
using RollAndCash.Utility;
using MoonTools.ECS;
using MoonWorks.Math.Float;

namespace RollAndCash.Systems;

public class Orders : MoonTools.ECS.System
{
    Filter CategoryFilter;
    Filter IngredientFilter;
    Filter OrderFilter;
    Filter PlayerFilter;
    Filter NPCFilter;
    Product ProductManipulator;

    public Orders(World world) : base(world)
    {
        CategoryFilter = FilterBuilder.Include<Category>().Build();
        IngredientFilter = FilterBuilder.Include<Ingredient>().Build();
        OrderFilter = FilterBuilder.Include<IsOrder>().Build();
        PlayerFilter = FilterBuilder.Include<Player>().Include<CanHold>().Build();
        ProductManipulator = new Product(world);
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

    public void SetNewOrderDetails(Entity order)
    {
        foreach (var categoryRequirement in OutRelations<RequiresCategory>(order))
        {
            Unrelate<RequiresCategory>(order, categoryRequirement);
        }

        foreach (var ingredientRequirement in OutRelations<RequiresIngredient>(order))
        {
            Unrelate<RequiresIngredient>(order, ingredientRequirement);
        }

        if (Rando.Value <= 0.5f)
        { // require category
            var category = CategoryFilter.RandomEntity;
            Relate(order, category, new RequiresCategory());
            Set(order, new Text(Fonts.KosugiID, 10, Get<Category>(category).ToString(), MoonWorks.Graphics.Font.HorizontalAlignment.Center));
        }
        else
        { // require ingredient
            var ingredient = IngredientFilter.RandomEntity;
            Relate(order, ingredient, new RequiresIngredient());
            Set(order, new Text(Fonts.KosugiID, 10, Get<Ingredient>(ingredient).ToString(), MoonWorks.Graphics.Font.HorizontalAlignment.Center));
        }
    }

    private int CalculateScore(Entity product)
    {
        var score = 0;

        foreach (var productIngredientEntity in OutRelations<HasIngredient>(product))
        {
            var price = Get<Price>(productIngredientEntity).Value;
            var ingredientScore = 50 - price;
            score += (int)ingredientScore;
        }

        return score;
    }

    public bool TryFillOrder(Entity player)
    {
        var product = OutRelationSingleton<Holding>(player);

        var (order, filled) = CheckOrders(product);

        if (filled)
        {
            var scoreEntity = OutRelationSingleton<HasScore>(player);
            var calculate = CalculateScore(product);
            var score = Get<Score>(scoreEntity).Value + calculate;

            Set(scoreEntity, new Score(score));
            Set(scoreEntity, new Text(Fonts.KosugiID, 8, score.ToString()));

            if (calculate < 0)
            {
                Send(new PlayStaticSoundMessage(StaticAudio.CursedCoin));
            }
            else
            {
                Send(new PlayStaticSoundMessage(StaticAudio.OrderComplete));
            }

            SetNewOrderDetails(order); // refill order
            Destroy(product);

            var position = Get<Position>(player);
            for (var i = 0; i < 5; i++)
            {
                var anim = Content.SpriteAnimations.Effect_SpinningCoin;
                if (calculate < 0)
                {
                    anim = Content.SpriteAnimations.Effect_SpinningSkull;
                }
                ProductManipulator.SpawnParticle(position.X, position.Y, new SpriteAnimation(anim, Rando.Int(30, 60), true, Rando.Int(0, 30)));
            }
        }

        return filled;
    }


    public (Entity order, bool filled) CheckOrders(Entity product)
    {
        foreach (var order in OrderFilter.Entities)
        {
            if (HasOutRelation<RequiresCategory>(order))
            {
                var requiredCategory = OutRelationSingleton<RequiresCategory>(order);
                if (HasOutRelation<IsInCategory>(product))
                {
                    var category = OutRelationSingleton<IsInCategory>(product);

                    if (requiredCategory == category)
                        return (order, true);

                }
            }

            else if (HasOutRelation<RequiresIngredient>(order))
            {
                var requiredIngredientEntity = OutRelationSingleton<RequiresIngredient>(order);
                var ingredient = Get<Ingredient>(requiredIngredientEntity);

                foreach (var productIngredientEntity in OutRelations<HasIngredient>(product))
                {
                    var productIngredient = Get<Ingredient>(productIngredientEntity);

                    if (ingredient == productIngredient)
                        return (order, true);
                }
            }
        }

        return (default, false);
    }

    public override void Update(TimeSpan delta)
    {
        var cashRegister = GetSingletonEntity<CanFillOrders>();

        foreach (var player in PlayerFilter.Entities)
        {
            if (HasOutRelation<Holding>(player))
            {
                foreach (var colliding in OutRelations<Colliding>(player))
                {
                    if (colliding == cashRegister)
                    {
                        TryFillOrder(player);
                    }
                }
            }
        }

        foreach (var npc in NPCFilter.Entities)
        {
            if (HasOutRelation<Holding>(npc))
            {
                foreach (var colliding in OutRelations<Colliding>(npc))
                {
                    if (colliding == cashRegister)
                    {
                        var product = OutRelationSingleton<Holding>(npc);
                        Destroy(product);
                    }
                }
            }
        }
    }
}
