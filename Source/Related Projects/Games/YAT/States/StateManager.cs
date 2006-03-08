using System;
using System.Collections;

using Axiom;
using Axiom.Input;

namespace YAT 
{
	public class StateManager
	{

		#region Fields
		protected RenderWindow window;
		protected ArrayList stateStack = new ArrayList();
		protected bool mRunning;
		protected float mInitialRepeatKeyDelay;
		protected float mContinousKeyRepeatDelay;
		protected Axiom.Input.KeyCodes mRepeatKey;
		protected float mRepeatKeyDelay;

		protected InputReader input;
		protected float keyDelay = 0.0f;
		#endregion

		#region Singleton implementation

		/// <summary>
		///     Singleton instance of Root.
		/// </summary>
		private static StateManager instance;
		public StateManager(RenderWindow window)
		{
			if (instance == null) 
			{
				instance = this;
				input = TetrisApplication.Instance.Input;
				this.window = window;
				mRunning = true;
				mRepeatKey = Axiom.Input.KeyCodes.Escape;
				mInitialRepeatKeyDelay = 0.15f;
				mContinousKeyRepeatDelay = 0.05f;
				Root.Instance.FrameEnded+=new FrameEvent(FrameEnded);
				Root.Instance.FrameStarted+=new FrameEvent(FrameStarted);

				input.KeyUp+=new KeyboardEventHandler(input_KeyUp);
				input.KeyDown+=new KeyboardEventHandler(input_KeyDown);



				
			}
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		/// <value></value>
		public static StateManager Instance 
		{
			get 
			{
				return instance;
			}
		}

		#endregion

		public InputReader Input 
		{
			get 
			{
				return input;
			}
		}


		
		#region State management
		public virtual void ChangeState(State state)
		{
			// Cleanup current state
			if (stateStack.Count>0)
			{
				State lastState = (State)stateStack[stateStack.Count-1];
				lastState.Cleanup();
				stateStack.Remove(lastState);
			}

			
			stateStack.Add(state);
			state.Initialize();
		}
		public virtual void AddState(State state)
		{
			// Pause current state
			if (stateStack.Count>0)
			{
				((State)stateStack[stateStack.Count-1]).Pause();
			}

			// Push and Initialize new state
			stateStack.Add(state);
			state.Initialize();
		}
		public virtual void RemoveCurrentState()
		{
			// Cleanup current state
			if (stateStack.Count>0)
			{

				int back = stateStack.Count-1;
				State state = (State)stateStack[back];
				state.Cleanup();
				stateStack.Remove(state);
			}

			// Resume previous state
			if (stateStack.Count>0)
			{
				int back = stateStack.Count-1;
				State state = (State)stateStack[back];
				state.Resume();
			}

		}
		public virtual void Quit()
		{
			mRunning = false;
			Root.Instance.QueueEndRendering();
		}
		#endregion

		#region FrameListener overrides


		public virtual void FrameStarted(object source, FrameEventArgs e)
		{
			// Quit if window is closed
//			if (!window.IsActive)
//				return false;

			// Send key repeated events
			if (mRepeatKey != Axiom.Input.KeyCodes.Escape)
			{
				mRepeatKeyDelay -= e.TimeSinceLastFrame;
				while (mRepeatKeyDelay <= 0.0)
				{
					int back = stateStack.Count-1;
					State state = (State)stateStack[back];
					state.KeyRepeated(mRepeatKey);
					mRepeatKeyDelay += mContinousKeyRepeatDelay;
				}
			}

			// Call current state

			((State)stateStack[stateStack.Count-1]).FrameStarted(e.TimeSinceLastFrame);
//			return mRunning;

			// subtract the time since last frame to delay specific key presses
			keyDelay -= e.TimeSinceLastFrame;
			if (keyDelay < 0)
			{
				InputHandler();
				keyDelay = .05f;

			}
		}

		public virtual void FrameEnded(object source, FrameEventArgs e)
		{

			int back = stateStack.Count-1;
			State state = (State)stateStack[back];
			state.FrameEnded(e.TimeSinceLastFrame);
//			return mRunning;
		}

		#endregion

		
		#region KeyListener overrides
		public virtual void KeyClicked(Axiom.KeyEventArgs e)
		{
			State state = (State)stateStack[stateStack.Count-1];
			state.KeyClicked(e);
		}
		public virtual void KeyPressed(Axiom.KeyEventArgs e)
		{

			State state = (State)stateStack[stateStack.Count-1];
			state.KeyPressed(e);

			// Start repeating key
			mRepeatKey = e.Key;
			mRepeatKeyDelay = mInitialRepeatKeyDelay;
			state.KeyRepeated(mRepeatKey);
		}
		public virtual void KeyReleased(Axiom.KeyEventArgs e)
		{

			State state = (State)stateStack[stateStack.Count-1];
			state.KeyReleased(e);

			// Stop repeating key
			if (e.Key == mRepeatKey)
				mRepeatKey = Axiom.Input.KeyCodes.Escape;
		}

		public void setKeyRepeatDelay(float initial, float continous)
		{
			mInitialRepeatKeyDelay = initial;
			mContinousKeyRepeatDelay = continous;
		}
		public void setKeyRepeatDelay()
		{
			mInitialRepeatKeyDelay = 0.1f;
			mContinousKeyRepeatDelay = 0.05f;
		}
		#endregion

		#region MouseMotionListener overrides
		public virtual void MouseMoved(Axiom.MouseEventArgs e)
		{
			State state = (State)stateStack[stateStack.Count-1];
			state.MouseMoved(e);
		}
		public virtual void MouseDragged(Axiom.MouseEventArgs e)
		{
			State state = (State)stateStack[stateStack.Count-1];
			state.MouseDragged(e);

		}
		#endregion

		public void InputHandler()
		{
			State state = (State)stateStack[stateStack.Count-1];
			state.HandleInput();
		}

		private void input_KeyUp(object sender, KeyEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine(e.KeyChar +  "up");
		}

		private void input_KeyDown(object sender, KeyEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine(e.KeyChar +  "up");
		}
	}
}