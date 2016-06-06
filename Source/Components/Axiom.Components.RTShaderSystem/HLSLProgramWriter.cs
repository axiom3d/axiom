using System.Collections.Generic;
using System.IO;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
	internal class HLSLProgramWriter : ProgramWriter
	{
		private Dictionary<GpuProgramParameters.GpuConstantType, string> gpuConstTypeMap;
		private Dictionary<Parameter.SemanticType, string> paramSemanticMap;

		public HLSLProgramWriter()
		{
			InitializeStringMaps();
		}

		private void InitializeStringMaps()
		{
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float1, "float" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float2, "float2" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float3, "float3" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Float4, "float4" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Sampler1D, "sampler1D" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Sampler2D, "sampler2D" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Sampler3D, "sampler3D" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.SamplerCube, "samplerCUBE" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_2X2, "float2x2" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_2X3, "float2x3" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_2X4, "float2x4" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_3X2, "float3x2" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_3X3, "float3x3" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_3X4, "float3x4" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_4X2, "float4x2" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_4X3, "float4x3" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Matrix_4X4, "float4x4" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int1, "int" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int2, "int2" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int3, "int3" );
			this.gpuConstTypeMap.Add( GpuProgramParameters.GpuConstantType.Int4, "int4" );

			this.paramSemanticMap.Add( Parameter.SemanticType.Position, "POSITION" );
			this.paramSemanticMap.Add( Parameter.SemanticType.BlendWeights, "BLENDWEIGHT" );
			this.paramSemanticMap.Add( Parameter.SemanticType.BlendIndicies, "BLENDINDICES" );
			this.paramSemanticMap.Add( Parameter.SemanticType.Normal, "NORMAL" );
			this.paramSemanticMap.Add( Parameter.SemanticType.Color, "COLOR" );
			this.paramSemanticMap.Add( Parameter.SemanticType.TextureCoordinates, "TEXCOORD" );
			this.paramSemanticMap.Add( Parameter.SemanticType.Binormal, "BINORMAL" );
			this.paramSemanticMap.Add( Parameter.SemanticType.Tangent, "TANGENT" );
			this.paramSemanticMap.Add( Parameter.SemanticType.Unknown, "" );
		}

		internal override void WriteSourceCode( System.IO.StreamWriter stream, Program program )
		{
			var functionList = program.Functions;
			var parameterList = program.Parameters;

			//Generate source code header
			WriteProgramTitle( stream, program );
			stream.WriteLine();

			WriteProgramDependencies( stream, program );
			stream.WriteLine();

			WriteUniformParametersTitle( stream, program );
			stream.WriteLine();

			foreach ( var itUniformParam in parameterList )
			{
				WriteUniformParameter( stream, itUniformParam );
				stream.WriteLine( ";" );
			}
			stream.WriteLine();

			foreach ( var curFunction in functionList )
			{
				bool needToTranslateHlsl4Color = false;
				Parameter colorParameter;
				WriteFunctionTitle( stream, curFunction );
				WriteFunctionDeclaration( stream, curFunction, out needToTranslateHlsl4Color, out colorParameter );

				stream.WriteLine( "{" );

				var localParams = curFunction.LocalParameters;
				foreach ( var itParam in localParams )
				{
					stream.Write( "\t" );
					WriteLocalParameter( stream, itParam );
					stream.WriteLine( ";" );
				}

				if ( needToTranslateHlsl4Color )
				{
					stream.WriteLine( "\t" );
					WriteLocalParameter( stream, colorParameter );
					stream.WriteLine( ";" );
					stream.WriteLine();
					stream.WriteLine( "\tFFP_Assign(iHlsl4Color_0, " + colorParameter.Name + ");" );
				}

				curFunction.SortAtomInstances();
				var atomInstances = curFunction.AtomInstances;
				foreach ( var itAtom in atomInstances )
				{
					WriteAtomInstance( stream, itAtom );
				}

				stream.WriteLine( "}" );
			}
			stream.WriteLine();
		}

		private void WriteProgramDependencies( StreamWriter stream, Program program )
		{
			stream.WriteLine( "//-----------------------------------------------------------------------------" );
			stream.WriteLine( "//                        PROGRAM DEPENDENCIES" );
			stream.WriteLine( "//-----------------------------------------------------------------------------" );

			for ( int i = 0; i < program.DependencyCount; i++ )
			{
				string curDependency = program.GetDependency( i );
				stream.WriteLine( "#include " + '\"' + curDependency + "." + TargetLanguage + '\"' );
			}
		}

		private void WriteUniformParameter( StreamWriter stream, UniformParameter parameter )
		{
			stream.WriteLine( this.gpuConstTypeMap[ parameter.Type ] );
			stream.Write( "\t" );
			stream.Write( parameter.Name );
			if ( parameter.IsArray )
			{
				stream.Write( "[" + parameter.Size.ToString() + "]" );
			}
			if ( parameter.IsSampler )
			{
				stream.Write( " : register(s" + parameter.Index + ")" );
			}
		}

		private void WriteFunctionParameter( StreamWriter stream, Parameter parameter )
		{
			stream.Write( this.gpuConstTypeMap[ parameter.Type ] );
			stream.Write( "\t" );
			if ( parameter.IsArray )
			{
				stream.Write( "[" + parameter.Size.ToString() + "]" );
			}
			if ( parameter.Semantic != Parameter.SemanticType.Unknown )
			{
				stream.Write( " : " );
				stream.Write( this.paramSemanticMap[ parameter.Semantic ] );

				if ( parameter.Semantic != Parameter.SemanticType.Position &&
				     parameter.Semantic != Parameter.SemanticType.Normal &&
				     parameter.Semantic != Parameter.SemanticType.BlendIndicies &&
				     parameter.Semantic != Parameter.SemanticType.BlendWeights &&
				     ( !( parameter.Semantic == Parameter.SemanticType.Color && parameter.Index == 0 ) ) &&
				     parameter.Index >= 0 )
				{
					stream.Write( parameter.Index.ToString() );
				}
			}
		}

		private void WriteLocalParameter( StreamWriter stream, Parameter parmeter )
		{
			stream.Write( this.gpuConstTypeMap[ parmeter.Type ] );
			stream.Write( "\t" );
			stream.Write( parmeter.Name );
			if ( parmeter.IsArray )
			{
				stream.Write( "[" + parmeter.Size.ToString() + "]" );
			}
		}

		private void WriteFunctionDeclaration( StreamWriter stream, Function function,
		                                       out bool needToTranslateHlsl4Color, out Parameter colorParameter )
		{
			colorParameter = null;
			needToTranslateHlsl4Color = false;

			var inParams = function.InputParameters;
			var outParams = function.OutputParameters;

			stream.Write( "void" );
			stream.Write( " " );

			stream.Write( function.Name );
			stream.WriteLine();
			stream.WriteLine( "\t(" );

			bool isVs4 = GpuProgramManager.Instance.IsSyntaxSupported( "vs_4_0" );

			int curParamIndex = 0;
			//Write input parameters
			foreach ( var it in inParams )
			{
				stream.Write( "\t in " );

				if ( isVs4 &&
				     function.FuncType == Function.FunctionType.VsMain &&
				     it.Semantic == Parameter.SemanticType.Color )
				{
					stream.Write( "unsigned int iHlsl4Color_0 : COLOR" );
					needToTranslateHlsl4Color = true;
					colorParameter = it;
				}
				else
				{
					WriteFunctionParameter( stream, it );
				}

				if ( curParamIndex != inParams.Count + outParams.Count )
				{
					stream.WriteLine( ", " );
				}

				curParamIndex++;
			}

			//Write output parameters
			foreach ( var it in outParams )
			{
				stream.Write( "\t out " );
				if ( isVs4 && function.FuncType == Function.FunctionType.PsMain )
				{
					stream.WriteLine( this.gpuConstTypeMap[ it.Type ] + " " + it.Name + " : SV_Target" );
				}
				else
				{
					WriteFunctionParameter( stream, it );
				}

				if ( curParamIndex + 1 != inParams.Count + outParams.Count )
				{
					stream.WriteLine( ", " );
				}
				curParamIndex++;
			}

			stream.WriteLine();
			stream.WriteLine( "\t" );
		}

		private void WriteAtomInstance( StreamWriter stream, FunctionAtom atom )
		{
			stream.WriteLine();
			stream.Write( "\t" );
			atom.WriteSourceCode( stream, TargetLanguage );
			stream.WriteLine();
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		internal override string TargetLanguage
		{
			get
			{
				return "hlsl";
			}
		}
	}
}