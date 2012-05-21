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

using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using Glenum = OpenTK.Graphics.ES20.All;
using PixelFormat = Axiom.Media.PixelFormat;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	public class GLES2RenderBuffer : GLES2HardwarePixelBuffer
	{
		private int renderBufferID;

		public GLES2RenderBuffer( All format, int width, int height, int numSamples )
			: base( width, height, 1, GLES2PixelUtil.GetClosestAxiomFormat( format, (All)PixelFormat.A8R8G8B8 ), BufferUsage.WriteOnly )
		{
			GlInternalFormat = format;
			//Genearte renderbuffer
			GL.GenRenderbuffers( 1, ref this.renderBufferID );
			GLES2Config.GlCheckError( this );
			//Bind it to FBO
			GL.BindRenderbuffer( All.Renderbuffer, this.renderBufferID );
			GLES2Config.GlCheckError( this );

			//Allocate storage for depth buffer
			if ( numSamples > 0 ) {}
			else
			{
				GL.RenderbufferStorage( All.Renderbuffer, format, width, height );
				GLES2Config.GlCheckError( this );
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			GL.DeleteRenderbuffers( 1, ref this.renderBufferID );
			GLES2Config.GlCheckError( this );
			base.dispose( disposeManagedResources );
		}

		public override void BindToFramebuffer( All attachment, int zoffset )
		{
			GL.FramebufferRenderbuffer( All.Framebuffer, attachment, All.Renderbuffer, this.renderBufferID );
			GLES2Config.GlCheckError( this );
		}
	}
}