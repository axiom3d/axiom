using System.Collections.Generic;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
	internal class GLSLESProgramProcessor : ProgramProcessor
	{
		private List<string> libraryPrograms;

		public GLSLESProgramProcessor()
		{
		}

		public override string TargetLanguage
		{
			get
			{
				return "glsles";
			}
		}

		internal override bool PreCreateGpuPrograms( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program fsProgram = programSet.CpuFragmentProgram;
			Function vsMain = vsProgram.EntryPointFunction;
			Function fsMain = fsProgram.EntryPointFunction;
			bool success;

			//Compact vertex shader outputs
			success = ProgramProcessor.CompactVsOutputs( vsMain, fsMain );
			if ( success == false )
			{
				return false;
			}

			return true;
		}

		internal override bool PostCreateGpuPrograms( ProgramSet programSet )
		{
			Program vsCpuProgram = programSet.CpuVertexProgram;
			GpuProgram vsGpuProgram = programSet.GpuVertexProgram;
			Program fsCpuProgram = programSet.CpuVertexProgram;
			GpuProgram fsGpuProgram = programSet.GpuFragmentProgram;

			BindAutoParameters( programSet.CpuVertexProgram, programSet.GpuVertexProgram );

			BindAutoParameters( programSet.CpuFragmentProgram, programSet.GpuFragmentProgram );

			BindTextureSamplers( vsCpuProgram, vsGpuProgram );

			BindTextureSamplers( fsCpuProgram, fsGpuProgram );

			return true;
		}

		private void BindTextureSamplers( Program cpuProgram, GpuProgram gpuProgram )
		{
			var gpuParams = gpuProgram.DefaultParameters;
			var progParams = cpuProgram.Parameters;

			//bind the samplers
			foreach ( var curParam in progParams )
			{
				if ( curParam.IsSampler )
				{
					// The optimizer may remove some unnecessary parameters, so we should ignore them
					gpuParams.IgnoreMissingParameters = true;
					gpuParams.SetNamedConstant( curParam.Name, curParam.Index );
				}
			}
		}

		public override void Dispose()
		{
			for ( int i = 0; i < libraryPrograms.Count; i++ )
			{
				HighLevelGpuProgramManager.Instance.Remove( libraryPrograms[ i ] );
			}
			libraryPrograms.Clear();
		}
	}
}