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
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Configuration;
using System.Collections;

namespace Axiom
{
    /// <summary>
    /// Plugin namespace extension
    /// </summary>
    public class PluginNamespaceExtender : INamespaceExtender
    {
        #region constants
        protected const string
            CURRENT_FOLDER = ".";
        protected const string
            AXIOM_CORE_DLL = "Axiom.dll";
        protected const string
            PLUIGIN_ASSEMBLYNAME_PREFIX = "Axiom.";
        #endregion constants

        #region protected fields - configuration, loaders, plugins
        /// <summary>
        /// Plugin manager configuration
        /// </summary>
        protected PluginManagerConfiguration managerConfig = null;
        
        /// <summary>
        /// List of plugin loaders
        /// </summary>
        protected Dictionary<Type, IPluginLoader> pluginLoaders =
            new Dictionary<Type, IPluginLoader>();

        /// <summary>
        /// Plugin registry
        /// </summary>
        protected HierarchicalRegistry<IPlugin> plugins =
            new HierarchicalRegistry<IPlugin>();
        #endregion

        #region construction
        public PluginNamespaceExtender()
        {
            // acquire configuration
            managerConfig = (PluginManagerConfiguration)
                ConfigurationManager.GetSection("Plugins");
            
            if (managerConfig == null)
                managerConfig = new PluginManagerConfiguration(CURRENT_FOLDER);

            // inspect assemblies for plugins
            preloadCoreAssembly();
            processAssemblies();

            // fill in plugin loaders
            pluginLoaders.Add(typeof(IPlugin), new PluginLoader());
            pluginLoaders.Add(typeof(ISingletonPlugin), new SingletonPluginLoader());

            // specify the plugin resolve method
            plugins.ObjectResolve +=
                new ObjectResolveEventHandler<string, IPlugin>(resolvePluginHandler);
        }
        #endregion

        #region how plugins are resolved
        void resolvePluginHandler(object sender, ObjectResolveEventArgs<string, IPlugin> e)
        {
            string pluginQualifiedName = e.Key; // e.g. /Axiom/RenderSystems/D3D9RenderSystemPlugin
            e.ResolvedObject = resolvePlugin(pluginQualifiedName);
        }

        /// <summary>
        /// Resolves an unloaded plugin
        /// </summary>
        /// <param name="pluginName">Qualified plugin name (/Axiom/RenderSystems/D3D9RenderSystem)</param>
        /// <returns><see cref="IPlugin"/>-compatible object 
        /// -OR- null if the plugin cannot be resolved</returns>
        protected virtual IPlugin resolvePlugin(string pluginName)
        {
            PluginMetadataAttribute metadata = null;

            if (!metadataStore.ContainsKey(pluginName))
                throw new PluginException("Metadata for plugin {0} is not found",
                    pluginName);

            metadata = metadataStore[pluginName];

            if (metadata.IsSingleton)
                return pluginLoaders[typeof(ISingletonPlugin)].LoadPlugin(metadata);
            else
                return pluginLoaders[typeof(IPlugin)].LoadPlugin(metadata);
        }
        #endregion how plugins are resolved

        #region plugin metadata implementation
        /// <summary>
        /// Stores metadata information for plugins
        /// </summary>
        protected Dictionary<string, PluginMetadataAttribute> metadataStore = new Dictionary<string, PluginMetadataAttribute>();

        #region private metadata helper methods
        private void preloadCoreAssembly()
        {
            Assembly.ReflectionOnlyLoadFrom(AXIOM_CORE_DLL);

            // wire other preload requests to satisfy lazy loaded
            // reflection only behaviour
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(loadAssemblyForInspection);
        }

        private Assembly loadAssemblyForInspection(object sender, ResolveEventArgs args)
        {
            return Assembly.ReflectionOnlyLoad(args.Name);
        }

        
        #endregion

