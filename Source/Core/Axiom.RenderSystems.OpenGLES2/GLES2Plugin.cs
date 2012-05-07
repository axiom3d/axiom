using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2Plugin : IPlugin
	{
		private RenderSystem _renderSystem;

		public void Initialize()
		{
			this._renderSystem = new GLES2RenderSystem();

			Root.Instance.RenderSystems.Add( this._renderSystem );
		}

		public void Shutdown()
		{
			this._renderSystem.Dispose();
			this._renderSystem = null;
		}
	}
}
