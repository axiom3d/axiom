using System.Collections.Generic;
using System.IO;

namespace Axiom.Components.RTShaderSystem
{
	internal class CGProgramWriter : ProgramWriter
	{
		private Dictionary<Axiom.Graphics.GpuProgramParameters.GpuConstantType, string> gpuConstTypeMap;
		private Dictionary<Parameter.SemanticType, string> paramSemanticMap;

		public CGProgramWriter()
		{
			InitializeStringMaps();
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		internal override void WriteSourceCode( System.IO.StreamWriter stream, Program program )
		{
			var functionList = program.Functions;
			var parameterList = program.Parameters;

			//Generate source code header
			WriteProgramTitle( stream, program );
			stream.WriteLine();
			//Generate dependencies
			WriteProgramDependencies( stream, program );
			stream.WriteLine();

			//Generate global variable code.
			WriteUniformParametersTitle( stream, program );
			stream.WriteLine();

			foreach ( var itUniformParam in parameterList )
			{
				WriteUniformParameter( stream, itUniformParam );
				stream.Write( ";\n" );
			}
			stream.WriteLine();

			//Write program function(s)
			foreach ( var curFunction in functionList )
			{
				Parameter colorParameter;

				WriteFunctionTitle( stream, curFunction );
				WriteFunctionDeclaration( stream, curFunction, out colorParameter );

				stream.Write( "{\n" );


				//Write local parameters
				var localParams = curFunction.LocalParameters;
				foreach ( var itParam in localParams )
				{
					stream.Write( "\t" );
					WriteLocalParameter( stream, itParam );
					stream.Write( ";\n" );
				}

				//Sort and write function atoms
				curFunction.SortAtomInstances();
				var atomInstances = curFunction.AtomInstances;
				foreach ( var itAtom in atomInstances )
				{
					WriteAtomInstance( stream, itAtom );
				}

				stream.Write( "}\n" );
			}
			stream.WriteLine();
		}

		private void InitializeStringMaps()
		{
			gpuConstTypeMap = new Dictionary<Graphics.GpuProgramParameters.GpuConstantType, string>();
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Float1, "float" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Float2, "float2" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Float3, "float3" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Float4, "float4" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Sampler1D, "sampler1D" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Sampler2D, "sampler2D" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Sampler3D, "sampler3D" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.SamplerCube, "samplerCUBE" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Matrix_2X2, "float2x2" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Matrix_2X3, "float2x3" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Matrix_2X4, "float2x4" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Matrix_3X2, "float3x2" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Matrix_3X3, "float3x3" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Matrix_3X4, "float3x4" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X2, "float4x2" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X3, "float4x3" );
			gpuConstTypeMap.Add( Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4, "float4x4" );

			paramSemanticMap = new Dictionary<Parameter.SemanticType, string>();
			paramSemanticMap.Add( Parameter.SemanticType.Position, "POSITION" );
			paramSemanticMap.Add( Parameter.SemanticType.BlendWeights, "BLENDWEIGHT" );
			paramSemanticMap.Add( Parameter.SemanticType.BlendIndicies, "BLENDINDICES" );
			paramSemanticMap.Add( Parameter.SemanticType.Normal, "NORMAL" );
			paramSemanticMap.Add( Parameter.SemanticType.Color, "COLOR" );
			paramSemanticMap.Add( Parameter.SemanticType.TextureCoordinates, "TEXCOORD" );
			paramSemanticMap.Add( Parameter.SemanticType.Binormal, "BINORMAL" );
			paramSemanticMap.Add( Parameter.SemanticType.Tangent, "TANGENT" );
			paramSemanticMap.Add( Parameter.SemanticType.Unknown, string.Empty );
		}

		private void WriteProgramDependencies( StreamWriter stream, Program program )
		{
			stream.WriteLine( "//-----------------------------------------------------------------------------" );
			stream.WriteLine( "//                    PROGRAM DEPENDENCIES" );
			stream.WriteLine( "//-----------------------------------------------------------------------------" );

			for ( int i = 0; i < program.DependencyCount; i++ )
			{
				string curDependency = program.GetDependency( i );

				stream.WriteLine( "#include " + '\"' + curDependency + "." + TargetLanguage + '\"' );
			}
		}

		private void WriteUniformParameter( StreamWriter stream, UniformParameter parameter )
		{
			stream.Write( gpuConstTypeMap[ parameter.Type ] );
			stream.Write( "\t" );
			stream.Write( parameter.Name );
			if ( parameter.IsArray )
			{
				stream.Write( "[" + parameter.Size + "]" );
			}

			if ( parameter.IsSampler )
			{
				stream.Write( " : register(s" + parameter.Index + ")" );
			}
		}

		private void WriteFunctionParameter( StreamWriter stream, Parameter parameter )
		{
			stream.Write( gpuConstTypeMap[ parameter.Type ] );
			stream.Write( "\t" );
			stream.Write( parameter.Name );

			if ( parameter.IsArray )
			{
				stream.Write( "[" + parameter.Size + "]" );
			}

			if ( parameter.Semantic != Parameter.SemanticType.Unknown )
			{
				stream.Write( " : " );
				stream.Write( paramSemanticMap[ parameter.Semantic ] );

				if ( parameter.Semantic != Parameter.SemanticType.Position &&
				     parameter.Semantic != Parameter.SemanticType.Normal &&
				     parameter.Semantic != Parameter.SemanticType.Tangent &&
				     parameter.Semantic != Parameter.SemanticType.BlendIndicies &&
				     parameter.Semantic != Parameter.SemanticType.BlendWeights &&
				     ( !( parameter.Semantic == Parameter.SemanticType.Color && parameter.Index == 0 ) ) &&
				     parameter.Index >= 0 )
				{
					stream.Write( parameter.Index.ToString() );
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

		private void WriteFunctionDeclaration( StreamWriter stream, Function function, out Parameter colorParam )
		{
			colorParam = null;

			var inParams = function.InputParameters;
			var outParams = function.OutputParameters;

			stream.Write( "void" );
			stream.Write( " " );

			stream.Write( function.Name );
			stream.WriteLine();
			stream.WriteLine( "\t(" );

			int curParamIndex = 0;
			int paramsCount = inParams.Count + outParams.Count;
			foreach ( var it in inParams )
			{
				stream.Write( "\t in" );
				WriteFunctionParameter( stream, it );

				if ( curParamIndex + 1 != paramsCount )
				{
					stream.WriteLine( ", " );
				}

				curParamIndex++;
			}

			foreach ( var it in outParams )
			{
				stream.Write( "\t out" );
				WriteFunctionParameter( stream, it );

				if ( curParamIndex + 1 != paramsCount )
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

		internal override string TargetLanguage
		{
			get
			{
				return "cg";
			}
		}
	}
}