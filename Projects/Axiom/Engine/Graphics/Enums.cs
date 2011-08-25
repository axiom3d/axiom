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

using System;
using System.Reflection;

using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///     Describes types of hardware buffer licenses.
	/// </summary>
	public enum BufferLicenseRelease
	{
		/// <summary>
		///     Licensee will only release buffer when it says so.
		/// </summary>
		Manual,
		/// <summary>
		///     Licensee can have license revoked.
		/// </summary>
		Automatic
	}

	/// <summary>
	///		Describes how a vertex buffer should act when it is locked.
	/// </summary>
	public enum BufferLocking
	{
		/// <summary>
		/// 
		/// </summary>
		Normal,
		/// <summary>
		///		Discards the <em>entire</em> buffer while locking; this allows optimisation to be 
		///		performed because synchronisation issues are relaxed. Only allowed on buffers 
		///		created with the Dynamic flag. 
		/// </summary>
		Discard,
		/// <summary>
		///		Lock the buffer for reading only. Not allowed in buffers which are created with WriteOnly. 
		///		Mandatory on static buffers, ie those created without the Dynamic flag.
		/// </summary>
		ReadOnly,
		/// <summary>
		///    Potential optimization for some API's.
		/// </summary>
		NoOverwrite
	}

	/// <summary>
	///	Describes how a vertex buffer is to be used, and affects how it is created.
	/// </summary>
	[Flags]
	public enum BufferUsage
	{
		/// <summary>
		/// 
		/// </summary>
		Static = 1,
		/// <summary>
		///		Indicates the application would like to modify this buffer with the CPU
		///		sometimes. Absence of this flag means the application will never modify. 
		///		Buffers created with this flag will typically end up in AGP memory rather 
		///		than video memory.
		/// </summary>
		Dynamic = 2,
		/// <summary>
		///		Indicates the application will never read the contents of the buffer back, 
		///		it will only ever write data. Locking a buffer with this flag will ALWAYS 
		///		return a pointer to new, blank memory rather than the memory associated 
		///		with the contents of the buffer; this avoids DMA stalls because you can 
		///		write to a new memory area while the previous one is being used
		/// </summary>
		WriteOnly = 4,
		/// <summary>
		///     Indicates that the application will be refilling the contents
		///     of the buffer regularly (not just updating, but generating the
		///     contents from scratch), and therefore does not mind if the contents 
		///     of the buffer are lost somehow and need to be recreated. This
		///     allows and additional level of optimisation on the buffer.
		///     This option only really makes sense when combined with 
		///     DynamicWriteOnly.
		/// </summary>
		Discardable = 8,
		/// <summary>
		///    Combination of Static and WriteOnly
		/// </summary>
		StaticWriteOnly = 5,
		/// <summary>
		///    Combination of Dynamic and WriteOnly. If you use 
		///    this, strongly consider using DynamicWriteOnlyDiscardable
		///    instead if you update the entire contents of the buffer very 
		///    regularly. 
		/// </summary>
		DynamicWriteOnly = 6,
		DynamicWriteOnlyDiscardable = 14
	}

	/// <summary>
	/// Enumerates the categories of capabilities
	/// </summary>
	public enum CapabilitiesCategory
	{
		Common = 0,
		Common2 = 1,
		D3D9 = 2,
		GL = 3
	}

	[AxiomHelper(0, 8, "Utility class for holding few constants")]
	internal static class CapsUtil
	{
		public const int Categories = 4;
		public const int Shift = ( 32 - Categories );
		public const int Mask = (((1 << Categories) - 1) << Shift);
	}

	[AxiomHelper(0, 8, "Utility enum used to build Capabilities values")]
	internal enum CapCategoryShift
	{
		Common = CapabilitiesCategory.Common << CapsUtil.Shift,
		Common2 = CapabilitiesCategory.Common2 << CapsUtil.Shift,
		D3D9 = CapabilitiesCategory.D3D9 << CapsUtil.Shift,
		GL = CapabilitiesCategory.GL << CapsUtil.Shift,
	}

	/// <summary>
	///	Various types of capabilities supported by hardware that must be checked.
	/// </summary>
	[Flags]
	public enum Capabilities
	{
		#region HardwareMipMaps

		/// <summary>
		///	Supports generating mipmaps in hardware.
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_AUTOMIPMAP in Ogre")]
		HardwareMipMaps = CapCategoryShift.Common | (1 << 0),

		#endregion

		#region Blending

		[OgreVersion(1, 7, 2790)]
		Blending = CapCategoryShift.Common | (1 << 1),

		#endregion

		#region AnisotropicFiltering

		/// <summary>
		/// Supports anisotropic texture filtering
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_ANISOTROPY in Ogre")]
		AnisotropicFiltering = CapCategoryShift.Common | (1 << 2),

		#endregion

		#region Dot3

		/// <summary>
		///	Supports fixed-function DOT3 texture blend.
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		Dot3 = CapCategoryShift.Common | (1 << 3),

		#endregion

		#region CubeMapping

		/// <summary>
		///	Supports cube mapping.
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		CubeMapping = CapCategoryShift.Common | (1 << 4),

		#endregion

		#region StencilBuffer

		/// <summary>
		///	Supports hardware stencil buffer.
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_HWSTENCIL in Ogre")]
		StencilBuffer = CapCategoryShift.Common | (1 << 5),

		#endregion

		#region VertexBuffer

		/// <summary>
		///	Supports hardware vertex and index buffers.
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_VBO in Ogre")]
		VertexBuffer = CapCategoryShift.Common | (1 << 7),

		#endregion

		#region VertexPrograms

		/// <summary>
		///	Supports vertex programs (vertex shaders).
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		VertexPrograms = CapCategoryShift.Common | (1 << 9),

		#endregion

		#region FragmentPrograms

		/// <summary>
		///	Supports fragment programs (pixel shaders).
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		FragmentPrograms = CapCategoryShift.Common | (1 << 10),

		#endregion

		#region ScissorTest

		/// <summary>
		///	Supports performing a scissor test to exclude areas of the screen.
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		ScissorTest = CapCategoryShift.Common | (1 << 11),

		#endregion

		#region TwoSidedStencil

		/// <summary>
		///	Supports separate stencil updates for both front and back faces.
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		TwoSidedStencil = CapCategoryShift.Common | (1 << 12),

		#endregion

		#region StencilWrap

		/// <summary>
		///	Supports wrapping the stencil value at the range extremeties.
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		StencilWrap = CapCategoryShift.Common | (1 << 13),

		#endregion

		#region HardwareOcculusion

		/// <summary>
		///	Supports hardware occlusion queries.
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_HWOCCLUSION in Ogre")]
		HardwareOcculusion = CapCategoryShift.Common | (1 << 14),

		#endregion

		#region UserClipPlanes

		/// <summary>
		///	Supports user clipping planes.
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		UserClipPlanes = CapCategoryShift.Common | (1 << 15),

		#endregion

		#region VertexFormatUByte4

		/// <summary>
		///	Supports the VET_UBYTE4 vertex element type
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		VertexFormatUByte4 = CapCategoryShift.Common | (1 << 16),

		#endregion

		#region InfiniteFarPlane

		/// <summary>
		///	Supports infinite far plane projection
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		InfiniteFarPlane = CapCategoryShift.Common | (1 << 17),

		#endregion

		#region HardwareRenderToTexture

		/// <summary>
		/// Supports hardware render-to-texture (bigger than framebuffer)
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_HWRENDER_TO_TEXTURE in Ogre")]
		HardwareRenderToTexture = CapCategoryShift.Common | (1 << 18),

		#endregion

		#region TextureFloat

		/// <summary>
		/// Supports float textures and render targets
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		TextureFloat = CapCategoryShift.Common | (1 << 19),

		#endregion  

		#region NonPowerOf2Textures

		/// <summary>
		/// Supports non-power of two textures
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		NonPowerOf2Textures = CapCategoryShift.Common | (1 << 20),

		#endregion

		#region Texture3D

		/// <summary>
		/// Supports 3d (volume) textures
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		Texture3D = CapCategoryShift.Common | (1 << 21),

		#endregion

		#region PointSprites

		/// <summary>
		/// Supports basic point sprite rendering
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		PointSprites = CapCategoryShift.Common | (1 << 22),

		#endregion

		#region PointExtendedParameters

		/// <summary>
		/// Supports extra point parameters (minsize, maxsize, attenuation)
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		PointExtendedParameters = CapCategoryShift.Common | (1 << 23),

		#endregion

		#region VertexTextureFetch

		/// <summary>
		///	Supports vertex texture fetch
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		VertexTextureFetch = CapCategoryShift.Common | (1 << 24),

		#endregion

		#region MipmapLODBias

		/// <summary>
		/// Supports mipmap LOD biasing
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		MipmapLODBias = CapCategoryShift.Common | (1 << 25),

		#endregion

		#region FragmentPrograms

		/// <summary>
		///	Supports hardware geometry programs
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		GeometryPrograms = CapCategoryShift.Common | (1 << 26),

		#endregion

		#region HardwareRenderToVertexBuffer

		/// <summary>
		/// HardwareRenderToVertexBuffer
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		HardwareRenderToVertexBuffer = CapCategoryShift.Common | ( 1 << 27),

		#endregion

		#region TextureCompression

		/// <summary>
		///	Supports compressed textures.
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		TextureCompression = CapCategoryShift.Common2 | (1 << 0),

		#endregion

		#region TextureCompressionDXT

		/// <summary>
		///	Supports compressed textures in the DXT/ST3C formats.
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		TextureCompressionDXT = CapCategoryShift.Common2 | (1 << 1),

		#endregion

		#region TextureCompressionVTC

		/// <summary>
		///	Supports compressed textures in the VTC format.
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		TextureCompressionVTC = CapCategoryShift.Common2 | (1 << 2),

		#endregion

		#region TextureCompressionPVRTC

		/// <summary>
		/// Supports compressed textures in the PVRTC format
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		TextureCompressionPVRTC = CapCategoryShift.Common2 | (1 << 3),

		#endregion

		#region FixedFunction

		/// <summary>
		/// Supports fixed-function pipeline
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		FixedFunction = CapCategoryShift.Common2 | (1 << 4),

		#endregion

		#region MRTDifferentBitDepths

		/// <summary>
		/// Supports MRTs with different bit depths
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		MRTDifferentBitDepths = CapCategoryShift.Common2 | (1 << 5),

		#endregion

		#region AlphaToCoverage

		/// <summary>
		/// Supports Alpha to Coverage (A2C)
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		AlphaToCoverage = CapCategoryShift.Common2 | (1 << 6),

		#endregion

		#region AdvancedBlendOperations

		/// <summary>
		/// Supports Blending operations other than +
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		AdvancedBlendOperations = CapCategoryShift.Common2 | (1 << 7),

		#endregion

		#region RTTSerperateDepthBuffer

		/// <summary>
		/// Supports a separate depth buffer for RTTs. D3D 9 & 10, OGL w/FBO (RSC_FBO implies this flag)
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		RTTSerperateDepthBuffer = CapCategoryShift.Common2 | (1 << 8),

		#endregion

		#region RTTMainDepthbufferAttachable

		/// <summary>
		/// Supports using the MAIN depth buffer for RTTs. D3D 9&10, OGL w/FBO support unknown
		/// (undefined behavior?), OGL w/ copy supports it
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		RTTMainDepthbufferAttachable = CapCategoryShift.Common2 | (1 << 9),

		#endregion
		
		#region RTTDepthbufferResolutionLessEqual

		/// <summary>
		/// Supports attaching a depth buffer to an RTT that has width & height less or equal than RTT's.
		/// Otherwise must be of _exact_ same resolution. D3D 9, OGL 3.0 (not 2.0, not D3D10)
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		RTTDepthbufferResolutionLessEqual = CapCategoryShift.Common2 | (1 << 10),

		#endregion

		#region VertexBufferInstanceData

		/// <summary>
		/// Supports using vertex buffers for instance data
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		VertexBufferInstanceData = CapCategoryShift.Common2 | (1 << 11),

		#endregion

		#region CanGetCompiledShaderBuffer

		[OgreVersion(1, 7, 2790)]
		CanGetCompiledShaderBuffer = CapCategoryShift.Common2 | (1 << 12),

		#endregion

		// ***** DirectX specific caps *****

		#region CanGetCompiledShaderBuffer

		/// <summary>
		/// Is DirectX feature "per stage constants" supported
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		PerStageConstant = CapCategoryShift.D3D9 | (1 << 0),

		#endregion

		// ***** GL Specific Caps *****

		#region GL15NoVbo

		/// <summary>
		/// Supports openGL GLEW version 1.5
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_GL1_5_NOVBO in Ogre")]
		GL15NoVbo = CapCategoryShift.GL | (1 << 1),

		#endregion

		#region FrameBufferObjects

		/// <summary>
		/// Support for Frame Buffer Objects (FBOs)
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_FBO in Ogre")]
		FrameBufferObjects = CapCategoryShift.GL | (1 << 2),

		#endregion

		#region FrameBufferObjectsARB

		/// <summary>
		/// Support for Frame Buffer Objects ARB implementation (regular FBO is higher precedence)
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_FBO_ARB in Ogre")]
		FrameBufferObjectsARB = CapCategoryShift.GL | (1 << 3),

		#endregion

		#region FrameBufferObjectsATI

		/// <summary>
		/// Support for Frame Buffer Objects ATI implementation (ARB FBO is higher precedence)
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_FBO_ATI in Ogre")]
		FrameBufferObjectsATI = CapCategoryShift.GL | (1 << 4),

		#endregion

		#region PBuffer

		/// <summary>
		/// Support for PBuffer
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		PBuffer = CapCategoryShift.GL | (1 << 5),

		#endregion

		#region GL15NoHardwareOcclusion

		/// <summary>
		/// Support for GL 1.5 but without HW occlusion workaround
		/// </summary>
		[OgreVersion(1, 7, 2790, "RSC_GL1_5_NOHWOCCLUSION in Ogre")]
		GL15NoHardwareOcclusion = CapCategoryShift.GL | (1 << 6),

		#endregion

		#region PointExtendedParametersARB

		/// <summary>
		/// Support for point parameters ARB implementation
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		PointExtendedParametersARB = CapCategoryShift.GL | (1 << 7),

		#endregion

		#region PointExtendedParametersEXT

		/// <summary>
		/// Support for point parameters EXT implementation
		/// </summary>
		[OgreVersion(1, 7, 2790)]
		PointExtendedParametersEXT = CapCategoryShift.GL | (1 << 8),

		#endregion

	}

	/// <summary>
	///  Comparison functions, for things such as stencil buffer or depth comparisons.
	/// </summary>
	public enum CompareFunction
	{
		[ScriptEnum( "always_fail" )]
		AlwaysFail,
		[ScriptEnum( "always_pass" )]
		AlwaysPass,
		[ScriptEnum( "less" )]
		Less,
		[ScriptEnum( "less_equal" )]
		LessEqual,
		[ScriptEnum( "equal" )]
		Equal,
		[ScriptEnum( "not_equal" )]
		NotEqual,
		[ScriptEnum( "greater_equal" )]
		GreaterEqual,
		[ScriptEnum( "greater" )]
		Greater
	};

	/// <summary>
	/// Options for deciding what geometry gets excluded from the rendering process.
	/// </summary>
	public enum CullingMode
	{
		/// <summary>
		///		Draw everything (2 sided geometry).
		///	 </summary>
		[ScriptEnum( "none" )]
		None,
		/// <summary>
		///		Only draw geomtry where vertices were specified in clockwise order.
		///	 </summary>
		[ScriptEnum( "clockwise" )]
		Clockwise,
		/// <summary>
		///		Only draw geomtry where vertices were specified in counterclockwise order.
		///	 </summary>
		[ScriptEnum( "anticlockwise" )]
		CounterClockwise
	}

	/// <summary>
	///		Specifes the type of environment mapping to use.
	/// </summary>
	/// <remarks>
	///    Note that these have no effect when using the programmable pipeline, since their
	///    effect is overridden by the vertex / fragment programs.
	/// </remarks>
	public enum EnvironmentMap
	{
		/// <summary>
		///		Envmap based on vector from camera to vertex position, good for planar geometry.
		///	 </summary>
		[ScriptEnum( "spherical" )]
		Curved,
		/// <summary>
		///		Envmap based on dot of vector from camera to vertex and vertex normal, good for curves.
		///	 </summary>
		[ScriptEnum( "planar" )]
		Planar,
		/// <summary>
		///		Envmap entended to supply reflection vectors for cube mapping.
		/// </summary>
		[ScriptEnum( "cubic_reflection" )]
		Reflection,
		/// <summary>
		///		Envmap entended to supply normal vectors for cube mapping
		/// </summary>
		[ScriptEnum( "cubic_normal" )]
		Normal
	}

	/// <summary>
	///     A type of face group, i.e. face list of procedural etc
	/// </summary>
	public enum FaceGroup
	{
		FaceList,
		Patch,
		Unknown
	}

	/// <summary>
	///    Filtering options for textures / mipmaps.
	/// </summary>
	public enum FilterOptions
	{
		/// <summary>
		///    No filtering, used for FilterType.Mip to turn off mipmapping.
		/// </summary>
		[ScriptEnum( "none" )]
		None,
		/// <summary>
		///    Use the closest pixel.
		/// </summary>
		[ScriptEnum( "point" )]
		Point,
		/// <summary>
		///    Average of a 2x2 pixel area, denotes bilinear for Min and Mag, trilinear for Mip.
		/// </summary>
		[ScriptEnum( "linear" )]
		Linear,
		/// <summary>
		///    Similar to Linear, but compensates for the angle of the texture plane.
		/// </summary>
		[ScriptEnum( "anisotropic" )]
		Anisotropic
	}

	/// <summary>
	///    Stages of texture rendering to which filters can be applied.
	/// </summary>
	public enum FilterType
	{
		/// <summary>
		///    The filter used when shrinking a texture.
		/// </summary>
		Min,
		/// <summary>
		///    The filter used when magnifiying a texture.
		/// </summary>
		Mag,
		/// <summary>
		///    The filter used when determining the mipmap.
		/// </summary>
		Mip
	}

	/// <summary>
	/// Type of fog to use in the scene.
	/// </summary>
	public enum FogMode
	{
		/// <summary>
		///		No fog.
		///	 </summary>
		[ScriptEnum( "none" )]
		None,
		/// <summary>
		///		Fog density increases exponentially from the camera (fog = 1/e^(distance * density)).
		///	 </summary>
		[ScriptEnum( "exp" )]
		Exp,
		/// <summary>
		///		Fog density increases at the square of FOG_EXP, i.e. even quicker (fog = 1/e^(distance * density)^2).
		///	 </summary>
		[ScriptEnum( "exp2" )]
		Exp2,
		/// <summary>
		///		Fog density increases linearly between the start and end distances.
		///	 </summary>
		[ScriptEnum( "linear" )]
		Linear
	}

	/// <summary>
	///    Enumerates the types of programs which can run on the GPU.
	/// </summary>
	public enum GpuProgramType
	{
		/// <summary>
		///    Executes for each vertex passed through the pipeline while this program is active.
		/// </summary>
		Vertex,
		/// <summary>
		///    Executes for each fragment (or pixel) for primitives that are passed through the pipeline
		///    while this program is active..
		/// </summary>
		Fragment,
		/// <summary>
		///    Executes for each geometry for primitives that are passed through the pipeline
		///    while this program is active..
		/// </summary>
		Geometry
	}

	/// <summary>
	///    Enumerates the types of parameters that can be specified for shaders
	/// </summary>
	public enum GpuProgramParameterType
	{
		/// <summary>
		///    Parameter is passed in by index. Used for ASM shaders.
		/// </summary>
		Indexed,

		/// <summary>
		///    Parameter is managed by Axiom and passed in by index. Used for ASM shaders.
		/// </summary>

		IndexedAuto,

		/// <summary>
		///    Parameter is passed in by name. Used for high-level shaders.
		/// </summary>
		Named,

		/// <summary>
		///    Parameter is managed by Axiom and passed in by name. Used for HL shaders.
		/// </summary>
		NamedAuto
	}


	/// <summary>
	///		Defines the frame buffers which can be cleared.
	/// </summary>
	[Flags]
	public enum FrameBufferType
	{
		Color = 0x1,
		Depth = 0x2,
		Stencil = 0x4
	}

	/// <summary>
	///		Describes the stage of rendering when performing complex illumination.
	/// </summary>
	public enum IlluminationRenderStage
	{
		/// <summary>
		///		No special illumination stage.
		/// </summary>
		None,
		/// <summary>
		///		Ambient stage, when background light is added.
		/// </summary>
		Ambient,
		/// <summary>
		///		Diffuse / specular stage, when individual light contributions are added.
		/// </summary>
		PerLight,
		/// <summary>
		///		Decal stage, when texture detail is added to the lit base.
		/// </summary>
		Decal,
		/// <summary>
		///		Render to texture stage, used for texture based shadows.
		/// </summary>
		RenderToTexture,
		/// <summary>
		///		Render from shadow texture to receivers stage.
		/// </summary>
		RenderReceiverPass
	}

	/// <summary>
	///		Possible stages of illumination during the rendering process.
	/// </summary>
	public enum IlluminationStage
	{
		/// <summary>
		///		Part of the rendering which occurs without any kind of direct lighting.
		/// </summary>
		Ambient,
		/// <summary>
		///		Part of the rendering which occurs per light.
		/// </summary>
		PerLight,
		/// <summary>
		///		Post-lighting rendering.
		/// </summary>
		Decal
	}

	/// <summary>
	///		Type of index buffer to use.
	/// </summary>
	/// <remarks>
	///		No declarations can begin with a number, so Size prefix is used.
	/// </remarks>
	public enum IndexType
	{
		Size16,
		Size32
	}

	/// <summary>
	///		Lists the texture layer operations that are available on both multipass and multitexture
	///		hardware.
	/// </summary>
	public enum LayerBlendOperation
	{
		/// <summary>
		///		Replace all color with texture and no adjustment.
		/// </summary>
		[ScriptEnum( "replace" )]
		Replace,
		/// <summary>
		///		Add color components together.
		/// </summary>
		[ScriptEnum( "add" )]
		Add,
		/// <summary>
		///		Multiply the color components together.
		/// </summary>
		[ScriptEnum( "modulate" )]
		Modulate,
		/// <summary>
		///		Blend based on texture alpha.
		/// </summary>
		[ScriptEnum( "alpha_blend" )]
		AlphaBlend
	}

	/// <summary>
	///		Full and complete list of valid texture blending operations.  Fallbacks will be required on older hardware
	///		that does not supports some of these multitexturing techniques.
	/// </summary>
	public enum LayerBlendOperationEx
	{
		/// <summary>
		///		Use source 1 as is.
		/// </summary>
		[ScriptEnum( "source1" )]
		Source1,
		/// <summary>
		///		Use source 2 as is.
		/// </summary>
		[ScriptEnum( "source2" )]
		Source2,
		/// <summary>
		///		Multiply source 1 and source 2 together.
		/// </summary>
		[ScriptEnum( "modulate" )]
		Modulate,
		/// <summary>
		///		Same as Modulate, but brightens as a result.
		/// </summary>
		[ScriptEnum( "modulate_x2" )]
		ModulateX2,
		/// <summary>
		///		Same as ModuleX2, but brightens even more as a result.
		/// </summary>
		[ScriptEnum( "modulate_x4" )]
		ModulateX4,
		/// <summary>
		///		Add source 1 and source 2 together.
		/// </summary>
		[ScriptEnum( "add" )]
		Add,
		/// <summary>
		///		Same as Add, but subtracts 0.5 from the result.
		/// </summary>
		[ScriptEnum( "add_signed" )]
		AddSigned,
		/// <summary>
		///		Same as Add, but subtracts the product from the sum.
		/// </summary>
		[ScriptEnum( "add_smooth" )]
		AddSmooth,
		/// <summary>
		///		Subtract source 2 from source 1.
		/// </summary>
		[ScriptEnum( "subtract" )]
		Subtract,
		/// <summary>
		///		Use interpolated alpha value from vertices to scale source 1, then add source 2 scaled by 1 - alpha
		/// </summary>
		[ScriptEnum( "blend_diffuse_alpha" )]
		BlendDiffuseAlpha,
		/// <summary>
		///		Same as BlendDiffuseAlpha, but uses current alpha from the texture.
		/// </summary>
		[ScriptEnum( "blend_texture_alpha" )]
		BlendTextureAlpha,
		/// <summary>
		///		Same as BlendDiffuseAlpha, but uses current alpha from previous stages.
		/// </summary>
		[ScriptEnum( "blend_current_alpha" )]
		BlendCurrentAlpha,
		/// <summary>
		///		Sames as BlendDiffuseAlpha, but uses a constant manual blend value from [0.0,1.0]
		/// </summary>
		[ScriptEnum( "blend_manual" )]
		BlendManual,
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum( "blend_diffuse_color" )]
		BlendDiffuseColor,
		/// <summary>
		///		Takes the dot product of color 1 and color 2.
		/// </summary>
		[ScriptEnum( "dotproduct" )]
		DotProduct
	}

	/// <summary>
	///		Valid sources of values for texture layer blending operations.
	/// </summary>
	public enum LayerBlendSource
	{
		/// <summary>
		///		The color as built up from previous stages.
		/// </summary>
		[ScriptEnum( "src_current" )]
		Current,
		/// <summary>
		///		The color derived from the texture assigned to the current layer.
		/// </summary>
		[ScriptEnum( "src_texture" )]
		Texture,
		/// <summary>
		///		The interpolated diffuse color from the vertices.
		/// </summary>
		[ScriptEnum( "src_diffuse" )]
		Diffuse,
		/// <summary>
		///		The interpolated specular color from the vertices.
		/// </summary>
		[ScriptEnum( "src_specular" )]
		Specular,
		/// <summary>
		///		A color supplied manually as a seperate argument.
		/// </summary>
		[ScriptEnum( "src_manual" )]
		Manual
	}

	/// <summary>
	///		Texture blending mode.
	/// </summary>
	public enum LayerBlendType
	{
		/// <summary>
		///		Based on diffuse color of the texture.
		/// </summary>
		[ScriptEnum( "color" )]
		Color,
		/// <summary>
		///		Based on the alpha value of the texture.
		/// </summary>
		[ScriptEnum( "alpha" )]
		Alpha
	}

	/// <summary>
	///		Defines the types of lights that can be added to a scene.
	/// </summary>
	public enum LightType
	{
		/// <summary>
		///		Point light sources give off light equally in all directions, so require only position not direction.
		///	 </summary>
		[ScriptEnum( "point" )]
		Point,
		/// <summary>
		///		Directional lights simulate parallel light beams from a distant source, hence have direction but no position.
		///	 </summary>
		[ScriptEnum( "directional" )]
		Directional,
		/// <summary>
		///		Spotlights simulate a cone of light from a source so require position and direction, plus extra values for falloff.
		///	 </summary>
		[ScriptEnum( "spot" )]
		Spotlight
	}

	/// <summary>
	///		Manual culling modes based on vertex normals.
	///		This setting applies to how the software culls triangles before sending them to the 
	///		hardware API. This culling mode is used by scene managers which choose to implement it -
	///		normally those which deal with large amounts of fixed world geometry which is often 
	///		planar (software culling movable variable geometry is expensive).
	/// </summary>
	public enum ManualCullingMode
	{
		/// <summary>
		///		No culling so everything is sent to the hardware.
		///	 </summary>
		[ScriptEnum( "none" )]
		None = 1,
		/// <summary>
		///		Cull triangles whose normal is pointing away from the camera (default).
		///	 </summary>
		[ScriptEnum( "back" )]
		Back = 2,
		/// <summary>
		///		Cull triangles whose normal is pointing towards the camera.
		///	 </summary>
		[ScriptEnum( "front" )]
		Front = 3
	}

	/// <summary>
	/// Type of projection used by the camera.
	/// </summary>
	public enum Projection
	{
		/// <summary> Things stay the same size no matter where they are in terms of the camera.  Normally only used in 3D HUD elements. </summary>
		Orthographic,
		/// <summary> Things get smaller when they are furthur away from the camera. </summary>
		Perspective
	}

	/// <summary>
	///		Types for determining which render operation to do for a series of vertices.
	/// </summary>
	public enum OperationType
	{
		/// <summary>
		///		Render the vertices as individual points.
		/// </summary>
		PointList = 1,
		/// <summary>
		///		Render the vertices as a series of individual lines.
		/// </summary>
		LineList,
		/// <summary>
		///		Render the vertices as a continuous line.
		/// </summary>
		LineStrip,
		/// <summary>
		///		Render the vertices as a series of individual triangles.
		/// </summary>
		TriangleList,
		/// <summary>
		///		Render the vertices as a continous set of triangles in a zigzag type fashion.
		/// </summary>
		TriangleStrip,
		/// <summary>
		///		Render the vertices as a set of trinagles in a fan like formation.
		/// </summary>
		TriangleFan
	}

	/// <summary>
	///    Specifies priorities for processing Render Targets.
	/// </summary>
	public enum RenderTargetPriority: byte
	{
		/// <summary>
		///    Will be processed last.
		/// </summary>
		Default = 4,
		/// <summary>
		///    Will be processed first (i.e. RenderTextures).
		/// </summary>
		RenderToTexture = 2
	}

	/// <summary>
	///		Blending factors for manually blending objects with the scene. If there isn't a predefined
	///		SceneBlendType that you like, then you can specify the blending factors directly to affect the
	///		combination of object and the existing scene. See Material.SceneBlending for more details.
	/// </summary>
	public enum SceneBlendFactor
	{
		/// <summary></summary>
		[ScriptEnum( "one" )]
		One,
		/// <summary></summary>
		[ScriptEnum( "zero" )]
		Zero,
		/// <summary></summary>
		[ScriptEnum( "dest_colour" )]
		DestColor,
		/// <summary></summary>
		[ScriptEnum( "src_colour" )]
		SourceColor,
		/// <summary></summary>
		[ScriptEnum( "one_minus_dest_colour" )]
		OneMinusDestColor,
		/// <summary></summary>
		[ScriptEnum( "one_minus_src_colour" )]
		OneMinusSourceColor,
		/// <summary></summary>
		[ScriptEnum( "dest_alpha" )]
		DestAlpha,
		/// <summary></summary>
		[ScriptEnum( "src_alpha" )]
		SourceAlpha,
		/// <summary></summary>
		[ScriptEnum( "one_minus_dest_alpha" )]
		OneMinusDestAlpha,
		/// <summary></summary>
		[ScriptEnum( "one_minus_src_alpha" )]
		OneMinusSourceAlpha
	}

	/// <summary>
	///		Types of blending that you can specify between an object and the existing contents of the scene.
	/// </summary>
	public enum SceneBlendType
	{
		/// <summary>
		///		Make the object transparent based on the final alpha values in the texture.
		///	 </summary>
		[ScriptEnum( "alpha_blend" )]
		TransparentAlpha,
		/// <summary>
		///		Make the object transparent based on the color values in the texture (brighter = more opaque).
		///	 </summary>
		[ScriptEnum( "colour_blend" )]
		[ScriptEnum( "color_blend" )]
		TransparentColor,
		/// <summary>
		///		Make the object transparent based on the color values in the texture (brighter = more opaque).
		///	 </summary>
		[ScriptEnum( "modulate" )]
		Modulate,
		/// <summary>
		///		Add the texture values to the existing scene content.
		///	 </summary>
		[ScriptEnum( "add" )]
		Add,
		/// <summary>
		/// The default blend mode where source replaces destination
		/// </summary>
		[ScriptEnum( "replace" )]
		Replace,
	}

	public enum SceneBlendOperation
	{
		Add,
		Subtract,
		ReverseSubtract,
		Min,
		Max
	}

	/// <summary>
	/// The broad type of detail for rendering.
	/// </summary>
	public enum PolygonMode
	{
		/// <summary>
		///		Render subsequent requests drawing only the vertices in the scene.
		/// </summary>
		Points,
		/// <summary>
		///		Render subsequent requests drawing only objects using wireframe mode.
		/// </summary>
		Wireframe,
		/// <summary>
		///		Render everything in the scene normally (textures, etc).
		/// </summary>
		Solid
	}

	/// <summary>
	/// Types for deciding how to shade geometry primitives.
	/// </summary>
	public enum ShadeOptions
	{
		/// <summary>
		///		Draw with a single color.
		///	 </summary>
		[ScriptEnum( "flat" )]
		Flat,
		/// <summary>
		///		Interpolate color across primitive vertices.
		///	 </summary>
		[ScriptEnum( "gouraud" )]
		Gouraud,
		/// <summary>
		///		Draw everything (2 sided geometry).
		///	 </summary>
		[ScriptEnum( "phong" )]
		Phong
	}

	/// <summary>
	///		A set of flags that can be used to influence <see cref="ShadowRenderable"/> creation.
	/// </summary>
	public enum ShadowRenderableFlags
	{
		/// <summary>
		///		For shadow volume techniques only, generate a light cap on the volume.
		/// </summary>
		IncludeLightCap = 1,
		/// <summary>
		///		For shadow volume techniques only, generate a dark cap on the volume.
		/// </summary>
		IncludeDarkCap = 2,
		/// <summary>
		///		For shadow volume techniques only, indicates volume is extruded to infinity
		/// </summary>
		ExtrudeToInfinity = 4
	}

	/// <summary>
	///	An enumeration of broad shadow techniques .
	/// </summary>
	public enum ShadowTechnique
	{
		/// <summary>
		///		No shadows.
		/// </summary>
		None,
		/// <summary>
		///		Stencil shadow technique which renders all shadow volumes as
		///		a modulation after all the non-transparent areas have been 
		///		rendered. This technique is considerably less fillrate intensive 
		///		than the additive stencil shadow approach when there are multiple
		///		lights, but is not an accurate model. 
		/// </summary>
		StencilModulative,
		///	<summary>		
		///		Stencil shadow technique which renders each light as a separate
		///		additive pass to the scene. This technique can be very fillrate
		///		intensive because it requires at least 2 passes of the entire
		///		scene, more if there are multiple lights. However, it is a more
		///		accurate model than the modulative stencil approach and this is
		///		especially apparant when using coloured lights or bump mapping.
		/// </summary>
		StencilAdditive,
		/// <summary>
		///		Texture-based shadow technique which involves a monochrome render-to-texture
		///		of the shadow caster and a projection of that texture onto the 
		///		shadow receivers as a modulative pass.
		/// </summary>
		TextureModulative,
		/// <summary>
		///		Texture-based shadow technique which involves a render-to-texture
		///		of the shadow caster and a projection of that texture onto the 
		///		shadow receivers, followed by a depth test to detect the closest
		///		fragment to the light.
		/// </summary>
		TextureAdditive
	}

	/// <summary>
	///		Describes the various actions which can be taken on the stencil buffer.
	///	</summary> 
	[OgreVersion(1, 7, 2790)]
	public enum StencilOperation
	{
		/// <summary>
		///		Leave the stencil buffer unchanged.
		///	 </summary>
		Keep,
		/// <summary>
		///		Set the stencil value to zero.
		///	 </summary>
		Zero,
		/// <summary>
		///		Set the stencil value to the reference value.
		///	 </summary>
		Replace,
		/// <summary>
		///		Increase the stencil value by 1, clamping at the maximum value.
		///	 </summary>
		Increment,
		/// <summary>
		///		Decrease the stencil value by 1, clamping at 0.
		///	 </summary>
		Decrement,
		/// <summary>
		///		Increase the stencil value by 1, wrapping back to 0 when incrementing the maximum value.
		///	 </summary>
		IncrementWrap,
		/// <summary>
		///		Decrease the stencil value by 1, wrapping when decrementing 0.
		///	 </summary>
		DecrementWrap,
		/// <summary>
		///		Invert the bits of the stencil buffer.
		///	 </summary>
		Invert
	};

	/// <summary>
	/// Texture addressing modes - default is Wrap.
	/// </summary>
	/// <remarks>
	///    These settings are relevant in both the fixed-function and programmable pipeline.
	/// </remarks>
	public enum TextureAddressing
	{
		/// <summary>
		///		Texture wraps at values over 1.0 
		///	 </summary>
		[ScriptEnum( "wrap" )]
		Wrap,
		/// <summary>
		///		Texture mirrors (flips) at joins over 1.0.
		///	 </summary>
		[ScriptEnum( "mirror" )]
		Mirror,
		/// <summary>
		///		Texture clamps at 1.0.
		///	 </summary>
		[ScriptEnum( "clamp" )]
		Clamp,
		/// <summary>
		///		Values outside the range [0.0, 1.0] are set to the border colour
		///	 </summary>
		[ScriptEnum( "border" )]
		Border
	}

	/// <summary>
	///		Describes the ways to generate texture coordinates.
	/// </summary>
	[OgreVersion(1, 7, 2790)]
	public enum TexCoordCalcMethod
	{
		/// <summary>
		///		No calculated texture coordinates.
		///	 </summary>
		None,
		/// <summary>
		///		Environment map based on vertex normals.
		///	 </summary>
		EnvironmentMap,
		/// <summary>
		///		Environment map based on vertex positions.
		///	 </summary>
		EnvironmentMapPlanar,
		EnvironmentMapReflection,
		EnvironmentMapNormal,
		/// <summary>
		///		Projective texture.
		///	 </summary>
		ProjectiveTexture
	}

	/// <summary>
	/// Enum identifying the frame indexes for faces of a cube map (not the composite 3D type.
	/// </summary>
	public enum TextureCubeFace
	{
		Front,
		Back,
		Left,
		Right,
		Up,
		Down
	}

	/// <summary>
	///    Definition of the broad types of texture effect you can apply to a texture layer.
	/// </summary>
	/// <remarks>
	///    Note that these have no effect when using the programmable pipeline, since their
	///    effect is overridden by the vertex / fragment programs.
	/// </remarks>
	public enum TextureEffectType
	{
		/// <summary>
		///		Generate all texture coords based on angle between camera and vertex.
		///	 </summary>
		EnvironmentMap,
		/// <summary>
		///		Generate texture coords based on a frustum.
		///	 </summary>
		ProjectiveTexture,
		/// <summary>
		///		Constant u/v scrolling effect.
		///	 </summary>
		UVScroll,
		/// <summary>
		///		Constant u scrolling effect.
		///	 </summary>
		UScroll,
		/// <summary>
		///		Constant v scrolling effect.
		///	 </summary>
		VScroll,
		/// <summary>
		///		Constant rotation.
		///	 </summary>
		Rotate,
		/// <summary>
		///		More complex transform.
		///	 </summary>
		Transform
	}

	/// <summary>
	///    Texture filtering defining the different minification and magnification.
	/// </summary>
	public enum TextureFiltering
	{
		/// <summary>
		///		Equal to: min=Point, mag=Point, mip=None
		///	 </summary>
		[ScriptEnum( "none" )]
		None,
		/// <summary>
		///		Equal to: min=Linear, mag=Linear, mip=Point
		///	 </summary>
		[ScriptEnum( "bilinear" )]
		Bilinear,
		/// <summary>
		///		Equal to: min=Linear, mag=Linear, mip=Linear
		///	 </summary>
		[ScriptEnum( "trilinear" )]
		Trilinear,
		/// <summary>
		///    Equal to: min=Anisotropic, max=Anisotropic, mip=Linear
		/// </summary>
		[ScriptEnum( "anisotropic" )]
		Anisotropic
	}

	/// <summary>
	/// Useful enumeration when dealing with procedural transforms.
	/// </summary>
	/// <remarks>
	///    Note that these have no effect when using the programmable pipeline, since their
	///    effect is overridden by the vertex / fragment programs.
	/// </remarks>
	public enum TextureTransform
	{
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum( "scroll_x" )]
		TranslateU,
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum( "scroll_y" )]
		TranslateV,
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum( "scale_x" )]
		ScaleU,
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum( "scale_y" )]
		ScaleV,
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum( "rotate" )]
		Rotate
	}

	/// <summary>
	///    Enum identifying the texture type.
	/// </summary>
	public enum TextureType
	{
		/// <summary>
		///    1D texture, used in combination with 1D texture coordinates.
		/// </summary>
		[ScriptEnum( "1d" )]
		OneD = 1,
		/// <summary>
		///    2D texture, used in combination with 2D texture coordinates (default).
		/// </summary>
		[ScriptEnum( "2d" )]
		TwoD = 2,
		/// <summary>
		///    3D volume texture, used in combination with 3D texture coordinates.
		/// </summary>
		[ScriptEnum( "3d" )]
		ThreeD = 3,
		/// <summary>
		///    3D cube map, used in combination with 3D texture coordinates.
		/// </summary>
		[ScriptEnum( "cubic" )]
		CubeMap = 4
	}

	/// <summary>
	/// Enum identifying special mipmap numbers
	/// </summary>
	public enum TextureMipmap
	{
		/// Generate mipmaps up to 1x1
		Unlimited = 0x7FFFFFFF,
		/// Use TextureManager default
		Default = -1
	}

	/// <summary>
	///		Specifies how a texture is to be used in the engine.
	/// </summary>
	[Flags]
	public enum TextureUsage
	{
		Static = BufferUsage.Static,
		Dynamic = BufferUsage.Dynamic,
		WriteOnly = BufferUsage.WriteOnly,
		StaticWriteOnly = BufferUsage.StaticWriteOnly,
		DynamicWriteOnly = BufferUsage.DynamicWriteOnly,
		DynamicWriteOnlyDiscardable = BufferUsage.DynamicWriteOnlyDiscardable,
		/// <summary>
		///    Mipmaps will be automatically generated for this texture
		///	 </summary>
		AutoMipMap = 0x100,
		/// <summary>
		///    This texture will be a render target, ie. used as a target for render to texture
		///    setting this flag will ignore all other texture usages except AutoMipMap
		///	 </summary>
		RenderTarget = 0x200,
		/// <summary>
		///    Default to automatic mipmap generation static textures
		///	</summary>
		Default = AutoMipMap | StaticWriteOnly
	}

	/// <summary>
	///		Types for definings what information a vertex will hold.
	/// </summary>
	/// <remarks>
	///		Implemented with the Flags attribute to treat this enum with bitwise addition
	///		and comparisons.
	/// </remarks>
	[Flags]
	public enum VertexFlags
	{
		/// <summary>
		///		Specifies the 3D coordinates of the vertex.
		///	 </summary>
		Position = 1,
		/// <summary>
		///		When applying 1 or more world matrices to a vertex, the weight values of a vertex dictate how much
		///		of an effect each matrix has in determining its final position.  
		/// </summary>
		BlendWeights = 2,
		/// <summary>
		///		Normal vector, determines the logical direction the vertex is facing for use in
		///		lighting calculations.
		///	 </summary>
		Normals = 4,
		/// <summary>
		///		Texture coordinate for the vertex.
		///	 </summary>
		TextureCoords = 8,
		/// <summary>
		///		The primary color of the vertex.
		/// </summary>
		Diffuse = 16,
		/// <summary>
		///		Specular color for this vertex.
		///	 </summary>
		Specular = 32
	}

	/// <summary>
	///     Vertex element semantics, used to identify the meaning of vertex buffer contents.
	/// </summary>
	public enum VertexElementSemantic
	{
		/// <summary>
		///     Position, 3 reals per vertex.
		/// </summary>
		Position = 1,
		/// <summary>
		///     Blending weights.
		/// </summary>
		BlendWeights = 2,
		/// <summary>
		///     Blending indices.
		/// </summary>
		BlendIndices = 3,
		/// <summary>
		///     Normal, 3 reals per vertex.
		/// </summary>
		Normal = 4,
		/// <summary>
		///     Diffuse colors.
		/// </summary>
		Diffuse = 5,
		/// <summary>
		///     Specular colors.
		/// </summary>
		Specular = 6,
		/// <summary>
		///     Texture coordinates.
		/// </summary>
		TexCoords = 7,
		/// <summary>
		///     Binormal (Y axis if normal is Z).
		/// </summary>
		Binormal = 8,
		/// <summary>
		///     Tangent (X axis if normal is Z).
		/// </summary>
		Tangent = 9
	}

	/// <summary>
	///     Vertex element type, used to identify the base types of the vertex contents.
	/// </summary>
	public enum VertexElementType
	{
		Float1,
		Float2,
		Float3,
		Float4,
		Color,
		Short1,
		Short2,
		Short3,
		Short4,
		UByte4,
		/// D3D style compact colour
		Color_ARGB = 10,
		/// GL style compact colour
		Color_ABGR = 11

	}

	/// <summary>
	///     The types of compositing passes
	/// </summary>
	public enum CompositorPassType
	{
		/// <summary>
		/// Clear target to one colour
		/// </summary>
		[ScriptEnum("clear")]
		Clear,

		/// <summary>
		/// Set stencil operation
		/// </summary>
		[ScriptEnum("stencil")]
		Stencil,

		/// <summary>
		/// Render the scene or part of it
		/// </summary>
		[ScriptEnum("render_scene")]
		RenderScene,

		/// <summary>
		/// Render a full screen quad
		/// </summary>
		[ScriptEnum("render_quad")]
		RenderQuad,

		[ScriptEnum("render_custom")]
		RenderCustom
	}

	/// <summary>
	///     Input mode of a TargetPass
	/// </summary>
	public enum CompositorInputMode
	{
		None,            // No input
		Previous         // Output of previous Composition in chain
	}
}
