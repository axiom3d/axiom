using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;
using Axiom.Core;

namespace Axiom.Components.RTShaderSystem
{
    class GLSLProgramProcessor : ProgramProcessor
    {
        List<string> libraryPrograms;

        public GLSLProgramProcessor()
        { }


        public override void Dispose()
        {
            for (int i = 0; i < libraryPrograms.Count; i++)
            {
                HighLevelGpuProgramManager.Instance.Remove(libraryPrograms[i]);
            }
            libraryPrograms.Clear();
        }
        internal override bool PreCreateGpuPrograms(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program fsProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function fsMain = fsProgram.EntryPointFunction;
            bool success;

           //compact vertex shader outputs.
            success = ProgramProcessor.CompactVsOutputs(vsMain, fsMain);
            if (success == false)
                return false;

            return true;
        }
        internal override bool PostCreateGpuPrograms(ProgramSet programSet)
        {
            Program vsCpuProgram = programSet.CpuVertexProgram;
            Program fsCpuProgram = programSet.CpuFragmentProgram;
            GpuProgram vsGpuProgram = programSet.GpuVertexProgram;
            GpuProgram fsGpuProgram = programSet.GpuFragmentProgram;

            BindSubShaders(vsCpuProgram, vsGpuProgram);

            BindSubShaders(fsCpuProgram, fsGpuProgram);

            BindAutoParameters(programSet.CpuVertexProgram, programSet.GpuVertexProgram);

            BindAutoParameters(programSet.CpuFragmentProgram, programSet.GpuFragmentProgram);

            BindTextureSampler(vsCpuProgram, vsGpuProgram);

            BindTextureSampler(fsCpuProgram, fsGpuProgram);

            return true;
        }
        private void BindTextureSampler(Program cpuProgram, GpuProgram gpuProgram)
        {
            var gpuParams = gpuProgram.DefaultParameters;
            var progParams = cpuProgram.Parameters;

            //Bind the samplers
            foreach (var curParam in progParams)
            {
                if (curParam.IsSampler)
                {
                    gpuParams.SetNamedConstant(curParam.Name, curParam.Index);
                }
            }
        }
        private void BindSubShaders(Program program, GpuProgram gpuProgram)
        {
            if (program.DependencyCount > 0)
            {
                // Get all attached shaders so we do not attach shaders twice.
                // maybe GLSLProgram should take care of that ( prevent add duplicate shaders )
                string attachedShaders = string.Empty; //TODO: gpuProgram.GetParameter("attach");
                string subSharedDef = string.Empty;

                for (int i = 0; i < program.DependencyCount; i++)
                {
                    // Here we append _VS and _FS to the library shaders (so max each lib shader
                    // is compiled twice once as vertex and once as fragment shader)

                    string subShaderName = program.GetDependency(i);
                    if (program.Type == GpuProgramType.Vertex)
                    {
                        subShaderName += "_VS";
                    }
                    else
                    {
                        subShaderName += "_FS";
                    }

                    //Check if the library shader already compiled
                    if (!HighLevelGpuProgramManager.Instance.ResourceExists(subShaderName))
                    {
                        //Create the library shader
                        HighLevelGpuProgram subGpuProgram = HighLevelGpuProgramManager.Instance.CreateProgram(subShaderName,
                            ResourceGroupManager.DefaultResourceGroupName, TargetLanguage, program.Type);

                        //Set the source name
                        string sourceName = program.GetDependency(i) + "." + TargetLanguage;
                        subGpuProgram.SourceFile = sourceName;

                        //If we have compiler errors than stop processing
                        if (subGpuProgram.HasCompileError)
                        {
                            throw new AxiomException("Could not compile shader library from the source file: " + sourceName);

                        }

                        libraryPrograms.Add(subShaderName);
                    }
                    
                    //Check if the lib shader already attached to this shader
                    if (attachedShaders.Contains(subShaderName))
                    {
                        subSharedDef += subShaderName + " ";
                    }
                }

                //Check if we have something to attach
                if(subSharedDef.Length > 0)
                {
                    Axiom.Collections.NameValuePairList nvpl = new Axiom.Collections.NameValuePairList();
                    nvpl.Add("attach", subSharedDef);
                    gpuProgram.SetParameters(nvpl);
                }
            }
        }
        public override string TargetLanguage
        {
            get { return "glsl"; }
        }

    }
}
