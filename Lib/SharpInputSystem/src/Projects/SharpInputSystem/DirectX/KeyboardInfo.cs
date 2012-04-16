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

#endregion Namespace Declarations

namespace SharpInputSystem
{

	public class KeyboardInfo : DeviceInfo
	{
		private int _id;
		public int ID
		{
			get
			{
				return _id;
			}
			set
			{
				_id = value;
			}
		}

		private Guid _deviceID;
		public Guid DeviceID
		{
			get
			{
				return _deviceID;
			}
			set
			{
				_deviceID = value;
			}
		}

		private string _vendor;
		public string Vendor
		{
			get
			{
				return _vendor;
			}
			set
			{
				_vendor = value;
			}
		}
	}
}
