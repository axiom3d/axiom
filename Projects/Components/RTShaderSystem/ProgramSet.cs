using System;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
    public class ProgramSet : IDisposable
    {
        private Program vsCpuProgram, psCpuProgram;
        private GpuProgram vsGpuProgram, psGpuProgram;

        public ProgramSet()
        {
        }

        public Program CpuVertexProgram
        {
            get
            {
                return vsCpuProgram;
            }
            set
            {
                vsCpuProgram = value;
            }
        }

        public Program CpuFragmentProgram
        {
            get
            {
                return psCpuProgram;
            }
            set
            {
                psCpuProgram = value;
            }
        }

        public Graphics.GpuProgram GpuVertexProgram
        {
            get
            {
                return vsGpuProgram;
            }
            set
            {
                vsGpuProgram = value;
            }
        }

        public Graphics.GpuProgram GpuFragmentProgram
        {
            get
            {
                return psGpuProgram;
            }
            set
            {
                psGpuProgram = value;
            }
        }

        public void Dispose()
        {
            if ( vsCpuProgram != null )
            {
                ProgramManager.Instance.DestroyCpuProgram( vsCpuProgram );
                vsCpuProgram = null;
            }
            if ( psCpuProgram != null )
            {
                ProgramManager.Instance.DestroyCpuProgram( psCpuProgram );
                psCpuProgram = null;
            }

            vsGpuProgram = null;
            psGpuProgram = null;
        }
    }
}