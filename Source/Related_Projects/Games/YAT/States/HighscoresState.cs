using System;

using Axiom;

namespace YAT 
{

	public class HighscoresState: MenuState
	{
		#region Singleton implementation

		private static HighscoresState instance;
		public HighscoresState()
		{
			if (instance == null) 
			{
				instance = this;
				input = TetrisApplication.Instance.Input;
				menuOverlay = OverlayManager.Instance.GetByName("Menu/HighscoresMenu");
				input = TetrisApplication.Instance.Input;
			}
		}
		public static HighscoresState Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion

		#region Methods
		public override void Initialize()
		{
			base.Initialize();

			OverlayManager overlayManager = OverlayManager.Instance;
			OverlayElement element;

			//issue
			for (int i = 0; i < game.mHighscores.getScoreCount(); ++i)
			{
				element = OverlayElementManager.Instance.GetElement("HighscoresMenu/Name" + i.ToString());
				element.Text = (game.mHighscores.getName(i));

				element = OverlayElementManager.Instance.GetElement("HighscoresMenu/Points" + i.ToString());
				element.Text = (game.mHighscores.getScore(i).ToString());
			}
		}

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