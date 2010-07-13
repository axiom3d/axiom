﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.SceneManagers.PortalConnected;
using System.IO;
using Axiom.Core;

namespace OctreeZone
{
	public class TerrainZonePageRow : List<TerrainZonePage> { }
	public class TerrainZonePage2D : List<TerrainZonePageRow> { }

	public class TerrainZone : OctreeZone
	{
		private const string TERRAIN_MATERIAL_NAME = "TerrainSceneManager/Terrain";
		/// The node to which all terrain tiles are attached
		PCZSceneNode mTerrainRoot;
		/// Terrain size, detail etc
		private TerrainZoneOptions mOptions = new TerrainZoneOptions();
		/// Should we use an externally-defined custom material?
		bool mUseCustomMaterial;
		/// The name of the custom material to use
		string mCustomMaterialName;
		/// The name of the world texture
		string mWorldTextureName;
		/// The name of the detail texture
		string mDetailTextureName;
		/// Are we using a named parameter to hook up LOD morph?
		bool mUseNamedParameterLodMorph;
		/// The name of the parameter to send the LOD morph to
		string mLodMorphParamName;
		/// The index of the parameter to send the LOD morph to
		int mLodMorphParamIndex;
		/// Whether paging is enabled, or whether a single page will be used
		bool mPagingEnabled;
		/// The number of pages to render outside the 'home' page
		ushort mLivePageMargin;
		/// The number of pages to keep loaded outside the 'home' page
		ushort mBufferedPageMargin;
		/// Grid of buffered pages
		TerrainZonePage2D mTerrainZonePages = new TerrainZonePage2D();
		//-- attributes to share across tiles
		/// Shared list of index buffers
		private TerrainBufferCache mIndexCache = new TerrainBufferCache();

		/// Shared array of IndexData (reuse indexes across tiles)
		private System.Collections.Hashtable mLevelIndex = Hashtable.Synchronized( new Hashtable() );
		//private List<KeyValuePair<uint, IndexData>> mLevelIndex = new List<KeyValuePair<uint, IndexData>>();
		/// Map of source type -> TerrainZonePageSource
		Dictionary<string, TerrainZonePageSource> mPageSources = new Dictionary<string, TerrainZonePageSource>();
		/// The currently active page source
		TerrainZonePageSource mActivePageSource;

		public TerrainZone( PCZSceneManager creator, string name )
			: base( creator, name )
		{
			mZoneTypeName = "ZoneType_Terrain";
			mUseCustomMaterial = false;
			mUseNamedParameterLodMorph = false;
			mLodMorphParamIndex = 3;
			mTerrainRoot = null;
			mActivePageSource = null;
			mPagingEnabled = false;
			mLivePageMargin = 0;
			mBufferedPageMargin = 0;
		}

		/// Terrain size, detail etc
		public TerrainZoneOptions Options
		{
			get { return mOptions; }
		}

		/// Shared array of IndexData (reuse indexes across tiles)
		public Hashtable LevelIndex
		{
			get { return mLevelIndex; }
		}

		/// Shared list of index buffers
		public TerrainBufferCache IndexCache
		{
			get { return mIndexCache; }
		}

		public int PageCount
		{
			get
			{
				return mTerrainZonePages.Count;
			}
		}

		public PCZSceneNode TerrainRootNode
		{
			get
			{
				return mTerrainRoot;
			}
		}

		public void shutdown()
		{
			// Make sure the indexes are destroyed during orderly shutdown
			// and not when statics are destroyed (may be too late)
			//IndexCache.shutdown();
			DestroyLevelIndexes();

			// Make sure we free up material (static)
			if ( null != Options.terrainMaterial )
			{
				//MaterialManager.Instance.Remove(Options.terrainMaterial);
				MaterialManager.Instance.NotifyResourceUnloaded( Options.terrainMaterial );
				Options.terrainMaterial = null;
			}

			// Shut down page source to free terrain pages
			if ( null != mActivePageSource )
			{
				mActivePageSource.Shutdown();
			}

		}
		//-------------------------------------------------------------------------
		~TerrainZone()
		{
		}

