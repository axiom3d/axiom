#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

using SWF = System.Windows.Forms;
using DX = SlimDX;
using DI = SlimDX.DirectInput;

using Axiom.Core;
using Axiom.Input;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Platforms.Win32
{
	/// <summary>
	///		Win32 input implementation using Managed DirectInput (tm).
	/// </summary>
	public class Win32InputReader : InputReader
	{
		#region Fields

		/// <summary>
		///
		/// </summary>
		protected DI.DirectInput dinput;

		/// <summary>
		///		Holds a snapshot of DirectInput keyboard state.
		/// </summary>
		protected DI.KeyboardState keyboardState;

		/// <summary>
		///		Holds a snapshot of DirectInput mouse state.
		/// </summary>
		protected DI.MouseState mouseState;

		/// <summary>
		///		DirectInput keyboard device.
		/// </summary>
		protected DI.Keyboard keyboardDevice;

		/// <summary>
		///		DirectInput mouse device.
		/// </summary>
		protected DI.Mouse mouseDevice;

		protected int mouseRelX, mouseRelY, mouseRelZ;
		protected int mouseAbsX, mouseAbsY, mouseAbsZ;
		protected bool isInitialized;
		protected bool useMouse, useKeyboard, useGamepad;
		protected int mouseButtons;

		/// <summary>
		///		Active host control that reserves control over the input.
		/// </summary>
		protected IntPtr winHandle;

		/// <summary>
		/// System.Windows.Forms.Form control to retrieve input from
		/// </summary>
		protected SWF.Control control;

		/// <summary>
		///		Do we want exclusive use of the mouse?
		/// </summary>
		protected bool ownMouse;

		/// <summary>
		///		Reference to the render window that is the target of the input.
		/// </summary>
		protected RenderWindow window;

		/// <summary>
		///		Flag used to remember the state of the render window the last time input was captured.
		/// </summary>
		protected bool lastWindowActive;

		#endregion Fields

		#region Constants

		/// <summary>
		///		Size to use for DirectInput's input buffer.
		/// </summary>
		private const int BufferSize = 16;

		#endregion Constants

		#region InputReader Members

		#region Properties

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the X (horizontal) axis.
		/// </summary>
		public override int RelativeMouseX { get { return mouseRelX; } }

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Y (vertical) axis.
		/// </summary>
		public override int RelativeMouseY { get { return mouseRelY; } }

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Z (mouse wheel) axis.
		/// </summary>
		public override int RelativeMouseZ { get { return mouseRelZ; } }

		/// <summary>
		///		Retrieves the absolute mouse position on the X (horizontal) axis.
		/// </summary>
		public override int AbsoluteMouseX { get { return mouseAbsX; } }

		/// <summary>
		///		Retrieves the absolute mouse position on the Y (vertical) axis.
		/// </summary>
		public override int AbsoluteMouseY { get { return mouseAbsY; } }

		/// <summary>
		///		Retrieves the absolute mouse position on the Z (mouse wheel) axis.
		/// </summary>
		public override int AbsoluteMouseZ { get { return mouseAbsZ; } }

		/// <summary>
		///		Get/Set whether or not to use event based keyboard input notification.
		/// </summary>
		/// <value>
		///		When true, events will be fired when keyboard input occurs on a call to <see cref="Capture"/>.
		///		When false, the current keyboard state will be available via <see cref="IsKeyPressed"/> .
		/// </value>
		public override bool UseKeyboardEvents
		{
			get { return useKeyboardEvents; }
			set
			{
				if( useKeyboardEvents != value )
				{
					useKeyboardEvents = value;

					// dump the current keyboard device (if any)
					if( keyboardDevice != null )
					{
						keyboardDevice.Unacquire();
						keyboardDevice.Dispose();
					}

					// re-init the keyboard
					InitializeKeyboard();
				}
			}
		}

		/// <summary>
		///		Get/Set whether or not to use event based mouse input notification.
		/// </summary>
		/// <value>
		///		When true, events will be fired when mouse input occurs on a call to <see cref="Capture"/>.
		///		When false, the current mouse state will be available via <see cref="IsMousePressed"/> .
		/// </value>
		public override bool UseMouseEvents
		{
			get { return useMouseEvents; }
			set
			{
				if( useMouseEvents != value )
				{
					useMouseEvents = value;

					// dump the current keyboard device (if any)
					if( mouseDevice != null )
					{
						mouseDevice.Unacquire();
						mouseDevice.Dispose();
					}

					// re-init the keyboard
					InitializeMouse();
				}
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///		Captures the state of all active input controllers.
		/// </summary>
		public override void Capture()
		{
			if( window.IsActive )
			{
				try
				{
					CaptureInput();
				}
				catch( Exception )
				{
					try
					{
						// try to acquire device and try again
						if( useKeyboard )
						{
							keyboardDevice.Acquire();
						}
						if( useMouse )
						{
							mouseDevice.Acquire();
						}
						CaptureInput();
					}
					catch( Exception )
					{
						ClearInput(); //not to appear as something would be pressed or whatever.
					}
				}
			}
		}

		/// <summary>
		///		Intializes DirectInput for use on Win32 platforms.
		/// </summary>
		/// <param name="window"></param>
		/// <param name="useKeyboard"></param>
		/// <param name="useMouse"></param>
		/// <param name="useGamepad"></param>
		public override void Initialize( RenderWindow window, bool useKeyboard, bool useMouse, bool useGamepad, bool ownMouse )
		{
			this.useKeyboard = useKeyboard;
			this.useMouse = useMouse;
			this.useGamepad = useGamepad;
			this.ownMouse = ownMouse;
			this.window = window;

			// for Windows, this should be an IntPtr to a Window Handle
			winHandle = (IntPtr)window[ "WINDOW" ];

			// Keyboard and mouse capture must use Form's handle not child
			control = SWF.Control.FromHandle( winHandle );
			while( control != null && control.Parent != null )
			{
				control = control.Parent;
			}
			if( control != null )
			{
				winHandle = control.Handle;
			}

			if( dinput == null )
			{
				dinput = new DI.DirectInput();
			}

			// initialize keyboard if needed
			if( useKeyboard )
			{
				InitializeKeyboard();
			}

			// initialize the mouse if needed
			if( useMouse )
			{
				InitializeImmediateMouse();
			}

			// we are initialized
			isInitialized = true;

			// mouse starts off in the center
			mouseAbsX = (int)( window.Width * 0.5f );
			mouseAbsY = (int)( window.Height * 0.5f );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public override bool IsKeyPressed( KeyCodes key )
		{
			if( keyboardState != null )
			{
				// get the DI.Key enum from the System.Windows.Forms.Keys enum passed in
				DI.Key daKey = ConvertKeyEnum( key );

				if( keyboardState.IsPressed( daKey ) )
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		///    Returns true if the specified mouse button is currently down.
		/// </summary>
		/// <param name="button">Mouse button to query.</param>
		/// <returns>True if the mouse button is down, false otherwise.</returns>
		public override bool IsMousePressed( Axiom.Input.MouseButtons button )
		{
			return ( mouseButtons & (int)button ) != 0;
		}

		/// <summary>
		///     Called when the platform manager is shutting down.
		/// </summary>
		public override void Dispose()
		{
			if( keyboardDevice != null )
			{
				keyboardDevice.Unacquire();
				keyboardDevice.Dispose();
				keyboardDevice = null;
			}

			if( mouseDevice != null )
			{
				mouseDevice.Unacquire();
				mouseDevice.Dispose();
				mouseDevice = null;
			}

			if( dinput != null )
			{
				dinput.Dispose();
				dinput = null;
			}
		}

		#endregion Methods

		#endregion InputReader Members

		#region Helper Methods

		/// <summary>
		///     Clear this class input buffers (those accesible to client through one of the public methods)
		/// </summary>
		private void ClearInput()
		{
			keyboardState = null;
			mouseRelX = mouseRelY = mouseRelZ = 0;
			mouseButtons = 0;
		}

		/// <summary>
		///     Capture buffered or unbuffered mouse and/or keyboard input.
		/// </summary>
		private void CaptureInput()
		{
			if( useKeyboard )
			{
				if( useKeyboardEvents )
				{
					ReadBufferedKeyboardData();
				}
				else
				{
					// TODO Grab keyboard modifiers
					CaptureKeyboard();
				}
			}

			if( useMouse )
			{
				if( useMouseEvents )
				{
					//TODO: implement
				}
				else
				{
					CaptureMouse();
				}
			}
		}

		/// <summary>
		///		Initializes the keyboard using either immediate mode or event based input.
		/// </summary>
		private void InitializeKeyboard()
		{
			if( dinput == null )
			{
				dinput = new DI.DirectInput();
			}

			if( useKeyboardEvents )
			{
				InitializeBufferedKeyboard();
			}
			else
			{
				InitializeImmediateKeyboard();
			}
		}

		/// <summary>
		///		Initializes the mouse using either immediate mode or event based input.
		/// </summary>
		private void InitializeMouse()
		{
			if( dinput == null )
			{
				dinput = new DI.DirectInput();
			}

			if( useMouseEvents )
			{
				InitializeBufferedMouse();
			}
			else
			{
				InitializeImmediateMouse();
			}
		}

		/// <summary>
		///		Initializes DirectInput for immediate input.
		/// </summary>
		private void InitializeImmediateKeyboard()
		{
			// Create the device.
			keyboardDevice = new DI.Keyboard( dinput );

			// grab the keyboard non-exclusively
			keyboardDevice.SetCooperativeLevel( winHandle, DI.CooperativeLevel.Nonexclusive | DI.CooperativeLevel.Background );

			// Set the data format to the keyboard pre-defined format.
			//keyboardDevice.SetDataFormat( DI.DeviceDataFormat.Keyboard );

			try
			{
				keyboardDevice.Acquire();
			}
			catch
			{
				throw new Exception( "Unable to acquire a keyboard using DirectInput." );
			}
		}

		/// <summary>
		///		Prepares DirectInput for non-immediate input capturing.
		/// </summary>
		private void InitializeBufferedKeyboard()
		{
			// create the device
			keyboardDevice = new DI.Keyboard( dinput );

			// Set the data format to the keyboard pre-defined format.
			//keyboardDevice.SetDataFormat( DI.DeviceDataFormat.Keyboard );

			// grab the keyboard non-exclusively
			keyboardDevice.SetCooperativeLevel( winHandle, DI.CooperativeLevel.Nonexclusive | DI.CooperativeLevel.Background );

			// set the buffer size to use for input
			keyboardDevice.Properties.BufferSize = BufferSize;

			try
			{
				keyboardDevice.Acquire();
			}
			catch
			{
				throw new Exception( "Unable to acquire a keyboard using DirectInput." );
			}
		}

		/// <summary>
		///		Prepares DirectInput for immediate mouse input.
		/// </summary>
		private void InitializeImmediateMouse()
		{
			// create the device
			mouseDevice = new DI.Mouse( dinput );

			//mouseDevice.Properties.AxisModeAbsolute = true;
			mouseDevice.Properties.AxisMode = DI.DeviceAxisMode.Relative;

			// set the device format so DInput knows this device is a mouse
			//mouseDevice.SetDataFormat( DI.DeviceDataFormat.Mouse );

			// set cooperation level
			if( ownMouse )
			{
				mouseDevice.SetCooperativeLevel( winHandle, DI.CooperativeLevel.Exclusive | DI.CooperativeLevel.Foreground );
			}
			else
			{
				mouseDevice.SetCooperativeLevel( winHandle, DI.CooperativeLevel.Nonexclusive | DI.CooperativeLevel.Background );
			}

			// note: dont acquire yet, wait till capture
		}

		/// <summary>
		///
		/// </summary>
		private void InitializeBufferedMouse()
		{
			LogManager.Instance.Write( "Initializing buffered mouse" );
			// create the device

			mouseDevice = new DI.Mouse( dinput );

			mouseDevice.Properties.AxisMode = DI.DeviceAxisMode.Absolute;

			// set the device format so DInput knows this device is a mouse
			//mouseDevice.SetDataFormat(DeviceDataFormat.Mouse);

			// set the buffer size to use for input
			mouseDevice.Properties.BufferSize = BufferSize;
			if( ownMouse )
			{
				mouseDevice.SetCooperativeLevel( winHandle, DI.CooperativeLevel.Exclusive | DI.CooperativeLevel.Foreground );
			}
			else
			{
				mouseDevice.SetCooperativeLevel( winHandle, DI.CooperativeLevel.Nonexclusive | DI.CooperativeLevel.Background );
			}

			//CooperativeLevelFlags excl = ownMouse ? CooperativeLevelFlags.Exclusive : CooperativeLevelFlags.NonExclusive;
			//CooperativeLevelFlags background = (backgroundMouse && !ownMouse) ? CooperativeLevelFlags.Background : CooperativeLevelFlags.Foreground;

			// set cooperation level
			//mouseDevice.SetCooperativeLevel(control.FindForm(), excl | background);
		}

		/// <summary>
		///		Reads buffered input data when in buffered mode.
		/// </summary>
		private void ReadBufferedKeyboardData()
		{
			// grab the collection of buffered data
			IEnumerable<DI.KeyboardState> bufferedData = keyboardDevice.GetBufferedData();

			// please tell me why this would ever come back null, rather than an empty collection...
			if( bufferedData == null )
			{
				return;
			}

			foreach( DI.KeyboardState packet in bufferedData )
			{
				foreach( DI.Key key in packet.PressedKeys )
				{
					KeyChanged( ConvertKeyEnum( key ), true );
				}
				foreach( DI.Key key in packet.ReleasedKeys )
				{
					KeyChanged( ConvertKeyEnum( key ), false );
				}
			}
		}

		/// <summary>
		///		Captures an immediate keyboard state snapshot (for non-buffered data).
		/// </summary>
		private void CaptureKeyboard()
		{
			keyboardState = keyboardDevice.GetCurrentState();
		}

		/// <summary>
		///		Captures the mouse input based on the preffered input mode.
		/// </summary>
		private void CaptureMouse()
		{
			mouseDevice.Acquire();

			// determine whether to used immediate or buffered mouse input
			if( useMouseEvents )
			{
				CaptureBufferedMouse();
			}
			else
			{
				CaptureImmediateMouse();
			}
		}

		/// <summary>
		///		Checks the buffered mouse events.
		/// </summary>
		private void CaptureBufferedMouse() {}

		/// <summary>
		///		Takes a snapshot of the mouse state for immediate input checking.
		/// </summary>
		private void CaptureImmediateMouse()
		{
			// capture the current mouse state
			mouseState = mouseDevice.GetCurrentState();

			// store the updated absolute values
			mouseAbsX = control.PointToClient( SWF.Cursor.Position ).X;
			mouseAbsY = control.PointToClient( SWF.Cursor.Position ).Y;
			mouseAbsZ += mouseState.Z;

			// calc relative deviance from center
			mouseRelX = mouseState.X;
			mouseRelY = mouseState.Y;
			mouseRelZ = mouseState.Z;

			bool[] buttons = mouseState.GetButtons();

			// clear the flags
			mouseButtons = 0;

			for( int i = 0; i < buttons.Length; i++ )
			{
				if( buttons[ i ] == true )
				{
					mouseButtons |= ( 1 << i );
				}
			}
		}

		/// <summary>
		///		Verifies the state of the host window and reacquires input if the window was
		///		previously minimized and has been brought back into focus.
		/// </summary>
		/// <returns>True if the input devices are acquired and input capturing can proceed, false otherwise.</returns>
		protected bool VerifyInputAcquired()
		{
			// if the window is coming back from being deactivated, lets grab input again
			if( window.IsActive && !lastWindowActive )
			{
				// no exceptions right now, thanks anyway
				//DX.DirectXException.IgnoreExceptions();

				// acquire and capture keyboard input
				if( useKeyboard )
				{
					keyboardDevice.Acquire();
					CaptureKeyboard();
				}

				// acquire and capture mouse input
				if( useMouse )
				{
					mouseDevice.Acquire();
					CaptureMouse();
				}

				// wait...i like exceptions!
				//DX.DirectXException.EnableExceptions();
			}

			// store the current window state
			lastWindowActive = window.IsActive;

			return lastWindowActive;
		}

		#region Keycode Conversions

		/// <summary>
		///		Used to convert an Axiom.Input.KeyCodes enum val to a DirectInput.Key enum val.
		/// </summary>
		/// <param name="key">Axiom keyboard code to query.</param>
		/// <returns>The equivalent enum value in the DI.Key enum.</returns>
		private DI.Key ConvertKeyEnum( KeyCodes key )
		{
			// TODO: Quotes
			DI.Key dinputKey = 0;

			switch( key )
			{
				case KeyCodes.A:
					dinputKey = DI.Key.A;
					break;
				case KeyCodes.B:
					dinputKey = DI.Key.B;
					break;
				case KeyCodes.C:
					dinputKey = DI.Key.C;
					break;
				case KeyCodes.D:
					dinputKey = DI.Key.D;
					break;
				case KeyCodes.E:
					dinputKey = DI.Key.E;
					break;
				case KeyCodes.F:
					dinputKey = DI.Key.F;
					break;
				case KeyCodes.G:
					dinputKey = DI.Key.G;
					break;
				case KeyCodes.H:
					dinputKey = DI.Key.H;
					break;
				case KeyCodes.I:
					dinputKey = DI.Key.I;
					break;
				case KeyCodes.J:
					dinputKey = DI.Key.J;
					break;
				case KeyCodes.K:
					dinputKey = DI.Key.K;
					break;
				case KeyCodes.L:
					dinputKey = DI.Key.L;
					break;
				case KeyCodes.M:
					dinputKey = DI.Key.M;
					break;
				case KeyCodes.N:
					dinputKey = DI.Key.N;
					break;
				case KeyCodes.O:
					dinputKey = DI.Key.O;
					break;
				case KeyCodes.P:
					dinputKey = DI.Key.P;
					break;
				case KeyCodes.Q:
					dinputKey = DI.Key.Q;
					break;
				case KeyCodes.R:
					dinputKey = DI.Key.R;
					break;
				case KeyCodes.S:
					dinputKey = DI.Key.S;
					break;
				case KeyCodes.T:
					dinputKey = DI.Key.T;
					break;
				case KeyCodes.U:
					dinputKey = DI.Key.U;
					break;
				case KeyCodes.V:
					dinputKey = DI.Key.V;
					break;
				case KeyCodes.W:
					dinputKey = DI.Key.W;
					break;
				case KeyCodes.X:
					dinputKey = DI.Key.X;
					break;
				case KeyCodes.Y:
					dinputKey = DI.Key.Y;
					break;
				case KeyCodes.Z:
					dinputKey = DI.Key.Z;
					break;
				case KeyCodes.Left:
					dinputKey = DI.Key.LeftArrow;
					break;
				case KeyCodes.Right:
					dinputKey = DI.Key.RightArrow;
					break;
				case KeyCodes.Up:
					dinputKey = DI.Key.UpArrow;
					break;
				case KeyCodes.Down:
					dinputKey = DI.Key.DownArrow;
					break;
				case KeyCodes.Escape:
					dinputKey = DI.Key.Escape;
					break;
				case KeyCodes.F1:
					dinputKey = DI.Key.F1;
					break;
				case KeyCodes.F2:
					dinputKey = DI.Key.F2;
					break;
				case KeyCodes.F3:
					dinputKey = DI.Key.F3;
					break;
				case KeyCodes.F4:
					dinputKey = DI.Key.F4;
					break;
				case KeyCodes.F5:
					dinputKey = DI.Key.F5;
					break;
				case KeyCodes.F6:
					dinputKey = DI.Key.F6;
					break;
				case KeyCodes.F7:
					dinputKey = DI.Key.F7;
					break;
				case KeyCodes.F8:
					dinputKey = DI.Key.F8;
					break;
				case KeyCodes.F9:
					dinputKey = DI.Key.F9;
					break;
				case KeyCodes.F10:
					dinputKey = DI.Key.F10;
					break;
				case KeyCodes.D0:
					dinputKey = DI.Key.D0;
					break;
				case KeyCodes.D1:
					dinputKey = DI.Key.D1;
					break;
				case KeyCodes.D2:
					dinputKey = DI.Key.D2;
					break;
				case KeyCodes.D3:
					dinputKey = DI.Key.D3;
					break;
				case KeyCodes.D4:
					dinputKey = DI.Key.D4;
					break;
				case KeyCodes.D5:
					dinputKey = DI.Key.D5;
					break;
				case KeyCodes.D6:
					dinputKey = DI.Key.D6;
					break;
				case KeyCodes.D7:
					dinputKey = DI.Key.D7;
					break;
				case KeyCodes.D8:
					dinputKey = DI.Key.D8;
					break;
				case KeyCodes.D9:
					dinputKey = DI.Key.D9;
					break;
				case KeyCodes.F11:
					dinputKey = DI.Key.F11;
					break;
				case KeyCodes.F12:
					dinputKey = DI.Key.F12;
					break;
				case KeyCodes.Enter:
					dinputKey = DI.Key.Return;
					break;
				case KeyCodes.Tab:
					dinputKey = DI.Key.Tab;
					break;
				case KeyCodes.LeftShift:
					dinputKey = DI.Key.LeftShift;
					break;
				case KeyCodes.RightShift:
					dinputKey = DI.Key.RightShift;
					break;
				case KeyCodes.LeftControl:
					dinputKey = DI.Key.LeftControl;
					break;
				case KeyCodes.RightControl:
					dinputKey = DI.Key.RightControl;
					break;
				case KeyCodes.Period:
					dinputKey = DI.Key.Period;
					break;
				case KeyCodes.Comma:
					dinputKey = DI.Key.Comma;
					break;
				case KeyCodes.Home:
					dinputKey = DI.Key.Home;
					break;
				case KeyCodes.PageUp:
					dinputKey = DI.Key.PageUp;
					break;
				case KeyCodes.PageDown:
					dinputKey = DI.Key.PageDown;
					break;
				case KeyCodes.End:
					dinputKey = DI.Key.End;
					break;
				case KeyCodes.Semicolon:
					dinputKey = DI.Key.Semicolon;
					break;
				case KeyCodes.Subtract:
					dinputKey = DI.Key.Minus;
					break;
				case KeyCodes.Add:
					dinputKey = DI.Key.Equals;
					break;
				case KeyCodes.Backspace:
					dinputKey = DI.Key.Backspace;
					break;
				case KeyCodes.Delete:
					dinputKey = DI.Key.Delete;
					break;
				case KeyCodes.Insert:
					dinputKey = DI.Key.Insert;
					break;
				case KeyCodes.LeftAlt:
					dinputKey = DI.Key.LeftAlt;
					break;
				case KeyCodes.RightAlt:
					dinputKey = DI.Key.RightAlt;
					break;
				case KeyCodes.Space:
					dinputKey = DI.Key.Space;
					break;
				case KeyCodes.Tilde:
					dinputKey = DI.Key.Grave;
					break;
				case KeyCodes.OpenBracket:
					dinputKey = DI.Key.LeftBracket;
					break;
				case KeyCodes.CloseBracket:
					dinputKey = DI.Key.RightBracket;
					break;
				case KeyCodes.Plus:
					dinputKey = DI.Key.Equals;
					break;
				case KeyCodes.QuestionMark:
					dinputKey = DI.Key.Slash;
					break;
				case KeyCodes.Quotes:
					dinputKey = DI.Key.Apostrophe;
					break;
				case KeyCodes.Backslash:
					dinputKey = DI.Key.Backslash;
					break;
				case KeyCodes.NumPad0:
					dinputKey = DI.Key.NumberPad0;
					break;
				case KeyCodes.NumPad1:
					dinputKey = DI.Key.NumberPad1;
					break;
				case KeyCodes.NumPad2:
					dinputKey = DI.Key.NumberPad2;
					break;
				case KeyCodes.NumPad3:
					dinputKey = DI.Key.NumberPad3;
					break;
				case KeyCodes.NumPad4:
					dinputKey = DI.Key.NumberPad4;
					break;
				case KeyCodes.NumPad5:
					dinputKey = DI.Key.NumberPad5;
					break;
				case KeyCodes.NumPad6:
					dinputKey = DI.Key.NumberPad6;
					break;
				case KeyCodes.NumPad7:
					dinputKey = DI.Key.NumberPad7;
					break;
				case KeyCodes.NumPad8:
					dinputKey = DI.Key.NumberPad8;
					break;
				case KeyCodes.NumPad9:
					dinputKey = DI.Key.NumberPad9;
					break;
			}

			return dinputKey;
		}

		/// <summary>
		///		Used to convert a DirectInput.Key enum val to a Axiom.Input.KeyCodes enum val.
		/// </summary>
		/// <param name="key">DirectInput.Key code to query.</param>
		/// <returns>The equivalent enum value in the Axiom.KeyCodes enum.</returns>
		private Axiom.Input.KeyCodes ConvertKeyEnum( DI.Key key )
		{
			Axiom.Input.KeyCodes axiomKey = 0;

			switch( key )
			{
				case DI.Key.A:
					axiomKey = Axiom.Input.KeyCodes.A;
					break;
				case DI.Key.B:
					axiomKey = Axiom.Input.KeyCodes.B;
					break;
				case DI.Key.C:
					axiomKey = Axiom.Input.KeyCodes.C;
					break;
				case DI.Key.D:
					axiomKey = Axiom.Input.KeyCodes.D;
					break;
				case DI.Key.E:
					axiomKey = Axiom.Input.KeyCodes.E;
					break;
				case DI.Key.F:
					axiomKey = Axiom.Input.KeyCodes.F;
					break;
				case DI.Key.G:
					axiomKey = Axiom.Input.KeyCodes.G;
					break;
				case DI.Key.H:
					axiomKey = Axiom.Input.KeyCodes.H;
					break;
				case DI.Key.I:
					axiomKey = Axiom.Input.KeyCodes.I;
					break;
				case DI.Key.J:
					axiomKey = Axiom.Input.KeyCodes.J;
					break;
				case DI.Key.K:
					axiomKey = Axiom.Input.KeyCodes.K;
					break;
				case DI.Key.L:
					axiomKey = Axiom.Input.KeyCodes.L;
					break;
				case DI.Key.M:
					axiomKey = Axiom.Input.KeyCodes.M;
					break;
				case DI.Key.N:
					axiomKey = Axiom.Input.KeyCodes.N;
					break;
				case DI.Key.O:
					axiomKey = Axiom.Input.KeyCodes.O;
					break;
				case DI.Key.P:
					axiomKey = Axiom.Input.KeyCodes.P;
					break;
				case DI.Key.Q:
					axiomKey = Axiom.Input.KeyCodes.Q;
					break;
				case DI.Key.R:
					axiomKey = Axiom.Input.KeyCodes.R;
					break;
				case DI.Key.S:
					axiomKey = Axiom.Input.KeyCodes.S;
					break;
				case DI.Key.T:
					axiomKey = Axiom.Input.KeyCodes.T;
					break;
				case DI.Key.U:
					axiomKey = Axiom.Input.KeyCodes.U;
					break;
				case DI.Key.V:
					axiomKey = Axiom.Input.KeyCodes.V;
					break;
				case DI.Key.W:
					axiomKey = Axiom.Input.KeyCodes.W;
					break;
				case DI.Key.X:
					axiomKey = Axiom.Input.KeyCodes.X;
					break;
				case DI.Key.Y:
					axiomKey = Axiom.Input.KeyCodes.Y;
					break;
				case DI.Key.Z:
					axiomKey = Axiom.Input.KeyCodes.Z;
					break;
				case DI.Key.LeftArrow:
					axiomKey = Axiom.Input.KeyCodes.Left;
					break;
				case DI.Key.RightArrow:
					axiomKey = Axiom.Input.KeyCodes.Right;
					break;
				case DI.Key.UpArrow:
					axiomKey = Axiom.Input.KeyCodes.Up;
					break;
				case DI.Key.DownArrow:
					axiomKey = Axiom.Input.KeyCodes.Down;
					break;
				case DI.Key.Escape:
					axiomKey = Axiom.Input.KeyCodes.Escape;
					break;
				case DI.Key.F1:
					axiomKey = Axiom.Input.KeyCodes.F1;
					break;
				case DI.Key.F2:
					axiomKey = Axiom.Input.KeyCodes.F2;
					break;
				case DI.Key.F3:
					axiomKey = Axiom.Input.KeyCodes.F3;
					break;
				case DI.Key.F4:
					axiomKey = Axiom.Input.KeyCodes.F4;
					break;
				case DI.Key.F5:
					axiomKey = Axiom.Input.KeyCodes.F5;
					break;
				case DI.Key.F6:
					axiomKey = Axiom.Input.KeyCodes.F6;
					break;
				case DI.Key.F7:
					axiomKey = Axiom.Input.KeyCodes.F7;
					break;
				case DI.Key.F8:
					axiomKey = Axiom.Input.KeyCodes.F8;
					break;
				case DI.Key.F9:
					axiomKey = Axiom.Input.KeyCodes.F9;
					break;
				case DI.Key.F10:
					axiomKey = Axiom.Input.KeyCodes.F10;
					break;
				case DI.Key.D0:
					axiomKey = Axiom.Input.KeyCodes.D0;
					break;
				case DI.Key.D1:
					axiomKey = Axiom.Input.KeyCodes.D1;
					break;
				case DI.Key.D2:
					axiomKey = Axiom.Input.KeyCodes.D2;
					break;
				case DI.Key.D3:
					axiomKey = Axiom.Input.KeyCodes.D3;
					break;
				case DI.Key.D4:
					axiomKey = Axiom.Input.KeyCodes.D4;
					break;
				case DI.Key.D5:
					axiomKey = Axiom.Input.KeyCodes.D5;
					break;
				case DI.Key.D6:
					axiomKey = Axiom.Input.KeyCodes.D6;
					break;
				case DI.Key.D7:
					axiomKey = Axiom.Input.KeyCodes.D7;
					break;
				case DI.Key.D8:
					axiomKey = Axiom.Input.KeyCodes.D8;
					break;
				case DI.Key.D9:
					axiomKey = Axiom.Input.KeyCodes.D9;
					break;
				case DI.Key.F11:
					axiomKey = Axiom.Input.KeyCodes.F11;
					break;
				case DI.Key.F12:
					axiomKey = Axiom.Input.KeyCodes.F12;
					break;
				case DI.Key.Return:
					axiomKey = Axiom.Input.KeyCodes.Enter;
					break;
				case DI.Key.Tab:
					axiomKey = Axiom.Input.KeyCodes.Tab;
					break;
				case DI.Key.LeftShift:
					axiomKey = Axiom.Input.KeyCodes.LeftShift;
					break;
				case DI.Key.RightShift:
					axiomKey = Axiom.Input.KeyCodes.RightShift;
					break;
				case DI.Key.LeftControl:
					axiomKey = Axiom.Input.KeyCodes.LeftControl;
					break;
				case DI.Key.RightControl:
					axiomKey = Axiom.Input.KeyCodes.RightControl;
					break;
				case DI.Key.Period:
					axiomKey = Axiom.Input.KeyCodes.Period;
					break;
				case DI.Key.Comma:
					axiomKey = Axiom.Input.KeyCodes.Comma;
					break;
				case DI.Key.Home:
					axiomKey = Axiom.Input.KeyCodes.Home;
					break;
				case DI.Key.PageUp:
					axiomKey = Axiom.Input.KeyCodes.PageUp;
					break;
				case DI.Key.PageDown:
					axiomKey = Axiom.Input.KeyCodes.PageDown;
					break;
				case DI.Key.End:
					axiomKey = Axiom.Input.KeyCodes.End;
					break;
				case DI.Key.Semicolon:
					axiomKey = Axiom.Input.KeyCodes.Semicolon;
					break;
				case DI.Key.Minus:
					axiomKey = Axiom.Input.KeyCodes.Subtract;
					break;
				case DI.Key.Equals:
					axiomKey = Axiom.Input.KeyCodes.Add;
					break;
				case DI.Key.Backspace:
					axiomKey = Axiom.Input.KeyCodes.Backspace;
					break;
				case DI.Key.Delete:
					axiomKey = Axiom.Input.KeyCodes.Delete;
					break;
				case DI.Key.Insert:
					axiomKey = Axiom.Input.KeyCodes.Insert;
					break;
				case DI.Key.LeftAlt:
					axiomKey = Axiom.Input.KeyCodes.LeftAlt;
					break;
				case DI.Key.RightAlt:
					axiomKey = Axiom.Input.KeyCodes.RightAlt;
					break;
				case DI.Key.Space:
					axiomKey = Axiom.Input.KeyCodes.Space;
					break;
				case DI.Key.Grave:
					axiomKey = Axiom.Input.KeyCodes.Tilde;
					break;
				case DI.Key.LeftBracket:
					axiomKey = Axiom.Input.KeyCodes.OpenBracket;
					break;
				case DI.Key.RightBracket:
					axiomKey = Axiom.Input.KeyCodes.CloseBracket;
					break;
					//case DI.Key.Equals:
					//    axiomKey = KeyCodes.Plus;
					//    break;
					//case DI.Key.Minus:
					//    axiomKey = KeyCodes.Subtract;
					//    break;
				case DI.Key.Slash:
					axiomKey = KeyCodes.QuestionMark;
					break;
				case DI.Key.Apostrophe:
					axiomKey = KeyCodes.Quotes;
					break;
				case DI.Key.Backslash:
					axiomKey = KeyCodes.Backslash;
					break;
				case DI.Key.NumberPad0:
					axiomKey = Axiom.Input.KeyCodes.NumPad0;
					break;
				case DI.Key.NumberPad1:
					axiomKey = Axiom.Input.KeyCodes.NumPad1;
					break;
				case DI.Key.NumberPad2:
					axiomKey = Axiom.Input.KeyCodes.NumPad2;
					break;
				case DI.Key.NumberPad3:
					axiomKey = Axiom.Input.KeyCodes.NumPad3;
					break;
				case DI.Key.NumberPad4:
					axiomKey = Axiom.Input.KeyCodes.NumPad4;
					break;
				case DI.Key.NumberPad5:
					axiomKey = Axiom.Input.KeyCodes.NumPad5;
					break;
				case DI.Key.NumberPad6:
					axiomKey = Axiom.Input.KeyCodes.NumPad6;
					break;
				case DI.Key.NumberPad7:
					axiomKey = Axiom.Input.KeyCodes.NumPad7;
					break;
				case DI.Key.NumberPad8:
					axiomKey = Axiom.Input.KeyCodes.NumPad8;
					break;
				case DI.Key.NumberPad9:
					axiomKey = Axiom.Input.KeyCodes.NumPad9;
					break;
			}

			return axiomKey;
		}

		#endregion Keycode Conversions

		#endregion Helper Methods
	}
}
