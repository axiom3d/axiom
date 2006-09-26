using System;

using Axiom.Input;


using Chess.Main;
namespace Chess.States
{
	public abstract class State
	{
		#region Fields

		#endregion

		#region Constructors
		public State()
		{

		}

		#endregion


		#region Virtual Methods
		public virtual void Delete()
		{

		}
		public virtual void Initialize()
		{

		}
		public virtual void Cleanup()
		{

		}

		public virtual void Pause()
		{

		}
		public virtual void Resume()
		{

		}

		public virtual void FrameStarted(float dt)
		{

		}
		public virtual void FrameEnded(float dt)
		{

		}
		#endregion


		#region Public Methods
		static public void ChangeState(State state)
		{
			StateManager.Instance.ChangeState(state);
		}
		#endregion

		#region Keyboard Input Methods
		public virtual void KeyClicked(object sender, KeyEventArgs e)
		{

		}
		public virtual void KeyPressed(object sender, KeyEventArgs e)
		{

		}
		public virtual void KeyReleased(object sender, KeyEventArgs e)
		{

		}
		public virtual void KeyRepeated(Axiom.Input.KeyCodes kc)
		{

		}
		#endregion

		
		protected bool IsKeyDown(Axiom.Input.KeyCodes kc)
		{
			return StateManager.Instance.Input.IsKeyPressed(kc);
		}

		
		#region Mouse Input Methods
		public virtual void MouseMoved(object sender, MouseEventArgs e)
		{

		}
		public virtual void MouseDragged(object sender, MouseEventArgs e)
		{

		}
		public virtual void MousePressed(object sender, MouseEventArgs e)
		{

		}
	
		public virtual void MouseReleased(object sender, MouseEventArgs e)
		{

		}


		#endregion


	}
}
