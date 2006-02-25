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
using System.Configuration;
using System.Xml;

namespace DotNet3D.Configuration
{
	/// <summary>System.Configuration Section wrapper for the Lu4NetConfig class.</summary>
	public class ConfigSection<ConfigType> : ConfigurationSection, IConfigSection where ConfigType : IConfig
	{
		////////////////////////////////////////////////////////////////////
		#region Properties, Fields and Constructor

		/// <summary>Underlying Config contained in this section.</summary>
		public IConfig Config { get { return _config; } }
		protected IConfig _config;

		/// <summary>Hard-code to the type of Config class that is being encapsulated.</summary>
		protected Type _configType { get { return typeof(ConfigType); } }

		/// <summary>Constructors.</summary>
		public ConfigSection()
		{
			_config = null;
			this.SectionInformation.ForceSave = true;
			this.SectionInformation.ForceDeclaration(true);
		}
		public ConfigSection(ConfigType config) : this()
		{
			_config = config;
		}

		#endregion Properties, Fields and Constructor
		////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////////
		#region Protected Override Methods - for Serialization/Deserialization

		/// <summary>Overrides to use XmlSerialization technique, instead of the Name/Value pairs methods.</summary>
		protected override void DeserializeSection(XmlReader reader)
		{
			try
			{
				_config = (IConfig)Data.XmlSerialization.LoadFromXmlReader(reader, _configType);
			}
			catch (System.Exception e)
			{	// failed, so just create a new default instance
				//G.Log.Error("DeserializeSection() failed: {0}", e);
				if (_config == null)
				{	// create a default config
					_config = (IConfig)Activator.CreateInstance(_configType);
				}
			}
		}
		/// <summary>Overrides to use XmlSerialization technique, instead of the Name/Value pairs methods.</summary>
		protected override string SerializeSection(ConfigurationElement parentElement, string name, ConfigurationSaveMode saveMode)
		{
			return Data.XmlSerialization.SerializeToString(_config);
		}

		#endregion Protected Override Methods - for Serialization/Deserialization
		////////////////////////////////////////////////////////////////////
	}
}
