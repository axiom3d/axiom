#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Reflection.Emit;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for PluginManager.
	/// </summary>
	public class PluginManager : DisposableObject
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
            : base()
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

		#region properties

		/// <summary>
		/// Gets a read only collection with all known plugins.
		/// </summary>
		public ReadOnlyCollection<IPlugin> InstalledPlugins
		{
			get { return new ReadOnlyCollection<IPlugin>( _plugins ); }
		}

		#endregion

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

		public void LoadDirectory( string path )
		{
			IList<ObjectCreator> newPlugins = ScanForPlugins( path );

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
		protected IList<ObjectCreator> ScanForPlugins()
		{
			return ScanForPlugins( "." );
		}


        /// <summary>
        /// Checks if the given Module contains managed code
        /// </summary>
        /// <param name="file">The file to check</param>
        /// <returns>True if the module contains CLR data</returns>
        private bool IsValidModule(string file)
        {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.Length < 1024)
                    return false;
                using (var reader = new BinaryReader(fs))
                {
                    fs.Position = 0x3C;
                    var offset = reader.ReadUInt32(); // go to NT_HEADER
                    offset += 24;  // go to optional header
                    offset += 208; // go to CLI header directory

                    if (fs.Length < offset + 4)
                        return false;

                    fs.Position = offset;
                    return reader.ReadUInt32() != 0; // check if the RVA to the CLI header is valid
                }
            }
        }

	    /// <summary>
		///		Scans for plugin files in the current directory.
		/// </summary>
		///<param name="folder"></param>
		///<returns></returns>
		protected IList<ObjectCreator> ScanForPlugins( string folder )
		{
            var pluginFactories = new List<ObjectCreator>();

            if (Directory.Exists(folder))
			{
                var files = Directory.GetFiles(folder);
                var assemblyName = Assembly.GetExecutingAssembly().GetName().Name + ".dll";

                foreach (var file in files)
				{
                    var currentFile = Path.GetFileName(file);

                    if (Path.GetExtension(file) != ".dll" || currentFile == assemblyName)
                        continue;
                    var fullPath = Path.GetFullPath(file);

                    if (!IsValidModule(fullPath))
                    {
                        Debug.WriteLine(String.Format("Skipped {0} [Not managed]", fullPath));
                        continue;
                    }

                    var loader = new DynamicLoader(fullPath);

				    pluginFactories.AddRange( loader.Find( typeof ( IPlugin ) ) );
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

		public static string GetAssemblyTitle( Type type )
		{
			Assembly assembly = type.Assembly;
			AssemblyTitleAttribute title = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(
											(Assembly)assembly, typeof( AssemblyTitleAttribute ) );
			if ( title == null )
				return assembly.GetName().Name;
			return title.Title;
		}

		/// <summary>
		///		Loads a plugin of the given class name from the given assembly, and calls Initialize() on it.
		///		This function does NOT add the plugin to the PluginManager's
		///		list of plugins.
		/// </summary>
		/// <returns>The loaded plugin.</returns>
		private static IPlugin LoadPlugin( ObjectCreator creator )
		{
			try
			{
				// create and start the plugin
				IPlugin plugin = creator.CreateInstance<IPlugin>();

                if (plugin == null)
                {
                    LogManager.Instance.Write("Failed to load plugin: {0}", creator.GetAssemblyTitle());
                    return null;
                }

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

        protected override void dispose(bool disposeManagedResources)
        {
            if (!this.IsDisposed)
            {
                if (disposeManagedResources)
                {
                    if (instance != null)
                    {
                        UnloadAll();
                        instance = null;
                    }
                }
            }

            base.dispose(disposeManagedResources);
        }

		#endregion IDisposable Implementation
	}

}