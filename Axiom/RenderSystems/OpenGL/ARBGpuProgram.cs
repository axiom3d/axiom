using System;
using System.Runtime.InteropServices;
using Axiom.Graphics;
using Axiom.RenderSystems.OpenGL;
using Tao.OpenGl;

namespace Axiom.RenderSystems.OpenGL.ARB {
	/// <summary>
	/// Summary description for ARBGpuProgram.
	/// </summary>
	public class ARBGpuProgram : GLGpuProgram {

		public ARBGpuProgram(string name, GpuProgramType type, string syntaxCode)
            : base(name, type, syntaxCode) {

            // set the type of program for ARB
            programType = (type == GpuProgramType.Vertex) ? Gl.GL_VERTEX_PROGRAM_ARB : Gl.GL_FRAGMENT_PROGRAM_ARB;

            // generate a new program
            Gl.glGenProgramsARB(1, out programId);
		}

        #region Implementation of GpuProgram

        /// <summary>
        ///     Load Assembler gpu program source.
        /// </summary>
        protected override void LoadFromSource() {
            Gl.glBindProgramARB(programType, programId);
     
            Gl.glProgramStringARB(programType, Gl.GL_PROGRAM_FORMAT_ASCII_ARB, source.Length, source);

            // check for any errors
            if(Gl.glGetError() == Gl.GL_INVALID_OPERATION) {
                int pos;
                string error;

                Gl.glGetIntegerv(Gl.GL_PROGRAM_ERROR_POSITION_ARB, out pos);
                error = Marshal.PtrToStringAnsi(Gl.glGetString(Gl.GL_PROGRAM_ERROR_STRING_ARB));

                throw new Exception(string.Format("Error on line {0} in program '{1}'\nError: {2}", pos, name, error));
            }
        }

        /// <summary>
        ///     Unload GL gpu programs.
        /// </summary>
        public override void Unload() {
            base.Unload ();

            Gl.glDeleteProgramsARB(1, ref programId);
        }

        #endregion Implementation of GpuProgram

        #region Implementation of GLGpuProgram

        public override void Bind() {
            Gl.glEnable(programType);
            Gl.glBindProgramARB(programType, programId);
        }

        public override void Unbind() {
            Gl.glBindProgramARB(programType, 0);
            Gl.glDisable(programType);
        }

        public override void BindParameters(GpuProgramParameters parms) {
            if(parms.HasFloatConstants) {

                for(int i = 0; i < parms.FloatConstantCount; i++) {
                    int index = parms.GetFloatConstantIndex(i);
                    Axiom.MathLib.Vector4 vec4 = parms.GetFloatConstant(i);

                    tempProgramFloats[0] = vec4.x;
                    tempProgramFloats[1] = vec4.y;
                    tempProgramFloats[2] = vec4.z;
                    tempProgramFloats[3] = vec4.w;

                    // send the params 4 at a time
                    Gl.glProgramLocalParameter4fvARB(programType, index, tempProgramFloats);
                }
            }            
        }

        #endregion Implementation of GLGpuProgram
	}

    /// <summary>
    ///     Creates a new ARB gpu program.
    /// </summary>
    public class ARBGpuProgramFactory : IOpenGLGpuProgramFactory {
        public GLGpuProgram Create(string name, GpuProgramType type, string syntaxCode) {
            return new ARBGpuProgram(name, type, syntaxCode);
        }
    }
}
