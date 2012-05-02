using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
    /// <summary>
    /// Ogre assumes that there are separate vertex and fragment programs to deal with but
     ///    GLSL ES has one program pipeline object that represents the active vertex and fragment program objects
     ///    during a rendering state.  GLSL vertex and fragment program objects are compiled separately
     ///    and then attached to a program object and then the program pipeline object is linked.
     ///    Since Ogre can only handle one vertex program stage and one fragment program stage being active
     ///    in a pass, the GLSL ES Program Pipeline Manager does the same.  The GLSL ES Program Pipeline
     ///    Manager acts as a state machine and activates a pipeline object based on the active
     ///    vertex and fragment program.  Previously created pipeline objects are stored along with a unique
    ///    key in a hash_map for quick retrieval the next time the pipeline object is required.
    /// </summary>
    class GLSLESProgramPipelineManager
    {
        private static GLSLESProgramPipelineManager _instance;
        private Dictionary<long, GLSLESProgramPipeline> programPipelines;
        private GLSLESProgramPipeline activeProgramPipeline;

        public GLSLESProgramPipelineManager()
        {

        }

        /// <summary>
        /// Gets active, if a program pipeline object was not already create and linked, a new one is create
        /// </summary>
        public GLSLESProgramPipeline ActiveProgramPipeline
        {
            get;
        }
        public GLSLESGpuProgram ActiveVertexLinkProgram
        {
            set;
        }
        public GLSLESGpuProgram ActiveFragmentLinkProgram
        {
            set;
        }
        public static GLSLESProgramPipelineManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GLSLESProgramPipelineManager();
                }
                return _instance;
            }
        }
    }
}
