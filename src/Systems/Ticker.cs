using System;
using System.Text;
using GGJ2024.Components;
using GGJ2024.Content;
using MoonTools.ECS;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;

namespace GGJ2024.Systems;

public class Ticker : MoonTools.ECS.System
{
	MoonTools.ECS.Filter TickerTextFilter;

	MoonTools.ECS.Random Random;

	string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ ";
	StringBuilder StringBuilder = new StringBuilder();

	CategoriesAndIngredients CategoriesAndIngredients;

	const int PRICE_SPEED = -10;
	const int NEWS_SPEED = -40;

	public Ticker(World world, CategoriesAndIngredients categoriesAndIngredients) : base(world)
	{
		TickerTextFilter = FilterBuilder
			.Include<Position>()
			.Include<TickerText>()
			.Build();

		Random = new MoonTools.ECS.Random();

		CategoriesAndIngredients = categoriesAndIngredients;
	}

	public override void Update(TimeSpan delta)
	{
		var farthestRight = 0f;

		foreach (var tickerTextEntity in TickerTextFilter.Entities)
		{
			var position = Get<Position>(tickerTextEntity);
			var width = Get<TickerText>(tickerTextEntity).Width;

			if (position.X + width > farthestRight)
			{
				farthestRight = position.X + width;
			}

			if (position.X + width < -5)
			{
				Destroy(tickerTextEntity);
			}
		}

		if (farthestRight < Dimensions.GAME_W)
		{
			if (Random.Next(10) == 0)
			{
				SpawnNews(farthestRight);
			}
			else
			{
				SpawnTickerText(farthestRight);
			}
		}
	}

	private void SpawnTickerText(float farthestRight)
	{
		var (price, change, ingredient) = CategoriesAndIngredients.ChangePrice();
		var symbol = CategoriesAndIngredients.GetStockTicker(ingredient);
		var priceString = price.ToString("F2");
		var changeString = change.ToString("F2");

		Fonts.FromID(Fonts.PixeltypeID).TextBounds(
			symbol,
			16,
			MoonWorks.Graphics.Font.HorizontalAlignment.Left,
			MoonWorks.Graphics.Font.VerticalAlignment.Top,
			out var textBounds
		);

		var x = farthestRight + 20;

		var tickerSymbolText = World.CreateEntity();
		World.Set(tickerSymbolText, new Position(x, 5));
		World.Set(tickerSymbolText, new Velocity(PRICE_SPEED, 0));
		World.Set(tickerSymbolText, new Text(Fonts.PixeltypeID, 16, symbol, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		World.Set(tickerSymbolText, new TickerText(textBounds.W));

		if (change > 0)
		{
			World.Set(tickerSymbolText, new ColorBlend(Color.LimeGreen));
		}
		else
		{
			World.Set(tickerSymbolText, new ColorBlend(Color.Red));
		}

		x += textBounds.W + 5;

		var tickerPriceText = World.CreateEntity();
		World.Set(tickerPriceText, new Position(x, 5));
		World.Set(tickerPriceText, new Velocity(PRICE_SPEED, 0));
		World.Set(tickerPriceText, new Text(Fonts.PixeltypeID, 16, priceString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));

		if (change > 0)
		{
			World.Set(tickerPriceText, new ColorBlend(Color.LimeGreen));
		}
		else
		{
			World.Set(tickerPriceText, new ColorBlend(Color.Red));
		}

		Fonts.FromID(Fonts.PixeltypeID).TextBounds(
			priceString,
			16,
			MoonWorks.Graphics.Font.HorizontalAlignment.Left,
			MoonWorks.Graphics.Font.VerticalAlignment.Top,
			out textBounds
		);

		World.Set(tickerPriceText, new TickerText(textBounds.W));

		x += textBounds.W + 5;

		var tickerChangeText = World.CreateEntity();
		World.Set(tickerChangeText, new Position(x, 5));
		World.Set(tickerChangeText, new Velocity(PRICE_SPEED, 0));
		World.Set(tickerChangeText, new Text(Fonts.PixeltypeID, 16, changeString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));

		Fonts.FromID(Fonts.PixeltypeID).TextBounds(
			changeString,
			16,
			MoonWorks.Graphics.Font.HorizontalAlignment.Left,
			MoonWorks.Graphics.Font.VerticalAlignment.Top,
			out textBounds
		);

		World.Set(tickerChangeText, new TickerText(textBounds.W));

		if (change > 0)
		{
			World.Set(tickerChangeText, new ColorBlend(Color.LimeGreen));
		}
		else
		{
			World.Set(tickerChangeText, new ColorBlend(Color.Red));
		}

		// slow down all entities
		foreach (var entity in TickerTextFilter.Entities)
		{
			Set(entity, new Velocity(PRICE_SPEED, 0));
		}
	}

	private void SpawnNews(float farthestRight)
	{
		var str = RandomString(70);

		Fonts.FromID(Fonts.PixeltypeID).TextBounds(
			str,
			16,
			HorizontalAlignment.Left,
			VerticalAlignment.Top,
			out var textBounds
		);

		var x = farthestRight + 20;

		var newsEntity = World.CreateEntity();
		Set(newsEntity, new Position(x, 5));
		Set(newsEntity, new Velocity(NEWS_SPEED, 0));
		Set(newsEntity, new Text(Fonts.PixeltypeID, 16, str, HorizontalAlignment.Left, VerticalAlignment.Top));
		Set(newsEntity, new TickerText(textBounds.W));

		// speed up all entities
		foreach (var entity in TickerTextFilter.Entities)
		{
			Set(entity, new Velocity(NEWS_SPEED, 0));
		}
	}

	private string RandomString(int length)
	{
		StringBuilder.Clear();
		for (var i = 0; i < length; i += 1)
		{
			var randomCharIndex = Random.Next(Chars.Length);
			StringBuilder.Append(Chars[randomCharIndex]);
		}
		return StringBuilder.ToString();
	}
}