using MoonWorks.Graphics;
using MoonWorks;
using MoonTools.ECS;
using GGJ2024.Systems;

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
			Renderer = new Renderer(GraphicsDevice, MainWindow.SwapchainFormat);
		}

		protected override void Update(System.TimeSpan dt)
		{
			// Insert your game update logic here.
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
