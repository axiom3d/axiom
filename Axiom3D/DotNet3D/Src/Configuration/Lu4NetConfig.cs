/* "Lu4Net" Logging and Utilities Library for .NET, LGPL License
 * Copyright(C)2005, Brian M. Knox (najak@najak.com)
 * 
 * This library is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
 * See the GNU Lesser General Public License for more details at http://www.gnu.org/copyleft/lesser.html.
 * 
 * You should have received a copy of the GNU Lesser General Public License along with this library; if not, write to the Free Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/
using System;
using System.Xml.Serialization;

namespace DotNet3D.Configuration
{
	/// <summary>A Standard Implementation of the Log Configuration Interface.</summary>
	[Serializable]
	[XmlRoot("Lu4Net")]
	public class Lu4NetConfig : Config, ILu4NetConfig
	{
		////////////////////////////////////////////////////////////////
		#region Xml-Base Properties, Fields, and Constructor

		/// <summary>Files which are required for the Application to Run.</summary>
		[XmlArray("RequiredFiles")]
		[XmlArrayItem("File")]
		public FileRequirement[] RequiredFiles
		{
			get { return _requiredFiles; }
			set { _requiredFiles = value; }
		}
		protected FileRequirement[] _requiredFiles;

		/// <summary>Default Constructor - required for Xml Serialization.</summary>
		public Lu4NetConfig()
		{
			_requiredFiles = null;
		}

		#endregion Xml-Base Properties, Fields, and Constructor
		////////////////////////////////////////////////////////////////

	}
}