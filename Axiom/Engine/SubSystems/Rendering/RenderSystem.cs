#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Collections;
using System.Reflection;
using Axiom.Core;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.SubSystems.Rendering;
using Axiom.Utility;
using Axiom.MathLib;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	/// Defines the functionality of a 3D API
	/// </summary>
    ///	<remarks>
	///		The RenderSystem class provides a base class
	///		which abstracts the general functionality of the 3D API
	///		e.g. Direct3D or OpenGL. Whilst a few of the general
	///		methods have implementations, most of this class is
	///		abstract, requiring a subclass based on a specific API
	///		to be constructed to provide the full functionality.
	///		<p/>
	///		Note there are 2 levels to the interface - one which
	///		will be used often by the caller of the engine library,
	///		and one which is at a lower level and will be used by the
	///		other classes provided by the engine. These lower level
	///		methods are marked as internal, and are not accessible outside
	///		of the Core library.
	///	</remarks>
	// INC: In progress
	public abstract class RenderSystem : IDisposable
	{
		#region Member variables

		protected RenderWindowCollection renderWindows;
		protected TextureManager textureMgr;
		protected HardwareBufferManager hardwareBufferManager;
		protected CullingMode cullingMode;
		protected bool isVSync;
		protected bool depthWrite;

		// Stored options
		protected EngineConfig engineConfig = new EngineConfig();

		// Active viewport (dest for future rendering operations) and target
		protected Viewport activeViewport;
		protected RenderTarget activeRenderTarget;

		// Store record of texture unit settings for efficient alterations
		protected TextureLayer[] textureUnits = new TextureLayer[Config.MaxTextureLayers];

		protected int numFaces, numVertices;

		// used to determine capabilies of the hardware
		protected HardwareCaps caps = new HardwareCaps();

		/// Saved set of world matrices
		protected Matrix4[] worldMatrices = new Matrix4[256];

		/// Temporary buffer for vertex blending in software
		/// TODO: Revisit this when software vertex blending gets implemented
		protected float[] tempVertexBlendBuffer;
		protected float[] tempNormalBlendBuffer;

		#endregion

		#region Constructor

		public RenderSystem()
		{
			this.renderWindows = new RenderWindowCollection();
			
			// default to true
			isVSync = true;

			// default to true
			depthWrite = true;

			// This means CULL clockwise vertices, i.e. front of poly is counter-clockwise
			// This makes it the same as OpenGL and other right-handed systems
			this.cullingMode = CullingMode.Clockwise; 

			// init the texture layer array
			for(int i = 0; i < Config.MaxTextureLayers; i++)
				textureUnits[i] = new TextureLayer();
		}


		#endregion

		#region Public properties

		/// <summary>
		/// Gets the name of this RenderSystem based on it's assembly attribute Title.
		/// </summary>
		public virtual String Name
		{
			get
			{
				AssemblyTitleAttribute attribute = 
					(AssemblyTitleAttribute)Attribute.GetCustomAttribute(this.GetType().Assembly, typeof(AssemblyTitleAttribute), false);

				if(attribute != null)
					return attribute.Title;
				else
					return "Not Found";
			}
		}

		/// <summary>
		/// Gets/Sets a value that determines whether or not to wait for the screen to finish refreshing
		/// before drawing the next frame.
		/// </summary>
		public bool IsVSync
		{
			get { return this.isVSync; }
			set { this.isVSync = value; }
		}

		/// <summary>
		///		Gets a set of hardware capabilities queryed by the current render system.
		/// </summary>
		public HardwareCaps Caps
		{
			get { return caps; }
		}

		/// <summary>
		/// Gets a dataset with the options set for the rendering system.
		/// </summary>
		public EngineConfig ConfigOptions
		{
			get { return this.engineConfig; }
		}

		/// <summary>
		/// Gets a collection of the RenderSystems list of RenderWindows.
		/// </summary>
		public RenderWindowCollection RenderWindows
		{
			get { return this.renderWindows; }
		}

		/// <summary>
		/// 
		/// </summary>
		public int FacesRendered
		{
			get { return numFaces; }
		}

		#endregion

		#region Abstract properties

		/// <summary>
		///		Sets the color & strength of the ambient (global directionless) light in the world.
		/// </summary>
		abstract public ColorEx AmbientLight { set; }

		/// <summary>
		///		Sets the type of light shading required (default = Gouraud).
		/// </summary>
		abstract public Shading ShadingType { set; }

		/// <summary>
		///		Sets the type of texture filtering used when rendering
		///	</summary>
		///	<remarks>
		///		This method sets the kind of texture filtering applied when rendering textures onto
		///		primitives. Filtering covers how the effects of minification and magnification are
		///		disguised by resampling.
		/// </remarks>
		abstract public TextureFiltering TextureFiltering { set; }

		/// <summary>
		///		Sets whether or not dynamic lighting is enabled.
		///		<p/>
		///		If true, dynamic lighting is performed on geometry with normals supplied, geometry without
		///		normals will not be displayed. If false, no lighting is applied and all geometry will be full brightness.
		/// </summary>
		abstract public bool LightingEnabled { set; }

		/// <summary>
		///		Turns stencil buffer checking on or off. 
		/// </summary>
		///	<remarks>
		///		Stencilling (masking off areas of the rendering target based on the stencil 
		///		buffer) can be turned on or off using this method. By default, stencilling is
		///		disabled.
		///	</remarks>
		abstract public bool StencilCheckEnabled { set; }

		/// <summary>
		///		Determines the bit depth of the hardware accelerated stencil buffer, if supported.
		/// </summary>
		/// <remarks>
		///		If hardware stencilling is not supported, the software will provide an 8-bit 
		///		software stencil.
		///	</remarks>
		abstract public short StencilBufferBitDepth { get; }

		/// <summary>
		///		Sets the stencil test function.
		/// </summary>
		/// <remarks>
		///		The stencil test is:
		///		(Reference Value & Mask) CompareFunction (Stencil Buffer Value & Mask)
		///	</remarks>
		abstract public CompareFunction StencilBufferFunction { set; }

		/// <summary>
		///		Sets the stencil buffer reference value.
		/// </summary>
		///	<remarks>
		///		This value is used in the stencil test:
		///		(Reference Value & Mask) CompareFunction (Stencil Buffer Value & Mask)
		///		It can also be used as the destination value for the stencil buffer if the
		///		operation which is performed is StencilOperation.Replace.
		/// </remarks>
		abstract public long StencilBufferReferenceValue { set; }

		/// <summary>
		///		Sets the stencil buffer mask value.
		/// </summary>
		///<remarks>
		///		This is applied thus:
		///		(Reference Value & Mask) CompareFunction (Stencil Buffer Value & Mask)
		///	</remarks>
		abstract public long StencilBufferMask { set; }

		/// <summary>
		///		Sets the action to perform if the stencil test fails.
		/// </summary>
		abstract public StencilOperation StencilBufferFailOperation { set; }

		/// <summary>
		///		Sets the action to perform if the stencil test passes, but the depth
		///		buffer test fails.
		/// </summary>
		abstract public StencilOperation StencilBufferDepthFailOperation { set; }

		/// <summary>
		///		Sets the action to perform if both the stencil test and the depth buffer 
		///		test passes.
		/// </summary>
		abstract public StencilOperation StencilBufferPassOperation { set; }

		#endregion

		#region Overridable virtual methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="autoCreateWindow"></param>
		virtual public RenderWindow Initialize(bool autoCreateWindow)
		{
			// initialize the MeshManager
			MeshManager.Init();

			// return null here, all subclasses of RenderSystem MUST override this, and
			// call this base class method in their first line
			return null;
		}

		/// <summary>
		///		Shuts down the RenderSystem.
		/// </summary>
		virtual public void Shutdown()
		{
			// destroy each render window
			foreach(RenderWindow window in renderWindows)
			{
				window.Destroy();
			}

			// Clear the render window list
			renderWindows.Clear();

			// dispose of the render system
			this.Dispose();
		}
		#endregion

		#region Abstract methods

		/// <summary>
		///		Should be implemented by each subclass to interogate the caps of the hardware using
		///		the specific API.
		/// </summary>
		abstract public void CheckCaps();

		/// <summary>
		/// Creates a new rendering window.
		/// </summary>
		/// <remarks>
		///	This method creates a new rendering window as specified
		///	by the paramteters. The rendering system could be
		///	responible for only a single window (e.g. in the case
		///	of a game), or could be in charge of multiple ones (in the
		///	case of a level editor). The option to create the window
		///	as a child of another is therefore given.
		///	This method will create an appropriate subclass of
		/// RenderWindow depending on the API and platform implementation.
		/// </remarks>
		/// <returns></returns>
		abstract public RenderWindow CreateRenderWindow(String name, System.Windows.Forms.Control target, int width, int height, int colorDepth,
			bool isFullscreen, int left, int top, bool depthBuffer, RenderWindow parent);

		/// <summary>
		///		Builds a perspective projection matrix suitable for this render system.
		/// </summary>
		/// <remarks>
		///		Because different APIs have different requirements (some incompatible) for the
		///		projection matrix, this method allows each to implement their own correctly and pass
		///		back a generic Matrix3 for storage in the engine.
		///	 </remarks>
		/// <param name="fov"></param>
		/// <param name="aspectRatio"></param>
		/// <param name="near"></param>
		/// <param name="far"></param>
		/// <returns></returns>
		abstract public Matrix4 MakeProjectionMatrix(float fov, float aspectRatio, float near, float far);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="light"></param>
		abstract internal protected void AddLight(Light light);
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="light"></param>
		abstract internal protected void UpdateLight(Light light);

		/// <summary>
		///		Sets the fog with the given params.
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="color"></param>
		/// <param name="density"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		abstract internal protected void SetFog(FogMode mode, ColorEx color, float density, float start, float end);

		/// <summary>
		///		Converts the System.Drawing.Color value to a uint.  Each API may need the 
		///		bytes of the packed color data in different orders. i.e. OpenGL - ABGR, D3D - ARGB
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		abstract public int ConvertColor(ColorEx color);

		/// <summary>
		///		Sets the global blending factors for combining subsequent renders with the existing frame contents.
		///		The result of the blending operation is:</p>
		///		<p align="center">final = (texture * src) + (pixel * dest)</p>
		///		Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
		///		enumerated type.
		/// </summary>
		/// <param name="src">The source factor in the above calculation, i.e. multiplied by the texture colour components.</param>
		/// <param name="dest">The destination factor in the above calculation, i.e. multiplied by the pixel colour components.</param>
		abstract internal protected void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest);

		/// <summary>
		///		Sets the surface parameters to be used during rendering an object.
		/// </summary>
		/// <param name="ambient"></param>
		/// <param name="diffuse"></param>
		/// <param name="specular"></param>
		/// <param name="emissive"></param>
		/// <param name="shininess"></param>
		abstract internal protected void SetSurfaceParams(ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess);

		/// <summary>
		///		Tells the hardware how to treat texture coordinates.
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="texAddressingMode"></param>
		abstract internal protected void SetTextureAddressingMode(int stage, TextureAddressing texAddressingMode);

		#endregion

		#region Protected methods

		/// <summary>
		///		Performs a software vertex blend on the passed in operation. 
		///	</summary>
		///	<remarks>
		///		This function is supplied to calculate a vertex blend when no hardware
		///		support is available. The vertices contained in the passed in operation
		///		will be modified by the matrices supplied according to the blending weights
		///		also in the operation. To avoid accidentally modifying core vertex data, a
		///		temporary vertex buffer is used for the result, which is then used in the
		///		VertexBuffer instead of the original passed in vertex data.
		/// </remarks>
		protected void SoftwareVertexBlend(RenderOperation op, Matrix4[] matrices)
		{
			// TODO: Implementation of RenderSystem.SoftwareVertexBlend
		}

		#endregion

		#region Object overrides

		/// <summary>
		/// Returns the name of this RenderSystem.
		/// </summary>
		/// <returns></returns>
		public override String ToString()
		{
			return this.Name;
		}

		#endregion

		#region Internal engine methods and properties

		/// <summary>
		/// 
		/// </summary>
		abstract protected internal bool DepthWrite { set; }

		/// <summary>
		/// 
		/// </summary>
		abstract protected internal bool DepthCheck { set; }

		/// <summary>
		/// 
		/// </summary>
		abstract protected internal bool DepthFunction { set; }

		/// <summary>
		/// 
		/// </summary>
		abstract protected internal ushort DepthBias { set; }

		/// <summary>Sets the current view matrix.</summary>
		abstract protected internal Matrix4 ViewMatrix	{ set; }

		/// <summary>Sets the current world matrix.</summary>
		abstract protected internal Matrix4 WorldMatrix { set; }

		/// <summary>Sets the current projection matrix.</summary>
		abstract protected internal Matrix4 ProjectionMatrix { set; }

		abstract protected VertexDeclaration VertexDeclaration { get; set; }
		abstract protected VertexBufferBinding VertexBufferBinding { get; set; }

		/// <summary>
		///		Sets how to rasterise triangles, as points, wireframe or solid polys.
		/// </summary>
		abstract protected internal SceneDetailLevel RasterizationMode { set; }

		/// <summary>
		///		Signifies the beginning of a frame, ie the start of rendering on a single viewport. Will occur
        ///		several times per complete frame if multiple viewports exist.
		/// </summary>
		abstract protected internal void BeginFrame();

		/// <summary>
		///		Ends rendering of a frame to the current viewport.
		/// </summary>
		abstract protected internal void EndFrame();

		/// <summary>
		///		Sets the details of a texture stage, to be used for all primitives
		///		rendered afterwards. User processes would
		///		not normally call this direct unless rendering
		///		primitives themselves - the SubEntity class
		///		is designed to manage materials for objects.
		///		Note that this method is called by SetMaterial.
		/// </summary>
		/// <param name="stage">The index of the texture unit to modify. Multitexturing hardware
		//		can support multiple units (see NumTextureUnits)</param>
		/// <param name="enabled">Boolean to turn the unit on/off</param>
		/// <param name="textureName">The name of the texture to use - this should have
		///		already been loaded with TextureManager.Load.</param>
		abstract protected internal void SetTexture(int stage, bool enabled, String textureName);

		/// <summary>
		///		Sets a method for automatically calculating texture coordinates for a stage.
		/// </summary>
		/// <param name="stage">Texture stage to modify.</param>
		/// <param name="method">Calculation method to use</param>
		abstract protected internal void SetTextureCoordCalculation(int stage, TexCoordCalcMethod method);

		/// <summary>
		///		Sets the index into the set of tex coords that will be currently used by the render system.
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="index"></param>
		abstract protected internal void SetTextureCoordSet(int stage, int index);

		/// <summary>
		///		Sets the texture matrix for the specified stage.  Used to apply rotations, translations,
		///		and scaling to textures.
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="xform"></param>
		abstract protected internal void SetTextureMatrix(int stage, Matrix4 xform);

		/// <summary>
		///		Sets the current viewport that will be rendered to.
		/// </summary>
		/// <param name="viewport"></param>
		abstract protected internal void SetViewport(Viewport viewport);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="op"></param>
		/// DOC
		public virtual void Render(RenderOperation op)
		{
			int val;

			if(op.useIndices)
				val = op.indexData.indexCount;
			else
				val = op.vertexData.vertexCount;

			// calculate faces
			switch(op.operationType)
			{
				case RenderMode.TriangleList:
					numFaces += val / 3;
					break;
				case RenderMode.TriangleStrip:
				case RenderMode.TriangleFan:
					numFaces += val - 2;
					break;
				case RenderMode.PointList:
				case RenderMode.LineList:
				case RenderMode.LineStrip:
					break;
			}

			// increment running vertex count
			numVertices += op.vertexData.vertexCount;

			// if hardware vertex blending isn't supported, check if we need to do
			// it in software
			if(!Caps.CheckCap(Capabilities.VertexBlending))
			{
				bool vertexBlend = false;

				IList elements = op.vertexData.vertexDeclaration.Elements;
				
				// see if we need to calc ver
				for(int i = 0; i < elements.Count; i++)
				{
					VertexElement element = (VertexElement)elements[i];

					// if we found a blend weights element, flag and break
					if(element.Semantic == VertexElementSemantic.BlendWeights)
					{
						vertexBlend = true;
						break;
					}
				}

				if(vertexBlend)
					SoftwareVertexBlend(op, worldMatrices);
			}
		}

		/// <summary>
		///		Utility function for setting all the properties of a texture unit at once.
		///		This method is also worth using over the individual texture unit settings because it
		///		only sets those settings which are different from the current settings for this
		///		unit, thus minimising render state changes.
		/// </summary>
		/// <param name="textureUnit">Index of the texture unit to configure</param>
		/// <param name="layer">Reference to a TextureLayer object which defines all the settings.</param>
		virtual protected internal void SetTextureUnit(int stage, TextureLayer layer)
		{
			// TODO: Finish this

			// get a reference to the locally cached texture unit
			TextureLayer current = (TextureLayer)textureUnits[stage].Clone();

			bool isCurrentBlank = current.Blank;

			// set the texture if it is different from the current
			if(isCurrentBlank || current.TextureName != layer.TextureName)
				SetTexture(stage, true, layer.TextureName);

			// Tex Coord Set
			if(isCurrentBlank || current.TexCoordSet != layer.TexCoordSet)
				SetTextureCoordSet(stage, layer.TexCoordSet);

			// TODO: Texture layer filtering

			// TODO: Texture layer anistropy

			// Texture blend modes
			LayerBlendModeEx newBlend = layer.ColorBlendMode;

			if(isCurrentBlank || current.ColorBlendMode != newBlend)
			{
				// set the texture blending mode
				SetTextureBlendMode(stage, newBlend);
			}

			newBlend = layer.AlphaBlendMode;
			if(isCurrentBlank || current.AlphaBlendMode != newBlend)
			{
				// set the texture blending mode
				SetTextureBlendMode(stage, newBlend);
			}

			// this must always be set for OpenGL.  DX9 will ignore dupe render states like this (observed in the
			// output window when debugging with high verbosity), so there is no harm
			SetTextureAddressingMode(stage, layer.TextureAddressing);

			bool anyCalcs = false;

			for(int i = 0; i < layer.Effects.Count; i++)
			{
				TextureEffect effect = (TextureEffect)layer.Effects[i];

				switch(effect.type)
				{
					case TextureEffectType.EnvironmentMap:
						if((EnvironmentMap)effect.subtype == EnvironmentMap.Curved)
						{
							SetTextureCoordCalculation(stage, TexCoordCalcMethod.EnvironmentMap);
							anyCalcs = true;
						}
						else if((EnvironmentMap)effect.subtype == EnvironmentMap.Planar)
						{
							SetTextureCoordCalculation(stage, TexCoordCalcMethod.EnvironmentMapPlanar);
							anyCalcs = true;
						}
						else if((EnvironmentMap)effect.subtype == EnvironmentMap.Reflection)
						{
							SetTextureCoordCalculation(stage, TexCoordCalcMethod.EnvironmentMapReflection);
							anyCalcs = true;
						}
						else if((EnvironmentMap)effect.subtype == EnvironmentMap.Normal)
						{
							SetTextureCoordCalculation(stage, TexCoordCalcMethod.EnvironmentMapNormal);
							anyCalcs = true;
						}
						break;

					case TextureEffectType.BumpMap:
					case TextureEffectType.Scroll:
					case TextureEffectType.Rotate:
					case TextureEffectType.Transform:
						break;
				} // switch
			} // for

			// Ensure any previous texcoord calc settings are reset if there are now none
			if(!anyCalcs)
			{
				SetTextureCoordCalculation(stage, TexCoordCalcMethod.None);
				SetTextureCoordSet(stage, layer.TexCoordSet);
			}

			// set the texture matrix to that of the current layer for any transformations
			SetTextureMatrix(stage, layer.TextureMatrix);

			// store the layer
			textureUnits[stage] = layer;
		}

		/// <summary>
		///		Sets the texture blend modes from a TextureLayer record.
		///		Meant for use internally only - apps should use the Material
		///		and TextureLayer classes.
		/// </summary>
		/// <param name="stage">Texture unit.</param>
		/// <param name="blendMode">Details of the blending modes.</param>
		public abstract void SetTextureBlendMode(int stage, LayerBlendModeEx blendMode);

		/// <summary>
		///		Turns off a texture unit if not needed.
		/// </summary>
		/// <param name="textureUnit"></param>
		virtual protected internal void DisableTextureUnit(int textureUnit)
		{
			SetTexture(textureUnit, false, "");
			textureUnits[textureUnit].Blank = true;
		}

		/// <summary>
		///	
		/// </summary>
		/// <param name="matrices"></param>
		/// <param name="count"></param>
		virtual internal void SetWorldMatrices(Matrix4[] matrices, ushort count)
		{
			if(!caps.CheckCap(Capabilities.VertexBlending))
			{
				// save these for later during software vertex blending
				for(int i = 0; i < count; i++)
					worldMatrices[i] = matrices[i];

				// reset the hardware world matrix to identity
				WorldMatrix = Matrix4.Identity;
			}

			// TODO: Implement hardware vertex blending in the API's
		}

		/// <summary>
		/// 
		/// </summary>
		internal void BeginGeometryCount()
		{
			numFaces = 0;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			//if(textureMgr != null)
			//	textureMgr.Dispose();

		}

		#endregion
	}

}