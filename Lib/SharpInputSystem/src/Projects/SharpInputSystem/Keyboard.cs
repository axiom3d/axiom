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

#endregion Namespace Declarations

namespace SharpInputSystem
{
    /// <summary>
    /// Keyboard scan codes
    /// </summary>
    public enum KeyCode
    {
		Key_UNASSIGNED  = 0x00,
		Key_ESCAPE      = 0x01,
		Key_1           = 0x02,
		Key_2           = 0x03,
		Key_3           = 0x04,
		Key_4           = 0x05,
		Key_5           = 0x06,
		Key_6           = 0x07,
		Key_7           = 0x08,
		Key_8           = 0x09,
		Key_9           = 0x0A,
		Key_0           = 0x0B,
		Key_MINUS       = 0x0C,    // - on main keyboard
		Key_EQUALS      = 0x0D,
		Key_BACK        = 0x0E,    // backspace
		Key_TAB         = 0x0F,
		Key_Q           = 0x10,
		Key_W           = 0x11,
		Key_E           = 0x12,
		Key_R           = 0x13,
		Key_T           = 0x14,
		Key_Y           = 0x15,
		Key_U           = 0x16,
		Key_I           = 0x17,
		Key_O           = 0x18,
		Key_P           = 0x19,
		Key_LBRACKET    = 0x1A,
		Key_RBRACKET    = 0x1B,
		Key_RETURN      = 0x1C,    // Enter on main keyboard
		Key_LCONTROL    = 0x1D,
		Key_A           = 0x1E,
		Key_S           = 0x1F,
		Key_D           = 0x20,
		Key_F           = 0x21,
		Key_G           = 0x22,
		Key_H           = 0x23,
		Key_J           = 0x24,
		Key_K           = 0x25,
		Key_L           = 0x26,
		Key_SEMICOLON   = 0x27,
		Key_APOSTROPHE  = 0x28,
		Key_GRAVE       = 0x29,    // accent
		Key_LSHIFT      = 0x2A,
		Key_BACKSLASH   = 0x2B,
		Key_Z           = 0x2C,
		Key_X           = 0x2D,
		Key_C           = 0x2E,
		Key_V           = 0x2F,
		Key_B           = 0x30,
		Key_N           = 0x31,
		Key_M           = 0x32,
		Key_COMMA       = 0x33,
		Key_PERIOD      = 0x34,    // . on main keyboard
		Key_SLASH       = 0x35,    // / on main keyboard
		Key_RSHIFT      = 0x36,
		Key_MULTIPLY    = 0x37,    // * on numeric keypad
		Key_LMENU       = 0x38,    // left Alt
		Key_SPACE       = 0x39,
		Key_CAPITAL     = 0x3A,
		Key_F1          = 0x3B,
		Key_F2          = 0x3C,
		Key_F3          = 0x3D,
		Key_F4          = 0x3E,
		Key_F5          = 0x3F,
		Key_F6          = 0x40,
		Key_F7          = 0x41,
		Key_F8          = 0x42,
		Key_F9          = 0x43,
		Key_F10         = 0x44,
		Key_NUMLOCK     = 0x45,
		Key_SCROLL      = 0x46,    // Scroll Lock
		Key_NUMPAD7     = 0x47,
		Key_NUMPAD8     = 0x48,
		Key_NUMPAD9     = 0x49,
		Key_SUBTRACT    = 0x4A,    // - on numeric keypad
		Key_NUMPAD4     = 0x4B,
		Key_NUMPAD5     = 0x4C,
		Key_NUMPAD6     = 0x4D,
		Key_ADD         = 0x4E,    // + on numeric keypad
		Key_NUMPAD1     = 0x4F,
		Key_NUMPAD2     = 0x50,
		Key_NUMPAD3     = 0x51,
		Key_NUMPAD0     = 0x52,
		Key_DECIMAL     = 0x53,    // . on numeric keypad
		Key_OEM_102     = 0x56,    // < > | on UK/Germany keyboards
		Key_F11         = 0x57,
		Key_F12         = 0x58,
		Key_F13         = 0x64,    //                     (NEC PC98)
		Key_F14         = 0x65,    //                     (NEC PC98)
		Key_F15         = 0x66,    //                     (NEC PC98)
		Key_KANA        = 0x70,    // (Japanese keyboard)
		Key_ABNT_C1     = 0x73,    // / ? on Portugese (Brazilian) keyboards
		Key_CONVERT     = 0x79,    // (Japanese keyboard)
		Key_NOCONVERT   = 0x7B,    // (Japanese keyboard)
		Key_YEN         = 0x7D,    // (Japanese keyboard)
		Key_ABNT_C2     = 0x7E,    // Numpad . on Portugese (Brazilian) keyboards
		Key_NUMPADEQUALS= 0x8D,    // = on numeric keypad (NEC PC98)
		Key_PREVTRACK   = 0x90,    // Previous Track (Key_CIRCUMFLEX on Japanese keyboard)
		Key_AT          = 0x91,    //                     (NEC PC98)
		Key_COLON       = 0x92,    //                     (NEC PC98)
		Key_UNDERLINE   = 0x93,    //                     (NEC PC98)
		Key_KANJI       = 0x94,    // (Japanese keyboard)
		Key_STOP        = 0x95,    //                     (NEC PC98)
		Key_AX          = 0x96,    //                     (Japan AX)
		Key_UNLABELED   = 0x97,    //                        (J3100)
		Key_NEXTTRACK   = 0x99,    // Next Track
		Key_NUMPADENTER = 0x9C,    // Enter on numeric keypad
		Key_RCONTROL    = 0x9D,
		Key_MUTE        = 0xA0,    // Mute
		Key_CALCULATOR  = 0xA1,    // Calculator
		Key_PLAYPAUSE   = 0xA2,    // Play / Pause
		Key_MEDIASTOP   = 0xA4,    // Media Stop
		Key_VOLUMEDOWN  = 0xAE,    // Volume -
		Key_VOLUMEUP    = 0xB0,    // Volume +
		Key_WEBHOME     = 0xB2,    // Web home
		Key_NUMPADCOMMA = 0xB3,    // , on numeric keypad (NEC PC98)
		Key_DIVIDE      = 0xB5,    // / on numeric keypad
		Key_SYSRQ       = 0xB7,
		Key_RMENU       = 0xB8,    // right Alt
		Key_PAUSE       = 0xC5,    // Pause
		Key_HOME        = 0xC7,    // Home on arrow keypad
		Key_UP          = 0xC8,    // UpArrow on arrow keypad
		Key_PGUP        = 0xC9,    // PgUp on arrow keypad
		Key_LEFT        = 0xCB,    // LeftArrow on arrow keypad
		Key_RIGHT       = 0xCD,    // RightArrow on arrow keypad
		Key_END         = 0xCF,    // End on arrow keypad
		Key_DOWN        = 0xD0,    // DownArrow on arrow keypad
		Key_PGDOWN      = 0xD1,    // PgDn on arrow keypad
		Key_INSERT      = 0xD2,    // Insert on arrow keypad
		Key_DELETE      = 0xD3,    // Delete on arrow keypad
		Key_LWIN        = 0xDB,    // Left Windows key
		Key_RWIN        = 0xDC,    // Right Windows key
		Key_APPS        = 0xDD,    // AppMenu key
		Key_POWER       = 0xDE,    // System Power
		Key_SLEEP       = 0xDF,    // System Sleep
		Key_WAKE        = 0xE3,    // System Wake
		Key_WEBSEARCH   = 0xE5,    // Web Search
		Key_WEBFAVORITES= 0xE6,    // Web Favorites
		Key_WEBREFRESH  = 0xE7,    // Web Refresh
		Key_WEBSTOP     = 0xE8,    // Web Stop
		Key_WEBFORWARD  = 0xE9,    // Web Forward
		Key_WEBBACK     = 0xEA,    // Web Back
		Key_MYCOMPUTER  = 0xEB,    // My Computer
		Key_MAIL        = 0xEC,    // Mail
		Key_MEDIASELECT = 0xED     // Media Select
    }

