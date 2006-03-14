using System;

using Axiom;


namespace YAT 
{

	public class HelpState: MenuState
	{
		#region Singleton implementation

		private static HelpState instance;
		public HelpState()
		{
			if (instance == null) 
			{
				instance = this;
				// Set this menu's overlay
				menuOverlay = OverlayManager.Instance.GetByName("Menu/HelpMenu");
				input = TetrisApplication.Instance.Input;
			}
		}
		public static HelpState Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion

		#region Methods

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