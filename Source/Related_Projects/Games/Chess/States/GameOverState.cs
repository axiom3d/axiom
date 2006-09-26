using System;
using Axiom;
using Axiom.Overlays;

namespace Chess.States
{
	/// <summary>
	/// Summary description for GameOverState.
	/// </summary>
	public class GameOverState: MenuState
	{
		#region Singleton implementation

		private static GameOverState instance;
		public GameOverState()
		{
			if (instance == null) 
			{
				instance = this;

				menuOverlay = OverlayManager.Instance.GetByName("Menu/GameOverMenu"); 
			}
		}
		public static GameOverState Instance 
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
		public void DisplayResult(string outcome, string winner)
		{
			OverlayElementManager.Instance.GetElement("GameOverMenu/Outcome").Text = outcome;
			OverlayElementManager.Instance.GetElement("GameOverMenu/Winner").Text = winner;
		}

	
		public override void KeyPressed(object sender, Axiom.Input.KeyEventArgs e)
		{
			base.KeyPressed(sender,e);
			// Return to main menu
			ChangeState(MainMenuState.Instance);
		}
	}
}
