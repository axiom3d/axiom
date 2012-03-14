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

using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;

using Axiom.Collections;
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
#if !(XBOX || XBOX360 || SILVERLIGHT || WINDOWS_PHONE)
#endif

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Bsp
{
    /// <summary>
    ///		Specialisation of the SceneManager class to deal with indoor scenes based on a BSP tree.
    ///	</summary>
    ///	<remarks>
    ///		This class refines the behaviour of the default SceneManager to manage
    ///		a scene whose bulk of geometry is made up of an indoor environment which
    ///		is organised by a Binary Space Partition (BSP) tree.
    ///		<p/>
    ///		A BSP tree progressively subdivides the space using planes which are the nodes of the tree.
    ///		At some point we stop subdividing and everything in the remaining space is part of a 'leaf' which
    ///		contains a number of polygons. Typically we traverse the tree to locate the leaf in which a
    ///		point in space is (say the camera origin) and work from there. A second structure, the
    ///		Potentially Visible Set, tells us which other leaves can been seen from this
    ///		leaf, and we test their bounding boxes against the camera frustum to see which
    ///		we need to draw. Leaves are also a good place to start for collision detection since
    ///		they divide the level into discrete areas for testing.
    ///		<p/>
    ///		This BSP and PVS technique has been made famous by engines such as Quake and Unreal. Ogre
    ///		provides support for loading Quake3 level files to populate your world through this class,
    ///		by calling the BspSceneManager.LoadWorldGeometry. Note that this interface is made
    ///		available at the top level of the SceneManager class so you don't have to write your code
    ///		specifically for this class - just call Root::getSceneManager passing a SceneType of ST_INDOOR
    ///		and in the current implementation you will get a BspSceneManager silently disguised as a
    ///		standard SceneManager.
    /// </remarks>
    public class BspSceneManager : SceneManager
    {
        #region Protected members

        protected RenderOperation aaBGeometry = new RenderOperation();
        protected BspGeometry bspGeometry;
        protected Dictionary<int, bool> faceGroupChecked = new Dictionary<int, bool>();
        protected BspLevel level;
        protected bool[] lightAddedToFrustum;

        protected Collections.MultiMap<Material, BspStaticFaceGroup> matFaceGroupMap = new Collections.MultiMap<Material, BspStaticFaceGroup>();

        protected MovableObjectCollection objectsForRendering = new MovableObjectCollection();
        protected RenderOperation renderOp = new RenderOperation();
        protected bool showNodeAABs;
        protected SpotlightFrustum spotlightFrustum;
        protected Material textureLightMaterial;
        protected Pass textureLightPass;

        #endregion Protected members

        #region Public properties

        public BspLevel Level
        {
            get
            {
                return this.level;
            }
        }

        public bool ShowNodeBoxes
        {
            get
            {
                return this.showNodeAABs;
            }
            set
            {
                this.showNodeAABs = value;
            }
        }

        public override string TypeName
        {
            get
            {
                return "BspSceneManager";
            }
        }

        #endregion Public properties

        #region Constructor

        public BspSceneManager( string name )
            : base( name )
        {
            // Set features for debugging render
            this.showNodeAABs = false;

            // No sky by default
            isSkyPlaneEnabled = false;
            isSkyBoxEnabled = false;
            isSkyDomeEnabled = false;

            this.level = null;

            new BspResourceManager();
        }

        #endregion Constructor

        #region Public methods

        public override int EstimateWorldGeometry( string filename )
        {
            return BspLevel.CalculateLoadingStages( filename );
        }

        public override int EstimateWorldGeometry( Stream stream, string typeName )
        {
            return base.EstimateWorldGeometry( stream, typeName );
        }

        public override void SetWorldGeometry( string filename )
        {
            if ( Path.GetExtension( filename.ToLower() ) != ".bsp" )
            {
                throw new AxiomException( "Unable to load world geometry. Invalid extension of map filename option (must be .bsp)." );
            }

            // Load using resource manager
            this.level = (BspLevel)BspResourceManager.Instance.Load( filename, ResourceGroupManager.Instance.WorldResourceGroupName );

            //if (this.level.IsSkyEnabled)
            //{
            //    // Quake3 is always aligned with Z upwards
            //    Quaternion q = Quaternion.FromAngleAxis(Utility.HALF_PI, Vector3.UnitX);
            //    // Also draw last, and make close to camera (far clip plane is shorter)
            //    SetSkyDome(true, this.level.SkyMaterialName, this.level.SkyCurvature, 12, 2000, false, q);
            //}
            //else
            //{
            //    SetSkyDome(false, String.Empty);
            //}

            // Init static render operation
            this.renderOp.vertexData = this.level.VertexData;
            // index data is per-frame
            this.renderOp.indexData = new IndexData();
            this.renderOp.indexData.indexStart = 0;
            this.renderOp.indexData.indexCount = 0;
            // Create enough index space to render whole level
            this.renderOp.indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size32, // always 32-bit
                                                                                                    this.level.NumIndexes, BufferUsage.DynamicWriteOnlyDiscardable, false );

            this.renderOp.operationType = OperationType.TriangleList;
            this.renderOp.useIndices = true;
        }

        public override void SetWorldGeometry( Stream stream, string typeName )
        {
            // Load using resource manager
            this.level = BspResourceManager.Instance.Load( stream, ResourceGroupManager.Instance.WorldResourceGroupName );

            //if (this.level.IsSkyEnabled)
            //{
            //    // Quake3 is always aligned with Z upwards
            //    Quaternion q = Quaternion.FromAngleAxis(Utility.HALF_PI, Vector3.UnitX);
            //    // Also draw last, and make close to camera (far clip plane is shorter)
            //    SetSkyDome(true, this.level.SkyMaterialName, this.level.SkyCurvature, 12, 2000, false, q);
            //}
            //else
            //{
            //    SetSkyDome(false, String.Empty);
            //}

            // Init static render operation
            this.renderOp.vertexData = this.level.VertexData;
            // index data is per-frame
            this.renderOp.indexData = new IndexData();
            this.renderOp.indexData.indexStart = 0;
            this.renderOp.indexData.indexCount = 0;
            // Create enough index space to render whole level
            this.renderOp.indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size32, // always 32-bit
                                                                                                    this.level.NumIndexes, BufferUsage.DynamicWriteOnlyDiscardable, false );

            this.renderOp.operationType = OperationType.TriangleList;
            this.renderOp.useIndices = true;
        }

        /// <summary>
        ///		Specialized from SceneManager to support Quake3 bsp files.
        /// </summary>
        public override void LoadWorldGeometry( string filename )
        {
            this.bspGeometry = new BspGeometry();

            if ( Path.GetExtension( filename ).ToLower() == ".xml" )
            {
#if !(XBOX || XBOX360 || SILVERLIGHT || WINDOWS_PHONE)
                var optionData = new DataSet();
                optionData.ReadXml( filename );

                DataTable table = optionData.Tables[ 0 ];
                DataRow row = table.Rows[ 0 ];

                if ( table.Columns[ "Map" ] != null )
                {
                    optionList[ "Map" ] = row[ "Map" ];
                }

                if ( table.Columns[ "SetYAxisUp" ] != null )
                {
                    optionList[ "SetYAxisUp" ] = ( string.Compare( (string)row[ "SetYAxisUp" ], "yes", true ) ) == 0 ? true : false;
                }

                if ( table.Columns[ "Scale" ] != null )
                {
                    optionList[ "Scale" ] = StringConverter.ParseFloat( (string)row[ "Scale" ] );
                }

                Vector3 move = Vector3.Zero;

                if ( table.Columns[ "MoveX" ] != null )
                {
                    move.x = StringConverter.ParseFloat( (string)row[ "MoveX" ] );
                }

                if ( table.Columns[ "MoveY" ] != null )
                {
                    move.y = StringConverter.ParseFloat( (string)row[ "MoveY" ] );
                }

                if ( table.Columns[ "MoveZ" ] != null )
                {
                    move.z = StringConverter.ParseFloat( (string)row[ "MoveZ" ] );
                }

                optionList[ "Move" ] = move;
                optionList[ "MoveX" ] = move.x;
                optionList[ "MoveY" ] = move.y;
                optionList[ "MoveZ" ] = move.z;

                if ( table.Columns[ "UseLightmaps" ] != null )
                {
                    optionList[ "UseLightmaps" ] = ( string.Compare( (string)row[ "UseLightmaps" ], "yes", true ) ) == 0 ? true : false;
                }

                if ( table.Columns[ "AmbientEnabled" ] != null )
                {
                    optionList[ "AmbientEnabled" ] = ( string.Compare( (string)row[ "AmbientEnabled" ], "yes", true ) ) == 0 ? true : false;
                }

                if ( table.Columns[ "AmbientRatio" ] != null )
                {
                    optionList[ "AmbientRatio" ] = StringConverter.ParseFloat( (string)row[ "AmbientRatio" ] );
                }
#endif
            }
            else
            {
                optionList[ "Map" ] = filename;
            }

            LoadWorldGeometry();
        }

        public void LoadWorldGeometry()
        {
            this.bspGeometry = new BspGeometry();

            if ( !optionList.ContainsKey( "Map" ) )
            {
                throw new AxiomException( "Unable to load world geometry. \"Map\" filename option is not set." );
            }

            if ( Path.GetExtension( ( (string)optionList[ "Map" ] ).ToLower() ) != ".bsp" )
            {
                throw new AxiomException( "Unable to load world geometry. Invalid extension of map filename option (must be .bsp)." );
            }

            if ( !optionList.ContainsKey( "SetYAxisUp" ) )
            {
                optionList[ "SetYAxisUp" ] = false;
            }

            if ( !optionList.ContainsKey( "Scale" ) )
            {
                optionList[ "Scale" ] = 1f;
            }

            if ( !optionList.ContainsKey( "Move" ) )
            {
                optionList[ "Move" ] = Vector3.Zero;
                optionList[ "MoveX" ] = 0;
                optionList[ "MoveY" ] = 0;
                optionList[ "MoveZ" ] = 0;
            }

            if ( !optionList.ContainsKey( "UseLightmaps" ) )
            {
                optionList[ "UseLightmaps" ] = true;
            }

            if ( !optionList.ContainsKey( "AmbientEnabled" ) )
            {
                optionList[ "AmbientEnabled" ] = false;
            }

            if ( !optionList.ContainsKey( "AmbientRatio" ) )
            {
                optionList[ "AmbientRatio" ] = 1f;
            }

            InitTextureLighting();

            if ( this.spotlightFrustum == null )
            {
                this.spotlightFrustum = new SpotlightFrustum();
            }

            var paramList = new NameValuePairList();

            foreach ( DictionaryEntry option in optionList )
            {
                paramList.Add( option.Key.ToString(), option.Value.ToString() );
            }

            // Load using resource manager
            this.level = (BspLevel)BspResourceManager.Instance.Load( (string)optionList[ "Map" ], ResourceGroupManager.Instance.WorldResourceGroupName, false, null, paramList );

            // Init static render operation
            this.renderOp.vertexData = this.level.VertexData;

            // index data is per-frame
            this.renderOp.indexData = new IndexData();
            this.renderOp.indexData.indexStart = 0;
            this.renderOp.indexData.indexCount = 0;

            // Create enough index space to render whole level
            this.renderOp.indexData.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer( IndexType.Size32, this.level.NumIndexes, BufferUsage.Dynamic, false );
            this.renderOp.operationType = OperationType.TriangleList;
            this.renderOp.useIndices = true;
        }

        /// <summary>
        ///		Specialised to suggest viewpoints.
        /// </summary>
        public override ViewPoint GetSuggestedViewpoint( bool random )
        {
            if ( ( this.level == null ) || ( this.level.PlayerStarts.Length == 0 ) )
            {
                return base.GetSuggestedViewpoint( random );
            }
            else
            {
                if ( random )
                {
                    return this.level.PlayerStarts[ (int)( Utility.UnitRandom() * this.level.PlayerStarts.Length ) ];
                }
                else
                {
                    return this.level.PlayerStarts[ 0 ];
                }
            }
        }

        /// <summary>
        ///		Overriden from SceneManager.
        /// </summary>
        public override void FindVisibleObjects( Camera camera, bool onlyShadowCasters )
        {
            if ( !onlyShadowCasters )
            {
                // Add this renderable to the RenderQueue so that the BspSceneManager gets
                // notified when the geometry needs rendering and with what lights
                //GetRenderQueue().AddRenderable( bspGeometry );
            }

            // Clear unique list of movables for this frame
            this.objectsForRendering.Clear();

            // Walk the tree, tag static geometry, return camera's node (for info only)
            // Movables are now added to the render queue in processVisibleLeaf
            BspNode cameraNode = WalkTree( camera, onlyShadowCasters );
        }

        protected override IList FindShadowCastersForLight( Light light, Camera camera )
        {
            // objectsForRendering was filled at ProcessVisibleLeaf which is called
            // during FindVisibleObjects

            IList casters = base.FindShadowCastersForLight( light, camera );

            for ( int i = 0; i < casters.Count; i++ )
            {
                if ( !this.objectsForRendering.ContainsKey( ( (MovableObject)casters[ i ] ).Name ) )
                {
                    // this shadow caster is not visible, remove it
                    casters.RemoveAt( i );
                    i--;
                }
            }

            return casters;
        }

        /// <summary>
        ///		Creates a specialized <see cref="Plugin_BSPSceneManager.BspSceneNode"/>.
        /// </summary>
        public override SceneNode CreateSceneNode()
        {
            var node = new BspSceneNode( this );
            sceneNodeList.Add( node );

            return node;
        }

        /// <summary>
        ///		Creates a specialized <see cref="Plugin_BSPSceneManager.BspSceneNode"/>.
        /// </summary>
        public override SceneNode CreateSceneNode( string name )
        {
            var node = new BspSceneNode( this, name );
            sceneNodeList.Add( node );

            return node;
        }

        /// <summary>
        ///		Internal method for tagging <see cref="Plugin_BSPSceneManager.BspNode"/>'s with objects which intersect them.
        /// </summary>
        internal void NotifyObjectMoved( MovableObject obj, Vector3 pos )
        {
            this.level.NotifyObjectMoved( obj, pos );
        }

        /// <summary>
        ///		Internal method for notifying the level that an object has been detached from a node.
        /// </summary>
        internal void NotifyObjectDetached( MovableObject obj )
        {
            this.level.NotifyObjectDetached( obj );
        }

        // TODO: Scene queries.
        /// <summary>
        ///		Creates an AxisAlignedBoxSceneQuery for this scene manager.
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for this scene manager,
        ///		for an axis aligned box region. See SceneQuery and AxisAlignedBoxSceneQuery
        ///		for full details.
        ///		<p/>
        ///		The instance returned from this method must be destroyed by calling
        ///		SceneManager.DestroyQuery when it is no longer required.
        /// </remarks>
        /// <param name="box">Details of the box which describes the region for this query.</param>
        /*public virtual AxisAlignedBoxSceneQuery CreateAABBQuery(AxisAlignedBox box)
		{
			return CreateAABBQuery(box, 0xFFFFFFFF);
		}

		/// <summary>
		///		Creates an AxisAlignedBoxSceneQuery for this scene manager.
		/// </summary>
		/// <remarks>
		///		This method creates a new instance of a query object for this scene manager,
		///		for an axis aligned box region. See SceneQuery and AxisAlignedBoxSceneQuery
		///		for full details.
		///		<p/>
		///		The instance returned from this method must be destroyed by calling
		///		SceneManager.DestroyQuery when it is no longer required.
		/// </remarks>
		/// <param name="box">Details of the box which describes the region for this query.</param>
		/// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
		public virtual AxisAlignedBoxSceneQuery CreateAABBQuery(AxisAlignedBox box, ulong mask)
		{
			// TODO:
			return null;
		}*/
        /// <summary>
        ///		Creates a SphereSceneQuery for this scene manager.
        /// </summary>
        /// <remarks>
        /// 	This method creates a new instance of a query object for this scene manager,
        ///		for a spherical region. See SceneQuery and SphereSceneQuery
        ///		for full details.
        /// </remarks>
        /// <param name="sphere">Details of the sphere which describes the region for this query.</param>
        /// <param name="mask">The query mask to apply to this query; can be used to filter out	certain objects; see SceneQuery for details.</param>
        public override SphereRegionSceneQuery CreateSphereRegionQuery( Sphere sphere, uint mask )
        {
            var q = new BspSphereRegionSceneQuery( this );
            q.Sphere = sphere;
            q.QueryMask = mask;

            return q;
        }

        /// <summary>
        ///		Creates a RaySceneQuery for this scene manager.
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for this scene manager,
        ///		looking for objects which fall along a ray. See SceneQuery and RaySceneQuery
        ///		for full details.
        /// </remarks>
        /// <param name="ray">Details of the ray which describes the region for this query.</param>
        /// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
        public override RaySceneQuery CreateRayQuery( Ray ray, uint mask )
        {
            var q = new BspRaySceneQuery( this );
            q.Ray = ray;
            q.QueryMask = mask;

            return q;
        }

        /// <summary>
        ///		Creates an IntersectionSceneQuery for this scene manager.
        /// </summary>
        /// <remarks>
        ///		This method creates a new instance of a query object for locating
        ///		intersecting objects. See SceneQuery and IntersectionSceneQuery
        ///		for full details.
        /// </remarks>
        /// <param name="mask">The query mask to apply to this query; can be used to filter out certain objects; see SceneQuery for details.</param>
        public override IntersectionSceneQuery CreateIntersectionQuery( uint mask )
        {
            var q = new BspIntersectionSceneQuery( this );
            q.QueryMask = mask;

            return q;
        }

        #endregion Public methods

        #region Protected methods

        protected void InitTextureLighting()
        {
            if ( targetRenderSystem.Capabilities.TextureUnitCount < 2 )
            {
                LogManager.Instance.Write( "--WARNING--At least 2 available texture units are required for BSP dynamic lighting!" );
            }

            Texture texLight = TextureLight.CreateTexture();

            this.textureLightMaterial = (Material)MaterialManager.Instance.GetByName( "Axiom/BspTextureLightMaterial" );
            if ( this.textureLightMaterial == null )
            {
                this.textureLightMaterial = (Material)MaterialManager.Instance.Create( "Axiom/BspTextureLightMaterial", ResourceGroupManager.DefaultResourceGroupName );
                this.textureLightPass = this.textureLightMaterial.GetTechnique( 0 ).GetPass( 0 );
                // the texture light
                TextureUnitState tex = this.textureLightPass.CreateTextureUnitState( texLight.Name );
                tex.SetColorOperation( LayerBlendOperation.Modulate );
                tex.ColorBlendMode.source2 = LayerBlendSource.Diffuse;
                tex.SetAlphaOperation( LayerBlendOperationEx.Modulate );
                tex.AlphaBlendMode.source2 = LayerBlendSource.Diffuse;
                tex.TextureCoordSet = 2;
                tex.SetTextureAddressingMode( TextureAddressing.Clamp );

                // The geometry texture without lightmap. Use the light texture on this
                // pass, the appropriate texture will be rendered at RenderTextureLighting
                tex = this.textureLightPass.CreateTextureUnitState( texLight.Name );
                tex.SetColorOperation( LayerBlendOperation.Modulate );
                tex.SetAlphaOperation( LayerBlendOperationEx.Modulate );
                tex.SetTextureAddressingMode( TextureAddressing.Wrap );

                this.textureLightPass.SetSceneBlending( SceneBlendType.TransparentAlpha );

                this.textureLightMaterial.CullingMode = CullingMode.None;
                this.textureLightMaterial.Lighting = false;
            }
            else
            {
                this.textureLightPass = this.textureLightMaterial.GetTechnique( 0 ).GetPass( 0 );
            }
        }

        /// <summary>
        ///		Walks the BSP tree looking for the node which the camera is in, and tags any geometry
        ///		which is in a visible leaf for later processing.
        /// </summary>
        protected BspNode WalkTree( Camera camera, bool onlyShadowCasters )
        {
            if ( this.level == null )
            {
                return null;
            }

            // Locate the leaf node where the camera is located
            BspNode cameraNode = this.level.FindLeaf( camera.DerivedPosition );

            this.matFaceGroupMap.Clear();
            this.faceGroupChecked.Clear();

            // Scan through all the other leaf nodes looking for visibles
            int i = this.level.NumNodes - this.level.LeafStart;
            int p = this.level.LeafStart;
            BspNode node;

            while ( i-- > 0 )
            {
                node = this.level.Nodes[ p ];

                if ( this.level.IsLeafVisible( cameraNode, node ) )
                {
                    // Visible according to PVS, check bounding box against frustum
                    //if ( camera.IsObjectVisible( node.BoundingBox ) )
                    {
                        ProcessVisibleLeaf( node, camera, onlyShadowCasters );

                        if ( this.showNodeAABs )
                        {
                            AddBoundingBox( node.BoundingBox, true );
                        }
                    }
                }

                p++;
            }

            return cameraNode;
        }

        /// <summary>
        ///		Tags geometry in the leaf specified for later rendering.
        /// </summary>
        protected void ProcessVisibleLeaf( BspNode leaf, Camera camera, bool onlyShadowCasters )
        {
            // Skip world geometry if we're only supposed to process shadow casters
            // World is pre-lit
            if ( !onlyShadowCasters )
            {
                // Parse the leaf node's faces, add face groups to material map
                int numGroups = leaf.NumFaceGroups;
                int idx = leaf.FaceGroupStart;

                while ( numGroups-- > 0 )
                {
                    int realIndex = this.level.LeafFaceGroups[ idx++ ];

                    // Is it already checked ?
                    if ( this.faceGroupChecked.ContainsKey( realIndex ) && this.faceGroupChecked[ realIndex ] )
                    {
                        continue;
                    }

                    this.faceGroupChecked[ realIndex ] = true;

                    BspStaticFaceGroup faceGroup = this.level.FaceGroups[ realIndex ];

                    // Get Material reference by handle
                    Material mat = GetMaterial( faceGroup.materialHandle );

                    // Check normal (manual culling)
                    ManualCullingMode cullMode = mat.GetTechnique( 0 ).GetPass( 0 ).ManualCullingMode;

                    if ( cullMode != ManualCullingMode.None )
                    {
                        float dist = faceGroup.plane.GetDistance( camera.DerivedPosition );

                        if ( ( ( dist < 0 ) && ( cullMode == ManualCullingMode.Back ) ) || ( ( dist > 0 ) && ( cullMode == ManualCullingMode.Front ) ) )
                        {
                            continue;
                        }
                    }

                    // Try to insert, will find existing if already there
                    this.matFaceGroupMap.Add( mat, faceGroup );
                }
            }

            // Add movables to render queue, provided it hasn't been seen already.
            foreach ( MovableObject obj in leaf.Objects.Values )
            {
                if ( !this.objectsForRendering.ContainsKey( obj.Name ) )
                {
                    if ( obj.IsVisible && ( !onlyShadowCasters || obj.CastShadows ) && camera.IsObjectVisible( obj.GetWorldBoundingBox() ) )
                    {
                        obj.NotifyCurrentCamera( camera );
                        obj.UpdateRenderQueue( renderQueue );
                        // Check if the bounding box should be shown.
                        var node = (SceneNode)obj.ParentNode;
                        if ( node.ShowBoundingBox || showBoundingBoxes )
                        {
                            node.AddBoundingBoxToQueue( renderQueue );
                        }
                        this.objectsForRendering.Add( obj );
                    }
                }
            }
        }

        /// <summary>
        ///		Caches a face group for imminent rendering.
        /// </summary>
        protected int CacheGeometry( BufferBase indexes, BspStaticFaceGroup faceGroup )
        {
            // Skip sky always
            if ( faceGroup.isSky )
            {
                return 0;
            }

            int idxStart = 0;
            int numIdx = 0;
            int vertexStart = 0;

            if ( faceGroup.type == FaceGroup.FaceList )
            {
                idxStart = faceGroup.elementStart;
                numIdx = faceGroup.numElements;
                vertexStart = faceGroup.vertexStart;
            }
            else if ( faceGroup.type == FaceGroup.Patch )
            {
                idxStart = faceGroup.patchSurf.IndexOffset;
                numIdx = faceGroup.patchSurf.CurrentIndexCount;
                vertexStart = faceGroup.patchSurf.VertexOffset;
            }
            else
            {
                // Unsupported face type
                return 0;
            }

#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                // Offset the indexes here
                // we have to do this now rather than up-front because the
                // indexes are sometimes reused to address different vertex chunks
                int idxSize = this.level.Indexes.IndexSize;
                BufferBase idxSrc = this.level.Indexes.Lock( idxStart * idxSize, numIdx * idxSize, BufferLocking.ReadOnly );
                if ( this.level.Indexes.Type == IndexType.Size16 )
                {
                    ushort* src = idxSrc.ToUShortPointer();
                    ushort* pIndexes = indexes.ToUShortPointer();
                    for ( int i = 0; i < numIdx; i++ )
                    {
                        pIndexes[ i ] = (ushort)( src[ i ] + vertexStart );
                    }
                }
                else
                {
                    uint* src = idxSrc.ToUIntPointer();
                    uint* pIndexes = indexes.ToUIntPointer();
                    for ( int i = 0; i < numIdx; i++ )
                    {
                        pIndexes[ i ] = (uint)( src[ i ] + vertexStart );
                    }
                }
                this.level.Indexes.Unlock();
            }

            // return number of elements
            return numIdx;
        }

        /// <summary>
        ///		Caches a face group and calculates texture lighting coordinates.
        /// </summary>
        protected int CacheLightGeometry( TextureLight light, BufferBase pIndexesBuf, BufferBase pTexLightMapsBuf, BufferBase pVerticesBuf, BspStaticFaceGroup faceGroup )
        {
#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                // Skip sky always
                if ( faceGroup.isSky )
                {
                    return 0;
                }

                int idxStart = 0;
                int numIdx = 0;
                int vertexStart = 0;

                if ( faceGroup.type == FaceGroup.FaceList )
                {
                    idxStart = faceGroup.elementStart;
                    numIdx = faceGroup.numElements;
                    vertexStart = faceGroup.vertexStart;
                }
                else if ( faceGroup.type == FaceGroup.Patch )
                {
                    idxStart = faceGroup.patchSurf.IndexOffset;
                    numIdx = faceGroup.patchSurf.CurrentIndexCount;
                    vertexStart = faceGroup.patchSurf.VertexOffset;
                }
                else
                {
                    // Unsupported face type
                    return 0;
                }

                int idxSize = this.level.Indexes.IndexSize;
                BufferBase idxSrc = this.level.Indexes.Lock( idxStart * idxSize, numIdx * idxSize, BufferLocking.ReadOnly );
#if SILVERLIGHT
                var src = idxSrc.ToUShortPointer();
#else
                uint* src = idxSrc.ToUIntPointer();
#endif

                int maxIndex = 0;
                for ( int i = 0; i < numIdx; i++ )
                {
                    var index = (int)src[ i ];
                    if ( index > maxIndex )
                    {
                        maxIndex = index;
                    }
                }

                var vertexPos = new Vector3[ maxIndex + 1 ];
                var vertexIsStored = new bool[ maxIndex + 1 ];

                for ( int i = 0; i < numIdx; i++ )
                {
                    var index = (int)src[ i ];
                    BspVertex* pVertices = pVerticesBuf.ToBspVertexPointer();
                    if ( !vertexIsStored[ index ] )
                    {
                        vertexPos[ index ] = pVertices[ vertexStart + index ].position;
                        vertexIsStored[ index ] = true;
                    }
                    pVerticesBuf.UnPin();
                }

                Vector2[] texCoors;
                ColorEx[] colors;

                bool res = light.CalculateTexCoordsAndColors( faceGroup.plane, vertexPos, out texCoors, out colors );

                if ( res )
                {
                    TextureLightMap* pTexLightMaps = pTexLightMapsBuf.ToTextureLightMapPointer();
                    for ( int i = 0; i <= maxIndex; i++ )
                    {
                        pTexLightMaps[ vertexStart + i ] = new TextureLightMap
                                                           {
                                                               color = Root.Instance.RenderSystem.ConvertColor( colors[ i ] ), textureLightMap = texCoors[ i ]
                                                           };
                    }
                    pTexLightMapsBuf.UnPin();

                    // Offset the indexes here
                    // we have to do this now rather than up-front because the
                    // indexes are sometimes reused to address different vertex chunks
                    if ( this.level.Indexes.Type == IndexType.Size16 )
                    {
                        ushort* pIndexes = pIndexesBuf.ToUShortPointer();
                        for ( int i = 0; i < numIdx; i++ )
                        {
                            pIndexes[ i ] = (ushort)( src[ i ] + vertexStart );
                        }
                    }
                    else
                    {
                        uint* pIndexes = pIndexesBuf.ToUIntPointer();
                        for ( int i = 0; i < numIdx; i++ )
                        {
                            pIndexes[ i ] = (uint)( src[ i ] + vertexStart );
                        }
                    }
                    this.level.Indexes.Unlock();

                    // return number of elements
                    return numIdx;
                }
                else
                {
                    this.level.Indexes.Unlock();

                    return 0;
                }
            }
        }

        /// <summary>
        ///		Adds a bounding box to draw if turned on.
        /// </summary>
        protected void AddBoundingBox( AxisAlignedBox aab, bool visible ) {}

        protected override bool OnRenderQueueEnded( RenderQueueGroupID group, string invocation )
        {
            bool repeat = base.OnRenderQueueEnded( group, invocation );
            if ( group == RenderQueueGroupID.SkiesEarly )
            {
                RenderStaticGeometry();
            }
            return repeat;
        }

        /// <summary>
        ///		Renders the static level geometry tagged in <see cref="Plugin_BSPSceneManager.BspSceneManager.WalkTree"/>.
        /// </summary>
        protected void RenderStaticGeometry()
        {
            // Check should we be rendering
            if ( !SpecialCaseRenderQueueList.IsRenderQueueToBeProcessed( worldGeometryRenderQueueId ) )
            {
                return;
            }
            if ( this.level == null )
            {
                LogManager.Instance.Write( "BSPSceneManager [Warning]: Skip RenderStaticGeometry, no level was set!" );
                return;
            }
            // no world transform required
            targetRenderSystem.WorldMatrix = Matrix4.Identity;

            // Set view / proj
            targetRenderSystem.ViewMatrix = cameraInProgress.ViewMatrix;
            targetRenderSystem.ProjectionMatrix = cameraInProgress.ProjectionMatrix;

            ColorEx bspAmbient = ColorEx.White;

            if ( this.level.BspOptions.ambientEnabled )
            {
                bspAmbient = new ColorEx( ambientColor.r * this.level.BspOptions.ambientRatio, ambientColor.g * this.level.BspOptions.ambientRatio, ambientColor.b * this.level.BspOptions.ambientRatio );
            }

            var ambientBlend = new LayerBlendModeEx();
            ambientBlend.blendType = LayerBlendType.Color;
            ambientBlend.operation = LayerBlendOperationEx.Modulate;
            ambientBlend.source1 = LayerBlendSource.Texture;
            ambientBlend.source2 = LayerBlendSource.Manual;
            ambientBlend.colorArg2 = bspAmbient;

            // For each material in turn, cache rendering data & render
            IEnumerator mapEnu = this.matFaceGroupMap.Keys.GetEnumerator();

            bool passIsSet = false;

            while ( mapEnu.MoveNext() )
            {
                // Get Material
                var thisMaterial = (Material)mapEnu.Current;
                List<BspStaticFaceGroup> faceGrp = this.matFaceGroupMap[ thisMaterial ];

                // if one face group is a quake shader then the material is a quake shader
                bool isQuakeShader = faceGrp[ 0 ].isQuakeShader;

                // Empty existing cache
                this.renderOp.indexData.indexCount = 0;

                // lock index buffer ready to receive data
#if !AXIOM_SAFE_ONLY
                BufferBase pIdx = this.renderOp.indexData.indexBuffer.Lock( BufferLocking.Discard );
                int sizeOfElement = this.renderOp.indexData.indexBuffer.Type == IndexType.Size32 ? sizeof ( uint ) : sizeof ( ushort );

                for ( int i = 0; i < faceGrp.Count; i++ )
                {
                    // Cache each
                    int numElems = CacheGeometry( pIdx, faceGrp[ i ] );
                    this.renderOp.indexData.indexCount += numElems;
                    pIdx += numElems * sizeOfElement;
                }

                // Unlock the buffer
                this.renderOp.indexData.indexBuffer.Unlock();

                // Skip if no faces to process (we're not doing flare types yet)
                if ( this.renderOp.indexData.indexCount == 0 )
                {
                    continue;
                }

                if ( isQuakeShader )
                {
                    for ( int i = 0; i < thisMaterial.GetTechnique( 0 ).PassCount; i++ )
                    {
                        SetPass( thisMaterial.GetTechnique( 0 ).GetPass( i ) );
                        targetRenderSystem.Render( this.renderOp );
                    }
                    passIsSet = false;
                }
                else if ( !passIsSet )
                {
                    int i;
                    for ( i = 0; i < thisMaterial.GetTechnique( 0 ).PassCount; i++ )
                    {
                        SetPass( thisMaterial.GetTechnique( 0 ).GetPass( i ) );

                        // for ambient lighting
                        if ( i == 0 && this.level.BspOptions.ambientEnabled )
                        {
                            targetRenderSystem.SetTextureBlendMode( 0, ambientBlend );
                        }

                        targetRenderSystem.Render( this.renderOp );
                    }

                    // if it's only 1 pass then there's no need to set it again
                    passIsSet = ( i > 1 ) ? false : true;
                }
                else
                {
                    Pass pass = thisMaterial.GetTechnique( 0 ).GetPass( 0 );
                    // Get the plain geometry texture
                    if ( pass.TextureUnitStatesCount > 0 )
                    {
                        TextureUnitState geometryTex = pass.GetTextureUnitState( 0 );
                        targetRenderSystem.SetTexture( 0, true, geometryTex.TextureName );
                    }

                    if ( pass.TextureUnitStatesCount > 1 )
                    {
                        // Get the lightmap
                        TextureUnitState lightmapTex = pass.GetTextureUnitState( 1 );
                        targetRenderSystem.SetTexture( 1, true, lightmapTex.TextureName );
                    }

                    targetRenderSystem.Render( this.renderOp );
                }
            }

            //if(showNodeAABs)
            //	targetRenderSystem.Render(aaBGeometry);
        }

        /// <summary>
        ///		Renders the texture lighting tagged in the specified light
        /// </summary>
        protected void RenderTextureLighting( Light light )
        {
            if ( !( light is TextureLight ) )
            {
                return;
            }

            var texLight = (TextureLight)light;

            if ( !texLight.IsTextureLight )
            {
                return;
            }

            if ( texLight.Type == LightType.Spotlight )
            {
                this.spotlightFrustum.Spotlight = texLight;
            }

            // no world transform required
            targetRenderSystem.WorldMatrix = Matrix4.Identity;

            // Set view / proj
            targetRenderSystem.ViewMatrix = cameraInProgress.ViewMatrix;
            targetRenderSystem.ProjectionMatrix = cameraInProgress.ProjectionMatrix;

            TextureUnitState lightTex = this.textureLightPass.GetTextureUnitState( 0 );
            TextureUnitState normalTex = this.textureLightPass.GetTextureUnitState( 1 );

            switch ( texLight.Intensity )
            {
                case LightIntensity.Normal:
                    normalTex.ColorBlendMode.operation = LayerBlendOperationEx.Modulate;
                    break;

                case LightIntensity.ModulateX2:
                    normalTex.ColorBlendMode.operation = LayerBlendOperationEx.ModulateX2;
                    break;

                case LightIntensity.ModulateX4:
                    normalTex.ColorBlendMode.operation = LayerBlendOperationEx.ModulateX4;
                    break;
            }

            if ( texLight.Type == LightType.Spotlight )
            {
                this.spotlightFrustum.Spotlight = texLight;
                lightTex.SetProjectiveTexturing( true, this.spotlightFrustum );
            }
            else
            {
                lightTex.SetProjectiveTexturing( false, null );
            }

            if ( texLight.Type == LightType.Directional )
            {
                // light it using only diffuse color and alpha
                normalTex.ColorBlendMode.source2 = LayerBlendSource.Diffuse;
                normalTex.AlphaBlendMode.source2 = LayerBlendSource.Diffuse;
            }
            else
            {
                // light it using the texture light
                normalTex.ColorBlendMode.source2 = LayerBlendSource.Current;
                normalTex.AlphaBlendMode.source2 = LayerBlendSource.Current;
            }

            SetPass( this.textureLightPass );

            if ( texLight.Type == LightType.Directional )
            {
                // Disable the light texture
                targetRenderSystem.SetTexture( 0, true, lightTex.TextureName );
            }

            // For each material in turn, cache rendering data & render
            IEnumerator mapEnu = this.matFaceGroupMap.Keys.GetEnumerator();

            while ( mapEnu.MoveNext() )
            {
                // Get Material
                var thisMaterial = (Material)mapEnu.Current;
                List<BspStaticFaceGroup> faceGrp = this.matFaceGroupMap[ thisMaterial ];

                // if one face group is a quake shader then the material is a quake shader
                if ( faceGrp[ 0 ].isQuakeShader )
                {
                    continue;
                }

                ManualCullingMode cullMode = thisMaterial.GetTechnique( 0 ).GetPass( 0 ).ManualCullingMode;

                // Empty existing cache
                this.renderOp.indexData.indexCount = 0;

                HardwareVertexBuffer bspVertexBuffer = this.level.VertexData.vertexBufferBinding.GetBuffer( 0 );
                HardwareVertexBuffer lightTexCoordBuffer = this.level.VertexData.vertexBufferBinding.GetBuffer( 1 );

                // lock index buffer ready to receive data
#if !AXIOM_SAFE_ONLY
                BufferBase pVertices = bspVertexBuffer.Lock( BufferLocking.ReadOnly );
                BufferBase pTexLightMap = lightTexCoordBuffer.Lock( BufferLocking.Discard );
                BufferBase pIdx = this.renderOp.indexData.indexBuffer.Lock( BufferLocking.Discard );

                for ( int i = 0; i < faceGrp.Count; i++ )
                {
                    if ( faceGrp[ i ].type != FaceGroup.Patch && texLight.AffectsFaceGroup( faceGrp[ i ], cullMode ) )
                    {
                        // Cache each
                        int numElems = CacheLightGeometry( texLight, pIdx, pTexLightMap, pVertices, faceGrp[ i ] );
                        this.renderOp.indexData.indexCount += numElems;
                        pIdx += numElems;
                    }
                }

                // Unlock the buffers
                this.renderOp.indexData.indexBuffer.Unlock();
                lightTexCoordBuffer.Unlock();
                bspVertexBuffer.Unlock();

                // Skip if no faces to process
                if ( this.renderOp.indexData.indexCount == 0 )
                {
                    continue;
                }

                // Get the plain geometry texture
                TextureUnitState geometryTex = thisMaterial.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 );
                if ( geometryTex.IsBlank )
                {
                    continue;
                }

                targetRenderSystem.SetTexture( 1, true, geometryTex.TextureName );
                // OpenGL requires the addressing mode to be set before every render operation
                targetRenderSystem.SetTextureAddressingMode( 0, new UVWAddressing( TextureAddressing.Clamp ) );
                targetRenderSystem.Render( this.renderOp );
            }
        }

        /// <summary>
        ///		Renders texture shadow on tagged in level geometry.
        /// </summary>
        protected void RenderTextureShadowOnGeometry()
        {
            // no world transform required
            targetRenderSystem.WorldMatrix = Matrix4.Identity;

            // Set view / proj
            targetRenderSystem.ViewMatrix = cameraInProgress.ViewMatrix;
            targetRenderSystem.ProjectionMatrix = cameraInProgress.ProjectionMatrix;

            Camera shadowCam = null;
            Vector3 camPos = Vector3.Zero, camDir = Vector3.Zero;
            TextureUnitState shadowTex = shadowReceiverPass.GetTextureUnitState( 0 );

            for ( int i = 0; i < shadowTex.NumEffects; i++ )
            {
                if ( shadowTex.GetEffect( i ).type == TextureEffectType.ProjectiveTexture )
                {
                    shadowCam = (Camera)shadowTex.GetEffect( i ).frustum;
                    camPos = shadowCam.DerivedPosition;
                    camDir = shadowCam.DerivedDirection;
                    break;
                }
            }

            CullingMode prevCullMode = shadowReceiverPass.CullingMode;
            LayerBlendModeEx colorBlend = shadowTex.ColorBlendMode;
            LayerBlendSource prevSource = colorBlend.source2;
            ColorEx prevColorArg = colorBlend.colorArg2;

            // Quake uses counter-clockwise culling
            shadowReceiverPass.CullingMode = CullingMode.CounterClockwise;
            colorBlend.source2 = LayerBlendSource.Manual;
            colorBlend.colorArg2 = ColorEx.White;

            SetPass( shadowReceiverPass );

            shadowReceiverPass.CullingMode = prevCullMode;
            colorBlend.source2 = prevSource;
            colorBlend.colorArg2 = prevColorArg;

            // Empty existing cache
            this.renderOp.indexData.indexCount = 0;

            // lock index buffer ready to receive data
#if !AXIOM_SAFE_ONLY
            BufferBase pIdx = this.renderOp.indexData.indexBuffer.Lock( BufferLocking.Discard );
            int sizeOfElement = this.renderOp.indexData.indexBuffer.Type == IndexType.Size32 ? sizeof ( uint ) : sizeof ( ushort );

            // For each material in turn, cache rendering data
            IEnumerator mapEnu = this.matFaceGroupMap.Keys.GetEnumerator();

            while ( mapEnu.MoveNext() )
            {
                // Get Material
                var thisMaterial = (Material)mapEnu.Current;
                BspStaticFaceGroup[] faceGrp = this.matFaceGroupMap[ thisMaterial ].ToArray();

                // if one face group is a quake shader then the material is a quake shader
                if ( faceGrp[ 0 ].isQuakeShader )
                {
                    continue;
                }

                for ( int i = 0; i < faceGrp.Length; i++ )
                {
                    float dist = faceGrp[ i ].plane.GetDistance( camPos );
                    float angle = faceGrp[ i ].plane.Normal.Dot( camDir );

                    if ( ( ( dist < 0 && angle > 0 ) || ( dist > 0 && angle < 0 ) ) && Utility.Abs( angle ) >= Utility.Cos( shadowCam.FieldOfView * 0.5 ) )
                    {
                        // face is in shadow's frustum

                        // Cache each
                        int numElems = CacheGeometry( pIdx, faceGrp[ i ] );
                        this.renderOp.indexData.indexCount += numElems;
                        pIdx += numElems * sizeOfElement;
                    }
                }
            }

            // Unlock the buffer
            this.renderOp.indexData.indexBuffer.Unlock();

            // Skip if no faces to process
            if ( this.renderOp.indexData.indexCount == 0 )
            {
                return;
            }

            targetRenderSystem.Render( this.renderOp );
        }

        /// <summary>
        ///		Overriden from SceneManager.
        /// </summary>
        protected override void RenderSingleObject( IRenderable renderable, Pass pass, bool doLightIteration, LightList manualLightList )
        {
            if ( renderable is BspGeometry )
            {
                // Render static level geometry
                if ( doLightIteration )
                {
                    // render all geometry without lights first
                    RenderStaticGeometry();

                    // render geometry affected by each visible light
                    foreach ( Light l in lightsAffectingFrustum )
                    {
                        RenderTextureLighting( l );
                    }
                }
                else
                {
                    if ( manualLightList.Count == 0 )
                    {
                        if ( illuminationStage == IlluminationRenderStage.RenderReceiverPass )
                        {
                            // texture shadows
                            RenderTextureShadowOnGeometry();
                        }
                        else
                        {
                            // ambient stencil pass, render geometry without lights
                            RenderStaticGeometry();
                        }
                    }
                    else
                    {
                        // render only geometry affected by the provided light
                        foreach ( Light l in manualLightList )
                        {
                            RenderTextureLighting( l );
                        }
                    }
                }
            }
            else
            {
                base.RenderSingleObject( renderable, pass, doLightIteration, manualLightList );
            }
        }

        #endregion Protected methods
    }

    /// <summary>
    ///		BSP specialisation of IntersectionSceneQuery.
    /// </summary>
    public class BspIntersectionSceneQuery : DefaultIntersectionSceneQuery
    {
        #region Constructor

        public BspIntersectionSceneQuery( SceneManager creator )
            : base( creator )
        {
            AddWorldFragmentType( WorldFragmentType.PlaneBoundedRegion );
        }

        #endregion Constructor

        #region Fields

        private readonly PlaneBoundedVolume boundedVolume = new PlaneBoundedVolume( PlaneSide.Positive );
        private readonly Collections.MultiMap<MovableObject, BspBrush> brushIntersections = new Collections.MultiMap<MovableObject, BspBrush>();
        private readonly Collections.MultiMap<MovableObject, MovableObject> objIntersections = new Collections.MultiMap<MovableObject, MovableObject>();

        private readonly List<MovableObject> objectsDone = new List<MovableObject>( 100 );

        #endregion Fields

        #region Public methods

        /// <summary>
        /// Go through each leaf node in <see cref="BspLevel"/> and check movables against each other and against <see cref="BspBrush"/> fragments.
        /// The bounding boxes of object are used when checking for the intersections.
        /// </summary>
        /// <param name="listener"></param>
        public override void Execute( IIntersectionSceneQueryListener listener )
        {
            //Issue: some movable-movable intersections could be reported twice if 2 movables
            //overlap 2 leaves?
            BspLevel lvl = ( (BspSceneManager)creator ).Level;
            int leafPoint = lvl.LeafStart;
            int numLeaves = lvl.NumLeaves;

            this.objIntersections.Clear();
            this.brushIntersections.Clear();

            while ( --numLeaves >= 0 )
            {
                BspNode leaf = lvl.Nodes[ leafPoint ];

                this.objectsDone.Clear();

                foreach ( MovableObject aObj in leaf.Objects.Values )
                {
                    // skip this object if collision not enabled
                    if ( ( aObj.QueryFlags & queryMask ) == 0 )
                    {
                        continue;
                    }

                    // get it's bounds
                    AxisAlignedBox aBox = aObj.GetWorldBoundingBox();

                    // check object against the others in this node
                    foreach ( MovableObject bObj in this.objectsDone )
                    {
                        if ( aBox.Intersects( bObj.GetWorldBoundingBox() ) )
                        {
                            // check if this pair is already reported
                            IList interObjList = this.objIntersections.FindBucket( aObj );
                            if ( interObjList == null || interObjList.Contains( bObj ) == false )
                            {
                                this.objIntersections.Add( aObj, bObj );
                                listener.OnQueryResult( aObj, bObj );
                            }
                        }
                    }

                    if ( ( QueryTypeMask & (uint)SceneQueryTypeMask.WorldGeometry ) != 0 )
                    {
                        // check object against brushes
                        if ( ( QueryTypeMask & (ulong)SceneQueryTypeMask.WorldGeometry ) != 0 )
                        {
                            foreach ( BspBrush brush in leaf.SolidBrushes )
                            {
                                if ( brush == null )
                                {
                                    continue;
                                }

                                // test brush against object
                                this.boundedVolume.planes = brush.Planes;
                                if ( this.boundedVolume.Intersects( aBox ) )
                                {
                                    // check if this pair is already reported
                                    IList interBrushList = this.brushIntersections.FindBucket( aObj );
                                    if ( interBrushList == null || interBrushList.Contains( brush ) == false )
                                    {
                                        this.brushIntersections.Add( aObj, brush );
                                        // report this brush as it's WorldFragment
                                        listener.OnQueryResult( aObj, brush.Fragment );
                                    }
                                }
                            }
                        }
                    }
                    this.objectsDone.Add( aObj );
                }

                ++leafPoint;
            }
        }

        #endregion Public methods
    }

    /// <summary>
    ///		BSP specialisation of RaySceneQuery.
    /// </summary>
    public class BspRaySceneQuery : DefaultRaySceneQuery
    {
        #region Constructor

        public BspRaySceneQuery( SceneManager creator )
            : base( creator )
        {
            AddWorldFragmentType( WorldFragmentType.PlaneBoundedRegion );
        }

        #endregion Constructor

        protected bool StopRayTracing;
        protected IRaySceneQueryListener listener;

        #region Public methods

        public override void Execute( IRaySceneQueryListener listener )
        {
            this.listener = listener;
            this.StopRayTracing = false;
            ProcessNode( ( (BspSceneManager)creator ).Level.RootNode, ray, float.PositiveInfinity, 0 );
        }

        #endregion Public methods

        #region Protected methods

        protected virtual void ProcessNode( BspNode node, Ray tracingRay, float maxDistance, float traceDistance )
        {
            // check if ray already encountered a solid brush
            if ( this.StopRayTracing )
            {
                return;
            }

            if ( node.IsLeaf )
            {
                ProcessLeaf( node, tracingRay, maxDistance, traceDistance );
                return;
            }

            IntersectResult result = tracingRay.Intersects( node.SplittingPlane );
            if ( result.Hit )
            {
                if ( result.Distance < maxDistance )
                {
                    if ( node.GetSide( tracingRay.Origin ) == PlaneSide.Negative )
                    {
                        ProcessNode( node.BackNode, tracingRay, result.Distance, traceDistance );
                        Vector3 splitPoint = tracingRay.Origin + tracingRay.Direction * result.Distance;
                        ProcessNode( node.FrontNode, new Ray( splitPoint, tracingRay.Direction ), maxDistance - result.Distance, traceDistance + result.Distance );
                    }
                    else
                    {
                        ProcessNode( node.FrontNode, tracingRay, result.Distance, traceDistance );
                        Vector3 splitPoint = tracingRay.Origin + tracingRay.Direction * result.Distance;
                        ProcessNode( node.BackNode, new Ray( splitPoint, tracingRay.Direction ), maxDistance - result.Distance, traceDistance + result.Distance );
                    }
                }
                else
                {
                    ProcessNode( node.GetNextNode( tracingRay.Origin ), tracingRay, maxDistance, traceDistance );
                }
            }
            else
            {
                ProcessNode( node.GetNextNode( tracingRay.Origin ), tracingRay, maxDistance, traceDistance );
            }
        }

        protected virtual void ProcessLeaf( BspNode leaf, Ray tracingRay, float maxDistance, float traceDistance )
        {
            //Check ray against objects
            foreach ( MovableObject obj in leaf.Objects.Values )
            {
                // Skip this object if collision not enabled
                if ( ( obj.QueryFlags & queryMask ) == 0 )
                {
                    continue;
                }

                //Test object as bounding box
                IntersectResult result = tracingRay.Intersects( obj.GetWorldBoundingBox() );
                // if the result came back positive and intersection point is inside
                // the node, fire the event handler
                if ( result.Hit && result.Distance <= maxDistance )
                {
                    this.listener.OnQueryResult( obj, result.Distance + traceDistance );
                }
            }

            var boundedVolume = new PlaneBoundedVolume( PlaneSide.Positive );
            BspBrush intersectBrush = null;
            float intersectBrushDist = float.PositiveInfinity;

            if ( ( QueryTypeMask & (ulong)SceneQueryTypeMask.WorldGeometry ) != 0 )
            {
                // Check ray against brushes
                if ( ( QueryTypeMask & (ulong)SceneQueryTypeMask.WorldGeometry ) != 0 )
                {
                    for ( int brushPoint = 0; brushPoint < leaf.SolidBrushes.Length; brushPoint++ )
                    {
                        BspBrush brush = leaf.SolidBrushes[ brushPoint ];

                        if ( brush == null )
                        {
                            continue;
                        }

                        boundedVolume.planes = brush.Planes;

                        IntersectResult result = tracingRay.Intersects( boundedVolume );
                        // if the result came back positive and intersection point is inside
                        // the node, check if this brush is closer
                        if ( result.Hit && result.Distance <= maxDistance )
                        {
                            if ( result.Distance < intersectBrushDist )
                            {
                                intersectBrushDist = result.Distance;
                                intersectBrush = brush;
                            }
                        }
                    }

                    if ( intersectBrush != null )
                    {
                        this.listener.OnQueryResult( intersectBrush.Fragment, intersectBrushDist + traceDistance );
                        this.StopRayTracing = true;
                    }
                }
            }

            if ( intersectBrush != null )
            {
                this.listener.OnQueryResult( intersectBrush.Fragment, intersectBrushDist + traceDistance );
                this.StopRayTracing = true;
            }
        }

        #endregion Protected methods
    }

    /// <summary>
    ///		BSP specialisation of SphereRegionSceneQuery.
    /// </summary>
    public class BspSphereRegionSceneQuery : DefaultSphereRegionSceneQuery
    {
        #region Constructor

        public BspSphereRegionSceneQuery( SceneManager creator )
            : base( creator )
        {
            AddWorldFragmentType( WorldFragmentType.PlaneBoundedRegion );
        }

        #endregion Constructor

        protected List<MovableObject> foundIntersections = new List<MovableObject>();
        protected ISceneQueryListener listener;

        #region Public methods

        public override void Execute( ISceneQueryListener listener )
        {
            this.listener = listener;
            this.foundIntersections.Clear();
            ProcessNode( ( (BspSceneManager)creator ).Level.RootNode );
        }

        #endregion Public methods

        #region Protected methods

        protected virtual void ProcessNode( BspNode node )
        {
            if ( node.IsLeaf )
            {
                ProcessLeaf( node );
                return;
            }

            float distance = node.GetDistance( sphere.Center );

            if ( Utility.Abs( distance ) < sphere.Radius )
            {
                // Sphere crosses the plane, do both.
                ProcessNode( node.BackNode );
                ProcessNode( node.FrontNode );
            }
            else if ( distance < 0 )
            {
                // Do back.
                ProcessNode( node.BackNode );
            }
            else
            {
                // Do front.
                ProcessNode( node.FrontNode );
            }
        }

        protected virtual void ProcessLeaf( BspNode leaf )
        {
            //Check sphere against objects
            foreach ( MovableObject obj in leaf.Objects.Values )
            {
                // Skip this object if collision not enabled
                if ( ( obj.QueryFlags & queryMask ) == 0 )
                {
                    continue;
                }

                //Test object as bounding box
                if ( sphere.Intersects( obj.GetWorldBoundingBox() ) )
                {
                    if ( !this.foundIntersections.Contains( obj ) )
                    {
                        this.listener.OnQueryResult( obj );
                        this.foundIntersections.Add( obj );
                    }
                }
            }

            var boundedVolume = new PlaneBoundedVolume( PlaneSide.Positive );

            // Check ray against brushes
            for ( int brushPoint = 0; brushPoint < leaf.SolidBrushes.Length; brushPoint++ )
            {
                BspBrush brush = leaf.SolidBrushes[ brushPoint ];
                if ( brush == null )
                {
                    continue;
                }

                boundedVolume.planes = brush.Planes;
                if ( boundedVolume.Intersects( sphere ) )
                {
                    this.listener.OnQueryResult( brush.Fragment );
                }
            }
        }

        #endregion Protected methods
    }

    /// <summary>
    ///		Factory for the BspSceneManager.
    /// </summary>
    internal class BspSceneManagerFactory : SceneManagerFactory
    {
        #region Methods

        protected override void InitMetaData()
        {
            metaData.typeName = "BspSceneManager";
            metaData.description = "Scene manager for loading Quake3 .bsp files.";
            metaData.sceneTypeMask = SceneType.Interior;
            metaData.worldGeometrySupported = true;
        }

        public override SceneManager CreateInstance( string name )
        {
            return new BspSceneManager( name );
        }

        public override void DestroyInstance( SceneManager instance )
        {
            instance.ClearScene();
        }

        #endregion Methods
    }
}
