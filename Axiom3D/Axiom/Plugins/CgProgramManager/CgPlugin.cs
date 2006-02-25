using System;

using Axiom;

namespace Axiom.CgPrograms
{
    /// <summary>
    ///    Main plugin class.
    /// </summary>
    [PluginMetadata(Namespace = "/Axiom/Plugins/CgProgramManager")]
    public class CgPlugin : IPlugin
    {
        private CgProgramFactory factory;

        /// <summary>
        ///    Called when the plugin is started.
        /// </summary>
        public void Start()
        {
            // register our Cg Program Factory
            factory = new CgProgramFactory();

            HighLevelGpuProgramManagerSingleton.Instance.AddFactory( factory );
        }

        /// <summary>
        ///    Called when the plugin is stopped.
        /// </summary>
        public void Stop()
        {
            //factory.Dispose();
        }
    }
}
