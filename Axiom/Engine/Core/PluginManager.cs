using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using Axiom.Exceptions;

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for PluginManager.
	/// </summary>
	public class PluginManager : IConfigurationSectionHandler {
		#region Singleton implementation

		static PluginManager() {
			Init();
		}

		protected PluginManager() {}

		protected static PluginManager instance;

		public static PluginManager Instance {
			get { 
				return instance; 
			}
		}

		public static void Init() {
			instance = new PluginManager();
		}
		
		#endregion

		#region Fields

		/// <summary>
		///		List of loaded plugins.
		/// </summary>
		private ArrayList plugins = new ArrayList();

		#endregion Fields

		/// <summary>
		///		Loads all plugins specified in the plugins section of the app.config file.
		/// </summary>
		public void LoadAll() {
			// trigger load of the plugins app.config section
			ConfigurationSettings.GetConfig("plugins");
		}

		/// <summary>
		///		Unloads all currently loaded plugins.
		/// </summary>
		public void UnloadAll() {
			// loop through and stop all loaded plugins
			for(int i = 0; i < plugins.Count; i++) {
				((IPlugin)plugins[i]).Stop();
			}

			// clear the plugin list
			plugins.Clear();
		}

		#region IConfigurationSectionHandler Members

		public object Create(object parent, object configContext, System.Xml.XmlNode section) {
			// grab the plugin nodes
			XmlNodeList pluginNodes = section.SelectNodes("plugin");

			// loop through each plugin node and load the plugins
			for(int i = 0; i < pluginNodes.Count; i++) {
				XmlNode pluginNode = pluginNodes[i];

				// grab the attributes for loading these plugins
				XmlAttribute assemblyAttribute = pluginNode.Attributes["assembly"];
				XmlAttribute classAttribute = pluginNode.Attributes["class"];

				string assemblyFile = assemblyAttribute.Value;
				assemblyFile = Environment.CurrentDirectory + Path.DirectorySeparatorChar + assemblyFile;
				string className = classAttribute.Value;

				// load the requested assembly
				Assembly pluginAssembly = Assembly.LoadFile(assemblyFile);

				// find the title of this assembly
				AssemblyTitleAttribute title = 
					(AssemblyTitleAttribute)Attribute.GetCustomAttribute(pluginAssembly, typeof(AssemblyTitleAttribute));

				// grab the plugin type from the assembly
				Type type = pluginAssembly.GetType(className);

				// see if this is a valid plugin first
				if(type.GetInterface("IPlugin") != null) {
                    try {
                        // create and start the plugin
                        IPlugin plugin = (IPlugin)Activator.CreateInstance(type);

                        plugin.Start();

                        plugins.Add(plugin);

                        Trace.WriteLine("Loaded plugin: " + title.Title);
                    }
                    catch(Exception ex) {
                        Trace.WriteLine(ex.ToString());
                    }
				}
				else {
					throw new PluginException("Class {0} is not a valid plugin.", className);
				}
			}

			return null;
		}

		#endregion
	}
}
