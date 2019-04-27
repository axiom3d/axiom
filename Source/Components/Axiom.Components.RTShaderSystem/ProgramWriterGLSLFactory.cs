namespace Axiom.Components.RTShaderSystem
{
    internal class ProgramWriterGLSLFactory : ProgramWriterFactory
    {
        public override string TargetLanguage
        {
            get
            {
                return "glsl";
            }
        }

        internal override ProgramWriter Create()
        {
            return new GLSLProgramWriter();
        }
    }
}