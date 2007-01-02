using System;
using Axiom;
using Chess.AI;
using Chess.Main;
using Axiom.Overlays;

namespace Chess.States
{
	/// <summary>
	/// Summary description for OptionState.
	/// </summary>
	public class OptionState: MenuState
	{
		#region Singleton implementation

		private static OptionState instance;
		public OptionState()
		{
			if (instance == null) 
			{
				instance = this;



				// Set this menu's overlay
				menuOverlay = OverlayManager.Instance.GetByName("Menu/OptionMenu");

				// Add emenu items
				menuItems.Add(OverlayElementManager.Instance.GetElement("OptionMenu/Easy"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("OptionMenu/Medium"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("OptionMenu/Hard"));
			}
		}
		public static OptionState Instance 
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
			if (element.Name == "OptionMenu/Easy")
			{
				 GameAI.Instance.SetDifficulty("Easy");
			}
			else if (element.Name == "OptionMenu/Medium")
			{
				 GameAI.Instance.SetDifficulty("Medium");
			}
			else if (element.Name == "OptionMenu/Hard")
			{
				 GameAI.Instance.SetDifficulty("Hard");
			}

			// Return to main menu
			ChangeState(MainMenuState.Instance);
		}
	}
}
