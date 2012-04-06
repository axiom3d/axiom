namespace Axiom.Components.RTShaderSystem
{
    internal class ProgramWriterHLSLFactory : ProgramWriterFactory
    {
        public override string TargetLanguage
        {
            get
            {
                return "hlsl";
            }
        }

        internal override ProgramWriter Create()
        {
            return new HLSLProgramWriter();
        }
    }
}