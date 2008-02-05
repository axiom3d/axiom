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
        private ArrayList plugins = new ArrayList();

        #endregion Fields

        #region Methods

        /// <summary>
        ///		Loads all plugins specified in the plugins section of the app.config file.
        /// </summary>
        public void LoadAll()
        {
            // TODO: Make optional, using scanning again in the meantim
            // trigger load of the plugins app.config section
            //ArrayList newPlugins = (ArrayList)ConfigurationSettings.GetConfig("plugins");
            ArrayList newPlugins = ScanForPlugins();

            foreach ( ObjectCreator pluginCreator in newPlugins )
            {
                plugins.Add( LoadPlugin( pluginCreator ) );
            }
        }

        /// <summary>
        ///		Scans for plugin files in the current directory.
        /// </summary>
        /// <returns></returns>
        protected ArrayList ScanForPlugins()
        {
            ArrayList plugins = new ArrayList();

            string[] files = Directory.GetFiles( ".", "*.dll" );

            foreach ( string file in files )
            {
                // TODO: Temp fix, allow exlusions in the app.config
                if ( file != "Axiom.Engine.dll" && file.IndexOf( "Axiom." ) != -1 )
                {
                    try
                    {
                        string fullPath = Path.GetFullPath( file );

                        Assembly assembly = Assembly.LoadFrom( fullPath );

                        foreach ( Type type in assembly.GetTypes() )
                        {
                            if ( type.GetInterface( "IPlugin" ) != null )
                            {
                                plugins.Add( new ObjectCreator( file, type.FullName ) );
                            }
                        }
                    }
                    catch ( BadImageFormatException )
                    {
                        // eat, not a valid assembly
                    }
                }
            }

            return plugins;
        }

        /// <summary>
        ///		Unloads all currently loaded plugins.
        /// </summary>
        public void UnloadAll()
        {
            // loop through and stop all loaded plugins
            for ( int i = 0; i < plugins.Count; i++ )
            {
                IPlugin plugin = (IPlugin)plugins[ i ];

                // find the title of this assembly
                AssemblyTitleAttribute title =
                    (AssemblyTitleAttribute)Attribute.GetCustomAttribute( plugin.GetType().Assembly, typeof( AssemblyTitleAttribute ) );

                LogManager.Instance.Write( "Unloading plugin: {0}", title.Title );

                plugin.Stop();
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
        private static IPlugin LoadPlugin( ObjectCreator creator )
        {
            // load the requested assembly
            Assembly pluginAssembly = creator.GetAssembly();

            // find the title of this assembly
            AssemblyTitleAttribute title =
                (AssemblyTitleAttribute)Attribute.GetCustomAttribute( pluginAssembly, typeof( AssemblyTitleAttribute ) );

            // grab the plugin type from the assembly
            Type type = pluginAssembly.GetType( creator.className );

            // see if this is a valid plugin first
            if ( type.GetInterface( "IPlugin" ) != null )
            {
                try
                {
                    // create and start the plugin
                    IPlugin plugin = (IPlugin)Activator.CreateInstance( type );

                    plugin.Start();

                    LogManager.Instance.Write( "Loaded plugin: {0}", title.Title );

                    return plugin;
                }
                catch ( Exception ex )
                {
                    LogManager.Instance.Write( ex.ToString() );
                }
            }
            else
            {
                throw new PluginException( "Class {0} is not a valid plugin.", creator.className );
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
