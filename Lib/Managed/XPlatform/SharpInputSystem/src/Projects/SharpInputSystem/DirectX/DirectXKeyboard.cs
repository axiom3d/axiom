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

#region tNamespace Declarations

using System;

using MDI = Microsoft.DirectX.DirectInput;
using log4net;

#endregion Namespace Declarations

namespace SharpInputSystem
{
    class DirectXKeyboard : Keyboard
    {
        #region Fields and Properties

		private static readonly ILog log = LogManager.GetLogger( typeof( DirectXKeyboard ) );

        private const int _BUFFER_SIZE = 17;

        private MDI.CooperativeLevelFlags _coopSettings;
        private MDI.Device _device;
        private MDI.Device _keyboard;
		private KeyboardInfo _kbInfo;

        private int[] _keyboardState = new int[256];
        
        #endregion Fields and Properties

        #region Construction and Destruction

        public DirectXKeyboard( InputManager creator, MDI.Device device, bool buffered, MDI.CooperativeLevelFlags coopSettings )
        {
            Creator = creator;
            _device = device;
            IsBuffered = buffered;
            _coopSettings = coopSettings;
            Type = InputType.Keyboard;
            EventListener = null;

			_kbInfo = (KeyboardInfo) ( (DirectXInputManager)Creator ).CaptureDevice<Keyboard>();

			if ( _kbInfo == null )
			{
				throw new Exception( "No devices match requested type." );
			}

			log.Debug( "DirectXKeyboard device created." );
        }

        protected override void _dispose( bool disposeManagedResources )
        {
            if ( !isDisposed )
            {
                if ( disposeManagedResources )
                {
                    // Dispose managed resources.
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
                try
                {
                    _keyboard.Unacquire();
                }
                catch ( Exception e )
                {
                }
                finally
                {
                    _keyboard.Dispose();
                    _keyboard = null;
                }
				
				( (DirectXInputManager)Creator ).ReleaseDevice<Keyboard>( _kbInfo );

				log.Debug( "DirectXKeyboard device disposed." );

            }
            isDisposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base._dispose( disposeManagedResources );
        }

        #endregion Construction and Destruction

        #region Methods

        private void _read()
        {
            MDI.KeyboardState state = _keyboard.GetCurrentKeyboardState();
            for ( int i = 0; i < 256; i++ )
                _keyboardState[ i ] = state[ (MDI.Key)i ] ? i : 0;

            //Set Shift, Ctrl, Alt
            shiftState = (ShiftState)0;
            if ( IsKeyDown( KeyCode.Key_LCONTROL ) || IsKeyDown( KeyCode.Key_RCONTROL ) )
                shiftState |= ShiftState.Ctrl;
            if ( IsKeyDown( KeyCode.Key_LSHIFT ) || IsKeyDown( KeyCode.Key_RSHIFT ) )
                shiftState |= ShiftState.Shift;
            if ( IsKeyDown( KeyCode.Key_LMENU ) || IsKeyDown( KeyCode.Key_RMENU ) )
                shiftState |= ShiftState.Alt;

        }

        private void _readBuffered()
        {
            // grab the collection of buffered data
            MDI.BufferedDataCollection bufferedData = _keyboard.GetBufferedData();

            // please tell me why this would ever come back null, rather than an empty collection...
            if ( bufferedData == null )
            {
                return;
            }

            for ( int i = 0; i < bufferedData.Count; i++ )
            {
                bool ret = true;

                MDI.BufferedData data = bufferedData[ i ];

                KeyCode key = (KeyCode)data.Offset;
        
                // is the key being pressed down, or released?
                bool down = ( data.ButtonPressedData == 1 );

                //Store result in our keyBuffer too
                _keyboardState[ (int)key ] = (int)data.Data;

                if ( ( data.Data & 0x80 ) == 0)
                {
                    if ( key == KeyCode.Key_RCONTROL || key == KeyCode.Key_LCONTROL )
                        shiftState |= ShiftState.Ctrl;
                    if ( key == KeyCode.Key_RMENU || key == KeyCode.Key_LMENU )
                        shiftState |= ShiftState.Alt;
                    if ( key == KeyCode.Key_RSHIFT || key == KeyCode.Key_LSHIFT )
                        shiftState |= ShiftState.Shift;
                }

                if ( this.EventListener != null )
                    if ( down )
                        ret = this.EventListener.KeyPressed( new KeyEventArgs( this, key, 0 ) );
                    else
                        ret = this.EventListener.KeyReleased( new KeyEventArgs( this, key, 0 ) );
                if ( ret == false )
                    break;
            }
        }

        #endregion Methods

        #region InputObject Implementation

        public override void Capture()
        {
            if ( this.IsBuffered )
                _readBuffered();
            else
                _read();
        }

        internal override void initialize()
        {
            _keyboard = new MDI.Device( MDI.SystemGuid.Keyboard );

            _keyboard.SetDataFormat( MDI.DeviceDataFormat.Keyboard );

            _keyboard.SetCooperativeLevel( null, _coopSettings );

            if ( IsBuffered )
            {
                _keyboard.Properties.BufferSize = _BUFFER_SIZE;
            }

            try
            {
                _keyboard.Acquire();
            }
            catch ( Exception e )
            {
                throw new Exception( "Failed to aquire keyboard using DirectInput.", e );
            }
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
            return _keyboard.Properties.GetKeyName( MDI.ParameterHow.ByOffset, (int)key );
        }

        public override bool IsShiftState( ShiftState state )
        {
            return base.IsShiftState( state );
        }
        #endregion Keyboard Implementation

    }
}
