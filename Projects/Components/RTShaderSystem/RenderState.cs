using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Graphics;
using Axiom.Core.Collections;

namespace Axiom.Components.RTShaderSystem
{
    /// <summary>
    /// This is a container class for sub render state class
    /// a render state is defined by the tsub render states that compose it.
    /// The user should use this interfact define globabl or per material custom behavior.
    /// I.E. in order to a pplhy per pixel to a specific material, one should implement a sub class of SubRenderState that
    /// performs a per pixel lighting model, get the render state of the target material, and add the custom sub class to it
    /// </summary>
    public class RenderState
    {
        protected List<SubRenderState> SubRenderStateList;
        bool lightCountAutoUpdate;
        int[] lightCount = new int[3];

        public RenderState()
        {
            lightCountAutoUpdate = true;
            lightCount[0] = 0;
            lightCount[1] = 0;
            lightCount[2] = 0;
        }

        public void Reset()
        {
            for (int i = 0; i < SubRenderStateList.Count; i++)
			{
                ShaderGenerator.Instance.DestroySubRenderState(SubRenderStateList[i]);
			}
            SubRenderStateList.Clear();
        }


        public virtual void AddTemplateSubRenderState(SubRenderState subRenderState)
        {
            bool addSubRenderState = true;

            for (int i = 0; i < SubRenderStateList.Count; i++)
            {
                SubRenderState it = SubRenderStateList[i];
                //Case the same instance already exists-> do not add to list
                if (it == subRenderState)
                {
                    addSubRenderState = false;
                    break;
                }

                //Case it is different sub render state instance with the same type, use the new sub render state,
                //instead of the previous sub render stet. This scenario is usually cause by material inheritance, so we use the derived material sub render state
                //and destroy the base sub render state
                else if (it.Type == subRenderState.Type)
                {
                    RemoveTemplateSubRenderState(it);
                    break;
                }
            }

            if (addSubRenderState)
            {
                SubRenderStateList.Add(subRenderState);
            }
        }
        public virtual void RemoveTemplateSubRenderState(SubRenderState subRenderState)
        {
            for (int i = 0; i < SubRenderStateList.Count; i++)
            {
                if (SubRenderStateList[i] == subRenderState)
                {
                    ShaderGenerator.Instance.DestroySubRenderState(SubRenderStateList[i]);
                    SubRenderStateList.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the light count
        /// lightCount[0] defines the point light count.
        /// lightCount[1] defines the directional light count.
        /// lightCount[2] defines the spot light count
        /// </summary>
        /// <param name="lightCount"></param>
        public virtual void GetLightCount(out int[] lightCount)
        {
            lightCount = this.lightCount;
        }
        public virtual void SetLightCount(int[] currLightCount)
        {
            lightCount[0] = this.lightCount[0];
            lightCount[1] = this.lightCount[1];
            lightCount[2] = this.lightCount[2];
        }
        public virtual void Dispose()
        {
        }
        public bool LightCountAutoUpdate
        {
            get { return lightCountAutoUpdate; }
            set { lightCountAutoUpdate = value; }
        }
        



        public List<SubRenderState> TemplateSubRenderStateList
        {
            get { return SubRenderStateList; }
        }
    }
    class TargetRenderStateComparer : IComparer<SubRenderState>
    {

        public int Compare(SubRenderState x, SubRenderState y)
        {
            return x.ExecutionOrder - y.ExecutionOrder;
        }
    }
    /// <summary>
    /// This is the target render state. This class wil hold the actual generated CPU/GPU programs.
    /// It will be initially built from the FFP state of a given pass by the FFP builder, and then will be linked 
    /// with the custom pass render state and the global scheme render state. See ShaderGenerator.SGPass.BuildTargetRenderState()
    /// </summary>
    public class TargetRenderState : RenderState, IDisposable
    {
        bool subRenderStateSortValid;
        ProgramSet programSet;

        public TargetRenderState()
        {
            programSet = null;
            subRenderStateSortValid = false;
        }
        /// <summary>
        /// Link this target render state with the given render state.
	    /// Only sub render states with execution order that don't exist in this render state will be added.	
        /// </summary>
        /// <param name="other">The other render state to append to this state.</param>
        /// <param name="srcPass">The source pass that this render state is constructed from.</param>
        /// <param name="dstPass">The destination pass that constructed from this render state</param>
        public void Link(RenderState other, Pass srcPass, Pass dstPass)
        {
            List<SubRenderState> customSubRenderStates = new List<SubRenderState>();

            SortSubRenderStates();

            //insert all custom render states. (I.E. Not FFP sub render states).
            var subRenderStateList = other.TemplateSubRenderStateList;

            for (int i = 0; i < subRenderStateList.Count; i++)
            {
                var srcSubRenderState = subRenderStateList[i];
                bool isCustomRenderState = true;

                if (srcSubRenderState.ExecutionOrder == (int)FFPRenderState.FFPShaderStage.Transform ||
                    srcSubRenderState.ExecutionOrder == (int)FFPRenderState.FFPShaderStage.Color ||
                    srcSubRenderState.ExecutionOrder == (int)FFPRenderState.FFPShaderStage.Lighting ||
                    srcSubRenderState.ExecutionOrder == (int)FFPRenderState.FFPShaderStage.Texturing ||
                    srcSubRenderState.ExecutionOrder == (int)FFPRenderState.FFPShaderStage.Fog)
                {
                    isCustomRenderState = false;
                }

                if (isCustomRenderState)
                {
                    bool subStateTypeExists = false;
                    //check if this type of render state already exist
                    for (int j = 0; j < SubRenderStateList.Count; j++)
                    {
                        var itDst = SubRenderStateList[j];

                        if (itDst.Type == srcSubRenderState.Type)
                        {
                            subStateTypeExists = true;
                            break;
                        }
                    }

                    //Case custom sub render state not exists -> add it to custom list
                    if (subStateTypeExists == false)
                    {
                        SubRenderState newSubRenderState = null;

                        newSubRenderState = ShaderGenerator.Instance.CreateSubRenderState(srcSubRenderState.Type);
                        customSubRenderStates.Add(newSubRenderState);
                    }
                }
            }
            //merge the local custom sub render states
            for (int itSrc = 0; itSrc < customSubRenderStates.Count; itSrc++)
            {
                var customSubRenderState = customSubRenderStates[itSrc];
                if (customSubRenderState.PreAddToRenderState(this, srcPass, dstPass))
                {
                    AddSubRenderStateInstance(customSubRenderState);
                }
                else
                {
                    ShaderGenerator.Instance.DestroySubRenderState(customSubRenderState);
                }
            }
        }
        /// <summary>
        /// Update the GPU programs constant parameters before a renderable is rendered.
        /// </summary>
        /// <param name="rend">The renderable object that is going to be rendered</param>
        /// <param name="pass">the pass that is used to do the rendering operation</param>
        /// <param name="source">The auto parameter auto source instance</param>
        /// <param name="lightList">The light list used for the current rendering operation</param>
        public void UpdateGpuProgramsParams(IRenderable rend, Pass pass, AutoParamDataSource source, LightList lightList)
        {
            for (int i = 0; i < SubRenderStateList.Count; i++)
            {
                var curSubRenderState = SubRenderStateList[i];
                curSubRenderState.UpdateGpuProgramsParams(rend, pass, source, lightList);
            }
        }
        public override void Dispose()
        {
            DestroyProgramSet();
            base.Dispose();
        }

        protected void SortSubRenderStates()
        {
            if (subRenderStateSortValid == false)
            {
                if (SubRenderStateList.Count > 1)
                {
                    SubRenderStateList.Sort(new TargetRenderStateComparer());

                    subRenderStateSortValid = true;
                }
            }
        }
        public bool CreateCpuPrograms()
        {
            SortSubRenderStates();

            ProgramSet cProgramSet = CreateProgramSet();
            Program vsProgram = ProgramManager.Instance.CreateCpuProgram(GpuProgramType.Vertex);
            Program psProgram = ProgramManager.Instance.CreateCpuProgram(GpuProgramType.Fragment);
            Function vsMainFunc = null;
            Function psMainFunc = null;

            cProgramSet.CpuVertexProgram = vsProgram;
            cProgramSet.CpuFragmentProgram = psProgram;

            //Create entry point functions
            vsMainFunc = vsProgram.CreateFunction("main", "Vertex Program Entry point", Function.FunctionType.VsMain);
            vsProgram.EntryPointFunction = vsMainFunc;
            psMainFunc = psProgram.CreateFunction("main", "Pixel Program Entry point", Function.FunctionType.PsMain);
            psProgram.EntryPointFunction = psMainFunc;

            for (int i = 0; i < SubRenderStateList.Count; i++)
            {
                SubRenderState srcSubRenderState = SubRenderStateList[i];

                if (srcSubRenderState.CreateCpuSubPrograms(programSet) == false)
                {
                    Axiom.Core.LogManager.Instance.Write("RTShader.TargetRenderState: Could not generate sub render program of type: {0}", srcSubRenderState.Type);
                    return false;
                }
            }
            return true;
        }
        internal ProgramSet CreateProgramSet()
        {
            DestroyProgramSet();
            programSet = new ProgramSet();
            return programSet;
        }
        internal void DestroyProgramSet()
        {
            if (programSet != null)
            {
                programSet.Dispose();
                programSet = null;
            }
        }
        public void AddSubRenderStateInstance(SubRenderState subRenderState)
        {
            base.SubRenderStateList.Add(subRenderState);
            subRenderStateSortValid = false;
        }
        protected void RemoveSubRenderStateInstance(SubRenderState subRenderState)
        {
            for (int i = 0; i < SubRenderStateList.Count; i++)
            {
                SubRenderState it = SubRenderStateList[i];

                if (it == subRenderState)
                {
                    ShaderGenerator.Instance.DestroySubRenderState(it);
                    SubRenderStateList.RemoveAt(i);
                    break;
                }
            }
        }


        internal ProgramSet ProgramSet { get; set; }

    }
}
