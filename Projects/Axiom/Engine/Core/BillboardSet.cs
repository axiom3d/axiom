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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Axiom.Collections;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///		A collection of billboards (faces which are always facing the camera) with the same (default) dimensions, material
	///		and which are fairly close proximity to each other.
	///	 </summary>
	///	 <remarks>
	///		Billboards are rectangles made up of 2 tris which are always facing the camera. They are typically used
	///		for special effects like particles. This class collects together a set of billboards with the same (default) dimensions,
	///		material and relative locality in order to process them more efficiently. The entire set of billboards will be
	///		culled as a whole (by default, although this can be changed if you want a large set of billboards
	///		which are spread out and you want them culled individually), individual Billboards have locations which are relative to the set (which itself derives it's
	///		position from the SceneNode it is attached to since it is a SceneObject), they will be rendered as a single rendering operation,
	///		and some calculations will be sped up by the fact that they use the same dimensions so some workings can be reused.
	///		<p/>
	///		A BillboardSet can be created using the SceneManager.CreateBillboardSet method. They can also be used internally
	///		by other classes to create effects.
	/// </remarks>
	public class BillboardSet : MovableObject, IRenderable
	{
		#region Fields

		/// <summary>Bounds of all billboards in this set</summary>
		protected AxisAlignedBox aab = new AxisAlignedBox();

		/// <summary>Origin of each billboard</summary>
		protected BillboardOrigin originType = BillboardOrigin.Center;

		protected BillboardRotationType rotationType = BillboardRotationType.Texcoord;

		/// <summary>Default width/height of each billboard.</summary>
		protected float defaultParticleWidth = 100;

		protected float defaultParticleHeight = 100;

		/// <summary>Name of the material to use</summary>
		protected string materialName = "BaseWhite";

		/// <summary>Reference to the material to use</summary>
		protected Material material;

		/// <summary></summary>
		protected bool allDefaultSize = true;

		protected bool allDefaultRotation = true;

		/// <summary></summary>
		protected bool autoExtendPool = true;

		/// <summary>True if particles follow the object the
		/// ParticleSystem is attached to.</summary>
		protected bool worldSpace = false;

		// various collections for pooling billboards
		protected List<Billboard> activeBillboards = new List<Billboard>();
		protected List<Billboard> freeBillboards = new List<Billboard>();
		protected List<Billboard> billboardPool = new List<Billboard>();

		// Geometry data.
		protected VertexData vertexData = null;
		protected IndexData indexData = null;

		/// <summary>Indicates whether or not each billboard should be culled individually.</summary>
		protected bool cullIndividual = false;

		/// <summary>Type of billboard to render.</summary>
		protected BillboardType billboardType = BillboardType.Point;

		/// <summary>Common direction for billboard oriented with type Common.</summary>
		protected Vector3 commonDirection = Vector3.UnitZ;

		/// <summary>Common up vector for billboard oriented with type Perpendicular.</summary>
		protected Vector3 commonUpVector = Vector3.UnitY;

		/// <summary>The local bounding radius of this object.</summary>
		protected float boundingRadius;

		protected int numVisibleBillboards;

		/// <summary>
		///		Are tex coords fixed?  If not they have been modified.
		/// </summary>
		protected bool fixedTextureCoords;

		// Temporary matrix for checking billboard visible
		protected Matrix4[] world = new Matrix4[ 1 ];
		protected Sphere sphere = new Sphere();

		// used to keep track of current index in GenerateVertices
		protected int posIndex = 0;
		protected int colorIndex = 0;
		protected int texIndex = 0;

		protected bool pointRendering = false;
		protected bool accurateFacing = false;
		protected IntPtr lockPtr = IntPtr.Zero;
		protected int ptrOffset = 0;
		protected Vector3[] vOffset = new Vector3[ 4 ];
		protected Camera currentCamera;
		protected float leftOff, rightOff, topOff, bottomOff;
		protected Vector3 camX, camY, camDir;
		protected Quaternion camQ;
		protected Vector3 camPos;

		private bool buffersCreated = false;
		private int poolSize = 0;
		private bool externalData = false;
		private List<RectangleF> textureCoords = new List<RectangleF>();

		protected HardwareVertexBuffer mainBuffer;

		protected List<Vector4> customParams = new List<Vector4>( 20 );

		// Template texcoord data
		private float[] texData = new float[ 8 ] {
													   -0.5f, 0.5f,
													   0.5f, 0.5f,
													   -0.5f, -0.5f,
													   0.5f, -0.5f
											   };

		#endregion Fields

		#region Constructors

		/// <summary>
		///		Public constructor.  Should not be created manually, must be created using a SceneManager.
		/// </summary>
		internal BillboardSet( string name, int poolSize )
			: this( name, poolSize, false )
		{
		}

		/// <summary>
		///		Public constructor.  Should not be created manually, must be created using a SceneManager.
		/// </summary>
		internal BillboardSet( string name, int poolSize, bool externalData )
			: base( name )
		{
			this.PoolSize = poolSize;
			this.externalData = externalData;

			this.SetDefaultDimensions( 100, 100 );
			this.MaterialName = "BaseWhite";
			this.castShadows = false;
			this.SetTextureStacksAndSlices( 1, 1 );
		}

        #endregion Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposeManagedResources"></param>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!this.IsDisposed)
            {
                if (disposeManagedResources)
                {
                    if (this.renderOperation != null)
                    {
                        if (!this.renderOperation.IsDisposed)
                            this.renderOperation.Dispose();

                        this.renderOperation = null;
                    }
                }
            }

            base.dispose(disposeManagedResources);
        }

        #region Methods

		/// <summary>
		///     Generate the vertices for all the billboards relative to the camera
		///     Also take the opportunity to update the vertex colours
		///     May as well do it here to save on loops elsewhere
		///	 </summary>
		internal void BeginBillboards()
		{
			// Make sure we aren't calling this more than once
			Debug.Assert( this.lockPtr == IntPtr.Zero );

			/* NOTE: most engines generate world coordinates for the billboards
			   directly, taking the world axes of the camera as offsets to the
			   center points. I take a different approach, reverse-transforming
			   the camera world axes into local billboard space.
			   Why?
			   Well, it's actually more efficient this way, because I only have to
			   reverse-transform using the billboardset world matrix (inverse)
			   once, from then on it's simple additions (assuming identically
			   sized billboards). If I transformed every billboard center by it's
			   world transform, that's a matrix multiplication per billboard
			   instead.
			   I leave the final transform to the render pipeline since that can
			   use hardware TnL if it is available.
			*/

			// create vertex and index buffers if they haven't already been
			if ( !this.buffersCreated )
			{
				this.CreateBuffers();
			}

			// Only calculate vertex offets et al if we're not point rendering
			if ( !this.pointRendering )
			{
				// Get offsets for origin type
				this.GetParametricOffsets( out this.leftOff, out this.rightOff, out this.topOff, out this.bottomOff );

				// Generate axes etc up-front if not oriented per-billboard
				if ( this.billboardType != BillboardType.OrientedSelf &&
					 this.billboardType != BillboardType.PerpendicularSelf &&
					 !( this.accurateFacing && this.billboardType != BillboardType.PerpendicularCommon ) )
				{
					this.GenerateBillboardAxes( ref this.camX, ref this.camY );

					/* If all billboards are the same size we can precalculate the
					   offsets and just use '+' instead of '*' for each billboard,
					   and it should be faster.
					*/
					this.GenerateVertexOffsets( this.leftOff,
												this.rightOff,
												this.topOff,
												this.bottomOff,
												this.defaultParticleWidth,
												this.defaultParticleHeight,
												ref this.camX,
												ref this.camY,
												this.vOffset );
				}
			}

			// Init num visible
			this.numVisibleBillboards = 0;

			// Lock the buffer
			this.lockPtr = this.mainBuffer.Lock( BufferLocking.Discard );
			this.ptrOffset = 0;
		}

		internal void InjectBillboard( Billboard bb )
		{
			// Skip if not visible (NB always true if not bounds checking individual billboards)
			if ( !this.IsBillboardVisible( this.currentCamera, bb ) )
			{
				return;
			}

			if ( !this.pointRendering &&
				 ( this.billboardType == BillboardType.OrientedSelf ||
				   this.billboardType == BillboardType.PerpendicularSelf ||
				   ( this.accurateFacing && this.billboardType != BillboardType.PerpendicularCommon ) ) )
			{
				// Have to generate axes & offsets per billboard
				this.GenerateBillboardAxes( ref this.camX, ref this.camY, bb );
			}

			// If they're all the same size or we're point rendering
			if ( this.allDefaultSize || this.pointRendering )
			{
				/* No per-billboard checking, just blast through.
				   Saves us an if clause every billboard which may
				   make a difference.
				*/

				if ( !this.pointRendering &&
					 ( this.billboardType == BillboardType.OrientedSelf ||
					   this.billboardType == BillboardType.PerpendicularSelf ||
					   ( this.accurateFacing && this.billboardType != BillboardType.PerpendicularCommon ) ) )
				{
					this.GenerateVertexOffsets( this.leftOff,
												this.rightOff,
												this.topOff,
												this.bottomOff,
												this.defaultParticleWidth,
												this.defaultParticleHeight,
												ref this.camX,
												ref this.camY,
												this.vOffset );
				}
				this.GenerateVertices( this.vOffset, bb );
			}
			else // not all default size and not point rendering
			{
				Vector3[] vOwnOffset = new Vector3[ 4 ];
				// If it has own dimensions, or self-oriented, gen offsets
				if ( this.billboardType == BillboardType.OrientedSelf ||
					 this.billboardType == BillboardType.PerpendicularSelf ||
					 bb.HasOwnDimensions ||
					 ( this.accurateFacing && this.billboardType != BillboardType.PerpendicularCommon ) )
				{
					// Generate using own dimensions
					this.GenerateVertexOffsets( this.leftOff,
												this.rightOff,
												this.topOff,
												this.bottomOff,
												bb.Width,
												bb.Height,
												ref this.camX,
												ref this.camY,
												vOwnOffset );
					// Create vertex data
					this.GenerateVertices( vOwnOffset, bb );
				}
				else // Use default dimension, already computed before the loop, for faster creation
				{
					this.GenerateVertices( this.vOffset, bb );
				}
			}
			// Increment visibles
			this.numVisibleBillboards++;
		}

		internal void EndBillboards()
		{
			// Make sure we aren't double unlocking
			Debug.Assert( this.lockPtr != IntPtr.Zero );
			this.mainBuffer.Unlock();
			this.lockPtr = IntPtr.Zero;
		}

		protected void SetBounds( AxisAlignedBox box, float radius )
		{
			this.aab = box;
			this.boundingRadius = radius;
		}

		/// <summary>
		///		Callback used by Billboards to notify their parent that they have been resized.
		///	 </summary>
		protected internal void NotifyBillboardResized()
		{
			this.allDefaultSize = false;
		}

		/// <summary>
		///		Callback used by Billboards to notify their parent that they have been resized.
		/// </summary>
		protected internal void NotifyBillboardRotated()
		{
			this.allDefaultRotation = false;
		}

		/// <summary>
		///		Notifies the billboardset that texture coordinates will be modified
		///		for this set.
		///	 </summary>
		protected internal void NotifyBillboardTextureCoordsModified()
		{
			this.fixedTextureCoords = false;
		}

		/// <summary>
		///		Internal method for increasing pool size.
		/// </summary>
		/// <param name="size"></param>
		protected virtual void IncreasePool( int size )
		{
			int oldSize = this.billboardPool.Count;

			// expand the capacity a bit
			this.billboardPool.Capacity += size;

			// add fresh Billboard objects to the new slots
			for ( int i = oldSize; i < size; ++i )
			{
				this.billboardPool.Add( new Billboard() );
			}
		}

		/// <summary>
		///		Determines whether the supplied billboard is visible in the camera or not.
		///	 </summary>
		/// <param name="camera"></param>
		/// <param name="billboard"></param>
		/// <returns></returns>
		protected bool IsBillboardVisible( Camera camera, Billboard billboard )
		{
			// if not culling each one, return true always
			if ( !this.cullIndividual )
			{
				return true;
			}

			// get the world matrix of this billboard set
			this.GetWorldTransforms( this.world );

			// get the center of the bounding sphere
			this.sphere.Center = this.world[ 0 ] * billboard.Position;

			// calculate the radius of the bounding sphere for the billboard
			if ( billboard.HasOwnDimensions )
			{
				this.sphere.Radius = Utility.Max( billboard.Width, billboard.Height );
			}
			else
			{
				this.sphere.Radius = Utility.Max( this.defaultParticleWidth, this.defaultParticleHeight );
			}

			// finally, see if the sphere is visible in the camera
			return camera.IsObjectVisible( this.sphere );
		}

		protected void SetTextureStacksAndSlices( int stacks, int slices )
		{
			if ( stacks == 0 )
			{
				stacks = 1;
			}
			if ( slices == 0 )
			{
				slices = 1;
			}
			//  clear out any previous allocation
			this.textureCoords.Clear();
			//  make room
			this.textureCoords.Capacity = stacks * slices;
			while ( this.textureCoords.Count < stacks * slices )
			{
				this.textureCoords.Add( new RectangleF() );
			}
			ushort coordIndex = 0;
			//  spread the U and V coordinates across the rects
			for ( uint v = 0; v < stacks; ++v )
			{
				//  (float)X / X is guaranteed to be == 1.0f for X up to 8 million, so
				//  our range of 1..256 is quite enough to guarantee perfect coverage.
				float top = (float)v / (float)stacks;
				float bottom = ( (float)v + 1 ) / (float)stacks;
				for ( uint u = 0; u < slices; ++u )
				{
					RectangleF r = new RectangleF();
					r.Left = (float)u / (float)slices;
					r.Top = top;
					r.Width = ( (float)u + 1 ) / (float)slices - r.Left;
					r.Height = bottom - top;
					this.textureCoords[ coordIndex ] = r;
					++coordIndex;
				}
			}
			Debug.Assert( coordIndex == stacks * slices );
		}

		/// <summary>
		///		Overloaded method.
		///	 </summary>
		/// <param name="camera"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		protected virtual void GenerateBillboardAxes( ref Vector3 x, ref Vector3 y )
		{
			this.GenerateBillboardAxes( ref x, ref y, null );
		}

		/// <summary>
		///		Generates billboard corners.
		///	 </summary>
		/// <param name="camera"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="billboard"></param>
		/// <remarks>Billboard param only required for type OrientedSelf</remarks>
		protected virtual void GenerateBillboardAxes( ref Vector3 x, ref Vector3 y, Billboard bb )
		{
			// If we're using accurate facing, recalculate camera direction per BB
			if ( this.accurateFacing &&
				 ( this.billboardType == BillboardType.Point ||
				   this.billboardType == BillboardType.OrientedCommon ||
				   this.billboardType == BillboardType.OrientedSelf ) )
			{
				// cam -> bb direction
				this.camDir = bb.Position - this.camPos;
				this.camDir.Normalize();
			}

			switch ( this.billboardType )
			{
				case BillboardType.Point:
					if ( this.accurateFacing )
					{
						// Point billboards will have 'up' based on but not equal to cameras
						y = this.camQ * Vector3.UnitY;
						x = this.camDir.Cross( y );
						x.Normalize();
						y = x.Cross( this.camDir ); // both normalised already
					}
					else
					{
						// Get camera axes for X and Y (depth is irrelevant)
						x = this.camQ * Vector3.UnitX;
						y = this.camQ * Vector3.UnitY;
					}
					break;

				case BillboardType.OrientedCommon:
					// Y-axis is common direction
					// X-axis is cross with camera direction
					y = this.commonDirection;
					x = this.camDir.Cross( y );
					x.Normalize();
					break;

				case BillboardType.OrientedSelf:
					// Y-axis is direction
					// X-axis is cross with camera direction
					// Scale direction first
					y = bb.Direction;
					x = this.camDir.Cross( y );
					x.Normalize();
					break;

				case BillboardType.PerpendicularCommon:
					// X-axis is up-vector cross common direction
					// Y-axis is common direction cross X-axis
					x = this.commonUpVector.Cross( this.commonDirection );
					y = this.commonDirection.Cross( x );
					break;

				case BillboardType.PerpendicularSelf:
					// X-axis is up-vector cross own direction
					// Y-axis is own direction cross X-axis
					x = this.commonUpVector.Cross( bb.Direction );
					x.Normalize();
					y = bb.Direction.Cross( x ); // both should be normalised
					break;
			}

#if NOT
		// Default behavior is that billboards are in local node space
		// so orientation of camera (in world space) must be reverse-transformed
		// into node space to generate the axes
			Quaternion invTransform = parentNode.DerivedOrientation.Inverse();
			Quaternion camQ = Quaternion.Zero;

			switch (billboardType) {
				case BillboardType.Point:
					// Get camera world axes for X and Y (depth is irrelevant)
					camQ = camera.DerivedOrientation;
						// Convert into billboard local space
						camQ = invTransform * camQ;
					x = camQ * Vector3.UnitX;
					y = camQ * Vector3.UnitY;
					break;
				case BillboardType.OrientedCommon:
					// Y-axis is common direction
					// X-axis is cross with camera direction
					y = commonDirection;
					y.Normalize();
						// Convert into billboard local space
						camQ = invTransform * camQ;
					x = camQ * camera.DerivedDirection.Cross(y);
					x.Normalize();
					break;
				case BillboardType.OrientedSelf:
					// Y-axis is direction
					// X-axis is cross with camera direction
					y = billboard.Direction;
						// Convert into billboard local space
						camQ = invTransform * camQ;
					x = camQ * camera.DerivedDirection.Cross(y);
					x.Normalize();
					break;
				case BillboardType.PerpendicularCommon:
					// X-axis is common direction cross common up vector
					// Y-axis is coplanar with common direction and common up vector
					x = commonDirection.Cross(commonUpVector);
					x.Normalize();
					y = x.Cross(commonDirection);
					y.Normalize();
					break;
				case BillboardType.PerpendicularSelf:
					// X-axis is direction cross common up vector
					// Y-axis is coplanar with direction and common up vector
					x = billboard.Direction.Cross(commonUpVector);
					x.Normalize();
					y = x.Cross(billboard.Direction);
					y.Normalize();
					break;
			}
#endif
		}

		/// <summary>
		///		Generate parametric offsets based on the origin.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="top"></param>
		/// <param name="bottom"></param>
		protected void GetParametricOffsets( out float left, out float right, out float top, out float bottom )
		{
			left = 0.0f;
			right = 0.0f;
			top = 0.0f;
			bottom = 0.0f;

			switch ( this.originType )
			{
				case BillboardOrigin.TopLeft:
					left = 0.0f;
					right = 1.0f;
					top = 0.0f;
					bottom = -1.0f;
					break;

				case BillboardOrigin.TopCenter:
					left = -0.5f;
					right = 0.5f;
					top = 0.0f;
					bottom = 1.0f;
					break;

				case BillboardOrigin.TopRight:
					left = -1.0f;
					right = 0.0f;
					top = 0.0f;
					bottom = -1.0f;
					break;

				case BillboardOrigin.CenterLeft:
					left = 0.0f;
					right = 1.0f;
					top = 0.5f;
					bottom = -0.5f;
					break;

				case BillboardOrigin.Center:
					left = -0.5f;
					right = 0.5f;
					top = 0.5f;
					bottom = -0.5f;
					break;

				case BillboardOrigin.CenterRight:
					left = -1.0f;
					right = 0.0f;
					top = 0.5f;
					bottom = -0.5f;
					break;

				case BillboardOrigin.BottomLeft:
					left = 0.0f;
					right = 1.0f;
					top = 1.0f;
					bottom = 0.0f;
					break;

				case BillboardOrigin.BottomCenter:
					left = -0.5f;
					right = 0.5f;
					top = 1.0f;
					bottom = 0.0f;
					break;

				case BillboardOrigin.BottomRight:
					left = -1.0f;
					right = 0.0f;
					top = 1.0f;
					bottom = 0.0f;
					break;
			}
		}

		protected void GenerateVertices( Vector3[] offsets, Billboard bb )
		{
			int color = Root.Instance.ConvertColor( bb.Color );
			// Texcoords
			Debug.Assert( bb.UseTexcoordRect || bb.TexcoordIndex < this.textureCoords.Count );
			RectangleF r = bb.UseTexcoordRect ? bb.TexcoordRect : this.textureCoords[ bb.TexcoordIndex ];

			if ( this.pointRendering )
			{
				unsafe
				{
					float* posPtr = (float*)this.lockPtr.ToPointer();
					int* colPtr = (int*)posPtr;

					// Single vertex per billboard, ignore offsets
					// position
					posPtr[ this.ptrOffset++ ] = bb.Position.x;
					posPtr[ this.ptrOffset++ ] = bb.Position.y;
					posPtr[ this.ptrOffset++ ] = bb.Position.z;
					colPtr[ this.ptrOffset++ ] = color;
					// No texture coords in point rendering
				}
			}
			else if ( this.allDefaultRotation || bb.Rotation == 0 )
			{
				unsafe
				{
					float* posPtr = (float*)this.lockPtr.ToPointer();
					int* colPtr = (int*)posPtr;
					float* texPtr = (float*)posPtr;

					// Left-top
					// Positions
					posPtr[ this.ptrOffset++ ] = offsets[ 0 ].x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = offsets[ 0 ].y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = offsets[ 0 ].z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = r.Left;
					texPtr[ this.ptrOffset++ ] = r.Top;

					// Right-top
					// Positions
					posPtr[ this.ptrOffset++ ] = offsets[ 1 ].x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = offsets[ 1 ].y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = offsets[ 1 ].z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = r.Right;
					texPtr[ this.ptrOffset++ ] = r.Top;

					// Left-bottom
					// Positions
					posPtr[ this.ptrOffset++ ] = offsets[ 2 ].x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = offsets[ 2 ].y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = offsets[ 2 ].z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = r.Left;
					texPtr[ this.ptrOffset++ ] = r.Bottom;

					// Right-bottom
					// Positions
					posPtr[ this.ptrOffset++ ] = offsets[ 3 ].x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = offsets[ 3 ].y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = offsets[ 3 ].z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = r.Right;
					texPtr[ this.ptrOffset++ ] = r.Bottom;
				}
			}
			else if ( this.rotationType == BillboardRotationType.Vertex )
			{
				// TODO: Cache axis when billboard type is BillboardType.Point or
				//       BillboardType.PerpendicularCommon
				Vector3 axis = ( offsets[ 3 ] - offsets[ 0 ] ).Cross( offsets[ 2 ] - offsets[ 1 ] );
				axis.Normalize();

				Quaternion rotation = Quaternion.FromAngleAxis( bb.rotationInRadians, axis );
				Vector3 pt;

				unsafe
				{
					float* posPtr = (float*)this.lockPtr.ToPointer();
					int* colPtr = (int*)posPtr;
					float* texPtr = (float*)posPtr;

					// Left-top
					// Positions
					pt = rotation * offsets[ 0 ];
					posPtr[ this.ptrOffset++ ] = offsets[ 0 ].x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = offsets[ 0 ].y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = offsets[ 0 ].z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = r.Left;
					texPtr[ this.ptrOffset++ ] = r.Top;

					// Right-top
					// Positions
					pt = rotation * offsets[ 1 ];
					posPtr[ this.ptrOffset++ ] = pt.x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = pt.y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = pt.z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = r.Right;
					texPtr[ this.ptrOffset++ ] = r.Top;

					// Left-bottom
					// Positions
					pt = rotation * offsets[ 2 ];
					posPtr[ this.ptrOffset++ ] = pt.x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = pt.y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = pt.z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = r.Left;
					texPtr[ this.ptrOffset++ ] = r.Bottom;

					// Right-bottom
					// Positions
					pt = rotation * offsets[ 3 ];
					posPtr[ this.ptrOffset++ ] = pt.x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = pt.y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = pt.z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = r.Right;
					texPtr[ this.ptrOffset++ ] = r.Bottom;
				}
			}
			else
			{
				float cos_rot = Utility.Cos( bb.rotationInRadians );
				float sin_rot = Utility.Sin( bb.rotationInRadians );

				float width = ( r.Right - r.Left ) / 2;
				float height = ( r.Bottom - r.Top ) / 2;
				float mid_u = r.Left + width;
				float mid_v = r.Top + height;

				float cos_rot_w = cos_rot * width;
				float cos_rot_h = cos_rot * height;
				float sin_rot_w = sin_rot * width;
				float sin_rot_h = sin_rot * height;

				unsafe
				{
					float* posPtr = (float*)this.lockPtr.ToPointer();
					int* colPtr = (int*)posPtr;
					float* texPtr = (float*)posPtr;

					// Left-top
					// Positions
					posPtr[ this.ptrOffset++ ] = offsets[ 0 ].x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = offsets[ 0 ].y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = offsets[ 0 ].z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = mid_u - cos_rot_w + sin_rot_h;
					texPtr[ this.ptrOffset++ ] = mid_v - sin_rot_w - cos_rot_h;

					// Right-top
					// Positions
					posPtr[ this.ptrOffset++ ] = offsets[ 1 ].x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = offsets[ 1 ].y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = offsets[ 1 ].z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = mid_u + cos_rot_w + sin_rot_h;
					texPtr[ this.ptrOffset++ ] = mid_v + sin_rot_w - cos_rot_h;

					// Left-bottom
					// Positions
					posPtr[ this.ptrOffset++ ] = offsets[ 2 ].x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = offsets[ 2 ].y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = offsets[ 2 ].z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = mid_u - cos_rot_w - sin_rot_h;
					texPtr[ this.ptrOffset++ ] = mid_v - sin_rot_w + cos_rot_h;

					// Right-bottom
					// Positions
					posPtr[ this.ptrOffset++ ] = offsets[ 3 ].x + bb.Position.x;
					posPtr[ this.ptrOffset++ ] = offsets[ 3 ].y + bb.Position.y;
					posPtr[ this.ptrOffset++ ] = offsets[ 3 ].z + bb.Position.z;
					// Color
					colPtr[ this.ptrOffset++ ] = color;
					// Texture coords
					texPtr[ this.ptrOffset++ ] = mid_u + cos_rot_w - sin_rot_h;
					texPtr[ this.ptrOffset++ ] = mid_v + sin_rot_w + cos_rot_h;
				}
			}
		}

		/// <summary>
		///		Generates vertex offsets.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="top"></param>
		/// <param name="bottom"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="destVec"></param>
		/// <remarks>
		///		Takes in parametric offsets as generated from GetParametericOffsets, width and height values
		///		and billboard x and y axes as generated from GenerateBillboardAxes.
		///		Fills output array of 4 vectors with vector offsets
		///		from origin for left-top, right-top, left-bottom, right-bottom corners.
		/// </remarks>
		protected void GenerateVertexOffsets( float left,
											  float right,
											  float top,
											  float bottom,
											  float width,
											  float height,
											  ref Vector3 x,
											  ref Vector3 y,
											  Vector3[] destVec )
		{
			Vector3 vLeftOff, vRightOff, vTopOff, vBottomOff;
			/* Calculate default offsets. Scale the axes by
			   parametric offset and dimensions, ready to be added to
			   positions.
			*/
			vLeftOff = x * ( left * width );
			vRightOff = x * ( right * width );
			vTopOff = y * ( top * height );
			vBottomOff = y * ( bottom * height );

			// Make final offsets to vertex positions
			destVec[ 0 ] = vLeftOff + vTopOff;
			destVec[ 1 ] = vRightOff + vTopOff;
			destVec[ 2 ] = vLeftOff + vBottomOff;
			destVec[ 3 ] = vRightOff + vBottomOff;
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Billboard CreateBillboard( Vector3 position )
		{
			return this.CreateBillboard( position, ColorEx.White );
		}

		/// <summary>
		///		Creates a new billboard and adds it to this set.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="color"></param>
		/// <returns></returns>
		public Billboard CreateBillboard( Vector3 position, ColorEx color )
		{
			// see if we need to auto extend the free billboard pool
			if ( this.freeBillboards.Count == 0 )
			{
				if ( this.autoExtendPool )
				{
					this.PoolSize = this.PoolSize * 2;
				}
				else
				{
					throw new AxiomException( "Could not create a billboard with AutoSize disabled and an empty pool." );
				}
			}

			// get the next free billboard from the queue
			Billboard newBillboard = this.freeBillboards[ 0 ];
			this.freeBillboards.RemoveAt( 0 );

			// add the billboard to the active list
			this.activeBillboards.Add( newBillboard );

			// initialize the billboard
			newBillboard.Position = position;
			newBillboard.Color = color;
			newBillboard.Direction = Vector3.Zero;
			newBillboard.Rotation = 0;
			// newBillboard.TexCoordIndex = 0;
			newBillboard.ResetDimensions();
			newBillboard.NotifyOwner( this );

			// Merge into bounds
			float adjust = Utility.Max( this.defaultParticleWidth, this.defaultParticleHeight );
			Vector3 adjustVec = new Vector3( adjust, adjust, adjust );
			Vector3 newMin = position - adjustVec;
			Vector3 newMax = position + adjustVec;

			this.aab.Merge( new AxisAlignedBox( newMin, newMax ) );

			float sqlen = (float)Utility.Max( newMin.LengthSquared, newMax.LengthSquared );
			this.boundingRadius = (float)Utility.Max( this.boundingRadius, Utility.Sqrt( sqlen ) );

			return newBillboard;
		}

		/// <summary>
		///     Allocate / reallocate vertex data
		///     Note that we allocate enough space for ALL the billboards in the pool, but only issue
		///     rendering operations for the sections relating to the active billboards
		/// </summary>
		private void CreateBuffers()
		{
			/* Alloc positions   ( 1 or 4 verts per billboard, 3 components )
					 colours     ( 1 x RGBA per vertex )
					 indices     ( 6 per billboard ( 2 tris ) if not point rendering )
					 tex. coords ( 2D coords, 1 or 4 per billboard )
			*/

			//             LogManager.Instance.Write(string.Format("BillBoardSet.CreateBuffers entered; vertexData {0}, indexData {1}, mainBuffer {2}",
			//                     vertexData == null ? "null" : vertexData.ToString(),
			//                     indexData == null ? "null" : indexData.ToString(),
			//                     mainBuffer == null ? "null" : mainBuffer.ToString()));

			// Warn if user requested an invalid setup
			// Do it here so it only appears once
			if ( this.pointRendering && this.billboardType != BillboardType.Point )
			{
				LogManager.Instance.Write(
						"Warning: BillboardSet {0} has point rendering enabled but is using a type " +
						"other than BillboardType.Point, this may not give you the results you " +
						"expect.",
						this.name );
			}

			this.vertexData = new VertexData();
			if ( this.pointRendering )
			{
				this.vertexData.vertexCount = this.poolSize;
			}
			else
			{
				this.vertexData.vertexCount = this.poolSize * 4;
			}

			this.vertexData.vertexStart = 0;

			// Vertex declaration
			VertexDeclaration decl = this.vertexData.vertexDeclaration;
			VertexBufferBinding binding = this.vertexData.vertexBufferBinding;

			int offset = 0;
			decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Position );
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
			decl.AddElement( 0, offset, VertexElementType.Color, VertexElementSemantic.Diffuse );
			offset += VertexElement.GetTypeSize( VertexElementType.Color );
			// Texture coords irrelevant when enabled point rendering (generated
			// in point sprite mode, and unused in standard point mode)
			if ( !this.pointRendering )
			{
				decl.AddElement( 0, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
			}

			this.mainBuffer =
					HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( 0 ), this.vertexData.vertexCount, BufferUsage.DynamicWriteOnlyDiscardable );

			// bind position and diffuses
			binding.SetBinding( 0, this.mainBuffer );

			if ( !this.pointRendering )
			{
				this.indexData = new IndexData();

				// calc index buffer size
				this.indexData.indexStart = 0;
				this.indexData.indexCount = this.poolSize * 6;

				// create the index buffer
				this.indexData.indexBuffer =
						HardwareBufferManager.Instance.CreateIndexBuffer(
								IndexType.Size16,
								this.indexData.indexCount,
								BufferUsage.StaticWriteOnly );

				/* Create indexes (will be the same every frame)
				   Using indexes because it means 1/3 less vertex transforms (4 instead of 6)

				   Billboard layout relative to camera:

					0-----1
					|    /|
					|  /  |
					|/    |
					2-----3
				*/

				// lock the index buffer
				IntPtr idxPtr = this.indexData.indexBuffer.Lock( BufferLocking.Discard );

				unsafe
				{
					ushort* pIdx = (ushort*)idxPtr.ToPointer();

					for ( int idx, idxOffset, bboard = 0; bboard < this.poolSize; ++bboard )
					{
						// Do indexes
						idx = bboard * 6;
						idxOffset = bboard * 4;

						pIdx[ idx ] = (ushort)idxOffset; // + 0;, for clarity
						pIdx[ idx + 1 ] = (ushort)( idxOffset + 2 );
						pIdx[ idx + 2 ] = (ushort)( idxOffset + 1 );
						pIdx[ idx + 3 ] = (ushort)( idxOffset + 1 );
						pIdx[ idx + 4 ] = (ushort)( idxOffset + 2 );
						pIdx[ idx + 5 ] = (ushort)( idxOffset + 3 );
					} // for
				} // unsafe

				// unlock the buffers
				this.indexData.indexBuffer.Unlock();
			}
			this.buffersCreated = true;
		}

		private void DestroyBuffers()
		{
			//             LogManager.Instance.Write(string.Format("BillBoardSet.DestroyBuffers entered; vertexData {0}, indexData {1}, mainBuffer {2}",
			//                     vertexData == null ? "null" : vertexData.ToString(),
			//                     indexData == null ? "null" : indexData.ToString(),
			//                     mainBuffer == null ? "null" : mainBuffer.ToString()));
			this.vertexData = null;
			this.indexData = null;
			this.mainBuffer = null;
			this.buffersCreated = false;
		}

		// Warn if user requested an invalid setup
		// Do it here so it only appears once

		/// <summary>
		///		Empties all of the active billboards from this set.
		/// </summary>
		public void Clear()
		{
			// Move actives to the free list
			this.freeBillboards.AddRange( this.activeBillboards );
			this.activeBillboards.Clear();
		}

		protected Billboard GetBillboard( int index )
		{
			return this.activeBillboards[ index ];
		}

		protected void RemoveBillboard( int index )
		{
			Billboard tmp = this.activeBillboards[ index ];
			this.activeBillboards.RemoveAt( index );
			this.freeBillboards.Add( tmp );
		}

		protected void RemoveBillboard( Billboard bill )
		{
			int index = this.activeBillboards.IndexOf( bill );
			Debug.Assert( index >= 0, "Billboard is not in the active list" );
			RemoveBillboard( index );
		}

		/// <summary>
		///		Update the bounds of the BillboardSet.
		/// </summary>
		public virtual void UpdateBounds()
		{
			if ( this.activeBillboards.Count == 0 )
			{
				// no billboards, so the bounding box is null
				this.aab.IsNull = true;
				this.boundingRadius = 0.0f;
			}
			else
			{
				float maxSqLen = -1.0f;
				Vector3 min = new Vector3( float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity );
				Vector3 max = new Vector3( float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity );

				foreach ( Billboard billboard in this.activeBillboards )
				{
					Vector3 pos = billboard.Position;
					min.Floor( pos );
					max.Ceil( pos );

					maxSqLen = Utility.Max( maxSqLen, pos.LengthSquared );
				}

				// adjust for billboard size
				float adjust = Utility.Max( this.defaultParticleWidth, this.defaultParticleHeight );
				Vector3 vecAdjust = new Vector3( adjust, adjust, adjust );
				min -= vecAdjust;
				max += vecAdjust;

				// update our local aabb
				this.aab.SetExtents( min, max );

				this.boundingRadius = Utility.Sqrt( maxSqLen );
			}
			// if we have a parent node, ask it to update us
			if ( this.parentNode != null )
			{
				this.parentNode.NeedUpdate();
			}
		}

		#endregion Methods

		#region Properties

		/// <summary>
		///		Tells the set whether to allow automatic extension of the pool of billboards.
		///	 </summary>
		///	 <remarks>
		///		A BillboardSet stores a pool of pre-constructed billboards which are used as needed when
		///		a new billboard is requested. This allows applications to create / remove billboards efficiently
		///		without incurring construction / destruction costs (a must for sets with lots of billboards like
		///		particle effects). This method allows you to configure the behaviour when a new billboard is requested
		///		but the billboard pool has been exhausted.
		///		<p/>
		///		The default behaviour is to allow the pool to extend (typically this allocates double the current
		///		pool of billboards when the pool is expended), equivalent to calling this property to
		///		true. If you set the property to false however, any attempt to create a new billboard
		///		when the pool has expired will simply fail silently, returning a null pointer.
		/// </remarks>
		public bool AutoExtend
		{
			get
			{
				return this.autoExtendPool;
			}
			set
			{
				this.autoExtendPool = value;
			}
		}

		/// <summary>
		///		Adjusts the size of the pool of billboards available in this set.
		///	 </summary>
		///	 <remarks>
		///		See the BillboardSet.AutoExtend property for full details of the billboard pool. This method adjusts
		///		the preallocated size of the pool. If you try to reduce the size of the pool, the set has the option
		///		of ignoring you if too many billboards are already in use. Bear in mind that calling this method will
		///		incur significant construction / destruction calls so should be avoided in time-critical code. The same
		///		goes for auto-extension, try to avoid it by estimating the pool size correctly up-front.
		/// </remarks>
		public int PoolSize
		{
			get
			{
				return this.billboardPool.Count;
			}
			set
			{
				// If we're driving this from our own data, allocate billboards
				if ( !this.externalData )
				{
					int size = value;
					// Never shrink below Count
					int currentSize = this.billboardPool.Count;
					if ( currentSize >= size )
					{
						return;
					}

					this.IncreasePool( size );

					// add new items to the queue
					for ( int i = currentSize; i < size; ++i )
					{
						this.freeBillboards.Add( this.billboardPool[ i ] );
					}
				}
				this.poolSize = value;
				this.DestroyBuffers();
			}
		}

