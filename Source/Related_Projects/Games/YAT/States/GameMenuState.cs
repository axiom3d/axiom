using System;

using Axiom;


namespace YAT 
{
	public class GameMenuState: MenuState
	{
		#region Singleton implementation

		private static GameMenuState instance;
		public GameMenuState()
		{
			if (instance == null) 
			{
				instance = this;
				// Set this menu's overlay
				menuOverlay = OverlayManager.Instance.GetByName("Menu/GameMenu");

				// Add emenu items
				menuItems.Add(OverlayElementManager.Instance.GetElement("GameMenu/ResumeGame"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("GameMenu/AbortGame"));
			}
		}
		public static GameMenuState Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion

		#region Methods
		// State overrides
		public override void FrameStarted(float dt)
		{
			base.FrameStarted(dt);
		}

		public override void KeyPressed(Axiom.KeyEventArgs e)
		{
			if (e.Key == Axiom.Input.KeyCodes.Escape)
				StateManager.Instance.RemoveCurrentState();
			else
				base.KeyPressed(e);
		}



		protected override void OnSelected(int item)
		{
			OverlayElement element = (OverlayElement)(this.menuItems[item]);
			if (element.Name == "GameMenu/ResumeGame")
			{
				// Resume game
				StateManager.Instance.RemoveCurrentState();
			}
			else if (element.Name == "GameMenu/AbortGame")
			{
				if (game.mHighscores.GetPlace(game.mPoints) >= 0)
				{
					// New highscore!
					ChangeState(NewHighscoreState.Instance);
				}
				else
				{
					// Return to the main menu
					ChangeState(MainMenuState.Instance);
				}
			}
		}

		#endregion

	}
}