    /// <summary>
    /// Specialised for key events
    /// </summary>
    public class KeyEventArgs : InputObjectEventArgs
    {
        public KeyEventArgs( InputObject obj, KeyCode key, int text )
            : base( obj )
        {
            _key = key;
            _text = text;
        }

        private KeyCode _key;
        public KeyCode Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
            }
        }

        private int _text;
        public int Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
            }
        }
    }

    /// <summary>
    /// To recieve buffered keyboard input, implement this interfacein a class. 
    /// Then set the call back to your Keyboard instance with Keyboard.SetEventCallback
    /// </summary>
    public interface IKeyboardListener
    {
        bool KeyPressed( KeyEventArgs e );
        bool KeyReleased( KeyEventArgs e );
    }

    /// <summary>
    /// Keyboard base class. To be implemented by specific system (ie. DirectX Keyboard)
	///	This class is useful as you remain OS independent using this common interface.
    /// </summary>
    public abstract class Keyboard : InputObject
    {
        public enum TextTranslationMode
        {
            Off,
            Unicode,
            Ascii
        }

        [Flags()]
        public enum ShiftState
        {
            Shift = 0x00000001,
            Ctrl  = 0x00000010,
            Alt   = 0x00000100 
        }

        #region Fields and Properties

        protected ShiftState shiftState;
        
        public abstract int[] KeyStates
        {
            get;
        }

        #region EventListener Property
			
        private IKeyboardListener _listener;
        /// <summary>
        /// Resisters an object to recieve the Keyboard events
        /// </summary>
        public IKeyboardListener EventListener
        {
            get
            {
                return _listener;
            }
            set
            {
                _listener = value;
            }
        }

		#endregion EventListener Property
			
        #region TextMode Property

        private TextTranslationMode _textMode;
        public virtual TextTranslationMode TextMode
        {

            get
            {
                return _textMode;
            }
            set
            {
                _textMode = value;
            }
        }

        #endregion TextMode Property
			
        #endregion Fields and Properties

        public Keyboard()
        {
            _textMode = TextTranslationMode.Unicode;
        }

        #region Methods

        /// <summary>
        /// Checks to see if the key is pressed
        /// </summary>
        /// <param name="key">KeyCode to check</param>
        /// <returns>True if the key is pressed</returns>
        public abstract bool IsKeyDown( KeyCode key );

        /// <summary>
        /// Translates KeyCode to string representation.
		///	For example, Key_ENTER will be "Enter" - Locale
		///	specific of course.
        /// </summary>
        /// <param name="key">The KeyCode to convert.</param>
        /// <returns>The string as determined from the current locale.</returns>
        public abstract string AsString( KeyCode key );

        /// <summary>
        /// Checks the Shift Status for the specified keys
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public virtual bool IsShiftState( ShiftState state )
        {
            return ( shiftState & state ) != 0;
        }

        #endregion Methods
    }
}
