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
using log4net;

#endregion Namespace Declarations

namespace SharpInputSystem
{
	class SdlKeyboard : Keyboard
	{
		private static readonly ILog log = LogManager.GetLogger( typeof( SdlKeyboard ) );

		protected const int _BUFFER_SIZE = 16;

		private Dictionary<int, KeyCode> _keyMap = new Dictionary<int, KeyCode>();

		private int[] _keyboardState = new int[ 256 ];


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffered">true for buffered keyboard input</param>
		public SdlKeyboard( InputManager creator, bool buffered )
		{
			Creator = creator;
			IsBuffered = buffered;
			Type = InputType.Keyboard;
			EventListener = null;
		}

		#region SharpInputSystem.Keyboard Implementation

		public override int[] KeyStates
		{
			get
			{
				return (int[])_keyboardState.Clone();
			}
		}

		public override bool IsKeyDown( KeyCode key )
		{
			return (_keyboardState[ (int)key ] == 1);
		}

		public override string AsString( KeyCode key )
		{
			return "";
		}

		public override void Capture()
		{
			// Get SDL Events off the stack
			Sdl.SDL_Event[] events = new Sdl.SDL_Event[ _BUFFER_SIZE ];
			Sdl.SDL_PumpEvents();
			int eventCount = Sdl.SDL_PeepEvents( events, _BUFFER_SIZE, Sdl.SDL_GETEVENT, ( Sdl.SDL_KEYDOWNMASK | Sdl.SDL_KEYUPMASK ) );

			// For each event 
			for ( int i = 0; i < eventCount; i++ )
			{
				KeyCode key = _keyMap[ events[ i ].key.keysym.sym ];

				// Store state for unbuffered handling
				_keyboardState[ (int)key ] = (int)events[ i ].key.state;

				// if using buffered input, fire event
				if ( IsBuffered && EventListener != null )
				{
					if ( events[ i ].type == Sdl.SDL_KEYDOWN )
					{
						if ( EventListener.KeyPressed( new KeyEventArgs( this, key, (int)events[ i ].key.keysym.unicode ) ) == false )
							break;
					}
					else
					{
						if ( EventListener.KeyReleased( new KeyEventArgs( this, key, (int)events[ i ].key.keysym.unicode ) ) == false )
							break;
					}
				}
			}

			// Release Grab Mode on Alt-Tab combinations
			if ( ( _keyboardState[ (int)KeyCode.Key_RMENU ] != 0 ) || ( _keyboardState[ (int)KeyCode.Key_LMENU ] != 0 ) )
				if ( _keyboardState[ (int)KeyCode.Key_TAB ] != 0 )
				{
					( (SdlInputManager)Creator ).GrabMode = false;
				}
		}

