#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;

namespace RenderSystem_DirectX9
{
	/// <summary>
	///		Helper class for dealing with D3D Drivers.
	/// </summary>
	public class Driver
	{
		#region Member variables

		private int adapterNum;
		private D3D.DisplayMode desktopMode;
		private VideoModeCollection videoModeList;
		private String name;
		private String description;

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public Driver(D3D.AdapterInformation adapterInfo)
		{
			this.desktopMode = adapterInfo.CurrentDisplayMode;
			this.name = adapterInfo.Information.DriverName;
			this.description = adapterInfo.Information.Description;
			this.adapterNum = adapterInfo.Adapter;

			videoModeList = new VideoModeCollection();
		}

		#endregion

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		public String Name
		{
			get { return name; }
		}

		/// <summary>
		/// 
		/// </summary>
		public String Description
		{
			get { return description; }
		}

		/// <summary>
		/// 
		/// </summary>
		public int AdapterNumber
		{
			get { return adapterNum; }
		}

		/// <summary>
		///		
		/// </summary>
		public D3D.DisplayMode DesktopMode
		{
			get { return desktopMode; }
		}

		/// <summary>
		///		
		/// </summary>
		public VideoModeCollection VideoModes
		{
			get { return videoModeList; } 
		}

		#endregion
	}
}
