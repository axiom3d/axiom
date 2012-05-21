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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.SceneManagers.PortalConnected;
using System.IO;
using Axiom.Core;

#endregion Namespace Declarations

namespace OctreeZone
{
	public class TerrainZonePageRow : List<TerrainZonePage>
	{
	}

	public class TerrainZonePage2D : List<TerrainZonePageRow>
	{
	}

	public class TerrainZone : OctreeZone
	{
		private const string TERRAIN_MATERIAL_NAME = "TerrainSceneManager/Terrain";

		/// The node to which all terrain tiles are attached
		private PCZSceneNode mTerrainRoot;

		/// Terrain size, detail etc
		private readonly TerrainZoneOptions mOptions = new TerrainZoneOptions();

		/// Should we use an externally-defined custom material?
		private bool mUseCustomMaterial;

		/// The name of the custom material to use
		private string mCustomMaterialName;

		/// The name of the world texture
		private string mWorldTextureName;

		/// The name of the detail texture
		private string mDetailTextureName;

		/// Are we using a named parameter to hook up LOD morph?
		private bool mUseNamedParameterLodMorph;

		/// The name of the parameter to send the LOD morph to
		private string mLodMorphParamName;

		/// The index of the parameter to send the LOD morph to
		private int mLodMorphParamIndex;

		/// Whether paging is enabled, or whether a single page will be used
		private readonly bool mPagingEnabled;

		/// The number of pages to render outside the 'home' page
		private ushort mLivePageMargin;

		/// The number of pages to keep loaded outside the 'home' page
		private readonly ushort mBufferedPageMargin;

		/// Grid of buffered pages
		private readonly TerrainZonePage2D mTerrainZonePages = new TerrainZonePage2D();

		//-- attributes to share across tiles
		/// Shared list of index buffers
		private readonly TerrainBufferCache mIndexCache = new TerrainBufferCache();

		/// Shared array of IndexData (reuse indexes across tiles)
		private readonly System.Collections.Hashtable mLevelIndex = Hashtable.Synchronized( new Hashtable() );

		//private List<KeyValuePair<uint, IndexData>> mLevelIndex = new List<KeyValuePair<uint, IndexData>>();
		/// Map of source type -> TerrainZonePageSource
		private readonly Dictionary<string, TerrainZonePageSource> mPageSources =
			new Dictionary<string, TerrainZonePageSource>();

		/// The currently active page source
		private TerrainZonePageSource mActivePageSource;

		public TerrainZone( PCZSceneManager creator, string name )
			: base( creator, name )
		{
			mZoneTypeName = "ZoneType_Terrain";
			this.mUseCustomMaterial = false;
			this.mUseNamedParameterLodMorph = false;
			this.mLodMorphParamIndex = 3;
			this.mTerrainRoot = null;
			this.mActivePageSource = null;
			this.mPagingEnabled = false;
			this.mLivePageMargin = 0;
			this.mBufferedPageMargin = 0;
		}

		/// Terrain size, detail etc
		public TerrainZoneOptions Options
		{
			get
			{
				return this.mOptions;
			}
		}

		/// Shared array of IndexData (reuse indexes across tiles)
		public Hashtable LevelIndex
		{
			get
			{
				return this.mLevelIndex;
			}
		}

		/// Shared list of index buffers
		public TerrainBufferCache IndexCache
		{
			get
			{
				return this.mIndexCache;
			}
		}

		public int PageCount
		{
			get
			{
				return this.mTerrainZonePages.Count;
			}
		}

