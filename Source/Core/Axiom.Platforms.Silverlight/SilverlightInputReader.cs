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

using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using KeyEventArgs = Axiom.Input.KeyEventArgs;
using ModifierKeys = Axiom.Input.ModifierKeys;

#endregion Namespace Declarations

namespace Axiom.Platforms.Silverlight
{
    /// <summary>
    ///	Silverlight input implementation using Managed DirectInput (tm).
    /// </summary>
    public class SilverlightInputReader : InputReader
    {
        #region Fields

        private DrawingSurface drawingSurface;

        protected int mouseRelX, mouseRelY, mouseRelZ;
        protected int mouseAbsX, mouseAbsY, mouseAbsZ;
        protected bool isInitialized;
        protected bool useMouse, useKeyboard, useGamepad;
        protected bool ownMouse;

        private readonly bool[] keys = new bool[(int)KeyCodes.RightAlt];

        /// <summary>
        ///		Reference to the render window that is the target of the input.
        /// </summary>
        protected RenderWindow window;

        /// <summary>
        ///		Flag used to remember the state of the render window the last time input was captured.
        /// </summary>
        protected bool lastWindowActive;

        #endregion Fields    

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
                var mX = mouseRelX;
                if (useMouse)
                    mouseRelX = 0;
                return mX;
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
                var mY = mouseRelY;
                if (useMouse)
                    mouseRelY = 0;
                return mY;
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
                var mZ= mouseRelZ;
                if (useMouse)
                    mouseRelZ = 0;
                return mZ;
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
                useKeyboardEvents = value;
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
                useMouseEvents = value;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///	Captures the state of all active input controllers.
        /// </summary>
        public override void Capture()
        {
            //if (window.IsActive)
            //{
            //    try
            //    {
            //        CaptureInput();
            //    }
            //    catch (Exception)
            //    {
            //        try
            //        {
            //            // try to acquire device and try again
            //            if (useKeyboard)
            //            {
            //                keyboardDevice.Acquire();
            //            }
            //            if (useMouse)
            //            {
            //                mouseDevice.Acquire();
            //            }
            //            CaptureInput();
            //        }
            //        catch (Exception)
            //        {
            //            ClearInput(); //not to appear as something would be pressed or whatever.
            //        }
            //    }
            //}
        }

        private bool mouseCaptured;

