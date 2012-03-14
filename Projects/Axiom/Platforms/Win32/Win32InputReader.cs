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
using System.Windows.Forms;

using Axiom.Graphics;
using Axiom.Input;

using SharpDX.DirectInput;

using SWF = System.Windows.Forms;
using DX = SharpDX;
using DI = SharpDX.DirectInput;
using MouseButtons = Axiom.Input.MouseButtons;

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
		/// System.Windows.Forms.Form control to retrieve input from
		/// </summary>
		protected Control control;

		/// <summary>
		///
		/// </summary>
		protected DirectInput dinput;

		protected bool isInitialized;

		/// <summary>
		///		DirectInput keyboard device.
		/// </summary>
		protected Keyboard keyboardDevice;

		/// <summary>
		///		Holds a snapshot of DirectInput keyboard state.
		/// </summary>
		protected KeyboardState keyboardState;

		/// <summary>
		///		Flag used to remember the state of the render window the last time input was captured.
		/// </summary>
		protected bool lastWindowActive;

		protected int mouseAbsX, mouseAbsY, mouseAbsZ;
		protected int mouseButtons;

		/// <summary>
		///		DirectInput mouse device.
		/// </summary>
		protected Mouse mouseDevice;

		protected int mouseRelX, mouseRelY, mouseRelZ;

		/// <summary>
		///		Holds a snapshot of DirectInput mouse state.
		/// </summary>
		protected MouseState mouseState;

		/// <summary>
		///		Do we want exclusive use of the mouse?
		/// </summary>
		protected bool ownMouse;

		protected bool useGamepad;
		protected bool useKeyboard;
		protected bool useMouse;

		/// <summary>
		///		Active host control that reserves control over the input.
		/// </summary>
		protected IntPtr winHandle;

		/// <summary>
		///		Reference to the render window that is the target of the input.
		/// </summary>
		protected RenderWindow window;

		#endregion Fields

		#region Constants

		/// <summary>
		///		Size to use for DirectInput's input buffer.
		/// </summary>
		private const int BufferSize = 16;

		#endregion Constants

		#region Properties

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the X (horizontal) axis.
		/// </summary>
		public override int RelativeMouseX
		{
			get
			{
				return this.mouseRelX;
			}
		}

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Y (vertical) axis.
		/// </summary>
		public override int RelativeMouseY
		{
			get
			{
				return this.mouseRelY;
			}
		}

		/// <summary>
		///		Retrieves the relative (compared to the last input poll) mouse movement
		///		on the Z (mouse wheel) axis.
		/// </summary>
		public override int RelativeMouseZ
		{
			get
			{
				return this.mouseRelZ;
			}
		}

		/// <summary>
		///		Retrieves the absolute mouse position on the X (horizontal) axis.
		/// </summary>
		public override int AbsoluteMouseX
		{
			get
			{
				return this.mouseAbsX;
			}
		}

		/// <summary>
		///		Retrieves the absolute mouse position on the Y (vertical) axis.
		/// </summary>
		public override int AbsoluteMouseY
		{
			get
			{
				return this.mouseAbsY;
			}
		}

		/// <summary>
		///		Retrieves the absolute mouse position on the Z (mouse wheel) axis.
		/// </summary>
		public override int AbsoluteMouseZ
		{
			get
			{
				return this.mouseAbsZ;
			}
		}

		/// <summary>
		///		Get/Set whether or not to use event based keyboard input notification.
		/// </summary>
		/// <value>
		///		When true, events will be fired when keyboard input occurs on a call to <see cref="Capture"/>.
		///		When false, the current keyboard state will be available via <see cref="IsKeyPressed"/> .
		/// </value>
		public override bool UseKeyboardEvents
		{
			get
			{
				return useKeyboardEvents;
			}
			set
			{
				if ( useKeyboardEvents != value )
				{
					useKeyboardEvents = value;

					// dump the current keyboard device (if any)
					if ( this.keyboardDevice != null )
					{
						this.keyboardDevice.Unacquire();
						this.keyboardDevice.Dispose();
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
			get
			{
				return useMouseEvents;
			}
			set
			{
				if ( useMouseEvents != value )
				{
					useMouseEvents = value;

					// dump the current keyboard device (if any)
					if ( this.mouseDevice != null )
					{
						this.mouseDevice.Unacquire();
						this.mouseDevice.Dispose();
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
			if ( this.window.IsActive )
			{
				try
				{
					CaptureInput();
				}
				catch ( Exception )
				{
					try
					{
						// try to acquire device and try again
						if ( this.useKeyboard )
						{
							this.keyboardDevice.Acquire();
						}
						if ( this.useMouse )
						{
							this.mouseDevice.Acquire();
						}
						CaptureInput();
					}
					catch ( Exception )
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
			this.winHandle = (IntPtr)window[ "WINDOW" ];

			// Keyboard and mouse capture must use Form's handle not child
			this.control = Control.FromHandle( this.winHandle );
			while ( this.control != null && this.control.Parent != null )
			{
				this.control = this.control.Parent;
			}
			if ( this.control != null )
			{
				this.winHandle = this.control.Handle;
			}

			if ( this.dinput == null )
			{
				this.dinput = new DirectInput();
			}

			// initialize keyboard if needed
			if ( useKeyboard )
			{
				InitializeKeyboard();
			}

			// initialize the mouse if needed
			if ( useMouse )
			{
				InitializeImmediateMouse();
			}

			// we are initialized
			this.isInitialized = true;

			// mouse starts off in the center
			this.mouseAbsX = (int)( window.Width * 0.5f );
			this.mouseAbsY = (int)( window.Height * 0.5f );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public override bool IsKeyPressed( KeyCodes key )
		{
			if ( this.keyboardState != null )
			{
				// get the DI.Key enum from the System.Windows.Forms.Keys enum passed in
				Key daKey = ConvertKeyEnum( key );

				if ( this.keyboardState.IsPressed( daKey ) )
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
		public override bool IsMousePressed( MouseButtons button )
		{
			return ( this.mouseButtons & (int)button ) != 0;
		}

		/// <summary>
		///     Called when the platform manager is shutting down.
		/// </summary>
		public override void Dispose()
		{
			if ( this.keyboardDevice != null )
			{
				this.keyboardDevice.Unacquire();
				this.keyboardDevice.Dispose();
				this.keyboardDevice = null;
			}

			if ( this.mouseDevice != null )
			{
				this.mouseDevice.Unacquire();
				this.mouseDevice.Dispose();
				this.mouseDevice = null;
			}

			if ( this.dinput != null )
			{
				this.dinput.Dispose();
				this.dinput = null;
			}
		}

		#endregion Methods

		#region Helper Methods

		/// <summary>
		///     Clear this class input buffers (those accesible to client through one of the public methods)
		/// </summary>
		private void ClearInput()
		{
			this.keyboardState = null;
			this.mouseRelX = this.mouseRelY = this.mouseRelZ = 0;
			this.mouseButtons = 0;
		}

		/// <summary>
		///     Capture buffered or unbuffered mouse and/or keyboard input.
		/// </summary>
		private void CaptureInput()
		{
			if ( this.useKeyboard )
			{
				if ( useKeyboardEvents )
				{
					ReadBufferedKeyboardData();
				}
				else
				{
					// TODO Grab keyboard modifiers
					CaptureKeyboard();
				}
			}

			if ( this.useMouse )
			{
				if ( useMouseEvents )
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
			if ( this.dinput == null )
			{
				this.dinput = new DirectInput();
			}

			if ( useKeyboardEvents )
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
			if ( this.dinput == null )
			{
				this.dinput = new DirectInput();
			}

			if ( useMouseEvents )
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
			this.keyboardDevice = new Keyboard( this.dinput );

			// grab the keyboard non-exclusively
			this.keyboardDevice.SetCooperativeLevel( this.winHandle, CooperativeLevel.NonExclusive | CooperativeLevel.Background );

			// Set the data format to the keyboard pre-defined format.
			//keyboardDevice.SetDataFormat( DI.DeviceDataFormat.Keyboard );

			try
			{
				this.keyboardDevice.Acquire();
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
			this.keyboardDevice = new Keyboard( this.dinput );

			// Set the data format to the keyboard pre-defined format.
			//keyboardDevice.SetDataFormat( DI.DeviceDataFormat.Keyboard );

			// grab the keyboard non-exclusively
			this.keyboardDevice.SetCooperativeLevel( this.winHandle, CooperativeLevel.NonExclusive | CooperativeLevel.Background );

			// set the buffer size to use for input
			this.keyboardDevice.Properties.BufferSize = BufferSize;

			try
			{
				this.keyboardDevice.Acquire();
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
			this.mouseDevice = new Mouse( this.dinput );

			//mouseDevice.Properties.AxisModeAbsolute = true;
			this.mouseDevice.Properties.AxisMode = DeviceAxisMode.Relative;

			// set the device format so DInput knows this device is a mouse
			//mouseDevice.SetDataFormat( DI.DeviceDataFormat.Mouse );

			// set cooperation level
			if ( this.ownMouse )
			{
				this.mouseDevice.SetCooperativeLevel( this.winHandle, CooperativeLevel.Exclusive | CooperativeLevel.Foreground );
			}
			else
			{
				this.mouseDevice.SetCooperativeLevel( this.winHandle, CooperativeLevel.NonExclusive | CooperativeLevel.Background );
			}

			// note: dont acquire yet, wait till capture
		}

		/// <summary>
		///
		/// </summary>
		private void InitializeBufferedMouse()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///		Reads buffered input data when in buffered mode.
		/// </summary>
		private void ReadBufferedKeyboardData()
		{
			// grab the collection of buffered data
			KeyboardUpdate[] bufferedData = this.keyboardDevice.GetBufferedData();

			// please tell me why this would ever come back null, rather than an empty collection...
			if ( bufferedData == null )
			{
				return;
			}

			foreach ( KeyboardUpdate packet in bufferedData )
			{
				if ( packet.IsPressed )
				{
					KeyChanged( ConvertKeyEnum( packet.Key ), true );
				}

				if ( packet.IsReleased )
				{
					KeyChanged( ConvertKeyEnum( packet.Key ), false );
				}
			}
		}

		/// <summary>
		///		Captures an immediate keyboard state snapshot (for non-buffered data).
		/// </summary>
		private void CaptureKeyboard()
		{
			this.keyboardState = this.keyboardDevice.GetCurrentState();
		}

		/// <summary>
		///		Captures the mouse input based on the preffered input mode.
		/// </summary>
		private void CaptureMouse()
		{
			this.mouseDevice.Acquire();

			// determine whether to used immediate or buffered mouse input
			if ( useMouseEvents )
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
		private void CaptureBufferedMouse()
		{
			// TODO: Implement
		}

		/// <summary>
		///		Takes a snapshot of the mouse state for immediate input checking.
		/// </summary>
		private void CaptureImmediateMouse()
		{
			// capture the current mouse state
			this.mouseState = this.mouseDevice.GetCurrentState();


			// store the updated absolute values
			this.mouseAbsX = this.control.PointToClient( Cursor.Position ).X;
			this.mouseAbsY = this.control.PointToClient( Cursor.Position ).Y;
			this.mouseAbsZ += this.mouseState.Z;

			// calc relative deviance from center
			this.mouseRelX = this.mouseState.X;
			this.mouseRelY = this.mouseState.Y;
			this.mouseRelZ = this.mouseState.Z;

			bool[] buttons = this.mouseState.Buttons;

			// clear the flags
			this.mouseButtons = 0;

			for ( int i = 0; i < buttons.Length; i++ )
			{
				if ( buttons[ i ] )
				{
					this.mouseButtons |= ( 1 << i );
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
			if ( this.window.IsActive && !this.lastWindowActive )
			{
				// no exceptions right now, thanks anyway
				//DX.DirectXException.IgnoreExceptions();

				// acquire and capture keyboard input
				if ( this.useKeyboard )
				{
					this.keyboardDevice.Acquire();
					CaptureKeyboard();
				}

				// acquire and capture mouse input
				if ( this.useMouse )
				{
					this.mouseDevice.Acquire();
					CaptureMouse();
				}

				// wait...i like exceptions!
				//DX.DirectXException.EnableExceptions();
			}

			// store the current window state
			this.lastWindowActive = this.window.IsActive;

			return this.lastWindowActive;
		}

		#region Keycode Conversions

		/// <summary>
		///		Used to convert an Axiom.Input.KeyCodes enum val to a DirectInput.Key enum val.
		/// </summary>
		/// <param name="key">Axiom keyboard code to query.</param>
		/// <returns>The equivalent enum value in the DI.Key enum.</returns>
		private Key ConvertKeyEnum( KeyCodes key )
		{
			// TODO: Quotes
			Key dinputKey = 0;

			switch ( key )
			{
				case KeyCodes.A:
					dinputKey = Key.A;
					break;
				case KeyCodes.B:
					dinputKey = Key.B;
					break;
				case KeyCodes.C:
					dinputKey = Key.C;
					break;
				case KeyCodes.D:
					dinputKey = Key.D;
					break;
				case KeyCodes.E:
					dinputKey = Key.E;
					break;
				case KeyCodes.F:
					dinputKey = Key.F;
					break;
				case KeyCodes.G:
					dinputKey = Key.G;
					break;
				case KeyCodes.H:
					dinputKey = Key.H;
					break;
				case KeyCodes.I:
					dinputKey = Key.I;
					break;
				case KeyCodes.J:
					dinputKey = Key.J;
					break;
				case KeyCodes.K:
					dinputKey = Key.K;
					break;
				case KeyCodes.L:
					dinputKey = Key.L;
					break;
				case KeyCodes.M:
					dinputKey = Key.M;
					break;
				case KeyCodes.N:
					dinputKey = Key.N;
					break;
				case KeyCodes.O:
					dinputKey = Key.O;
					break;
				case KeyCodes.P:
					dinputKey = Key.P;
					break;
				case KeyCodes.Q:
					dinputKey = Key.Q;
					break;
				case KeyCodes.R:
					dinputKey = Key.R;
					break;
				case KeyCodes.S:
					dinputKey = Key.S;
					break;
				case KeyCodes.T:
					dinputKey = Key.T;
					break;
				case KeyCodes.U:
					dinputKey = Key.U;
					break;
				case KeyCodes.V:
					dinputKey = Key.V;
					break;
				case KeyCodes.W:
					dinputKey = Key.W;
					break;
				case KeyCodes.X:
					dinputKey = Key.X;
					break;
				case KeyCodes.Y:
					dinputKey = Key.Y;
					break;
				case KeyCodes.Z:
					dinputKey = Key.Z;
					break;
				case KeyCodes.Left:
					dinputKey = Key.Left;
					break;
				case KeyCodes.Right:
					dinputKey = Key.Right;
					break;
				case KeyCodes.Up:
					dinputKey = Key.UpArrow;
					break;
				case KeyCodes.Down:
					dinputKey = Key.Down;
					break;
				case KeyCodes.Escape:
					dinputKey = Key.Escape;
					break;
				case KeyCodes.F1:
					dinputKey = Key.F1;
					break;
				case KeyCodes.F2:
					dinputKey = Key.F2;
					break;
				case KeyCodes.F3:
					dinputKey = Key.F3;
					break;
				case KeyCodes.F4:
					dinputKey = Key.F4;
					break;
				case KeyCodes.F5:
					dinputKey = Key.F5;
					break;
				case KeyCodes.F6:
					dinputKey = Key.F6;
					break;
				case KeyCodes.F7:
					dinputKey = Key.F7;
					break;
				case KeyCodes.F8:
					dinputKey = Key.F8;
					break;
				case KeyCodes.F9:
					dinputKey = Key.F9;
					break;
				case KeyCodes.F10:
					dinputKey = Key.F10;
					break;
				case KeyCodes.D0:
					dinputKey = Key.D0;
					break;
				case KeyCodes.D1:
					dinputKey = Key.D1;
					break;
				case KeyCodes.D2:
					dinputKey = Key.D2;
					break;
				case KeyCodes.D3:
					dinputKey = Key.D3;
					break;
				case KeyCodes.D4:
					dinputKey = Key.D4;
					break;
				case KeyCodes.D5:
					dinputKey = Key.D5;
					break;
				case KeyCodes.D6:
					dinputKey = Key.D6;
					break;
				case KeyCodes.D7:
					dinputKey = Key.D7;
					break;
				case KeyCodes.D8:
					dinputKey = Key.D8;
					break;
				case KeyCodes.D9:
					dinputKey = Key.D9;
					break;
				case KeyCodes.F11:
					dinputKey = Key.F11;
					break;
				case KeyCodes.F12:
					dinputKey = Key.F12;
					break;
				case KeyCodes.Enter:
					dinputKey = Key.Return;
					break;
				case KeyCodes.Tab:
					dinputKey = Key.Tab;
					break;
				case KeyCodes.LeftShift:
					dinputKey = Key.LeftShift;
					break;
				case KeyCodes.RightShift:
					dinputKey = Key.RightShift;
					break;
				case KeyCodes.LeftControl:
					dinputKey = Key.LeftControl;
					break;
				case KeyCodes.RightControl:
					dinputKey = Key.RightControl;
					break;
				case KeyCodes.Period:
					dinputKey = Key.Period;
					break;
				case KeyCodes.Comma:
					dinputKey = Key.Comma;
					break;
				case KeyCodes.Home:
					dinputKey = Key.Home;
					break;
				case KeyCodes.PageUp:
					dinputKey = Key.PageUp;
					break;
				case KeyCodes.PageDown:
					dinputKey = Key.PageDown;
					break;
				case KeyCodes.End:
					dinputKey = Key.End;
					break;
				case KeyCodes.Semicolon:
					dinputKey = Key.Semicolon;
					break;
				case KeyCodes.Subtract:
					dinputKey = Key.Minus;
					break;
				case KeyCodes.Add:
					dinputKey = Key.Equals;
					break;
				case KeyCodes.Backspace:
					dinputKey = Key.Back;
					break;
				case KeyCodes.Delete:
					dinputKey = Key.Delete;
					break;
				case KeyCodes.Insert:
					dinputKey = Key.Insert;
					break;
				case KeyCodes.LeftAlt:
					dinputKey = Key.LeftAlt;
					break;
				case KeyCodes.RightAlt:
					dinputKey = Key.RightAlt;
					break;
				case KeyCodes.Space:
					dinputKey = Key.Space;
					break;
				case KeyCodes.Tilde:
					dinputKey = Key.Grave;
					break;
				case KeyCodes.OpenBracket:
					dinputKey = Key.LeftBracket;
					break;
				case KeyCodes.CloseBracket:
					dinputKey = Key.RightBracket;
					break;
				case KeyCodes.Plus:
					dinputKey = Key.Equals;
					break;
				case KeyCodes.QuestionMark:
					dinputKey = Key.Slash;
					break;
				case KeyCodes.Quotes:
					dinputKey = Key.Apostrophe;
					break;
				case KeyCodes.Backslash:
					dinputKey = Key.Backslash;
					break;
				case KeyCodes.NumPad0:
					dinputKey = Key.NumberPad0;
					break;
				case KeyCodes.NumPad1:
					dinputKey = Key.NumberPad1;
					break;
				case KeyCodes.NumPad2:
					dinputKey = Key.NumberPad2;
					break;
				case KeyCodes.NumPad3:
					dinputKey = Key.NumberPad3;
					break;
				case KeyCodes.NumPad4:
					dinputKey = Key.NumberPad4;
					break;
				case KeyCodes.NumPad5:
					dinputKey = Key.NumberPad5;
					break;
				case KeyCodes.NumPad6:
					dinputKey = Key.NumberPad6;
					break;
				case KeyCodes.NumPad7:
					dinputKey = Key.NumberPad7;
					break;
				case KeyCodes.NumPad8:
					dinputKey = Key.NumberPad8;
					break;
				case KeyCodes.NumPad9:
					dinputKey = Key.NumberPad9;
					break;
			}

			return dinputKey;
		}

		/// <summary>
		///		Used to convert a DirectInput.Key enum val to a Axiom.Input.KeyCodes enum val.
		/// </summary>
		/// <param name="key">DirectInput.Key code to query.</param>
		/// <returns>The equivalent enum value in the Axiom.KeyCodes enum.</returns>
		private KeyCodes ConvertKeyEnum( Key key )
		{
			KeyCodes axiomKey = 0;

			switch ( key )
			{
				case Key.A:
					axiomKey = KeyCodes.A;
					break;
				case Key.B:
					axiomKey = KeyCodes.B;
					break;
				case Key.C:
					axiomKey = KeyCodes.C;
					break;
				case Key.D:
					axiomKey = KeyCodes.D;
					break;
				case Key.E:
					axiomKey = KeyCodes.E;
					break;
				case Key.F:
					axiomKey = KeyCodes.F;
					break;
				case Key.G:
					axiomKey = KeyCodes.G;
					break;
				case Key.H:
					axiomKey = KeyCodes.H;
					break;
				case Key.I:
					axiomKey = KeyCodes.I;
					break;
				case Key.J:
					axiomKey = KeyCodes.J;
					break;
				case Key.K:
					axiomKey = KeyCodes.K;
					break;
				case Key.L:
					axiomKey = KeyCodes.L;
					break;
				case Key.M:
					axiomKey = KeyCodes.M;
					break;
				case Key.N:
					axiomKey = KeyCodes.N;
					break;
				case Key.O:
					axiomKey = KeyCodes.O;
					break;
				case Key.P:
					axiomKey = KeyCodes.P;
					break;
				case Key.Q:
					axiomKey = KeyCodes.Q;
					break;
				case Key.R:
					axiomKey = KeyCodes.R;
					break;
				case Key.S:
					axiomKey = KeyCodes.S;
					break;
				case Key.T:
					axiomKey = KeyCodes.T;
					break;
				case Key.U:
					axiomKey = KeyCodes.U;
					break;
				case Key.V:
					axiomKey = KeyCodes.V;
					break;
				case Key.W:
					axiomKey = KeyCodes.W;
					break;
				case Key.X:
					axiomKey = KeyCodes.X;
					break;
				case Key.Y:
					axiomKey = KeyCodes.Y;
					break;
				case Key.Z:
					axiomKey = KeyCodes.Z;
					break;
				case Key.Left:
					axiomKey = KeyCodes.Left;
					break;
				case Key.Right:
					axiomKey = KeyCodes.Right;
					break;
				case Key.UpArrow:
					axiomKey = KeyCodes.Up;
					break;
				case Key.Down:
					axiomKey = KeyCodes.Down;
					break;
				case Key.Escape:
					axiomKey = KeyCodes.Escape;
					break;
				case Key.F1:
					axiomKey = KeyCodes.F1;
					break;
				case Key.F2:
					axiomKey = KeyCodes.F2;
					break;
				case Key.F3:
					axiomKey = KeyCodes.F3;
					break;
				case Key.F4:
					axiomKey = KeyCodes.F4;
					break;
				case Key.F5:
					axiomKey = KeyCodes.F5;
					break;
				case Key.F6:
					axiomKey = KeyCodes.F6;
					break;
				case Key.F7:
					axiomKey = KeyCodes.F7;
					break;
				case Key.F8:
					axiomKey = KeyCodes.F8;
					break;
				case Key.F9:
					axiomKey = KeyCodes.F9;
					break;
				case Key.F10:
					axiomKey = KeyCodes.F10;
					break;
				case Key.D0:
					axiomKey = KeyCodes.D0;
					break;
				case Key.D1:
					axiomKey = KeyCodes.D1;
					break;
				case Key.D2:
					axiomKey = KeyCodes.D2;
					break;
				case Key.D3:
					axiomKey = KeyCodes.D3;
					break;
				case Key.D4:
					axiomKey = KeyCodes.D4;
					break;
				case Key.D5:
					axiomKey = KeyCodes.D5;
					break;
				case Key.D6:
					axiomKey = KeyCodes.D6;
					break;
				case Key.D7:
					axiomKey = KeyCodes.D7;
					break;
				case Key.D8:
					axiomKey = KeyCodes.D8;
					break;
				case Key.D9:
					axiomKey = KeyCodes.D9;
					break;
				case Key.F11:
					axiomKey = KeyCodes.F11;
					break;
				case Key.F12:
					axiomKey = KeyCodes.F12;
					break;
				case Key.Return:
					axiomKey = KeyCodes.Enter;
					break;
				case Key.Tab:
					axiomKey = KeyCodes.Tab;
					break;
				case Key.LeftShift:
					axiomKey = KeyCodes.LeftShift;
					break;
				case Key.RightShift:
					axiomKey = KeyCodes.RightShift;
					break;
				case Key.LeftControl:
					axiomKey = KeyCodes.LeftControl;
					break;
				case Key.RightControl:
					axiomKey = KeyCodes.RightControl;
					break;
				case Key.Period:
					axiomKey = KeyCodes.Period;
					break;
				case Key.Comma:
					axiomKey = KeyCodes.Comma;
					break;
				case Key.Home:
					axiomKey = KeyCodes.Home;
					break;
				case Key.PageUp:
					axiomKey = KeyCodes.PageUp;
					break;
				case Key.PageDown:
					axiomKey = KeyCodes.PageDown;
					break;
				case Key.End:
					axiomKey = KeyCodes.End;
					break;
				case Key.Semicolon:
					axiomKey = KeyCodes.Semicolon;
					break;
				case Key.Minus:
					axiomKey = KeyCodes.Subtract;
					break;
				case Key.Equals:
					axiomKey = KeyCodes.Add;
					break;
				case Key.Back:
					axiomKey = KeyCodes.Backspace;
					break;
				case Key.Delete:
					axiomKey = KeyCodes.Delete;
					break;
				case Key.Insert:
					axiomKey = KeyCodes.Insert;
					break;
				case Key.LeftAlt:
					axiomKey = KeyCodes.LeftAlt;
					break;
				case Key.RightAlt:
					axiomKey = KeyCodes.RightAlt;
					break;
				case Key.Space:
					axiomKey = KeyCodes.Space;
					break;
				case Key.Grave:
					axiomKey = KeyCodes.Tilde;
					break;
				case Key.LeftBracket:
					axiomKey = KeyCodes.OpenBracket;
					break;
				case Key.RightBracket:
					axiomKey = KeyCodes.CloseBracket;
					break;
				//case DI.Key.Equals:
				//    axiomKey = KeyCodes.Plus;
				//    break;
				//case DI.Key.Minus:
				//    axiomKey = KeyCodes.Subtract;
				//    break;
				case Key.Slash:
					axiomKey = KeyCodes.QuestionMark;
					break;
				case Key.Apostrophe:
					axiomKey = KeyCodes.Quotes;
					break;
				case Key.Backslash:
					axiomKey = KeyCodes.Backslash;
					break;
				case Key.NumberPad0:
					axiomKey = KeyCodes.NumPad0;
					break;
				case Key.NumberPad1:
					axiomKey = KeyCodes.NumPad1;
					break;
				case Key.NumberPad2:
					axiomKey = KeyCodes.NumPad2;
					break;
				case Key.NumberPad3:
					axiomKey = KeyCodes.NumPad3;
					break;
				case Key.NumberPad4:
					axiomKey = KeyCodes.NumPad4;
					break;
				case Key.NumberPad5:
					axiomKey = KeyCodes.NumPad5;
					break;
				case Key.NumberPad6:
					axiomKey = KeyCodes.NumPad6;
					break;
				case Key.NumberPad7:
					axiomKey = KeyCodes.NumPad7;
					break;
				case Key.NumberPad8:
					axiomKey = KeyCodes.NumPad8;
					break;
				case Key.NumberPad9:
					axiomKey = KeyCodes.NumPad9;
					break;
			}

			return axiomKey;
		}

		#endregion Keycode Conversions

		#endregion Helper Methods
	};
}
