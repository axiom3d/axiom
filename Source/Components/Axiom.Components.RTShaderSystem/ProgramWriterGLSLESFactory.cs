namespace Axiom.Components.RTShaderSystem
{
	internal class ProgramWriterGLSLESFactory : ProgramWriterFactory
	{
		public override string TargetLanguage
		{
			get
			{
				return "glsles";
			}
		}

		internal override ProgramWriter Create()
		{
			return new GLSLESProgramWriter();
		}
	}
}