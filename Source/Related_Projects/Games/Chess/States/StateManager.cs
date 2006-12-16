using System;
using System.Collections;

using Axiom.Core;
using Axiom.Input;
using Axiom.Graphics;

using Chess.Main;

namespace Chess.States
{
	public class StateManager
	{

		#region Fields
		protected RenderWindow window;
		protected ArrayList stateStack = new ArrayList();
		protected bool running;
		protected float initialRepeatKeyDelay;
		protected float continousKeyRepeatDelay;
		protected Axiom.Input.KeyCodes repeatKey;
		protected float repeatKeyDelay;

		protected InputReader input;
		protected float keyDelay = 0.0f;

		protected bool lastLeftMouseDown = false;
		protected bool lastRightMouseDown = false;
		#endregion

		#region Singleton implementation

		/// <summary>
		///     Singleton instance of StateManager.
		/// </summary>
		private static StateManager instance;
		public StateManager(RenderWindow window)
		{
			if (instance == null) 
			{
				instance = this;
				input = ChessApplication.Instance.Input;
				this.window = window;
				running = true;
				repeatKey = Axiom.Input.KeyCodes.NoName;
				initialRepeatKeyDelay = 0.15f;
				continousKeyRepeatDelay = 0.05f;

				Root.Instance.FrameEnded+=new FrameEvent(FrameEnded);
				Root.Instance.FrameStarted+=new FrameEvent(FrameStarted);

				input.KeyDown+=new KeyboardEventHandler(KeyPressed);
				input.KeyUp+=new KeyboardEventHandler(KeyReleased);
				
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
		public void Delete()
		{
//			// Cleanup all states
//			while (!mStateStack.empty())
//			{
//				mStateStack.back()->cleanup();
//				mStateStack.pop_back();
//			}
//
//			// Unregister event listeners
//			mEventProcessor->removeKeyListener(this);
//			mEventProcessor->removeMouseMotionListener(this);
//			mEventProcessor->removeMouseListener(this);  
//
//			// Destroy event processor
//			delete mEventProcessor;
//
//			// Unregister frame listener
//			Root::getSingleton().removeFrameListener(this);
		}
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
		public virtual void RemoveCurrentState()//popState
		{
			// Cleanup current state
			if (stateStack.Count>0)
			{
				State state = (State)stateStack[stateStack.Count-1];
				state.Cleanup();
				stateStack.Remove(state);
			}

			// Resume previous state
			if (stateStack.Count>0)
			{
				State state = (State)stateStack[stateStack.Count-1];
				state.Resume();
			}

		}
		public virtual void Quit()
		{
			running = false;
			Root.Instance.QueueEndRendering();
		}
		#endregion

		#region FrameListener overrides

		public virtual void FrameStarted(object source, FrameEventArgs e)
		{
			input.Capture();
			UpdateCeGui();

			// Quit if window is closed
//			if (!window.IsActive)
//				return false;

			// Send key repeated events
			if (repeatKey != Axiom.Input.KeyCodes.NoName)
			{
				repeatKeyDelay -= e.TimeSinceLastFrame;
				while (repeatKeyDelay <= 0.0)
				{
					State state = (State)stateStack[stateStack.Count-1];
					state.KeyRepeated(repeatKey);
					repeatKeyDelay += continousKeyRepeatDelay;
				}
			}

			// Call current state
			((State)stateStack[stateStack.Count-1]).FrameStarted(e.TimeSinceLastFrame);
//			return running;
		}

		private void UpdateCeGui()
		{
			bool mouseMoved = false;
			// grab the relative mouse movement
			int rotX = (int)input.RelativeMouseX;
			int rotY = (int)input.RelativeMouseY;

			// inject mouse movement into the GuiSystem
			if(rotX != 0 || rotY != 0) 
			{
				mouseMoved = true;
				CeGui.GuiSystem.Instance.InjectMouseMove(rotX, rotY);
			}

			// Mouse wheel test
			int wheelDelta = input.RelativeMouseZ;

			if(wheelDelta > 0) 
			{
				CeGui.GuiSystem.Instance.InjectMouseWheel(1);
			}
			else if(wheelDelta < 0) 
			{
				CeGui.GuiSystem.Instance.InjectMouseWheel(-1);
			}

			// End mouse wheel test

			#region LeftMouse button
			bool currentLeftMouseDown = input.IsMousePressed(Axiom.Input.MouseButtons.Left);
			// when the mouse state changes, fire the appropriate event
			if(currentLeftMouseDown != lastLeftMouseDown) 
			{
				if(currentLeftMouseDown) 
				{
					CeGui.GuiSystem.Instance.InjectMouseDown(System.Windows.Forms.MouseButtons.Left);
				}
				else 
				{
                    CeGui.GuiSystem.Instance.InjectMouseUp( System.Windows.Forms.MouseButtons.Left );
				}
			}
			if(currentLeftMouseDown == true && currentLeftMouseDown != lastLeftMouseDown) 
			{
				Axiom.Input.MouseEventArgs e = new Axiom.Input.MouseEventArgs(MouseButtons.Left,Axiom.Input.ModifierKeys.MouseButton0,0,0,0,0,0,0);
				this.MousePressed(this,e);
			}

			if(currentLeftMouseDown == false && currentLeftMouseDown != lastLeftMouseDown) 
			{
				Axiom.Input.MouseEventArgs e = new Axiom.Input.MouseEventArgs(MouseButtons.Left,Axiom.Input.ModifierKeys.MouseButton0,0,0,0,0,0,0);
				this.MouseReleased(this,e);
			}
			lastLeftMouseDown = currentLeftMouseDown;
			#endregion

			#region Right Mouse
			bool currentRightMouseDown = input.IsMousePressed(Axiom.Input.MouseButtons.Right);
			// when the mouse state changes, fire the appropriate event
			if(currentRightMouseDown == true && currentRightMouseDown != lastRightMouseDown) 
			{
				Axiom.Input.MouseEventArgs e = new Axiom.Input.MouseEventArgs(MouseButtons.Right,Axiom.Input.ModifierKeys.MouseButton0,0,0,0,0,0,0);
				this.MousePressed(this,e);
			}
			if(currentRightMouseDown == false && currentRightMouseDown != lastRightMouseDown) 
			{
				Axiom.Input.MouseEventArgs e = new Axiom.Input.MouseEventArgs(MouseButtons.Right,Axiom.Input.ModifierKeys.MouseButton0,0,0,0,0,0,0);
				this.MouseReleased(this,e);
			}
			lastRightMouseDown = currentRightMouseDown;
			#endregion

			#region MouseMoved event
			if (mouseMoved)
			{
                Axiom.Input.MouseEventArgs e1 = new Axiom.Input.MouseEventArgs(MouseButtons.Right,Axiom.Input.ModifierKeys.None,input.AbsoluteMouseX,input.AbsoluteMouseY,input.AbsoluteMouseZ,input.RelativeMouseX,input.RelativeMouseY,input.RelativeMouseZ);
                this.MouseMoved(this,e1);
			}
			#endregion

		}

		public virtual void FrameEnded(object source, FrameEventArgs e)
		{
			State state = (State)stateStack[stateStack.Count-1];
			state.FrameEnded(e.TimeSinceLastFrame);
//			return running;
		}

		#endregion

		
		#region KeyListener overrides
		public virtual void KeyClicked(object sender, Axiom.Input.KeyEventArgs e)
		{
			State state = (State)stateStack[stateStack.Count-1];
			state.KeyClicked(sender, e);
		}
		public virtual void KeyPressed(object sender, Axiom.Input.KeyEventArgs e)
		{

			State state = (State)stateStack[stateStack.Count-1];
			state.KeyPressed(sender, e);

			// Start repeating key
			repeatKey = e.Key;
			repeatKeyDelay = initialRepeatKeyDelay;
			state.KeyRepeated(repeatKey);
		}
		public virtual void KeyReleased(object sender, Axiom.Input.KeyEventArgs e)
		{

			State state = (State)stateStack[stateStack.Count-1];
			state.KeyReleased(sender, e);

			// Stop repeating key
			if (e.Key == repeatKey)
				repeatKey = Axiom.Input.KeyCodes.NoName;
		}

		public void setKeyRepeatDelay(float initial, float continous)
		{
			initialRepeatKeyDelay = initial;
			continousKeyRepeatDelay = continous;
		}
		public void setKeyRepeatDelay()
		{
			initialRepeatKeyDelay = 0.1f;
			continousKeyRepeatDelay = 0.05f;
		}
		#endregion

		#region MouseMotionListener overrides
		public virtual void MouseMoved(object sender, Axiom.Input.MouseEventArgs e)
		{
			State state = (State)stateStack[stateStack.Count-1];
			state.MouseMoved(sender, e);
		}
		public virtual void MouseDragged(object sender, Axiom.Input.MouseEventArgs e)
		{
			State state = (State)stateStack[stateStack.Count-1];
			state.MouseDragged(sender, e);

		}
		public virtual void MousePressed(object sender, Axiom.Input.MouseEventArgs e)
		{
			State state = (State)stateStack[stateStack.Count-1];
			state.MousePressed(sender, e);

		}
		public virtual void  MouseReleased(object sender, Axiom.Input.MouseEventArgs e)
		{
			State state = (State)stateStack[stateStack.Count-1];
			state.MouseReleased(sender, e);
		}
		#endregion




	}
}