        /// <summary>
        /// Allows to retrieve plugin metadata for the given namespace branch without
        /// loading them
        /// </summary>
        /// <param name="namespacePrefix">namespace to look for plugin metadata in</param>
        /// <returns>IEnumerable-compatible iterator</returns>
        public IEnumerable<PluginMetadataAttribute> PluginMetadata
        {
            get
            {
                IEnumerator<KeyValuePair<string, PluginMetadataAttribute>> enu =
                    metadataStore.GetEnumerator();

                while (enu.MoveNext())
                    yield return enu.Current.Value;
            }
        }

        /// <summary>
        /// Inspects the given assembly for the list of exported plugins
        /// </summary>
        /// <param name="assemblyPath">full path to the assembly file</param>
        /// <remarks>Assembly is loaded into the "reflection only" context thus
        /// making it impossible to execute code from that assembly. This 
        /// eliminates the discrepancy that all assemblies are loaded into
        /// the exection context upon Axiom initialization</remarks>
        protected void inspectAssemblyForPlugins(string assemblyPath)
        {
            Assembly asm = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            Type[] types = asm.GetExportedTypes();

            foreach (Type type in types)
            {
                if (type.GetInterface("IPlugin") != null)
                {
                    IList<CustomAttributeData> attrs =
                        CustomAttributeData.GetCustomAttributes(type);

                    if (attrs.Count == 1)
                    {
                        PluginMetadataAttribute metadata =
                            PluginMetadataAttribute.ReflectionOnlyConstructor(attrs[0].NamedArguments);

                        if (metadata.IsSingleton && type.GetInterface("ISingletonPlugin") == null)
                            throw new PluginException("Plugin {0} is declared as singleton but does not implement ISingletonPlugin",
                                metadata.Name);

                        metadata.TypeName = type.AssemblyQualifiedName;

                        // just in case
                        if(metadata.Name != string.Empty)
                            metadataStore.Add(metadata.Name, metadata);
                    }
                }
            }
        }

        #endregion

        #region assembly processing implementation
        private bool _isInitialized = false;
        void processAssemblies()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            string[] files = Directory.GetFiles(managerConfig.PluginFolder,
                    "*.dll");

            foreach (string fileName in files)
            {
                if (fileName.IndexOf(AXIOM_CORE_DLL) == -1
                    && fileName.IndexOf(PLUIGIN_ASSEMBLYNAME_PREFIX) != -1)
                {
                    try
                    {
                        string fullPath = Path.GetFullPath(fileName);
                        inspectAssemblyForPlugins(fullPath);
                    }
                    catch (BadImageFormatException)
                    {
                        // eat, not a valid assembly
                    }
                }
            }
        }
        #endregion

        #region getting plugins by namespace
        /// <summary>
        /// Returns a plugin by its qualified name
        /// </summary>
        /// <param name="pluginQualifiedName">short name</param>
        /// <returns>IPlugin-compatible object</returns>
        public IPlugin GetPlugin(string pluginQualifiedName)
        {
            return plugins[pluginQualifiedName];
        }

        /// <summary>
        /// Returns all plugins that are registered under the given namespace branch
        /// </summary>
        /// <param name="namespacePrefix">namespace to look for plugins in</param>
        /// <returns>IEnumerable-compatible iterator</returns>
        public IEnumerable<IPlugin> GetPlugins(string namespacePrefix)
        {
            return plugins.Subtree<IPlugin>(namespacePrefix);
        }
        #endregion

        #region INamespaceExtender implementation
        public IEnumerable<K> Subtree<K>()
        {
            IEnumerator enu = plugins.GetEnumerator();

            while (enu.MoveNext())
                yield return (K)(enu.Current);
        }

        const string PLUGIN_NAMESPACE = "/Axiom/Plugins/";

        public string Namespace
        {
            get
            {
                return PLUGIN_NAMESPACE;
            }
        }

        public K GetObject<K>(string objectName)
        {
            return (K)((object)plugins[objectName]);
        }
        #endregion
    }
}
