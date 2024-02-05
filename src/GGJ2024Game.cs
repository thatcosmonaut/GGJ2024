using MoonWorks.Graphics;
using MoonWorks;
using RollAndCash.Systems;
using RollAndCash.Content;
using RollAndCash.GameStates;

namespace RollAndCash
{
	public class RollAndCashGame : Game
	{
		LogoState LogoState;
		CreditsState CreditsState;
		GameplayState GameplayState;
		TitleState TitleState;
		HowToPlayState HowToPlayState;

		GameState CurrentState;

		public RollAndCashGame(
			WindowCreateInfo windowCreateInfo,
			FrameLimiterSettings frameLimiterSettings,
			bool debugMode
		) : base(windowCreateInfo, frameLimiterSettings, 60, debugMode)
		{
			Inputs.Mouse.Hidden = true;

			TextureAtlases.LoadAll();
			SpriteAnimations.LoadAll();
			ProductLoader.Load();

			var commandBuffer = GraphicsDevice.AcquireCommandBuffer();
			TextureAtlases.TP_Sprites.Load(GraphicsDevice, commandBuffer);
			TextureAtlases.TP_HiRes.Load(GraphicsDevice, commandBuffer);
			GraphicsDevice.Submit(commandBuffer);

			StaticAudioPacks.LoadAll(AudioDevice);
			StaticAudio.LoadAll();
			StreamingAudio.InitAll(AudioDevice);
			Fonts.LoadAll(GraphicsDevice);


			CreditsState = new CreditsState(this, TitleState);
			LogoState = new LogoState(this, CreditsState, TitleState);
			TitleState = new TitleState(this, LogoState, HowToPlayState);
			CreditsState.SetTransitionState(TitleState); // i hate this

			GameplayState = new GameplayState(this, TitleState);
			HowToPlayState = new HowToPlayState(this, GameplayState);
			TitleState.SetTransitionStateB(HowToPlayState);

			SetState(LogoState);

		}

		protected override void Update(System.TimeSpan dt)
		{
			if (Inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.F11))
			{
				if (MainWindow.ScreenMode == ScreenMode.Fullscreen)
					MainWindow.SetScreenMode(ScreenMode.Windowed);
				else
					MainWindow.SetScreenMode(ScreenMode.Fullscreen);

			}

			CurrentState.Update(dt);
		}

		protected override void Draw(double alpha)
		{
			CurrentState.Draw(MainWindow, alpha);
		}

		protected override void Destroy()
		{

		}

		public void SetState(GameState gameState)
		{
			if (CurrentState != null)
			{
				CurrentState.End();
			}

			gameState.Start();
			CurrentState = gameState;
		}
	}
}
