using System;
using System.Collections;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Input;
using Axiom.Graphics;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using DInput = Microsoft.DirectX.DirectInput;

namespace Axiom.Platforms.Win32
{
	/// <summary>
	/// Summary description for Win32InputReader.
	/// </summary>
	public class Win32InputReader : IInputReader {
		private KeyboardState keyboardState;
		private MouseState mouseState;
		private DInput.Device keyboardDevice;
		private DInput.Device mouseDevice;
		private int mouseRelX, mouseRelY, mouseRelZ;
		private int mouseAbsX, mouseAbsY, mouseAbsZ;
		private bool	 isInitialised;
		private bool useMouse, useKeyboard, useGamepad;
		private int mouseButtons;
		private System.Windows.Forms.Control control;

		public Win32InputReader() {
		}

		#region Implementation of Axiom.Input.InputSystem

		/// <summary>
		///		
		/// </summary>
		public void Capture() {
			if(useKeyboard) {
				// capture the current keyboard state
				try {
					keyboardState =  keyboardDevice.GetCurrentKeyboardState();		
				}
				catch (InputException) {
					// DirectInput may be telling us that the input stream has been
					// interrupted.  We aren't tracking any state between polls, so
					// we don't have any special reset that needs to be done.
					// We just re-acquire and try again.
	        
					// If input is lost then acquire and keep trying.
					InputException ie;

					bool loop = true;
					do {
						try {
							// attempt to re-acquire the device
							keyboardDevice.Acquire();

							// grab the fresh keyboard state
							// not doing so produces unpredictable results, mainly keys pressed that
							// really are not
							keyboardState =  keyboardDevice.GetCurrentKeyboardState();		

							loop = false;
						}
						catch(InputLostException) {
							loop = true;
						}
						catch(InputException inputException) {
							ie = inputException;
							loop = false;
						}
						catch(Exception) {}
					} while (loop);

					// Exception may be OtherApplicationHasPriorityException or other exceptions.
					// This may occur when the app is minimized or in the process of 
					// switching, so just try again later.
					return; 

				}
			}

			if(useMouse) {
				try {
					// try to capture the current mouse state
					mouseState = mouseDevice.CurrentMouseState;

					// store the updated absolute values
					mouseAbsX += mouseState.X;
					mouseAbsY += mouseState.Y;
					mouseAbsZ += mouseState.Z;

					// calc relative deviance from center
					mouseRelX = mouseState.X;
					mouseRelY = mouseState.Y; 
					mouseRelZ = mouseState.Z; 

					byte[] buttons = mouseState.GetMouseButtons();

					// clear the flags
					mouseButtons = 0;

					for(int i = 0; i < buttons.Length; i++) {
						if((buttons[i] & 0x80) != 0) {
							mouseButtons |= (1 << i);
						}
					}
				}
				catch (DirectXException) {
					// DirectInput may be telling us that the input stream has been
					// interrupted.  We aren't tracking any state between polls, so
					// we don't have any special reset that needs to be done.
					// We just re-acquire and try again.
        
					// If input is lost then acquire and keep trying.
					InputException ie;

					bool loop = true;
					do {
						try {
							// attempt to re-acquire the device
							mouseDevice.Acquire();

							// grab the fresh keyboard state
							// not doing so produces unpredictable results, mainly keys pressed that
							// really are not
							mouseState = mouseDevice.CurrentMouseState;		

							loop = false;
						}
						catch(InputLostException) {
							loop = true;
						}
						catch(InputException inputException) {
							ie = inputException;
							loop = false;
						}
					} while (loop);

					// Exception may be OtherApplicationHasPriorityException or other exceptions.
					// This may occur when the app is minimized or in the process of 
					// switching, so just try again later.
					return; 
				}
			}
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="window"></param>
		/// <param name="eventQueue"></param>
		/// <param name="useKeyboard"></param>
		/// <param name="useMouse"></param>
		/// <param name="useGamepad"></param>
		public void Initialize(RenderWindow window, Queue eventQueue, bool useKeyboard, bool useMouse, bool useGamepad) {
			this.useKeyboard = useKeyboard;
			this.useMouse = useMouse;
			this.useGamepad = useGamepad;

			control = window.Handle as System.Windows.Forms.Control;

            if(control is System.Windows.Forms.Form)
                control = control;
            else if(control is System.Windows.Forms.PictureBox)
                control = control.Parent;
            else
                throw new Axiom.Exceptions.AxiomException("Input subsystem must have either a Form or PictureBox to set coop level.");


			if(useKeyboard)
				InitializeImmediateKeyboard();

			if(useMouse)
				InitializeImmediateMouse();

			// we are initialized
			isInitialised = true;		
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsKeyPressed(KeyCodes key) {
			// get the DInput.Key enum from the System.Windows.Forms.Keys enum passed in
			DInput.Key daKey = ConvertKeyEnum(key);

			if(keyboardState[daKey]) {
				return true;
			}
            
			return false;
		}

		/// <summary>
		///    Returns true if the specified mouse button is currently down.
		/// </summary>
		/// <param name="button">Mouse button to query.</param>
		/// <returns>True if the mouse button is down, false otherwise.</returns>
		public bool IsMousePressed(Axiom.Input.MouseButtons button) {
			return (mouseButtons & (int)button) != 0;
		}

		#endregion

		/// <summary>
		///		Used to convert an Axiom.Input.KeyCodes enum val to a DirectInput.Key enum val.
		/// </summary>
		/// <param name="key">Axiom keyboard code to query.</param>
		/// <returns>The equivalent enum value in the DInput.Key enum.</returns>
		private DInput.Key ConvertKeyEnum(KeyCodes key) {
			// TODO: Tilde, Quotes
			DInput.Key dinputKey = 0;

			switch(key) {
				case KeyCodes.A:
					dinputKey = DInput.Key.A;
					break;
				case KeyCodes.B:
					dinputKey = DInput.Key.B;
					break;
				case KeyCodes.C:
					dinputKey = DInput.Key.C;
					break;
				case KeyCodes.D:
					dinputKey = DInput.Key.D;
					break;
				case KeyCodes.E:
					dinputKey = DInput.Key.E;
					break;
				case KeyCodes.F:
					dinputKey = DInput.Key.F;
					break;
				case KeyCodes.G:
					dinputKey = DInput.Key.G;
					break;
				case KeyCodes.H:
					dinputKey = DInput.Key.H;
					break;
				case KeyCodes.I:
					dinputKey = DInput.Key.I;
					break;
				case KeyCodes.J:
					dinputKey = DInput.Key.J;
					break;
				case KeyCodes.K:
					dinputKey = DInput.Key.K;
					break;
				case KeyCodes.L:
					dinputKey = DInput.Key.L;
					break;
				case KeyCodes.M:
					dinputKey = DInput.Key.M;
					break;
				case KeyCodes.N:
					dinputKey = DInput.Key.N;
					break;
				case KeyCodes.O:
					dinputKey = DInput.Key.O;
					break;
				case KeyCodes.P:
					dinputKey = DInput.Key.P;
					break;
				case KeyCodes.Q:
					dinputKey = DInput.Key.Q;
					break;
				case KeyCodes.R:
					dinputKey = DInput.Key.R;
					break;
				case KeyCodes.S:
					dinputKey = DInput.Key.S;
					break;
				case KeyCodes.T:
					dinputKey = DInput.Key.T;
					break;
				case KeyCodes.U:
					dinputKey = DInput.Key.U;
					break;
				case KeyCodes.V:
					dinputKey = DInput.Key.V;
					break;
				case KeyCodes.W:
					dinputKey = DInput.Key.W;
					break;
				case KeyCodes.X:
					dinputKey = DInput.Key.X;
					break;
				case KeyCodes.Y:
					dinputKey = DInput.Key.Y;
					break;
				case KeyCodes.Z:
					dinputKey = DInput.Key.Z;
					break;
				case KeyCodes.Left :
					dinputKey = DInput.Key.LeftArrow;
					break;
				case KeyCodes.Right:
					dinputKey = DInput.Key.RightArrow;
					break;
				case KeyCodes.Up:
					dinputKey = DInput.Key.UpArrow;
					break;
				case KeyCodes.Down:
					dinputKey = DInput.Key.DownArrow;
					break;
				case KeyCodes.Escape:
					dinputKey = DInput.Key.Escape;
					break;
				case KeyCodes.F1:
					dinputKey = DInput.Key.F1;
					break;
				case KeyCodes.F2:
					dinputKey = DInput.Key.F2;
					break;
				case KeyCodes.F3:
					dinputKey = DInput.Key.F3;
					break;
				case KeyCodes.F4:
					dinputKey = DInput.Key.F4;
					break;
				case KeyCodes.F5:
					dinputKey = DInput.Key.F5;
					break;
				case KeyCodes.F6:
					dinputKey = DInput.Key.F6;
					break;
				case KeyCodes.F7:
					dinputKey = DInput.Key.F7;
					break;
				case KeyCodes.F8:
					dinputKey = DInput.Key.F8;
					break;
				case KeyCodes.F9:
					dinputKey = DInput.Key.F9;
					break;
				case KeyCodes.F10:
					dinputKey = DInput.Key.F10;
					break;
                case KeyCodes.D0:
                    dinputKey = DInput.Key.D0;
                    break;
                case KeyCodes.D1:
                    dinputKey = DInput.Key.D1;
                    break;
                case KeyCodes.D2:
                    dinputKey = DInput.Key.D2;
                    break;
                case KeyCodes.D3:
                    dinputKey = DInput.Key.D3;
                    break;
                case KeyCodes.D4:
                    dinputKey = DInput.Key.D4;
                    break;
                case KeyCodes.D5:
                    dinputKey = DInput.Key.D5;
                    break;
                case KeyCodes.D6:
                    dinputKey = DInput.Key.D6;
                    break;
                case KeyCodes.D7:
                    dinputKey = DInput.Key.D7;
                    break;
                case KeyCodes.D8:
                    dinputKey = DInput.Key.D8;
                    break;
                case KeyCodes.D9:
                    dinputKey = DInput.Key.D9;
                    break;
				case KeyCodes.F11:
					dinputKey = DInput.Key.F11;
					break;
				case KeyCodes.F12:
					dinputKey = DInput.Key.F12;
					break;
				case KeyCodes.Enter:
					dinputKey = DInput.Key.Return;
					break;
				case KeyCodes.Tab:
					dinputKey = DInput.Key.Tab;
					break;
				case KeyCodes.LeftShift:
					dinputKey = DInput.Key.LeftShift;
					break;
				case KeyCodes.RightShift:
					dinputKey = DInput.Key.RightShift;
					break;
				case KeyCodes.LeftControl:
					dinputKey = DInput.Key.LeftControl;
					break;
				case KeyCodes.RightControl:
					dinputKey = DInput.Key.RightControl;
					break;
				case KeyCodes.Period:
					dinputKey = DInput.Key.Period;
					break;
				case KeyCodes.Comma:
					dinputKey = DInput.Key.Comma;
					break;
				case KeyCodes.Home:
					dinputKey = DInput.Key.Home;
					break;
				case KeyCodes.PageUp:
					dinputKey = DInput.Key.PageUp;
					break;
				case KeyCodes.PageDown:
					dinputKey = DInput.Key.PageDown;
					break;
				case KeyCodes.End:
					dinputKey = DInput.Key.End;
					break;
				case KeyCodes.Semicolon:
					dinputKey = DInput.Key.SemiColon;
					break;
				case KeyCodes.Subtract:
					dinputKey = DInput.Key.Subtract;
					break;
				case KeyCodes.Add:
					dinputKey = DInput.Key.Add;
					break;
				case KeyCodes.Backspace:
					dinputKey = DInput.Key.BackSpace;
					break;
				case KeyCodes.Delete:
					dinputKey = DInput.Key.Delete;
					break;
				case KeyCodes.Insert:
					dinputKey = DInput.Key.Insert;
					break;
				case KeyCodes.LeftAlt:
					dinputKey = DInput.Key.LeftAlt;
					break;
				case KeyCodes.RightAlt:
					dinputKey = DInput.Key.RightAlt;
					break;
				case KeyCodes.Space:
					dinputKey = DInput.Key.Space;
					break;
			}

			return dinputKey;
		}

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the X (horizontal) axis.
		/// </summary>
		public int RelativeMouseX {
			get { 
				return mouseRelX; 
			}
		}

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Y (vertical) axis.
		/// </summary>
		public int RelativeMouseY {
			get { 
				return mouseRelY; 
			}
		}

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Z (mouse wheel) axis.
		/// </summary>
		public int RelativeMouseZ {
			get { 
				return mouseRelZ; 
			}
		}

		/// <summary>
		///		Retrieves the absolute mouse position on the X (horizontal) axis.
		/// </summary>
		public int AbsoluteMouseX {
			get { 
				return mouseAbsX; 
			}
		}

		/// <summary>
		///		Retrieves the absolute mouse position on the Y (vertical) axis.
		/// </summary>
		public int AbsoluteMouseY {
			get { 
				return mouseAbsY; 
			}
		}

		/// <summary>
		///		Retrieves the absolute mouse position on the Z (mouse wheel) axis.
		/// </summary>
		public int AbsoluteMouseZ {
			get { 
				return mouseAbsZ; 
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void InitializeImmediateKeyboard() {
			// Create the device.
			keyboardDevice = new DInput.Device(SystemGuid.Keyboard);

			// grab the keyboard non-exclusively
			keyboardDevice.SetCooperativeLevel(control, CooperativeLevelFlags.NonExclusive | CooperativeLevelFlags.Background);

			// Set the data format to the keyboard pre-defined format.
			keyboardDevice.SetDataFormat(DeviceDataFormat.Keyboard);

			try {
				keyboardDevice.Acquire();
			}
			catch {
				throw new Exception("Unable to acquire a keyboard using DirectInput.");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void InitializeImmediateMouse() {
			// create the device
			mouseDevice = new DInput.Device(SystemGuid.Mouse);

			mouseDevice.Properties.AxisModeAbsolute = true;

			// set the device format so DInput knows this device is a mouse
			mouseDevice.SetDataFormat(DeviceDataFormat.Mouse);

			// set cooperation level
			mouseDevice.SetCooperativeLevel(control, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);

			// note: dont acquire yet, wait till capture
		}
	}
}