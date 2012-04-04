using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Components.RTShaderSystem
{
    class ProgramWriterGLSLFactory : ProgramWriterFactory
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
