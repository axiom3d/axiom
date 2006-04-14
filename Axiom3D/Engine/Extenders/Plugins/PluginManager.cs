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
using System.Collections;
#endregion

namespace Axiom
{
    /// <summary>
    /// Axiom plugin manager 
    /// </summary>
    public class PluginManager : IDisposable
    {
        protected const string
            CURRENT_FOLDER = ".";

        #region Singleton implementation

        /// <summary>
        ///     Singleton instance of this class.
        /// </summary>
        private static PluginManager _instance = null;

        /// <summary>
        /// Private constructor.  This class cannot be instantiated by any third party.
        /// </summary>
        internal PluginManager()
        {
            if (_instance == null)
                _instance = this;

            managerConfig = (PluginManagerConfiguration)
                ConfigurationManager.GetSection("Plugins");

            Vfs.Instance.RegisterNamespace(new PluginNamespaceExtender());

            if (managerConfig == null)
                managerConfig = new PluginManagerConfiguration(CURRENT_FOLDER);
        }

        /// <summary>
        /// Gets the singleton instance of this class.
        /// </summary>
        public static PluginManager Instance
        {
            get
            {
                return _instance;
            }
        }

        #endregion Singleton implementation

        #region protected class members 
        /// <summary>
        /// Plugin manager configuration
        /// </summary>
        protected PluginManagerConfiguration managerConfig = null;

        #endregion

        #region getting plugin references by namespace and subsystem name
        /// <summary>
        /// Returns a plugin by its qualified name
        /// </summary>
        /// <param name="pluginQualifiedName">either short or namespace qualified 
        /// name in /X/Y/Z... format</param>
        /// <returns>IPlugin-compatible object</returns>
        public IPlugin GetPlugin(string pluginName)
        {
            string lookup = pluginName.IndexOf("/") > -1 ? pluginName :
                "/Axiom/Plugins/" + pluginName;

            return (IPlugin)Vfs.Instance[lookup];
        }

        /// <summary>
        /// Returns the list of plugins for a specific namespace
        /// </summary>
        /// <param name="subsystemName"></param>
        /// <returns></returns>
        public List<PluginMetadataAttribute> RequestSubsystemPlugins(object subsystem)
        {
            List<PluginMetadataAttribute> result = new List<PluginMetadataAttribute>();

            string subsName = subsystems[subsystem.GetType()].Name;
            PluginNamespaceExtender plugins = 
                (PluginNamespaceExtender)Vfs.Instance["/Axiom/Plugins/"];

            foreach(PluginMetadataAttribute meta in plugins.PluginMetadata)
                if(meta.Subsystem != null && meta.Subsystem.FullName == subsystem.GetType().FullName)
                    result.Add(meta);

            return result;
        }


        #endregion getting plugin references by namespace
        
        #region initialization
        static Dictionary<Type, SubsystemAttribute> subsystems = 
            new Dictionary<Type,SubsystemAttribute>();
        /// <summary>
        /// Initializes static data for the PluginManager
        /// </summary>
        static PluginManager()
        {
            // parse all types in the engine to extract subsystem metadata from
            // them
            foreach (Type typ in Assembly.GetExecutingAssembly().GetExportedTypes())
            {
                object[] attrs = typ.GetCustomAttributes(typeof(SubsystemAttribute), true);

                if (attrs != null && attrs.Length > 0)
                {
                    SubsystemAttribute subs = (SubsystemAttribute)attrs[0];
                    subsystems.Add(typ, subs);
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
        public void LoadPlugins()
        {
            if (managerConfig.Plugins.Count > 0)
            {
                foreach (string pluginTypeName in managerConfig.Plugins)
                {
                    IPlugin plugin = (IPlugin)Vfs.Instance[pluginTypeName];

                    //// load only the unloaded plugins
                    if(!plugin.IsStarted)
                        plugin.Start();
                }
            }
        }
        #endregion initialization

        #region IDisposable Members

        public void Dispose()
        {
            // TODO: implement "register for dispose" registry stub
        }

        #endregion
    }
}