		/// Validates that the size picked for the terrain is acceptable
		protected static bool CheckSize( int s )
		{
			for ( int i = 0; i < 16; i++ )
			{
				if ( s == ( 1 << i ) + 1 )
					return true;
			}

			return false;

		}

		/** Set the enclosure node for this TerrainZone
			*/
		public override void SetEnclosureNode( PCZSceneNode node )
		{
			mEnclosureNode = node;
			if ( null != node )
			{
				// anchor the node to this zone
				node.AnchorToHomeZone( this );
				// make sure node world bounds are up to date
				//node._updateBounds();
				// DON'T resize the octree to the same size as the enclosure node bounding box
				// resize(node->_getWorldAABB());
				// EXPERIMENTAL - prevent terrain zone enclosure node from visiting other zones
				node.AllowToVisit = false;
			}
		}

		//-------------------------------------------------------------------------
		void LoadConfig( Stream stream )
		{
			/* Set up the options */
			ConfigFile config = new ConfigFile( "TerrainConfig" );
			string val;

			config.Load( stream );

			val = config.getSetting( "DetailTile" );
			if ( !string.IsNullOrEmpty( val ) )
				setDetailTextureRepeat( Convert.ToInt32( val ) );

			val = config.getSetting( "MaxMipMapLevel" );
			if ( !string.IsNullOrEmpty( val ) )
				setMaxGeoMipMapLevel( Convert.ToInt32( val ) );


			val = config.getSetting( "PageSize" );
			if ( !string.IsNullOrEmpty( val ) )
				setPageSize( Convert.ToInt32( val ) );
			else
				throw new AxiomException( "Missing option 'PageSize'. LoadConfig" );


			val = config.getSetting( "TileSize" );
			if ( !string.IsNullOrEmpty( val ) )
				setTileSize( Convert.ToInt32( val ) );
			else
				throw new AxiomException( "Missing option 'TileSize'. LoadConfig" );

			Vector3 v = Vector3.UnitScale;

			val = config.getSetting( "PageWorldX" );
			if ( !string.IsNullOrEmpty( val ) )
				v.x = (float)Convert.ToDouble( val );

			val = config.getSetting( "MaxHeight" );
			if ( !string.IsNullOrEmpty( val ) )
				v.y = (float)Convert.ToDouble( val );

			val = config.getSetting( "PageWorldZ" );
			if ( !string.IsNullOrEmpty( val ) )
				v.z = (float)Convert.ToDouble( val );

			// Scale x/z relative to pagesize
			v.x /= Options.pageSize - 1;
			v.z /= Options.pageSize - 1;
			setScale( v );

			val = config.getSetting( "MaxPixelError" );
			if ( !string.IsNullOrEmpty( val ) )
				setMaxPixelError( Convert.ToInt32( val ) );

			mDetailTextureName = config.getSetting( "DetailTexture" );

			mWorldTextureName = config.getSetting( "WorldTexture" );

			if ( config.getSetting( "VertexColours" ) == "yes" )
				Options.coloured = true;

			if ( config.getSetting( "VertexNormals" ) == "yes" )
				Options.lit = true;

			if ( config.getSetting( "UseTriStrips" ) == "yes" )
				SetUseTriStrips( true );

			if ( config.getSetting( "VertexProgramMorph" ) == "yes" )
				SetUseLODMorph( true );

			val = config.getSetting( "LODMorphStart" );
			if ( !string.IsNullOrEmpty( val ) )
				setLODMorphStart( (float)Convert.ToDouble( val ) );

			val = config.getSetting( "MaterialName" );
			if ( !string.IsNullOrEmpty( val ) )
				setCustomMaterial( val );

			val = config.getSetting( "MorphLODFactorParamName" );
			if ( !string.IsNullOrEmpty( val ) )
				setCustomMaterialMorphFactorParam( val );

			val = config.getSetting( "MorphLODFactorParamIndex" );
			if ( !string.IsNullOrEmpty( val ) )
				setCustomMaterialMorphFactorParam( Convert.ToInt32( val ) );

			// Now scan through the remaining settings, looking for any PageSource
			// prefixed items
			string pageSourceName = config.getSetting( "PageSource" );
			if ( pageSourceName == "" )
			{
				throw new AxiomException( "Missing option 'PageSource'. LoadConfig" );
			}

			TerrainZonePageSourceOptionList optlist = new TerrainZonePageSourceOptionList();

			foreach ( string[] s in config.GetEnumerator() )
			{
				string name = s[ 0 ];
				string value = s[ 1 ];
				if ( name != pageSourceName )
				{
					optlist.Add( name, value );
				}
			}
			// set the page source
			SelectPageSource( pageSourceName, optlist );
		}
		//-------------------------------------------------------------------------
		public void SetupTerrainMaterial()
		{
			if ( string.IsNullOrEmpty( mCustomMaterialName ) )
			{
				// define our own material
				Options.terrainMaterial = (Material)MaterialManager.Instance.GetByName( TERRAIN_MATERIAL_NAME );
				// Make unique terrain material name
				string s = mName + "/Terrain";
				Options.terrainMaterial = (Material)MaterialManager.Instance.GetByName( s );
				if ( null == Options.terrainMaterial )
				{
					Options.terrainMaterial = (Material)MaterialManager.Instance.Create(
						s,
						ResourceGroupManager.Instance.WorldResourceGroupName );

				}
				else
				{
					Options.terrainMaterial.GetTechnique( 0 ).GetPass( 0 ).RemoveAllTextureUnitStates();
				}

				Pass pass = Options.terrainMaterial.GetTechnique( 0 ).GetPass( 0 );

				if ( mWorldTextureName != "" )
				{
					pass.CreateTextureUnitState( mWorldTextureName, 0 );
				}
				if ( mDetailTextureName != "" )
				{
					pass.CreateTextureUnitState( mDetailTextureName, 1 );
				}

				Options.terrainMaterial.Lighting = Options.lit;

				if ( Options.lodMorph &&
					mPCZSM.TargetRenderSystem.HardwareCapabilities.HasCapability( Capabilities.VertexPrograms ) &&
					GpuProgramManager.Instance.GetByName( "Terrain/VertexMorph" ) == null )
				{
					// Create & assign LOD morphing vertex program
					String syntax;
					if ( GpuProgramManager.Instance.IsSyntaxSupported( "arbvp1" ) )
					{
						syntax = "arbvp1";
					}
					else
					{
						syntax = "vs_1_1";
					}

					// Get source, and take into account current fog mode
					FogMode fm = mPCZSM.FogMode;
					string source = new TerrainVertexProgram().getProgramSource( fm, syntax, false );

					GpuProgram prog = GpuProgramManager.Instance.CreateProgramFromString(
						"Terrain/VertexMorph", ResourceGroupManager.Instance.WorldResourceGroupName,
						source, GpuProgramType.Vertex, syntax );

					// Attach
					pass.SetVertexProgram( "Terrain/VertexMorph" );

					// Get params
					GpuProgramParameters paras = pass.VertexProgramParameters;

					// worldviewproj
					paras.SetAutoConstant( 0, GpuProgramParameters.AutoConstantType.WorldViewProjMatrix );
					// morph factor
					paras.SetAutoConstant( 4, GpuProgramParameters.AutoConstantType.Custom, TerrainZoneRenderable.MORPH_CUSTOM_PARAM_ID );
					// fog exp density(if relevant)
					if ( fm == FogMode.Exp || fm == FogMode.Exp2 )
					{
						paras.SetConstant( 5, new Vector3( mPCZSM.FogDensity, 0, 0 ) );
						// Override scene fog since otherwise it's applied twice
						// Set to linear and we derive [0,1] fog value in the shader
						pass.SetFog( true, FogMode.Linear, mPCZSM.FogColor, 0, 1, 0 );
					}

					// Also set shadow receiver program
					string source2 = new TerrainVertexProgram().getProgramSource( fm, syntax, true );

					prog = GpuProgramManager.Instance.CreateProgramFromString(
						"Terrain/VertexMorphShadowReceive",
						ResourceGroupManager.Instance.WorldResourceGroupName,
						source2, GpuProgramType.Vertex, syntax );
					pass.SetShadowReceiverVertexProgram( "Terrain/VertexMorphShadowReceive" );
					paras = pass.ShadowReceiverVertexProgramParameters;
					// worldviewproj
					paras.SetAutoConstant( 0, GpuProgramParameters.AutoConstantType.WorldViewProjMatrix );
					// world
					paras.SetAutoConstant( 4, GpuProgramParameters.AutoConstantType.WorldMatrix );
					// texture view / proj
					paras.SetAutoConstant( 8, GpuProgramParameters.AutoConstantType.TextureViewProjMatrix );
					// morph factor
					paras.SetAutoConstant( 12, GpuProgramParameters.AutoConstantType.Custom, TerrainZoneRenderable.MORPH_CUSTOM_PARAM_ID );


					// Set param index
					mLodMorphParamName = "";
					mLodMorphParamIndex = 4;
				}

				Options.terrainMaterial.Load();

			}
			else
			{
				// Custom material
				Options.terrainMaterial = (Material)MaterialManager.Instance.GetByName( mCustomMaterialName );
				Options.terrainMaterial.Load();

			}

			// now set up the linkage between vertex program and LOD morph param
			if ( Options.lodMorph )
			{
				Technique t = Options.terrainMaterial.GetBestTechnique();
				for ( ushort i = 0; i < t.PassCount; ++i )
				{
					Pass p = t.GetPass( i );
					if ( p.HasVertexProgram )
					{
						// we have to assume vertex program includes LOD morph capability
						GpuProgramParameters paras = p.VertexProgramParameters;
						// Check to see if custom param is already there
						//GpuProgramParameters::AutoConstantIterator aci = params->getAutoConstantIterator();
						bool found = false;
						foreach ( GpuProgramParameters.AutoConstantEntry ace in paras.AutoConstantList )
						{
							if ( ace.Type == GpuProgramParameters.AutoConstantType.Custom &&
								ace.Data == TerrainZoneRenderable.MORPH_CUSTOM_PARAM_ID )
							{
								found = true;
								break;
							}
						}
						if ( !found )
						{
							if ( mLodMorphParamName != "" )
							{
								paras.SetNamedAutoConstant( mLodMorphParamName,
									GpuProgramParameters.AutoConstantType.Custom, TerrainZoneRenderable.MORPH_CUSTOM_PARAM_ID );
							}
							else
							{
								paras.SetAutoConstant( mLodMorphParamIndex,
									GpuProgramParameters.AutoConstantType.Custom, TerrainZoneRenderable.MORPH_CUSTOM_PARAM_ID );
							}
						}

					}
				}
			}

		}
		//-------------------------------------------------------------------------
		public void SetupTerrainZonePages( PCZSceneNode parentNode )
		{

			//create a root terrain node.
			if ( null == mTerrainRoot )
			{
				mTerrainRoot = (PCZSceneNode)( parentNode.CreateChildSceneNode( this.Name + "_Node" ) );
				SetEnclosureNode( mTerrainRoot );
			}
			//setup the page array.
			ushort pageSlots = (ushort)( 1 + ( mBufferedPageMargin * 2 ) );
			ushort i, j;
			for ( i = 0; i < pageSlots; ++i )
			{
				mTerrainZonePages.Add( new TerrainZonePageRow() ); ;
				for ( j = 0; j < pageSlots; ++j )
				{
					mTerrainZonePages[ i ].Add( null );
				}
			}

			// If we're not paging, load immediate for convenience
			if ( mActivePageSource != null && !mPagingEnabled )
				mActivePageSource.RequestPage( 0, 0 );


		}
		//-------------------------------------------------------------------------
		public override void SetZoneGeometry( string filename, PCZSceneNode parentNode )
		{
			// try to open in the current folder first
			FileStream fs = null;
			try
			{
				fs = File.Open( filename, FileMode.Open );
			}
			catch { }

			if ( null != fs )
			{
				// Wrap as a stream
				SetZoneGeometry( fs, parentNode, null );
			}
			else
			{
				// otherwise try resource system
				Stream stream = ResourceGroupManager.Instance.OpenResource( filename,
																		   ResourceGroupManager.Instance.
																			   WorldResourceGroupName );

				SetZoneGeometry( stream, parentNode, null );
			}
		}

