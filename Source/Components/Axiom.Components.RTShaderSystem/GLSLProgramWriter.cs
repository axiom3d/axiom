using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
	internal class GLSLProgramWriter : ProgramWriter
	{
		private Dictionary<Axiom.Graphics.GpuProgramParameters.GpuConstantType, string> gpuConstTypeMap;
		private Dictionary<Parameter.SemanticType, string> paramSemanticMap;
		private Dictionary<string, string> inputToGLStatesMap;
		private Dictionary<Parameter.ContentType, string> contentToPerVertexAttributes;
		private readonly int glslVersion;
		private List<string> fragInputParams;

		public GLSLProgramWriter()
		{
			glslVersion = 120;
			InitializeStringMaps();
		}

		internal override void WriteSourceCode( System.IO.StreamWriter stream, Program program )
		{
			var gpuType = program.Type;
			if ( gpuType == GpuProgramType.Geometry )
			{
				throw new Core.AxiomException( "Geometry Program not supported iin GLSL writer" );
			}

			fragInputParams.Clear();
			var functionList = program.Functions;
			var parameterList = program.Parameters;

			// Write the current version (this force the driver to more fulfill the glsl standard)
			stream.WriteLine( "#version " + glslVersion.ToString() );

			//Generate source code header
			WriteProgramTitle( stream, program );
			stream.WriteLine();

			//Write forward declarations
			WriteForwardDeclarations( stream, program );
			stream.WriteLine();

			//Generate global variable code
			WriteUniformParametersTitle( stream, program );
			stream.WriteLine();

			//Write the uniforms
			foreach ( var uniformParam in parameterList )
			{
				stream.Write( "uniform\t" );
				stream.Write( gpuConstTypeMap[ uniformParam.Type ] );
				stream.Write( "\t" );
				stream.Write( uniformParam.Name );
				if ( uniformParam.IsArray )
				{
					stream.Write( "[" + uniformParam.Size.ToString() + "]" );
				}
				stream.Write( ";" );
				stream.WriteLine();
			}
			stream.WriteLine();

			//Write program function(s)
			foreach ( var curFunction in functionList )
			{
				WriteFunctionTitle( stream, curFunction );

				//Clear output mapping this map is used when we use
				//glsl built in types like gl_Color for example
				inputToGLStatesMap.Clear();

				//Write inout params and fill inputToGLStatesMap
				WriteInputParameters( stream, curFunction, gpuType );
				WriteOutParameters( stream, curFunction, gpuType );

				stream.Write( "void main() {" );
				stream.WriteLine();
				//Write local parameters
				var localParams = curFunction.LocalParameters;

				foreach ( var itParam in localParams )
				{
					stream.Write( "\t" );
					WriteLocalParameter( stream, itParam );
					stream.Write( ";" );
					stream.WriteLine();
				}

				stream.WriteLine();
				//Sort function atoms
				curFunction.SortAtomInstances();
				var atomInstances = curFunction.AtomInstances;

				foreach ( var itAtom in atomInstances )
				{
					var funcInvoc = itAtom as FunctionInvocation;

					var localOs = new StringBuilder();

					//Write function name
					localOs.Append( "\t" + funcInvoc.FunctionAtomType + "(" );
					int curIndLevel = 0;

					int itOperand = 0;
					int itOperandEnd = funcInvoc.OperandList.Count;

					while ( itOperand != itOperandEnd )
					{
						Operand op = funcInvoc.OperandList[ itOperand ];
						Operand.OpSemantic opSemantic = op.Semantic;
						string paramName = op.Parameter.Name;
						Parameter.ContentType content = op.Parameter.Content;

						if ( opSemantic == Operand.OpSemantic.Out || opSemantic == Operand.OpSemantic.InOut )
						{
							bool isVarying = false;

							// Check if we write to an varying because the are only readable in fragment programs 
							if ( gpuType == GpuProgramType.Fragment )
							{
								if ( fragInputParams.Contains( paramName ) )
								{
									//Declare the copy variable
									string newVar = "local_" + paramName;
									string tempVar = paramName;
									isVarying = true;

									// We stored the original values in the mFragInputParams thats why we have to replace the first var with o
									// because all vertex output vars are prefixed with o in glsl the name has to match in the fragment program.
									tempVar = tempVar.Remove( 0 );
									tempVar = tempVar.Insert( 0, "o" );

									//Declare the copy variable and assign the original
									stream.WriteLine( "\t" + gpuConstTypeMap[ op.Parameter.Type ] + " " + newVar + " = " +
									                  tempVar );

									//From now on we replace it automatic
									inputToGLStatesMap[ paramName ] = newVar;

									//Remove the param because now it is replaced automatic with the local variable
									//(which could be written).
									fragInputParams.Remove( paramName );
								}
							}

							//If its not varying param check if a uniform is written
							if ( !isVarying )
							{
								foreach ( var param in parameterList )
								{
									if ( GLSLProgramWriter.CompareUniformByName( param, paramName ) )
									{
										//Declare the copy variable
										string newVar = "local_" + paramName;

										//now we check if we already declared a uniform redirector var
										if ( inputToGLStatesMap.ContainsKey( newVar ) == false )
										{
											//Declare the copy variable and assign the original
											stream.WriteLine( "\t" + gpuConstTypeMap[ param.Type ] + " " + newVar +
											                  paramName + ";\n" );

											//From now on we replace it automatic
											inputToGLStatesMap.Add( paramName, newVar );
										}
									}
								}
							}
						}
						if ( inputToGLStatesMap.ContainsKey( paramName ) )
						{
							int mask = op.Mask; // our swizzle mask

							//Here we insert the renamed param name
							localOs.Append( "." + Operand.GetMaskAsString( mask ) );

							if ( mask != (int)Operand.OpMask.All )
							{
								localOs.Append( "." + Operand.GetMaskAsString( mask ) );
							}

								//Now that every texcoord is a vec4 (passed as vertex attributes)
								//we have to swizzle them aoccording the desired type.
							else if ( gpuType == GpuProgramType.Vertex &&
							          content == Parameter.ContentType.TextureCoordinate0 ||
							          content == Parameter.ContentType.TextureCoordinate1 ||
							          content == Parameter.ContentType.TextureCoordinate2 ||
							          content == Parameter.ContentType.TextureCoordinate3 ||
							          content == Parameter.ContentType.TextureCoordinate4 ||
							          content == Parameter.ContentType.TextureCoordinate5 ||
							          content == Parameter.ContentType.TextureCoordinate6 ||
							          content == Parameter.ContentType.TextureCoordinate7 )
							{
								//Now generate the swizzel mask according
								//the type.
								switch ( op.Parameter.Type )
								{
									case GpuProgramParameters.GpuConstantType.Float1:
										localOs.Append( ".x" );
										break;
									case GpuProgramParameters.GpuConstantType.Float2:
										localOs.Append( ".xy" );
										break;
									case GpuProgramParameters.GpuConstantType.Float3:
										localOs.Append( ".xyz" );
										break;
									case GpuProgramParameters.GpuConstantType.Float4:
										localOs.Append( ".xyzw" );
										break;
									default:
										break;
								}
							}
						}
						else
						{
							localOs.Append( op.ToString() );
						}
						itOperand++;

						//Prepare for the next operand
						int opIndLevel = 0;
						if ( itOperand != itOperandEnd )
						{
							opIndLevel = funcInvoc.OperandList[ itOperand ].IndirectionLevel;
						}

						if ( curIndLevel < opIndLevel )
						{
							while ( curIndLevel < opIndLevel )
							{
								curIndLevel++;
								localOs.Append( "[" );
							}
						}
						else
						{
							while ( curIndLevel > opIndLevel )
							{
								curIndLevel--;
								localOs.Append( "]" );
							}
							if ( opIndLevel != 0 )
							{
								localOs.Append( "][" );
							}
							else if ( itOperand != itOperandEnd )
							{
								localOs.Append( ", " );
							}
						}
						if ( curIndLevel != 0 )
						{
							localOs.Append( "int(" );
						}
					}

					//Write function call closer
					localOs.AppendLine( ");" );
					localOs.AppendLine();
					stream.Write( localOs.ToString() );
				}
				stream.WriteLine( "}" );
			}
			stream.WriteLine();
		}

		private void InitializeStringMaps()
		{
			//basic glsl types
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float1, "float" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float2, "vec2" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float3, "vec3" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float4, "vec4" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Sampler1D, "sampler1D" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Sampler2D, "sampler2D" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Sampler3D, "sampler3D" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.SamplerCube, "samplerCube" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_2X2, "mat2" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_2X3, "mat2x3" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_2X4, "mat2x4" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_3X2, "mat3x2" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_3X3, "mat3" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_3X4, "mat3x4" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_4X2, "mat4x2" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_4X3, "mat4x3" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_4X4, "mat4" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int1, "int" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int2, "int2" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int3, "int3" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int4, "int4" );

			// Custom vertex attributes defined http://www.ogre3d.org/docs/manual/manual_21.html
			contentToPerVertexAttributes.Add( Parameter.ContentType.PositionObjectSpace, "vertex" );
			contentToPerVertexAttributes.Add( Parameter.ContentType.NormalObjectSpace, "normal" );
			contentToPerVertexAttributes.Add( Parameter.ContentType.TangentObjectSpace, "tangent" );
			contentToPerVertexAttributes.Add( Parameter.ContentType.BinormalObjectSpace, "binormal" );

			contentToPerVertexAttributes.Add( Parameter.ContentType.TextureCoordinate0, "uv0" );
			contentToPerVertexAttributes.Add( Parameter.ContentType.TextureCoordinate1, "uv1" );
			contentToPerVertexAttributes.Add( Parameter.ContentType.TextureCoordinate3, "uv2" );
			contentToPerVertexAttributes.Add( Parameter.ContentType.TextureCoordinate4, "uv3" );
			contentToPerVertexAttributes.Add( Parameter.ContentType.TextureCoordinate5, "uv4" );
			contentToPerVertexAttributes.Add( Parameter.ContentType.TextureCoordinate6, "uv5" );
			contentToPerVertexAttributes.Add( Parameter.ContentType.TextureCoordinate7, "uv6" );

			if ( glslVersion == 130 )
			{
				contentToPerVertexAttributes.Add( Parameter.ContentType.ColorDiffuse, "colour" );
				contentToPerVertexAttributes.Add( Parameter.ContentType.ColorSpecular, "secondary_colour" );
			}
		}

		private void WriteLocalParameter( StreamWriter stream, Parameter parameter )
		{
			stream.Write( gpuConstTypeMap[ parameter.Type ] );
			stream.Write( "\t" );
			stream.Write( parameter.Name );
			if ( parameter.IsArray )
			{
				stream.Write( "[" + parameter.ToString() + "]" );
			}
		}

		private void WriteForwardDeclarations( StreamWriter stream, Program program )
		{
			stream.WriteLine( "//-----------------------------------------------------------------------------" );
			stream.WriteLine( "//                FORWARD DECLARATIONS" );
			stream.WriteLine( "//-----------------------------------------------------------------------------" );

			var forwardDecl = new List<string>(); // hold all generated function declarations
			var functionList = program.Functions;

			foreach ( var curFunction in functionList )
			{
				var atomInstances = curFunction.AtomInstances;

				for ( int i = 0; i < atomInstances.Count; i++ )
				{
					var itAtom = atomInstances[ i ];

					//Skip non function invocation atoms
					if ( !( itAtom is FunctionInvocation ) )
					{
						continue;
					}

					var funcInvoc = itAtom as FunctionInvocation;

					int itOperator = 0;
					int itOperatorEnd = funcInvoc.OperandList.Count;

					//Start with function declaration
					string funcDecl = funcInvoc.ReturnType + " " + funcInvoc.FunctionName + "(";

					//Now iterate overall operands
					while ( itOperator != itOperatorEnd )
					{
						Parameter param = funcInvoc.OperandList[ itOperator ].Parameter;
						Operand.OpSemantic opSemantic = funcInvoc.OperandList[ itOperator ].Semantic;
						int opMask = funcInvoc.OperandList[ itOperator ].Mask;
						Axiom.Graphics.GpuProgramParameters.GpuConstantType gpuType =
							GpuProgramParameters.GpuConstantType.Unknown;

						//Write the semantic in, out, inout
						switch ( opSemantic )
						{
							case Operand.OpSemantic.In:
								funcDecl += "in ";
								break;
							case Operand.OpSemantic.Out:
								funcDecl += "out ";
								break;
							case Operand.OpSemantic.InOut:
								funcDecl += "inout ";
								break;
							default:
								break;
						}

						//Swizzle masks are only defined fro types like vec2, vec3, vec4
						if ( opMask == (int)Operand.OpMask.All )
						{
							gpuType = param.Type;
						}
						else
						{
							//Now we have to conver the mask to operator
							gpuType = Operand.GetGpuConstantType( opMask );
						}

						//We need a valid type otherwise glsl compilation will not work
						if ( gpuType == GpuProgramParameters.GpuConstantType.Unknown )
						{
							throw new Core.AxiomException( "Cannot convert Operand.OpMask to GpuConstantType" );
						}

						//Write the operand type.
						funcDecl += gpuConstTypeMap[ gpuType ];

						itOperator++;
						//move over all operators with indirection
						while ( ( itOperator != itOperatorEnd ) &&
						        ( funcInvoc.OperandList[ itOperator ].IndirectionLevel != 0 ) )
						{
							itOperator++;
						}

						//Prepare for the next operand
						if ( itOperator != itOperatorEnd )
						{
							funcDecl += ", ";
						}
					}

					//Write function call closer.
					funcDecl += ");\n";

					//Push the generated declaration into the vector
					//duplicate declarations will be removed later.
					forwardDecl.Add( funcDecl );
				}
			}
			//Now remove duplicate declaration, first we have to sort the vector.
			forwardDecl.Sort();
			forwardDecl = forwardDecl.Distinct().ToList();

			foreach ( var it in forwardDecl )
			{
				stream.Write( it );
			}
		}

		private void WriteInputParameters( StreamWriter stream, Function function, GpuProgramType gpuType )
		{
			var inParams = function.InputParameters;

			foreach ( var param in inParams )
			{
				Parameter.ContentType paramContent = param.Content;
				string paramName = param.Name;

				if ( gpuType == GpuProgramType.Fragment )
				{
					// push fragment inputs they all could be written (in glsl you can not write
					// input params in the fragment program)
					fragInputParams.Add( paramName );

					// In the vertex and fragment program the variable names must match.
					// Unfortunately now the input params are prefixed with an 'i' and output params with 'o'.
					// Thats why we are using a map for name mapping (we rename the params which are used in function atoms).
					paramName = paramName.Remove( 0 ); //get rid of the i
					paramName = paramName.Insert( 0, "o" ); //place in o at the beginning instead
					inputToGLStatesMap.Add( param.Name, paramName );

					//After glsl 120 varying is deprececated
					if ( glslVersion <= 120 )
					{
						stream.Write( "varying\t" );
					}
					else
					{
						stream.Write( "out\t" );
					}

					stream.Write( gpuConstTypeMap[ param.Type ] );
					stream.Write( "\t" );
					stream.Write( paramName );
					stream.WriteLine( ";" );
				}
				else if ( gpuType == GpuProgramType.Vertex &&
				          contentToPerVertexAttributes.ContainsKey( paramContent ) )
				{
					// Due the fact that glsl does not have register like cg we have to rename the params
					// according there content.
					inputToGLStatesMap.Add( paramName, contentToPerVertexAttributes[ paramContent ] );
					stream.Write( "attribute\t" );

					//All uv texcoords passed by axiom are vec4
					if ( paramContent == Parameter.ContentType.TextureCoordinate0 ||
					     paramContent == Parameter.ContentType.TextureCoordinate1 ||
					     paramContent == Parameter.ContentType.TextureCoordinate2 ||
					     paramContent == Parameter.ContentType.TextureCoordinate3 ||
					     paramContent == Parameter.ContentType.TextureCoordinate4 ||
					     paramContent == Parameter.ContentType.TextureCoordinate5 ||
					     paramContent == Parameter.ContentType.TextureCoordinate6 ||
					     paramContent == Parameter.ContentType.TextureCoordinate7 )
					{
						stream.Write( "vec4" );
					}
					else
					{
						stream.Write( gpuConstTypeMap[ param.Type ] );
					}
					stream.Write( "\t" );
					stream.Write( contentToPerVertexAttributes[ paramContent ] );
					stream.WriteLine( ";" );
				}
				else if ( paramContent == Parameter.ContentType.ColorDiffuse )
				{
					inputToGLStatesMap.Add( paramName, "gl_Color" );
				}
				else if ( paramContent == Parameter.ContentType.ColorSpecular )
				{
					inputToGLStatesMap.Add( paramName, "gl_SecondaryColor" );
				}
				else
				{
					stream.Write( "uniform \t" );
					stream.Write( gpuConstTypeMap[ param.Type ] );
					stream.Write( "\t" );
					stream.Write( paramName );
					stream.WriteLine( ";" );
				}
			}
		}

		private void WriteOutParameters( StreamWriter stream, Function function, GpuProgramType gpuType )
		{
			var outParams = function.OutputParameters;

			foreach ( var param in outParams )
			{
				if ( gpuType == GpuProgramType.Vertex )
				{
					//GLSL vertex program has to write always gl_Position (but this is also deprecated after version 130)
					if ( param.Content == Parameter.ContentType.PositionProjectiveSpace )
					{
						inputToGLStatesMap.Add( param.Name, "gl_Position" );
					}
					else
					{
						//After glsl 120 varying is deprecated
						if ( glslVersion <= 120 )
						{
							stream.Write( "varying\t" );
						}
						else
						{
							stream.Write( "out\t" );
						}

						stream.Write( gpuConstTypeMap[ param.Type ] );
						stream.Write( "\t" );
						stream.Write( param.Name );
						if ( param.IsArray )
						{
							stream.Write( "[" + param.Size.ToString() + "]" );
						}
						stream.WriteLine( ";" );
					}
				}
				else if ( gpuType == GpuProgramType.Fragment &&
				          param.Semantic == Parameter.SemanticType.Color )
				{
					// GLSL fragment program has to write always gl_FragColor (but this is also deprecated after version 130)
					inputToGLStatesMap.Add( param.Name, "gl_FragColor" );
				}
			}
		}

		internal override string TargetLanguage
		{
			get
			{
				return "glsl";
			}
		}

		private static bool CompareUniformByName( UniformParameter param, string str )
		{
			return param.Name == str;
		}
	}
}