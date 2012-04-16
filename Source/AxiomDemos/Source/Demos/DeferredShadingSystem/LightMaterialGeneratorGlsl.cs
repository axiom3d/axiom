using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Graphics;
using MaterialPermutation = System.UInt32;
using Axiom.Core;

namespace Axiom.Demos.DeferredShadingSystem
{
    class LightMaterialGeneratorGlsl : IMaterialGeneratorStrategy
    {
        #region Fields and Properties

        private string _baseName;

        #endregion Fields and Properties

        #region Construction and Destruction

        public LightMaterialGeneratorGlsl( string baseName )
        {
            this._baseName = baseName;
        }

        #endregion Construction and Destruction

        #region IMaterialGeneratorStrategy Implementation

        public GpuProgram GenerateVertexShader( MaterialPermutation permutation )
        {
            string programName;

            if ( ( permutation & (uint)MaterialId.Quad ) != 0 )
            {
                programName = "DeferredShading/post/glsl/vs";
            }
            else
            {
                programName = "DeferredShading/post/glsl/LightMaterial_vs";
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

            shader.Append( "uniform sampler2D tex0;\n" );
            shader.Append( "uniform sampler2D tex1;\n" );
            shader.Append( "varying vec2 texCoord;\n" );
            shader.Append( "varying vec3 projCoord;\n" );
            /// World view matrix to get object position in view space
            shader.Append( "uniform mat4 worldView;\n" );
            /// Attributes of light
            shader.Append( "uniform vec3 lightDiffuseColor;\n" );
            shader.Append( "uniform vec3 lightSpecularColor;\n" );
            shader.Append( "uniform vec3 lightFalloff;\n" );
            shader.Append( "void main()\n" );
            shader.Append( "{\n" );
            shader.Append( "	 vec4 a0 = texture2D(tex0, texCoord);\n" ); // Attribute 0: Diffuse color+shininess
            shader.Append( "    vec4 a1 = texture2D(tex1, texCoord);\n" ); // Attribute 1: Normal+depth
            /// Attributes
            shader.Append( "    vec3 colour = a0.rgb;\n" );
            shader.Append( "    float alpha = a0.a;\n" );		// Specularity
            shader.Append( "    float distance = a1.w;\n" );  // Distance from viewer (w)
            shader.Append( "    vec3 normal = a1.xyz;\n" );
            /// Calculate position of texel in view space
            shader.Append( "    vec3 position = projCoord*distance;\n" );
            /// Extract position in view space from worldView matrix
            shader.Append( "	 vec3 lightPos = vec3(worldView[3][0],worldView[3][1],worldView[3][2]);\n" );
            /// Calculate light direction and distance
            shader.Append( "    vec3 lightVec = lightPos - position;\n" );
            shader.Append( "    float len_sq = dot(lightVec, lightVec);\n" );
            shader.Append( "    float len = sqrt(len_sq);\n" );
            shader.Append( "    vec3 lightDir = lightVec/len;\n" );
            /// Calculate attenuation
            shader.Append( "    float attenuation = dot(lightFalloff, vec3(1, len, len_sq));\n" );
            /// Calculate diffuse colour
            shader.Append( "    vec3 light_diffuse = max(0.0,dot(lightDir, normal)) * lightDiffuseColor;\n" );
            /// Calculate specular component
            shader.Append( "    vec3 viewDir = -normalize(position);\n" );
            shader.Append( "    vec3 h = normalize(viewDir + lightDir);\n" );
            shader.Append( "    vec3 light_specular = pow(dot(normal, h),32.0) * lightSpecularColor;\n" );
            /// Calcalate total lighting for this fragment
            shader.Append( "    vec3 total_light_contrib;\n" );
            shader.Append( "    total_light_contrib = light_diffuse;\n" );
            if ( isSpecular )
            {
                shader.Append( "	 total_light_contrib += alpha * light_specular;\n" );
            }
            if ( isAttenuated )
            {
                shader.Append( "    gl_FragColor = vec4(total_light_contrib*colour/attenuation, 0);\n" );
            }
            else
            {
                shader.Append( "    gl_FragColor = vec4(total_light_contrib*colour, 0);\n" );
            }
            shader.Append( "}\n" );

            /// Create shader object
            HighLevelGpuProgram program = HighLevelGpuProgramManager.Instance.CreateProgram( name, ResourceGroupManager.DefaultResourceGroupName, "glsl", GpuProgramType.Fragment );
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

            parameters.SetNamedConstant( "tex0", 0 );
            parameters.SetNamedConstant( "tex1", 1 );

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
