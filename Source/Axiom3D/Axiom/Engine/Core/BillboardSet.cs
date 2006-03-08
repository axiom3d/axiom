#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;

using Axiom.MathLib;
// This is coming from RealmForge.Utility
using Axiom.Core;

#endregion Namespace Declarations

#region Versioning Information
/// File								Revision
/// ===============================================
/// OgreBillboardSet.h		            1.30
/// OgreBillboardSet.cpp		        1.59
/// 
#endregion

namespace Axiom
{
    /// <summary>
    ///		Covers what a billboards position means.
    /// </summary>
    public enum BillboardOrigin
    {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        Center,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    /// <summary>
    ///		Type of billboard to use for a BillboardSet.
    /// </summary>
    public enum BillboardType
    {
        /// <summary>Standard point billboard (default), always faces the camera completely and is always upright</summary>
        [ScriptEnum( "point" )]
        Point,
        /// <summary>Billboards are oriented around a shared direction vector (used as Y axis) and only rotate around this to face the camera</summary>
        [ScriptEnum( "oriented_common" )]
        OrientedCommon,
        /// <summary>Billboards are oriented around their own direction vector (their own Y axis) and only rotate around this to face the camera</summary>
        [ScriptEnum( "oriented_self" )]
        OrientedSelf
    }
    
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
    ///     <p/>
    ///     Billboard bounds are only automatically calculated when you create them. If you modify the position of a 
    ///     billboard you may need to call UpdateBounds if the billboard moves outside the original bounds.
    ///     Similarly, the bounds do no shrink when you remove a billboard, if you want them to, call UpdateBounds, 
    ///     but note this requires a potentially expensive examination of every billboard in the set.
    /// </remarks>
    public class BillboardSet : MovableObject, IRenderable
    {
        #region Fields
        /// <summary> internal flag for determining if buffers are initialized. </summary>
        private bool buffersCreated = false;

        /// <summary>Bounds of all billboards in this set</summary>
        protected AxisAlignedBox aab = new AxisAlignedBox();
        /// <summary>Origin of each billboard</summary>
        protected BillboardOrigin originType = BillboardOrigin.Center;
        /// <summary>Default width/height of each billboard.</summary>
        protected float defaultWidth = 100;
        protected float defaultHeight = 100;
        /// <summary>Name of the material to use</summary>
        protected string materialName = "BaseWhite";
        /// <summary>Reference to the material to use</summary>
        protected Material material;
        /// <summary></summary>
        protected bool allDefaultSize;
        /// <summary></summary>
        protected int poolSize;
        /// <summary></summary>
        protected bool autoExtendPool = true;

        // various collections for pooling billboards
        protected BillboardList activeBillboards = new BillboardList();
        protected Queue freeBillboards = new Queue();
        protected BillboardList billboardPool = new BillboardList();

        // Geometry data.
        protected VertexData vertexData = null;
        protected IndexData indexData = null;
        /// Shortcut to main buffer (positions, colours, texture coords)
        protected HardwareVertexBuffer mainBuffer;


        /// <summary>Indicates whether or not each billboard should be culled individually.</summary>
        protected bool cullIndividually;
        /// <summary>Type of billboard to render.</summary>
        protected BillboardType billboardType = BillboardType.Point;
        /// <summary>Common direction for billboard oriented with type Common.</summary>
        protected Vector3 commonDirection;
        /// <summary>The local bounding radius of this object.</summary>
        protected float boundingRadius;

        /// <summary> </summary>
        protected int numVisibleBillboards;

        /// <summary>Are tex coords fixed?  If not they have been modified. </summary>
        protected bool fixedTextureCoords;

        protected bool worldSpace = false;

        protected Matrix4[] world = new Matrix4[1];
        protected Sphere sphere = new Sphere();

        protected Hashtable customParams = new Hashtable( 20 );

        // Are we receiving the data from an eternal source or our own internal billboards?
        private bool externalData = false;
        
        // Base texCoord Data
        private float[] basicTexData = new float[8] { 0.0F, 1.0F,
                                                      1.0F, 1.0F,
                                                      0.0F, 0.0F,
                                                      1.0F, 0.0F };

