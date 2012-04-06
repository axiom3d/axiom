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
        private uint normalMapAnisotropy;
        private readonly Real normalMapMipBias;
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
            trackVertexColorType = TrackVertexColor.None;
            normalMapSamplerIndex = 0;
            vsTexCoordSetIndex = 0;
            specularEnabled = false;
            normalMapSpace = NormalMapSpace.Tangent;
            normalMapMinFilter = FilterOptions.Linear;
            normalMapMagFilter = FilterOptions.Linear;
            normalMapMipfilter = FilterOptions.Point;
            normalMapAnisotropy = 1;
            normalMapMipBias = -1.0;

            blankLight.Diffuse = ColorEx.Black;
            blankLight.Specular = ColorEx.Black;
            blankLight.SetAttenuation( 0, 1, 0, 0 );
        }

        #endregion

        #region Public Methods

        public void SetNormalMapFiltering( FilterOptions minFilter, FilterOptions magFilter, FilterOptions mipFilter )
        {
            normalMapMinFilter = minFilter;
            normalMapMagFilter = magFilter;
            normalMapMipfilter = mipFilter;
        }

        public void GetNormalMapFiltering( out FilterOptions minFilter, out FilterOptions magFilter,
                                           out FilterOptions mipFilter )
        {
            minFilter = normalMapMinFilter;
            magFilter = normalMapMagFilter;
            mipFilter = normalMapMipfilter;
        }

        #endregion

        #region Overriden Methods

        public override void GetLightCount( out int[] lightCount )
        {
            base.GetLightCount( out lightCount );
        }

        public override void SetLightCount( int[] currLightCount )
        {
            base.SetLightCount( currLightCount );
        }

        public override void UpdateGpuProgramsParams( IRenderable rend, Pass pass, AutoParamDataSource source,
                                                      Core.Collections.LightList lightList )
        {
            if ( lightParamsList.Count == 0 )
            {
                return;
            }

            var curLightType = LightType.Directional;
            int curSearchLightIndex = 0;
            Matrix4 matWorld = source.WorldMatrix;
            Matrix3 matWorldInvRotation;

            var vRow0 = new Vector3( matWorld[ 0, 0 ], matWorld[ 0, 1 ], matWorld[ 0, 2 ] );
            var vRow1 = new Vector3( matWorld[ 1, 0 ], matWorld[ 1, 1 ], matWorld[ 1, 2 ] );
            var vRow2 = new Vector3( matWorld[ 2, 0 ], matWorld[ 2, 1 ], matWorld[ 2, 2 ] );

            vRow0.Normalize();
            vRow1.Normalize();
            vRow2.Normalize();

            matWorldInvRotation = new Matrix3( vRow0, vRow1, vRow2 );
            //update inverse rotation parameter
            if ( worldInvRotMatrix != null )
            {
                worldInvRotMatrix.SetGpuParameter( matWorldInvRotation );
            }

            //update per light parameters
            for ( int i = 0; i < lightParamsList.Count; i++ )
            {
                LightParams curParams = lightParamsList[ i ];

                if ( curLightType != curParams.Type )
                {
                    curLightType = curParams.Type;
                    curSearchLightIndex = 0;
                }

                Light srcLight = null;
                Vector4 vParameter;
                ColorEx color;

                //Search a matching light from the current sorted lights of the given renderable
                for ( int j = 0; j < lightList.Count; j++ )
                {
                    if ( lightList[ j ].Type == curLightType )
                    {
                        srcLight = lightList[ j ];
                        curSearchLightIndex = j + 1;
                        break;
                    }
                }

                //No matching light found -> use a blank dummy light for parameter update
                if ( srcLight == null )
                {
                    srcLight = blankLight;
                }

                switch ( curParams.Type )
                {
                    case LightType.Directional:
                    {
                        Vector3 vec3;

                        //Update light direction (object space)
                        vec3 = matWorldInvRotation*srcLight.DerivedDirection;
                        vec3.Normalize();

                        vParameter.x = -vec3.x;
                        vParameter.y = -vec3.y;
                        vParameter.z = -vec3.z;
                        vParameter.w = 0.0;
                        curParams.Direction.SetGpuParameter( vParameter );
                    }
                        break;
                    case LightType.Point:
                        //update light position (world space)
                        vParameter = srcLight.GetAs4DVector( true );
                        curParams.Position.SetGpuParameter( vParameter );

                        //Update light attenuation parameters.
                        vParameter.x = srcLight.AttenuationRange;
                        vParameter.y = srcLight.AttenuationConstant;
                        vParameter.z = srcLight.AttenuationLinear;
                        vParameter.w = srcLight.AttenuationQuadratic;
                        curParams.AttenuatParams.SetGpuParameter( vParameter );

                        break;
                    case LightType.Spotlight:
                    {
                        Vector3 vec3;

                        //Update light position (world space)
                        vParameter = srcLight.GetAs4DVector( true );
                        curParams.Position.SetGpuParameter( vParameter );

                        //Update light direction (object space)
                        vec3 = matWorldInvRotation*srcLight.DerivedDirection;
                        vec3.Normalize();

                        vParameter.x = -vec3.x;
                        vParameter.y = -vec3.y;
                        vParameter.z = -vec3.z;
                        vParameter.w = 0.0;

                        curParams.Direction.SetGpuParameter( vParameter );

                        //Update light attenuation parameters.
                        vParameter.x = srcLight.AttenuationRange;
                        vParameter.y = srcLight.AttenuationConstant;
                        vParameter.z = srcLight.AttenuationLinear;
                        vParameter.w = srcLight.AttenuationQuadratic;
                        curParams.AttenuatParams.SetGpuParameter( vParameter );

                        //Update spotlight parameters
                        Real phi = System.Math.Cos( (double)srcLight.SpotlightOuterAngle*0.5f );
                        Real theta = System.Math.Cos( (double)srcLight.SpotlightInnerAngle*0.5f );

                        vec3.x = theta;
                        vec3.y = phi;
                        vec3.z = srcLight.SpotlightFalloff;

                        curParams.SpotParams.SetGpuParameter( vec3 );
                    }
                        break;
                }

                //Update diffuse color
                if ( ( trackVertexColorType & TrackVertexColor.Diffuse ) == 0 )
                {
                    color = srcLight.Diffuse*pass.Diffuse;
                    curParams.DiffuseColor.SetGpuParameter( color );
                }
                else
                {
                    color = srcLight.Diffuse;
                    curParams.DiffuseColor.SetGpuParameter( color );
                }

                //Update specular color if need to
                if ( specularEnabled )
                {
                    //Update diffuse color
                    if ( ( trackVertexColorType & TrackVertexColor.Diffuse ) == 0 )
                    {
                        color = srcLight.Specular*pass.Specular;
                        curParams.SpecularColor.SetGpuParameter( color );
                    }
                    else
                    {
                        color = srcLight.Specular;
                        curParams.SpecularColor.SetGpuParameter( color );
                    }
                }
            }
        }

        protected override bool ResolveParameters( ProgramSet programSet )
        {
            if ( ResolveGlobalParameters( programSet ) == false )
            {
                return false;
            }
            if ( ResolvePerLightParameters( programSet ) == false )
            {
                return false;
            }

            return true;
        }

        private bool ResolvePerLightParameters( ProgramSet programSet )
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            for ( int i = 0; i < lightParamsList.Count; i++ )
            {
                switch ( lightParamsList[ i ].Type )
                {
                    case LightType.Directional:
                        lightParamsList[ i ].Direction =
                            vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.PerObject,
                                                        "light_direction_obj_space" );
                        if ( lightParamsList[ i ].Direction == null )
                        {
                            return false;
                        }


                        Parameter.ContentType pctToUse;

                        if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
                        {
                            switch ( i )
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
                                    throw new AxiomException( "Index out of range" );
                            }
                            lightParamsList[ i ].VSOutDirection =
                                vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1, pctToUse,
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                        }
                        else if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Object )
                        {
                            switch ( i )
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
                                    throw new AxiomException( "Index out of range" );
                            }
                            lightParamsList[ i ].VSOutToLightDir =
                                vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1, pctToUse,
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                        }
                        if ( lightParamsList[ i ].VSOutToLightDir == null )
                        {
                            return false;
                        }

                        lightParamsList[ i ].PSInDirection =
                            psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates,
                                                          lightParamsList[ i ].VSOutToLightDir.Index,
                                                          lightParamsList[ i ].VSOutToLightDir.Content,
                                                          lightParamsList[ i ].VSOutToLightDir.Type );
                        if ( lightParamsList[ i ].PSInDirection == null )
                        {
                            return false;
                        }

                        break;
                    case LightType.Point:
                        vsInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
                                                                     Parameter.ContentType.PositionObjectSpace,
                                                                     GpuProgramParameters.GpuConstantType.Float4 );
                        if ( vsInPosition == null )
                        {
                            return false;
                        }

                        lightParamsList[ i ].Position =
                            vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights |
                                                        GpuProgramParameters.GpuParamVariability.PerObject,
                                                        "light_position_world_space" );
                        if ( lightParamsList[ i ].Position == null )
                        {
                            return false;
                        }

                        if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
                        {
                            lightParamsList[ i ].VSOutToLightDir =
                                vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum( RTShaderSystem.NormalMapSpace.Tangent, i ),
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                        }
                        else if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Object )
                        {
                            lightParamsList[ i ].VSOutToLightDir =
                                vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum( RTShaderSystem.NormalMapSpace.Object, i ),
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                        }
                        if ( lightParamsList[ i ].VSOutToLightDir == null )
                        {
                            return false;
                        }

                        lightParamsList[ i ].PSInToLightDir =
                            psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates,
                                                          lightParamsList[ i ].VSOutToLightDir.Index,
                                                          lightParamsList[ i ].VSOutToLightDir.Content,
                                                          lightParamsList[ i ].VSOutToLightDir.Type );
                        if ( lightParamsList[ i ].PSInToLightDir == null )
                        {
                            return false;
                        }

                        lightParamsList[ i ].AttenuatParams =
                            psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights,
                                                        "light_attenuation" );
                        if ( lightParamsList[ i ].AttenuatParams == null )
                        {
                            return false;
                        }

                        //Resolve local dir
                        if ( vsLocalDir == null )
                        {
                            vsLocalDir = vsMain.ResolveLocalParameter( Parameter.SemanticType.Unknown, 0,
                                                                       "lNormalMapTempDir",
                                                                       GpuProgramParameters.GpuConstantType.Float3 );
                            if ( vsLocalDir == null )
                            {
                                return false;
                            }
                        }

                        //Resolve world position
                        if ( vsWorldPosition == null )
                        {
                            vsWorldPosition = vsMain.ResolveLocalParameter( Parameter.SemanticType.Position, 0,
                                                                            Parameter.ContentType.PositionWorldSpace,
                                                                            GpuProgramParameters.GpuConstantType.Float3 );
                            if ( vsWorldPosition == null )
                            {
                                return false;
                            }
                        }

                        //resolve world matrix
                        if ( worldMatrix == null )
                        {
                            worldMatrix =
                                vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldMatrix, 0 );
                            if ( worldMatrix == null )
                            {
                                return false;
                            }
                        }
                        //resolve inverse world rotation matrix
                        if ( worldInvRotMatrix == null )
                        {
                            worldInvRotMatrix =
                                vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Matrix_4X4, -1,
                                                            GpuProgramParameters.GpuParamVariability.PerObject,
                                                            "inv_world_rotation_matrix" );
                            if ( worldInvRotMatrix == null )
                            {
                                return false;
                            }
                        }
                        break;
                    case LightType.Spotlight:
                        vsInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
                                                                     Parameter.ContentType.PositionObjectSpace,
                                                                     GpuProgramParameters.GpuConstantType.Float4 );
                        if ( vsInPosition == null )
                        {
                            return false;
                        }

                        lightParamsList[ i ].Position =
                            vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights |
                                                        GpuProgramParameters.GpuParamVariability.PerObject,
                                                        "light_position_world_space" );
                        if ( lightParamsList[ i ].Position == null )
                        {
                            return false;
                        }

                        if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
                        {
                            lightParamsList[ i ].VSOutToLightDir =
                                vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum( RTShaderSystem.NormalMapSpace.Tangent, i ),
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                        }
                        if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Object )
                        {
                            lightParamsList[ i ].VSOutToLightDir =
                                vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum( RTShaderSystem.NormalMapSpace.Object, i ),
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                        }
                        if ( lightParamsList[ i ].VSOutToLightDir == null )
                        {
                            return false;
                        }

                        lightParamsList[ i ].PSInToLightDir =
                            psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates,
                                                          lightParamsList[ i ].VSOutToLightDir.Index,
                                                          lightParamsList[ i ].VSOutToLightDir.Content,
                                                          lightParamsList[ i ].VSOutToLightDir.Type );
                        if ( lightParamsList[ i ].PSInToLightDir == null )
                        {
                            return false;
                        }

                        lightParamsList[ i ].Direction =
                            vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights |
                                                        GpuProgramParameters.GpuParamVariability.PerObject,
                                                        "light_direction_obj_space" );
                        if ( lightParamsList[ i ].Direction == null )
                        {
                            return false;
                        }
                        if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
                        {
                            lightParamsList[ i ].VSOutDirection =
                                vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum( RTShaderSystem.NormalMapSpace.Tangent, i ),
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                            if ( lightParamsList[ i ].VSOutDirection == null )
                            {
                                return false;
                            }
                        }
                        else if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Object )
                        {
                            lightParamsList[ i ].VSOutDirection =
                                vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
                                                               IntToEnum( RTShaderSystem.NormalMapSpace.Object, i ),
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                            if ( lightParamsList[ i ].VSOutDirection == null )
                            {
                                return false;
                            }
                        }
                        if ( lightParamsList[ i ].VSOutDirection == null )
                        {
                            return false;
                        }

                        lightParamsList[ i ].PSInDirection =
                            psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates,
                                                          lightParamsList[ i ].VSOutDirection.Index,
                                                          lightParamsList[ i ].VSOutDirection.Content,
                                                          lightParamsList[ i ].VSOutDirection.Type );
                        if ( lightParamsList[ i ].PSInDirection == null )
                        {
                            return false;
                        }

                        lightParamsList[ i ].AttenuatParams =
                            psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights,
                                                        "light_attenuation" );
                        if ( lightParamsList[ i ].AttenuatParams == null )
                        {
                            return false;
                        }

                        lightParamsList[ i ].SpotParams =
                            psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float3, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights,
                                                        "spotlight_params" );
                        if ( lightParamsList[ i ].SpotParams == null )
                        {
                            return false;
                        }

                        //Resolve local dir
                        if ( vsLocalDir == null )
                        {
                            vsLocalDir = vsMain.ResolveLocalParameter( Parameter.SemanticType.Unknown, 0,
                                                                       "lNormalMapTempDir",
                                                                       GpuProgramParameters.GpuConstantType.Float3 );
                            if ( vsLocalDir == null )
                            {
                                return false;
                            }
                        }

                        //resolve world postion
                        if ( vsWorldPosition == null )
                        {
                            vsWorldPosition = vsMain.ResolveLocalParameter( Parameter.SemanticType.Position, 0,
                                                                            Parameter.ContentType.PositionWorldSpace,
                                                                            GpuProgramParameters.GpuConstantType.Float3 );
                            if ( vsWorldPosition == null )
                            {
                                return false;
                            }
                        }
                        //resolve world matrix
                        if ( worldMatrix == null )
                        {
                            worldMatrix =
                                vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldMatrix, 0 );
                            if ( worldMatrix == null )
                            {
                                return false;
                            }
                        }
                        //resovle inverse world rotation matrix
                        if ( worldInvRotMatrix == null )
                        {
                            worldInvRotMatrix =
                                vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Matrix_4X4, -1,
                                                            GpuProgramParameters.GpuParamVariability.PerObject,
                                                            "inv_world_rotation_matrix" );
                            if ( worldInvRotMatrix == null )
                            {
                                return false;
                            }
                        }
                        break;
                }

                //resolve diffuse color
                if ( ( trackVertexColorType & TrackVertexColor.Diffuse ) == 0 )
                {
                    lightParamsList[ i ].DiffuseColor =
                        psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
                                                    GpuProgramParameters.GpuParamVariability.Lights,
                                                    "derived_light_diffuse" );
                    if ( lightParamsList[ i ].DiffuseColor == null )
                    {
                        return false;
                    }
                }
                else
                {
                    lightParamsList[ i ].DiffuseColor =
                        psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
                                                    GpuProgramParameters.GpuParamVariability.Lights, "light_diffuse" );
                    if ( lightParamsList[ i ].DiffuseColor == null )
                    {
                        return false;
                    }
                }

                if ( specularEnabled )
                {
                    //resolve specular color
                    if ( ( trackVertexColorType & TrackVertexColor.Specular ) == 0 )
                    {
                        lightParamsList[ i ].SpecularColor =
                            psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights,
                                                        "derived_light_specular" );
                        if ( lightParamsList[ i ].SpecularColor == null )
                        {
                            return false;
                        }
                    }
                    else
                    {
                        lightParamsList[ i ].SpecularColor =
                            psProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Float4, -1,
                                                        GpuProgramParameters.GpuParamVariability.Lights,
                                                        "light_specular" );
                        if ( lightParamsList[ i ].SpecularColor == null )
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private Parameter.ContentType IntToEnum( NormalMapSpace nms, int index )
        {
            if ( nms == RTShaderSystem.NormalMapSpace.Tangent )
            {
                switch ( index )
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
                        throw new AxiomException( "Index out of range" );
                }
            }
            else
            {
                switch ( index )
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
                        throw new AxiomException( "Index out of range" );
                }
            }
        }

        private bool ResolveGlobalParameters( ProgramSet programSet )
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            //Resolve normal map texture sampler parameter
            normalMapSampler = psProgram.ResolveParameter(
                Axiom.Graphics.GpuProgramParameters.GpuConstantType.Sampler2D, normalMapSamplerIndex,
                GpuProgramParameters.GpuParamVariability.PerObject, "gNormalMapSampler" );
            if ( normalMapSampler == null )
            {
                return false;
            }

            //Get surface ambient color if need to
            if ( ( trackVertexColorType & TrackVertexColor.Ambient ) == 0 )
            {
                derivedAmbientLightColor =
                    psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.DerivedAmbientLightColor, 0 );
                if ( derivedAmbientLightColor == null )
                {
                    return false;
                }
            }
            else
            {
                lightAmbientColor =
                    psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.AmbientLightColor, 0 );
                if ( lightAmbientColor == null )
                {
                    return false;
                }

                surfaceAmbientColor =
                    psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceAmbientColor, 0, 0 );
                if ( surfaceAmbientColor == null )
                {
                    return false;
                }
            }

            //Get surface diffuse color if need to
            if ( ( trackVertexColorType & TrackVertexColor.Diffuse ) == 0 )
            {
                surfaceDiffuseColor =
                    psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceDiffuseColor, 0 );
                if ( surfaceDiffuseColor == null )
                {
                    return false;
                }
            }

            //Get surface specular color if need to
            if ( ( trackVertexColorType & TrackVertexColor.Specular ) == 0 )
            {
                surfaceSpecularColor =
                    psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceSpecularColor, 0 );
                if ( surfaceSpecularColor == null )
                {
                    return false;
                }
            }

            //Get surface emissive color if need to
            if ( ( trackVertexColorType & TrackVertexColor.Emissive ) == 0 )
            {
                surfaceEmissiveColor =
                    psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.SurfaceEmissiveColor, 0 );
                if ( surfaceEmissiveColor == null )
                {
                    return false;
                }
            }

            //Get derived scene color
            derivedSceneColor =
                psProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.DerivedSceneColor, 0 );
            if ( derivedSceneColor == null )
            {
                return false;
            }

            //Get surface shininess.
            surfaceShininess = psProgram.ResolveAutoParameterInt(
                GpuProgramParameters.AutoConstantType.SurfaceShininess, 0 );
            if ( surfaceShininess == null )
            {
                return false;
            }

            //Resolve input vertex shader normal
            vsInNormal = vsMain.ResolveInputParameter( Parameter.SemanticType.Normal, 0,
                                                       Parameter.ContentType.NormalObjectSpace,
                                                       Axiom.Graphics.GpuProgramParameters.GpuConstantType.Float3 );
            if ( vsInNormal == null )
            {
                return false;
            }

            //Resolve input vertex shader tangent
            if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
            {
                vsInTangent = vsMain.ResolveInputParameter( Parameter.SemanticType.Tangent, 0,
                                                            Parameter.ContentType.TangentObjectSpace,
                                                            GpuProgramParameters.GpuConstantType.Float3 );
                if ( vsInTangent == null )
                {
                    return false;
                }

                //Resolve local vertex shader TNB matrix
                vsTBNMatrix = vsMain.ResolveLocalParameter( Parameter.SemanticType.Unknown, 0, "lMatTBN",
                                                            GpuProgramParameters.GpuConstantType.Matrix_3X3 );
                if ( vsTBNMatrix == null )
                {
                    return false;
                }
            }

            //resolve input vertex shader texture coordinates
            Parameter.ContentType texCoordToUse = Parameter.ContentType.TextureCoordinate0;
            switch ( vsTexCoordSetIndex )
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
                    throw new AxiomException( "vsTexCoordIndexOut of range", new ArgumentOutOfRangeException() );
            }
            vsInTexcoord = vsMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates, vsTexCoordSetIndex,
                                                         texCoordToUse, GpuProgramParameters.GpuConstantType.Float2 );
            if ( vsInTexcoord == null )
            {
                return false;
            }

            //Resolve output vertex shader texture coordinates
            vsOutTexcoord = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1, texCoordToUse,
                                                           GpuProgramParameters.GpuConstantType.Float2 );
            if ( vsOutTexcoord == null )
            {
                return false;
            }

            //resolve pixel input texture coordinates normal
            psInTexcoord = psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates, vsOutTexcoord.Index,
                                                         vsOutTexcoord.Content, vsOutTexcoord.Type );
            if ( psInTexcoord == null )
            {
                return false;
            }

            //Resolve pixel shader normal.
            if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Object )
            {
                psNormal = psMain.ResolveLocalParameter( Parameter.SemanticType.Normal, 0,
                                                         Parameter.ContentType.NormalObjectSpace,
                                                         GpuProgramParameters.GpuConstantType.Float3 );
                if ( psNormal == null )
                {
                    return false;
                }
            }
            else if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
            {
                psNormal = psMain.ResolveLocalParameter( Parameter.SemanticType.Normal, 0,
                                                         Parameter.ContentType.NormalTangentSpace,
                                                         GpuProgramParameters.GpuConstantType.Float3 );
                if ( psNormal == null )
                {
                    return false;
                }
            }

            var inputParams = psMain.InputParameters;
            var localParams = psMain.LocalParameters;

            psDiffuse = Function.GetParameterByContent( inputParams, Parameter.ContentType.ColorDiffuse,
                                                        GpuProgramParameters.GpuConstantType.Float4 );
            if ( psDiffuse == null )
            {
                psDiffuse = Function.GetParameterByContent( localParams, Parameter.ContentType.ColorDiffuse,
                                                            GpuProgramParameters.GpuConstantType.Float4 );
                if ( psDiffuse == null )
                {
                    return false;
                }
            }

            psOutDiffuse = psMain.ResolveOutputParameter( Parameter.SemanticType.Color, 0,
                                                          Parameter.ContentType.ColorDiffuse,
                                                          GpuProgramParameters.GpuConstantType.Float4 );
            if ( psOutDiffuse == null )
            {
                return false;
            }

            psTempDiffuseColor = psMain.ResolveLocalParameter( Parameter.SemanticType.Unknown, 0, "lNormalMapDiffuse",
                                                               GpuProgramParameters.GpuConstantType.Float4 );
            if ( psTempDiffuseColor == null )
            {
                return false;
            }

            if ( specularEnabled )
            {
                psSpecular = Function.GetParameterByContent( inputParams, Parameter.ContentType.ColorSpecular,
                                                             GpuProgramParameters.GpuConstantType.Float4 );
                if ( psSpecular == null )
                {
                    psSpecular = Function.GetParameterByContent( localParams, Parameter.ContentType.ColorSpecular,
                                                                 GpuProgramParameters.GpuConstantType.Float4 );
                    if ( psSpecular == null )
                    {
                        return false;
                    }
                }

                psTempSpecularColor = psMain.ResolveLocalParameter( Parameter.SemanticType.Unknown, 0,
                                                                    "lNormalMapSpecular",
                                                                    GpuProgramParameters.GpuConstantType.Float4 );
                if ( psTempSpecularColor == null )
                {
                    return false;
                }

                vsInPosition = vsMain.ResolveInputParameter( Parameter.SemanticType.Position, 0,
                                                             Parameter.ContentType.PositionObjectSpace,
                                                             GpuProgramParameters.GpuConstantType.Float4 );
                if ( vsInPosition == null )
                {
                    return false;
                }
                if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
                {
                    vsOutView = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
                                                               Parameter.ContentType.PostOCameraTangentSpace,
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                    if ( vsOutView == null )
                    {
                        return false;
                    }
                }
                else if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Object )
                {
                    vsOutView = vsMain.ResolveOutputParameter( Parameter.SemanticType.TextureCoordinates, -1,
                                                               Parameter.ContentType.PostOCameraTangentSpace,
                                                               GpuProgramParameters.GpuConstantType.Float3 );
                    if ( vsOutView == null )
                    {
                        return false;
                    }
                }

                psInView = psMain.ResolveInputParameter( Parameter.SemanticType.TextureCoordinates, vsOutView.Index,
                                                         vsOutView.Content, vsOutView.Type );
                if ( psInView == null )
                {
                    return false;
                }

                //Resolve camera position world space
                camPosWorldSpace =
                    vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.CameraPosition, 0 );
                if ( camPosWorldSpace == null )
                {
                    return false;
                }

                vsLocalDir = vsMain.ResolveLocalParameter( Parameter.SemanticType.Unknown, 0, "lNormalMapTempDir",
                                                           GpuProgramParameters.GpuConstantType.Float3 );
                if ( vsLocalDir == null )
                {
                    return false;
                }

                vsWorldPosition = vsMain.ResolveLocalParameter( Parameter.SemanticType.Position, 0,
                                                                Parameter.ContentType.PositionWorldSpace,
                                                                GpuProgramParameters.GpuConstantType.Float3 );
                if ( vsWorldPosition == null )
                {
                    return false;
                }

                //Resolve world matrix
                worldMatrix = vsProgram.ResolveAutoParameterInt( GpuProgramParameters.AutoConstantType.WorldMatrix, 0 );
                if ( worldMatrix == null )
                {
                    return false;
                }

                //Resolve inverse world rotation matrix
                worldInvRotMatrix = vsProgram.ResolveParameter( GpuProgramParameters.GpuConstantType.Matrix_4X4, -1,
                                                                GpuProgramParameters.GpuParamVariability.PerObject,
                                                                "inv_world_rotation_matrix" );
                if ( worldInvRotMatrix == null )
                {
                    return false;
                }
            }


            return true;
        }

        protected override bool ResolveDependencies( ProgramSet programSet )
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;

            vsProgram.AddDependency( FFPRenderState.FFPLibCommon );
            vsProgram.AddDependency( SGXLibNormalMapLighting );

            psProgram.AddDependency( FFPRenderState.FFPLibCommon );
            psProgram.AddDependency( SGXLibNormalMapLighting );

            return true;
        }

        protected override bool AddFunctionInvocations( ProgramSet programSet )
        {
            Program vsProgram = programSet.CpuVertexProgram;
            Program psProgram = programSet.CpuFragmentProgram;
            Function vsMain = vsProgram.EntryPointFunction;
            Function psMain = psProgram.EntryPointFunction;

            int internalCounter = 0;

            //Add the global illumination functions.
            if ( AddVSInvocation( vsMain, (int)FFPRenderState.FFPVertexShaderStage.VSLighting, internalCounter ) ==
                 false )
            {
                return false;
            }

            internalCounter = 0;

            //Add the normal fetch function invocation.
            if ( AddPSNormalFetchInvocation( psMain, -1 + 1, ref internalCounter ) == false )
            {
                return false;
            }

            //Add the global illuminatin functions
            if ( AddPSGlobalIlluminationInvocation( psMain, -1 + 1, ref internalCounter ) == false )
            {
                return false;
            }

            //Add per light functions
            for ( int i = 0; i < lightParamsList.Count; i++ )
            {
                if ( AddPSIlluminationInvocation( lightParamsList[ i ], psMain, -1 + 1, ref internalCounter ) == false )
                {
                    return false;
                }
            }

            //Assign back temporary variables to the ps diffuse and specular components.
            if ( AddPSFinalAssignmentInvocation( psMain, -1 + 1, ref internalCounter ) == false )
            {
                return false;
            }

            return true;
        }

        public override bool PreAddToRenderState( TargetRenderState targetRenderState, Pass srcPass, Pass dstPass )
        {
            if ( srcPass.LightingEnabled == false )
            {
                return false;
            }

            var lightCount = new int[3];

            targetRenderState.GetLightCount( out lightCount );

            TextureUnitState normalMapTexture = dstPass.CreateTextureUnitState();

            normalMapTexture.SetTextureName( normalMapTextureName );
            normalMapTexture.SetTextureFiltering( normalMapMinFilter, normalMapMagFilter, normalMapMipfilter );
            normalMapTexture.TextureAnisotropy = (int)normalMapAnisotropy;
            normalMapTexture.TextureMipmapBias = normalMapMipBias;
            normalMapSamplerIndex = dstPass.TextureUnitStatesCount - 1;

            SetTrackVertexColorType( srcPass.VertexColorTracking );

            if ( srcPass.Shininess > 0 && srcPass.Specular != ColorEx.Black )
            {
                SpecularEnable = true;
            }
            else
            {
                SpecularEnable = false;
            }

            //Case this pass should run once per light(s)
            if ( srcPass.IteratePerLight )
            {
                //This is the preferred case -> only one type of light is handled
                if ( srcPass.RunOnlyOncePerLightType )
                {
                    if ( srcPass.OnlyLightType == LightType.Point )
                    {
                        lightCount[ 0 ] = srcPass.LightsPerIteration;
                        lightCount[ 1 ] = 0;
                        lightCount[ 2 ] = 0;
                    }
                    else if ( srcPass.OnlyLightType == LightType.Directional )
                    {
                        lightCount[ 0 ] = 0;
                        lightCount[ 1 ] = srcPass.LightsPerIteration;
                        lightCount[ 2 ] = 0;
                    }
                    else if ( srcPass.OnlyLightType == LightType.Spotlight )
                    {
                        lightCount[ 0 ] = 0;
                        lightCount[ 1 ] = 0;
                        lightCount[ 2 ] = srcPass.LightsPerIteration;
                    }
                }

                    //This is wors case -> all light types epected to be handled.
                    //Can not handle this request in efficient way - throw an exception
                else
                {
                    throw new AxiomException(
                        "Using iterative lighting method with RT Shader System requires specifying explicit light type." );
                }
            }

            SetLightCount( lightCount );
            return true;
        }

        private void SetTrackVertexColorType( TrackVertexColor trackVertexColor )
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Protected Methods

        protected bool AddVSInvocation( Function vsMain, int groupOrder, int internalCounter )
        {
            FunctionInvocation curFuncInvocation = null;


            //Construct TNB matrix.
            if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
            {
                curFuncInvocation = new FunctionInvocation( SGXFuncConstructTbnMatrix, groupOrder, internalCounter++ );
                curFuncInvocation.PushOperand( vsInNormal, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( vsInTangent, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( vsTBNMatrix, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );
            }

            //Output texture coordinates
            curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder, internalCounter++ );
            curFuncInvocation.PushOperand( vsInTexcoord, Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( vsOutTexcoord, Operand.OpSemantic.Out );
            vsMain.AddAtomInstance( curFuncInvocation );

            //computer world space position
            if ( vsWorldPosition != null )
            {
                curFuncInvocation = new FunctionInvocation( SGXFuncTransformPosition, groupOrder, internalCounter++ );
                curFuncInvocation.PushOperand( worldMatrix, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( vsInPosition, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( vsWorldPosition, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );
            }

            //compute the view vector
            if ( vsInPosition != null && vsOutView != null )
            {
                //View vector in world space

                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncSubtract, groupOrder,
                                                            internalCounter++ );
                curFuncInvocation.PushOperand( camPosWorldSpace, Operand.OpSemantic.In,
                                               ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z ) );
                curFuncInvocation.PushOperand( vsWorldPosition, Operand.OpSemantic.In,
                                               ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z ) );
                curFuncInvocation.PushOperand( vsLocalDir, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );

                //Transform to object space.
                curFuncInvocation = new FunctionInvocation( SGXFuncTranformNormal, groupOrder, internalCounter++ );
                curFuncInvocation.PushOperand( worldInvRotMatrix, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( vsLocalDir, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( vsLocalDir, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );

                //Transform to tangent space
                if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
                {
                    curFuncInvocation = new FunctionInvocation( SGXFuncTranformNormal, groupOrder, internalCounter++ );
                    curFuncInvocation.PushOperand( vsTBNMatrix, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( vsLocalDir, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( vsOutView, Operand.OpSemantic.Out );
                    vsMain.AddAtomInstance( curFuncInvocation );
                }
                    //output object space
                else if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Object )
                {
                    curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++ );
                    curFuncInvocation.PushOperand( vsLocalDir, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( vsOutView, Operand.OpSemantic.Out );
                    vsMain.AddAtomInstance( curFuncInvocation );
                }
            }

            //Add per light functions
            for ( int i = 0; i < lightParamsList.Count; i++ )
            {
                if ( AddVSIlluminationInvocation( lightParamsList[ i ], vsMain, groupOrder, ref internalCounter ) ==
                     false )
                {
                    return false;
                }
            }
            return true;
        }

        internal bool AddVSIlluminationInvocation( LightParams curLightParams, Function vsMain, int groupOrder,
                                                   ref int internalCounter )
        {
            FunctionInvocation curFuncInvocation = null;

            //Computer light direction in texture space.
            if ( curLightParams.Direction != null && curLightParams.VSOutDirection != null )
            {
                //Transform to texture space.
                if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
                {
                    curFuncInvocation = new FunctionInvocation( SGXFuncTranformNormal, groupOrder, internalCounter++ );
                    curFuncInvocation.PushOperand( vsTBNMatrix, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( curLightParams.Direction, Operand.OpSemantic.In,
                                                   ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                     (int)Operand.OpMask.Z ) );
                    curFuncInvocation.PushOperand( curLightParams.VSOutDirection, Operand.OpSemantic.Out );
                    vsMain.AddAtomInstance( curFuncInvocation );
                }
                    //Output object space
                else if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Object )
                {
                    curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++ );
                    curFuncInvocation.PushOperand( curLightParams.Direction, Operand.OpSemantic.In,
                                                   ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                     (int)Operand.OpMask.Z ) );
                    curFuncInvocation.PushOperand( curLightParams.VSOutDirection, Operand.OpSemantic.Out );
                    vsMain.AddAtomInstance( curFuncInvocation );
                }
            }

            //Transform light vector to target space
            if ( curLightParams.Position != null && curLightParams.VSOutToLightDir != null )
            {
                //Compute light vector.
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncSubtract, groupOrder,
                                                            internalCounter++ );
                curFuncInvocation.PushOperand( curLightParams.Position, Operand.OpSemantic.In,
                                               ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z ) );
                curFuncInvocation.PushOperand( vsWorldPosition, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( vsLocalDir, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );

                //Transform to object space
                curFuncInvocation = new FunctionInvocation( SGXFuncTranformNormal, groupOrder, internalCounter++ );
                curFuncInvocation.PushOperand( worldInvRotMatrix, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( vsLocalDir, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( vsLocalDir, Operand.OpSemantic.Out );
                vsMain.AddAtomInstance( curFuncInvocation );

                if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Tangent )
                {
                    curFuncInvocation = new FunctionInvocation( SGXFuncTranformNormal, groupOrder, internalCounter++ );
                    curFuncInvocation.PushOperand( vsTBNMatrix, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( vsLocalDir, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( curLightParams.VSOutToLightDir, Operand.OpSemantic.Out );
                    vsMain.AddAtomInstance( curFuncInvocation );
                }
                    //Output object space
                else if ( normalMapSpace == RTShaderSystem.NormalMapSpace.Object )
                {
                    curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCounter++ );
                    curFuncInvocation.PushOperand( vsLocalDir, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( curLightParams.VSOutToLightDir, Operand.OpSemantic.Out );
                    vsMain.AddAtomInstance( curFuncInvocation );
                }
            }

            return true;
        }

        protected bool AddPSNormalFetchInvocation( Function psMain, int groupOrder, ref int internalCounter )
        {
            FunctionInvocation curFuncInvocation = null;
            curFuncInvocation = new FunctionInvocation( SGXFuncFetchNormal, groupOrder, internalCounter++ );
            curFuncInvocation.PushOperand( normalMapSampler, Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( psInTexcoord, Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( psNormal, Operand.OpSemantic.Out );
            psMain.AddAtomInstance( curFuncInvocation );

            return true;
        }

        protected bool AddPSGlobalIlluminationInvocation( Function psMain, int groupOrder, ref int internalCount )
        {
            FunctionInvocation curFuncInvocation = null;

            if ( ( trackVertexColorType & TrackVertexColor.Ambient ) == 0 &&
                 ( trackVertexColorType & TrackVertexColor.Emissive ) == 0 )
            {
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder, internalCount++ );
                curFuncInvocation.PushOperand( derivedSceneColor, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out );
                psMain.AddAtomInstance( curFuncInvocation );
            }
            else
            {
                if ( ( trackVertexColorType & TrackVertexColor.Ambient ) != 0 )
                {
                    curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate, groupOrder,
                                                                internalCount++ );
                    curFuncInvocation.PushOperand( lightAmbientColor, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( psDiffuse, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out );
                    psMain.AddAtomInstance( curFuncInvocation );
                }
                else
                {
                    curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder,
                                                                internalCount++ );
                    curFuncInvocation.PushOperand( derivedAmbientLightColor, Operand.OpSemantic.In,
                                                   ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                     (int)Operand.OpMask.Z ) );
                    curFuncInvocation.PushOperand( psDiffuse, Operand.OpSemantic.In,
                                                   ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                     (int)Operand.OpMask.Z ) );
                    curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out );
                    psMain.AddAtomInstance( curFuncInvocation );
                }

                if ( ( trackVertexColorType & TrackVertexColor.Emissive ) != 0 )
                {
                    curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAdd, groupOrder, internalCount++ );
                    curFuncInvocation.PushOperand( psDiffuse, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out );
                    psMain.AddAtomInstance( curFuncInvocation );
                }
                else
                {
                    curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAdd, groupOrder, internalCount++ );
                    curFuncInvocation.PushOperand( surfaceEmissiveColor, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.In );
                    curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out );
                    psMain.AddAtomInstance( curFuncInvocation );
                }
            }
            if ( specularEnabled )
            {
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign, groupOrder, internalCount++ );
                curFuncInvocation.PushOperand( psSpecular, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( psTempSpecularColor, Operand.OpSemantic.Out );
                psMain.AddAtomInstance( curFuncInvocation );
            }
            return true;
        }

        protected bool AddPSIlluminationInvocation( LightParams curLightParams, Function psMain, int groupOrder,
                                                    ref int internalCounter )
        {
            FunctionInvocation curFuncInvocation = null;
            //Merge diffuse color with vertex color if need to
            if ( ( trackVertexColorType & TrackVertexColor.Diffuse ) != 0 )
            {
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate, groupOrder,
                                                            internalCounter++ );
                curFuncInvocation.PushOperand( psDiffuse, Operand.OpSemantic.In,
                                               ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z ) );
                curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                               ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z ) );
                curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.Out,
                                               ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z ) );
                psMain.AddAtomInstance( curFuncInvocation );
            }

            //Merge specular color with vertex color if need to
            if ( specularEnabled && ( trackVertexColorType & TrackVertexColor.Specular ) != 0 )
            {
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncModulate, groupOrder,
                                                            internalCounter++ );
                curFuncInvocation.PushOperand( psDiffuse, Operand.OpSemantic.In,
                                               ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z ) );
                curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
                                               ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z ) );
                curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.Out,
                                               ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z ) );
                psMain.AddAtomInstance( curFuncInvocation );
            }

            switch ( curLightParams.Type )
            {
                case LightType.Directional:
                    if ( specularEnabled )
                    {
                        curFuncInvocation = new FunctionInvocation( SGXFuncLightDirectionDiffuseSpecular, groupOrder,
                                                                    internalCounter++ );
                        curFuncInvocation.PushOperand( psNormal, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psInView, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.PSInDirection, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( surfaceShininess, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempSpecularColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempSpecularColor, Operand.OpSemantic.Out,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        psMain.AddAtomInstance( curFuncInvocation );
                    }
                    else
                    {
                        curFuncInvocation = new FunctionInvocation( SGXFuncLightDirectionalDiffuse, groupOrder,
                                                                    internalCounter++ );
                        curFuncInvocation.PushOperand( psNormal, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.PSInDirection, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        psMain.AddAtomInstance( curFuncInvocation );
                    }
                    break;
                case LightType.Point:
                    if ( specularEnabled )
                    {
                        curFuncInvocation = new FunctionInvocation( SGXFuncLightPointDiffuseSpecular, groupOrder,
                                                                    internalCounter++ );
                        curFuncInvocation.PushOperand( psNormal, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( psInView, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.PSInToLightDir, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( surfaceShininess, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempSpecularColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempSpecularColor, Operand.OpSemantic.Out,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        psMain.AddAtomInstance( curFuncInvocation );
                    }
                    else
                    {
                        curFuncInvocation = new FunctionInvocation( SGXFuncLightPointDiffuse, groupOrder,
                                                                    internalCounter++ );
                        curFuncInvocation.PushOperand( psNormal, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.PSInToLightDir, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        psMain.AddAtomInstance( curFuncInvocation );
                    }
                    break;
                case LightType.Spotlight:
                    if ( specularEnabled )
                    {
                        curFuncInvocation = new FunctionInvocation( SGXFuncLightSpotDiffuseSpecular, groupOrder,
                                                                    internalCounter++ );
                        curFuncInvocation.PushOperand( psNormal, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( psInView, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.PSInToLightDir, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.PSInDirection, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.SpotParams, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.SpecularColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( surfaceShininess, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempSpecularColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempSpecularColor, Operand.OpSemantic.Out,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        psMain.AddAtomInstance( curFuncInvocation );
                    }
                    else
                    {
                        curFuncInvocation = new FunctionInvocation( SGXFuncLightSpotDiffuse, groupOrder,
                                                                    internalCounter++ );
                        curFuncInvocation.PushOperand( psNormal, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.PSInToLightDir, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.PSInDirection, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( curLightParams.AttenuatParams, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.SpotParams, Operand.OpSemantic.In );
                        curFuncInvocation.PushOperand( curLightParams.DiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.In,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.Out,
                                                       ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y |
                                                         (int)Operand.OpMask.Z ) );
                        psMain.AddAtomInstance( curFuncInvocation );
                    }
                    break;
            }
            return true;
        }

        protected bool AddPSFinalAssignmentInvocation( Function psMain, int groupOrder, ref int internalCounter )
        {
            var curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
                                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
                                                            internalCounter++ );
            curFuncInvocation.PushOperand( psTempDiffuseColor, Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( psDiffuse, Operand.OpSemantic.Out );
            psMain.AddAtomInstance( curFuncInvocation );

            curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
                                                        (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
                                                        internalCounter++ );
            curFuncInvocation.PushOperand( psDiffuse, Operand.OpSemantic.In );
            curFuncInvocation.PushOperand( psOutDiffuse, Operand.OpSemantic.Out );
            psMain.AddAtomInstance( curFuncInvocation );

            if ( specularEnabled )
            {
                curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
                                                            (int)FFPRenderState.FFPFragmentShaderStage.PSColorBegin + 1,
                                                            internalCounter++ );
                curFuncInvocation.PushOperand( psTempSpecularColor, Operand.OpSemantic.In );
                curFuncInvocation.PushOperand( psSpecular, Operand.OpSemantic.Out );
                psMain.AddAtomInstance( curFuncInvocation );
            }


            return true;
        }

        #endregion

        #region Properties

        public int TexCoordIndex
        {
            get
            {
                return vsTexCoordSetIndex;
            }
            set
            {
                vsTexCoordSetIndex = value;
            }
        }

        public NormalMapSpace NormalMapSpace
        {
            get
            {
                return normalMapSpace;
            }
            set
            {
                normalMapSpace = value;
            }
        }

        public string NormalMapTextureName
        {
            get
            {
                return normalMapTextureName;
            }
            set
            {
                normalMapTextureName = value;
            }
        }

        public uint NormalMapAnisotropy
        {
            get
            {
                return normalMapAnisotropy;
            }
            set
            {
                normalMapAnisotropy = value;
            }
        }

        public Real NormalMapMipBias
        {
            get
            {
                return normalMapMipBias;
            }
        }

        public TrackVertexColor TrackVertexColorType
        {
            get
            {
                return trackVertexColorType;
            }
            set
            {
                trackVertexColorType = value;
            }
        }

        public bool SpecularEnable
        {
            get
            {
                return specularEnabled;
            }
            set
            {
                specularEnabled = value;
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