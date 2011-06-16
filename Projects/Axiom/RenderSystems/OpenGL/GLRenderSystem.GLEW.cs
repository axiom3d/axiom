// ReSharper disable InconsistentNaming
using System;

namespace Axiom.RenderSystems.OpenGL
{
    public partial class GLRenderSystem
    {

        [AxiomHelper(0, 8)]
        private bool GLEW_VERSION_1_2;

        [AxiomHelper(0, 8)]
        private bool GL_VERSION_1_3;

        [AxiomHelper(0, 8)]
        private bool GLEW_VERSION_1_3;

        [AxiomHelper(0, 8)]
        private bool GLEW_VERSION_1_4;

        [AxiomHelper(0, 8)]
        private bool GLEW_VERSION_1_5;

        [AxiomHelper(0, 8)]
        private bool GLEW_VERSION_2_0;

        [AxiomHelper(0, 8)]
        private bool GLEW_VERSION_3_3;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_blend_equation_separate;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_blend_minmax;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_imaging;

        [AxiomHelper(0, 8)]
        private bool GLEW_SGIS_generate_mipmap;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_texture_env_combine;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_texture_env_combine;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_multitexture;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_fragment_program;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_texture_filter_anisotropic;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_texture_env_dot3;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_texture_env_dot3;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_texture_cube_map;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_texture_cube_map;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_point_sprite;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_point_parameters;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_point_parameters;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_vertex_buffer_object;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_vertex_program;

        [AxiomHelper(0, 8)]
        private bool GLEW_NV_vertex_program2_option;

        [AxiomHelper(0, 8)]
        private bool GLEW_NV_vertex_program3;

        [AxiomHelper(0, 8)]
        private bool GLEW_NV_vertex_program4;

        [AxiomHelper(0, 8)]
        private bool GLEW_NV_register_combiners2;

        [AxiomHelper(0, 8)]
        private bool GLEW_NV_texture_shader;

        [AxiomHelper(0, 8)]
        private bool GLEW_ATI_fragment_shader;

        [AxiomHelper(0, 8)]
        private bool GLEW_NV_fragment_program_option;

        [AxiomHelper(0, 8)]
        private bool GLEW_NV_fragment_program2;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_shading_language_100;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_shader_objects;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_fragment_shader;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_vertex_shader;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_geometry_shader4;

        [AxiomHelper(0, 8)]
        private bool GLEW_NV_transform_feedback;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_texture_compression;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_texture_compression_s3tc;

        [AxiomHelper(0, 8)]
        private bool GLEW_NV_texture_compression_vtc;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_stencil_two_side;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_stencil_wrap;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_occlusion_query;

        [AxiomHelper(0, 8)]
        private bool GLEW_NV_occlusion_query;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_texture_non_power_of_two;

        [AxiomHelper(0, 8)]
        private bool GLEW_ATI_texture_float;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_texture_float;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_framebuffer_object;

        [AxiomHelper(0, 8)]
        private bool GLEW_ARB_draw_buffers;

        [AxiomHelper(0, 8)]
        private bool GLEW_ATI_draw_buffers;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_texture_lod_bias;

        [AxiomHelper(0, 8)]
        private bool GLEW_EXT_secondary_color;