        /// <summary>
        ///		Intializes DirectInput for use on Win32 platforms.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="useKeyboard"></param>
        /// <param name="useMouse"></param>
        /// <param name="useGamepad"></param>
        public override void Initialize( RenderWindow window, bool useKeyboard, bool useMouse, bool useGamepad,
                                         bool ownMouse )
        {
            this.useKeyboard = useKeyboard;
            this.useMouse = useMouse;
            this.useGamepad = useGamepad;
            this.ownMouse = ownMouse;
            this.window = window;

            drawingSurface = (DrawingSurface)window[ "DRAWINGSURFACE" ];

            // initialize keyboard if needed
            if ( useKeyboard )
            {
                for (var ui = drawingSurface as FrameworkElement; ui != null; ui = ui.Parent as FrameworkElement)
                {
                    ui.KeyDown += ( s, e ) =>
                                  {
                                      var key = ConvertKeyEnum( e.Key );
                                      keys[ (int)key ] = true;
                                      modifiers |= ConvertModifierKeysEnum( e.Key );

                                      if ( useKeyboardEvents )
                                          OnKeyDown( new KeyEventArgs( key, modifiers ) );

                                      //HACK: temporary
                                      if (key == KeyCodes.F9)
                                      {
                                          foreach (var cam in Root.Instance.SceneManager.Cameras)
                                              cam.PolygonMode = cam.PolygonMode == PolygonMode.Wireframe
                                                                    ? PolygonMode.Solid
                                                                    : PolygonMode.Wireframe;
                                      }
                                  };
                    ui.KeyUp += ( s, e ) =>
                                {
                                    var key = ConvertKeyEnum( e.Key );
                                    keys[ (int)key ] = false;
                                    modifiers &= ~ConvertModifierKeysEnum( e.Key );

                                    if ( useKeyboardEvents )
                                        OnKeyUp( new KeyEventArgs( key, modifiers ) );
                                };
                }
            }

            // initialize the mouse if needed
            if ( useMouse )
            {
                drawingSurface.MouseEnter += ( s, e ) =>
                                             {
                                                 var p = e.GetPosition( drawingSurface );
                                                 mouseAbsX = (int)p.X;
                                                 mouseAbsY = (int)p.Y;
                                             };

                drawingSurface.MouseMove += ( s, e ) =>
                                            {
                                                if ( mouseCaptured )
                                                {
                                                    var p = e.GetPosition( drawingSurface );
                                                    if (true)
                                                    {
                                                        mouseRelX = mouseAbsX - (int)p.X;
                                                        mouseRelY = mouseAbsY - (int)p.Y;
                                                        mouseAbsX -= mouseRelX;
                                                        mouseAbsY -= mouseRelY;
                                                    }
                                                    else
                                                    {
                                                        mouseRelX = (int)p.X - mouseAbsX;
                                                        mouseRelY = (int)p.Y - mouseAbsY;
                                                        mouseAbsX += mouseRelX;
                                                        mouseAbsY += mouseRelY;
                                                    }
                                                }
                                            };

                drawingSurface.MouseLeftButtonDown += ( s, e ) =>
                                                      {
                                                          var p = e.GetPosition(drawingSurface);
                                                          mouseAbsX = (int)p.X;
                                                          mouseAbsY = (int)p.Y;
                                                          mouseCaptured = ((UIElement)s).CaptureMouse();
                                                          modifiers |= ModifierKeys.MouseButton0;
                                                      };

                drawingSurface.MouseLeftButtonUp += ( s, e ) =>
                                                    {
                                                        ((UIElement)s).ReleaseMouseCapture();
                                                        mouseCaptured = false;
                                                        modifiers &= ~ModifierKeys.MouseButton0;
                                                    };

                drawingSurface.MouseRightButtonDown += ( s, e ) => modifiers |= ModifierKeys.MouseButton1;

                drawingSurface.MouseRightButtonUp += ( s, e ) => modifiers &= ~ModifierKeys.MouseButton1;
            }

            // we are initialized
            isInitialized = true;

            // mouse starts off in the center
            mouseAbsX = window.Width >> 1;
            mouseAbsY = window.Height >> 1;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool IsKeyPressed( KeyCodes key )
        {
            return keys[ (int)key ];
        }

        /// <summary>
        ///    Returns true if the specified mouse button is currently down.
        /// </summary>
        /// <param name="button">Mouse button to query.</param>
        /// <returns>True if the mouse button is down, false otherwise.</returns>
        public override bool IsMousePressed( MouseButtons button )
        {
            return ( ( ( (int)modifiers ) << 3 ) & (int)button ) != 0;
        }

        /// <summary>
        ///     Called when the platform manager is shutting down.
        /// </summary>
        public override void Dispose()
        {
        }

        #endregion Methods

        #endregion InputReader Members

        #region Helper Methods

        ///// <summary>
        /////     Clear this class input buffers (those accesible to client through one of the public methods)
        ///// </summary>
        //private void ClearInput()
        //{
        //    keyboardState = null;
        //    mouseRelX = mouseRelY = mouseRelZ = 0;
        //    mouseButtons = 0;
        //}

        ///// <summary>
        /////     Capture buffered or unbuffered mouse and/or keyboard input.
        ///// </summary>
        //private void CaptureInput()
        //{

        //    if ( useKeyboard )
        //    {
        //        if ( useKeyboardEvents )
        //        {
        //            ReadBufferedKeyboardData();
        //        }
        //        else
        //        {
        //            // TODO Grab keyboard modifiers
        //            CaptureKeyboard();
        //        }
        //    }

        //    if ( useMouse )
        //    {
        //        if ( useMouseEvents )
        //        {
        //            //TODO: implement
        //        }
        //        else
        //        {
        //            CaptureMouse();
        //        }
        //    }
        //}

        ///// <summary>
        /////		Initializes the keyboard using either immediate mode or event based input.
        ///// </summary>
        //private void InitializeKeyboard()
        //{
        //    if ( dinput == null )
        //    {
        //        dinput = new DI.DirectInput();
        //    }

        //    if ( useKeyboardEvents )
        //    {
        //        InitializeBufferedKeyboard();
        //    }
        //    else
        //    {
        //        InitializeImmediateKeyboard();
        //    }
        //}

        ///// <summary>
        /////		Initializes the mouse using either immediate mode or event based input.
        ///// </summary>
        //private void InitializeMouse()
        //{
        //    if ( dinput == null )
        //    {
        //        dinput = new DI.DirectInput();
        //    }

        //    if ( useMouseEvents )
        //    {
        //        InitializeBufferedMouse();
        //    }
        //    else
        //    {
        //        InitializeImmediateMouse();
        //    }
        //}

        ///// <summary>
        /////		Initializes DirectInput for immediate input.
        ///// </summary>
        //private void InitializeImmediateKeyboard()
        //{
        //    // Create the device.
        //    keyboardDevice = new DI.Keyboard( dinput );

        //    // grab the keyboard non-exclusively
        //    keyboardDevice.SetCooperativeLevel( winHandle, DI.CooperativeLevel.Nonexclusive | DI.CooperativeLevel.Background );

        //    // Set the data format to the keyboard pre-defined format.
        //    //keyboardDevice.SetDataFormat( DI.DeviceDataFormat.Keyboard );

        //    try
        //    {
        //        keyboardDevice.Acquire();
        //    }
        //    catch
        //    {
        //        throw new Exception( "Unable to acquire a keyboard using DirectInput." );
        //    }
        //}

        ///// <summary>
        /////		Prepares DirectInput for non-immediate input capturing.
        ///// </summary>
        //private void InitializeBufferedKeyboard()
        //{
        //    // create the device
        //    keyboardDevice = new DI.Keyboard( dinput );

        //    // Set the data format to the keyboard pre-defined format.
        //    //keyboardDevice.SetDataFormat( DI.DeviceDataFormat.Keyboard );

        //    // grab the keyboard non-exclusively
        //    keyboardDevice.SetCooperativeLevel( winHandle, DI.CooperativeLevel.Nonexclusive | DI.CooperativeLevel.Background );

        //    // set the buffer size to use for input
        //    keyboardDevice.Properties.BufferSize = BufferSize;

        //    try
        //    {
        //        keyboardDevice.Acquire();
        //    }
        //    catch
        //    {
        //        throw new Exception( "Unable to acquire a keyboard using DirectInput." );
        //    }
        //}

        ///// <summary>
        /////		Prepares DirectInput for immediate mouse input.
        ///// </summary>
        //private void InitializeImmediateMouse()
        //{
        //    // create the device
        //    mouseDevice = new DI.Mouse( dinput );

        //    //mouseDevice.Properties.AxisModeAbsolute = true;
        //    mouseDevice.Properties.AxisMode = DI.DeviceAxisMode.Relative;

        //    // set the device format so DInput knows this device is a mouse
        //    //mouseDevice.SetDataFormat( DI.DeviceDataFormat.Mouse );

        //    // set cooperation level
        //    if ( ownMouse )
        //    {
        //        mouseDevice.SetCooperativeLevel( winHandle, DI.CooperativeLevel.Exclusive | DI.CooperativeLevel.Foreground );
        //    }
        //    else
        //    {
        //        mouseDevice.SetCooperativeLevel( winHandle, DI.CooperativeLevel.Nonexclusive | DI.CooperativeLevel.Background );
        //    }

        //    // note: dont acquire yet, wait till capture
        //}

        ///// <summary>
        /////
        ///// </summary>
        //private void InitializeBufferedMouse()
        //{
        //    throw new NotImplementedException();
        //}

        ///// <summary>
        /////		Reads buffered input data when in buffered mode.
        ///// </summary>
        //private void ReadBufferedKeyboardData()
        //{
        //    // grab the collection of buffered data
        //    IEnumerable<DI.KeyboardState> bufferedData = keyboardDevice.GetBufferedData();

        //    // please tell me why this would ever come back null, rather than an empty collection...
        //    if ( bufferedData == null )
        //    {
        //        return;
        //    }

        //    foreach ( DI.KeyboardState packet in bufferedData )
        //    {
        //        foreach ( DI.Key key in packet.PressedKeys )
        //        {
        //            KeyChanged( ConvertKeyEnum( key ), true );

        //        }
        //        foreach ( DI.Key key in packet.ReleasedKeys )
        //        {

        //            KeyChanged( ConvertKeyEnum( key ), false );
        //        }

        //    }
        //}

        ///// <summary>
        /////		Captures an immediate keyboard state snapshot (for non-buffered data).
        ///// </summary>
        //private void CaptureKeyboard()
        //{
        //    keyboardState = keyboardDevice.GetCurrentState();
        //}

        ///// <summary>
        /////		Captures the mouse input based on the preffered input mode.
        ///// </summary>
        //private void CaptureMouse()
        //{
        //    mouseDevice.Acquire();

        //    // determine whether to used immediate or buffered mouse input
        //    if ( useMouseEvents )
        //    {
        //        CaptureBufferedMouse();
        //    }
        //    else
        //    {
        //        CaptureImmediateMouse();
        //    }
        //}

        ///// <summary>
        /////		Checks the buffered mouse events.
        ///// </summary>
        //private void CaptureBufferedMouse()
        //{
        //    // TODO: Implement
        //}

        ///// <summary>
        /////		Takes a snapshot of the mouse state for immediate input checking.
        ///// </summary>
        //private void CaptureImmediateMouse()
        //{
        //    // capture the current mouse state
        //    mouseState = mouseDevice.GetCurrentState();


        //    // store the updated absolute values
        //    mouseAbsX = control.PointToClient( SWF.Cursor.Position ).X;
        //    mouseAbsY = control.PointToClient( SWF.Cursor.Position ).Y;
        //    mouseAbsZ += mouseState.Z;

        //    // calc relative deviance from center
        //    mouseRelX = mouseState.X;
        //    mouseRelY = mouseState.Y;
        //    mouseRelZ = mouseState.Z;

        //    bool[] buttons = mouseState.GetButtons();

        //    // clear the flags
        //    mouseButtons = 0;

        //    for ( int i = 0; i < buttons.Length; i++ )
        //    {
        //        if ( buttons[ i ] == true )
        //        {
        //            mouseButtons |= ( 1 << i );
        //        }
        //    }
        //}

        ///// <summary>
        /////		Verifies the state of the host window and reacquires input if the window was
        /////		previously minimized and has been brought back into focus.
        ///// </summary>
        ///// <returns>True if the input devices are acquired and input capturing can proceed, false otherwise.</returns>
        //protected bool VerifyInputAcquired()
        //{
        //    // if the window is coming back from being deactivated, lets grab input again
        //    if ( window.IsActive && !lastWindowActive )
        //    {
        //        // no exceptions right now, thanks anyway
        //        //DX.DirectXException.IgnoreExceptions();

        //        // acquire and capture keyboard input
        //        if ( useKeyboard )
        //        {
        //            keyboardDevice.Acquire();
        //            CaptureKeyboard();
        //        }

        //        // acquire and capture mouse input
        //        if ( useMouse )
        //        {
        //            mouseDevice.Acquire();
        //            CaptureMouse();
        //        }

        //        // wait...i like exceptions!
        //        //DX.DirectXException.EnableExceptions();
        //    }

        //    // store the current window state
        //    lastWindowActive = window.IsActive;

        //    return lastWindowActive;
        //}

        #region Keycode Conversions

        private static Key ConvertModifierKeysEnum( ModifierKeys key )
        {
            Key dinputKey = 0;
            switch ( key )
            {
                case ModifierKeys.Shift:
                    dinputKey = Key.Shift;
                    break;
                case ModifierKeys.Control:
                    dinputKey = Key.Ctrl;
                    break;
                case ModifierKeys.Alt:
                    dinputKey = Key.Alt;
                    break;
            }
            return dinputKey;
        }

        private static ModifierKeys ConvertModifierKeysEnum( Key key )
        {
            ModifierKeys axiomKey = 0;
            switch ( key )
            {
                case Key.Shift:
                    axiomKey = ModifierKeys.Shift;
                    break;
                case Key.Ctrl:
                    axiomKey = ModifierKeys.Control;
                    break;
                case Key.Alt:
                    axiomKey = ModifierKeys.Alt;
                    break;
            }
            return axiomKey;
        }

        /// <summary>
        /// Used to convert an Axiom.Input.KeyCodes enum val to a Key enum val.
        /// </summary>
        /// <param name="key">Axiom keyboard code to query.</param>
        /// <returns>
        /// The equivalent enum value in the Key enum.
        /// </returns>
        private static Key ConvertKeyEnum( KeyCodes key )
        {
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
                    dinputKey = Key.Up;
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
                    dinputKey = Key.Enter;
                    break;
                case KeyCodes.Tab:
                    dinputKey = Key.Tab;
                    break;
                case KeyCodes.LeftShift:
                case KeyCodes.RightShift:
                    dinputKey = Key.Shift;
                    break;
                case KeyCodes.LeftControl:
                case KeyCodes.RightControl:
                    dinputKey = Key.Ctrl;
                    break;
                //case KeyCodes.Period:
                //    dinputKey = Key.Period;
                //    break;
                //case KeyCodes.Comma:
                //    dinputKey = Key.Comma;
                //    break;
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
                //case KeyCodes.Semicolon:
                //    dinputKey = Key.Semicolon;
                //    break;
                case KeyCodes.Subtract:
                    dinputKey = Key.Subtract;
                    break;
                case KeyCodes.Add:
                    dinputKey = Key.Add;
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
                case KeyCodes.RightAlt:
                    dinputKey = Key.Alt;
                    break;
                case KeyCodes.Space:
                    dinputKey = Key.Space;
                    break;
                //case KeyCodes.Tilde:
                //    dinputKey = Key.Grave;
                //    break;
                //case KeyCodes.OpenBracket:
                //    dinputKey = Key.LeftBracket;
                //    break;
                //case KeyCodes.CloseBracket:
                //    dinputKey = Key.RightBracket;
                //    break;               
                //case KeyCodes.QuestionMark:
                //    dinputKey = Key.Slash;
                //    break;
                //case KeyCodes.Quotes:
                //    dinputKey = Key.Apostrophe;
                //    break;
                //case KeyCodes.Backslash:
                //    dinputKey = Key.Backslash;
                //    break;
                case KeyCodes.NumPad0:
                    dinputKey = Key.NumPad0;
                    break;
                case KeyCodes.NumPad1:
                    dinputKey = Key.NumPad1;
                    break;
                case KeyCodes.NumPad2:
                    dinputKey = Key.NumPad2;
                    break;
                case KeyCodes.NumPad3:
                    dinputKey = Key.NumPad3;
                    break;
                case KeyCodes.NumPad4:
                    dinputKey = Key.NumPad4;
                    break;
                case KeyCodes.NumPad5:
                    dinputKey = Key.NumPad5;
                    break;
                case KeyCodes.NumPad6:
                    dinputKey = Key.NumPad6;
                    break;
                case KeyCodes.NumPad7:
                    dinputKey = Key.NumPad7;
                    break;
                case KeyCodes.NumPad8:
                    dinputKey = Key.NumPad8;
                    break;
                case KeyCodes.NumPad9:
                    dinputKey = Key.NumPad9;
                    break;
            }

            return dinputKey;
        }