#if OLD
		// 4 vertices per billboard, 3 components = 12
		// 1 int value per vertex
		// 2 tris, 6 per billboard
		// 2d coords, 4 per billboard = 8

					vertexData = new VertexData();
					indexData = new IndexData();

					vertexData.vertexCount = size * 4;
					vertexData.vertexStart = 0;

					// get references to the declaration and buffer binding
					VertexDeclaration decl = vertexData.vertexDeclaration;
					VertexBufferBinding binding = vertexData.vertexBufferBinding;

					// create the 3 vertex elements we need
					int offset = 0;
					decl.AddElement(POSITION, offset, VertexElementType.Float3, VertexElementSemantic.Position);
					decl.AddElement(COLOR, offset, VertexElementType.Color, VertexElementSemantic.Diffuse);
					decl.AddElement(TEXCOORD, 0, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0);

					// create position buffer
					HardwareVertexBuffer vBuffer =
						HardwareBufferManager.Instance.CreateVertexBuffer(
						decl.GetVertexSize(POSITION),
						vertexData.vertexCount,
						BufferUsage.StaticWriteOnly);

					binding.SetBinding(POSITION, vBuffer);

					// create color buffer
					vBuffer =
						HardwareBufferManager.Instance.CreateVertexBuffer(
						decl.GetVertexSize(COLOR),
						vertexData.vertexCount,
						BufferUsage.StaticWriteOnly);

					binding.SetBinding(COLOR, vBuffer);

					// create texcoord buffer
					vBuffer =
						HardwareBufferManager.Instance.CreateVertexBuffer(
						decl.GetVertexSize(TEXCOORD),
						vertexData.vertexCount,
						BufferUsage.StaticWriteOnly);

					binding.SetBinding(TEXCOORD, vBuffer);

					// calc index buffer size
					indexData.indexStart = 0;
					indexData.indexCount = size * 6;

					// create the index buffer
					indexData.indexBuffer =
						HardwareBufferManager.Instance.CreateIndexBuffer(
						IndexType.Size16,
						indexData.indexCount,
						BufferUsage.StaticWriteOnly);

					/* Create indexes and tex coords (will be the same every frame)
					   Using indexes because it means 1/3 less vertex transforms (4 instead of 6)

					   Billboard layout relative to camera:

						2-----3
						|    /|
						|  /  |
						|/    |
						0-----1
					*/

					float[] texData = new float[] {
						 0.0f, 1.0f,
						 1.0f, 1.0f,
						 0.0f, 0.0f,
						 1.0f, 0.0f };

					// lock the index buffer
					IntPtr idxPtr = indexData.indexBuffer.Lock(BufferLocking.Discard);

					// get the texcoord buffer
					vBuffer = vertexData.vertexBufferBinding.GetBuffer(TEXCOORD);

					// lock the texcoord buffer
					IntPtr texPtr = vBuffer.Lock(BufferLocking.Discard);

					unsafe {
						ushort* pIdx = (ushort*)idxPtr.ToPointer();
						float* pTex = (float*)texPtr.ToPointer();

						for(int idx, idxOffset, texOffset, bboard = 0; bboard < size; bboard++) {
							// compute indexes
							idx = bboard * 6;
							idxOffset = bboard * 4;
							texOffset = bboard * 8;

							pIdx[idx]   =	(ushort)idxOffset; // + 0;, for clarity
							pIdx[idx + 1] = (ushort)(idxOffset + 1);
							pIdx[idx + 2] = (ushort)(idxOffset + 3);
							pIdx[idx + 3] = (ushort)(idxOffset + 0);
							pIdx[idx + 4] = (ushort)(idxOffset + 3);
							pIdx[idx + 5] = (ushort)(idxOffset + 2);

							// Do tex coords
							pTex[texOffset]   = texData[0];
							pTex[texOffset+1] = texData[1];
							pTex[texOffset+2] = texData[2];
							pTex[texOffset+3] = texData[3];
							pTex[texOffset+4] = texData[4];
							pTex[texOffset+5] = texData[5];
							pTex[texOffset+6] = texData[6];
							pTex[texOffset+7] = texData[7];
						} // for
					} // unsafe

					// unlock the buffers
					indexData.indexBuffer.Unlock();
					vBuffer.Unlock();
				} // if
			} // set
		}
