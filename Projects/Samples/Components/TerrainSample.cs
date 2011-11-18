#region MIT/X11 License
//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

using System.Collections.Generic;
using Axiom.Math;
using Axiom.Core;
using Axiom.Components.Terrain;
using Axiom.Media;
using Axiom.Graphics;

namespace Axiom.Samples.Components
{
	/// <summary>
	/// 
	/// </summary>
	enum Mode : int
	{
		Normal = 0,
		EditHeight = 1,
		EditBlend = 2,
		Count = 3
	}

	/// <summary>
	/// 
	/// </summary>
	enum ShadowMode : int
	{
		None = 0,
		Color = 1,
		Depth = 2,
		Count = 3
	}

	class TerrainSample : SdkSample
	{
		const int TerrainSize = 513;
		const float TerrainWorldSize = 12000f;
		const string TerrainFilePrefix = "TestTerrain";
		const string TerrainFileSuffix = "dat";
		const long TerrainPageMinX = 0;
		const long TerrainPageMinY = 0;
		const long TerrainPageMaxX = 0;
		const long TerrainPageMaxY = 0;
		protected TerrainGlobalOptions terrainGlobals;
		protected TerrainGroup terrainGroup;
		protected bool paging;
		//TODO PAGING
		protected Mode mode;
		protected ShadowMode shadowMode;
		protected byte layerEdit;
		protected Real brushSizeTerrainSpace;
		protected SceneNode editNode;
		protected Entity editMarker;
		protected Real heightUpdateCountdown;
		protected Real heightUpdateRate;
		protected Vector3 terrainPos;
		protected SelectMenu editMenu;
		protected SelectMenu shadowsMenu;
		protected CheckBox flyBox;
		protected Label infoLabel;
		protected bool terrainsImported;
		protected IShadowCameraSetup pssmSetup;
		protected List<Entity> houseList;

