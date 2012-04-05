using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
    public class PerPixelLighting : SubRenderState
    {
        public class LightParams
        {
           public LightType Type;
           public UniformParameter Position;
           public UniformParameter Direction;
           public UniformParameter AttenuatParams;
           public UniformParameter SpotParams;
           public UniformParameter DiffuseColor;
           public UniformParameter SpecularColor;
        }
        public static string SGXType = "SGX_PerPixelLighting";
        static string SGXLibPerPixelLighting = "SGXLib_PerPixelLighting";
        static string SGXFuncTransformNormal = "SGX_TransformNormal";
        static string SGXFuncTransformPosition = "SGX_TransformPosition";
        static string SGXFuncLightDirectionalDiffuse = "SGX_Light_Directional_Diffuse";
        static string SGXFuncLightDirectionalDiffuseSpecular = "SGX_Light_Directional_DiffuseSpecular";
        static string SGXFuncLightPointDiffuse = "SGX_Light_Point_Diffuse";
        static string SGXFuncLightPointDiffuseSpecular = "SGX_Light_Point_DiffuseSpecular";
        static string SGXFuncLightSpotDiffuse = "SGX_Light_Spot_Diffuse";
        static string SGXFuncLightSpotDiffuseSpecular = "SGX_Light_Spot_DiffuseSpecular";
        TrackVertexColor trackVertexColorType;
        bool specularEnable;
        List<LightParams> lightParamsList;
        UniformParameter worldViewMatrix, worldViewITMatrix;
        Parameter vsInPosition;
        Parameter vsOutViewPos;
        Parameter psInViewPos;
        Parameter vsInNormal;
        Parameter vsOutNormal;
        Parameter psInNormal;
        Parameter psTempDiffuseColor;
        Parameter psTempSpecularColor;
        Parameter psDiffuse;
        Parameter psSpecular;
        Parameter psOutSpecular;
        Parameter psOutDiffuse;
        UniformParameter derivedSceneColor;
        UniformParameter lightAmbientColor;
        UniformParameter derivedAmbientLightColor;
        UniformParameter surfaceAmbientColor;
        UniformParameter surfaceDiffuseColor;
        UniformParameter surfaceSpecularColor;
        UniformParameter surfaceEmissiveColor;
        UniformParameter surfaceShininess;
        Light blankLight;

        public PerPixelLighting()
        {
            trackVertexColorType = TrackVertexColor.None;
            specularEnable = false;
            blankLight = new Light();
            blankLight.Diffuse = ColorEx.Black;
            blankLight.Specular = ColorEx.Black;
            blankLight.SetAttenuation(0, 1, 0, 0);
        }

        public override void UpdateGpuProgramsParams(Graphics.IRenderable rend, Graphics.Pass pass, Graphics.AutoParamDataSource source, Core.Collections.LightList lightList)
        {
            if (lightParamsList.Count == 0)
                return;

            Matrix4 matView = source.ViewMatrix;
            LightType curLightType = LightType.Directional;
            int curSearchLightIndex = 0;

            //Update per light parameters.
            for (int i = 0; i < lightParamsList.Count; i++)
            {
                PerPixelLighting.LightParams curParams = lightParamsList[i];

                if (curLightType != curParams.Type)
                {
                    curLightType = curParams.Type;
                    curSearchLightIndex = 0;
                }
                Light srcLight = null;
                Vector4 vParamter;
                ColorEx color;

                //Search a matching light from the current sorted light of the given renderble
                for (int j = curSearchLightIndex; j < lightList.Count; j++)
                {
                    if (lightList[j].Type == curLightType)
                    {
                        srcLight = lightList[j];
                        curSearchLightIndex = j + 1;
                        break;
                    }
                }
                //no matching light found -> use a blnak dummy light for parameter update.
                if (srcLight == null)
                {
                    srcLight = blankLight;
                }

                switch (curParams.Type)
                {
                    case LightType.Directional:
                        //Update light direction
                        vParamter = matView.TransformAffine(srcLight.GetAs4DVector(true));
                        curParams.Direction.SetGpuParameter(vParamter);
                        break;
                    case LightType.Point:
                        //Update light position.
                        vParamter = matView.TransformAffine(srcLight.GetAs4DVector(true));
                        curParams.Position.SetGpuParameter(vParamter);

                        //Update light attenuation paramters.
                        vParamter.x = srcLight.AttenuationRange;
                        vParamter.y = srcLight.AttenuationConstant;
                        vParamter.z = srcLight.AttenuationLinear;
                        vParamter.w = srcLight.AttenuationQuadratic;
                        curParams.AttenuatParams.SetGpuParameter(vParamter);
                        break;
                    case LightType.Spotlight:
                        {
                            Vector3 vec3;
                            Matrix3 matViewIT;

                            //Update light position
                            vParamter = matView.TransformAffine(srcLight.GetAs4DVector(true));
                            curParams.Position.SetGpuParameter(vParamter);

                            //Update light direction
                            source.InverseTransposeViewMatrix.Extract3x3Matrix(out matViewIT);
                            vec3 = matViewIT * srcLight.DerivedDirection;
                            vec3.Normalize();

                            vParamter.x = -vec3.x;
                            vParamter.y = -vec3.y;
                            vParamter.z = -vec3.z;
                            vParamter.w = 0.0;
                            curParams.Direction.SetGpuParameter(vParamter);

                            //Update spotlight parameters
                            Real phi = Axiom.Math.Utility.Cos(srcLight.SpotlightOuterAngle);
                            Real theta = Axiom.Math.Utility.Cos(srcLight.SpotlightInnerAngle);

                            vec3.x = theta;
                            vec3.y = phi;
                            vec3.z = srcLight.SpotlightFalloff;

                            curParams.SpotParams.SetGpuParameter(vec3);
                        }
                        break;
                }

                //update diffuse color
                if ((trackVertexColorType & TrackVertexColor.Diffuse) == 0)
                {
                    color = srcLight.Diffuse * pass.Diffuse;
                    curParams.DiffuseColor.SetGpuParameter(color);
                }
                else
                {
                    color = srcLight.Diffuse;
                    curParams.DiffuseColor.SetGpuParameter(color);
                }
            }
        }
        public override bool PreAddToRenderState(TargetRenderState targetRenderState, Graphics.Pass srcPass, Graphics.Pass dstPass)
        {
            if (srcPass.LightingEnabled == false)
                return false;

            int[] lightCount = new int[3];
            targetRenderState.GetLightCount(out lightCount);

            TrackVertexColorType = srcPass.VertexColorTracking;

            if (srcPass.Shininess > 0.0 && srcPass.Specular != ColorEx.Black)
            {
                SpecularEnable = true;
            }
            else
            {
                SpecularEnable = false; 
            }

            //Case this pass should run once per light(s) -> override the light policy.
            if (srcPass.IteratePerLight)
            {
                //This is the preferred case -> only one type of light is handled
                if (srcPass.RunOnlyOncePerLightType)
                {
                    if (srcPass.OnlyLightType == LightType.Point)
                    {
                        lightCount[0] = srcPass.LightsPerIteration;
                        lightCount[1] = 0;
                        lightCount[2] = 0;
                    }
                    else if (srcPass.OnlyLightType == LightType.Directional)
                    {
                        lightCount[0] = 0;
                        lightCount[1] = srcPass.LightsPerIteration;
                        lightCount[2] = 0;
                    }
                    else if (srcPass.OnlyLightType == LightType.Spotlight)
                    {
                        lightCount[0] = 0;
                        lightCount[1] = 0;
                        lightCount[2] = srcPass.LightsPerIteration;
                    }
                }
                else
                {
                    throw new AxiomException("Using iterative lighting method with RT Shader System requires specifying explicit light type.");

                }
            }

            SetLightCount(lightCount);

            return true;
        }
        public override void GetLightCount(out int[] lightCount)
        {
            lightCount = new int[3] { 0, 0, 0 };

            for (int i = 0; i < lightParamsList.Count; i++)
            {
                LightParams curParams = lightParamsList[i];

                if (curParams.Type == LightType.Point)
                    lightCount[0]++;
                else if (curParams.Type == LightType.Directional)
                    lightCount[1]++;
                else if (curParams.Type == LightType.Spotlight)
                    lightCount[2]++;
            }
        }
        public override void SetLightCount(int[] currLightCount)
        {
            for (int type = 0; type < 3; type++)
            {
                for (int i = 0; i < currLightCount[type]; i++)
                {
                    LightParams curParams = new LightParams();

                    if (type == 0)
                    {
                        curParams.Type = LightType.Point;
                    }
                    else if (type == 1)
                        curParams.Type = LightType.Directional;
                    else if (type == 2)
                        curParams.Type = LightType.Spotlight;

                    lightParamsList.Add(curParams);
                }
            }
        }
        internal override bool ResolveParameters(ProgramSet programSet)
        {
            if (ResolveGlobalParameters(programSet) == false)
                return false;
            if (ResolveParameters(programSet) == false)
                return false;

            return true;
        }
        protected bool ResolveGlobalParameters(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            //Resolve world view IT matrix
            worldViewITMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.InverseTransposeWorldViewMatrix, 0);
            if (worldViewITMatrix == null)
                return false;

            //Get surface ambient color if need to
            if ((trackVertexColorType & TrackVertexColor.Ambient) == 0)
            {
                derivedAmbientLightColor = psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.DerivedAmbientLightColor, 0);
                if (derivedAmbientLightColor == null)
                    return false;
            }
            else
            {
                lightAmbientColor = psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.AmbientLightColor, 0);
                if (lightAmbientColor == null)
                    return false;

                surfaceAmbientColor = psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.SurfaceAmbientColor, 0);
                if (surfaceAmbientColor == null)
                    return false;
            }

            //Get surface diffuse color if need to
            if ((trackVertexColorType & TrackVertexColor.Diffuse) == 0)
            {
                surfaceDiffuseColor = psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.SurfaceDiffuseColor, 0);
                if (surfaceDiffuseColor == null)
                    return false;
            }

            //Get surface emissive color if need to
            if ((trackVertexColorType & TrackVertexColor.Emissive) == 0)
            {
                surfaceEmissiveColor = psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.SurfaceEmissiveColor, 0);
                if (surfaceEmissiveColor == null)
                    return false;
            }
            
            //Get derived scene color
            derivedSceneColor = psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.DerivedSceneColor, 0);
            if (derivedSceneColor == null)
                return false;

            //Get surface shininess
            surfaceShininess = psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.SurfaceShininess, 0);
            if (surfaceShininess == null)
                return false;

            //Resolve input vertex shader normal
            vsInNormal = vsMain.ResolveInputParameter(Parameter.SemanticType.Normal, 0, Parameter.ContentType.NormalObjectSpace, GpuProgramParameters.GpuConstantType.Float3);
            if (vsInNormal == null)
                return false;

            vsOutNormal = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1, Parameter.ContentType.NormalViewSpace, GpuProgramParameters.GpuConstantType.Float3);
            if (vsOutNormal == null)
                return false;

            //Resolve input pixel shader normal.
            psInNormal = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, vsOutNormal.Index, vsOutNormal.Content, GpuProgramParameters.GpuConstantType.Float3);
            if (psInNormal == null)
                return false;

            var inputParams = psMain.InputParameters;
            var localParams = psMain.LocalParameters;

            psDiffuse = Function.GetParameterByContent(inputParams, Parameter.ContentType.ColorDiffuse, GpuProgramParameters.GpuConstantType.Float4);
            if (psDiffuse == null)
            {
                psDiffuse = Function.GetParameterByContent(localParams, Parameter.ContentType.ColorDiffuse, GpuProgramParameters.GpuConstantType.Float4);
                if (psDiffuse == null)
                    return false;
            }

            psOutDiffuse = psMain.ResolveOutputParameter(Parameter.SemanticType.Color, 0, Parameter.ContentType.ColorDiffuse, GpuProgramParameters.GpuConstantType.Float4);
            if (psOutDiffuse == null)
                return false;

            psTempDiffuseColor = psMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0, "lPerPixelDiffuse", GpuProgramParameters.GpuConstantType.Float4);
            if (psTempDiffuseColor == null)
                return false;

            if (specularEnable)
            {
                psSpecular = Function.GetParameterByContent(inputParams, Parameter.ContentType.ColorSpecular, GpuProgramParameters.GpuConstantType.Float4);
                if (psSpecular == null)
                {
                    psSpecular = Function.GetParameterByContent(localParams, Parameter.ContentType.ColorSpecular, GpuProgramParameters.GpuConstantType.Float4);
                    if (psSpecular == null)
                        return false;
                }

                psTempSpecularColor = psMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0, "lPerPixelSpecular", GpuProgramParameters.GpuConstantType.Float4);
                if (psTempSpecularColor == null)
                    return false;

                vsInPosition = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0, Parameter.ContentType.PositionObjectSpace, GpuProgramParameters.GpuConstantType.Float4);
                if (vsInPosition == null)
                    return false;

                vsOutViewPos = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1, Parameter.ContentType.PositionViewSpace, GpuProgramParameters.GpuConstantType.Float3);
                if (vsOutViewPos == null)
                    return false;

                psInViewPos = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, vsOutViewPos.Index, vsOutViewPos.Content, GpuProgramParameters.GpuConstantType.Float3);
                if (psInViewPos == null)
                    return false;

                worldViewMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.WorldViewMatrix, 0);
                if (worldViewMatrix == null)
                    return false;
            }

            return true;
        }
        protected bool ResolvePerLightParameters(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            //Resolve per light parameters
            for (int i = 0; i < lightParamsList.Count; i++)
            {
                switch (lightParamsList[i].Type)
                {
                    case LightType.Directional:
                        lightParamsList[i].Direction = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Lights, "light_direction_view_space");
                        if (lightParamsList[i].Direction == null)
                            return false;
                        break;
                    case LightType.Point:
                        worldViewMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.WorldViewMatrix, 0);
                        if (worldViewMatrix == null)
                            return false;

                        vsInPosition = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0, Parameter.ContentType.PositionObjectSpace, GpuProgramParameters.GpuConstantType.Float4);
                        if (vsInPosition == null)
                            return false;

                        lightParamsList[i].Position = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Lights, "light_position_view_space");
                        if (lightParamsList[i].Position == null)
                            return false;

                        lightParamsList[i].AttenuatParams = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Lights, "light_attenuation");
                        if (lightParamsList[i].AttenuatParams == null)
                            return false;

                        if (vsOutViewPos == null)
                        {
                            vsOutViewPos = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1, Parameter.ContentType.PositionViewSpace, GpuProgramParameters.GpuConstantType.Float3);
                            if (vsOutViewPos == null)
                                return false;

                            psInViewPos = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, vsOutViewPos.Index, vsOutViewPos.Content, GpuProgramParameters.GpuConstantType.Float3);
                            if (psInViewPos == null)
                                return false;
                        }
                        break;
                    case LightType.Spotlight:
                        worldViewMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.WorldViewMatrix, 0);
                        if (worldViewMatrix == null)
                            return false;

                        vsInPosition = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0, Parameter.ContentType.PositionObjectSpace, GpuProgramParameters.GpuConstantType.Float4);
                        if (vsInPosition == null)
                            return false;

                        lightParamsList[i].Position = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Lights, "light_position_view_space");
                        if (lightParamsList[i].Position == null)
                            return false;

                        lightParamsList[i].Direction = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Lights, "light_direction_view_space");
                        if (lightParamsList[i].Direction == null)
                            return false;

                        lightParamsList[i].AttenuatParams = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Lights, "light_attenuation");
                        if (lightParamsList[i].AttenuatParams == null)
                            return false;

                        lightParamsList[i].SpotParams = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float3, -1, GpuProgramParameters.GpuParamVariability.Lights, "spotlight_params");
                        if (lightParamsList[i].SpotParams == null)
                            return false;

                        if (vsOutViewPos == null)
                        {
                            vsOutViewPos = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1, Parameter.ContentType.PositionViewSpace, GpuProgramParameters.GpuConstantType.Float3);
                            if (vsOutViewPos == null)
                                return false;

                            psInViewPos = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                vsOutViewPos.Index,
                                vsOutViewPos.Content,
                                 GpuProgramParameters.GpuConstantType.Float3);

                            if (psInViewPos == null)
                                return false;
                        }
                        break;
                }

                //Resolve diffuse color
                if ((trackVertexColorType & TrackVertexColor.Diffuse) == 0)
                {
                    lightParamsList[i].DiffuseColor = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Lights | GpuProgramParameters.GpuParamVariability.Global, "derived_light_diffuse");
                    if (lightParamsList[i].DiffuseColor == null)
                        return false;
                }
                else
                {
                    lightParamsList[i].DiffuseColor = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Lights, "light_diffuse");
                    if (lightParamsList[i].DiffuseColor == null)
                        return false;
                }

                if (specularEnable)
                {
                    //Resolve specular color
                    if ((trackVertexColorType & TrackVertexColor.Specular) == 0)
                    {
                        lightParamsList[i].SpecularColor = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Lights | GpuProgramParameters.GpuParamVariability.Global, "derived_light_specular");
                        if (lightParamsList[i].SpecularColor == null)
                            return false;
                    }
                    else
                    {
                        lightParamsList[i].SpecularColor = psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1, GpuProgramParameters.GpuParamVariability.Lights, "light_specular");
                        if (lightParamsList[i].SpecularColor == null)
                            return false;
                    }
                }
            }
            return true;
        }

        internal override bool ResolveDependencies(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;

            vsProgram.AddDependency(FFPRenderState.FFPLibCommon);
            vsProgram.AddDependency(SGXLibPerPixelLighting);

            psProgram.AddDependency(FFPRenderState.FFPLibCommon);
            psProgram.AddDependency(SGXLibPerPixelLighting);

            return true;
            
        }
        internal override bool AddFunctionInvocations(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            int internalCounter = 0;
            //Add the global illumination functions.
            if (AddVSInvocation(vsMain, (int)FFPRenderState.FFPVertexShaderStage.VSLighting, ref internalCounter) == false)
                return false;

            internalCounter = 0;

            //Add the global illumination fuctnions
            if (AddPSGlobalIlluminationInvocation(psMain, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1, ref internalCounter) == false)
                return false;

            //Add per light functions
            for (int i = 0; i < lightParamsList.Count; i++)
            {
                if (AddPSIlluminationInvocation(lightParamsList[i], psMain, -1, ref internalCounter) == false)
                    return false;
            }

            //Assign back temporary variables to the ps diffuse and specular components.
            if (AddPSFinalAssignmentInvocation(psMain, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1, ref internalCounter) == false)
                return false;

            return true;
        }

        private bool AddPSGlobalIlluminationInvocation(Function psMain, int groupOrder, ref int internalCounter)
        {
            FunctionInvocation curFuncInvocation = null;
            if ((trackVertexColorType & TrackVertexColor.Ambient) == 0 && (trackVertexColorType & TrackVertexColor.Emissive) == 0)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++);
                curFuncInvocation.PushOperand(derivedSceneColor, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }
            else
            {
                if ((trackVertexColorType & TrackVertexColor.Ambient) != 0)
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncModulate, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(lightAmbientColor, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(psDiffuse, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                }
                else
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(derivedAmbientLightColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                    curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                    psMain.AddAtomInstance(curFuncInvocation);
                }

                if ((trackVertexColorType & TrackVertexColor.Emissive) != 0)
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAdd, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(psDiffuse, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                }
                else
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAdd, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(surfaceEmissiveColor, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                }
            }

            if (specularEnable)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++);
                curFuncInvocation.PushOperand(psSpecular, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psTempSpecularColor, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }

            return true;
        }
        protected bool AddVSInvocation(Function vsMain, int groupOrder, ref int internalCounter)
        {
            FunctionInvocation curFuncInvocation = null;

            //transform normal in view space
            curFuncInvocation = new FunctionInvocation(SGXFuncTransformNormal, groupOrder, internalCounter++);
            curFuncInvocation.PushOperand(worldViewITMatrix, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(vsInNormal, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(vsOutNormal, Operand.OpSemantic.Out);
            vsMain.AddAtomInstance(curFuncInvocation);

            //Transform view space position if need to
            if (vsOutViewPos != null)
            {
                curFuncInvocation = new FunctionInvocation(SGXFuncTransformPosition, groupOrder, internalCounter++);
                curFuncInvocation.PushOperand(worldViewMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(vsInPosition, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(vsOutViewPos, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }

            return true;
        }
        internal bool AddPSIlluminationInvocation(LightParams curLightParams, Function psMain, int groupOrder, ref int internalCounter)
        {
            FunctionInvocation curFuncInvocation = null;

            //merge diffuse color with vertex color if need to
            if ((trackVertexColorType & TrackVertexColor.Diffuse) != 0)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncModulate, groupOrder, internalCounter++);
                curFuncInvocation.PushOperand(psDiffuse, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.Out, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                psMain.AddAtomInstance(curFuncInvocation);
            }
            //merge specular color with vertex color if need to
            if (specularEnable && (trackVertexColorType & TrackVertexColor.Specular) != 0)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncModulate, groupOrder, internalCounter++);
                curFuncInvocation.PushOperand(psDiffuse, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                curFuncInvocation.PushOperand(curLightParams.SpecularColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                curFuncInvocation.PushOperand(curLightParams.SpecularColor, Operand.OpSemantic.Out, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                psMain.AddAtomInstance(curFuncInvocation);
            }

            switch (curLightParams.Type)
            {
                case LightType.Directional:
                    if (specularEnable)
		            {				
			            curFuncInvocation = new FunctionInvocation(SGXFuncLightDirectionalDiffuseSpecular, groupOrder, internalCounter++); 
			            curFuncInvocation.PushOperand(psInNormal, Operand.OpSemantic.In);
			            curFuncInvocation.PushOperand(psInViewPos, Operand.OpSemantic.In);			
			            curFuncInvocation.PushOperand(curLightParams.Direction, Operand.OpSemantic.In, (int)(Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));
			            curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));			
			            curFuncInvocation.PushOperand(curLightParams.SpecularColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));			
			            curFuncInvocation.PushOperand(surfaceShininess, Operand.OpSemantic.In);
			            curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));	
			            curFuncInvocation.PushOperand(psTempSpecularColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));
			            curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out,(int) (Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));	
			            curFuncInvocation.PushOperand(psTempSpecularColor, Operand.OpSemantic.Out,(int) (Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));	
			            psMain.AddAtomInstance(curFuncInvocation);
                    }

		else
		{
			curFuncInvocation = new FunctionInvocation(SGXFuncLightDirectionalDiffuse, groupOrder, internalCounter++); 			
			curFuncInvocation.PushOperand(psInNormal, Operand.OpSemantic.In);
			curFuncInvocation.PushOperand(curLightParams.Direction, Operand.OpSemantic.In, (int)(Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));
			curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In,(int)(Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));					
			curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.In,(int)(Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));	
			curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out, (int)(Operand.OpMask.X |  Operand.OpMask.Y | Operand.OpMask.Z));	
			psMain.AddAtomInstance(curFuncInvocation);	
		}	
                    break;
                case LightType.Point:
                    if (specularEnable)
                    {
                        curFuncInvocation = new FunctionInvocation(SGXFuncLightPointDiffuseSpecular, groupOrder, internalCounter++);
                        curFuncInvocation.PushOperand(psInNormal, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(psInViewPos, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.Position, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.AttenuatParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.SpecularColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(surfaceShininess, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempSpecularColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempSpecularColor, Operand.OpSemantic.Out, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        psMain.AddAtomInstance(curFuncInvocation);
                    }
                    else
                    {
                        curFuncInvocation = new FunctionInvocation(SGXFuncLightPointDiffuse, groupOrder, internalCounter++);
                        curFuncInvocation.PushOperand(psInNormal, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(psInViewPos, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.Position, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.AttenuatParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        psMain.AddAtomInstance(curFuncInvocation);
                    }
                    break;
                case LightType.Spotlight:
                    if (specularEnable)
                    {
                        curFuncInvocation = new FunctionInvocation(SGXFuncLightSpotDiffuseSpecular, groupOrder, internalCounter++);
                        curFuncInvocation.PushOperand(psInNormal, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(psInViewPos, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.Position, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.Direction, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.AttenuatParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.SpotParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.SpecularColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(surfaceShininess, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempSpecularColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempSpecularColor, Operand.OpSemantic.Out, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));


                    }
                    else
                    {
                        curFuncInvocation = new FunctionInvocation(SGXFuncLightSpotDiffuse, groupOrder, internalCounter++);
                        curFuncInvocation.PushOperand(psInNormal, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(psInViewPos, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.Position, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.Direction, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.AttenuatParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.SpotParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.In, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.Out, (int)(Operand.OpMask.X | Operand.OpMask.Y | Operand.OpMask.Z));
                        psMain.AddAtomInstance(curFuncInvocation);
                    }
                    break;
            }

            return true;
        }
        protected bool AddPSFinalAssignmentInvocation(Function psMain, int groupOrder, ref int internalCounter)
        {
            FunctionInvocation curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1, internalCounter++);
            curFuncInvocation.PushOperand(psTempDiffuseColor, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(psDiffuse, Operand.OpSemantic.Out);
            psMain.AddAtomInstance(curFuncInvocation);

            curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1, internalCounter++);
            curFuncInvocation.PushOperand(psDiffuse, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(psOutDiffuse, Operand.OpSemantic.Out);
            psMain.AddAtomInstance(curFuncInvocation);

            if (specularEnable)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1, internalCounter++);
                curFuncInvocation.PushOperand(psTempSpecularColor, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(psSpecular, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }
            return true;
        }
        TrackVertexColor TrackVertexColorType
        {
            get { return trackVertexColorType; }
            set { trackVertexColorType = value; }
        }
        public bool SpecularEnable
        {
            get { return specularEnable; }
            set { specularEnable = value; }
        }
        public override string Type
        {
            get
            {
                return PerPixelLighting.SGXType;
            }
        }
        public override int ExecutionOrder
        {
            get
            {
                return (int)FFPRenderState.FFPShaderStage.Lighting;
            }
        }
    }
}
