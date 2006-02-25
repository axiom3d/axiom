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
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using AppConfig = System.Configuration.Configuration;
using System.Configuration;

namespace DotNet3D.Configuration
{
	public static class ConfigSerializer
	{
		/// <summary>For "SaveToFile()" method, indicates whether or not the previous Config should be backed up.
		/// Defaults to False.</summary>
		static public bool BackupConfigOnOverwrite = false;

		/// <summary>Returns the default file path based on the specified Assembly.</summary>
		static public string GetConfigFilePathDefault(Assembly assembly)
		{
			string fullPath = assembly.Location.Replace(".EXE", ".exe").Replace(".DLL", ".dll"); //TODO: Kludge to fix .NET20 bug, which sometimes returns uppercase EXE extensions.
			string configFileName = Path.GetFileName(fullPath) + ".config";
			return configFileName;
		}

		//////////////////////////////////////////
		#region STATIC LOAD METHODS

		/// <summary>Loads and Instantiates a Config object from the specified serialized XML file.</summary>
		/// <param name="assembly">Assembly that owns the parent System Configuration object.</param>
		/// <param name="configClassType">Type of the Config object that will be instantiated.</param>
		static public IConfig LoadFromAppConfig(IConfigTypeDef configTypeDef)
		{
			//G.Log.Info("LoadFromAppConfig(): assembly: {0}, type = {1}", configTypeDef.ExePath, configTypeDef.ConfigType);
			IConfig config = null;
			try
			{
				DotNet3D.Configuration.IConfiguration sysConfig = configTypeDef.CreateConfiguration();
				config = sysConfig.Load();
			}
			catch (System.Exception e)
			{
				//G.Log.Error("LoadFromAppConfig() - failed to initialize from System Configuration from file: {0}, {1}", configTypeDef.ExePath + ".config", e.Message);
				config = (IConfig)Activator.CreateInstance(configTypeDef.ConfigType);
			}
			return config;
		}

		/// <summary>Loads and Instantiates a Config object from the specified serialized XML file.</summary>
		/// <param name="configFilePath">Filepath to the config file that is being loaded.</param>
		/// <param name="configClassType">Type of Config object that will be loaded from the config file path.</param>
		static public IConfig LoadFromFile(string configFilePath, Type configClassType, bool makeNewOnFailure)
		{
			//G.Log.Info("LoadFromFile({0}), type = {1}", configFilePath, configClassType);
			IConfig config = null;
			try
			{
				config = Data.XmlSerialization.LoadFromFile(configFilePath, configClassType) as IConfig;
				if (config != null)
				{	// Initialize it.
					//G.Log.Info("LoadFromFile() - Deserialized to IConfig file.  Intializing it...");
					config.Initialize(configFilePath);
				}
				else if (makeNewOnFailure) // It's NULL, and they want us to fix the issue.
				{	// Find out why it failed and try to recover
					if (File.Exists(configFilePath))
					{	// See if it looks like an AppConfig.
						string configContents = File.ReadAllText(configFilePath);
						if (configContents.IndexOf("<configSections>") > -1)
						{	// Ooops, this looks like a System.Config, so say this as a log error
							//G.Log.Error("Need to fix this!  Config failed to load... Looks like you should be using method 'LoadFromSystemConfig' instead.");
						}
						// Move the existing file out of the way, labeling it "BAD"
						LabelBadConfigFile(configFilePath);
					}
					// create a default instance, and save it back to the XML file
					//G.Log.Warn("LoadFromFile() Failed.  Defaulting to creating a default Config and Saving it to disk.");
					config = (IConfig)System.Activator.CreateInstance(configClassType);
					config.Initialize(configFilePath);
					SaveToFile(config);
				}
			}
			catch (System.Exception e)
			{	// Log the error
				//G.Log.Error("LoadFromFile() - Failed to load or even to recover: {0}", e);
			}
			return config;
		}

		/// <summary>Called when ConfigFile is corrupt and it needs to be renamed to label is at "BAD".</summary>
		static public void LabelBadConfigFile(string configFilePath)
		{
			// Just move it out of the way by adding the ".BAD" extension to it.
			string dirPath = Path.GetDirectoryName(configFilePath);
			string badname = "_BAD." + Path.GetFileName(configFilePath);
			string badpath = dirPath + Path.DirectorySeparatorChar + badname;
			if (File.Exists(badpath))
			{	// get rid of previous BAD config file first
				File.Delete(badpath);
			}
			// now rename the current bad config to the .BAD named filename
			File.Move(configFilePath, badpath);
		}

		#endregion STATIC LOAD METHODS
		//////////////////////////////////////////

		//////////////////////////////////////////
		#region STATIC SAVE METHODS

		/// <summary>Serializes the Config object to the specified file path.</summary>
		static public void SaveToAppConfig(IConfig config)
		{
			XmlTextWriter writer = null;
			try
			{
				////G.Log.Info("SaveToAppConfig() - Serializing Configuration to file: ", config.FilePath);
				config.SystemConfig.Save();
			}
			catch (System.Exception e)
			{
				////G.Log.Error("SaveToAppConfig() - Serialization of object failed: {0}", e);
			}
			finally
			{
				if (writer != null)
					writer.Close();
			}
		}

		/// <summary>Serializes the Config object to the specified file path.</summary>
		static public void SaveToFile(IConfig config)
		{
			if (config.SystemConfig != null)
			{	// seems suspicious.
				////G.Log.WarnDebug("SaveToFile() - should be using 'SaveToSystemConfig' instead.");
			}
//if (config.SystemConfig != null)
//{	// Fix a detected usage error.
//   Logger.Log.Error("SaveToFile() called by mistake.  Should have called 'SaveToSystemConfig' instead.  Doing this now...");
//   SaveToAppConfig(config);
//   return;
//}
			string configFilePath = config.FilePath;
			//G.Log.Info("SaveToFile() - Serializing Config type to file: {0}", configFilePath);
			try
			{
				if (File.Exists(configFilePath))
				{
					if (BackupConfigOnOverwrite)
					{
						string backupFileName = configFilePath + ".backup";
						////G.Log.Info("SaveToFile() - making backup of existing file: {0}", backupFileName);
						if (File.Exists(backupFileName))
						{	// Gotta remove it first
							File.Delete(backupFileName);
						}
						File.Move(configFilePath, configFilePath + ".backup");
					}
					else
					{	// no backups.  Just delete it.
						File.Delete(configFilePath);
					}
				}
			}
			catch (System.Exception e)
			{
				//G.Log.Error("SaveToFile() - Serialization of config failed: {0}", e);
			}
			// conflicting file has been moved, so save it now
			Data.XmlSerialization.SaveToFile(config, configFilePath);
		}

		#endregion STATIC SAVE METHODS
		//////////////////////////////////////////
	}
}
