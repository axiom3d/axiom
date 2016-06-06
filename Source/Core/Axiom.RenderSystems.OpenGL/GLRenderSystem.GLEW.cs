// ReSharper disable InconsistentNaming

using System;

namespace Axiom.RenderSystems.OpenGL
{
	public partial class GLRenderSystem
	{
		[AxiomHelper( 0, 8 )] private bool GLEW_VERSION_1_2;

		[AxiomHelper( 0, 8 )] private bool GL_VERSION_1_3;

		[AxiomHelper( 0, 8 )] private bool GLEW_VERSION_1_3;

		[AxiomHelper( 0, 8 )] private bool GLEW_VERSION_1_4;

		[AxiomHelper( 0, 8 )] private bool GLEW_VERSION_1_5;

		[AxiomHelper( 0, 8 )] private bool GLEW_VERSION_2_0;

		[AxiomHelper( 0, 8 )] private bool GLEW_VERSION_3_3;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_blend_equation_separate;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_blend_minmax;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_imaging;

		[AxiomHelper( 0, 8 )] private bool GLEW_SGIS_generate_mipmap;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_texture_env_combine;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_texture_env_combine;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_multitexture;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_fragment_program;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_texture_filter_anisotropic;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_texture_env_dot3;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_texture_env_dot3;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_texture_cube_map;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_texture_cube_map;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_point_sprite;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_point_parameters;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_point_parameters;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_vertex_buffer_object;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_vertex_program;

		[AxiomHelper( 0, 8 )] private bool GLEW_NV_vertex_program2_option;

		[AxiomHelper( 0, 8 )] private bool GLEW_NV_vertex_program3;

		[AxiomHelper( 0, 8 )] private bool GLEW_NV_vertex_program4;

		[AxiomHelper( 0, 8 )] private bool GLEW_NV_register_combiners2;

		[AxiomHelper( 0, 8 )] private bool GLEW_NV_texture_shader;

		[AxiomHelper( 0, 8 )] private bool GLEW_ATI_fragment_shader;

		[AxiomHelper( 0, 8 )] private bool GLEW_NV_fragment_program_option;

		[AxiomHelper( 0, 8 )] private bool GLEW_NV_fragment_program2;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_shading_language_100;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_shader_objects;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_fragment_shader;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_vertex_shader;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_geometry_shader4;

		[AxiomHelper( 0, 8 )] private bool GLEW_NV_transform_feedback;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_texture_compression;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_texture_compression_s3tc;

		[AxiomHelper( 0, 8 )] private bool GLEW_NV_texture_compression_vtc;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_stencil_two_side;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_stencil_wrap;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_occlusion_query;

		[AxiomHelper( 0, 8 )] private bool GLEW_NV_occlusion_query;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_texture_non_power_of_two;

		[AxiomHelper( 0, 8 )] private bool GLEW_ATI_texture_float;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_texture_float;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_framebuffer_object;

		[AxiomHelper( 0, 8 )] private bool GLEW_ARB_draw_buffers;

		[AxiomHelper( 0, 8 )] private bool GLEW_ATI_draw_buffers;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_texture_lod_bias;

		[AxiomHelper( 0, 8 )] private bool GLEW_EXT_secondary_color;

