using System;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGL
{
    // TODO: implement
    internal class GLDepthBuffer : DepthBuffer
    {
        public GLContext GLContext;

        public GLDepthBuffer( PoolId poolId, ushort bitDepth, int width, int height, int fsaa, string fsaaHint, bool manual ) 
            : base( poolId, bitDepth, width, height, fsaa, fsaaHint, manual )
        {
        }
    }
}
