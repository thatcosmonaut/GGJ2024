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

	public void ShowTitleScreen()
	{
		var titleScreenEntity = CreateEntity();
		Set(titleScreenEntity, new Position(0, 0));
		Set(titleScreenEntity, new SpriteAnimation(SpriteAnimations.Title, 0));
		Set(titleScreenEntity, new Depth(0.02f));
		Set(titleScreenEntity, new IsTitleScreen());

		Send(new PlayTitleMusic());
		Send(new PlayStaticSoundMessage(StaticAudio.RollAndCash, RollAndCash.Data.SoundCategory.Generic, 1.5f));
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

	void StartGame()
	{
		Destroy(GetSingletonEntity<IsTitleScreen>());

		var gameInProgressEntity = CreateEntity();
		Set(gameInProgressEntity, new GameInProgress());

		Set(GameTimerFilter.NthEntity(0), new RollAndCash.Components.GameTimer(Time.ROUND_TIME));

		var playerOne = PlayerFilter.NthEntity(0);
		var playerTwo = PlayerFilter.NthEntity(1);

		Set(playerOne, new Position(Dimensions.GAME_W * 0.47f + 0 * 48.0f, Dimensions.GAME_H * 0.25f));
		Set(playerTwo, new Position(Dimensions.GAME_W * 0.47f + 1 * 48.0f, Dimensions.GAME_H * 0.25f));

		foreach (var entity in ScoreFilter.Entities)
		{
			Set(entity, new Score(0));
			Set(entity, new DisplayScore(0));
			Set(entity, new Text(Fonts.KosugiID, FontSizes.SCORE, "0"));
		}

		World.Send(new PlaySongMessage());


		foreach (var entity in DestroyAtGameEndFilter.Entities)
		{
			Destroy(entity);
		}

		// respawn products

		ProductSpawner.ClearProducts();
		ProductSpawner.SpawnAllProducts();

		// reset orders
	}

	void BackToTitle()
	{
		foreach (var entity in ScoreScreenFilter.Entities)
		{
			Destroy(entity);
		}

		ShowTitleScreen();
	}

	public void AdvanceGameState()
	{
		if (Some<IsTitleScreen>())
		{
			StartGame();
		}
		else if (Some<IsScoreScreen>())
		{
			BackToTitle();
		}
		else
		{
			ShowScoreScreen();
		}
	}
}
