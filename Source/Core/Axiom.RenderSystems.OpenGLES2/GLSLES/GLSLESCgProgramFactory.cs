using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
    class GLSLESCgProgramFactory : HighLevelGpuProgramFactory
    {
        public override string Language
        {
            get { return "cg"; }
        }

        public override HighLevelGpuProgram CreateInstance(Core.ResourceManager creator, string name, ulong handle, string group, bool isManual, Core.IManualResourceLoader loader)
        {
            throw new NotImplementedException();
        }
    }
}
