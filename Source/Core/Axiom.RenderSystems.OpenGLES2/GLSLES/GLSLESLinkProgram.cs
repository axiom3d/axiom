using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;
using Axiom.Graphics;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
    class GLSLESLinkProgram : GLSLESProgramCommon
    {

        public GLSLESLinkProgram(GLSLESGpuProgram vertexProgram, GLSLESGpuProgram fragmentProgram)
            : base(vertexProgram, fragmentProgram)
        {
            if (vertexProgram == null || fragmentProgram == null)
            {
                throw new Core.AxiomException("Attempted to create a shader program without both a vertex and fragment program.");
            }
        }
        ~GLSLESLinkProgram()
        {
            GL.DeleteProgram(glProgramHandle);
        }
        protected override void CompileAndLink()
        {
            //Compile and attach vertex program
            if (!vertexProgram.GLSLProgram.Compile(true))
            {
                triedToLinkAndFailed = true;
                return;
            }
            vertexProgram.GLSLProgram.AttachToProgramObject(glProgramHandle);
            SkeletalAnimationIncluded = vertexProgram.IsSkeletalAnimationIncluded;

            //Compile and attach fragment program
            if (!fragmentProgram.GLSLProgram.Compile(true))
            {
                triedToLinkAndFailed = true;
                return;
            }
            fragmentProgram.GLSLProgram.AttachToProgramObject(glProgramHandle);

            //The link
            GL.LinkProgram(glProgramHandle);
            GL.GetProgram(glProgramHandle, GLenum.LinkStatus, ref linked);

            triedToLinkAndFailed = (linked == 0) ? true : false;

        }
        protected override void BuildGLUniformReferences()
        {
            if (!uniformRefsBuilt)
            {
                Axiom.Graphics.GpuProgramParameters.GpuConstantDefinitionMap vertParams = null;
                Axiom.Graphics.GpuProgramParameters.GpuConstantDefinitionMap fragParams = null;

                if (vertexProgram != null)
                {
                    vertParams = vertexProgram.GLSLProgram.ConstantDefinitions.Map;
                }
                if (fragmentProgram != null)
                {
                    fragParams = fragmentProgram.GLSLProgram.ConstantDefinitions.Map;
                }

                GLSLESLinkProgramManager.Instance.ExtractUniforms(glProgramHandle, vertParams, fragParams, glUniformReferences);

                uniformRefsBuilt = true;
            }
        }
        protected override void _useProgram()
        {
            if (linked != 0)
            {
                GL.UseProgram(glProgramHandle);
            }
        }

        public override void Activate()
        {
            if (linked == 0 && !triedToLinkAndFailed)
            {
                GL.GetError();

                glProgramHandle = GL.CreateProgram();

                if (vertexProgram != null)
                {
                    string paramStr = vertexProgram.GLSLProgram.GetParameter("use_optimiser");
                    if (paramStr == "true" || paramStr.Length == 0)
                    {
                        GLSLESLinkProgramManager.Instance.OptimizeShaderSource(vertexProgram);
                    }
                }
                if (vertexProgram != null)
                {
                    string paramStr = fragmentProgram.GLSLProgram.GetParameter("use_optimiser");
                    if (paramStr == "true" || paramStr.Length == 0)
                    {
                        GLSLESLinkProgramManager.Instance.OptimizeShaderSource(fragmentProgram);
                    }
                }
                CompileAndLink();

                ExtractLayoutQualifiers();
                BuildGLUniformReferences();
            }
            _useProgram();
        }

        public override void UpdateUniforms(Graphics.GpuProgramParameters parms, int mask, Graphics.GpuProgramType fromProgType)
        {
            foreach (var currentUniform in glUniformReferences)
            {
                //Only pull values from buffer it's supposed to be in (vertex or fragment)
                //This method will be called twice, once for vertex program params,
                //and once for fragment program params.
                if (fromProgType == currentUniform.SourceProgType)
                {
                    var def = currentUniform.ConstantDef;
                    if (((int)def.Variability & mask) != 0)
                    {
                        int glArraySize = def.ArraySize;

                        switch (def.ConstantType)
                        {
                            case GpuProgramParameters.GpuConstantType.Float1:
                                unsafe
                                {
                                    GL.Uniform1(currentUniform.Location, glArraySize, parms.GetFloatPointer(def.PhysicalIndex).Pointer.ToFloatPointer());
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Float2:
                                unsafe
                                {
                                    GL.Uniform2(currentUniform.Location, glArraySize, parms.GetFloatPointer(def.PhysicalIndex).Pointer.ToFloatPointer()); 
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Float3:
                                unsafe
                                {
                                    GL.Uniform3(currentUniform.Location, glArraySize, parms.GetFloatPointer(def.PhysicalIndex).Pointer.ToFloatPointer());
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Float4:
                                unsafe
                                {
                                    GL.Uniform4(currentUniform.Location, glArraySize, parms.GetFloatPointer(def.PhysicalIndex).Pointer.ToFloatPointer());
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Matrix_2X2:
                                unsafe
                                {
                                    GL.UniformMatrix2(currentUniform.Location, glArraySize, false, parms.GetFloatPointer(def.PhysicalIndex).Pointer.ToFloatPointer());
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Matrix_3X3:
                                unsafe
                                {
                                    GL.UniformMatrix3(currentUniform.Location, glArraySize, false, parms.GetFloatPointer(def.PhysicalIndex).Pointer.ToFloatPointer());
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Matrix_4X4:
                                unsafe
                                {
                                    GL.UniformMatrix4(currentUniform.Location, glArraySize, false, parms.GetFloatPointer(def.PhysicalIndex).Pointer.ToFloatPointer());
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Int1:
                                unsafe
                                {
                                    GL.Uniform1(currentUniform.Location, glArraySize, parms.GetIntPointer(def.PhysicalIndex).Pointer.ToIntPointer());
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Int2:
                                unsafe
                                {
                                    GL.Uniform2(currentUniform.Location, glArraySize, parms.GetIntPointer(def.PhysicalIndex).Pointer.ToIntPointer());
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Int3:
                                unsafe
                                {
                                    GL.Uniform3(currentUniform.Location, glArraySize, parms.GetIntPointer(def.PhysicalIndex).Pointer.ToIntPointer());
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Int4:
                                unsafe
                                {
                                    GL.Uniform4(currentUniform.Location, glArraySize, parms.GetIntPointer(def.PhysicalIndex).Pointer.ToIntPointer());
                                }
                                break;
                            case GpuProgramParameters.GpuConstantType.Sampler1D:
                            case GpuProgramParameters.GpuConstantType.Sampler1DShadow:
                            case GpuProgramParameters.GpuConstantType.Sampler2D:
                            case GpuProgramParameters.GpuConstantType.Sampler2DShadow:
                            case GpuProgramParameters.GpuConstantType.Sampler3D:
                            case GpuProgramParameters.GpuConstantType.SamplerCube:
                                //samplers handled like 1-elemnt ints
                                unsafe
                                {

                                    GL.Uniform1(currentUniform.Location, 1, parms.GetIntPointer(def.PhysicalIndex).Pointer.ToIntPointer());
                                }
                                break;
                        }
                    }
                }
            }
        }

        public override void UpdatePassIterationUniforms(Graphics.GpuProgramParameters parms)
        {
            if (parms.HasPassIterationNumber)
            {
                int index = parms.PassIterationNumberIndex;

                foreach (var currentUniform in glUniformReferences)
                {
                    if (index == currentUniform.ConstantDef.PhysicalIndex)
                    {
                        unsafe
                        {
                            GL.Uniform1(currentUniform.Location, 1, parms.GetFloatPointer(index).Pointer.ToFloatPointer());
                            //There will only be one multipass entry
                            return;
                        }
                    }
                }
            }
        }
        protected void ExtractLayoutQualifiers()
        { }
    }
}