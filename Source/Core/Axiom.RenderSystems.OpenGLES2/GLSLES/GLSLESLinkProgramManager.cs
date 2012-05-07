using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLenum = OpenTK.Graphics.ES20.All;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
    class GLSLESLinkProgramManager : GLSLESProgramManagerCommon
    {
        private Dictionary<long, GLSLESLinkProgram> linkPrograms;

        private GLSLESLinkProgram activeLinkProgram;
        private Dictionary<string, GLenum> typeEnumMap;
        private static GLSLESLinkProgramManager _instance = null;

        public GLSLESLinkProgramManager()
        {
            activeLinkProgram = null;
        }
        ~GLSLESLinkProgramManager()
        {
            foreach (var key in linkPrograms.Keys)
            {
                linkPrograms[key] = null;
            }
        }
        public GLSLESLinkProgram ActiveLinkProgram
        {
            get
            {
                if (activeLinkProgram != null)
                    return activeLinkProgram;

                long activeKey = 0;

                if (activeVertexGpuProgram != null)
                {
                    activeKey = activeVertexGpuProgram.ProgramID << 32;
                }
                if (activeFragmentGpuProgram != null)
                {
                    activeKey += activeFragmentGpuProgram.ProgramID;
                }

                //Only return a link program object if a vertex or fragment program exist
                if (activeKey > 0)
                {
                    if (!linkPrograms.ContainsKey(activeKey))
                    {
                        activeLinkProgram = new GLSLESLinkProgram(activeVertexGpuProgram, activeFragmentGpuProgram);
                        linkPrograms.Add(activeKey, activeLinkProgram);
                    }
                    else
                    {
                        activeLinkProgram = linkPrograms[activeKey];
                    }
                }
                //Make the program object active
                if (activeLinkProgram != null)
                    activeLinkProgram.Activate();

                return activeLinkProgram;
            }
        }

        public GLSLESGpuProgram ActiveFragmentShader
        {
            set
            {
                if (value != activeFragmentGpuProgram)
                {
                    activeFragmentGpuProgram = value;
                    activeLinkProgram = null;
                }
            }
        }
        public GLSLESGpuProgram ActiveVertexShader
        {
            set
            {
                if (value != activeVertexGpuProgram)
                {
                    activeVertexGpuProgram = value;
                    activeLinkProgram = null;
                }
            }
        }
        public static GLSLESLinkProgramManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GLSLESLinkProgramManager();
                }
                return _instance;
            }
        }
    }
}
