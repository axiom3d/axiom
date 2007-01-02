using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Forms;
using Crownwood.Magic.Docking;

using Axiom.Math.Collections;

namespace Chronos.Core
{
    public class PluginInfo
    {
        public string Path;
        public IEditorPlugin Instance;
    }

    public class PluginManager : IDisposable
    {
        #region Singleton Pattern

        private static PluginManager _Instance;
		
        public static PluginManager Instance {
			get {
                if(_Instance == null) {
    			    _Instance = new PluginManager();
                }
				return _Instance; 
			}
		}

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // This class doesn't access any unmanaged resources,
            // so there is not to dispose. The assemblies don't count
            // since we can't unload them from the AppDomain any way.
        }

        #endregion

        /// <summary>
        /// Maps plugin keys to PluginInfos.
        /// </summary>
        private SortedList _Plugins;

        /// <summary>
        /// Maps plugin keys to serialization handler types.
        /// </summary>
        private SortedList _XmlHandlers;

        private PluginManager() 
		{
            _Plugins = new SortedList();
            _XmlHandlers = new SortedList();
		}

        /// <summary>
        /// Loads a plugin.
        /// </summary>
        /// <param name="pluginName"></param>
        /// <param name="fileName"></param>
        /// <param name="typeName"></param>
        /// <param name="throwOnError">If false, the method returns null if an exception is thrown.</param>
        /// <returns></returns>
        public IEditorPlugin Load(string fileName, string typeName, bool throwOnError)
        {
            PluginInfo info;

            try {
                // Ensure the path exists before wasting time trying to load it.
                 if(!File.Exists(fileName)) {
                    string message = "Plugin file not found.";
                    throw new FileNotFoundException(message, fileName);
                }

                // Load the assembly presumably containing the plugin.
		        Assembly pluginAssembly = Assembly.LoadFile(fileName);

                // Get and check the plugin type.
		        Type type = pluginAssembly.GetType(typeName, false, false);
                if(type == null) {
                    string message = "Bad or missing type, " + typeName + ".";
                    throw new ArgumentException(message, "typeName");
                }

		        if(type.GetInterface("IEditorPlugin") == null) {
                    string message = "The type, " + typeName + ", does not implement the IEditorPlugin interface.";
                    throw new ArgumentException(message, "typeName");
                }

                // Create an instance of the plugin by accessing its nonpublic or public default constructor.
                // Non-public constructors are included so that plugins can follow a Singleton strategy in their
                // implementation.
                IEditorPlugin plugin;
                try {
                    BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			        plugin = Activator.CreateInstance(type, flags, null, null, null) as IEditorPlugin;
                } catch(Exception e) {
                    string message = "The default constructor does not exist, or threw an exception.";
                    throw new Exception(message, e);
                }

                // Update the plugin map.
                info = new PluginInfo();
                info.Path = fileName;
                info.Instance = plugin;
                
                // This throws if a plugin with the given name already exists.
				string name = plugin.GetType().ToString();
                _Plugins.Add(name, info);

                // Update the handler map.
                IXmlWriterPlugin xmlPlugin = plugin as IXmlWriterPlugin;
                if(xmlPlugin != null) {
                    StringCollection list = xmlPlugin.XmlElementHandlers;
                    foreach(string key in list) {
                        if(!_XmlHandlers.ContainsKey(key)) {
                            _XmlHandlers.Add(key, name);
                        } else {
                            // IMHO, duplicate registration really isn't a problem
                            // which should force the plugin to fail loading. So,
                            // we'll just log the error instead, and continue as 
                            // if this wasn't an IXmlWriterPlugin.

                            string message = "The plugin, {0}, cannot handle serialization for the element, {1}, because the plugin, {2}, already is.";
                            message = string.Format(message, name, key, (string) _XmlHandlers[key]);

                            System.Diagnostics.Trace.Write(message);

                            //_Plugins.Remove(plugin.Name);
                            //throw new Exception(message);
                        }
                    }
                }
            } catch (Exception e) {
                if(!throwOnError) {
                    return null;
                } else {
                    string message = "Unable to load a plugin from the file, {0}.";
                    throw new Exception(string.Format(message, fileName), e);
                }
            }

            return info.Instance;
        }

        /// <summary>
        /// Removes a PluginInfo from the manager.
        /// </summary>
        /// <param name="pluginName"></param>
        public void Remove(string pluginName)
        {
            if(_Plugins.ContainsKey(pluginName)) {
                int index = _XmlHandlers.IndexOfValue(pluginName);
                if(index != -1) {
                    _XmlHandlers.RemoveAt(index);
                }
                _Plugins.Remove(pluginName);
            }
        }

        /// <summary>
        /// Gets a plugin by the serialization type it handles.
        /// </summary>
        /// <param name="handlerType"></param>
        /// <returns></returns>
        public IXmlWriterPlugin GetByHandler(string handlerType)
        {
            if(_XmlHandlers.ContainsKey(handlerType)) {
                string pluginName = (string) _XmlHandlers[handlerType];
                PluginInfo info = _Plugins[pluginName] as PluginInfo;
                return info.Instance as IXmlWriterPlugin;
            }
            return null;
        }

        /// <summary>
        /// Gets a plugin by name, or null if it's not in the list.
        /// </summary>
        public IEditorPlugin this[string pluginName] {
            get {
                if(_Plugins.ContainsKey(pluginName)) {
                    PluginInfo info = _Plugins[pluginName] as PluginInfo;
                    return info.Instance;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the list of elements which can be serialized by one
        /// of the current plugins.
        /// </summary>
        public string[] HandlerTypes {
            get { 
                string[] list = new string[_XmlHandlers.Count];
                _XmlHandlers.Keys.CopyTo(list, 0);
                return list; 
            }
        }
    }
}
