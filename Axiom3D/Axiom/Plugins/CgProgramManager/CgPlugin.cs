using System;

using Axiom;

namespace Axiom.CgPrograms
{
    /// <summary>
    ///    Main plugin class.
    /// </summary>
    [PluginMetadata(Name = "CgProgramManager", 
        Subsystem=typeof(HighLevelGpuProgramManager))]
    public class CgPlugin : IPlugin
    {
        private CgProgramFactory factory;
        private bool _isStarted = false;

        public bool IsStarted
        {
            get { return _isStarted; }
        }

        /// <summary>
        ///    Called when the plugin is started.
        /// </summary>
        public void Start()
        {
            // register our Cg Program Factory
            factory = new CgProgramFactory();

            HighLevelGpuProgramManager.Instance.AddFactory( factory );
            _isStarted = true;
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
