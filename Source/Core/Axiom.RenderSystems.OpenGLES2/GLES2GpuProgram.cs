using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;
using GLenum = OpenTK.Graphics.ES20.All;
using Axiom.Core;

namespace Axiom.RenderSystems.OpenGLES2
{
    /// <summary>
    /// Genralized low-level GL program, can be applied to multiple types (eg ARB and NV)
    /// </summary>
    class GLES2GpuProgram : GpuProgram
    {
        protected int _programID;
        private GLenum _programType;

        public GLES2GpuProgram(ResourceManager creator, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader)
            : base(creator, name, handle, group, isManual, loader)
        {
            
        }
        public virtual void BindProgram()
        { }
        public virtual void UnbindProgram()
        { }
        public virtual void BindProgramParameters(GpuProgramParameters parms, uint mask)
        { }
        public virtual void BindProgramPassIterationParameters(GpuProgramParameters parms)
        { }
        protected override void dispose(bool disposeManagedResources)
        {
            //Have to call this here rather than in Resource destructor
            //since calling virtual methods in base destructors causes crash
            unload();
            base.dispose(disposeManagedResources);
        }
        public static GLenum GetGLShaderType(GpuProgramType programType)
        {
            switch (programType)
            {
                case GpuProgramType.Vertex:
                default:
                    return GLenum.VertexShader;

                case GpuProgramType.Fragment:
                    return GLenum.FragmentShader;
            }
        }
        /// <summary>
        /// Gets the assigned GL program id
        /// </summary>
        public int ProgramID
        {
            get { return _programID; }
        }
        
        protected override void LoadFromSource()
        {
            //abstract override, nothing todo
        }
    }
}