		[AxiomHelper( 0, 8 )]
		private void InitGLEW()
		{
			this.GLEW_VERSION_1_2 = this._glSupport.CheckMinVersion( "1.2" );
			this.GL_VERSION_1_3 = this.GLEW_VERSION_1_3 = this._glSupport.CheckMinVersion( "1.3" );
			this.GLEW_VERSION_1_4 = this._glSupport.CheckMinVersion( "1.4" );
			this.GLEW_VERSION_1_5 = this._glSupport.CheckMinVersion( "1.5" );
			this.GLEW_VERSION_2_0 = this._glSupport.CheckMinVersion( "2.0" );
			this.GLEW_VERSION_3_3 = this._glSupport.CheckMinVersion( "3.3" );

			this.GLEW_EXT_blend_equation_separate = this._glSupport.CheckExtension( "EXT_blend_equation_separate" );
			this.GLEW_EXT_blend_minmax = this._glSupport.CheckExtension( "EXT_blend_minmax" );
			this.GLEW_ARB_imaging = this._glSupport.CheckExtension( "ARB_imaging" );
			this.GLEW_SGIS_generate_mipmap = this._glSupport.CheckExtension( "SGIS_generate_mipmap" );
			this.GLEW_ARB_texture_env_combine = this._glSupport.CheckExtension( "ARB_texture_env_combine" );
			this.GLEW_EXT_texture_env_combine = this._glSupport.CheckExtension( "EXT_texture_env_combine" );
			this.GLEW_ARB_multitexture = this._glSupport.CheckExtension( "ARB_multitexture" );
			this.GLEW_ARB_fragment_program = this._glSupport.CheckExtension( "ARB_fragment_program" );
			this.GLEW_EXT_texture_filter_anisotropic = this._glSupport.CheckExtension( "EXT_texture_filter_anisotropic" );
			this.GLEW_ARB_texture_env_dot3 = this._glSupport.CheckExtension( "ARB_texture_env_dot3" );
			this.GLEW_EXT_texture_env_dot3 = this._glSupport.CheckExtension( "EXT_texture_env_dot3" );
			this.GLEW_ARB_texture_cube_map = this._glSupport.CheckExtension( "ARB_texture_cube_map" );
			this.GLEW_EXT_texture_cube_map = this._glSupport.CheckExtension( "EXT_texture_cube_map" );
			this.GLEW_ARB_point_sprite = this._glSupport.CheckExtension( "ARB_point_sprite" );
			this.GLEW_ARB_point_parameters = this._glSupport.CheckExtension( "ARB_point_parameters" );
			this.GLEW_EXT_point_parameters = this._glSupport.CheckExtension( "EXT_point_parameters" );
			this.GLEW_ARB_vertex_buffer_object = this._glSupport.CheckExtension( "ARB_vertex_buffer_object" );
			this.GLEW_ARB_vertex_program = this._glSupport.CheckExtension( "ARB_vertex_program" );
			this.GLEW_NV_vertex_program2_option = this._glSupport.CheckExtension( "NV_vertex_program2_option" );
			this.GLEW_NV_vertex_program3 = this._glSupport.CheckExtension( "NV_vertex_program3" );
			this.GLEW_NV_vertex_program4 = this._glSupport.CheckExtension( "NV_vertex_program4" );
			this.GLEW_NV_register_combiners2 = this._glSupport.CheckExtension( "NV_register_combiners2" );
			this.GLEW_NV_texture_shader = this._glSupport.CheckExtension( "NV_texture_shader" );
			this.GLEW_ATI_fragment_shader = this._glSupport.CheckExtension( "ATI_fragment_shader" );
			this.GLEW_NV_fragment_program_option = this._glSupport.CheckExtension( "NV_fragment_program_option" );
			this.GLEW_NV_fragment_program2 = this._glSupport.CheckExtension( "NV_fragment_program2" );
			this.GLEW_ARB_shading_language_100 = this._glSupport.CheckExtension( "ARB_shading_language_100" );
			this.GLEW_ARB_shader_objects = this._glSupport.CheckExtension( "ARB_shader_objects" );
			this.GLEW_ARB_fragment_shader = this._glSupport.CheckExtension( "ARB_fragment_shader" );
			this.GLEW_ARB_vertex_shader = this._glSupport.CheckExtension( "ARB_vertex_shader" );
			this.GLEW_EXT_geometry_shader4 = this._glSupport.CheckExtension( "EXT_geometry_shader4" );
			this.GLEW_NV_transform_feedback = this._glSupport.CheckExtension( "NV_transform_feedback" );
			this.GLEW_ARB_texture_compression = this._glSupport.CheckExtension( "ARB_texture_compression" );
			this.GLEW_EXT_texture_compression_s3tc = this._glSupport.CheckExtension( "EXT_texture_compression_s3tc" );
			this.GLEW_NV_texture_compression_vtc = this._glSupport.CheckExtension( "NV_texture_compression_vtc" );
			this.GLEW_EXT_stencil_two_side = this._glSupport.CheckExtension( "EXT_stencil_two_side" );
			this.GLEW_EXT_stencil_wrap = this._glSupport.CheckExtension( "EXT_stencil_wrap" );
			this.GLEW_ARB_occlusion_query = this._glSupport.CheckExtension( "ARB_occlusion_query" );
			this.GLEW_NV_occlusion_query = this._glSupport.CheckExtension( "NV_occlusion_query" );
			this.GLEW_ARB_texture_non_power_of_two = this._glSupport.CheckExtension( "ARB_texture_non_power_of_two" );
			this.GLEW_ATI_texture_float = this._glSupport.CheckExtension( "ATI_texture_float" );
			this.GLEW_ARB_texture_float = this._glSupport.CheckExtension( "ARB_texture_float" );
			this.GLEW_EXT_framebuffer_object = this._glSupport.CheckExtension( "EXT_framebuffer_object" );
			this.GLEW_ARB_draw_buffers = this._glSupport.CheckExtension( "ARB_draw_buffers" );
			this.GLEW_ATI_draw_buffers = this._glSupport.CheckExtension( "ATI_draw_buffers" );
			this.GLEW_EXT_texture_lod_bias = this._glSupport.CheckExtension( "EXT_texture_lod_bias" );
			this.GLEW_EXT_secondary_color = this._glSupport.CheckExtension( "EXT_secondary_color" );
		}

		private void glVertexAttribDivisor( int index, int divisor )
		{
			throw new NotImplementedException( "OpenTK.Tao does not expose glVertexAttribDivisor" );
		}
	}
}

// ReSharper restore InconsistentNaming