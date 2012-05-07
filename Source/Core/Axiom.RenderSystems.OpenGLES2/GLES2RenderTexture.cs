#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Graphics;

#endregion Namespace Declarations
			
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
