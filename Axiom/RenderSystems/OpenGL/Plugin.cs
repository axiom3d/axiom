using System;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGL {
	/// <summary>
	/// Summary description for Plugin.
	/// </summary>
	public class Plugin : IPlugin {
		#region Implementation of IPlugin

		protected GLSL.GLSLProgramFactory factory = new GLSL.GLSLProgramFactory();

		public void Start() {
			// add an instance of this plugin to the list of available RenderSystems
			Root.Instance.RenderSystems.Add("OpenGL", new GLRenderSystem());

			HighLevelGpuProgramManager.Instance.AddFactory(factory);
		}

		public void Stop() {
		}

		#endregion Implementation of IPlugin
	}
}
