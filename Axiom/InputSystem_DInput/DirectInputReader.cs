#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Collections;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using DInput = Microsoft.DirectX.DirectInput;
using Axiom.Core;
using Axiom.Input;
using Axiom.SubSystems.Rendering;

namespace InputSystem_DInput
{
	/// <summary>
	/// Summary description for DirectInputReader.
	/// </summary>
	public class DirectInputReader : InputSystem, IPlugin
	{
		#region Member variables
		
		private KeyboardState keyboardState;
		private MouseState mouseState;
		private DInput.Device keyboardDevice;
		private DInput.Device mouseDevice;
		private int mouseRelX, mouseRelY, mouseRelZ;
		private int mouseAbsX, mouseAbsY, mouseAbsZ;
		private bool	 isInitialised;

		#endregion

		public DirectInputReader()
		{
		}

		#region Implementation of Axiom.Input.InputSystem

		/// <summary>
		///		
		/// </summary>
		public override void Capture()
		{
			if(useKeyboard)
			{
				// capture the current keyboard state
				try
				{
					keyboardState =  keyboardDevice.GetCurrentKeyboardState();		
				}
				catch (InputException)
				{
					// DirectInput may be telling us that the input stream has been
					// interrupted.  We aren't tracking any state between polls, so
					// we don't have any special reset that needs to be done.
					// We just re-acquire and try again.
	        
					// If input is lost then acquire and keep trying.
					InputException ie;

					bool loop = true;
					do
					{
						try
						{
							// attempt to re-acquire the device
							keyboardDevice.Acquire();

							// grab the fresh keyboard state
							// not doing so produces unpredictable results, mainly keys pressed that
							// really are not
							keyboardState =  keyboardDevice.GetCurrentKeyboardState();		

							loop = false;
						}
						catch(InputLostException)
						{
							loop = true;
						}
						catch(InputException inputException)
						{
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

			if(useMouse)
			{
				try
				{
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

					//Console.WriteLine("Rel X: {0} Rel Y: {1} Rel Z: {2}", mouseAbsX, mouseAbsY, mouseAbsZ);
				}
				catch (DirectXException)
				{
					// DirectInput may be telling us that the input stream has been
					// interrupted.  We aren't tracking any state between polls, so
					// we don't have any special reset that needs to be done.
					// We just re-acquire and try again.
        
					// If input is lost then acquire and keep trying.
					InputException ie;

					bool loop = true;
					do
					{
						try
						{
							// attempt to re-acquire the device
							mouseDevice.Acquire();

							// grab the fresh keyboard state
							// not doing so produces unpredictable results, mainly keys pressed that
							// really are not
							mouseState =  mouseDevice.CurrentMouseState;		

							loop = false;
						}
						catch(InputLostException)
						{
							loop = true;
						}
						catch(InputException inputException)
						{
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
		public override void Initialize(RenderWindow window, Queue eventQueue, bool useKeyboard, bool useMouse, bool useGamepad)
		{
			// call base class method first
			base.Initialize(window, eventQueue, useKeyboard, useMouse, useGamepad);

			// Set the cooperative level for the device. 
			// TODO: Find out best way to discover if a IntPtr is not initialized.
			Control handle;

			if(parent.Control is System.Windows.Forms.Form)
				handle = parent.Control;
			else if(parent.Control is System.Windows.Forms.PictureBox)
				handle = parent.Control.Parent;
			else
				throw new Axiom.Exceptions.AxiomException("Input subsystem must have either a Form or PictureBox to set coop level.");

			if(useKeyboard)
				InitializeImmediateKeyboard(handle, window.IsFullScreen);

			if(useMouse)
				InitializeImmediateMouse(handle, window.IsFullScreen);

			// we are initialized
			isInitialised = true;		
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public override bool IsKeyPressed(System.Windows.Forms.Keys key)
		{
			// get the DInput.Key enum from the System.Windows.Forms.Keys enum passed in
			DInput.Key daKey = ConvertKeyEnum(key);

			if(keyboardState[daKey])
			{
				return true;
			}
            
			return false;
		}

		#endregion

		/// <summary>
		///		Used to convert a System.Windows.Forms.Keys enum vals to a DirectInput.Key enum.
		/// </summary>
		/// <param name="winKey"></param>
		/// <returns></returns>
		private DInput.Key ConvertKeyEnum(System.Windows.Forms.Keys winKey)
		{
			DInput.Key dinputKey = 0;

			switch(winKey)
			{
				case Keys.A:
					dinputKey = DInput.Key.A;
					break;
				case Keys.B:
					dinputKey = DInput.Key.B;
					break;
				case Keys.C:
					dinputKey = DInput.Key.C;
					break;
				case Keys.D:
					dinputKey = DInput.Key.D;
					break;
				case Keys.E:
					dinputKey = DInput.Key.E;
					break;
				case Keys.F:
					dinputKey = DInput.Key.F;
					break;
				case Keys.G:
					dinputKey = DInput.Key.G;
					break;
				case Keys.H:
					dinputKey = DInput.Key.H;
					break;
				case Keys.I:
					dinputKey = DInput.Key.I;
					break;
				case Keys.J:
					dinputKey = DInput.Key.J;
					break;
				case Keys.K:
					dinputKey = DInput.Key.K;
					break;
				case Keys.L:
					dinputKey = DInput.Key.L;
					break;
				case Keys.M:
					dinputKey = DInput.Key.M;
					break;
				case Keys.N:
					dinputKey = DInput.Key.N;
					break;
				case Keys.O:
					dinputKey = DInput.Key.O;
					break;
				case Keys.P:
					dinputKey = DInput.Key.P;
					break;
				case Keys.Q:
					dinputKey = DInput.Key.Q;
					break;
				case Keys.R:
					dinputKey = DInput.Key.R;
					break;
				case Keys.S:
					dinputKey = DInput.Key.S;
					break;
				case Keys.T:
					dinputKey = DInput.Key.T;
					break;
				case Keys.U:
					dinputKey = DInput.Key.U;
					break;
				case Keys.V:
					dinputKey = DInput.Key.V;
					break;
				case Keys.W:
					dinputKey = DInput.Key.W;
					break;
				case Keys.X:
					dinputKey = DInput.Key.X;
					break;
				case Keys.Y:
					dinputKey = DInput.Key.Y;
					break;
				case Keys.Z:
					dinputKey = DInput.Key.Z;
					break;
				case Keys.Left :
					dinputKey = DInput.Key.LeftArrow;
					break;
				case Keys.Right:
					dinputKey = DInput.Key.RightArrow;
					break;
				case Keys.Up:
					dinputKey = DInput.Key.UpArrow;
					break;
				case Keys.Down:
					dinputKey = DInput.Key.DownArrow;
					break;
				case Keys.Escape:
					dinputKey = DInput.Key.Escape;
					break;
			}

			return dinputKey;
		}

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the X (horizontal) axis.
		/// </summary>
		public override int RelativeMouseX
		{
			get { return mouseRelX; }
		}

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Y (vertical) axis.
		/// </summary>
		public override int RelativeMouseY
		{
			get { return mouseRelY; }
		}

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Z (mouse wheel) axis.
		/// </summary>
		public override int RelativeMouseZ
		{
			get { return mouseRelZ; }
		}

		/// <summary>
		///		Retrieves the absolute mouse position on the X (horizontal) axis.
		/// </summary>
		public override int AbsoluteMouseX
		{
			get { return mouseAbsX; }
		}

		/// <summary>
		///		Retrieves the absolute mouse position on the Y (vertical) axis.
		/// </summary>
		public override int AbsoluteMouseY
		{
			get { return mouseAbsY; }
		}

		/// <summary>
		///		Retrieves the absolute mouse position on the Z (mouse wheel) axis.
		/// </summary>
		public override int AbsoluteMouseZ
		{
			get { return mouseAbsZ; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		private void InitializeImmediateKeyboard(Control handle, bool fullScreen)
		{
			// Create the device.
			keyboardDevice = new DInput.Device(SystemGuid.Keyboard);

			if(fullScreen)
				keyboardDevice.SetCooperativeLevel(handle, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);
			else
				keyboardDevice.SetCooperativeLevel(handle, CooperativeLevelFlags.NonExclusive | CooperativeLevelFlags.Background);

			// Set the data format to the keyboard pre-defined format.
			keyboardDevice.SetDataFormat(DeviceDataFormat.Keyboard);

			bool loop = true;

			do
			{
				// acquire the keyboard
				try
				{
					keyboardDevice.Acquire();

					loop = false;
				}
				catch
				{
					Application.DoEvents();
				}

			} while(loop);
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		private void InitializeImmediateMouse(Control handle, bool fullScreen)
		{
			// hide the cursor
			Cursor.Hide();

			// create the device
			mouseDevice = new DInput.Device(SystemGuid.Mouse);

			mouseDevice.Properties.AxisModeAbsolute = true;

			// set the device format so DInput knows this device is a mouse
			mouseDevice.SetDataFormat(DeviceDataFormat.Mouse);

			// set cooperation level
			mouseDevice.SetCooperativeLevel(handle, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);

			// note: dont acquire yet, wait till capture
		}

		#region Implementation of IPlugin

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public void Start()
		{
			// set ourself as the current input system
			Engine.Instance.InputSystem = this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public void Stop()
		{
			// Unacquire all Dinput objects.
			if(isInitialised)
			{
				if(useKeyboard)
				{
					keyboardDevice.Unacquire();		
					keyboardDevice.Dispose();
					keyboardDevice = null;
				}

				if(useMouse)
				{
					mouseDevice.Unacquire();		
					mouseDevice.Dispose();
					mouseDevice = null;
				}
			}

			// show the cursor again
			Cursor.Show();
		}
		#endregion
	}
}
