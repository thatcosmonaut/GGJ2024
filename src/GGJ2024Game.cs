using MoonWorks.Graphics;
using MoonWorks;
using MoonTools.ECS;
using GGJ2024.Systems;
using MoonWorks.Math.Float;
using GGJ2024.Content;

namespace GGJ2024
{
	class GGJ2024Game : Game
	{
		Renderer Renderer;
		World World = new World();
		Input Input;
		Motion Motion;
		Audio Audio;

		public GGJ2024Game(
			WindowCreateInfo windowCreateInfo,
			FrameLimiterSettings frameLimiterSettings,
			bool debugMode
		) : base(windowCreateInfo, frameLimiterSettings, 60, debugMode)
		{
			StaticAudioPacks.LoadAll(AudioDevice);
			StaticAudio.LoadAll();

			Input = new Input(World, Inputs);
			Motion = new Motion(World);
			Audio = new Audio(World, AudioDevice);
			Renderer = new Renderer(World, GraphicsDevice, MainWindow.SwapchainFormat);

			var rect = World.CreateEntity();
			World.Set(rect, new Position(0f, Dimensions.GAME_H * 0.5f));
			World.Set(rect, new Rectangle(0, 0, 16, 16));
			World.Set(rect, new Velocity(Vector2.UnitX));

			var rect2 = World.CreateEntity();
			World.Set(rect2, new Position(Dimensions.GAME_W * 0.5f, (Dimensions.GAME_H * 0.5f) - 64));
			World.Set(rect2, new Rectangle(0, 0, 128, 128));
		}

		protected override void Update(System.TimeSpan dt)
		{
			Input.Update(dt);
			Motion.Update(dt);
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