		//-------------------------------------------------------------------------
		public void SetZoneGeometry( Stream stream, PCZSceneNode parentNode, string typeName )
		{
			// Clear out any existing world resources (if not default)
			if ( ResourceGroupManager.Instance.WorldResourceGroupName !=
				ResourceGroupManager.DefaultResourceGroupName )
			{
				ResourceGroupManager.Instance.ClearResourceGroup(
					ResourceGroupManager.Instance.WorldResourceGroupName );
			}
			DestroyLevelIndexes();
			mTerrainZonePages.Clear();
			// Load the configuration
			LoadConfig( stream );
			InitLevelIndexes();


			SetupTerrainMaterial();

			SetupTerrainZonePages( parentNode );

			// Resize the octree allow for 1 page for now
			float max_x = Options.scale.x * Options.pageSize;
			float max_y = Options.scale.y;
			float max_z = Options.scale.z * Options.pageSize;
			Resize( new AxisAlignedBox( new Vector3( 0, 0, 0 ), new Vector3( max_x, max_y, max_z ) ) );

		}
		//-------------------------------------------------------------------------
		public void ClearZone()
		{
			mTerrainZonePages.Clear();
			DestroyLevelIndexes();
			// Octree has destroyed our root
			mTerrainRoot = null;
		}
		//-------------------------------------------------------------------------
		public override void NotifyBeginRenderScene()
		{
			// For now, no paging and expect immediate response
			if ( mTerrainZonePages.Count != 0 && mTerrainZonePages[ 0 ][ 0 ] == null )
			{
				mActivePageSource.RequestPage( 0, 0 );
			}

		}
		//-------------------------------------------------------------------------
		public void AttachPage( ushort pageX, ushort pageZ, TerrainZonePage page )
		{
			Debug.Assert( pageX == 0 && pageZ == 0, "Multiple pages not yet supported" );

			Debug.Assert( mTerrainZonePages[ pageX ][ pageZ ] == null, "Page at that index not yet expired!" );
			// Insert page into list
			mTerrainZonePages[ pageX ][ pageZ ] = page;
			// Attach page to terrain root
			if ( page.PageSceneNode.Parent != mTerrainRoot )
				mTerrainRoot.AddChild( page.PageSceneNode );

		}
		//-------------------------------------------------------------------------
		public float GetHeightAt( float x, float z )
		{


			Vector3 pt = new Vector3( x, 0f, z );

			TerrainZoneRenderable t = GetTerrainTile( pt );

			if ( null == t )
			{
				//  printf( "No tile found for point\n" );
				return -1;
			}

			float h = t.GetHeightAt( x, z );

			// printf( "Height is %f\n", h );
			return h;

		}
		//-------------------------------------------------------------------------
		public TerrainZonePage GetTerrainZonePage( Vector3 pt )
		{
			if ( mPagingEnabled )
			{
				// TODO
				return null;
			}
			else
			{
				// Single page
				if ( mTerrainZonePages.Count == 0 || mTerrainZonePages[ 0 ] == null )
					return null;
				return mTerrainZonePages[ 0 ][ 0 ];
			}
		}
		//-------------------------------------------------------------------------
		public TerrainZonePage GetTerrainZonePage( ushort x, ushort z )
		{
			if ( mPagingEnabled )
			{
				// TODO
				return null;
			}
			else
			{
				// Single page
				if ( mTerrainZonePages.Count == 0 || mTerrainZonePages[ 0 ] == null )
					return null;
				if ( x > Options.pageSize || z > Options.pageSize )
				{
					return mTerrainZonePages[ 0 ][ 0 ];
				}
				return mTerrainZonePages[ x ][ z ];
			}
		}

