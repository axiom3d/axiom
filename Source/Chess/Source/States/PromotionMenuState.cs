using System;
using Chess.Main;
using Chess.AI;
using Axiom;
using Axiom.Overlays;

namespace Chess.States
{
	/// <summary>
	/// Summary description for PromotionMenuState.
	/// </summary>
	public class PromotionMenuState: MenuState
	{
		#region Singleton implementation

		private static PromotionMenuState instance;
		public PromotionMenuState()
		{
			if (instance == null) 
			{
				instance = this;


				// Set this menu's overlay
				menuOverlay = OverlayManager.Instance.GetByName("Menu/OptionMenu");

				// Add emenu items
				menuItems.Add(OverlayElementManager.Instance.GetElement("PromotionMenu/Queen"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("PromotionMenu/Rook"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("PromotionMenu/Bishop"));
				menuItems.Add(OverlayElementManager.Instance.GetElement("PromotionMenu/Knight"));
			}
		}
		public static PromotionMenuState Instance 
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
			  // set which piece is to be promoted
			GameAI.Instance.SetPromotion(element.Text);
			// leave this state
			StateManager.Instance.RemoveCurrentState();
		}
	}
}
