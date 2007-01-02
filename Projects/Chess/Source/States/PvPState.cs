using System;
using Axiom;
using Axiom.Overlays;

namespace Chess.States
{
	/// <summary>
	/// Summary description for PvPState.
	/// </summary>
	public class PvPState: GameState
	{
		#region Singleton implementation

		private static PvPState instance;
		public PvPState()
		{
			if (instance == null) 
			{
				instance = this;
			}
		}
		public static PvPState Instance 
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
			if (gameAI!=null) gameAI.EndGame();
			gameAI.InitializeGame(); 
			// set the game type
			ColourState.players = ColourState.GameType.PvP;
			// change the colour menu options

			OverlayElementManager.Instance.GetElement("ColourMenu/1P").Text = "1P";
			OverlayElementManager.Instance.GetElement("ColourMenu/2P").Text = "2P";
		}
	}
}
