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

using System.Collections.Generic;

using Axiom.Graphics;

using OpenTK.Graphics.ES20;

using GLenum = OpenTK.Graphics.ES20.All;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
	internal abstract class GLSLESProgramManagerCommon
	{
		public struct GLUniformReference
		{
			public int Location;
			public GpuProgramType SourceProgType;
			public Axiom.Graphics.GpuProgramParameters.GpuConstantDefinition ConstantDef;
		}

		private static int GLSampler2DShadowExt = 0x8B62;
		protected GLSLESGpuProgram activeVertexGpuProgram;
		protected GLSLESGpuProgram activeFragmentGpuProgram;
		private Dictionary<string, GLenum> typeEnumMap;

		protected void CompleteDefInfo( GLenum glType, Axiom.Graphics.GpuProgramParameters.GpuConstantDefinition defToUpdate )
		{
			//Decode unifrom size and type
			//Note GLSL ES never packs rows into float4's (from an API perspective anyway)
			//therefore all values are tight in the buffer
			switch ( glType )
			{
				case GLenum.Float:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Float1;
					break;
				case GLenum.FloatVec2:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Float2;
					break;
				case GLenum.FloatVec3:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Float3;
					break;
				case GLenum.FloatVec4:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Float4;
					break;
				case GLenum.Sampler2D:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Sampler2D;
					break;
				case GLenum.SamplerCube:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.SamplerCube;
					break;
				case GLenum.Int:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Int1;
					break;
				case GLenum.IntVec2:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Int2;
					break;
				case GLenum.IntVec3:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Int3;
					break;
				case GLenum.IntVec4:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Int4;
					break;
				case GLenum.FloatMat2:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_2X2;
					break;
				case GLenum.FloatMat3:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_3X3;
					break;
				case GLenum.FloatMat4:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Unknown;
					break;
				default:
					defToUpdate.ConstantType = GpuProgramParameters.GpuConstantType.Unknown;
					break;
			}
			//GL doesn't pad
			defToUpdate.ElementSize = Axiom.Graphics.GpuProgramParameters.GpuConstantDefinition.GetElementSize( defToUpdate.ConstantType, false );
		}

		protected bool CompleteParamSource( string paramName, Graphics.GpuProgramParameters.GpuConstantDefinitionMap vertexConstantDefs, Graphics.GpuProgramParameters.GpuConstantDefinitionMap fragmentConstantDefs, GLUniformReference refToUpdate )
		{
			if ( vertexConstantDefs != null )
			{
				if ( vertexConstantDefs.ContainsKey( paramName ) )
				{
					var parami = vertexConstantDefs[ paramName ];
					refToUpdate.SourceProgType = GpuProgramType.Vertex;
					refToUpdate.ConstantDef = parami;
					return true;
				}
			}

			if ( fragmentConstantDefs != null )
			{
				if ( fragmentConstantDefs.ContainsKey( paramName ) )
				{
					refToUpdate.SourceProgType = GpuProgramType.Fragment;
					refToUpdate.ConstantDef = fragmentConstantDefs[ paramName ];
					return true;
				}
			}

			return false;
		}

		public GLSLESProgramManagerCommon()
		{
			this.activeVertexGpuProgram = null;
			this.activeFragmentGpuProgram = null;

            typeEnumMap = new Dictionary<string, GLenum>();
			//Fill in the relationship between type names and enums
			this.typeEnumMap.Add( "float", GLenum.Float );
			this.typeEnumMap.Add( "vec2", GLenum.FloatVec2 );
			this.typeEnumMap.Add( "vec3", GLenum.FloatVec3 );
			this.typeEnumMap.Add( "vec4", GLenum.FloatVec4 );
			this.typeEnumMap.Add( "sampler2D", GLenum.Sampler2D );
			this.typeEnumMap.Add( "samplerCube", GLenum.SamplerCube );
			//typeEnumMap.Add("sampler2DShadow", GLenum.sh
			this.typeEnumMap.Add( "int", GLenum.Int );
			this.typeEnumMap.Add( "ivec2", GLenum.IntVec2 );
			this.typeEnumMap.Add( "ivec3", GLenum.IntVec3 );
			this.typeEnumMap.Add( "ivec4", GLenum.IntVec4 );
			this.typeEnumMap.Add( "mat2", GLenum.FloatMat2 );
			this.typeEnumMap.Add( "mat3", GLenum.FloatMat3 );
			this.typeEnumMap.Add( "mat4", GLenum.FloatMat4 );
		}

		public void OptimizeShaderSource( GLSLESGpuProgram gpuProgram )
		{
			/*Port Notes
			 * As best I can reckon' OpenTK doesn't support the kind of optimization done in Ogre
			 * I figure this is okay, seeing as Ogre checks for #if optimize, and it seems to be off by default
			 */
		}

		///<summary>
		///  Populate a list of uniforms based on a program object
		///</summary>
		///<param name="programObject"> Handle to the program object to query </param>
		///<param name="vertexConstantDefs"> vertexConstantDefs Definition of the constants extracted from the vertex program, used to match up physical buffer indexes with program uniforms. May be null if there is no vertex program. </param>
		///<param name="fragmentConstantDefs"> fragmentConstantDefs Definition of the constants extracted from the fragment program, used to match up physical buffer indexes with program uniforms. May be null if there is no fragment program. </param>
		///<param name="list"> The list to populate (will not be cleared before adding, clear it yourself before calling this if that's what you want). </param>
		public void ExtractUniforms( int programObject, Graphics.GpuProgramParameters.GpuConstantDefinitionMap vertexConstantDefs, Graphics.GpuProgramParameters.GpuConstantDefinitionMap fragmentConstantDefs, List<GLUniformReference> list )
		{
			//Scan through the active uniforms and add them to the reference list
			int uniformCount = 0;
			int maxLength = 0;
			string uniformName = string.Empty;

			GL.GetProgram( programObject, GLenum.ActiveUniformMaxLength, ref maxLength );
			GLES2Config.GlCheckError( this );

			//If the max length of active uniforms is 0, then there are 0 active.
			//There won't be any to extract so we can return
			if ( maxLength == 0 )
			{
				return;
			}

			GLUniformReference newGLUniformReference;

			//Get the number of active uniforms
			GL.GetProgram( programObject, GLenum.ActiveUniforms, ref uniformCount );
			GLES2Config.GlCheckError( this );

			//Loop over each of the active uniforms, and add them to the reference container
			//only do this for user defined uniforms, ignore built in gl state uniforms
			for ( int index = 0; index < uniformCount; index++ )
			{
				int arraySize = 0;
				GLenum glType = GLenum.None;
				int tmp = 0;
				GL.GetActiveUniform( programObject, index, maxLength, ref tmp, ref arraySize, ref glType, uniformName );
				GLES2Config.GlCheckError( this );

				//don't add built in uniforms
				newGLUniformReference = new GLUniformReference();
				newGLUniformReference.Location = GL.GetUniformLocation( programObject, uniformName );
				GLES2Config.GlCheckError( this );
				if ( newGLUniformReference.Location >= 0 )
				{
					//User defined uniform found, add it to the reference list
					string paramName = uniformName;

					//If the uniform name has a '[' in it then its an array element uniform.
					int arrayStart = -1;
					for ( int i = 0; i < paramName.Length; i++ )
					{
						if ( paramName[ i ] == '[' )
						{
							arrayStart = i;
							break;
						}
					}
					if ( arrayStart != -1 )
					{
						//If not the first array element then skip it and continue to the next uniform
						string sub = paramName.Substring( arrayStart, paramName.Length - 1 );
						if ( sub == "[0]" )
						{
							continue;
						}

						paramName = paramName.Substring( 0, arrayStart );
					}

					//Find out which params object this comes from
					bool foundSource = this.CompleteParamSource( paramName, vertexConstantDefs, fragmentConstantDefs, newGLUniformReference );

					//Only add this param if we found the source
					if ( foundSource )
					{
						list.Add( newGLUniformReference );
					}
				}
			}

			if ( uniformName != string.Empty )
			{
				uniformName = string.Empty;
			}
		}

		/// <summary>
		///   Populate a list of uniforms based on GLSL ES source.
		/// </summary>
		/// <param name="src"> Reference to the source code </param>
		/// <param name="constantDefs"> The defs to populate (will not be cleared before adding, clear it yourself before calling this if that's what you want). </param>
		/// <param name="fileName"> The file name this came from, for logging errors </param>
		public void ExtractConstantDefs( string src, Axiom.Graphics.GpuProgramParameters.GpuNamedConstants constantDefs, string fileName )
		{
			// Parse the output string and collect all uniforms
			// NOTE this relies on the source already having been preprocessed
			// which is done in GLSLESProgram::loadFromSource
			string line;
            int currPos = src.IndexOf("uniform");
			while ( currPos != -1 )
			{
				var def = new GpuProgramParameters.GpuConstantDefinition();
                string paramName = string.Empty;

				//Now check for using the word 'uniform' in a larger string & ignore
				bool inLargerString = false;
				if ( currPos != 0 )
				{
					char prev = src[ currPos - 1 ];
					if ( prev != ' ' && prev != '\t' && prev != '\r' && prev != '\n' && prev != ';' )
					{
						inLargerString = true;
					}
				}
				if ( !inLargerString && currPos + 7 < src.Length )
				{
					char next = src[ currPos + 7 ];
					if ( next != ' ' && next != '\t' && next != '\r' && next != '\n' )
					{
						inLargerString = true;
					}
				}

				//skip uniform 
				currPos += 7;

				if ( !inLargerString )
				{
					//find terminatiing semicolon
					int endPos = -1;
					for ( int i = 0; i < src.Length; i++ )
					{
						if ( src[ i ] == ';' )
						{
							endPos = i;
							break;
						}
					}
					if ( endPos == -1 )
					{
						//problem, missing semicolon, abort
						break;
					}
					line = src.Substring( currPos, endPos - currPos );

					//remove spaces before opening square braces, otherwise the following split() can split the line at inapppropriate
					//places (e.g. "vec3 sometihng [3]" won't work).

                    for (int sqp = line.IndexOf("["); sqp != -1; sqp = line.IndexOf(" ["))
					{
						line.Remove( sqp, 1 );
					}
					string[] parts = line.Split( '\t', '\r', '\n' );

					foreach ( string i in parts )
					{
						//Is this a type
						if ( this.typeEnumMap.ContainsKey( i ) )
						{
							this.CompleteDefInfo( this.typeEnumMap[ i ], def );
						}
						else
						{
							//If this is not a type, and not empty, it should be a name
							string trim = i.Trim();
							if ( trim.Length == 0 )
							{
								continue;
							}

							//Skip over precision keywords
							if ( trim == "lowp" || trim == "mediump" || trim == "highp" )
							{
								continue;
							}

							int arrayStart = -1;
							for ( int j = 0; j < trim.Length; j++ )
							{
								if ( trim[ j ] == '[' )
								{
									arrayStart = j;
									break;
								}
							}
							if ( arrayStart != -1 )
							{
								//potential name (if butted up to array)
								string name = trim.Substring( 0, arrayStart );
								name = name.Trim();
								if ( name.Length > 0 )
								{
									paramName = name;
								}

								int arrayEnd = -1;
								for ( int k = 0; k < trim.Length; k++ )
								{
									if ( trim[ k ] == ']' )
									{
										arrayEnd = k;
										break;
									}
								}
								string arrayDimTerm = trim.Substring( arrayStart + 1, arrayEnd - arrayStart - 1 );
								arrayDimTerm = arrayDimTerm.Trim();
								// the array term might be a simple number or it might be
								// an expression (e.g. 24*3) or refer to a constant expression
								// we'd have to evaluate the expression which could get nasty
								// Ogre TODO
								def.ArraySize = int.Parse( arrayDimTerm );
							}
							else
							{
								paramName = trim;
								def.ArraySize = 1;
							}

							//Name should be after the type, so complete def and add
							//We do this now so that comma-seperated params will do
							//this part once for each name mentioned
							if ( def.ConstantType == GpuProgramParameters.GpuConstantType.Unknown )
							{
								Axiom.Core.LogManager.Instance.Write( "Problem parsing the following GLSL Uniform: " + line + " in file " + fileName );
								//next uniform
								break;
							}

							//Complete def and add
							//increment physical buffer location
							def.LogicalIndex = 0; // not valid in GLSL
							if ( def.IsFloat )
							{
								def.PhysicalIndex = constantDefs.FloatBufferSize;
								constantDefs.FloatBufferSize += def.ArraySize * def.ElementSize;
							}
							else
							{
								def.PhysicalIndex = constantDefs.IntBufferSize;
								constantDefs.IntBufferSize += def.ArraySize * def.ElementSize;
							}
							constantDefs.Map.Add( paramName, def );
						}
					}
				}
				//Find next one
                currPos = src.IndexOf("uniform");
			}
		}
	}
}
