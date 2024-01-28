using MoonWorks.Graphics;
using MoonWorks;
using MoonTools.ECS;
using GGJ2024.Systems;
using MoonWorks.Math.Float;
using GGJ2024.Content;
using GGJ2024.Components;
using GGJ2024.Utility;
using GGJ2024.Data;

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
		Timer Timer;

		PlayerController PlayerController;

		public GGJ2024Game(
			WindowCreateInfo windowCreateInfo,
			FrameLimiterSettings frameLimiterSettings,
			bool debugMode
		) : base(windowCreateInfo, frameLimiterSettings, 60, debugMode)
		{
			StaticAudioPacks.LoadAll(AudioDevice);
			StaticAudio.LoadAll();
			Fonts.LoadAll(GraphicsDevice);

			Timer = new Timer(World);
			Ticker = new Ticker(World);
			Input = new Input(World, Inputs);
			Motion = new Motion(World);
			Audio = new Audio(World, AudioDevice);
			PlayerController = new PlayerController(World);
			Hold = new Hold(World);
			ProductSpawner = new ProductSpawner(World);
			Renderer = new Renderer(World, GraphicsDevice, MainWindow.SwapchainFormat);

			CategoriesAndIngredients cats = new CategoriesAndIngredients(World);
			cats.Initialize(World);

			var player = World.CreateEntity();
			World.Set(player, new Position(0f, Dimensions.GAME_H * 0.5f));
			World.Set(player, new Rectangle(0, 0, 16, 16));
			World.Set(player, new Player(0));
			World.Set(player, new CanHold());
			World.Set(player, new Solid());
			World.Set(player, Color.Green);

			var timer = World.CreateEntity();
			World.Set(timer, new GameTimer(260));
			World.Set(timer, new Position(Dimensions.GAME_W / 2, Dimensions.GAME_H * 3 / 4));
		}

		protected override void Update(System.TimeSpan dt)
		{
			Timer.Update(dt);
			Ticker.Update(dt);
			Input.Update(dt);
			PlayerController.Update(dt);
			Motion.Update(dt);
			Hold.Update(dt);
			Audio.Update(dt);
			ProductSpawner.Update(dt);

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
