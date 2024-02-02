
using System;
using RollAndCash.Components;
using RollAndCash.Utility;
using MoonTools.ECS;
using MoonWorks.Math.Float;

namespace RollAndCash.Systems;

public class ShelfSpawner : MoonTools.ECS.Manipulator
{
	Category[] Categories;
	int spawnStepDistance = 32;

	public ShelfSpawner(World world) : base(world)
	{
		Categories = Enum.GetValues<Category>();
	}

	// TODO: shuffle shelf categories on game restart
	public void SpawnShelves()
	{
		Rando.Shuffle(Categories);

		var x = 150;
		var topShelfY = 115;
		var bottomShelfY = 250;
		SpawnVerticalDoubleShelf(x, topShelfY, 0);
		SpawnVerticalDoubleShelf(x, bottomShelfY - 8, 2);
		x = 640 - 80 - 80 - 10;
		SpawnVerticalDoubleShelf(x, topShelfY, 4);
		SpawnVerticalDoubleShelf(x, bottomShelfY, 6);

		SpawnShelf(330 - spawnStepDistance - 16, 200, 4, 2);
		var collision = CreateEntity();
		Set(collision, new Solid());
		Set(collision, new Position(330 - spawnStepDistance - 16, 200));
		Set(collision, new Rectangle(-12, 8, 4 * spawnStepDistance, 8));

		SpawnShelf(640 - spawnStepDistance + 10, 160, 1, 4, Categories[9]);
	}

	private void SpawnVerticalDoubleShelf(int x, int y, int categoryInt)
	{
		var verticalProductAmount = 3;
		SpawnShelf(x, y, 1, verticalProductAmount, Categories[categoryInt]);
		SpawnShelf(x + spawnStepDistance, y, 1, verticalProductAmount, Categories[categoryInt + 1]);

		var collision = CreateEntity();
		Set(collision, new Solid());
		Set(collision, new Position(x, y));
		Set(collision, new Rectangle(12, -12, 8, (verticalProductAmount * spawnStepDistance) - 8));
	}

    private void SpawnShelf(int x, int y, int width, int height, Category category)
    {
        var shelf = CreateEntity();
        Set(shelf, new Position(x, y));
        Set(shelf, new SpawnCategory(category));

        for (var spawnerY = y; spawnerY < y + height * spawnStepDistance; spawnerY += spawnStepDistance)
        {
            for (var spawnerX = x; spawnerX < x + width * spawnStepDistance; spawnerX += spawnStepDistance)
            {
                var productSpawnerEntity = CreateEntity();
                Set(productSpawnerEntity, new Position(spawnerX, spawnerY));
                Set(productSpawnerEntity, new CanSpawn());
                Set(productSpawnerEntity, new SpawnCategory(category));
            }
        }
    }

    private void SpawnShelf(int x, int y, int width, int height)
    {
        var shelf = CreateEntity();
        Set(shelf, new Position(x, y));

        for (var spawnerY = y; spawnerY < y + height * spawnStepDistance; spawnerY += spawnStepDistance)
        {
            for (var spawnerX = x; spawnerX < x + width * spawnStepDistance; spawnerX += spawnStepDistance)
            {
                var productSpawnerEntity = CreateEntity();
                Set(productSpawnerEntity, new Position(spawnerX, spawnerY));
                Set(productSpawnerEntity, new CanSpawn());
            }
        }
    }
}
