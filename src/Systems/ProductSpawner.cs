using System;
using MoonTools.ECS;
using RollAndCash.Utility;
using RollAndCash.Components;
using MoonWorks.Math.Float;
using RollAndCash.Data;
using RollAndCash.Relations;
using MoonWorks.Math;

namespace RollAndCash.Systems;

public readonly record struct ProductData(string Name, Category Category, Ingredient[] Ingredients);

public class ProductSpawner : MoonTools.ECS.Manipulator
{
    Filter ProductFilter;
	Filter ProductSpawnerFilter;

    Filter CategoryFilter;
    Filter IngredientFilter;

	Category[] Categories;

    public ProductSpawner(World world) : base(world)
    {
        ProductFilter = FilterBuilder.Include<CanBeHeld>().Build();
		ProductSpawnerFilter = FilterBuilder.Include<Position>().Include<CanSpawn>().Build();

        CategoryFilter = FilterBuilder.Include<Category>().Build();
        IngredientFilter = FilterBuilder.Include<Ingredient>().Build();
        Categories = Enum.GetValues<Category>();
    }

    public float GetPrice(Entity e)
    {
        float total = 0.0f;
        foreach (var ingredient in OutRelations<HasIngredient>(e))
        {
            total += Get<Price>(ingredient).Value;
        }

        return MathF.Round(MathF.Max(1, total), 2);
    }

    public Entity SpawnProduct(Position position, Category category)
    {
        var entity = CreateEntity();
        Set(entity, position);
        Set(entity, new Rectangle(-8, -8, 16, 16));
        Set(entity, new CanBeHeld());
        Set(entity, new CanInteract());
        Set(entity, new SlowDownAnimation(15, 1));
        var depth = MathHelper.Lerp(100, 10, position.Y / (float)Dimensions.GAME_H);
        Set(entity, new Depth(depth));
        Set(entity, new DestroyWhenOutOfBounds());

        var product = ProductLoader.CategoryToProductList[category].GetRandomItem();

        Set(entity, new Name(TextStorage.GetID(product.Name)));

        var animation = CategoriesAndIngredients.GetIcon(product.Category);

        Set(entity, new SpriteAnimation(animation, 10, true, Rando.Int(0, animation.Frames.Length)));

        foreach (var c in CategoryFilter.Entities)
        {
            if (Get<Category>(c) == product.Category)
            {
                Relate(entity, c, new IsInCategory());
                break;
            }
        }

        // this is slow as shit! nevertheless, we ball -evan
        foreach (var ingredient in product.Ingredients)
        {
            foreach (var ingredientEntity in IngredientFilter.Entities)
            {
                if (Get<Ingredient>(ingredientEntity) == ingredient)
                {
                    Relate(entity, ingredientEntity, new HasIngredient());
                }
            }
        }

        return entity;
    }

    public Entity SpawnRandomProduct(Position position)
    {
        return SpawnProduct(position, Rando.GetRandomItem(Categories));
    }

    public void SpawnParticle(int x, int y, SpriteAnimation spriteAnimation)
    {
        var e = CreateEntity();
        var speed = 200 + Rando.Value * 100;
        Set(e, new Position(x, y));
        Set(e, new Depth(1));
        Set(e, spriteAnimation);
        Set(e, new SlowDownAnimation(5, 1));
        Set(e, new Velocity(Vector2.Rotate(Vector2.UnitX * speed, float.DegreesToRadians(Rando.Int(0, 360)))));
        Set(e, new FallSpeed(10));
        Set(e, new DestroyAtScreenBottom());
    }

    public void SpawnScoreEffect(Entity owner, Position target, SpriteAnimation spriteAnimation, int amount)
    {
        var maxTime = ((amount / 60f / 2f) + .1f) / 2f;
        for (var i = 0; i < amount; i++)
        {
            var e = CreateEntity();
            Set(e, new Depth(1));
            if (i % 2 == 0)
                Set(e, spriteAnimation);
            Set(e, new Velocity(new Vector2(Rando.Range(-10f, 10f), Rando.Range(-400f, -350f))));
            Set(e, new DestroyAtScreenBottom());
            Set(e, new AccelerateToPosition(target, 1300f, 1.01f));
            Set(e, new DestroyAtGameEnd());
            Relate(e, owner, new UpdateDisplayScoreOnDestroy());

            var timer = CreateEntity();
            float factor = (float)i / ((float)amount / 2);
            Set(timer, new Timer(factor * factor * factor * maxTime));
            Relate(timer, e, new TeleportToAtTimerEnd(owner));
            Set(timer, new DestroyAtGameEnd());
        }
    }

    public void ClearProducts()
	{
		foreach (var entity in ProductFilter.Entities)
		{
			Destroy(entity);
		}
	}

	// completely restock store
	// TODO: also reshuffle spawner categories
	public void SpawnAllProducts()
	{
		foreach (var spawner in ProductSpawnerFilter.Entities)
		{
			if (!HasInRelation<BelongsToProductSpawner>(spawner))
			{
				var position = Get<Position>(spawner);

				Category category;
				if (Has<SpawnCategory>(spawner))
				{
					category = Get<SpawnCategory>(spawner).Category;
				}
				else
				{
					category = Rando.GetRandomItem(Categories);
				}

				var product = SpawnProduct(position, category);
				Relate(product, spawner, new BelongsToProductSpawner());
			}
		}
	}
}
