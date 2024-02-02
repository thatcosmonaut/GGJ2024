using RollAndCash;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Messages;
using RollAndCash.Systems;
using MoonTools.ECS;
using System.Reflection.Metadata;
using Microsoft.VisualBasic;
using System.Numerics;
using MoonWorks.Graphics.Font;
using RollAndCash.Relations;

public class GameLoopManipulator : MoonTools.ECS.Manipulator
{
	Filter ScoreFilter;
	Filter PlayerFilter;
	Filter GameTimerFilter;
	Filter ScoreScreenFilter;

	RollAndCash.Systems.ProductSpawner ProductSpawner;

	public GameLoopManipulator(World world) : base(world)
	{
		PlayerFilter = FilterBuilder.Include<Player>().Build();
		ScoreFilter = FilterBuilder.Include<Score>().Build();
		GameTimerFilter = FilterBuilder.Include<RollAndCash.Components.GameTimer>().Build();
		ScoreScreenFilter = FilterBuilder.Include<IsScoreScreen>().Build();

		ProductSpawner = new RollAndCash.Systems.ProductSpawner(world);
	}

	public void ShowTitleScreen()
	{
		var titleScreenEntity = CreateEntity();
		Set(titleScreenEntity, new Position(0, 0));
		Set(titleScreenEntity, new SpriteAnimation(SpriteAnimations.Title, 0));
		Set(titleScreenEntity, new Depth(0.02f));
		Set(titleScreenEntity, new IsTitleScreen());

		Send(new PlayTitleMusic());
		Send(new PlayStaticSoundMessage(StaticAudio.RollAndCash));
	}

	public void ShowScoreScreen()
	{
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

		var p1ScoreEntity = CreateEntity();
		Set(p1ScoreEntity, new Position(Dimensions.GAME_W * 0.5f - 64.0f, Dimensions.GAME_H * 0.5f + 32.0f));
		Set(p1ScoreEntity, new Text(
			Fonts.KosugiID,
			FontSizes.SCORE,
			$"{p1Score}",
			HorizontalAlignment.Center,
			VerticalAlignment.Middle
		));
		Set(p1ScoreEntity, new TextDropShadow(1, 1));
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
		Set(p2ScoreEntity, new TextDropShadow(1, 1));
		Set(p2ScoreEntity, new Depth(0.1f));
		Set(p2ScoreEntity, new IsScoreScreen());

	}

	void StartGame()
	{
		Destroy(GetSingletonEntity<IsTitleScreen>());
		Set(GameTimerFilter.NthEntity(0), new RollAndCash.Components.GameTimer(Time.RoundTime));

		var playerOne = PlayerFilter.NthEntity(0);
		var playerTwo = PlayerFilter.NthEntity(1);

		Set(playerOne, new Position(Dimensions.GAME_W * 0.47f + 0 * 48.0f, Dimensions.GAME_H * 0.25f));
		Set(playerTwo, new Position(Dimensions.GAME_W * 0.47f + 1 * 48.0f, Dimensions.GAME_H * 0.25f));

		foreach (var entity in ScoreFilter.Entities)
		{
			Set(entity, new Score(0));
			Set(entity, new Text(Fonts.KosugiID, FontSizes.SCORE, "0"));
		}

		World.Send(new PlaySongMessage());

		// respawn products

		ProductSpawner.ClearProducts();
		ProductSpawner.SpawnProducts();

		// reset orders
	}

	void BackToTitle()
	{
		foreach (var entity in ScoreScreenFilter.Entities)
			Destroy(entity);
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
