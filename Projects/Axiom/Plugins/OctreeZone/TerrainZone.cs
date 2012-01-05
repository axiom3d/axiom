#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
//     <id value="$Id:$"/>
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

public class TerrainZonePageRow : List<TerrainZonePage> {}

public class TerrainZonePage2D : List<TerrainZonePageRow> {}

public class TerrainZone : OctreeZone.OctreeZone
{
	/// <summary>
	/// name generator
	/// </summary>
	private static NameGenerator<TerrainZone> _nameGenerator = new NameGenerator<TerrainZone>( "TerrainZone" );

	private const string _terrainMaterialName = "TerrainSceneManager/Terrain";

	/// The node to which all terrain tiles are attached
	private PCZSceneNode _terrainRoot = null;

	/// Terrain size, detail etc
	private TerrainZoneOptions _options = new TerrainZoneOptions();

	/// Should we use an externally-defined custom material?
	private bool _useCustomMaterial = false;

	/// The name of the custom material to use
	private string _customMaterialName = "";

	/// The name of the world texture
	private string _worldTextureName = "";

	/// The name of the detail texture
	private string _detailTextureName = "";

	/// Are we using a named parameter to hook up LOD morph?
	private bool _useNamedParameterLodMorph = false;

	/// The name of the parameter to send the LOD morph to
	private string _lodMorphParamName = "";

	/// The index of the parameter to send the LOD morph to
	private int _lodMorphParamIndex = 3;

	/// Whether paging is enabled, or whether a single page will be used
	private bool _pagingEnabled = false;

	/// The number of pages to render outside the 'home' page
	private ushort _livePageMargin = 0;

	/// The number of pages to keep loaded outside the 'home' page
	private ushort _bufferedPageMargin = 0;

	/// Grid of buffered pages
	private TerrainZonePage2D _terrainZonePages = new TerrainZonePage2D();

	//-- attributes to share across tiles
	/// Shared list of index buffers
	private TerrainBufferCache _indexCache = new TerrainBufferCache();

	/// Shared array of IndexData (reuse indexes across tiles)
	private System.Collections.Hashtable _levelIndex = Hashtable.Synchronized( new Hashtable() );

	//private List<KeyValuePair<uint, IndexData>> mLevelIndex = new List<KeyValuePair<uint, IndexData>>();
	/// Map of source type -> TerrainZonePageSource
	private Dictionary<string, TerrainZonePageSource> _pageSources = new Dictionary<string, TerrainZonePageSource>();

	/// The currently active page source
	private TerrainZonePageSource _activePageSource = null;

	/// <summary>
	/// Default constructor
	/// </summary>
	public TerrainZone( PCZSceneManager creator )
		: this( creator, _nameGenerator.GetNextUniqueName() ) {}

	public TerrainZone( PCZSceneManager creator, string name )
		: base( creator, name )
	{
		ZoneTypeName = "ZoneType_Terrain";
	}

	/// Terrain size, detail etc
	public TerrainZoneOptions Options { get { return _options; } }

	/// Shared array of IndexData (reuse indexes across tiles)
	public Hashtable LevelIndex { get { return _levelIndex; } }

	/// Shared list of index buffers
	public TerrainBufferCache IndexCache { get { return _indexCache; } }

	public int PageCount { get { return _terrainZonePages.Count; } }

	public PCZSceneNode TerrainRootNode { get { return _terrainRoot; } }

	public void shutdown()
	{
		// Make sure the indexes are destroyed during orderly shutdown
		// and not when statics are destroyed (may be too late)
		//IndexCache.shutdown();
		DestroyLevelIndexes();

		// Make sure we free up material (static)
		if( null != Options.terrainMaterial )
		{
			//MaterialManager.Instance.Remove(Options.terrainMaterial);
			MaterialManager.Instance.NotifyResourceUnloaded( Options.terrainMaterial );
			Options.terrainMaterial = null;
		}

		// Shut down page source to free terrain pages
		if( null != _activePageSource )
		{
			_activePageSource.Shutdown();
		}
	}

	//-------------------------------------------------------------------------
	~TerrainZone() {}

