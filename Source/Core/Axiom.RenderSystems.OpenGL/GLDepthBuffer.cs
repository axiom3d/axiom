using System;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGL
{
	// TODO: implement
	internal class GLDepthBuffer : DepthBuffer
	{
		public GLContext GLContext;

		public GLDepthBuffer( PoolId poolId, GLRenderSystem renderSystem, GLContext creatorContext, GLRenderBuffer depth,
		                      GLRenderBuffer stencil, int width, int height, int fsaa, int multiSampleQuality, bool manual )
			: base( poolId, 0, width, height, fsaa, "", manual )
		{
		}
	}
}