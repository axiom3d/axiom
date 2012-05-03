using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
	internal class GLSLESProgramWriter : ProgramWriter
	{
		private Dictionary<Axiom.Graphics.GpuProgramParameters.GpuConstantType, string> gpuConstTypeMap;
		private Dictionary<Parameter.SemanticType, string> paramSemanticMap;
		private Dictionary<string, string> inputToGLStatesMap;
		private Dictionary<FunctionInvocation, string> functionCacheMap;
		private Dictionary<string, string> definesMap;
		private Dictionary<Parameter.ContentType, string> contentToPerVertexAttributes;
		private readonly int glslVersion;
		private List<string> fragInputParams;
		private Dictionary<string, string> cachedFunctionLibraries;

		public GLSLESProgramWriter()
		{
			glslVersion = 100;
			InitializeStringMaps();
			functionCacheMap.Clear();
		}

		internal override string TargetLanguage
		{
			get
			{
				return "glsles";
			}
		}

		private void InitializeStringMaps()
		{
			gpuConstTypeMap = new Dictionary<GpuProgramParameters.GpuConstantType, string>();
			//Basic glsl es types
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float1, "float" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float2, "vec2" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float3, "vec3" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float4, "vec4" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Sampler1D, "sampler1D" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Sampler2D, "sampler2D" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.SamplerCube, "samplerCube" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_2X2, "mat2" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_3X3, "mat3" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_4X4, "mat4" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int1, "int" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int2, "int2" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int3, "int3" );
			gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int4, "int4" );


			contentToPerVertexAttributes = new Dictionary<Parameter.ContentType, string>();
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
			contentToPerVertexAttributes.Add( Parameter.ContentType.ColorDiffuse, "colour" );
			contentToPerVertexAttributes.Add( Parameter.ContentType.ColorSpecular, "secondary_colour" );
		}

		internal override void WriteSourceCode( System.IO.StreamWriter stream, Program program )
		{
			var gpuType = program.Type;

			if ( gpuType == GpuProgramType.Geometry )
			{
				throw new Core.AxiomException( "Geometry Programs not supported in GLSL ES writer" );
			}

			fragInputParams.Clear();
			var functionList = program.Functions;
			var parameterList = program.Parameters;

			// Write the current version (this forces the driver to fulfill the glsl es standard)
			stream.WriteLine( "#version" + glslVersion.ToString() );

			//Default precision declaration is required in fragment and vertex shaders.
			stream.WriteLine( "precision highp float" );
			stream.WriteLine( "precision highp int" );

			//Generate source code header
			WriteProgramTitle( stream, program );
			stream.WriteLine();

			//Embed depndencies.
			WriteProgramDependencies( stream, program );
			stream.WriteLine();

			//Generate global variable code.
			WriteUniformParametersTitle( stream, program );
			stream.WriteLine();

			//Write the uniforms
			foreach ( var uniformParams in parameterList )
			{
				stream.Write( "uniform\t" );
				stream.Write( gpuConstTypeMap[ uniformParams.Type ] );
				stream.Write( "\t" );
				stream.Write( uniformParams.Name );
				if ( uniformParams.IsArray )
				{
					stream.Write( "[" + uniformParams.Size.ToString() + "]" );
				}
				stream.WriteLine( ";" );
			}
			stream.WriteLine();

			//Write program function(s)
			foreach ( var curFunction in functionList )
			{
				WriteFunctionTitle( stream, curFunction );

				inputToGLStatesMap.Clear();

				WriteInputParameters( stream, curFunction, gpuType );
				WriteOutParameters( stream, curFunction, gpuType );

				stream.WriteLine( "void main() {" );

				if ( gpuType == GpuProgramType.Fragment )
				{
					stream.WriteLine( "\tvec4 outputColor;" );
				}
				else if ( gpuType == GpuProgramType.Vertex )
				{
					stream.WriteLine( "\tvec4 outputPosition;" );
				}

				//Write local paraemters
				var localParam = curFunction.LocalParameters;

				foreach ( var itParam in localParam )
				{
					stream.Write( "\t" );
					WriteLocalParameter( stream, itParam );
					stream.WriteLine( ";" );
				}
				stream.WriteLine();

				//sort function atoms
				curFunction.SortAtomInstances();

				var atomInstances = curFunction.AtomInstances;
				foreach ( var itAtom in atomInstances )
				{
					var funcInvoc = (FunctionInvocation)itAtom;
					int itOperand = 0;
					int itOperandEnd = funcInvoc.OperandList.Count;

					var localOs = new StringBuilder();
					localOs.Append( "\t" + funcInvoc.FunctionName + "(" );

					int curIndLevel = 0;
					while ( itOperand != itOperandEnd )
					{
						Operand op = funcInvoc.OperandList[ itOperand ];
						Operand.OpSemantic opSemantic = op.Semantic;
						string paramName = op.Parameter.Name;
						Parameter.ContentType content = op.Parameter.Content;

						// Check if we write to a varying because the are only readable in fragment programs 
						if ( opSemantic == Operand.OpSemantic.Out || opSemantic == Operand.OpSemantic.InOut )
						{
							bool isVarying = false;

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
									tempVar.Remove( 0 );
									tempVar.Insert( 0, "o" );

									//Declare the copy variable and assign the original
									stream.WriteLine( "\t" + gpuConstTypeMap[ op.Parameter.Type ] + " " + newVar + " = " +
									                  tempVar + ";\n" );

									//Fromnow on we replace it automatic
									inputToGLStatesMap.Add( paramName, newVar );

									fragInputParams.Remove( paramName );
								}
							}

							if ( !isVarying )
							{
								foreach ( var param in parameterList )
								{
									if ( CompareUniformByName( param, paramName ) )
									{
										string newVar = "local_" + paramName;

										if ( inputToGLStatesMap.ContainsKey( newVar ) == false )
										{
											//Declare the copy variable and assign the original

											stream.WriteLine( "\t" + gpuConstTypeMap[ param.Type ] + " " + newVar +
											                  " = " + paramName + ";\n" );

											//From now on we replace it automatic
											inputToGLStatesMap.Add( paramName, newVar );
										}
									}
								}
							}

							string newParam;
							if ( inputToGLStatesMap.ContainsKey( paramName ) )
							{
								int mask = op.Mask; //our swizzle mask

								//Here we insert the renamed param name
								newParam = inputToGLStatesMap[ paramName ];

								if ( mask != (int)Operand.OpMask.All )
								{
									newParam += "." + Operand.GetMaskAsString( mask );
								}
									// Now that every texcoord is a vec4 (passed as vertex attributes) we
									// have to swizzle them according the desired type.
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
									//Now generate the swizzle mask according
									// the type.
									switch ( op.Parameter.Type )
									{
										case GpuProgramParameters.GpuConstantType.Float1:
											newParam += ".x";
											break;
										case GpuProgramParameters.GpuConstantType.Float2:
											newParam += ".xy";
											break;
										case GpuProgramParameters.GpuConstantType.Float3:
											newParam += ".xyz";
											break;
										case GpuProgramParameters.GpuConstantType.Float4:
											newParam += ".xyzw";
											break;
										default:
											break;
									}
								}
							}
							else
							{
								newParam = op.ToString();
							}

							itOperand++;

							//Prepare for the next operand
							localOs.Append( newParam );

							int opIndLevel = 0;
							if ( itOperand != itOperandEnd )
							{
								opIndLevel = funcInvoc.OperandList[ itOperand ].IndirectionLevel;
							}

							if ( curIndLevel != 0 )
							{
								localOs.Append( ")" );
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
				}

				if ( gpuType == GpuProgramType.Fragment )
				{
					stream.WriteLine( "\tgl_FragColor = outputColor;" );
				}
				else if ( gpuType == GpuProgramType.Vertex )
				{
					stream.WriteLine( "\tgl_Position = outputPosition;" );
				}

				stream.WriteLine( "}" );
			}

			stream.WriteLine();
		}

		private void CacheDependencyFunctions( string libName )
		{
			if ( cachedFunctionLibraries.ContainsKey( libName ) )
			{
				return; //lib is already in cach
			}

			string libFileName = libName + ".glsles";

			var dataStream = ResourceGroupManager.Instance.OpenResource( libFileName );
			var reader = new StreamReader( dataStream, Encoding.Default );
			var functionCache = new Dictionary<string, string>();
			string line;
			while ( !reader.EndOfStream )
			{
				line = reader.ReadLine();

				//Ignore empty lines and comments
				if ( line.Length > 0 )
				{
					line = line.Trim();

					if ( line[ 0 ] == '/' && line[ 1 ] == '*' )
					{
						bool endFound = false;
						while ( !endFound )
						{
							//Get the next line
							line = reader.ReadLine();

							//Skip empties
							if ( line.Length > 0 )
							{
								//Look for the ending sequence
								if ( line.Contains( "*/" ) )
								{
									endFound = true;
								}
							}
						}
					}
					else if ( line.Length > 1 && line[ 0 ] != '/' && line[ 1 ] != '/' )
					{
						//Break up the line.
						string[] tokens = line.Split( ' ', '(', '\n', '\r' );

						//Cache #defines
						if ( tokens[ 0 ] == "#define" )
						{
							definesMap.Add( line, libName );

							continue;
						}
						// Try to identify a function definition
						// First, look for a return type
						if ( IsBasicType( tokens[ 0 ] ) && ( ( tokens.Length < 3 ) || tokens[ 2 ] != "=" ) )
						{
							string functionSig = string.Empty;
							string functionBody = string.Empty;
							FunctionInvocation functionInvoc = null;

							//Return type
							functionSig = tokens[ 0 ];
							functionSig += " ";

							//Function name
							functionSig += tokens[ 1 ];
							functionSig += "(";

							bool foundEndOfSignature = false;
							//Now look for all the paraemters, the may span multiple lines
							while ( !foundEndOfSignature )
							{
								//Trim whitespace from both sides of the line
								line = line.Trim();

								//First we want to get everything right of the paren
								string[] paramTokens;
								if ( line.Contains( '(' ) )
								{
									string[] lineTokens = line.Split( ')' );
									paramTokens = lineTokens[ 1 ].Split( ',' );
								}
								else
								{
									paramTokens = line.Split( ',' );
								}

								foreach ( var itParam in paramTokens )
								{
									functionSig += itParam;

									if ( !itParam.Contains( ')' ) )
									{
										functionSig += ",";
									}
								}
								if ( line.Contains( ')' ) )
								{
									foundEndOfSignature = true;
								}
								line = reader.ReadLine();
							}
							functionInvoc = CreateInvocationFromString( functionSig );

							//Ok, now if we have founc the signature, iterate throug the file until we find the found
							//of the function
							bool foundEndOfBody = false;
							int braceCount = 0;
							while ( !foundEndOfBody )
							{
								functionBody += line;

								if ( line.Contains( '{' ) )
								{
									braceCount++;
								}

								if ( line.Contains( '}' ) )
								{
									braceCount--;
								}

								if ( braceCount == 0 )
								{
									foundEndOfBody = true;

									//Remove first and last brace
									int pos = -1;
									for ( int i = 0; i < functionBody.Length; i++ )
									{
										if ( functionBody[ i ] == '{' )
										{
											pos = i;
											break;
										}
									}
									functionBody.Remove( pos, 1 );
									functionCacheMap.Add( functionInvoc, functionBody );
								}
								functionBody += "\n";
								line = reader.ReadLine();
							}
						}
					}
				}
			}

			reader.Close();
		}

		private FunctionInvocation CreateInvocationFromString( string input )
		{
			string functionName, returnType;
			FunctionInvocation invoc = null;

			//Get the function name and return type
			var leftTokens = input.Split( '(' );
			var leftTokens2 = leftTokens[ 0 ].Split( ' ' );
			leftTokens2[ 0 ] = leftTokens2[ 0 ].Trim();
			leftTokens2[ 1 ] = leftTokens2[ 1 ].Trim();
			returnType = leftTokens2[ 0 ];
			functionName = leftTokens2[ 1 ];


			invoc = new FunctionInvocation( functionName, 0, 0, returnType );

			string[] parameters;
			int lparen_pos = -1;
			for ( int i = 0; i < input.Length; i++ )
			{
				if ( input[ i ] == '(' )
				{
					lparen_pos = i;
					break;
				}
			}
			if ( lparen_pos != -1 )
			{
				string[] tokens = input.Split( '(' );
				parameters = tokens[ 1 ].Split( ',' );
			}
			else
			{
				parameters = input.Split( ',' );
			}
			for ( int i = 0; i < parameters.Length; i++ )
			{
				string itParam = parameters[ i ];
				itParam = itParam.Replace( ")", string.Empty );
				itParam = itParam.Replace( ",", string.Empty );
				string[] paramTokens = itParam.Split( ' ' );

				// There should be three parts for each token
				// 1. The operand type(in, out, inout)
				// 2. The type
				// 3. The name
				if ( paramTokens.Length == 3 )
				{
					Operand.OpSemantic semantic = Operand.OpSemantic.In;
					GpuProgramParameters.GpuConstantType gpuType = GpuProgramParameters.GpuConstantType.Unknown;

					if ( paramTokens[ 0 ] == "in" )
					{
						semantic = Operand.OpSemantic.In;
					}
					else if ( paramTokens[ 0 ] == "out" )
					{
						semantic = Operand.OpSemantic.Out;
					}
					else if ( paramTokens[ 0 ] == "inout" )
					{
						semantic = Operand.OpSemantic.InOut;
					}

					//Find the internal type based on the string that we're given
					foreach ( var key in gpuConstTypeMap.Keys )
					{
						if ( gpuConstTypeMap[ key ] == paramTokens[ 1 ] )
						{
							gpuType = key;
							break;
						}
					}

					//We need a valid type otherwise glsl compilation will not work
					if ( gpuType == GpuProgramParameters.GpuConstantType.Unknown )
					{
						throw new Core.AxiomException( "Cannot convert Operand.OpMask to GpuConstantType" );
					}
					if ( gpuType == GpuProgramParameters.GpuConstantType.Sampler1D )
					{
						gpuType = GpuProgramParameters.GpuConstantType.Sampler2D;
					}

					var p = new Parameter( gpuType, paramTokens[ 2 ], Parameter.SemanticType.Unknown, i,
					                       Parameter.ContentType.Unknown, 0 );
					invoc.PushOperand( p, semantic, (int)Operand.OpMask.All, 0 );
				}
			}

			return invoc;
		}

		private void WriteProgramDependencies( StreamWriter stream, Program program )
		{
			for ( int i = 0; i < program.DependencyCount; i++ )
			{
				string curDependency = program.GetDependency( i );
				CacheDependencyFunctions( curDependency );
			}

			stream.WriteLine( "//-----------------------------------------------------------------------------" );
			stream.WriteLine( "//                        PROGRAM DEPENDENCIES" );
			stream.WriteLine();

			var forwardDecl = new List<FunctionInvocation>();
			var functionList = program.Functions;
			int itFunction = 0;
			Function curFunction = functionList[ 0 ];
			var atomInstances = curFunction.AtomInstances;
			int itAtom = 0;
			int itAtomEnd = atomInstances.Count;

			//Now iterate over all function atoms
			for ( ; itAtom != itAtomEnd; itAtom++ )
			{
				//Skip non function invocation atom
				if ( atomInstances[ itAtom ] is FunctionInvocation == false )
				{
					continue;
				}

				var funcInvoc = atomInstances[ itAtom ] as FunctionInvocation;
				forwardDecl.Add( funcInvoc );

				// Now look into that function for other non-builtin functions and add them to the declaration list
				// Look for non-builtin functions
				// Do so by assuming that these functions do not have several variations.
				// Also, because GLSL is C based, functions must be defined before they are used
				// so we can make the assumption that we already have this function cached.
				//
				// If we find a function, look it up in the map and write it out
				DiscoverFunctionDependencies( funcInvoc, forwardDecl );
			}

			//Now remove duplicate declarations
			forwardDecl.Sort();
			forwardDecl = forwardDecl.Distinct( new FunctionInvocation.FunctionInvocationComparer() ).ToList();

			for ( int i = 0; i < program.DependencyCount; i++ )
			{
				string curDependency = program.GetDependency( i );

				foreach ( var key in definesMap.Keys )
				{
					if ( definesMap[ key ] == curDependency )
					{
						stream.Write( definesMap[ key ] );
						stream.Write( "\n" );
					}
				}
			}
			// Parse the source shader and write out only the needed functions
			foreach ( var it in forwardDecl )
			{
				var invoc = new FunctionInvocation( string.Empty, 0, 0, string.Empty );

				string body = string.Empty;

				//find the function in the cache
				foreach ( var key in functionCacheMap.Keys )
				{
					if ( !( it == key ) )
					{
						continue;
					}

					invoc = key;
					body = functionCacheMap[ key ];
					break;
				}

				if ( invoc.FunctionName.Length > 0 )
				{
					//Write out the funciton name from the cached FunctionInvocation
					stream.Write( invoc.ReturnType );
					stream.Write( " " );
					stream.Write( invoc.FunctionName );
					stream.Write( "(" );

					int itOperand = 0;
					int itOperandEnd = invoc.OperandList.Count;

					while ( itOperand != itOperandEnd )
					{
						Operand op = invoc.OperandList[ itOperand ];
						Operand.OpSemantic opSemantic = op.Semantic;
						string paramName = op.Parameter.Name;
						int opMask = op.Mask;
						GpuProgramParameters.GpuConstantType gpuType = GpuProgramParameters.GpuConstantType.Unknown;

						switch ( opSemantic )
						{
							case Operand.OpSemantic.In:
								stream.Write( "in " );
								break;
							case Operand.OpSemantic.Out:
								stream.Write( "out " );
								break;
							case Operand.OpSemantic.InOut:
								stream.Write( "inout " );
								break;
							default:
								break;
						}

						//Swizzle masks are onluy defined for types like vec2, vec3, vec4
						if ( opMask == (int)Operand.OpMask.All )
						{
							gpuType = op.Parameter.Type;
						}
						else
						{
							gpuType = Operand.GetGpuConstantType( opMask );
						}

						//We need a valid type otherwise glsl compilation will not work
						if ( gpuType == GpuProgramParameters.GpuConstantType.Unknown )
						{
							throw new Core.AxiomException( "Cannot convert Operand.OpMask to GpuConstantType" );
						}

						stream.Write( gpuConstTypeMap[ gpuType ] + " " + paramName );

						itOperand++;

						//Prepare for the next operand
						if ( itOperand != itOperandEnd )
						{
							stream.Write( ", " );
						}
					}
					stream.WriteLine();
					stream.WriteLine( "{" );
					stream.WriteLine( body );
					stream.WriteLine( "}" );
					stream.WriteLine();
				}
			}
		}

		private void WriteLocalParameter( StreamWriter stream, Parameter parameter )
		{
			stream.Write( gpuConstTypeMap[ parameter.Type ] );
			stream.Write( "\t" );
			stream.Write( parameter.Name );
			if ( parameter.IsArray )
			{
				stream.Write( "[" + parameter.Size.ToString() + "]" );
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
					paramName.Remove( 0 );
					paramName.Insert( 0, "o" );

					stream.Write( "varying\t" );
					stream.Write( gpuConstTypeMap[ param.Type ] );
					stream.Write( "\t" );
					stream.Write( paramName );
					stream.WriteLine( ";" );
				}
				else if ( gpuType == GpuProgramType.Vertex &&
				          contentToPerVertexAttributes.ContainsKey( paramContent ) )
				{
					// Due the fact that glsl does not have register like cg we have to rename the params
					// according their content.
					inputToGLStatesMap.Add( paramName, contentToPerVertexAttributes[ paramContent ] );
					stream.Write( "attribute\t" );

					//All uv texcoords passed by Axiom are vec4
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
					//GLSL Vertex program has to write always gl_Position
					if ( param.Content == Parameter.ContentType.PositionProjectiveSpace )
					{
						inputToGLStatesMap.Add( param.Name, "outputPosition" );
					}
					else
					{
						stream.Write( "varying\t" );
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
					//GLSL ES fragment program has to always write gl_FragColor
					inputToGLStatesMap.Add( param.Name, "outputColor" );
				}
			}
		}

		private bool IsBasicType( string type )
		{
			if ( type == "void" ||
			     type == "float" ||
			     type == "vec2" ||
			     type == "vec3" ||
			     type == "vec4" ||
			     type == "sampler2D" ||
			     type == "samplerCube" ||
			     type == "mat2" ||
			     type == "mat3" ||
			     type == "mat4" ||
			     type == "int" ||
			     type == "int2" ||
			     type == "int3" ||
			     type == "int4" )
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private void DiscoverFunctionDependencies( FunctionInvocation invoc, List<FunctionInvocation> depVector )
		{
			string body = string.Empty;
			foreach ( var key in functionCacheMap.Keys )
			{
				if ( invoc == key )
				{
					continue;
				}

				body = functionCacheMap[ key ];
				break;
			}

			if ( body != string.Empty )
			{
				//Trim whitespace
				body = body.Trim();
				string[] tokens = body.Split( '(' );

				foreach ( var it in tokens )
				{
					string[] moreTokens = it.Split( ' ' );

					foreach ( var key in functionCacheMap.Keys )
					{
						if ( key.FunctionName == moreTokens[ moreTokens.Length - 1 ] )
						{
							//Add the function declaration
							depVector.Add( key );

							DiscoverFunctionDependencies( key, depVector );
						}
					}
				}
			}
			else
			{
				Axiom.Core.LogManager.Instance.DefaultLog.Write( "ERROR: Cached function not found " +
				                                                 invoc.FunctionName );
			}
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		private static bool CompareUniformByName( UniformParameter param, string str )
		{
			return param.Name == str;
		}
	}
}