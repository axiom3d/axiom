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
using System.Collections.Generic;
using SC = System.Configuration;
using System.Xml.Serialization;

namespace DotNet3D.Configuration
{
	/// <summary>Encapsulates the System.Configuration.Configuration Type inside the AppConfig Property, 
	/// and extends it to support a single CustomConfig type, and has safef Load/Save options, to 
	/// ensure the integrity of the CustomConfig inside the AppConfig.</summary>
	public class Configuration<ConfigType> : IConfiguration where ConfigType : IConfig
	{
		/////////////////////////////////////////////////
		#region Properties, Fields, and Constructor

		/// <summary>Array of Configurations that have been loaded into this AppDomain.</summary>
		static protected List<SC.Configuration> _AppConfigList = new List<System.Configuration.Configuration>(1);

		/// <summary>Underlying System.Configuration.Configuration from which this Config gets it's data.</summary>
		public System.Configuration.Configuration AppConfig { get { return _appConfig; } }
		protected System.Configuration.Configuration _appConfig;

		/// <summary>Lu4Net Config object associated with this Application Configuration object.</summary>
		public IConfig CustomConfig { get { return _customConfig; } }
		protected IConfig _customConfig;

		/// <summary>System.Configuration Section which contains this Config.</summary>
		public IConfigSection Section { get { return _section; } }
		protected ConfigSection<ConfigType> _section;

		/// <summary>Name of the Section for the Config from the AppConfig.</summary>
		public string SectionName { get { return _sectionName; } }
		protected string _sectionName;

		/// <summary>Defines the custom Configuration Class Types contained in this AppConfig.</summary>
		public IConfigTypeDef ConfigTypeDef { get { return _configTypeDef; } }
		protected IConfigTypeDef _configTypeDef;

		/// <summary>Constructor.</summary>
		public Configuration(IConfigTypeDef configTypeDef)
		{
			_configTypeDef = configTypeDef;
			_appConfig = null;
			_sectionName = null;
			_customConfig = null;
			_section = null;
		}

		#endregion Properties, Fields, and Constructor
		/////////////////////////////////////////////////

		/////////////////////////////////////////////////
		#region Public Load() and Save() Methods

		/// <summary>Loads the System Configuration from the AppConfig file for the owning Assembly.
		/// Returns a reference to the CustomConfig loaded from the AppConfig.</summary>
		public virtual IConfig Load()
		{
			// Determine the Section Name
			if (_sectionName == null)
			{
				XmlRootAttribute[] rootAttribs =
					_configTypeDef.ConfigType.GetCustomAttributes(typeof(XmlRootAttribute), false) as XmlRootAttribute[];
				if (rootAttribs != null && rootAttribs.Length > 0)
				{	// If found, use the XmlRoot attribute element name
					_sectionName = rootAttribs[0].ElementName;
				}
				else
				{	// Default to using the ConfigClass Type name
					_sectionName = _configTypeDef.ConfigType.Name;
				}
			}
			// First check to see if this Config has already been loaded.
			LoadAppConfig();

			try
			{	// Now get the Section, or if not found, create a new one
				_section = _appConfig.Sections[_sectionName] as ConfigSection<ConfigType>;
			}
			catch
			{	// config file must be a bad format.  Let's fix it.  Move the Config file and create a new one
				//G.Log.Warn("Configuration.Load() - failed to get section from AppConfig.  Fixing issue...");
				ConfigSerializer.LabelBadConfigFile(_appConfig.FilePath);
				_appConfig = SC.ConfigurationManager.OpenExeConfiguration(_configTypeDef.ExePath);
			}
			if (_section == null)
			{	// Section not found, so we'll create the default Section and add it
				_customConfig = (IConfig)System.Activator.CreateInstance(_configTypeDef.ConfigType);
				_customConfig.Initialize(this);
				_section = (ConfigSection<ConfigType>)
					Activator.CreateInstance(_configTypeDef.SectionType, _customConfig);
				_appConfig.Sections.Remove(_sectionName); // make sure a faulty one isn't already there.
				_appConfig.Sections.Add(_sectionName, _section);
				Save(); // save it, so the next time, it'll be found.
			}
			else
			{	// Now initialize the Custom Config
				_customConfig = _section.Config;
				_customConfig.Initialize(this);
			}
			_customConfig.FilePath = _appConfig.FilePath;
			return _customConfig;
		}
		private void LoadAppConfig()
		{
			for(int i=0; i < _AppConfigList.Count; i++)
			{
				SC.Configuration appConfig = _AppConfigList[i];
				if (appConfig.FilePath.StartsWith(_configTypeDef.ExePath))
				{	// Found a match, so use it
					_appConfig = _AppConfigList[i];
					//G.Log.Debug("LoadAppConfig() - found pre-loaded config: {0}", _appConfig.FilePath);
					return;
				}
			}
			// Else, not found.  We've got to load it from the file, and add it to the static list.
			_appConfig = SC.ConfigurationManager.OpenExeConfiguration(_configTypeDef.ExePath);
			_AppConfigList.Add(_appConfig);
			//G.Log.Debug("LoadAppConfig() - opening new config for: {0}", _appConfig.FilePath);
		}

		/// <summary>Saves the System Configuration to the AppConfig file for the owning Assembly.</summary>
		public virtual void Save()
		{
			if (_section == null || _appConfig == null)
			{	// can't do it
				//G.Log.Error("Configuration.Save() - Failed.  Nothing to save.  Try calling Load() first.");
				return;
			}
			try
			{	// Sometimes this can fail
				_appConfig.Save();
			}
			catch (System.Exception e)
			{
				//G.Log.Warn("Configuration.Save() - failed: {0}", e.Message);
			}
		}

		#endregion Public Load/Save() Methods
		/////////////////////////////////////////////////
	}
}
