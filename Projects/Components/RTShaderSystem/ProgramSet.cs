using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
    internal class ProgramSet : IDisposable
    {

        Program vsCpuProgram, psCpuProgram;
        GpuProgram vsGpuProgram, psGpuProgram;

        public ProgramSet()
        { }

        public Program CpuVertexProgram
        {
            get { return vsCpuProgram; }
            set { vsCpuProgram = value; }
        }

        public Program CpuFragmentProgram
        {
            get { return psCpuProgram; }
            set { psCpuProgram = value; }
        }

        public Graphics.GpuProgram GpuVertexProgram
        {
            get { return vsGpuProgram; }
            set { vsGpuProgram = value; }
        }
        public Graphics.GpuProgram GpuFragmentProgram
        {
            get { return psGpuProgram; }
            set { psGpuProgram = value; }
        }

        public void Dispose()
        {
            if (vsCpuProgram != null)
            {
                ProgramManager.Instance.DestroyCpuProgram(vsCpuProgram);
                vsCpuProgram = null;
            }
            if (psCpuProgram != null)
            {
                ProgramManager.Instance.DestroyCpuProgram(psCpuProgram);
                psCpuProgram = null;
            }

            vsGpuProgram = null;
            psGpuProgram = null;
        }
    }
}
