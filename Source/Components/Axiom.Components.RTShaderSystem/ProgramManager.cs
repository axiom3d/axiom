using System;
using System.Collections.Generic;
using System.IO;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
    internal class ProgramManager : IDisposable
    {
        private List<Program> cpuProgramList;
        private Dictionary<string, ProgramWriter> programWritersMap;
        private Dictionary<string, ProgramProcessor> programProcessorMap;
        private List<ProgramWriterFactory> programWriterFactories;
        private Dictionary<string, GpuProgram> vertexShaderMap;
        private Dictionary<string, GpuProgram> fragmentShaderMap;
        private List<ProgramProcessor> defaultProgramProcessors;
        private static ProgramManager _instance;

        public ProgramManager()
        {
            CreateDefaultProgramProcessors();
            CreateDefaultProgramWriterFactories();
        }

        internal void AcquirePrograms(Graphics.Pass pass, TargetRenderState renderState)
        {
            //Create the CPU programs
            if (!renderState.CreateCpuPrograms())
            {
                throw new Core.AxiomException("Could not apply render state ");
            }

            ProgramSet programSet = renderState.ProgramSet;

            //Create the GPU programs
            if (!CreateGpuPrograms(programSet))
            {
                throw new Core.AxiomException("Could not create gpu program from render state ");
            }

            //bind the created gpu programs to the target pass
            pass.SetVertexProgram(programSet.GpuVertexProgram.Name);
            pass.SetFragmentProgram(programSet.GpuFragmentProgram.Name);

            //Bind uniform parameters to pass parameters
            BindUniformParameters(programSet.CpuVertexProgram, pass.VertexProgramParameters);
            BindUniformParameters(programSet.CpuFragmentProgram, pass.FragmentProgramParameters);
        }

        internal void ReleasePrograms(Graphics.Pass pass, TargetRenderState renderState)
        {
            ProgramSet programSet = renderState.ProgramSet;

            pass.SetVertexProgram(string.Empty);
            pass.SetFragmentProgram(string.Empty);

            renderState.DestroyProgramSet();

            foreach (var key in this.vertexShaderMap.Keys)
            {
                if (true) //TODO
                {
                    DestroyGpuProgram(this.vertexShaderMap[key]);
                    this.vertexShaderMap.Remove(key);
                    break;
                }
            }

            foreach (var key in this.fragmentShaderMap.Keys)
            {
                if (true)
                {
                    DestroyGpuProgram(this.fragmentShaderMap[key]);
                    this.fragmentShaderMap.Remove(key);
                    break;
                }
            }
        }

        internal void FlushGpuProgramsCache()
        {
            FlushGpuProgramsCache(this.vertexShaderMap);
            FlushGpuProgramsCache(this.fragmentShaderMap);
        }

        internal void FlushGpuProgramsCache(Dictionary<string, GpuProgram> gpuProgramsMap)
        {
            foreach (var key in gpuProgramsMap.Keys)
            {
                DestroyGpuProgram(gpuProgramsMap[key]);
            }
            gpuProgramsMap.Clear();
        }

        public Program CreateCpuProgram(Graphics.GpuProgramType gpuProgramType)
        {
            var shaderProgram = new Program(gpuProgramType);

            this.cpuProgramList.Add(shaderProgram);

            return shaderProgram;
        }

        public void DestroyCpuProgram(Program shaderProgram)
        {
            for (int i = 0; i < this.cpuProgramList.Count; i++)
            {
                if (this.cpuProgramList[i] == shaderProgram)
                {
                    this.cpuProgramList[i].Dispose();
                    this.cpuProgramList[i] = null;
                    this.cpuProgramList.RemoveAt(i);
                    break;
                }
            }
        }

        private void CreateDefaultProgramProcessors()
        {
            //Add standard shader processors
            //#if #if OGRE_PLATFORM != OGRE_PLATFORM_ANDROID
            this.defaultProgramProcessors.Add(new CGProgramProcessor());
            this.defaultProgramProcessors.Add(new GLSLProgramProcessor());
            this.defaultProgramProcessors.Add(new HLSLProgramProcessor());
            //#endif
            this.defaultProgramProcessors.Add(new GLSLESProgramProcessor());

            for (int i = 0; i < this.defaultProgramProcessors.Count; i++)
            {
                AddProgramProcessor(this.defaultProgramProcessors[i]);
            }
        }

        private void DestroyDefaultProgramProcessors()
        {
            for (int i = 0; i < this.defaultProgramProcessors.Count; i++)
            {
                RemoveProgramProcessor(this.defaultProgramProcessors[i]);
                this.defaultProgramProcessors[i] = null;
            }

            this.defaultProgramProcessors.Clear();
        }

        private void CreateDefaultProgramWriterFactories()
        {
            //Add Standard shader writer factories
            //#if OGRE_PLATFORM != OGRE_PLATFORM_ANDROID
            this.programWriterFactories.Add(new ProgramWriterCGFactory());
            this.programWriterFactories.Add(new ProgramWriterGLSLFactory());
            this.programWriterFactories.Add(new ProgramWriterHLSLFactory());
            //#endif
            this.programWriterFactories.Add(new ProgramWriterGLSLESFactory());

            for (int i = 0; i < this.programWriterFactories.Count; i++)
            {
                ProgramWriterManager.Instance.AddFactory(this.programWriterFactories[i]);
            }
        }

        private void DestroyDefaultProgramWriterFactories()
        {
            for (int i = 0; i < this.programWriterFactories.Count; i++)
            {
                ProgramWriterManager.Instance.RemoveFactory(this.programWriterFactories[i]);
                this.programWriterFactories[i] = null;
            }
            this.programWriterFactories.Clear();
        }

        private void DestroyProgramWriters()
        {
            foreach (var key in this.programWritersMap.Keys)
            {
                if (this.programWritersMap[key] != null)
                {
                    this.programWritersMap[key].Dispose();
                    this.programWritersMap[key] = null;
                }
            }
            this.programWritersMap.Clear();
        }

        private bool CreateGpuPrograms(ProgramSet programSet)
        {
            // Before we start we need to make sure that the pixel shader input
            //  parameters are the same as the vertex output, this required by 
            //  shader models 4 and 5.
            // This change may incrase the number of register used in older shader
            //  models - this is why the check is present here.
            bool isVs4 = GpuProgramManager.Instance.IsSyntaxSupported("vs_4_0");
            if (isVs4)
            {
                SynchronizePixelnToBeVertexOut(programSet);
            }

            //Grab the matching writer
            string language = ShaderGenerator.Instance.TargetLangauge;
            ProgramWriter programWriter = null;

            if (this.programWritersMap.ContainsKey(language))
            {
                programWriter = this.programWritersMap[language];
            }
            else
            {
                programWriter = ProgramWriterManager.Instance.CreateProgramWriter(language);
                this.programWritersMap.Add(language, programWriter);
            }

            ProgramProcessor programProcessor = null;
            if (this.programProcessorMap.ContainsKey(language) == false)
            {
                throw new AxiomException("Could not find processor for language " + language);
            }

            programProcessor = this.programProcessorMap[language];


            bool success;

            //Call the pre creation of GPU programs method
            success = programProcessor.PreCreateGpuPrograms(programSet);
            if (success == false)
            {
                return false;
            }

            //Create the vertex shader program
            GpuProgram vsGpuProgram;

            vsGpuProgram = CreateGpuProgram(programSet.CpuVertexProgram, programWriter, language,
                                             ShaderGenerator.Instance.VertexShaderProfiles,
                                             ShaderGenerator.Instance.VertexShaderProfilesList,
                                             ShaderGenerator.Instance.ShaderChachePath);

            if (vsGpuProgram == null)
            {
                return false;
            }

            programSet.GpuVertexProgram = vsGpuProgram;

            //update flags
            programSet.GpuVertexProgram.IsSkeletalAnimationIncluded =
                programSet.CpuVertexProgram.SkeletalAnimationIncluded;

            //Create the fragment shader program.
            GpuProgram psGpuProgram;

            psGpuProgram = CreateGpuProgram(programSet.CpuFragmentProgram, programWriter, language,
                                             ShaderGenerator.Instance.FragmentShaderProfiles,
                                             ShaderGenerator.Instance.FragmentShaderProfilesList,
                                             ShaderGenerator.Instance.ShaderChachePath);

            if (psGpuProgram == null)
            {
                return false;
            }

            programSet.GpuFragmentProgram = psGpuProgram;

            //Call the post creation of GPU programs method.
            success = programProcessor.PostCreateGpuPrograms(programSet);
            if (success == false)
            {
                return false;
            }

            return true;
        }

        private GpuProgram CreateGpuProgram(Program shaderProgram, ProgramWriter programWriter, string language,
                                             string profiles, string[] profilesList, string cachePath)
        {
            StreamWriter sourceCodeStringStream = null;

            int programHashCode;

            string programName;

            //Generate source code
            programWriter.WriteSourceCode(sourceCodeStringStream, shaderProgram);

            programHashCode = sourceCodeStringStream.GetHashCode();

            //Generate program name
            programName = programHashCode.ToString();

            if (shaderProgram.Type == GpuProgramType.Vertex)
            {
                programName += "_VS";
            }
            else if (shaderProgram.Type == GpuProgramType.Fragment)
            {
                programName += "_FS";
            }

            HighLevelGpuProgram gpuProgram;

            //Try to get program by name
            gpuProgram = (HighLevelGpuProgram)HighLevelGpuProgramManager.Instance.GetByName(programName);

            //Case the program doesn't exist yet
            if (gpuProgram == null)
            {
                //Create new GPU program.
                gpuProgram = HighLevelGpuProgramManager.Instance.CreateProgram(programName,
                                                                                ResourceGroupManager.
                                                                                    DefaultResourceGroupName, language,
                                                                                shaderProgram.Type);

                //Case cache directory specified -> create program from file
                if (cachePath == string.Empty)
                {
                    string programFullName = programName + "." + language;
                    string programFileName = cachePath + programFullName;
                    bool writeFile = true;

                    //Check if program file already exists
                    if (File.Exists(programFileName))
                    {
                        writeFile = true;
                    }
                    else
                    {
                        writeFile = false;
                    }

                    if (writeFile)
                    {
                        var outFile = new StreamWriter(programFileName);

                        outFile.Write(sourceCodeStringStream);
                        outFile.Close();
                    }

                    gpuProgram.SourceFile = programFullName;
                }
                else // no cache directory specified -> create program from system memory
                {
                    //TODO
                    // gpuProgram.Source = sourceCodeStringStream;
                }
                var gpuParams = new Collections.NameValuePairList();
                gpuParams.Add("entry_point", shaderProgram.EntryPointFunction.Name);
                gpuProgram.SetParameters(gpuParams);

                gpuParams.Clear();

                // HLSL program requires specific target profile settings - we have to split the profile string.
                if (language == "hlsl")
                {
                    foreach (var it in profilesList)
                    {
                        if (GpuProgramManager.Instance.IsSyntaxSupported(it))
                        {
                            gpuParams.Add("target", it);
                            gpuProgram.SetParameters(gpuParams);
                            gpuParams.Clear();
                            break;
                        }
                    }
                }
                gpuParams.Add("profiles", profiles);
                gpuProgram.SetParameters(gpuParams);
                gpuProgram.Load();

                //Case an error occurred
                if (gpuProgram.HasCompileError)
                {
                    gpuProgram = null;
                    return gpuProgram;
                }

                //Add the created GPU prgram to local cache
                if (gpuProgram.Type == GpuProgramType.Vertex)
                {
                    this.vertexShaderMap[programName] = gpuProgram;
                }
                else if (gpuProgram.Type == GpuProgramType.Fragment)
                {
                    this.fragmentShaderMap[programName] = gpuProgram;
                }
            }
            return gpuProgram;
        }

        private void AddProgramProcessor(ProgramProcessor processor)
        {
            if (this.programProcessorMap.ContainsKey(processor.TargetLanguage))
            {
                throw new AxiomException("A processor for language " + processor.TargetLanguage + " already exists");
            }

            this.programProcessorMap.Add(processor.TargetLanguage, processor);
        }

        private void RemoveProgramProcessor(ProgramProcessor processor)
        {
            this.programProcessorMap.Remove(processor.TargetLanguage);
        }

        private void DestroyGpuProgram(GpuProgram gpuProgram)
        {
            string programName = gpuProgram.Name;
            Resource res = HighLevelGpuProgramManager.Instance.GetByName(programName);

            if (res != null)
            {
                HighLevelGpuProgramManager.Instance.Remove(programName);
            }
        }

        private void SynchronizePixelnToBeVertexOut(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;

            //first find the vertex shader
            Function vertexMain = null;
            Function pixelMain = null;

            {
                var functionList = vsProgram.Functions;
                foreach (var curFunction in functionList)
                {
                    if (curFunction.FuncType == Function.FunctionType.VsMain)
                    {
                        vertexMain = curFunction;
                        break;
                    }
                }
            }
            //find pixel shader main
            {
                var functionList = psProgram.Functions;
                foreach (var curFunction in functionList)
                {
                    if (curFunction.FuncType == Function.FunctionType.PsMain)
                    {
                        pixelMain = curFunction;
                        break;
                    }
                }
            }

            //save the pixel program original input parameters
            var pixelOriginalINParams = pixelMain.InputParameters;

            //set the pixel input to be the same as the vertex prog output
            pixelMain.DeleteAllInputParameters();

            // Loop the vertex shader output parameters and make sure that
            //   all of them exist in the pixel shader input.
            // If the parameter type exist in the original output - use it
            // If the parameter doesn't exist - use the parameter from the 
            //   vertex shader input.
            // The order will be based on the vertex shader parameters order 
            // Write output parameters.
            var outParams = vertexMain.OutputParameters;
            foreach (var curOutParameter in outParams)
            {
                Parameter paramToAdd = Function.GetParameterBySemantic(
                    pixelOriginalINParams,
                    curOutParameter.Semantic,
                    curOutParameter.Index);

                if (paramToAdd == null)
                {
                    //param not found - we will add the one from the vertex shader
                    paramToAdd = curOutParameter;
                }

                pixelMain.AddInputParameter(paramToAdd);
            }
        }

        private void BindUniformParameters(Program cpuProgram, GpuProgramParameters passParams)
        {
            var progParams = cpuProgram.Parameters;
            foreach (var item in progParams)
            {
                item.Bind(passParams);
            }
        }


        public void Dispose()
        {
            FlushGpuProgramsCache();
            DestroyDefaultProgramProcessors();
            DestroyDefaultProgramWriterFactories();
            DestroyProgramWriters();
        }


        public int VertexShaderCount
        {
            get
            {
                return this.vertexShaderMap.Count;
            }
        }

        public int FragmentShaderCount
        {
            get
            {
                return this.fragmentShaderMap.Count;
            }
        }


        public static ProgramManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProgramManager();
                }
                return _instance;
            }
        }
    }
}