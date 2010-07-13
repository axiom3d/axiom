using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom.RenderSystems.OpenGL
{
	internal class GLCopyingRenderTexture : GLRenderTexture
	{
		public GLCopyingRenderTexture( GLCopyingRTTManager manager, string name, GLSurfaceDesc target, bool writeGamma, int fsaa )
			: base( name, target, writeGamma, fsaa )
		{
		}

		public override object this[ string attribute ]
		{
			get
			{
				if ( attribute.ToLower() == "target" )
				{
					GLSurfaceDesc desc;
					desc.Buffer = this.pixelBuffer as GLHardwarePixelBuffer;
					desc.ZOffset = this.zOffset;
					return desc;
				}

				return null;
			}
		}
	}
}