using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
    public enum NormalMapSpace
    {
        Tangent,
        Object
    }

    public class LightParams
    {
        public LightType Type;
        public UniformParameter Position;
        public Parameter VSOutToLightDir;
        public Parameter PSInToLightDir;
        public UniformParameter Direction;
        public Parameter VSOutDirection;
        public Parameter PSInDirection;
        public UniformParameter AttenuatParams;
        public UniformParameter SpotParams;
        public UniformParameter DiffuseColor;
        public UniformParameter SpecularColor;
    }

    public class NormalMapLighting : SubRenderState
    {
        #region Structs/Enums

        #endregion

        #region Fields

        public static string SGXType = "SGX_NormalMapLighting";
        private static string SGXLibNormalMapLighting = "SGXLib_NormalMapLighting";
        private static string SGXFuncConstructTbnMatrix = "SGX_ConstructTBNMatrix";
        private static string SGXFuncTranformNormal = "SGX_TransformNormal";
        private static string SGXFuncTransformPosition = "SGX_TransformPosition";
        private static string SGXFuncFetchNormal = "SGX_FetchNormal";
        private static string SGXFuncLightDirectionalDiffuse = "SGX_Light_Directional_Diffuse";
        private static string SGXFuncLightDirectionDiffuseSpecular = "SGX_Light_Directional_DiffuseSpecular";
        private static string SGXFuncLightPointDiffuse = "SGX_Light_Point_Diffuse";
        private static string SGXFuncLightPointDiffuseSpecular = "SGX_Light_Point_DiffuseSpecular";
        private static string SGXFuncLightSpotDiffuse = "SGX_Light_Spot_Diffuse";
        private static string SGXFuncLightSpotDiffuseSpecular = "SGX_Light_Spot_DiffuseSpecular";

        private string normalMapTextureName;
        private TrackVertexColor trackVertexColorType;
        private bool specularEnabled;
        private readonly List<LightParams> lightParamsList = new List<LightParams>();
        private int normalMapSamplerIndex;
        private int vsTexCoordSetIndex;
        private FilterOptions normalMapMinFilter;
        private FilterOptions normalMapMagFilter;
        private FilterOptions normalMapMipfilter;
        private int normalMapAnisotropy;
        private Real normalMapMipBias;
        private NormalMapSpace normalMapSpace;
        private UniformParameter worldMatrix, worldInvRotMatrix, camPosWorldSpace;
        private Parameter vsInPosition, vsWorldPosition, vsOutView;
        private Parameter psInView, vsInNormal, vsInTangent;
        private Parameter vsTBNMatrix;
        private Parameter vsLocalDir;
        private UniformParameter normalMapSampler;
        private Parameter psNormal, vsInTexcoord, vsOutTexcoord, psInTexcoord;
        private Parameter psTempDiffuseColor, psTempSpecularColor, psDiffuse, psSpecular, psOutDiffuse, psOutSpecular;
        private UniformParameter derivedSceneColor, lightAmbientColor, derivedAmbientLightColor;
        private UniformParameter surfaceAmbientColor, surfaceDiffuseColor, surfaceSpecularColor, surfaceEmissiveColor;
        private UniformParameter surfaceShininess;
        private static Light blankLight; //Shared blank light 

        #endregion

        #region C'Tor

        public NormalMapLighting()
        {
            this.trackVertexColorType = TrackVertexColor.None;
            this.normalMapSamplerIndex = 0;
            this.vsTexCoordSetIndex = 0;
            this.specularEnabled = false;
            this.normalMapSpace = NormalMapSpace.Tangent;
            this.normalMapMinFilter = FilterOptions.Linear;
            this.normalMapMagFilter = FilterOptions.Linear;
            this.normalMapMipfilter = FilterOptions.Point;
            this.normalMapAnisotropy = 1;
            this.normalMapMipBias = -1.0;

            blankLight.Diffuse = ColorEx.Black;
            blankLight.Specular = ColorEx.Black;
            blankLight.SetAttenuation(0, 1, 0, 0);
        }

        #endregion

        #region Public Methods

        public void SetNormalMapFiltering(FilterOptions minFilter, FilterOptions magFilter, FilterOptions mipFilter)
        {
            this.normalMapMinFilter = minFilter;
            this.normalMapMagFilter = magFilter;
            this.normalMapMipfilter = mipFilter;
        }

        public void GetNormalMapFiltering(out FilterOptions minFilter, out FilterOptions magFilter,
                                           out FilterOptions mipFilter)
        {
            minFilter = this.normalMapMinFilter;
            magFilter = this.normalMapMagFilter;
            mipFilter = this.normalMapMipfilter;
        }

        #endregion

        #region Overriden Methods

        public override void GetLightCount(out int[] lightCount)
        {
            base.GetLightCount(out lightCount);
        }

        public override void SetLightCount(int[] currLightCount)
        {
            base.SetLightCount(currLightCount);
        }

        public override void UpdateGpuProgramsParams(IRenderable rend, Pass pass, AutoParamDataSource source,
                                                      Core.Collections.LightList lightList)
        {
            if (this.lightParamsList.Count == 0)
            {
                return;
            }

            var curLightType = LightType.Directional;
            int curSearchLightIndex = 0;
            Matrix4 matWorld = source.WorldMatrix;
            Matrix3 matWorldInvRotation;

            var vRow0 = new Vector3(matWorld[0, 0], matWorld[0, 1], matWorld[0, 2]);
            var vRow1 = new Vector3(matWorld[1, 0], matWorld[1, 1], matWorld[1, 2]);
            var vRow2 = new Vector3(matWorld[2, 0], matWorld[2, 1], matWorld[2, 2]);

            vRow0.Normalize();
            vRow1.Normalize();
            vRow2.Normalize();

            matWorldInvRotation = new Matrix3(vRow0, vRow1, vRow2);
            //update inverse rotation parameter
            if (this.worldInvRotMatrix != null)
            {
                this.worldInvRotMatrix.SetGpuParameter(matWorldInvRotation);
            }

            //update per light parameters
            for (int i = 0; i < this.lightParamsList.Count; i++)
            {
                LightParams curParams = this.lightParamsList[i];

                if (curLightType != curParams.Type)
                {
                    curLightType = curParams.Type;
                    curSearchLightIndex = 0;
                }

                Light srcLight = null;
                Vector4 vParameter;
                ColorEx color;

                //Search a matching light from the current sorted lights of the given renderable
                for (int j = 0; j < lightList.Count; j++)
                {
                    if (lightList[j].Type == curLightType)
                    {
                        srcLight = lightList[j];
                        curSearchLightIndex = j + 1;
                        break;
                    }
                }

                //No matching light found -> use a blank dummy light for parameter update
                if (srcLight == null)
                {
                    srcLight = blankLight;
                }

                switch (curParams.Type)
                {
                    case LightType.Directional:
                        {
                            Vector3 vec3;

                            //Update light direction (object space)
                            vec3 = matWorldInvRotation * srcLight.DerivedDirection;
                            vec3.Normalize();

                            vParameter.x = -vec3.x;
                            vParameter.y = -vec3.y;
                            vParameter.z = -vec3.z;
                            vParameter.w = 0.0;
                            curParams.Direction.SetGpuParameter(vParameter);
                        }
                        break;
                    case LightType.Point:
                        //update light position (world space)
                        vParameter = srcLight.GetAs4DVector(true);
                        curParams.Position.SetGpuParameter(vParameter);

                        //Update light attenuation parameters.
                        vParameter.x = srcLight.AttenuationRange;
                        vParameter.y = srcLight.AttenuationConstant;
                        vParameter.z = srcLight.AttenuationLinear;
                        vParameter.w = srcLight.AttenuationQuadratic;
                        curParams.AttenuatParams.SetGpuParameter(vParameter);

                        break;
                    case LightType.Spotlight:
                        {
                            Vector3 vec3;

                            //Update light position (world space)
                            vParameter = srcLight.GetAs4DVector(true);
                            curParams.Position.SetGpuParameter(vParameter);

                            //Update light direction (object space)
                            vec3 = matWorldInvRotation * srcLight.DerivedDirection;
                            vec3.Normalize();

                            vParameter.x = -vec3.x;
                            vParameter.y = -vec3.y;
                            vParameter.z = -vec3.z;
                            vParameter.w = 0.0;

                            curParams.Direction.SetGpuParameter(vParameter);

                            //Update light attenuation parameters.
                            vParameter.x = srcLight.AttenuationRange;
                            vParameter.y = srcLight.AttenuationConstant;
                            vParameter.z = srcLight.AttenuationLinear;
                            vParameter.w = srcLight.AttenuationQuadratic;
                            curParams.AttenuatParams.SetGpuParameter(vParameter);

                            //Update spotlight parameters
                            Real phi = System.Math.Cos((double)srcLight.SpotlightOuterAngle * 0.5f);
                            Real theta = System.Math.Cos((double)srcLight.SpotlightInnerAngle * 0.5f);

                            vec3.x = theta;
                            vec3.y = phi;
                            vec3.z = srcLight.SpotlightFalloff;

                            curParams.SpotParams.SetGpuParameter(vec3);
                        }
                        break;
                }

                //Update diffuse color
                if ((this.trackVertexColorType & TrackVertexColor.Diffuse) == 0)
                {
                    color = srcLight.Diffuse * pass.Diffuse;
                    curParams.DiffuseColor.SetGpuParameter(color);
                }
                else
                {
                    color = srcLight.Diffuse;
                    curParams.DiffuseColor.SetGpuParameter(color);
                }

                //Update specular color if need to
                if (this.specularEnabled)
                {
                    //Update diffuse color
                    if ((this.trackVertexColorType & TrackVertexColor.Diffuse) == 0)
                    {
                        color = srcLight.Specular * pass.Specular;
                        curParams.SpecularColor.SetGpuParameter(color);
                    }
                    else
                    {
                        color = srcLight.Specular;
                        curParams.SpecularColor.SetGpuParameter(color);
                    }
                }
            }
        }

        protected override bool ResolveParameters(ProgramSet programSet)
        {
            if (ResolveGlobalParameters(programSet) == false)
            {
                return false;
            }
            if (ResolvePerLightParameters(programSet) == false)
            {
                return false;
            }

            return true;
        }

        private bool ResolvePerLightParameters(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            for (int i = 0; i < this.lightParamsList.Count; i++)
            {
                switch (this.lightParamsList[i].Type)
                {
                    case LightType.Directional:
                        this.lightParamsList[i].Direction =
                            vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.PerObject,
                                                        "light_direction_obj_space");
                        if (this.lightParamsList[i].Direction == null)
                        {
                            return false;
                        }


                        Parameter.ContentType pctToUse;

                        if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
                        {
                            switch (i)
                            {
                                case 0:
                                    pctToUse = Parameter.ContentType.LightDirectionTangentSpace0;
                                    break;
                                case 1:
                                    pctToUse = Parameter.ContentType.LightDirectionTangentSpace1;
                                    break;
                                case 2:
                                    pctToUse = Parameter.ContentType.LightDirectionTangentSpace2;
                                    break;
                                case 3:
                                    pctToUse = Parameter.ContentType.LightDirectionTangentSpace3;
                                    break;
                                case 4:
                                    pctToUse = Parameter.ContentType.LightDirectionTangentSpace4;
                                    break;
                                case 5:
                                    pctToUse = Parameter.ContentType.LightDirectionTangentSpace5;
                                    break;
                                case 6:
                                    pctToUse = Parameter.ContentType.LightDirectionTangentSpace6;
                                    break;
                                default:
                                    throw new AxiomException("Index out of range");
                            }
                            this.lightParamsList[i].VSOutDirection =
                                vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1, pctToUse,
                                                               GpuProgramParameters.GpuConstantType.Float3);
                        }
                        else if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Object)
                        {
                            switch (i)
                            {
                                case 0:
                                    pctToUse = Parameter.ContentType.LightDirectionTangentSpace0;
                                    break;
                                case 1:
                                    pctToUse = Parameter.ContentType.LightDirectionTangentSpace1;
                                    break;
                                case 2:
                                    pctToUse = Parameter.ContentType.LightDirectionObjectSpace2;
                                    break;
                                case 3:
                                    pctToUse = Parameter.ContentType.LightDirectionObjectSpace3;
                                    break;
                                case 4:
                                    pctToUse = Parameter.ContentType.LightDirectionObjectSpace4;
                                    break;
                                case 5:
                                    pctToUse = Parameter.ContentType.LightDirectionObjectSpace5;
                                    break;
                                case 6:
                                    pctToUse = Parameter.ContentType.LightDirectionObjectSpace6;
                                    break;
                                default:
                                    throw new AxiomException("Index out of range");
                            }
                            this.lightParamsList[i].VSOutToLightDir =
                                vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1, pctToUse,
                                                               GpuProgramParameters.GpuConstantType.Float3);
                        }
                        if (this.lightParamsList[i].VSOutToLightDir == null)
                        {
                            return false;
                        }

                        this.lightParamsList[i].PSInDirection =
                            psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                                          this.lightParamsList[i].VSOutToLightDir.Index,
                                                          this.lightParamsList[i].VSOutToLightDir.Content,
                                                          this.lightParamsList[i].VSOutToLightDir.Type);
                        if (this.lightParamsList[i].PSInDirection == null)
                        {
                            return false;
                        }

                        break;
                    case LightType.Point:
                        this.vsInPosition = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0,
                                                                          Parameter.ContentType.PositionObjectSpace,
                                                                          GpuProgramParameters.GpuConstantType.Float4);
                        if (this.vsInPosition == null)
                        {
                            return false;
                        }

                        this.lightParamsList[i].Position =
                            vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights |
                                                        GpuProgramParameters.GpuParamVariability.PerObject,
                                                        "light_position_world_space");
                        if (this.lightParamsList[i].Position == null)
                        {
                            return false;
                        }

                        if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
                        {
                            this.lightParamsList[i].VSOutToLightDir =
                                vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum(RTShaderSystem.NormalMapSpace.Tangent, i),
                                                               GpuProgramParameters.GpuConstantType.Float3);
                        }
                        else if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Object)
                        {
                            this.lightParamsList[i].VSOutToLightDir =
                                vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum(RTShaderSystem.NormalMapSpace.Object, i),
                                                               GpuProgramParameters.GpuConstantType.Float3);
                        }
                        if (this.lightParamsList[i].VSOutToLightDir == null)
                        {
                            return false;
                        }

                        this.lightParamsList[i].PSInToLightDir =
                            psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                                          this.lightParamsList[i].VSOutToLightDir.Index,
                                                          this.lightParamsList[i].VSOutToLightDir.Content,
                                                          this.lightParamsList[i].VSOutToLightDir.Type);
                        if (this.lightParamsList[i].PSInToLightDir == null)
                        {
                            return false;
                        }

                        this.lightParamsList[i].AttenuatParams =
                            psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights,
                                                        "light_attenuation");
                        if (this.lightParamsList[i].AttenuatParams == null)
                        {
                            return false;
                        }

                        //Resolve local dir
                        if (this.vsLocalDir == null)
                        {
                            this.vsLocalDir = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0,
                                                                            "lNormalMapTempDir",
                                                                            GpuProgramParameters.GpuConstantType.Float3);
                            if (this.vsLocalDir == null)
                            {
                                return false;
                            }
                        }

                        //Resolve world position
                        if (this.vsWorldPosition == null)
                        {
                            this.vsWorldPosition = vsMain.ResolveLocalParameter(Parameter.SemanticType.Position, 0,
                                                                                 Parameter.ContentType.PositionWorldSpace,
                                                                                 GpuProgramParameters.GpuConstantType.Float3);
                            if (this.vsWorldPosition == null)
                            {
                                return false;
                            }
                        }

                        //resolve world matrix
                        if (this.worldMatrix == null)
                        {
                            this.worldMatrix =
                                vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.WorldMatrix, 0);
                            if (this.worldMatrix == null)
                            {
                                return false;
                            }
                        }
                        //resolve inverse world rotation matrix
                        if (this.worldInvRotMatrix == null)
                        {
                            this.worldInvRotMatrix =
                                vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Matrix_4X4, -1,
                                                            GpuProgramParameters.GpuParamVariability.PerObject,
                                                            "inv_world_rotation_matrix");
                            if (this.worldInvRotMatrix == null)
                            {
                                return false;
                            }
                        }
                        break;
                    case LightType.Spotlight:
                        this.vsInPosition = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0,
                                                                          Parameter.ContentType.PositionObjectSpace,
                                                                          GpuProgramParameters.GpuConstantType.Float4);
                        if (this.vsInPosition == null)
                        {
                            return false;
                        }

                        this.lightParamsList[i].Position =
                            vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights |
                                                        GpuProgramParameters.GpuParamVariability.PerObject,
                                                        "light_position_world_space");
                        if (this.lightParamsList[i].Position == null)
                        {
                            return false;
                        }

                        if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
                        {
                            this.lightParamsList[i].VSOutToLightDir =
                                vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum(RTShaderSystem.NormalMapSpace.Tangent, i),
                                                               GpuProgramParameters.GpuConstantType.Float3);
                        }
                        if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Object)
                        {
                            this.lightParamsList[i].VSOutToLightDir =
                                vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum(RTShaderSystem.NormalMapSpace.Object, i),
                                                               GpuProgramParameters.GpuConstantType.Float3);
                        }
                        if (this.lightParamsList[i].VSOutToLightDir == null)
                        {
                            return false;
                        }

                        this.lightParamsList[i].PSInToLightDir =
                            psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                                          this.lightParamsList[i].VSOutToLightDir.Index,
                                                          this.lightParamsList[i].VSOutToLightDir.Content,
                                                          this.lightParamsList[i].VSOutToLightDir.Type);
                        if (this.lightParamsList[i].PSInToLightDir == null)
                        {
                            return false;
                        }

                        this.lightParamsList[i].Direction =
                            vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights |
                                                        GpuProgramParameters.GpuParamVariability.PerObject,
                                                        "light_direction_obj_space");
                        if (this.lightParamsList[i].Direction == null)
                        {
                            return false;
                        }
                        if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
                        {
                            this.lightParamsList[i].VSOutDirection =
                                vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum(RTShaderSystem.NormalMapSpace.Tangent, i),
                                                               GpuProgramParameters.GpuConstantType.Float3);
                            if (this.lightParamsList[i].VSOutDirection == null)
                            {
                                return false;
                            }
                        }
                        else if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Object)
                        {
                            this.lightParamsList[i].VSOutDirection =
                                vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum(RTShaderSystem.NormalMapSpace.Object, i),
                                                               GpuProgramParameters.GpuConstantType.Float3);
                            if (this.lightParamsList[i].VSOutDirection == null)
                            {
                                return false;
                            }
                        }
                        if (this.lightParamsList[i].VSOutDirection == null)
                        {
                            return false;
                        }

                        this.lightParamsList[i].PSInDirection =
                            psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates,
                                                          this.lightParamsList[i].VSOutDirection.Index,
                                                          this.lightParamsList[i].VSOutDirection.Content,
                                                          this.lightParamsList[i].VSOutDirection.Type);
                        if (this.lightParamsList[i].PSInDirection == null)
                        {
                            return false;
                        }

                        this.lightParamsList[i].AttenuatParams =
                            psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights,
                                                        "light_attenuation");
                        if (this.lightParamsList[i].AttenuatParams == null)
                        {
                            return false;
                        }

                        this.lightParamsList[i].SpotParams =
                            psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float3, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights,
                                                        "spotlight_params");
                        if (this.lightParamsList[i].SpotParams == null)
                        {
                            return false;
                        }

                        //Resolve local dir
                        if (this.vsLocalDir == null)
                        {
                            this.vsLocalDir = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0,
                                                                            "lNormalMapTempDir",
                                                                            GpuProgramParameters.GpuConstantType.Float3);
                            if (this.vsLocalDir == null)
                            {
                                return false;
                            }
                        }

                        //resolve world postion
                        if (this.vsWorldPosition == null)
                        {
                            this.vsWorldPosition = vsMain.ResolveLocalParameter(Parameter.SemanticType.Position, 0,
                                                                                 Parameter.ContentType.PositionWorldSpace,
                                                                                 GpuProgramParameters.GpuConstantType.Float3);
                            if (this.vsWorldPosition == null)
                            {
                                return false;
                            }
                        }
                        //resolve world matrix
                        if (this.worldMatrix == null)
                        {
                            this.worldMatrix =
                                vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.WorldMatrix, 0);
                            if (this.worldMatrix == null)
                            {
                                return false;
                            }
                        }
                        //resovle inverse world rotation matrix
                        if (this.worldInvRotMatrix == null)
                        {
                            this.worldInvRotMatrix =
                                vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Matrix_4X4, -1,
                                                            GpuProgramParameters.GpuParamVariability.PerObject,
                                                            "inv_world_rotation_matrix");
                            if (this.worldInvRotMatrix == null)
                            {
                                return false;
                            }
                        }
                        break;
                }

                //resolve diffuse color
                if ((this.trackVertexColorType & TrackVertexColor.Diffuse) == 0)
                {
                    this.lightParamsList[i].DiffuseColor =
                        psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                    GpuProgramParameters.GpuParamVariability.Lights,
                                                    "derived_light_diffuse");
                    if (this.lightParamsList[i].DiffuseColor == null)
                    {
                        return false;
                    }
                }
                else
                {
                    this.lightParamsList[i].DiffuseColor =
                        psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                    GpuProgramParameters.GpuParamVariability.Lights, "light_diffuse");
                    if (this.lightParamsList[i].DiffuseColor == null)
                    {
                        return false;
                    }
                }

                if (this.specularEnabled)
                {
                    //resolve specular color
                    if ((this.trackVertexColorType & TrackVertexColor.Specular) == 0)
                    {
                        this.lightParamsList[i].SpecularColor =
                            psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights,
                                                        "derived_light_specular");
                        if (this.lightParamsList[i].SpecularColor == null)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        this.lightParamsList[i].SpecularColor =
                            psProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights,
                                                        "light_specular");
                        if (this.lightParamsList[i].SpecularColor == null)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private Parameter.ContentType IntToEnum(NormalMapSpace nms, int index)
        {
            if (nms == RTShaderSystem.NormalMapSpace.Tangent)
            {
                switch (index)
                {
                    case 0:
                        return Parameter.ContentType.PostoLightTangentSpace0;
                    case 1:
                        return Parameter.ContentType.PostoLightTangentSpace1;
                    case 2:
                        return Parameter.ContentType.PostoLightTangentSpace2;
                    case 3:
                        return Parameter.ContentType.PostoLightTangentSpace3;
                    case 4:
                        return Parameter.ContentType.PostoLightTangentSpace4;
                    case 5:
                        return Parameter.ContentType.PostoLightTangentSpace5;
                    case 6:
                        return Parameter.ContentType.PostoLightTangentSpace6;
                    case 7:
                        return Parameter.ContentType.PostoLightTangentSpace7;
                    default:
                        throw new AxiomException("Index out of range");
                }
            }
            else
            {
                switch (index)
                {
                    case 0:
                        return Parameter.ContentType.PostoLightObjectSpace0;
                    case 1:
                        return Parameter.ContentType.PostoLightObjectSpace1;
                    case 2:
                        return Parameter.ContentType.PostoLightObjectSpace2;
                    case 3:
                        return Parameter.ContentType.PostoLightObjectSpace3;
                    case 4:
                        return Parameter.ContentType.PostoLightObjectSpace4;
                    case 5:
                        return Parameter.ContentType.PostoLightObjectSpace5;
                    case 6:
                        return Parameter.ContentType.PostoLightObjectSpace6;
                    case 7:
                        return Parameter.ContentType.PostoLightObjectSpace7;
                    default:
                        throw new AxiomException("Index out of range");
                }
            }
        }

        private bool ResolveGlobalParameters(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            //Resolve normal map texture sampler parameter
            this.normalMapSampler = psProgram.ResolveParameter(
                Axiom.Graphics.GpuProgramParameters.GpuConstantType.Sampler2D, this.normalMapSamplerIndex,
                GpuProgramParameters.GpuParamVariability.PerObject, "gNormalMapSampler");
            if (this.normalMapSampler == null)
            {
                return false;
            }

            //Get surface ambient color if need to
            if ((this.trackVertexColorType & TrackVertexColor.Ambient) == 0)
            {
                this.derivedAmbientLightColor =
                    psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.DerivedAmbientLightColor, 0);
                if (this.derivedAmbientLightColor == null)
                {
                    return false;
                }
            }
            else
            {
                this.lightAmbientColor =
                    psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.AmbientLightColor, 0);
                if (this.lightAmbientColor == null)
                {
                    return false;
                }

                this.surfaceAmbientColor =
                    psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.SurfaceAmbientColor, 0, 0);
                if (this.surfaceAmbientColor == null)
                {
                    return false;
                }
            }

            //Get surface diffuse color if need to
            if ((this.trackVertexColorType & TrackVertexColor.Diffuse) == 0)
            {
                this.surfaceDiffuseColor =
                    psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.SurfaceDiffuseColor, 0);
                if (this.surfaceDiffuseColor == null)
                {
                    return false;
                }
            }

            //Get surface specular color if need to
            if ((this.trackVertexColorType & TrackVertexColor.Specular) == 0)
            {
                this.surfaceSpecularColor =
                    psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.SurfaceSpecularColor, 0);
                if (this.surfaceSpecularColor == null)
                {
                    return false;
                }
            }

            //Get surface emissive color if need to
            if ((this.trackVertexColorType & TrackVertexColor.Emissive) == 0)
            {
                this.surfaceEmissiveColor =
                    psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.SurfaceEmissiveColor, 0);
                if (this.surfaceEmissiveColor == null)
                {
                    return false;
                }
            }

            //Get derived scene color
            this.derivedSceneColor =
                psProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.DerivedSceneColor, 0);
            if (this.derivedSceneColor == null)
            {
                return false;
            }

            //Get surface shininess.
            this.surfaceShininess = psProgram.ResolveAutoParameterInt(
                GpuProgramParameters.AutoConstantType.SurfaceShininess, 0);
            if (this.surfaceShininess == null)
            {
                return false;
            }

            //Resolve input vertex shader normal
            this.vsInNormal = vsMain.ResolveInputParameter(Parameter.SemanticType.Normal, 0,
                                                            Parameter.ContentType.NormalObjectSpace,
                                                            Axiom.Graphics.GpuProgramParameters.GpuConstantType.Float3);
            if (this.vsInNormal == null)
            {
                return false;
            }

            //Resolve input vertex shader tangent
            if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
            {
                this.vsInTangent = vsMain.ResolveInputParameter(Parameter.SemanticType.Tangent, 0,
                                                                 Parameter.ContentType.TangentObjectSpace,
                                                                 GpuProgramParameters.GpuConstantType.Float3);
                if (this.vsInTangent == null)
                {
                    return false;
                }

                //Resolve local vertex shader TNB matrix
                this.vsTBNMatrix = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0, "lMatTBN",
                                                                 GpuProgramParameters.GpuConstantType.Matrix_3X3);
                if (this.vsTBNMatrix == null)
                {
                    return false;
                }
            }

            //resolve input vertex shader texture coordinates
            Parameter.ContentType texCoordToUse = Parameter.ContentType.TextureCoordinate0;
            switch (this.vsTexCoordSetIndex)
            {
                case 0:
                    texCoordToUse = Parameter.ContentType.TextureCoordinate0;
                    break;
                case 1:
                    texCoordToUse = Parameter.ContentType.TextureCoordinate1;
                    break;
                case 2:
                    texCoordToUse = Parameter.ContentType.TextureCoordinate2;
                    break;
                case 3:
                    texCoordToUse = Parameter.ContentType.TextureCoordinate3;
                    break;
                case 4:
                    texCoordToUse = Parameter.ContentType.TextureCoordinate4;
                    break;
                case 5:
                    texCoordToUse = Parameter.ContentType.TextureCoordinate5;
                    break;
                case 6:
                    texCoordToUse = Parameter.ContentType.TextureCoordinate6;
                    break;
                case 7:
                    texCoordToUse = Parameter.ContentType.TextureCoordinate7;
                    break;
                default:
                    throw new AxiomException("vsTexCoordIndexOut of range", new ArgumentOutOfRangeException());
            }
            this.vsInTexcoord = vsMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, this.vsTexCoordSetIndex,
                                                              texCoordToUse, GpuProgramParameters.GpuConstantType.Float2);
            if (this.vsInTexcoord == null)
            {
                return false;
            }

            //Resolve output vertex shader texture coordinates
            this.vsOutTexcoord = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1, texCoordToUse,
                                                                GpuProgramParameters.GpuConstantType.Float2);
            if (this.vsOutTexcoord == null)
            {
                return false;
            }

            //resolve pixel input texture coordinates normal
            this.psInTexcoord = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, this.vsOutTexcoord.Index,
                                                              this.vsOutTexcoord.Content, this.vsOutTexcoord.Type);
            if (this.psInTexcoord == null)
            {
                return false;
            }

            //Resolve pixel shader normal.
            if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Object)
            {
                this.psNormal = psMain.ResolveLocalParameter(Parameter.SemanticType.Normal, 0,
                                                              Parameter.ContentType.NormalObjectSpace,
                                                              GpuProgramParameters.GpuConstantType.Float3);
                if (this.psNormal == null)
                {
                    return false;
                }
            }
            else if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
            {
                this.psNormal = psMain.ResolveLocalParameter(Parameter.SemanticType.Normal, 0,
                                                              Parameter.ContentType.NormalTangentSpace,
                                                              GpuProgramParameters.GpuConstantType.Float3);
                if (this.psNormal == null)
                {
                    return false;
                }
            }

            var inputParams = psMain.InputParameters;
            var localParams = psMain.LocalParameters;

            this.psDiffuse = Function.GetParameterByContent(inputParams, Parameter.ContentType.ColorDiffuse,
                                                             GpuProgramParameters.GpuConstantType.Float4);
            if (this.psDiffuse == null)
            {
                this.psDiffuse = Function.GetParameterByContent(localParams, Parameter.ContentType.ColorDiffuse,
                                                                 GpuProgramParameters.GpuConstantType.Float4);
                if (this.psDiffuse == null)
                {
                    return false;
                }
            }

            this.psOutDiffuse = psMain.ResolveOutputParameter(Parameter.SemanticType.Color, 0,
                                                               Parameter.ContentType.ColorDiffuse,
                                                               GpuProgramParameters.GpuConstantType.Float4);
            if (this.psOutDiffuse == null)
            {
                return false;
            }

            this.psTempDiffuseColor = psMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0, "lNormalMapDiffuse",
                                                                    GpuProgramParameters.GpuConstantType.Float4);
            if (this.psTempDiffuseColor == null)
            {
                return false;
            }

            if (this.specularEnabled)
            {
                this.psSpecular = Function.GetParameterByContent(inputParams, Parameter.ContentType.ColorSpecular,
                                                                  GpuProgramParameters.GpuConstantType.Float4);
                if (this.psSpecular == null)
                {
                    this.psSpecular = Function.GetParameterByContent(localParams, Parameter.ContentType.ColorSpecular,
                                                                      GpuProgramParameters.GpuConstantType.Float4);
                    if (this.psSpecular == null)
                    {
                        return false;
                    }
                }

                this.psTempSpecularColor = psMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0,
                                                                         "lNormalMapSpecular",
                                                                         GpuProgramParameters.GpuConstantType.Float4);
                if (this.psTempSpecularColor == null)
                {
                    return false;
                }

                this.vsInPosition = vsMain.ResolveInputParameter(Parameter.SemanticType.Position, 0,
                                                                  Parameter.ContentType.PositionObjectSpace,
                                                                  GpuProgramParameters.GpuConstantType.Float4);
                if (this.vsInPosition == null)
                {
                    return false;
                }
                if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
                {
                    this.vsOutView = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                                    Parameter.ContentType.PostOCameraTangentSpace,
                                                                    GpuProgramParameters.GpuConstantType.Float3);
                    if (this.vsOutView == null)
                    {
                        return false;
                    }
                }
                else if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Object)
                {
                    this.vsOutView = vsMain.ResolveOutputParameter(Parameter.SemanticType.TextureCoordinates, -1,
                                                                    Parameter.ContentType.PostOCameraTangentSpace,
                                                                    GpuProgramParameters.GpuConstantType.Float3);
                    if (this.vsOutView == null)
                    {
                        return false;
                    }
                }

                this.psInView = psMain.ResolveInputParameter(Parameter.SemanticType.TextureCoordinates, this.vsOutView.Index,
                                                              this.vsOutView.Content, this.vsOutView.Type);
                if (this.psInView == null)
                {
                    return false;
                }

                //Resolve camera position world space
                this.camPosWorldSpace =
                    vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.CameraPosition, 0);
                if (this.camPosWorldSpace == null)
                {
                    return false;
                }

                this.vsLocalDir = vsMain.ResolveLocalParameter(Parameter.SemanticType.Unknown, 0, "lNormalMapTempDir",
                                                                GpuProgramParameters.GpuConstantType.Float3);
                if (this.vsLocalDir == null)
                {
                    return false;
                }

                this.vsWorldPosition = vsMain.ResolveLocalParameter(Parameter.SemanticType.Position, 0,
                                                                     Parameter.ContentType.PositionWorldSpace,
                                                                     GpuProgramParameters.GpuConstantType.Float3);
                if (this.vsWorldPosition == null)
                {
                    return false;
                }

                //Resolve world matrix
                this.worldMatrix = vsProgram.ResolveAutoParameterInt(GpuProgramParameters.AutoConstantType.WorldMatrix, 0);
                if (this.worldMatrix == null)
                {
                    return false;
                }

                //Resolve inverse world rotation matrix
                this.worldInvRotMatrix = vsProgram.ResolveParameter(GpuProgramParameters.GpuConstantType.Matrix_4X4, -1,
                                                                     GpuProgramParameters.GpuParamVariability.PerObject,
                                                                     "inv_world_rotation_matrix");
                if (this.worldInvRotMatrix == null)
                {
                    return false;
                }
            }


            return true;
        }

        protected override bool ResolveDependencies(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;

            vsProgram.AddDependency(FFPRenderState.FFPLibCommon);
            vsProgram.AddDependency(SGXLibNormalMapLighting);

            psProgram.AddDependency(FFPRenderState.FFPLibCommon);
            psProgram.AddDependency(SGXLibNormalMapLighting);

            return true;
        }

        protected override bool AddFunctionInvocations(ProgramSet programSet)
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            int internalCounter = 0;

            //Add the global illumination functions.
            if (AddVSInvocation(vsMain, (int)FFPRenderState.FFPVertexShaderStage.VSLighting, internalCounter) ==
                 false)
            {
                return false;
            }

            internalCounter = 0;

            //Add the normal fetch function invocation.
            if (AddPSNormalFetchInvocation(psMain, -1 + 1, ref internalCounter) == false)
            {
                return false;
            }

            //Add the global illuminatin functions
            if (AddPSGlobalIlluminationInvocation(psMain, -1 + 1, ref internalCounter) == false)
            {
                return false;
            }

            //Add per light functions
            for (int i = 0; i < this.lightParamsList.Count; i++)
            {
                if (AddPSIlluminationInvocation(this.lightParamsList[i], psMain, -1 + 1, ref internalCounter) == false)
                {
                    return false;
                }
            }

            //Assign back temporary variables to the ps diffuse and specular components.
            if (AddPSFinalAssignmentInvocation(psMain, -1 + 1, ref internalCounter) == false)
            {
                return false;
            }

            return true;
        }

        public override bool PreAddToRenderState(TargetRenderState targetRenderState, Pass srcPass, Pass dstPass)
        {
            if (srcPass.LightingEnabled == false)
            {
                return false;
            }

            var lightCount = new int[3];

            targetRenderState.GetLightCount(out lightCount);

            TextureUnitState normalMapTexture = dstPass.CreateTextureUnitState();

            normalMapTexture.SetTextureName(this.normalMapTextureName);
            normalMapTexture.SetTextureFiltering(this.normalMapMinFilter, this.normalMapMagFilter, this.normalMapMipfilter);
            normalMapTexture.TextureAnisotropy = (int)this.normalMapAnisotropy;
            normalMapTexture.TextureMipmapBias = this.normalMapMipBias;
            this.normalMapSamplerIndex = dstPass.TextureUnitStatesCount - 1;

            TrackVertexColorType = (srcPass.VertexColorTracking);

            if (srcPass.Shininess > 0 && srcPass.Specular != ColorEx.Black)
            {
                SpecularEnable = true;
            }
            else
            {
                SpecularEnable = false;
            }

            //Case this pass should run once per light(s)
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

                //This is wors case -> all light types epected to be handled.
                //Can not handle this request in efficient way - throw an exception
                else
                {
                    throw new AxiomException(
                        "Using iterative lighting method with RT Shader System requires specifying explicit light type.");
                }
            }

            SetLightCount(lightCount);
            return true;
        }

        #endregion

        #region Protected Methods

        protected bool AddVSInvocation(Function vsMain, int groupOrder, int internalCounter)
        {
            FunctionInvocation curFuncInvocation = null;


            //Construct TNB matrix.
            if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
            {
                curFuncInvocation = new FunctionInvocation(SGXFuncConstructTbnMatrix, groupOrder, internalCounter++);
                curFuncInvocation.PushOperand(this.vsInNormal, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.vsInTangent, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.vsTBNMatrix, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }

            //Output texture coordinates
            curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++);
            curFuncInvocation.PushOperand(this.vsInTexcoord, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.vsOutTexcoord, Operand.OpSemantic.Out);
            vsMain.AddAtomInstance(curFuncInvocation);

            //computer world space position
            if (this.vsWorldPosition != null)
            {
                curFuncInvocation = new FunctionInvocation(SGXFuncTransformPosition, groupOrder, internalCounter++);
                curFuncInvocation.PushOperand(this.worldMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.vsInPosition, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.vsWorldPosition, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);
            }

            //compute the view vector
            if (this.vsInPosition != null && this.vsOutView != null)
            {
                //View vector in world space

                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncSubtract, groupOrder,
                                                            internalCounter++);
                curFuncInvocation.PushOperand(this.camPosWorldSpace, Operand.OpSemantic.In,
                                               ((int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z));
                curFuncInvocation.PushOperand(this.vsWorldPosition, Operand.OpSemantic.In,
                                               ((int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z));
                curFuncInvocation.PushOperand(this.vsLocalDir, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);

                //Transform to object space.
                curFuncInvocation = new FunctionInvocation(SGXFuncTranformNormal, groupOrder, internalCounter++);
                curFuncInvocation.PushOperand(this.worldInvRotMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.vsLocalDir, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.vsLocalDir, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);

                //Transform to tangent space
                if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
                {
                    curFuncInvocation = new FunctionInvocation(SGXFuncTranformNormal, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(this.vsTBNMatrix, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.vsLocalDir, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.vsOutView, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);
                }
                //output object space
                else if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Object)
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(this.vsLocalDir, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.vsOutView, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);
                }
            }

            //Add per light functions
            for (int i = 0; i < this.lightParamsList.Count; i++)
            {
                if (AddVSIlluminationInvocation(this.lightParamsList[i], vsMain, groupOrder, ref internalCounter) ==
                     false)
                {
                    return false;
                }
            }
            return true;
        }

        internal bool AddVSIlluminationInvocation(LightParams curLightParams, Function vsMain, int groupOrder,
                                                   ref int internalCounter)
        {
            FunctionInvocation curFuncInvocation = null;

            //Computer light direction in texture space.
            if (curLightParams.Direction != null && curLightParams.VSOutDirection != null)
            {
                //Transform to texture space.
                if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
                {
                    curFuncInvocation = new FunctionInvocation(SGXFuncTranformNormal, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(this.vsTBNMatrix, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(curLightParams.Direction, Operand.OpSemantic.In,
                                                   ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                     (int)Operand.OpMask.Z));
                    curFuncInvocation.PushOperand(curLightParams.VSOutDirection, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);
                }
                //Output object space
                else if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Object)
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(curLightParams.Direction, Operand.OpSemantic.In,
                                                   ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                     (int)Operand.OpMask.Z));
                    curFuncInvocation.PushOperand(curLightParams.VSOutDirection, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);
                }
            }

            //Transform light vector to target space
            if (curLightParams.Position != null && curLightParams.VSOutToLightDir != null)
            {
                //Compute light vector.
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncSubtract, groupOrder,
                                                            internalCounter++);
                curFuncInvocation.PushOperand(curLightParams.Position, Operand.OpSemantic.In,
                                               ((int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z));
                curFuncInvocation.PushOperand(this.vsWorldPosition, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.vsLocalDir, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);

                //Transform to object space
                curFuncInvocation = new FunctionInvocation(SGXFuncTranformNormal, groupOrder, internalCounter++);
                curFuncInvocation.PushOperand(this.worldInvRotMatrix, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.vsLocalDir, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.vsLocalDir, Operand.OpSemantic.Out);
                vsMain.AddAtomInstance(curFuncInvocation);

                if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent)
                {
                    curFuncInvocation = new FunctionInvocation(SGXFuncTranformNormal, groupOrder, internalCounter++);
                    curFuncInvocation.PushOperand(this.vsTBNMatrix, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.vsLocalDir, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(curLightParams.VSOutToLightDir, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);
                }
                //Output object space
                else if (this.normalMapSpace == RTShaderSystem.NormalMapSpace.Object)
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++);
                    curFuncInvocation.PushOperand(this.vsLocalDir, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(curLightParams.VSOutToLightDir, Operand.OpSemantic.Out);
                    vsMain.AddAtomInstance(curFuncInvocation);
                }
            }

            return true;
        }

        protected bool AddPSNormalFetchInvocation(Function psMain, int groupOrder, ref int internalCounter)
        {
            FunctionInvocation curFuncInvocation = null;
            curFuncInvocation = new FunctionInvocation(SGXFuncFetchNormal, groupOrder, internalCounter++);
            curFuncInvocation.PushOperand(this.normalMapSampler, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psInTexcoord, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psNormal, Operand.OpSemantic.Out);
            psMain.AddAtomInstance(curFuncInvocation);

            return true;
        }

        protected bool AddPSGlobalIlluminationInvocation(Function psMain, int groupOrder, ref int internalCount)
        {
            FunctionInvocation curFuncInvocation = null;

            if ((this.trackVertexColorType & TrackVertexColor.Ambient) == 0 &&
                 (this.trackVertexColorType & TrackVertexColor.Emissive) == 0)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCount++);
                curFuncInvocation.PushOperand(this.derivedSceneColor, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }
            else
            {
                if ((this.trackVertexColorType & TrackVertexColor.Ambient) != 0)
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncModulate, groupOrder,
                                                                internalCount++);
                    curFuncInvocation.PushOperand(this.lightAmbientColor, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                }
                else
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCount++);
                    curFuncInvocation.PushOperand(this.derivedAmbientLightColor, Operand.OpSemantic.In,
                                                   ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                     (int)Operand.OpMask.Z));
                    curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In,
                                                   ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                     (int)Operand.OpMask.Z));
                    curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                }

                if ((this.trackVertexColorType & TrackVertexColor.Emissive) != 0)
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAdd, groupOrder, internalCount++);
                    curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                }
                else
                {
                    curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAdd, groupOrder, internalCount++);
                    curFuncInvocation.PushOperand(this.surfaceEmissiveColor, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.In);
                    curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out);
                    psMain.AddAtomInstance(curFuncInvocation);
                }
            }
            if (this.specularEnabled)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign, groupOrder, internalCount++);
                curFuncInvocation.PushOperand(this.psSpecular, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.psTempSpecularColor, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }
            return true;
        }

        protected bool AddPSIlluminationInvocation(LightParams curLightParams, Function psMain, int groupOrder,
                                                    ref int internalCounter)
        {
            FunctionInvocation curFuncInvocation = null;
            //Merge diffuse color with vertex color if need to
            if ((this.trackVertexColorType & TrackVertexColor.Diffuse) != 0)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncModulate, groupOrder,
                                                            internalCounter++);
                curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In,
                                               ((int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z));
                curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                               ((int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z));
                curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.Out,
                                               ((int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z));
                psMain.AddAtomInstance(curFuncInvocation);
            }

            //Merge specular color with vertex color if need to
            if (this.specularEnabled && (this.trackVertexColorType & TrackVertexColor.Specular) != 0)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncModulate, groupOrder,
                                                            internalCounter++);
                curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In,
                                               ((int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z));
                curFuncInvocation.PushOperand(curLightParams.SpecularColor, Operand.OpSemantic.In,
                                               ((int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z));
                curFuncInvocation.PushOperand(curLightParams.SpecularColor, Operand.OpSemantic.Out,
                                               ((int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z));
                psMain.AddAtomInstance(curFuncInvocation);
            }

            switch (curLightParams.Type)
            {
                case LightType.Directional:
                    if (this.specularEnabled)
                    {
                        curFuncInvocation = new FunctionInvocation(SGXFuncLightDirectionDiffuseSpecular, groupOrder,
                                                                    internalCounter++);
                        curFuncInvocation.PushOperand(this.psNormal, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psInView, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.PSInDirection, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.SpecularColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.surfaceShininess, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempSpecularColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempSpecularColor, Operand.OpSemantic.Out,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        psMain.AddAtomInstance(curFuncInvocation);
                    }
                    else
                    {
                        curFuncInvocation = new FunctionInvocation(SGXFuncLightDirectionalDiffuse, groupOrder,
                                                                    internalCounter++);
                        curFuncInvocation.PushOperand(this.psNormal, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.PSInDirection, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        psMain.AddAtomInstance(curFuncInvocation);
                    }
                    break;
                case LightType.Point:
                    if (this.specularEnabled)
                    {
                        curFuncInvocation = new FunctionInvocation(SGXFuncLightPointDiffuseSpecular, groupOrder,
                                                                    internalCounter++);
                        curFuncInvocation.PushOperand(this.psNormal, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(this.psInView, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.PSInToLightDir, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.AttenuatParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.SpecularColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.surfaceShininess, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempSpecularColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempSpecularColor, Operand.OpSemantic.Out,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        psMain.AddAtomInstance(curFuncInvocation);
                    }
                    else
                    {
                        curFuncInvocation = new FunctionInvocation(SGXFuncLightPointDiffuse, groupOrder,
                                                                    internalCounter++);
                        curFuncInvocation.PushOperand(this.psNormal, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.PSInToLightDir, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.AttenuatParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        psMain.AddAtomInstance(curFuncInvocation);
                    }
                    break;
                case LightType.Spotlight:
                    if (this.specularEnabled)
                    {
                        curFuncInvocation = new FunctionInvocation(SGXFuncLightSpotDiffuseSpecular, groupOrder,
                                                                    internalCounter++);
                        curFuncInvocation.PushOperand(this.psNormal, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(this.psInView, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.PSInToLightDir, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.PSInDirection, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.AttenuatParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.SpotParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.SpecularColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.surfaceShininess, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempSpecularColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempSpecularColor, Operand.OpSemantic.Out,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        psMain.AddAtomInstance(curFuncInvocation);
                    }
                    else
                    {
                        curFuncInvocation = new FunctionInvocation(SGXFuncLightSpotDiffuse, groupOrder,
                                                                    internalCounter++);
                        curFuncInvocation.PushOperand(this.psNormal, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.PSInToLightDir, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.PSInDirection, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(curLightParams.AttenuatParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.SpotParams, Operand.OpSemantic.In);
                        curFuncInvocation.PushOperand(curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ((int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z));
                        psMain.AddAtomInstance(curFuncInvocation);
                    }
                    break;
            }
            return true;
        }

        protected bool AddPSFinalAssignmentInvocation(Function psMain, int groupOrder, ref int internalCounter)
        {
            var curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign,
                                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
                                                            internalCounter++);
            curFuncInvocation.PushOperand(this.psTempDiffuseColor, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.Out);
            psMain.AddAtomInstance(curFuncInvocation);

            curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign,
                                                        (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
                                                        internalCounter++);
            curFuncInvocation.PushOperand(this.psDiffuse, Operand.OpSemantic.In);
            curFuncInvocation.PushOperand(this.psOutDiffuse, Operand.OpSemantic.Out);
            psMain.AddAtomInstance(curFuncInvocation);

            if (this.specularEnabled)
            {
                curFuncInvocation = new FunctionInvocation(FFPRenderState.FFPFuncAssign,
                                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
                                                            internalCounter++);
                curFuncInvocation.PushOperand(this.psTempSpecularColor, Operand.OpSemantic.In);
                curFuncInvocation.PushOperand(this.psSpecular, Operand.OpSemantic.Out);
                psMain.AddAtomInstance(curFuncInvocation);
            }


            return true;
        }

        #endregion

        #region Properties

        public int TexCoordIndex
        {
            get
            {
                return this.vsTexCoordSetIndex;
            }
            set
            {
                this.vsTexCoordSetIndex = value;
            }
        }

        public NormalMapSpace NormalMapSpace
        {
            get
            {
                return this.normalMapSpace;
            }
            set
            {
                this.normalMapSpace = value;
            }
        }

        public string NormalMapTextureName
        {
            get
            {
                return this.normalMapTextureName;
            }
            set
            {
                this.normalMapTextureName = value;
            }
        }

        public int NormalMapAnisotropy
        {
            get
            {
                return this.normalMapAnisotropy;
            }
            set
            {
                this.normalMapAnisotropy = value;
            }
        }

        public Real NormalMapMipBias
        {
            get
            {
                return this.normalMapMipBias;
            }
            set
            {
                this.normalMapMipBias = value;
            }
        }

        public TrackVertexColor TrackVertexColorType
        {
            get
            {
                return this.trackVertexColorType;
            }
            set
            {
                this.trackVertexColorType = value;
            }
        }

        public bool SpecularEnable
        {
            get
            {
                return this.specularEnabled;
            }
            set
            {
                this.specularEnabled = value;
            }
        }

        public override string Type
        {
            get
            {
                return "SGX_NormalMapLighting";
            }
        }

        public override int ExecutionOrder
        {
            get
            {
                return (int)FFPRenderState.FFPShaderStage.Lighting;
            }
        }

        #endregion
    }
}