using System;
using System.Diagnostics;
using Axiom.Core;
using Axiom.Exceptions;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;

namespace RenderSystem_DirectX9
{
	/// <summary>
	/// 	Direct3D implementation of a few things common to low-level vertex & fragment programs
	/// </summary>
	public abstract class D3DGpuProgram : GpuProgram
	{
        /// <summary>
        ///    Reference to the current D3D device object.
        /// </summary>
        protected D3D.Device device;
		
		public D3DGpuProgram(string name, GpuProgramType type, D3D.Device device) : base(name, type) {
            this.device = device;
		}
	}

    /// <summary>
    ///    Direct3D implementation of low-level vertex programs.
    /// </summary>
    public class D3DVertexProgram : D3DGpuProgram {
        /// <summary>
        ///    Reference to the current D3D VertexShader object.
        /// </summary>
        protected D3D.VertexShader vertexShader;

        internal D3DVertexProgram(string name, D3D.Device device) : base(name, GpuProgramType.Vertex, device) {}

        protected override void LoadFromSource() {
            string errors;

            GraphicsStream code = ShaderLoader.FromString(source, null, 0, out errors);

            if(errors != null) {
                string msg = string.Format("Error while compiling vertex shader '{0}':\n {1}", name, errors);
                throw new AxiomException(msg);
            }

            vertexShader = new VertexShader(device, code);
        }

        public override void Unload() {
            base.Unload ();

            vertexShader.Dispose();
        }

        /// <summary>
        ///    Used internally by the D3DRenderSystem to get a reference to the underlying
        ///    VertexShader object.
        /// </summary>
        internal D3D.VertexShader VertexShader {
            get {
                return vertexShader;
            }
        }
    }

    /// <summary>
    ///    Direct3D implementation of low-level vertex programs.
    /// </summary>
    public class D3DFragmentProgram : D3DGpuProgram {
        /// <summary>
        ///    Reference to the current D3D PixelShader object.
        /// </summary>
        protected D3D.PixelShader pixelShader;

        internal D3DFragmentProgram(string name, D3D.Device device) : base(name, GpuProgramType.Fragment, device) {}

        protected override void LoadFromSource() {
            string errors;

            GraphicsStream code = ShaderLoader.FromString(source, null, 0, out errors);

            if(errors != null) {
                string msg = string.Format("Error while compiling pixel shader '{0}':\n {1}", name, errors);
                throw new AxiomException(msg);
            }

            pixelShader = new PixelShader(device, code);
        }

        public override void Unload() {
            base.Unload();

            pixelShader.Dispose();
        }

        /// <summary>
        ///    Used internally by the D3DRenderSystem to get a reference to the underlying
        ///    PixelShader object.
        /// </summary>
        internal D3D.PixelShader PixelShader {
            get {
                return pixelShader;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class D3DGpuProgramParameters : GpuProgramParameters {
        public override void SetConstant(int index, ref Matrix4 val) {
            // TODO: Verify
            Matrix4 temp = val.Transpose();
            float[] floats = new float[16];
            temp.MakeFloatArray(floats);
            SetConstant(index, floats);
        }
    }
}