		/// <summary>
		/// 
		/// </summary>
		public TerrainSample()
		{
			Metadata[ "Title" ] = "Terrain";
			Metadata[ "Description" ] = "Demonstrates use of the terrain rendering plugin.";
			Metadata[ "Thumbnail" ] = "thumb_terrain.png";
			Metadata[ "Category" ] = "Environment";
			Metadata[ "Help" ] = "Left click and drag anywhere in the scene to look around. Let go again to show " +
			"cursor and access widgets. Use WASD keys to move. Use +/- keys when in edit mode to change content.";

			mode = Mode.Normal;
			layerEdit = 1;
			brushSizeTerrainSpace = 0.02f;
			terrainPos = new Vector3( 1000, 0, 5000 );

			// Update terrain at max 20fps
			heightUpdateRate = 1.0f / 2.0f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		private void DefineTerrain( long x, long y )
		{
			DefineTerrain( x, y, false );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		private void DefineTerrain( long x, long y, bool flat )
		{
			// if a file is available, use it
			// if not, generate file from import

			// Usually in a real project you'll know whether the compact terrain data is
			// available or not; I'm doing it this way to save distribution size
			if ( flat )
			{
				terrainGroup.DefineTerrain( x, y, 0 );
			}
			else
			{
				string filename = terrainGroup.GenerateFilename( x, y );
				if ( ResourceGroupManager.Instance.ResourceExists( terrainGroup.ResourceGroup, filename ) )
				{
					terrainGroup.DefineTerrain( x, y );
				}
				else
				{
					Image img = GetTerrainImage( x % 2 != 0, y % 2 != 0 );
					terrainGroup.DefineTerrain( x, y, img );
					terrainsImported = true;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="flipX"></param>
		/// <param name="flipY"></param>
		/// <returns></returns>
		private Image GetTerrainImage( bool flipX, bool flipY )
		{
			Image img = Image.FromFile( "terrain.png" );
			if ( flipX )
				img.FlipAroundX();
			//if ( flipY )
			//    img.FlipAroundX();

			return img;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="terrain"></param>
		private void InitBlendMaps( Axiom.Components.Terrain.Terrain terrain )
		{

			TerrainLayerBlendMap blendMap0 = terrain.GetLayerBlendMap( 1 );
			TerrainLayerBlendMap blendMap1 = terrain.GetLayerBlendMap( 2 );
			Real minHeight0 = 70;
			Real fadeDist0 = 40;
			Real minHeight1 = 70;
			Real fadeDist1 = 15;

			float[] pBlend1 = blendMap1.BlendPointer;
			int blendIdx = 0;
			for ( ushort y = 0; y < terrain.LayerBlendMapSize; y++ )
			{
				for ( ushort x = 0; x < terrain.LayerBlendMapSize; x++ )
				{
					float tx = 0;
					float ty = 0;
					blendMap0.ConvertImageToTerrainSpace( x, y, ref tx, ref ty );
					Real height = terrain.GetHeightAtTerrainPosition( tx, ty );
					Real val = ( height - minHeight0 ) / fadeDist0;
					val = Utility.Clamp( val, 0, 1 );

					val = ( height - minHeight1 ) / fadeDist1;
					val = Utility.Clamp( val, 0, 1 );
					pBlend1[ blendIdx++ ] = val;
				}
			}
			blendMap0.Dirty();
			blendMap1.Dirty();
			blendMap0.Update();
			blendMap1.Update();

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="l"></param>
		private ImportData ConfigureTerrainDefaults( Light l )
		{
			TerrainGlobalOptions.MaxPixelError = 8;
			TerrainGlobalOptions.CompositeMapDistance = 3000;
			TerrainGlobalOptions.LightMapDirection = l.DerivedDirection;
			TerrainGlobalOptions.CompositeMapAmbient = SceneManager.AmbientLight;
			TerrainGlobalOptions.CompositeMapDiffuse = l.Diffuse;

			ImportData defaultImp = terrainGroup.DefaultImportSettings;
			defaultImp.TerrainSize = TerrainSize;
			defaultImp.WorldSize = TerrainWorldSize;
			defaultImp.InputScale = 100;
			defaultImp.MinBatchSize = 33;
			defaultImp.MaxBatchSize = 65;

			defaultImp.LayerList = new List<LayerInstance>();
			LayerInstance inst = new LayerInstance();
			inst.WorldSize = 100;
			inst.TextureNames = new List<string>();
			inst.TextureNames.Add( "dirt_grayrocky_diffusespecular.dds" );
			inst.TextureNames.Add( "dirt_grayrocky_normalheight.dds" );
			defaultImp.LayerList.Add( inst );

			inst = new LayerInstance();
			inst.WorldSize = 30;
			inst.TextureNames = new List<string>();
			inst.TextureNames.Add( "grass_green-01_diffusespecular.dds" );
			inst.TextureNames.Add( "grass_green-01_normalheight.dds" );
			defaultImp.LayerList.Add( inst );

			inst = new LayerInstance();
			inst.WorldSize = 30;
			inst.TextureNames = new List<string>();
			inst.TextureNames.Add( "growth_weirdfungus-03_diffusespecular.dds" );
			inst.TextureNames.Add( "growth_weirdfungus-03_normalheight.dds" );
			defaultImp.LayerList.Add( inst );

			return defaultImp;
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void SetupView()
		{
			base.SetupView();

			Camera.Position = terrainPos + new Vector3( 1683, 50, 2116 );
			Camera.LookAt( new Vector3( 1963, 50, 1660 ) );
			Camera.Near = 0.1f;
			Camera.Far = 50000;

			if ( Root.Instance.RenderSystem.HardwareCapabilities.HasCapability( Graphics.Capabilities.InfiniteFarPlane ) )
			{
				Camera.Far = 0;// enable infinite far clip distance if we can
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void SetupContent()
		{
			bool blankTerrain = false;

			//editMarker = SceneManager.CreateEntity( "editMarker", "sphere.mesh" );
			//editNode = SceneManager.RootSceneNode.CreateChildSceneNode();
			//editNode.AttachObject( editMarker );
			//editNode.Scale = new Vector3( 0.05f, 0.05f, 0.05f );

			SetupControls();

			CameraManager.TopSpeed = 50;

			DragLook = true;

			MaterialManager.Instance.SetDefaultTextureFiltering( TextureFiltering.Anisotropic );
			MaterialManager.Instance.DefaultAnisotropy = 7;

			SceneManager.SetFog( FogMode.Linear, new ColorEx( 0.07f, 0.07f, 0.08f ), 0, 10000, 25000 );

			Vector3 lightDir = new Vector3( 0.55f, 0.3f, 0.75f );
			lightDir.Normalize();

			Light l = SceneManager.CreateLight( "tsLight" );
			l.Type = LightType.Directional;
			l.Direction = lightDir;
			l.Diffuse = ColorEx.White;
			l.Specular = new ColorEx( 0.4f, 0.4f, 0.4f );

			SceneManager.AmbientLight = new ColorEx( 0.8f, 0.8f, 0.8f );

			terrainGroup = new TerrainGroup( SceneManager, Alignment.Align_X_Z, (ushort)TerrainSize, TerrainWorldSize );
			terrainGroup.SetFilenamConvention( TerrainFilePrefix, TerrainFileSuffix );
			terrainGroup.Origin = Vector3.Zero;
			
			Terrain terrain = new Terrain( SceneManager );
			terrain.Position = terrainPos;
			ImportData data  = ConfigureTerrainDefaults( l );

			for ( long x = TerrainPageMinX; x <= TerrainPageMaxX; x++ )
			{
				for ( long y = TerrainPageMinY; y <= TerrainPageMaxY; y++ )
				{
					DefineTerrain( x, y, false );
				}
			}
			// sync load since we want everything in place when we start
			terrainGroup.LoadAllTerrains( true );

			if ( terrainsImported)
			{
				Axiom.Components.Terrain.Terrain t = terrainGroup.GetTerrain( 0, 0 );
				InitBlendMaps( t );
			}

			//Entity e = SceneManager.CreateEntity( "TudoMesh", "tudorhouse.mesh" );
			//Vector3 entPos = new Vector3( terrainPos.x + 2043, 0, terrainPos.z + 1715 );
			//Quaternion rot = new Quaternion();
			//entPos.y = terrainGroup.GetTerrain(0,0).GetHeightAtWorldPosition( entPos ) + 65.5f + terrainPos.y;
			//rot = Quaternion.FromAngleAxis( Utility.RangeRandom( -180, 180 ), Vector3.UnitY );
			//SceneNode sn = SceneManager.RootSceneNode.CreateChildSceneNode( entPos, rot );
			//sn.Scale = new Vector3( 0.12, 0.12, 0.12 );
			//sn.AttachObject( e );
			//Camera.Position = entPos;
			terrainGroup.FreeTemporaryResources();
			SceneManager.SetSkyDome( true, "Examples/CloudySky", 5, 8 );
			//SceneManager.SetSkyBox( true, "Examples/CloudyNoonSkyBox" , 5000);
		}
		/// <summary>
		/// 
		/// </summary>
		private void SetupControls()
		{
			TrayManager.ShowCursor();

			// make room for the controls
			TrayManager.ShowLogo( TrayLocation.TopRight );
			TrayManager.ShowFrameStats( TrayLocation.TopRight );
			TrayManager.ToggleAdvancedFrameStats();

			infoLabel = TrayManager.CreateLabel( TrayLocation.Top, "TInfo", "", 350 );

			editMenu = TrayManager.CreateLongSelectMenu( TrayLocation.Bottom, "EditMode", "Edit Mode", 370, 250, 3 );
			editMenu.AddItem( "None" );
			editMenu.AddItem( "Elevation" );
			editMenu.AddItem( "Blend" );
			editMenu.SelectItem( 0 ); // no edit mode

			flyBox = TrayManager.CreateCheckBox( TrayLocation.Bottom, "Fly", "Fly" );
			flyBox.SetChecked( false, false );

			shadowsMenu = TrayManager.CreateLongSelectMenu( TrayLocation.Bottom, "Shadows", "Shadows", 370, 250, 3 );
			shadowsMenu.AddItem( "None" );
			shadowsMenu.AddItem( "Color Shadows" );
			shadowsMenu.AddItem( "Depth Shadows" );
			shadowsMenu.SelectItem( 0 );

			IList<string> names = new List<string>();
			names.Add("Help");
			//a friendly reminder
			TrayManager.CreateParamsPanel( TrayLocation.TopLeft, "Help", 100, names ).SetParamValue( 0, "H/F1" );
		}

		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			if ( heightUpdateCountdown > 0 )
			{
				heightUpdateCountdown -= evt.TimeSinceLastFrame;
				if ( heightUpdateCountdown <= 0 )
				{
					terrainGroup.Update();
					heightUpdateCountdown = 0;
				}
			}
			//if ( terrainGroup.IsDerivedDataUpdateInProgress )
			//{
			//    TrayManager.MoveWidgetToTray( infoLabel, TrayLocation.Top, 0 );
			//    infoLabel.Show();
			//    if ( terrainsImported )
			//    {
			//        infoLabel.Caption = "Building terrain, please wait..";
			//    }
			//    else
			//    {
			//        infoLabel.Caption = "Updating textures, patience...";
			//    }
			//}
			//else
			{
				TrayManager.RemoveWidgetFromTray( infoLabel );
				infoLabel.Hide();
				
			}
			return base.FrameRenderingQueued( evt );
		}
	}
}
