#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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

#region SVN Version Information
// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Graphics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

#endregion Namespace Declarations

namespace Axiom.Core
{
    public interface WindowEventListener
    {
        /// <summary>
        /// Window has moved position
        /// </summary>
        /// <param name="rw">The RenderWindow which created this event</param>
        void WindowMoved( RenderWindow rw );

        /// <summary>
        /// Window has resized
        /// </summary>
        /// <param name="rw">The RenderWindow which created this event</param>
        void WindowResized( RenderWindow rw );

        /// <summary>
        /// Window has closed
        /// </summary>
        /// <param name="rw">The RenderWindow which created this event</param>
        void WindowClosed( RenderWindow rw );

        /// <summary>
        /// Window lost/regained the focus
        /// </summary>
        /// <param name="rw">The RenderWindow which created this event</param>
        void WindowFocusChange( RenderWindow rw );
    }

    public class WindowEventMonitor : IDisposable // Singleton<WindowMonitor>
    {
        #region P/Invoke Declarations

        enum WindowMessage
        {
            Create = 0x0001,
            Destroy = 0x0002,
            Move = 0x0003,
            Size = 0x0005,
            Activate = 0x0006,
            Close = 0x0010,

            GetMinMaxInfo = 0x0024,
            SysKeyDown = 0x0104,
            SysKeyUp = 0x0105,
            EnterSizeMove = 0x0231,
            ExitSizeMove = 0x0232
        }

        enum ActivateState
        {
            InActive = 0,
            Active = 1,
            ClickActive = 2
        }

        enum VirtualKeys
        {
            Shift = 0x10,
            Control = 0x11,
            Menu = 0x12
        }

        struct Msg
        {
            public int hWnd;
            public int Message;
            public int wParam;
            public int lParam;
            public int time;
            public POINTAPI pt;
        }

        struct POINTAPI
        {
            public int x;
            public int y;

            // Just to get rid of Warning CS0649.
            public POINTAPI( int x, int y )
            {
                this.x = x;
                this.y = y;
            }
        }

        /// <summary>
        ///		PeekMessage option to remove the message from the queue after processing.
        /// </summary>
        const int PM_REMOVE = 0x0001;
        const string USER_DLL = "user32.dll";

        /// <summary>
        ///		The PeekMessage function dispatches incoming sent messages, checks the thread message 
        ///		queue for a posted message, and retrieves the message (if any exist).
        /// </summary>
        /// <param name="msg">A <see cref="Msg"/> structure that receives message information.</param>
        /// <param name="handle"></param>
        /// <param name="msgFilterMin"></param>
        /// <param name="msgFilterMax"></param>
        /// <param name="removeMsg"></param>
        [DllImport( USER_DLL )]
        private static extern int PeekMessage( out Msg msg, IntPtr handle, int msgFilterMin, int msgFilterMax, int removeMsg );

        /// <summary>
        ///		The TranslateMessage function translates virtual-key messages into character messages.
        /// </summary>
        /// <param name="msg">
        ///		an MSG structure that contains message information retrieved from the calling thread's message queue 
        ///		by using the GetMessage or <see cref="PeekMessage"/> function.
        /// </param>
        [DllImport( USER_DLL )]
        private static extern void TranslateMessage( ref Msg msg );

        /// <summary>
        ///		The DispatchMessage function dispatches a message to a window procedure.
        /// </summary>
        /// <param name="msg">A <see cref="Msg"/> structure containing the message.</param>
        [DllImport( USER_DLL )]
        private static extern void DispatchMessage( ref Msg msg );

        #endregion P/Invoke Declarations

        private Dictionary<RenderWindow, List<WindowEventListener>> _listeners = new Dictionary<RenderWindow, List<WindowEventListener>>();
        private List<RenderWindow> _windows = new List<RenderWindow>();

        private WindowEventMonitor()
        {
        }

