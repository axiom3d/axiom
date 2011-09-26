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
using System.Windows.Forms;
using SWF = System.Windows.Forms;

using MDI = Microsoft.DirectX.DirectInput;
using log4net;

#endregion Namespace Declarations

namespace SharpInputSystem
{
	/// <summary>
	/// DirectX 9.0c InputManager specialization
	/// </summary>
	class DirectXInputManager : InputManager, InputObjectFactory
	{
		#region Fields and Properties

		private static readonly ILog log = LogManager.GetLogger( typeof( DirectXInputManager ) );

		private Dictionary<String, MDI.CooperativeLevelFlags> _settings = new Dictionary<string, Microsoft.DirectX.DirectInput.CooperativeLevelFlags>();
		private List<DeviceInfo> _unusedDevices = new List<DeviceInfo>();
		private int _joystickCount = 0;

		#region keyboardInUse Property
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
		#endregion keyboardInUse Property

		#region mouseInUse Property
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
		#endregion keyboardInUse Property

		#region WindowHandle Property
		private SWF.Control _hwnd;
		public SWF.Control WindowHandle
		{
			get
			{
				return _hwnd;
			}
		}
		#endregion WindowHandle Property

		#endregion Fields and Properties

		internal DirectXInputManager()
			: base()
		{
			RegisterFactory( this );
		}

		protected override void _initialize( ParameterList args )
		{
			// Find the first Parameter entry with WINDOW
		    var found = args.Find( p => p.first.ToUpper() == "WINDOW").second;

            _hwnd = Control.FromHandle((IntPtr)found);

			if ( _hwnd is SWF.Form )
			{
				//_hwnd = _hwnd;
			}
			else if ( _hwnd is SWF.PictureBox )
			{
				// if the control is a picturebox, we need to grab its parent form
				while ( !( _hwnd is SWF.Form ) && _hwnd != null )
				{
					_hwnd = _hwnd.Parent;
				}
			}
			else
			{
				throw new Exception( "SharpInputSystem.DirectXInputManger requires the handle of either a PictureBox or a Form." );
			}

			_settings.Add( typeof( Mouse ).Name, 0 );
			_settings.Add( typeof( Keyboard ).Name, 0 );
			_settings.Add( typeof( Joystick ).Name, 0 );

			//Ok, now we have DirectInput, parse whatever extra settings were sent to us
			_parseConfigSettings( args );
			_enumerateDevices();

		}

		private void _enumerateDevices()
		{
			KeyboardInfo keyboardInfo = new KeyboardInfo();
			keyboardInfo.Vendor = this.InputSystemName;
			keyboardInfo.ID = 0;
			_unusedDevices.Add( keyboardInfo );

			MouseInfo mouseInfo = new MouseInfo();
			mouseInfo.Vendor = this.InputSystemName;
			mouseInfo.ID = 0;
			_unusedDevices.Add( mouseInfo );

			foreach ( MDI.DeviceInstance device in MDI.Manager.Devices )
			{
				if ( device.DeviceType == MDI.DeviceType.Joystick || device.DeviceType == MDI.DeviceType.Gamepad ||
					 device.DeviceType == MDI.DeviceType.FirstPerson || device.DeviceType == MDI.DeviceType.Driving ||
					 device.DeviceType == MDI.DeviceType.Flight )
				{
					JoystickInfo joystickInfo = new JoystickInfo();
					joystickInfo.DeviceID = device.InstanceGuid;
					joystickInfo.Vendor = device.ProductName;
					joystickInfo.ID = _joystickCount++;

					this._unusedDevices.Add( joystickInfo );
				}
			}
		}

