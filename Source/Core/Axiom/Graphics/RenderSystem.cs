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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Graphics.Collections;
using Axiom.Math;
using Axiom.Math.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// Defines the functionality of a 3D API
	/// </summary>
	/// <remarks>
	/// The RenderSystem class provides a base class
	/// which abstracts the general functionality of the 3D API
	/// e.g. Direct3D or OpenGL. Whilst a few of the general
	/// methods have implementations, most of this class is
	/// abstract, requiring a subclass based on a specific API
	/// to be constructed to provide the full functionality.
	/// <p/>
	/// Note there are 2 levels to the interface - one which
	/// will be used often by the caller of the engine library,
	/// and one which is at a lower level and will be used by the
	/// other classes provided by the engine. These lower level
	/// methods are marked as internal, and are not accessible outside
	/// of the Core library.
	/// </remarks>
	public abstract class RenderSystem : DisposableObject
	{
		// Not implemented: RenderTargetIterator

		#region Constants

		private const string CommentNoVirt = "this is nonvirtual";

		private const string CommentDefOverride = "default parameter override";

		/// <summary>
		/// Default window title if one is not specified upon a call to <see cref="Initialize"/>.
		/// </summary>
		private const string DefaultWindowTitle = "Axiom Window";

		// TODO: should this go into Config?
		private const int NumRendertargetGroups = 10;

		#endregion Constants

		#region Inner Types

		public class RenderTargetMap : Dictionary<string, RenderTarget>
		{
		}

		public class RenderTargetPriorityMap : MultiMap<RenderTargetPriority, RenderTarget>
		{
		}

		/// <summary>
		/// Dummy structure for render system contexts - implementing RenderSystems can extend
		/// as needed
		/// </summary>
		public class RenderSystemContext : DisposableObject
		{
		}

		public class DepthBufferVec : List<DepthBuffer>
		{
		}

		public class DepthBufferMap : Dictionary<PoolId, DepthBufferVec>
		{
		}

		public class HardwareOcclusionQueryList : List<HardwareOcclusionQuery>
		{
		}

		#endregion

		#region Fields

		[OgreVersion( 1, 7, 2790 )] protected HardwareOcclusionQueryList hwOcclusionQueries = new HardwareOcclusionQueryList();

		/// <summary>
		/// List of current render targets (i.e. a <see cref="RenderWindow"/>, or a<see cref="RenderTexture"/>) by priority
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected RenderTargetPriorityMap prioritizedRenderTargets = new RenderTargetPriorityMap();

		/// <summary>
		/// List of current render targets (i.e. a <see cref="RenderWindow"/>, or a<see cref="RenderTexture"/>)
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected RenderTargetMap renderTargets = new RenderTargetMap();

		/// <summary>
		/// A reference to the texture management class specific to this implementation.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected TextureManager textureManager;

		/// <summary>
		/// Active render target.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected RenderTarget activeRenderTarget;


		[OgreVersion( 1, 7, 2790 )] protected int vSyncInterval;

		/// <summary>
		/// Capabilites of the current hardware (populated at startup).
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected RenderSystemCapabilities realCapabilities;

		[OgreVersion( 1, 7, 2790 )] protected bool useCustomCapabilities;

		[OgreVersion( 1, 7, 2790 )] protected int currentPassIterationNum;

		[OgreVersion( 1, 7, 2790 )] protected bool vertexProgramBound;

		[OgreVersion( 1, 7, 2790 )] protected bool fragmentProgramBound;

		[OgreVersion( 1, 7, 2790 )] protected bool geometryProgramBound;

		/// <summary>
		/// Saved manual color blends
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected ColorEx[,] manualBlendColors = new ColorEx[Config.MaxTextureLayers,2];

		[OgreVersion( 1, 7, 2790 )] protected int disabledTexUnitsFrom;

		[OgreVersion( 1, 7, 2790 )] protected bool derivedDepthBias;

		[OgreVersion( 1, 7, 2790 )] protected float derivedDepthBiasBase;

		[OgreVersion( 1, 7, 2790 )] protected float derivedDepthBiasMultiplier;

		[OgreVersion( 1, 7, 2790 )] protected float derivedDepthBiasSlopeScale;


		// The Active GPU programs and gpu program parameters
		[OgreVersion( 1, 7, 2790 )] protected GpuProgramParameters activeVertexGpuProgramParameters;

		[OgreVersion( 1, 7, 2790 )] protected GpuProgramParameters activeGeometryGpuProgramParameters;

		[OgreVersion( 1, 7, 2790 )] protected GpuProgramParameters activeFragmentGpuProgramParameters;

		[OgreVersion( 1, 7, 2790 )] protected DepthBufferMap depthBufferPool = new DepthBufferMap
		                                                                       {
		                                                                       	{
		                                                                       		PoolId.Default, new DepthBufferVec()
		                                                                       		},
		                                                                       	// { PoolId.ManualUsage, new DepthBufferVec() },
		                                                                       	{
		                                                                       		PoolId.NoDepth, new DepthBufferVec()
		                                                                       		}
		                                                                       };

		[OgreVersion( 1, 7, 2790 )] protected bool texProjRelative;

		[OgreVersion( 1, 7, 2790 )] protected Vector3 texProjRelativeOrigin;

		#endregion Fields

		#region Constructor

		/// <summary>
		/// Base constructor.
		/// </summary>
		protected RenderSystem()
			: base()
		{
			// This means CULL clockwise vertices, i.e. front of poly is counter-clockwise
			// This makes it the same as OpenGL and other right-handed systems
			cullingMode = CullingMode.Clockwise;


			vSync = true;
			vSyncInterval = 1;

			globalNumberOfInstances = 1;
			clipPlanesDirty = true;
		}

		#endregion

		#region Properties

		#region RenderTarget

		/// <summary>
		/// Set current render target to target, enabling its device context if needed
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract RenderTarget RenderTarget { set; }

		#endregion

		#region WaitForVerticalBlank

		[OgreVersion( 1, 7, 2790 )] protected bool vSync;

		/// <summary>
		/// Defines whether or now fullscreen render windows wait for the vertical blank before flipping buffers.
		/// </summary>
		/// <remarks>
		/// By default, all rendering windows wait for a vertical blank (when the CRT beam turns off briefly to move
		/// from the bottom right of the screen back to the top left) before flipping the screen buffers. This ensures
		/// that the image you see on the screen is steady. However it restricts the frame rate to the refresh rate of
		/// the monitor, and can slow the frame rate down. You can speed this up by not waiting for the blank, but
		/// this has the downside of introducing 'tearing' artefacts where part of the previous frame is still displayed
		/// as the buffers are switched. Speed vs quality, you choose.
		/// </remarks>
		/// <note>
		/// Has NO effect on windowed mode render targets. Only affects fullscreen mode.
		/// </note>
		[OgreVersion( 1, 7, 2790, CommentNoVirt )]
		public bool WaitForVerticalBlank
		{
			get
			{
				return vSync;
			}
			set
			{
				vSync = value;
			}
		}

		#endregion

		#region GlobalInstanceVertexBuffer

		[OgreVersion( 1, 7, 2790 )] private HardwareVertexBuffer globalInstanceVertexBuffer;

		/// <summary>
		/// a global vertex buffer for global instancing
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public HardwareVertexBuffer GlobalInstanceVertexBuffer
		{
			get
			{
				return globalInstanceVertexBuffer;
			}
			set
			{
				if ( value != null && !value.IsInstanceData )
				{
					throw new AxiomException( "A none instance data vertex buffer was set to be the global instance vertex buffer." );
				}
				globalInstanceVertexBuffer = value;
			}
		}

		#endregion

		#region GlobalInstanceVertexBufferVertexDeclaration

		[OgreVersion( 1, 7, 2790 )] protected VertexDeclaration globalInstanceVertexBufferVertexDeclaration;

		/// <summary>
		/// a vertex declaration for the global vertex buffer for the global instancing
		/// </summary>
		[OgreVersion( 1, 7, 2790, CommentNoVirt )]
		public VertexDeclaration GlobalInstanceVertexBufferVertexDeclaration
		{
			get
			{
				return globalInstanceVertexBufferVertexDeclaration;
			}
			set
			{
				globalInstanceVertexBufferVertexDeclaration = value;
			}
		}

		#endregion

		#region GlobalNumberOfInstances

		[OgreVersion( 1, 7, 2790 )] protected int globalNumberOfInstances;

		/// <summary>
		/// the number of global instances (this number will be multiply by the render op instance number) 
		/// </summary>
		[OgreVersion( 1, 7, 2790, CommentNoVirt )]
		public int GlobalNumberOfInstances
		{
			get
			{
				return globalNumberOfInstances;
			}
			set
			{
				globalNumberOfInstances = value;
			}
		}

		#endregion

		#region AreFixedFunctionLightsInViewSpace

		/// <summary>
		/// Are fixed-function lights provided in view space? Affects optimisation.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public bool AreFixedFunctionLightsInViewSpace
		{
			get
			{
				return false;
			}
		}

		#endregion

		#region PointSpritesEnabled

		/// <summary>
		/// Sets whether or not rendering points using OT_POINT_LIST will 
		/// render point sprites (textured quads) or plain points.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract bool PointSpritesEnabled { set; }

		#endregion

		#region RenderSystemCapabilities

		[OgreVersion( 1, 7, 2790 )] protected RenderSystemCapabilities currentCapabilities;

		/// <summary>
		/// Gets a set of hardware capabilities queryed by the current render system.
		/// </summary>
		[OgreVersion( 1, 7, 2790, CommentNoVirt )]
		public RenderSystemCapabilities MutableCapabilities
		{
			get
			{
				return currentCapabilities;
			}
		}

		/// <summary>
		///  Gets the capabilities of the render system
		/// </summary>
		[OgreVersion( 1, 7, 2790, CommentNoVirt )]
		public RenderSystemCapabilities Capabilities
		{
			get
			{
				return currentCapabilities;
			}
		}

		#endregion

		#region Viewport

		[OgreVersion( 1, 7, 2790, "Public due to Ogre design deficit: directly intruding into this using friend" )] public
			Viewport activeViewport;

		/// <summary>
		/// Get or set the current active viewport for rendering.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual Viewport Viewport
		{
			get
			{
				return activeViewport;
			}
			set
			{
				throw new NotImplementedException( "Abstract call: Viewport set" );
			}
		}

		#endregion

		#region CullingMode

		[OgreVersion( 1, 7, 2790 )] protected CullingMode cullingMode;

		/// <summary>
		/// Gets/Sets the culling mode for the render system based on the 'vertex winding'.
		/// </summary>
		/// <remarks>
		/// A typical way for the rendering engine to cull triangles is based on the
		/// 'vertex winding' of triangles. Vertex winding refers to the direction in
		/// which the vertices are passed or indexed to in the rendering operation as viewed
		/// from the camera, and will wither be clockwise or counterclockwise.  The default is <see name="CullingMode.Clockwise"/>  
		/// i.e. that only triangles whose vertices are passed/indexed in counterclockwise order are rendered - this 
		/// is a common approach and is used in 3D studio models for example. You can alter this culling mode 
		/// if you wish but it is not advised unless you know what you are doing. You may wish to use the 
		/// <see cref="Graphics.CullingMode.None"/> option for mesh data that you cull yourself where the vertex winding is uncertain.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual CullingMode CullingMode
		{
			get
			{
				return cullingMode;
			}
			set
			{
				throw new MethodAccessException( "Abstract call" );
			}
		}

		#endregion

		#region DepthBufferCheckEnabled

		/// <summary>
		/// Sets whether or not the depth buffer check is performed before a pixel write
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Default = true" )]
		public abstract bool DepthBufferCheckEnabled { set; }

		#endregion

		#region DepthBufferWriteEnabled

		/// <summary>
		/// Sets whether or not the depth buffer is updated after a pixel write.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Default = true" )]
		public abstract bool DepthBufferWriteEnabled { set; }

		#endregion

		#region DepthBufferFunction

		/// <summary>
		/// Sets the comparison function for the depth buffer check.
		/// Advanced use only - allows you to choose the function applied to compare the depth values of
		/// new and existing pixels in the depth buffer. Only an issue if the depth buffer check is enabled
		/// <see cref="DepthBufferCheckEnabled"/>
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Default = (CompareFunction.LessEqual" )]
		public abstract CompareFunction DepthBufferFunction { set; }

		#endregion

		#region DriverVersion

		[OgreVersion( 1, 7, 2790 )] protected DriverVersion driverVersion;

		/// <summary>
		///  Returns the driver version.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual DriverVersion DriverVersion
		{
			get
			{
				return driverVersion;
			}
		}

		#endregion

		#region DefaultViewportMaterialScheme

		/// <summary>
		///  Returns the driver version.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "No RTSHADER support" )]
		public virtual string DefaultViewportMaterialScheme
		{
			get
			{
				return MaterialManager.DefaultSchemeName;
			}
		}

		#endregion

		#region ClipPlanes

		[OgreVersion( 1, 7, 2790 )] protected PlaneList clipPlanes = new PlaneList();

		[OgreVersion( 1, 7, 2790 )] protected bool clipPlanesDirty;

		/// <summary>
		///  Returns the driver version.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual PlaneList ClipPlanes
		{
			set
			{
				if ( value == null )
				{
					throw new ArgumentNullException(); // Ogre passes this as ref so must be non null
				}
				if ( !value.Equals( clipPlanes ) )
				{
					clipPlanes = value;
					clipPlanesDirty = true;
				}
			}
		}

		#endregion

		#region ConfigOptions

		protected ConfigOptionMap configOptions = new ConfigOptionMap();

		/// <summary>
		/// Gets a dataset with the options set for the rendering system.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "abstract in Ogre" )]
		[AxiomHelper( 0, 8, "provides default backing field to reg options" )]
		public virtual ConfigOptionMap ConfigOptions
		{
			get
			{
				// return a COPY of the current config options
				return configOptions;
			}
		}

		#endregion ConfigOptions

		#region FaceCount

		[OgreVersion( 1, 7, 2790 )] protected int faceCount;

		/// <summary>
		/// Number of faces rendered during the current frame so far.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual int FaceCount
		{
			get
			{
				return faceCount;
			}
		}

		#endregion

		#region BatchCount

		[OgreVersion( 1, 7, 2790 )] protected int batchCount;

		/// <summary>
		/// Number of batches rendered during the current frame so far.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual int BatchCount
		{
			get
			{
				return batchCount;
			}
		}

		#endregion

		#region VertexCount

		[OgreVersion( 1, 7, 2790 )] protected int vertexCount;

		/// <summary>
		/// Number of vertices processed during the current frame so far.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual int VertexCount
		{
			get
			{
				return vertexCount;
			}
		}

		#endregion

		#region ColorVertexElementType

		/// <summary>
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract VertexElementType ColorVertexElementType { get; }

		#endregion

		#region VertexDeclaration

		/// <summary>
		/// Sets the current vertex declaration, ie the source of vertex data.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract VertexDeclaration VertexDeclaration { set; }

		#endregion

		#region VertexBufferBinding

		/// <summary>
		/// Sets the current vertex buffer binding state.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract VertexBufferBinding VertexBufferBinding { set; }

		#endregion

		#region CurrentPassIterationCount

		[OgreVersion( 1, 7, 2790 )] protected int currentPassIterationCount;

		/// <summary>
		/// Number of times to render the current state.
		/// </summary>
		/// <remarks>Set the current multi pass count value.  This must be set prior to 
		/// calling render() if multiple renderings of the same pass state are 
		/// required.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual int CurrentPassIterationCount
		{
			get
			{
				return currentPassIterationCount;
			}
			set
			{
				currentPassIterationCount = value;
			}
		}

		#endregion

		#region InvertVertexWinding

		[OgreVersion( 1, 7, 2790 )] protected bool invertVertexWinding;

		/// <summary>
		/// Sets whether or not vertex windings set should be inverted; this can be important
		/// for rendering reflections.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool InvertVertexWinding
		{
			get
			{
				return invertVertexWinding;
			}
			set
			{
				invertVertexWinding = value;
			}
		}

		#endregion

		#region Name

		/// <summary>
		/// Gets the name of this RenderSystem based on it's assembly attribute Title.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "abstract in Ogre" )]
		[AxiomHelper( 0, 8, "Axiom uses reflection to supply a default value" )]
		public virtual string Name
		{
			get
			{
				var attribute =
					(AssemblyTitleAttribute)
					Attribute.GetCustomAttribute( GetType().Assembly, typeof ( AssemblyTitleAttribute ), false );

				if ( attribute != null )
				{
					return attribute.Title;
				}
				//else
				return "Not Found";
			}
		}

		#endregion

		#region Listener

		protected event Action<string, NameValuePairList> eventListeners;

		[OgreVersion( 1, 7, 2790 )]
		public virtual event Action<string, NameValuePairList> Listener
		{
			add
			{
				eventListeners += value;
			}
			remove
			{
				eventListeners -= value;
			}
		}

		[AxiomHelper( 0, 8 )] private readonly
			Dictionary<Axiom.Math.Tuple<string, Action<NameValuePairList>>, Action<string, NameValuePairList>> _namedEvents =
				new Dictionary<Axiom.Math.Tuple<string, Action<NameValuePairList>>, Action<string, NameValuePairList>>();

		[AxiomHelper( 0, 8 )]
		public void AddEvent( string name, Action<NameValuePairList> handler )
		{
			var key = new Axiom.Math.Tuple<string, Action<NameValuePairList>>( name, handler );
			Action<string, NameValuePairList> wrapper = ( n, args ) =>
			                                            {
			                                            	if ( n == name )
			                                            	{
			                                            		handler( args );
			                                            	}
			                                            };
			Listener += wrapper;
			_namedEvents.Add( key, wrapper );
		}

		[AxiomHelper( 0, 8 )]
		public void RemoveEvent( string name, Action<NameValuePairList> handler )
		{
			var key = new Axiom.Math.Tuple<string, Action<NameValuePairList>>( name, handler );
			Listener -= _namedEvents[ key ];
			_namedEvents.Remove( key );
		}

		#endregion

		#region RenderSystemEvents

		[OgreVersion( 1, 7, 2790 )] protected List<string> eventNames = new List<string>();

		[OgreVersion( 1, 7, 2790 )]
		public virtual List<string> RenderSystemEvents
		{
			get
			{
				return eventNames;
			}
		}

		#endregion

		#region DisplayMonitorCount

		/// <summary>
		/// Gets the number of display monitors. <see name="Root.DisplayMonitorCount"/>
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract int DisplayMonitorCount { get; }

		#endregion

		#region WBufferEnabled

		[OgreVersion( 1, 7, 2790 )] protected bool wBuffer;

		[OgreVersion( 1, 7, 2790 )]
		public bool WBufferEnabled
		{
			get
			{
				return wBuffer;
			}
			set
			{
				wBuffer = value;
			}
		}

		#endregion

		#region AmbientLight

		/// <summary>
		/// Sets the color &amp; strength of the ambient (global directionless) light in the world.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Axiom interface uses ColorEx while Ogre uses a ternary (r,g,b) setter" )]
		public abstract ColorEx AmbientLight { get; set; }

		[OgreVersion( 1, 7, 2790 )]
		public abstract ShadeOptions ShadingType { get; set; }

		#endregion

		#region HorizontalTexelOffset

		/// <summary>
		/// Returns the horizontal texel offset value required for mapping 
		/// texel origins to pixel origins in this rendersystem.
		/// </summary>
		/// <remarks>
		/// Since rendersystems sometimes disagree on the origin of a texel, 
		/// mapping from texels to pixels can sometimes be problematic to 
		/// implement generically. This method allows you to retrieve the offset
		/// required to map the origin of a texel to the origin of a pixel in
		/// the horizontal direction.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public abstract Real HorizontalTexelOffset { get; }

		#endregion

		#region LightingEnabled

		/// <summary>
		/// Gets/Sets whether or not dynamic lighting is enabled.
		/// <p/>
		/// If true, dynamic lighting is performed on geometry with normals supplied, geometry without
		/// normals will not be displayed. If false, no lighting is applied and all geometry will be full brightness.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract bool LightingEnabled { get; set; }

		#endregion

		#region NormalizeNormals

		/// <summary>
		/// Get/Sets whether or not normals are to be automatically normalized.
		/// </summary>
		/// <remarks>
		/// This is useful when, for example, you are scaling SceneNodes such that
		/// normals may not be unit-length anymore. Note though that this has an
		/// overhead so should not be turn on unless you really need it.
		/// <p/>
		/// You should not normally call this direct unless you are rendering
		/// world geometry; set it on the Renderable because otherwise it will be
		/// overridden by material settings. 
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public abstract bool NormalizeNormals { get; set; }

		#endregion

		#region ProjectionMatrix

		/// <summary>
		/// Gets/Sets the current projection matrix.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract Matrix4 ProjectionMatrix { get; set; }

		#endregion

		#region PolygonMode

		/// <summary>
		/// Gets/Sets how to rasterise triangles, as points, wireframe or solid polys.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract PolygonMode PolygonMode { get; set; }

		#endregion

		#region StencilCheckEnabled

		/// <summary>
		/// Turns stencil buffer checking on or off. 
		/// </summary>
		/// <remarks>
		/// Stencilling (masking off areas of the rendering target based on the stencil 
		/// buffer) can be turned on or off using this method. By default, stencilling is
		/// disabled.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public abstract bool StencilCheckEnabled { get; set; }

		#endregion

		#region VerticalTexelOffset

		/// <summary>
		/// Returns the vertical texel offset value required for mapping 
		/// texel origins to pixel origins in this rendersystem.
		/// </summary>
		/// <remarks>
		/// Since rendersystems sometimes disagree on the origin of a texel, 
		/// mapping from texels to pixels can sometimes be problematic to 
		/// implement generically. This method allows you to retrieve the offset
		/// required to map the origin of a texel to the origin of a pixel in
		/// the vertical direction.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public abstract Real VerticalTexelOffset { get; }

		#endregion

		#region ViewMatrix

		/// <summary>
		/// Gets/Sets the current view matrix.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract Matrix4 ViewMatrix { get; set; }

		#endregion

		#region WorldMatrix

		/// <summary>
		/// Sets the current world matrix.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract Matrix4 WorldMatrix { get; set; }

		#endregion

		#region MinimumDepthInputValue

		/// <summary>
		/// Gets the maximum (closest) depth value to be used when rendering using identity transforms.
		/// </summary>
		/// <remarks>
		/// When using identity transforms you can manually set the depth
		/// of a vertex; however the input values required differ per
		/// rendersystem. This method lets you retrieve the correct value.
		/// <see cref="SimpleRenderable.UseIdentityView"/>
		/// <see cref="SimpleRenderable.UseIdentityProjection"/>
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public abstract Real MinimumDepthInputValue { get; }

		#endregion

		#region MaximumDepthInputValue

		/// <summary>
		/// Gets the maximum (farthest) depth value to be used when rendering using identity transforms.
		/// </summary>
		/// <remarks>
		/// When using identity transforms you can manually set the depth
		/// of a vertex; however the input values required differ per
		/// rendersystem. This method lets you retrieve the correct value.
		/// <see cref="SimpleRenderable.UseIdentityView"/>
		/// <see cref="SimpleRenderable.UseIdentityProjection"/>
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public abstract Real MaximumDepthInputValue { get; }

		#endregion

		#endregion Properties

		#region Methods

		#region FireEvent

		/// <summary>
		/// Internal method for firing a rendersystem event
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected virtual void FireEvent( string name )
		{
			if ( eventListeners != null )
			{
				eventListeners( name, null );
			}
		}

		/// <summary>
		/// Internal method for firing a rendersystem event
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected virtual void FireEvent( string name, NameValuePairList pars )
		{
			if ( eventListeners != null )
			{
				eventListeners( name, pars );
			}
		}

		#endregion

		#region PreExtraThreadsStarted

		/// <summary>
		/// Tell the rendersystem to perform any prep tasks it needs to directly
		/// before other threads which might access the rendering API are registered.
		/// </summary>
		/// <remarks>
		/// Call this from your main thread before starting your other threads
		/// (which themselves should call registerThread()). Note that if you
		/// start your own threads, there is a specific startup sequence which 
		/// must be respected and requires synchronisation between the threads:
		/// <ol>
		/// <li>[Main thread]Call <see cref="PreExtraThreadsStarted"/></li>
		/// <li>[Main thread]Start other thread, wait</li>
		/// <li>[Other thread]Call <see cref="RegisterThread"/>, notify main thread &amp; continue</li>
		/// <li>[Main thread]Wake up &amp; call <see cref="PostExtraThreadsStarted"/></li>
		/// </ol>
		/// Once this init sequence is completed the threads are independent but
		/// this startup sequence must be respected.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void PreExtraThreadsStarted();

		#endregion

		#region PostExtraThreadsStarted

		/// <summary>
		/// Tell the rendersystem to perform any tasks it needs to directly
		/// after other threads which might access the rendering API are registered.
		/// <see cref="PreExtraThreadsStarted"/>
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void PostExtraThreadsStarted();

		#endregion

		#region RegisterThread

		/// <summary>
		/// Register the an additional thread which may make calls to rendersystem-related objects.
		/// </summary>
		/// <remarks>
		/// This method should only be called by additional threads during their
		/// initialisation. If they intend to use hardware rendering system resources 
		/// they should call this method before doing anything related to the render system.
		/// Some rendering APIs require a per-thread setup and this method will sort that
		/// out. It is also necessary to call unregisterThread before the thread shuts down.
		/// </remarks>
		/// <note>
		/// This method takes no parameters - it must be called from the thread being
		/// registered and that context is enough.
		/// </note>        
		[OgreVersion( 1, 7, 2790 )]
		public abstract void RegisterThread();

		#endregion

		#region UnregisterThread

		/// <summary>
		/// Unregister an additional thread which may make calls to rendersystem-related objects.
		/// <see cref="RegisterThread"/>
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void UnregisterThread();

		#endregion

		#region UseCustomRenderSystemCapabilities

		/// <summary>
		/// Force the render system to use the special capabilities. Can only be called
		/// before the render system has been fully initializer (before createWindow is called) 
		/// </summary>
		/// <param name="capabilities">
		/// capabilities has to be a subset of the real capabilities and the caller is 
		/// responsible for deallocating capabilities.
		/// </param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void UseCustomRenderSystemCapabilities( RenderSystemCapabilities capabilities )
		{
			if ( realCapabilities != null )
			{
				throw new AxiomException( "Custom render capabilities must be set before the RenderSystem is initialized." );
			}

			currentCapabilities = capabilities;
			useCustomCapabilities = true;
		}

		#endregion

		//        #region SetDepthBufferFor

		//        /// <summary>
		//        /// Retrieves an existing DepthBuffer or creates a new one suited for the given RenderTarget and sets it.
		//        /// </summary>
		//        /// <remarks>
		//        /// RenderTarget's pool ID is respected. <see name="RenderTarget.DepthBufferPool"/>
		//        /// </remarks>
		//        /// <param name="renderTarget"></param>
		//        [OgreVersion( 1, 7, 2790 )]
		//        public virtual void SetDepthBufferFor( RenderTarget renderTarget )
		//        {
		//            var poolId = renderTarget.DepthBufferPool;
		//            //Find a depth buffer in the pool

		//            // Axiom: emulate std::map [] access
		//            if ( !depthBufferPool.ContainsKey( poolId ) )
		//                depthBufferPool[ poolId ] = new DepthBufferVec();

		//            var itor = depthBufferPool[ poolId ].GetEnumerator();

		//            var bAttached = false;
		//            while ( itor.MoveNext() && !bAttached )
		//                bAttached = renderTarget.AttachDepthBuffer( itor.Current );

		//            //Not found yet? Create a new one!
		//            if ( !bAttached )
		//            {
		//                var newDepthBuffer = CreateDepthBufferFor( renderTarget );

		//                if ( newDepthBuffer != null )
		//                {
		//                    newDepthBuffer.SetPoolId( poolId );
		//                    depthBufferPool[ poolId ].Add( newDepthBuffer );

		//                    bAttached = renderTarget.AttachDepthBuffer( newDepthBuffer );

		//                    Debug.Assert( bAttached,
		//                                  @"A new DepthBuffer for a RenderTarget was created, but after creation
		//it says it's incompatible with that RT" );
		//                }
		//                else
		//                    LogManager.Instance.Write( "WARNING: Couldn't create a suited DepthBuffer for RT: {0}",
		//                                               renderTarget.Name );
		//            }
		//        }

		//        #endregion

		#region SetDepthBias

		/// <summary>
		/// Sets the depth bias, NB you should use the Material version of this.
		/// </summary>
		/// <param name="constantBias"></param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetDepthBias( float constantBias )
		{
			SetDepthBias( constantBias, 0.0f );
		}

		/// <summary>
		/// Sets the depth bias, NB you should use the Material version of this.
		/// </summary>
		/// <param name="constantBias"></param>
		/// <param name="slopeScaleBias"></param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetDepthBias( float constantBias, float slopeScaleBias );

		#endregion

		#region SetDerivedDepthBias

		/// <summary>
		/// Tell the render system whether to derive a depth bias on its own based on 
		/// the values passed to it in setCurrentPassIterationCount.
		/// The depth bias set will be baseValue + iteration * multiplier
		/// </summary>
		/// <param name="derive">true to tell the RS to derive this automatically</param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetDerivedDepthBias( bool derive )
		{
			SetDerivedDepthBias( derive, 0.0f, 0.0f, 0.0f );
		}

		/// <summary>
		/// Tell the render system whether to derive a depth bias on its own based on 
		/// the values passed to it in setCurrentPassIterationCount.
		/// The depth bias set will be baseValue + iteration * multiplier
		/// </summary>
		/// <param name="derive">true to tell the RS to derive this automatically</param>
		/// <param name="baseValue">The base value to which the multiplier should be added</param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetDerivedDepthBias( bool derive, float baseValue )
		{
			SetDerivedDepthBias( derive, baseValue, 0.0f, 0.0f );
		}

		/// <summary>
		/// Tell the render system whether to derive a depth bias on its own based on 
		/// the values passed to it in setCurrentPassIterationCount.
		/// The depth bias set will be baseValue + iteration * multiplier
		/// </summary>
		/// <param name="derive">true to tell the RS to derive this automatically</param>
		/// <param name="baseValue">The base value to which the multiplier should be added</param>
		/// <param name="multiplier">The amount of depth bias to apply per iteration</param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetDerivedDepthBias( bool derive, float baseValue, float multiplier )
		{
			SetDerivedDepthBias( derive, baseValue, multiplier, 0.0f );
		}

		/// <summary>
		/// Tell the render system whether to derive a depth bias on its own based on 
		/// the values passed to it in setCurrentPassIterationCount.
		/// The depth bias set will be baseValue + iteration * multiplier
		/// </summary>
		/// <param name="derive">true to tell the RS to derive this automatically</param>
		/// <param name="baseValue">The base value to which the multiplier should be added</param>
		/// <param name="multiplier">The amount of depth bias to apply per iteration</param>
		/// <param name="slopeScale">The constant slope scale bias for completeness</param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void SetDerivedDepthBias( bool derive, float baseValue, float multiplier, float slopeScale )
		{
			derivedDepthBias = derive;
			derivedDepthBiasBase = baseValue;
			derivedDepthBiasMultiplier = multiplier;
			derivedDepthBiasSlopeScale = slopeScale;
		}

		#endregion

		#region ValidateConfigOptions

		/// <summary>
		/// Validates the configuration of the rendering system
		/// </summary>
		/// <remarks>Calling this method can cause the rendering system to modify the ConfigOptions collection.</remarks>
		/// <returns>Error message is configuration is invalid <see cref="String.Empty"/> if valid.</returns>
		[OgreVersion( 1, 7, 2790 )]
		public abstract string ValidateConfigOptions();

		#endregion

		#region AttachRenderTarget

		/// <summary>
		/// Attaches a render target to this render system.
		/// </summary>
		/// <param name="target">Reference to the render target to attach to this render system.</param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void AttachRenderTarget( RenderTarget target )
		{
			Debug.Assert( (int)target.Priority < NumRendertargetGroups );
			renderTargets.Add( target.Name, target );
			prioritizedRenderTargets.Add( target.Priority, target );
		}

		#endregion

		#region BeginGeometryCount

		/// <summary>
		/// The RenderSystem will keep a count of tris rendered, this resets the count.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void BeginGeometryCount()
		{
			batchCount = vertexCount = faceCount = 0;
		}

		#endregion

		#region DetachRenderTarget

		/// <summary>
		/// Detaches the render target from this render system.
		/// </summary>
		/// <param name="name">Name of the render target to detach.</param>
		/// <returns>the render target that was detached</returns>
		[OgreVersion( 1, 7, 2790 )]
		public virtual RenderTarget DetachRenderTarget( string name )
		{
			RenderTarget ret;
			if ( renderTargets.TryGetValue( name, out ret ) )
			{
				// Remove the render target from the priority groups.
				prioritizedRenderTargets.RemoveWhere( ( k, v ) => v == ret );
			}

			// If detached render target is the active render target, reset active render target
			if ( ret == activeRenderTarget )
			{
				activeRenderTarget = null;
			}

			return ret;
		}

		[AxiomHelper( 0, 8 )]
		public void DetachRenderTarget( RenderTarget target )
		{
			DetachRenderTarget( target.Name );
		}

		#endregion

		#region GetErrorDescription

		/// <summary>
		/// Returns a description of an error code.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract string GetErrorDescription( int errorNumber );

		#endregion

		#region DisableTextureUnit

		/// <summary>
		/// Turns off a texture unit if not needed.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void DisableTextureUnit( int texUnit )
		{
			SetTexture( texUnit, false, (Texture)null );
		}

		#endregion

		#region DisableTextureUnitsFrom

		/// <summary>
		/// Disables all texture units from the given unit upwards
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void DisableTextureUnitsFrom( int texUnit )
		{
			var disableTo = Config.MaxTextureLayers;
			if ( disableTo > disabledTexUnitsFrom )
			{
				disableTo = disabledTexUnitsFrom;
			}
			disabledTexUnitsFrom = texUnit;
			for ( var i = texUnit; i < disableTo; ++i )
			{
				DisableTextureUnit( i );
			}
		}

		#endregion

		#region InitRenderTargets

		/// <summary>
		/// Utility method for initializing all render targets attached to this rendering system.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void InitRenderTargets()
		{
			// init stats for each render target
			foreach ( var item in renderTargets )
			{
				item.Value.ResetStatistics();
			}
		}

		#endregion

		#region AddClipPlane

		/// <summary>
		/// Add a user clipping plane.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void AddClipPlane( Plane p )
		{
			clipPlanes.Add( p );
			clipPlanesDirty = true;
		}

		#endregion

		#region AddClipPlane

		/// <summary>
		/// Add a user clipping plane.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void AddClipPlane( Real a, Real b, Real c, Real d )
		{
			AddClipPlane( new Plane( new Vector3( a, b, c ), d ) );
		}

		#endregion

		#region ResetClipPlanes

		/// <summary>
		/// Clears the user clipping region.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void ResetClipPlanes()
		{
			if ( clipPlanes.Count != 0 )
			{
				clipPlanes.Clear();
				clipPlanesDirty = true;
			}
		}

		#endregion

		#region NotifyCameraRemoved

		/// <summary>
		/// Utility method to notify all render targets that a camera has been removed, 
		/// incase they were referring to it as their viewer. 
		/// </summary>
		/// <param name="camera">Camera being removed.</param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void NotifyCameraRemoved( Camera camera )
		{
			foreach ( var item in renderTargets )
			{
				item.Value.NotifyCameraRemoved( camera );
			}
		}

		#endregion

		#region SetClipPlanesImpl

		/// <summary>
		/// Internal method used to set the underlying clip planes when needed
		/// </summary>
		/// <param name="clipPlanes"></param>
		[OgreVersion( 1, 7, 2790 )]
		protected abstract void SetClipPlanesImpl( PlaneList clipPlanes );

		#endregion

		#region Render

		/// <summary>
		/// Render something to the active viewport.
		/// </summary>
		/// <remarks>
		/// Low-level rendering interface to perform rendering
		/// operations. Unlikely to be used directly by client
		/// applications, since the <see cref="SceneManager"/> and various support
		/// classes will be responsible for calling this method.
		/// Can only be called between BeginScene and EndScene
		/// </remarks>
		/// <param name="op">
		/// A rendering operation instance, which contains details of the operation to be performed.
		/// </param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void Render( RenderOperation op )
		{
			var val = op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount;

			val *= op.numberOfInstances;

			// account for a pass having multiple iterations
			if ( currentPassIterationCount > 1 )
			{
				val *= currentPassIterationCount;
			}

			currentPassIterationNum = 0;

			// calculate faces
			switch ( op.operationType )
			{
				case OperationType.TriangleList:
					faceCount += val/3;
					break;
				case OperationType.TriangleStrip:
				case OperationType.TriangleFan:
					faceCount += val - 2;
					break;
				case OperationType.PointList:
				case OperationType.LineList:
				case OperationType.LineStrip:
					break;
			}

			// increment running vertex count
			vertexCount += op.vertexData.vertexCount;
			batchCount += currentPassIterationCount;

			// sort out clip planes
			// have to do it here in case of matrix issues
			if ( clipPlanesDirty )
			{
				SetClipPlanesImpl( clipPlanes );
				clipPlanesDirty = false;
			}
		}

		#endregion

		#region UpdatePassIterationRenderState

		/// <summary>
		/// updates pass iteration rendering state including bound gpu program parameter pass iteration auto constant entry
		/// </summary>
		/// <returns>True if more iterations are required</returns>
		[OgreVersion( 1, 7, 2790 )]
		protected bool UpdatePassIterationRenderState()
		{
			if ( currentPassIterationCount <= 1 )
			{
				return false;
			}

			--currentPassIterationCount;
			++currentPassIterationNum;
			if ( activeVertexGpuProgramParameters != null )
			{
				activeVertexGpuProgramParameters.IncPassIterationNumber();
				BindGpuProgramPassIterationParameters( GpuProgramType.Vertex );
			}
			if ( activeGeometryGpuProgramParameters != null )
			{
				activeGeometryGpuProgramParameters.IncPassIterationNumber();
				BindGpuProgramPassIterationParameters( GpuProgramType.Geometry );
			}
			if ( activeFragmentGpuProgramParameters != null )
			{
				activeFragmentGpuProgramParameters.IncPassIterationNumber();
				BindGpuProgramPassIterationParameters( GpuProgramType.Fragment );
			}
			return true;
		}

		#endregion

		#region SetTextureUnitSettings

		/// <summary>
		/// Utility function for setting all the properties of a texture unit at once.
		/// This method is also worth using over the individual texture unit settings because it
		/// only sets those settings which are different from the current settings for this
		/// unit, thus minimising render state changes.
		/// </summary>
		/// <param name="texUnit"></param>
		/// <param name="tl"></param>
		[OgreVersion( 1, 7, 2790, "resolving texture from resourcemanager atm" )]
		public virtual void SetTextureUnitSettings( int texUnit, TextureUnitState tl )
		{
			// TODO: implement TextureUnitState.TexturePtr
			// var tex = tl.TexturePtr
			var tex = (Texture)TextureManager.Instance.GetByName( tl.TextureName );

			// Vertex Texture Binding?
			if ( Capabilities.HasCapability( Graphics.Capabilities.VertexTextureFetch ) && !Capabilities.VertexTextureUnitsShared )
			{
				if ( tl.BindingType == TextureBindingType.Vertex )
				{
					// Bind Vertex Texture
					SetVertexTexture( texUnit, tex );
					// bind nothing to fragment unit (hardware isn't shared but fragment
					// unit can't be using the same index
					SetTexture( texUnit, true, (Texture)null );
				}
				else
				{
					// vice versa
					SetVertexTexture( texUnit, null );
					SetTexture( texUnit, true, tex );
				}
			}
			else
			{
				// Shared vertex / fragment textures or no vertex texture support
				// Bind texture (may be blank)
				SetTexture( texUnit, true, tex );
			}

			// Set texture coordinate set
			SetTextureCoordSet( texUnit, tl.TextureCoordSet );

			// Texture layer filtering
			SetTextureUnitFiltering( texUnit, tl.GetTextureFiltering( FilterType.Min ), tl.GetTextureFiltering( FilterType.Mag ),
			                         tl.GetTextureFiltering( FilterType.Mip ) );

			// Texture layer anistropy
			SetTextureLayerAnisotropy( texUnit, tl.TextureAnisotropy );

			// Set mipmap biasing
			SetTextureMipmapBias( texUnit, tl.TextureMipmapBias );

			// set the texture blending modes
			// NOTE: Color before Alpha is important
			SetTextureBlendMode( texUnit, tl.ColorBlendMode );
			SetTextureBlendMode( texUnit, tl.AlphaBlendMode );

			// Texture addressing mode
			var uvw = tl.TextureAddressingMode;
			SetTextureAddressingMode( texUnit, uvw );

			// Set the texture border color only if needed.
			if ( uvw.U == TextureAddressing.Border || uvw.V == TextureAddressing.Border || uvw.W == TextureAddressing.Border )
			{
				SetTextureBorderColor( texUnit, tl.TextureBorderColor );
			}

			// Set texture Effects
			var anyCalcs = false;
			// TODO: Change TextureUnitState Effects to use Enumerator
			for ( var i = 0; i < tl.NumEffects; i++ )
			{
				var effect = tl.GetEffect( i );

				switch ( effect.type )
				{
					case TextureEffectType.EnvironmentMap:
						switch ( (EnvironmentMap)effect.subtype )
						{
							case EnvironmentMap.Curved:
								SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.EnvironmentMap );
								break;
							case EnvironmentMap.Planar:
								SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.EnvironmentMapPlanar );
								break;
							case EnvironmentMap.Reflection:
								SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.EnvironmentMapReflection );
								break;
							case EnvironmentMap.Normal:
								SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.EnvironmentMapNormal );
								break;
						}
						anyCalcs = true;
						break;

					case TextureEffectType.UVScroll:
					case TextureEffectType.UScroll:
					case TextureEffectType.VScroll:
					case TextureEffectType.Rotate:
					case TextureEffectType.Transform:
						break;

					case TextureEffectType.ProjectiveTexture:
						SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.ProjectiveTexture, effect.frustum );
						anyCalcs = true;
						break;
				} // switch
			} // for

			// Ensure any previous texcoord calc settings are reset if there are now none
			if ( !anyCalcs )
			{
				SetTextureCoordCalculation( texUnit, TexCoordCalcMethod.None );
			}

			// set the texture matrix to that of the current layer for any transformations
			SetTextureMatrix( texUnit, tl.TextureMatrix );
		}

		#endregion

		#region SetWorldMatrices

		/// <summary>
		/// Sets multiple world matrices (vertex blending).
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void SetWorldMatrices( Matrix4[] matrices, ushort count )
		{
			// Do nothing with these matrices here, it never used for now,
			// derived class should take care with them if required.

			// reset the hardware world matrix to identity
			WorldMatrix = Matrix4.Identity;
		}

		#endregion

		#region Shutdown

		/// <summary>
		/// Shuts down the RenderSystem.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void Shutdown()
		{
			// Remove occlusion queries
			foreach ( var i in hwOcclusionQueries )
			{
				i.SafeDispose();
			}
			hwOcclusionQueries.Clear();

			CleanupDepthBuffers();

			// Remove all the render targets.
			// (destroy primary target last since others may depend on it)
			RenderTarget primary = null;
			foreach ( var it in renderTargets )
			{
				if ( primary == null && it.Value.IsPrimary )
				{
					primary = it.Value;
				}
				else
				{
					it.Value.SafeDispose();
				}
			}

			primary.SafeDispose();
			renderTargets.Clear();
			prioritizedRenderTargets.Clear();
		}

		#endregion

		#region UpdateAllRenderTargets

		/// <summary>
		/// Internal method for updating all render targets attached to this rendering system.
		/// </summary>
		/// <param name="swapBuffers"></param>
		[OgreVersion( 1, 7, 2790 )]
		public void UpdateAllRenderTargets()
		{
			UpdateAllRenderTargets( true );
		}

		/// <summary>
		/// Internal method for updating all render targets attached to this rendering system.
		/// </summary>
		/// <param name="swapBuffers"></param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void UpdateAllRenderTargets( bool swapBuffers )
		{
			// Update all in order of priority
			// This ensures render-to-texture targets get updated before render windows
			foreach ( var targets in prioritizedRenderTargets )
			{
				foreach ( var target in targets.Value )
				{
					// only update if it is active
					if ( target.IsActive && target.IsAutoUpdated )
					{
						target.Update( swapBuffers );
					}
				}
			}
		}

		#endregion

		#region SwapAllRenderTargetBuffers

		/// <summary>
		/// Internal method for swapping all the buffers on all render targets,
		/// if <see cref="UpdateAllRenderTargets( bool )"/> was called with a 'false' parameter.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public void SwapAllRenderTargetBuffers()
		{
			SwapAllRenderTargetBuffers( true );
		}

		/// <summary>
		/// Internal method for swapping all the buffers on all render targets,
		/// if <see cref="UpdateAllRenderTargets( bool )"/> was called with a 'false' parameter.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void SwapAllRenderTargetBuffers( bool waitForVSync )
		{
			// Update all in order of priority
			// This ensures render-to-texture targets get updated before render windows
			foreach ( var targets in prioritizedRenderTargets )
			{
				foreach ( var target in targets.Value )
				{
					// only update if it is active
					if ( target.IsActive && target.IsAutoUpdated )
					{
						target.SwapBuffers( waitForVSync );
					}
				}
			}
		}

		#endregion

		#region GetRenderTarget

		/// <summary>
		/// Returns a pointer to the render target with the passed name, or null if that
		/// render target cannot be found.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[OgreVersion( 1, 7, 2790 )]
		public virtual RenderTarget GetRenderTarget( string name )
		{
			RenderTarget ret;
			renderTargets.TryGetValue( name, out ret );
			return ret;
		}

		#endregion

		#region BeginFrame

		/// <summary>
		/// Signifies the beginning of a frame, ie the start of rendering on a single viewport. Will occur
		/// several times per complete frame if multiple viewports exist.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void BeginFrame();

		#endregion

		#region PauseFrame

		/// <summary>
		/// Pause rendering for a frame. This has to be called after 
		/// <see cref="BeginFrame"/> and before <see cref="EndFrame"/>.
		/// will usually be called by the SceneManager, don't use this manually unless you know what
		/// you are doing.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual RenderSystemContext PauseFrame()
		{
			EndFrame();
			return new RenderSystemContext();
		}

		#endregion

		#region ResumeFrame

		/// <summary>
		/// Resume rendering for a frame. This has to be called after a <see cref="PauseFrame"/> call
		/// Will usually be called by the SceneManager, don't use this manually unless you know what
		/// you are doing.
		/// </summary>
		/// <param name="context">the render system context, as returned by <see cref="PauseFrame"/></param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void ResumeFrame( RenderSystemContext context )
		{
			BeginFrame();
			context.Dispose();
		}

		#endregion

		#region BindGpuProgram

		/// <summary>
		/// Binds a given GpuProgram (but not the parameters). 
		/// </summary>
		/// <remarks>
		/// Only one GpuProgram of each type can be bound at once, binding another
		/// one will simply replace the existing one.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void BindGpuProgram( GpuProgram program )
		{
			switch ( program.Type )
			{
				case GpuProgramType.Vertex:
					// mark clip planes dirty if changed (programmable can change space)
					if ( !vertexProgramBound && clipPlanes.Count != 0 )
					{
						clipPlanesDirty = true;
					}

					vertexProgramBound = true;
					break;
				case GpuProgramType.Geometry:
					geometryProgramBound = true;
					break;
				case GpuProgramType.Fragment:
					fragmentProgramBound = true;
					break;
			}
		}

		#endregion

		#region BindGpuProgramParameters

		/// <summary>
		/// Bind Gpu program parameters.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms,
		                                               GpuProgramParameters.GpuParamVariability mask );

		#endregion

		#region BindGpuProgramPassIterationParameters

		/// <summary>
		/// Only binds Gpu program parameters used for passes that have more than one iteration rendering
		/// </summary>
		/// <param name="gptype"></param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void BindGpuProgramPassIterationParameters( GpuProgramType gptype );

		#endregion

		#region ClearFrameBuffer

		/// <summary>
		/// Clears one or more frame buffers on the active render target.
		/// </summary>
		/// <param name="buffers">
		///  Combination of one or more elements of <see cref="Graphics.RenderTarget.FrameBuffer"/>
		///  denoting which buffers are to be cleared.
		/// </param>
		/// <param name="color">The color to clear the color buffer with, if enabled.</param>
		/// <param name="depth">The value to initialize the depth buffer with, if enabled.</param>
		/// <param name="stencil">The value to initialize the stencil buffer with, if enabled.</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, Real depth, ushort stencil );

		public void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, Real depth )
		{
			ClearFrameBuffer( buffers, color, depth, 0 );
		}

		public void ClearFrameBuffer( FrameBufferType buffers, ColorEx color )
		{
			ClearFrameBuffer( buffers, color, Real.One, 0 );
		}

		public void ClearFrameBuffer( FrameBufferType buffers )
		{
			ClearFrameBuffer( buffers, ColorEx.Black, Real.One, 0 );
		}

		#endregion

		#region ConvertColor

		/// <summary>
		/// Converts the Axiom.Core.ColorEx value to a int.  Each API may need the 
		/// bytes of the packed color data in different orders. i.e. OpenGL - ABGR, D3D - ARGB
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		[OgreVersion( 1, 7, 2790, "Axiom uses slightly different interface" )]
		public virtual int ConvertColor( ColorEx color )
		{
			return VertexElement.ConvertColorValue( color, ColorVertexElementType );
		}

		#endregion

		#region CreateRenderWindow

		/// <summary>
		/// Creates a new render window.
		/// </summary>
		/// <remarks>
		/// This method creates a new rendering window as specified
		/// by the paramteters. The rendering system could be
		/// responible for only a single window (e.g. in the case
		/// of a game), or could be in charge of multiple ones (in the
		/// case of a level editor). The option to create the window
		/// as a child of another is therefore given.
		/// This method will create an appropriate subclass of
		/// RenderWindow depending on the API and platform implementation.
		/// </remarks>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="isFullScreen"></param>
		/// <param name="miscParams">
		/// A collection of addition rendersystem specific options.
		/// </param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen,
		                                                 NamedParameterList miscParams );

		#endregion CreateRenderWindow

		#region CreateRenderWindows

		/// <summary>
		/// Creates multiple rendering windows.
		/// </summary>
		/// <param name="renderWindowDescriptions">
		/// Array of structures containing the descriptions of each render window.
		/// The structure's members are the same as the parameters of CreateRenderWindow:
		/// <see cref="CreateRenderWindow"/>
		/// </param>
		/// <param name="createdWindows">This array will hold the created render windows.</param>
		/// <returns>true on success.</returns>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool CreateRenderWindows( RenderWindowDescriptionList renderWindowDescriptions,
		                                         RenderWindowList createdWindows )
		{
			var fullscreenWindowsCount = 0;

			for ( var nWindow = 0; nWindow < renderWindowDescriptions.Count; ++nWindow )
			{
				var curDesc = renderWindowDescriptions[ nWindow ];
				if ( curDesc.UseFullScreen )
				{
					fullscreenWindowsCount++;
				}

				var renderWindowFound = false;

				if ( renderTargets.ContainsKey( curDesc.Name ) )
				{
					renderWindowFound = true;
				}
				else
				{
					for ( var nSecWindow = nWindow + 1; nSecWindow < renderWindowDescriptions.Count; ++nSecWindow )
					{
						if ( curDesc.Name != renderWindowDescriptions[ nSecWindow ].Name )
						{
							continue;
						}
						renderWindowFound = true;
						break;
					}
				}

				// Make sure we don't already have a render target of the 
				// same name as the one supplied
				if ( renderWindowFound )
				{
					throw new AxiomException(
						"A render target of the same name '{0}' already exists.  You cannot create a new window with this name.",
						curDesc.Name );
				}
			}

			// Case we have to create some full screen rendering windows.
			if ( fullscreenWindowsCount > 0 )
			{
				// Can not mix full screen and windowed rendering windows.
				if ( fullscreenWindowsCount != renderWindowDescriptions.Count )
				{
					throw new AxiomException( "Can not create mix of full screen and windowed rendering windows" );
				}
			}

			return true;
		}

		#endregion CreateRenderWindows

		#region CreateMultiRenderTarget

		/// <summary>
		/// Create a MultiRenderTarget, which is a render target that renders to multiple RenderTextures at once.
		/// </summary>
		/// <Remarks>
		/// Surfaces can be bound and unbound at will. This fails if Capabilities.MultiRenderTargetsCount is smaller than 2.
		/// </Remarks>
		/// <returns></returns>
		[OgreVersion( 1, 7, 2790 )]
		public abstract MultiRenderTarget CreateMultiRenderTarget( string name );

		#endregion

		#region CreateHardwareOcclusionQuery

		/// <summary>
		/// Requests an API implementation of a hardware occlusion query used to test for the number
		/// of fragments rendered between calls to <see cref="HardwareOcclusionQuery.Begin"/> and 
		/// <see cref="HardwareOcclusionQuery.End"/> that pass the depth buffer test.
		/// </summary>
		/// <returns>An API specific implementation of an occlusion query.</returns>
		[OgreVersion( 1, 7, 2790 )]
		public abstract HardwareOcclusionQuery CreateHardwareOcclusionQuery();

		#endregion

		#region DestroyHardwareOcclusionQuery

		/// <summary>
		/// Destroy a hardware occlusion query object. 
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void DestroyHardwareOcclusionQuery( HardwareOcclusionQuery hq )
		{
			if ( hwOcclusionQueries.Remove( hq ) )
			{
				hq.Dispose();
			}
		}

		#endregion

		#region EndFrame

		/// <summary>
		/// Ends rendering of a frame to the current viewport.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void EndFrame();

		#endregion

		#region Initialize

		/// <summary>
		/// Initialize the rendering engine.
		/// </summary>
		/// <param name="autoCreateWindow">If true, a default window is created to serve as a rendering target.</param>
		/// <returns>A RenderWindow implementation specific to this RenderSystem.</returns>
		/// <remarks>All subclasses should call this method from within thier own intialize methods.</remarks>
		[OgreVersion( 1, 7, 2790 )]
		public RenderWindow Initialize( bool autoCreateWindow )
		{
			return Initialize( autoCreateWindow, DefaultWindowTitle );
		}

		/// <summary>
		/// Initialize the rendering engine.
		/// </summary>
		/// <param name="autoCreateWindow">If true, a default window is created to serve as a rendering target.</param>
		/// <param name="windowTitle">Text to display on the window caption if not fullscreen.</param>
		/// <returns>A RenderWindow implementation specific to this RenderSystem.</returns>
		/// <remarks>All subclasses should call this method from within thier own intialize methods.</remarks>
		[OgreVersion( 1, 7, 2790 )]
		public virtual RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			vertexProgramBound = false;
			geometryProgramBound = false;
			fragmentProgramBound = false;
			return null;
		}

		#endregion

		#region Reinitialize

		/// <summary>
		/// Reinitializes the Render System
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void Reinitialize();

		#endregion

		#region CreateRenderSystemCapabilities

		/// <summary>
		/// Query the real capabilities of the GPU and driver in the RenderSystem
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract RenderSystemCapabilities CreateRenderSystemCapabilities();

		#endregion

		#region MakeOrthoMatrix

		/// <summary>
		/// Builds an orthographic projection matrix suitable for this render system.
		/// </summary>
		/// <remarks>
		/// Because different APIs have different requirements (some incompatible) for the
		/// projection matrix, this method allows each to implement their own correctly and pass
		/// back a generic Matrix4 for storage in the engine.
		/// </remarks>
		/// <param name="fov">Field of view angle.</param>
		/// <param name="aspectRatio">Aspect ratio.</param>
		/// <param name="near">Near clipping plane distance.</param>
		/// <param name="far">Far clipping plane distance.</param>
		/// <param name="dest"></param>
		/// <param name="forGpuPrograms"></param>
		[OgreVersion( 1, 7, 2790 )]
