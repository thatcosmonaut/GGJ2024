using System;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Messages;
using RollAndCash.Relations;
using RollAndCash.Utility;
using MoonTools.ECS;
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
    ProductSpawner ProductManipulator;
    Filter DestroyForDebugTestReasonsFilter;

    public Orders(World world) : base(world)
    {
        CategoryFilter = FilterBuilder.Include<Category>().Build();
        IngredientFilter = FilterBuilder.Include<Ingredient>().Build();
        OrderFilter = FilterBuilder.Include<IsOrder>().Build();
        PlayerFilter = FilterBuilder.Include<Player>().Include<CanHold>().Build();
        ProductManipulator = new ProductSpawner(world);
        DestroyForDebugTestReasonsFilter = FilterBuilder.Include<DestroyForDebugTestReasons>().Build();
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

    public void InitializeOrders()
    {
        foreach (var entity in DestroyForDebugTestReasonsFilter.Entities)
        {
            Destroy(entity);
        }

        // Spawn Orders and cards
        var horizontalCardSpacing = 126;
        int spawnX = (int)System.MathF.Floor(Dimensions.GAME_W * .5f - (horizontalCardSpacing * 3f / 2f));
        var spawnY = Dimensions.GAME_H - 40;

        for (var i = 0; i < 3; i++)
        {
            var orderData = World.CreateEntity();
            World.Set(orderData, new IsOrder());
            World.Set(orderData, new DestroyForDebugTestReasons());

            var orderCardText = World.CreateEntity();
            World.Set(orderCardText, new DestroyForDebugTestReasons());
            World.Set(orderCardText, new Position(spawnX + 5, spawnY + 5));
            World.Set(orderCardText, new Text(Fonts.KosugiID, FontSizes.ORDER, "ORDER TITLE", HorizontalAlignment.Left, VerticalAlignment.Top));
            World.Relate(orderData, orderCardText, new OrderTitleText());

            var orderCardPrice = World.CreateEntity();
            World.Set(orderCardPrice, new DestroyForDebugTestReasons());
            World.Set(orderCardPrice, new Position(spawnX + 5, spawnY + FontSizes.ORDER * 2f + 2));
            World.Set(orderCardPrice, new Text(Fonts.KosugiID, FontSizes.ORDER, "$$$", HorizontalAlignment.Left, VerticalAlignment.Top));
            World.Relate(orderData, orderCardPrice, new OrderPriceText());

            var orderCardBG = World.CreateEntity();
            World.Set(orderCardBG, new DestroyForDebugTestReasons());
            World.Set(orderCardBG, new Position(spawnX, spawnY));
            World.Set(orderCardBG, new Depth(9));
            World.Set(orderCardBG, new SpriteAnimation(SpriteAnimations.HUD_Card, 0));
            World.Relate(orderData, orderCardBG, new OrderBG());

            var orderCardCategoryIcon = World.CreateEntity();
            World.Set(orderCardCategoryIcon, new DestroyForDebugTestReasons());
            World.Set(orderCardCategoryIcon, new Position(spawnX + 100, spawnY + 32));
            World.Set(orderCardCategoryIcon, new Depth(8));
            World.Set(orderCardCategoryIcon, new SpriteAnimation(SpriteAnimations.HUD_Card, 0));
            World.Set(orderCardCategoryIcon, new ColorFlicker(0, Colors.OrderCategory));
            World.Relate(orderData, orderCardCategoryIcon, new OrderIcon());

            var orderCardTimerRectangle = World.CreateEntity();
            World.Set(orderCardTimerRectangle, new DestroyForDebugTestReasons());
            World.Set(orderCardTimerRectangle, new Position(spawnX, spawnY + 1));
            World.Set(orderCardTimerRectangle, new DrawAsRectangle());
            World.Set(orderCardTimerRectangle, new Depth(7));
            World.Relate(orderData, orderCardTimerRectangle, new OrderTimerRectangle());

            spawnX += horizontalCardSpacing;
        }
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

        var orderCardTitleText = OutRelationSingleton<OrderTitleText>(order);
        var orderCardPriceText = OutRelationSingleton<OrderPriceText>(order);
        var titleText = Get<Text>(orderCardTitleText);

        bool suitableCandidate = false;

        int titleFontSize = FontSizes.ORDER;

        while (!suitableCandidate)
        {
            if (Rando.Value <= 0.5f)
            {
                // CATEGORY Order
                var category = CategoryFilter.RandomEntity;
                var (min, max) = GetPriceRange(category);

                if (min == float.PositiveInfinity || max == float.NegativeInfinity)
                    continue; //no item can fulfill this order, skip it

                if (HasInRelation<RequiresCategory>(category))
                    continue; //another order already wants this, skip it

                suitableCandidate = true;

                // Update Order Data
                Relate(order, category, new RequiresCategory());
                var price = MathF.Round(Rando.Range(min, max), 2);
                Set(order, new Price(price));

                var color = Colors.OrderCategory;
                Set(order, new ColorBlend(color));
                Set(orderCardTitleText, new ColorBlend(color));
                Set(orderCardPriceText, new ColorBlend(color));

                // Update Order Title
                var text = CategoriesAndIngredients.GetDisplayName(Get<Category>(category));
                Set(orderCardTitleText, new Text(Fonts.KosugiID, titleFontSize, text, titleText.HorizontalAlignment, titleText.VerticalAlignment));

                // Update Order Icon
                var animation = CategoriesAndIngredients.GetIcon(Get<Category>(category));

                Set(OutRelationSingleton<OrderIcon>(order), new SpriteAnimation(
                    animation,
                    10,
                    true,
                    0
                ));
            }
            else
            {
                // INGREDIENT Order
                titleFontSize = FontSizes.INGREDIENT;
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
                Set(orderCardTitleText, new Text(Fonts.KosugiID, titleFontSize, text, titleText.HorizontalAlignment, titleText.VerticalAlignment));

                var color = Colors.OrderIngredient;
                Set(order, new ColorBlend(color));
                Set(orderCardTitleText, new ColorBlend(color));
                Set(orderCardPriceText, new ColorBlend(color));

                if (HasOutRelation<OrderIcon>(order))
                {
                    Remove<SpriteAnimation>(OutRelationSingleton<OrderIcon>(order));
                }
            }
        }

        // Shrink font if text too wide
        var font = Fonts.FromID(Fonts.KosugiID);
        var orderText = Get<Text>(orderCardTitleText);
        font.TextBounds(
            TextStorage.GetString(orderText.TextID),
            titleFontSize,
            titleText.HorizontalAlignment,
            titleText.VerticalAlignment,
            out var textBounds
        );

        if (textBounds.W > Dimensions.CARD_WIDTH)
        {
            titleFontSize = FontSizes.SMALL_ORDER;
            Set(orderCardTitleText, new Text(Fonts.KosugiID, titleFontSize, orderText.TextID, titleText.HorizontalAlignment, titleText.VerticalAlignment));
        }

        // Set Price
        var priceString = "$" + Get<Price>(order).Value.ToString("F2");
        Set(orderCardPriceText, new Text(Fonts.KosugiID, FontSizes.ORDER, priceString, HorizontalAlignment.Left, VerticalAlignment.Top));

        var timer = CreateEntity();
        Set(timer, new Timer(HasOutRelation<RequiresCategory>(order) ? Time.CATEGORY_ORDER_TIME : Time.INGREDIENT_ORDER_TIME));
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
        if (!HasOutRelation<Holding>(player)) { return false; }

        var product = OutRelationSingleton<Holding>(player);

        var (order, filled) = CheckOrders(product);

        if (filled)
        {
            var scoreEntity = OutRelationSingleton<HasScore>(player);
            var calculate = CalculateScore(product, order);
            var score = Get<Score>(scoreEntity).Value + calculate;
            var playerIndex = Get<Player>(player).Index;

            Set(scoreEntity, new Score(score));

            if (calculate < 0)
            {
                var x = playerIndex == 1 ? Dimensions.GAME_W - 100 : 100;
                ProductManipulator.SpawnScoreEffect(
                    player,
                    new Position(x, Dimensions.GAME_H),
                    new SpriteAnimation(Content.SpriteAnimations.Effect_SpinningSkull, Rando.Int(30, 60), true),
                    calculate
                );

                Send(new PlayStaticSoundMessage(StaticAudio.CursedCoin));
            }
            else
            {
                var x = playerIndex == 1 ? Dimensions.GAME_W - 100 : 100;
                ProductManipulator.SpawnScoreEffect(
                    player,
                    new Position(x, Dimensions.GAME_H),
                    new SpriteAnimation(Content.SpriteAnimations.Effect_SpinningCoin, Rando.Int(30, 60), true),
                    calculate
                );

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
        if (Some<Price>())
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
                    if (Has<CanFillOrders>(colliding))
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
                var product = OutRelationSingleton<Holding>(npc);

                foreach (var colliding in OutRelations<Colliding>(npc))
                {
                    if (Has<CanFillOrders>(colliding))
                    {
                        Destroy(product);
                    }
                }
            }
        }

        // Udate Order Card Timer Visual
        foreach (var orderEntity in OrderFilter.Entities)
        {
            if (HasOutRelation<OrderTimer>(orderEntity) && HasOutRelation<OrderTimerRectangle>(orderEntity))
            {
                var timerEntity = OutRelationSingleton<OrderTimer>(orderEntity);
                float timeFactor = Get<Timer>(timerEntity).Remaining;
                var hudRectangleEntity = OutRelationSingleton<OrderTimerRectangle>(orderEntity);
                Set(hudRectangleEntity, new Rectangle(0, 0, (int)(timeFactor * 120f), 2));

                Set(hudRectangleEntity, Get<ColorBlend>(orderEntity));
                // Flicker Effect
                if (timeFactor < .25f && Math.Floor(Get<Timer>(timerEntity).Time * 10) % 2 == 0)
                {
                    Set(hudRectangleEntity, new ColorBlend(MoonWorks.Graphics.Color.Black));
                }
            }
        }
    }
}