        [AxiomHelper(0, 8)]
        private void InitGLEW()
        {
            GLEW_VERSION_1_2 = _glSupport.CheckMinVersion("1.2");
            GL_VERSION_1_3 = GLEW_VERSION_1_3 = _glSupport.CheckMinVersion("1.3");
            GLEW_VERSION_1_4 = _glSupport.CheckMinVersion("1.4");
            GLEW_VERSION_1_5 = _glSupport.CheckMinVersion("1.5");
            GLEW_VERSION_2_0 = _glSupport.CheckMinVersion("2.0");
            GLEW_VERSION_3_3 = _glSupport.CheckMinVersion("3.3");

            GLEW_EXT_blend_equation_separate = _glSupport.CheckExtension("EXT_blend_equation_separate");
            GLEW_EXT_blend_minmax = _glSupport.CheckExtension("EXT_blend_minmax");
            GLEW_ARB_imaging = _glSupport.CheckExtension("ARB_imaging");
            GLEW_SGIS_generate_mipmap = _glSupport.CheckExtension("SGIS_generate_mipmap");
            GLEW_ARB_texture_env_combine = _glSupport.CheckExtension("ARB_texture_env_combine");
            GLEW_EXT_texture_env_combine = _glSupport.CheckExtension("EXT_texture_env_combine");
            GLEW_ARB_multitexture = _glSupport.CheckExtension("ARB_multitexture");
            GLEW_ARB_fragment_program = _glSupport.CheckExtension("ARB_fragment_program");
            GLEW_EXT_texture_filter_anisotropic = _glSupport.CheckExtension("EXT_texture_filter_anisotropic");
            GLEW_ARB_texture_env_dot3 = _glSupport.CheckExtension("ARB_texture_env_dot3");
            GLEW_EXT_texture_env_dot3 = _glSupport.CheckExtension("EXT_texture_env_dot3");
            GLEW_ARB_texture_cube_map = _glSupport.CheckExtension("ARB_texture_cube_map");
            GLEW_EXT_texture_cube_map = _glSupport.CheckExtension("EXT_texture_cube_map");
            GLEW_ARB_point_sprite = _glSupport.CheckExtension("ARB_point_sprite");
            GLEW_ARB_point_parameters = _glSupport.CheckExtension("ARB_point_parameters");
            GLEW_EXT_point_parameters = _glSupport.CheckExtension("EXT_point_parameters");
            GLEW_ARB_vertex_buffer_object = _glSupport.CheckExtension("ARB_vertex_buffer_object");
            GLEW_ARB_vertex_program = _glSupport.CheckExtension("ARB_vertex_program");
            GLEW_NV_vertex_program2_option = _glSupport.CheckExtension("NV_vertex_program2_option");
            GLEW_NV_vertex_program3 = _glSupport.CheckExtension("NV_vertex_program3");
            GLEW_NV_vertex_program4 = _glSupport.CheckExtension("NV_vertex_program4");
            GLEW_NV_register_combiners2 = _glSupport.CheckExtension("NV_register_combiners2");
            GLEW_NV_texture_shader = _glSupport.CheckExtension("NV_texture_shader");
            GLEW_ATI_fragment_shader = _glSupport.CheckExtension("ATI_fragment_shader");
            GLEW_NV_fragment_program_option = _glSupport.CheckExtension("NV_fragment_program_option");
            GLEW_NV_fragment_program2 = _glSupport.CheckExtension("NV_fragment_program2");
            GLEW_ARB_shading_language_100 = _glSupport.CheckExtension("ARB_shading_language_100");
            GLEW_ARB_shader_objects = _glSupport.CheckExtension("ARB_shader_objects");
            GLEW_ARB_fragment_shader = _glSupport.CheckExtension("ARB_fragment_shader");
            GLEW_ARB_vertex_shader = _glSupport.CheckExtension("ARB_vertex_shader");
            GLEW_EXT_geometry_shader4 = _glSupport.CheckExtension("EXT_geometry_shader4");
            GLEW_NV_transform_feedback = _glSupport.CheckExtension("NV_transform_feedback");
            GLEW_ARB_texture_compression = _glSupport.CheckExtension("ARB_texture_compression");
            GLEW_EXT_texture_compression_s3tc = _glSupport.CheckExtension("EXT_texture_compression_s3tc");
            GLEW_NV_texture_compression_vtc = _glSupport.CheckExtension("NV_texture_compression_vtc");
            GLEW_EXT_stencil_two_side = _glSupport.CheckExtension("EXT_stencil_two_side");
            GLEW_EXT_stencil_wrap = _glSupport.CheckExtension("EXT_stencil_wrap");
            GLEW_ARB_occlusion_query = _glSupport.CheckExtension("ARB_occlusion_query");
            GLEW_NV_occlusion_query = _glSupport.CheckExtension("NV_occlusion_query");
            GLEW_ARB_texture_non_power_of_two = _glSupport.CheckExtension("ARB_texture_non_power_of_two");
            GLEW_ATI_texture_float = _glSupport.CheckExtension("ATI_texture_float");
            GLEW_ARB_texture_float = _glSupport.CheckExtension("ARB_texture_float");
            GLEW_EXT_framebuffer_object = _glSupport.CheckExtension("EXT_framebuffer_object");
            GLEW_ARB_draw_buffers = _glSupport.CheckExtension("ARB_draw_buffers");
            GLEW_ATI_draw_buffers = _glSupport.CheckExtension("ATI_draw_buffers");
            GLEW_EXT_texture_lod_bias = _glSupport.CheckExtension("EXT_texture_lod_bias");
            GLEW_EXT_secondary_color = _glSupport.CheckExtension("EXT_secondary_color");
        }

        void glVertexAttribDivisor(int index, int divisor)
        {
            throw new NotImplementedException( "OpenTK.Tao does not expose glVertexAttribDivisor" );
        }
    }
}
// ReSharper restore InconsistentNaming
