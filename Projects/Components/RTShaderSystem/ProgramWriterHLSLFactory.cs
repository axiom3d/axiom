using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Components.RTShaderSystem
{
    class ProgramWriterHLSLFactory : ProgramWriterFactory
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