		public PCZSceneNode TerrainRootNode
		{
			get
			{
				return this.mTerrainRoot;
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
			if ( null != this.mActivePageSource )
			{
				this.mActivePageSource.Shutdown();
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
				{
					return true;
				}
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
		private void LoadConfig( Stream stream )
		{
			/* Set up the options */
			var config = new ConfigFile( "TerrainConfig" );
			string val;

			config.Load( stream );

			val = config.getSetting( "DetailTile" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				setDetailTextureRepeat( Convert.ToInt32( val ) );
			}

			val = config.getSetting( "MaxMipMapLevel" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				setMaxGeoMipMapLevel( Convert.ToInt32( val ) );
			}


			val = config.getSetting( "PageSize" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				setPageSize( Convert.ToInt32( val ) );
			}
			else
			{
				throw new AxiomException( "Missing option 'PageSize'. LoadConfig" );
			}


			val = config.getSetting( "TileSize" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				setTileSize( Convert.ToInt32( val ) );
			}
			else
			{
				throw new AxiomException( "Missing option 'TileSize'. LoadConfig" );
			}

			Vector3 v = Vector3.UnitScale;

			val = config.getSetting( "PageWorldX" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				v.x = (float)Convert.ToDouble( val );
			}

			val = config.getSetting( "MaxHeight" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				v.y = (float)Convert.ToDouble( val );
			}

			val = config.getSetting( "PageWorldZ" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				v.z = (float)Convert.ToDouble( val );
			}

			// Scale x/z relative to pagesize
			v.x /= Options.pageSize - 1;
			v.z /= Options.pageSize - 1;
			setScale( v );

			val = config.getSetting( "MaxPixelError" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				setMaxPixelError( Convert.ToInt32( val ) );
			}

			this.mDetailTextureName = config.getSetting( "DetailTexture" );

			this.mWorldTextureName = config.getSetting( "WorldTexture" );

			if ( config.getSetting( "VertexColours" ) == "yes" )
			{
				Options.coloured = true;
			}

			if ( config.getSetting( "VertexNormals" ) == "yes" )
			{
				Options.lit = true;
			}

			if ( config.getSetting( "UseTriStrips" ) == "yes" )
			{
				SetUseTriStrips( true );
			}

			if ( config.getSetting( "VertexProgramMorph" ) == "yes" )
			{
				SetUseLODMorph( true );
			}

			val = config.getSetting( "LODMorphStart" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				setLODMorphStart( (float)Convert.ToDouble( val ) );
			}

			val = config.getSetting( "MaterialName" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				setCustomMaterial( val );
			}

			val = config.getSetting( "MorphLODFactorParamName" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				setCustomMaterialMorphFactorParam( val );
			}

			val = config.getSetting( "MorphLODFactorParamIndex" );
			if ( !string.IsNullOrEmpty( val ) )
			{
				setCustomMaterialMorphFactorParam( Convert.ToInt32( val ) );
			}

			// Now scan through the remaining settings, looking for any PageSource
			// prefixed items
			string pageSourceName = config.getSetting( "PageSource" );
			if ( pageSourceName == "" )
			{
				throw new AxiomException( "Missing option 'PageSource'. LoadConfig" );
			}

			var optlist = new TerrainZonePageSourceOptionList();

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
			if ( string.IsNullOrEmpty( this.mCustomMaterialName ) )
			{
				// define our own material
				Options.terrainMaterial = (Material)MaterialManager.Instance.GetByName( TERRAIN_MATERIAL_NAME );
				// Make unique terrain material name
				string s = mName + "/Terrain";
				Options.terrainMaterial = (Material)MaterialManager.Instance.GetByName( s );
				if ( null == Options.terrainMaterial )
				{
					Options.terrainMaterial =
						(Material)MaterialManager.Instance.Create( s, ResourceGroupManager.Instance.WorldResourceGroupName );
				}
				else
				{
					Options.terrainMaterial.GetTechnique( 0 ).GetPass( 0 ).RemoveAllTextureUnitStates();
				}

				Pass pass = Options.terrainMaterial.GetTechnique( 0 ).GetPass( 0 );

				if ( this.mWorldTextureName != "" )
				{
					pass.CreateTextureUnitState( this.mWorldTextureName, 0 );
				}
				if ( this.mDetailTextureName != "" )
				{
					pass.CreateTextureUnitState( this.mDetailTextureName, 1 );
				}

				Options.terrainMaterial.Lighting = Options.lit;

				if ( Options.lodMorph && mPCZSM.TargetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms ) &&
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

					GpuProgram prog = GpuProgramManager.Instance.CreateProgramFromString( "Terrain/VertexMorph",
					                                                                      ResourceGroupManager.Instance.
					                                                                      	WorldResourceGroupName, source,
					                                                                      GpuProgramType.Vertex, syntax );

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

					prog = GpuProgramManager.Instance.CreateProgramFromString( "Terrain/VertexMorphShadowReceive",
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
					paras.SetAutoConstant( 12, GpuProgramParameters.AutoConstantType.Custom,
					                       TerrainZoneRenderable.MORPH_CUSTOM_PARAM_ID );


					// Set param index
					this.mLodMorphParamName = "";
					this.mLodMorphParamIndex = 4;
				}

				Options.terrainMaterial.Load();
			}
			else
			{
				// Custom material
				Options.terrainMaterial = (Material)MaterialManager.Instance.GetByName( this.mCustomMaterialName );
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
							if ( this.mLodMorphParamName != "" )
							{
								paras.SetNamedAutoConstant( this.mLodMorphParamName, GpuProgramParameters.AutoConstantType.Custom,
								                            TerrainZoneRenderable.MORPH_CUSTOM_PARAM_ID );
							}
							else
							{
								paras.SetAutoConstant( this.mLodMorphParamIndex, GpuProgramParameters.AutoConstantType.Custom,
								                       TerrainZoneRenderable.MORPH_CUSTOM_PARAM_ID );
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
			if ( null == this.mTerrainRoot )
			{
				this.mTerrainRoot = (PCZSceneNode)( parentNode.CreateChildSceneNode( Name + "_Node" ) );
				SetEnclosureNode( this.mTerrainRoot );
			}
			//setup the page array.
			var pageSlots = (ushort)( 1 + ( this.mBufferedPageMargin*2 ) );
			ushort i, j;
			for ( i = 0; i < pageSlots; ++i )
			{
				this.mTerrainZonePages.Add( new TerrainZonePageRow() );
				;
				for ( j = 0; j < pageSlots; ++j )
				{
					this.mTerrainZonePages[ i ].Add( null );
				}
			}

			// If we're not paging, load immediate for convenience
			if ( this.mActivePageSource != null && !this.mPagingEnabled )
			{
				this.mActivePageSource.RequestPage( 0, 0 );
			}
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
			catch
			{
			}

			if ( null != fs )
			{
				// Wrap as a stream
				SetZoneGeometry( fs, parentNode, null );
			}
			else
			{
				// otherwise try resource system
				Stream stream = ResourceGroupManager.Instance.OpenResource( filename,
				                                                            ResourceGroupManager.Instance.WorldResourceGroupName );

				SetZoneGeometry( stream, parentNode, null );
			}
		}

		//-------------------------------------------------------------------------
		public void SetZoneGeometry( Stream stream, PCZSceneNode parentNode, string typeName )
		{
			// Clear out any existing world resources (if not default)
			if ( ResourceGroupManager.Instance.WorldResourceGroupName != ResourceGroupManager.DefaultResourceGroupName )
			{
				ResourceGroupManager.Instance.ClearResourceGroup( ResourceGroupManager.Instance.WorldResourceGroupName );
			}
			DestroyLevelIndexes();
			this.mTerrainZonePages.Clear();
			// Load the configuration
			LoadConfig( stream );
			InitLevelIndexes();


			SetupTerrainMaterial();

			SetupTerrainZonePages( parentNode );

			// Resize the octree allow for 1 page for now
			float max_x = Options.scale.x*Options.pageSize;
			float max_y = Options.scale.y;
			float max_z = Options.scale.z*Options.pageSize;
			Resize( new AxisAlignedBox( new Vector3( 0, 0, 0 ), new Vector3( max_x, max_y, max_z ) ) );
		}

		//-------------------------------------------------------------------------
		public void ClearZone()
		{
			this.mTerrainZonePages.Clear();
			DestroyLevelIndexes();
			// Octree has destroyed our root
			this.mTerrainRoot = null;
		}

		//-------------------------------------------------------------------------
		public override void NotifyBeginRenderScene()
		{
			// For now, no paging and expect immediate response
			if ( this.mTerrainZonePages.Count != 0 && this.mTerrainZonePages[ 0 ][ 0 ] == null )
			{
				this.mActivePageSource.RequestPage( 0, 0 );
			}
		}

		//-------------------------------------------------------------------------
		public void AttachPage( ushort pageX, ushort pageZ, TerrainZonePage page )
		{
			Debug.Assert( pageX == 0 && pageZ == 0, "Multiple pages not yet supported" );

			Debug.Assert( this.mTerrainZonePages[ pageX ][ pageZ ] == null, "Page at that index not yet expired!" );
			// Insert page into list
			this.mTerrainZonePages[ pageX ][ pageZ ] = page;
			// Attach page to terrain root
			if ( page.PageSceneNode.Parent != this.mTerrainRoot )
			{
				this.mTerrainRoot.AddChild( page.PageSceneNode );
			}
		}

		//-------------------------------------------------------------------------
		public float GetHeightAt( float x, float z )
		{
			var pt = new Vector3( x, 0f, z );

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
			if ( this.mPagingEnabled )
			{
				// TODO
				return null;
			}
			else
			{
				// Single page
				if ( this.mTerrainZonePages.Count == 0 || this.mTerrainZonePages[ 0 ] == null )
				{
					return null;
				}
				return this.mTerrainZonePages[ 0 ][ 0 ];
			}
		}

		//-------------------------------------------------------------------------
		public TerrainZonePage GetTerrainZonePage( ushort x, ushort z )
		{
			if ( this.mPagingEnabled )
			{
				// TODO
				return null;
			}
			else
			{
				// Single page
				if ( this.mTerrainZonePages.Count == 0 || this.mTerrainZonePages[ 0 ] == null )
				{
					return null;
				}
				if ( x > Options.pageSize || z > Options.pageSize )
				{
					return this.mTerrainZonePages[ 0 ][ 0 ];
				}
				return this.mTerrainZonePages[ x ][ z ];
			}
		}

		//-------------------------------------------------------------------------
		public TerrainZoneRenderable GetTerrainTile( Vector3 pt )
		{
			TerrainZonePage tp = GetTerrainZonePage( pt );
			if ( null == tp )
			{
				return null;
			}
			else
			{
				return tp.GetTerrainZoneTile( pt );
			}
		}

		//-------------------------------------------------------------------------
		public bool intersectSegment( Vector3 start, Vector3 end, ref Vector3 result )
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
		private void SetUseTriStrips( bool useStrips )
		{
			Options.useTriStrips = useStrips;
		}

		//-------------------------------------------------------------------------
		private void SetUseLODMorph( bool morph )
		{
			// Set true only if vertex programs are supported
			Options.lodMorph = morph && mPCZSM.TargetRenderSystem.Capabilities.HasCapability( Capabilities.VertexPrograms );
		}

		//-------------------------------------------------------------------------
		private void SetUseVertexNormals( bool useNormals )
		{
			Options.lit = useNormals;
		}

		//-------------------------------------------------------------------------
		private void SetUseVertexColours( bool useColours )
		{
			Options.coloured = useColours;
		}

		//-------------------------------------------------------------------------
		private void SetWorldTexture( string textureName )
		{
			this.mWorldTextureName = textureName;
		}

		//-------------------------------------------------------------------------
		private void SetDetailTexture( string textureName )
		{
			this.mDetailTextureName = textureName;
		}

		//-------------------------------------------------------------------------
		public void setDetailTextureRepeat( int repeat )
		{
			Options.detailTile = repeat;
		}

		//-------------------------------------------------------------------------
		private void setTileSize( int size )
		{
			Options.tileSize = size;
		}

		//-------------------------------------------------------------------------
		private void setPageSize( int size )
		{
			Options.pageSize = size;
		}

		//-------------------------------------------------------------------------
		private void setMaxPixelError( int pixelError )
		{
			Options.maxPixelError = pixelError;
		}

		//-------------------------------------------------------------------------
		private void setScale( Vector3 scale )
		{
			Options.scale = scale;
		}

		//-------------------------------------------------------------------------
		private void setMaxGeoMipMapLevel( int maxMip )
		{
			Options.maxGeoMipMapLevel = maxMip;
		}

		//-------------------------------------------------------------------------
		private void setCustomMaterial( string materialName )
		{
			this.mCustomMaterialName = materialName;
			if ( materialName != "" )
			{
				this.mUseCustomMaterial = true;
			}
			else
			{
				this.mUseCustomMaterial = false;
			}
		}

		//-------------------------------------------------------------------------
		private void setCustomMaterialMorphFactorParam( string paramName )
		{
			this.mUseNamedParameterLodMorph = true;
			this.mLodMorphParamName = paramName;
		}

		//-------------------------------------------------------------------------
		private void setCustomMaterialMorphFactorParam( int paramIndex )
		{
			this.mUseNamedParameterLodMorph = false;
			this.mLodMorphParamIndex = paramIndex;
		}

		//-------------------------------------------------------------------------
		private void setLODMorphStart( Real morphStart )
		{
			Options.lodMorphStart = morphStart;
		}

		//-------------------------------------------------------------------------
		public override void NotifyCameraCreated( Camera c )
		{
			// Set primary camera, if none
			if ( null == Options.primaryCamera )
			{
				setPrimaryCamera( c );
			}

			return;
		}

		//-------------------------------------------------------------------------
		private void setPrimaryCamera( Camera cam )
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
			if ( this.mPageSources.ContainsKey( typeName ) )
			{
				throw new AxiomException( "The page source " + typeName + " is already registered. registerPageSource" );
			}
			this.mPageSources.Add( typeName, source );
			LogManager.Instance.Write( "TerrainZone: Registered a new PageSource for type " + typeName );
		}

		//-------------------------------------------------------------------------
		public void SelectPageSource( string typeName, TerrainZonePageSourceOptionList optionList )
		{
			if ( !this.mPageSources.ContainsKey( typeName ) )
			{
				throw new AxiomException( "Cannot locate a TerrainZonePageSource for type " + typeName + ". SelectPageSource" );
			}

			if ( null != this.mActivePageSource )
			{
				this.mActivePageSource.Shutdown();
			}
			this.mActivePageSource = this.mPageSources[ typeName ];
			this.mActivePageSource.Initialize( this, Options.tileSize, Options.pageSize, this.mPagingEnabled, optionList );

			LogManager.Instance.Write( "TerrainZone: Activated PageSource " + typeName );
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
		private Material getTerrainMaterial()
		{
			return Options.terrainMaterial;
		}

		//-------------------------------------------------------------------------
		//-------------------------------------------------------------------------
		public override void NotifyWorldGeometryRenderQueue( int qid )
		{
			foreach ( TerrainZonePageRow row in this.mTerrainZonePages )
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
		private readonly List<TerrainZonePageSource> mTerrainZonePageSources = new List<TerrainZonePageSource>();

		public TerrainZoneFactory( string typeName )
			: base( typeName )
		{
			factoryTypeName = typeName;
		}

		~TerrainZoneFactory()
		{
			this.mTerrainZonePageSources.Clear();
		}

		public override bool SupportsPCZoneType( string zoneType )
		{
			return zoneType == factoryTypeName;
		}

		public override PCZone CreatePCZone( PCZSceneManager pczsm, string zoneName )
		{
			//return new TerrainZone(pczsm, zoneName);
			var tz = new TerrainZone( pczsm, zoneName );
			// Create & register default sources (one per zone)
			var ps = new HeightmapTerrainZonePageSource();
			this.mTerrainZonePageSources.Add( ps );
			tz.registerPageSource( "Heightmap", ps );
			return tz;
		}
	}
}