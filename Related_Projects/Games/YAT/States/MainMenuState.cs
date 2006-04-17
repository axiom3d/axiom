using System;

using Axiom;

namespace YAT 
{

	public class MainMenuState: MenuState
	{
		#region Singleton implementation

		private static MainMenuState instance;
		public MainMenuState()
		{
			if (instance == null) 
			{
				instance = this;
				input = TetrisApplication.Instance.Input;

				// Set this menu's overlay
				menuOverlay = OverlayManager.Instance.GetByName("Menu/MainMenu");

				// Add emenu items
				menuItems.Add(OverlayElementManager.Instance.GetElement("MainMenu/NewGame"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("MainMenu/Help"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("MainMenu/Highscores"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("MainMenu/Quit"));

			}
		}
		public static MainMenuState Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion

		#region Methods

		protected override void OnSelected(int item)
		{
			OverlayElement element = (OverlayElement)(this.menuItems[item]);
			if (element.Name == "MainMenu/NewGame")
			{
				// Pop the main menu state
				StateManager.Instance.RemoveCurrentState();

				// Start a new game
				ChangeState(NewGameState.Instance);
			}
			else if (element.Name == "MainMenu/Help")
			{
				// Show help
				ChangeState(HelpState.Instance);
			}
			else if (element.Name == "MainMenu/Highscores")
			{
				// Show highscores
				ChangeState(HighscoresState.Instance);
			}
			else if (element.Name == "MainMenu/Quit")
			{
				// Tell the state manager to quit
				StateManager.Instance.Quit();
			}
		}

		#endregion

	}
}