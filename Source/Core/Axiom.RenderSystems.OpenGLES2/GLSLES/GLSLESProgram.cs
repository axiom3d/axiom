using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;
using Axiom.Core;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
    class GLSLESProgram : HighLevelGpuProgram
    {
        #region NestedTypes
        class CmdOptimization
        {
            public string DoGet(object target)
            { }
            public void DoSet(object target, string val)
            { }
        }
        class CmdPreprocessorDefines
        {
            public string DoGet(object target)
            { }
            public void DoSet(object target, string val)
            { }
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
            throw new NotImplementedException();
        }

        protected override void UnloadHighLevelImpl()
        {
            throw new NotImplementedException();
        }

        protected override void BuildConstantDefinitions()
        {
            throw new NotImplementedException();
        }

        protected override void LoadFromSource()
        {
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
                            //todo: add cpp defines
                        }
                        else
                        {
                            //No definition part, define as "1"
                            ++pos;
                            //todo: add define
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
                string outVal = string.Empty; //todo!
                if (outVal == null || outSize == 0)
                {
                    //Failed to preprocess, break out
                    throw new AxiomException("Failed to preprocess shader " + base.Name);
                }

                source = outVal;
                Axiom.Media.PixelUtil
            }
        }
        public override GpuProgramParameters CreateParameters()
        {
            return base.CreateParameters();
        }
        protected override void unload()
        {
            base.unload();
        }
        protected override void PopulateParameterNames(GpuProgramParameters parms)
        {
            base.PopulateParameterNames(parms);
        }
        protected void BuildConstantDefinitions()
        { }
        public void CheckAndFixInvalidDefaultPrecisionError(string message)
        { }
        public bool Compile()
        {
           return this.Compile(false);
        }
        public bool Compile(bool checkErrors)
        { }
        public void AttachToProgramObject(int programObject)
        { }
        public void DetachFromProgramObject(int programObject)
        { }

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
                return base.PassTransformStates;
            }
        }
        public override bool PassSurfaceAndLightStates
        {
            get
            {
                return base.PassSurfaceAndLightStates;
            }
        }
        public override bool PassFogStates
        {
            get
            {
                return base.PassFogStates;
            }
        }
        public override string Language
        {
            get
            {
                return base.Language;
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
    }
}