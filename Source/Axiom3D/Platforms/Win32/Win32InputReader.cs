#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
#endregion

#region Namespace Declarations

using System;
using System.Collections;
using System.Windows.Forms;

using Axiom.Input;
using Axiom;

using DX = Microsoft.DirectX;
using DXI = Microsoft.DirectX.DirectInput;

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
        ///		Holds a snapshot of DirectInput keyboard state.
        /// </summary>
        protected DXI.KeyboardState keyboardState;
        /// <summary>
        ///		Holds a snapshot of DirectInput mouse state.
        /// </summary>
        protected DXI.MouseState mouseState;

        /// <summary>
        ///		DirectInput keyboard device.
        /// </summary>
        protected DXI.Device keyboardDevice;

        /// <summary>
        ///		DirectInput mouse device.
        /// </summary>
        protected DXI.Device mouseDevice;
        protected int mouseRelX, mouseRelY, mouseRelZ;
        protected int mouseAbsX, mouseAbsY, mouseAbsZ;
        protected bool isInitialized;
        protected bool useMouse, useKeyboard, useGamepad;
        protected int mouseButtons;

        /// <summary>
        ///		Active host control that reserves control over the input.
        /// </summary>
        protected System.Windows.Forms.Control control;

        /// <summary>
        ///		Do we want exclusive use of the mouse?
        /// </summary>
        protected bool ownMouse;

        /// <summary>
        ///		Reference to the render window that is the target of the input.
        /// </summary>
        protected RenderWindow window;

        #endregion Fields

        #region Constants

        /// <summary>
        ///		Size to use for DirectInput's input buffer.
        /// </summary>
        const int BufferSize = 16;

        #endregion Constants

        #region InputReader Members

        #region Properties

        /// <summary>
        ///		Retrieves the relative (compared to the last input poll) mouse movement
        ///		on the X (horizontal) axis.
        /// </summary>
        public override int RelativeMouseX
        {
            get
            {
                return mouseRelX;
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
                return mouseRelY;
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
                return mouseRelZ;
            }
        }

        /// <summary>
        ///		Retrieves the absolute mouse position on the X (horizontal) axis.
        /// </summary>
        public override int AbsoluteMouseX
        {
            get
            {
                return mouseAbsX;
            }
        }

        /// <summary>
        ///		Retrieves the absolute mouse position on the Y (vertical) axis.
        /// </summary>
        public override int AbsoluteMouseY
        {
            get
            {
                return mouseAbsY;
            }
        }

        /// <summary>
        ///		Retrieves the absolute mouse position on the Z (mouse wheel) axis.
        /// </summary>
        public override int AbsoluteMouseZ
        {
            get
            {
                return mouseAbsZ;
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
                    if ( keyboardDevice != null )
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
                    if ( mouseDevice != null )
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
            this.control.BringToFront();
            this.control.Select();
            this.control.Show();

            if ( window.IsActive )
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
                        if ( useKeyboard )
                        {
                            keyboardDevice.Acquire();
                        }
                        if ( useMouse )
                        {
                            mouseDevice.Acquire();
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

            // for Windows, this should be a S.W.F.Control
            control = window.Handle as System.Windows.Forms.Control;

            if ( control is System.Windows.Forms.Form )
            {
                //control = control;
            }
            else if ( control is System.Windows.Forms.PictureBox )
            {
                // if the control is a picturebox, we need to grab its parent form
                while ( !( control is System.Windows.Forms.Form ) && control != null )
                {
                    control = control.Parent;
                }
            }
            else
            {
                throw new AxiomException( "Win32InputReader requires the RenderWindow to have an associated handle of either a PictureBox or a Form." );
            }

            // initialize keyboard if needed
            if ( useKeyboard )
            {
                InitializeKeyboard();
            }

            // initialize the mouse if needed
            if ( useMouse )
            {
                InitializeMouse();
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
            if ( keyboardState != null )
            {
                // get the DXI.Key enum from the System.Windows.Forms.Keys enum passed in
                DXI.Key daKey = ConvertKeyEnum( key );

                if ( keyboardState[daKey] )
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
            if ( keyboardDevice != null )
            {
                keyboardDevice.Unacquire();
                keyboardDevice.Dispose();
                keyboardDevice = null;
            }

            if ( mouseDevice != null )
            {
                mouseDevice.Unacquire();
                mouseDevice.Dispose();
                mouseDevice = null;
            }
        }


        #endregion Methods

        #endregion InputReader implementation

        #region Helper Methods

        /// <summary>
        ///		Initializes the keyboard using either immediate mode or event based input.
        /// </summary>
        private void InitializeKeyboard()
        {
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

            if ( useKeyboard )
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

            if ( useMouse )
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
        ///		Initializes the mouse using either immediate mode or event based input.
        /// </summary>
        private void InitializeMouse()
        {
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
            // Find the GUID
            Guid keyboardGuid = new Guid();
            System.Collections.ObjectModel.ReadOnlyCollection<DXI.DeviceInstance> deviceInstances = DXI.Manager.GetDevices( DXI.DeviceType.Keyboard, DXI.EnumDevicesFlags.AllDevices );
            if ( deviceInstances.Count >= 1 )
                keyboardGuid = deviceInstances[ 0 ].Instance;

            // Create the device.
            keyboardDevice = new DXI.Device( keyboardGuid );

            // grab the keyboard non-exclusively
            keyboardDevice.SetCooperativeLevel( IntPtr.Zero, DXI.CooperativeLevelFlags.NonExclusive | DXI.CooperativeLevelFlags.Background );

            // Set the data format to the keyboard pre-defined format.
            keyboardDevice.SetDataFormat( DXI.DeviceDataFormat.Keyboard );

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
            // Find the GUID
            Guid keyboardGuid = new Guid();
            System.Collections.ObjectModel.ReadOnlyCollection<DXI.DeviceInstance> deviceInstances = DXI.Manager.GetDevices( DXI.DeviceType.Keyboard, DXI.EnumDevicesFlags.AllDevices );
            if ( deviceInstances.Count >= 1 )
                keyboardGuid = deviceInstances[ 0 ].Instance;

            // create the device
            keyboardDevice = new DXI.Device( keyboardGuid );

            // Set the data format to the keyboard pre-defined format.
            keyboardDevice.SetDataFormat( DXI.DeviceDataFormat.Keyboard );

            // grab the keyboard non-exclusively
            keyboardDevice.SetCooperativeLevel( IntPtr.Zero, DXI.CooperativeLevelFlags.NonExclusive | DXI.CooperativeLevelFlags.Background );

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
            // Find the GUID
            Guid mouseGuid = new Guid();
            System.Collections.ObjectModel.ReadOnlyCollection<DXI.DeviceInstance> deviceInstances = DXI.Manager.GetDevices( DXI.DeviceType.Mouse, DXI.EnumDevicesFlags.AllDevices );
            if ( deviceInstances.Count >= 1 )
                mouseGuid = deviceInstances[ 0 ].Instance;

            // create the device
            mouseDevice = new DXI.Device( mouseGuid );

            mouseDevice.Properties.IsAxisModeAbsolute = true;

            // set the device format so DXI knows this device is a mouse
            mouseDevice.SetDataFormat( DXI.DeviceDataFormat.Mouse );

            // set cooperation level
            if ( ownMouse )
            {
                mouseDevice.SetCooperativeLevel( control.Handle, DXI.CooperativeLevelFlags.Exclusive | DXI.CooperativeLevelFlags.Foreground );
            }
            else
            {
                mouseDevice.SetCooperativeLevel( IntPtr.Zero, DXI.CooperativeLevelFlags.NonExclusive | DXI.CooperativeLevelFlags.Background );
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
            DXI.BufferedDataCollection bufferedData = keyboardDevice.GetBufferedData();

            // please tell me why this would ever come back null, rather than an empty collection...
            if ( bufferedData == null )
            {
                return;
            }

            for ( int i = 0; i < bufferedData.Count; i++ )
            {
                DXI.BufferedData data = bufferedData[i];

                KeyCodes key = ConvertKeyEnum( (DXI.Key)data.Offset );

                KeyChanged( key, data.IsButtonPressed );
            }
        }

        /// <summary>
        ///		Captures an immediate keyboard state snapshot (for non-buffered data).
        /// </summary>
        private void CaptureKeyboard()
        {
            keyboardState = keyboardDevice.CurrentKeyboardState;
        }

        /// <summary>
        ///		Captures the mouse input based on the preffered input mode.
        /// </summary>
        private void CaptureMouse()
        {
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
            // TODO Implement
        }

        /// <summary>
        ///		Takes a snapshot of the mouse state for immediate input checking.
        /// </summary>
        private void CaptureImmediateMouse()
        {
            try
            {
                // capture the current mouse state
                mouseState = mouseDevice.CurrentMouseState;
            }
            catch ( DXI.InputLostException )
            {
                try
                {
                    mouseDevice.Acquire();
                    mouseState = mouseDevice.CurrentMouseState;
                }
                catch ( Exception )
                {
                }
            }

            // store the updated absolute values
            mouseAbsX += mouseState.X;
            mouseAbsY += mouseState.Y;
            mouseAbsZ += mouseState.Z;

            // calc relative deviance from center
            mouseRelX = mouseState.X;
            mouseRelY = mouseState.Y;
            mouseRelZ = mouseState.Z;

            bool[] buttons = mouseState.GetButtons();

            // clear the flags
            mouseButtons = 0;

            for ( int i = 0; i < buttons.Length; i++ )
            {
                if ( buttons[i] )
                {
                    mouseButtons |= ( 1 << i );
                }
            }
        }


        #region Keycode Conversions

        /// <summary>
        ///		Used to convert an Axiom.Input.KeyCodes enum val to a DirectInput.Key enum val.
        /// </summary>
        /// <param name="key">Axiom keyboard code to query.</param>
        /// <returns>The equivalent enum value in the DXI.Key enum.</returns>
        private DXI.Key ConvertKeyEnum( KeyCodes key )
        {
            // TODO Quotes
            DXI.Key DXIKey = 0;

            switch ( key )
            {
                case KeyCodes.A:
                    DXIKey = DXI.Key.A;
                    break;
                case KeyCodes.B:
                    DXIKey = DXI.Key.B;
                    break;
                case KeyCodes.C:
                    DXIKey = DXI.Key.C;
                    break;
                case KeyCodes.D:
                    DXIKey = DXI.Key.D;
                    break;
                case KeyCodes.E:
                    DXIKey = DXI.Key.E;
                    break;
                case KeyCodes.F:
                    DXIKey = DXI.Key.F;
                    break;
                case KeyCodes.G:
                    DXIKey = DXI.Key.G;
                    break;
                case KeyCodes.H:
                    DXIKey = DXI.Key.H;
                    break;
                case KeyCodes.I:
                    DXIKey = DXI.Key.I;
                    break;
                case KeyCodes.J:
                    DXIKey = DXI.Key.J;
                    break;
                case KeyCodes.K:
                    DXIKey = DXI.Key.K;
                    break;
                case KeyCodes.L:
                    DXIKey = DXI.Key.L;
                    break;
                case KeyCodes.M:
                    DXIKey = DXI.Key.M;
                    break;
                case KeyCodes.N:
                    DXIKey = DXI.Key.N;
                    break;
                case KeyCodes.O:
                    DXIKey = DXI.Key.O;
                    break;
                case KeyCodes.P:
                    DXIKey = DXI.Key.P;
                    break;
                case KeyCodes.Q:
                    DXIKey = DXI.Key.Q;
                    break;
                case KeyCodes.R:
                    DXIKey = DXI.Key.R;
                    break;
                case KeyCodes.S:
                    DXIKey = DXI.Key.S;
                    break;
                case KeyCodes.T:
                    DXIKey = DXI.Key.T;
                    break;
                case KeyCodes.U:
                    DXIKey = DXI.Key.U;
                    break;
                case KeyCodes.V:
                    DXIKey = DXI.Key.V;
                    break;
                case KeyCodes.W:
                    DXIKey = DXI.Key.W;
                    break;
                case KeyCodes.X:
                    DXIKey = DXI.Key.X;
                    break;
                case KeyCodes.Y:
                    DXIKey = DXI.Key.Y;
                    break;
                case KeyCodes.Z:
                    DXIKey = DXI.Key.Z;
                    break;
                case KeyCodes.Left:
                    DXIKey = DXI.Key.LeftArrow;
                    break;
                case KeyCodes.Right:
                    DXIKey = DXI.Key.RightArrow;
                    break;
                case KeyCodes.Up:
                    DXIKey = DXI.Key.UpArrow;
                    break;
                case KeyCodes.Down:
                    DXIKey = DXI.Key.DownArrow;
                    break;
                case KeyCodes.Escape:
                    DXIKey = DXI.Key.Escape;
                    break;
                case KeyCodes.F1:
                    DXIKey = DXI.Key.F1;
                    break;
                case KeyCodes.F2:
                    DXIKey = DXI.Key.F2;
                    break;
                case KeyCodes.F3:
                    DXIKey = DXI.Key.F3;
                    break;
                case KeyCodes.F4:
                    DXIKey = DXI.Key.F4;
                    break;
                case KeyCodes.F5:
                    DXIKey = DXI.Key.F5;
                    break;
                case KeyCodes.F6:
                    DXIKey = DXI.Key.F6;
                    break;
                case KeyCodes.F7:
                    DXIKey = DXI.Key.F7;
                    break;
                case KeyCodes.F8:
                    DXIKey = DXI.Key.F8;
                    break;
                case KeyCodes.F9:
                    DXIKey = DXI.Key.F9;
                    break;
                case KeyCodes.F10:
                    DXIKey = DXI.Key.F10;
                    break;
                case KeyCodes.D0:
                    DXIKey = DXI.Key.D0;
                    break;
                case KeyCodes.D1:
                    DXIKey = DXI.Key.D1;
                    break;
                case KeyCodes.D2:
                    DXIKey = DXI.Key.D2;
                    break;
                case KeyCodes.D3:
                    DXIKey = DXI.Key.D3;
                    break;
                case KeyCodes.D4:
                    DXIKey = DXI.Key.D4;
                    break;
                case KeyCodes.D5:
                    DXIKey = DXI.Key.D5;
                    break;
                case KeyCodes.D6:
                    DXIKey = DXI.Key.D6;
                    break;
                case KeyCodes.D7:
                    DXIKey = DXI.Key.D7;
                    break;
                case KeyCodes.D8:
                    DXIKey = DXI.Key.D8;
                    break;
                case KeyCodes.D9:
                    DXIKey = DXI.Key.D9;
                    break;
                case KeyCodes.F11:
                    DXIKey = DXI.Key.F11;
                    break;
                case KeyCodes.F12:
                    DXIKey = DXI.Key.F12;
                    break;
                case KeyCodes.Enter:
                    DXIKey = DXI.Key.Return;
                    break;
                case KeyCodes.Tab:
                    DXIKey = DXI.Key.Tab;
                    break;
                case KeyCodes.LeftShift:
                    DXIKey = DXI.Key.LeftShift;
                    break;
                case KeyCodes.RightShift:
                    DXIKey = DXI.Key.RightShift;
                    break;
                case KeyCodes.LeftControl:
                    DXIKey = DXI.Key.LeftControl;
                    break;
                case KeyCodes.RightControl:
                    DXIKey = DXI.Key.RightControl;
                    break;
                case KeyCodes.Period:
                    DXIKey = DXI.Key.Period;
                    break;
                case KeyCodes.Comma:
                    DXIKey = DXI.Key.Comma;
                    break;
                case KeyCodes.Home:
                    DXIKey = DXI.Key.Home;
                    break;
                case KeyCodes.PageUp:
                    DXIKey = DXI.Key.PageUp;
                    break;
                case KeyCodes.PageDown:
                    DXIKey = DXI.Key.PageDown;
                    break;
                case KeyCodes.End:
                    DXIKey = DXI.Key.End;
                    break;
                case KeyCodes.Semicolon:
                    DXIKey = DXI.Key.SemiColon;
                    break;
                case KeyCodes.Subtract:
                    DXIKey = DXI.Key.Subtract;
                    break;
                case KeyCodes.Add:
                    DXIKey = DXI.Key.Add;
                    break;
                case KeyCodes.Backspace:
                    DXIKey = DXI.Key.BackSpace;
                    break;
                case KeyCodes.Delete:
                    DXIKey = DXI.Key.Delete;
                    break;
                case KeyCodes.Insert:
                    DXIKey = DXI.Key.Insert;
                    break;
                case KeyCodes.LeftAlt:
                    DXIKey = DXI.Key.LeftAlt;
                    break;
                case KeyCodes.RightAlt:
                    DXIKey = DXI.Key.RightAlt;
                    break;
                case KeyCodes.Space:
                    DXIKey = DXI.Key.Space;
                    break;
                case KeyCodes.Tilde:
                    DXIKey = DXI.Key.Grave;
                    break;
                case KeyCodes.OpenBracket:
                    DXIKey = DXI.Key.LeftBracket;
                    break;
                case KeyCodes.CloseBracket:
                    DXIKey = DXI.Key.RightBracket;
                    break;
                case KeyCodes.Plus:
                    DXIKey = DXI.Key.Equals;
                    break;
                case KeyCodes.QuestionMark:
                    DXIKey = DXI.Key.Slash;
                    break;
                case KeyCodes.Quotes:
                    DXIKey = DXI.Key.Apostrophe;
                    break;
                case KeyCodes.Backslash:
                    DXIKey = DXI.Key.BackSlash;
                    break;
                case KeyCodes.NumPad0:
                    DXIKey = DXI.Key.NumPad0;
                    break;
                case KeyCodes.NumPad1:
                    DXIKey = DXI.Key.NumPad1;
                    break;
                case KeyCodes.NumPad2:
                    DXIKey = DXI.Key.NumPad2;
                    break;
                case KeyCodes.NumPad3:
                    DXIKey = DXI.Key.NumPad3;
                    break;
                case KeyCodes.NumPad4:
                    DXIKey = DXI.Key.NumPad4;
                    break;
                case KeyCodes.NumPad5:
                    DXIKey = DXI.Key.NumPad5;
                    break;
                case KeyCodes.NumPad6:
                    DXIKey = DXI.Key.NumPad6;
                    break;
                case KeyCodes.NumPad7:
                    DXIKey = DXI.Key.NumPad7;
                    break;
                case KeyCodes.NumPad8:
                    DXIKey = DXI.Key.NumPad8;
                    break;
                case KeyCodes.NumPad9:
                    DXIKey = DXI.Key.NumPad9;
                    break;
                case KeyCodes.PrintScreen:
                    DXIKey = DXI.Key.SysRq;
                    break;
            }

            return DXIKey;
        }

        /// <summary>
        ///		Used to convert a DirectInput.Key enum val to a Axiom.Input.KeyCodes enum val.
        /// </summary>
        /// <param name="key">DirectInput.Key code to query.</param>
        /// <returns>The equivalent enum value in the Axiom.KeyCodes enum.</returns>
        private Axiom.Input.KeyCodes ConvertKeyEnum( DXI.Key key )
        {
            // TODO Quotes
            Axiom.Input.KeyCodes axiomKey = 0;

            switch ( key )
            {
                case DXI.Key.SysRq:	//same key as PrintScreen
                    axiomKey = Axiom.Input.KeyCodes.PrintScreen;
                    break;
                case DXI.Key.A:
                    axiomKey = Axiom.Input.KeyCodes.A;
                    break;
                case DXI.Key.B:
                    axiomKey = Axiom.Input.KeyCodes.B;
                    break;
                case DXI.Key.C:
                    axiomKey = Axiom.Input.KeyCodes.C;
                    break;
                case DXI.Key.D:
                    axiomKey = Axiom.Input.KeyCodes.D;
                    break;
                case DXI.Key.E:
                    axiomKey = Axiom.Input.KeyCodes.E;
                    break;
                case DXI.Key.F:
                    axiomKey = Axiom.Input.KeyCodes.F;
                    break;
                case DXI.Key.G:
                    axiomKey = Axiom.Input.KeyCodes.G;
                    break;
                case DXI.Key.H:
                    axiomKey = Axiom.Input.KeyCodes.H;
                    break;
                case DXI.Key.I:
                    axiomKey = Axiom.Input.KeyCodes.I;
                    break;
                case DXI.Key.J:
                    axiomKey = Axiom.Input.KeyCodes.J;
                    break;
                case DXI.Key.K:
                    axiomKey = Axiom.Input.KeyCodes.K;
                    break;
                case DXI.Key.L:
                    axiomKey = Axiom.Input.KeyCodes.L;
                    break;
                case DXI.Key.M:
                    axiomKey = Axiom.Input.KeyCodes.M;
                    break;
                case DXI.Key.N:
                    axiomKey = Axiom.Input.KeyCodes.N;
                    break;
                case DXI.Key.O:
                    axiomKey = Axiom.Input.KeyCodes.O;
                    break;
                case DXI.Key.P:
                    axiomKey = Axiom.Input.KeyCodes.P;
                    break;
                case DXI.Key.Q:
                    axiomKey = Axiom.Input.KeyCodes.Q;
                    break;
                case DXI.Key.R:
                    axiomKey = Axiom.Input.KeyCodes.R;
                    break;
                case DXI.Key.S:
                    axiomKey = Axiom.Input.KeyCodes.S;
                    break;
                case DXI.Key.T:
                    axiomKey = Axiom.Input.KeyCodes.T;
                    break;
                case DXI.Key.U:
                    axiomKey = Axiom.Input.KeyCodes.U;
                    break;
                case DXI.Key.V:
                    axiomKey = Axiom.Input.KeyCodes.V;
                    break;
                case DXI.Key.W:
                    axiomKey = Axiom.Input.KeyCodes.W;
                    break;
                case DXI.Key.X:
                    axiomKey = Axiom.Input.KeyCodes.X;
                    break;
                case DXI.Key.Y:
                    axiomKey = Axiom.Input.KeyCodes.Y;
                    break;
                case DXI.Key.Z:
                    axiomKey = Axiom.Input.KeyCodes.Z;
                    break;
                case DXI.Key.LeftArrow:
                    axiomKey = Axiom.Input.KeyCodes.Left;
                    break;
                case DXI.Key.RightArrow:
                    axiomKey = Axiom.Input.KeyCodes.Right;
                    break;
                case DXI.Key.UpArrow:
                    axiomKey = Axiom.Input.KeyCodes.Up;
                    break;
                case DXI.Key.DownArrow:
                    axiomKey = Axiom.Input.KeyCodes.Down;
                    break;
                case DXI.Key.Escape:
                    axiomKey = Axiom.Input.KeyCodes.Escape;
                    break;
                case DXI.Key.F1:
                    axiomKey = Axiom.Input.KeyCodes.F1;
                    break;
                case DXI.Key.F2:
                    axiomKey = Axiom.Input.KeyCodes.F2;
                    break;
                case DXI.Key.F3:
                    axiomKey = Axiom.Input.KeyCodes.F3;
                    break;
                case DXI.Key.F4:
                    axiomKey = Axiom.Input.KeyCodes.F4;
                    break;
                case DXI.Key.F5:
                    axiomKey = Axiom.Input.KeyCodes.F5;
                    break;
                case DXI.Key.F6:
                    axiomKey = Axiom.Input.KeyCodes.F6;
                    break;
                case DXI.Key.F7:
                    axiomKey = Axiom.Input.KeyCodes.F7;
                    break;
                case DXI.Key.F8:
                    axiomKey = Axiom.Input.KeyCodes.F8;
                    break;
                case DXI.Key.F9:
                    axiomKey = Axiom.Input.KeyCodes.F9;
                    break;
                case DXI.Key.F10:
                    axiomKey = Axiom.Input.KeyCodes.F10;
                    break;
                case DXI.Key.D0:
                    axiomKey = Axiom.Input.KeyCodes.D0;
                    break;
                case DXI.Key.D1:
                    axiomKey = Axiom.Input.KeyCodes.D1;
                    break;
                case DXI.Key.D2:
                    axiomKey = Axiom.Input.KeyCodes.D2;
                    break;
                case DXI.Key.D3:
                    axiomKey = Axiom.Input.KeyCodes.D3;
                    break;
                case DXI.Key.D4:
                    axiomKey = Axiom.Input.KeyCodes.D4;
                    break;
                case DXI.Key.D5:
                    axiomKey = Axiom.Input.KeyCodes.D5;
                    break;
                case DXI.Key.D6:
                    axiomKey = Axiom.Input.KeyCodes.D6;
                    break;
                case DXI.Key.D7:
                    axiomKey = Axiom.Input.KeyCodes.D7;
                    break;
                case DXI.Key.D8:
                    axiomKey = Axiom.Input.KeyCodes.D8;
                    break;
                case DXI.Key.D9:
                    axiomKey = Axiom.Input.KeyCodes.D9;
                    break;
                case DXI.Key.F11:
                    axiomKey = Axiom.Input.KeyCodes.F11;
                    break;
                case DXI.Key.F12:
                    axiomKey = Axiom.Input.KeyCodes.F12;
                    break;
                case DXI.Key.Return:
                    axiomKey = Axiom.Input.KeyCodes.Enter;
                    break;
                case DXI.Key.Tab:
                    axiomKey = Axiom.Input.KeyCodes.Tab;
                    break;
                case DXI.Key.LeftShift:
                    axiomKey = Axiom.Input.KeyCodes.LeftShift;
                    break;
                case DXI.Key.RightShift:
                    axiomKey = Axiom.Input.KeyCodes.RightShift;
                    break;
                case DXI.Key.LeftControl:
                    axiomKey = Axiom.Input.KeyCodes.LeftControl;
                    break;
                case DXI.Key.RightControl:
                    axiomKey = Axiom.Input.KeyCodes.RightControl;
                    break;
                case DXI.Key.Period:
                    axiomKey = Axiom.Input.KeyCodes.Period;
                    break;
                case DXI.Key.Comma:
                    axiomKey = Axiom.Input.KeyCodes.Comma;
                    break;
                case DXI.Key.Home:
                    axiomKey = Axiom.Input.KeyCodes.Home;
                    break;
                case DXI.Key.PageUp:
                    axiomKey = Axiom.Input.KeyCodes.PageUp;
                    break;
                case DXI.Key.PageDown:
                    axiomKey = Axiom.Input.KeyCodes.PageDown;
                    break;
                case DXI.Key.End:
                    axiomKey = Axiom.Input.KeyCodes.End;
                    break;
                case DXI.Key.SemiColon:
                    axiomKey = Axiom.Input.KeyCodes.Semicolon;
                    break;
                case DXI.Key.Subtract:
                    axiomKey = Axiom.Input.KeyCodes.Subtract;
                    break;
                case DXI.Key.Add:
                    axiomKey = Axiom.Input.KeyCodes.Add;
                    break;
                case DXI.Key.BackSpace:
                    axiomKey = Axiom.Input.KeyCodes.Backspace;
                    break;
                case DXI.Key.Delete:
                    axiomKey = Axiom.Input.KeyCodes.Delete;
                    break;
                case DXI.Key.Insert:
                    axiomKey = Axiom.Input.KeyCodes.Insert;
                    break;
                case DXI.Key.LeftAlt:
                    axiomKey = Axiom.Input.KeyCodes.LeftAlt;
                    break;
                case DXI.Key.RightAlt:
                    axiomKey = Axiom.Input.KeyCodes.RightAlt;
                    break;
                case DXI.Key.Space:
                    axiomKey = Axiom.Input.KeyCodes.Space;
                    break;
                case DXI.Key.Grave:
                    axiomKey = Axiom.Input.KeyCodes.Tilde;
                    break;
                case DXI.Key.LeftBracket:
                    axiomKey = Axiom.Input.KeyCodes.OpenBracket;
                    break;
                case DXI.Key.RightBracket:
                    axiomKey = Axiom.Input.KeyCodes.CloseBracket;
                    break;
                case DXI.Key.Equals:
                    axiomKey = KeyCodes.Plus;
                    break;
                case DXI.Key.Minus:
                    axiomKey = KeyCodes.Subtract;
                    break;
                case DXI.Key.Slash:
                    axiomKey = KeyCodes.QuestionMark;
                    break;
                case DXI.Key.Apostrophe:
                    axiomKey = KeyCodes.Quotes;
                    break;
                case DXI.Key.BackSlash:
                    axiomKey = KeyCodes.Backslash;
                    break;
                case DXI.Key.NumPad0:
                    axiomKey = Axiom.Input.KeyCodes.NumPad0;
                    break;
                case DXI.Key.NumPad1:
                    axiomKey = Axiom.Input.KeyCodes.NumPad1;
                    break;
                case DXI.Key.NumPad2:
                    axiomKey = Axiom.Input.KeyCodes.NumPad2;
                    break;
                case DXI.Key.NumPad3:
                    axiomKey = Axiom.Input.KeyCodes.NumPad3;
                    break;
                case DXI.Key.NumPad4:
                    axiomKey = Axiom.Input.KeyCodes.NumPad4;
                    break;
                case DXI.Key.NumPad5:
                    axiomKey = Axiom.Input.KeyCodes.NumPad5;
                    break;
                case DXI.Key.NumPad6:
                    axiomKey = Axiom.Input.KeyCodes.NumPad6;
                    break;
                case DXI.Key.NumPad7:
                    axiomKey = Axiom.Input.KeyCodes.NumPad7;
                    break;
                case DXI.Key.NumPad8:
                    axiomKey = Axiom.Input.KeyCodes.NumPad8;
                    break;
                case DXI.Key.NumPad9:
                    axiomKey = Axiom.Input.KeyCodes.NumPad9;
                    break;
            }

            return axiomKey;
        }

        #endregion Keycode Conversions

        #endregion Helper Methods
    }
}