#if NET_40
        public abstract void MakeOrthoMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuPrograms = false );
#else
		public abstract void MakeOrthoMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest,
		                                      bool forGpuPrograms );
#endif

#if !NET_40
		/// <see cref="Axiom.Graphics.RenderSystem.MakeOrthoMatrix(Radian, Real, Real, Real, out Matrix4, bool)"/>
		public void MakeOrthoMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest )
		{
			MakeOrthoMatrix( fov, aspectRatio, near, far, out dest, false );
		}
#endif

		#endregion MakeOrthoMatrix

		#region ApplyObliqueDepthProjection

		/// <summary>
		/// Update a perspective projection matrix to use 'oblique depth projection'.
		/// </summary>
		/// <remarks>
		/// This method can be used to change the nature of a perspective 
		/// transform in order to make the near plane not perpendicular to the 
		/// camera view direction, but to be at some different orientation. 
		/// This can be useful for performing arbitrary clipping (e.g. to a 
		/// reflection plane) which could otherwise only be done using user
		/// clip planes, which are more expensive, and not necessarily supported
		/// on all cards.
		/// </remarks>
		/// <param name="matrix">The existing projection matrix. Note that this must be a
		/// perspective transform (not orthographic), and must not have already
		/// been altered by this method. The matrix will be altered in-place.
		/// </param>
		/// <param name="plane">
		/// The plane which is to be used as the clipping plane. This
		/// plane must be in CAMERA (view) space.
		/// </param>
		/// <param name="forGpuProgram">Is this for use with a Gpu program or fixed-function</param>
		[OgreVersion( 1, 7, 2 )]
		public abstract void ApplyObliqueDepthProjection( ref Matrix4 matrix, Plane plane, bool forGpuProgram );

		#endregion ApplyObliqueDepthProjection

		#region ConvertProjectionMatrix

		/// <summary>
		/// Converts a uniform projection matrix to one suitable for this render system.
		/// </summary>
		/// <remarks>
		/// Because different APIs have different requirements (some incompatible) for the
		/// projection matrix, this method allows each to implement their own correctly and pass
		/// back a generic Matrix4 for storage in the engine.
		/// </remarks>
		/// <param name="matrix"></param>
		/// <param name="dest"></param>
		/// <param name="forGpuProgram"></param>
		[OgreVersion( 1, 7, 2790 )]
