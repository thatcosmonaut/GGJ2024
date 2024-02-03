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

		GameState CurrentState;

		public RollAndCashGame(
			WindowCreateInfo windowCreateInfo,
			FrameLimiterSettings frameLimiterSettings,
			bool debugMode
		) : base(windowCreateInfo, frameLimiterSettings, 60, debugMode)
		{
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

			GameplayState = new GameplayState(this);
			CreditsState = new CreditsState(this, GameplayState);
			LogoState = new LogoState(this, CreditsState);

#if DEBUG
			SetState(GameplayState);
#else
			SetState(LogoState);
#endif
		}

		protected override void Update(System.TimeSpan dt)
		{
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
