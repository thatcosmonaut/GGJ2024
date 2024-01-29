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
		public static string UserDataDirectory = $"{Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "ROLLANDCASH")}";

		static void Main(string[] args)
		{
			if (!System.IO.Directory.Exists(UserDataDirectory))
			{
				System.IO.Directory.CreateDirectory(UserDataDirectory);
			}

			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

			WindowCreateInfo windowCreateInfo = new WindowCreateInfo
			{
				WindowWidth = 1280,
				WindowHeight = 720,
				WindowTitle = "GGJ2024",
				ScreenMode = ScreenMode.Fullscreen,
				PresentMode = PresentMode.Immediate
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

		static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			Exception e = (Exception) args.ExceptionObject;
			Logger.LogError("Unhandled exception caught!");
			Logger.LogError(e.ToString());

			Game.ShowRuntimeError("FLAGRANT SYSTEM ERROR", e.ToString());

			StreamWriter streamWriter = new StreamWriter(Path.Combine(UserDataDirectory, "log.txt"));

			streamWriter.WriteLine(e.ToString());
			streamWriter.Flush();
			streamWriter.Close();
		}
	}
}
