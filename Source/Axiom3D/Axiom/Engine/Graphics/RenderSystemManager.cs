#region Namespace Declarations
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace Axiom
{
    /// <summary>
    /// Manages available render systems
    /// </summary>
    [Subsystem("RenderSystemManager")]
    public class RenderSystemManager : ISubsystem
    {
        #region protected members
        /// <summary>
        /// Namespace extender
        /// </summary>
        protected RenderSystemNamespaceExtender renderNamespace = null;
        #endregion

        #region initialization
        /// <summary>
        /// Initalizes the RenderSystemManager
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            // prevent double initialization
            if (_isInitialized)
                return true;

            // register the render system namespace
            renderNamespace = new RenderSystemNamespaceExtender();
            Vfs.Instance.RegisterNamespace(renderNamespace);

            // request RenderSystemManager plugins...
            List<PluginMetadataAttribute> plugins =
                PluginManager.Instance.RequestSubsystemPlugins(this);

            // ... and initialize them
            foreach (PluginMetadataAttribute pluginData in plugins)
            {
                IPlugin plugin = PluginManager.Instance.GetPlugin(pluginData.Name);
                if (!plugin.IsStarted)
                    plugin.Start();
            }

            _isInitialized = true;

            return true;
        }

        private bool _isInitialized = false;
        /// <summary>
        /// Checks whether the render system manager is already initialized
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                return _isInitialized;
            }
        }
        #endregion

        #region Singleton implementation
        private static RenderSystemManager instance = null;

        internal RenderSystemManager()
        {
            if (instance == null)
            {
                instance = this;
                instance.Initialize();
            }
        }

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static RenderSystemManager Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

    }
}
