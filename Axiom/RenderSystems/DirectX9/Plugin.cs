using System;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.RenderSystems.DirectX9 {
	/// <summary>
	/// Summary description for Plugin.
	/// </summary>
	public class Plugin : IPlugin {
		#region Implementation of IPlugin

		protected HLSL.HLSLProgramFactory factory = new HLSL.HLSLProgramFactory();

		public void Start() {
			// add an instance of this plugin to the list of available RenderSystems
			Root.Instance.RenderSystems.Add("Direct3D9", new D3D9RenderSystem());

			// register the HLSL program manager
			HighLevelGpuProgramManager.Instance.AddFactory(factory);
		}

		public void Stop() {
			// nothiing at the moment
		}

		#endregion
	}
}
