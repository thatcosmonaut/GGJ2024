using MoonWorks.Graphics;
using MoonWorks;
using MoonTools.ECS;
using RollAndCash.Systems;
using MoonWorks.Math.Float;
using RollAndCash.Content;
using RollAndCash.Components;
using RollAndCash.Utility;
using RollAndCash.Data;
using RollAndCash.Messages;
using RollAndCash.Relations;
using LD54.Systems;

namespace RollAndCash
{
	class RollAndCashGame : Game
	{
		Renderer Renderer;
		World World = new World();
		Input Input;
		Motion Motion;
		Audio Audio;
		Hold Hold;
		RollAndCash.Systems.ProductSpawner ProductSpawner;
		Product ProductManipulator;
		Ticker Ticker;
		Systems.GameTimer GameTimer;
		Timing Timing;
		Orders Orders;
		SetSpriteAnimationSystem SetSpriteAnimationSystem;
		DirectionalAnimation DirectionalAnimation;
		UpdateSpriteAnimationSystem UpdateSpriteAnimationSystem;
		ColorAnimation ColorAnimation;
		NPCController NPCController;

		PlayerController PlayerController;

		GameLoopManipulator GameLoopManipulator;

		public RollAndCashGame(
			WindowCreateInfo windowCreateInfo,
			FrameLimiterSettings frameLimiterSettings,
			bool debugMode
		) : base(windowCreateInfo, frameLimiterSettings, 60, debugMode)
		{
			TextureAtlases.LoadAll();
			SpriteAnimations.LoadAll();
			ProductLoader.Load();

			var commandBuffer = GraphicsDevice.AcquireCommandBuffer();
			TextureAtlases.TP_Sprites.Load(GraphicsDevice, commandBuffer);
			GraphicsDevice.Submit(commandBuffer);

			StaticAudioPacks.LoadAll(AudioDevice);
			StaticAudio.LoadAll();
			StreamingAudio.InitAll(AudioDevice);
			Fonts.LoadAll(GraphicsDevice);

			GameLoopManipulator = new GameLoopManipulator(World);

			GameTimer = new(World);
			Timing = new(World);
			Input = new Input(World, Inputs);
			Motion = new Motion(World);
			Audio = new Audio(World, AudioDevice);
			PlayerController = new PlayerController(World);
			Hold = new Hold(World);
			Orders = new Orders(World);
			ProductSpawner = new RollAndCash.Systems.ProductSpawner(World);
			ProductManipulator = new Product(World);
			SetSpriteAnimationSystem = new SetSpriteAnimationSystem(World);
			UpdateSpriteAnimationSystem = new UpdateSpriteAnimationSystem(World);
			ColorAnimation = new ColorAnimation(World);
			DirectionalAnimation = new DirectionalAnimation(World);
			NPCController = new NPCController(World);

			CategoriesAndIngredients cats = new CategoriesAndIngredients(World);
			cats.Initialize(World);

			Ticker = new Ticker(World, cats);

			Renderer = new Renderer(World, GraphicsDevice, MainWindow.SwapchainFormat);

			NPCController.SpawnNPC();

			var orderOne = World.CreateEntity();
			World.Set(orderOne, new Position(195, 345));
			World.Set(orderOne, new IsOrder());

			var orderTwo = World.CreateEntity();
			World.Set(orderTwo, new Position(320, 345));
			World.Set(orderTwo, new IsOrder());

			var orderThree = World.CreateEntity();
			World.Set(orderThree, new Position(440, 345));
			World.Set(orderThree, new IsOrder());

			Orders.SetNewOrderDetails(orderOne);
			Orders.SetNewOrderDetails(orderTwo);
			Orders.SetNewOrderDetails(orderThree);

			var topBorder = World.CreateEntity();
			World.Set(topBorder, new Position(0, 65));
			World.Set(topBorder, new Rectangle(0, 0, Dimensions.GAME_W, 10));
			World.Set(topBorder, new Solid());

			var leftBorder = World.CreateEntity();
			World.Set(leftBorder, new Position(-10, 0));
			World.Set(leftBorder, new Rectangle(0, 0, 10, Dimensions.GAME_H));
			World.Set(leftBorder, new Solid());

			var rightBorder = World.CreateEntity();
			World.Set(rightBorder, new Position(Dimensions.GAME_W, 0));
			World.Set(rightBorder, new Rectangle(0, 0, 10, Dimensions.GAME_H));
			World.Set(rightBorder, new Solid());

			var bottomBorder = World.CreateEntity();
			World.Set(bottomBorder, new Position(0, Dimensions.GAME_H));
			World.Set(bottomBorder, new Rectangle(0, 0, Dimensions.GAME_W, 10));
			World.Set(bottomBorder, new Solid());

			var background = World.CreateEntity();
			World.Set(background, new Position(0, 0));
			World.Set(background, new Depth(999));
			World.Set(background, new SpriteAnimation(Content.SpriteAnimations.BG, 0));

			var uiTickerBackground = World.CreateEntity();
			World.Set(uiTickerBackground, new Position(0, 0));
			World.Set(uiTickerBackground, new Depth(1));
			World.Set(uiTickerBackground, new SpriteAnimation(Content.SpriteAnimations.HUD_Ticker, 0));

			var uiBottomBackground = World.CreateEntity();
			World.Set(uiBottomBackground, new Position(0, Dimensions.GAME_H - 40));
			World.Set(uiBottomBackground, new Depth(9));
			World.Set(uiBottomBackground, new SpriteAnimation(Content.SpriteAnimations.HUD_Bottom, 0));

			var cashRegister = World.CreateEntity();
			World.Set(cashRegister, new Position(8, 77));
			World.Set(cashRegister, new Rectangle(0, 0, 80, 90));
			World.Set(cashRegister, new CanInteract());
			World.Set(cashRegister, new CanFillOrders());
			World.Set(cashRegister, Color.ForestGreen);

			var exit = World.CreateEntity();
			World.Set(exit, new Position(Dimensions.GAME_W * 0.5f - 44, 0));
			World.Set(exit, new Rectangle(0, 0, 80, 88));
			World.Set(exit, new StoreExit());
			World.Set(exit, new CanInteract());

			var timer = World.CreateEntity();
			World.Set(timer, new Components.GameTimer(90));
			World.Set(timer, new Position(Dimensions.GAME_W - 40, 38));

			var scoreOne = World.CreateEntity();
			World.Set(scoreOne, new Position(80, 345));
			World.Set(scoreOne, new Score(0));
			World.Set(scoreOne, new Text(Fonts.KosugiID, Dimensions.SCORE_FONT_SIZE, "0"));

			var scoreTwo = World.CreateEntity();
			World.Set(scoreTwo, new Position(560, 345));
			World.Set(scoreTwo, new Score(0));
			World.Set(scoreTwo, new Text(Fonts.KosugiID, Dimensions.SCORE_FONT_SIZE, "0"));

			var playerOne = PlayerController.SpawnPlayer(0);
			var playerTwo = PlayerController.SpawnPlayer(1);

			World.Relate(playerOne, scoreOne, new HasScore());
			World.Relate(playerTwo, scoreTwo, new HasScore());

			GameLoopManipulator.ShowTitleScreen();

			//GameLoopManipulator.Restart();
		}

		protected override void Update(System.TimeSpan dt)
		{
			Timing.Update(dt);
			UpdateSpriteAnimationSystem.Update(dt);
			GameTimer.Update(dt);
			Ticker.Update(dt);
			Input.Update(dt);
			PlayerController.Update(dt);
			NPCController.Update(dt);
			Motion.Update(dt);
			Hold.Update(dt);
			Orders.Update(dt);
			DirectionalAnimation.Update(dt);
			SetSpriteAnimationSystem.Update(dt);
			ColorAnimation.Update(dt);
			Audio.Update(dt);

			World.FinishUpdate();
		}

		protected override void Draw(double alpha)
		{
			Renderer.Render(MainWindow);
		}

		protected override void Destroy()
		{

		}
	}
}
