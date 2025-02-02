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
				ScreenMode = ScreenMode.Windowed
			};

			FramePacingSettings framePacingSettings = FramePacingSettings.CreateLatencyOptimized(60);

			var appInfo = new AppInfo("JerryCrew", "RollAndCashContentBuilder");

			ContentBuilderUIGame game = new ContentBuilderUIGame(
				appInfo,
				windowCreateInfo,
				framePacingSettings,
				true
			);

			game.Run();
		}
	}
}
