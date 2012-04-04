using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Components.RTShaderSystem
{
    class ProgramWriterCGFactory : ProgramWriterFactory
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
