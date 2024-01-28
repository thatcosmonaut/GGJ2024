
using System;
using MoonWorks.Math.Float;
using MoonTools.ECS;
using MoonWorks.Graphics;
using GGJ2024.Components;
using GGJ2024.Data;

namespace GGJ2024.Systems;

public class Hold : MoonTools.ECS.System
{
    MoonTools.ECS.Filter TryHoldFilter;
    MoonTools.ECS.Filter CanHoldFilter;
    float HoldSpeed = 32.0f;
    Product Product;

    public Hold(World world) : base(world)
    {
        TryHoldFilter =
            FilterBuilder
            .Include<Rectangle>()
            .Include<Position>()
            .Include<CanHold>()
            .Include<TryHold>()
            .Build();

        CanHoldFilter =
            FilterBuilder
            .Include<Rectangle>()
            .Include<Position>()
            .Include<CanHold>()
            .Build();
        Product = new Product(world);
    }

    void HoldOrDrop(Entity e)
    {
        if (!HasOutRelation<Holding>(e))
        {
            bool holding = false;

            foreach (var o in OutRelations<Colliding>(e))
            {
                if (Has<CanBeHeld>(o))
                {
                    holding = true;
                    Relate(e, o, new Holding());
                    var category = Get<Category>(OutRelationSingleton<IsInCategory>(o));
                    System.Console.Write($" {TextStorage.GetString(Get<Name>(o).TextID)} ${Product.GetPrice(o)} category: {category} ");

                    System.Console.Write("ingredience: ");
                    foreach (var ingredient in OutRelations<HasIngredient>(o))
                    {
                        System.Console.Write($"{Get<Ingredient>(ingredient)} (${Get<Price>(ingredient).Value}) ");
                    }
                    System.Console.WriteLine("");
                }
            }

            if (!holding)
            {
                foreach (var i in InRelations<Colliding>(e))
                {
                    if (Has<CanBeHeld>(i))
                    {
                        Set(i, Color.Yellow);
                        Relate(e, i, new Holding());
                    }
                }
            }
        }
        else
        {
            var holding = OutRelationSingleton<Holding>(e);
            Remove<Velocity>(holding);
            UnrelateAll<Holding>(e);
        }
    }

    void SetHoldVelocity(Entity e, float dt)
    {
        var holding = OutRelationSingleton<Holding>(e);
        var holdingPos = Get<Position>(holding);
        var holderPos = Get<Position>(e);

        var vel = holderPos - holdingPos;

        Set(holding, new Velocity(vel * HoldSpeed * dt));
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var e in CanHoldFilter.Entities)
        {
            if (Has<TryHold>(e))
                HoldOrDrop(e);

            if (HasOutRelation<Holding>(e))
                SetHoldVelocity(e, (float)delta.TotalSeconds);

        }

    }
}