		//-------------------------------------------------------------------------
		public TerrainZoneRenderable GetTerrainTile( Vector3 pt )
		{
			TerrainZonePage tp = GetTerrainZonePage( pt );
			if ( null == tp )
				return null;
			else
				return tp.GetTerrainZoneTile( pt );
		}
		//-------------------------------------------------------------------------
		public bool intersectSegment( Vector3 start,
			 Vector3 end, ref Vector3 result )
		{
			TerrainZoneRenderable t = GetTerrainTile( start );

			if ( null == t )
			{
				result = new Vector3( -1, -1, -1 );
				return false;
			}

			return t.IntersectSegment( start, end, ref result );
		}
		//-------------------------------------------------------------------------
		void SetUseTriStrips( bool useStrips )
		{
			Options.useTriStrips = useStrips;
		}
		//-------------------------------------------------------------------------
		void SetUseLODMorph( bool morph )
		{
			// Set true only if vertex programs are supported
			Options.lodMorph = morph && mPCZSM.TargetRenderSystem.HardwareCapabilities.HasCapability( Capabilities.VertexPrograms );
		}
		//-------------------------------------------------------------------------
		void SetUseVertexNormals( bool useNormals )
		{
			Options.lit = useNormals;
		}
		//-------------------------------------------------------------------------
		void SetUseVertexColours( bool useColours )
		{
			Options.coloured = useColours;
		}
		//-------------------------------------------------------------------------
		void SetWorldTexture( string textureName )
		{
			mWorldTextureName = textureName;
		}
		//-------------------------------------------------------------------------
		void SetDetailTexture( string textureName )
		{
			mDetailTextureName = textureName;

		}
		//-------------------------------------------------------------------------
		public void setDetailTextureRepeat( int repeat )
		{
			Options.detailTile = repeat;
		}
		//-------------------------------------------------------------------------
		void setTileSize( int size )
		{
			Options.tileSize = size;
		}
		//-------------------------------------------------------------------------
		void setPageSize( int size )
		{
			Options.pageSize = size;
		}
		//-------------------------------------------------------------------------
		void setMaxPixelError( int pixelError )
		{
			Options.maxPixelError = pixelError;
		}
		//-------------------------------------------------------------------------
		void setScale( Vector3 scale )
		{
			Options.scale = scale;
		}
		//-------------------------------------------------------------------------
		void setMaxGeoMipMapLevel( int maxMip )
		{
			Options.maxGeoMipMapLevel = maxMip;
		}
		//-------------------------------------------------------------------------
		void setCustomMaterial( string materialName )
		{
			mCustomMaterialName = materialName;
			if ( materialName != "" )
				mUseCustomMaterial = true;
			else
				mUseCustomMaterial = false;
		}
		//-------------------------------------------------------------------------
		void setCustomMaterialMorphFactorParam( string paramName )
		{
			mUseNamedParameterLodMorph = true;
			mLodMorphParamName = paramName;

		}
		//-------------------------------------------------------------------------
		void setCustomMaterialMorphFactorParam( int paramIndex )
		{
			mUseNamedParameterLodMorph = false;
			mLodMorphParamIndex = paramIndex;
		}
		//-------------------------------------------------------------------------
		void setLODMorphStart( Real morphStart )
		{
			Options.lodMorphStart = morphStart;
		}
		//-------------------------------------------------------------------------
		public override void NotifyCameraCreated( Camera c )
		{
			// Set primary camera, if none
			if ( null == Options.primaryCamera )
				setPrimaryCamera( c );

			return;
		}
		//-------------------------------------------------------------------------
		void setPrimaryCamera( Camera cam )
		{
			Options.primaryCamera = cam;
		}
		//-------------------------------------------------------------------------
		public bool setOption( string name, object value )
		{
			if ( name == "PageSize" )
			{
				setPageSize( (int)value );
				return true;
			}
			else if ( name == "TileSize" )
			{
				setTileSize( (int)value );
				return true;
			}
			else if ( name == "PrimaryCamera" )
			{
				setPrimaryCamera( (Camera)value );
				return true;
			}
			else if ( name == "MaxMipMapLevel" )
			{
				setMaxGeoMipMapLevel( (int)value );
				return true;
			}
			else if ( name == "Scale" )
			{
				setScale( (Vector3)value );
				return true;
			}
			else if ( name == "MaxPixelError" )
			{
				setMaxPixelError( (int)value );
				return true;
			}
			else if ( name == "UseTriStrips" )
			{
				SetUseTriStrips( (bool)value );
				return true;
			}
			else if ( name == "VertexProgramMorph" )
			{
				SetUseLODMorph( (bool)value );
				return true;
			}
			else if ( name == "DetailTile" )
			{
				setDetailTextureRepeat( (int)value );
				return true;
			}
			else if ( name == "LodMorphStart" )
			{
				setLODMorphStart( (Real)value );
				return true;
			}
			else if ( name == "VertexNormals" )
			{
				SetUseVertexNormals( (bool)value );
				return true;
			}
			else if ( name == "VertexColours" )
			{
				SetUseVertexColours( (bool)value );
				return true;
			}
			else if ( name == "MorphLODFactorParamName" )
			{
				setCustomMaterialMorphFactorParam( value.ToString() );
				return true;
			}
			else if ( name == "MorphLODFactorParamIndex" )
			{
				setCustomMaterialMorphFactorParam( (int)value );
				return true;
			}
			else if ( name == "CustomMaterialName" )
			{
				setCustomMaterial( (string)value );
				return true;
			}
			else if ( name == "WorldTexture" )
			{
				SetWorldTexture( (string)value );
				return true;
			}
			else if ( name == "DetailTexture" )
			{
				SetDetailTexture( (string)value );
				return true;
			}
			else
			{
				return base.SetOption( name, value );
			}

			return false;
		}
		//-------------------------------------------------------------------------
		public void registerPageSource( string typeName, TerrainZonePageSource source )
		{
			if ( mPageSources.ContainsKey( typeName ) )
			{
				throw new AxiomException( "The page source " + typeName + " is already registered. registerPageSource" );
			}
			mPageSources.Add( typeName, source );
			LogManager.Instance.Write( "TerrainZone: Registered a new PageSource for type " + typeName );
		}
		//-------------------------------------------------------------------------
		public void SelectPageSource( string typeName, TerrainZonePageSourceOptionList optionList )
		{
			if ( !mPageSources.ContainsKey( typeName ) )
			{
				throw new AxiomException( "Cannot locate a TerrainZonePageSource for type " + typeName +
					". SelectPageSource" );
			}

			if ( null != mActivePageSource )
			{
				mActivePageSource.Shutdown();
			}
			mActivePageSource = mPageSources[ typeName ];
			mActivePageSource.Initialize( this, Options.tileSize, Options.pageSize,
				mPagingEnabled, optionList );

			LogManager.Instance.Write(
				"TerrainZone: Activated PageSource " + typeName );

		}
		//-------------------------------------------------------------------------
		public int getDetailTextureRepeat()
		{
			return Options.detailTile;
		}
		//-------------------------------------------------------------------------
		public int getTileSize()
		{
			return Options.tileSize;
		}
		//-------------------------------------------------------------------------
		public int getPageSize()
		{
			return Options.pageSize;
		}
		//-------------------------------------------------------------------------
		public int getMaxPixelError()
		{
			return Options.maxPixelError;
		}
		//-------------------------------------------------------------------------
		public Vector3 getScale()
		{
			return Options.scale;
		}
		//-------------------------------------------------------------------------
		public int getMaxGeoMipMapLevel()
		{
			return Options.maxGeoMipMapLevel;
		}
		//-----------------------------------------------------------------------
		public void InitLevelIndexes()
		{
			if ( LevelIndex.Count == 0 )
			{
				for ( int i = 0; i < 16; i++ )
				{
					//mLevelIndex.Add( new {IndexMap, MEMCATEGORY_GEOMETRY} );
				}

			}
		}
		//-----------------------------------------------------------------------
		public void DestroyLevelIndexes()
		{
			//for ( int i = 0; i < mLevelIndex.Count ; i++ )
			//{
			//    OGRE_DELETE_T(mLevelIndex[i], IndexMap, MEMCATEGORY_GEOMETRY);
			//}
			LevelIndex.Clear();
		}
		//-------------------------------------------------------------------------
		//-------------------------------------------------------------------------
		/*    RaySceneQuery*
				createRayQuery( Ray& ray, unsigned long mask)
			{
				TerrainRaySceneQuery *trsq = OGRE_NEW TerrainRaySceneQuery(this);
				trsq->setRay(ray);
				trsq->setQueryMask(mask);
				return trsq;
			}
			//-------------------------------------------------------------------------
			TerrainRaySceneQuery::TerrainRaySceneQuery(SceneManager* creator)
				:OctreeRaySceneQuery(creator)
			{
			  mSupportedWorldFragments.insert(SceneQuery::WFT_SINGLE_INTERSECTION);
			}
			//-------------------------------------------------------------------------
			TerrainRaySceneQuery::~TerrainRaySceneQuery()
			{
			}
			//-------------------------------------------------------------------------
			void TerrainRaySceneQuery::execute(RaySceneQueryListener* listener)
			{
				mWorldFrag.fragmentType = SceneQuery::WFT_SINGLE_INTERSECTION;

				 Vector3& dir = mRay.getDirection();
				 Vector3& origin = mRay.getOrigin();
				// Straight up / down?
				if (dir == Vector3::UNIT_Y || dir == Vector3::NEGATIVE_UNIT_Y)
				{
					Real height = static_cast<TerrainZone*>(mParentSceneMgr)->getHeightAt(
						origin.x, origin.z);
					if (height != -1 && (height <= origin.y && dir.y < 0) || (height >= origin.y && dir.y > 0))
					{
						mWorldFrag.singleIntersection.x = origin.x;
						mWorldFrag.singleIntersection.z = origin.z;
						mWorldFrag.singleIntersection.y = height;
						if (!listener->queryResult(&mWorldFrag,
							(mWorldFrag.singleIntersection - origin).length()))
							return;
					}
				}
				else
				{
					// Perform arbitrary query
					if (static_cast<TerrainZone*>(mParentSceneMgr)->intersectSegment(
						origin, origin + (dir * 100000), &mWorldFrag.singleIntersection))
					{
						if (!listener->queryResult(&mWorldFrag,
							(mWorldFrag.singleIntersection - origin).length()))
							return;
					}


				}
				OctreeRaySceneQuery::execute(listener);

			}
			*/
		//-------------------------------------------------------------------------
		Material getTerrainMaterial()
		{
			return Options.terrainMaterial;
		}
		//-------------------------------------------------------------------------
		//-------------------------------------------------------------------------
		public override void NotifyWorldGeometryRenderQueue( int qid )
		{
			foreach ( TerrainZonePageRow row in mTerrainZonePages )
			{
				foreach ( TerrainZonePage page in row )
				{
					if ( null != page )
					{
						page.SetRenderQueue( qid );
					}
				}
			}

		}

	}

	public class TerrainZoneFactory : PCZoneFactory
	{
		List<TerrainZonePageSource> mTerrainZonePageSources = new List<TerrainZonePageSource>();

		public TerrainZoneFactory( string typeName )
			: base( typeName )
		{
			factoryTypeName = typeName;
		}
		~TerrainZoneFactory()
		{
			mTerrainZonePageSources.Clear();
		}

		public override bool SupportsPCZoneType( string zoneType )
		{
			return zoneType == factoryTypeName;
		}

		public override PCZone CreatePCZone( PCZSceneManager pczsm, string zoneName )
		{
			//return new TerrainZone(pczsm, zoneName);
			TerrainZone tz = new TerrainZone( pczsm, zoneName );
			// Create & register default sources (one per zone)
			HeightmapTerrainZonePageSource ps = new HeightmapTerrainZonePageSource();
			mTerrainZonePageSources.Add( ps );
			tz.registerPageSource( "Heightmap", ps );
			return tz;
		}
	}

}