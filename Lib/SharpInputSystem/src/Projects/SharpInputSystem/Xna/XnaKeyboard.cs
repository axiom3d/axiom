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

using Xna = Microsoft.Xna.Framework;
using XInput = Microsoft.Xna.Framework.Input;
using log4net;

#endregion Namespace Declarations

namespace SharpInputSystem
{
    class XnaKeyboard : Keyboard
    {
        #region Fields and Properties
        private static readonly ILog log = LogManager.GetLogger( typeof( XnaKeyboard ) );

        // Variables for XnaKeyboard
        private Dictionary<XInput.Keys, KeyCode> _keyMap = new Dictionary<XInput.Keys, KeyCode>();

        private int[] _keyboardState = new int[ 256 ];

        #endregion Fields and Properties

        #region Construction and Destruction

        public XnaKeyboard( InputManager creator, bool buffered )
        {
            Creator = creator;
            IsBuffered = buffered;
            Type = InputType.Keyboard;
            EventListener = null;
        }

        #endregion Construction and Destruction

        #region InputObject Implementation

        public override void Capture()
        {
            XInput.KeyboardState currentState = XInput.Keyboard.GetState();
            XInput.Keys[] pressedKeys = currentState.GetPressedKeys();

            bool keyReleased;
            //Process KeyRelease
            for ( int key = 0; key < _keyboardState.Length; key++ )
            {
                if ( _keyboardState[ key ] != 0 )
                {
                    keyReleased = true;       
                    for ( int pressed = 1; pressed < pressedKeys.Length; pressed++ )
                    {
                        if ( (KeyCode)key == _keyMap[pressedKeys[pressed]] )
                        {
                            keyReleased = currentState.IsKeyDown(pressedKeys[pressed]) ;
                        }
                    }
                    if ( keyReleased )
                    {
                        _keyboardState[ key ] = 0;
                        if ( IsBuffered && EventListener != null )
                        {
                            if ( EventListener.KeyReleased( new KeyEventArgs( this, (KeyCode)key, 0 ) ) == false )
                                break;
                        }
                    }
                }
            }

            //Process KeyDowns
            for ( int key = 1; key < pressedKeys.Length; key++ )
            {
                _keyboardState[ (int)_keyMap[ pressedKeys[ key ] ] ] = 1;
                if ( IsBuffered && EventListener != null )
                {
                    if ( EventListener.KeyPressed( new KeyEventArgs( this, (KeyCode)key, (int)pressedKeys[ key ] ) ) == false )
                        break;
                }
            }
        }

