using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;
using Axiom.Core;
using GLenum = OpenTK.Graphics.ES20.All;
using GL = OpenTK.Graphics.ES20.GL;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
    class GLSLESProgram : HighLevelGpuProgram
    {
        #region NestedTypes
        class CmdOptimization
        {
            public string DoGet(GLSLESProgram target)
            {
                return target.optimizerEnabled.ToString();
            }
            public void DoSet(GLSLESProgram target, string val)
            {
                target.OptimizerEnabled = bool.Parse(val);
            }
        }
        class CmdPreprocessorDefines
        {
            public string DoGet(GLSLESProgram target)
            {
                return target.PreprocessorDefines;
            }
            public void DoSet(GLSLESProgram target, string val)
            {
                target.PreprocessorDefines = val;
            }
        } 
        #endregion


        private int glShaderHandle, glProgramHandle;
        int compiled;
        bool isOptimized;
        string preprocessorDefines;
        bool optimizerEnabled;

        protected static CmdPreprocessorDefines cmdPreprocessorDefines;
        static CmdOptimization cmdOptimization;

        public GLSLESProgram(ResourceManager creator, string name, ulong handle,
            string group, bool isManual, IManualResourceLoader loader)
            :base(creator, name, handle, group, isManual, loader)
        {
            glShaderHandle = 0;
            glProgramHandle = 0;
            compiled = 0;
            isOptimized = false;
            optimizerEnabled = true;

            //todo: ogre does something funky with a dictionary here...
          
            syntaxCode = "glsles";
        }
        protected override void dispose(bool disposeManagedResources)
        {
            // Have to call this here reather than in Resource destructor
            // since calling virtual methods in base destructors causes crash
            if (IsLoaded)
            {
                Unload();
            }
            else
            {
                UnloadHighLevel();
            }
            base.dispose(disposeManagedResources);
        }
        protected override void CreateLowLevelImpl()
        {
            assemblerProgram = new GLSLESProgram(Creator, Name, Handle, Group, IsManuallyLoaded, loader);
        }

        protected override void UnloadHighLevelImpl()
        {
            if (IsSupported)
            {
                GL.DeleteShader(glShaderHandle);

                if (true)//Root.Instance.RenderSystem.Capabilities.HasCapability(Capabilities.SeperateShaderObjects))
                {
                    GL.DeleteProgram(glProgramHandle);
                }
            }
        }
        protected override void LoadFromSource()
        {
            GLSLESPreprocessor cpp = new GLSLESPreprocessor();

            //Pass all user-defined macros to preprocessor
            if (preprocessorDefines.Length > 0)
            {
                int pos = 0;
                while (pos != preprocessorDefines.Length)
                {
                    //Find delims
                    int endpos = FindFirstOf(preprocessorDefines, ";,=", pos);

                    if (endpos != -1)
                    {
                        int macroNameStart = pos;
                        int macroNameLen = endpos - pos;
                        pos = endpos;

                        //Check definition part
                        if (preprocessorDefines[pos] == '=')
                        {
                            //Set up a definition, skip delim
                            ++pos;
                            int macroValStart = pos;
                            int macroValLen;

                            endpos = FindFirstOf(preprocessorDefines, ";,", pos);
                            if (endpos == -1)
                            {
                                macroValLen = preprocessorDefines.Length - pos;
                                pos = endpos;
                            }
                            else
                            {
                                macroValLen = endpos - pos;
                                pos = endpos + 1;
                            }
                            cpp.Define(preprocessorDefines + macroNameStart, macroNameLen, preprocessorDefines + macroValStart, macroValLen);
                        }
                        else
                        {
                            //No definition part, define as "1"
                            ++pos;
                            cpp.Define(preprocessorDefines + macroNameStart, macroNameLen, 1);
                        }
                    }
                    else
                    {
                        pos = endpos;
                    }
                }
                int outSize = 0;
                string src = source;
                int srcLen = source.Length;
                string outVal = cpp.Parse(src, srcLen, out outSize);
                if (outVal == null || outSize == 0)
                {
                    //Failed to preprocess, break out
                    throw new AxiomException("Failed to preprocess shader " + base.Name);
                }

                source = new string(outVal.ToCharArray(), 0, outSize);

            }
        }
        public override GpuProgramParameters CreateParameters()
        {
            var parms = base.CreateParameters();
            parms.TransposeMatrices = true;
            return parms;
        }
        protected override void unload()
        {
            // We didn't create mAssemblerProgram through a manager, so override this
            // implementation so that we don't try to remove it from one. Since getCreator()
            // is used, it might target a different matching handle!
            assemblerProgram = null;

            UnloadHighLevel();
            base.unload();
        }
        protected override void PopulateParameterNames(GpuProgramParameters parms)
        {
            
            parms.NamedConstants = ConstantDefinitions;
            // Don't set logical / physical maps here, as we can't access parameters by logical index in GLHL.
        }
        protected OperationType ParseOperationType(string val)
        {
            if (val == "point_list")
            {
                return OperationType.PointList;
            }
            else if (val == "line_list")
            {
                return OperationType.LineList;
            }
            else if (val == "line_strip")
            {
                return OperationType.LineStrip;
            }
            else if (val == "triangle_strip")
            {
                return OperationType.TriangleStrip;
            }
            else if (val == "triangle_fan")
            {
                return OperationType.TriangleFan;
            }
            else
            {
                return OperationType.TriangleList;
            }
        }
        protected string OperationTypeToString(OperationType val)
        {
            switch (val)
            {
                case OperationType.PointList:
                    return "point_list";
                case OperationType.LineList:
                    return "line_list";
                case OperationType.LineStrip:
                    return "line_strip";
                case OperationType.TriangleList:
                    return "triangle_list";
                case OperationType.TriangleStrip:
                    return "triangle_strip";
                case OperationType.TriangleFan:
                    return "triangle_fan";
                default:
                    return "triangle_list";
            }
        }
        protected override void BuildConstantDefinitions()
        {
            // We need an accurate list of all the uniforms in the shader, but we
            // can't get at them until we link all the shaders into a program object.

            // Therefore instead, parse the source code manually and extract the uniforms
            CreateParameterMappingStructures(true);
            if (true)//Root.Instance.RenderSystem.Capabilities.HasCapability(Capabilities.SeperateShaderObjects))
            {
                GLSLESProgramPipelineManager.Instance.ExtractConstantDefs(source, constantDefs, Name);
            }
            else
            {
                GLSLESLinkProgramManager.Instance.ExtractConstantDefs(source, constantDefs, Name);
            }
        }
        public void CheckAndFixInvalidDefaultPrecisionError(string message)
        {
            string precisionQualifierErrorString = ": 'Default Precision Qualifier' : invalid type Type for default precision qualifier can be only float or int";
            string[] los = source.Split('\n');
            List<string> linesOfSource = new List<string>(los);
            if (message.Contains(precisionQualifierErrorString))
            {
                LogManager.Instance.Write("Fixing invalid type Type fore default precision qualifier by deleting bad lines then re-compiling");

                //remove releavant lines from source
                string[] errors = message.Split('\n');
                
                //going from the end so when we delete a line the numbers of the lines beforew will not change
                for (int i = errors.Length - 1; i >= 0; i--)
                {
                    string curError = errors[i];
                    int foundPos = Find(curError, precisionQualifierErrorString);
                    if (foundPos != -1)
                    {
                        string lineNumber = curError.Substring(0, foundPos);
                        int posOfStartOfNumber = FindLastOf(lineNumber, ':');
                        if (posOfStartOfNumber != -1)
                        {
                            lineNumber = lineNumber.Substring(posOfStartOfNumber + 1, lineNumber.Length - (posOfStartOfNumber + 1));
                            int numLine = -1;
                            if (int.TryParse(lineNumber, out numLine))
                            {
                                linesOfSource.RemoveAt(numLine - 1);
                            }
                        }
                    }

                }
                //rebuild source
                StringBuilder newSource = new StringBuilder();
                for (int i = 0; i < linesOfSource.Count; i++)
                {
                    newSource.AppendLine(linesOfSource[i]);
                }
                source = newSource.ToString();

                int r = 0;
                string[] sourceArray = new string[] { source };
                GL.ShaderSource(glShaderHandle, 1, sourceArray, ref r);

                if (Compile())
                {
                    LogManager.Instance.Write("The removing of the lines fixed the invalid type Type for default precision qualifier error.");

                }
                else
                {
                    LogManager.Instance.Write("The removing of the lines didn't help.");
                }

            }
        }
        public bool Compile()
        {
           return this.Compile(false);
        }
        public bool Compile(bool checkErrors)
        {
            if (compiled == 1)
                return true;

            //ONly creaet a shader object if glsl es is supported
            if (IsSupported)
            {
                //Create shader object
                GLenum shaderType = GLenum.None;
                if (type == GpuProgramType.Vertex)
                {
                    shaderType = GLenum.VertexShader;
                }
                else
                {
                    shaderType = GLenum.FragmentShader;
                }
                glShaderHandle = GL.CreateShader(shaderType);

                if (true)//Root.Instance.RenderSystem.Capabilities.HasCapability(Capabilities.SeperateShaderObjects))
                {
                    glProgramHandle = GL.CreateProgram();
                }
            }

            //Add preprocessor extras and main source
            if (source.Length > 0)
            {
                string[] sourceArray = new string[] { source };
                int r = 0;
                GL.ShaderSource(glShaderHandle, 1, sourceArray, ref r);
            }
            if (checkErrors)
            {
                LogManager.Instance.Write("GLSL ES compiling: " + Name);
            }
            GL.CompileShader(glShaderHandle);
            
            //check for compile errors
            GL.GetShader(glShaderHandle, GLenum.CompileStatus, ref compiled);
            if (compiled == 0 && checkErrors)
            {
                string message = "GLSL ES compile log: " + Name;
                CheckAndFixInvalidDefaultPrecisionError(message);
            }

            //Log a message that the shader compiled successfully.
            if (compiled == 1 && checkErrors)
                LogManager.Instance.Write("GLSL ES compiled: " + Name);

            return (compiled == 1);
        }
        public void AttachToProgramObject(int programObject)
        {
            GL.AttachShader(programObject, glShaderHandle);
        }
        public void DetachFromProgramObject(int programObject)
        {
            GL.DetachShader(programObject, glShaderHandle);
        }

        public int GLShaderHandle
        {
            get { return glShaderHandle; }
        }
        public int GLProgramHandle
        {
            get { return glProgramHandle; }
        }
        public string PreprocessorDefines
        {
            get { return preprocessorDefines; }
            set { preprocessorDefines = value; }
        }
        public bool OptimizerEnabled
        {
            get { return optimizerEnabled; }
            set { optimizerEnabled = value; }
        }
        public bool IsOptimized
        {
            get { return isOptimized; }
            set { isOptimized = value; }
        }

        public override bool PassTransformStates
        {
            get
            {
                //Scenemanager should pass on transform state to the render system
                return true;
            }
        }
        public override bool PassSurfaceAndLightStates
        {
            get
            {
                //scenemanager should pass on light & material state to the rendersystem
                return true;
            }
        }
        public override bool PassFogStates
        {
            get
            {
                //Scenemanager should pass on fog state to the rendersystem
                return true;
            }
        }
        public override string Language
        {
            get
            {
                return "glsles";
            }
        }
        

        //Helper method
        private static int FindFirstOf(string strToCheck, string characters, int startPos)
        {
            for (int i = 0; i < strToCheck.Length; i++)
            {
                if (characters.Contains(strToCheck[i]))
                {
                    return i;
                }
            }

            return -1;
        }
        private static int FindLastOf(string toCheck, char c)
        {
            for (int i = toCheck.Length - 1; i >= 0; i--)
            {
                if (toCheck[i] == c)
                    return i;
            }
            return -1;
        }
        public static int Find(string toCheck, string toFind)
        {

            if (toCheck.Length < toFind.Length)
            {
                return -1;
            }

            for (int i = 0; i < toCheck.Length; i++)
            {
                char c = toCheck[i];

                if (toCheck[i] == toFind[0])
                {
                    int startPos = i;
                    int index = i + 1;
                    bool broken = false;

                    while (index < toCheck.Length)
                    {
                        if (toCheck[index] != toFind[index - startPos])
                        {
                            broken = true;
                            //this isn't the string we're looking for, move along
                            break;
                        }
                        index++;
                        if (index - startPos >= toFind.Length)
                        {
                            //good break, not bad. Means we've found the full substring
                            break;
                        }
                    }
                    if (broken == false)
                    {
                        //found it
                        return startPos;
                    }
                }
            }
            return -1;
        }
    }
}