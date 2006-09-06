using System;

using Axiom;


namespace YAT 
{

	public abstract class TetrisState : State
	{
		#region Fields
		protected Game game;
		protected KeyState escapeKey;
		protected KeyState enterKey;
		protected KeyState downKey;
		protected KeyState upKey;
		protected KeyState leftKey;
		protected KeyState rightKey;
		protected KeyState spaceKey;
		#endregion

		#region Constructors
		public TetrisState()
		{
			escapeKey = new KeyState(Axiom.Input.KeyCodes.Escape);
			enterKey = new KeyState(Axiom.Input.KeyCodes.Enter);
			downKey = new KeyState(Axiom.Input.KeyCodes.Down);
			upKey = new KeyState(Axiom.Input.KeyCodes.Up);
			leftKey = new KeyState(Axiom.Input.KeyCodes.Left);
			rightKey = new KeyState(Axiom.Input.KeyCodes.Right);
			spaceKey = new KeyState(Axiom.Input.KeyCodes.Space);
		}
		#endregion

		#region Public Methods
		// State overrides
		public override void Initialize()
		{
				game = Game.Instance;
		}
		public override void KeyPressed(Axiom.KeyEventArgs e)
		{
			// Handle keys common to all game states
			switch (e.Key)
			{
			case Axiom.Input.KeyCodes.F12:
				{
					TetrisApplication.Instance.ShowDebugOverlay = !TetrisApplication.Instance.ShowDebugOverlay;
				}
				break;
			}
		}
		public override void FrameStarted(float dt)
		{
			// Update game
			game.Update(dt);
		}
		public override void FrameEnded(float dt)
		{
			TetrisApplication.Instance.UpdateStats();
		}
		#endregion

	}
}