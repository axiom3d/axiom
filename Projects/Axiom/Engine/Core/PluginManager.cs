#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for PluginManager.
	/// </summary>
	public class PluginManager : IDisposable
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static PluginManager instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		internal PluginManager()
		{
			if ( instance == null )
			{
				instance = this;
			}
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static PluginManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

		#region Fields

		/// <summary>
		///		List of loaded plugins.
		/// </summary>
		private List<IPlugin> _plugins = new List<IPlugin>();

		#endregion Fields

		#region Methods

		/// <summary>
		///		Loads all plugins specified in the plugins section of the app.config file.
		/// </summary>
		public void LoadAll()
		{
            IList<ObjectCreator> newPlugins = ScanForPlugins();

            foreach ( ObjectCreator pluginCreator in newPlugins )
			{
                IPlugin plugin = LoadPlugin( pluginCreator );
                if ( plugin != null )
                {
                    _plugins.Add( plugin );
                }
            }
        }

		/// <summary>
		///		Scans for plugin files in the current directory.
		/// </summary>
		/// <returns></returns>
        protected IList<ObjectCreator> ScanForPlugins()
		{
		    string[] files = Directory.GetFiles( ".", "*.dll" );
		    Assembly assembly = null;
            List<ObjectCreator> pluginFactories = new List<ObjectCreator>();

		    foreach ( string file in files )
		    {
		        // TODO: allow exlusions in the app.config
		        if ( file != Assembly.GetExecutingAssembly().GetName().Name + ".dll" )
		        {
                    string fullPath = Path.GetFullPath(file);

		            DynamicLoader loader = new DynamicLoader( fullPath );

		            foreach ( ObjectCreator factory in loader.Find( typeof ( IPlugin ) ) )
		            {
		                pluginFactories.Add( factory );
		            }

		        }

		    }
            return pluginFactories;
        }

	    /// <summary>
		///		Unloads all currently loaded plugins.
		/// </summary>
		public void UnloadAll()
		{
			// loop through and stop all loaded plugins
			for ( int i = _plugins.Count - 1; i >= 0; i-- )
			{
				IPlugin plugin = (IPlugin)_plugins[ i ];

                LogManager.Instance.Write( "Unloading plugin: {0}", GetAssemblyTitle( plugin.GetType() ) );

				plugin.Shutdown();
			}

			// clear the plugin list
			_plugins.Clear();
		}

        public static string GetAssemblyTitle(Type type)
        {
            Assembly assembly = type.Assembly;
            AssemblyTitleAttribute title = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(
                                            (Assembly)assembly, typeof(AssemblyTitleAttribute));
            if (title == null)
                return assembly.GetName().Name;
            return title.Title;
        }

        /// <summary>
        ///		Loads a plugin of the given class name from the given assembly, and calls Initialize() on it.
        ///		This function does NOT add the plugin to the PluginManager's
        ///		list of plugins.
        /// </summary>
        /// <param name="assemblyName">The assembly filename ("xxx.dll")</param>
        /// <param name="className">The class ("MyNamespace.PluginClassname") that implemented IPlugin.</param>
        /// <returns>The loaded plugin.</returns>
        private static IPlugin LoadPlugin( ObjectCreator creator )
        {
            try
            {
                // create and start the plugin
                IPlugin plugin = creator.CreateInstance<IPlugin>();

                plugin.Initialize();

                LogManager.Instance.Write( "Loaded plugin: {0}", creator.GetAssemblyTitle() );

                return plugin;
            }
            catch ( Exception ex )
            {
                LogManager.Instance.Write( LogManager.BuildExceptionString( ex ) );
            }

            return null;
        }

		#endregion Methods

		#region IDisposable Implementation

		public void Dispose()
		{
			if ( instance != null )
			{
				instance = null;

				UnloadAll();
			}
		}

		#endregion IDiposable Implementation
	}

}
