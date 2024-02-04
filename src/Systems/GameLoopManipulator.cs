using RollAndCash;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Messages;
using RollAndCash.Systems;
using MoonTools.ECS;
using MoonWorks.Graphics.Font;
using RollAndCash.Relations;
using System.IO;
using RollAndCash.Utility;
using MoonWorks;

public class GameLoopManipulator : MoonTools.ECS.Manipulator
{
	Filter ScoreFilter;
	Filter PlayerFilter;
	Filter GameTimerFilter;
	Filter ScoreScreenFilter;
	Filter DestroyAtGameEndFilter;

	ProductSpawner ProductSpawner;

	string[] ScoreStrings;
	public GameLoopManipulator(World world) : base(world)
	{
		PlayerFilter = FilterBuilder.Include<Player>().Build();
		ScoreFilter = FilterBuilder.Include<Score>().Build();
		GameTimerFilter = FilterBuilder.Include<RollAndCash.Components.GameTimer>().Build();
		ScoreScreenFilter = FilterBuilder.Include<IsScoreScreen>().Build();
		DestroyAtGameEndFilter = FilterBuilder.Include<DestroyAtGameEnd>().Build();

		ProductSpawner = new ProductSpawner(world);

		var scoreStringsFilePath = Path.Combine(
			System.AppContext.BaseDirectory,
			"Content",
			"Data",
			"score"
		);

		ScoreStrings = File.ReadAllLines(scoreStringsFilePath);
	}


	public void ShowScoreScreen()
	{
		Destroy(GetSingletonEntity<GameInProgress>());

		Send(new StopDroneSounds());
		Send(new PlayStaticSoundMessage(StaticAudio.Score));

		var scoreScreenEntity = CreateEntity();
		Set(scoreScreenEntity, new Position(0, 0));
		Set(scoreScreenEntity, new SpriteAnimation(SpriteAnimations.Score, 0));
		Set(scoreScreenEntity, new Depth(0.5f));
		Set(scoreScreenEntity, new IsScoreScreen());

		var p1Entity = CreateEntity();
		Set(p1Entity, new Position(Dimensions.GAME_W * 0.5f - 64.0f, Dimensions.GAME_H * 0.5f));
		Set(p1Entity, new SpriteAnimation(SpriteAnimations.Char_Walk_Down, 0));
		Set(p1Entity, new Depth(0.1f));
		Set(p1Entity, new IsScoreScreen());

		var p1Score = 0;
		var p2Score = 0;

		foreach (var player in PlayerFilter.Entities)
		{
			var p = Get<Player>(player);
			var score = Get<Score>(OutRelationSingleton<HasScore>(player));

			if (p.Index == 0)
				p1Score = score.Value;
			else
				p2Score = score.Value;

		}

		var trophy = CreateEntity();
		Set(trophy, new SpriteAnimation(SpriteAnimations.UI_Trophy, 10, true));
		Set(trophy, p1Score >= p2Score ?
			new Position(Dimensions.GAME_W * 0.5f - 64.0f, Dimensions.GAME_H * 0.5f - 64.0f) :
			new Position(Dimensions.GAME_W * 0.5f + 64.0f, Dimensions.GAME_H * 0.5f - 64.0f)
		);
		Set(trophy, new Depth(0.1f));
		Set(trophy, new IsScoreScreen());

		var p1ScoreEntity = CreateEntity();
		Set(p1ScoreEntity, new Position(Dimensions.GAME_W * 0.5f - 64.0f, Dimensions.GAME_H * 0.5f + 32.0f));
		Set(p1ScoreEntity, new Text(
			Fonts.KosugiID,
			FontSizes.SCORE,
			$"{p1Score}",
			HorizontalAlignment.Center,
			VerticalAlignment.Middle
		));
		Set(p1ScoreEntity, new Depth(0.1f));
		Set(p1ScoreEntity, new IsScoreScreen());

		var p2Entity = CreateEntity();
		Set(p2Entity, new Position(Dimensions.GAME_W * 0.5f + 64.0f, Dimensions.GAME_H * 0.5f));
		Set(p2Entity, new SpriteAnimation(SpriteAnimations.Char2_Walk_Down, 0));
		Set(p2Entity, new Depth(0.1f));
		Set(p2Entity, new IsScoreScreen());

		var p2ScoreEntity = CreateEntity();
		Set(p2ScoreEntity, new Position(Dimensions.GAME_W * 0.5f + 64.0f, Dimensions.GAME_H * 0.5f + 32.0f));
		Set(p2ScoreEntity, new Text(
			Fonts.KosugiID,
			FontSizes.SCORE,
			$"{p2Score}",
			HorizontalAlignment.Center,
			VerticalAlignment.Middle
		));
		Set(p2ScoreEntity, new Depth(0.1f));
		Set(p2ScoreEntity, new IsScoreScreen());

		var scoreStringEntity = CreateEntity();
		var str = ScoreStrings.GetRandomItem();
		var fontSize = FontSizes.SCORE_STRING;

		Set(scoreStringEntity, new Position(Dimensions.GAME_W * 0.5f, 32.0f));

		var font = Fonts.FromID(Fonts.KosugiID);

		font.TextBounds(
			str,
			fontSize,
			MoonWorks.Graphics.Font.HorizontalAlignment.Center,
			MoonWorks.Graphics.Font.VerticalAlignment.Middle,
			out var textBounds
		);

		while (textBounds.W > 640)
		{
			fontSize--;
			font.TextBounds(
				str,
				fontSize,
				MoonWorks.Graphics.Font.HorizontalAlignment.Left,
				MoonWorks.Graphics.Font.VerticalAlignment.Top,
				out textBounds
			);
		}

		Set(scoreStringEntity, new Text(
			Fonts.KosugiID,
			fontSize,
			$"{str}",
			HorizontalAlignment.Center,
			VerticalAlignment.Middle
		));

		Set(scoreStringEntity, new Depth(0.1f));
		Set(scoreStringEntity, new IsScoreScreen());

	}


	void BackToTitle()
	{
		foreach (var entity in ScoreScreenFilter.Entities)
		{
			Destroy(entity);
		}

		Send(new EndGame());
	}

	public void AdvanceGameState()
	{
		if (Some<IsScoreScreen>())
		{
			BackToTitle();
		}
		else
		{
			ShowScoreScreen();
		}
	}
}
