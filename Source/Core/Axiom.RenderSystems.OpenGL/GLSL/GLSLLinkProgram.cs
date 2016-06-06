#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Net.NetworkInformation;
using Axiom.Core;
using Axiom.Graphics;
using Tao.OpenGl;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.GLSL
{
	/// <summary>
	///		Encapsulation of GLSL Program Object.
	/// </summary>
	public class GLSLLinkProgram : IDisposable
	{
		#region Structs

		/// <summary>
		///		 Structure used to keep track of named uniforms in the linked program object.
		/// </summary>
		public class UniformReference
		{
			/// <summary>GL location handle</summary>
			public int Location;

			/// <summary>Which type of program params will this value come from?</summary>
			public GpuProgramType SourceProgType;

			/// <summary>The constant definition it relates to</summary>
			public GpuProgramParameters.GpuConstantDefinition ConstantDef;
		}

		private struct CustomAttribute
		{
			public readonly string name;
			public readonly uint attrib;

			public CustomAttribute( string _name, uint _attrib )
			{
				this.name = _name;
				this.attrib = _attrib;
			}
		}

		#endregion Structs

		#region Inner Classes

		public class UniformReferenceList : List<UniformReference>
		{
		}

		public class AttributeSet : HashSet<uint>
		{
		}

		#endregion Inner Classes

		#region Fields

		/// <summary>
		///		Container of uniform references that are active in the program object.
		/// </summary>
		protected UniformReferenceList uniformReferences = new UniformReferenceList();

		/// <summary>
		/// associated vertex program
		/// </summary>
		protected GLSLGpuProgram vertexProgram;

		/// <summary>
		/// associated fragment program
		/// </summary>
		protected GLSLGpuProgram fragmentProgram;

		/// <summary>
		/// associated geometry program
		/// </summary>
		protected GLSLGpuProgram geometryProgram;

		/// <summary>
		///		Flag to indicate that uniform references have already been built.
		/// </summary>
		protected bool uniformRefsBuilt;

		/// <summary>
		///		GL handle for the program object.
		/// </summary>
		protected int glHandle;

		/// <summary>
		///		Flag indicating that the program object has been successfully linked
		/// </summary>
		protected bool linked;

		/// <summary>
		///		Flag indicating that the program object has tried to link and failed
		/// </summary>
		protected bool triedToLinkAndFailed;

		// Custom attribute bindings
		protected AttributeSet validAttributes = new AttributeSet();

		private readonly CustomAttribute[] sCustomAttributes = new CustomAttribute[]
															   {
																new CustomAttribute( "vertex",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.Position, 0 ) ),
																new CustomAttribute( "blendWeights",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.BlendWeights, 0 ) )
																,
																new CustomAttribute( "normal",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.Normal, 0 ) ),
																new CustomAttribute( "colour",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.Diffuse, 0 ) ),
																new CustomAttribute( "secondary_colour",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.Specular, 0 ) ),
																new CustomAttribute( "blendIndices",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.BlendIndices, 0 ) )
																,
																new CustomAttribute( "uv0",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.TexCoords, 0 ) ),
																new CustomAttribute( "uv1",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.TexCoords, 1 ) ),
																new CustomAttribute( "uv2",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.TexCoords, 2 ) ),
																new CustomAttribute( "uv3",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.TexCoords, 3 ) ),
																new CustomAttribute( "uv4",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.TexCoords, 4 ) ),
																new CustomAttribute( "uv5",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.TexCoords, 5 ) ),
																new CustomAttribute( "uv6",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.TexCoords, 6 ) ),
																new CustomAttribute( "uv7",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.TexCoords, 7 ) ),
																new CustomAttribute( "tangent",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.Tangent, 0 ) ),
																new CustomAttribute( "binormal",
																					 GLGpuProgram.FixedAttributeIndex(
																						VertexElementSemantic.Binormal, 0 ) ),
															   };

		#endregion Fields

		/// <summary>
		/// Retrieves the corresponding OpenGL primitive type for an OperationType 
		/// </summary>
		public int GetGLGeometryInputPrimitiveType( OperationType operationType, bool requiresAdjacency )
		{
			switch ( operationType )
			{
				case OperationType.PointList:
					return Tao.OpenGl.Gl.GL_POINTS;
				case OperationType.LineList:
				case OperationType.LineStrip:
					return requiresAdjacency ? Tao.OpenGl.Gl.GL_LINES_ADJACENCY_EXT : Tao.OpenGl.Gl.GL_LINES;
				default:
				case OperationType.TriangleList:
				case OperationType.TriangleStrip:
				case OperationType.TriangleFan:
					return requiresAdjacency ? Tao.OpenGl.Gl.GL_TRIANGLES_ADJACENCY_EXT : Tao.OpenGl.Gl.GL_TRIANGLES;
			}
		}

		///<summary>
		/// Retrieves the corresponding OpenGL primitive for an operation, used for geometry shader output
		///</summary>
		public int GetGLGeometryOutputPrimitiveType( OperationType operationType )
		{
			switch ( operationType )
			{
				case OperationType.PointList:
					return Tao.OpenGl.Gl.GL_POINTS;
				case OperationType.LineStrip:
					return Tao.OpenGl.Gl.GL_LINE_STRIP;
				case OperationType.TriangleStrip:
					return Tao.OpenGl.Gl.GL_TRIANGLE_STRIP;
				default:
					throw new AxiomException( "Geometry shader output operation type can only be point list," +
											  "line strip or triangle strip" + "GLSLLinkProgram::activate" );
			}
		}

		#region Constructor

		/// <summary>
		///		Default constructor.
		/// </summary>
		public GLSLLinkProgram( GLSLGpuProgram vertexProgram, GLSLGpuProgram geometryProgram, GLSLGpuProgram fragmentProgram )
		{
			this.vertexProgram = vertexProgram;
			this.geometryProgram = geometryProgram;
			this.fragmentProgram = fragmentProgram;
			this.uniformRefsBuilt = false;
			this.linked = false;
			this.triedToLinkAndFailed = false;
		}

		#endregion Constructor

		// TODO: this region needs review!!!

		#region Properties

		/// <summary>
		///		Gets the GL Handle for the program object.
		/// </summary>
		public int GLHandle
		{
			get
			{
				return this.glHandle;
			}
		}

		public string CombinedName
		{
			get
			{
				String name = "";
				if ( this.vertexProgram != null )
				{
					name += "Vertex Program:";
					name += this.vertexProgram.Name;
				}
				if ( this.fragmentProgram != null )
				{
					name += " Fragment Program:";
					name += this.fragmentProgram.Name;
				}
				if ( this.geometryProgram != null )
				{
					name += " Geometry Program:";
					name += this.geometryProgram.Name;
				}
				return name;
			}
		}


		public bool IsSkeletalAnimationIncluded { get; private set; }

		#endregion Properties

		#region Methods

		/// <summary>
		///		Makes a program object active by making sure it is linked and then putting it in use.
		/// </summary>
		public void Activate()
		{
			if ( !this.linked && !this.triedToLinkAndFailed )
			{
				Gl.glGetError(); //Clean up the error. Otherwise will flood log.
				this.glHandle = Gl.glCreateProgramObjectARB();
				GLSLHelper.CheckForGLSLError( "Error Creating GLSL Program Object", 0 );

				// TODO: support microcode caching
				//if (GpuProgramManager.Instance.CanGetCompiledShaderBuffer() &&
				//    GpuProgramManager.Instance.IsMicrocodeAvailableInCache(CombinedName))
				if ( false )
				{
					GetMicrocodeFromCache();
				}
				else
				{
					CompileAndLink();
				}

				BuildUniformReferences();
				ExtractAttributes();
			}

			if ( this.linked )
			{
				GLSLHelper.CheckForGLSLError( "Error prior to using GLSL Program Object : ", this.glHandle, false, false );

				Gl.glUseProgramObjectARB( this.glHandle );

				GLSLHelper.CheckForGLSLError( "Error using GLSL Program Object : ", this.glHandle, false, false );
			}
		}

		private void GetMicrocodeFromCache()
		{
			throw new NotImplementedException();
		}

		private void ExtractAttributes()
		{
			foreach ( var a in this.sCustomAttributes )
			{
				var attrib = Gl.glGetAttribLocationARB( this.glHandle, a.name );

				if ( attrib != -1 )
				{
					this.validAttributes.Add( a.attrib );
				}
			}
		}

		public uint GetAttributeIndex( VertexElementSemantic semantic, uint index )
		{
			return GLGpuProgram.FixedAttributeIndex( semantic, index );
		}

		public bool IsAttributeValid( VertexElementSemantic semantic, uint index )
		{
			return this.validAttributes.Contains( GetAttributeIndex( semantic, index ) );
		}

		/// <summary>
		///		Build uniform references from active named uniforms.
		/// </summary>
		private void BuildUniformReferences()
		{
			if ( !this.uniformRefsBuilt )
			{
				var vertParams = GpuProgramParameters.GpuConstantDefinitionMap.Empty;
				var fragParams = GpuProgramParameters.GpuConstantDefinitionMap.Empty;
				var geomParams = GpuProgramParameters.GpuConstantDefinitionMap.Empty;
				if ( this.vertexProgram != null )
				{
					vertParams = this.vertexProgram.GLSLProgram.ConstantDefinitions.Map;
				}
				if ( this.geometryProgram != null )
				{
					geomParams = this.geometryProgram.GLSLProgram.ConstantDefinitions.Map;
				}
				if ( this.fragmentProgram != null )
				{
					fragParams = this.fragmentProgram.GLSLProgram.ConstantDefinitions.Map;
				}

				GLSLLinkProgramManager.Instance.ExtractUniforms( this.glHandle, vertParams, geomParams, fragParams,
																 this.uniformReferences );

				this.uniformRefsBuilt = true;
			}
		}

		/// <summary>
		///		Updates program object uniforms using data from GpuProgramParameters.
		///		normally called by GLSLGpuProgram.BindParameters() just before rendering occurs.
		/// </summary>
		/// <param name="parameters">GPU Parameters to use to update the uniforms params.</param>
		public void UpdateUniforms( GpuProgramParameters parameters, GpuProgramParameters.GpuParamVariability mask,
									GpuProgramType fromProgType )
		{
			foreach ( var currentUniform in this.uniformReferences )
			{
				// Only pull values from buffer it's supposed to be in (vertex or fragment)
				// This method will be called twice, once for vertex program params, 
				// and once for fragment program params.
				if ( fromProgType == currentUniform.SourceProgType )
				{
					var def = currentUniform.ConstantDef;

					if ( ( def.Variability & mask ) != 0 )
					{
						var glArraySize = def.ArraySize;

						// get the index in the parameter real list
						switch ( def.ConstantType )
						{
							case GpuProgramParameters.GpuConstantType.Float1:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniform1fvARB( currentUniform.Location, glArraySize, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Float2:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniform2fvARB( currentUniform.Location, glArraySize, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Float3:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniform3fvARB( currentUniform.Location, glArraySize, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Float4:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniform4fvARB( currentUniform.Location, glArraySize, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_2X2:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniformMatrix2fvARB( currentUniform.Location, glArraySize, 1, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_2X3:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniformMatrix2x3fv( currentUniform.Location, glArraySize, 1, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_2X4:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniformMatrix2x4fv( currentUniform.Location, glArraySize, 1, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_3X2:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniformMatrix3x2fv( currentUniform.Location, glArraySize, 1, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_3X3:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniformMatrix3fv( currentUniform.Location, glArraySize, 1, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_3X4:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniformMatrix3x4fv( currentUniform.Location, glArraySize, 1, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_4X2:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniformMatrix4x2fv( currentUniform.Location, glArraySize, 1, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_4X3:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniformMatrix4x3fv( currentUniform.Location, glArraySize, 1, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Matrix_4X4:
								using ( var ptr = parameters.GetFloatPointer( def.PhysicalIndex ) )
								{
									Gl.glUniformMatrix4fv( currentUniform.Location, glArraySize, 1, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Int1:
								using ( var ptr = parameters.GetIntPointer( def.PhysicalIndex ) )
								{
									Gl.glUniform1ivARB( currentUniform.Location, glArraySize, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Int2:
								using ( var ptr = parameters.GetIntPointer( def.PhysicalIndex ) )
								{
									Gl.glUniform2ivARB( currentUniform.Location, glArraySize, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Int3:
								using ( var ptr = parameters.GetIntPointer( def.PhysicalIndex ) )
								{
									Gl.glUniform3ivARB( currentUniform.Location, glArraySize, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Int4:
								using ( var ptr = parameters.GetIntPointer( def.PhysicalIndex ) )
								{
									Gl.glUniform4ivARB( currentUniform.Location, glArraySize, ptr.Pointer.Pin() );
								}
								break;
							case GpuProgramParameters.GpuConstantType.Sampler1D:
							case GpuProgramParameters.GpuConstantType.Sampler1DShadow:
							case GpuProgramParameters.GpuConstantType.Sampler2D:
							case GpuProgramParameters.GpuConstantType.Sampler2DShadow:
							case GpuProgramParameters.GpuConstantType.Sampler3D:
							case GpuProgramParameters.GpuConstantType.SamplerCube:
								var value = parameters.GetIntConstant( def.PhysicalIndex );
								Gl.glUniform1iARB( currentUniform.Location, value );
								break;
							case GpuProgramParameters.GpuConstantType.Unknown:
								break;
						} // end switch
						GLSLHelper.CheckForGLSLError( "GLSLLinkProgram::updateUniforms", 0 );
					} // variability & mask
				} // fromProgType == currentUniform->mSourceProgType
			} // end for
		}

		public void UpdatePassIterationUniforms( GpuProgramParameters parameters )
		{
			throw new NotImplementedException();
		}

		private void CompileAndLink()
		{
			if ( this.vertexProgram != null )
			{
				// compile and attach Vertex Program
				if ( !this.vertexProgram.GLSLProgram.Compile( true ) )
				{
					// todo error
					return;
				}
				this.vertexProgram.GLSLProgram.AttachToProgramObject( this.glHandle );
				IsSkeletalAnimationIncluded = this.vertexProgram.IsSkeletalAnimationIncluded;

				// Some drivers (e.g. OS X on nvidia) incorrectly determine the attribute binding automatically

				// and end up aliasing existing built-ins. So avoid! 
				// Bind all used attribs - not all possible ones otherwise we'll get 
				// lots of warnings in the log, and also may end up aliasing names used
				// as varyings by accident
				// Because we can't ask GL whether an attribute is used in the shader
				// until it is linked (chicken and egg!) we have to parse the source

				var vpSource = this.vertexProgram.GLSLProgram.Source;
				foreach ( var a in this.sCustomAttributes )
				{
					// we're looking for either: 
					//   attribute vec<n> <semantic_name>
					//   in vec<n> <semantic_name>
					// The latter is recommended in GLSL 1.3 onwards 
					// be slightly flexible about formatting
					var pos = vpSource.IndexOf( a.name );

					if ( pos != -1 )
					{
						var startpos = vpSource.IndexOf( "attribute", pos < 20 ? 0 : pos - 20 );
						if ( startpos == -1 )
						{
							startpos = vpSource.IndexOf( "in", pos < 20 ? 0 : pos - 20 );
						}
						if ( startpos != -1 && startpos < pos )
						{
							// final check 
							var expr = vpSource.Substring( startpos, pos + a.name.Length - startpos );
							var vec = expr.Split();

							if ( ( vec[ 0 ] == "in" || vec[ 0 ] == "attribute" ) && vec[ 2 ] == a.name )
							{
								Gl.glBindAttribLocationARB( this.glHandle, (int)a.attrib, a.name );
							}
						}
					}
				}
			}

			if ( this.geometryProgram != null )
			{
				// compile and attach Geometry Program
				if ( !this.geometryProgram.GLSLProgram.Compile( true ) )
				{
					// todo error
					return;
				}

				this.geometryProgram.GLSLProgram.AttachToProgramObject( this.glHandle );

				//Don't set adjacency flag. We handle it internally and expose "false"

				OperationType inputOperationType = this.geometryProgram.GLSLProgram.InputOperationType;
				Gl.glProgramParameteriEXT( this.glHandle, Gl.GL_GEOMETRY_INPUT_TYPE_EXT,
										   GetGLGeometryInputPrimitiveType( inputOperationType,
																			this.geometryProgram.IsAdjacencyInfoRequired ) );

				OperationType outputOperationType = this.geometryProgram.GLSLProgram.OutputOperationType;
				switch ( outputOperationType )
				{
					case OperationType.PointList:
					case OperationType.LineStrip:
					case OperationType.TriangleStrip:
					case OperationType.LineList:
					case OperationType.TriangleList:
					case OperationType.TriangleFan:
						break;
				}
				Gl.glProgramParameteriEXT( this.glHandle, Gl.GL_GEOMETRY_OUTPUT_TYPE_EXT,
										   GetGLGeometryOutputPrimitiveType( outputOperationType ) );

				Gl.glProgramParameteriEXT( this.glHandle, Gl.GL_GEOMETRY_VERTICES_OUT_EXT,
										   this.geometryProgram.GLSLProgram.MaxOutputVertices );
			}

			if ( this.fragmentProgram != null )
			{
				if ( !this.fragmentProgram.GLSLProgram.Compile( true ) )
				{
					// todo error
					return;
				}
				this.fragmentProgram.GLSLProgram.AttachToProgramObject( this.glHandle );
			}

			// now the link

			Gl.glLinkProgramARB( this.glHandle );
			int linkStatus;
			Gl.glGetObjectParameterivARB( this.glHandle, Gl.GL_OBJECT_LINK_STATUS_ARB, out linkStatus );
			this.linked = linkStatus != 0;
			this.triedToLinkAndFailed = !this.linked;

			GLSLHelper.CheckForGLSLError( "Error linking GLSL Program Object", this.glHandle, !this.linked, !this.linked );

			if ( this.linked )
			{
				GLSLHelper.LogObjectInfo( CombinedName + " GLSL link result : ", this.glHandle );

				// TODO: cache the microcode.
				// OpenTK is not up to date yet for this.
				// We need deeper engine updates for this as well

				/*
				if (GpuProgramManager.Instance.SaveMicrocodesToCache)
				{
					// add to the microcode to the cache
					var name = CombinedName;

					// get buffer size
					int binaryLength;
					Gl.glGetProgramiv( glHandle, Gl.GL_PROGRAM_BINARY_LENGTH, out binaryLength );

					// turns out we need this param when loading
					// it will be the first bytes of the array in the microcode
					int binaryFormat;

					// create microcode
					GpuProgramManager.Microcode newMicrocode =
						GpuProgramManager.Instance.CreateMicrocode( binaryLength + sizeof ( GLenum ) );

					// get binary
					uint8* programBuffer = newMicrocode->getPtr() + sizeof ( GLenum );
					glGetProgramBinary( mGLHandle, binaryLength, NULL, &binaryFormat, programBuffer );

					// save binary format
					memcpy( newMicrocode->getPtr(), &binaryFormat, sizeof ( GLenum ) );

					// add to the microcode to the cache
					GpuProgramManager::getSingleton().addMicrocodeToCache( name, newMicrocode );
				}*/
			}
		}

		#endregion Methods

		#region IDisposable Members

		/// <summary>
		///     Called to destroy the program used by this link program.
		/// </summary>
		public void Dispose()
		{
			Gl.glDeleteObjectARB( this.glHandle );
		}

		#endregion
	}
}