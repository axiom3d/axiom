using System;
using System.Reflection;
using Axiom.Scripting;

namespace Axiom.SubSystems.Rendering
{
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
		ReadOnly
	}

	/// <summary>
	///		Describes how a vertex buffer is to be used, and affects how it is created.
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
		/// 
		/// </summary>
		StaticWriteOnly = 8,
		/// <summary>
		/// 
		/// </summary>
		DynamicWriteOnly = 16
	}

	/// <summary>
	///		Various types of capabilities supported by hardware that must be checked.
	/// </summary>
	[Flags]
	public enum Capabilities
	{
		StencilBuffer = 1,
		TextureBlending = 2,
		VertexBlending = 4,
		AnisotropicFiltering = 8,
		Dot3Bump = 16,
		VertexBuffer = 32,
		MultiTexturing = 64,
		HardwareMipMaps = 128
	}

	/// <summary>
	///  Comparison functions, for things such as stencil buffer or depth comparisons.
	/// </summary>
	public enum CompareFunction
	{
		AlwaysFail,
		AlwaysPass,
		Less,
		LessEqual,
		Equal,
		NotEqual,
		GreaterEqual,
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
		None,
		/// <summary>
		///		Only draw geomtry where vertices were specified in clockwise order.
		///	 </summary>
		Clockwise,
		/// <summary>
		///		Only draw geomtry where vertices were specified in counterclockwise order.
		///	 </summary>
		CounterClockwise
	}

	/// <summary>
	///		Specifes the type of environment mapping to use.
	/// </summary>
	public enum EnvironmentMap
	{
		/// <summary>
		///		Envmap based on vector from camera to vertex position, good for planar geometry.
		///	 </summary>
		[ScriptEnum("spherical")]
		Curved,
		/// <summary>
		///		Envmap based on dot of vector from camera to vertex and vertex normal, good for curves.
		///	 </summary>
		[ScriptEnum("planar")]
		Planar,
		/// <summary>
		///		Envmap entended to supply reflection vectors for cube mapping.
		/// </summary>
		[ScriptEnum("cubic_reflection")]
		Reflection,
		/// <summary>
		///		Envmap entended to supply normal vectors for cube mapping
		/// </summary>
		[ScriptEnum("cubic_normal")]
		Normal
	}

	/// <summary>
	/// Type of fog to use in the scene.
	/// </summary>
	public enum FogMode
	{
		/// <summary>
		///		No fog.
		///	 </summary>
		None,
		/// <summary>
		///		Fog density increases exponentially from the camera (fog = 1/e^(distance * density)).
		///	 </summary>
		Exp,
		/// <summary>
		///		Fog density increases at the square of FOG_EXP, i.e. even quicker (fog = 1/e^(distance * density)^2).
		///	 </summary>
		Exp2,
		/// <summary>
		///		Fog density increases linearly between the start and end distances.
		///	 </summary>
		Linear
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
		[ScriptEnum("replace")]
		Replace,
		/// <summary>
		///		Add color components together.
		/// </summary>
		[ScriptEnum("add")]
		Add,
		/// <summary>
		///		Multiply the color components together.
		/// </summary>
		[ScriptEnum("modulate")]
		Modulate,
		/// <summary>
		///		Blend based on texture alpha.
		/// </summary>
		[ScriptEnum("alpha_blend")]
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
		Source1,
		/// <summary>
		///		Use source 2 as is.
		/// </summary>
		Source2,
		/// <summary>
		///		Multiply source 1 and source 2 together.
		/// </summary>
		Modulate,
		/// <summary>
		///		Same as Modulate, but brightens as a result.
		/// </summary>
		ModulateX2,
		/// <summary>
		///		Same as ModuleX2, but brightens even more as a result.
		/// </summary>
		ModulateX4,
		/// <summary>
		///		Add source 1 and source 2 together.
		/// </summary>
		Add,
		/// <summary>
		///		Same as Add, but subtracts 0.5 from the result.
		/// </summary>
		AddSigned,
		/// <summary>
		///		Same as Add, but subtracts the product from the sum.
		/// </summary>
		AddSmooth,
		/// <summary>
		///		Subtract source 2 from source 1.
		/// </summary>
		Subtract,
		/// <summary>
		///		Use interpolated alpha value from vertices to scale source 1, then add source 2 scaled by 1 - alpha
		/// </summary>
		BlendDiffuseAlpha,
		/// <summary>
		///		Same as BlendDiffuseAlpha, but uses current alpha from the texture.
		/// </summary>
		BlendTextureAlpha,
		/// <summary>
		///		Same as BlendDiffuseAlpha, but uses current alpha from previous stages.
		/// </summary>
		BlendCurrentAlpha,
		/// <summary>
		///		Sames as BlendDiffuseAlpha, but uses a constant manual blend value from [0.0,1.0]
		/// </summary>
		BlendManual,
		/// <summary>
		///		Takes the dot product of color 1 and color 2.
		/// </summary>
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
		Current,
		/// <summary>
		///		The color derived from the texture assigned to the current layer.
		/// </summary>
		Texture,
		/// <summary>
		///		The interpolated diffuse color from the vertices.
		/// </summary>
		Diffuse,
		/// <summary>
		///		The interpolated specular color from the vertices.
		/// </summary>
		Specular,
		/// <summary>
		///		A color supplied manually as a seperate argument.
		/// </summary>
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
		Color,
		/// <summary>
		///		Based on the alpha value of the texture.
		/// </summary>
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
		Point,
		/// <summary>
		///		Directional lights simulate parallel light beams from a distant source, hence have direction but no position.
		///	 </summary>
		Directional,
		/// <summary>
		///		Spotlights simulate a cone of light from a source so require position and direction, plus extra values for falloff.
		///	 </summary>
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
		None = 1,
		/// <summary>
		///		Cull triangles whose normal is pointing away from the camera (default).
		///	 </summary>
		Back = 2,
		/// <summary>
		///		Cull triangles whose normal is pointing towards the camera.
		///	 </summary>
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
	public enum RenderMode
	{
		/// <summary>
		///		Render the vertices as individual points.
		/// </summary>
		PointList,
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
	///		Blending factors for manually blending objects with the scene. If there isn't a predefined
	///		SceneBlendType that you like, then you can specify the blending factors directly to affect the
	///		combination of object and the existing scene. See Material.SceneBlending for more details.
	/// </summary>
	// TODO: Comment types
	public enum SceneBlendFactor
	{
		/// <summary></summary>
		One,
		/// <summary></summary>
		Zero,
		/// <summary></summary>
		DestColor,
		/// <summary></summary>
		SourceColor,
		/// <summary></summary>
		OneMinusDestColor,
		/// <summary></summary>
		OneMinusSourceColor,
		/// <summary></summary>
		DestAlpha,
		/// <summary></summary>
		SourceAlpha,
		/// <summary></summary>
		OneMinusDestAlpha,
		/// <summary></summary>
		OneMinusSourceAlpha,
		/// <summary>
		///		Blend pixels together by taking the dot product of the color values being blended.
		/// </summary>
		Dot3
	}

	/// <summary>
	///		Types of blending that you can specify between an object and the existing contents of the scene.
	/// </summary>
	public enum SceneBlendType
	{
		/// <summary>
		///		Make the object transparent based on the final alpha values in the texture.
		///	 </summary>
		[ScriptEnum("alpha_blend")]
		TransparentAlpha,
		/// <summary>
		///		Make the object transparent based on the color values in the texture (brighter = more opaque).
		///	 </summary>
		[ScriptEnum("modulate")]
		TransparentColor,
		/// <summary>
		///		Add the texture values to the existing scene content.
		///	 </summary>
		[ScriptEnum("add")]
		Add
	}

	/// <summary>
	/// The broad type of detail for rendering.
	/// </summary>
	public enum SceneDetailLevel
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
	public enum Shading
	{
		/// <summary>
		///		Draw with a single color.
		///	 </summary>
		Flat,
		/// <summary>
		///		Interpolate color across primitive vertices.
		///	 </summary>
		Gouraud,
		/// <summary>
		///		Draw everything (2 sided geometry).
		///	 </summary>
		Phong
	}

	/// <summary>
	///		Describes the various actions which can be taken on the stencil buffer.
	///	</summary> 
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
		///		Invert the bits of the stencil buffer.
		///	 </summary>
		Invert
	};

	/// <summary>
	/// Texture addressing modes - default is Wrap.
	/// </summary>
	public enum TextureAddressing
	{
		/// <summary>
		///		Texture wraps at values over 1.0 
		///	 </summary>
		[ScriptEnum("wrap")] 
		Wrap,
		/// <summary>
		///		Texture mirrors (flips) at joins over 1.0.
		///	 </summary>
		[ScriptEnum("mirror")]
		Mirror,
		/// <summary>
		///		Texture clamps at 1.0.
		///	 </summary>
		[ScriptEnum("clamp")]
		Clamp
	}

	/// <summary>
	///		Describes the ways to generate texture coordinates.
	/// </summary>
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
		EnvironmentMapNormal
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
	/// Definition of the broad types of texture effect you can apply to a texture layer.
	/// </summary>
	public enum TextureEffectType
	{
		/// <summary>
		///		Bump mapping.
		///	 </summary>
		BumpMap,
		/// <summary>
		///		Generate all texture coords based on angle between camera and vertex.
		///	 </summary>
		EnvironmentMap,
		/// <summary>
		///		Constant u/v scrolling effect.
		///	 </summary>
		Scroll,
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
	/// Modes for improving the visual quality of rendered textures.
	/// </summary>
	public enum TextureFiltering
	{
		/// <summary>
		///		No filtering.
		///	 </summary>
		None,
		/// <summary>
		///		Good lookin, slower than none.
		///	 </summary>
		Bilinear,
		/// <summary>
		///		Even better looking, but even slower than bilinear.
		///	 </summary>
		Trilinear
	}

	/// <summary>
	/// Useful enumeration when dealing with procedural transforms.
	/// </summary>
	public enum TextureTransform
	{
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum("scroll_x")]
		TranslateU,
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum("scroll_y")]
		TranslateV,
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum("scale_x")]
		ScaleU,
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum("scale_y")]
		ScaleV,
		/// <summary>
		/// 
		/// </summary>
		[ScriptEnum("rotate")]
		Rotate
	}

	/// <summary>
	///		Specifies how a texture is to be used in the engine.
	/// </summary>
	public enum TextureUsage
	{
		/// <summary>
		///		Standard usage.
		///	 </summary>
		Default,
		/// <summary>
		///		Target of rendering.  Example would be a billboard in a wrestling or sports game, or rendering a 
		///		movie to a texture.
		///	 </summary>
		RenderTarget
	}

	/// <summary>
	///		Types for definings what information a vertex will hold.
	/// </summary>
	/// <remarks>
	///		Implemented with the Flags attribute to treat this enum with bitwise addition
	///		and comparisons.
	/// </remarks>
	// TODO: Verify ordering
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
		Specular = 32,

	}

	/// <summary>
	/// 
	/// </summary>
	/// DOC
	public enum VertexElementSemantic
	{
		Position,
		Normal,
		BlendWeights,
		BlendIndices,
		Diffuse,
		Specular,
		TexCoords
	}

	/// <summary>
	/// 
	/// </summary>
	/// DOC
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
		Short4
	}
}