		private void _parseConfigSettings( ParameterList args )
		{
			System.Collections.Generic.Dictionary<String, MDI.CooperativeLevelFlags> valueMap = new System.Collections.Generic.Dictionary<string, MDI.CooperativeLevelFlags>();

			valueMap.Add( "CLF_BACKGROUND", MDI.CooperativeLevelFlags.Background );
			valueMap.Add( "CLF_FOREGROUND", MDI.CooperativeLevelFlags.Foreground );
			valueMap.Add( "CLF_EXCLUSIVE", MDI.CooperativeLevelFlags.Exclusive );
			valueMap.Add( "CLF_NONEXCLUSIVE", MDI.CooperativeLevelFlags.NonExclusive );
			valueMap.Add( "CLF_NOWINDOWSKEY", MDI.CooperativeLevelFlags.NoWindowsKey );

			foreach ( Parameter p in args )
			{
				switch ( p.first.ToUpper() )
				{
					case "W32_MOUSE":
						_settings[ typeof( Mouse ).Name ] |= valueMap[ p.second.ToString().ToUpper() ];
						break;
					case "W32_KEYBOARD":
						_settings[ typeof( Keyboard ).Name ] |= valueMap[ p.second.ToString().ToUpper() ];
						break;
					case "W32_JOYSTICK":
						_settings[ typeof( Joystick ).Name ] |= valueMap[ p.second.ToString().ToUpper() ];
						break;
					default:
						break;
				}
			}

			if ( _settings[ typeof( Mouse ).Name ] == 0 )
				_settings[ typeof( Mouse ).Name ] = MDI.CooperativeLevelFlags.NonExclusive | MDI.CooperativeLevelFlags.Background;
			if ( _settings[ typeof( Keyboard ).Name ] == 0 )
				_settings[ typeof( Keyboard ).Name ] = MDI.CooperativeLevelFlags.NonExclusive | MDI.CooperativeLevelFlags.Background;
			if ( _settings[ typeof( Joystick ).Name ] == 0 )
				_settings[ typeof( Joystick ).Name ] = MDI.CooperativeLevelFlags.Exclusive | MDI.CooperativeLevelFlags.Foreground;

		}

		internal DeviceInfo PeekDevice<T>() where T : InputObject
		{
			string devType = typeof( T ).Name + "Info";

			foreach ( DeviceInfo device in _unusedDevices )
			{
				if ( devType == device.GetType().Name )
					return device;
			}

			return null;
		}

		internal DeviceInfo CaptureDevice<T>() where T : InputObject
		{
			string devType = typeof(T).Name + "Info";

			foreach ( DeviceInfo device in _unusedDevices )
			{
				if ( devType == device.GetType().Name )
					_unusedDevices.Remove( device );
				return device;
			}

			return null;
		}

		internal void ReleaseDevice<T>( DeviceInfo device ) where T : InputObject
		{
			_unusedDevices.Add( device );
		}


		#region InputObjectFactory Implementation

		IEnumerable<KeyValuePair<Type, string>> InputObjectFactory.FreeDevices
		{
			get
			{
				List<KeyValuePair<Type, string>> freeDevices = new List<KeyValuePair<Type, string>>();
				foreach ( DeviceInfo dev in _unusedDevices )
				{
					if ( dev.GetType() == typeof(KeyboardInfo ) && !keyboardInUse )
						freeDevices.Add( new KeyValuePair<Type, string>( typeof( Keyboard ), this.InputSystemName ) );

					if ( dev.GetType() == typeof( KeyboardInfo ) && !_mouseInUse )
						freeDevices.Add( new KeyValuePair<Type, string>( typeof( Mouse ), this.InputSystemName ) );

					if ( dev.GetType() == typeof(JoystickInfo ) )
						freeDevices.Add( new KeyValuePair<Type, string>( typeof( Joystick ), dev.Vendor ) );
				}
				return freeDevices;
			}
		}

		int InputObjectFactory.DeviceCount<T>()
		{
			if ( typeof( T ) == typeof( Keyboard ) )
				return 1;
			if ( typeof( T ) == typeof( Mouse ) )
				return 1;
			if ( typeof( T ) == typeof( Joystick ) )
				return _joystickCount;
			return 0;
		}

		int InputObjectFactory.FreeDeviceCount<T>()
		{
            string devType = typeof( T ).Name + "Info";
            int deviceCount = 0;
			foreach ( DeviceInfo device in _unusedDevices )
			{
                if ( devType == device.GetType().Name )
                    deviceCount++;
			}
			return deviceCount;
		}

		bool InputObjectFactory.VendorExists<T>( string vendor )
		{
			if ( typeof( T ) == typeof( Keyboard ) || typeof( T ) == typeof( Mouse ) || vendor.ToLower() == InputSystemName.ToLower() )
			{
				return true;
			}
			else
			{
				if ( typeof( T ) == typeof( Joystick ) )
				{
					foreach ( DeviceInfo dev in _unusedDevices )
					{
						if ( dev.GetType() == typeof( JoystickInfo ) )
						{
							JoystickInfo joy = (JoystickInfo)dev;
							if ( joy.Vendor.ToLower() == vendor.ToLower() )
								return true;
						}
					}
				}
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
											  new object[] { this, null, bufferMode, _settings[ typeof( T ).Name ] } );
			return obj;
		}

		void InputObjectFactory.DestroyInputObject( InputObject obj )
		{
			obj.Dispose();
		}

		#endregion InputObjectFactory Implementation
	}
}
