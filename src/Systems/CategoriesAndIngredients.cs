using System;
using MoonWorks;
using RollAndCash.Components;
using MoonTools.ECS;
using MoonWorks.Graphics;
using RollAndCash.Utility;
using RollAndCash.Data;

namespace RollAndCash.Systems;

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
    Relics
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
    Oatmeal
}

public class CategoriesAndIngredients : Manipulator
{
    const float MaxPriceDelta = 10.0f;
    MoonTools.ECS.Filter IngredientFilter;

    public CategoriesAndIngredients(World world) : base(world)
    {
        IngredientFilter = FilterBuilder.Include<Ingredient>().Build();
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
            "oatmeal" => Ingredient.Oatmeal
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
            "ip" => Category.IntellectualProperty,
            "pharmacy" => Category.Pharmacy,
            "relics" => Category.Relics
        };
    }

    public static string GetDisplayName(Ingredient ingredient)
    {
        return ingredient switch
        {
            Ingredient.Ectoplasm => "Ectoplasm",
            Ingredient.Silicon => "Silicon",
            Ingredient.Blueberry => "Blueberry",
            Ingredient.Gems => "Gems",
            Ingredient.Fungus => "Fungus",
            Ingredient.Elastic => "Elastic",
            Ingredient.Carbon => "Carbon",
            Ingredient.Mint => "Mint",
            Ingredient.Milk => "Milk",
            Ingredient.Bones => "Bones",
            Ingredient.Spirits => "Spirits (Supernatural)",
            Ingredient.Booze => "Spirits (Booze)",
            Ingredient.Kelp => "Kelp",
            Ingredient.Microplastics => "Microplastics",
            Ingredient.Glass => "Glass Shards",
            Ingredient.Helium => "Helium",
            Ingredient.Shrimp => "Brine Shrimp",
            Ingredient.Uranium => "Uranium",
            Ingredient.Gold => "Gold",
            Ingredient.Oatmeal => "Oatmeal"
        };
    }


    public static string GetDisplayName(Category category)
    {
        return category switch
        {
            Category.Animals => "Animals",
            Category.Clothes => "Clothes",
            Category.Cosmetics => "Cosmetics",
            Category.Electronics => "Electronics",
            Category.Food => "Food",
            Category.Furniture => "Furniture",
            Category.Gasses => "Gasses",
            Category.IntellectualProperty => "Intellectual Property",
            Category.Pharmacy => "Pharmacy",
            Category.Relics => "Relics"
        };
    }

    public static SpriteAnimationInfo GetIcon(Category category)
    {
        return category switch
        {
            Category.Animals => Content.SpriteAnimations.Item_Animal,

            Category.Clothes => Content.SpriteAnimations.Item_Clothing,

            Category.Cosmetics => Content.SpriteAnimations.Item_Cosmetics,

            Category.Electronics => Content.SpriteAnimations.Item_Electronics,

            Category.Food => Content.SpriteAnimations.Item_Food,

            Category.Furniture => Content.SpriteAnimations.Item_Furniture,

            Category.Gasses => Content.SpriteAnimations.Item_Gasses,

            Category.IntellectualProperty => Content.SpriteAnimations.Item_IP,

            Category.Pharmacy => Content.SpriteAnimations.Item_Pharmacy,

            Category.Relics => Content.SpriteAnimations.Item_Relic,
        };
    }

    public static Color GetColor(Category category)
    {
        return category switch
        {
            Category.Animals => Color.GreenYellow,
            Category.Clothes => Color.Blue,
            Category.Cosmetics => Color.Red,
            Category.Electronics => Color.Beige,
            Category.Food => Color.ForestGreen,
            Category.Furniture => Color.Brown,
            Category.Gasses => Color.Pink,
            Category.IntellectualProperty => Color.Gray,
            Category.Pharmacy => Color.Purple,
            Category.Relics => Color.Gold,
            _ => Color.White
        };
    }

    public static string GetStockTicker(Ingredient ingredient)
    {
        return ingredient switch
        {
            Ingredient.Ectoplasm => "ECTO",
            Ingredient.Silicon => "SIL",
            Ingredient.Blueberry => "BLU",
            Ingredient.Gems => "GME",
            Ingredient.Fungus => "FUNG",
            Ingredient.Elastic => "ELAS",
            Ingredient.Carbon => "CARB",
            Ingredient.Mint => "MINT",
            Ingredient.Milk => "MILK",
            Ingredient.Bones => "BONE",
            Ingredient.Spirits => "SPIR",
            Ingredient.Booze => "BOOZ",
            Ingredient.Kelp => "KELP",
            Ingredient.Microplastics => "MPLA",
            Ingredient.Glass => "GLASS",
            Ingredient.Helium => "HELI",
            Ingredient.Shrimp => "SHRI",
            Ingredient.Uranium => "URA",
            Ingredient.Gold => "GOLD",
            Ingredient.Oatmeal => "OAT",
            _ => "NONE"
        };
    }

    public (float price, float delta, Ingredient ingredient) ChangePrice()
    {
        float delta = Rando.Range(-MaxPriceDelta, MaxPriceDelta);

        if (!IngredientFilter.Empty)
        {
            var entity = IngredientFilter.RandomEntity;
            var price = Get<Price>(entity).Value;
            var ingredient = Get<Ingredient>(entity);
            price = MathF.Round(price + delta, 2);
            Set(entity, new Price(price));
            return (price, delta, ingredient);
        }

        return (0, 0, Ingredient.Blueberry); // this should never happen!
    }

    public void Initialize(World world)
    {
        var categories = Enum.GetValues(typeof(Category));
        var ingredients = Enum.GetValues(typeof(Ingredient));

        foreach (Category category in categories)
        {
            var e = world.CreateEntity();
            world.Set(e, category);
        }

        foreach (Ingredient ingredient in ingredients)
        {
            var e = world.CreateEntity();
            world.Set(e, ingredient);
            world.Set(e, new Price(Rando.Range(0f, 100.0f)));
        }
    }

}
