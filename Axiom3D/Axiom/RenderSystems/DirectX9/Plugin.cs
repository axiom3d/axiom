using System;

using Axiom;

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// Summary description for Plugin.
    /// </summary>
    [PluginMetadata(Namespace = "/Axiom/RenderSystems/DirectX",
        Description="Axiom DirectX 9 Renderer")]
    public sealed class Plugin : IPlugin
    {
        #region Fields

        /// <summary>
        ///     Factory for HLSL programs.
        /// </summary>
        private HLSL.HLSLProgramFactory factory = new HLSL.HLSLProgramFactory();
        /// <summary>
        ///     Reference to the render system instance.
        /// </summary>
        private RenderSystem renderSystem = new D3D9RenderSystem();

        #endregion Fields

        #region Implementation of IPlugin

        public void Start()
        {
            // add an instance of this plugin to the list of available RenderSystems
            Root.Instance.RenderSystems.Add( "Direct3D9", renderSystem );

            // register the HLSL program manager
            HighLevelGpuProgramManagerSingleton.Instance.AddFactory( factory );
        }

        public void Stop()
        {
            // nothiing at the moment
            renderSystem.Shutdown();
        }

        #endregion
    }
}