#if NET_40
		public abstract void ConvertProjectionMatrix( Matrix4 matrix, out Matrix4 dest, bool forGpuProgram = false );
#else
		public abstract void ConvertProjectionMatrix( Matrix4 matrix, out Matrix4 dest, bool forGpuProgram );
#endif

#if !NET_40
		/// <see cref="Axiom.Graphics.RenderSystem.ConvertProjectionMatrix(Matrix4, out Matrix4, bool)"/>
		public void ConvertProjectionMatrix( Matrix4 matrix, out Matrix4 dest )
		{
			ConvertProjectionMatrix( matrix, out dest, false );
		}
#endif

		#endregion ConvertProjectionMatrix

		#region MakeProjectionMatrix

		/// <summary>
		/// Builds a perspective projection matrix suitable for this render system.
		/// </summary>
		/// <remarks>
		/// Because different APIs have different requirements (some incompatible) for the
		/// projection matrix, this method allows each to implement their own correctly and pass
		/// back a generic Matrix4 for storage in the engine.
		/// </remarks>
		/// <param name="fov">Field of view angle.</param>
		/// <param name="aspectRatio">Aspect ratio.</param>
		/// <param name="near">Near clipping plane distance.</param>
		/// <param name="far">Far clipping plane distance.</param>
		/// <param name="dest"></param>
		/// <param name="forGpuProgram"></param>
		[OgreVersion( 1, 7, 2790 )]
