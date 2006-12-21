using System;
using Axiom;
using Chess.Main;
using Axiom.Overlays;

namespace Chess.States
{
	/// <summary>
	/// Summary description for GameMenuState.
	/// </summary>
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
				menuItems.Add(OverlayElementManager.Instance.GetElement("GameMenu/Resign"));
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
		public override void Delete()
		{
			base.Delete();
		}
		public override void KeyPressed(object sender, Axiom.Input.KeyEventArgs e)
		{
			// Handle keys common to all game states
			switch (e.Key)
			{
				case Axiom.Input.KeyCodes.Escape:
					StateManager.Instance.RemoveCurrentState();
					break;
				default:
					base.KeyPressed(sender,e);
					break;
			}
		}
		protected override void OnSelected(int item)
		{
			 // leave this menu
			StateManager.Instance.RemoveCurrentState();

			OverlayElement element = (OverlayElement)(this.menuItems[item]);
			if (element.Name == "GameMenu/Resign")
			{
				// Pop the main menu state
				StateManager.Instance.AddState(CPUvCPUState.Instance);
				// Start a new game
				ChangeState(MainMenuState.Instance);

			}

		}
	}
}
