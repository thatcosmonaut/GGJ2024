
using System;
using GGJ2024.Components;
using GGJ2024.Utility;
using MoonTools.ECS;

namespace GGJ2024.Systems;

public class ProductSpawner : MoonTools.ECS.System
{
    Product Product;
    Filter ProductSpawnerFilter;
    Category[] Categories;

    public ProductSpawner(World world) : base(world)
    {
        Product = new Product(world);
        ProductSpawnerFilter = FilterBuilder.Include<CanSpawn>().Build();
        Categories = Enum.GetValues<Category>();
    }

    public override void Update(TimeSpan delta)
    {
        if (!Some<Components.CanBeHeld>())
        {

            foreach (var entity in ProductSpawnerFilter.Entities)
            {
                var position = Get<Position>(entity);
                var rect = Get<CanSpawn>(entity).Rectangle;

                var spawnAsCategory = Has<SpawnCategory>(entity);


                var spawnStepDistance = 24;

                for (var y = position.Y; y < position.Y + rect.Height; y += spawnStepDistance)
                {
                    for (var x = position.X; x < position.X + rect.Width; x += spawnStepDistance)
                    {
                        if (spawnAsCategory)
                        {
                           var category = Get<SpawnCategory>(entity).Category;
                            Product.SpawnProduct(new Position(x, y), category);
                        }
                        else
                        {
                            var category = Rando.GetRandomItem(Categories);
                            Product.SpawnProduct(new Position(x, y), category);
                        }
                    }
                }
            }

            /*
            for (var i = 0; i < 30; i++)
            {
                Product.SpawnProduct(new Position(
                    Rando.IntInclusive(0, Dimensions.GAME_W),
                    Rando.IntInclusive(0, Dimensions.GAME_H)
                ),
                Category.Clothes);
            }*/
        }
    }
}
