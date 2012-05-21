namespace Axiom.Components.RTShaderSystem
{
	internal class HLSLProgramProcessor : ProgramProcessor
	{
		public HLSLProgramProcessor()
		{
		}

		public override string TargetLanguage
		{
			get
			{
				return "hlsl";
			}
		}

		internal override bool PreCreateGpuPrograms( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;
			Function vsMain = vsProgram.EntryPointFunction;
			Function psMain = psProgram.EntryPointFunction;
			bool success;

			success = ProgramProcessor.CompactVsOutputs( vsMain, psMain );
			if ( success == false )
			{
				return false;
			}

			return true;
		}

		internal override bool PostCreateGpuPrograms( ProgramSet programSet )
		{
			BindAutoParameters( programSet.CpuVertexProgram, programSet.GpuVertexProgram );

			BindAutoParameters( programSet.CpuFragmentProgram, programSet.GpuFragmentProgram );

			return true;
		}
	}
}