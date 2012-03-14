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
/*
 * Many thanks to the folks at Multiverse for providing the initial port for this class
 */

#endregion

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Configuration;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	///<summary>
	///    Object representing one pass or operation in a composition sequence. This provides a 
	///    method to conviently interleave RenderSystem commands between Render Queues.
	///</summary>
	public class CompositionPass : DisposableObject
	{
		#region Nested type: InputTexture

		public struct InputTexture
		{
			/// <summary>
			/// MRT surface index if applicable
			/// </summary>
			public int MrtIndex;

			/// <summary>
			/// Name (local) of the input texture (empty == no input)
			/// </summary>
			public string Name;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name">Name (local) of the input texture (empty == no input)</param>
			public InputTexture( string name )
			{
				this.Name = name;
				this.MrtIndex = 0;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name">Name (local) of the input texture (empty == no input)</param>
			/// <param name="mrtIndex">MRT surface index if applicable</param>
			public InputTexture( string name, int mrtIndex )
			{
				this.Name = name;
				this.MrtIndex = mrtIndex;
			}
		}

		#endregion

		#region Fields and Properties

		///<summary>
		///    Clear buffers (in case of CompositorPassType.Clear)
		///</summary>
		protected FrameBufferType clearBuffers;

		///<summary>
		///    Clear colour (in case of CompositorPassType.Clear)
		///</summary>
		protected ColorEx clearColor;

		///<summary>
		///    Clear depth (in case of CompositorPassType.Clear)
		///</summary>
		protected float clearDepth;

		///<summary>
		///    Clear stencil value (in case of CompositorPassType.Clear)
		///</summary>
		protected int clearStencil;

		protected string customType;

		///<summary>
		///    first render queue to render this pass (in case of CompositorPassType.RenderScene)
		///</summary>
		protected RenderQueueGroupID firstRenderQueue;

		///<summary>
		///    Identifier for this pass
		///</summary>
		protected uint identifier;

		///<summary>
		///    Inputs (for material used for rendering the quad)
		///    An empty string signifies that no input is used
		///</summary>
		protected InputTexture[] inputs = new InputTexture[ Config.MaxTextureLayers ];

		///<summary>
		///    last render queue to render this pass (in case of CompositorPassType.RenderScene)
		///</summary>
		protected RenderQueueGroupID lastRenderQueue;

		///<summary>
		///    Material used for rendering
		///</summary>
		protected Material material;

		/// <summary>
		/// Material scheme name
		/// </summary>
		protected string materialSchemeName;

		///<summary>
		///    Parent technique
		///</summary>
		protected CompositionTargetPass parent;

		/// <summary>
		/// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
		/// </summary>
		protected float quadBottom;

		/// <summary>
		/// true if quad should not cover whole screen
		/// </summary>
		protected bool quadCornerModified;

		/// <summary>
		/// 
		/// </summary>
		protected bool quadFarCorners;

		/// <summary>
		/// 
		/// </summary>
		protected bool quadFarCornersViewSpace;

		/// <summary>
		/// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
		/// </summary>
		protected float quadLeft;

		/// <summary>
		/// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
		/// </summary>
		protected float quadRight;

		/// <summary>
		/// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
		/// </summary>
		protected float quadTop;

		///<summary>
		///    Stencil operation parameters
		///</summary>
		protected bool stencilCheck;

		protected StencilOperation stencilDepthFailOp;
		protected StencilOperation stencilFailOp;
		protected CompareFunction stencilFunc;
		protected int stencilMask;
		protected StencilOperation stencilPassOp;
		protected int stencilRefValue;
		protected bool stencilTwoSidedOperation;

		///<summary>
		///    Type of composition pass
		///</summary>
		protected CompositorPassType type;

		///<summary>
		///    Parent technique
		///</summary>
		public CompositionTargetPass Parent
		{
			get
			{
				return this.parent;
			}
		}

		///<summary>
		///    Type of composition pass
		///</summary>
		public CompositorPassType Type
		{
			get
			{
				return this.type;
			}
			set
			{
				this.type = value;
			}
		}

		///<summary>
		///    Identifier for this pass
		///</summary>
		public uint Identifier
		{
			get
			{
				return this.identifier;
			}
			set
			{
				this.identifier = value;
			}
		}

		///<summary>
		///    Material used for rendering
		///</summary>
		public Material Material
		{
			get
			{
				return this.material;
			}
			set
			{
				this.material = value;
			}
		}

		///<summary>
		///    Material name to use for rendering
		///</summary>
		public string MaterialName
		{
			get
			{
				if ( this.material != null )
				{
					return this.material.Name;
				}
				return string.Empty;
			}
			set
			{
				this.material = (Material)MaterialManager.Instance[ value ];
			}
		}

		/// <summary>
		/// Get's or set's the material scheme used by this pass
		/// </summary>
		/// <remarks>
		/// Only applicable to passes that render the scene.
		/// <see cref="Technique.Scheme"/>
		/// </remarks>
		public string MaterialScheme
		{
			get
			{
				return this.materialSchemeName;
			}
			set
			{
				this.materialSchemeName = value;
			}
		}

		///<summary>
		///    first render queue to render this pass (in case of CompositorPassType.RenderScene)
		///</summary>
		public RenderQueueGroupID FirstRenderQueue
		{
			get
			{
				return this.firstRenderQueue;
			}
			set
			{
				this.firstRenderQueue = value;
			}
		}

		///<summary>
		///    last render queue to render this pass (in case of CompositorPassType.RenderScene)
		///</summary>
		public RenderQueueGroupID LastRenderQueue
		{
			get
			{
				return this.lastRenderQueue;
			}
			set
			{
				this.lastRenderQueue = value;
			}
		}

		///<summary>
		///    Clear buffers (in case of CompositorPassType.Clear)
		///</summary>
		public FrameBufferType ClearBuffers
		{
			get
			{
				return this.clearBuffers;
			}
			set
			{
				this.clearBuffers = value;
			}
		}

		///<summary>
		///    Clear colour (in case of CompositorPassType.Clear)
		///</summary>
		public ColorEx ClearColor
		{
			get
			{
				return this.clearColor;
			}
			set
			{
				this.clearColor = value;
			}
		}

		///<summary>
		///    Clear depth (in case of CompositorPassType.Clear)
		///</summary>
		public float ClearDepth
		{
			get
			{
				return this.clearDepth;
			}
			set
			{
				this.clearDepth = value;
			}
		}

		///<summary>
		///    Clear stencil value (in case of CompositorPassType.Clear)
		///</summary>
		public int ClearStencil
		{
			get
			{
				return this.clearStencil;
			}
			set
			{
				this.clearStencil = value;
			}
		}

		///<summary>
		///    Inputs (for material used for rendering the quad)
		///    An empty string signifies that no input is used
		///</summary>
		public InputTexture[] Inputs
		{
			get
			{
				return this.inputs;
			}
		}

		public bool StencilCheck
		{
			get
			{
				return this.stencilCheck;
			}
			set
			{
				this.stencilCheck = value;
			}
		}

		public CompareFunction StencilFunc
		{
			get
			{
				return this.stencilFunc;
			}
			set
			{
				this.stencilFunc = value;
			}
		}

		public int StencilRefValue
		{
			get
			{
				return this.stencilRefValue;
			}
			set
			{
				this.stencilRefValue = value;
			}
		}

		public int StencilMask
		{
			get
			{
				return this.stencilMask;
			}
			set
			{
				this.stencilMask = value;
			}
		}

		public StencilOperation StencilFailOp
		{
			get
			{
				return this.stencilFailOp;
			}
			set
			{
				this.stencilFailOp = value;
			}
		}

		public StencilOperation StencilDepthFailOp
		{
			get
			{
				return this.stencilDepthFailOp;
			}
			set
			{
				this.stencilDepthFailOp = value;
			}
		}

		public StencilOperation StencilPassOp
		{
			get
			{
				return this.stencilPassOp;
			}
			set
			{
				this.stencilPassOp = value;
			}
		}

		public bool StencilTwoSidedOperation
		{
			get
			{
				return this.stencilTwoSidedOperation;
			}
			set
			{
				this.stencilTwoSidedOperation = value;
			}
		}

		/// <summary>
		/// Returns true if camera frustum far corners are provided in the quad.
		/// </summary>
		public bool QuadFarCorners
		{
			get
			{
				return this.quadFarCorners;
			}
		}

		/// <summary>
		/// Returns true if the far corners provided in the quad are in view space
		/// </summary>
		public bool QuadFarCornersViewSpace
		{
			get
			{
				return this.quadFarCornersViewSpace;
			}
		}

		/// <summary>
		/// Get's or set's the type name of this custom composition pass.
		/// <see cref="CompositorManager.RegisterCustomCompositorPass"/>
		/// </summary>
		/// <note>
		/// applies when PassType is RenderCustom
		/// </note>
		public string CustomType
		{
			get
			{
				return this.customType;
			}
			set
			{
				this.customType = value;
			}
		}

		public bool IsSupported
		{
			get
			{
				if ( this.type == CompositorPassType.RenderQuad )
				{
					if ( this.material == null )
					{
						return false;
					}
					this.material.Compile();
					if ( this.material.SupportedTechniques.Count == 0 )
					{
						return false;
					}
				}
				return true;
			}
		}

		#endregion Fields and Properties

		#region Constructor

		public CompositionPass( CompositionTargetPass parent )
		{
			this.parent = parent;
			this.type = CompositorPassType.RenderQuad;
			this.identifier = 0;
			this.firstRenderQueue = RenderQueueGroupID.Background;
			this.lastRenderQueue = RenderQueueGroupID.SkiesLate;
			this.materialSchemeName = string.Empty;
			this.clearBuffers = FrameBufferType.Color | FrameBufferType.Depth;
			this.clearColor = new ColorEx( 0f, 0f, 0f, 0f );
			this.clearDepth = 1.0f;
			this.clearStencil = 0;
			this.stencilCheck = false;
			this.stencilFunc = CompareFunction.AlwaysPass;
			this.stencilRefValue = 0;
			this.stencilMask = 0x7FFFFFFF;
			this.stencilFailOp = StencilOperation.Keep;
			this.stencilDepthFailOp = StencilOperation.Keep;
			this.stencilPassOp = StencilOperation.Keep;
			this.stencilTwoSidedOperation = false;
			this.quadCornerModified = false;
			this.quadLeft = -1;
			this.quadTop = 1;
			this.quadRight = 1;
			this.quadBottom = -1;
			this.quadFarCorners = false;
			this.quadFarCornersViewSpace = false;
		}

		#endregion Constructor

		#region Methods

		#region InputTexture Management

		///<summary>
		///    Get the number of inputs used.  If there are holes in the inputs array,
		///    this number will include those entries as well.
		///</summary>
		///<remarks>
		///    Note applies when CompositorPassType is RenderQuad 
		///</remarks>	
		public int InputsCount
		{
			get
			{
				int count = 0;
				for ( int i = 0; i < this.inputs.Length; ++i )
				{
					if ( !string.IsNullOrEmpty( this.inputs[ i ].Name ) )
					{
						count = i + 1;
					}
				}
				return count;
			}
		}

		///<summary>
		///    Set an input local texture. An empty string clears the input.
		///</summary>
		///<param name="id">Input to set. Must be in 0..Config.MaxTextureLayers-1</param>
		///<param name="name">Which texture to bind to this input. An empty string clears the input.</param>
		/// <param name="mrtIndex"></param>
		///<remarks>
		///    Note applies when CompositorPassType is RenderQuad 
		///</remarks>	
		public void SetInput( int id, string name, int mrtIndex )
		{
			this.inputs[ id ] = new InputTexture( name, mrtIndex );
		}

		public void SetInput( int id, string name )
		{
			SetInput( id, name, 0 );
		}

		public void SetInput( int id )
		{
			SetInput( id, null, 0 );
		}

		///<summary>
		///    Get the value of an input.
		///</summary>
		///<param name="id">Input to get. Must be in 0..Config.MaxTextureLayers-1.</param>
		///<remarks>
		///    Note applies when CompositorPassType is RenderQuad 
		///</remarks>	
		public InputTexture GetInput( int id )
		{
			return this.inputs[ id ];
		}

		///<summary>
		///    Clear all inputs.
		///</summary>
		///<remarks>
		///    Note applies when CompositorPassType is RenderQuad 
		///</remarks>	
		public void ClearAllInputs()
		{
			for ( int i = 0; i < Config.MaxTextureLayers; i++ )
			{
				this.inputs[ i ].Name = String.Empty;
			}
		}

		#endregion InputTexture Management

		#region Quad Management

		/// <summary>
		/// Set quad normalised positions [-1;1]x[-1;1]
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		public void SetQuadCorners( float left, float top, float right, float bottom )
		{
			this.quadCornerModified = true;
			this.quadLeft = left;
			this.quadRight = right;
			this.quadTop = top;
			this.quadBottom = bottom;
		}

		/// <summary>
		/// Get quad normalised positions [-1;1]x[-1;1]
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		/// <returns></returns>
		public bool GetQuadCorners( out float left, out float top, out float right, out float bottom )
		{
			left = this.quadLeft;
			top = this.quadTop;
			right = this.quadRight;
			bottom = this.quadBottom;
			return this.quadCornerModified;
		}

		/// <summary>
		/// Sets the use of camera frustum far corners provided in the quad's normals
		/// </summary>
		/// <param name="farCorners"></param>
		/// <param name="farCornersViewSpace"></param>
		public void SetQuadFarCorners( bool farCorners, bool farCornersViewSpace )
		{
			this.quadFarCorners = farCorners;
			this.quadFarCornersViewSpace = farCornersViewSpace;
		}

		#endregion Quad Management

		#endregion Methods

		#region Disposable

		protected override void dispose( bool disposeManagedResources )
		{
			base.dispose( disposeManagedResources );
		}

		#endregion
	}
}
