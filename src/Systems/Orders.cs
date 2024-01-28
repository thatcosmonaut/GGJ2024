using System;
using GGJ2024.Components;
using GGJ2024.Utility;
using MoonTools.ECS;

namespace GGJ2024.Systems;

public class Orders : MoonTools.ECS.System
{
    Filter CategoryFilter;
    Filter IngredientFilter;
    Product Product;
    float OrderWaitTime = 3.0f;

    public Orders(World world) : base(world)
    {
        CategoryFilter = FilterBuilder.Include<Category>().Build();
        IngredientFilter = FilterBuilder.Include<Ingredient>().Build();
        Product = new Product(world);
    }

    public void GetNewOrder(Entity player)
    {
        var entity = CreateEntity();
        Relate(player, entity, new HasOrder());

        if (Rando.Value <= 0.5f)
        { // require category
            Relate(entity, CategoryFilter.RandomEntity, new RequiresCategory());
        }
        else
        { // require ingredient
            Relate(entity, IngredientFilter.RandomEntity, new RequiresIngredient());
        }

        if (HasOutRelation<RequiresCategory>(entity))
            System.Console.WriteLine("Requires category: " + Get<Category>(OutRelationSingleton<RequiresCategory>(entity)));
        else if (HasOutRelation<RequiresIngredient>(entity))
            System.Console.WriteLine("Requires ingredient: " + Get<Ingredient>(OutRelationSingleton<RequiresIngredient>(entity)));
    }

    private int CalculateScore(Entity product)
    {
        var price = Product.GetPrice(product);
        return 300 - (int)MathF.Min(300, price);
    }

    public bool TryFillOrder(Entity player)
    {
        if (!HasOutRelation<Holding>(player))
            return false;

        var product = OutRelationSingleton<Holding>(player);

        var (order, filled) = CheckOrders(player, product);

        if (filled)
        {
            var p = Get<Player>(player);
            var score = p.Score + CalculateScore(product);
            System.Console.WriteLine($"score: {score}");

            Set(player, new Player(p.Index, score));

            Destroy(order);
            Destroy(product);

        }

        return filled;
    }

    public (Entity order, bool filled) CheckOrders(Entity player, Entity product)
    {
        foreach (var order in OutRelations<HasOrder>(player))
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
                var requiredIngredient = OutRelationSingleton<RequiresIngredient>(order);
                if (HasOutRelation<HasIngredient>(product))
                {
                    var ingredient = OutRelationSingleton<HasIngredient>(product);

                    if (requiredIngredient == ingredient)
                        return (order, true);
                }
            }
        }

        return (default, false);
    }

    public override void Update(TimeSpan delta)
    {
        var player = GetSingletonEntity<Player>();


        var cashRegister = GetSingletonEntity<CanFillOrders>();

        foreach (var o in OutRelations<Colliding>(cashRegister))
        {
            if (HasOutRelation<Holding>(o))
            {
                if (Has<Player>(o))
                    TryFillOrder(o);
                else
                {
                    Destroy(OutRelationSingleton<Holding>(o));
                    Destroy(o);
                }
            }
        }

        foreach (var i in InRelations<Colliding>(cashRegister))
        {
            if (HasOutRelation<Holding>(i))
            {
                if (Has<Player>(i))
                    TryFillOrder(i);
                else
                {
                    Destroy(OutRelationSingleton<Holding>(i));
                    Destroy(i);
                }
            }
        }

        var ordersKiosk = GetSingletonEntity<CanGiveOrders>();

        foreach (var o in OutRelations<Colliding>(ordersKiosk))
        {
            if (Has<Player>(o) && !HasOutRelation<CantGetOrders>(o))
            {
                var timer = CreateEntity();
                Set(timer, new Timer(OrderWaitTime));
                Relate(o, timer, new CantGetOrders());
                GetNewOrder(o);
            }
        }

        foreach (var i in InRelations<Colliding>(ordersKiosk))
        {
            if (Has<Player>(i) && !HasOutRelation<CantGetOrders>(i))
            {
                var timer = CreateEntity();
                Set(timer, new Timer(OrderWaitTime));
                Relate(i, timer, new CantGetOrders());
                GetNewOrder(i);
            }
        }

    }
}