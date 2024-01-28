
using System;
using MoonWorks.Math.Float;
using MoonTools.ECS;
using MoonWorks.Graphics;
using GGJ2024.Components;
using GGJ2024.Data;
using GGJ2024.Content;
using GGJ2024.Relations;
using MoonWorks.Math;
using GGJ2024.Messages;

namespace GGJ2024.Systems;

public class Hold : MoonTools.ECS.System
{
	MoonTools.ECS.Filter TryHoldFilter;
	MoonTools.ECS.Filter CanHoldFilter;
	MoonTools.ECS.Filter CanBeHeldFilter;
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

		CanBeHeldFilter =
			FilterBuilder.Include<CanBeHeld>().Build();

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

					var spriteInfo = Get<SpriteAnimation>(o).SpriteAnimationInfo;
					Send(new SetAnimationMessage(
						o,
						new SpriteAnimation(spriteInfo, 90, true)
					));
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
			// Dropping
			var holding = OutRelationSingleton<Holding>(e);
			Remove<Velocity>(holding);
			UnrelateAll<Holding>(e);
			var spriteInfo = Get<SpriteAnimation>(holding).SpriteAnimationInfo;
			Send(new SetAnimationMessage(
				holding,
				new SpriteAnimation(spriteInfo, 90, true)
			));
			Set(holding, Get<Position>(holding) + new Position(0, 10));
		}
	}

	void SetHoldParameters(Entity e, float dt)
	{
		var holding = OutRelationSingleton<Holding>(e);
		var holderPos = Get<Position>(e);
		var holderDirection = Get<LastDirection>(e).Direction;

		Set(holding, holderPos + holderDirection * 16 + new Position(0, -10));
		var depth = MathHelper.Lerp(100, 10, Get<Position>(holding).Y / (float) Dimensions.GAME_H);
		Set(holding, new Depth(depth));
	}

	public void Inspect(Entity potentialHolder, Entity product)
	{
		var playerIndex = Get<Player>(potentialHolder).Index;

		var index = 0;
		if (Some<IsPopupBox>())
		{
			// jank to push old boxes farther back
			foreach (var (_, uiElement) in Relations<ShowingPopup>())
			{
				if (Has<IsPopupBox>(uiElement))
				{
					Set(uiElement, new Depth(11));
				}
				else
				{
					Set(uiElement, new Depth(9));
				}
			}

			// newly created popups will draw on top of older ones
			index = 1;
		}

		var font = Fonts.FromID(Fonts.KosugiID);

		var holderPosition = Get<Position>(potentialHolder);

		Relate(potentialHolder, product, new Inspecting());

		var xOffset = holderPosition.X < Dimensions.GAME_W * 3 / 4 ? 10 : -100;
		var yOffset = -30;

		var backgroundRect = CreateEntity();
		Set(backgroundRect, holderPosition + new Position(xOffset - 5, yOffset - 5));
		Set(backgroundRect, new DrawAsRectangle());
		Set(backgroundRect, new Depth(11 - index * 4));
		Set(backgroundRect, new IsPopupBox());

		if (playerIndex == 0)
		{
			Set(backgroundRect, new ColorBlend(Color.DarkGreen));
		}
		else
		{
			Set(backgroundRect, new ColorBlend(new Color(0, 52, 139)));
		}

		Relate(potentialHolder, backgroundRect, new ShowingPopup());

		var name = CreateEntity();
		Set(name, holderPosition + new Position(xOffset, yOffset));
		Set(name, new Text(Fonts.KosugiID, 10, Get<Name>(product).TextID, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		Set(name, new TextDropShadow(1, 1));
		Set(name, new Depth(9 - index * 4));

		Relate(potentialHolder, name, new ShowingPopup());

		font.TextBounds(
			TextStorage.GetString(Get<Name>(product).TextID),
			10,
			MoonWorks.Graphics.Font.HorizontalAlignment.Left,
			MoonWorks.Graphics.Font.VerticalAlignment.Top,
			out var textBounds
		);

		var textBoundsRectangle = TextRectangle(textBounds, new Position(xOffset - 5, yOffset - 5));

		yOffset += 15;

		var price = CreateEntity();
		Set(price, holderPosition + new Position(xOffset, yOffset));
		Set(price, new Text(Fonts.KosugiID, 10, "$" + Product.GetPrice(product).ToString("F2"), MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		Set(price, new TextDropShadow(1, 1));
		Set(price, new Depth(9 - index * 4));

		Relate(potentialHolder, price, new ShowingPopup());
		Relate(price, product, new DisplayingProductPrice());

		yOffset += 15;

		foreach (var ingredient in OutRelations<HasIngredient>(product))
		{
			var ingredientString = Get<Ingredient>(ingredient).ToString();
			var ingredientPriceString = "$" + Get<Price>(ingredient).Value.ToString("F2");

			var ingredientName = CreateEntity();
			Set(ingredientName, holderPosition + new Position(xOffset, yOffset));
			Set(ingredientName, new Text(Fonts.KosugiID, 8, ingredientString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
			Set(ingredientName, new TextDropShadow(1, 1));
			Set(ingredientName, new Depth(9 - index * 4));

			Relate(potentialHolder, ingredientName, new ShowingPopup());

			font.TextBounds(
				ingredientString,
				8,
				MoonWorks.Graphics.Font.HorizontalAlignment.Left,
				MoonWorks.Graphics.Font.VerticalAlignment.Top,
				out textBounds
			);

			textBoundsRectangle = Rectangle.Union(
				textBoundsRectangle,
				TextRectangle(textBounds, new Position(xOffset, yOffset))
			);

			var ingredientPrice = CreateEntity();
			Set(ingredientPrice, holderPosition + new Position(xOffset + textBounds.W + 3, yOffset));
			Set(ingredientPrice, new Text(Fonts.KosugiID, 8, ingredientPriceString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
			Set(ingredientPrice, new TextDropShadow(1, 1));
			Set(ingredientPrice, new Depth(9 - index * 4));

			Relate(potentialHolder, ingredientPrice, new ShowingPopup());
			Relate(ingredientPrice, ingredient, new DisplayingIngredientPrice());

			font.TextBounds(
				ingredientPriceString,
				8,
				MoonWorks.Graphics.Font.HorizontalAlignment.Left,
				MoonWorks.Graphics.Font.VerticalAlignment.Top,
				out textBounds
			);

			textBoundsRectangle = Rectangle.Union(
				textBoundsRectangle,
				TextRectangle(textBounds, new Position(xOffset, yOffset))
			);

			yOffset += 15;
		}

		textBoundsRectangle.Inflate(5, 5);

		Set(backgroundRect, new Rectangle(0, 0, textBoundsRectangle.Width, textBoundsRectangle.Height));
	}

	public void StopInspect(Entity potentialHolder)
	{
		foreach (var other in OutRelations<Inspecting>(potentialHolder))
		{
			Unrelate<Inspecting>(potentialHolder, other);
		}

		foreach (var other in OutRelations<ShowingPopup>(potentialHolder))
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
					if (Has<CanInspect>(holder) && Has<CanBeHeld>(other))
					{
						Inspect(holder, other);
						break;
					}
				}
			}

			if (HasOutRelation<Holding>(holder))
			{
				SetHoldParameters(holder, (float)delta.TotalSeconds);
			}
        }

		// real-time price updates
		foreach (var (uiText, product) in Relations<DisplayingProductPrice>())
		{
			Set(uiText, new Text(Fonts.KosugiID, 10, "$" + Product.GetPrice(product).ToString("F2"), MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		}

		foreach (var (uiText, ingredient) in Relations<DisplayingIngredientPrice>())
		{
			var ingredientPriceString = "$" + Get<Price>(ingredient).Value.ToString("F2");
			Set(uiText, new Text(Fonts.KosugiID, 8, ingredientPriceString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		}
    }

	private static Rectangle TextRectangle(WellspringCS.Wellspring.Rectangle textBounds, Position position)
	{
		return new Rectangle((int) textBounds.X + position.X, (int) textBounds.Y + position.Y, (int) textBounds.W, (int) textBounds.H);
	}
}
