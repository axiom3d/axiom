#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
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
            [ScriptEnum("world_matrix")]
            WorldMatrix,
            /// <summary>
            /// Provides transpose of world matrix. Equivalent to RenderMonkey's "WorldTranspose".
            /// </summary>
            [ScriptEnum("transpose_world_matrix")]
            TransposeWorldMatrix,
            /// <summary>
            ///    The current array of world matrices, as a 3x4 matrix, used for blending.
            /// </summary>
            [ScriptEnum("world_matrix_array_3x4")]
            WorldMatrixArray3x4,
            /// <summary>
            ///    The current array of world matrices, used for blending
            /// </summary>
            [ScriptEnum("world_matrix_array")]
            WorldMatrixArray,
            /// <summary>
            ///    Current view matrix.
            /// </summary>
            [ScriptEnum("view_matrix")]
            ViewMatrix,
            /// <summary>
            ///    Current projection matrix.
            /// </summary>
            [ScriptEnum("projection_matrix")]
            ProjectionMatrix,
            /// <summary>
            ///    The current view & projection matrices concatenated.
            /// </summary>
            [ScriptEnum("viewproj_matrix")]
            ViewProjMatrix,
            /// <summary>
            ///    Current world and view matrices concatenated.
            /// </summary>
            [ScriptEnum("worldview_matrix")]
            WorldViewMatrix,
            /// <summary>
            ///    Current world, view, and projection matrics concatenated.
            /// </summary>
            [ScriptEnum("worldviewproj_matrix")]
            WorldViewProjMatrix,
            /// <summary>
            ///    Current world matrix, inverted.
            /// </summary>
            [ScriptEnum("inverse_world_matrix")]
            InverseWorldMatrix,
            /// <summary>
            ///    Current view matrix, inverted.
            /// </summary>
            [ScriptEnum("inverse_view_matrix")]
            InverseViewMatrix,
            /// <summary>
            ///    Current world and view matrices concatenated, then inverted.
            /// </summary>
            [ScriptEnum("inverse_worldview_matrix")]
            InverseWorldViewMatrix,
            /// <summary>
            ///    Current world and view matrices concatenated, then inverted.
            /// </summary>
            [ScriptEnum("inverse_transpose_worldview_matrix")]
            InverseTransposeWorldViewMatrix,
            /// <summary>
            ///    Global ambient light color.
            /// </summary>
            [ScriptEnum("ambient_light_colour")]
            AmbientLightColor,
            /// <summary>
            ///    Light diffuse color.  Index determined when setting up auto constants.
            /// </summary>
            [ScriptEnum("light_diffuse_colour")]
            LightDiffuseColor,
            /// <summary>
            ///    Light specular color.  Index determined when setting up auto constants.
            /// </summary>
            [ScriptEnum("light_specular_colour")]
            LightSpecularColor,
            /// <summary>
            ///    Light attenuation.  Vector4(range, constant, linear, quadratic).
            /// </summary>
            [ScriptEnum("light_attenuation")]
            LightAttenuation,
            /// <summary>
            ///    A light position in world space.  Index determined when setting up auto constants.
            /// </summary>
            [ScriptEnum("light_position")]
            LightPosition,
            /// <summary>
            ///    A light direction in world space.  Index determined when setting up auto constants.
            /// </summary>
            [ScriptEnum("light_direction")]
            LightDirection,
            /// <summary>
            ///    A light position in object space.  Index determined when setting up auto constants.
            /// </summary>
            [ScriptEnum("light_position_object_space")]
            LightPositionObjectSpace,
            /// <summary>
            ///    A light direction in object space.  Index determined when setting up auto constants.
            /// </summary>
            [ScriptEnum("light_direction_object_space")]
            LightDirectionObjectSpace,
            /// <summary>
            ///    The distance of the light from the center of the object a useful approximation as an 
            ///    alternative to per-vertex distance calculations.
            /// </summary>
            [ScriptEnum("light_distance_object_space")]
            LightDistanceObjectSpace,
            /// <summary>
            ///    The distance a shadow volume should be extruded when using finite extrusion programs.
            /// </summary>
            [ScriptEnum("shadow_extrusion_distance")]
            ShadowExtrusionDistance,
            /// <summary>
            ///    The distance a shadow volume should be extruded when using finite extrusion programs.
            /// </summary>
            [ScriptEnum("texture_viewproj_matrix")]
            TextureViewProjMatrix,
            /// <summary>
            ///    The current camera's position in object space.
            /// </summary>
            [ScriptEnum("camera_position_object_space")]
            CameraPositionObjectSpace,
            /// <summary>
            ///    The current camera's position in world space.
            /// </summary>
            [ScriptEnum("camera_position")]
            CameraPosition,
            /// <summary>
            ///    A custom parameter which will come from the renderable, using 'data' as the identifier.
            /// </summary>
            [ScriptEnum("custom")]
            Custom,
            /// <summary>
            ///		Specifies that the time elapsed since last frame will be passed along to the program.
            /// </summary>
            [ScriptEnum("time")]
            Time,
            /// <summary>
            ///		Specifies that the time elapsed since last frame modulo X 
            ///     will be passed along to the program.
            /// </summary>
            [ScriptEnum("time_0_x")]
            Time_0_X,
            /// <summary>
            ///		Specifies that the sin of the time elapsed since last frame 
            ///     will be passed along to the program.
            /// </summary>
            [ScriptEnum("sintime_0_x")]
            SinTime_0_X,
            /// <summary>
            ///		Specifies that the time elapsed since last frame modulo 1
            ///     will be passed along to the program.
            /// </summary>
            [ScriptEnum("time_0_1")]
            Time_0_1,
            /// <summary>
            ///     Allows you to adjust the position to match with the 'requires texture flipping' 
            ///     flag on render targets when bypassing the standard projection transform.
            /// </summary>
            [ScriptEnum("render_target_flipping")]
            RenderTargetFlipping,
            /// <summary>
            ///   The params needed for the vertex program to be able to compute the fog weight.
            ///   Includes fogStart, fogEnd, and fogScale.
            /// </summary>
            [ScriptEnum("fog_params")]
            FogParams,
            /// <summary>
            /// Direction of the camera
            /// </summary>
            [ScriptEnum("view_direction")]
            ViewDirection,
            /// <summary>
            /// View local X axis
            /// </summary>
            [ScriptEnum("view_side_vector")]
            ViewSideVector,
            /// <summary>
            /// View local Y axis
            /// </summary>
            [ScriptEnum("view_up_vector")]
            ViewUpVector,
            /// <summary>
            /// Technique pass number
            /// </summary>
            [ScriptEnum("pass_number")]
            PassNumber,
            /// <summary>
            /// Provides a parametric animation value [0..1], only available
            /// where the renderable specifically implements it.
            /// </summary>
            [ScriptEnum("animation_parametric")]
            AnimationParametric,
            /// <summary>
            /// Distance from camera to near clip plane
            /// </summary>
            [ScriptEnum("near_clip_distance")]
            NearClipDistance,
            /// <summary>
            /// Distance from camera to far clip plane
            /// </summary>
            [ScriptEnum("far_clip_distance")]
            FarClipDistance,
            /// <summary>
            /// Multiverse specific shadow technique in use
            /// </summary>
            [ScriptEnum("mv_shadow_technique")]
            MVShadowTechnique,
        }

        /// <summary>
        /// Defines the base element type of the auto constant
        /// </summary>
        public enum ElementType
        {
            Int,
            Real
        }

        /// <summary>
        /// Defines the type of the extra data item used by the auto constant.
        /// </summary>
        public enum AutoConstantDataType
        {
            None,
            Int,
            Real
        }

        /// <summary>
        /// Structure defining an auto constant that's available for use in a parameters object.
        /// </summary>
        public struct AutoConstantDefinition
        {
            public AutoConstantType AutoConstantType;
            public string Name;
            public int ElementCount;
            /// The type of the constant in the program
            public ElementType ElementType;
            /// The type of any extra data
            public AutoConstantDataType DataType;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="autoConstantType"></param>
            /// <param name="name"></param>
            /// <param name="elementCount"></param>
            /// <param name="elementType"></param>
            /// <param name="dataType"></param>
            public AutoConstantDefinition(AutoConstantType autoConstantType, string name, int elementCount, ElementType elementType, AutoConstantDataType dataType)
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