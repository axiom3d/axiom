namespace Axiom.Components.RTShaderSystem
{
	internal class ProgramWriterCGFactory : ProgramWriterFactory
	{
		public override string TargetLanguage
		{
			get
			{
				return "cg";
			}
		}

		internal override ProgramWriter Create()
		{
			return new CGProgramWriter();
		}
	}
}