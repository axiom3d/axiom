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
				return this.vsCpuProgram;
			}
			set
			{
				this.vsCpuProgram = value;
			}
		}

		public Program CpuFragmentProgram
		{
			get
			{
				return this.psCpuProgram;
			}
			set
			{
				this.psCpuProgram = value;
			}
		}

		public Graphics.GpuProgram GpuVertexProgram
		{
			get
			{
				return this.vsGpuProgram;
			}
			set
			{
				this.vsGpuProgram = value;
			}
		}

		public Graphics.GpuProgram GpuFragmentProgram
		{
			get
			{
				return this.psGpuProgram;
			}
			set
			{
				this.psGpuProgram = value;
			}
		}

		public void Dispose()
		{
			if ( this.vsCpuProgram != null )
			{
				ProgramManager.Instance.DestroyCpuProgram( this.vsCpuProgram );
				this.vsCpuProgram = null;
			}
			if ( this.psCpuProgram != null )
			{
				ProgramManager.Instance.DestroyCpuProgram( this.psCpuProgram );
				this.psCpuProgram = null;
			}

			this.vsGpuProgram = null;
			this.psGpuProgram = null;
		}
	}
}