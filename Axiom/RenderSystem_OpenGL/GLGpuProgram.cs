using System;
using Axiom.Core;
using Axiom.SubSystems.Rendering;
using Tao.OpenGl;

namespace RenderSystem_OpenGL
{
	/// <summary>
	/// 	Specialization of vertex/fragment programs for OpenGL.
	/// </summary>
	public class GLGpuProgram : GpuProgram
	{
        /// <summary>
        ///    Internal OpenGL id assigned to this program.
        /// </summary>
        protected int programId;

        /// <summary>
        ///    Type of this program (vertex or fragment).
        /// </summary>
        protected int programType;

        internal GLGpuProgram(string name, GpuProgramType type) : base(name, type) {
            programType = GLHelper.ConvertEnum(type);

            Ext.glGenProgramsARB(1, out programId);
        }

        protected override void LoadFromSource() {
            Ext.glBindProgramARB(programType, programId);
     
            Ext.glProgramStringARB(programType, Gl.GL_PROGRAM_FORMAT_ASCII_ARB, source.Length, source);

            // check for any errors
            if(Gl.glGetError() == Gl.GL_INVALID_OPERATION) {
                int pos;
                string error;

                Gl.glGetIntegerv(Gl.GL_PROGRAM_ERROR_POSITION_ARB, out pos);
                error = Gl.glGetString(Gl.GL_PROGRAM_ERROR_STRING_ARB);

                throw new Exception(string.Format("Error on line {0} in program '{1}'\nError: {2}", pos, name, error));
            }
        }

        public override void Unload() {
            base.Unload ();

            Ext.glDeleteProgramsARB(1, ref programId);
        }

        /// <summary>
        ///    Access to the internal program id.
        /// </summary>
        public int ProgramID {
            get {
                return programId;
            }
        }

        /// <summary>
        ///    Gets the program type (GL_VERTEX_PROGRAM_ARB, GL_FRAGMENT_PROGRAM_ARB);
        /// </summary>
        public int GLProgramType {
            get {
                return programType;
            }
        }
	}

    /// <summary>
    ///    Customized parameters class for OpenGL.
    /// </summary>
    public class GLGpuProgramParameters : GpuProgramParameters {
        public override void SetConstant(int index, ref Axiom.MathLib.Matrix4 val) {
            // TODO: Verify
            float[] floats = new float[16];
            val.MakeFloatArray(floats);
            SetConstant(index, floats);
        }

    }
}
