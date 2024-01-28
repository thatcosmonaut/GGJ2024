using System;
using System.Text;
using GGJ2024.Components;
using GGJ2024.Content;
using MoonTools.ECS;
using MoonWorks.Graphics;

namespace GGJ2024.Systems;

public class Ticker : MoonTools.ECS.System
{
	MoonTools.ECS.Filter TickerTextFilter;

	MoonTools.ECS.Random Random;

	string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	StringBuilder StringBuilder = new StringBuilder();

	public Ticker(World world) : base(world)
	{
		TickerTextFilter = FilterBuilder
			.Include<Position>()
			.Include<TickerText>()
			.Build();

		Random = new MoonTools.ECS.Random();
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
			SpawnTickerText();
		}
	}

	private void SpawnTickerText()
	{
		var symbol = RandomTickerSymbol();
		var change = Random.NextSingle() * 5 - 2.5f;
		var changeString = change.ToString("F2");

		Fonts.FromID(Fonts.PixeltypeID).TextBounds(
			symbol,
			16,
			MoonWorks.Graphics.Font.HorizontalAlignment.Left,
			MoonWorks.Graphics.Font.VerticalAlignment.Top,
			out var textBounds
		);

		var tickerText = World.CreateEntity();
		World.Set(tickerText, new Position(Dimensions.GAME_W + 10, 5));
		World.Set(tickerText, new Velocity(-10, 0));
		World.Set(tickerText, new Text(Fonts.PixeltypeID, 16, symbol, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		World.Set(tickerText, new TickerText(textBounds.W));

		if (change > 0)
		{
			World.Set(tickerText, new ColorBlend(Color.LimeGreen));
		}
		else
		{
			World.Set(tickerText, new ColorBlend(Color.Red));
		}

		Fonts.FromID(Fonts.PixeltypeID).TextBounds(
			changeString,
			16,
			MoonWorks.Graphics.Font.HorizontalAlignment.Left,
			MoonWorks.Graphics.Font.VerticalAlignment.Top,
			out textBounds
		);

		var tickerNumberText = World.CreateEntity();
		World.Set(tickerNumberText, new Position(Dimensions.GAME_W + 20 + textBounds.W, 5));
		World.Set(tickerNumberText, new Velocity(-10, 0));
		World.Set(tickerNumberText, new Text(Fonts.PixeltypeID, 16, changeString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		World.Set(tickerNumberText, new TickerText(textBounds.W));

		if (change > 0)
		{
			World.Set(tickerNumberText, new ColorBlend(Color.LimeGreen));
		}
		else
		{
			World.Set(tickerNumberText, new ColorBlend(Color.Red));
		}
	}

	private string RandomTickerSymbol()
	{
		StringBuilder.Clear();
		for (var i = 0; i < 4; i += 1)
		{
			var randomCharIndex = Random.Next(Chars.Length);
			StringBuilder.Append(Chars[randomCharIndex]);
		}
		return StringBuilder.ToString();
	}
}
