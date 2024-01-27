using MoonWorks.Graphics;
using MoonWorks;

namespace GGJ2024
{
	class GGJ2024Game : Game
	{
		public GGJ2024Game(
			WindowCreateInfo windowCreateInfo,
			FrameLimiterSettings frameLimiterSettings,
			bool debugMode
		) : base(windowCreateInfo, frameLimiterSettings, 60, debugMode)
		{
			// Insert your game initialization logic here.
		}

		protected override void Update(System.TimeSpan dt)
		{
			// Insert your game update logic here.
		}

		protected override void Draw(double alpha)
		{
			// Replace this with your own drawing code.

			var commandBuffer = GraphicsDevice.AcquireCommandBuffer();
			var swapchainTexture = commandBuffer.AcquireSwapchainTexture(MainWindow);

			if (swapchainTexture != null)
			{
				commandBuffer.BeginRenderPass(
					new ColorAttachmentInfo(swapchainTexture, Color.CornflowerBlue)
				);

				commandBuffer.EndRenderPass();
			}

			GraphicsDevice.Submit(commandBuffer);
		}

		protected override void Destroy()
		{

		}
	}
}
