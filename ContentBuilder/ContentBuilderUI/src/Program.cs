using System;
using System.IO;
using System.Runtime.InteropServices;
using MoonWorks;

namespace ContentBuilderUI
{
	class Program
	{
		static void Main(string[] args)
		{
			WindowCreateInfo windowCreateInfo = new WindowCreateInfo
			{
				WindowWidth = 800,
				WindowHeight = 800,
				WindowTitle = "ContentBuilderUI",
				ScreenMode = ScreenMode.Windowed,
				PresentMode = PresentMode.FIFORelaxed
			};

			FrameLimiterSettings frameLimiterSettings = new FrameLimiterSettings
			{
				Mode = FrameLimiterMode.Capped,
				Cap = 60
			};

			ContentBuilderUIGame game = new ContentBuilderUIGame(
				windowCreateInfo,
				frameLimiterSettings,
				true
			);

			game.Run();
		}
	}
}
