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

        return MathF.Round(MathF.Max(float.Epsilon, total), 2);
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

        SpriteAnimationInfo animation = null;
        switch (product.Category)
        {
            case Category.Animals:
                animation = Content.SpriteAnimations.Item_Animal;
                break;
            case Category.Clothes:
                animation = Content.SpriteAnimations.Item_Clothing;
                break;
            case Category.Cosmetics:
                animation = Content.SpriteAnimations.Item_Cosmetics;
                break;
            case Category.Electronics:
                animation = Content.SpriteAnimations.Item_Electronics;
                break;
            case Category.Food:
                animation = Content.SpriteAnimations.Item_Food;
                break;
            case Category.Furniture:
                animation = Content.SpriteAnimations.Item_Furniture;
                break;
            case Category.Gasses:
                animation = Content.SpriteAnimations.Item_Gasses;
                break;
            case Category.IntellectualProperty:
                animation = Content.SpriteAnimations.Item_IP;
                break;
            case Category.Pharmacy:
                animation = Content.SpriteAnimations.Item_Pharmacy;
                break;
            case Category.Relics:
                animation = Content.SpriteAnimations.Item_Relic;
                break;
            default:
                break;
        }

        if (animation != null)
        {
            Set(entity, new SpriteAnimation(animation, 10, true, Rando.Int(0, animation.Frames.Length)));
        }

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
}
