using RollAndCash;
using RollAndCash.Components;
using RollAndCash.Content;
using RollAndCash.Messages;
using RollAndCash.Systems;
using MoonTools.ECS;

public class GameLoopManipulator : MoonTools.ECS.Manipulator
{
	Filter ScoreFilter;
	Filter PlayerFilter;
	Filter GameTimerFilter;

	ProductSpawner ProductSpawner;

	public GameLoopManipulator(World world) : base(world)
	{
		PlayerFilter = FilterBuilder.Include<Player>().Build();
		ScoreFilter = FilterBuilder.Include<Score>().Build();
		GameTimerFilter = FilterBuilder.Include<RollAndCash.Components.GameTimer>().Build();

		ProductSpawner = new ProductSpawner(world);
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

	public void Restart()
	{
		if (Some<IsTitleScreen>())
		{
			Destroy(GetSingletonEntity<IsTitleScreen>());
		}

		Set(GameTimerFilter.NthEntity(0), new RollAndCash.Components.GameTimer(90));

		var playerOne = PlayerFilter.NthEntity(0);
		var playerTwo = PlayerFilter.NthEntity(1);

		Set(playerOne, new Position(Dimensions.GAME_W * 0.47f + 0 * 48.0f, Dimensions.GAME_H * 0.25f));
		Set(playerTwo, new Position(Dimensions.GAME_W * 0.47f + 1 * 48.0f, Dimensions.GAME_H * 0.25f));

		foreach (var entity in ScoreFilter.Entities)
		{
			Set(entity, new Score(0));
			Set(entity, new Text(Fonts.KosugiID, Dimensions.SCORE_FONT_SIZE, "0"));
		}

		World.Send(new PlaySongMessage());

		// respawn products

		ProductSpawner.ClearProducts();
		ProductSpawner.SpawnProducts();

		// reset orders
	}
}
