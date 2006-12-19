using System;
using Chess.Main;
using Axiom;
using Axiom.Overlays;

namespace Chess.States
{
	/// <summary>
	/// Summary description for HelpState.
	/// </summary>
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
		public override void Delete()
		{
			base.Delete();
		}
		public override void KeyPressed(object sender, Axiom.Input.KeyEventArgs e)
		{
			base.KeyPressed(sender,e);
			ChangeState(MainMenuState.Instance);
		}
	}
}
