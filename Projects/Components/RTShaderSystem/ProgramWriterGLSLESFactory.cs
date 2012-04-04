using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Components.RTShaderSystem
{
    class ProgramWriterGLSLESFactory : ProgramWriterFactory
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
