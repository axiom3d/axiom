using System;
using Axiom.Core;
using Axiom.SubSystems.Rendering;

namespace RenderSystem_OpenGL
{
	/// <summary>
	/// 	Summary description for GLGpuProgramManager.
	/// </summary>
	public class GLGpuProgramManager : GpuProgramManager
	{	
		public GLGpuProgramManager() : base() {
		}

        /// <summary>
        ///    Create the specified type of GpuProgram.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override GpuProgram Create(string name, GpuProgramType type) {
            int glType = GLHelper.ConvertEnum(type);

            return new GLGpuProgram(name, type);
        }

        /// <summary>
        ///    Returns a specialized version of GpuProgramParameters.
        /// </summary>
        /// <returns></returns>
        public override GpuProgramParameters CreateParameters() {
            return new GLGpuProgramParameters();
        }
	}
}
