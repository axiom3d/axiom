using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
    class GLSLESGpuProgram : GLES2GpuProgram
    {
        private GLSLESProgram glslProgram;

        private static int VertexShaderCount = 0;
        private static int FragmentShaderCount = 0;

        private int linked;

        public GLSLESGpuProgram(GLSLESProgram parent)
            :base(parent.Creator, parent.Name, parent.Handle, parent.Group, false, null)
        {
            this.glslProgram = parent;

            type = parent.Type;
            syntaxCode = "glsles";

            linked = 0;

            if (parent.Type == Graphics.GpuProgramType.Vertex)
            {
                _programID = ++VertexShaderCount;
            }
            else if (parent.Type == Graphics.GpuProgramType.Fragment)
            {
                _programID = ++FragmentShaderCount;
            }

            isSkeletalAnimationIncluded = glslProgram.IsSkeletalAnimationIncluded;
            LoadFromFile = false;
        }
        ~GLSLESGpuProgram()
        {
            unload();
        }
        public override void BindProgram()
        {
            switch (type)
            {
                case Axiom.Graphics.GpuProgramType.Vertex:
                    GLSLESLinkProgramManager.Instance.ActiveVertexShader = this;
                    break;
                case Axiom.Graphics.GpuProgramType.Fragment:
                    GLSLESLinkProgramManager.Instance.ActiveFragmentShader = this;
                    break;
                case Axiom.Graphics.GpuProgramType.Geometry:
                default:
                    break;
            }
        }
        public override void UnbindProgram()
        {
            if (type == Graphics.GpuProgramType.Vertex)
            {
                GLSLESLinkProgramManager.Instance.ActiveVertexShader = null;
            }
            else if (type == Graphics.GpuProgramType.Fragment)
            {
                GLSLESLinkProgramManager.Instance.ActiveFragmentShader = null;
            }
        }
        public override void BindProgramParameters(Graphics.GpuProgramParameters parms, uint mask)
        {
            //Link can throw exceptions, ignore them at this pioont
            try
            {
                GLSLESLinkProgram linkProgram = GLSLESLinkProgramManager.Instance.ActiveLinkProgram;
                linkProgram.UpdateUniforms(parms, (int)mask, type);
            }
            catch
            {
            }
        }
        public override void BindProgramPassIterationParameters(Graphics.GpuProgramParameters parms)
        {
            GLSLESLinkProgram linkProgram = GLSLESLinkProgramManager.Instance.ActiveLinkProgram;
            linkProgram.UpdatePassIterationUniforms(parms);
        }

        public GLSLESProgram GLSLProgram
        {
            get
            {
                return glslProgram;
            }
        }
        public bool IsLinked
        {
            get { return linked != 0; }
            set { linked = value == true ? 1 : 0; }
        }

        protected override void LoadFromSource()
        {
            //nothing to load
        }
        protected override void unload()
        {
            //nothing to unload
        }
        protected override void load()
        {
            //nothing to load
        }
    }
}
