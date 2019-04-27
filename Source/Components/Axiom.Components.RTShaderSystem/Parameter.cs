using System.Collections.Generic;
using System.Linq;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
    public class Parameter
    {
        public static Dictionary<Axiom.Graphics.GpuProgramParameters.AutoConstantType, AutoShaderParameter>
            AutoParameters
        {
            get
            {
                var retVal = new Dictionary<Graphics.GpuProgramParameters.AutoConstantType, AutoShaderParameter>();

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.WorldMatrix,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.WorldMatrix,
                                                     "world_matrix",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseWorldMatrix,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.InverseWorldMatrix,
                                                     "inverse_world_matrix",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TransposeWorldMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.TransposeWorldMatrix,
                                "transpose_world_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeWorldMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeWorldMatrix,
                                "inverse_transpose_world_matrix",
                                Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.WorldMatrixArray3x4,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.WorldMatrixArray3x4,
                                "world_matrix_array_3x4", Graphics.GpuProgramParameters.GpuConstantType.Matrix_3X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.WorldMatrixArray,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.WorldMatrixArray,
                                                     "world_matrix_array",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                //retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.WorldDualQuaternionArray2x4, new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.WorldDualQuaternionArray2x4, "world_dualquaternion_array_2x4", Graphics.GpuProgramParameters.GpuConstantType.Matrix_2X4));
                //retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.WorldScaleShearMatrixArray3x4, new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.WorldScaleShearMatrixArray3x4, "world_scale_shear_matrix_array_3x4", Graphics.GpuProgramParameters.GpuConstantType.Matrix_3X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ViewMatrix,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.ViewMatrix,
                                                     "view_matrix",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseViewMatrix,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.InverseViewMatrix,
                                                     "inverse_view_matrix",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeViewMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeViewMatrix,
                                "inverse_transpose_view_matrix",
                                Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ProjectionMatrix,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.ProjectionMatrix,
                                                     "projection_matrix",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseProjectionMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseProjectionMatrix,
                                "inverse_projection_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TransposeProjectionMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.TransposeProjectionMatrix,
                                "transpose_projection_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeProjectionMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeProjectionMatrix,
                                "inverse_transpose_projection_matrix",
                                Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ViewProjMatrix,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.ViewProjMatrix,
                                                     "viewproj_matrix",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseViewProjMatrix,
                                "inverse_viewproj_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TransposeViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.TransposeViewProjMatrix,
                                "transpose_viewproj_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeViewProjMatrix,
                                "inverse_transpose_viewproj_matrix",
                                Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.WorldViewMatrix,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.WorldViewMatrix,
                                                     "worldview_matrix",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseWorldViewMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseWorldViewMatrix,
                                "inverse_worldview_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TransposeWorldViewMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.TransposeWorldViewMatrix,
                                "transpose_worldview_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeWorldViewMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeWorldViewMatrix,
                                "inverse_transpose_worldview_matrix",
                                Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.WorldViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.WorldViewProjMatrix,
                                "worldviewproj_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseWorldViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseWorldViewProjMatrix,
                                "inverse_worldviewproj_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TransposeWorldViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.TransposeWorldViewProjMatrix,
                                "transpose_worldviewproj_matrix",
                                Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeWorldViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseTransposeWorldViewProjMatrix,
                                "inverse_transpose_worldviewproj_matrix",
                                Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.RenderTargetFlipping,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.RenderTargetFlipping,
                                "render_target_flipping", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.VertexWinding,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.VertexWinding,
                                                     "vertex_winding",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.FogColor,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.FogColor,
                                                     "fog_color", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.FogParams,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.FogParams,
                                                     "fog_params", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SurfaceAmbientColor,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.SurfaceAmbientColor,
                                "surface_ambient_color", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SurfaceDiffuseColor,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.SurfaceDiffuseColor,
                                "surface_diffuse_color", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SurfaceSpecularColor,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.SurfaceSpecularColor,
                                "surface_specular_color", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SurfaceEmissiveColor,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.SurfaceEmissiveColor,
                                "surface_emissive_color", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SurfaceShininess,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.SurfaceShininess,
                                                     "surface_shininess",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightCount,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LightCount,
                                                     "light_count", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.AmbientLightColor,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.AmbientLightColor,
                                                     "ambient_light_color",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDiffuseColor,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LightDiffuseColor,
                                                     "light_diffuse_color",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightSpecularColor,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LightSpecularColor,
                                                     "light_specular_color",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightAttenuation,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LightAttenuation,
                                                     "light_attenuation",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SpotLightParams,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.SpotLightParams,
                                                     "spotlight_params",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightPosition,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LightPosition,
                                                     "light_position",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightPositionObjectSpace,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightPositionObjectSpace,
                                "light_position_object_space", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightPositionViewSpace,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightPositionViewSpace,
                                "light_position_view_space", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDirection,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LightDirection,
                                                     "light_direction",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDirectionViewSpace,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightDirectionViewSpace,
                                "light_direction_view_space", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDirectionObjectSpace,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightDirectionObjectSpace,
                                "light_direction_object_space", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightPowerScale,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LightPowerScale,
                                                     "light_power", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDiffuseColorPowerScaled,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightDiffuseColorPowerScaled,
                                "light_diffuse_color_power_scaled", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightSpecularColorPowerScaled,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightSpecularColorPowerScaled,
                                "light_specular_color_power_scaled",
                                Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDiffuseColorArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightDiffuseColorArray,
                                "light_diffuse_color_array", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightSpecularColorArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightSpecularColorArray,
                                "light_specular_color_array", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDiffuseColorPowerScaledArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightDiffuseColorPowerScaledArray,
                                "light_diffuse_color_power_scaled_array",
                                Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightSpecularColorPowerScaledArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightSpecularColorPowerScaledArray,
                                "light_specular_color_power_scaled_array",
                                Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightAttenuationArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightAttenuationArray,
                                "light_attenuation_array", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightPositionArray,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LightPositionArray,
                                                     "light_position_array",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightPositionObjectSpaceArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightPositionObjectSpaceArray,
                                "light_position_object_space_array",
                                Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightPositionViewSpaceArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightPositionViewSpaceArray,
                                "light_position_view_space_array", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDirectionArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightDirectionArray,
                                "light_direction_array", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDirectionObjectSpace,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightDirectionObjectSpace,
                                "light_direction_object_space_array",
                                Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDirectionViewSpaceArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightDirectionViewSpaceArray,
                                "light_direction_view_space_array", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightDistanceObjectSpaceArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightDistanceObjectSpaceArray,
                                "light_distance_object_space_array",
                                Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightPowerScaleArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LightPowerScaleArray, "light_power_array",
                                Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SpotLightParamsArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.SpotLightParamsArray,
                                "spotlight_params_array", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.DerivedAmbientLightColor,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.DerivedAmbientLightColor,
                                "derived_ambient_light_color", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.DerivedSceneColor,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.DerivedSceneColor,
                                                     "derived_scene_color",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.DerivedLightDiffuseColor,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.DerivedLightDiffuseColor,
                                "derived_light_diffuse_color", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.DerivedLightSpecularColor,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.DerivedLightSpecularColor,
                                "derived_light_specular_color", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.DerivedLightDiffuseColorArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.DerivedLightDiffuseColorArray,
                                "derived_light_diffuse_color_array",
                                Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.DerivedLightSpecularColorArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.DerivedLightSpecularColorArray,
                                "derived_light_specular_color_array",
                                Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightCount,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LightNumber,
                                                     "light_number",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                ;
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LightCastsShadows,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LightCastsShadows,
                                                     "light_casts_shadows",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ShadowExtrusionDistance,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.ShadowExtrusionDistance,
                                "shadow_extrusion_distance", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.CameraPosition,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.CameraPosition,
                                                     "camera_position",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float3));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.CameraPositionObjectSpace,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.CameraPositionObjectSpace,
                                "camera_position_object_space", Graphics.GpuProgramParameters.GpuConstantType.Float3));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TextureViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.TextureViewProjMatrix,
                                "texture_viewproj_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TextureViewProjMatrixArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.TextureViewProjMatrixArray,
                                "texture_viewproj_matrix_array",
                                Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TextureWorldViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.TextureWorldViewProjMatrix,
                                "texture_worldviewproj_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TextureWorldViewProjMatrixArray,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.TextureWorldViewProjMatrixArray,
                                "texture_worldviewproj_matrix_array",
                                Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SpotLightViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.SpotLightViewProjMatrix,
                                "spotlight_viewproj_matrix", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                //retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SpotLightViewProjMatrixArray, new AutoShaderParameter( Graphics.GpuProgramParameters.AutoConstantType.SpotLightWorldViewProjMatrixArray, "spotlight_viewproj_matrix_array", Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SpotLightWorldViewProjMatrix,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.SpotLightWorldViewProjMatrix,
                                "spotlight_worldviewproj_matrix",
                                Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Custom,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.Custom, "custom",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                //OGRE NOTE: ***needs to be tested
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Time,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.Time, "time",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Time_0_X,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.Time_0_X, "time_0_x",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.CosTime_0_X,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.CosTime_0_X,
                                                     "costime_0_x", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SinTime_0_X,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.SinTime_0_X,
                                                     "sintime_0_x", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TanTime_0_X,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.TanTime_0_X,
                                                     "tantime_0_x", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Time_0_X_Packed,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.Time_0_X_Packed,
                                                     "time_0_x_packed",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Time_0_1,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.Time_0_1, "time_0_1",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.CosTime_0_1,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.CosTime_0_1,
                                                     "costime_0_1", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SinTime_0_1,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.SinTime_0_1,
                                                     "sintime_0_1", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TanTime_0_1,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.TanTime_0_1,
                                                     "tantime_0_1", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Time_0_1_Packed,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.Time_0_1_Packed,
                                                     "time_0_1_packed",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Time_0_2PI,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.Time_0_2PI,
                                                     "time_0_2pi", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Time_0_2PI,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.CosTime_0_2PI,
                                                     "costime_0_2pi",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Time_0_2PI,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.SinTime_0_2PI,
                                                     "sintime_0_2pi",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Time_0_2PI,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.TanTime_0_2PI,
                                                     "tantime_0_2pi",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.Time_0_2PI_Packed,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.Time_0_2PI_Packed,
                                                     "time_0_2pi_packed",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.FrameTime,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.FrameTime,
                                                     "frame_time", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.FPS,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.FPS, "fps",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ViewportWidth,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.ViewportWidth,
                                                     "viewport_width",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ViewportHeight,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.ViewportHeight,
                                                     "viewport_height",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseViewportWidth,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseViewportWidth,
                                "inverse_viewport_width", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseViewportHeight,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.InverseViewportHeight,
                                "inverse_viewport_height", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ViewportSize,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.ViewportSize,
                                                     "viewport_size",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ViewDirection,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.ViewDirection,
                                                     "view_direction",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float3));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ViewSideVector,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.ViewSideVector,
                                                     "view_side_vector",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float3));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ViewUpVector,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.ViewUpVector,
                                                     "view_up_vector",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float3));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.FOV,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.FOV, "fov",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.NearClipDistance,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.NearClipDistance,
                                                     "near_clip_distance",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.FarClipDistance,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.FarClipDistance,
                                                     "far_clip_distance",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float1));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.PassNumber,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.PassNumber,
                                                     "pass_number", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.PassIterationNumber,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.PassIterationNumber,
                                "pass_iteration_number", Graphics.GpuProgramParameters.GpuConstantType.Float1));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.AnimationParametric,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.AnimationParametric,
                                "animation_parametric", Graphics.GpuProgramParameters.GpuConstantType.Float4));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TexelOffsets,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.TexelOffsets,
                                                     "texel_offsets",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.SceneDepthRange,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.SceneDepthRange,
                                                     "scene_depth_range",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ShadowSceneDepthRange,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.ShadowSceneDepthRange,
                                "shadow_scene_depth_range", Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.ShadowColor,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.ShadowColor,
                                                     "shadow_color",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TextureSize,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.TextureSize,
                                                     "texture_size",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.InverseTextureSize,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.InverseTextureSize,
                                                     "inverse_texture_size",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.PackedTextureSize,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.PackedTextureSize,
                                                     "packed_texture_size",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float4));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.TextureMatrix,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.TextureMatrix,
                                                     "texture_matrix",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Matrix_4X4));

                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LODCameraPosition,
                            new AutoShaderParameter(Graphics.GpuProgramParameters.AutoConstantType.LODCameraPosition,
                                                     "lod_camera_position",
                                                     Graphics.GpuProgramParameters.GpuConstantType.Float3));
                retVal.Add(Graphics.GpuProgramParameters.AutoConstantType.LODCameraPositionObjectSpace,
                            new AutoShaderParameter(
                                Graphics.GpuProgramParameters.AutoConstantType.LODCameraPositionObjectSpace,
                                "lod_camera_position_object_space", Graphics.GpuProgramParameters.GpuConstantType.Float3));

                return retVal;
            }
        }

        public enum SemanticType : int
        {
            Unknown = 0,
            Position = 1,
            BlendWeights = 2,
            BlendIndicies = 3,
            Normal = 4,
            Color = 5,
            TextureCoordinates = 7,
            Binormal = 8,
            Tangent = 9
        }

        public enum ContentType
        {
            Unknown,
            PositionObjectSpace,
            PositionWorldSpace,
            PositionViewSpace,
            PositionProjectiveSpace,

            PositionLightSpace0,
            PositionLightSpace1,
            PositionLightSpace2,
            PositionLightSpace3,
            PositionLightSpace4,
            PositionLightSpace5,
            PositionLightSpace6,
            PositionLightSpace7,

            NormalObjectSpace,
            NormalWorldSpace,
            NormalTangentSpace,

            PostOCameraObjectSpace,
            PostOCameraWorldSpace,
            PostOCameraViewSpace,
            PostOCameraTangentSpace,

            PostoLightObjectSpace0,
            PostoLightObjectSpace1,
            PostoLightObjectSpace2,
            PostoLightObjectSpace3,
            PostoLightObjectSpace4,
            PostoLightObjectSpace5,
            PostoLightObjectSpace6,
            PostoLightObjectSpace7,

            PostoLightWorldSpace0,
            PostoLightWorldSpace1,
            PostoLightWorldSpace2,
            PostoLightWorldSpace3,
            PostoLightWorldSpace4,
            PostoLightWorldSpace5,
            PostoLightWorldSpace6,
            PostoLightWorldSpace7,

            PostoLightViewSpace0,
            PostoLightViewSpace1,
            PostoLightViewSpace2,
            PostoLightViewSpace3,
            PostoLightViewSpace4,
            PostoLightViewSpace5,
            PostoLightViewSpace6,
            PostoLightViewSpace7,

            PostoLightTangentSpace0,
            PostoLightTangentSpace1,
            PostoLightTangentSpace2,
            PostoLightTangentSpace3,
            PostoLightTangentSpace4,
            PostoLightTangentSpace5,
            PostoLightTangentSpace6,
            PostoLightTangentSpace7,

            LightDirectionObjectSpace0,
            LightDirectionObjectSpace1,
            LightDirectionObjectSpace2,
            LightDirectionObjectSpace3,
            LightDirectionObjectSpace4,
            LightDirectionObjectSpace5,
            LightDirectionObjectSpace6,
            LightDirectionObjectSpace7,

            LightDirectionWorldSpace0,
            LightDirectionWorldSpace1,
            LightDirectionWorldSpace2,
            LightDirectionWorldSpace3,
            LightDirectionWorldSpace4,
            LightDirectionWorldSpace5,
            LightDirectionWorldSpace6,
            LightDirectionWorldSpace7,

            LightDirectionViewSpace0,
            LightDirectionViewSpace1,
            LightDirectionViewSpace2,
            LightDirectionViewSpace3,
            LightDirectionViewSpace4,
            LightDirectionViewSpace5,
            LightDirectionViewSpace6,
            LightDirectionViewSpace7,

            LightDirectionTangentSpace0,
            LightDirectionTangentSpace1,
            LightDirectionTangentSpace2,
            LightDirectionTangentSpace3,
            LightDirectionTangentSpace4,
            LightDirectionTangentSpace5,
            LightDirectionTangentSpace6,
            LightDirectionTangentSpace7,

            LightPositionObjectSpace0,
            LightPositionObjectSpace1,
            LightPositionObjectSpace2,
            LightPositionObjectSpace3,
            LightPositionObjectSpace4,
            LightPositionObjectSpace5,
            LightPositionObjectSpace6,
            LightPositionObjectSpace7,

            LightPositionWorldSpace0,
            LightPositionWorldSpace1,
            LightPositionWorldSpace2,
            LightPositionWorldSpace3,
            LightPositionWorldSpace4,
            LightPositionWorldSpace5,
            LightPositionWorldSpace6,
            LightPositionWorldSpace7,

            LightPositionViewSpace0,
            LightPositionViewSpace1,
            LightPositionViewSpace2,
            LightPositionViewSpace3,
            LightPositionViewSpace4,
            LightPositionViewSpace5,
            LightPositionViewSpace6,
            LightPositionViewSpace7,

            LightPositionTangentSpace,

            BlendWeights,
            BlendIndices,

            TangentObjectSpace,
            TangentWorldSpace,
            TangentViewSpace,
            TangentTangentspace,

            BinormalObjectSpace,
            BinormalWorldSpace,
            BinormalViewSpace,
            BinormalTangentSpace,

            ColorDiffuse,
            ColorSpecular,

            DepthObjectSpace,
            DepthWorldSpace,
            DepthViewSpace,
            DepthProjectiveSpace,

            TextureCoordinate0,
            TextureCoordinate1,
            TextureCoordinate2,
            TextureCoordinate3,
            TextureCoordinate4,
            TextureCoordinate5,
            TextureCoordinate6,
            TextureCoordinate7,

            //reserved custom content range to bu used by custom shader extensions
            CustomContentBegin = 1000,
            CustomContentEnd = 2000,
            NormalViewSpace
        }

        protected string _name;
        protected Axiom.Graphics.GpuProgramParameters.GpuConstantType _type;
        protected SemanticType _semantic;
        protected int _index;
        protected ContentType _content;
        protected int _size;

        public Parameter(Axiom.Graphics.GpuProgramParameters.GpuConstantType type, string name, SemanticType semantic,
                          int index, ContentType content, int size)
        {
            this._name = name;
            this._type = type;
            this._semantic = semantic;
            this._index = index;
            this._content = content;
            this._size = size;
        }

        public Parameter()
        {
        }

        /// <summary>
        ///   Gets the name of this parameter
        /// </summary>
        public string Name
        {
            get
            {
                return this._name;
            }
        }

        /// <summary>
        ///   Gets the semantic of this parameter
        /// </summary>
        public SemanticType Semantic
        {
            get
            {
                return this._semantic;
            }
        }

        /// <summary>
        ///   Get the type of this parameter
        /// </summary>
        public Graphics.GpuProgramParameters.GpuConstantType Type
        {
            get
            {
                return this._type;
            }
        }

        /// <summary>
        ///   Gets the index of this parameter
        /// </summary>
        public int Index
        {
            get
            {
                return this._index;
            }
        }

        /// <summary>
        ///   Gets the content of this parameter
        /// </summary>
        public ContentType Content
        {
            get
            {
                return this._content;
            }
        }

        /// <summary>
        ///   Return whether this parameter is an array
        /// </summary>
        public bool IsArray
        {
            get
            {
                return this._size > 0;
            }
        }

        /// <summary>
        ///   Gets/Sets the number of elements in the parameter (for arrays)
        /// </summary>
        public int Size
        {
            get
            {
                return this._size;
            }
            set
            {
                this._size = value;
            }
        }

        /// <summary>
        ///   Returns true if this instance is a constParamater otherwise false.
        /// </summary>
        public virtual bool IsConstParameter
        {
            get
            {
                return false;
            }
        }

        public override string ToString()
        {
            return this._name;
        }
    }

    #region ConstParameter types

    internal class ConstParameter<T> : Parameter
    {
        protected T value;

        public ConstParameter(
            Axiom.Graphics.GpuProgramParameters.GpuConstantType gpuType,
            SemanticType semantic,
            ContentType content)
            : base(gpuType, "Constant", semantic, 0, content, 0)
        {
        }

        public T Value
        {
            get
            {
                return this.value;
            }
            protected set
            {
                this.value = value;
            }
        }

        public override bool IsConstParameter
        {
            get
            {
                return true;
            }
        }
    }

    internal class ConstParameterVec2 : ConstParameter<Vector2>
    {
        public ConstParameterVec2(Vector2 val, Axiom.Graphics.GpuProgramParameters.GpuConstantType type,
                                   SemanticType semantic, ContentType content)
            : base(type, semantic, content)
        {
            base.value = val;
        }

        public override string ToString()
        {
            string lang = ShaderGenerator.Instance.TargetLangauge;
            string retVal = string.Empty;

            retVal += ((lang != string.Empty) && (lang[0] == 'g') ? "vec2(" : "float2(");
            retVal += value.x.ToString() + "," + value.y.ToString() + ")";

            return retVal;
        }
    }

    internal class ConstParameterVec3 : ConstParameter<Vector3>
    {
        public ConstParameterVec3(Vector3 val, Axiom.Graphics.GpuProgramParameters.GpuConstantType type,
                                   SemanticType semantic, ContentType content)
            : base(type, semantic, content)
        {
            base.value = val;
        }

        public override string ToString()
        {
            string lang = ShaderGenerator.Instance.TargetLangauge;
            string retVal = string.Empty;

            retVal += ((lang != string.Empty) && (lang[0] == 'g') ? "vec2(" : "float2(");
            retVal += value.x.ToString() + "," + value.y.ToString() + value.z.ToString() + ")";

            return retVal;
        }
    }

    internal class ConstParameterVec4 : ConstParameter<Vector4>
    {
        public ConstParameterVec4(Vector4 val, Axiom.Graphics.GpuProgramParameters.GpuConstantType type,
                                   SemanticType semantic, ContentType content)
            : base(type, semantic, content)
        {
            base.value = val;
        }

        public override string ToString()
        {
            string lang = ShaderGenerator.Instance.TargetLangauge;
            string retVal = string.Empty;

            retVal += ((lang != string.Empty) && (lang[0] == 'g') ? "vec2(" : "float2(");
            retVal += value.x.ToString() + "," + value.y.ToString() + value.z.ToString() + value.w.ToString() + ")";

            return retVal;
        }
    }

    internal class ConstParameterFloat : ConstParameter<float>
    {
        public ConstParameterFloat(float val, Axiom.Graphics.GpuProgramParameters.GpuConstantType type,
                                    SemanticType semantic, ContentType content)
            : base(type, semantic, content)
        {
            base.value = val;
        }

        public override string ToString()
        {
            string val = value.ToString();

            if (val.Contains('.') == false)
            {
                val += ".0";
            }

            return val;
        }
    }

    internal class ConstParameterInt : ConstParameter<float>
    {
        public ConstParameterInt(int val, Axiom.Graphics.GpuProgramParameters.GpuConstantType type,
                                  SemanticType semantic, ContentType content)
            : base(type, semantic, content)
        {
            base.value = val;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    #endregion

    internal static class ParameterFactory
    {
        public static Parameter CreateInPosition(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float4, "iPos_" + index.ToString(),
                                  Parameter.SemanticType.Position, index, Parameter.ContentType.PositionObjectSpace, 0);
        }

        public static Parameter CreateOutPosition(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float4, "oPos_" + index.ToString(),
                                  Parameter.SemanticType.Position, index, Parameter.ContentType.PositionProjectiveSpace,
                                  0);
        }

        public static Parameter CreateInNormal(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float3, "iNormal_" + index.ToString(),
                                  Parameter.SemanticType.Normal, index, Parameter.ContentType.NormalObjectSpace, 0);
        }

        public static Parameter CreateInWeights(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float4,
                                  "iBlendWeights_" + index.ToString(), Parameter.SemanticType.BlendWeights, index,
                                  Parameter.ContentType.BlendWeights, 0);
        }

        public static Parameter CreateInIndices(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float4,
                                  "iBlendIndices_" + index.ToString(), Parameter.SemanticType.Binormal, index,
                                  Parameter.ContentType.BinormalObjectSpace, 0);
        }

        public static Parameter CreateOutNormal(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float3, "oNormal_" + index.ToString(),
                                  Parameter.SemanticType.Normal, index, Parameter.ContentType.NormalObjectSpace, 0);
        }

        public static Parameter CreateInBiNormal(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float3, "iBiNormal_" + index.ToString(),
                                  Parameter.SemanticType.Binormal, index, Parameter.ContentType.BinormalObjectSpace, 0);
        }

        public static Parameter CreateOutBiNormal(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float3, "oBiNormal_" + index.ToString(),
                                  Parameter.SemanticType.Binormal, index, Parameter.ContentType.BinormalObjectSpace, 0);
        }

        public static Parameter CreateInTangent(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float3, "iTangent_" + index.ToString(),
                                  Parameter.SemanticType.Tangent, index, Parameter.ContentType.TangentObjectSpace, 0);
        }

        public static Parameter CreateOutTangent(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float3, "oTangent_" + index.ToString(),
                                  Parameter.SemanticType.Tangent, index, Parameter.ContentType.TangentObjectSpace, 0);
        }

        public static Parameter CreateInColor(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float4, "iColor_" + index.ToString(),
                                  Parameter.SemanticType.Color, index,
                                  index == 0 ? Parameter.ContentType.ColorDiffuse : Parameter.ContentType.ColorSpecular,
                                  0);
        }

        public static Parameter CreateOutColor(int index)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float4, "oColor_" + index.ToString(),
                                  Parameter.SemanticType.Color, index,
                                  index == 0 ? Parameter.ContentType.ColorDiffuse : Parameter.ContentType.ColorSpecular,
                                  0);
        }

        public static Parameter CreateInTexcoord(Axiom.Graphics.GpuProgramParameters.GpuConstantType type, int index,
                                                  Parameter.ContentType content)
        {
            switch (type)
            {
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Float1:
                    return CreateInTexcoord1(index, content);
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Float2:
                    return CreateInTexcoord2(index, content);
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Float3:
                    return CreateInTexcoord3(index, content);
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Float4:
                    return CreateInTexcoord4(index, content);
                default:
                    return new Parameter();
            }
        }

        public static Parameter CreateOutTexcoord(Axiom.Graphics.GpuProgramParameters.GpuConstantType type, int index,
                                                   Parameter.ContentType content)
        {
            switch (type)
            {
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Float1:
                    return CreateOutTexcoord1(index, content);
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Float2:
                    return CreateOutTexcoord2(index, content);
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Float3:
                    return CreateOutTexcoord3(index, content);
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Float4:
                    return CreateOutTexcoord4(index, content);
                default:
                    return new Parameter();
            }
        }

        public static Parameter CreateInTexcoord1(int index, Parameter.ContentType content)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float1, "iTexcoord1_" + index.ToString(),
                                  Parameter.SemanticType.TextureCoordinates, index, content, 0);
        }

        public static Parameter CreateOutTexcoord1(int index, Parameter.ContentType content)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float1, "oTexcoord1_" + index.ToString(),
                                  Parameter.SemanticType.TextureCoordinates, index, content, 0);
        }

        public static Parameter CreateInTexcoord2(int index, Parameter.ContentType content)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float2, "iTexcoord2_" + index.ToString(),
                                  Parameter.SemanticType.TextureCoordinates, index, content, 0);
        }

        public static Parameter CreateOutTexcoord2(int index, Parameter.ContentType content)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float2, "oTexcoord2_" + index.ToString(),
                                  Parameter.SemanticType.TextureCoordinates, index, content, 0);
        }

        public static Parameter CreateInTexcoord3(int index, Parameter.ContentType content)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float3, "iTexcoord3_" + index.ToString(),
                                  Parameter.SemanticType.TextureCoordinates, index, content, 0);
        }

        public static Parameter CreateOutTexcoord3(int index, Parameter.ContentType content)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float3, "oTexcoord3_" + index.ToString(),
                                  Parameter.SemanticType.TextureCoordinates, index, content, 0);
        }

        public static Parameter CreateInTexcoord4(int index, Parameter.ContentType content)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float4, "iTexcoord4_" + index.ToString(),
                                  Parameter.SemanticType.TextureCoordinates, index, content, 0);
        }

        public static Parameter CreateOutTexcoord4(int index, Parameter.ContentType content)
        {
            return new Parameter(Graphics.GpuProgramParameters.GpuConstantType.Float4, "oTexcoord4_" + index.ToString(),
                                  Parameter.SemanticType.TextureCoordinates, index, content, 0);
        }

        public static Parameter CreateConstParamVector2(Vector2 val)
        {
            return new ConstParameterVec2(val, Graphics.GpuProgramParameters.GpuConstantType.Float2,
                                           Parameter.SemanticType.Unknown, Parameter.ContentType.Unknown);
        }

        public static Parameter CreateConstParamVector3(Vector3 val)
        {
            return new ConstParameterVec3(val, Graphics.GpuProgramParameters.GpuConstantType.Float3,
                                           Parameter.SemanticType.Unknown, Parameter.ContentType.Unknown);
        }

        public static Parameter CreateConstParamVector4(Vector4 val)
        {
            return new ConstParameterVec4(val, Graphics.GpuProgramParameters.GpuConstantType.Float4,
                                           Parameter.SemanticType.Unknown, Parameter.ContentType.Unknown);
        }

        public static Parameter CreateConstParamFloat(float val)
        {
            return new ConstParameterFloat(val, Graphics.GpuProgramParameters.GpuConstantType.Float1,
                                            Parameter.SemanticType.Unknown, Parameter.ContentType.Unknown);
        }

        public static UniformParameter CreateSampler(Axiom.Graphics.GpuProgramParameters.GpuConstantType type,
                                                      int index)
        {
            switch (type)
            {
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Sampler1D:
                    return CreateSampler1D(index);
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Sampler2D:
                    return CreateSampler2D(index);
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.Sampler3D:
                    return CreateSampler3D(index);
                case Axiom.Graphics.GpuProgramParameters.GpuConstantType.SamplerCube:
                    return CreateSamplerCUBE(index);
                default:
                    return new UniformParameter();
            }
        }

        public static UniformParameter CreateSampler1D(int index)
        {
            return new UniformParameter(Graphics.GpuProgramParameters.GpuConstantType.Sampler1D,
                                         "gSampler1D_" + index.ToString(), Parameter.SemanticType.Unknown, index,
                                         Parameter.ContentType.Unknown,
                                         (int)Axiom.Graphics.GpuProgramParameters.GpuParamVariability.Global, 1);
        }

        public static UniformParameter CreateSampler2D(int index)
        {
            return new UniformParameter(Graphics.GpuProgramParameters.GpuConstantType.Sampler2D,
                                         "gSampler2D_" + index.ToString(), Parameter.SemanticType.Unknown, index,
                                         Parameter.ContentType.Unknown,
                                         (int)Axiom.Graphics.GpuProgramParameters.GpuParamVariability.Global, 1);
        }

        public static UniformParameter CreateSampler3D(int index)
        {
            return new UniformParameter(Graphics.GpuProgramParameters.GpuConstantType.Sampler3D,
                                         "gSampler3D_" + index.ToString(), Parameter.SemanticType.Unknown, index,
                                         Parameter.ContentType.Unknown,
                                         (int)Axiom.Graphics.GpuProgramParameters.GpuParamVariability.Global, 1);
        }

        public static UniformParameter CreateSamplerCUBE(int index)
        {
            return new UniformParameter(Graphics.GpuProgramParameters.GpuConstantType.SamplerCube,
                                         "gSamplerCUBE_" + index.ToString(), Parameter.SemanticType.Unknown, index,
                                         Parameter.ContentType.Unknown,
                                         (int)Axiom.Graphics.GpuProgramParameters.GpuParamVariability.Global, 1);
        }

        public static UniformParameter CreateUniform(Axiom.Graphics.GpuProgramParameters.GpuConstantType type,
                                                      int index, int variability, string suggestedName, int size)
        {
            UniformParameter param;
            param = new UniformParameter(type, suggestedName + index.ToString(), Parameter.SemanticType.Unknown, index,
                                          Parameter.ContentType.Unknown, variability, size);
            return param;
        }
    }

    public struct AutoShaderParameter
    {
        public string Name;
        public Axiom.Graphics.GpuProgramParameters.AutoConstantType AutoType;
        public Axiom.Graphics.GpuProgramParameters.GpuConstantType Type;

        public AutoShaderParameter(Axiom.Graphics.GpuProgramParameters.AutoConstantType autoType, string name,
                                    Axiom.Graphics.GpuProgramParameters.GpuConstantType type)
        {
            this.Name = name;
            this.AutoType = autoType;
            this.Type = type;
        }
    }
}