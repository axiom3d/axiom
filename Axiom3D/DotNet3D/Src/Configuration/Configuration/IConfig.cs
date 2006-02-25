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

namespace DotNet3D.Configuration
{
	/// <summary>Interface that must be implemented by all Config class.</summary>
	public interface IConfig : ISavable, IDisposable, INamed 
	{
		/// <summary>Underlying Application Configuration from which this Config gets it's data.</summary>
		IConfiguration SystemConfig { get; }

		/// <summary>Filepath from which this Config was loaded, and to which it will be saved.</summary>
		string FilePath { get; set; }

		/// <summary>Initialization for config that was loaded directly from it's own private XML config.</summary>
		void Initialize(string filePath);

		/// <summary>Should be called after construction completes.</summary>
		void Initialize(IConfiguration sysConfig);

		///// <summary>Saves the Config to it's Config disk file, located at 'FilePath'.</summary>
		//void Save();

		/// <summary>Loads a new instance of this Config from the disk file, located at 'FilePath'.</summary>
		IConfig LoadNew();

		/// <summary>Event should be fired whenever the ConfigFile itself changes.</summary>
		event Delegates.ObjectNotifierBool ConfigFileChanged;
	}
}
