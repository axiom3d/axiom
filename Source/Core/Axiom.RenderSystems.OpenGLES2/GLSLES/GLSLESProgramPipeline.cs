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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;

using Axiom.Graphics;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
	/// <summary>
	/// This class is used for when a Vertex and Fragment shader can stand independent of each other, which is not the standard for OpenGLES
	/// In fact, OpenTK doesn't have this ability set in as of yet, so this class should never be used.
	/// However, it is partially filled out as much as possible to allow easy completion supposing OpenTK ever opens access to this ability
	/// </summary>
	internal class GLSLESProgramPipeline : GLSLESProgramCommon
	{
		protected int glProgramPipelineHandle;

		protected enum Linked
		{
			VertexProgram = 0x01,
			FragmentProgram = 0x10
		}

		public GLSLESProgramPipeline( GLSLESGpuProgram vertexProgram, GLSLESGpuProgram fragmentProgram )
			: base( vertexProgram, fragmentProgram )
		{
			throw new Core.AxiomException( "This class should never be instantied, use GLSLESLinkProgram instead" );
		}

		~GLSLESProgramPipeline() {}

		protected override void CompileAndLink()
		{
			int linkStatus = 0;

			//Compile and attach vertex program
			if ( vertexProgram != null && !vertexProgram.IsLinked )
			{
				if ( !vertexProgram.GLSLProgram.Compile() )
				{
					triedToLinkAndFailed = true;
					return;
				}

				int programHandle = vertexProgram.GLSLProgram.GLProgramHandle;
				//GL.ProgramParameter(programHandle, GLenum.LinkStatus, ref linkStatus);
				vertexProgram.GLSLProgram.AttachToProgramObject( programHandle );
				GL.LinkProgram( programHandle );
				GLES2Config.GlCheckError( this );
				GL.GetProgram( programHandle, GLenum.LinkStatus, ref linkStatus );
				GLES2Config.GlCheckError( this );

				if ( linkStatus != 0 )
				{
					vertexProgram.IsLinked = true;
					linked |= (int) Linked.VertexProgram;
				}
				bool bLinkStatus = ( linkStatus != 0 );
				triedToLinkAndFailed = !bLinkStatus;

				SkeletalAnimationIncluded = vertexProgram.IsSkeletalAnimationIncluded;
			}

			//Compile and attach Fragment program
			if ( fragmentProgram != null && !fragmentProgram.IsLinked )
			{
				if ( !fragmentProgram.GLSLProgram.Compile( true ) )
				{
					triedToLinkAndFailed = true;
					return;
				}

				int programHandle = fragmentProgram.GLSLProgram.GLProgramHandle;
				//GL.ProgramParameter(programHandle, GLenum.ProgramSeperableExt, true);
				fragmentProgram.GLSLProgram.AttachToProgramObject( programHandle );
				GL.LinkProgram( programHandle );
				GLES2Config.GlCheckError( this );
				GL.GetProgram( programHandle, GLenum.LinkStatus, ref linkStatus );
				GLES2Config.GlCheckError( this );

				if ( linkStatus != 0 )
				{
					fragmentProgram.IsLinked = true;
					linked |= (int) Linked.FragmentProgram;
				}
				triedToLinkAndFailed = !fragmentProgram.IsLinked;
			}

			if ( linked != 0 ) {}
		}

		protected override void _useProgram() {}

		public override void Activate()
		{
			throw new NotImplementedException();
		}

		public override void UpdateUniforms( Graphics.GpuProgramParameters parms, int mask, Graphics.GpuProgramType fromProgType )
		{
			throw new NotImplementedException();
		}

		public override void UpdatePassIterationUniforms( Graphics.GpuProgramParameters parms )
		{
			throw new NotImplementedException();
		}

		public override int GetAttributeIndex( Graphics.VertexElementSemantic semantic, int index )
		{
			int res = customAttribues[ (int) semantic - 1, index ];
			if ( res == NullCustomAttributesIndex )
			{
				int handle = vertexProgram.GLSLProgram.GLProgramHandle;
				string attString = GetAttributeSemanticString( semantic );
				int attrib = GL.GetAttribLocation( handle, attString );
				GLES2Config.GlCheckError( this );

				if ( attrib == NotFoundCustomAttributesIndex && semantic == VertexElementSemantic.Position )
				{
					attrib = GL.GetAttribLocation( handle, "position" );
					GLES2Config.GlCheckError( this );
				}

				if ( attrib == NotFoundCustomAttributesIndex )
				{
					string attStringWithSemantic = attString + index.ToString();
					attrib = GL.GetAttribLocation( handle, attStringWithSemantic );
					GLES2Config.GlCheckError( this );
				}

				customAttribues[ (int) semantic - 1, index ] = attrib;
				res = attrib;
			}
			return res;
		}

		protected virtual void ExtractLayoutQualifiers() {}

		public int GLProgramPipelineHandle
		{
			get { return this.glProgramPipelineHandle; }
		}
	}
}