#endif

		/// <summary>
		///		Gets/Sets the point which acts as the origin point for all billboards in this set.
		///	 </summary>
		///	 <remarks>
		///		This setting controls the fine tuning of where a billboard appears in relation to it's
		///		position. It could be that a billboard's position represents it's center (e.g. for fireballs),
		///		it could mean the center of the bottom edge (e.g. a tree which is positioned on the ground),
		/// </remarks>
		public BillboardOrigin BillboardOrigin
		{
			get
			{
				return this.originType;
			}
			set
			{
				this.originType = value;
			}
		}

		/// <summary>
		///		Gets/Sets the name of the material to use for this billboard set.
		/// </summary>
		public string MaterialName
		{
			get
			{
				return this.materialName;
			}
			set
			{
				this.materialName = value;

				// find the requested material
				this.material = (Material)MaterialManager.Instance[ this.materialName ];

				if ( this.material != null )
				{
					// make sure it is loaded
					this.material.Load();
				}
				else
				{
					throw new AxiomException(
							"Material '{0}' could not be found to be set as the material for BillboardSet '{0}'.",
							this.materialName,
							this.name );
				}
			}
		}

		/// <summary>
		///		Sets whether culling tests billboards in this individually as well as in a group.
		/// </summary>
		///	 <remarks>
		///		Billboard sets are always culled as a whole group, based on a bounding box which
		///		encloses all billboards in the set. For fairly localised sets, this is enough. However, you
		///		can optionally tell the set to also cull individual billboards in the set, i.e. to test
		///		each individual billboard before rendering. The default is not to do this.
		///		<p/>
		///		This is useful when you have a large, fairly distributed set of billboards, like maybe
		///		trees on a landscape. You probably still want to group them into more than one
		///		set (maybe one set per section of landscape), which will be culled coarsely, but you also
		///		want to cull the billboards individually because they are spread out. Whilst you could have
		///		lots of single-tree sets which are culled separately, this would be inefficient to render
		///		because each tree would be issued as it's own rendering operation.
		///		<p/>
		///		By setting this property to true, you can have large billboard sets which
		///		are spaced out and so get the benefit of batch rendering and coarse culling, but also have
		///		fine-grained culling so unnecessary rendering is avoided.
		/// </remarks>
		public bool CullIndividual
		{
			get
			{
				return this.cullIndividual;
			}
			set
			{
				this.cullIndividual = value;
			}
		}

		/// <summary>
		///		Gets/Sets the type of billboard to render.
		/// </summary>
		///	 <remarks>
		///		The default sort of billboard (Point), always has both x and y axes parallel to
		///		the camera's local axes. This is fine for 'point' style billboards (e.g. flares,
		///		smoke, anything which is symmetrical about a central point) but does not look good for
		///		billboards which have an orientation (e.g. an elongated raindrop). In this case, the
		///		oriented billboards are more suitable (OrientedCommon or OrientedSelf) since they retain an independant Y axis
		///		and only the X axis is generated, perpendicular to both the local Y and the camera Z.
		/// </remarks>
		public BillboardType BillboardType
		{
			get
			{
				return this.billboardType;
			}
			set
			{
				this.billboardType = value;
			}
		}

		public BillboardRotationType BillboardRotationType
		{
			get
			{
				return this.rotationType;
			}
			set
			{
				this.rotationType = value;
			}
		}

		/// <summary>
		///		Use this to specify the common direction given to billboards of types OrientedCommon or PerpendicularCommon.
		/// </summary>
		///	 <remarks>
		///		Use OrientedCommon when you want oriented billboards but you know they are always going to
		///		be oriented the same way (e.g. rain in calm weather). It is faster for the system to calculate
		///		the billboard vertices if they have a common direction.
		/// </remarks>
		public Vector3 CommonDirection
		{
			get
			{
				return this.commonDirection;
			}
			set
			{
				this.commonDirection = value;
			}
		}

		/// <summary>
		///		Use this to determine the orientation given to billboards of types PerpendicularCommon or PerpendicularSelf.
		/// </summary>
		///	 <remarks>
		///		Billboards will be oriented with their Y axis coplanar with the up direction vector.
		/// </remarks>
		public Vector3 CommonUpVector
		{
			get
			{
				return this.commonUpVector;
			}
			set
			{
				this.commonUpVector = value;
			}
		}

		public bool UseAccurateFacing
		{
			get
			{
				return this.accurateFacing;
			}
			set
			{
				this.accurateFacing = value;
			}
		}

		/// <summary>
		///		Gets the list of active billboards.
		/// </summary>
		public List<Billboard> Billboards
		{
			get
			{
				return this.activeBillboards;
			}
		}

		/// <summary>
		///    Local bounding radius of this billboard set.
		/// </summary>
		public override float BoundingRadius
		{
			get
			{
				return this.boundingRadius;
			}
		}

		#endregion Properties

		#region IRenderable Members

		public bool CastsShadows
		{
			get
			{
				return false;
			}
		}

		public Material Material
		{
			get
			{
				return this.material;
			}
		}

		public Technique Technique
		{
			get
			{
				return this.material.GetBestTechnique();
			}
		}

		protected RenderOperation renderOperation = new RenderOperation();
		public RenderOperation RenderOperation
		{
			get
			{
				renderOperation.vertexData = this.vertexData;
				renderOperation.vertexData.vertexStart = 0;

				if ( this.pointRendering )
				{
					renderOperation.operationType = OperationType.PointList;
					renderOperation.useIndices = false;
					renderOperation.indexData = null;
					renderOperation.vertexData.vertexCount = this.numVisibleBillboards;
				}
				else
				{
					renderOperation.operationType = OperationType.TriangleList;
					renderOperation.useIndices = true;
					renderOperation.vertexData.vertexCount = this.numVisibleBillboards * 4;
					renderOperation.indexData = this.indexData;
					renderOperation.indexData.indexCount = this.numVisibleBillboards * 6;
					renderOperation.indexData.indexStart = 0;
				}
				return renderOperation;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="matrices"></param>
		public virtual void GetWorldTransforms( Matrix4[] matrices )
		{
			// It's actually more natural to be in local space, which means
			// that the emitted particles move when the parent object moves.
			// Sometimes you only want the emitter to move though, such as
			// when you are generating smoke
			if ( this.worldSpace )
			{
				matrices[ 0 ] = Matrix4.Identity;
			}
			else
			{
				matrices[ 0 ] = this.parentNode.FullTransform;
			}
		}

		/// <summary>
		///
		/// </summary>
		public virtual ushort NumWorldTransforms
		{
			get
			{
				return 1;
			}
		}

		///
		/// </summary>
		public bool UseIdentityProjection
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool UseIdentityView
		{
			get
			{
				return false;
			}
		}

		public virtual bool PolygonModeOverrideable
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public virtual float GetSquaredViewDepth( Camera camera )
		{
			Debug.Assert( this.parentNode != null,
						  "BillboardSet must have a parent scene node to get the squared view depth." );

			return this.parentNode.GetSquaredViewDepth( camera );
		}

		/// <summary>
		///
		/// </summary>
		public Quaternion WorldOrientation
		{
			get
			{
				return this.parentNode.DerivedOrientation;
			}
		}

		/// <summary>
		///
		/// </summary>
		public Vector3 WorldPosition
		{
			get
			{
				return this.parentNode.DerivedPosition;
			}
		}

		public LightList Lights
		{
			get
			{
				return this.QueryLights();
			}
		}

		public Vector4 GetCustomParameter( int index )
		{
			if ( this.customParams[ index ] == null )
			{
				throw new Exception( "A parameter was not found at the given index" );
			}
			else
			{
				return (Vector4)this.customParams[ index ];
			}
		}

		public void SetCustomParameter( int index, Vector4 val )
		{
			while ( customParams.Count <= index )
				customParams.Add( Vector4.Zero );
			this.customParams[ index ] = val;
		}

		public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry,
											  GpuProgramParameters gpuParams )
		{
			if ( this.customParams[ entry.Data ] != null )
			{
				gpuParams.SetConstant( entry.PhysicalIndex, (Vector4)this.customParams[ entry.Data ] );
			}
		}

		#endregion IRenderable Members

		#region Implementation of MovableObject

		public override AxisAlignedBox BoundingBox
		{
			// cloning to prevent direct modification
			get
			{
				return (AxisAlignedBox)this.aab.Clone();
			}
		}

		public bool NormalizeNormals
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///		Generate the vertices for all the billboards relative to the camera
		/// </summary>
		/// <param name="camera"></param>
		public override void NotifyCurrentCamera( Camera camera )
		{
			// base.NotifyCurrentCamera(camera);
			this.currentCamera = camera;
			this.camQ = camera.DerivedOrientation;
			this.camPos = camera.DerivedPosition;
			if ( !this.worldSpace )
			{
				// Default behaviour is that billboards are in local node space
				// so orientation of camera (in world space) must be reverse-transformed
				// into node space
				this.camQ = this.parentNode.DerivedOrientation.UnitInverse * this.camQ;
				this.camPos = this.parentNode.DerivedOrientation.UnitInverse *
							  ( this.camPos - this.parentNode.DerivedPosition ) / this.parentNode.DerivedScale;
			}
			// Camera direction points down -Z
			this.camDir = this.camQ * Vector3.NegativeUnitZ;
		}

		/// <summary>
		///		Sets the default dimensions of the billboards in this set.
		/// </summary>
		///	 <remarks>
		///		All billboards in a set are created with these default dimensions. The set will render most efficiently if
		///		all the billboards in the set are the default size. It is possible to alter the size of individual
		///		billboards at the expense of extra calculation. See the Billboard class for more info.
		/// </remarks>
		public void SetDefaultDimensions( float width, float height )
		{
			this.defaultParticleWidth = width;
			this.defaultParticleHeight = height;
		}

		public void SetBillboardsInWorldSpace( bool worldSpace )
		{
			this.worldSpace = worldSpace;
		}

		public override void UpdateRenderQueue( RenderQueue queue )
		{
			if ( !this.externalData )
			{
				// TODO: Implement sorting of billboards
				//if (sortingEnabled)
				//    SortBillboards(currentCamera);

				this.BeginBillboards();
				foreach ( Billboard billboard in this.activeBillboards )
				{
					this.InjectBillboard( billboard );
				}
				this.EndBillboards();
			}
			// TODO: Ogre checks mRenderQueueIDSet
			// add ourself to the render queue
			queue.AddRenderable( this, RenderQueue.DEFAULT_PRIORITY, this.renderQueueID );
		}

		public bool PointRenderingEnabled
		{
			get
			{
				return this.pointRendering;
			}
			set
			{
				bool enabled = value;
				// Override point rendering if not supported
				if ( enabled
					 && !Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.PointSprites ) )
				{
					enabled = false;
				}
				if ( enabled != this.pointRendering )
				{
					this.pointRendering = true;
					// Different buffer structure (1 or 4 verts per billboard)
					this.DestroyBuffers();
				}
			}
		}

		/// <summary>
		/// Get the 'type flags' for this <see cref="BillboardSet"/>.
		/// </summary>
		/// <seealso cref="MovableObject.TypeFlags"/>
		public override uint TypeFlags
		{
			get
			{
				return (uint)SceneQueryTypeMask.Fx;
			}
		}

		#endregion Implementation of MovableObject
	}

	#region MovableObjectFactory implementation

	public class BillboardSetFactory : MovableObjectFactory
	{
		public new const string TypeName = "BillboardSet";

		public BillboardSetFactory()
			: base()
		{
			base.Type = BillboardSetFactory.TypeName;
			base.TypeFlag = (uint)SceneQueryTypeMask.Fx;
		}

		protected override MovableObject _createInstance( string name, NamedParameterList param )
		{
			// may have parameters
			bool externalData = false;
			int poolSize = 0;

			if ( param != null )
			{
				object ni;
				if ( param.ContainsKey( "poolSize" ) )
				{
					poolSize = Convert.ToInt32( param[ "poolSize" ] );
				}

				if ( param.ContainsKey( "externalData" ) )
				{
					externalData = Convert.ToBoolean( param[ "externalData" ] );
				}
			}

			BillboardSet bSet;

			if ( poolSize > 0 )
			{
				bSet = new BillboardSet( name, poolSize, externalData );
			}
			else
			{
				bSet = new BillboardSet( name, 0 );
			}

			bSet.MovableType = TypeName;

			return bSet;
		}
	}

	#endregion MovableObjectFactory implementation
}