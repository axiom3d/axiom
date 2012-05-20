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

using Axiom.Scripting;

using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;

using Axiom.Graphics;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
	internal class GLSLESLinkProgram : GLSLESProgramCommon
	{
		public GLSLESLinkProgram( GLSLESGpuProgram vertexProgram, GLSLESGpuProgram fragmentProgram )
			: base( vertexProgram, fragmentProgram )
		{
			if ( vertexProgram == null || fragmentProgram == null )
			{
				throw new Core.AxiomException( "Attempted to create a shader program without both a vertex and fragment program." );
			}
		}

		~GLSLESLinkProgram()
		{
			GL.DeleteProgram( glProgramHandle );
			GLES2Config.GlCheckError( this );
		}

		protected override void CompileAndLink()
		{
			//Compile and attach vertex program
			if ( !vertexProgram.GLSLProgram.Compile( true ) )
			{
				triedToLinkAndFailed = true;
				return;
			}
			vertexProgram.GLSLProgram.AttachToProgramObject( glProgramHandle );
			SkeletalAnimationIncluded = vertexProgram.IsSkeletalAnimationIncluded;

			//Compile and attach fragment program
			if ( !fragmentProgram.GLSLProgram.Compile( true ) )
			{
				triedToLinkAndFailed = true;
				return;
			}
			fragmentProgram.GLSLProgram.AttachToProgramObject( glProgramHandle );

			//The link
			GL.LinkProgram( glProgramHandle );
			GLES2Config.GlCheckError( this );
			GL.GetProgram( glProgramHandle, GLenum.LinkStatus, ref linked );
			GLES2Config.GlCheckError( this );

			triedToLinkAndFailed = ( linked == 0 ) ? true : false;
		}

		protected override void BuildGLUniformReferences()
		{
			if ( !uniformRefsBuilt )
			{
				Axiom.Graphics.GpuProgramParameters.GpuConstantDefinitionMap vertParams = null;
				Axiom.Graphics.GpuProgramParameters.GpuConstantDefinitionMap fragParams = null;

				if ( vertexProgram != null )
				{
					vertParams = vertexProgram.GLSLProgram.ConstantDefinitions.Map;
				}
				if ( fragmentProgram != null )
				{
					fragParams = fragmentProgram.GLSLProgram.ConstantDefinitions.Map;
				}

				GLSLESLinkProgramManager.Instance.ExtractUniforms( glProgramHandle, vertParams, fragParams, glUniformReferences );

				uniformRefsBuilt = true;
			}
		}

		protected override void _useProgram()
		{
			if ( linked != 0 )
			{
				GL.UseProgram( glProgramHandle );
				GLES2Config.GlCheckError( this );
			}
		}

		public override void Activate()
		{
			if ( linked == 0 && !triedToLinkAndFailed )
			{
				GL.GetError();

				glProgramHandle = GL.CreateProgram();
				GLES2Config.GlCheckError( this );
#if !AXIOM_NO_GLES2_GLSL_OPTIMIZER
				if ( vertexProgram != null )
				{
					string paramStr = vertexProgram.GLSLProgram.GetParameter[ "use_optimiser" ];
					if ( paramStr == "true" || paramStr.Length == 0 )
					{
						GLSLESLinkProgramManager.Instance.OptimizeShaderSource( vertexProgram );
					}
				}
				if ( vertexProgram != null )
				{
					string paramStr = fragmentProgram.GLSLProgram.GetParameter( "use_optimiser" );
					if ( paramStr == "true" || paramStr.Length == 0 )
					{
						GLSLESLinkProgramManager.Instance.OptimizeShaderSource( fragmentProgram );
					}
				}
#endif
				this.CompileAndLink();

				this.ExtractLayoutQualifiers();
				this.BuildGLUniformReferences();
			}
			this._useProgram();
		}

		public override void UpdateUniforms( Graphics.GpuProgramParameters parms, int mask, Graphics.GpuProgramType fromProgType )
		{
			foreach ( var currentUniform in glUniformReferences )
			{
				//Only pull values from buffer it's supposed to be in (vertex or fragment)
				//This method will be called twice, once for vertex program params,
				//and once for fragment program params.
				if ( fromProgType == currentUniform.SourceProgType )
				{
					var def = currentUniform.ConstantDef;
					if ( ( (int) def.Variability & mask ) != 0 )
					{
						int glArraySize = def.ArraySize;

						switch ( def.ConstantType )
						{
							case GpuProgramParameters.GpuConstantType.Float1:
								unsafe
								{
									GL.Uniform1( currentUniform.Location, glArraySize, parms.GetFloatPointer( def.PhysicalIndex ).Pointer.ToFloatPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Float2:
								unsafe
								{
									GL.Uniform2( currentUniform.Location, glArraySize, parms.GetFloatPointer( def.PhysicalIndex ).Pointer.ToFloatPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Float3:
								unsafe
								{
									GL.Uniform3( currentUniform.Location, glArraySize, parms.GetFloatPointer( def.PhysicalIndex ).Pointer.ToFloatPointer() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Float4:
								unsafe
								{
									GL.Uniform4( currentUniform.Location, glArraySize, parms.GetFloatPointer( def.PhysicalIndex ).Pointer.ToFloatPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_2X2:
								unsafe
								{
									GL.UniformMatrix2( currentUniform.Location, glArraySize, false, parms.GetFloatPointer( def.PhysicalIndex ).Pointer.ToFloatPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_3X3:
								unsafe
								{
									GL.UniformMatrix3( currentUniform.Location, glArraySize, false, parms.GetFloatPointer( def.PhysicalIndex ).Pointer.ToFloatPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_4X4:
								unsafe
								{
									GL.UniformMatrix4( currentUniform.Location, glArraySize, false, parms.GetFloatPointer( def.PhysicalIndex ).Pointer.ToFloatPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Int1:
								unsafe
								{
									GL.Uniform1( currentUniform.Location, glArraySize, parms.GetIntPointer( def.PhysicalIndex ).Pointer.ToIntPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Int2:
								unsafe
								{
									GL.Uniform2( currentUniform.Location, glArraySize, parms.GetIntPointer( def.PhysicalIndex ).Pointer.ToIntPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Int3:
								unsafe
								{
									GL.Uniform3( currentUniform.Location, glArraySize, parms.GetIntPointer( def.PhysicalIndex ).Pointer.ToIntPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Int4:
								unsafe
								{
									GL.Uniform4( currentUniform.Location, glArraySize, parms.GetIntPointer( def.PhysicalIndex ).Pointer.ToIntPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Sampler1D:
							case GpuProgramParameters.GpuConstantType.Sampler1DShadow:
							case GpuProgramParameters.GpuConstantType.Sampler2D:
							case GpuProgramParameters.GpuConstantType.Sampler2DShadow:
							case GpuProgramParameters.GpuConstantType.Sampler3D:
							case GpuProgramParameters.GpuConstantType.SamplerCube:
								//samplers handled like 1-elemnt ints
								unsafe
								{
									GL.Uniform1( currentUniform.Location, 1, parms.GetIntPointer( def.PhysicalIndex ).Pointer.ToIntPointer() );
									GLES2Config.GlCheckError( this );
								}
								break;
						}
					}
				}
			}
		}

		public override void UpdatePassIterationUniforms( Graphics.GpuProgramParameters parms )
		{
			if ( parms.HasPassIterationNumber )
			{
				int index = parms.PassIterationNumberIndex;

				foreach ( var currentUniform in glUniformReferences )
				{
					if ( index == currentUniform.ConstantDef.PhysicalIndex )
					{
						unsafe
						{
							GL.Uniform1( currentUniform.Location, 1, parms.GetFloatPointer( index ).Pointer.ToFloatPointer() );
							GLES2Config.GlCheckError( this );
							//There will only be one multipass entry
							return;
						}
					}
				}
			}
		}

		protected void ExtractLayoutQualifiers() {}
	}
}
