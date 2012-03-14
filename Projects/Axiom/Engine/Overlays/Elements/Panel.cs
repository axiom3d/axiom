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
using System.Diagnostics;

using Axiom.Configuration;
using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Scripting;

#endregion Namespace Declarations

#region Ogre Synchronization Information

// <ogresynchronization>
//     <file name="OgrePanelOverlayElement.h"   revision="1.3.2.1" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
//     <file name="OgrePanelOverlayElement.cpp" revision="1.10.2.1" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
// </ogresynchronization>

#endregion

namespace Axiom.Overlays.Elements
{
	/// <summary>
	/// 	GuiElement representing a flat, single-material (or transparent) panel which can contain other elements.
	/// </summary>
	/// <remarks>
	/// 	This class subclasses OverlayElementContainer because it can contain other elements. Like other
	/// 	containers, if hidden it's contents are also hidden, if moved it's contents also move etc. 
	/// 	The panel itself is a 2D rectangle which is either completely transparent, or is rendered 
	/// 	with a single material. The texture(s) on the panel can be tiled depending on your requirements.
	/// 	<p/>
	/// 	This component is suitable for backgrounds and grouping other elements. Note that because
	/// 	it has a single repeating material it cannot have a discrete border (unless the texture has one and
	/// 	the texture is tiled only once). For a bordered panel, see it's subclass BorderPanel.
	/// 	<p/>
	/// 	Note that the material can have all the usual effects applied to it like multiple texture
	/// 	layers, scrolling / animated textures etc. For multiple texture layers, you have to set 
	/// 	the tiling level for each layer.
	/// </remarks>
	public class Panel : OverlayElementContainer
	{
		#region Member variables

		private const int POSITION = 0;
		private const int TEXTURE_COORDS = 1;
		protected Vector2 bottomRight;
		protected bool isTransparent;
		protected int numTexCoordsInBuffer;
		protected float[] tileX = new float[ Config.MaxTextureLayers ];
		protected float[] tileY = new float[ Config.MaxTextureLayers ];
		protected Vector2 topLeft;

		#endregion

		#region Constructors

		internal Panel( string name )
			: base( name )
		{
			//this.IsTransparent = false; //[FXCop Optimization : Do not initialize unnecessarily], Defaults to false, left here for clarity
			// initialize the default tiling to 1 for all layers
			for ( int i = 0; i < Config.MaxTextureLayers; i++ )
			{
				this.tileX[ i ] = 1.0f;
				this.tileY[ i ] = 1.0f;
			}

			// Defer creation of texcoord buffer until we know how big it needs to be
			//this.numTexCoordsInBuffer = 0; //[FXCop Optimization : Do not initialize unnecessarily], Defaults to 0, left here for clarity
			this.topLeft = new Vector2( 0.0f, 0.0f );
			this.bottomRight = new Vector2( 1.0f, 1.0f );
		}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			bool init = !isInitialized;
			base.Initialize();
			if ( init )
			{
				// setup the vertex data
				renderOperation.vertexData = new VertexData();

				// Vertex declaration: 1 position, add texcoords later depending on #layers
				// Create as separate buffers so we can lock & discard separately
				VertexDeclaration decl = renderOperation.vertexData.vertexDeclaration;
				decl.AddElement( POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position );
				renderOperation.vertexData.vertexStart = 0;
				renderOperation.vertexData.vertexCount = 4;

				// create the first vertex buffer, mostly static except during resizing
				HardwareVertexBuffer buffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( POSITION ), renderOperation.vertexData.vertexCount, BufferUsage.StaticWriteOnly );

				// bind the vertex buffer
				renderOperation.vertexData.vertexBufferBinding.SetBinding( POSITION, buffer );

