
using System;
using MoonWorks.Math.Float;
using MoonTools.ECS;
using MoonWorks.Graphics;
using GGJ2024.Components;
using GGJ2024.Data;
using GGJ2024.Content;

namespace GGJ2024.Systems;

public class Hold : MoonTools.ECS.System
{
    MoonTools.ECS.Filter TryHoldFilter;
    MoonTools.ECS.Filter CanHoldFilter;
    float HoldSpeed = 300.0f;
    Product Product;

    public Hold(World world) : base(world)
    {
        TryHoldFilter =
            FilterBuilder
            .Include<Rectangle>()
            .Include<Position>()
            .Include<CanHold>()
            .Include<TryHold>()
            .Build();

        CanHoldFilter =
            FilterBuilder
            .Include<Rectangle>()
            .Include<Position>()
            .Include<CanHold>()
            .Build();
        Product = new Product(world);
    }

    void HoldOrDrop(Entity e)
    {
        if (!HasOutRelation<Holding>(e))
        {
            bool holding = false;

            foreach (var o in OutRelations<Colliding>(e))
            {
                if (Has<CanBeHeld>(o))
                {
                    holding = true;
                    Relate(e, o, new Holding());
                    var category = Get<Category>(OutRelationSingleton<IsInCategory>(o));
                    System.Console.Write($" {TextStorage.GetString(Get<Name>(o).TextID)} ${Product.GetPrice(o)} category: {category} ");

                    System.Console.Write("ingredience: ");
                    foreach (var ingredient in OutRelations<HasIngredient>(o))
                    {
                        System.Console.Write($"{Get<Ingredient>(ingredient)} (${Get<Price>(ingredient).Value}) ");
                    }
                    System.Console.WriteLine("");
                }
            }

            if (!holding)
            {
                foreach (var i in InRelations<Colliding>(e))
                {
                    if (Has<CanBeHeld>(i))
                    {
                        Set(i, Color.Yellow);
                        Relate(e, i, new Holding());

						StopInspect(e);
                    }
                }
            }
        }
        else
        {
            var holding = OutRelationSingleton<Holding>(e);
            Remove<Velocity>(holding);
            UnrelateAll<Holding>(e);
        }
    }

    void SetHoldVelocity(Entity e, float dt)
    {
        var holding = OutRelationSingleton<Holding>(e);
        var holdingPos = Get<Position>(holding);
        var holderPos = Get<Position>(e);

        var vel = holderPos - holdingPos;

        Set(holding, new Velocity(vel * HoldSpeed * dt));
    }

	public void Inspect(Entity potentialHolder, Entity product)
	{
		var holderPosition = Get<Position>(potentialHolder);

		Relate(potentialHolder, product, new Inspecting());

		var xOffset = holderPosition.X < Dimensions.GAME_W * 3 / 4 ? 10 : -100;
		var yOffset = -30;

		// TODO: set this rectangle based on font textbounds
		var backgroundRect = CreateEntity();
		Set(backgroundRect, holderPosition + new Position(xOffset - 5, yOffset - 5));
		Set(backgroundRect, new Rectangle(0, 0, 100, 100));
		Set(backgroundRect, new DrawAsRectangle());
		Set(backgroundRect, new ColorBlend(new Color(0, 52, 139)));

		Relate(potentialHolder, backgroundRect, new Displaying());

		var name = CreateEntity();
		Set(name, holderPosition + new Position(xOffset, yOffset));
		Set(name, new Text(Fonts.KosugiID, 10, Get<Name>(product).TextID, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		Set(name, new TextDropShadow(1, 1));

		Relate(potentialHolder, name, new Displaying());

		yOffset += 15;

		var price = CreateEntity();
		Set(price, holderPosition + new Position(xOffset, yOffset));
		Set(price, new Text(Fonts.KosugiID, 10, "$" + Product.GetPrice(product).ToString("F2"), MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		Set(price, new TextDropShadow(1, 1));

		Relate(potentialHolder, price, new Displaying());

		yOffset += 15;

		foreach (var ingredient in OutRelations<HasIngredient>(product))
		{
			var ingredientString = Get<Ingredient>(ingredient).ToString();
			var ingredientPriceString = "$" + Get<Price>(ingredient).Value.ToString("F2");

			var ingredientName = CreateEntity();
			Set(ingredientName, holderPosition + new Position(xOffset, yOffset));
			Set(ingredientName, new Text(Fonts.KosugiID, 8, ingredientString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
			Set(ingredientName, new TextDropShadow(1, 1));

			Relate(potentialHolder, ingredientName, new Displaying());

			Fonts.FromID(Fonts.KosugiID).TextBounds(
				ingredientString,
				10,
				MoonWorks.Graphics.Font.HorizontalAlignment.Left,
				MoonWorks.Graphics.Font.VerticalAlignment.Top,
				out var textBounds
			);

			var ingredientPrice = CreateEntity();
			Set(ingredientPrice, holderPosition + new Position(xOffset + textBounds.W + 3, yOffset));
			Set(ingredientPrice, new Text(Fonts.KosugiID, 8, ingredientPriceString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
			Set(ingredientPrice, new TextDropShadow(1, 1));

			Relate(potentialHolder, ingredientPrice, new Displaying());

			yOffset += 15;
		}
	}

	public void StopInspect(Entity potentialHolder)
	{
		foreach (var other in OutRelations<Inspecting>(potentialHolder))
		{
			Unrelate<Inspecting>(potentialHolder, other);
		}

		foreach (var other in OutRelations<Displaying>(potentialHolder))
		{
			Destroy(other);
		}
	}

    public override void Update(TimeSpan delta)
    {
        foreach (var holder in CanHoldFilter.Entities)
        {
			if (HasOutRelation<Inspecting>(holder))
			{
				var inspectedProduct = OutRelationSingleton<Inspecting>(holder);
				if (!Related<Colliding>(holder, inspectedProduct))
				{
					StopInspect(holder);
				}
			}

            if (Has<TryHold>(holder))
			{
                HoldOrDrop(holder);
			}
			else if (!HasOutRelation<Inspecting>(holder))
			{
				foreach (var other in OutRelations<Colliding>(holder))
				{
					if (Has<CanBeHeld>(other))
					{
						Inspect(holder, other);
						break;
					}
				}
			}

            if (HasOutRelation<Holding>(holder))
			{
                SetHoldVelocity(holder, (float)delta.TotalSeconds);
			}
        }

    }
}
