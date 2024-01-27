

using System;
using GGJ2024.Data;
using MoonTools.ECS;
using GGJ2024.Utility;
using GGJ2024.Components;
using MoonWorks.Math.Float;

public class Product : MoonTools.ECS.Manipulator
{
    Filter CategoryFilter;
    Filter IngredientFilter;

    public Product(World world) : base(world)
    {
        CategoryFilter = FilterBuilder.Include<Category>().Build();
        IngredientFilter = FilterBuilder.Include<Ingredient>().Build();
    }

    public void SpawnProduct(Position position)
    {
        var entity = CreateEntity();
        Set(entity, position);
        Set(entity, new Rectangle(0, 0, 16, 16));
        Set(entity, new CanBeHeld());

        var category = Enum.GetValues(typeof(Category)).GetRandomItem<Category>(); //TODO: replace with actual data
        var ingredient = Enum.GetValues(typeof(Ingredient)).GetRandomItem<Ingredient>();

        Set(entity, CategoriesAndIngredients.GetColor(category));

        foreach (var ce in CategoryFilter.Entities)
        {
            var cat = Get<Category>(ce);
            if (cat == category)
            {
                Relate(entity, ce, new IsInCategory());
                break;
            }
        }

        foreach (var ie in IngredientFilter.Entities)
        {
            var i = Get<Ingredient>(ie);
            if (i == ingredient)
            {
                Relate(entity, ie, new HasIngredient());
            }
        }
    }
}