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

		public delegate void MessagePumpDelegate();
		public MessagePumpDelegate MessagePump;

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
			_listeners.Add( window, new List<WindowEventListener>() );
		}

        /// <summary>
        /// Called by RenderWindows upon destruction for Ogre generated windows. You are free to remove your
        /// external windows here too if needed.
        /// </summary>
        /// <param name="window">The RenderWindow to remove from list</param>
        public void UnregisterWindow( RenderWindow window )
        {
            _windows.Remove( window );
			_listeners[ window ].Clear();
			_listeners.Remove( window );
		}

		public void WindowFocusChange( RenderWindow win, bool hasFocus )
        {
			if ( _windows.Contains( win ) )
			{
				// Notify Window of focus change
				win.IsActive = hasFocus;

                // Notify listeners of focus change
                foreach ( WindowEventListener listener in _listeners[ win ] )
                {
                    listener.WindowFocusChange( win );
                }

                return;
            }
        }

		public void WindowMoved( RenderWindow win )
        {
			if ( _windows.Contains( win ) )
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

            //throw new Exception( "The method or operation is not implemented." );
        }

		public void WindowResized( RenderWindow win )
        {
			if ( _windows.Contains( win ) )
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


            //throw new Exception( "The method or operation is not implemented." );
        }

		public void WindowClosed( RenderWindow win )
        {
			if ( _windows.Contains( win ) )
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
