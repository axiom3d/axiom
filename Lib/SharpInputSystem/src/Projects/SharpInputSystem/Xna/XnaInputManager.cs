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

#endregion Namespace Declarations

namespace SharpInputSystem
{
    /// <summary>
    /// Xna 2.0 Input Manager Specialization
    /// </summary>
    class XnaInputManager : InputManager, InputObjectFactory
    {
        #region Fields and Properties

        private IntPtr _hwnd;
        private int _gamePadCount = 4;

        private bool _keyboardInUse = false;
        internal bool keyboardInUse
        {
            get
            {
                return _keyboardInUse;
            }
            set
            {
                _keyboardInUse = value;
            }
        }

        private bool _mouseInUse = false;
        internal bool mouseInUse
        {
            get
            {
                return _mouseInUse;
            }
            set
            {
                _mouseInUse = value;
            }
        }

        #endregion Fields and Properties

        #region Construction and Destruction

        internal XnaInputManager() : base()
        {
            RegisterFactory( this );
        }

        #endregion Construction and Destruction
        protected override void _initialize( ParameterList args )
        {
            // Find the first Parameter entry with WINDOW
            _hwnd = (IntPtr)args.Find(
                                      delegate( Parameter p )
                                      {
                                          return p.first.ToUpper() == "WINDOW";
                                      }
                                     ).second;

            _parseConfigSettings( args );
            _enumerateDevices();
        }

        private void _enumerateDevices()
        {
        }

        private void _parseConfigSettings( ParameterList args )
        {
        }


        #region InputObjectFactory Members

        IEnumerable<KeyValuePair<Type, string>> InputObjectFactory.FreeDevices
        {
            get
            {
                return new List<KeyValuePair<Type, string>>();
            }
        }

        int InputObjectFactory.DeviceCount<T>()
        {
            if ( typeof( T ) == typeof( Keyboard ) )
                return 1;
            if ( typeof( T ) == typeof( Mouse ) )
                return 1;
            if ( typeof( T ) == typeof( Joystick ) )
                return _gamePadCount;
            return 0;
        }

        public int FreeDeviceCount<T>() where T : InputObject
        {
            if ( typeof( T ) == typeof( Keyboard ) )
                return _keyboardInUse ? 0 : 1;
            if ( typeof( T ) == typeof( Mouse ) )
                return _mouseInUse ? 0 : 1;
            return 0;
        }

        public bool VendorExists<T>( string vendor ) where T : InputObject
        {
            if ( typeof( T ) == typeof( Keyboard ) || typeof( T ) == typeof( Mouse ) || vendor.ToLower() == InputSystemName.ToLower() )
            {
                return true;
            }

            return false;
        }

        InputObject InputObjectFactory.CreateInputObject<T>( InputManager creator, bool bufferMode, string vendor )
        {
            string typeName = this.InputSystemName + typeof( T ).Name;
            Type objectType = System.Reflection.Assembly.GetExecutingAssembly().GetType( "SharpInputSystem." + typeName );
            T obj;

            System.Reflection.BindingFlags bindingFlags = System.Reflection.BindingFlags.CreateInstance;

            obj = (T)objectType.InvokeMember( typeName,
                                              bindingFlags,
                                              null,
                                              null,
                                              new object[] { this, bufferMode } );
            return obj;
        }

        void InputObjectFactory.DestroyInputObject( InputObject obj )
        {
            obj.Dispose();
        }

        #endregion
    }
}
