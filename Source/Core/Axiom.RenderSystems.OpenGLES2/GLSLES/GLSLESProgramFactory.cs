using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
    class GLSLESProgramFactory : HighLevelGpuProgramFactory
    {
        private static GLSLESLinkProgramManager linkProgramManager;
        private static GLSLESProgramPipelineManager programPipelineManager;

        public GLSLESProgramFactory()
        {
            if (linkProgramManager == null)
            {
                linkProgramManager = new GLSLESLinkProgramManager();
            }

            if (false) //Root.Instance.RenderSystem.Capabilities.HasCapability(SeperateShaderObjects)
            {
                if (programPipelineManager == null)
                {
                    programPipelineManager = new GLSLESProgramPipelineManager();
                }
            }
        }
        ~GLSLESProgramFactory()
        {
            if (linkProgramManager != null)
            {
                linkProgramManager = null;
            }
            if (programPipelineManager != null)
            {
                programPipelineManager = null;
            }
        }
        public override string Language
        {
            get { return "glsles"; }
        }

        public override HighLevelGpuProgram CreateInstance(Core.ResourceManager creator, string name, ulong handle, string group, bool isManual, Core.IManualResourceLoader loader)
        {
            return new GLSLESProgram(creator, name, handle, group, isManual, loader);
        }
        public override void DestroyInstance(ref HighLevelGpuProgram obj)
        {
            base.DestroyInstance(ref obj);
        }
    }
}