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
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace DotNet3D.Configuration
{
	/// <summary>Application Configuration Object - container for all other component-specific configs.
	/// Requirement: All objects contained in this Config must be configured for XML.Serialization, 
	/// such that it can be loaded from XML and Saved to XML without issue.</summary>
	[Serializable]
	public abstract class Config : IConfig
	{
		////////////////////////////////////////////////////////////////
		#region PROPERTIES/FIELDS AND CONSTRUCTORS

		/// <summary>Name assigned to this Config.</summary>
		public string Name { get { return _name; } }
		protected string _name;

		/// <summary>System.Configuration Information associated with this Config.
		/// NOTE: Not mandatory.  
		/// Will be NULL for Configs loaded from their own configFile, or manually constructed.</summary>
		[XmlIgnore]
		public IConfiguration SystemConfig { get { return _systemConfig; } }
		protected IConfiguration _systemConfig;

		/// <summary>Indicates the file path of the associated Config File.
		/// NOTE: If not "IsConfigurable", then this is never used.</summary>
		[XmlIgnore]
		public string FilePath
		{ 
			get { return _filePath; }
			set 
			{	// Only allow Set if no system config
				//G.Log.Debug("Setting FilePath to : {0}", value);
				//G.Errors.ThrowIf(_systemConfig != null, "Can't manually set the 'FilePath' for a Config based on a System.Configuration object.");
				_filePath = value;
				if (_defaultConfigFilePath == String.Empty || _defaultConfigFilePath == null)
				{	// First FilePath becomes the default
					_defaultConfigFilePath = _filePath;
				}
			}
		}
		protected string _filePath;

		/// <summary>The Default Config File Path - which is usually the App.Config location.</summary>
		public string DefaultConfigFilePath { get { return _defaultConfigFilePath; } }
		protected string _defaultConfigFilePath;

		/// <summary>If ConfigFile is being monitored, this event indicates that a change was detected.
		/// Delegate should return "True" to indicate that the event was handled, and the default
		/// processing can be cancelled (note: this only applies to the last delegate in the list, but
		/// normally there should only be one anyway).</summary>
		public event Delegates.ObjectNotifierBool ConfigFileChanged
		{
			add
			{	// First remove it to ensure that it is never double-registered.
				_configFileChanged -= value;	
				_configFileChanged += value;
			}
			remove { _configFileChanged -= value; }
		}
		protected event Delegates.ObjectNotifierBool _configFileChanged;

		/// <summary>Indicates if class has been initialized after construction completed.</summary>
		protected bool _isInitialized;

		/// <summary>Indicates if Configuration will monitor the source ConfigFile for changes,
		/// and if changed, will notify listeners.</summary>
		[XmlAttribute("WatchFile")]
		public bool WatchConfigFileForChanges
		{
			get { return _watchConfigFileForChanges; }
			set
			{	// Apply the change.
				//G.Log.Debug("WatchConfigFileForChanges - set to: {0}", value);
				_watchConfigFileForChanges = value;
				if (_isInitialized)
				{	// we've modified it run-time, so we've got to turn on/off the FileWatcher
					if (_watchConfigFileForChanges)
					{	// Turn on the Watcher
						StartFileWatcher();
					}
					else
					{	// Turn off the watcher
						StopFileWatcher();
					}
				}
			}
		}
		protected bool _watchConfigFileForChanges;

		/// <summary>If configured, this watches the ConfigFile for changes, and notifies listeners.</summary>
		protected FileSystemWatcher _fileWatcher;

		/// <summary>Default Constructor - required for Xml Serialization.</summary>
		public Config() : this(null)
		{}
		public Config(string name)
		{
			if (name == null)
			{	// Default to the TypeName.
				name = this.GetType().Name;
			}
			_name = name;
			_isInitialized = false;
			_watchConfigFileForChanges = false;
			_fileWatcher = null;
			_filePath = String.Empty;
			_defaultConfigFilePath = String.Empty;
			_configFileChanged = null;
			_systemConfig = null;
		}
		~Config()
		{
			Dispose();
		}
		public virtual void Dispose()
		{
			GC.SuppressFinalize(this);
			StopFileWatcher();
		}

		#endregion PROPERTIES/FIELDS AND CONSTRUCTORS
		////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////
		#region Public and Protected Methods

		/// <summary>Initialization for config that was loaded directly from it's own private XML config.</summary>
		public void Initialize(string filePath)
		{
			_filePath = filePath;
			Initialize();
		}
		/// <summary>Should be called after construction completes.</summary>
		public void Initialize(IConfiguration sysConfig)
		{
			_systemConfig = sysConfig as IConfiguration;
			Initialize();
		}
		protected void Initialize()
		{
			if (_isInitialized)
			{	// don't init twice!
				return; 
			}
			_isInitialized = true;

			if (_systemConfig != null)
			{	// inherit from system config
				_filePath = _systemConfig.AppConfig.FilePath;
			}
			//G.Log.Debug("Initialize() called, for FilePath: {0}", _filePath);
			if (!OnInitialize())
			{	// Custom Initialization logic wants to abort.
				return;
			}
			StartFileWatcher(); // only starts it if configured to do so
		}
		/// <summary>Called by Initialize.  Override to add custom initialization logic.
		/// Return True to continue with Initialization, or False to Abort the remainder of Initialization.</summary>
		protected virtual bool OnInitialize() { return true; }

		/// <summary>Called when Config File changes (if being watched).  Notifies listeners.</summary>
		protected void OnConfigFileChanged(object sender, FileSystemEventArgs e)
		{
			_fileWatcher.EnableRaisingEvents = false;

			//G.Log.Debug("OnConfigFileChanged() triggered.");
			if (_configFileChanged != null)
			{	// Notify listeners.
				if (_configFileChanged(this))
				{	// Last Listener indicates that default processing should be Cancelled
					return;
				}
			}
			// handle the event locally.
			HandleOnConfigFileChanged();
		}
		/// <summary>Called by OnConfigFileChanged() if final Listener doesn't return False.</summary>
		protected virtual void HandleOnConfigFileChanged() {}

		/// <summary>ReLoads a fresh instance of the Config from the disk and returns the
		/// new instance.</summary>
		public IConfig LoadNew()
		{
			if (_systemConfig != null)
			{
				//G.Log.Debug("LoadNew() - from AppConfig");
				return ConfigSerializer.LoadFromAppConfig(_systemConfig.ConfigTypeDef);
			}
			else if (this.FilePath != String.Empty)
			{
				//G.Log.Debug("LoadNew() - from File");
				return ConfigSerializer.LoadFromFile(this.FilePath, this.GetType(), false);
			}
			else
			{
				throw G.Errors.New("LoadNew() - Can't do this for a Config that was programmatically constructed.");
			}
		}

		/// <summary>Saves Config to the FilePath as XML.</summary>
		public void Save()
		{
			StopFileWatcher();
			if ((_systemConfig != null) && 
				((this.FilePath == String.Empty) || (this.FilePath == this.DefaultConfigFilePath)))
			{	// Save as part of System.Configuration
				//G.Log.Debug("Save() - to AppConfig");
				ConfigSerializer.SaveToAppConfig(this);
			}
			else
			{	// Save to File
				//G.Log.Debug("Save() - to File");
				ConfigSerializer.SaveToFile(this);
			}
			StartFileWatcher();
		}
		public void SaveAs(string path)
		{
			this.FilePath = path;
			Save();
		}
		protected virtual void customSave() { }

		protected virtual void StartFileWatcher()
		{
			if (_watchConfigFileForChanges)
			{
				StopFileWatcher(); // make sure the previous one is stopped before starting a new one.
				_fileWatcher = new FileSystemWatcher();
				string fullPath = System.IO.Path.GetFullPath(this.FilePath);
				string dirPath = System.IO.Path.GetDirectoryName(fullPath);
				string filename = System.IO.Path.GetFileName(fullPath);
				_fileWatcher.Path = dirPath;
				_fileWatcher.Filter = filename;
				_fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
				_fileWatcher.Changed += new FileSystemEventHandler(OnConfigFileChanged);
				_fileWatcher.EnableRaisingEvents = true;
				//G.Log.Debug("StartFileWatcher() - for path: {0}/{1}", dirPath, filename);
			}
		}

		/// <summary>Stops the FileWatcher from monitoring for changes.</summary>
		protected virtual void StopFileWatcher()
		{
			if (_fileWatcher != null)
			{
				_fileWatcher.EnableRaisingEvents = false;
				_fileWatcher.Dispose();
				_fileWatcher = null;
				//G.Log.Debug("StopFileWatcher() - stopped filewatcher");
			}
		}
		#endregion Public and Protected Methods
		////////////////////////////////////////////////////////////////
	}
}