#if NET_40
        public abstract void MakeProjectionMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuProgram = false );
#else
		public abstract void MakeProjectionMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest,
		                                           bool forGpuProgram );
#endif

#if !NET_40
		/// <see cref="Axiom.Graphics.RenderSystem.MakeProjectionMatrix(Radian, Real, Real, Real, out Matrix4, bool)"/>
		public void MakeProjectionMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest )
		{
			MakeProjectionMatrix( fov, aspectRatio, near, far, out dest, false );
		}
#endif

		/// <summary>
		/// Builds a perspective projection matrix for the case when frustum is
		/// not centered around camera.
		/// </summary>
		/// <remarks>
		/// Viewport coordinates are in camera coordinate frame, i.e. camera is 
		/// at the origin.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void MakeProjectionMatrix( Real left, Real right, Real bottom, Real top, Real nearPlane, Real farPlane,
		                                           out Matrix4 dest,
#if NET_40
            bool forGpuProgram = false );
#else
		                                           bool forGpuProgram );
#endif

#if !NET_40
		/// <see cref="Axiom.Graphics.RenderSystem.MakeProjectionMatrix(Real, Real, Real, Real, Real, Real, out Matrix4, bool)"/>
		public void MakeProjectionMatrix( Real left, Real right, Real bottom, Real top, Real nearPlane, Real farPlane,
		                                  out Matrix4 dest )
		{
			MakeProjectionMatrix( left, right, bottom, top, nearPlane, farPlane, out dest, false );
		}
