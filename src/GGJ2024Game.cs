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
		Ticker Ticker;
		Systems.GameTimer GameTimer;
		Timing Timing;
		Orders Orders;
		SetSpriteAnimationSystem SetSpriteAnimationSystem;
		UpdateSpriteAnimationSystem UpdateSpriteAnimationSystem;

		PlayerController PlayerController;

		public GGJ2024Game(
			WindowCreateInfo windowCreateInfo,
			FrameLimiterSettings frameLimiterSettings,
			bool debugMode
		) : base(windowCreateInfo, frameLimiterSettings, 60, debugMode)
		{
			TextureAtlases.LoadAll();
			SpriteAnimations.LoadAll();

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
			SetSpriteAnimationSystem = new SetSpriteAnimationSystem(World);
			UpdateSpriteAnimationSystem = new UpdateSpriteAnimationSystem(World);

			Renderer = new Renderer(World, GraphicsDevice, MainWindow.SwapchainFormat);

			CategoriesAndIngredients cats = new CategoriesAndIngredients(World);
			cats.Initialize(World);

			Ticker = new Ticker(World, cats);

			var cashRegister = World.CreateEntity();
			World.Set(cashRegister, new Position(Vector2.Zero));
			World.Set(cashRegister, new Rectangle(0, 0, 32, 32));
			World.Set(cashRegister, new CanFillOrders());
			World.Set(cashRegister, Color.ForestGreen);


			var ordersKiosk = World.CreateEntity();
			World.Set(ordersKiosk, new Position(Dimensions.GAME_W - 32, Dimensions.GAME_H - 32));
			World.Set(ordersKiosk, new Rectangle(0, 0, 32, 32));
			World.Set(ordersKiosk, new CanGiveOrders());
			World.Set(ordersKiosk, Color.Orange);

			var timer = World.CreateEntity();
			World.Set(timer, new Components.GameTimer(260));
			World.Set(timer, new Position(Dimensions.GAME_W / 2, Dimensions.GAME_H * 3 / 4));

			PlayerController.SpawnPlayer(0);
			PlayerController.SpawnPlayer(1);
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
