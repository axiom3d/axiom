using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
    /// <summary>
    /// This class is used for when a Vertex and Fragment shader can stand independent of each other, which is not the standard for OpenGLES
    /// In fact, OpenTK doesn't have this ability set in as of yet, so this class should never be used.
    /// However, it is partially filled out as much as possible to allow easy completion supposing OpenTK ever opens access to this ability
    /// </summary>
    class GLSLESProgramPipeline : GLSLESProgramCommon
    {
        protected int glProgramPipelineHandle;
        protected enum Linked
        {
            VertexProgram = 0x01,
            FragmentProgram = 0x10
        }
        public GLSLESProgramPipeline(GLSLESGpuProgram vertexProgram, GLSLESGpuProgram fragmentProgram)
            :base(vertexProgram, fragmentProgram)
        {
            throw new Core.AxiomException("This class should never be instantied, use GLSLESLinkProgram instead");
        }
        ~GLSLESProgramPipeline()
        {

        }
        protected override void CompileAndLink()
        {
            int linkStatus = 0;

            //Compile and attach vertex program
            if (vertexProgram != null && !vertexProgram.IsLinked)
            {
                if (!vertexProgram.GLSLProgram.Compile())
                {
                    triedToLinkAndFailed = true;
                    return;
                }

                int programHandle = vertexProgram.GLSLProgram.GLProgramHandle;
                //GL.ProgramParameter(programHandle, GLenum.LinkStatus, ref linkStatus);
                vertexProgram.GLSLProgram.AttachToProgramObject(programHandle);
                GL.LinkProgram(programHandle);
                GL.GetProgram(programHandle, GLenum.LinkStatus, ref linkStatus);

                if (linkStatus != 0)
                {
                    vertexProgram.IsLinked = true;
                    linked |= (int)Linked.VertexProgram;
                }
                bool bLinkStatus = (linkStatus != 0);
                triedToLinkAndFailed = !bLinkStatus;

                SkeletalAnimationIncluded = vertexProgram.IsSkeletalAnimationIncluded;
            }

            //Compile and attach Fragment program
            if (fragmentProgram != null && !fragmentProgram.IsLinked)
            {
                if (!fragmentProgram.GLSLProgram.Compile(true))
                {
                    triedToLinkAndFailed = true;
                    return;
                }

                int programHandle = fragmentProgram.GLSLProgram.GLProgramHandle;
                //GL.ProgramParameter(programHandle, GLenum.ProgramSeperableExt, true);
                fragmentProgram.GLSLProgram.AttachToProgramObject(programHandle);
                GL.LinkProgram(programHandle);
                GL.GetProgram(programHandle, GLenum.LinkStatus, ref linkStatus);

                if (linkStatus != 0)
                {
                    fragmentProgram.IsLinked = true;
                    linked |= (int)Linked.FragmentProgram;
                }
                triedToLinkAndFailed = !fragmentProgram.IsLinked;

            }

            if (linked != 0)
            {
                
            }
        }

        protected override void _useProgram()
        {


        }

        public override void Activate()
        {
            throw new NotImplementedException();
        }
        public override void UpdateUniforms(Graphics.GpuProgramParameters parms, int mask, Graphics.GpuProgramType fromProgType)
        {
            throw new NotImplementedException();
        }

        public override void UpdatePassIterationUniforms(Graphics.GpuProgramParameters parms)
        {
            throw new NotImplementedException();
        }
        public override int GetAttributeIndex(Graphics.VertexElementSemantic semantic, int index)
        {
            int res = customAttribues[(int)semantic - 1, index];
            if (res == NullCustomAttributesIndex)
            {
                int handle = vertexProgram.GLSLProgram.GLProgramHandle;
                string attString = GetAttributeSemanticString(semantic);
                int attrib = GL.GetAttribLocation(handle, attString);

                if (attrib == NotFoundCustomAttributesIndex && semantic == VertexElementSemantic.Position)
                {
                    attrib = GL.GetAttribLocation(handle, "position");
                }

                if (attrib == NotFoundCustomAttributesIndex)
                {
                    string attStringWithSemantic = attString + index.ToString();
                    attrib = GL.GetAttribLocation(handle, attStringWithSemantic);
                }

                customAttribues[(int)semantic - 1, index] = attrib;
                res = attrib;
            }
            return res;
        }
        protected virtual void ExtractLayoutQualifiers()
        { }
        public int GLProgramPipelineHandle
        {
            get
            {
                return glProgramPipelineHandle;
            }
        }
    }
}
