using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
    internal class LinearSkinning : HardwareSkinningTechnique
    {
        public LinearSkinning()
        {
        }

        internal override bool ResolveParameters( ProgramSet programSet )
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Function vsMain = vsProgram.EntryPointFunction;

            //if needed mark this vertex program as hardware skinned
            if ( DoBoneCalculations )
            {
                vsProgram.SkeletalAnimationIncluded = true;
            }

            //get the parameters we need whther we are doing bone calculations or not

            //note in order t be consistent we will always output position, normal,
            //tangent, and binormal in both object and world space. And output position
            //in projective space to cover the responsibility of the transform stage

            //input param
            paramInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
                                                            Parameter.ContentType.PositionObjectSpace,
                                                            Graphics.GpuProgramParameters.GpuConstantType.Float4 );
            paramInNormal = vsMain.ResolveInputParameter( Parameter.SemanticType.Normal, 0,
                                                          Parameter.ContentType.NormalObjectSpace,
                                                          Graphics.GpuProgramParameters.GpuConstantType.Float3 );
            paramInBiNormal = vsMain.ResolveInputParameter( Parameter.SemanticType.Binormal, 0,
                                                            Parameter.ContentType.BinormalObjectSpace,
                                                            Graphics.GpuProgramParameters.GpuConstantType.Float3 );
            paramInTangent = vsMain.ResolveInputParameter( Parameter.SemanticType.Tangent, 0,
                                                           Parameter.ContentType.TangentObjectSpace,
                                                           Graphics.GpuProgramParameters.GpuConstantType.Float3 );

            //local param
            paramLocalPositionWorld = vsMain.ResolveLocalParameter( Parameter.SemanticType.Position, 0,
                                                                    Parameter.ContentType.PositionWorldSpace,
                                                                    Graphics.GpuProgramParameters.GpuConstantType.Float4 );
            paramLocalNormalWorld = vsMain.ResolveLocalParameter( Parameter.SemanticType.Normal, 0,
                                                                  Parameter.ContentType.NormalWorldSpace,
                                                                  Graphics.GpuProgramParameters.GpuConstantType.Float3 );
            paramLocalTangentWorld = vsMain.ResolveLocalParameter( Parameter.SemanticType.Tangent, 0,
                                                                   Parameter.ContentType.TangentWorldSpace,
                                                                   Graphics.GpuProgramParameters.GpuConstantType.Float3 );
            paramLocalBiNormalWorld = vsMain.ResolveLocalParameter( Parameter.SemanticType.Binormal, 0,
                                                                    Parameter.ContentType.BinormalWorldSpace,
                                                                    Graphics.GpuProgramParameters.GpuConstantType.Float3 );

            //output param
            paramOutPositionProj = vsMain.ResolveOutputParameter( Parameter.SemanticType.Position, 0,
                                                                  Parameter.ContentType.PositionProjectiveSpace,
                                                                  Graphics.GpuProgramParameters.GpuConstantType.Float4 );

            //check if parameter retrieval went well
            bool isValid =
                ( paramInPosition != null &&
                  paramInNormal != null &&
                  paramInBiNormal != null &&
                  paramInTangent != null &&
                  paramLocalPositionWorld != null &&
                  paramLocalNormalWorld != null &&
                  paramLocalTangentWorld != null &&
                  paramLocalBiNormalWorld != null &&
                  paramOutPositionProj != null );

            if ( doBoneCalculations )
            {
                GpuProgramParameters.AutoConstantType worldMatrixType =
                    GpuProgramParameters.AutoConstantType.WorldMatrixArray3x4;

                if ( ShaderGenerator.Instance.TargetLangauge == "hlsl" )
                {
                    //given that hlsl shaders use column major matrices which are not compatible with the cg
                    //and glsl method of row major matrices, we will use a full matrix instead
                    worldMatrixType = GpuProgramParameters.AutoConstantType.WorldMatrixArray;
                }

                //input parameters
                paramInNormal = vsMain.ResolveInputParameter( Parameter.SemanticType.Normal, 0,
                                                              Parameter.ContentType.NormalObjectSpace,
                                                              GpuProgramParameters.GpuConstantType.Float3 );
                paramInBiNormal = vsMain.ResolveInputParameter( Parameter.SemanticType.Binormal, 0,
                                                                Parameter.ContentType.BinormalObjectSpace,
                                                                GpuProgramParameters.GpuConstantType.Float3 );
                paramInTangent = vsMain.ResolveInputParameter( Parameter.SemanticType.Tangent, 0,
                                                               Parameter.ContentType.TangentObjectSpace,
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                paramInIndices = vsMain.ResolveInputParameter( Parameter.SemanticType.BlendIndicies, 0,
                                                               Parameter.ContentType.Unknown,
                                                               GpuProgramParameters.GpuConstantType.Float4 );
                paramInWeights = vsMain.ResolveInputParameter( Parameter.SemanticType.BlendWeights, 0,
                                                               Parameter.ContentType.Unknown,
                                                               GpuProgramParameters.GpuConstantType.Float4 );
                paramInWorldMatrices = vsProgram.ResolveAutoParameterInt( worldMatrixType, 0, boneCount );
                paramInInvWorldMatrix =
                    vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.InverseWorldMatrix, 0 );
                paramInViewProjMatrix =
                    vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.ViewProjMatrix, 0 );

                paramTempFloat4 = vsMain.ResolveLocalParameter( Parameter.SemanticType.Unknown, -1, "TempVal4",
                                                                GpuProgramParameters.GpuConstantType.Float3 );
                paramTempFloat3 = vsMain.ResolveLocalParameter( Parameter.SemanticType.Unknown, -1, "TempVal3",
                                                                GpuProgramParameters.GpuConstantType.Float3 );

                //check if parameter retrival went well
                isValid &=
                    ( paramInIndices != null &&
                      paramInWeights != null &&
                      paramInWorldMatrices != null &&
                      paramInViewProjMatrix != null &&
                      paramInInvWorldMatrix != null &&
                      paramTempFloat4 != null &&
                      paramTempFloat3 != null );
            }
            else
            {
                paramInWorldMatrices =
                    vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldMatrix, 0 );
                paramInWorldViewProjMatrix =
                    vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldViewProjMatrix, 0 );

                //check if parameter retrieval went well
                isValid &=
                    ( paramInWorldMatrix != null &&
                      paramInWorldViewProjMatrix != null );
            }

            return isValid;
        }

        internal override bool ResolveDependencies( ProgramSet programSet )
        {
            Program vsProgram = programSet.CpuVertexProgram;
            vsProgram.AddDependency( FFPRenderState.FFPLibCommon );
            vsProgram.AddDependency( FFPRenderState.FFPLibTransform );


            return true;
        }

        internal override bool AddFunctionInvocations( ProgramSet programSet )
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            int internalCounter = 0;

            //add functions to calculate position data in wold, object and projective space
            AddPositionCalculations( vsMain, ref internalCounter );

            //add functions to calculate normal and normal related data in world and object space
            AddNormalRelatedCalculations( vsMain, paramInNormal, paramLocalNormalWorld, ref internalCounter );
            AddNormalRelatedCalculations( vsMain, paramInTangent, paramLocalTangentWorld, ref internalCounter );
            AddNormalRelatedCalculations( vsMain, paramInBiNormal, paramLocalBiNormalWorld, ref internalCounter );

            return true;
        }

        private void AddPositionCalculations( Function vsMain, ref int funcCounter )
        {
            FunctionInvocation curFuncInvocation = null;

            if ( doBoneCalculations == true )
            {
                //set functions to calculate world position
                for ( int i = 0; i < WeightCount; i++ )
                {
                    AddIndexedPositionWeight( vsMain, i, ref funcCounter );
                }

                //update back the original position relative to the object
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncTransform,
                                                            (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                            funcCounter++ );
                curFuncInvocation.PushOperand( paramInInvWorldMatrix, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramLocalPositionWorld, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramInPosition, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );

                //update the projective position thereby filling the transform stage role
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncTransform,
                                                            (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                            funcCounter++ );
                curFuncInvocation.PushOperand( paramInViewProjMatrix, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramLocalPositionWorld, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramOutPositionProj, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );
            }
            else
            {
                //update from object to world space
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncTransform,
                                                            (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                            funcCounter++ );
                curFuncInvocation.PushOperand( paramInWorldMatrix, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramInPosition, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramLocalPositionWorld, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );

                //update from ojbect to projective space
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncTransform,
                                                            (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                            funcCounter++ );
                curFuncInvocation.PushOperand( paramInWorldViewProjMatrix, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramInPosition, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramOutPositionProj, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );
            }
        }

        private void AddIndexedPositionWeight( Function vsMain, int index, ref int funcCounter )
        {
            Operand.OpMask indexMask = IndexToMask( index );

            FunctionInvocation curFuncInvocation;

            var outputMask = (int)( Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z );

            if ( paramInWorldMatrices.Type == GpuProgramParameters.GpuConstantType.Matrix_4X4 )
            {
                outputMask = (int)Operand.OpMask.All;
            }

            //multiply posiiton with world matrix and put into temporary param
            curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncTransform,
                                                        (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                        funcCounter++ );
            curFuncInvocation.PushOperand( paramInWorldMatrix, Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( paramInIndices, Operand.OpSemantic.In, (int)indexMask, 1 );
            curFuncInvocation.PushOperand( paramInPosition, Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( paramTempFloat4, Operand.OpSemantic.Out, outputMask );
            vsMain.AddAtomInstance( curFuncInvocation );

            //set w value of temporary param to 1
            curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
                                                        (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                        funcCounter++ );
            curFuncInvocation.PushOperand( ParameterFactory.CreateConstParamFloat( 1.0f ), Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( paramTempFloat4, Operand.OpSemantic.Out, (int)Operand.OpMask.W );
            vsMain.AddAtomInstance( curFuncInvocation );

            //multiply temporary param with weight
            curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate,
                                                        (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                        funcCounter++ );
            curFuncInvocation.PushOperand( paramTempFloat4, Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( paramInWeights, Operand.OpSemantic.In, (int)indexMask );
            curFuncInvocation.PushOperand( paramTempFloat4, Operand.OpSemantic.Out );
            vsMain.AddAtomInstance( curFuncInvocation );

            //check if on first iteration
            if ( index == 0 )
            {
                //set the local param as the value of the world param
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
                                                            (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                            funcCounter++ );
                curFuncInvocation.PushOperand( paramTempFloat4, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramLocalPositionWorld, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );
            }
            else
            {
                //add the local param as the value of the world param
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAdd,
                                                            (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                            funcCounter++ );
                curFuncInvocation.PushOperand( paramTempFloat4, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramLocalPositionWorld, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( paramLocalPositionWorld, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );
            }
        }

        private void AddNormalRelatedCalculations( Function vsMain, Parameter normalRelatedParam,
                                                   Parameter normalWorldRelatedParam, ref int funcCounter )
        {
            FunctionInvocation curFuncInvocation;

            if ( doBoneCalculations )
            {
                //set functions to calculate world normal
                for ( int i = 0; i < weightCount; i++ )
                {
                    AddIndexedNormalRelatedWeight( vsMain, normalRelatedParam, normalWorldRelatedParam, i,
                                                   ref funcCounter );
                }

                //update back the original position relative to the object
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncTransform,
                                                            (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                            funcCounter++ );
                curFuncInvocation.PushOperand( paramInInvWorldMatrix, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( normalWorldRelatedParam, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( normalRelatedParam, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );
            }
            else
            {
                //update back the original position relative to the object
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncTransform,
                                                            (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                            funcCounter++ );
                curFuncInvocation.PushOperand( paramInWorldMatrix, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( normalRelatedParam, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( normalWorldRelatedParam, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );
            }
        }

        private void AddIndexedNormalRelatedWeight( Function vsMain, Parameter normalRelatedParam,
                                                    Parameter normalWorldRelatedParam, int index, ref int funcCounter )
        {
            FunctionInvocation curFuncInvocation;
            Operand.OpMask indexMask = IndexToMask( index );

            //multiply position with world matrix and put into temporary param
            curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncTransform,
                                                        (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                        funcCounter++ );
            curFuncInvocation.PushOperand( paramInWorldMatrices, Operand.OpSemantic.In, (int)Operand.OpMask.All );
            curFuncInvocation.PushOperand( paramInIndices, Operand.OpSemantic.In, (int)indexMask, 1 );
            curFuncInvocation.PushOperand( normalRelatedParam, Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( paramTempFloat3, Operand.OpSemantic.Out );
            vsMain.AddAtomInstance( curFuncInvocation );

            //multiply temporary param with weight
            curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate,
                                                        (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                        funcCounter++ );
            curFuncInvocation.PushOperand( paramTempFloat3, Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( paramInWeights, Operand.OpSemantic.In, (int)indexMask );
            curFuncInvocation.PushOperand( paramTempFloat3, Operand.OpSemantic.Out );
            vsMain.AddAtomInstance( curFuncInvocation );

            //check if on first iteration
            if ( index == 0 )
            {
                //set the local param as the value of the world normal
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
                                                            (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                            funcCounter++ );
                curFuncInvocation.PushOperand( paramTempFloat3, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( normalWorldRelatedParam, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( normalWorldRelatedParam, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );
            }
            else
            {
                //add the local param as the value of the world normal
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAdd,
                                                            (int)FFPRenderState.FFPVertexShaderStage.VSTransform,
                                                            funcCounter++ );
                curFuncInvocation.PushOperand( paramTempFloat3, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( normalWorldRelatedParam, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( normalWorldRelatedParam, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );
            }
        }
    }
}