				// no indices, and issue as a tri strip
				renderOperation.useIndices = false;
				renderOperation.operationType = OperationType.TriangleStrip;
				isInitialized = true;
			}
		}

		/// <summary>
		/// </summary>
		public void SetTiling( float x, float y, int layer )
		{
			Debug.Assert( layer < Config.MaxTextureLayers, "layer < Config.MaxTextureLayers" );
			Debug.Assert( x != 0 && y != 0, "tileX != 0 && tileY != 0" );

			this.tileX[ layer ] = x;
			this.tileY[ layer ] = y;

			isGeomUVsOutOfDate = true;
		}

		public float GetTileX( int layer )
		{
			return this.tileX[ layer ];
		}

		public float GetTileY( int layer )
		{
			return this.tileY[ layer ];
		}

		/// <summary>
		///    Internal method for setting up geometry, called by GuiElement.Update
		/// </summary>
		protected override void UpdatePositionGeometry()
		{
			/*
				0-----2
				|    /|
				|  /  |
				|/    |
				1-----3
			*/
			float left, right, top, bottom;

			/* Convert positions into -1, 1 coordinate space (homogenous clip space).
				- Left / right is simple range conversion
				- Top / bottom also need inverting since y is upside down - this means
				  that top will end up greater than bottom and when computing texture
				  coordinates, we have to flip the v-axis (ie. subtract the value from
				  1.0 to get the actual correct value).
			*/

			left = DerivedLeft * 2 - 1;
			right = left + ( width * 2 );
			top = -( ( DerivedTop * 2 ) - 1 );
			bottom = top - ( height * 2 );

			// get a reference to the position buffer
			HardwareVertexBuffer buffer = renderOperation.vertexData.vertexBufferBinding.GetBuffer( POSITION );

			// lock the buffer
			BufferBase data = buffer.Lock( BufferLocking.Discard );
			int index = 0;

			// Use the furthest away depth value, since materials should have depth-check off
			// This initialised the depth buffer for any 3D objects in front
			float zValue = Root.Instance.RenderSystem.MaximumDepthInputValue;
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				float* posPtr = data.ToFloatPointer();

				posPtr[ index++ ] = left;
				posPtr[ index++ ] = top;
				posPtr[ index++ ] = zValue;

				posPtr[ index++ ] = left;
				posPtr[ index++ ] = bottom;
				posPtr[ index++ ] = zValue;

				posPtr[ index++ ] = right;
				posPtr[ index++ ] = top;
				posPtr[ index++ ] = zValue;

				posPtr[ index++ ] = right;
				posPtr[ index++ ] = bottom;
				posPtr[ index ] = zValue;
			}

			// unlock the position buffer
			buffer.Unlock();
		}

		public override void UpdateRenderQueue( RenderQueue queue )
		{
			if ( isVisible )
			{
				// only add this panel to the render queue if it is not transparent
				// that would mean the panel should be a virtual container of sorts,
				// and the children would still be rendered
				if ( !this.isTransparent && material != null )
				{
					base.UpdateRenderQueue( queue, false );
				}

				foreach ( OverlayElement child in children.Values )
				{
					child.UpdateRenderQueue( queue );
				}
			}
		}


		/// <summary>
		///    Called to update the texture coords when layers change.
		/// </summary>
		protected override void UpdateTextureGeometry()
		{
			if ( material != null && isInitialized )
			{
				int numLayers = material.GetTechnique( 0 ).GetPass( 0 ).TextureUnitStatesCount;

				VertexDeclaration decl = renderOperation.vertexData.vertexDeclaration;

				// if the required layers is less than the current amount of tex coord buffers, remove
				// the extraneous buffers
				if ( this.numTexCoordsInBuffer > numLayers )
				{
					for ( int i = this.numTexCoordsInBuffer; i > numLayers; --i )
					{
						decl.RemoveElement( VertexElementSemantic.TexCoords, i );
					}
				}
				else if ( this.numTexCoordsInBuffer < numLayers )
				{
					// we need to add more buffers
					int offset = VertexElement.GetTypeSize( VertexElementType.Float2 ) * this.numTexCoordsInBuffer;

					for ( int i = this.numTexCoordsInBuffer; i < numLayers; ++i )
					{
						decl.AddElement( TEXTURE_COORDS, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, i );
						offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
					} // for
				} // if

				// if the number of layers changed at all, we'll need to reallocate buffer
				if ( this.numTexCoordsInBuffer != numLayers )
				{
					HardwareVertexBuffer newBuffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( TEXTURE_COORDS ), renderOperation.vertexData.vertexCount, BufferUsage.StaticWriteOnly );

					// Bind buffer, note this will unbind the old one and destroy the buffer it had
					renderOperation.vertexData.vertexBufferBinding.SetBinding( TEXTURE_COORDS, newBuffer );

					// record the current number of tex layers now
					this.numTexCoordsInBuffer = numLayers;
				} // if

				if ( this.numTexCoordsInBuffer != 0 )
				{
					// get the tex coord buffer
					HardwareVertexBuffer buffer = renderOperation.vertexData.vertexBufferBinding.GetBuffer( TEXTURE_COORDS );
					BufferBase data = buffer.Lock( BufferLocking.Discard );

#if !AXIOM_SAFE_ONLY
					unsafe
#endif
					{
						float* texPtr = data.ToFloatPointer();
						int texIndex = 0;

						int uvSize = VertexElement.GetTypeSize( VertexElementType.Float2 ) / sizeof( float );
						int vertexSize = decl.GetVertexSize( TEXTURE_COORDS ) / sizeof( float );

						for ( int i = 0; i < numLayers; i++ )
						{
							// Calc upper tex coords
							float upperX = this.bottomRight.x * this.tileX[ i ];
							float upperY = this.bottomRight.y * this.tileY[ i ];

							/*
								0-----2
								|    /|
								|  /  |
								|/    |
								1-----3
							*/
							// Find start offset for this set
							texIndex = ( i * uvSize );

							texPtr[ texIndex ] = this.topLeft.x;
							texPtr[ texIndex + 1 ] = this.topLeft.y;

							texIndex += vertexSize; // jump by 1 vertex stride
							texPtr[ texIndex ] = this.topLeft.x;
							texPtr[ texIndex + 1 ] = upperY;

							texIndex += vertexSize;
							texPtr[ texIndex ] = upperX;
							texPtr[ texIndex + 1 ] = this.topLeft.y;

							texIndex += vertexSize;
							texPtr[ texIndex ] = upperX;
							texPtr[ texIndex + 1 ] = upperY;
						} // for
					} // unsafev

					// unlock the buffer
					buffer.Unlock();
				}
			} // if material != null
		}

		public void SetUV( Real u1, Real v1, Real u2, Real v2 )
		{
			this.topLeft = new Vector2( u1, v1 );
			this.bottomRight = new Vector2( u2, v2 );
			isGeomUVsOutOfDate = true;
		}

		public void GetUV( out Real u1, out Real v1, out Real u2, out Real v2 )
		{
			u1 = this.topLeft.x;
			v1 = this.topLeft.y;
			u2 = this.bottomRight.x;
			v2 = this.bottomRight.y;
		}

		#endregion

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		public bool IsTransparent
		{
			get
			{
				return this.isTransparent;
			}
			set
			{
				this.isTransparent = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override string MaterialName
		{
			set
			{
				base.MaterialName = value;
				//UpdateTextureGeometry();
			}
			get
			{
				return base.MaterialName;
			}
		}

		#endregion

		#region ScriptableObject Interface Command Classes

		#region Nested type: TilingAttributeCommand

		[ScriptableProperty( "tiling", "The number of times to repeat the background texture.", typeof( Panel ) )]
		public class TilingAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as Panel;
				if ( element != null )
				{
					// NOTE: Only returns the top tiling
					return String.Format( "{0} {1} {2}", "0", element.GetTileX( 0 ), element.GetTileY( 0 ) );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as Panel;
				string[] parms = val.Split( ' ' );
				if ( element != null )
				{
					element.SetTiling( StringConverter.ParseFloat( parms[ 1 ] ), StringConverter.ParseFloat( parms[ 2 ] ), int.Parse( parms[ 0 ] ) );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: TransparentAttributeCommand

		[ScriptableProperty( "transparent", "Sets whether the panel is transparent, i.e. invisible, itself " + "but it's contents are still displayed.", typeof( Panel ) )]
		public class TransparentAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as Panel;
				if ( element != null )
				{
					return element.IsTransparent.ToString();
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as Panel;
				if ( element != null )
				{
					element.IsTransparent = StringConverter.ParseBool( val );
				}
			}

			#endregion
		}

		#endregion

		#region Nested type: UVCoordinatesAttributeCommand

		[ScriptableProperty( "uv_coords", "The texture coordinates for the texture. 1 set of uv values.", typeof( Panel ) )]
		public class UVCoordinatesAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as Panel;
				if ( element != null )
				{
					Real u1, v1, u2, v2;
					element.GetUV( out u1, out v1, out u2, out v2 );
					return string.Format( "{0} {1} {2} {3}", u1, v1, u2, v2 );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as Panel;
				string[] parms = val.Split( ' ' );
				if ( element != null )
				{
					element.SetUV( StringConverter.ParseFloat( parms[ 0 ] ), StringConverter.ParseFloat( parms[ 1 ] ), StringConverter.ParseFloat( parms[ 2 ] ), StringConverter.ParseFloat( parms[ 3 ] ) );
				}
			}

			#endregion
		}

		#endregion

		#endregion ScriptableObject Interface Command Classes
	}
}
