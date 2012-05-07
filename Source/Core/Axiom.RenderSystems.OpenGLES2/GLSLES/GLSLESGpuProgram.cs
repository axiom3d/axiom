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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
	internal class GLSLESGpuProgram : GLES2GpuProgram
	{
		private readonly GLSLESProgram glslProgram;

		private static int VertexShaderCount = 0;
		private static int FragmentShaderCount = 0;

		private int linked;

		public GLSLESGpuProgram( GLSLESProgram parent )
			: base( parent.Creator, parent.Name, parent.Handle, parent.Group, false, null )
		{
			this.glslProgram = parent;

			type = parent.Type;
			syntaxCode = "glsles";

			this.linked = 0;

			if ( parent.Type == Graphics.GpuProgramType.Vertex )
			{
				_programID = ++VertexShaderCount;
			}
			else if ( parent.Type == Graphics.GpuProgramType.Fragment )
			{
				_programID = ++FragmentShaderCount;
			}

			isSkeletalAnimationIncluded = this.glslProgram.IsSkeletalAnimationIncluded;
			LoadFromFile = false;
		}

		~GLSLESGpuProgram()
		{
			this.unload();
		}

		public override void BindProgram()
		{
			switch ( type )
			{
				case Axiom.Graphics.GpuProgramType.Vertex:
					GLSLESLinkProgramManager.Instance.ActiveVertexShader = this;
					break;
				case Axiom.Graphics.GpuProgramType.Fragment:
					GLSLESLinkProgramManager.Instance.ActiveFragmentShader = this;
					break;
				case Axiom.Graphics.GpuProgramType.Geometry:
				default:
					break;
			}
		}

		public override void UnbindProgram()
		{
			if ( type == Graphics.GpuProgramType.Vertex )
			{
				GLSLESLinkProgramManager.Instance.ActiveVertexShader = null;
			}
			else if ( type == Graphics.GpuProgramType.Fragment )
			{
				GLSLESLinkProgramManager.Instance.ActiveFragmentShader = null;
			}
		}

		public override void BindProgramParameters( Graphics.GpuProgramParameters parms, uint mask )
		{
			//Link can throw exceptions, ignore them at this pioont
			try
			{
				GLSLESLinkProgram linkProgram = GLSLESLinkProgramManager.Instance.ActiveLinkProgram;
				linkProgram.UpdateUniforms( parms, (int) mask, type );
			}
			catch {}
		}

		public override void BindProgramPassIterationParameters( Graphics.GpuProgramParameters parms )
		{
			GLSLESLinkProgram linkProgram = GLSLESLinkProgramManager.Instance.ActiveLinkProgram;
			linkProgram.UpdatePassIterationUniforms( parms );
		}

		public GLSLESProgram GLSLProgram
		{
			get { return this.glslProgram; }
		}

		public bool IsLinked
		{
			get { return this.linked != 0; }
			set { this.linked = value == true ? 1 : 0; }
		}

		protected override void LoadFromSource()
		{
			//nothing to load
		}

		protected override void unload()
		{
			//nothing to unload
		}

		protected override void load()
		{
			//nothing to load
		}
	}
}
