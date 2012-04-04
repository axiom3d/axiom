using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Components.RTShaderSystem
{
    class DualQuaternionSkinning : HardwareSkinningTechnique
    {
        UniformParameter paramInScaleShearMatrices;
        Parameter paramLocalBlendPosition;
        Parameter paramBlendS;
        Parameter paramBlendDQ;
        Parameter paramInitialDQ;
        Parameter paramTempWorldMatrix;

        Parameter paramTempFloat2x4;
        Parameter paramTempFloat3x3;
        Parameter paramTempFloat3x4;

        Parameter paramIndex1, paramIndex2;
        static string SGXLibQuaternion = "SGXLib_DualQuaternion";
        static string SGXFuncBlendWeight = "SGX_BlendWeight";
        static string SGXFuncAntipodalityAdjustment = "SGX_AntipodalityAdjustment";
        static string SGXFuncCalculateBlendPosition = "SGX_CalculateBlendPosition";
        static string SGXFuncCalculateBlendNormal = "SGX_CalculateBlendNormal";
        static string SGXFuncNormalizeDualQuaternion = "SGX_NormalizeDualQuaternion";
        static string SGXFuncAdjointTransposeMatrix = "SGX_AdjointTransposeMatrix";
        static string SGXFuncBuildDualQuaternionMatrix = "SGX_BuildDualQuaternionMatrix";

        public DualQuaternionSkinning()
        { }

        internal override bool ResolveParameters(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Function vsMain = vsProgram.EntryPointFunction;

            //if needed mark this vertex program as hardware skinned
            if (doBoneCalculations)
            {
                vsProgram.SkeletalAnimationIncluded = true;
            }

            //get the parameters we need whether we are doing bone calculations or not

            // Note: in order to be consistent we will always output position, normal,
            // tangent and binormal in both object and world space. And output position
            // in projective space to cover the responsibility of the transform stage

            //input param
            paramInPosition = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0, Parameter.ContentType.PositionObjectSpace, Graphics.GpuProgramParameters.GpuConstantType.Float4);
            paramInNormal = vsMain.ResolveInputParameter(Parameter.SemanticType.Normal, 0, Parameter.ContentType.NormalObjectSpace, Graphics.GpuProgramParameters.GpuConstantType.Float3);
            paramInBiNormal = vsMain.ResolveInputParameter(Parameter.SemanticType.Binormal, 0, Parameter.ContentType.BinormalObjectSpace, Graphics.GpuProgramParameters.GpuConstantType.Float3);
            paramInTangent = vsMain.ResolveInputParameter(Parameter.SemanticType.Tangent, 0, Parameter.ContentType.TangentObjectSpace, Graphics.GpuProgramParameters.GpuConstantType.Float3);

            //local param
            paramLocalBlendPosition = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "BlendedPosition", Graphics.GpuProgramParameters.GpuConstantType.Float3);
            paramLocalPositionWorld = vsMain.ResolveLocalParameter(Parameter.SemanticType.Position, 0, Parameter.ContentType.PositionWorldSpace, Graphics.GpuProgramParameters.GpuConstantType.Float4);
            paramLocalNormalWorld = vsMain.ResolveLocalParameter(Parameter.SemanticType.Normal, 0, Parameter.ContentType.NormalWorldSpace, Graphics.GpuProgramParameters.GpuConstantType.Float3);
            paramLocalTangentWorld = vsMain.ResolveLocalParameter(Parameter.SemanticType.Tangent, 0, Parameter.ContentType.TangentWorldSpace, Graphics.GpuProgramParameters.GpuConstantType.Float3);
            paramLocalBiNormalWorld = vsMain.ResolveLocalParameter(Parameter.SemanticType.Binormal, 0, Parameter.ContentType.BinormalWorldSpace, Graphics.GpuProgramParameters.GpuConstantType.Float3);

            //output param
            paramOutPositionProj = vsMain.ResolveOutputParameter(Parameter.SemanticType.Position, 0, Parameter.ContentType.PositionProjectiveSpace, Graphics.GpuProgramParameters.GpuConstantType.Float4);

            //check if parameter retrieval went well
            bool isValid =
                (paramInPosition != null &&
                paramInNormal != null &&
                paramInBiNormal != null &&
                paramInTangent != null &&
                paramLocalPositionWorld != null &&
                paramLocalNormalWorld != null &&
                paramLocalTangentWorld != null &&
                paramLocalBiNormalWorld != null &&
                paramOutPositionProj != null);

            if (doBoneCalculations)
            {
                //input parameters
                paramInNormal = vsMain.ResolveInputParameter(Parameter.SemanticType.Normal, 0, Parameter.ContentType.NormalObjectSpace, Graphics.GpuProgramParameters.GpuConstantType.Float3);
                paramInBiNormal = vsMain.ResolveInputParameter(Parameter.SemanticType.Binormal, 0, Parameter.ContentType.BinormalObjectSpace, Graphics.GpuProgramParameters.GpuConstantType.Float3);
                paramInTangent = vsMain.ResolveInputParameter(Parameter.SemanticType.Tangent, 0, Parameter.ContentType.TangentObjectSpace, Graphics.GpuProgramParameters.GpuConstantType.Float3);
                paramInIndices = vsMain.ResolveInputParameter(Parameter.SemanticType.BlendIndicies, 0, Parameter.ContentType.Unknown, Graphics.GpuProgramParameters.GpuConstantType.Float4);
                paramInWeights = vsMain.ResolveInputParameter(Parameter.SemanticType.BlendWeights, 0, Parameter.ContentType.Unknown, Graphics.GpuProgramParameters.GpuConstantType.Float4);
                //ACT_WORLD_DUALQUATERNION_ARRAY_2x4 is an array of float4s, so there are two indices for each bone
                //TODO
                //paramInWorldMatrices = vsProgram.ResolveAutoParameterInt(Graphics.GpuProgramParameters.AutoConstantType.WorldDualQuatenrionArray_2x4, Graphics.GpuProgramParameters.GpuConstantType.Float4, 0, boneCount * 2);
                paramInInvWorldMatrix = vsProgram.ResolveAutoParameterInt(Graphics.GpuProgramParameters.AutoConstantType.InverseWorldMatrix, 0);
                paramInViewProjMatrix = vsProgram.ResolveAutoParameterInt(Graphics.GpuProgramParameters.AutoConstantType.ViewProjMatrix, 0);

                paramTempWorldMatrix = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "worldMatrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_2X4);
                paramBlendDQ = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "blendDQ", Graphics.GpuProgramParameters.GpuConstantType.Matrix_2X4);
                paramInitialDQ = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "initialDQ", Graphics.GpuProgramParameters.GpuConstantType.Matrix_2X4);
                paramIndex1 = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "index1", Graphics.GpuProgramParameters.GpuConstantType.Float1);
                paramIndex2 = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "index2", Graphics.GpuProgramParameters.GpuConstantType.Float1);

                if (scalingShearingSupport)
                {
                    //TODO
                    //paramInScaleShearMatrices = vsProgram.ResolveAutoParameterInt(Graphics.GpuProgramParameters.AutoConstantType.WorldScaleShearMatrixArray3x4, 0, boneCount);
                    paramBlendS = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "blendS", Graphics.GpuProgramParameters.GpuConstantType.Matrix_3X4);
                    paramTempFloat3x3 = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "TempVal3x3", Graphics.GpuProgramParameters.GpuConstantType.Matrix_3X3);
                    paramTempFloat3x4 = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "TempVal3x4", Graphics.GpuProgramParameters.GpuConstantType.Matrix_3X4);

                }
                paramTempFloat2x4 = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "TempVal2x4", Graphics.GpuProgramParameters.GpuConstantType.Matrix_2X4);
                paramTempFloat4 = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "TempVal4", Graphics.GpuProgramParameters.GpuConstantType.Float4);
                paramTempFloat3 = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, -1, "TempVal3", Graphics.GpuProgramParameters.GpuConstantType.Float3);

                //check if parameter retrieval went well
                isValid &=
                    (paramInIndices != null &&
                    paramInWeights != null &&
                    paramInWorldMatrices != null &&
                    paramInViewProjMatrix != null &&
                    paramInInvWorldMatrix != null &&
                    paramBlendDQ != null &&
                   paramInitialDQ != null &&
                   paramIndex1 != null &&
                   paramIndex2 != null &&

                   (!scalingShearingSupport || (scalingShearingSupport &&
                   paramInScaleShearMatrices != null && paramBlendS != null &&
                   paramTempFloat3x3 != null && paramTempFloat3x4 != null)) &&

                   paramTempFloat2x4 != null &&
                   paramTempFloat4 != null &&
                   paramTempFloat3 != null);

            }
            else
            {
                paramInWorldMatrix = vsProgram.ResolveAutoParameterInt(Graphics.GpuProgramParameters.AutoConstantType.WorldMatrix, 0);
                paramInWorldViewProjMatrix = vsProgram.ResolveAutoParameterInt(Graphics.GpuProgramParameters.AutoConstantType.WorldViewProjMatrix, 0);

                isValid &= paramInWorldMatrix != null && paramInWorldViewProjMatrix != null;

            }

            return isValid;
        }
        internal override bool ResolveDependencies(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            vsProgram.AddDependency(FFPRenderState.FFPLibCommon);
            vsProgram.AddDependency(FFPRenderState.FFPLibTransform);
            vsProgram.AddDependency(SGXLibQuaternion);

            return true;
        }
        internal override bool AddFunctionInvocations(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            int internalCounter = 0;

            //add functions to calculate position data in world, object and projective space
            AddPositionCalculations(vsMain, ref internalCounter);

            //add functions to calculate normal and normal related data in world and object space
            AddNormalRelatedCalculations(vsMain, paramInNormal, paramLocalNormalWorld, ref internalCounter);
            AddNormalRelatedCalculations(vsMain, paramInTangent, paramLocalTangentWorld, ref internalCounter);
            AddNormalRelatedCalculations(vsMain, paramInBiNormal, paramLocalBiNormalWorld, ref internalCounter);
            return true;
        }

        private void AddPositionCalculations(Function vsMain, ref int funcCounter)
        {
            FunctionInvocation curFuncInvocation = null;

            if (doBoneCalculations)
            {
                if (scalingShearingSupport)
                {
                    //Construct a scaling and shearing matrix based on the blend weights
                    for (int i = 0; i < WeightCount; i++)
                    {
                        //Assign the local param based on the current index of the scaling and shearing matrices
                        curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                        curFuncInvocation.PushOperand(paramInScaleShearMatrices, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(paramInIndices, Operand.OpSemantic.In, (int)IndexToMask(i), 1);
                        curFuncInvocation.PushOperand(paramTempFloat3x4, Operand.OpSemantic.Out);
                        vsMain.AddAtomInstance(curFuncInvocation);

                        //Calculate the resultant scaling and shearing matrix based on the weigts given
                        AddIndexedPositionWeight(vsMain, i, paramTempFloat3x4, paramTempFloat3x4, paramBlendS, ref funcCounter);

                    }

                    //Transform the position based on the scaling and shearing matrix
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncTransform, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                    curFuncInvocation.PushOperand(paramBlendS, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(paramInPosition, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                    curFuncInvocation.PushOperand(paramLocalBlendPosition, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);
                }
                else
                {
                    //Assign the input position to the local blended position
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                    curFuncInvocation.PushOperand(paramInPosition, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                    curFuncInvocation.PushOperand(paramLocalBlendPosition, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);
                }

                //Set functions to calculate world position
                for (int i = 0; i < weightCount; i++)
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                    curFuncInvocation.PushOperand(paramInIndices, Operand.OpSemantic.In, (int)IndexToMask(i));
                    curFuncInvocation.PushOperand(paramIndex1, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);

                    //multiply the index by 2
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncModulate, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                    curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(2.0f), Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(paramIndex1, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(paramIndex1, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);

                    //Add 1 to the index and assign as the second row's index
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAdd, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                    curFuncInvocation.PushOperand(ParameterFactory.CreateConstParamFloat(1.0f), Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(paramIndex1, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(paramIndex2, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);

                    //Build the dual quaternion matrix
                    curFuncInvocation = new FunctionInvocation(DualQuaternionSkinning.SGXFuncBuildDualQuaternionMatrix, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                    curFuncInvocation.PushOperand(paramInWorldMatrices, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(paramIndex1, Operand.OpSemantic.In, (int)Operand.OpMask.All, 1);
                    curFuncInvocation.PushOperand(paramInWorldMatrices, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(paramIndex2, Operand.OpSemantic.In, (int)Operand.OpMask.All, 1);
                    curFuncInvocation.PushOperand(paramTempFloat2x4, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);

                    if (correctAntipodalityHandling)
                    {
                        AdjustForCorrectAntipodality(vsMain, i, ref funcCounter, paramTempFloat2x4);
                    }

                    //Calculate the resultant dual quaternion based based on the weights given
                    AddIndexedPositionWeight(vsMain, i, paramTempFloat2x4, paramTempFloat2x4, paramBlendDQ, ref funcCounter);

                }
                //Normalize the dual quaternion
                curFuncInvocation = new FunctionInvocation(DualQuaternionSkinning.SGXFuncNormalizeDualQuaternion, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(paramBlendDQ, Operand.OpSemantic.InOut);
                vsMain.AddAtomInstance(curFuncInvocation);

                //Calculate the blend position
                curFuncInvocation = new FunctionInvocation(DualQuaternionSkinning.SGXFuncCalculateBlendPosition, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(paramLocalBlendPosition, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramBlendDQ, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramTempFloat4, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);

                //Update from object to projective space
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncTransform, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(paramInViewProjMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramTempFloat4, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramOutPositionProj, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }
            else
            {
                //update from object to world space
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncTransform, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(paramInWorldMatrices, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramInPosition, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramLocalPositionWorld, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);

                //update from object to projective space
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncTransform, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(paramInWorldViewProjMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramInPosition, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramOutPositionProj, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }
        }
        private void AddNormalRelatedCalculations(Function vsMain, Parameter normalRelatedParam, Parameter normalWorldRelatedParam, ref int funcCounter)
        {
            FunctionInvocation curFuncInvocation = null;

            if (doBoneCalculations)
            {
                if (scalingShearingSupport)
                {
                    //Calculate the adjoint transpose of the blended scaling and shearing matrix
                    curFuncInvocation = new FunctionInvocation(DualQuaternionSkinning.SGXFuncAdjointTransposeMatrix, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                    curFuncInvocation.PushOperand(paramBlendS, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(paramTempFloat3x3, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);

                    //Transform the normal by the adjoint transpose of the blended scaling and shearing matrix
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncTransform, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                    curFuncInvocation.PushOperand(paramTempFloat3x3, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(normalRelatedParam, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(normalRelatedParam, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);

                    //Need to normalize again after transforming the normal
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncNormalize, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                    curFuncInvocation.PushOperand(normalRelatedParam, Operand.OpSemantic.InOut);
                    vsMain.AddAtomInstance(curFuncInvocation);
                }

                //Transform the normal according to the dual quaternion
                curFuncInvocation = new FunctionInvocation(DualQuaternionSkinning.SGXFuncCalculateBlendNormal, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(normalRelatedParam, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramBlendDQ, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(normalWorldRelatedParam, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);

                //update back the original position relative to the object
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncTransform, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(paramInInvWorldMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(normalWorldRelatedParam, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(normalRelatedParam, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }
            else
            {
                //update from object to world space
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncTransform, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(paramInWorldMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(normalRelatedParam, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(normalWorldRelatedParam, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }
        }
        private void AdjustForCorrectAntipodality(Function vsMain, int index, ref int funcCounter, Parameter tempWorldMatrix)
        {
            FunctionInvocation curFuncInvocation = null;
            //Antipodality doesn't need to be adjusted for dq0 on itself (used as the basis of antipodality calculations)
            if (index > 0)
            {
                curFuncInvocation = new FunctionInvocation(SGXFuncAntipodalityAdjustment, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                //This is the base dual quaternion dq0, which the antipodality calculations are based on
                curFuncInvocation.PushOperand(paramInitialDQ, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramTempFloat2x4, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(tempWorldMatrix, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }
            else if (index == 0)
            {
                //Set the first dual quaternion as the initial dq
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(paramTempFloat2x4, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(paramInitialDQ, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }
        }

        private void AddIndexedPositionWeight(Function vsMain, int index, Parameter worldMatrix, Parameter positionTempParameter, Parameter positionRelatedOutputParam, ref int funcCounter)
        {
            Operand.OpMask indexMask = IndexToMask(index);
            FunctionInvocation curFuncInvocation = null;

            //multiply position with world matrix and put into temporary param
            curFuncInvocation = new FunctionInvocation(SGXFuncBlendWeight, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
            curFuncInvocation.PushOperand(paramInWeights, Operand.OpSemantic.In, indexMask);
            curFuncInvocation.PushOperand(worldMatrix, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(positionTempParameter, Operand.OpSemantic.Out);
            vsMain.AddAtomInstance(curFuncInvocation);

            //check if on first iteration
            if (index == 0)
            {
                //set the local param as the value of the world param
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(positionTempParameter, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(positionRelatedOutputParam, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }
            else
            {
                //add the local param as the value of the world param
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAdd, (int)FFPRenderState.FFPVertexShaderStage.VSTransform, funcCounter++);
                curFuncInvocation.PushOperand(positionTempParameter, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(positionRelatedOutputParam, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(positionRelatedOutputParam, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }
        }

       

    }
}