        // Template texCoord Data
        private float[] texDataBase = new float[8] {  -0.5F, 0.5F,
				      		    				       0.5F, 0.5F,
									        	      -0.5F,-0.5F,
										               0.5F,-0.5F };
        
        // pointers for Hardware buffers
        private IntPtr posPtr = IntPtr.Zero;
        unsafe private float* lockPtr;        
        
        Camera currentCamera;
        // Boundary offsets based on origin and camera orientation
        // Final vertex offsets, used where sizes all default to save calcs
        protected Vector3[] vecOffsets = new Vector3[4];
        // Parametric offsets of origin
        float leftOffset, rightOffset, topOffset, bottomOffset;
        
        // Camera axes in billboard space
        protected Vector3 camX = new Vector3();
        protected Vector3 camY = new Vector3();

        #endregion Fields

        #region Constructors

        /// <summary>
        ///	Usual constructor - this is called by the SceneManager.
        /// </summary>
        internal BillboardSet()
        {
            this.PoolSize = 0;

            // default to fixed
            fixedTextureCoords = true;
        }

        /// <summary>
        ///	Usual constructor - this is called by the SceneManager.
        /// </summary>
        /// <param name="name">The name to give the billboard set (must be unique)</param>
        /// <param name="poolSize">The initial size of the billboard pool. Estimate of the number of billboards
        ///        which will be required, and pass it using this parameter. The set will
        ///        preallocate this number to avoid memory fragmentation. The default behaviour
        ///        once this pool has run out is to double it.
        /// </param>
        internal BillboardSet( string name, int poolSize )
        {
            this.name = name;
            this.PoolSize = poolSize;

            // default to fixed
            fixedTextureCoords = true;
        }

        /// <summary>
        /// Usual constructor - this is called by the SceneManager.
        /// </summary>
        /// <param name="name">The name to give the billboard set (must be unique)</param>
        /// <param name="poolSize">The initial size of the billboard pool. Estimate of the number of billboards
        ///        which will be required, and pass it using this parameter. The set will
        ///        preallocate this number to avoid memory fragmentation. The default behaviour
        ///        once this pool has run out is to double it.
        /// </param>
        /// <param name="externalData">If true, the source of data for drawing the 
        ///        billboards will not be the internal billboard list, but external 
        ///        data. When driving thebillboard from external data, you must call
        ///        _notifyCurrentCamera to reorient the billboards, setPoolSize to set
        ///        the maximum billboards you want to use, beginBillboards to 
        ///        start the update, and injectBillboard per billboard, 
        ///        followed by endBillboards.
        /// </param>
        internal BillboardSet( string name, int poolSize, bool externalData )
        {
            this.name = name;
            this.PoolSize = poolSize;
            this.externalData = externalData;
            
            // default to fixed
            fixedTextureCoords = true;
        }

        #endregion

        #region Methods

