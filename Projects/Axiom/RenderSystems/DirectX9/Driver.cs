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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	///		Helper class for dealing with D3D Devices.
	/// </summary>
	public class Driver
	{
		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public Driver( D3D.AdapterInformation adapterInfo )
		{
			this._desktopMode = adapterInfo.CurrentDisplayMode;
			this._name = adapterInfo.Details.DriverName;
			this._description = adapterInfo.Details.Description;
			this._adapterNum = adapterInfo.Adapter;
			this._adapterIdentifier = adapterInfo.Details.DeviceIdentifier;

			_videoModeList = new VideoModeCollection();
		}

		#endregion Constructors

		#region Properties

		#region Name Property
		private string _name;
		/// <summary>
		///
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
		}
		#endregion Name Property

		#region Description Property
		private string _description;
		/// <summary>
		///
		/// </summary>
		public string Description
		{
			get
			{
				return _description;
			}
		}
		#endregion Description Property

		#region AdapterNumber Property
		private int _adapterNum;
		/// <summary>
		///
		/// </summary>
		public int AdapterNumber
		{
			get
			{
				return _adapterNum;
			}
		}
		#endregion AdapterNumber Property

		#region AdapterIdentifier Property
		private Guid _adapterIdentifier;
		/// <summary>
		///
		/// </summary>
		public Guid AdapterIdentifier
		{
			get
			{
				return _adapterIdentifier;
			}
		}
		#endregion AdapterIdentifier Property

		#region DesktopMode Property
		private D3D.DisplayMode _desktopMode;
		/// <summary>
		///
		/// </summary>
		public D3D.DisplayMode DesktopMode
		{
			get
			{
				return _desktopMode;
			}
		}
		#endregion DesktopMode Property

		#region VideoModes Property
		private VideoModeCollection _videoModeList;
		/// <summary>
		///
		/// </summary>
		public VideoModeCollection VideoModes
		{
			get
			{
				return _videoModeList;
			}
		}
		#endregion VideoModes Property

		#region D3DDevice Property
		private D3D.Device _device;
		/// <summary>
		///
		/// </summary>
		public D3D.Device D3DDevice
		{
			get
			{
				return _device;
			}
			set
			{
				_device = value;
			}
		}
		#endregion D3DDevice Property

		#region Direct3D Property
		private D3D.Direct3D _direct3D;
		/// <summary>
		/// This is the main Direct3D object
		/// </summary>
		public D3D.Direct3D Direct3D
		{
			get
			{
				return _direct3D;
			}
			set
			{
				_direct3D = value;
			}
		}
		#endregion Direct3D Property

		#endregion Properties
	}
}