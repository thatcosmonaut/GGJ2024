
using System;
using RollAndCash.Components;
using RollAndCash.Relations;
using RollAndCash.Utility;
using MoonTools.ECS;
using MoonWorks.Math.Float;

namespace RollAndCash.Systems;

public class ProductSpawner : MoonTools.ECS.Manipulator
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
		ProductSpawnerFilter = FilterBuilder.Include<Position>().Include<CanSpawn>().Build();
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

		var x = 150;
		var topShelfY = 115;
		var bottomShelfY = 250;
		SpawnVerticalDoubleShelf(x, topShelfY, 0);
		SpawnVerticalDoubleShelf(x, bottomShelfY - 8, 2);
		x = 640 - 80 - 80 - 10;
		SpawnVerticalDoubleShelf(x, topShelfY, 4);
		SpawnVerticalDoubleShelf(x, bottomShelfY, 6);

		Product.SpawnShelf(330 - spawnStepDistance - 16, 200, 4, 2);
		var collision = CreateEntity();
		Set(collision, new Solid());
		Set(collision, new Position(330 - spawnStepDistance - 16, 200));
		Set(collision, new Rectangle(-12, 8, 4 * spawnStepDistance, 8));

		Product.SpawnShelf(640 - spawnStepDistance + 10, 160, 1, 4, Categories[9]);

	}

	public void SpawnVerticalDoubleShelf(int x, int y, int categoryInt)
	{
		var verticalProductAmount = 3;
		Product.SpawnShelf(x, y, 1, verticalProductAmount, Categories[categoryInt]);
		Product.SpawnShelf(x + spawnStepDistance, y, 1, verticalProductAmount, Categories[categoryInt + 1]);

		var collision = CreateEntity();
		Set(collision, new Solid());
		Set(collision, new Position(x, y));
		Set(collision, new Rectangle(12, -12, 8, (verticalProductAmount * spawnStepDistance) - 8));
	}

	public void ClearProducts()
	{
		foreach (var entity in ProductFilter.Entities)
		{
			Destroy(entity);
		}
	}

	public void SpawnProducts()
	{
		foreach (var entity in ProductSpawnerFilter.Entities)
		{
			if (!HasInRelation<BelongsToProductSpawner>(entity))
			{
				var position = Get<Position>(entity);

				Category category;
				if (Has<SpawnCategory>(entity))
				{
					category = Get<SpawnCategory>(entity).Category;
				}
				else
				{
					category = Rando.GetRandomItem(Categories);
				}

				var product = Product.SpawnProduct(position, category);
				Relate(product, entity, new BelongsToProductSpawner());
			}

			// for (var y = position.Y; y < position.Y + canSpawn.Height * spawnStepDistance; y += spawnStepDistance)
			// {
			// 	for (var x = position.X; x < position.X + canSpawn.Width * spawnStepDistance; x += spawnStepDistance)
			// 	{
			// 		if (spawnAsCategory)
			// 		{
			// 			var category = Get<SpawnCategory>(entity).Category;
			// 			Product.SpawnProduct(new Position(x, y), category);
			// 		}
			// 		else
			// 		{
			// 			var category = Rando.GetRandomItem(Categories);
			// 			Product.SpawnProduct(new Position(x, y), category);
			// 		}
			// 	}
			// }
		}
	}
}
