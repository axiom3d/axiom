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

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using System.Xml;
using System.Diagnostics;
using System.Reflection;

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    /// Axiom plugin manager 
    /// </summary>
    public class PluginManager : IDisposable
    {
        #region Singleton implementation

        /// <summary>
        ///     Singleton instance of this class.
        /// </summary>
        private static PluginManager _instance = null;

        /// <summary>
        /// Private constructor.  This class cannot be instantiated by any third party.
        /// </summary>
        private PluginManager()
        {
            _instance = this;

            preloadCoreAssembly();

            _instance.plugins.ObjectResolve +=
                new ObjectResolveEventHandler<string, IPlugin>( resolvePluginHandler );

            managerConfig = (PluginManagerConfiguration)
                ConfigurationManager.GetSection( "Plugins" );

            if ( managerConfig == null )
                managerConfig = new PluginManagerConfiguration( CURRENT_FOLDER );

            // fill in plugin loaders
            pluginLoaders.Add( typeof( IPlugin ), new PluginLoader() );
            pluginLoaders.Add( typeof( ISingletonPlugin ), new SingletonPluginLoader() );

            processAssemblies();
        }

        /// <summary>
        /// Gets the singleton instance of this class.
        /// </summary>
        public static PluginManager Instance
        {
            get
            {
                if ( _instance == null )
                    new PluginManager();

                return _instance;
            }
        }

        #endregion Singleton implementation

        protected const string
            CURRENT_FOLDER = ".";
        protected const string
            AXIOM_CORE_DLL = "Axiom.dll";
        protected const string
            PLUIGIN_ASSEMBLYNAME_PREFIX = "Axiom.";

        /// <summary>
        /// Represents a list of plugin loaders
        /// </summary>
        private Dictionary<Type, IPluginLoader> pluginLoaders =
            new Dictionary<Type, IPluginLoader>();

        /// <summary>
        /// Plugin manager configuration
        /// </summary>
        protected PluginManagerConfiguration managerConfig = null;

        /// <summary>
        /// Plugin registry
        /// </summary>
        HierarchicalRegistry<IPlugin> plugins = new HierarchicalRegistry<IPlugin>();

        void resolvePluginHandler( object sender, ObjectResolveEventArgs<string, IPlugin> e )
        {
            string pluginQualifiedName = e.Key; // e.g. /Axiom/RenderSystems/D3D9RenderSystemPlugin
            e.ResolvedObject = resolvePlugin( pluginQualifiedName );
        }

        /// <summary>
        /// Resolves an unloaded plugin
        /// </summary>
        /// <param name="pluginName">Qualified plugin name (/Axiom/RenderSystems/D3D9RenderSystem)</param>
        /// <returns><see cref="IPlugin"/>-compatible object 
        /// -OR- null if the plugin cannot be resolved</returns>
        /// <remarks>
        /// The Plugin Manager makes a number of assumptions about the
        /// qualified plugin name. 
        /// 1) It contains at least 3 parts (/Axiom/RenderSystems/DirectX9). In this
        /// case the plugin manager tries to instantiate 
        /// "Axiom.RenderSystems.DirectX9.Plugin" class
        /// 2) If the qualified plugin name contains more than 3 parts, slashes are
        /// replaced with full-stops and the plugin manager tries to instantiate 
        /// this type
        /// 3) Also, it blindly tries to create the plugin from the passed
        /// plugin name. This allows to query plugins by type name directly
        /// </remarks>
        protected virtual IPlugin resolvePlugin( string pluginName )
        {
            PluginMetadataAttribute metadata = null;

            if ( !metadataStore.ContainsKey( pluginName ) )
                throw new PluginException( "Metadata for plugin {0} is not found",
                    pluginName );

            metadata = metadataStore[ pluginName ];

            if ( metadata.IsSingleton )
                return pluginLoaders[ typeof( ISingletonPlugin ) ].LoadPlugin( metadata );
            else
                return pluginLoaders[ typeof( IPlugin ) ].LoadPlugin( metadata );
        }


        public IPlugin GetPlugin( string pluginQualifiedName )
        {
            return plugins[ pluginQualifiedName ];
        }

        public IEnumerable<IPlugin> GetPlugins( string namespacePrefix )
        {
            return plugins.Subtree( namespacePrefix );
        }

        public IEnumerable<PluginMetadataAttribute> GetPluginInfo( string namespacePrefix )
        {
            IEnumerator<KeyValuePair<string, PluginMetadataAttribute>> enu =
                metadataStore.GetEnumerator();

            while ( enu.MoveNext() )
                if ( enu.Current.Key.StartsWith( namespacePrefix ) )
                    yield return enu.Current.Value;
        }

        /// <summary>
        /// Stores metadata information for plugins
        /// </summary>
        protected Dictionary<string, PluginMetadataAttribute> metadataStore = new Dictionary<string, PluginMetadataAttribute>();

        private void preloadCoreAssembly()
        {
            Assembly.ReflectionOnlyLoadFrom( AXIOM_CORE_DLL );

            // wire other preload requests to satisfy lazy loaded
            // reflection only behaviour
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler( loadAssemblyForInspection );
        }

        Assembly loadAssemblyForInspection( object sender, ResolveEventArgs args )
        {
            return Assembly.ReflectionOnlyLoad( args.Name );
        }

        /// <summary>
        /// Inspects the given assembly for the list of exported plugins
        /// </summary>
        /// <param name="assemblyPath">full path to the assembly file</param>
        /// <remarks>Assembly is loaded into the "reflection only" context thus
        /// making it impossible to execute code from that assembly. This 
        /// eliminates the discrepancy that all assemblies are loaded into
        /// the exection context upon Axiom initialization</remarks>
        protected void inspectAssemblyForPlugins( string assemblyPath )
        {
            Assembly asm = Assembly.ReflectionOnlyLoadFrom( assemblyPath );
            Type[] types = asm.GetExportedTypes();

            foreach ( Type type in types )
            {
                if ( type.GetInterface( "IPlugin" ) != null )
                {
                    IList<CustomAttributeData> attrs =
                        CustomAttributeData.GetCustomAttributes( type );

                    if ( attrs.Count == 1 )
                    {
                        PluginMetadataAttribute metadata =
                            PluginMetadataAttribute.ReflectionOnlyConstructor( attrs[ 0 ].NamedArguments );

                        if ( metadata.IsSingleton && type.GetInterface( "ISingletonPlugin" ) == null )
                            throw new PluginException( "Plugin {0} is declared as singleton but does not implement ISingletonPlugin",
                                metadata.Namespace );

                        metadata.TypeName = type.AssemblyQualifiedName;
                        metadataStore.Add( metadata.Namespace, metadata );
                    }
                }
            }
        }

        private bool _isInitialized = false;
        void processAssemblies()
        {
            if ( _isInitialized )
                return;

            _isInitialized = true;

            string[] files = Directory.GetFiles( managerConfig.PluginFolder,
                    "*.dll" );

            foreach ( string fileName in files )
            {
                if ( fileName.IndexOf( AXIOM_CORE_DLL ) == -1
                    && fileName.IndexOf( PLUIGIN_ASSEMBLYNAME_PREFIX ) != -1 )
                {
                    try
                    {
                        string fullPath = Path.GetFullPath( fileName );
                        inspectAssemblyForPlugins( fullPath );
                    }
                    catch ( BadImageFormatException )
                    {
                        // eat, not a valid assembly
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the PluginManager
        /// </summary>
        /// <remarks>
        /// All assemblies in the plugin folder are inspected for plugin metadata
        /// information including namespace -- assembly qualified plugin type name 
        /// matching. Then, if the plugin configuration section contains any records,
        /// the corresponding plugins are instantiated. 
        /// Other plugins are not instantiated until somebody makes a request to
        /// <see cref="PluginManager.GetPlugin"/> method.
        /// </remarks>
        /// TODO: specify plugin exclusions in app.config
        public void Init()
        {
            processAssemblies();

            if ( managerConfig.Plugins.Count > 0 )
            {
                foreach ( string pluginTypeName in managerConfig.Plugins )
                {
                    IPlugin plugin = plugins[ pluginTypeName ];
                    plugin.Start();
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            // TODO: implement "register for dispose" registry stub
        }

        #endregion
    }

    /// <summary>
    /// <see cref="PluginManager"/> configuration class
    /// </summary>
    public class PluginManagerConfiguration
    {
        string _pluginFolder = string.Empty;
        /// <summary>
        /// Root folder for plugin assemblies
        /// </summary>
        public string PluginFolder
        {
            get
            {
                return _pluginFolder;
            }
        }

        List<string> plugins = new List<string>();
        /// <summary>
        /// List of registered plugin names
        /// </summary>
        public List<string> Plugins
        {
            get
            {
                return plugins;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pluginFolder">root plugin folder</param>
        public PluginManagerConfiguration( string pluginFolder )
        {
            _pluginFolder = pluginFolder;
        }
    }

    #region PluginConfigurationSectionHandler
    /// <summary>
    /// The plugin configuration handler
    /// </summary>
    /// <remarks>
    /// <see cref="PluginManager"/> can be configured using the "plugins" configuration
    /// section. Example:
    /// <code>
    /// <![CDATA[
    /// <plugins folders="plugins">
    ///     <plugin type="Axiom.RenderSystems.D3D9RenderSystemPlugin,Axiom.RenderSystems.DirectX9" />
    /// </plugins>
    /// ]]>
    /// </code>
    /// In this example we specify plugin root folder and one plugin to load.
    /// Plugin type must be specified by qualified type names. If no plugin types are
    /// specified, all assemblies are traversed in the specified plugin folder. 
    /// If the folder is not specified, current folder is assumed. If the section is not
    /// present in the app.config file, the behavior is the same.
    /// </remarks>
    public class PluginConfigurationSectionHandler : IConfigurationSectionHandler
    {
        object IConfigurationSectionHandler.Create( object parent, object configContext, System.Xml.XmlNode section )
        {
            PluginManagerConfiguration config =
                new PluginManagerConfiguration( section.Attributes[ "folder" ] == null
                ? "." : section.Attributes[ "folder" ].Value );

            // grab the plugin nodes
            XmlNodeList pluginNodes = section.SelectNodes( "plugin" );

            // loop through each plugin node and load the plugins
            foreach ( XmlNode pluginNode in pluginNodes )
                config.Plugins.Add( pluginNode.Attributes[ "type" ].Value );

            return config;
        }
    }
    #endregion PluginConfigurationSectionHandler

    #region Plugin loaders
    /// <summary>
    /// Strategry that specifies how a plugin is loaded
    /// </summary>
    interface IPluginLoader
    {
        /// <summary>
        /// Loads the plugin specified by its metadata
        /// </summary>
        /// <param name="metadata">plugin metadata</param>
        /// <returns></returns>
        IPlugin LoadPlugin( PluginMetadataAttribute metadata );
    }

    class PluginLoader : IPluginLoader
    {
        /// <summary>
        /// Creates an instance of the specified plugin type
        /// </summary>
        /// <param name="pluginTypeName">qualified type name of the plugin type</param>
        /// <returns><see cref="IPlugin"/>-compatible object instance 
        /// -OR-
        /// null if the type was not found</returns>
        protected virtual IPlugin createPluginInstance( string pluginTypeName )
        {
            Type pluginType = Type.GetType( pluginTypeName, false );

            if ( pluginType != null )
                return createPluginInstance( pluginType );
            else
                return null;
        }

        /// <summary>
        /// Creates an instance of the specified plugin type
        /// </summary>
        /// <param name="pluginType">plugin type</param>
        /// <returns><see cref="IPlugin"/>-compatible object instance 
        /// -OR-
        /// null if the type was not found</returns>
        protected virtual IPlugin createPluginInstance( Type pluginType )
        {
            if ( pluginType.GetInterface( "IPlugin" ) == null )
                Trace.Fail( "Plugin " + pluginType.FullName +
                    "does not implement IPlugin" );
            else
                return (IPlugin)Activator.CreateInstance( pluginType );

            return null;
        }

        public virtual IPlugin LoadPlugin( PluginMetadataAttribute metadata )
        {
            IPlugin plugin = createPluginInstance( metadata.TypeName );
            return plugin;
        }
    }

    class SingletonPluginLoader : PluginLoader
    {
        static List<string>
            _loadedSingletons = new List<string>();

        public override IPlugin LoadPlugin( PluginMetadataAttribute metadata )
        {
            if ( _loadedSingletons.Contains( metadata.Namespace ) )
                throw new PluginException( "Plugin {0} is already loaded and only one instance can exist",
                    metadata.Namespace );

            _loadedSingletons.Add( metadata.Namespace );

            return base.LoadPlugin( metadata );
        }
    }

    #endregion
}
