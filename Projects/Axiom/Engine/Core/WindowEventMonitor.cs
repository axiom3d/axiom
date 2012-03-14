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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id: WindowEventMonitor.cs 1617 2009-06-02 20:04:03Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;

using Axiom.Graphics;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.Core
{
	public interface IWindowEventListener
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

	public class WindowEventMonitor : DisposableObject // Singleton<WindowMonitor>
	{
		#region Delegates

		/// <summary>
		///
		/// </summary>
		public delegate void MessagePumpDelegate();

		#endregion

		private static readonly WindowEventMonitor _instance = new WindowEventMonitor();
		public MessagePumpDelegate MessagePump;

		private Dictionary<RenderWindow, List<IWindowEventListener>> _listeners = new Dictionary<RenderWindow, List<IWindowEventListener>>();
		private List<RenderWindow> _windows = new List<RenderWindow>();

		private WindowEventMonitor() { }

		public IEnumerable<RenderWindow> Windows
		{
			get
			{
				return this._windows;
			}
		}

		/// <summary>
		/// Singleton Instance of the class
		/// </summary>
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
		public void RegisterListener( RenderWindow window, IWindowEventListener listener )
		{
			Contract.RequiresNotNull( window, "window" );
			Contract.RequiresNotNull( listener, "listener" );

			if ( !this._listeners.ContainsKey( window ) )
			{
				this._listeners.Add( window, new List<IWindowEventListener>() );
			}
			this._listeners[ window ].Add( listener );
		}

		/// <summary>
		/// Remove previously added listener
		/// </summary>
		/// <param name="window">The RenderWindow you registered with</param>
		/// <param name="listener">The listener registered</param>
		public void UnregisterListener( RenderWindow window, IWindowEventListener listener )
		{
			Contract.RequiresNotNull( window, "window" );
			Contract.RequiresNotNull( listener, "listener" );

			if ( this._listeners.ContainsKey( window ) )
			{
				this._listeners[ window ].Remove( listener );
			}
		}

		/// <summary>
		/// Called by RenderWindows upon creation for Ogre generated windows. You are free to add your
		/// external windows here too if needed.
		/// </summary>
		/// <param name="window">The RenderWindow to monitor</param>
		public void RegisterWindow( RenderWindow window )
		{
			Contract.RequiresNotNull( window, "window" );

			this._windows.Add( window );
			this._listeners.Add( window, new List<IWindowEventListener>() );
		}

		/// <summary>
		/// Called by RenderWindows upon destruction for Ogre generated windows. You are free to remove your
		/// external windows here too if needed.
		/// </summary>
		/// <param name="window">The RenderWindow to remove from list</param>
		public void UnregisterWindow( RenderWindow window )
		{
			Contract.RequiresNotNull( window, "window" );

			if ( this._windows.Contains( window ) )
			{
				this._windows.Remove( window );
			}

			if ( this._listeners.ContainsKey( window ) )
			{
				this._listeners[ window ].Clear();
				this._listeners.Remove( window );
			}
		}

		/// <summary>
		/// Window has either gained or lost the focus
		/// </summary>
		/// <param name="window">RenderWindow that caused the event</param>
		/// <param name="hasFocus">True if window has focus</param>
		public void WindowFocusChange( RenderWindow window, bool hasFocus )
		{
			Contract.RequiresNotNull( window, "window" );

			if ( this._windows.Contains( window ) )
			{
				// Notify Window of focus change
				window.IsActive = hasFocus;

				// Notify listeners of focus change
				foreach ( IWindowEventListener listener in this._listeners[ window ] )
				{
					listener.WindowFocusChange( window );
				}

				return;
			}
		}

		/// <summary>
		/// Window has moved position
		/// </summary>
		/// <param name="window">RenderWindow that caused the event</param>
		public void WindowMoved( RenderWindow window )
		{
			Contract.RequiresNotNull( window, "window" );

			if ( this._windows.Contains( window ) )
			{
				// Notify Window of Move or Resize
				window.WindowMovedOrResized();

				// Notify listeners of Resize
				foreach ( IWindowEventListener listener in this._listeners[ window ] )
				{
					listener.WindowMoved( window );
				}
				return;
			}
		}

		/// <summary>
		/// Window has changed size
		/// </summary>
		/// <param name="window">RenderWindow that caused the event</param>
		public void WindowResized( RenderWindow window )
		{
			Contract.RequiresNotNull( window, "window" );

			if ( this._windows.Contains( window ) )
			{
				// Notify Window of Move or Resize
				window.WindowMovedOrResized();

				// Notify listeners of Resize
				foreach ( IWindowEventListener listener in this._listeners[ window ] )
				{
					listener.WindowResized( window );
				}
				return;
			}
		}

		/// <summary>
		/// Window has closed
		/// </summary>
		/// <param name="window">RenderWindow that caused the event</param>
		public void WindowClosed( RenderWindow window )
		{
			Contract.RequiresNotNull( window, "window" );

			if ( this._windows.Contains( window ) )
			{
				// Notify listeners of close
				foreach ( IWindowEventListener listener in this._listeners[ window ] )
				{
					listener.WindowClosed( window );
				}

				// Notify Window of closure
				window.Dispose();

				return;
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( this._listeners != null )
					{
						foreach ( var list in this._listeners.Values )
						{
							list.Clear();
						}
						this._listeners.Clear();
						this._listeners = null;
					}

					if ( this._windows != null )
					{
						this._windows.Clear();
						this._windows = null;
					}
				}
			}

			base.dispose( disposeManagedResources );
		}
	}
}