        internal override void initialize()
        {
            _keyMap.Add( XInput.Keys.D0, KeyCode.Key_0 );
            _keyMap.Add( XInput.Keys.D1, KeyCode.Key_1 );
            _keyMap.Add( XInput.Keys.D2, KeyCode.Key_2 );
            _keyMap.Add( XInput.Keys.D3, KeyCode.Key_3 );
            _keyMap.Add( XInput.Keys.D4, KeyCode.Key_4 );
            _keyMap.Add( XInput.Keys.D5, KeyCode.Key_5 );
            _keyMap.Add( XInput.Keys.D6, KeyCode.Key_6 );
            _keyMap.Add( XInput.Keys.D7, KeyCode.Key_7 );
            _keyMap.Add( XInput.Keys.D8, KeyCode.Key_8 );
            _keyMap.Add( XInput.Keys.D9, KeyCode.Key_9 );
            _keyMap.Add( XInput.Keys.A, KeyCode.Key_A );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_ABNT_C1 );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_ABNT_C2 );
            _keyMap.Add( XInput.Keys.Add, KeyCode.Key_ADD );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_APOSTROPHE );
            _keyMap.Add( XInput.Keys.Apps, KeyCode.Key_APPS );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_AT );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_AX );
            _keyMap.Add( XInput.Keys.B, KeyCode.Key_B );
            _keyMap.Add( XInput.Keys.Back, KeyCode.Key_BACK );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_BACKSLASH );
            _keyMap.Add( XInput.Keys.C, KeyCode.Key_C );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_CALCULATOR );
            _keyMap.Add( XInput.Keys.CapsLock, KeyCode.Key_CAPITAL );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_COLON );
            _keyMap.Add( XInput.Keys.OemComma, KeyCode.Key_COMMA );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_CONVERT );
            _keyMap.Add( XInput.Keys.D, KeyCode.Key_D );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_DECIMAL );
            _keyMap.Add( XInput.Keys.Delete, KeyCode.Key_DELETE );
            _keyMap.Add( XInput.Keys.Divide, KeyCode.Key_DIVIDE );
            _keyMap.Add( XInput.Keys.Down, KeyCode.Key_DOWN );
            _keyMap.Add( XInput.Keys.E, KeyCode.Key_E );
            _keyMap.Add( XInput.Keys.End, KeyCode.Key_END );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_EQUALS );
            _keyMap.Add( XInput.Keys.Escape, KeyCode.Key_ESCAPE );
            _keyMap.Add( XInput.Keys.F, KeyCode.Key_F );
            _keyMap.Add( XInput.Keys.F1, KeyCode.Key_F1 );
            _keyMap.Add( XInput.Keys.F10, KeyCode.Key_F10 );
            _keyMap.Add( XInput.Keys.F11, KeyCode.Key_F11 );
            _keyMap.Add( XInput.Keys.F12, KeyCode.Key_F12 );
            _keyMap.Add( XInput.Keys.F13, KeyCode.Key_F13 );
            _keyMap.Add( XInput.Keys.F14, KeyCode.Key_F14 );
            _keyMap.Add( XInput.Keys.F15, KeyCode.Key_F15 );
            _keyMap.Add( XInput.Keys.F16, KeyCode.Key_UNASSIGNED );
            _keyMap.Add( XInput.Keys.F17, KeyCode.Key_UNASSIGNED );
            _keyMap.Add( XInput.Keys.F18, KeyCode.Key_UNASSIGNED );
            _keyMap.Add( XInput.Keys.F19, KeyCode.Key_UNASSIGNED );
            _keyMap.Add( XInput.Keys.F2, KeyCode.Key_F2 );
            _keyMap.Add( XInput.Keys.F20, KeyCode.Key_UNASSIGNED );
            _keyMap.Add( XInput.Keys.F21, KeyCode.Key_UNASSIGNED );
            _keyMap.Add( XInput.Keys.F22, KeyCode.Key_UNASSIGNED );
            _keyMap.Add( XInput.Keys.F23, KeyCode.Key_UNASSIGNED );
            _keyMap.Add( XInput.Keys.F24, KeyCode.Key_UNASSIGNED );
            _keyMap.Add( XInput.Keys.F3, KeyCode.Key_F3 );
            _keyMap.Add( XInput.Keys.F4, KeyCode.Key_F4 );
            _keyMap.Add( XInput.Keys.F5, KeyCode.Key_F5 );
            _keyMap.Add( XInput.Keys.F6, KeyCode.Key_F6 );
            _keyMap.Add( XInput.Keys.F7, KeyCode.Key_F7 );
            _keyMap.Add( XInput.Keys.F8, KeyCode.Key_F8 );
            _keyMap.Add( XInput.Keys.F9, KeyCode.Key_F9 );
            _keyMap.Add( XInput.Keys.G, KeyCode.Key_G );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_GRAVE );
            _keyMap.Add( XInput.Keys.H, KeyCode.Key_H );
            _keyMap.Add( XInput.Keys.Home, KeyCode.Key_HOME );
            _keyMap.Add( XInput.Keys.I, KeyCode.Key_I );
            _keyMap.Add( XInput.Keys.Insert, KeyCode.Key_INSERT );
            _keyMap.Add( XInput.Keys.J, KeyCode.Key_J );
            _keyMap.Add( XInput.Keys.K, KeyCode.Key_K );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_KANA );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_KANJI );
            _keyMap.Add( XInput.Keys.L, KeyCode.Key_L );
            _keyMap.Add( XInput.Keys.OemOpenBrackets, KeyCode.Key_LBRACKET );
            _keyMap.Add( XInput.Keys.LeftControl, KeyCode.Key_LCONTROL );
            _keyMap.Add( XInput.Keys.Left, KeyCode.Key_LEFT );
            _keyMap.Add( XInput.Keys.LeftAlt, KeyCode.Key_LMENU );
            _keyMap.Add( XInput.Keys.LeftShift, KeyCode.Key_LSHIFT );
            _keyMap.Add( XInput.Keys.LeftWindows, KeyCode.Key_LWIN );
            _keyMap.Add( XInput.Keys.M, KeyCode.Key_M );
            _keyMap.Add( XInput.Keys.LaunchMail, KeyCode.Key_MAIL );
            _keyMap.Add( XInput.Keys.SelectMedia, KeyCode.Key_MEDIASELECT );
            _keyMap.Add( XInput.Keys.MediaStop, KeyCode.Key_MEDIASTOP );
            _keyMap.Add( XInput.Keys.OemMinus, KeyCode.Key_MINUS );
            _keyMap.Add( XInput.Keys.Multiply, KeyCode.Key_MULTIPLY );
            _keyMap.Add( XInput.Keys.VolumeMute, KeyCode.Key_MUTE );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_MYCOMPUTER );
            _keyMap.Add( XInput.Keys.N, KeyCode.Key_N );
            _keyMap.Add( XInput.Keys.MediaNextTrack, KeyCode.Key_NEXTTRACK );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_NOCONVERT );
            _keyMap.Add( XInput.Keys.NumLock, KeyCode.Key_NUMLOCK );
            _keyMap.Add( XInput.Keys.NumPad0, KeyCode.Key_NUMPAD0 );
            _keyMap.Add( XInput.Keys.NumPad1, KeyCode.Key_NUMPAD1 );
            _keyMap.Add( XInput.Keys.NumPad2, KeyCode.Key_NUMPAD2 );
            _keyMap.Add( XInput.Keys.NumPad3, KeyCode.Key_NUMPAD3 );
            _keyMap.Add( XInput.Keys.NumPad4, KeyCode.Key_NUMPAD4 );
            _keyMap.Add( XInput.Keys.NumPad5, KeyCode.Key_NUMPAD5 );
            _keyMap.Add( XInput.Keys.NumPad6, KeyCode.Key_NUMPAD6 );
            _keyMap.Add( XInput.Keys.NumPad7, KeyCode.Key_NUMPAD7 );
            _keyMap.Add( XInput.Keys.NumPad8, KeyCode.Key_NUMPAD8 );
            _keyMap.Add( XInput.Keys.NumPad9, KeyCode.Key_NUMPAD9 );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_NUMPADCOMMA );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_NUMPADENTER );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_NUMPADEQUALS );
            _keyMap.Add( XInput.Keys.O, KeyCode.Key_O );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_OEM_102 );
            _keyMap.Add( XInput.Keys.P, KeyCode.Key_P );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_PAUSE );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_PERIOD );
            _keyMap.Add( XInput.Keys.PageDown, KeyCode.Key_PGDOWN );
            _keyMap.Add( XInput.Keys.PageUp, KeyCode.Key_PGUP );
            _keyMap.Add( XInput.Keys.MediaPlayPause, KeyCode.Key_PLAYPAUSE );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_POWER );
            _keyMap.Add( XInput.Keys.MediaPreviousTrack, KeyCode.Key_PREVTRACK );
            _keyMap.Add( XInput.Keys.Q, KeyCode.Key_Q );
            _keyMap.Add( XInput.Keys.R, KeyCode.Key_R );
            _keyMap.Add( XInput.Keys.OemCloseBrackets, KeyCode.Key_RBRACKET );
            _keyMap.Add( XInput.Keys.RightControl, KeyCode.Key_RCONTROL );
            _keyMap.Add( XInput.Keys.Enter, KeyCode.Key_RETURN );
            _keyMap.Add( XInput.Keys.Right, KeyCode.Key_RIGHT );
            _keyMap.Add( XInput.Keys.RightAlt, KeyCode.Key_RMENU );
            _keyMap.Add( XInput.Keys.RightShift, KeyCode.Key_RSHIFT );
            _keyMap.Add( XInput.Keys.RightWindows, KeyCode.Key_RWIN );
            _keyMap.Add( XInput.Keys.S, KeyCode.Key_S );
            _keyMap.Add( XInput.Keys.Scroll, KeyCode.Key_SCROLL );
            _keyMap.Add( XInput.Keys.OemSemicolon, KeyCode.Key_SEMICOLON );
            _keyMap.Add( XInput.Keys.OemBackslash, KeyCode.Key_SLASH );
            _keyMap.Add( XInput.Keys.Sleep, KeyCode.Key_SLEEP );
            _keyMap.Add( XInput.Keys.Space, KeyCode.Key_SPACE );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_STOP );
            _keyMap.Add( XInput.Keys.Subtract, KeyCode.Key_SUBTRACT );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_SYSRQ );
            _keyMap.Add( XInput.Keys.T, KeyCode.Key_T );
            _keyMap.Add( XInput.Keys.Tab, KeyCode.Key_TAB );
            _keyMap.Add( XInput.Keys.U, KeyCode.Key_U );
            _keyMap.Add( XInput.Keys.None, KeyCode.Key_UNASSIGNED );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_UNDERLINE );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_UNLABELED );
            _keyMap.Add( XInput.Keys.Up, KeyCode.Key_UP );
            _keyMap.Add( XInput.Keys.V, KeyCode.Key_V );
            _keyMap.Add( XInput.Keys.VolumeDown, KeyCode.Key_VOLUMEDOWN );
            _keyMap.Add( XInput.Keys.VolumeUp, KeyCode.Key_VOLUMEUP );
            _keyMap.Add( XInput.Keys.W, KeyCode.Key_W );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_WAKE );
            _keyMap.Add( XInput.Keys.BrowserBack, KeyCode.Key_WEBBACK );
            _keyMap.Add( XInput.Keys.BrowserFavorites, KeyCode.Key_WEBFAVORITES );
            _keyMap.Add( XInput.Keys.BrowserForward, KeyCode.Key_WEBFORWARD );
            _keyMap.Add( XInput.Keys.BrowserHome, KeyCode.Key_WEBHOME );
            _keyMap.Add( XInput.Keys.BrowserRefresh, KeyCode.Key_WEBREFRESH );
            _keyMap.Add( XInput.Keys.BrowserSearch, KeyCode.Key_WEBSEARCH );
            _keyMap.Add( XInput.Keys.BrowserStop, KeyCode.Key_WEBSTOP );
            _keyMap.Add( XInput.Keys.X, KeyCode.Key_X );
            _keyMap.Add( XInput.Keys.Y, KeyCode.Key_Y );
            //_keyMap.Add( XInput.Keys, KeyCode.Key_YEN );
            _keyMap.Add( XInput.Keys.Z, KeyCode.Key_Z );
        }

        #endregion InputObject Implementation

        #region Keyboard Implementation

        public override int[] KeyStates
        {
            get
            {
                return (int[])_keyboardState.Clone();
            }
        }

        public override bool IsKeyDown( KeyCode key )
        {
            return ( ( _keyboardState[ (int)key ] ) != 0 );
        }

        public override string AsString( KeyCode key )
        {
            return "";
        }

        public override bool IsShiftState( ShiftState state )
        {
            return base.IsShiftState( state );
        }

        #endregion Keyboard Implementation
    }
}
