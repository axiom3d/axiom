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
using System.Drawing;
using SWF = System.Windows.Forms;

using MDI = Microsoft.DirectX.DirectInput;
using log4net;

#endregion Namespace Declarations

namespace SharpInputSystem
{
    class DirectXMouse : Mouse
    {
        #region Fields and Properties

		private static readonly ILog log = LogManager.GetLogger( typeof( DirectXMouse ) );

        private const int _BUFFER_SIZE = 64;

        private MDI.CooperativeLevelFlags _coopSettings;
        private MDI.Device _device;
        private MDI.Device _mouse;
		private MouseInfo _msInfo;

        private SWF.Control _window;

        #endregion Fields and Properties

        #region Construction and Destruction

        public DirectXMouse( InputManager creator, MDI.Device device, bool buffered, MDI.CooperativeLevelFlags coopSettings )
        {
            Creator = creator;
            _device = device;
            IsBuffered = buffered;
            _coopSettings = coopSettings;
            Type = InputType.Mouse;
            EventListener = null;

			_msInfo = (MouseInfo)( (DirectXInputManager)Creator ).CaptureDevice<Mouse>();

			if ( _msInfo == null )
			{
				throw new Exception( "No devices match requested type." );
			}

			log.Debug( "DirectXMouse device created." );

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
                if ( _mouse != null )
                {
                    try
                    {
                        _mouse.Unacquire();
                    }
                    catch ( Exception e )
                    {
                    }
                    finally
                    {
                        _mouse.Dispose();
                        _mouse = null;
                    }
                }

				( (DirectXInputManager)Creator ).ReleaseDevice<Mouse>( _msInfo );

				log.Debug( "DirectXMouse device disposed." );
            }
            isDisposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base._dispose( disposeManagedResources );
        }

        #endregion Construction and Destruction

        #region Methods

        private bool _doMouseClick( int mouseButton, MDI.BufferedData bufferedData )
        {
            if ( ( bufferedData.Data & 0x80 ) != 0 )
            {
                MouseState.Buttons |= 1 << mouseButton; //turn the bit flag on
                if ( EventListener != null && IsBuffered )
                    return EventListener.MousePressed( new MouseEventArgs( this, MouseState ), (MouseButtonID)mouseButton );
            }
            else
            {
                MouseState.Buttons &= ~( 1 << mouseButton ); //turn the bit flag off
                if ( EventListener != null && IsBuffered )
                    return EventListener.MouseReleased( new MouseEventArgs( this, MouseState ), (MouseButtonID)mouseButton );
            }

            return true;
        }

        #endregion Methods

        #region Mouse Implementation

        public override void Capture()
        {
			// Clear Relative movement
			MouseState.X.Relative = MouseState.Y.Relative = MouseState.Z.Relative = 0;

            MDI.BufferedDataCollection bufferedData = _mouse.GetBufferedData();
            if ( bufferedData == null )
            {
                try
                {
                    _mouse.Acquire();
                    bufferedData = _mouse.GetBufferedData();

                    if ( bufferedData == null )
                        return;
                }
                catch ( Exception e )
                {
                    return;
                }
            }

	        bool axesMoved = false;

	        //Accumulate all axis movements for one axesMove message..
	        //Buttons are fired off as they are found
            for ( int i = 0; i < bufferedData.Count; i++ )
            {
                switch ( (MDI.MouseOffset)bufferedData[ i ].Offset )
                {
                    case MDI.MouseOffset.Button0:
                        if ( !_doMouseClick( 0, bufferedData[ i ] ) )
                            return;
                        break;
                    case MDI.MouseOffset.Button1:
                        if ( !_doMouseClick( 1, bufferedData[ i ] ) )
                            return;
                        break;
                    case MDI.MouseOffset.Button2:
                        if ( !_doMouseClick( 2, bufferedData[ i ] ) )
                            return;
                        break;
                    case MDI.MouseOffset.Button3:
                        if ( !_doMouseClick( 3, bufferedData[ i ] ) )
                            return;
                        break;
                    case MDI.MouseOffset.Button4:
                        if ( !_doMouseClick( 4, bufferedData[ i ] ) )
                            return;
                        break;
                    case MDI.MouseOffset.Button5:
                        if ( !_doMouseClick( 5, bufferedData[ i ] ) )
                            return;
                        break;
                    case MDI.MouseOffset.Button6:
                        if ( !_doMouseClick( 6, bufferedData[ i ] ) )
                            return;
                        break;
                    case MDI.MouseOffset.Button7:
                        if ( !_doMouseClick( 7, bufferedData[ i ] ) )
                            return;
                        break;
                    case MDI.MouseOffset.X:
                        MouseState.X.Relative = bufferedData[ i ].Data;
                        axesMoved = true;
                        break;
                    case MDI.MouseOffset.Y:
                        MouseState.Y.Relative = bufferedData[ i ].Data;
                        axesMoved = true;
                        break;
                    case MDI.MouseOffset.Z:
                        MouseState.Z.Relative = bufferedData[ i ].Data;
                        axesMoved = true;
                        break;
                    default:
                        break;
                }
	        }

	        if( axesMoved )
	        {
                if ( ( this._coopSettings & MDI.CooperativeLevelFlags.NonExclusive ) == MDI.CooperativeLevelFlags.NonExclusive )
                {
                    //DirectInput provides us with meaningless values, so correct that
                    //POINT point;
                    //GetCursorPos(&point);
                    Point point = SWF.Cursor.Position;
                    point = _window.PointToClient( point );
                    MouseState.X.Absolute = point.X;
                    MouseState.Y.Absolute = point.Y;
                }
                else
                {
                    MouseState.X.Absolute += MouseState.X.Relative;
                    MouseState.Y.Absolute += MouseState.Y.Relative;
                }
		        MouseState.Z.Absolute +=  MouseState.Z.Relative;

		        //Clip values to window
				if ( MouseState.X.Absolute < 0 )
					MouseState.X.Absolute = 0;
				else if ( MouseState.X.Absolute > MouseState.Width )
					MouseState.X.Absolute = MouseState.Width;
				if ( MouseState.Y.Absolute < 0 )
					MouseState.Y.Absolute = 0;
				else if ( MouseState.Y.Absolute > MouseState.Height )
					MouseState.Y.Absolute = MouseState.Height;

		        //Do the move
		        if( EventListener != null && IsBuffered )
                    EventListener.MouseMoved( new MouseEventArgs( this, MouseState ) );
	        }

        }

        internal override void initialize()
        {
            MouseState.Clear();

            _mouse = new MDI.Device( MDI.SystemGuid.Mouse );

			_mouse.Properties.AxisModeAbsolute = true;

            _mouse.SetDataFormat( MDI.DeviceDataFormat.Mouse );

			_window = ( (DirectXInputManager)Creator ).WindowHandle;

			_mouse.SetCooperativeLevel( _window, _coopSettings );

            if ( IsBuffered )
            {
                _mouse.Properties.BufferSize = _BUFFER_SIZE;
            }

            try
            {
                _mouse.Acquire();
            }
            catch ( Exception e )
            {
                throw new Exception( "Failed to aquire mouse using DirectInput.", e );
            }
        }

        #endregion Mouse Implementation

    }
}
