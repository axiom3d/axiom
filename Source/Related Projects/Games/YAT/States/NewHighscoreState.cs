using System;

using Axiom;

namespace YAT 
{

	public class NewHighscoreState: MenuState
	{
		#region Fields
		protected String name;
		protected bool needsUpdate;
		#endregion


		#region Singleton implementation

		private static NewHighscoreState instance;
		public NewHighscoreState()
		{
			if (instance == null) 
			{
				instance = this;
				input = TetrisApplication.Instance.Input;
				menuOverlay = OverlayManager.Instance.GetByName("Menu/NewHighscoreMenu");
			}
		}
		public static NewHighscoreState Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion

		
		#region Method Overrides
		public override void Initialize()
		{
			int place;
			string placeStr;

			base.Initialize();

			// Get highscore place
			place = 1 + game.mHighscores.GetPlace(game.mPoints);
			placeStr = place.ToString();
			switch (place)
			{
				case 1:
					placeStr += "st";
					break;

				case 2:
					placeStr += "nd";
					break;

				case 3:
					placeStr += "rd";
					break;

				default:
					placeStr += "th";
					break;
			}

			// Update you placed text
			OverlayElementManager.Instance.GetElement("NewHighscoreMenu/YouPlacedText").Text = (
				"You placed " + placeStr + "...");
		}

		public override void KeyPressed(Axiom.KeyEventArgs e)
		{
			switch (e.Key)
			{
			case Axiom.Input.KeyCodes.Return:
			case Axiom.Input.KeyCodes.Enter:

				// Do not allow empty names
				if (name != string.Empty)
					break;

				// Add the highscore
				game.mHighscores.addHighscore(name, game.mPoints);

				// Display highscores
				ChangeState(HighscoresState.Instance);
				break;

			default:
				{
					char ch = e.KeyChar;

					if (name.Length < 16 && ch != '\0' &&
						ch != '#' && ch != '@' &&
						ch != '\t' && ch != ':' && ch != '=')
					{
						name += ch;//issue possible here
						needsUpdate = true;
					}

					// Also let TetrisState have a look
					base.KeyPressed(e);
				}
				break;
			}
		}
		public override void KeyRepeated(Axiom.Input.KeyCodes kc)
		{
			if ((kc == Axiom.Input.KeyCodes.Backspace) && name != string.Empty)
			{
				name.Remove(name.Length,1);//issue possible here
				needsUpdate = true;
			}
		}
		public override void FrameStarted(float dt)
		{
			if (needsUpdate)
			{

				OverlayElement element;

				// Update highscore name
				element = OverlayElementManager.Instance.GetElement("NewHighscoreMenu/Name");
				element.Text = (name + "_");

				needsUpdate = false;
			}

			base.FrameStarted(dt);
		}
		protected override void OnSelected(int item)
		{

		}

		public override void HandleInput()
		{


			if(enterKey.KeyDownEvent()) 
			{
				name = Environment.UserName;

				// Add the highscore
				game.mHighscores.addHighscore(name, game.mPoints);

				// Display highscores
				ChangeState(HighscoresState.Instance);
			}

		}
		#endregion




		
	}
}