        /// <summary>
        /// Used to convert a Key enum val to a Axiom.Input.KeyCodes enum val.
        /// </summary>
        /// <param name="key">Key code to query.</param>
        /// <returns>
        /// The equivalent enum value in the Axiom.KeyCodes enum.
        /// </returns>
        private static KeyCodes ConvertKeyEnum( Key key )
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
                case Key.Up:
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
                case Key.Enter:
                    axiomKey = KeyCodes.Enter;
                    break;
                case Key.Tab:
                    axiomKey = KeyCodes.Tab;
                    break;
                case Key.Shift:
                    axiomKey = Axiom.Input.KeyCodes.RightShift;
                    axiomKey = Axiom.Input.KeyCodes.LeftShift;
                    break;
                case Key.Ctrl:
                    axiomKey = Axiom.Input.KeyCodes.RightControl;
                    axiomKey = Axiom.Input.KeyCodes.LeftControl;
                    break;
                //case Key.Period:
                //    axiomKey = KeyCodes.Period;
                //    break;
                //case Key.Comma:
                //    axiomKey = KeyCodes.Comma;
                //    break;
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
                //case Key.Semicolon:
                //    axiomKey = KeyCodes.Semicolon;
                //    break;
                case Key.Subtract:
                    axiomKey = KeyCodes.Subtract;
                    break;
                case Key.Add:
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
                case Key.Alt:
                    axiomKey = Axiom.Input.KeyCodes.RightAlt;
                    axiomKey = Axiom.Input.KeyCodes.LeftAlt;
                    break;
                case Key.Space:
                    axiomKey = KeyCodes.Space;
                    break;
                //case Key.Grave:
                //    axiomKey = KeyCodes.Tilde;
                //    break;
                //case Key.LeftBracket:
                //    axiomKey = KeyCodes.OpenBracket;
                //    break;
                //case Key.RightBracket:
                //    axiomKey = KeyCodes.CloseBracket;
                //    break;               
                //case Key.Slash:
                //    axiomKey = KeyCodes.QuestionMark;
                //    break;
                //case Key.Apostrophe:
                //    axiomKey = KeyCodes.Quotes;
                //    break;
                //case Key.Backslash:
                //    axiomKey = KeyCodes.Backslash;
                //    break;
                case Key.NumPad0:
                    axiomKey = KeyCodes.NumPad0;
                    break;
                case Key.NumPad1:
                    axiomKey = KeyCodes.NumPad1;
                    break;
                case Key.NumPad2:
                    axiomKey = KeyCodes.NumPad2;
                    break;
                case Key.NumPad3:
                    axiomKey = KeyCodes.NumPad3;
                    break;
                case Key.NumPad4:
                    axiomKey = KeyCodes.NumPad4;
                    break;
                case Key.NumPad5:
                    axiomKey = KeyCodes.NumPad5;
                    break;
                case Key.NumPad6:
                    axiomKey = KeyCodes.NumPad6;
                    break;
                case Key.NumPad7:
                    axiomKey = KeyCodes.NumPad7;
                    break;
                case Key.NumPad8:
                    axiomKey = KeyCodes.NumPad8;
                    break;
                case Key.NumPad9:
                    axiomKey = KeyCodes.NumPad9;
                    break;
            }

            return axiomKey;
        }

        #endregion Keycode Conversions

        #endregion Helper Methods
    }
}