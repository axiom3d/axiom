using System;

using Axiom;

namespace YAT 
{

	public class GameOverState: MenuState
	{
		#region Singleton implementation

		private static GameOverState instance;
		public GameOverState()
		{
			if (instance == null) 
			{
				instance = this;
				input = TetrisApplication.Instance.Input;
				// Set this menu's overlay
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

		#region Methods
		// State overrides

		public override void KeyPressed(Axiom.KeyEventArgs e)
		{
			base.KeyPressed(e);

			// Return to main menu
			ChangeState(MainMenuState.Instance);
		}


		protected override void OnSelected(int item)
		{

		}

		public override void HandleInput()
		{

			if(enterKey.KeyDownEvent()) 
			{
				// Return to main menu
				ChangeState(MainMenuState.Instance);
			}

		}
		#endregion
	}
}