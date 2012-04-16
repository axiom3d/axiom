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
using System.Collections.Generic;

#endregion Namespace Declarations

namespace SharpInputSystem
{
    /// <summary>
    /// 
    /// </summary>
    class DirectXJoystick : Joystick
    {
        #region Fields and Properties

        private const int _BUFFER_SIZE = 124;

        private MDI.CooperativeLevelFlags _coopSettings;
        private MDI.Device _device;
        private MDI.Device _joystick;
        private DirectXForceFeedback _forceFeedback;
		private JoystickInfo _joyInfo;

        private Guid _deviceGuid;

        private SWF.Control _window;

        private int _axisNumber;
        private Dictionary<int, int> _axisMapping = new Dictionary<int,int>();

        #endregion Fields and Properties

        #region Construction and Destruction

        internal DirectXJoystick(InputManager creator, MDI.Device device, bool buffered, MDI.CooperativeLevelFlags coopSettings )
        {
            Creator = creator;
            _device = device;
            IsBuffered = buffered;
            _coopSettings = coopSettings;
            Type = InputType.Joystick;
            EventListener = null;

			_joyInfo = (JoystickInfo)( (DirectXInputManager)Creator ).CaptureDevice<Joystick>();

			if ( _joyInfo == null )
			{
				throw new Exception( "No devices match requested type." );
			}

			_deviceGuid = _joyInfo.DeviceID;
			Vendor = _joyInfo.Vendor;
			DeviceID = _joyInfo.ID.ToString();
            
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
                if ( _joystick != null )
                {
                    try
                    {
                        _joystick.Unacquire();
                    }
                    catch ( Exception e )
                    {
                    }
                    finally
                    {
                        _joystick.Dispose();
                        _joystick = null;
						_device = null;
						_forceFeedback = null;
					}

					( (DirectXInputManager)Creator ).ReleaseDevice<Joystick>( _joyInfo );

                }
            }
            isDisposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base._dispose( disposeManagedResources );
        }
        
        #endregion Construction and Destruction

        #region Methods

        protected void _enumerate()
        {
            //We can check force feedback here too
            MDI.DeviceCaps joystickCapabilities;

            joystickCapabilities = _joystick.Caps;
            this.AxisCount = (short)joystickCapabilities.NumberAxes;
            this.ButtonCount = (short)joystickCapabilities.NumberButtons;
            this.HatCount = (short)joystickCapabilities.NumberPointOfViews;

            _axisNumber = 0;
            _axisMapping.Clear();

            //Enumerate Force Feedback (if any)
            if (_joystick.GetEffects(MDI.EffectType.All).Count != 0)
            {
                _forceFeedback = new DirectXForceFeedback(this);
            }

            //Enumerate and set axis constraints (and check FF Axes)
            foreach(MDI.DeviceObjectInstance doi  in _joystick.GetObjects( MDI.DeviceObjectTypeFlags.Axis ))
            {
                _axisMapping.Add( doi.Offset, _axisNumber++);

                if ( ( doi.Flags & (int)MDI.DeviceObjectTypeFlags.ForceFeedbackActuator ) != 0 )
                {
                    if (_forceFeedback == null)
                    {
                        throw new Exception("ForceFeedback Axis found but reported no ForceFeedback Effects!");
                    }
                }
            }

        }

        #endregion Methods

        #region SharpInputSystem.Joystick Implementation

        public override void Capture()
        {
            MDI.BufferedDataCollection bufferedData = null;
            try
            {
                _joystick.Acquire();
                _joystick.Poll();
                bufferedData = _joystick.GetBufferedData();
            }
            catch( Exception e )
            {
            }

            if (bufferedData == null)
            {
                try
                {
                    _joystick.Acquire();
                    _joystick.Poll();
                    bufferedData = _joystick.GetBufferedData();

                    if (bufferedData == null)
                        return;
                }
                catch (Exception e)
                {
                    return;
                }
            }

	        bool[] axisMoved = {false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,
						          false,false,false,false,false,false,false,false};
	        bool[] sliderMoved = {false,false,false,false};

            for (int i = 0; i < bufferedData.Count; i++)
            {
                if (_axisMapping.ContainsKey(bufferedData[i].Offset))
                {
                    int axis = _axisMapping[bufferedData[i].Offset];
                    this.JoystickState.Axis[axis].Absolute = bufferedData[i].Data;
                    axisMoved[axis] = true;
                }
                else
                {
                    switch ((MDI.JoystickOffset)bufferedData[i].Offset)
                    {
                        default:
                            break;
                    }
                }
            }

            //Check to see if any of the axes values have changed.. if so send events
            if( (IsBuffered == true ) && ( EventListener != null ) && ( bufferedData.Count > 0 ) )
            {
	            JoystickEventArgs temp = new JoystickEventArgs(this, JoystickState);

	            //Update axes
	            for( int i = 0; i < 24; i++ )
		            if( axisMoved[i] )
			            if( EventListener.AxisMoved( temp, i ) == false )
				            return;

	            //Now update sliders
	            for( int i = 0; i < 4; i++ )
		            if( sliderMoved[i] )
			            if( EventListener.SliderMoved( temp, i ) == false )
				            return;
            }

        }

        internal override void initialize()
        {
            JoystickState.Axis.Clear();

            _joystick = new MDI.Device(_deviceGuid);

            _joystick.SetDataFormat(MDI.DeviceDataFormat.Joystick);

            _window = ((DirectXInputManager)Creator).WindowHandle;

            _joystick.SetCooperativeLevel(_window, _coopSettings);

            if (IsBuffered)
            {
                _joystick.Properties.BufferSize = _BUFFER_SIZE;
            }

            //Enumerate all axes/buttons/sliders/etc before aquiring
            _enumerate();

            JoystickState.Axis.Capacity = this.AxisCount;
            for (int i = 0; i < this.AxisCount; i++)
            {
                JoystickState.Axis.Add(new Axis());
            }
            JoystickState.Clear();

            Capture();
            
        }

        public override IInputObjectInterface QueryInterface<T>() 
        {
            if (typeof(T) == typeof(ForceFeedback) )
            {
                return _forceFeedback;
            }
            return base.QueryInterface<T>();
        }
        #endregion SharpInputSystem.Joystick Implementation
    }
}
