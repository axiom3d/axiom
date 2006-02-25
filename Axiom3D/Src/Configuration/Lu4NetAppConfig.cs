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
	public class Lu4NetAppConfig : Lu4NetConfig
	{
		////////////////////////////////////////////////////////////////
		#region Xml-Base Properties, Fields, and Constructor

		/// <summary>Logging Configuration Settings.</summary>'
		[XmlElement("Logging")]
		public LogConfig LogConfig { get{return _logConfig;} set{_logConfig = value;} }
		protected LogConfig _logConfig;

		/// <summary>Indicates if the application will auto-start the global TaskManager on
		/// a background thread automatically upon calling Application.Run().
		/// If True, then the G.TaskMgr.StartMainLoop() will be called, else
		/// if False, the G.TaskMgr MainLoop will not be started automatically.</summary>
		[XmlAttribute("autoTasking")]
		public bool AutoStartTaskMgr
		{
			get { return _isAutoStartTaskMgr; }
			set { _isAutoStartTaskMgr = value; }
		}
		protected bool _isAutoStartTaskMgr;

		/// <summary>WindowsCE Setting Only.
		/// Configures the WinCE Waiter to Check for Timeouts at the specified interval (msec).
		/// So this defines the actually minimum resolution for Waiter timeouts for WinCE.
		/// For other platforms, the real WaitOne(timeout) works, and is not augmented.</summary>
		[XmlElement("WinCeWaiterInterval")][System.ComponentModel.DefaultValue(100)]
		public int WinCE_WaiterCheckInterval
		{
			get { return _wince_WaiterCheckInterval; }
			set { _wince_WaiterCheckInterval = value; }
		}
		protected int _wince_WaiterCheckInterval;

		/// <summary>Default Constructor - required for Xml Serialization.</summary>
		public Lu4NetAppConfig() : base()
		{
			CommonConstructorInit();
		}
		/// <summary>Default Constructor.</summary>
		public Lu4NetAppConfig(string baseLogFilename) : base()
		{
			CommonConstructorInit();
#if DEBUG
			// Set Default Log Level according to release/debug mode
			LogLevel defaultLogLevel = Lu.LogLevel.Info;
#else
			LogLevel defaultLogLevel = Lu.LogLevel.Warn;
#endif
			Logging.Writers.LogWriterConfig defaultWriterConfig = new Writers.LogWriterConfig(LogManager.DefaultName);
			Logging.Targets.LogTargetConfig defaultTargetConfig = new Lu.Logging.Targets.LogTargetConfig(Logging.LogManager.DefaultName, baseLogFilename);
			_logConfig = new Lu.Logging.LogConfig(defaultLogLevel, defaultTargetConfig, null, null);
			Initialize();
		}
		private void CommonConstructorInit()
		{
			_isAutoStartTaskMgr = true;
			_wince_WaiterCheckInterval = 100;
			_logConfig = null;
		}

		#endregion Xml-Base Properties, Fields, and Constructor
		////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////
		#region Public and Protected Methods

		/// <summary>Called by Initialize.  Override to add custom initialization logic.
		/// Return True to continue with Initialization, or False to Abort the remainder of Initialization.</summary>
		protected override bool OnInitialize() 
		{
			if (LogConfig == null)
			{	// Create a default LogConfig
				_logConfig = new Lu.Logging.LogConfig();
			}
			// Else, LogConfig was loaded, so check the custom path
			if (!LoadLogConfigFromCustomPath(_logConfig.CustomFilePath))
			{	// not custom, so we've got to init it here
				_logConfig.Initialize();
			}
			return base.OnInitialize();
		}

		/// <summary>Loads the LogConfig from the specified custom filepath.  If succeeds, then the 
		/// LogConfig member will be replaced with the newly loaded LogConfig.</summary>
		public bool LoadLogConfigFromCustomPath(string customLogFilePath)
		{
			if (customLogFilePath.Length == 0)
			{	// no custom path
				return false;
			}
			if (!System.IO.File.Exists(customLogFilePath))
			{	// no such file, so clear it
				//G.Log.Warn("LoadLogConfigFromCustomPath({0}) - file not found.", customLogFilePath);
				if (_logConfig.CustomFilePath == customLogFilePath)
				{	// clear it.
					_logConfig.CustomFilePath = string.Empty;
				}
				return false;
			}
			// File found, now try to load it.
			Lu.Logging.LogConfig newLogConfig = (Lu.Logging.LogConfig)
				Lu.Data.XmlSerialization.LoadFromFile(customLogFilePath, typeof(Lu.Loggin//G.LogConfig));
			if (newLogConfig != null)
			{	// Load succeeded, so Initialize it.
				_logConfig = newLogConfig;
				_logConfig.CustomFilePath = customLogFilePath;
				_logConfig.Initialize();
				if (Logging.LogManager.IsInitialized)
				{	// We need to force a re-configuration of the LogManager to apply it
					//G.LogMgr.Configure(_logConfig);
				}
				return true;
			}
			// Else, load failed
			//G.Log.Warn("LoadLogConfigFromCustomPath({0}) - failed to load from this file.", customLogFilePath);
			return false;
		}

		/// <summary>Allows LogConfig to be saved to a custom filepath as well.</summary>
		public void SaveLogConfigToCustomPath()
		{
			string customLogFilePath = _logConfig.CustomFilePath;

			if (customLogFilePath.Length > 0)
			{
				string dirPath = System.IO.Path.GetDirectoryName(customLogFilePath);
				if (System.IO.Directory.Exists(dirPath))
				{	// Try to Serialize it to this file
					Data.XmlSerialization.SaveToFile(_logConfig, customLogFilePath);
				}
				else
				{	// else the directory doesn't exist, and we're not going to create it
					//G.Log.Warn("SaveLogConfigToCustomPath() - directory does not exist: {0}", customLogFilePath);
				}
			}
		}

		/// <summary>Called by OnConfigFileChanged() if final Listener doesn't return False.
		/// NOTE: Often applications will want to override this default logic, and reload the Config
		/// to respond to other aspects of the change.</summary>
		protected override void HandleOnConfigFileChanged() 
		{
			// Reload the Config from Disk
			//G.Application.ReloadConfig();

			// Now apply the New Config only to the LogManager
			//G.LogMgr.Configure(this.LogConfig);
		}

		#endregion Public and Protected Methods
		////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////
		#region CreateExampleSettings() - DEBUG MODE ONLY
#if DEBUG
		/// <summary>When there is the absence of a Config file on the disk, and the application wishes
		/// to create an example Config file, call this method to create example data.</summary>
		[System.Diagnostics.Conditional("DEBUG")]
		public virtual void CreateExampleSettings()
		{
			string baseLogName = this.FilePath.Replace(".config", "");

			Logging.Writers.LogWriterConfig defaultWriterConfig =
				new Lu.Logging.Writers.LogWriterConfig(Loggin//G.LogManager.DefaultName);
			defaultWriterConfig.AlwaysFlush = true;
			defaultWriterConfig.ShowStackFrames = 1;
			defaultWriterConfig.ShowStackFullNames = false;
			Logging.Targets.LogTargetConfig defaultTargetConfig =
				new Lu.Logging.Targets.LogTargetConfig(Loggin//G.LogManager.DefaultName, baseLogName);
			_logConfig = new Lu.Loggin//G.LogConfig(LogLevel.Info, defaultTargetConfig, defaultWriterConfig, null);

			// Show an example of each LogGroup type
			_logConfig.Groups.Add(new Loggin//G.LogGroupSetting("'SilentGroup", LogLevel.Silent));
			_logConfig.Groups.Add(new Loggin//G.LogGroupSetting("'ErrorsGroup", LogLevel.Error, "errors"));
			_logConfig.Groups.Add(new Loggin//G.LogGroupSetting("'WarningsGroup", LogLevel.Warn));
			_logConfig.Groups.Add(new Loggin//G.LogGroupSetting("'.*WildPrefix", LogLevel.Info));
			_logConfig.Groups.Add(new Loggin//G.LogGroupSetting("'WildSuffix.*", LogLevel.Debug));
			_logConfig.Groups.Add(new Loggin//G.LogGroupSetting("'.*WildBoth.*", LogLevel.Verbose));

			_logConfig.Filters.Add(new Lu.Logging.Filters.LogFilterConfig("taskLoopOnly", LogLevel.Warn, 
					Lu.Logging.Filters.LogFilterMode.PassIfAny,
					new string[] { "TaskLoop" }));
			_logConfig.Filters.Add(new Lu.Logging.Filters.LogFilterConfig("allTasking", LogLevel.Warn, 
					Lu.Logging.Filters.LogFilterMode.PassIfAny,
					new string[] { "Task" }));
			_logConfig.UseFullNamesForMessages = false;

			_logConfig.Writers.Add(new Lu.Logging.Writers.LogWriterConfig("filtered", Lu.Loggin//G.LogManager.DefaultName, "taskLoopOnly"));
			_logConfig.Writers.Add(new Lu.Logging.Writers.LogWriterConfig("errors", "errorFile"));
			_logConfig.Writers.Add(new Lu.Logging.Writers.LogWriterConfig("temp", "tempFile"));

			_logConfig.Targets.Add(new Lu.Logging.Targets.LogTargetConfig("errorFile", "errors"));
			_logConfig.Targets.Add(new Lu.Logging.Targets.LogTargetConfig("tempFile", "temp"));
			_requiredFiles = new FileRequirement[]
			{
				new FileRequirement("'MustHaveThisAssembly.dll", FileType.Assembly),
				new FileRequirement("'MustHaveThisInstalled.dll", FileType.Assembly, 
					new byte[] {0x0, 0x1, 0x2, 0x3}, true),
				new FileRequirement("'MustHaveThisConfig.cfg", FileType.Config),
				new FileRequirement("'MustHaveThisContent.image", FileType.Content),
				new FileRequirement("'MustHaveThisNative.dll", FileType.NativeDLL),
				new FileRequirement("'MustHaveValidated.dll", FileType.NativeDLL, 12345, 7654321)
			};
			// Watch for File Changes.
			_watchConfigFileForChanges = true;
		}
#endif
		#endregion CreateExampleSettings() - DEBUG MODE ONLY
		////////////////////////////////////////////////////////////////
	}
}