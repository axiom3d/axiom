#region LGPL License
/*
Sharp Input System Library
Copyright (C) 2007 Michael Cummings

The overall design, and a majority of the core code contained within 
this library is a derivative of the open source Open Input System ( OIS ) , 
which can be found at http://www.sourceforge.net/projects/wgois.  
Many thanks to the Phillip Castaneda for maintaining such a high quality project.

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
using System.Collections.Generic;
using System.Text;
using Tao.Sdl;

#endregion Namespace Declarations

namespace SharpInputSystem
{
	class SdlMouse : Mouse
	{
		#region Fields and Properties

		private const int _BUFFER_SIZE = 64;

		//Used for going from SDL Button to OIS button
		static MouseButtonID[] ButtonMask = new MouseButtonID[ 4 ] { MouseButtonID.Left, MouseButtonID.Left, MouseButtonID.Middle, MouseButtonID.Right };

		private bool _regainFocus;

		private bool _grabbed;
		protected bool IsGrabbed
		{
			get
			{
				return _grabbed;
			}
			set
			{
				Sdl.SDL_WM_GrabInput( value ? Sdl.SDL_GRAB_ON : Sdl.SDL_GRAB_OFF );
				_grabbed = value;
			}
		}

		protected bool IsVisible
		{
			set
			{
				Sdl.SDL_ShowCursor( value ? 1 : 0 );
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffered">true for buffered mouse input</param>
		public SdlMouse( InputManager creator, bool buffered )
		{
			Creator = creator;
			IsBuffered = buffered;
			Type = InputType.Mouse;
			EventListener = null;
		}

		protected override void _dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					IsGrabbed = false;
					IsVisible = true;
					( (SdlInputManager)Creator ).GrabMode = false;
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			isDisposed = true;

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base._dispose( disposeManagedResources );
		}

		#endregion Construction and Destruction

		#region SharpInputSystem.Mouse Implementation

		public override void Capture()
		{

			//Clear old relative values
			MouseState.X.Relative = MouseState.Y.Relative = MouseState.Z.Relative = 0;

			Sdl.SDL_Event[] events = new Sdl.SDL_Event[ _BUFFER_SIZE ];
			Sdl.SDL_PumpEvents();
			int count = Sdl.SDL_PeepEvents( events, _BUFFER_SIZE, Sdl.SDL_GETEVENT, Sdl.SDL_MOUSEEVENTMASK );

			bool mouseXYMoved = false;
			bool mouseZMoved = false;
			int sdlButton;

			for ( int i = 0; i < count; ++i )
			{
				switch ( events[ i ].type )
				{
					case Sdl.SDL_MOUSEMOTION:
						mouseXYMoved = true;
						break;

					case Sdl.SDL_MOUSEBUTTONDOWN:
						_regainFocus = true;
						sdlButton = events[ i ].button.button;
						if ( sdlButton <= Sdl.SDL_BUTTON_RIGHT )
						{	//Left, Right, or Middle
							MouseState.Buttons |= (int)ButtonMask[ sdlButton ];
							if ( IsBuffered && EventListener != null )
								if ( EventListener.MousePressed( new MouseEventArgs( this, MouseState ), ButtonMask[ sdlButton ] ) == false )
									return;
						}
						else
						{	//mouse Wheel
							mouseZMoved = true;
							if ( sdlButton == Sdl.SDL_BUTTON_WHEELUP )
								MouseState.Z.Relative += 120;
							else if ( sdlButton == Sdl.SDL_BUTTON_WHEELDOWN )
								MouseState.Z.Relative -= 120;
						}
						break;

					case Sdl.SDL_MOUSEBUTTONUP:
						sdlButton = events[ i ].button.button;
						if ( sdlButton <= Sdl.SDL_BUTTON_RIGHT )
						{	//Left, Right, or Middle
							MouseState.Buttons &= ~( (int)ButtonMask[ sdlButton ] );
							if ( IsBuffered && EventListener != null )
								if ( EventListener.MouseReleased( new MouseEventArgs( this, MouseState ), ButtonMask[ sdlButton ] ) == false )
									return;
						}
						break;
				}
			}

			//Handle X/Y axis move
			if ( mouseXYMoved )
			{
				int relX, relY, absX, absY;

				Sdl.SDL_GetMouseState( out absX, out absY );
				Sdl.SDL_GetRelativeMouseState( out relX, out relY );

				MouseState.X.Relative = relX;
				MouseState.Y.Relative = relY;
				MouseState.X.Absolute = absX;
				MouseState.Y.Absolute = absY;

				if ( IsBuffered && EventListener != null )
					EventListener.MouseMoved( new MouseEventArgs( this, MouseState ) );
			}

			//Handle Z Motion
			if ( mouseZMoved )
			{
				MouseState.Z.Absolute += MouseState.Z.Relative;
				if ( IsBuffered && EventListener != null )
					EventListener.MouseMoved( new MouseEventArgs( this, MouseState ) );
			}

			//Handle Alt-Tabbing
			SdlInputManager man = (SdlInputManager)Creator;
			if ( man.GrabMode == false )
			{
				if ( _regainFocus == false && IsGrabbed == true )
				{	//We had focus, but must release it now
					IsGrabbed = false;
					IsVisible = true;
				}
				else if ( _regainFocus == true && IsGrabbed == false )
				{	//We are gaining focus back (mouse clicked in window)
					IsGrabbed = true;
					IsVisible = false;
					man.GrabMode = true;	//Notify manager
				}
			}
		}

		internal override void initialize()
		{
			MouseState.Clear();
			_regainFocus = false;
			IsGrabbed = true;
			IsVisible = false;

			( (SdlInputManager)Creator ).GrabMode = true;
		}

		#endregion SharpInputSystem.Mouse Implementation
	}
}
