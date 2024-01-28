using System;
using MoonTools.ECS;
using GGJ2024.Utility;
using GGJ2024.Components;
using MoonWorks.Math.Float;

namespace GGJ2024.Systems;

public class Product : MoonTools.ECS.Manipulator
{
    Filter CategoryFilter;
    Filter IngredientFilter;

    public Product(World world) : base(world)
    {
        CategoryFilter = FilterBuilder.Include<Category>().Build();
        IngredientFilter = FilterBuilder.Include<Ingredient>().Build();
    }

    public float GetPrice(Entity e)
    {
        float total = 0.0f;
        foreach (var ingredient in OutRelations<HasIngredient>(e))
        {
            total += Get<Price>(ingredient).Value;
        }

        return MathF.Max(float.Epsilon, total);
    }

    public void SpawnProduct(Position position)
    {
        var entity = CreateEntity();
        Set(entity, position);
        Set(entity, new Rectangle(0, 0, 16, 16));
        Set(entity, new CanBeHeld());

        foreach (var category in CategoryFilter.EntitiesInRandomOrder)
        {
            Relate(entity, category, new IsInCategory());
            Set(entity, CategoriesAndIngredients.GetColor(Get<Category>(category)));
            break;
        }

        foreach (var ingredient in IngredientFilter.EntitiesInRandomOrder)
        {
            Relate(entity, ingredient, new HasIngredient());
            if (Rando.Value > 0.33)
                break;

        }
    }
}