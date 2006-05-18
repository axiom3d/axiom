using System;

using Axiom;

namespace Axiom.RenderSystems.OpenGL
{
    /// <summary>
    /// Summary description for Plugin.
    /// </summary>
    [PluginMetadata(Name = "OpenGL", Description = "Axiom OpenGL Renderer", 
        Subsystem = typeof(RenderSystemManager))]
    public sealed class Plugin : IPlugin
    {
        #region Implementation of IPlugin

        /// <summary>
        ///     Reference to a GLSL program factory.
        /// </summary>
        private GLSL.GLSLProgramFactory factory = new GLSL.GLSLProgramFactory();
        /// <summary>
        ///     Reference to the render system instance.
        /// </summary>
        private GLRenderSystem renderSystem = new GLRenderSystem();

        private bool _isStarted = false;

        public bool IsStarted
        {
            get { return _isStarted; }
        }

        public void Start()
        {
            RenderSystemNamespaceExtender renderNamespace = (RenderSystemNamespaceExtender)
                Vfs.Instance["/Axiom/RenderSystems/"];

            // add an instance of this plugin to the list of available RenderSystems
            renderNamespace.RegisterRenderSystem("OpenGL", renderSystem);

            HighLevelGpuProgramManager.Instance.AddFactory( factory );
            _isStarted = true;
        }

        public void Stop()
        {
            factory.Dispose();
            renderSystem.Shutdown();
        }

        #endregion Implementation of IPlugin
    }
}
