using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Graphics;
using MaterialPermutation = System.UInt32;
using Axiom.Core;

namespace Axiom.Demos.DeferredShadingSystem
{
    class LightMaterialGeneratorHlsl : IMaterialGeneratorStrategy
    {
        #region Fields and Properties

        private string _baseName;

        #endregion Fields and Properties

        #region Construction and Destruction

        public LightMaterialGeneratorHlsl( string baseName )
        {
            this._baseName = baseName;
        }

        #endregion Construction and Destruction

        #region IMaterialGeneratorStrategy Implementation

        public GpuProgram GenerateVertexShader( MaterialPermutation permutation )
        {
            string programName;

            if ( ( permutation & (int)MaterialId.Quad ) != 0 )
            {
                programName = "DeferredShading/post/hlsl/vs";
            }
            else
            {
                programName = "DeferredShading/post/hlsl/LightMaterial_vs";
            }

            return (GpuProgram)HighLevelGpuProgramManager.Instance.GetByName( programName );
        }

        public GpuProgram GeneratePixelShader( MaterialPermutation permutation )
        {
            bool isAttenuated = ( ( permutation & (uint)MaterialId.Attenuated ) != 0 );
            bool isSpecular = ( ( permutation & (uint)MaterialId.Specular ) != 0 );

            /// Create name
            String name = _baseName + permutation.ToString() + "_ps";
            /// Create shader
            StringBuilder shader = new StringBuilder();
            shader.Append( "sampler Tex0: register(s0);\n" );
            shader.Append( "sampler Tex1: register(s1);\n" );
            shader.Append( "float4x4 worldView;\n" );
            // Attributes of light
            shader.Append( "float4 lightDiffuseColor;\n" );
            shader.Append( "float4 lightSpecularColor;\n" );
            shader.Append( "float4 lightFalloff;\n" );
            shader.Append( "float4 main(float2 texCoord: TEXCOORD0, float3 projCoord: TEXCOORD1) : COLOR\n" );
            shader.Append( "{\n" );
            shader.Append( "    float4 a0 = tex2D(Tex0, texCoord); \n" );// Attribute 0: Diffuse color+shininess
            shader.Append( "    float4 a1 = tex2D(Tex1, texCoord); \n" );// Attribute 1: Normal+depth
            // Attributes
            shader.Append( "    float3 colour = a0.rgb;\n" );
            shader.Append( "    float alpha = a0.a;" );		// Specularity
            shader.Append( "    float distance = a1.w;" );	// Distance from viewer (w)
            shader.Append( "    float3 normal = a1.xyz;\n" );
            // Calculate position of texel in view space
            shader.Append( "    float3 position = projCoord*distance;\n" );
            // Extract position in view space from worldView matrix
            shader.Append( "	 float3 lightPos = float3(worldView[0][3],worldView[1][3],worldView[2][3]);\n" );
            // Calculate light direction and distance
            shader.Append( "    float3 lightVec = lightPos - position;\n" );
            shader.Append( "    float len_sq = dot(lightVec, lightVec);\n" );
            shader.Append( "    float len = sqrt(len_sq);\n" );
            shader.Append( "    float3 lightDir = lightVec/len;\n" );
            /// Calculate attenuation
            shader.Append( "    float attenuation = dot(lightFalloff, float3(1, len, len_sq));\n" );
            /// Calculate diffuse colour
            shader.Append( "    float3 light_diffuse = max(0,dot(lightDir, normal)) * lightDiffuseColor;\n" );
            /// Calculate specular component
            shader.Append( "    float3 viewDir = -normalize(position);\n" );
            shader.Append( "    float3 h = normalize(viewDir + lightDir);\n" );
            shader.Append( "    float3 light_specular = pow(dot(normal, h),32) * lightSpecularColor;\n" );
            // Accumulate total lighting for this fragment
            shader.Append( "    float3 total_light_contrib;\n" );
            shader.Append( "    total_light_contrib = light_diffuse;\n" );
            if ( isSpecular )
            {
                /// Calculate specular contribution
                shader.Append( "	 total_light_contrib += alpha * light_specular;\n" );
            }
            if ( isAttenuated )
            {
                shader.Append( "    return float4(total_light_contrib*colour/attenuation, 0);\n" );
            }
            else
            {
                shader.Append( "    return float4(total_light_contrib*colour, 0);\n" );
            }
            shader.Append( "}\n" );

            /// Create shader object
            HighLevelGpuProgram program = HighLevelGpuProgramManager.Instance.CreateProgram( name, ResourceGroupManager.DefaultResourceGroupName, "hlsl", GpuProgramType.Fragment );
            program.Source = shader.ToString();
            program.SetParam( "target", "ps_2_0" );
            program.SetParam( "entry_point", "main" );
            /// Set up default parameters
            GpuProgramParameters parameters = program.DefaultParameters;
            parameters.SetNamedAutoConstant( "worldView", GpuProgramParameters.AutoConstantType.WorldViewMatrix, 0 );
            parameters.SetNamedAutoConstant( "lightDiffuseColor", GpuProgramParameters.AutoConstantType.Custom, 1 );
            if ( isSpecular )
                parameters.SetNamedAutoConstant( "lightSpecularColor", GpuProgramParameters.AutoConstantType.Custom, 2 );
            if ( isAttenuated )
                parameters.SetNamedAutoConstant( "lightFalloff", GpuProgramParameters.AutoConstantType.Custom, 3 );

            return (GpuProgram)HighLevelGpuProgramManager.Instance.GetByName( program.Name );
        }

        public Material GenerateTemplateMaterial( MaterialPermutation permutation )
        {
            string templateName;

            if ( ( permutation & (int)MaterialId.Quad ) != 0 )
            {
                templateName = "DeferredShading/LightMaterialQuad";
            }
            else
            {
                templateName = "DeferredShading/LightMaterialQuad";
            }

            return (Material)MaterialManager.Instance.GetByName( templateName );
        }

        #endregion IMaterialGeneratorStrategy Implementation
    }
}
