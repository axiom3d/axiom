using System;
using Chess.Main;
using Axiom.Core;
using Axiom;
using Axiom.Overlays;
namespace Chess.States
{
	/// <summary>
	/// Summary description for MainMenuState.
	/// </summary>
	public class MainMenuState: MenuState
	{
		#region Singleton implementation

		private static MainMenuState instance;
		public MainMenuState()
		{
			if (instance == null) 
			{
				instance = this;


				// Set this menu's overlay
				menuOverlay = OverlayManager.Instance.GetByName("Menu/MainMenu");

				// Add emenu items
				menuItems.Add(OverlayElementManager.Instance.GetElement("MainMenu/PvCPU"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("MainMenu/PvP"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("MainMenu/Options"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("MainMenu/Help"));
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
		public override void Delete()
		{
			base.Delete();
		}
		protected override void OnSelected(int item)
		{
			OverlayElement element = (OverlayElement)(this.menuItems[item]);
			if (element.Name == "MainMenu/PvCPU")
			{
				// Start a new game
				ChangeState(PvCPUState.Instance);
				// Choose a color
				ChangeState(ColourState.Instance);
			}
			else if (element.Name == "MainMenu/PvP")
			{
				// Start a new game
				ChangeState(PvPState.Instance);
				// Choose a color
				ChangeState(ColourState.Instance);

			}
			else if (element.Name == "MainMenu/Options")
			{
				// Show highscores
				ChangeState(OptionState.Instance);
			}
			else if (element.Name == "MainMenu/Help")
			{
				// Show highscores
				ChangeState(HelpState.Instance);
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
