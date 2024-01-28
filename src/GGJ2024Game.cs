using MoonWorks.Graphics;
using MoonWorks;
using MoonTools.ECS;
using GGJ2024.Systems;
using MoonWorks.Math.Float;
using GGJ2024.Content;
using GGJ2024.Components;
using GGJ2024.Utility;
using GGJ2024.Data;
using GGJ2024.Messages;
using GGJ2024.Relations;
using LD54.Systems;

namespace GGJ2024
{
	class GGJ2024Game : Game
	{
		Renderer Renderer;
		World World = new World();
		Input Input;
		Motion Motion;
		Audio Audio;
		Hold Hold;
		ProductSpawner ProductSpawner;
		Product ProductManipulator;
		Ticker Ticker;
		Systems.GameTimer GameTimer;
		Timing Timing;
		Orders Orders;
		SetSpriteAnimationSystem SetSpriteAnimationSystem;
		UpdateSpriteAnimationSystem UpdateSpriteAnimationSystem;
		ColorAnimation ColorAnimation;

		PlayerController PlayerController;

		public GGJ2024Game(
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
			Fonts.LoadAll(GraphicsDevice);

			GameTimer = new(World);
			Timing = new(World);
			Input = new Input(World, Inputs);
			Motion = new Motion(World);
			Audio = new Audio(World, AudioDevice);
			PlayerController = new PlayerController(World);
			Hold = new Hold(World);
			Orders = new Orders(World);
			ProductSpawner = new ProductSpawner(World);
			ProductManipulator = new Product(World);
			SetSpriteAnimationSystem = new SetSpriteAnimationSystem(World);
			UpdateSpriteAnimationSystem = new UpdateSpriteAnimationSystem(World);
			ColorAnimation = new ColorAnimation(World);

			CategoriesAndIngredients cats = new CategoriesAndIngredients(World);
			cats.Initialize(World);

			Ticker = new Ticker(World, cats);

			Renderer = new Renderer(World, GraphicsDevice, MainWindow.SwapchainFormat);

			var orderOne = World.CreateEntity();
			World.Set(orderOne, new Position(240, 280));
			World.Set(orderOne, new IsOrder());

			var orderTwo = World.CreateEntity();
			World.Set(orderTwo, new Position(320, 280));
			World.Set(orderTwo, new IsOrder());

			var orderThree = World.CreateEntity();
			World.Set(orderThree, new Position(400, 280));
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

			var ordersKiosk = World.CreateEntity();
			World.Set(ordersKiosk, new Position(Dimensions.GAME_W - 32, Dimensions.GAME_H - 32));
			World.Set(ordersKiosk, new Rectangle(0, 0, 32, 32));
			World.Set(ordersKiosk, new CanGiveOrders());
			World.Set(ordersKiosk, Color.Orange);

			var timer = World.CreateEntity();
			World.Set(timer, new Components.GameTimer(260));
			World.Set(timer, new Position(Dimensions.GAME_W / 2 + 10, Dimensions.GAME_H - 20));

			var scoreOne = World.CreateEntity();
			World.Set(scoreOne, new Position(100, 280));
			World.Set(scoreOne, new Score(0));
			World.Set(scoreOne, new Text(Fonts.KosugiID, 8, "0"));

			var scoreTwo = World.CreateEntity();
			World.Set(scoreTwo, new Position(540, 280));
			World.Set(scoreTwo, new Score(0));
			World.Set(scoreTwo, new Text(Fonts.KosugiID, 8, "0"));

			var playerOne = PlayerController.SpawnPlayer(0);
			var playerTwo = PlayerController.SpawnPlayer(1);

			World.Relate(playerOne, scoreOne, new HasScore());
			World.Relate(playerTwo, scoreTwo, new HasScore());
		}

		protected override void Update(System.TimeSpan dt)
		{
			Timing.Update(dt);
			UpdateSpriteAnimationSystem.Update(dt);
			GameTimer.Update(dt);
			Ticker.Update(dt);
			Input.Update(dt);
			PlayerController.Update(dt);
			Motion.Update(dt);
			Hold.Update(dt);
			Audio.Update(dt);
			ProductSpawner.Update(dt);
			Orders.Update(dt);
			SetSpriteAnimationSystem.Update(dt);
			ColorAnimation.Update(dt);

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
