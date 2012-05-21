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
//     <id value="$Id:"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	internal class Win32MessageHandling
	{
		#region P/Invoke Declarations

		private enum WindowMessage
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

		private enum ActivateState
		{
			InActive = 0,
			Active = 1,
			ClickActive = 2
		}

		private enum VirtualKeys
		{
			Shift = 0x10,
			Control = 0x11,
			Menu = 0x12
		}

		[StructLayout( LayoutKind.Sequential )]
		private struct Msg
		{
			public readonly IntPtr hWnd;
			public readonly uint Message;
			public readonly IntPtr wParam;
			public readonly IntPtr lParam;
			public readonly uint time;
			public readonly POINTAPI pt;
		}

		[StructLayout( LayoutKind.Sequential )]
		private struct POINTAPI
		{
			public readonly int x;
			public readonly int y;

			// Just to get rid of Warning CS0649.
			public POINTAPI( int x, int y )
			{
				this.x = x;
				this.y = y;
			}

			public static implicit operator System.Drawing.Point( POINTAPI p )
			{
				return new System.Drawing.Point( p.x, p.y );
			}

			public static implicit operator POINTAPI( System.Drawing.Point p )
			{
				return new POINTAPI( p.X, p.Y );
			}
		}

		/// <summary>
		///		PeekMessage option to remove the message from the queue after processing.
		/// </summary>
		private const int PM_REMOVE = 0x0001;

		private const string USER_DLL = "user32.dll";

		/// <summary>
		///	The PeekMessage function dispatches incoming sent messages, checks the thread message
		///	queue for a posted message, and retrieves the message (if any exist).
		/// </summary>
		/// <param name="msg">A <see cref="Msg"/> structure that receives message information.</param>
		/// <param name="handle"></param>
		/// <param name="msgFilterMin"></param>
		/// <param name="msgFilterMax"></param>
		/// <param name="removeMsg"></param>
		[DllImport( USER_DLL )]
		[return: MarshalAs( UnmanagedType.Bool )]
		private static extern bool PeekMessage( out Msg msg, IntPtr handle, uint msgFilterMin, uint msgFilterMax,
		                                        uint removeMsg );

		/// <summary>
		///	The TranslateMessage function translates virtual-key messages into character messages.
		/// </summary>
		/// <param name="msg">
		///	an MSG structure that contains message information retrieved from the calling thread's message queue
		///	by using the GetMessage or <see cref="PeekMessage"/> function.
		/// </param>
		[DllImport( USER_DLL )]
		[return: MarshalAs( UnmanagedType.Bool )]
		private static extern bool TranslateMessage( ref Msg msg );

		/// <summary>
		///	The DispatchMessage function dispatches a message to a window procedure.
		/// </summary>
		/// <param name="msg">A <see cref="Msg"/> structure containing the message.</param>
		[DllImport( USER_DLL )]
		private static extern IntPtr DispatchMessage( ref Msg msg );

		#endregion P/Invoke Declarations

		#region Construction and Destruction

		public Win32MessageHandling()
		{
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Internal winProc (RenderWindow's use this when creating the Win32 Window)
		/// </summary>
		public static bool WndProc( RenderWindow win, ref Message m )
		{
			switch ( (WindowMessage)m.Msg )
			{
				case WindowMessage.Activate:
				{
					bool active = ( (ActivateState)( m.WParam.ToInt32() & 0xFFFF ) ) != ActivateState.InActive;
					win.IsActive = active;
					WindowEventMonitor.Instance.WindowFocusChange( win, active );
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
					//win.WindowMovedOrResized();
					WindowEventMonitor.Instance.WindowMoved( win );
					break;
				case WindowMessage.Size:
					//log->logMessage("WM_SIZE");
					//win.WindowMovedOrResized();
					WindowEventMonitor.Instance.WindowResized( win );
					break;
				case WindowMessage.GetMinMaxInfo:
					// Prevent the window from going smaller than some minimum size
					//((MINMAXINFO*)lParam)->ptMinTrackSize.x = 100;
					//((MINMAXINFO*)lParam)->ptMinTrackSize.y = 100;
					break;
				case WindowMessage.Close:
					//log->logMessage("WM_CLOSE");
					WindowEventMonitor.Instance.WindowClosed( win );
					break;
			}
			return false;
		}

		public static void MessagePump()
		{
			Msg msg;

			// pump those events!
			while ( PeekMessage( out msg, IntPtr.Zero, 0, 0, PM_REMOVE ) )
			{
				TranslateMessage( ref msg );
				DispatchMessage( ref msg );
			}
		}

		#endregion Methods
	};
}