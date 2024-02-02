using System;
using MoonTools.ECS;
using RollAndCash.Utility;
using RollAndCash.Components;
using MoonWorks.Math.Float;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using RollAndCash.Data;
using RollAndCash.Relations;
using MoonWorks.Math;

namespace RollAndCash.Systems;

public readonly record struct ProductData(string Name, Category Category, Ingredient[] Ingredients);

public class Product : MoonTools.ECS.Manipulator
{
    Filter CategoryFilter;
    Filter IngredientFilter;

    public Product(World world) : base(world)
    {
        CategoryFilter = FilterBuilder.Include<Category>().Build();
        IngredientFilter = FilterBuilder.Include<Ingredient>().Build();
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

        foreach (var ingredient in IngredientFilter.Entities)
        {
            if (product.Ingredients.Contains(Get<Ingredient>(ingredient)))
            {
                Relate(entity, ingredient, new HasIngredient());
            }
        }

        return entity;
    }

    int spawnStepDistance = 32;
    public void SpawnShelf(int x, int y, int width, int height, Category category)
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

                Relate(shelf, productSpawnerEntity, new Relations.ProductSpawner());
            }
        }
    }

    public void SpawnShelf(int x, int y, int width, int height)
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

                Relate(shelf, productSpawnerEntity, new Relations.ProductSpawner());
            }
        }
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
}
