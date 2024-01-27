using MoonWorks.Graphics;
using MoonWorks;
using MoonTools.ECS;
using GGJ2024.Systems;
using System.IO;
using System;
using MoonWorks.Math.Float;

namespace GGJ2024
{
	class GGJ2024Game : Game
	{

		Renderer Renderer;
		World World = new World();
		Input Input;

		public GGJ2024Game(
			WindowCreateInfo windowCreateInfo,
			FrameLimiterSettings frameLimiterSettings,
			bool debugMode
		) : base(windowCreateInfo, frameLimiterSettings, 60, debugMode)
		{
			Input = new Input(World, Inputs);
			Renderer = new Renderer(World, GraphicsDevice, MainWindow.SwapchainFormat);

			var rect = World.CreateEntity();
			World.Set(rect, new Position(0, 0));
			World.Set(rect, new Rectangle(0, 0, 32, 32));
		}

		protected override void Update(System.TimeSpan dt)
		{
			Input.Update(dt);
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
