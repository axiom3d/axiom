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
using System.Diagnostics;
using System.Drawing;
using Axiom.Collections;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

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
	public class BillboardSet : SceneObject, IRenderable 
	{
		#region Member variables

		/// <summary>Bounds of all billboards in this set</summary>
		protected AxisAlignedBox aab = new AxisAlignedBox();
		/// <summary>Origin of each billboard</summary>
		protected BillboardOrigin originType = BillboardOrigin.Center;
		/// <summary>Default width/height of each billboard.</summary>
		protected Size defaultDimensions = new Size(100, 100);
		/// <summary>Name of the material to use</summary>
		protected String materialName = "BaseWhite";
		/// <summary>Reference to the material to use</summary>
		protected Material material;
		/// <summary></summary>
		protected bool allDefaultSize;
		/// <summary></summary>
		protected bool autoExtendPool = true;

		// various collections for pooling billboards
		protected ArrayList activeBillboards = new ArrayList();
		protected Queue freeBillboards = new Queue();
		protected ArrayList billboardPool = new ArrayList();

		// Geometry data.
		protected VertexData vertexData = new VertexData();
		protected IndexData indexData = new IndexData();

		/// <summary>Indicates whether or not each billboard should be culled individually.</summary>
		protected bool cullIndividual;
		/// <summary>Type of billboard to render.</summary>
		protected BillboardType billboardType = BillboardType.Point;
		/// <summary>Common direction for billboard oriented with type Common.</summary>
		protected Vector3 commonDirection;
		
		protected int numVisibleBillboards;

		// used to keep track of current index in GenerateVertices
		static protected int posIndex = 0;
		static protected int colorIndex = 0;

		const int POSITION = 0;
		const int COLOR = 1;
		const int TEXCOORD = 2;

		#endregion

		#region Constructors

		/// <summary>
		///		
		/// </summary>
		protected BillboardSet()
		{
		}

		/// <summary>
		///		Public constructor.  Should not be created manually, must be created using a SceneManager.
		/// </summary>
		public BillboardSet( String name, int poolSize)
		{
			this.name = name;
			this.PoolSize = poolSize;
		}

		#endregion

		#region Methods

		/// <summary>
		///		Callback used by Billboards to notify their parent that they have been resized.
		/// </summary>
		public void NotifyBillboardResized()
		{
			allDefaultSize = false;
		}

		/// <summary>
		///		Internal method for increasing pool size.
		/// </summary>
		/// <param name="size"></param>
		virtual protected void IncreasePool(int size)
		{
			int oldSize = billboardPool.Count;

			// expand the capacity a bit
			billboardPool.Capacity += size;

			// add fresh Billboard objects to the new slots
			for(int i = oldSize; i < size; i++)
				billboardPool.Add(new Billboard());
		}

		/// <summary>
		///		Determines whether the supplied billboard is visible in the camera or not.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="billboard"></param>
		/// <returns></returns>
		protected bool IsBillboardVisible(Camera camera, Billboard billboard)
		{
			// if not culling each one, return true always
			if(!cullIndividual)
				return true;

			Sphere sphere = new Sphere();

			// HACK: Fix this
			Matrix4 world = this.WorldTransforms[0];

			// get the center of the bounding sphere
			sphere.Center = world * billboard.Position;

			// calculate the radius of the bounding sphere for the billboard
			if(billboard.HasOwnDimensions)
			{
				sphere.Radius = MathUtil.Max(billboard.Dimensions.Width, billboard.Dimensions.Height);
			}
			else
			{
				sphere.Radius = MathUtil.Max(defaultDimensions.Width, defaultDimensions.Height);
			}

			// finally, see if the sphere is visible in the camera
			return camera.IsObjectVisible(sphere);
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		virtual protected void GenerateBillboardAxes(Camera camera, ref Vector3 x, ref Vector3 y)
		{
			GenerateBillboardAxes(camera, ref x, ref y, null);
		}

		/// <summary>
		///		Generates billboard corners.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="billboard"></param>
		/// <remarks>Billboard param only required for type OrientedSelf</remarks>
		virtual protected void GenerateBillboardAxes(Camera camera, ref Vector3 x, ref Vector3 y, Billboard billboard)
		{
			// Default behavior is that billboards are in local node space
			// so orientation of camera (in world space) must be reverse-transformed 
			// into node space to generate the axes
			Quaternion invTransform = parentNode.DerivedOrientation.Inverse();
			Quaternion camQ = Quaternion.Zero;

			switch (billboardType)
			{
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
					// Convert into billboard local space
					camQ = invTransform * camQ;
					x = camQ * camera.DerivedDirection.Cross(y);
            
					break;
				case BillboardType.OrientedSelf:
					// Y-axis is direction
					// X-axis is cross with camera direction 
					y = billboard.Direction;
					// Convert into billboard local space
					camQ = invTransform * camQ;
					x = camQ * camera.DerivedDirection.Cross(y);

					break;
			}
		}

		/// <summary>
		///		Generate parametric offsets based on the origin.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="top"></param>
		/// <param name="bottom"></param>
		protected void GetParametericOffsets(out float left, out float right, out float top, out float bottom)
		{
			// ok, so the compiler doesn't trust me that the switch will set the value of the out params
			// before the method returns.  two words: FUCK YOU
			left = 0.0f;
			right = 0.0f;
			top = 0.0f;
			bottom = 0.0f;

			switch(originType)
			{
				case BillboardOrigin.TopLeft:
					left = 0.0f;
					right = 1.0f;
					top = 0.0f;
					bottom = 1.0f;
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
					bottom = 1.0f;
					break;

				case BillboardOrigin.CenterLeft:
					left = 0.0f;
					right = 1.0f;
					top = -0.5f;
					bottom = 0.5f;
					break;

				case BillboardOrigin.Center:
					left = -0.5f;
					right = 0.5f;
					top = -0.5f;
					bottom = 0.5f;
					break;

				case BillboardOrigin.CenterRight:
					left = -1.0f;
					right = 0.0f;
					top = -0.5f;
					bottom = 0.5f;
					break;

				case BillboardOrigin.BottomLeft:
					left = 0.0f;
					right = 1.0f;
					top = -1.0f;
					bottom = 0.0f;
					break;

				case BillboardOrigin.BottomCenter:
					left = -0.5f;
					right = 0.5f;
					top = -1.0f;
					bottom = 0.0f;
					break;

				case BillboardOrigin.BottomRight:
					left = -1.0f;
					right = 0.0f;
					top = -1.0f;
					bottom = 0.0f;
					break;
			}
		}

		/// <summary>
		///		Generates vertex data for a billboard.
		/// </summary>
		/// <param name="position">Vertex positions.</param>
		/// <param name="colors">Vertex colors</param>
		/// <param name="offsets">Array of 4 Vector3 offsets.</param>
		/// <param name="billboard">A billboard.</param>
		protected void GenerateVertices(IntPtr posPtr, IntPtr colPtr, Vector3[] offsets, Billboard billboard)
		{
			unsafe
			{
				float* positions = (float*)posPtr.ToPointer();
				int* colors = (int*)colPtr.ToPointer();

				// Left-top
				positions[posIndex++] = offsets[0].x + billboard.Position.x;
				positions[posIndex++] = offsets[0].y + billboard.Position.y;
				positions[posIndex++] = offsets[0].z + billboard.Position.z;
				// Right-top
				positions[posIndex++] = offsets[1].x + billboard.Position.x;
				positions[posIndex++] = offsets[1].y + billboard.Position.y;
				positions[posIndex++] = offsets[1].z + billboard.Position.z;
				// Left-bottom
				positions[posIndex++] = offsets[2].x + billboard.Position.x;
				positions[posIndex++] = offsets[2].y + billboard.Position.y;
				positions[posIndex++] = offsets[2].z + billboard.Position.z;
				// Right-bottom
				positions[posIndex++] = offsets[3].x + billboard.Position.x;
				positions[posIndex++] = offsets[3].y + billboard.Position.y;
				positions[posIndex++] = offsets[3].z + billboard.Position.z;
				
				// Update colors
				int colorVal = Engine.Instance.ConvertColor(billboard.Color);

				colors[colorIndex++] = colorVal;
				colors[colorIndex++] = colorVal;
				colors[colorIndex++] = colorVal;
				colors[colorIndex++] = colorVal;
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
		protected void GenerateVertexOffsets(float left, float right, float top, float bottom, float width, float height, ref Vector3 x, ref Vector3 y, Vector3[] destVec)
		{
			Vector3 vLeftOff, vRightOff, vTopOff, vBottomOff;
			/* Calculate default offsets. Scale the axes by
			   parametric offset and dimensions, ready to be added to
			   positions.
			*/

			vLeftOff   = x * ( left * width );
			vRightOff  = x * ( right * width );
			vTopOff    = y * ( top * height );
			vBottomOff = y * ( bottom * height );

			// Make final offsets to vertex positions
			destVec[0] = vLeftOff  + vTopOff;
			destVec[1] = vRightOff + vTopOff;
			destVec[2] = vLeftOff  + vBottomOff;
			destVec[3] = vRightOff + vBottomOff;
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Billboard CreateBillboard(Vector3 position)
		{
			return CreateBillboard(position, ColorEx.FromColor(Color.White));
		}

		/// <summary>
		///		Creates a new billboard and adds it to this set.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="color"></param>
		/// <returns></returns>
		public Billboard CreateBillboard(Vector3 position, ColorEx color)
		{
			// see if we need to auto extend the free billboard pool
			if(freeBillboards.Count == 0)
			{
				if(autoExtendPool)
					this.PoolSize = this.PoolSize * 2;
				else
					throw new Axiom.Exceptions.AxiomException("Could not create a billboard with AutoSize disabled and an empty pool.");
			}

			// get the next free billboard from the queue
			Billboard newBillboard = (Billboard)freeBillboards.Dequeue();

			// add the billboard to the active list
			activeBillboards.Add(newBillboard);

			// initialize the billboard
			newBillboard.Position = position;
			newBillboard.Color = color;
			newBillboard.NotifyOwner(this);

			// update the bounding volume of the set
			UpdateBounds();

			return newBillboard;
		}

		/// <summary>
		///		Empties all of the active billboards from this set.
		/// </summary>
		public void Clear()
		{
			// clear the active billboard list
			activeBillboards.Clear();
		}

		/// <summary>
		///		Update the bounds of the BillboardSet.
		/// </summary>
		virtual public void UpdateBounds()
		{
			Vector3 min = new Vector3(Single.PositiveInfinity, Single.PositiveInfinity, Single.PositiveInfinity);
			Vector3 max = new Vector3(Single.NegativeInfinity, Single.NegativeInfinity, Single.NegativeInfinity);

			for(int i = 0; i < activeBillboards.Count; i++)
			{
				Billboard billboard = (Billboard)activeBillboards[i];

				min.Floor(billboard.Position);
				max.Ceil(billboard.Position);
			}

			// adjust for billboard size
			float adjust = MathUtil.Max(defaultDimensions.Width, defaultDimensions.Height);
			Vector3 vecAdjust = new Vector3(adjust, adjust, adjust);
			min -= vecAdjust;
			max += vecAdjust;

			// update our local aabb
			aab.SetExtents(min, max);

			// if we have a parent node, ask it to update us
			if(parentNode != null)
				parentNode.NeedUpdate();
		}

		#endregion

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
			get { return autoExtendPool; }
			set { autoExtendPool = value; }
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
			get { return billboardPool.Count; }
			set 
			{
				int size = value;
				int currentSize = billboardPool.Count;

				if(currentSize < size)
				{
					IncreasePool(size);

					// add new items to the queue
					for(int i = currentSize; i < size; i++)
						freeBillboards.Enqueue(billboardPool[i]);

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
					decl.AddElement(new VertexElement(POSITION, offset, VertexElementType.Float3, VertexElementSemantic.Position));
					decl.AddElement(new VertexElement(COLOR, offset, VertexElementType.Color, VertexElementSemantic.Diffuse));
					decl.AddElement(new VertexElement(TEXCOORD, 0, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0));

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

					float[] texData = new float[] {0.0f, 0.0f, 
																	1.0f, 0.0f, 
																	0.0f, 1.0f, 
																	1.0f, 1.0f};

					// lock the index buffer
					IntPtr idxPtr = indexData.indexBuffer.Lock(0, indexData.indexBuffer.Size, BufferLocking.Discard);

					// get the texcoord buffer
					vBuffer = vertexData.vertexBufferBinding.GetBuffer(TEXCOORD);

					// lock the texcoord buffer
					IntPtr texPtr = vBuffer.Lock(0, vBuffer.Size, BufferLocking.Discard);

					unsafe
					{
						short* pIdx = (short*)idxPtr.ToPointer();
						float* pTex = (float*)texPtr.ToPointer();

						for(short idx, idxOffset, texOffset, bboard = 0; bboard < size; bboard++)
						{
							// compute indexes
							idx = (short)(bboard * 6);
							idxOffset = (short)(bboard * 4);
							texOffset = (short)(bboard * 8);

							pIdx[idx]   = idxOffset; // + 0;, for clarity
							pIdx[idx + 1] = (short)(idxOffset + 1);
							pIdx[idx + 2] = (short)(idxOffset + 3);
							pIdx[idx + 3] = (short)(idxOffset + 0);
							pIdx[idx + 4] = (short)(idxOffset + 3);
							pIdx[idx + 5] = (short)(idxOffset + 2);

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
			get { return originType; }
			set  { originType = value; }
		}

		/// <summary>
		///		Sets the default dimensions of the billboards in this set.
		///	 </summary>
		///	 <remarks>
		///		All billboards in a set are created with these default dimensions. The set will render most efficiently if
		///		all the billboards in the set are the default size. It is possible to alter the size of individual
		///		billboards at the expense of extra calculation. See the Billboard class for more info.
		/// </remarks>
		public Size DefaultDimensions
		{
			get { return defaultDimensions; }
			set { defaultDimensions = value; }
		}

		/// <summary>
		///		Gets/Sets the name of the material to use for this billboard set.
		/// </summary>
		public String MaterialName
		{
			get { return materialName; }
			set
			{
				materialName = value;
				
				// find the requested material
				material = (Material)MaterialManager.Instance[materialName];

				// make sure it is loaded
				material.Load();
			}
		}

		/// <summary>
		///		Sets whether culling tests billboards in this individually as well as in a group.
		///	 </summary>
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
			get { return cullIndividual; }
			set 
			{ 
				this.cullIndividual = value;
			}
		}

		/// <summary>
		///		Gets/Sets the type of billboard to render.
		///	 </summary>
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
			get { return billboardType; }
			set { billboardType = value; }
		}

		/// <summary>
		///		Use this to specify the common direction given to billboards of type OrientedCommon.
		///	 </summary>
		///	 <remarks>
		///		Use OrientedCommon when you want oriented billboards but you know they are always going to 
		///		be oriented the same way (e.g. rain in calm weather). It is faster for the system to calculate
		///		the billboard vertices if they have a common direction.
		/// </remarks>
		public Vector3 CommonDirection
		{
			get { return commonDirection; }
			set { commonDirection = value; }
		}

		/// <summary>
		///		Gets the list of active billboards.
		/// </summary>
		public ArrayList Billboards
		{
			get { return activeBillboards; }
		}

		#endregion

		#region IRenderable Members

		public Material Material
		{
			get { return material; }
		}

		public void GetRenderOperation(RenderOperation op)
		{
			// fill the render operation with our vertex and index data

			// indexed triangle list
			op.operationType = RenderMode.TriangleList;
			op.useIndices = true;

			op.vertexData = vertexData;
			op.vertexData.vertexCount = numVisibleBillboards * 4;
			op.vertexData.vertexStart = 0;

			op.indexData = indexData;
			op.indexData.indexCount = numVisibleBillboards * 6;
			op.indexData.indexStart = 0;
		}		

		virtual public Axiom.MathLib.Matrix4[] WorldTransforms
		{
			get { return new Matrix4[] {parentNode.FullTransform}; }
		}

		virtual public ushort NumWorldTransforms
		{
			get { return 1;	}
		}

		public bool UseIdentityProjection
		{
			get { return false; }
		}

		public bool UseIdentityView
		{
			get { return false; }
		}

		public SceneDetailLevel RenderDetail
		{
			get { return SceneDetailLevel.Solid; }
		}

		virtual public float GetSquaredViewDepth(Camera camera)
		{
			Debug.Assert(parentNode != null, "BillboardSet must have a parent scene node to get the squared view depth.");

			return parentNode.GetSquaredViewDepth(camera);
		}

		#endregion

		#region Implementation of MovableObjects
	
		public override AxisAlignedBox BoundingBox
		{
			// cloning to prevent direct modification
			get { return (AxisAlignedBox)aab.Clone(); }
		}
	
		/// <summary>
		///		Generate the vertices for all the billboards relative to the camera
		/// </summary>
		/// <param name="camera"></param>
		internal override void NotifyCurrentCamera(Camera camera)
		{
			uint j;

			// Take the reverse transform of the camera world axes into billboard space for efficiency

			// parametrics offsets of the origin
			float leftOffset, rightOffset, topOffset, bottomOffset;

			// get offsets for the origin type
			GetParametericOffsets(out leftOffset, out rightOffset, out topOffset, out bottomOffset);

			// Boundary offsets based on origin and camera orientation
			// Final vertex offsets, used where sizes all default to save calcs
			Vector3[] vecOffsets = new Vector3[4];
			Vector3 camX = new Vector3();
			Vector3 camY = new Vector3();

			// generates axes up front if not orient per-billboard
			if(billboardType != BillboardType.OrientedSelf)
			{
				GenerateBillboardAxes(camera, ref camX, ref camY);

				//	if all billboards are the same size we can precalculare the
				// offsets and just use + instead of * for each billboard, which should be faster.
				GenerateVertexOffsets(leftOffset, rightOffset, topOffset, bottomOffset, 
						defaultDimensions.Width, defaultDimensions.Height, ref camX, ref camY, vecOffsets);
			}

			// reset counter
			numVisibleBillboards = 0;			

			// get a reference to the vertex buffers to update
			HardwareVertexBuffer posBuffer = vertexData.vertexBufferBinding.GetBuffer(POSITION);
			HardwareVertexBuffer colBuffer = vertexData.vertexBufferBinding.GetBuffer(COLOR);

			// lock the buffers
			IntPtr posPtr = posBuffer.Lock(0, posBuffer.Size, BufferLocking.Discard);
			IntPtr colPtr = colBuffer.Lock(0, colBuffer.Size, BufferLocking.Discard);

			// reset the static index counters
			posIndex = 0;
			colorIndex = 0;

			// if they are all the same size...
			if(allDefaultSize)
			{
				for(int i = 0; i < activeBillboards.Count; i++)
				{
					Billboard b = (Billboard)activeBillboards[i];
					// skip if not visible dammit
					if(!IsBillboardVisible(camera, b))
						continue;

					if(billboardType == BillboardType.OrientedSelf)
					{
						// generate per billboard
						GenerateBillboardAxes(camera, ref camX, ref camY, b);
						GenerateVertexOffsets(leftOffset, rightOffset, topOffset, bottomOffset, defaultDimensions.Width,
							defaultDimensions.Height, ref camX, ref camY, vecOffsets);
					}

					// generate the billboard vertices
					GenerateVertices(posPtr, colPtr, vecOffsets, b);

					numVisibleBillboards++;
				}
			}
			else
			{
				// billboards aren't all default size
				for(int i = 0; i < activeBillboards.Count; i++)
				{
					Billboard b = (Billboard)activeBillboards[i];
					// skip if not visible dammit
					if(!IsBillboardVisible(camera, b))
						continue;

					if(billboardType == BillboardType.OrientedSelf)
					{
						// generate per billboard
						GenerateBillboardAxes(camera, ref camX, ref camY, b);
					}

					// if it has it's own dimensions. or self oriented, gen offsets
					if(b.HasOwnDimensions || billboardType == BillboardType.OrientedSelf)
					{
						// generate using it's own dimensions
						GenerateVertexOffsets(leftOffset, rightOffset, topOffset, bottomOffset, b.Dimensions.Width,
							b.Dimensions.Height, ref camX, ref camY, vecOffsets);
					}

					// generate the billboard vertices
					GenerateVertices(posPtr, colPtr, vecOffsets, b);

					numVisibleBillboards++;
				}

				// unlock the buffers
				posBuffer.Unlock();
				colBuffer.Unlock();
			}
		}
	
		internal override void UpdateRenderQueue(RenderQueue queue)
		{
			// add ourself to the render queue
			queue.AddRenderable(this, RenderQueue.DEFAULT_PRIORITY, renderQueueID);
		}

		#endregion

	}

}