        private static readonly WindowEventMonitor _instance = new WindowEventMonitor();
        public static WindowEventMonitor Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Add a listener to listen to renderwindow events (multiple listener's per renderwindow is fine)
        /// The same listener can listen to multiple windows, as the Window Pointer is sent along with
        /// any messages.
        /// </summary>
        /// <param name="window">The RenderWindow you are interested in monitoring</param>
        /// <param name="listener">Your callback listener</param>
        public void RegisterListener( RenderWindow window, WindowEventListener listener )
        {
            if ( !_listeners.ContainsKey( window ) )
            {
                _listeners.Add( window, new List<WindowEventListener>() );
            }
            _listeners[ window ].Add( listener );
        }

        /// <summary>
        /// Remove previously added listener
        /// </summary>
        /// <param name="window">The RenderWindow you registered with</param>
        /// <param name="listener">The listener registered</param>
        public void UnregisterListener( RenderWindow window, WindowEventListener listener )
        {
            if ( _listeners.ContainsKey( window ) )
            {
                _listeners[ window ].Remove( listener );
            }
        }

        /// <summary>
        /// Called by RenderWindows upon creation for Ogre generated windows. You are free to add your
        /// external windows here too if needed.
        /// </summary>
        /// <param name="window">The RenderWindow to monitor</param>
        public void RegisterWindow( RenderWindow window )
        {
            _windows.Add( window );
            _attachEventHandlers( window );
        }

        /// <summary>
        /// Called by RenderWindows upon destruction for Ogre generated windows. You are free to remove your
        /// external windows here too if needed.
        /// </summary>
        /// <param name="window">The RenderWindow to remove from list</param>
        public void UnregisterWindow( RenderWindow window )
        {
            _windows.Remove( window );
            _detachEventHandlers( window );
        }

        /// <summary>
        /// Internal winProc (RenderWindow's use this when creating the Win32 Window)
        /// </summary>
        /// <param name="m"></param>
        public bool WinProc( Form frm, RenderWindow win, ref Message m )
        {
            //Iterator of all listeners registered to this RenderWindow
            List<WindowEventListener> winListeners = _listeners[ win ];

            switch ( (WindowMessage)m.Msg )
            {
                case WindowMessage.Activate:
                    {
                        bool active = ( (ActivateState)m.WParam ) != ActivateState.InActive;
                        win.IsActive = active;
                        foreach ( WindowEventListener listener in winListeners )
                        {
                            listener.WindowFocusChange( win );
                        }
                        break;
                    }
                case WindowMessage.SysKeyDown:
                    switch ( (VirtualKeys)m.WParam )
                    {
                        case VirtualKeys.Control:
                        case VirtualKeys.Shift:
                        case VirtualKeys.Menu: //ALT
                            //return true to bypass defProc and signal we processed the message
                            return true;
                    }
                    break;
                case WindowMessage.SysKeyUp:
                    switch ( (VirtualKeys)m.WParam )
                    {
                        case VirtualKeys.Control:
                        case VirtualKeys.Shift:
                        case VirtualKeys.Menu: //ALT
                            //return true to bypass defProc and signal we processed the message
                            return true;
                    }
                    break;
                case WindowMessage.EnterSizeMove:
                    //log->logMessage("WM_ENTERSIZEMOVE");
                    break;
                case WindowMessage.ExitSizeMove:
                    //log->logMessage("WM_EXITSIZEMOVE");
                    break;
                case WindowMessage.Move:
                    //log->logMessage("WM_MOVE");
                    win.WindowMovedOrResized();
                    foreach ( WindowEventListener listener in winListeners )
                    {
                        listener.WindowMoved( win );
                    }
                    break;
                case WindowMessage.Size:
                    //log->logMessage("WM_SIZE");
                    win.WindowMovedOrResized();
                    foreach ( WindowEventListener listener in winListeners )
                    {
                        listener.WindowResized( win );
                    }
                    break;
                case WindowMessage.GetMinMaxInfo:
                    // Prevent the window from going smaller than some minimum size
                    //((MINMAXINFO*)lParam)->ptMinTrackSize.x = 100;
                    //((MINMAXINFO*)lParam)->ptMinTrackSize.y = 100;
                    break;
                case WindowMessage.Close:
                    //log->logMessage("WM_CLOSE");
                    win.Dispose();
                    foreach ( WindowEventListener listener in winListeners )
                    {
                        listener.WindowClosed( win );
                    }
                    return true;
            }
            return false;
        }

        public void MessagePump()
        {
            Msg msg;

            // pump those events!
            while ( !( PeekMessage( out msg, IntPtr.Zero, 0, 0, PM_REMOVE ) == 0 ) )
            {
                TranslateMessage( ref msg );
                DispatchMessage( ref msg );
            }
        }

        private void _attachEventHandlers( RenderWindow window )
        {
            System.Windows.Forms.Control ctrl = (System.Windows.Forms.Control)window[ "WINDOW" ];

            ctrl.Resize += new EventHandler( _windowResize );
            ctrl.Move += new EventHandler( _windowMove );
            ctrl.ClientSizeChanged += new EventHandler( _windowResize );
            ctrl.GotFocus += new EventHandler( _windowFocus );
            ctrl.LostFocus += new EventHandler( _windowFocus );
            ctrl.Disposed += new EventHandler( _windowClose );

            _listeners.Add( window, new List<WindowEventListener>() );
        }

        private void _detachEventHandlers( RenderWindow window )
        {
            System.Windows.Forms.Control ctrl = (System.Windows.Forms.Control)window[ "WINDOW" ];

            ctrl.Resize -= new EventHandler( _windowResize );
            ctrl.Move -= new EventHandler( _windowMove );
            ctrl.ClientSizeChanged -= new EventHandler( _windowResize );
            ctrl.GotFocus -= new EventHandler( _windowFocus );
            ctrl.LostFocus -= new EventHandler( _windowFocus );
            ctrl.Disposed -= new EventHandler( _windowClose );

            _listeners[ window ].Clear();
            _listeners.Remove( window );
        }

        private void _windowFocus( object sender, EventArgs e )
        {
            foreach ( RenderWindow win in _windows )
            {
                if ( IntPtr.ReferenceEquals( win[ "WINDOW" ], sender ) )
                {
                    // Notify Window of focus change
                    //bool active = ( (ActivateState)e.WParam ) != ActivateState.InActive;
                    //win.IsActive = active;

                    // Notify listeners of focus change
                    foreach ( WindowEventListener listener in _listeners[ win ] )
                    {
                        listener.WindowFocusChange( win );
                    }

                    return;
                }
            }

            //throw new Exception( "The method or operation is not implemented." );
        }

        private void _windowMove( object sender, EventArgs e )
        {
            foreach ( RenderWindow win in _windows )
            {
                if ( IntPtr.ReferenceEquals( win[ "WINDOW" ], sender ) )
                {
                    // Notify Window of Move or Resize
                    win.WindowMovedOrResized();

                    // Notify listeners of Resize
                    foreach ( WindowEventListener listener in _listeners[ win ] )
                    {
                        listener.WindowMoved( win );
                    }
                    return;
                }
            }

            //throw new Exception( "The method or operation is not implemented." );
        }

        private void _windowResize( object sender, EventArgs e )
        {
            foreach ( RenderWindow win in _windows )
            {
                if ( IntPtr.ReferenceEquals( win[ "WINDOW" ], sender ) )
                {
                    // Notify Window of Move or Resize
                    win.WindowMovedOrResized();

                    // Notify listeners of Resize
                    foreach ( WindowEventListener listener in _listeners[ win ] )
                    {
                        listener.WindowResized( win );
                    }
                    return;
                }
            }


            //throw new Exception( "The method or operation is not implemented." );
        }

        private void _windowClose( object sender, EventArgs e )
        {
            foreach ( RenderWindow win in _windows )
            {
                if ( IntPtr.ReferenceEquals( win[ "WINDOW" ], sender ) )
                {
                    // Notify Window of closure
                    win.Dispose();

                    // Notify listeners of close
                    foreach ( WindowEventListener listener in _listeners[ win ] )
                    {
                        listener.WindowClosed( win );
                    }
                    return;
                }
            }

            //throw new Exception( "The method or operation is not implemented." );
        }

        #region Singleton<WindowManager> Members

        public void Dispose()
        {
            foreach ( List<WindowEventListener> list in _listeners.Values )
            {
                list.Clear();
            }
            _listeners.Clear();

            _windows.Clear();

            //base.Dispose();
        }
        #endregion
    }
}
