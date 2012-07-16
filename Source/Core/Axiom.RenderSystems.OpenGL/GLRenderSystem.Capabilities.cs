using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Tao.OpenGl;

// TAO is deprecated we should migrate to real OpenTK someday
#pragma warning disable 612,618

namespace Axiom.RenderSystems.OpenGL
{
	public partial class GLRenderSystem
	{
		[OgreVersion( 1, 7, 2790 )]
		public override RenderSystemCapabilities CreateRenderSystemCapabilities()
		{
			var rsc = new RenderSystemCapabilities();

			rsc.SetCategoryRelevant( CapabilitiesCategory.GL, true );
			rsc.DriverVersion = driverVersion;
			var deviceName = Gl.glGetString( Gl.GL_RENDERER );
			var vendorName = Gl.glGetString( Gl.GL_VENDOR );
			rsc.DeviceName = deviceName;
			rsc.RendersystemName = Name;

			// determine vendor
			if ( vendorName.Contains( "NVIDIA" ) )
			{
				rsc.Vendor = GPUVendor.Nvidia;
			}
			else if ( vendorName.Contains( "ATI" ) )
			{
				rsc.Vendor = GPUVendor.Ati;
			}
			else if ( vendorName.Contains( "Intel" ) )
			{
				rsc.Vendor = GPUVendor.Intel;
			}
			else if ( vendorName.Contains( "S3" ) )
			{
				rsc.Vendor = GPUVendor.S3;
			}
			else if ( vendorName.Contains( "Matrox" ) )
			{
				rsc.Vendor = GPUVendor.Matrox;
			}
			else if ( vendorName.Contains( "3DLabs" ) )
			{
				rsc.Vendor = GPUVendor._3DLabs;
			}
			else if ( vendorName.Contains( "SiS" ) )
			{
				rsc.Vendor = GPUVendor.Sis;
			}
			else
			{
				rsc.Vendor = GPUVendor.Unknown;
			}

			rsc.SetCapability( Graphics.Capabilities.FixedFunction );


			if ( this.GLEW_VERSION_1_4 || this.GLEW_SGIS_generate_mipmap )
			{
				var disableAutoMip = false;
#if AXIOM_PLATFORM == AXIOM_PLATFORM_APPLE || AXIOM_PLATFORM == AXIOM_PLATFORM_LINUX
				// Apple & Linux ATI drivers have faults in hardware mipmap generation
				if ( rsc.Vendor == GPUVendor.Ati )
				{
					disableAutoMip = true;
				}
#endif
				// The Intel 915G frequently corrupts textures when using hardware mip generation
				// I'm not currently sure how many generations of hardware this affects, 
				// so for now, be safe.
				if ( rsc.Vendor == GPUVendor.Intel )
				{
					disableAutoMip = true;
				}

				// SiS chipsets also seem to have problems with this
				if ( rsc.Vendor == GPUVendor.Sis )
				{
					disableAutoMip = true;
				}

				if ( !disableAutoMip )
				{
					rsc.SetCapability( Graphics.Capabilities.HardwareMipMaps );
				}
			}

			// Check for blending support
			if ( this.GLEW_VERSION_1_3 || this.GLEW_ARB_texture_env_combine || this.GLEW_EXT_texture_env_combine )
			{
				rsc.SetCapability( Graphics.Capabilities.Blending );
			}

			// Check for Multitexturing support and set number of texture units
			if ( this.GLEW_VERSION_1_3 || this.GLEW_ARB_multitexture )
			{
				int units;
				Gl.glGetIntegerv( Gl.GL_MAX_TEXTURE_UNITS, out units );

				if ( this.GLEW_ARB_fragment_program )
				{
					// Also check GL_MAX_TEXTURE_IMAGE_UNITS_ARB since NV at least
					// only increased this on the FX/6x00 series
					int arbUnits;
					Gl.glGetIntegerv( Gl.GL_MAX_TEXTURE_IMAGE_UNITS_ARB, out arbUnits );
					if ( arbUnits > units )
					{
						units = arbUnits;
					}
				}
				rsc.TextureUnitCount = units;
			}
			else
			{
				// If no multitexture support then set one texture unit
				rsc.TextureUnitCount = 1;
			}

			// Check for Anisotropy support
			if ( this.GLEW_EXT_texture_filter_anisotropic )
			{
				rsc.SetCapability( Graphics.Capabilities.AnisotropicFiltering );
			}

			// Check for DOT3 support
			if ( this.GLEW_VERSION_1_3 || this.GLEW_ARB_texture_env_dot3 || this.GLEW_EXT_texture_env_dot3 )
			{
				rsc.SetCapability( Graphics.Capabilities.Dot3 );
			}

			// Check for cube mapping
			if ( this.GLEW_VERSION_1_3 || this.GLEW_ARB_texture_cube_map || this.GLEW_EXT_texture_cube_map )
			{
				rsc.SetCapability( Graphics.Capabilities.CubeMapping );
			}

			// Point sprites
			if ( this.GLEW_VERSION_2_0 || this.GLEW_ARB_point_sprite )
			{
				rsc.SetCapability( Graphics.Capabilities.PointSprites );
			}
			// Check for point parameters
			if ( this.GLEW_VERSION_1_4 )
			{
				rsc.SetCapability( Graphics.Capabilities.PointExtendedParameters );
			}
			if ( this.GLEW_ARB_point_parameters )
			{
				rsc.SetCapability( Graphics.Capabilities.PointExtendedParametersARB );
			}
			if ( this.GLEW_EXT_point_parameters )
			{
				rsc.SetCapability( Graphics.Capabilities.PointExtendedParametersEXT );
			}

			// Check for hardware stencil support and set bit depth
			int stencil;
			Gl.glGetIntegerv( Gl.GL_STENCIL_BITS, out stencil );

			if ( stencil != 0 )
			{
				rsc.SetCapability( Graphics.Capabilities.StencilBuffer );
				rsc.StencilBufferBitCount = stencil;
			}

			if ( this.GLEW_VERSION_1_5 || this.GLEW_ARB_vertex_buffer_object )
			{
				if ( !this.GLEW_ARB_vertex_buffer_object )
				{
					rsc.SetCapability( Graphics.Capabilities.GL15NoVbo );
				}
				rsc.SetCapability( Graphics.Capabilities.VertexBuffer );
			}

			if ( this.GLEW_ARB_vertex_program )
			{
				rsc.SetCapability( Graphics.Capabilities.VertexPrograms );

				// Vertex Program Properties
				rsc.VertexProgramConstantBoolCount = 0;
				rsc.VertexProgramConstantIntCount = 0;

				int floatConstantCount;
				Gl.glGetProgramivARB( Gl.GL_VERTEX_PROGRAM_ARB, Gl.GL_MAX_PROGRAM_LOCAL_PARAMETERS_ARB, out floatConstantCount );
				rsc.VertexProgramConstantFloatCount = floatConstantCount;

				rsc.AddShaderProfile( "arbvp1" );
				if ( this.GLEW_NV_vertex_program2_option )
				{
					rsc.AddShaderProfile( "vp30" );
				}

				if ( this.GLEW_NV_vertex_program3 )
				{
					rsc.AddShaderProfile( "vp40" );
				}

				if ( this.GLEW_NV_vertex_program4 )
				{
					rsc.AddShaderProfile( "gp4vp" );
					rsc.AddShaderProfile( "gpu_vp" );
				}
			}

			if ( this.GLEW_NV_register_combiners2 && this.GLEW_NV_texture_shader )
			{
				rsc.SetCapability( Graphics.Capabilities.FragmentPrograms );
				rsc.AddShaderProfile( "fp20" );
			}

			// NFZ - check for ATI fragment shader support
			if ( this.GLEW_ATI_fragment_shader )
			{
				rsc.SetCapability( Graphics.Capabilities.FragmentPrograms );
				// no boolean params allowed
				rsc.FragmentProgramConstantBoolCount = 0;
				// no integer params allowed
				rsc.FragmentProgramConstantIntCount = 0;

				// only 8 Vector4 constant floats supported
				rsc.FragmentProgramConstantFloatCount = 8;

				rsc.AddShaderProfile( "ps_1_4" );
				rsc.AddShaderProfile( "ps_1_3" );
				rsc.AddShaderProfile( "ps_1_2" );
				rsc.AddShaderProfile( "ps_1_1" );
			}

			if ( this.GLEW_ARB_fragment_program )
			{
				rsc.SetCapability( Graphics.Capabilities.FragmentPrograms );

				// Fragment Program Properties
				rsc.FragmentProgramConstantBoolCount = 0;
				rsc.FragmentProgramConstantIntCount = 0;

				int floatConstantCount;
				Gl.glGetProgramivARB( Gl.GL_FRAGMENT_PROGRAM_ARB, Gl.GL_MAX_PROGRAM_LOCAL_PARAMETERS_ARB, out floatConstantCount );
				rsc.FragmentProgramConstantFloatCount = floatConstantCount;

				rsc.AddShaderProfile( "arbfp1" );
				if ( this.GLEW_NV_fragment_program_option )
				{
					rsc.AddShaderProfile( "fp30" );
				}

				if ( this.GLEW_NV_fragment_program2 )
				{
					rsc.AddShaderProfile( "fp40" );
				}
			}

			// NFZ - Check if GLSL is supported
			if ( this.GLEW_VERSION_2_0 ||
				 ( this.GLEW_ARB_shading_language_100 && this.GLEW_ARB_shader_objects && this.GLEW_ARB_fragment_shader &&
				   this.GLEW_ARB_vertex_shader ) )
			{
				rsc.AddShaderProfile( "glsl" );
			}

			// Check if geometry shaders are supported
			if ( this.GLEW_VERSION_2_0 && this.GLEW_EXT_geometry_shader4 )
			{
				rsc.SetCapability( Graphics.Capabilities.GeometryPrograms );
				rsc.AddShaderProfile( "nvgp4" );

				//Also add the CG profiles
				rsc.AddShaderProfile( "gpu_gp" );
				rsc.AddShaderProfile( "gp4gp" );

				rsc.GeometryProgramConstantBoolCount = 0;
				rsc.GeometryProgramConstantIntCount = 0;

				int floatConstantCount;
				Gl.glGetProgramivARB( Gl.GL_GEOMETRY_PROGRAM_NV, Gl.GL_MAX_PROGRAM_LOCAL_PARAMETERS_ARB, out floatConstantCount );
				rsc.GeometryProgramConstantFloatCount = floatConstantCount;

				int maxOutputVertices;
				Gl.glGetIntegerv( Gl.GL_MAX_GEOMETRY_OUTPUT_VERTICES_EXT, out maxOutputVertices );
				rsc.GeometryProgramNumOutputVertices = maxOutputVertices;
			}

			if ( this._glSupport.CheckExtension( "GL_ARB_get_program_binary" ) )
			{
				// states 3.0 here: http://developer.download.nvidia.com/opengl/specs/GL_ARB_get_program_binary.txt
				// but not here: http://www.opengl.org/sdk/docs/man4/xhtml/glGetProgramBinary.xml
				// and here states 4.1: http://www.geeks3d.com/20100727/opengl-4-1-allows-the-use-of-binary-shaders/
				rsc.SetCapability( Graphics.Capabilities.CanGetCompiledShaderBuffer );
			}

			if ( this.GLEW_VERSION_3_3 )
			{
				// states 3.3 here: http://www.opengl.org/sdk/docs/man3/xhtml/glVertexAttribDivisor.xml
				rsc.SetCapability( Graphics.Capabilities.VertexBufferInstanceData );
			}

			//Check if render to vertex buffer (transform feedback in OpenGL)
			if ( this.GLEW_VERSION_2_0 && this.GLEW_NV_transform_feedback )
			{
				rsc.SetCapability( Graphics.Capabilities.HardwareRenderToTexture );
			}

			// Check for texture compression
			if ( this.GLEW_VERSION_1_3 || this.GLEW_ARB_texture_compression )
			{
				rsc.SetCapability( Graphics.Capabilities.TextureCompression );

				// Check for dxt compression
				if ( this.GLEW_EXT_texture_compression_s3tc )
				{
#if __APPLE__ && __PPC__
	// Apple on ATI & PPC has errors in DXT
				if (_glSupport.Vendor.Contains("ATI") == false)
	#endif
					rsc.SetCapability( Graphics.Capabilities.TextureCompressionDXT );
				}
				// Check for vtc compression
				if ( this.GLEW_NV_texture_compression_vtc )
				{
					rsc.SetCapability( Graphics.Capabilities.TextureCompressionVTC );
				}
			}

			// Scissor test is standard in GL 1.2 (is it emulated on some cards though?)
			rsc.SetCapability( Graphics.Capabilities.ScissorTest );
			// As are user clipping planes
			rsc.SetCapability( Graphics.Capabilities.UserClipPlanes );

			// 2-sided stencil?
			if ( this.GLEW_VERSION_2_0 || this.GLEW_EXT_stencil_two_side )
			{
				rsc.SetCapability( Graphics.Capabilities.TwoSidedStencil );
			}
			// stencil wrapping?
			if ( this.GLEW_VERSION_1_4 || this.GLEW_EXT_stencil_wrap )
			{
				rsc.SetCapability( Graphics.Capabilities.StencilWrap );
			}

			// Check for hardware occlusion support
			if ( this.GLEW_VERSION_1_5 || this.GLEW_ARB_occlusion_query )
			{
				// Some buggy driver claim that it is GL 1.5 compliant and
				// not support ARB_occlusion_query
				if ( !this.GLEW_ARB_occlusion_query )
				{
					rsc.SetCapability( Graphics.Capabilities.GL15NoHardwareOcclusion );
				}

				rsc.SetCapability( Graphics.Capabilities.HardwareOcculusion );
			}
			else if ( this.GLEW_NV_occlusion_query )
			{
				// Support NV extension too for old hardware
				rsc.SetCapability( Graphics.Capabilities.HardwareOcculusion );
			}

			// UBYTE4 always supported
			rsc.SetCapability( Graphics.Capabilities.VertexFormatUByte4 );

			// Infinite far plane always supported
			rsc.SetCapability( Graphics.Capabilities.InfiniteFarPlane );

			// Check for non-power-of-2 texture support
			if ( this.GLEW_ARB_texture_non_power_of_two )
			{
				rsc.SetCapability( Graphics.Capabilities.NonPowerOf2Textures );
			}

			// Check for Float textures
			if ( this.GLEW_ATI_texture_float || this.GLEW_ARB_texture_float )
			{
				rsc.SetCapability( Graphics.Capabilities.TextureFloat );
			}

			// 3D textures should be supported by GL 1.2, which is our minimum version
			rsc.SetCapability( Graphics.Capabilities.Texture3D );

			// Check for framebuffer object extension
			if ( this.GLEW_EXT_framebuffer_object )
			{
				// Probe number of draw buffers
				// Only makes sense with FBO support, so probe here
				if ( this.GLEW_VERSION_2_0 || this.GLEW_ARB_draw_buffers || this.GLEW_ATI_draw_buffers )
				{
					int buffers;
					Gl.glGetIntegerv( Gl.GL_MAX_DRAW_BUFFERS_ARB, out buffers );
					rsc.MultiRenderTargetCount = Utility.Min( buffers, Config.MaxMultipleRenderTargets );
					rsc.SetCapability( Graphics.Capabilities.MRTDifferentBitDepths );
					if ( !this.GLEW_VERSION_2_0 )
					{
						// Before GL version 2.0, we need to get one of the extensions
						if ( this.GLEW_ARB_draw_buffers )
						{
							rsc.SetCapability( Graphics.Capabilities.FrameBufferObjectsARB );
						}
						if ( this.GLEW_ATI_draw_buffers )
						{
							rsc.SetCapability( Graphics.Capabilities.FrameBufferObjectsATI );
						}
					}
					// Set FBO flag for all 3 'subtypes'
					rsc.SetCapability( Graphics.Capabilities.FrameBufferObjects );
				}
				rsc.SetCapability( Graphics.Capabilities.HardwareRenderToTexture );
			}

			// Check GLSupport for PBuffer support
			if ( this._glSupport.SupportsPBuffers )
			{
				// Use PBuffers
				rsc.SetCapability( Graphics.Capabilities.HardwareRenderToTexture );
				rsc.SetCapability( Graphics.Capabilities.PBuffer );
			}

			// Point size
			if ( this.GLEW_VERSION_1_4 )
			{
				float ps;
				Gl.glGetFloatv( Gl.GL_POINT_SIZE_MAX, out ps );
				rsc.MaxPointSize = ps;
			}
			else
			{
				var vSize = new int[2];
				Gl.glGetIntegerv( Gl.GL_POINT_SIZE_RANGE, vSize );
				rsc.MaxPointSize = vSize[ 1 ];
			}

			// Vertex texture fetching
			if ( this._glSupport.CheckExtension( "GL_ARB_vertex_shader" ) )
			{
				int vUnits;
				Gl.glGetIntegerv( Gl.GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS_ARB, out vUnits );
				rsc.VertexTextureUnitCount = vUnits;
				if ( vUnits > 0 )
				{
					rsc.SetCapability( Graphics.Capabilities.VertexTextureFetch );
				}
				// GL always shares vertex and fragment texture units (for now?)
				rsc.VertexTextureUnitsShared = true;
			}

			// Mipmap LOD biasing?
			if ( this.GLEW_VERSION_1_4 || this.GLEW_EXT_texture_lod_bias )
			{
				rsc.SetCapability( Graphics.Capabilities.MipmapLODBias );
			}

			// Alpha to coverage?
			if ( this._glSupport.CheckExtension( "GL_ARB_multisample" ) )
			{
				// Alpha to coverage always 'supported' when MSAA is available
				// although card may ignore it if it doesn't specifically support A2C
				rsc.SetCapability( Graphics.Capabilities.AlphaToCoverage );
			}

			// Advanced blending operations
			if ( this.GLEW_VERSION_2_0 )
			{
				rsc.SetCapability( Graphics.Capabilities.AdvancedBlendOperations );
			}

			return rsc;
		}
	}
}

#pragma warning restore 612,618