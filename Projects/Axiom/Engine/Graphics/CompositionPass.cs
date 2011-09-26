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
using System.Collections;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Configuration;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Graphics
{

	///<summary>
	///    Object representing one pass or operation in a composition sequence. This provides a 
	///    method to conviently interleave RenderSystem commands between Render Queues.
	///</summary>
	public class CompositionPass
	{
		public struct InputTexture
		{
			/// <summary>
			/// Name (local) of the input texture (empty == no input)
			/// </summary>
			public string Name;

			/// <summary>
			/// MRT surface index if applicable
			/// </summary>
			public int MrtIndex;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name">Name (local) of the input texture (empty == no input)</param>
			public InputTexture( string name )
			{
				Name = name;
				this.MrtIndex = 0;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name">Name (local) of the input texture (empty == no input)</param>
			/// <param name="mrtIndex">MRT surface index if applicable</param>
			public InputTexture( string name, int mrtIndex )
			{
				Name = name;
				this.MrtIndex = mrtIndex;
			}
		}

		#region Fields and Properties

		///<summary>
		///    Parent technique
		///</summary>
		protected CompositionTargetPass parent;
		///<summary>
		///    Parent technique
		///</summary>
		public CompositionTargetPass Parent
		{
			get
			{
				return parent;
			}
		}

		///<summary>
		///    Type of composition pass
		///</summary>
		protected CompositorPassType type;
		///<summary>
		///    Type of composition pass
		///</summary>
		public CompositorPassType Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
			}
		}

		///<summary>
		///    Identifier for this pass
		///</summary>
		protected uint identifier;
		///<summary>
		///    Identifier for this pass
		///</summary>
		public uint Identifier
		{
			get
			{
				return identifier;
			}
			set
			{
				identifier = value;
			}
		}

		///<summary>
		///    Material used for rendering
		///</summary>
		protected Material material;
		///<summary>
		///    Material used for rendering
		///</summary>
		public Material Material
		{
			get
			{
				return material;
			}
			set
			{
				material = value;
			}
		}

		///<summary>
		///    Material name to use for rendering
		///</summary>
		public string MaterialName
		{
			get
			{
				if ( material != null )
				{
					return material.Name;
				}
				return string.Empty;
			}
			set
			{
				material = (Material)MaterialManager.Instance[ value ];
			}
		}

		/// <summary>
		/// Material scheme name
		/// </summary>
		protected string materialSchemeName;
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
				return materialSchemeName;
			}
			set
			{
				materialSchemeName = value;
			}
		}

		///<summary>
		///    first render queue to render this pass (in case of CompositorPassType.RenderScene)
		///</summary>
		protected RenderQueueGroupID firstRenderQueue;
		///<summary>
		///    first render queue to render this pass (in case of CompositorPassType.RenderScene)
		///</summary>
		public RenderQueueGroupID FirstRenderQueue
		{
			get
			{
				return firstRenderQueue;
			}
			set
			{
				firstRenderQueue = value;
			}
		}

		///<summary>
		///    last render queue to render this pass (in case of CompositorPassType.RenderScene)
		///</summary>
		protected RenderQueueGroupID lastRenderQueue;
		///<summary>
		///    last render queue to render this pass (in case of CompositorPassType.RenderScene)
		///</summary>
		public RenderQueueGroupID LastRenderQueue
		{
			get
			{
				return lastRenderQueue;
			}
			set
			{
				lastRenderQueue = value;
			}
		}

		///<summary>
		///    Clear buffers (in case of CompositorPassType.Clear)
		///</summary>
		protected FrameBufferType clearBuffers;
		///<summary>
		///    Clear buffers (in case of CompositorPassType.Clear)
		///</summary>
		public FrameBufferType ClearBuffers
		{
			get
			{
				return clearBuffers;
			}
			set
			{
				clearBuffers = value;
			}
		}

		///<summary>
		///    Clear colour (in case of CompositorPassType.Clear)
		///</summary>
		protected ColorEx clearColor;
		///<summary>
		///    Clear colour (in case of CompositorPassType.Clear)
		///</summary>
		public ColorEx ClearColor
		{
			get
			{
				return clearColor;
			}
			set
			{
				clearColor = value;
			}
		}

		///<summary>
		///    Clear depth (in case of CompositorPassType.Clear)
		///</summary>
		protected float clearDepth;
		///<summary>
		///    Clear depth (in case of CompositorPassType.Clear)
		///</summary>
		public float ClearDepth
		{
			get
			{
				return clearDepth;
			}
			set
			{
				clearDepth = value;
			}
		}

		///<summary>
		///    Clear stencil value (in case of CompositorPassType.Clear)
		///</summary>
		protected int clearStencil;
		///<summary>
		///    Clear stencil value (in case of CompositorPassType.Clear)
		///</summary>
		public int ClearStencil
		{
			get
			{
				return clearStencil;
			}
			set
			{
				clearStencil = value;
			}
		}

		///<summary>
		///    Inputs (for material used for rendering the quad)
		///    An empty string signifies that no input is used
		///</summary>
		protected InputTexture[] inputs = new InputTexture[ Config.MaxTextureLayers ];
		///<summary>
		///    Inputs (for material used for rendering the quad)
		///    An empty string signifies that no input is used
		///</summary>
		public InputTexture[] Inputs
		{
			get
			{
				return inputs;
			}
		}

		///<summary>
		///    Stencil operation parameters
		///</summary>
		protected bool stencilCheck;
		public bool StencilCheck
		{
			get
			{
				return stencilCheck;
			}
			set
			{
				stencilCheck = value;
			}
		}

		protected CompareFunction stencilFunc;
		public CompareFunction StencilFunc
		{
			get
			{
				return stencilFunc;
			}
			set
			{
				stencilFunc = value;
			}
		}

		protected int stencilRefValue;
		public int StencilRefValue
		{
			get
			{
				return stencilRefValue;
			}
			set
			{
				stencilRefValue = value;
			}
		}

		protected int stencilMask;
		public int StencilMask
		{
			get
			{
				return stencilMask;
			}
			set
			{
				stencilMask = value;
			}
		}

		protected StencilOperation stencilFailOp;
		public StencilOperation StencilFailOp
		{
			get
			{
				return stencilFailOp;
			}
			set
			{
				stencilFailOp = value;
			}
		}

		protected StencilOperation stencilDepthFailOp;
		public StencilOperation StencilDepthFailOp
		{
			get
			{
				return stencilDepthFailOp;
			}
			set
			{
				stencilDepthFailOp = value;
			}
		}

		protected StencilOperation stencilPassOp;
		public StencilOperation StencilPassOp
		{
			get
			{
				return stencilPassOp;
			}
			set
			{
				stencilPassOp = value;
			}
		}

		protected bool stencilTwoSidedOperation;
		public bool StencilTwoSidedOperation
		{
			get
			{
				return stencilTwoSidedOperation;
			}
			set
			{
				stencilTwoSidedOperation = value;
			}
		}

		/// <summary>
		/// true if quad should not cover whole screen
		/// </summary>
		protected bool quadCornerModified;

		/// <summary>
		/// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
		/// </summary>
		protected float quadLeft;

		/// <summary>
		/// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
		/// </summary>
		protected float quadTop;

		/// <summary>
		/// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
		/// </summary>
		protected float quadRight;

		/// <summary>
		/// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
		/// </summary>
		protected float quadBottom;

		/// <summary>
		/// 
		/// </summary>
		protected bool quadFarCorners;
		/// <summary>
		/// Returns true if camera frustum far corners are provided in the quad.
		/// </summary>
		public bool QuadFarCorners
		{
			get
			{
				return quadFarCorners;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected bool quadFarCornersViewSpace;
		/// <summary>
		/// Returns true if the far corners provided in the quad are in view space
		/// </summary>
		public bool QuadFarCornersViewSpace
		{
			get
			{
				return quadFarCornersViewSpace;
			}
		}

		protected string customType;
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
				return customType;
			}
			set
			{
				customType = value;
			}
		}

		public bool IsSupported
		{
			get
			{
				if ( type == CompositorPassType.RenderQuad )
				{
					if ( material == null )
						return false;
					material.Compile();
					if ( material.SupportedTechniques.Count == 0 )
						return false;
				}
				return true;
			}
		}

		#endregion Fields and Properties

		#region Constructor

		public CompositionPass( CompositionTargetPass parent )
		{
			this.parent = parent;
			type = CompositorPassType.RenderQuad;
			identifier = 0;
			firstRenderQueue = RenderQueueGroupID.Background;
			lastRenderQueue = RenderQueueGroupID.SkiesLate;
			materialSchemeName = string.Empty;
			clearBuffers = FrameBufferType.Color | FrameBufferType.Depth;
			clearColor = new ColorEx( 0f, 0f, 0f, 0f );
			clearDepth = 1.0f;
			clearStencil = 0;
			stencilCheck = false;
			stencilFunc = CompareFunction.AlwaysPass;
			stencilRefValue = 0;
			stencilMask = 0x7FFFFFFF;
			stencilFailOp = StencilOperation.Keep;
			stencilDepthFailOp = StencilOperation.Keep;
			stencilPassOp = StencilOperation.Keep;
			stencilTwoSidedOperation = false;
			quadCornerModified = false;
			quadLeft = -1;
			quadTop = 1;
			quadRight = 1;
			quadBottom = -1;
			quadFarCorners = false;
			quadFarCornersViewSpace = false;
		}

		#endregion Constructor

		#region Methods

		#region InputTexture Management
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
			inputs[ id ] = new InputTexture( name, mrtIndex );
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
			return inputs[ id ];
		}

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
				var count = 0;
				for ( var i = 0; i < inputs.Length; ++i )
				{
					if ( !string.IsNullOrEmpty( inputs[ i ].Name ) )
						count = i + 1;
				}
				return count;
			}
		}

		///<summary>
		///    Clear all inputs.
		///</summary>
		///<remarks>
		///    Note applies when CompositorPassType is RenderQuad 
		///</remarks>	
		public void ClearAllInputs()
		{
			for ( var i = 0; i < Config.MaxTextureLayers; i++ )
				inputs[ i ].Name = String.Empty;
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
			quadCornerModified = true;
			quadLeft = left;
			quadRight = right;
			quadTop = top;
			quadBottom = bottom;
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
			left = quadLeft;
			top = quadTop;
			right = quadRight;
			bottom = quadBottom;
			return quadCornerModified;
		}

		/// <summary>
		/// Sets the use of camera frustum far corners provided in the quad's normals
		/// </summary>
		/// <param name="farCorners"></param>
		/// <param name="farCornersViewSpace"></param>
		public void SetQuadFarCorners( bool farCorners, bool farCornersViewSpace )
		{
			quadFarCorners = farCorners;
			quadFarCornersViewSpace = farCornersViewSpace;
		}

		#endregion Quad Management

		#endregion Methods
	}
}
