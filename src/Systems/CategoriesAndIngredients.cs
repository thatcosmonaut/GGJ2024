using System;
using MoonWorks;
using GGJ2024.Components;
using MoonTools.ECS;
using MoonWorks.Graphics;
using GGJ2024.Utility;

namespace GGJ2024.Systems;

public enum Category
{
    Animals,
    Clothes,
    Cosmetics,
    Electronics,
    Food,
    Furniture,
    Gasses,
    IntellectualProperty,
    Pharmacy,
    Relics,
    None
}

public enum Ingredient
{
    Ectoplasm,
    Silicon,
    Blueberry,
    Gems,
    Fungus,
    Elastic,
    Carbon,
    Mint,
    Milk,
    Bones,
    Spirits,
    Booze,
    Kelp,
    Microplastics,
    Glass,
    Helium,
    Shrimp,
    Uranium,
    Gold,
    Oatmeal,
    None
}

public class CategoriesAndIngredients : Manipulator
{
    const float MaxPriceDelta = 10.0f;

    public CategoriesAndIngredients(World world) : base(world)
    {
    }

    public static Ingredient GetIngredient(string s)
    {
        return s.ToLowerInvariant() switch
        {
            "ectoplasm" => Ingredient.Ectoplasm,
            "silicon" => Ingredient.Silicon,
            "blueberry" => Ingredient.Blueberry,
            "gems" => Ingredient.Gems,
            "fungus" => Ingredient.Fungus,
            "elastic" => Ingredient.Elastic,
            "carbon" => Ingredient.Carbon,
            "mint" => Ingredient.Mint,
            "milk" => Ingredient.Milk,
            "bones" => Ingredient.Bones,
            "spirits" => Ingredient.Spirits,
            "booze" => Ingredient.Booze,
            "kelp" => Ingredient.Kelp,
            "microplastics" => Ingredient.Microplastics,
            "glass" => Ingredient.Glass,
            "helium" => Ingredient.Helium,
            "shrimp" => Ingredient.Shrimp,
            "uranium" => Ingredient.Uranium,
            "gold" => Ingredient.Gold,
            "oatmeal" => Ingredient.Oatmeal,
            _ => Ingredient.None
        };
    }

    public static Category GetCategory(string s)
    {
        return s.ToLowerInvariant() switch
        {
            "animals" => Category.Animals,
            "clothes" => Category.Clothes,
            "cosmetics" => Category.Cosmetics,
            "electronics" => Category.Electronics,
            "food" => Category.Food,
            "furniture" => Category.Furniture,
            "gasses" => Category.Gasses,
            "intellectual property" => Category.IntellectualProperty,
            "pharmacy" => Category.Pharmacy,
            "relics" => Category.Relics,
            _ => Category.None
        };
    }

    public static Color GetColor(Category category)
    {
        return category switch
        {
            Category.Animals => Color.GreenYellow,
            Category.Clothes => Color.Azure,
            Category.Cosmetics => Color.Red,
            Category.Electronics => Color.Black,
            Category.Food => Color.ForestGreen,
            Category.Furniture => Color.Brown,
            Category.Gasses => Color.Pink,
            Category.IntellectualProperty => Color.Gray,
            Category.Pharmacy => Color.Purple,
            Category.Relics => Color.Gold,
            Category.None => Color.White
        };
    }

    public float GetPriceChange()
    {
        float delta = Rando.Range(-MaxPriceDelta, MaxPriceDelta);
        return delta;
    }

    public void Initialize(World world)
    {
        var categories = Enum.GetValues(typeof(Category));
        var ingredients = Enum.GetValues(typeof(Ingredient));

        foreach (Category category in categories)
        {
            if (category != Category.None)
            {
                var e = world.CreateEntity();
                world.Set(e, category);
            }
        }

        foreach (Ingredient ingredient in ingredients)
        {
            if (ingredient != Ingredient.None)
            {
                var e = world.CreateEntity();
                world.Set(e, ingredient);
                world.Set(e, new Price(Rando.Range(0f, 100.0f)));
            }
        }
    }

}