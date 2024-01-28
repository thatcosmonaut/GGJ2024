
using System;
using GGJ2024.Utility;
using MoonTools.ECS;

namespace GGJ2024.Systems;

public class ProductSpawner : MoonTools.ECS.System
{
    Product Product;

    public ProductSpawner(World world) : base(world)
    {
        Product = new Product(world);
    }

    public override void Update(TimeSpan delta)
    {
        if (!Some<Components.CanBeHeld>())
        {
            for (var i = 0; i < 30; i++)
            {
                Product.SpawnProduct(new Position(
                    Rando.IntInclusive(0, Dimensions.GAME_W),
                    Rando.IntInclusive(0, Dimensions.GAME_H)
                ),
                Category.Clothes);
            }
        }
    }
}