#endif

		#endregion MakeProjectionMatrix

		#region SetAlphaRejectSettings

		/// <summary>
		///  Sets the global alpha rejection approach for future renders.
		/// </summary>
		/// <param name="func">The comparison function which must pass for a pixel to be written.</param>
		/// <param name="value">The value to compare each pixels alpha value to (0-255)</param>
		/// <param name="alphaToCoverage">Whether to enable alpha to coverage, if supported</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetAlphaRejectSettings( CompareFunction func, byte value, bool alphaToCoverage );

		#endregion

		#region SetTextureProjectionRelativeTo

		[OgreVersion( 1, 7, 2790 )]
		public virtual void SetTextureProjectionRelativeTo( bool enabled, Vector3 pos )
		{
			texProjRelative = true;
			texProjRelativeOrigin = pos;
		}

		#endregion

		//#region CreateDepthBufferFor

		///// <summary>
		///// Creates a DepthBuffer that can be attached to the specified RenderTarget
		///// </summary>
		///// <remarks>
		///// It doesn't attach anything, it just returns a pointer to a new DepthBuffer
		///// Caller is responsible for putting this buffer into the right pool, for
		///// attaching, and deleting it. Here's where API-specific magic happens.
		///// Don't call this directly unless you know what you're doing.
		///// </remarks>
		//[OgreVersion( 1, 7, 2790 )]
		//public abstract DepthBuffer CreateDepthBufferFor( RenderTarget renderTarget );

		//#endregion

		#region CleanupDepthBuffers

		/// <summary>
		/// Removes all depth buffers. Should be called on device lost and shutdown
		/// </summary>
		/// <remarks>
		/// Advanced users can call this directly with bCleanManualBuffers=false to
		/// remove all depth buffers created for RTTs; when they think the pool has
		/// grown too big or they've used lots of depth buffers they don't need anymore,
		/// freeing GPU RAM.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public void CleanupDepthBuffers()
		{
			CleanupDepthBuffers( true );
		}

		/// <summary>
		/// Removes all depth buffers. Should be called on device lost and shutdown
		/// </summary>
		/// <remarks>
		/// Advanced users can call this directly with bCleanManualBuffers=false to
		/// remove all depth buffers created for RTTs; when they think the pool has
		/// grown too big or they've used lots of depth buffers they don't need anymore,
		/// freeing GPU RAM.
		/// </remarks>
		/// <param name="cleanManualBuffers"></param>
		[OgreVersion( 1, 7, 2790 )]
		public void CleanupDepthBuffers( bool cleanManualBuffers )
		{
			foreach ( var itmap in depthBufferPool )
			{
				var itor = itmap.Value.GetEnumerator();
				while ( itor.MoveNext() )
				{
					if ( cleanManualBuffers || !itor.Current.IsManual )
					{
						itor.Dispose();
					}
				}

				itmap.Value.Clear();
			}

			depthBufferPool.Clear();
		}

		#endregion

		#region SetConfigOption

		/// <summary>
		/// Used to confirm the settings (normally chosen by the user) in
		/// order to make the renderer able to inialize with the settings as required.
		/// This make be video mode, D3D driver, full screen / windowed etc.
		/// Called automatically by the default configuration
		/// dialog, and by the restoration of saved settings.
		/// These settings are stored and only activeated when 
		/// RenderSystem::Initalize or RenderSystem::Reinitialize are called
		/// </summary>
		/// <param name="name">the name of the option to alter</param>
		/// <param name="value">the value to set the option to</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetConfigOption( string name, string value );

		#endregion

		#region SetColorBufferWriteEnabled

		/// <summary>
		/// Sets whether or not color buffer writing is enabled, and for which channels. 
		/// </summary>
		/// <remarks>
		/// For some advanced effects, you may wish to turn off the writing of certain color
		/// channels, or even all of the color channels so that only the depth buffer is updated
		/// in a rendering pass. However, the chances are that you really want to use this option
		/// through the Material class.
		/// </remarks>
		/// <param name="red">Writing enabled for red channel.</param>
		/// <param name="green">Writing enabled for green channel.</param>
		/// <param name="blue">Writing enabled for blue channel.</param>
		/// <param name="alpha">Writing enabled for alpha channel.</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha );

		#endregion

		#region SetDepthBufferParams

		/// <summary>
		/// Sets the mode of operation for depth buffer tests from this point onwards.
		/// </summary>
		/// <remarks>
		/// Sometimes you may wish to alter the behavior of the depth buffer to achieve
		/// special effects. Because it's unlikely that you'll set these options for an entire frame,
		/// but rather use them to tweak settings between rendering objects, this is intended for internal
		/// uses, which will be used by a <see cref="SceneManager"/> implementation rather than directly from 
		/// the client application.
		/// </remarks>
		/// <param name="depthTest">
		/// If true, the depth buffer is tested for each pixel and the frame buffer is only updated
		/// if the depth function test succeeds. If false, no test is performed and pixels are always written.
		/// </param>
		/// <param name="depthWrite">
		/// If true, the depth buffer is updated with the depth of the new pixel if the depth test succeeds.
		/// If false, the depth buffer is left unchanged even if a new pixel is written.
		/// </param>
		/// <param name="depthFunction">Sets the function required for the depth test.</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction );

		public void SetDepthBufferParams()
		{
			SetDepthBufferParams( true, true, CompareFunction.LessEqual );
		}

		public void SetDepthBufferParams( bool depthTest, bool depthWrite )
		{
			SetDepthBufferParams( depthTest, depthWrite, CompareFunction.LessEqual );
		}

		#endregion

		#region SetFog

		/// <summary>
		/// Sets the fog with the given params.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetFog( FogMode mode, ColorEx color, Real density, Real linearStart, Real linearEnd );

		[AxiomHelper( 0, 8, CommentDefOverride )]
		public void SetFog()
		{
			SetFog( FogMode.None, ColorEx.White, Real.One, Real.Zero, Real.One );
		}

		[AxiomHelper( 0, 8, CommentDefOverride )]
		public void SetFog( FogMode mode )
		{
			SetFog( mode, ColorEx.White, Real.One, Real.Zero, Real.One );
		}

		[AxiomHelper( 0, 8, CommentDefOverride )]
		public void SetFog( FogMode mode, ColorEx color )
		{
			SetFog( mode, color, Real.One, Real.Zero, Real.One );
		}

		[AxiomHelper( 0, 8, CommentDefOverride )]
		public void SetFog( FogMode mode, ColorEx color, Real density )
		{
			SetFog( mode, color, density, Real.Zero, Real.One );
		}

		[AxiomHelper( 0, 8, CommentDefOverride )]
		public void SetFog( FogMode mode, ColorEx color, Real density, Real linearStart )
		{
			SetFog( mode, color, density, linearStart, Real.One );
		}

		#endregion

		#region SetSceneBlending

		/// <summary>
		/// Sets the global blending factors for combining subsequent renders with the existing frame contents.
		/// The result of the blending operation is:
		/// <p align="center">final = (texture * src) + (pixel * dest)</p>
		/// Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
		/// enumerated type.
		/// </summary>
		/// <param name="src">The source factor in the above calculation, i.e. multiplied by the texture color components.</param>
		/// <param name="dest">The destination factor in the above calculation, i.e. multiplied by the pixel color components.</param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
		{
			SetSceneBlending( src, dest, SceneBlendOperation.Add );
		}

		/// <summary>
		/// Sets the global blending factors for combining subsequent renders with the existing frame contents.
		/// The result of the blending operation is:
		/// <p align="center">final = (texture * src) + (pixel * dest)</p>
		/// Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
		/// enumerated type.
		/// </summary>
		/// <param name="src">The source factor in the above calculation, i.e. multiplied by the texture color components.</param>
		/// <param name="dest">The destination factor in the above calculation, i.e. multiplied by the pixel color components.</param>
		/// <param name="op">The blend operation mode for combining pixels</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest, SceneBlendOperation op );

		#endregion

		#region SetSeparateSceneBlending

		/// <summary>
		/// Sets the global blending factors for combining subsequent renders with the existing frame contents.
		/// The result of the blending operation is:
		/// final = (texture * sourceFactor) + (pixel * destFactor).
		/// Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
		/// enumerated type.
		/// </summary>
		/// <param name="sourceFactor">The source factor in the above calculation, i.e. multiplied by the texture color components.</param>
		/// <param name="destFactor">The destination factor in the above calculation, i.e. multiplied by the pixel color components.</param>
		/// <param name="sourceFactorAlpha">The source factor in the above calculation for the alpha channel, i.e. multiplied by the texture alpha components.</param>
		/// <param name="destFactorAlpha">The destination factor in the above calculation for the alpha channel, i.e. multiplied by the pixel alpha components.</param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor,
		                                      SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha )
		{
			SetSeparateSceneBlending( sourceFactor, destFactor, sourceFactorAlpha, destFactorAlpha, SceneBlendOperation.Add,
			                          SceneBlendOperation.Add );
		}

		/// <summary>
		/// Sets the global blending factors for combining subsequent renders with the existing frame contents.
		/// The result of the blending operation is:
		/// final = (texture * sourceFactor) + (pixel * destFactor).
		/// Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
		/// enumerated type.
		/// </summary>
		/// <param name="sourceFactor">The source factor in the above calculation, i.e. multiplied by the texture color components.</param>
		/// <param name="destFactor">The destination factor in the above calculation, i.e. multiplied by the pixel color components.</param>
		/// <param name="sourceFactorAlpha">The source factor in the above calculation for the alpha channel, i.e. multiplied by the texture alpha components.</param>
		/// <param name="destFactorAlpha">The destination factor in the above calculation for the alpha channel, i.e. multiplied by the pixel alpha components.</param>
		/// <param name="op">The blend operation mode for combining pixels</param>
		/// <param name="alphaOp">The blend operation mode for combining pixel alpha values</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor,
		                                               SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha,
		                                               SceneBlendOperation op, SceneBlendOperation alphaOp );

		#endregion

		#region SetScissorTest

		/// <summary>
		/// Sets the 'scissor region' ie the region of the target in which rendering can take place.
		/// </summary>
		/// <remarks>
		/// This method allows you to 'mask off' rendering in all but a given rectangular area
		/// as identified by the parameters to this method.
		/// <p/>
		/// Not all systems support this method. Check the <see cref="Capabilities"/> enum for the
		/// ScissorTest capability to see if it is supported.
		/// </remarks>
		/// <param name="enable">True to enable the scissor test, false to disable it.</param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetScissorTest( bool enable )
		{
			SetScissorTest( enable, 0, 0, 800, 600 );
		}

		/// <summary>
		/// Sets the 'scissor region' ie the region of the target in which rendering can take place.
		/// </summary>
		/// <remarks>
		/// This method allows you to 'mask off' rendering in all but a given rectangular area
		/// as identified by the parameters to this method.
		/// <p/>
		/// Not all systems support this method. Check the <see cref="Capabilities"/> enum for the
		/// ScissorTest capability to see if it is supported.
		/// </remarks>
		/// <param name="enable">True to enable the scissor test, false to disable it.</param>
		/// <param name="left">Left corner (in pixels).</param>
		/// <param name="top">Top corner (in pixels).</param>
		/// <param name="right">Right corner (in pixels).</param>
		/// <param name="bottom">Bottom corner (in pixels).</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetScissorTest( bool enable, int left, int top, int right, int bottom );

		#endregion

		#region SetStencilBufferParams

		/// <summary>
		/// This method allows you to set all the stencil buffer parameters in one call.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The stencil buffer is used to mask out pixels in the render target, allowing
		/// you to do effects like mirrors, cut-outs, stencil shadows and more. Each of
		/// your batches of rendering is likely to ignore the stencil buffer, 
		/// update it with new values, or apply it to mask the output of the render.
		/// The stencil test is:<PRE>
		/// (Reference Value &amp; Mask) CompareFunction (Stencil Buffer Value &amp; Mask)</PRE>
		/// The result of this will cause one of 3 actions depending on whether the test fails,
		/// succeeds but with the depth buffer check still failing, or succeeds with the
		/// depth buffer check passing too.</para>
		/// <para>
		/// Unlike other render states, stencilling is left for the application to turn
		/// on and off when it requires. This is because you are likely to want to change
		/// parameters between batches of arbitrary objects and control the ordering yourself.
		/// In order to batch things this way, you'll want to use OGRE's separate render queue
		/// groups (see RenderQueue) and register a RenderQueueListener to get notifications
		/// between batches.</para>
		/// <para>
		/// There are individual state change methods for each of the parameters set using 
		/// this method. 
		/// Note that the default values in this method represent the defaults at system 
		/// start up too.</para>
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		public void SetStencilBufferParams()
		{
			SetStencilBufferParams( CompareFunction.AlwaysPass, 0, -1, StencilOperation.Keep, StencilOperation.Keep,
			                        StencilOperation.Keep, false );
		}

		/// <summary>
		/// This method allows you to set all the stencil buffer parameters in one call.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The stencil buffer is used to mask out pixels in the render target, allowing
		/// you to do effects like mirrors, cut-outs, stencil shadows and more. Each of
		/// your batches of rendering is likely to ignore the stencil buffer, 
		/// update it with new values, or apply it to mask the output of the render.
		/// The stencil test is:<PRE>
		/// (Reference Value &amp; Mask) CompareFunction (Stencil Buffer Value &amp; Mask)</PRE>
		/// The result of this will cause one of 3 actions depending on whether the test fails,
		/// succeeds but with the depth buffer check still failing, or succeeds with the
		/// depth buffer check passing too.</para>
		/// <para>
		/// Unlike other render states, stencilling is left for the application to turn
		/// on and off when it requires. This is because you are likely to want to change
		/// parameters between batches of arbitrary objects and control the ordering yourself.
		/// In order to batch things this way, you'll want to use OGRE's separate render queue
		/// groups (see RenderQueue) and register a RenderQueueListener to get notifications
		/// between batches.</para>
		/// <para>
		/// There are individual state change methods for each of the parameters set using 
		/// this method. 
		/// Note that the default values in this method represent the defaults at system 
		/// start up too.</para>
		/// </remarks>
		/// <param name="function">The comparison function applied.</param>
		/// <param name="refValue">The reference value used in the comparison.</param>
		/// <param name="mask">
		/// The bitmask applied to both the stencil value and the reference value 
		/// before comparison.
		/// </param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetStencilBufferParams( CompareFunction function, int refValue )
		{
			SetStencilBufferParams( function, refValue, -1, StencilOperation.Keep, StencilOperation.Keep, StencilOperation.Keep,
			                        false );
		}

		/// <summary>
		/// This method allows you to set all the stencil buffer parameters in one call.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The stencil buffer is used to mask out pixels in the render target, allowing
		/// you to do effects like mirrors, cut-outs, stencil shadows and more. Each of
		/// your batches of rendering is likely to ignore the stencil buffer, 
		/// update it with new values, or apply it to mask the output of the render.
		/// The stencil test is:<PRE>
		/// (Reference Value &amp; Mask) CompareFunction (Stencil Buffer Value &amp; Mask)</PRE>
		/// The result of this will cause one of 3 actions depending on whether the test fails,
		/// succeeds but with the depth buffer check still failing, or succeeds with the
		/// depth buffer check passing too.</para>
		/// <para>
		/// Unlike other render states, stencilling is left for the application to turn
		/// on and off when it requires. This is because you are likely to want to change
		/// parameters between batches of arbitrary objects and control the ordering yourself.
		/// In order to batch things this way, you'll want to use OGRE's separate render queue
		/// groups (see RenderQueue) and register a RenderQueueListener to get notifications
		/// between batches.</para>
		/// <para>
		/// There are individual state change methods for each of the parameters set using 
		/// this method. 
		/// Note that the default values in this method represent the defaults at system 
		/// start up too.</para>
		/// </remarks>
		/// <param name="function">The comparison function applied.</param>
		/// <param name="refValue">The reference value used in the comparison.</param>
		/// <param name="mask">
		/// The bitmask applied to both the stencil value and the reference value 
		/// before comparison.
		/// </param>
		/// <param name="stencilFailOp">The action to perform when the stencil check fails.</param>
		/// <param name="depthFailOp">
		/// The action to perform when the stencil check passes, but the depth buffer check still fails.
		/// </param>
		/// <param name="passOp">The action to take when both the stencil and depth check pass.</param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetStencilBufferParams( CompareFunction function, int refValue, int mask, StencilOperation stencilFailOp,
		                                    StencilOperation depthFailOp, StencilOperation passOp )
		{
			SetStencilBufferParams( function, refValue, mask, stencilFailOp, depthFailOp, passOp, false );
		}

		/// <summary>
		/// This method allows you to set all the stencil buffer parameters in one call.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The stencil buffer is used to mask out pixels in the render target, allowing
		/// you to do effects like mirrors, cut-outs, stencil shadows and more. Each of
		/// your batches of rendering is likely to ignore the stencil buffer, 
		/// update it with new values, or apply it to mask the output of the render.
		/// The stencil test is:<PRE>
		/// (Reference Value &amp; Mask) CompareFunction (Stencil Buffer Value &amp; Mask)</PRE>
		/// The result of this will cause one of 3 actions depending on whether the test fails,
		/// succeeds but with the depth buffer check still failing, or succeeds with the
		/// depth buffer check passing too.</para>
		/// <para>
		/// Unlike other render states, stencilling is left for the application to turn
		/// on and off when it requires. This is because you are likely to want to change
		/// parameters between batches of arbitrary objects and control the ordering yourself.
		/// In order to batch things this way, you'll want to use OGRE's separate render queue
		/// groups (see RenderQueue) and register a RenderQueueListener to get notifications
		/// between batches.</para>
		/// <para>
		/// There are individual state change methods for each of the parameters set using 
		/// this method. 
		/// Note that the default values in this method represent the defaults at system 
		/// start up too.</para>
		/// </remarks>
		/// <param name="function">The comparison function applied.</param>
		/// <param name="refValue">The reference value used in the comparison.</param>
		/// <param name="mask">
		/// The bitmask applied to both the stencil value and the reference value 
		/// before comparison.
		/// </param>
		/// <param name="stencilFailOp">The action to perform when the stencil check fails.</param>
		/// <param name="depthFailOp">
		/// The action to perform when the stencil check passes, but the depth buffer check still fails.
		/// </param>
		/// <param name="passOp">The action to take when both the stencil and depth check pass.</param>
		/// <param name="twoSidedOperation">
		/// If set to true, then if you render both back and front faces 
		/// (you'll have to turn off culling) then these parameters will apply for front faces, 
		/// and the inverse of them will happen for back faces (keep remains the same).
		/// </param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetStencilBufferParams( CompareFunction function, int refValue, int mask,
		                                             StencilOperation stencilFailOp, StencilOperation depthFailOp,
		                                             StencilOperation passOp, bool twoSidedOperation );

		#endregion

		#region SetSurfaceParams

		/// <summary>
		/// Sets the surface properties to be used for future rendering.
		/// 
		/// This method sets the the properties of the surfaces of objects
		/// to be rendered after it. In this context these surface properties
		/// are the amount of each type of light the object reflects (determining
		/// it's color under different types of light), whether it emits light
		/// itself, and how shiny it is. Textures are not dealt with here,
		/// <see cref="SetTexture(int, bool, Texture)"/> method for details.
		/// This method is used by SetMaterial so does not need to be called
		/// direct if that method is being used.
		/// </summary>
		/// <param name="ambient">
		/// The amount of ambient (sourceless and directionless)
		/// light an object reflects. Affected by the color/amount of ambient light in the scene.
		/// </param>
		/// <param name="diffuse">
		/// The amount of light from directed sources that is
		/// reflected (affected by color/amount of point, directed and spot light sources)
		/// </param>
		/// <param name="specular">
		/// The amount of specular light reflected. This is also
		/// affected by directed light sources but represents the color at the
		/// highlights of the object.
		/// </param>
		/// <param name="emissive">
		/// The color of light emitted from the object. Note that
		/// this will make an object seem brighter and not dependent on lights in
		/// the scene, but it will not act as a light, so will not illuminate other
		/// objects. Use a light attached to the same SceneNode as the object for this purpose.
		/// </param>
		/// <param name="shininess">
		/// A value which only has an effect on specular highlights (so
		/// specular must be non-black). The higher this value, the smaller and crisper the
		/// specular highlights will be, imitating a more highly polished surface.
		/// This value is not constrained to 0.0-1.0, in fact it is likely to
		/// be more (10.0 gives a modest sheen to an object).
		/// </param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, Real shininess )
		{
			SetSurfaceParams( ambient, diffuse, specular, emissive, shininess, TrackVertexColor.None );
		}

		/// <summary>
		/// Sets the surface properties to be used for future rendering.
		/// 
		/// This method sets the the properties of the surfaces of objects
		/// to be rendered after it. In this context these surface properties
		/// are the amount of each type of light the object reflects (determining
		/// it's color under different types of light), whether it emits light
		/// itself, and how shiny it is. Textures are not dealt with here,
		/// <see cref="SetTexture(int, bool, Texture)"/> method for details.
		/// This method is used by SetMaterial so does not need to be called
		/// direct if that method is being used.
		/// </summary>
		/// <param name="ambient">
		/// The amount of ambient (sourceless and directionless)
		/// light an object reflects. Affected by the color/amount of ambient light in the scene.
		/// </param>
		/// <param name="diffuse">
		/// The amount of light from directed sources that is
		/// reflected (affected by color/amount of point, directed and spot light sources)
		/// </param>
		/// <param name="specular">
		/// The amount of specular light reflected. This is also
		/// affected by directed light sources but represents the color at the
		/// highlights of the object.
		/// </param>
		/// <param name="emissive">
		/// The color of light emitted from the object. Note that
		/// this will make an object seem brighter and not dependent on lights in
		/// the scene, but it will not act as a light, so will not illuminate other
		/// objects. Use a light attached to the same SceneNode as the object for this purpose.
		/// </param>
		/// <param name="shininess">
		/// A value which only has an effect on specular highlights (so
		/// specular must be non-black). The higher this value, the smaller and crisper the
		/// specular highlights will be, imitating a more highly polished surface.
		/// This value is not constrained to 0.0-1.0, in fact it is likely to
		/// be more (10.0 gives a modest sheen to an object).
		/// </param>
		/// <param name="tracking">
		/// A bit field that describes which of the ambient, diffuse, specular
		/// and emissive colors follow the vertex color of the primitive. When a bit in this field is set
		/// its colorValue is ignored. This is a combination of TVC_AMBIENT, TVC_DIFFUSE, TVC_SPECULAR(note that the shininess value is still
		/// taken from shininess) and TVC_EMISSIVE. TVC_NONE means that there will be no material property
		/// tracking the vertex colors.
		/// </param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive,
		                                       Real shininess, TrackVertexColor tracking );

		#endregion

		#region SetPointParameters

		/// <summary>
		/// Sets the size of points and how they are attenuated with distance.
		/// <remarks>
		/// When performing point rendering or point sprite rendering,
		/// point size can be attenuated with distance. The equation for
		/// doing this is attenuation = 1 / (constant + linear * dist + quadratic * d^2) .
		/// </remarks>
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetPointParameters( Real size, bool attenuationEnabled, Real constant, Real linear,
		                                         Real quadratic, Real minSize, Real maxSize );

		#endregion

		#region SetTexture

		/// <summary>
		/// Sets the details of a texture stage, to be used for all primitives
		/// rendered afterwards. User processes would
		/// not normally call this direct unless rendering
		/// primitives themselves - the SubEntity class
		/// is designed to manage materials for objects.
		/// Note that this method is called by SetMaterial.
		/// </summary>
		/// <param name="unit">The index of the texture unit to modify. Multitexturing hardware 
		/// can support multiple units (see TextureUnitCount)</param>
		/// <param name="enabled">Boolean to turn the unit on/off</param>
		/// <param name="textureName">
		/// The name of the texture to use - this should have
		/// already been loaded with TextureManager.Load.
		/// </param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetTexture( int unit, bool enabled, string textureName )
		{
			// load the texture
			var texture = (Texture)TextureManager.Instance.GetByName( textureName );
			SetTexture( unit, enabled, texture );
		}

		/// <summary>
		/// Sets the texture to bind to a given texture unit.
		/// 
		/// User processes would not normally call this direct unless rendering
		/// primitives themselves.
		/// </summary>
		/// <param name="unit">
		/// The index of the texture unit to modify. Multitexturing
		/// hardware can support multiple units <see cref="RenderSystemCapabilities.TextureUnitCount"/> 
		/// </param>
		/// <param name="enabled"></param>
		/// <param name="texture"></param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetTexture( int unit, bool enabled, Texture texture );

		#endregion

		#region SetVertexTexture

		/// <summary>
		/// Binds a texture to a vertex sampler.
		/// </summary>
		/// <remarks>
		/// Not all rendersystems support separate vertex samplers. For those that
		/// do, you can set a texture for them, separate to the regular texture
		/// samplers, using this method. For those that don't, you should use the
		/// regular texture samplers which are shared between the vertex and
		/// fragment units; calling this method will throw an exception.
		/// <see cref="RenderSystemCapabilities.VertexTextureUnitsShared"/>
		/// </remarks>
		/// <param name="unit"></param>
		/// <param name="texture"></param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void SetVertexTexture( int unit, Texture texture )
		{
			throw new NotSupportedException( "This rendersystem does not support separate vertex texture samplers, " +
			                                 "you should use the regular texture samplers which are shared between " +
			                                 "the vertex and fragment units." );
		}

		#endregion

		#region SetTextureAddressingMode

		/// <summary>
		/// Tells the hardware how to treat texture coordinates.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetTextureAddressingMode( int unit, UVWAddressing uvw );

		#endregion

		#region SetTextureMipmapBias

		/// <summary>
		/// Sets the mipmap bias value for a given texture unit.
		/// </summary>
		/// <remarks>
		/// This allows you to adjust the mipmap calculation up or down for a
		/// given texture unit. Negative values force a larger mipmap to be used, 
		/// positive values force a smaller mipmap to be used. Units are in numbers
		/// of levels, so +1 forces the mipmaps to one smaller level.
		/// </remarks>
		/// <note>Only does something if render system has capability RSC_MIPMAP_LOD_BIAS.</note>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetTextureMipmapBias( int unit, float bias );

		#endregion

		#region SetTextureBorderColor

		/// <summary>
		/// Tells the hardware what border color to use when texture addressing mode is set to Border
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="borderColor"></param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetTextureBorderColor( int unit, ColorEx borderColor );

		#endregion

		#region SetTextureBlendMode

		/// <summary>
		/// Sets the texture blend modes from a TextureLayer record.
		/// Meant for use internally only - apps should use the Material
		/// and TextureLayer classes.
		/// </summary>
		/// <param name="unit">Texture unit.</param>
		/// <param name="bm">Details of the blending modes.</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetTextureBlendMode( int unit, LayerBlendModeEx bm );

		#endregion

		#region SetTextureCoordCalculation

		/// <summary>
		/// Sets a method for automatically calculating texture coordinates for a stage.
		/// </summary>
		/// <param name="unit">Texture stage to modify.</param>
		/// <param name="method">Calculation method to use</param>
		/// <param name="frustum">Frustum, only used for projective effects</param>
		[OgreVersion( 1, 7, 2790 )]
		public void SetTextureCoordCalculation( int unit, TexCoordCalcMethod method )
		{
			SetTextureCoordCalculation( unit, method, null );
		}

		/// <summary>
		/// Sets a method for automatically calculating texture coordinates for a stage.
		/// </summary>
		/// <param name="unit">Texture stage to modify.</param>
		/// <param name="method">Calculation method to use</param>
		/// <param name="frustum">Frustum, only used for projective effects</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetTextureCoordCalculation( int unit, TexCoordCalcMethod method, Frustum frustum );

		#endregion

		#region SetTextureCoordSet

		/// <summary>
		/// Sets the index into the set of tex coords that will be currently used by the render system.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetTextureCoordSet( int stage, int index );

		#endregion

		#region SetTextureLayerAnisotropy

		/// <summary>
		/// Sets the maximal anisotropy for the specified texture unit.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetTextureLayerAnisotropy( int unit, int maxAnisotropy );

		#endregion

		#region SetTextureMatrix

		/// <summary>
		/// Sets the texture matrix for the specified stage.  Used to apply rotations, translations,
		/// and scaling to textures.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetTextureMatrix( int stage, Matrix4 xform );

		#endregion

		#region SetTextureUnitFiltering

		/// <summary>
		/// Sets a single filter for a given texture unit.
		/// </summary>
		/// <param name="unit">The texture unit to set the filtering options for.</param>
		/// <param name="type">The filter type.</param>
		/// <param name="filter">The filter to be used.</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void SetTextureUnitFiltering( int unit, FilterType type, FilterOptions filter );

		/// <summary>
		/// Sets the filtering options for a given texture unit.
		/// </summary>
		/// <param name="unit">The texture unit to set the filtering options for.</param>
		/// <param name="minFilter">The filter used when a texture is reduced in size.</param>
		/// <param name="magFilter">The filter used when a texture is magnified.</param>
		/// <param name="mipFilter">
		/// The filter used between mipmap levels, <see cref="FilterOptions.None"/> disables mipmapping.
		/// </param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void SetTextureUnitFiltering( int unit, FilterOptions minFilter, FilterOptions magFilter,
		                                             FilterOptions mipFilter )
		{
			SetTextureUnitFiltering( unit, FilterType.Min, minFilter );
			SetTextureUnitFiltering( unit, FilterType.Mag, magFilter );
			SetTextureUnitFiltering( unit, FilterType.Mip, mipFilter );
		}

		#endregion

		#region UnbindGpuProgram

		/// <summary>
		/// Unbinds the current GpuProgram of a given GpuProgramType.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void UnbindGpuProgram( GpuProgramType type )
		{
			switch ( type )
			{
				case GpuProgramType.Vertex:
					// mark clip planes dirty if changed (programmable can change space)
					if ( vertexProgramBound && clipPlanes.Count != 0 )
					{
						clipPlanesDirty = true;
					}

					vertexProgramBound = false;
					break;
				case GpuProgramType.Geometry:
					geometryProgramBound = false;
					break;
				case GpuProgramType.Fragment:
					fragmentProgramBound = false;
					break;
			}
		}

		#endregion

		#region IsGpuProgramBound

		/// <summary>
		/// Gets the bound status of a given GpuProgramType.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public bool IsGpuProgramBound( GpuProgramType type )
		{
			switch ( type )
			{
				case GpuProgramType.Vertex:
					return vertexProgramBound;
				case GpuProgramType.Geometry:
					return geometryProgramBound;
				case GpuProgramType.Fragment:
					return fragmentProgramBound;
			}
			return false;
		}

		#endregion

		#region UseLights

		/// <summary>
		/// Tells the rendersystem to use the attached set of lights (and no others) 
		/// up to the number specified (this allows the same list to be used with different
		/// count limits).
		/// </summary>
		/// <param name="lightList">List of lights.</param>
		/// <param name="limit">Max number of lights that can be used from the list currently.</param>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void UseLights( LightList lightList, int limit );

		#endregion

		#region DestroyRenderTarget

		/// <summary>
		/// Destroys a render target of any sort
		/// </summary>
		/// <param name="name"></param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void DestroyRenderTarget( string name )
		{
			var rt = DetachRenderTarget( name );
			rt.Dispose();
		}

		#endregion

		#region DestroyRenderWindow

		/// <summary>
		/// Destroys a render window
		/// </summary>
		/// <param name="name"></param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void DestroyRenderWindow( string name )
		{
			DestroyRenderTarget( name );
		}

		#endregion

		#region DestroyRenderTexture

		/// <summary>
		/// Destroys a render texture
		/// </summary>
		/// <param name="name"></param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void DestroyRenderTexture( string name )
		{
			DestroyRenderTarget( name );
		}

		#endregion

		#region InitializeFromRenderSystemCapabilities

		/// <summary>
		/// Initialize the render system from the capabilities
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public abstract void InitializeFromRenderSystemCapabilities( RenderSystemCapabilities caps, RenderTarget primary );

		#endregion

		#endregion Methods

		#region Object overrides

		/// <summary>
		/// Returns the name of this RenderSystem.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Name;
		}

		#endregion Object overrides

		#region DisposableObject Members

		/// <summary>
		/// Class level dispose method
		/// </summary>
		[OgreVersion( 1, 7, 2, "~RenderSystem" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					Shutdown();
					realCapabilities = null;
					currentCapabilities = null;
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion DisposableObject Members
	}
}