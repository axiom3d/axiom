namespace Axiom.Components.RTShaderSystem
{
	internal class CGProgramProcessor : ProgramProcessor
	{
		public CGProgramProcessor()
		{
		}


		internal override bool PreCreateGpuPrograms( ProgramSet programSet )
		{
			Program vsProgram = programSet.CpuVertexProgram;
			Program psProgram = programSet.CpuFragmentProgram;

			Function vsMain = vsProgram.EntryPointFunction;
			Function fsMain = psProgram.EntryPointFunction;
			bool success;

			success = ProgramProcessor.CompactVsOutputs( vsMain, fsMain );
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

		public override string TargetLanguage
		{
			get
			{
				return "cg";
			}
		}
	}
}