		internal override void initialize()
		{
			_keyMap.Add( Sdl.SDLK_ESCAPE, KeyCode.Key_ESCAPE );
			_keyMap.Add( Sdl.SDLK_1, KeyCode.Key_1 );
			_keyMap.Add( Sdl.SDLK_2, KeyCode.Key_2 );
			_keyMap.Add( Sdl.SDLK_3, KeyCode.Key_3 );
			_keyMap.Add( Sdl.SDLK_4, KeyCode.Key_4 );
			_keyMap.Add( Sdl.SDLK_5, KeyCode.Key_5 );
			_keyMap.Add( Sdl.SDLK_6, KeyCode.Key_6 );
			_keyMap.Add( Sdl.SDLK_7, KeyCode.Key_7 );
			_keyMap.Add( Sdl.SDLK_8, KeyCode.Key_8 );
			_keyMap.Add( Sdl.SDLK_9, KeyCode.Key_9 );
			_keyMap.Add( Sdl.SDLK_0, KeyCode.Key_0 );
			_keyMap.Add( Sdl.SDLK_MINUS, KeyCode.Key_MINUS );
			_keyMap.Add( Sdl.SDLK_EQUALS, KeyCode.Key_EQUALS );
			_keyMap.Add( Sdl.SDLK_BACKSPACE, KeyCode.Key_BACK );
			_keyMap.Add( Sdl.SDLK_TAB, KeyCode.Key_TAB );
			_keyMap.Add( Sdl.SDLK_q, KeyCode.Key_Q );
			_keyMap.Add( Sdl.SDLK_w, KeyCode.Key_W );
			_keyMap.Add( Sdl.SDLK_e, KeyCode.Key_E );
			_keyMap.Add( Sdl.SDLK_r, KeyCode.Key_R );
			_keyMap.Add( Sdl.SDLK_t, KeyCode.Key_T );
			_keyMap.Add( Sdl.SDLK_y, KeyCode.Key_Y );
			_keyMap.Add( Sdl.SDLK_u, KeyCode.Key_U );
			_keyMap.Add( Sdl.SDLK_i, KeyCode.Key_I );
			_keyMap.Add( Sdl.SDLK_o, KeyCode.Key_O );
			_keyMap.Add( Sdl.SDLK_p, KeyCode.Key_P );
			_keyMap.Add( Sdl.SDLK_RETURN, KeyCode.Key_RETURN );
			_keyMap.Add( Sdl.SDLK_LCTRL, KeyCode.Key_LCONTROL );
			_keyMap.Add( Sdl.SDLK_a, KeyCode.Key_A );
			_keyMap.Add( Sdl.SDLK_s, KeyCode.Key_S );
			_keyMap.Add( Sdl.SDLK_d, KeyCode.Key_D );
			_keyMap.Add( Sdl.SDLK_f, KeyCode.Key_F );
			_keyMap.Add( Sdl.SDLK_g, KeyCode.Key_G );
			_keyMap.Add( Sdl.SDLK_h, KeyCode.Key_H );
			_keyMap.Add( Sdl.SDLK_j, KeyCode.Key_J );
			_keyMap.Add( Sdl.SDLK_k, KeyCode.Key_K );
			_keyMap.Add( Sdl.SDLK_l, KeyCode.Key_L );
			_keyMap.Add( Sdl.SDLK_SEMICOLON, KeyCode.Key_SEMICOLON );
			_keyMap.Add( Sdl.SDLK_COLON, KeyCode.Key_COLON );
			_keyMap.Add( Sdl.SDLK_QUOTE, KeyCode.Key_APOSTROPHE );
			_keyMap.Add( Sdl.SDLK_BACKQUOTE, KeyCode.Key_GRAVE );
			_keyMap.Add( Sdl.SDLK_LSHIFT, KeyCode.Key_LSHIFT );
			_keyMap.Add( Sdl.SDLK_BACKSLASH, KeyCode.Key_BACKSLASH );
			_keyMap.Add( Sdl.SDLK_SLASH, KeyCode.Key_SLASH );
			_keyMap.Add( Sdl.SDLK_z, KeyCode.Key_Z );
			_keyMap.Add( Sdl.SDLK_x, KeyCode.Key_X );
			_keyMap.Add( Sdl.SDLK_c, KeyCode.Key_C );
			_keyMap.Add( Sdl.SDLK_v, KeyCode.Key_V );
			_keyMap.Add( Sdl.SDLK_b, KeyCode.Key_B );
			_keyMap.Add( Sdl.SDLK_n, KeyCode.Key_N );
			_keyMap.Add( Sdl.SDLK_m, KeyCode.Key_M );
			_keyMap.Add( Sdl.SDLK_COMMA, KeyCode.Key_COMMA );
			_keyMap.Add( Sdl.SDLK_PERIOD, KeyCode.Key_PERIOD );
			_keyMap.Add( Sdl.SDLK_RSHIFT, KeyCode.Key_RSHIFT );
			_keyMap.Add( Sdl.SDLK_KP_MULTIPLY, KeyCode.Key_MULTIPLY );
			_keyMap.Add( Sdl.SDLK_LALT, KeyCode.Key_LMENU );
			_keyMap.Add( Sdl.SDLK_SPACE, KeyCode.Key_SPACE );
			_keyMap.Add( Sdl.SDLK_CAPSLOCK, KeyCode.Key_CAPITAL );
			_keyMap.Add( Sdl.SDLK_F1, KeyCode.Key_F1 );
			_keyMap.Add( Sdl.SDLK_F2, KeyCode.Key_F2 );
			_keyMap.Add( Sdl.SDLK_F3, KeyCode.Key_F3 );
			_keyMap.Add( Sdl.SDLK_F4, KeyCode.Key_F4 );
			_keyMap.Add( Sdl.SDLK_F5, KeyCode.Key_F5 );
			_keyMap.Add( Sdl.SDLK_F6, KeyCode.Key_F6 );
			_keyMap.Add( Sdl.SDLK_F7, KeyCode.Key_F7 );
			_keyMap.Add( Sdl.SDLK_F8, KeyCode.Key_F8 );
			_keyMap.Add( Sdl.SDLK_F9, KeyCode.Key_F9 );
			_keyMap.Add( Sdl.SDLK_F10, KeyCode.Key_F10 );
			_keyMap.Add( Sdl.SDLK_NUMLOCK, KeyCode.Key_NUMLOCK );
			_keyMap.Add( Sdl.SDLK_SCROLLOCK, KeyCode.Key_SCROLL );
			_keyMap.Add( Sdl.SDLK_KP7, KeyCode.Key_NUMPAD7 );
			_keyMap.Add( Sdl.SDLK_KP8, KeyCode.Key_NUMPAD8 );
			_keyMap.Add( Sdl.SDLK_KP9, KeyCode.Key_NUMPAD9 );
			_keyMap.Add( Sdl.SDLK_KP_MINUS, KeyCode.Key_SUBTRACT );
			_keyMap.Add( Sdl.SDLK_KP4, KeyCode.Key_NUMPAD4 );
			_keyMap.Add( Sdl.SDLK_KP5, KeyCode.Key_NUMPAD5 );
			_keyMap.Add( Sdl.SDLK_KP6, KeyCode.Key_NUMPAD6 );
			_keyMap.Add( Sdl.SDLK_KP_PLUS, KeyCode.Key_ADD );
			_keyMap.Add( Sdl.SDLK_KP1, KeyCode.Key_NUMPAD1 );
			_keyMap.Add( Sdl.SDLK_KP2, KeyCode.Key_NUMPAD2 );
			_keyMap.Add( Sdl.SDLK_KP3, KeyCode.Key_NUMPAD3 );
			_keyMap.Add( Sdl.SDLK_KP0, KeyCode.Key_NUMPAD0 );
			_keyMap.Add( Sdl.SDLK_KP_PERIOD, KeyCode.Key_DECIMAL );
			_keyMap.Add( Sdl.SDLK_F11, KeyCode.Key_F11 );
			_keyMap.Add( Sdl.SDLK_F12, KeyCode.Key_F12 );
			_keyMap.Add( Sdl.SDLK_F13, KeyCode.Key_F13 );
			_keyMap.Add( Sdl.SDLK_F14, KeyCode.Key_F14 );
			_keyMap.Add( Sdl.SDLK_F15, KeyCode.Key_F15 );
			_keyMap.Add( Sdl.SDLK_KP_EQUALS, KeyCode.Key_NUMPADEQUALS );
			_keyMap.Add( Sdl.SDLK_KP_DIVIDE, KeyCode.Key_DIVIDE );
			_keyMap.Add( Sdl.SDLK_SYSREQ, KeyCode.Key_SYSRQ );
			_keyMap.Add( Sdl.SDLK_RALT, KeyCode.Key_RMENU );
			_keyMap.Add( Sdl.SDLK_HOME, KeyCode.Key_HOME );
			_keyMap.Add( Sdl.SDLK_UP, KeyCode.Key_UP );
			_keyMap.Add( Sdl.SDLK_PAGEUP, KeyCode.Key_PGUP );
			_keyMap.Add( Sdl.SDLK_LEFT, KeyCode.Key_LEFT );
			_keyMap.Add( Sdl.SDLK_RIGHT, KeyCode.Key_RIGHT );
			_keyMap.Add( Sdl.SDLK_END, KeyCode.Key_END );
			_keyMap.Add( Sdl.SDLK_DOWN, KeyCode.Key_DOWN );
			_keyMap.Add( Sdl.SDLK_PAGEDOWN, KeyCode.Key_PGDOWN );
			_keyMap.Add( Sdl.SDLK_INSERT, KeyCode.Key_INSERT );
			_keyMap.Add( Sdl.SDLK_DELETE, KeyCode.Key_DELETE );
			_keyMap.Add( Sdl.SDLK_LSUPER, KeyCode.Key_LWIN );
			_keyMap.Add( Sdl.SDLK_RSUPER, KeyCode.Key_RWIN );

			Sdl.SDL_EnableUNICODE( 1 );
		}

		#endregion SharpInputSystem.Keyboard Implementation
	}
}
