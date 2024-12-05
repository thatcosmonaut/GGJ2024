using RollAndCash;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Messages;
using RollAndCash.Systems;
using MoonTools.ECS;
using MoonWorks.Graphics.Font;
using RollAndCash.Relations;
using System.IO;
using System.Numerics;
using RollAndCash.Utility;

public class GameLoopManipulator : MoonTools.ECS.Manipulator
{
	Filter ScoreFilter;
	Filter PlayerFilter;
	Filter GameTimerFilter;
	Filter ScoreScreenFilter;
	Filter DestroyAtGameEndFilter;

	DroneSpawner DroneSpawner;
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
		DroneSpawner = new DroneSpawner(world);

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

		// Find Highest Score
		var highestScore = -999f;
		var tie = false;
		foreach (var score in ScoreFilter.Entities)
		{
			var value = Get<Score>(score).Value;

			if (value >= highestScore)
			{
				if (value == highestScore)
				{
					tie = true;
				}
				highestScore = value;
			}
		}

		// Spawn Players + HUD entities
		foreach (var player in PlayerFilter.Entities)
		{
			var playerIndex = Get<Player>(player).Index;
			var score = Get<Score>(OutRelationSingleton<HasScore>(player)).Value;
			var x = Dimensions.GAME_W * 0.5f - 64.0f + 128.0f * playerIndex;
			var y = Dimensions.GAME_H * .7f;
			var sprite = playerIndex == 0 ? SpriteAnimations.Char_Walk_Down : SpriteAnimations.Char2_Walk_Down;

			var playerEntity = CreateEntity();
			Set(playerEntity, new Position(x, y));
			Set(playerEntity, new SpriteAnimation(sprite, 0));
			Set(playerEntity, new Depth(0.1f));
			Set(playerEntity, new IsScoreScreen());
			Set(playerEntity, new SpriteScale(new Vector2(2, 2)));

			// Spawn trophy for winning player
			var countUpTime = 1.4f;
			if (score == highestScore)
			{

				var trophySprite = tie ? SpriteAnimations.NPC_DroneEvil_Fly_Down : SpriteAnimations.UI_Trophy;
				countUpTime = 1.8f + score / 3000;
				var trophy = CreateEntity();
				Set(trophy, new SpriteAnimation(trophySprite, 140, true));
				Set(trophy, new SlowDownAnimation(15, 1));
				Set(trophy, new Position(x, y - 32));
				Set(trophy, new Velocity(new Vector2(0, -330)));
				Set(trophy, new MotionDamp(10));
				Set(trophy, new Depth(0.1f));
				Set(trophy, new IsScoreScreen());
				Set(trophy, new SpriteScale(new Vector2(2, 2)));

				var timer = CreateEntity();
				Set(timer, new Timer(countUpTime + 1f));
				Set(timer, new PlaySoundOnTimerEnd(new PlayStaticSoundMessage(
					tie ? Rando.GetRandomItem(DroneSpawner.EvilDroneSounds) : StaticAudio.OrderComplete
				)));
				Relate(trophy, timer, new DontMove());
				Relate(trophy, timer, new DontDraw());
			}

			// Score below
			var scoreEntity = CreateEntity();
			Set(scoreEntity, new Position(x, y + 38));
			Set(scoreEntity, new Depth(0.1f));
			Set(scoreEntity, new IsScoreScreen());
			Set(scoreEntity, new Text());
			Set(scoreEntity, new LastValue(0));
			var scoreTimer = CreateEntity();
			Set(scoreTimer, new Timer(countUpTime));
			Relate(scoreEntity, scoreTimer, new CountUpScore(0, score));
			var dontDrawTextTimer = CreateEntity();
			Set(dontDrawTextTimer, new Timer(.4f));
			Relate(scoreTimer, dontDrawTextTimer, new DontTime());
			Relate(scoreEntity, dontDrawTextTimer, new DontDraw());
		}

		var scoreTitleEntity = CreateEntity();
		var str = ScoreStrings.GetRandomItem();
		var fontSize = FontSizes.SCORE_STRING;

		Set(scoreTitleEntity, new Position(Dimensions.GAME_W * 0.5f, 32.0f));

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

		Set(scoreTitleEntity, new Text(
			Fonts.KosugiID,
			fontSize,
			$"{str}",
			HorizontalAlignment.Center,
			VerticalAlignment.Middle
		));

		Set(scoreTitleEntity, new Depth(0.1f));
		Set(scoreTitleEntity, new IsScoreScreen());

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
