using System;
using System.IO;
using System.Runtime.InteropServices;
using MoonWorks;
using MoonTools.ECS;
using GGJ2024.Systems;
using GGJ2024.Components;

namespace GGJ2024
{
	class Program
	{
		static void Main(string[] args)
		{
			WindowCreateInfo windowCreateInfo = new WindowCreateInfo
			{
				WindowWidth = 1280,
				WindowHeight = 720,
				WindowTitle = "GGJ2024",
				ScreenMode = ScreenMode.Windowed,
				PresentMode = PresentMode.FIFORelaxed
			};

			FrameLimiterSettings frameLimiterSettings = new FrameLimiterSettings
			{
				Mode = FrameLimiterMode.Capped,
				Cap = 60
			};

			var debugMode = false;

#if DEBUG
			debugMode = true;
#endif

			GGJ2024Game game = new GGJ2024Game(
				windowCreateInfo,
				frameLimiterSettings,
				debugMode
			);

			game.Run();
		}
	}
}
