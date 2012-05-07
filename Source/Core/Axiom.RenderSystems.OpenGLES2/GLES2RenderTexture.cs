using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2SurfaceDesc
	{
		public GLES2HardwarePixelBuffer buffer = null;
		public int zoffset = 0;
		public int numSamples = 0;

		public GLES2SurfaceDesc( GLES2HardwarePixelBuffer buffer, int zoffset, int numSamples )
		{
			this.buffer = buffer;
			this.zoffset = zoffset;
			this.numSamples = numSamples;
		}

		public GLES2SurfaceDesc() {}
	}

	internal class GLES2RenderTexture : RenderTexture
	{
		public GLES2RenderTexture( string name, GLES2SurfaceDesc target, bool writeGamma, int fsaa )
			: base( target.buffer, target.zoffset )
		{
			base.name = name;
			hwGamma = writeGamma;
			base.fsaa = fsaa;
		}

		public override object this[ string attribute ]
		{
			get
			{
				if ( attribute == "TARGET" )
				{
					var target = (GLES2SurfaceDesc) base[ attribute ];
					target.zoffset = zOffset;
					target.buffer = (GLES2HardwarePixelBuffer) pixelBuffer;
					return target;
				}
				return base[ attribute ];
			}
		}

		public override bool RequiresTextureFlipping
		{
			get { return true; }
		}
	}
}
