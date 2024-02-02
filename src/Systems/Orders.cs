using System;
using System.IO.IsolatedStorage;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Messages;
using RollAndCash.Relations;
using RollAndCash.Utility;
using MoonTools.ECS;
using MoonWorks.Math.Float;
using System.Text;
using MoonWorks.Graphics.Font;
using RollAndCash.Data;

namespace RollAndCash.Systems;

public class Orders : MoonTools.ECS.System
{
    Filter CategoryFilter;
    Filter IngredientFilter;
    Filter OrderFilter;
    Filter PlayerFilter;
    Filter NPCFilter;
    Product ProductManipulator;

    const float OrderTime = 20.0f;

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

    (float min, float max) GetPriceRange(Entity e)
    {
        if (Has<Category>(e))
        {
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            foreach (var product in InRelations<IsInCategory>(e))
            {
                var price = ProductManipulator.GetPrice(product);
                if (price < min)
                    min = price;

                if (price > max)
                    max = price;
            }

            return (min, max);
        }
        else if (Has<Ingredient>(e))
        {
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            foreach (var product in InRelations<HasIngredient>(e))
            {
                var price = ProductManipulator.GetPrice(product);
                if (price < min)
                    min = price;

                if (price > max)
                    max = price;
            }

            return (min, max);
        }

        return (0, 0);
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

        bool suitableCandidate = false;

        while (!suitableCandidate)
        {
            if (Rando.Value <= 0.5f)
            { // require category
                var category = CategoryFilter.RandomEntity;
                var (min, max) = GetPriceRange(category);

                if (min == float.PositiveInfinity || max == float.NegativeInfinity)
                    continue; //no item can fulfill this order, skip it

                if (HasInRelation<RequiresCategory>(category))
                    continue; //another order already wants this, skip it

                suitableCandidate = true;

                Relate(order, category, new RequiresCategory());
                var price = MathF.Round(Rando.Range(min, max), 2);
                Set(order, new Price(price));

                var text = CategoriesAndIngredients.GetDisplayName(Get<Category>(category));
                Set(order, new Text(Fonts.KosugiID, FontSizes.ORDER, text, HorizontalAlignment.Center));

                var animation = CategoriesAndIngredients.GetIcon(Get<Category>(category));

                if (!HasOutRelation<OrderIcon>(order))
                {
                    var iconEntity = CreateEntity();
                    Set(iconEntity, Get<Position>(order) + Vector2.UnitY * 16.0f);
                    Set(iconEntity, new Depth(8));
                    Relate(order, iconEntity, new OrderIcon());
                }

                Set(OutRelationSingleton<OrderIcon>(order), new SpriteAnimation(
                    animation,
                    10,
                    true,
                    0
                ));
            }
            else
            { // require ingredient
                var ingredient = IngredientFilter.RandomEntity;
                var (min, max) = GetPriceRange(ingredient);

                if (min == float.PositiveInfinity || max == float.NegativeInfinity)
                    continue;

                if (HasInRelation<RequiresIngredient>(ingredient))
                    continue;

                suitableCandidate = true;

                Relate(order, ingredient, new RequiresIngredient());
                var price = MathF.Round(Rando.Range(min, max), 2);
                Set(order, new Price(price));
                var text = CategoriesAndIngredients.GetDisplayName(Get<Ingredient>(ingredient));
                Set(order, new Text(Fonts.KosugiID, FontSizes.ORDER, text, MoonWorks.Graphics.Font.HorizontalAlignment.Center));

                if (HasOutRelation<OrderIcon>(order))
                {
                    Destroy(OutRelationSingleton<OrderIcon>(order));
                }
            }
        }

        var font = Fonts.FromID(Fonts.KosugiID);
        var orderText = Get<Text>(order);
        font.TextBounds(
            TextStorage.GetString(orderText.TextID),
            FontSizes.ORDER,
            HorizontalAlignment.Center,
            VerticalAlignment.Middle,
            out var textBounds
        );

        if (textBounds.W > Dimensions.CARD_WIDTH)
        {
            Set(order, new Text(Fonts.KosugiID, FontSizes.SMALL_ORDER, orderText.TextID, HorizontalAlignment.Center));
        }

        if (!HasOutRelation<OrderPriceText>(order))
        {
            var position = Get<Position>(order);
            var priceTextEntity = CreateEntity();
            Set(priceTextEntity, position + Vector2.UnitY * FontSizes.ORDER * 2);
            Relate(order, priceTextEntity, new OrderPriceText());
        }

        var priceText = OutRelationSingleton<OrderPriceText>(order);
        var priceString = $"${Get<Price>(order).Value}";
        Set(priceText, new Text(Fonts.KosugiID, FontSizes.ORDER, priceString, HorizontalAlignment.Center));

        var timer = CreateEntity();
        Set(timer, new Timer(OrderTime));
        Relate(order, timer, new OrderTimer());
    }

    private int CalculateScore(Entity product, Entity order)
    {
        var price = ProductManipulator.GetPrice(product);
        var bounty = Get<Price>(order).Value;

        return (int)(bounty - price);
    }

    public bool TryFillOrder(Entity player)
    {
        var product = OutRelationSingleton<Holding>(player);

        var (order, filled) = CheckOrders(product);

        if (filled)
        {
            var scoreEntity = OutRelationSingleton<HasScore>(player);
            var calculate = CalculateScore(product, order);
            var score = Get<Score>(scoreEntity).Value + calculate;

            Set(scoreEntity, new Score(score));
            Set(scoreEntity, new Text(Fonts.KosugiID, FontSizes.SCORE, score.ToString()));

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

        if (!Some<IsTitleScreen>() && Some<Price>())
        {
            foreach (var order in OrderFilter.Entities)
            {
                if (!HasOutRelation<OrderTimer>(order))
                {
                    SetNewOrderDetails(order);
                }
            }
        }

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