        /// <summary>
        ///		Sets the default dimensions of the billboards in this set.
        ///	 </summary>
        ///	 <remarks>
        ///		All billboards in a set are created with these default dimensions. The set will render most efficiently if
        ///		all the billboards in the set are the default size. It is possible to alter the size of individual
        ///		billboards at the expense of extra calculation. See the Billboard class for more info.
        /// </remarks>
        public void SetDefaultDimensions( float width, float height )
        {
            defaultWidth = width;
            defaultHeight = height;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public Billboard CreateBillboard( float x, float y, float z )
        {
            return CreateBillboard( new Vector3( x, y, z), ColorEx.White );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public Billboard CreateBillboard( float x, float y, float z , ColorEx color)
        {
            return CreateBillboard( new Vector3( x, y, z ), color );
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Billboard CreateBillboard( Vector3 position )
        {
            return CreateBillboard( position, ColorEx.White );
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
            if ( freeBillboards.Count == 0 )
            {
                if ( autoExtendPool )
                    this.PoolSize = this.PoolSize * 2;
                else
                    throw new AxiomException( "Could not create a billboard with AutoSize disabled and an empty pool." );
            }

            // get the next free billboard from the queue
            Billboard newBillboard = (Billboard)freeBillboards.Dequeue();

            // add the billboard to the active list
            activeBillboards.Add( newBillboard );

            // initialize the billboard
            newBillboard.Position = position;
            newBillboard.Color = color;
            newBillboard.NotifyOwner( this );

            // update the bounding volume of the set
            UpdateBounds();

            return newBillboard;
        }

        /// <summary>
        ///		Empties all of the active billboards from this set.
        /// </summary>
        public void Clear()
        {
            // Insert actives into free list
            foreach ( Billboard bill in activeBillboards )
            {
                freeBillboards.Enqueue( bill );
            }
            // clear the active billboard list
            activeBillboards.Clear();
        }

        /// <summary>
        ///		Update the bounds of the BillboardSet.
        /// </summary>
        public virtual void UpdateBounds()
        {
            if ( activeBillboards.Count == 0 )
            {
                // no billboards, so the bounding box is null
                aab.IsNull = true;
                boundingRadius = 0.0f;
            }
            else
            {
                float maxSqLen = -1.0f;
                Vector3 min = new Vector3( float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity );
                Vector3 max = new Vector3( float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity );

                for ( int i = 0; i < activeBillboards.Count; i++ )
                {
                    Billboard billboard = (Billboard)activeBillboards[i];

                    Vector3 pos = billboard.Position;

                    min.Floor( pos );
                    max.Ceil( pos );

                    maxSqLen = MathUtil.Max( maxSqLen, pos.LengthSquared );
                }

                // adjust for billboard size
                float adjust = MathUtil.Max( defaultWidth, defaultHeight );
                Vector3 vecAdjust = new Vector3( adjust, adjust, adjust );
                min -= vecAdjust;
                max += vecAdjust;

                // update our local aabb
                aab.SetExtents( min, max );

                boundingRadius = MathUtil.Sqrt( maxSqLen );

                // if we have a parent node, ask it to update us
                if ( parentNode != null )
                {
                    parentNode.NeedUpdate();
                }
            }
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
            get
            {
                return autoExtendPool;
            }
            set
            {
                autoExtendPool = value;
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
                return billboardPool.Count;
            }
            set
            {
                int size = value;
                int currentSize = billboardPool.Count;

                if ( currentSize < size )
                {
                    IncreasePool( size );

                    // add new items to the queue
                    for ( int i = currentSize; i < size; i++ )
                        freeBillboards.Enqueue( billboardPool[i] );

                    poolSize = size;
                    buffersCreated = false;

                    vertexData = null;
                    indexData = null;
                    
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
            get
            {
                return originType;
            }
            set
            {
                originType = value;
            }
        }

        /// <summary>
        ///		Gets/Sets the name of the material to use for this billboard set.
        /// </summary>
        public string MaterialName
        {
            get
            {
                return materialName;
            }
            set
            {
                materialName = value;

                // find the requested material
                material = MaterialManager.Instance.GetByName( materialName );

                if ( material != null )
                {
                    // make sure it is loaded
                    material.Load();
                }
                else
                {
                    throw new AxiomException( "Material '{0}' could not be found to be set as the material for BillboardSet '{0}'.", materialName, this.name );
                }
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
        public bool CullIndividually
        {
            get
            {
                return cullIndividually;
            }
            set
            {
                cullIndividually = value;
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
            get
            {
                return billboardType;
            }
            set
            {
                billboardType = value;
            }
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
            get
            {
                return commonDirection;
            }
            set
            {
                commonDirection = value;
            }
        }

        /// <summary>
        ///		Gets the list of active billboards.
        /// </summary>
        public BillboardList Billboards
        {
            get
            {
                return activeBillboards;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public float DefaultWidth
        {
            get
            {
                return defaultWidth;
            }
            set
            {
                defaultWidth = value;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public float DefaultHeight
        {
            get
            {
                return defaultHeight;
            }
            
            set
            {
                defaultHeight = value;
            }
        }
        
        #endregion

        #region Protected Methods

        /// <summary>
        /// 
        /// </summary>
        private void createBuffers()
        {
            // 4 vertices per billboard, 3 components = 12
            // 1 int value per vertex
            // 2 tris, 6 per billboard
            // 2d coords, 4 per billboard = 8

            vertexData = new VertexData();
            indexData = new IndexData();

            vertexData.vertexCount = poolSize * 4;
            vertexData.vertexStart = 0;

            // get references to the declaration and buffer binding
            VertexDeclaration decl = vertexData.vertexDeclaration;
            VertexBufferBinding binding = vertexData.vertexBufferBinding;

            // create the 3 vertex elements we need
            int offset = 0;
            decl.AddElement( 0, offset, VertexElementType.Float3, VertexElementSemantic.Position );
            offset += VertexElement.GetTypeSize( VertexElementType.Float3 );
            decl.AddElement( 0, offset, VertexElementType.Color, VertexElementSemantic.Diffuse );
            offset += VertexElement.GetTypeSize( VertexElementType.Color );
            decl.AddElement( 0, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
            
            // create position buffer
            mainBuffer = HardwareBufferManager.Instance.CreateVertexBuffer( decl.GetVertexSize( 0 ),
                vertexData.vertexCount,
                BufferUsage.DynamicWriteOnly );
            binding.SetBinding( 0, mainBuffer );

            // calc index buffer size
            indexData.indexStart = 0;
            indexData.indexCount = poolSize * 6;

            // create the index buffer
            indexData.indexBuffer =
                HardwareBufferManager.Instance.CreateIndexBuffer(
                IndexType.Size16,
                indexData.indexCount,
                BufferUsage.StaticWriteOnly );

            /* Create indexes and tex coords (will be the same every frame)
               Using indexes because it means 1/3 less vertex transforms (4 instead of 6)

               Billboard layout relative to camera:

                2-----3
                |    /|
                |  /  |
                |/    |
                0-----1
            */

            //float[] texData = new float[] {
            //             0.0f, 1.0f,
            //             1.0f, 1.0f,
            //             0.0f, 0.0f,
            //             1.0f, 0.0f };

            // lock the index buffer
            IntPtr idxPtr = indexData.indexBuffer.Lock( BufferLocking.Discard );

            unsafe
            {
                ushort* pIdx = (ushort*)idxPtr.ToPointer();

                for ( int idx = 0, idxOffset = 0, bboard = 0; bboard < poolSize; bboard++ )
                {
                    // compute indexes
                    //idx = bboard * 6;
                    //idxOffset = bboard * 4;
                    //texOffset = bboard * 8;

                    pIdx[idx++] = (ushort)idxOffset; // + 0;, for clarity
                    pIdx[idx++] = (ushort)( idxOffset + 1 );
                    pIdx[idx++] = (ushort)( idxOffset + 3 );
                    pIdx[idx++] = (ushort)( idxOffset );
                    pIdx[idx++] = (ushort)( idxOffset + 3 );
                    pIdx[idx++] = (ushort)( idxOffset + 2 );

                    idxOffset += 4;
                } // for
            } // unsafe

            // unlock the buffers
            indexData.indexBuffer.Unlock();
            //vBuffer.Unlock();

            buffersCreated = true;
        }
        
        /// <summary>
        ///		Callback used by Billboards to notify their parent that they have been resized.
        /// </summary>
        protected internal void NotifyBillboardResized()
        {
            allDefaultSize = false;
        }

        /// <summary>
        ///		Notifies the billboardset that texture coordinates will be modified
        ///		for this set.
        /// </summary>
        protected internal void NotifyBillboardTextureCoordsModified()
        {
            fixedTextureCoords = false;
        }

        /// <summary>
        ///		Internal method for increasing pool size.
        /// </summary>
        /// <param name="size"></param>
        protected virtual void IncreasePool( int size )
        {
            int oldSize = billboardPool.Count;

            // expand the capacity a bit
            billboardPool.Capacity += size;

            // add fresh Billboard objects to the new slots
            for ( int i = oldSize; i < size; i++ )
                billboardPool.Add( new Billboard() );
        }

        /// <summary>
        ///		Determines whether the supplied billboard is visible in the camera or not.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="billboard"></param>
        /// <returns></returns>
        protected bool IsBillboardVisible( Camera camera, Billboard billboard )
        {
            // if not culling each one, return true always
            if ( !cullIndividually )
                return true;

            // get the world matrix of this billboard set
            GetWorldTransforms( world );

            // get the center of the bounding sphere
            sphere.Center = world[0] * billboard.Position;

            // calculate the radius of the bounding sphere for the billboard
            if ( billboard.HasOwnDimensions )
            {
                sphere.Radius = MathUtil.Max( billboard.Width, billboard.Height );
            }
            else
            {
                sphere.Radius = MathUtil.Max( defaultWidth, defaultHeight );
            }

            // finally, see if the sphere is visible in the camera
            return camera.IsObjectVisible( sphere );
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected virtual void GenerateBillboardAxes( Camera camera, ref Vector3 x, ref Vector3 y )
        {
            GenerateBillboardAxes( camera, ref x, ref y, null );
        }

        /// <summary>
        ///		Generates billboard corners.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="billboard"></param>
        /// <remarks>Billboard param only required for type OrientedSelf</remarks>
        protected virtual void GenerateBillboardAxes( Camera camera, ref Vector3 x, ref Vector3 y, Billboard billboard )
        {
            // Default behavior is that billboards are in local node space
            // so orientation of camera (in world space) must be reverse-transformed 
            // into node space to generate the axes
            Quaternion invTransform = parentNode.DerivedOrientation.Inverse();
            Quaternion camQ = Quaternion.Zero;

            switch ( billboardType )
            {
                case BillboardType.Point:
                    // Get camera world axes for X and Y (depth is irrelevant)
                    camQ = camera.DerivedOrientation;
                    if ( !worldSpace )
                    {
                        // Convert into billboard local space
                        camQ = invTransform * camQ;                 
                    }
                    x = camQ * Vector3.UnitX;
                    y = camQ * Vector3.UnitY;
                    break;
                case BillboardType.OrientedCommon:
                    // Y-axis is common direction
                    // X-axis is cross with camera direction 
                    y = commonDirection;
                    if ( !worldSpace )
                    {
                        // Convert into billboard local space
                        camQ = invTransform * camQ;
                        x = camQ * camera.DerivedDirection.CrossProduct( y );                        
                    }
                    else
                    {
                        x = camera.DerivedDirection.CrossProduct( y );
                    }
                    x.Normalize();
                    break;
                case BillboardType.OrientedSelf:
                    // Y-axis is direction
                    // X-axis is cross with camera direction 
                    y = billboard.Direction;
                    if ( !worldSpace )
                    {
                        // Convert into billboard local space
                        camQ = invTransform * camQ;
                        x = camQ * camera.DerivedDirection.CrossProduct( y );
                    }
                    else
                    {
                        y *= 0.01f;
                        x = camera.DerivedDirection.CrossProduct( y );
                    }

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
        protected void GetParametericOffsets( out float left, out float right, out float top, out float bottom )
        {

            left = 0.0f;
            right = 0.0f;
            top = 0.0f;
            bottom = 0.0f;

            switch ( originType )
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
        /// <param name="offsets">Array of 4 Vector3 offsets.</param>
        /// <param name="billboard">A billboard.</param>
        protected void GenerateVertices( Vector3[] offsets, Billboard billboard )
        {
            int colorVal = Root.Instance.ConvertColor( billboard.Color );
            float[] rotTexData;
            
            unsafe
            {
                if ( !fixedTextureCoords )
                {
                    rotTexData = new float[8];
                    float rotation = billboard.rotationInRadians;
                    float cosRot = MathUtil.Cos( rotation );
                    float sinRot = MathUtil.Sin( rotation );

                    rotTexData[0] = ( cosRot * texDataBase[0] ) + ( sinRot * texDataBase[1] ) + 0.5f;
                    rotTexData[1] = ( sinRot * texDataBase[0] ) - ( cosRot * texDataBase[1] ) + 0.5f;

                    rotTexData[2] = ( cosRot * texDataBase[2] ) + ( sinRot * texDataBase[3] ) + 0.5f;
                    rotTexData[3] = ( sinRot * texDataBase[2] ) - ( cosRot * texDataBase[3] ) + 0.5f;

                    rotTexData[4] = ( cosRot * texDataBase[4] ) + ( sinRot * texDataBase[5] ) + 0.5f;
                    rotTexData[5] = ( sinRot * texDataBase[4] ) - ( cosRot * texDataBase[5] ) + 0.5f;

                    rotTexData[6] = ( cosRot * texDataBase[6] ) + ( sinRot * texDataBase[7] ) + 0.5f;
                    rotTexData[7] = ( sinRot * texDataBase[6] ) - ( cosRot * texDataBase[7] ) + 0.5f;
                }
                else
                {
                    rotTexData = (float[])basicTexData.Clone();
                }

                int* colors;

                // Add Vetices , left-top, right-top, left-bottom, right-bottom
                for ( int i = 0; i < 4; i++ )
                {
                    // Positions
                    *lockPtr++ = offsets[i].x + billboard.Position.x;
                    *lockPtr++ = offsets[i].y + billboard.Position.y;
                    *lockPtr++ = offsets[i].z + billboard.Position.z;
                    // Color
                    colors = (int*)lockPtr;
                    *colors++ = colorVal;
                    // Texture
                    lockPtr = (float*)colors;
                    *lockPtr++ = rotTexData[i * 2];
                    *lockPtr++ = rotTexData[i * 2 + 1];
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
        protected void GenerateVertexOffsets( float left, float right, float top, float bottom, float width, float height, ref Vector3 x, ref Vector3 y, Vector3[] destVec )
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
            destVec[0] = vLeftOff + vTopOff;
            destVec[1] = vRightOff + vTopOff;
            destVec[2] = vLeftOff + vBottomOff;
            destVec[3] = vRightOff + vBottomOff;
        }
        
        #endregion Protected Methods
        
        #region IRenderable Implementation

        public bool NormalizeNormals
        {
            get
            {
                return false;
            }
        }

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
                return material;
            }
        }

        public Technique Technique
        {
            get
            {
                return material.GetBestTechnique();
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

        /// <summary>
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

        /// <summary>
        /// 
        /// </summary>
        public SceneDetailLevel RenderDetail
        {
            get
            {
                return SceneDetailLevel.Solid;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Quaternion WorldOrientation
        {
            get
            {
                return parentNode.DerivedOrientation;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 WorldPosition
        {
            get
            {
                return parentNode.DerivedPosition;
            }
        }

        public LightList Lights
        {
            get
            {
                return parentNode.Lights;
            }
        }


        public void GetRenderOperation( RenderOperation op )
        {
            // fill the render operation with our vertex and index data

            // indexed triangle list
            op.operationType = OperationType.TriangleList;
            op.useIndices = true;

            op.vertexData = vertexData;
            op.vertexData.vertexStart = 0;
            op.vertexData.vertexCount = numVisibleBillboards * 4;

            op.indexData = indexData;
            op.indexData.indexStart = 0;
            op.indexData.indexCount = numVisibleBillboards * 6;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrices"></param>
        public virtual void GetWorldTransforms( Matrix4[] matrices )
        {
            if ( worldSpace )
            {
                matrices[0] = Matrix4.Identity;
            }
            else
            {
                matrices[0] = parentNode.FullTransform;                
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public virtual float GetSquaredViewDepth( Camera camera )
        {
            Debug.Assert( parentNode != null, "BillboardSet must have a parent scene node to get the squared view depth." );

            return parentNode.GetSquaredViewDepth( camera );
        }

        public Vector4 GetCustomParameter( int index )
        {
            if ( customParams[index] == null )
            {
                throw new Exception( "A parameter was not found at the given index" );
            }
            else
            {
                return (Vector4)customParams[index];
            }
        }

        public void SetCustomParameter( int index, Vector4 val )
        {
            customParams[index] = val;
        }

        public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
        {
            if ( customParams[entry.data] != null )
            {
                gpuParams.SetConstant( entry.index, (Vector4)customParams[entry.data] );
            }
        }

        #endregion

        #region MovableObject Overrides

        public override AxisAlignedBox BoundingBox
        {
            // cloning to prevent direct modification
            get
            {
                return (AxisAlignedBox)aab.Clone();
            }
        }

        /// <summary>
        ///    Local bounding radius of this billboard set.
        /// </summary>
        public override float BoundingRadius
        {
            get
            {
                return boundingRadius;
            }
        }

        /// <summary>
        ///		Generate the vertices for all the billboards relative to the camera
        /// </summary>
        /// <param name="camera"></param>
        public override void NotifyCurrentCamera( Camera camera )
        {
            currentCamera = camera;
            
            // Take the reverse transform of the camera world axes into billboard space for efficiency

            // create vertex and index buffers if they haven't already been
            if ( !buffersCreated )
            {
                createBuffers();
            }

            // get offsets for the origin type
            GetParametericOffsets( out leftOffset, out rightOffset, out topOffset, out bottomOffset );

            // generates axes up front if not orient per-billboard
            if ( billboardType != BillboardType.OrientedSelf )
            {
                GenerateBillboardAxes( camera, ref camX, ref camY );

                //	if all billboards are the same size we can precalculare the
                // offsets and just use + instead of * for each billboard, which should be faster.
                GenerateVertexOffsets( leftOffset, rightOffset, topOffset, bottomOffset,
                    defaultWidth, defaultHeight, ref camX, ref camY, vecOffsets );
            }

            // If we're driving this from our own data, go ahead
            if ( !externalData )
            {
                BeginBillboards();
                foreach ( Billboard bill in activeBillboards )
                {
                    InjectBillboard( bill );
                }
                EndBillboards();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public void BeginBillboards()
        {
            // reset counter
            numVisibleBillboards = 0;

            // get a reference to the vertex buffers to update
            //mainBuffer = vertexData.vertexBufferBinding.GetBuffer( 0 );

            // lock the buffers
            posPtr = mainBuffer.Lock( BufferLocking.Discard );
            unsafe
            {
                lockPtr = (float*)posPtr.ToPointer();
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="billboard"></param>
        public void InjectBillboard( Billboard billboard )
        {

            // Skip if not visible (NB always true if not bounds checking individual billboards)
            if ( !IsBillboardVisible( currentCamera, billboard ) )
                return;

            if ( billboardType == BillboardType.OrientedSelf )
            {
                // Have to generate axes & offsets per billboard
                GenerateBillboardAxes( currentCamera,  ref camX, ref camY, billboard  );
            }
            
            // if they are all the same size...
            if ( allDefaultSize )
            {

                if ( billboardType == BillboardType.OrientedSelf )
                {
                    // generate per billboard
                    GenerateVertexOffsets( leftOffset, rightOffset, topOffset, bottomOffset, defaultWidth,
                        defaultHeight, ref camX, ref camY, vecOffsets );
                }

                // generate the billboard vertices
                GenerateVertices( vecOffsets, billboard );

            }
            else
            {
                Vector3[] vecOwnOffset = new Vector3[4];

                // if it has it's own dimensions. or self oriented, gen offsets
                if ( billboard.HasOwnDimensions || billboardType == BillboardType.OrientedSelf )
                {
                    // generate using it's own dimensions
                    GenerateVertexOffsets( leftOffset, rightOffset, topOffset, bottomOffset, billboard.Width,
                        billboard.Height, ref camX, ref camY, vecOwnOffset );
                    GenerateVertices( vecOwnOffset, billboard );
                }
                else
                {
                    // generate the billboard vertices
                    GenerateVertices( vecOffsets, billboard );
                }
            }
            numVisibleBillboards++;

        }
        
        /// <summary>
        /// 
        /// </summary>
        public void EndBillboards()
        {
            // unlock the buffers
            mainBuffer.Unlock();
            unsafe
            {
                lockPtr = null;
            }    
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        public override void UpdateRenderQueue( RenderQueue queue )
        {
            // add ourself to the render queue
            if ( renderQueueIDSet )
            {
                queue.AddRenderable( this, RenderQueue.DEFAULT_PRIORITY, renderQueueID );                
            }
            else
            {
                queue.AddRenderable( this, RenderQueue.DEFAULT_PRIORITY );
            }
        }

        #endregion MovableObject Overrides
    }
}
