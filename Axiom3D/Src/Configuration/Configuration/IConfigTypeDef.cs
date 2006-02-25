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
	/// <summary>Common interface implemented by all derivatives of the generic ConfigTypeDef class.</summary>
	public interface IConfigTypeDef
	{
		/// <summary>Factory method for creating a Configuration of the right type.</summary>
		IConfiguration CreateConfiguration();

		System.Reflection.Assembly Assembly { get; }

		Type ConfigType { get; }

		Type SectionType { get; }

		/// <summary>Use this instead of "Assembly.Location" because this property guarantees more consistent
		/// results, with regard to capitalization of file extensions.</summary>
		string ExePath { get; }
	}
}
