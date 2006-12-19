using System;
using Axiom;
using Axiom.Overlays;

namespace Chess.States
{
	/// <summary>
	/// Summary description for PvCPUState.
	/// </summary>
	public class PvCPUState: GameState
	{
		#region Singleton implementation

		private static PvCPUState instance;
		public PvCPUState()
		{
			if (instance == null) 
			{
				instance = this;
			}
		}
		public static PvCPUState Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion

		public override void Delete()
		{
			base.Delete();
		}
		public override void Initialize()
		{
			// Initialize game state
			base.Initialize();
			// finish any running games
			if (gameAI!=null) 
			{
					gameAI.EndGame();
			}
			gameAI.InitializeGame(); 
			// set the game type
			ColourState.players = ColourState.GameType.PvCPU;
			// change the colour menu options

			OverlayElementManager.Instance.GetElement("ColourMenu/1P").Text = "1P";
			OverlayElementManager.Instance.GetElement("ColourMenu/2P").Text = "CPU";
		}
	}
}
