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
    class GLSLESProgramPipelineManager : GLSLESProgramManagerCommon
    {
        private Dictionary<long, GLSLESProgramPipeline> programPipelines;
        private GLSLESProgramPipeline activeProgramPipeline;
        private static GLSLESProgramPipelineManager _instance = null;

        public GLSLESProgramPipelineManager()
        {
            activeProgramPipeline = null;
        }
        ~GLSLESProgramPipelineManager()
        {
            //Iterate through map container and delete program pipelines
            foreach (var key in programPipelines.Keys)
            {
                programPipelines[key] = null;
            }
        }

        public GLSLESProgramPipeline ActiveProgramPipeline
        {
            get
            {
                //if there is an active link program then return it
                if (activeProgramPipeline != null)
                    return activeProgramPipeline;

                //No active link program so find one or make a new one
                //Is there an active key?
                long activeKey = 0;

                if (activeVertexGpuProgram != null)
                {
                    activeKey = activeVertexGpuProgram.ProgramID << 32;
                }
                if (activeFragmentGpuProgram != null)
                {
                    activeKey += activeFragmentGpuProgram.ProgramID;
                }

                //Only return a program pipeline object if a vertex or fragment stage exist
                if (activeKey > 0)
                {
                    //Find the key in the hash map
                    if (!programPipelines.ContainsKey(activeKey))
                    {
                        activeProgramPipeline = new GLSLESProgramPipeline(activeVertexGpuProgram, activeFragmentGpuProgram);
                        programPipelines.Add(activeKey, new GLSLESProgramPipeline(activeVertexGpuProgram, activeFragmentGpuProgram);
                    }
                    else
                    {
                        //Found a link program in map container so make it active
                        activeProgramPipeline = programPipelines[activeKey];
                    }
                }
                //Make the program object active
                if(activeProgramPipeline != null)
                {
                    activeProgramPipeline.Activate();
                }

                return activeProgramPipeline;
            }
        }

        public GLSLESGpuProgram ActiveVertexLinkProgram
        {
            set
            {
                if (value != activeVertexGpuProgram)
                {
                    activeVertexGpuProgram = value;
                    activeProgramPipeline = null;
                }
            }
        }
        public GLSLESGpuProgram ActiveFragmentLinkProgram
        {
            set
            {
                if (value != activeFragmentGpuProgram)
                {
                    activeFragmentGpuProgram = value;
                    activeProgramPipeline = null;
                }
            }
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
