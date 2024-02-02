using System;
using System.IO;
using System.Collections.Generic;

namespace RollAndCash.Systems;

public static class ProductLoader
{
    public static List<ProductData> AllProducts;
    public static Dictionary<Category, List<ProductData>> CategoryToProductList = new();

    public static void Load()
    {
        foreach (Category category in Enum.GetValues(typeof(Category)))
        {
            CategoryToProductList[category] = new List<ProductData>();
        }

        var productsFilePath = Path.Combine(
            System.AppContext.BaseDirectory,
            "Content",
            "Data",
            "products.tsv"
        );

        var productsFile = File.ReadLines(productsFilePath);
        AllProducts = new List<ProductData>();

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

            var category = CategoriesAndIngredients.GetCategory(tags[tags.Length - 1]);
            var product = new ProductData(
                split[0],
                category,
                ingredients);

            AllProducts.Add(product);

            var categoryList = CategoryToProductList[category];
            categoryList.Add(product);
        }
    }
}
