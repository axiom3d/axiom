#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

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


using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using Axiom.Exceptions;

namespace Axiom.Core {
	/// <summary>
	/// Summary description for PluginManager.
	/// </summary>
	public class PluginManager : IDisposable {
		#region Singleton implementation

		protected PluginManager() {
			LoadAll();
		}

		protected static PluginManager instance;

		public static PluginManager Instance {
			get { 
				return instance; 
			}
		}

		public static void Init() {
			if (instance != null) {
				throw new ApplicationException("PluginManager.Instance is null!");
			}
			instance = new PluginManager();
			GarbageManager.Instance.Add(instance);
		}

		public void Dispose() {
			UnloadAll();
			if (instance == this) {
				instance = null;
			}
		}
		
		#endregion

		#region Fields

		/// <summary>
		///		List of loaded plugins.
		/// </summary>
		private ArrayList plugins = new ArrayList();

		#endregion Fields

		#region Plugin loading and unloading code

		/// <summary>
		///		Loads all plugins specified in the plugins section of the app.config file.
		/// </summary>
		public void LoadAll() {
			// trigger load of the plugins app.config section
			ArrayList newPlugins = (ArrayList)ConfigurationSettings.GetConfig("plugins");
			foreach (ObjectCreator pluginCreator in newPlugins) {
				plugins.Add(LoadPlugin(pluginCreator));
			}
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

		/// <summary>
		///		Loads a plugin of the given class name from the given assembly, and calls Start() on it.
		///		This function does NOT add the plugin to the PluginManager's
		///		list of plugins.
		/// </summary>
		/// <param name="assemblyName">The assembly filename ("xxx.dll")</param>
		/// <param name="className">The class ("MyNamespace.PluginClassname") that implemented IPlugin.</param>
		/// <returns>The loaded plugin.</returns>
		private static IPlugin LoadPlugin(ObjectCreator creator) {
			// load the requested assembly
			Assembly pluginAssembly = creator.GetAssembly();

			// find the title of this assembly
			AssemblyTitleAttribute title = 
				(AssemblyTitleAttribute)Attribute.GetCustomAttribute(pluginAssembly, typeof(AssemblyTitleAttribute));

			// grab the plugin type from the assembly
			Type type = pluginAssembly.GetType(creator.className);

			// see if this is a valid plugin first
			if(type.GetInterface("IPlugin") != null) {
				try {
					// create and start the plugin
					IPlugin plugin = (IPlugin)Activator.CreateInstance(type);

					plugin.Start();

					Trace.WriteLine("Loaded plugin: " + title.Title);

					return plugin;
				}
				catch(Exception ex) {
					Trace.WriteLine(ex.ToString());
				}
			}
			else {
				throw new PluginException("Class {0} is not a valid plugin.", creator.className);
			}

			return null;
		}

		#endregion

	}

	/// <summary>
	/// The plugin configuration handler
	/// </summary>
	public class PluginConfigurationSectionHandler : IConfigurationSectionHandler {

		public object Create(object parent, object configContext, System.Xml.XmlNode section) {
			ArrayList plugins = new ArrayList();

			// grab the plugin nodes
			XmlNodeList pluginNodes = section.SelectNodes("plugin");

			// loop through each plugin node and load the plugins
			for(int i = 0; i < pluginNodes.Count; i++) {
				XmlNode pluginNode = pluginNodes[i];

				// grab the attributes for loading these plugins
				XmlAttribute assemblyAttribute = pluginNode.Attributes["assembly"];
				XmlAttribute classAttribute = pluginNode.Attributes["class"];

				plugins.Add(new ObjectCreator(assemblyAttribute.Value, classAttribute.Value));
			}

			return plugins;
		}
	}

}
