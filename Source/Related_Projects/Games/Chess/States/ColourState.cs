using System;
using Chess.AI;
using Chess.Main;
using Axiom;
using Axiom.Overlays;

namespace Chess.States
{
	/// <summary>
	/// Summary description for ColourState.
	/// </summary>
	public class ColourState: MenuState
	{
		public enum GameType {CPUvCPU, PvCPU, PvP};
		public static GameType players = GameType.PvCPU;

		#region Singleton implementation

		private static ColourState instance;
		public ColourState()
		{
			if (instance == null) 
			{
				instance = this;

				// Set this menu's overlay
				menuOverlay = OverlayManager.Instance.GetByName("Menu/ColourMenu");

				// Add emenu items
				menuItems.Add(OverlayElementManager.Instance.GetElement("ColourMenu/1P"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("ColourMenu/2P"));
			}
		}
		public static ColourState Instance 
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
		protected override void OnSelected(int item)
		{
			OverlayElement element = (OverlayElement)(this.menuItems[item]);
			if (element.Text == "1P")
			{
				// Pop the main menu state
				StateManager.Instance.RemoveCurrentState();
  
				// set up the players
				if (ColourState.players == ColourState.GameType.PvP)     
				{		  
					gameAI.SetPlayer(Player.SIDE_WHITE, Player.TYPE_HUMAN);
					gameAI.SetPlayer(Player.SIDE_BLACK, Player.TYPE_HUMAN);  
				} else 
				{		  
					gameAI.SetPlayer(Player.SIDE_WHITE, Player.TYPE_HUMAN);
					gameAI.SetPlayer(Player.SIDE_BLACK, Player.TYPE_AI);
				}		
				}
			else if (element.Text == "2P")
				{		
				// Pop the main menu state
				StateManager.Instance.RemoveCurrentState();  
				// set up the players    		  
				gameAI.SetPlayer(Player.SIDE_WHITE, Player.TYPE_HUMAN);
				gameAI.SetPlayer(Player.SIDE_BLACK, Player.TYPE_HUMAN);      
				}
			else if (element.Text == "CPU")
				{		
				// Pop the main menu state
				StateManager.Instance.RemoveCurrentState();  
				// set up the players    		  
				gameAI.SetPlayer(Player.SIDE_WHITE, Player.TYPE_AI);
				gameAI.SetPlayer(Player.SIDE_BLACK, Player.TYPE_HUMAN);      
				}
			// start the game AI
			gameAI.Start(); 

		}
	}
}