	/// Validates that the size picked for the terrain is acceptable
	protected static bool CheckSize( int s )
	{
		for( int i = 0; i < 16; i++ )
		{
			if( s == ( 1 << i ) + 1 )
			{
				return true;
			}
		}

		return false;
	}

	/** Set the enclosure node for this TerrainZone
		*/

	public override PCZSceneNode EnclosureNode
	{
		get { return base.EnclosureNode; }
		set
		{
			base.EnclosureNode = value;
			if( null != value )
			{
				// anchor the node to this zone
				base.EnclosureNode.AnchorToHomeZone( this );
				// make sure node world bounds are up to date
				//node._updateBounds();
				// DON'T resize the octree to the same size as the enclosure node bounding box
				// resize(node->_getWorldAABB());
				// EXPERIMENTAL - prevent terrain zone enclosure node from visiting other zones
				base.EnclosureNode.AllowToVisit = false;
			}
		}
	}

	//-------------------------------------------------------------------------
	private void LoadConfig( Stream stream )
	{
		/* Set up the options */
		ConfigFile config = new ConfigFile( "TerrainConfig" );
		string val;

		config.Load( stream );

		val = config.GetSetting( "DetailTile" );
		if( !string.IsNullOrEmpty( val ) )
		{
			setDetailTextureRepeat( Convert.ToInt32( val ) );
		}

		val = config.GetSetting( "MaxMipMapLevel" );
		if( !string.IsNullOrEmpty( val ) )
		{
			setMaxGeoMipMapLevel( Convert.ToInt32( val ) );
		}

		val = config.GetSetting( "PageSize" );
		if( !string.IsNullOrEmpty( val ) )
		{
			setPageSize( Convert.ToInt32( val ) );
		}
		else
		{
			throw new AxiomException( "Missing option 'PageSize'. LoadConfig" );
		}

		val = config.GetSetting( "TileSize" );
		if( !string.IsNullOrEmpty( val ) )
		{
			setTileSize( Convert.ToInt32( val ) );
		}
		else
		{
			throw new AxiomException( "Missing option 'TileSize'. LoadConfig" );
		}

		Vector3 v = Vector3.UnitScale;

		val = config.GetSetting( "PageWorldX" );
		if( !string.IsNullOrEmpty( val ) )
		{
			v.x = (float)Convert.ToDouble( val );
		}

		val = config.GetSetting( "MaxHeight" );
		if( !string.IsNullOrEmpty( val ) )
		{
			v.y = (float)Convert.ToDouble( val );
		}

		val = config.GetSetting( "PageWorldZ" );
		if( !string.IsNullOrEmpty( val ) )
		{
			v.z = (float)Convert.ToDouble( val );
		}

		// Scale x/z relative to pagesize
		v.x /= Options.PageSize - 1;
		v.z /= Options.PageSize - 1;
		setScale( v );

		val = config.GetSetting( "MaxPixelError" );
		if( !string.IsNullOrEmpty( val ) )
		{
			setMaxPixelError( Convert.ToInt32( val ) );
		}

		_detailTextureName = config.GetSetting( "DetailTexture" );

		_worldTextureName = config.GetSetting( "WorldTexture" );

		if( config.GetSetting( "VertexColours" ) == "yes" )
		{
			Options.Coloured = true;
		}

		if( config.GetSetting( "VertexNormals" ) == "yes" )
		{
			Options.UseDynamicLighting = true;
		}

		if( config.GetSetting( "UseTriStrips" ) == "yes" )
		{
			SetUseTriStrips( true );
		}

		if( config.GetSetting( "VertexProgramMorph" ) == "yes" )
		{
			SetUseLODMorph( true );
		}

		val = config.GetSetting( "LODMorphStart" );
		if( !string.IsNullOrEmpty( val ) )
		{
			setLODMorphStart( (float)Convert.ToDouble( val ) );
		}

		val = config.GetSetting( "MaterialName" );
		if( !string.IsNullOrEmpty( val ) )
		{
			setCustomMaterial( val );
		}

		val = config.GetSetting( "MorphLODFactorParamName" );
		if( !string.IsNullOrEmpty( val ) )
		{
			setCustomMaterialMorphFactorParam( val );
		}

		val = config.GetSetting( "MorphLODFactorParamIndex" );
		if( !string.IsNullOrEmpty( val ) )
		{
			setCustomMaterialMorphFactorParam( Convert.ToInt32( val ) );
		}

		// Now scan through the remaining settings, looking for any PageSource
		// prefixed items
		string pageSourceName = config.GetSetting( "PageSource" );
		if( pageSourceName == "" )
		{
			throw new AxiomException( "Missing option 'PageSource'. LoadConfig" );
		}

		TerrainZonePageSourceOptionList optlist = new TerrainZonePageSourceOptionList();

		foreach( string[] s in config.GetEnumerator() )
		{
			string name = s[ 0 ];
			string value = s[ 1 ];
			if( name != pageSourceName )
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
		if( string.IsNullOrEmpty( _customMaterialName ) )
		{
			// define our own material
			Options.terrainMaterial = (Material)MaterialManager.Instance.GetByName( _terrainMaterialName );
			// Make unique terrain material name
			string s = Name + "/Terrain";
			Options.terrainMaterial = (Material)MaterialManager.Instance.GetByName( s );
			if( null == Options.terrainMaterial )
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

			if( _worldTextureName != "" )
			{
				pass.CreateTextureUnitState( _worldTextureName, 0 );
			}
			if( _detailTextureName != "" )
			{
				pass.CreateTextureUnitState( _detailTextureName, 1 );
			}

			Options.terrainMaterial.Lighting = Options.UseDynamicLighting;

			if( Options.LodMorph &&
			    PCZSM.TargetRenderSystem.HardwareCapabilities.HasCapability( Capabilities.VertexPrograms ) &&
			    GpuProgramManager.Instance.GetByName( "Terrain/VertexMorph" ) == null )
			{
				// Create & assign LOD morphing vertex program
				String syntax;
				if( GpuProgramManager.Instance.IsSyntaxSupported( "arbvp1" ) )
				{
					syntax = "arbvp1";
				}
				else
				{
					syntax = "vs_1_1";
				}

				// Get source, and take into account current fog mode
				FogMode fm = PCZSM.FogMode;
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
				if( fm == FogMode.Exp || fm == FogMode.Exp2 )
				{
					paras.SetConstant( 5, new Vector3( PCZSM.FogDensity, 0, 0 ) );
					// Override scene fog since otherwise it's applied twice
					// Set to linear and we derive [0,1] fog value in the shader
					pass.SetFog( true, FogMode.Linear, PCZSM.FogColor, 0, 1, 0 );
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
				_lodMorphParamName = "";
				_lodMorphParamIndex = 4;
			}

			Options.terrainMaterial.Load();
		}
		else
		{
			// Custom material
			Options.terrainMaterial = (Material)MaterialManager.Instance.GetByName( _customMaterialName );
			Options.terrainMaterial.Load();
		}

		// now set up the linkage between vertex program and LOD morph param
		if( Options.LodMorph )
		{
			Technique t = Options.terrainMaterial.GetBestTechnique();
			for( ushort i = 0; i < t.PassCount; ++i )
			{
				Pass p = t.GetPass( i );
				if( p.HasVertexProgram )
				{
					// we have to assume vertex program includes LOD morph capability
					GpuProgramParameters paras = p.VertexProgramParameters;
					// Check to see if custom param is already there
					//GpuProgramParameters::AutoConstantIterator aci = params->getAutoConstantIterator();
					bool found = false;
					foreach( GpuProgramParameters.AutoConstantEntry ace in paras.AutoConstantList )
					{
						if( ace.Type == GpuProgramParameters.AutoConstantType.Custom &&
						    ace.Data == TerrainZoneRenderable.MORPH_CUSTOM_PARAM_ID )
						{
							found = true;
							break;
						}
					}
					if( !found )
					{
						if( _lodMorphParamName != "" )
						{
							paras.SetNamedAutoConstant( _lodMorphParamName,
							                            GpuProgramParameters.AutoConstantType.Custom, TerrainZoneRenderable.MORPH_CUSTOM_PARAM_ID );
						}
						else
						{
							paras.SetAutoConstant( _lodMorphParamIndex,
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
		if( null == _terrainRoot )
		{
			_terrainRoot = (PCZSceneNode)( parentNode.CreateChildSceneNode( this.Name + "_Node" ) );
			EnclosureNode = _terrainRoot;
		}
		//setup the page array.
		ushort pageSlots = (ushort)( 1 + ( _bufferedPageMargin * 2 ) );
		ushort i, j;
		for( i = 0; i < pageSlots; ++i )
		{
			_terrainZonePages.Add( new TerrainZonePageRow() );
			;
			for( j = 0; j < pageSlots; ++j )
			{
				_terrainZonePages[ i ].Add( null );
			}
		}

		// If we're not paging, load immediate for convenience
		if( _activePageSource != null && !_pagingEnabled )
		{
			_activePageSource.RequestPage( 0, 0 );
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
		catch {}

		if( null != fs )
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
		if( ResourceGroupManager.Instance.WorldResourceGroupName !=
		    ResourceGroupManager.DefaultResourceGroupName )
		{
			ResourceGroupManager.Instance.ClearResourceGroup(
			                                                 ResourceGroupManager.Instance.WorldResourceGroupName );
		}
		DestroyLevelIndexes();
		_terrainZonePages.Clear();
		// Load the configuration
		LoadConfig( stream );
		InitLevelIndexes();

		SetupTerrainMaterial();

		SetupTerrainZonePages( parentNode );

		// Resize the octree allow for 1 page for now
		float max_x = Options.Scale.x * Options.PageSize;
		float max_y = Options.Scale.y;
		float max_z = Options.Scale.z * Options.PageSize;
		Resize( new AxisAlignedBox( new Vector3( 0, 0, 0 ), new Vector3( max_x, max_y, max_z ) ) );
	}

	//-------------------------------------------------------------------------
	public void ClearZone()
	{
		_terrainZonePages.Clear();
		DestroyLevelIndexes();
		// Octree has destroyed our root
		_terrainRoot = null;
	}

	//-------------------------------------------------------------------------
	public override void NotifyBeginRenderScene()
	{
		// For now, no paging and expect immediate response
		if( _terrainZonePages.Count != 0 && _terrainZonePages[ 0 ][ 0 ] == null )
		{
			_activePageSource.RequestPage( 0, 0 );
		}
	}

	//-------------------------------------------------------------------------
	public void AttachPage( ushort pageX, ushort pageZ, TerrainZonePage page )
	{
		Debug.Assert( pageX == 0 && pageZ == 0, "Multiple pages not yet supported" );

		Debug.Assert( _terrainZonePages[ pageX ][ pageZ ] == null, "Page at that index not yet expired!" );
		// Insert page into list
		_terrainZonePages[ pageX ][ pageZ ] = page;
		// Attach page to terrain root
		if( page.PageSceneNode.Parent != _terrainRoot )
		{
			_terrainRoot.AddChild( page.PageSceneNode );
		}
	}

	//-------------------------------------------------------------------------
	public float GetHeightAt( float x, float z )
	{
		Vector3 pt = new Vector3( x, 0f, z );

		TerrainZoneRenderable t = GetTerrainTile( pt );

		if( null == t )
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
		if( _pagingEnabled )
		{
			// TODO
			return null;
		}
		else
		{
			// Single page
			if( _terrainZonePages.Count == 0 || _terrainZonePages[ 0 ] == null )
			{
				return null;
			}
			return _terrainZonePages[ 0 ][ 0 ];
		}
	}

	//-------------------------------------------------------------------------
	public TerrainZonePage GetTerrainZonePage( ushort x, ushort z )
	{
		if( _pagingEnabled )
		{
			// TODO
			return null;
		}
		else
		{
			// Single page
			if( _terrainZonePages.Count == 0 || _terrainZonePages[ 0 ] == null )
			{
				return null;
			}
			if( x > Options.PageSize || z > Options.PageSize )
			{
				return _terrainZonePages[ 0 ][ 0 ];
			}
			return _terrainZonePages[ x ][ z ];
		}
	}

	//-------------------------------------------------------------------------
	public TerrainZoneRenderable GetTerrainTile( Vector3 pt )
	{
		TerrainZonePage tp = GetTerrainZonePage( pt );
		if( null == tp )
		{
			return null;
		}
		else
		{
			return tp.GetTerrainZoneTile( pt );
		}
	}

	//-------------------------------------------------------------------------
	public bool intersectSegment( Vector3 start,
	                              Vector3 end, ref Vector3 result )
	{
		TerrainZoneRenderable t = GetTerrainTile( start );

		if( null == t )
		{
			result = new Vector3( -1, -1, -1 );
			return false;
		}

		return t.IntersectSegment( start, end, ref result );
	}

	//-------------------------------------------------------------------------
	private void SetUseTriStrips( bool useStrips )
	{
		Options.UseTriStrips = useStrips;
	}

	//-------------------------------------------------------------------------
	private void SetUseLODMorph( bool morph )
	{
		// Set true only if vertex programs are supported
		Options.LodMorph = morph && PCZSM.TargetRenderSystem.HardwareCapabilities.HasCapability( Capabilities.VertexPrograms );
	}

	//-------------------------------------------------------------------------
	private void SetUseVertexNormals( bool useNormals )
	{
		Options.UseDynamicLighting = useNormals;
	}

	//-------------------------------------------------------------------------
	private void SetUseVertexColours( bool useColours )
	{
		Options.Coloured = useColours;
	}

	//-------------------------------------------------------------------------
	private void SetWorldTexture( string textureName )
	{
		_worldTextureName = textureName;
	}

	//-------------------------------------------------------------------------
	private void SetDetailTexture( string textureName )
	{
		_detailTextureName = textureName;
	}

	//-------------------------------------------------------------------------
	public void setDetailTextureRepeat( int repeat )
	{
		Options.DetailTile = repeat;
	}

	//-------------------------------------------------------------------------
	private void setTileSize( int size )
	{
		Options.TileSize = size;
	}

	//-------------------------------------------------------------------------
	private void setPageSize( int size )
	{
		Options.PageSize = size;
	}

	//-------------------------------------------------------------------------
	private void setMaxPixelError( int pixelError )
	{
		Options.MaxPixelError = pixelError;
	}

	//-------------------------------------------------------------------------
	private void setScale( Vector3 scale )
	{
		Options.Scale = scale;
	}

	//-------------------------------------------------------------------------
	private void setMaxGeoMipMapLevel( int maxMip )
	{
		Options.MaxGeoMipMapLevel = maxMip;
	}

	//-------------------------------------------------------------------------
	private void setCustomMaterial( string materialName )
	{
		_customMaterialName = materialName;
		if( materialName != "" )
		{
			_useCustomMaterial = true;
		}
		else
		{
			_useCustomMaterial = false;
		}
	}

	//-------------------------------------------------------------------------
	private void setCustomMaterialMorphFactorParam( string paramName )
	{
		_useNamedParameterLodMorph = true;
		_lodMorphParamName = paramName;
	}

	//-------------------------------------------------------------------------
	private void setCustomMaterialMorphFactorParam( int paramIndex )
	{
		_useNamedParameterLodMorph = false;
		_lodMorphParamIndex = paramIndex;
	}

	//-------------------------------------------------------------------------
	private void setLODMorphStart( Real morphStart )
	{
		Options.LodMorphStart = morphStart;
	}

	//-------------------------------------------------------------------------
	public override void NotifyCameraCreated( Camera c )
	{
		// Set primary camera, if none
		if( null == Options.PrimaryCamera )
		{
			setPrimaryCamera( c );
		}

		return;
	}

	//-------------------------------------------------------------------------
	private void setPrimaryCamera( Camera cam )
	{
		Options.PrimaryCamera = cam;
	}

	//-------------------------------------------------------------------------
	public bool setOption( string name, object value )
	{
		if( name == "PageSize" )
		{
			setPageSize( (int)value );
			return true;
		}
		else if( name == "TileSize" )
		{
			setTileSize( (int)value );
			return true;
		}
		else if( name == "PrimaryCamera" )
		{
			setPrimaryCamera( (Camera)value );
			return true;
		}
		else if( name == "MaxMipMapLevel" )
		{
			setMaxGeoMipMapLevel( (int)value );
			return true;
		}
		else if( name == "Scale" )
		{
			setScale( (Vector3)value );
			return true;
		}
		else if( name == "MaxPixelError" )
		{
			setMaxPixelError( (int)value );
			return true;
		}
		else if( name == "UseTriStrips" )
		{
			SetUseTriStrips( (bool)value );
			return true;
		}
		else if( name == "VertexProgramMorph" )
		{
			SetUseLODMorph( (bool)value );
			return true;
		}
		else if( name == "DetailTile" )
		{
			setDetailTextureRepeat( (int)value );
			return true;
		}
		else if( name == "LodMorphStart" )
		{
			setLODMorphStart( (Real)value );
			return true;
		}
		else if( name == "VertexNormals" )
		{
			SetUseVertexNormals( (bool)value );
			return true;
		}
		else if( name == "VertexColours" )
		{
			SetUseVertexColours( (bool)value );
			return true;
		}
		else if( name == "MorphLODFactorParamName" )
		{
			setCustomMaterialMorphFactorParam( value.ToString() );
			return true;
		}
		else if( name == "MorphLODFactorParamIndex" )
		{
			setCustomMaterialMorphFactorParam( (int)value );
			return true;
		}
		else if( name == "CustomMaterialName" )
		{
			setCustomMaterial( (string)value );
			return true;
		}
		else if( name == "WorldTexture" )
		{
			SetWorldTexture( (string)value );
			return true;
		}
		else if( name == "DetailTexture" )
		{
			SetDetailTexture( (string)value );
			return true;
		}
		else
		{
			return base.SetOption( name, value );
		}
	}

	//-------------------------------------------------------------------------
	public void registerPageSource( string typeName, TerrainZonePageSource source )
	{
		if( _pageSources.ContainsKey( typeName ) )
		{
			throw new AxiomException( "The page source " + typeName + " is already registered. registerPageSource" );
		}
		_pageSources.Add( typeName, source );
		LogManager.Instance.Write( "TerrainZone: Registered a new PageSource for type " + typeName );
	}

	//-------------------------------------------------------------------------
	public void SelectPageSource( string typeName, TerrainZonePageSourceOptionList optionList )
	{
		if( !_pageSources.ContainsKey( typeName ) )
		{
			throw new AxiomException( "Cannot locate a TerrainZonePageSource for type " + typeName +
			                          ". SelectPageSource" );
		}

		if( null != _activePageSource )
		{
			_activePageSource.Shutdown();
		}
		_activePageSource = _pageSources[ typeName ];
		_activePageSource.Initialize( this, Options.TileSize, Options.PageSize,
		                              _pagingEnabled, optionList );

		LogManager.Instance.Write(
		                          "TerrainZone: Activated PageSource " + typeName );
	}

	//-------------------------------------------------------------------------
	public int getDetailTextureRepeat()
	{
		return Options.DetailTile;
	}

	//-------------------------------------------------------------------------
	public int getTileSize()
	{
		return Options.TileSize;
	}

	//-------------------------------------------------------------------------
	public int getPageSize()
	{
		return Options.PageSize;
	}

	//-------------------------------------------------------------------------
	public int getMaxPixelError()
	{
		return Options.MaxPixelError;
	}

	//-------------------------------------------------------------------------
	public Vector3 getScale()
	{
		return Options.Scale;
	}

	//-------------------------------------------------------------------------
	public int getMaxGeoMipMapLevel()
	{
		return Options.MaxGeoMipMapLevel;
	}

	//-----------------------------------------------------------------------
	public void InitLevelIndexes()
	{
		if( LevelIndex.Count == 0 )
		{
			for( int i = 0; i < 16; i++ )
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
	public override void NotifyWorldGeometryRenderQueue( RenderQueueGroupID qid )
	{
		foreach( TerrainZonePageRow row in _terrainZonePages )
		{
			foreach( TerrainZonePage page in row )
			{
				if( null != page )
				{
					page.SetRenderQueue( qid );
				}
			}
		}
	}
}

public class TerrainZoneFactory : PCZoneFactory
{
	private List<TerrainZonePageSource> mTerrainZonePageSources = new List<TerrainZonePageSource>();

	public TerrainZoneFactory( string typeName )
		: base( typeName )
	{
		_factoryTypeName = typeName;
	}

	~TerrainZoneFactory()
	{
		mTerrainZonePageSources.Clear();
	}

	public override bool SupportsPCZoneType( string zoneType )
	{
		return zoneType == _factoryTypeName;
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
