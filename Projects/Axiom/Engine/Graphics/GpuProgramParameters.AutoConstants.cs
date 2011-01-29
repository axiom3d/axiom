#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	partial class GpuProgramParameters
	{
		/// <summary>
		///    Parameters that are available via the engine and automatically caclulated for use in GPU programs.
		/// </summary>
		public enum AutoConstantType
		{
			/// <summary>
			///    Current world matrix.
			/// </summary>
			[ScriptEnum( "world_matrix" )]
			WorldMatrix,
            /// <summary>
            ///    Current world matrix, inverted.
            /// </summary>
            [ScriptEnum( "inverse_world_matrix" )]
            InverseWorldMatrix,
			/// <summary>
			/// Provides transpose of world matrix.
            /// Equivalent to RenderMonkey's "WorldTranspose".
			/// </summary>
			[ScriptEnum( "transpose_world_matrix" )]
			TransposeWorldMatrix,
            /// <summary>
            /// The current world matrix, inverted & transposed
            /// </summary>
            [ScriptEnum( "inverse_transpose_world_matrix" )]
            InverseTransposeWorldMatrix,



            /// <summary>
            ///    The current array of world matrices, as a 3x4 matrix, used for blending.
            /// </summary>
            [ScriptEnum( "world_matrix_array_3x4" )]
            WorldMatrixArray3x4,
            /// <summary>
            ///    The current array of world matrices, used for blending
            /// </summary>
			[ScriptEnum( "world_matrix_array" )]
			WorldMatrixArray,



			/// <summary>
			///    Current view matrix.
			/// </summary>
			[ScriptEnum( "view_matrix" )]
			ViewMatrix,
            /// <summary>
            ///    Current view matrix, inverted.
            /// </summary>
            [ScriptEnum( "inverse_view_matrix" )]
            InverseViewMatrix,
            /// <summary>
            /// Provides transpose of view matrix.
            /// Equivalent to RenderMonkey's "ViewTranspose".
            /// </summary>
            [ScriptEnum( "transpose_view_matrix" )]
            TransposeViewMatrix,
            /// <summary>
            /// Provides inverse transpose of view matrix.
            /// Equivalent to RenderMonkey's "ViewInverseTranspose".
            /// </summary>
            [ScriptEnum( "inverse_transpose_view_matrix" )]
            InverseTransposeViewMatrix,



			/// <summary>
			///    Current projection matrix.
			/// </summary>
			[ScriptEnum( "proj_matrix" )]
			ProjectionMatrix,
            /// <summary>
            /// Provides inverse of projection matrix.
            /// Equivalent to RenderMonkey's "ProjectionInverse".
            /// </summary>
            [ScriptEnum( "inverse_proj_matrix" )]
            InverseProjectionMatrix,
            /// <summary>
            /// Provides transpose of projection matrix.
            /// Equivalent to RenderMonkey's "ProjectionTranspose".
            /// </summary>
            [ScriptEnum( "transpose_proj_matrix" )]
            TransposeProjectionMatrix,
            /// <summary>
            /// Provides inverse transpose of projection matrix.
            /// Equivalent to RenderMonkey's "ProjectionInverseTranspose".
            /// </summary>
            [ScriptEnum( "inverse_transpose_proj_matrix" )]
            InverseTransposeProjectionMatrix,



			/// <summary>
			///    The current view & projection matrices concatenated.
			/// </summary>
			[ScriptEnum( "viewproj_matrix" )]
			ViewProjMatrix,
            /// <summary>
            /// Provides inverse of concatenated view and projection matrices.
            /// Equivalent to RenderMonkey's "ViewProjectionInverse".
            /// </summary>
            [ScriptEnum( "inverse_viewproj_matrix" )]
            InverseViewProjMatrix,
            /// <summary>
            /// Provides transpose of concatenated view and projection matrices.
            /// Equivalent to RenderMonkey's "ViewProjectionTranspose".
            /// </summary>
            [ScriptEnum( "transpose_viewproj_matrix" )]
            TransposeViewProjMatrix,
            /// <summary>
            /// Provides inverse transpose of concatenated view and projection matrices.
            /// Equivalent to RenderMonkey's "ViewProjectionInverseTranspose".
            /// </summary>
            [ScriptEnum( "inverse_transpose_viewproj_matrix" )]
            InverseTransposeViewProjMatrix,



			/// <summary>
			///    Current world and view matrices concatenated.
			/// </summary>
			[ScriptEnum( "worldview_matrix" )]
			WorldViewMatrix,
            /// <summary>
            /// The current world & view matrices concatenated, then inverted
            /// </summary>
            [ScriptEnum("inverse_worldview_matrix")]
            InverseWorldViewMatrix,
            /// <summary>
            /// Provides transpose of concatenated world and view matrices.
            /// Equivalent to RenderMonkey's "WorldViewTranspose".
            /// </summary>
            [ScriptEnum( "transpose_worldview_matrix" )]
            TransposeWorldViewMatrix,
            /// <summary>
            /// The current world & view matrices concatenated, then inverted & transposed
            /// </summary>
            [ScriptEnum( "inverse_transpose_worldview_matrix" )]
            InverseTransposeWorldViewMatrix,



			/// <summary>
			///    Current world, view, and projection matrics concatenated.
			/// </summary>
			[ScriptEnum( "worldviewproj_matrix" )]
			WorldViewProjMatrix,
            /// <summary>
            /// Provides inverse of concatenated world, view and projection matrices.
            /// Equivalent to RenderMonkey's "WorldViewProjectionInverse".
            /// </summary>
            [ScriptEnum( "inverse_worldviewproj_matrix" )]
            InverseWorldViewProjMatrix,
            /// <summary>
            /// Provides transpose of concatenated world, view and projection matrices.
            /// Equivalent to RenderMonkey's "WorldViewProjectionTranspose".
            /// </summary>
            [ScriptEnum( "transpose_worldviewproj_matrix" )]
            TransposeWorldViewProjMatrix,
            /// <summary>
            /// Provides inverse transpose of concatenated world, view and projection matrices.
            /// Equivalent to RenderMonkey's "WorldViewProjectionInverseTranspose".
            /// </summary>
            [ScriptEnum( "inverse_transpose_worldviewproj_matrix" )]
            InverseTransposeWorldViewProjMatrix,



            /// <summary>
            /// render target related values
			/// -1 if requires texture flipping, +1 otherwise. It's useful when you bypassed
			/// projection matrix transform, still able use this value to adjust transformed y position.
            /// </summary>
            [ScriptEnum( "render_target_flipping" )]
            RenderTargetFlipping,
            
            /// <summary>
            /// -1 if the winding has been inverted (e.g. for reflections), +1 otherwise.
            /// </summary>
            [ScriptEnum("vertex_winding")]
            VertexWinding,

            /// <summary>
            /// Fog colour
            /// </summary>
            [ScriptEnum( "fog_color" )]
            FogColor,
            /// <summary>
            /// Fog params: density, linear start, linear end, 1/(end-start)
            /// </summary>
            [ScriptEnum( "fog_params" )]
            FogParams,

            /// <summary>
            /// Surface ambient colour, as set in Pass::setAmbient
            /// </summary>
            [ScriptEnum( "surface_ambient_color" )]
            SurfaceAmbientColor,
            /// <summary>
            /// Surface diffuse colour, as set in Pass::setDiffuse
            /// </summary>
            [ScriptEnum( "surface_diffuse_color" )]
            SurfaceDiffuseColor,
            /// <summary>
            /// Surface specular colour, as set in Pass::setSpecular
            /// </summary>
            [ScriptEnum( "surface_specular_color" )]
            SurfaceSpecularColor,
            /// <summary>
            /// Surface emissive colour, as set in Pass::setSelfIllumination
            /// </summary>
            [ScriptEnum( "surface_emissive_color" )]
            SurfaceEmissiveColor,
            /// <summary>
            /// Surface shininess, as set in Pass::setShininess
            /// </summary>
            [ScriptEnum( "surface_shininess" )]
            SurfaceShininess,

            
            /// <summary>
            /// The number of active light sources (better than gl_MaxLights)
            /// </summary>
            [ScriptEnum( "light_count" )]
            LightCount,

            
            
            /// <summary>
            /// The ambient light colour set in the scene
            /// </summary>
            [ScriptEnum( "ambient_light_color" )]
            AmbientLightColor,

            
            
            /// <summary>
            /// Light diffuse colour (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_diffuse_color" )]
            LightDiffuseColor,
            /// <summary>
            /// Light specular colour (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_specular_color" )]
            LightSpecularColor,
            /// <summary>
            /// Light attenuation parameters, Vector4(range, constant, linear, quadric)
            /// </summary>
            [ScriptEnum( "light_attenuation" )]
            LightAttenuation,



            /// <summary>
            /// Spotlight parameters, Vector4(innerFactor, outerFactor, falloff, isSpot)
			/// innerFactor and outerFactor are cos(angle/2)
			/// The isSpot parameter is 0.0f for non-spotlights, 1.0f for spotlights.
			/// Also for non-spotlights the inner and outer factors are 1 and nearly 1 respectively
            /// </summary>
            [ScriptEnum( "spotlight_params" )]
            SpotLightParams,



            /// <summary>
            /// A light position in world space (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_position" )]
            LightPosition,
            /// <summary>
            /// A light position in object space (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_position_object_space" )]
            LightPositionObjectSpace,
            /// <summary>
            /// A light position in view space (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_position_view_space" )]
            LightPositionViewSpace,
            /// <summary>
            /// A light direction in world space (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_direction" )]
            LightDirection,
            /// <summary>
            /// A light direction in object space (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_direction_object_space" )]
            LightDirectionObjectSpace,
            /// <summary>
            /// A light direction in view space (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_direction_view_space" )]
            LightDirectionViewSpace,
            /// <summary>
            /// The distance of the light from the center of the object
			/// a useful approximation as an alternative to per-vertex distance
			/// calculations.
            /// </summary>
            [ScriptEnum( "light_distance_object_space" )]
            LightDistanceObjectSpace,
            /// <summary>
            /// Light power level, a single scalar as set in Light::setPowerScale  (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_power_scale" )]
            LightPowerScale,
            /// <summary>
            /// Light diffuse colour pre-scaled by Light::setPowerScale (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_diffuse_color_power_scaled" )]
            LightDiffuseColorPowerScaled,
            /// <summary>
            /// Light specular colour pre-scaled by Light::setPowerScale (index determined by setAutoConstant call)
            /// </summary>
            [ScriptEnum( "light_specular_color_power_scaled" )]
            LightSpecularColorPowerScaled,
            /// <summary>
            /// Array of light diffuse colours (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_diffuse_color_array" )]
            LightDiffuseColorArray,
            /// <summary>
            /// Array of light specular colours (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_specular_color_array" )]
            LightSpecularColorArray,
            /// <summary>
            /// Array of light diffuse colours scaled by light power (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_diffuse_color_power_scaled_array" )]
            LightDiffuseColorPowerScaledArray,
            /// <summary>
            /// Array of light specular colours scaled by light power (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_specular_color_power_scaled_array" )]
            LightSpecularColorPowerScaledArray,
            /// <summary>
            /// Array of light attenuation parameters, Vector4(range, constant, linear, quadric) (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_attenuation_array" )]
            LightAttenuationArray,
            /// <summary>
            /// Array of light positions in world space (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_position_array" )]
            LightPositionArray,
            /// <summary>
            /// Array of light positions in object space (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_position_object_space_array" )]
            LightPositionObjectSpaceArray,
            /// <summary>
            /// Array of light positions in view space (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_position_view_space_array" )]
            LightPositionViewSpaceArray,
            /// <summary>
            /// Array of light directions in world space (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_direction_array" )]
            LightDirectionArray,
            /// <summary>
            /// Array of light directions in object space (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_direction_object_space" )]
            LightDirectionObjectSpaceArray,
            /// <summary>
            /// Array of light directions in view space (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_direction_view_space_array" )]
            LightDirectionViewSpaceArray,
            /// <summary>
            /// Array of distances of the lights from the center of the object
			/// a useful approximation as an alternative to per-vertex distance
			/// calculations. (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_distance_object_space_array" )]
            LightDistanceObjectSpaceArray,
            /// <summary>
            /// Array of light power levels, a single scalar as set in Light::setPowerScale 
			/// (count set by extra param)
            /// </summary>
            [ScriptEnum( "light_power_scale_array" )]
            LightPowerScaleArray,



            /// <summary>
            /// Spotlight parameters array of Vector4(innerFactor, outerFactor, falloff, isSpot)
			/// innerFactor and outerFactor are cos(angle/2)
			/// The isSpot parameter is 0.0f for non-spotlights, 1.0f for spotlights.
			/// Also for non-spotlights the inner and outer factors are 1 and nearly 1 respectively.
			/// (count set by extra param)
            /// </summary>
            [ScriptEnum( "spotlight_params_array" )]
            SpotLightParamsArray,



            /// <summary>
            /// The derived ambient light colour, with 'r', 'g', 'b' components filled with
			/// product of surface ambient colour and ambient light colour, respectively,
			/// and 'a' component filled with surface ambient alpha component.
            /// </summary>
            [ScriptEnum( "derived_ambient_light_color" )]
            DerivedAmbientLightColor,
            /// <summary>
            /// The derived scene colour, with 'r', 'g' and 'b' components filled with sum
			/// of derived ambient light colour and surface emissive colour, respectively,
			/// and 'a' component filled with surface diffuse alpha component.
            /// </summary>
            [ScriptEnum( "derived_scene_color" )]
            DerivedSceneColor,
            /// <summary>
            /// The derived light diffuse colour (index determined by setAutoConstant call),
			/// with 'r', 'g' and 'b' components filled with product of surface diffuse colour,
			/// light power scale and light diffuse colour, respectively, and 'a' component filled with surface
			/// diffuse alpha component.
            /// </summary>
            [ScriptEnum( "derived_light_diffuse_color" )]
            DerivedLightDiffuseColor,
            /// <summary>
            /// The derived light specular colour (index determined by setAutoConstant call),
			/// with 'r', 'g' and 'b' components filled with product of surface specular colour
			/// and light specular colour, respectively, and 'a' component filled with surface
			/// specular alpha component.
            /// </summary>
            [ScriptEnum( "derived_light_specular_color" )]
            DerivedLightSpecularColor,
            /// <summary>
            /// Array of derived light diffuse colours (count set by extra param)
            /// </summary>
            [ScriptEnum( "derived_light_diffuse_color_array" )]
            DerivedLightDiffuseColorArray,
            /// <summary>
            /// Array of derived light specular colours (count set by extra param)
            /// </summary>
            [ScriptEnum( "derived_light_specular_color_array" )]
            DerivedLightSpecularColorArray,



            /// <summary>
            /// The absolute light number of a local light index. Each pass may have
			/// a number of lights passed to it, and each of these lights will have
			/// an index in the overall light list, which will differ from the local
			/// light index due to factors like setStartLight and setIteratePerLight.
			/// This binding provides the global light index for a local index.
            /// </summary>
            [ScriptEnum( "light_number" )]
            LightNumber,
            /// <summary>
            /// Returns (int) 1 if the  given light casts shadows, 0 otherwise (index set in extra param)
            /// </summary>
            [ScriptEnum( "light_casts_shadows" )]
            LightCastsShadows,



            /// <summary>
            /// The distance a shadow volume should be extruded when using
			/// finite extrusion programs.
            /// </summary>
            [ScriptEnum( "shadow_extrusion_distance" )]
            ShadowExtrusionDistance,



            /// <summary>
            /// The current camera's position in world space
            /// </summary>
            [ScriptEnum( "camera_position" )]
            CameraPosition,
            /// <summary>
            /// The current camera's position in object space
            /// </summary>
            [ScriptEnum( "camera_position_object_space" )]
            CameraPositionObjectSpace,



            /// <summary>
            /// The view/projection matrix of the assigned texture projection frustum
            /// </summary>
            [ScriptEnum( "texture_viewproj_matrix" )]
            TextureViewProjMatrix,
            /// <summary>
            /// Array of view/projection matrices of the first n texture projection frustums
            /// </summary>
            [ScriptEnum( "texture_viewproj_matrix_array" )]
            TextureViewProjMatrixArray,
            /// <summary>
            /// The view/projection matrix of the assigned texture projection frustum, 
			/// combined with the current world matrix
            /// </summary>
            [ScriptEnum( "texture_worldviewproj_matrix" )]
            TextureWorldViewProjMatrix,
            /// <summary>
            /// Array of world/view/projection matrices of the first n texture projection frustums
            /// </summary>
            [ScriptEnum( "texture_worldviewproj_matrix_array" )]
            TextureWorldViewProjMatrixArray,



            /// <summary>
            /// The view/projection matrix of a given spotlight
            /// </summary>
            [ScriptEnum( "spotlight_viewproj_matrix" )]
            SpotLightViewProjMatrix,
            /// <summary>
            /// The view/projection matrix of a given spotlight projection frustum, 
			/// combined with the current world matrix
            /// </summary>
            [ScriptEnum( "spotlight_worldviewproj_matrix" )]
            SpotLightWorldViewProjMatrix,



            /// <summary>
            /// A custom parameter which will come from the renderable, using 'data' as the identifier
            /// </summary>
            [ScriptEnum( "custom" )]
            Custom,



            /// <summary>
            /// provides current elapsed time
            /// </summary>
            [ScriptEnum( "time" )]
            Time,
            /// <summary>
            /// Single float value, which repeats itself based on given as
			/// parameter "cycle time". Equivalent to RenderMonkey's "Time0_X".
            /// </summary>
            [ScriptEnum( "time_0_x" )]
            Time_0_X,
            /// <summary>
            /// Cosine of "Time0_X". Equivalent to RenderMonkey's "CosTime0_X".
            /// </summary>
            [ScriptEnum( "cos_time_0_x" )]
            CosTime_0_X,
            /// <summary>
            /// Sine of "Time0_X". Equivalent to RenderMonkey's "SinTime0_X".
            /// </summary>
            [ScriptEnum( "sin_time_0_x" )]
            SinTime_0_X,
            /// <summary>
            /// Tangent of "Time0_X". Equivalent to RenderMonkey's "TanTime0_X".
            /// </summary>
            [ScriptEnum( "tan_time_0_x" )]
            TanTime_0_X,
            /// <summary>
            /// Vector of "Time0_X", "SinTime0_X", "CosTime0_X", 
			/// "TanTime0_X". Equivalent to RenderMonkey's "Time0_X_Packed".
            /// </summary>
            [ScriptEnum( "time_0_x_packed" )]
            Time_0_X_Packed,



            /// <summary>
            /// Single float value, which represents scaled time value [0..1],
			/// which repeats itself based on given as parameter "cycle time".
			/// Equivalent to RenderMonkey's "Time0_1".
            /// </summary>
            [ScriptEnum( "time_0_1" )]
            Time_0_1,
            /// <summary>
            /// Cosine of "Time0_1". Equivalent to RenderMonkey's "CosTime0_1".
            /// </summary>
            [ScriptEnum( "cos_time_0_1" )]
            CosTime_0_1,
            /// <summary>
            /// Sine of "Time0_1". Equivalent to RenderMonkey's "SinTime0_1".
            /// </summary>
            [ScriptEnum( "sin_time_0_1" )]
            SinTime_0_1,
            /// <summary>
            /// Tangent of "Time0_1". Equivalent to RenderMonkey's "TanTime0_1".
            /// </summary>
            [ScriptEnum( "tan_time_0_1" )]
            TanTime_0_1,
            /// <summary>
            /// Vector of "Time0_1", "SinTime0_1", "CosTime0_1",
			/// "TanTime0_1". Equivalent to RenderMonkey's "Time0_1_Packed".
            /// </summary>
            [ScriptEnum( "time_0_1_packed" )]
            Time_0_1_Packed,



            /// <summary>
            /// Single float value, which represents scaled time value [0..2*Pi],
			/// which repeats itself based on given as parameter "cycle time".
			/// Equivalent to RenderMonkey's "Time0_2PI".
            /// </summary>
            [ScriptEnum( "time_0_2pi" )]
            Time_0_2PI,
            /// <summary>
            /// Cosine of "Time0_2PI". Equivalent to RenderMonkey's "CosTime0_2PI".
            /// </summary>
            [ScriptEnum( "cos_time_0_2pi" )]
            CosTime_0_2PI,
            /// <summary>
            /// Sine of "Time0_2PI". Equivalent to RenderMonkey's "SinTime0_2PI".
            /// </summary>
            [ScriptEnum( "sin_time_0_2pi" )]
            SinTime_0_2PI,
            /// <summary>
            /// Tangent of "Time0_2PI". Equivalent to RenderMonkey's "TanTime0_2PI".
            /// </summary>
            [ScriptEnum( "tan_time_0_2pi" )]
            TanTime_0_2PI,
            /// <summary>
            /// Vector of "Time0_2PI", "SinTime0_2PI", "CosTime0_2PI",
			/// "TanTime0_2PI". Equivalent to RenderMonkey's "Time0_2PI_Packed".
            /// </summary>
            [ScriptEnum( "time_0_2pi_packed" )]
            Time_0_2PI_Packed,



            /// <summary>
            /// provides the scaled frame time, returned as a floating point value.
            /// </summary>
            [ScriptEnum( "frame_time" )]
            FrameTime,
            /// <summary>
            /// provides the calculated frames per second, returned as a floating point value.
            /// </summary>
            [ScriptEnum( "fps" )]
            FPS,

            // viewport-related values

            /// <summary>
            /// Current viewport width (in pixels) as floating point value.
			/// Equivalent to RenderMonkey's "ViewportWidth".
            /// </summary>
            [ScriptEnum( "viewport_width" )]
            ViewportWidth,
            /// <summary>
            /// Current viewport height (in pixels) as floating point value.
			/// Equivalent to RenderMonkey's "ViewportHeight".
            /// </summary>
            [ScriptEnum( "viewport_height" )]
            ViewportHeight,
            /// <summary>
            /// This variable represents 1.0/ViewportWidth. 
			/// Equivalent to RenderMonkey's "ViewportWidthInverse".
            /// </summary>
            [ScriptEnum( "inverse_viewport_width" )]
            InverseViewportWidth,
            /// <summary>
            /// This variable represents 1.0/ViewportHeight.
			/// Equivalent to RenderMonkey's "ViewportHeightInverse".
            /// </summary>
            [ScriptEnum( "inverse_viewport_height" )]
            InverseViewportHeight,
            /// <summary>
            /// Packed of "ViewportWidth", "ViewportHeight", "ViewportWidthInverse",
			/// "ViewportHeightInverse".
            /// </summary>
            [ScriptEnum( "viewport_size" )]
            ViewportSize,

            // view parameters

            /// <summary>
            /// This variable provides the view direction vector (world space).
			/// Equivalent to RenderMonkey's "ViewDirection".
            /// </summary>
            [ScriptEnum( "view_direction" )]
            ViewDirection,
            /// <summary>
            /// This variable provides the view side vector (world space).
			/// Equivalent to RenderMonkey's "ViewSideVector".
            /// </summary>
            [ScriptEnum( "view_side_vector" )]
            ViewSideVector,
            /// <summary>
            /// This variable provides the view up vector (world space).
			/// Equivalent to RenderMonkey's "ViewUpVector".
            /// </summary>
            [ScriptEnum( "view_up_vector" )]
            ViewUpVector,
            /// <summary>
            /// This variable provides the field of view as a floating point value.
			/// Equivalent to RenderMonkey's "FOV".
            /// </summary>
            [ScriptEnum( "fov" )]
            FOV,
            /// <summary>
            /// This variable provides the near clip distance as a floating point value.
			/// Equivalent to RenderMonkey's "NearClipPlane".
            /// </summary>
            [ScriptEnum( "near_clip_distance" )]
            NearClipDistance,
            /// <summary>
            /// This variable provides the far clip distance as a floating point value.
			/// Equivalent to RenderMonkey's "FarClipPlane".
            /// </summary>
            [ScriptEnum( "far_clip_distance" )]
            FarClipDistance,



            /// <summary>
            /// provides the pass index number within the technique
			/// of the active material.
            /// </summary>
            [ScriptEnum( "pass_number" )]
            PassNumber,
            /// <summary>
            /// provides the current iteration number of the pass. The iteration
			/// number is the number of times the current render operation has
			/// been drawn for the active pass.
            /// </summary>
            [ScriptEnum( "pass_iteration_number" )]
            PassIterationNumber,



            /// <summary>
            /// Provides a parametric animation value [0..1], only available
			/// where the renderable specifically implements it.
            /// </summary>
            [ScriptEnum( "animation_parametric" )]
            AnimationParametric,
            /// <summary>
            /// Provides the texel offsets required by this rendersystem to map
			/// texels to pixels. Packed as 
			/// float4(absoluteHorizontalOffset, absoluteVerticalOffset, 
			/// horizontalOffset / viewportWidth, verticalOffset / viewportHeight)
            /// </summary>
            [ScriptEnum( "texel_offset" )]
            TexelOffset,
            /// <summary>
            /// Provides information about the depth range of the scene as viewed
			/// from the current camera. 
			/// Passed as float4(minDepth, maxDepth, depthRange, 1 / depthRange)
            /// </summary>
            [ScriptEnum( "scene_depth_range" )]
            SceneDepthRange,



            /// <summary>
            /// Provides information about the depth range of the scene as viewed
			/// from a given shadow camera. Requires an index parameter which maps
			/// to a light index relative to the current light list.
			/// Passed as float4(minDepth, maxDepth, depthRange, 1 / depthRange)
            /// </summary>
            [ScriptEnum( "shadow_scene_depth_range" )]
            ShadowSceneDepthRange,
            /// <summary>
            /// Provides the fixed shadow colour as configured via SceneManager::setShadowColour;
			/// useful for integrated modulative shadows.
            /// </summary>
            [ScriptEnum( "shadow_color" )]
            ShadowColor,
            /// <summary>
            /// Provides texture size of the texture unit (index determined by setAutoConstant
			/// call). Packed as float4(width, height, depth, 1)
            /// </summary>
            [ScriptEnum( "texture_size" )]
            TextureSize,
            /// <summary>
            /// Provides inverse texture size of the texture unit (index determined by setAutoConstant
			/// call). Packed as float4(1 / width, 1 / height, 1 / depth, 1)
            /// </summary>
            [ScriptEnum( "inverse_texture_size" )]
            InverseTextureSize,
            /// <summary>
            /// Provides packed texture size of the texture unit (index determined by setAutoConstant
			/// call). Packed as float4(width, height, 1 / width, 1 / height)
            /// </summary>
            [ScriptEnum( "packed_texture_size" )]
            PackedTextureSize,
            /// <summary>
            /// Provides the current transform matrix of the texture unit (index determined by setAutoConstant
			/// call), as seen by the fixed-function pipeline. 
            /// </summary>
            [ScriptEnum( "texture_matrix" )]
            TextureMatrix,
            /// <summary>
            /// Provides the position of the LOD camera in world space, allowing you 
			/// to perform separate LOD calculations in shaders independent of the rendering
			/// camera. If there is no separate LOD camera then this is the real camera
			/// position. See Camera::setLodCamera.
            /// </summary>
            [ScriptEnum( "lod_camera_position" )]
            LODCameraPosition,
            /// <summary>
            /// Provides the position of the LOD camera in object space, allowing you 
			/// to perform separate LOD calculations in shaders independent of the rendering
			/// camera. If there is no separate LOD camera then this is the real camera
			/// position. See Camera::setLodCamera.
            /// </summary>
            [ScriptEnum( "lod_camera_position_object_space" )]
            LODCameraPositionObjectSpace,
            /// <summary>
            /// Binds custom per-light constants to the shaders.
            /// </summary>
            [ScriptEnum( "light_custom" )]
            LightCustom,

            //-------------

            /// <summary>
            /// Multiverse specific shadow technique in use
            /// </summary>
            [ScriptEnum( "mv_shadow_technique" )]
            MVShadowTechnique
		}

		/// <summary>
		/// Defines the base element type of the auto constant
		/// </summary>
		public enum ElementType
		{
            /// <summary>
            /// 
            /// </summary>
			Int,

            /// <summary>
            /// 
            /// </summary>
			Real
		}

		/// <summary>
		/// Defines the type of the extra data item used by the auto constant.
		/// </summary>
		public enum AutoConstantDataType
		{
            /// <summary>
            /// no data is required
            /// </summary>
			None,

            /// <summary>
            /// the auto constant requires data of type int
            /// </summary>
			Int,

            /// <summary>
            /// the auto constant requires data of type real
            /// </summary>
			Real
		}

		/// <summary>
		/// Structure defining an auto constant that's available for use in a parameters object.
		/// </summary>
		public struct AutoConstantDefinition
		{
            /// <summary>
            /// 
            /// </summary>
			public AutoConstantType AutoConstantType;

            /// <summary>
            /// 
            /// </summary>
			public string Name;

            /// <summary>
            /// 
            /// </summary>
			public int ElementCount;
			
            /// <summary>
            /// The type of the constant in the program
            /// </summary>
			public ElementType ElementType;

			/// <summary>
            /// The type of any extra data
			/// </summary>
			public AutoConstantDataType DataType;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="autoConstantType"></param>
			/// <param name="name"></param>
			/// <param name="elementCount"></param>
			/// <param name="elementType"></param>
			/// <param name="dataType"></param>
			public AutoConstantDefinition( AutoConstantType autoConstantType, string name, int elementCount, ElementType elementType, AutoConstantDataType dataType )
			{
				this.AutoConstantType = autoConstantType;
				this.Name = name;
				this.ElementCount = elementCount;
				this.ElementType = elementType;
				this.DataType = dataType;
			}
		}
	}
}