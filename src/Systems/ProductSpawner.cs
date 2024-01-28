
using System;
using GGJ2024.Components;
using GGJ2024.Utility;
using MoonTools.ECS;

namespace GGJ2024.Systems;

public class ProductSpawner : MoonTools.ECS.System
{
    Product Product;
    Filter ProductSpawnerFilter;
    Filter ProductFilter;
    Category[] Categories;
    int spawnStepDistance = 32;

    public ProductSpawner(World world) : base(world)
    {
        Product = new Product(world);
        ProductFilter = FilterBuilder.Include<CanBeHeld>().Build();
        ProductSpawnerFilter = FilterBuilder.Include<CanSpawn>().Build();
        Categories = Enum.GetValues<Category>();
        SpawnShelves();
    }

    public void SpawnShelves()
    {
        foreach (var entity in ProductFilter.Entities)
        {
            Destroy(entity);
        }
        foreach (var entity in ProductSpawnerFilter.Entities)
        {
            Destroy(entity);
        }
        Rando.Shuffle(Categories);

        var x = 140;
        var topShelfY = 110;
        var bottomShelfY = 250;
        SpawnVerticalDoubleShelf(x, topShelfY, 0);
        SpawnVerticalDoubleShelf(x, bottomShelfY, 2);
        x = 640 - 80 - 80 - 10;
        SpawnVerticalDoubleShelf(x, topShelfY, 4);
        SpawnVerticalDoubleShelf(x, bottomShelfY, 6);

        Product.SpawnShelf(320 - spawnStepDistance - 16, 200, 4, 2);
        Product.SpawnShelf(640 - spawnStepDistance + 3, 170, 1, 4, Categories[9]);
    }

    public void SpawnVerticalDoubleShelf(int x, int y, int categoryInt)
    {
        var verticalProductAmount = 3;
        Product.SpawnShelf(x, y, 1, verticalProductAmount, Categories[categoryInt]);
        Product.SpawnShelf(x + spawnStepDistance, y, 1, verticalProductAmount, Categories[categoryInt + 1]);
    }

    float time = 0;
    public override void Update(TimeSpan delta)
    {
        // This is for auto refresh level editing
        if (false)
        {
            var respawnTime = .5f;
            time += (float)delta.TotalSeconds;
            if (time > respawnTime)
            {
                time -= respawnTime;
                SpawnShelves();
            }
        }

        if (!Some<Components.CanBeHeld>())
        {
            foreach (var entity in ProductSpawnerFilter.Entities)
            {
                var position = Get<Position>(entity);
                var canSpawn = Get<CanSpawn>(entity);

                var spawnAsCategory = Has<SpawnCategory>(entity);

                for (var y = position.Y; y < position.Y + canSpawn.Height * spawnStepDistance; y += spawnStepDistance)
                {
                    for (var x = position.X; x < position.X + canSpawn.Width * spawnStepDistance; x += spawnStepDistance)
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
        }
    }
}
