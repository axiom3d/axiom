using System;
using Axiom.Graphics;
using Axiom.RenderSystems.OpenGL;
using Tao.OpenGl;

namespace Axiom.RenderSystems.OpenGL.ATI {
	/// <summary>
	/// Summary description for ATIFragmentShaderGpuProgram.
	/// </summary>
	public class ATIFragmentShaderGpuProgram : GLGpuProgram {
		public ATIFragmentShaderGpuProgram(string name, GpuProgramType type, string syntaxCode)
            : base(name, type, syntaxCode) {

            programType = Gl.GL_FRAGMENT_SHADER_ATI;
            programId = Ext.glGenFragmentShadersATI(1);
		}

        #region Implementation of GpuProgram

        protected override void LoadFromSource() {
            PixelShader assembler = new PixelShader();

            bool error = false;

            if(!error) {
                Ext.glBindFragmentShaderATI(programId);
                Ext.glBeginFragmentShaderATI();

                // TODO: Compile

                Ext.glEndFragmentShaderATI();
            }
            else {
            }
        }

        public override void Unload() {
            base.Unload ();

            // delete the fragment shader for good
            Ext.glDeleteFragmentShaderATI(programId);
        }


        #endregion Implementation of GpuProgram

        #region Implementation of GLGpuProgram

        public override void Bind() {
            Ext.glBindFragmentShaderATI(programId);
        }

        public override void BindParameters(GpuProgramParameters parms) {

            // program constants done internally by compiler for local

            if(parms.HasFloatConstants) {

                for(int i = 0; i < parms.FloatConstantCount; i++) {
                    int index = parms.GetFloatConstantIndex(i);
                    Axiom.MathLib.Vector4 vec4 = parms.GetFloatConstant(i);

                    tempProgramFloats[0] = vec4.x;
                    tempProgramFloats[1] = vec4.y;
                    tempProgramFloats[2] = vec4.z;
                    tempProgramFloats[3] = vec4.w;

                    // send the params 4 at a time
                    Ext.glSetFragmentShaderConstantATI(index, tempProgramFloats);
                }
            }   
        }

        public override void Unbind() {
        }

        #endregion Implementation of GLGpuProgram
	}

    /// <summary>
    /// 
    /// </summary>
    public class ATIFragmentShaderFactory : IOpenGLGpuProgramFactory {
        #region IOpenGLGpuProgramFactory Members

        public GLGpuProgram Create(string name, Axiom.Graphics.GpuProgramType type, string syntaxCode) {
            // creates and returns a new ATI fragment shader implementation
            return new ATIFragmentShaderGpuProgram(name, type, syntaxCode);
        }

        #endregion

    }

}
