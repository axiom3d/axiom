using System;
using Chess.Main;
using Chess.AI;

namespace Chess.States
{
	/// <summary>
	/// Summary description for ChessState.
	/// </summary>
	public class ChessState: State
	{
		#region Fields
		protected GameOGRE game;
		protected GameAI gameAI;
		protected ChessApplication application;
		#endregion

		#region Constructors
		public ChessState()
		{

		}
		#endregion

		#region Methods
		public override void Delete()
		{
			base.Delete();
		}

	
		public override void Cleanup()
		{

		}
	
		public override void FrameEnded(float dt)
		{
			// Update debug overlay
			application.UpdateDebugOverlay();
		}
	
		public override void FrameStarted(float dt)
		{
			// Run an AI loop
			gameAI.Update();
			// Update game  
			game.Update(dt);
		}
	
		public override void Initialize()
		{
			// Store pointer to Game instances
			game = GameOGRE.Instance; 
			gameAI = GameAI.Instance;
			application = ChessApplication.Instance;
		}
	
		public override void KeyClicked(object sender, Axiom.Input.KeyEventArgs e)
		{
			base.KeyClicked (sender, e);
		}
	
		public override void KeyPressed(object sender, Axiom.Input.KeyEventArgs e)
		{
			// Handle keys common to all game states
			switch (e.Key)
			{
				case Axiom.Input.KeyCodes.F12:
					ChessApplication.Instance.ShowDebugOverlay = !ChessApplication.Instance.ShowDebugOverlay;
					break;
				case Axiom.Input.KeyCodes.Tilde:
					ChessApplication.Instance.TakeScreenshot("trial");
					break;
			}
		}
	
		public override void KeyReleased(object sender, Axiom.Input.KeyEventArgs e)
		{

		}
	
		public override void KeyRepeated(Axiom.Input.KeyCodes kc)
		{

		}
	
		public override void MouseDragged(object sender, Axiom.Input.MouseEventArgs e)
		{

		}
	
		public override void MouseMoved(object sender, Axiom.Input.MouseEventArgs e)
		{
			// Update CeGui with the mouse motion  
            int deltaX = (int)e.RelativeX; //(int)( e.RelativeX * application.GUIRenderer.Width );
            int deltaY = (int)e.RelativeY; //(int)( e.RelativeY * application.GUIRenderer.Height );
            //CeGui.GuiSystem.Instance.InjectMouseMove( deltaX, deltaY );

		}
	
		public override void MousePressed(object sender, Axiom.Input.MouseEventArgs e)
		{

		}
	
		public override void MouseReleased(object sender, Axiom.Input.MouseEventArgs e)
		{

		}
	
		public override void Pause()
		{

		}
	
		public override void Resume()
		{

		}
		#endregion
	}
}
