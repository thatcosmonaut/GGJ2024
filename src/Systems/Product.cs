using System;
using MoonTools.ECS;
using GGJ2024.Utility;
using GGJ2024.Components;
using MoonWorks.Math.Float;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GGJ2024.Data;

namespace GGJ2024.Systems;

public class Product : MoonTools.ECS.Manipulator
{
    private readonly record struct ProductData(string Name, Category Category, Ingredient[] Ingredients);

    Filter CategoryFilter;
    Filter IngredientFilter;

    List<ProductData> Products;

    public Product(World world) : base(world)
    {
        CategoryFilter = FilterBuilder.Include<Category>().Build();
        IngredientFilter = FilterBuilder.Include<Ingredient>().Build();

        var productsFilePath = Path.Combine(
            System.AppContext.BaseDirectory,
            "Content",
            "Data",
            "products.tsv"
        );

        var productsFile = File.ReadLines(productsFilePath);
        Products = new List<ProductData>();

        foreach (var line in productsFile)
        {
            var split = line.Split('\t');
            var tags = split[1].Split(',');
            var ingredients = new Ingredient[tags.Length - 1];

            for (var i = 0; i < tags.Length; i++)
            {
                if (i < tags.Length - 1)
                {
                    ingredients[i] = CategoriesAndIngredients.GetIngredient(tags[i]);
                }
            }

            Products.Add(new ProductData(
                split[0],
                CategoriesAndIngredients.GetCategory(tags[tags.Length - 1]),
                ingredients
            ));
        }
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

        var product = Products.GetRandomItem();

        Set(entity, new Name(TextStorage.GetID(product.Name)));

        foreach (var category in CategoryFilter.Entities)
        {
            if (Get<Category>(category) == product.Category)
            {
                Relate(entity, category, new IsInCategory());
                Set(entity, CategoriesAndIngredients.GetColor(Get<Category>(category)));
                break;
            }
        }

        foreach (var ingredient in IngredientFilter.Entities)
        {
            if (product.Ingredients.Contains(Get<Ingredient>(ingredient)))
            {
                Relate(entity, ingredient, new HasIngredient());
            }
        }
    }
}