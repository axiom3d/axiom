using System;
using Axiom.Core;
using Axiom.SubSystems.Rendering;
using Microsoft.DirectX;
using D3D = Microsoft.DirectX.Direct3D;

namespace RenderSystem_DirectX9
{
	/// <summary>
	/// 	Summary description for D3DGpuProgramManager.
	/// </summary>
	public class D3DGpuProgramManager : GpuProgramManager
	{
		#region Member variables
		
        protected D3D.Device device;

		#endregion
		
		#region Constructors
		
		public D3DGpuProgramManager(D3D.Device device) : base() {
            this.device = device;
		}
		
		#endregion
		
		#region Methods

        /// <summary>
        ///    Create the specified type of GpuProgram.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override GpuProgram Create(string name, GpuProgramType type) {
            switch(type) {
                case GpuProgramType.Vertex:
                    return new D3DVertexProgram(name, device);

                case GpuProgramType.Fragment:
                    return new D3DFragmentProgram(name, device);
            }

            // if this line is ever reached, I will eat a plate of shit.
            return null;
        }

        /// <summary>
        ///    Returns a specialized version of GpuProgramParameters.
        /// </summary>
        /// <returns></returns>
        public override GpuProgramParameters CreateParameters() {
            return new D3DGpuProgramParameters();
        }

		#endregion
		
		#region Properties
		
		#endregion
	}
}
