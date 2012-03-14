#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
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

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

#if !( WINDOWS_PHONE || XBOX || XBOX360 || ANDROID || IOS )
using System.Collections.Generic;

using Axiom.Components.Paging;
using Axiom.Components.Terrain;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using Axiom.Overlays;

using SharpInputSystem;

using SIS = SharpInputSystem;
using Vector3 = Axiom.Math.Vector3;

#endif

#endregion Namespace Declarations

namespace Axiom.Samples.Terrain
{
	public class TerrainSample : SdkSample
	{
		private const string TerrainFilePrefix = "TestTerrain";
		private const string TerrainFileSuffix = "dat";
		private const float TerrainWorldSize = 12000f;
		private const int TerrainSize = 513;

		private const int TerrainPageMinX = 0;
		private const int TerrainPageMinY = 0;
		private const int TerrainPageMaxX = 0;
		private const int TerrainPageMaxY = 0;

		protected TerrainGlobalOptions terrainGlobals;
		protected TerrainGroup terrainGroup;
		protected bool paging;

		protected TerrainPaging terrainPaging;
		protected PageManager pageManager;

#if PAGING
    /// <summary>
    /// This class just pretends to provide prcedural page content to avoid page loading
    /// </summary>
		protected class DummyPageProvider : PageProvider
		{
			[OgreVersion( 1, 7, 2 )]
			public override bool PrepareProcedualPage( Page page, PagedWorldSection section ) { return true; }

			[OgreVersion( 1, 7, 2 )]
			public override bool LoadProcedualPage( Page page, PagedWorldSection section ) { return true; }

			[OgreVersion( 1, 7, 2 )]
			public override bool UnloadProcedualPage( Page page, PagedWorldSection section ) { return true; }

			[OgreVersion( 1, 7, 2 )]
			public override bool UnPrepareProcedualPage( Page page, PagedWorldSection section ) { return true; }
		};

		protected DummyPageProvider dummyPageProvider;
#endif

		protected bool fly;
		protected Real fallVelocity;
		protected Mode mode = Mode.Normal;
		protected ShadowMode shadowMode;
		protected byte layerEdit = 1;
		protected Real brushSizeTerrainSpace = 0.02f;
		protected SceneNode editNode;
		protected Entity editMarker;
		protected Real heightUpdateCountDown;
		protected Real heightUpdateRate;
		protected Vector3 terrainPos = new Vector3( 1000, 0, 5000 );
		protected SelectMenu editMenu;
		protected SelectMenu shadowsMenu;
		protected CheckBox flyBox;
		protected Label infoLabel;
		protected bool terrainsImported;
		protected IShadowCameraSetup pssmSetup;
		protected List<Entity> houseList = new List<Entity>();


		[OgreVersion( 1, 7, 2 )]
		protected enum Mode
		{
			Normal = 0,
			EditHeight = 1,
			EditBlend = 2,
			Count = 3
		}

		[OgreVersion( 1, 7, 2 )]
		protected enum ShadowMode
		{
			None = 0,
			Color = 1,
			Depth = 2,
			Count = 3
		}

		[OgreVersion( 1, 7, 2 )]
		public TerrainSample()
		{
			Metadata[ "Title" ] = "Terrain";
			Metadata[ "Description" ] = "Demonstrates use of the terrain rendering plugin.";
			Metadata[ "Thumbnail" ] = "thumb_terrain.png";
			Metadata[ "Category" ] = "Environment";
			Metadata[ "Help" ] = "Left click and drag anywhere in the scene to look around. Let go again to show " + "cursor and access widgets. Use WASD keys to move. Use +/- keys when in edit mode to change content.";

			// Update terrain at max 20fps
			this.heightUpdateRate = 1.0f / 2.0f;
		}

		[OgreVersion( 1, 7, 2 )]
		public override void TestCapabilities( RenderSystemCapabilities capabilities )
		{
			if ( !capabilities.HasCapability( Capabilities.VertexPrograms ) || !capabilities.HasCapability( Capabilities.FragmentPrograms ) )
			{
				throw new AxiomException( "Your graphics card does not support vertex or fragment shaders, so you cannot run this sample. Sorry!" );
			}
		}

		public void DoTerrainModify( Components.Terrain.Terrain terrain, Vector3 centrepos, Real timeElapsed )
		{
			Vector3 tsPos = Vector3.Zero;
			terrain.GetTerrainPosition( centrepos, ref tsPos );

#if !( WINDOWS_PHONE || XBOX || XBOX360 || ANDROID || IOS )
			if ( Keyboard.IsKeyDown( KeyCode.Key_EQUALS ) || Keyboard.IsKeyDown( KeyCode.Key_ADD ) || Keyboard.IsKeyDown( KeyCode.Key_MINUS ) || Keyboard.IsKeyDown( KeyCode.Key_SUBTRACT ) )
			{
				switch ( this.mode )
				{
					case Mode.EditHeight:
						{
							// we need point coords
							Real terrainSize = ( terrain.Size - 1 );
							var startx = (long)( ( tsPos.x - this.brushSizeTerrainSpace ) * terrainSize );
							var starty = (long)( ( tsPos.y - this.brushSizeTerrainSpace ) * terrainSize );
							var endx = (long)( ( tsPos.x + this.brushSizeTerrainSpace ) * terrainSize );
							var endy = (long)( ( tsPos.y + this.brushSizeTerrainSpace ) * terrainSize );
							startx = Utility.Max( startx, 0L );
							starty = Utility.Max( starty, 0L );
							endx = Utility.Min( endx, (long)terrainSize );
							endy = Utility.Min( endy, (long)terrainSize );
							for ( long y = starty; y <= endy; ++y )
							{
								for ( long x = startx; x <= endx; ++x )
								{
									Real tsXdist = ( x / terrainSize ) - tsPos.x;
									Real tsYdist = ( y / terrainSize ) - tsPos.y;

									Real weight = Utility.Min( 1.0, Utility.Sqrt( tsYdist * tsYdist + tsXdist * tsXdist ) / ( 0.5 * this.brushSizeTerrainSpace ) );
									weight = 1.0 - ( weight * weight );

									Real addedHeight = weight * 250.0 * timeElapsed;
									float newheight;
									if ( Keyboard.IsKeyDown( KeyCode.Key_EQUALS ) || Keyboard.IsKeyDown( KeyCode.Key_ADD ) )
									{
										newheight = terrain.GetHeightAtPoint( x, y ) + addedHeight;
									}
									else
									{
										newheight = terrain.GetHeightAtPoint( x, y ) - addedHeight;
									}
									terrain.SetHeightAtPoint( x, y, newheight );
								}
							}
							if ( this.heightUpdateCountDown == 0 )
							{
								this.heightUpdateCountDown = this.heightUpdateRate;
							}
						}
						break;

					case Mode.EditBlend:
						{
							TerrainLayerBlendMap layer = terrain.GetLayerBlendMap( this.layerEdit );
							// we need image coords
							Real imgSize = terrain.LayerBlendMapSize;
							var startx = (long)( ( tsPos.x - this.brushSizeTerrainSpace ) * imgSize );
							var starty = (long)( ( tsPos.y - this.brushSizeTerrainSpace ) * imgSize );
							var endx = (long)( ( tsPos.x + this.brushSizeTerrainSpace ) * imgSize );
							var endy = (long)( ( tsPos.y + this.brushSizeTerrainSpace ) * imgSize );
							startx = Utility.Max( startx, 0L );
							starty = Utility.Max( starty, 0L );
							endx = Utility.Min( endx, (long)imgSize );
							endy = Utility.Min( endy, (long)imgSize );
							for ( var y = (int)starty; y <= endy; ++y )
							{
								for ( var x = (int)startx; x <= endx; ++x )
								{
									Real tsXdist = ( x / imgSize ) - tsPos.x;
									Real tsYdist = ( y / imgSize ) - tsPos.y;

									Real weight = Utility.Min( 1.0, Utility.Sqrt( tsYdist * tsYdist + tsXdist * tsXdist ) / ( 0.5 * this.brushSizeTerrainSpace ) );
									weight = 1.0 - ( weight * weight );

									float paint = weight * timeElapsed;
									var imgY = (int)( imgSize - y );
									float val;
									if ( Keyboard.IsKeyDown( KeyCode.Key_EQUALS ) || Keyboard.IsKeyDown( KeyCode.Key_ADD ) )
									{
										val = layer.GetBlendValue( x, imgY ) + paint;
									}
									else
									{
										val = layer.GetBlendValue( x, imgY ) - paint;
									}
									val = Utility.Clamp( val, 1.0f, 0.0f );
									layer.SetBlendValue( x, imgY, val );
								}
							}

							layer.Update();
						}
						break;

					case Mode.Normal:
					case Mode.Count:
					default:
						break;
				}
				;
			}
#endif
		}

		[OgreVersion( 1, 7, 2 )]
		public override bool FrameRenderingQueued( FrameEventArgs evt )
		{
			if ( this.mode != Mode.Normal )
			{
				// fire ray
				Ray ray;
				ray = TrayManager.GetCursorRay( Camera );

				TerrainGroup.RayResult rayResult = this.terrainGroup.RayIntersects( ray );
				if ( rayResult.Hit )
				{
					this.editMarker.IsVisible = true;
					this.editNode.Position = rayResult.Position;

					// figure out which terrains this affects
					List<Components.Terrain.Terrain> terrainList;
					Real brushSizeWorldSpace = TerrainWorldSize * this.brushSizeTerrainSpace;
					var sphere = new Sphere( rayResult.Position, brushSizeWorldSpace );
					this.terrainGroup.SphereIntersects( sphere, out terrainList );

					foreach ( Components.Terrain.Terrain ti in terrainList )
					{
						DoTerrainModify( ti, rayResult.Position, evt.TimeSinceLastFrame );
					}
				}
				else
				{
					this.editMarker.IsVisible = false;
				}
			}

			if ( !this.fly )
			{
				// clamp to terrain
				Vector3 camPos = Camera.Position;
				var ray = new Ray( new Vector3( camPos.x, this.terrainPos.y + 10000, camPos.z ), Vector3.NegativeUnitY );

				TerrainGroup.RayResult rayResult = this.terrainGroup.RayIntersects( ray );
				Real distanceAboveTerrain = 50;
				Real fallSpeed = 300;
				Real newy = camPos.y;
				if ( rayResult.Hit )
				{
					if ( camPos.y > rayResult.Position.y + distanceAboveTerrain )
					{
						this.fallVelocity += evt.TimeSinceLastFrame * 20;
						this.fallVelocity = Utility.Min( this.fallVelocity, fallSpeed );
						newy = camPos.y - this.fallVelocity * evt.TimeSinceLastFrame;
					}
					newy = Utility.Max( rayResult.Position.y + distanceAboveTerrain, newy );
					Camera.Position = new Vector3( camPos.x, newy, camPos.z );
				}
			}

			if ( this.heightUpdateCountDown > 0 )
			{
				this.heightUpdateCountDown -= evt.TimeSinceLastFrame;
				if ( this.heightUpdateCountDown <= 0 )
				{
					this.terrainGroup.Update();
					this.heightUpdateCountDown = 0;
				}
			}

			if ( this.terrainGroup.IsDerivedDataUpdateInProgress )
			{
				TrayManager.MoveWidgetToTray( this.infoLabel, TrayLocation.Top, 0 );
				this.infoLabel.Show();
				if ( this.terrainsImported )
				{
					this.infoLabel.Caption = "Building terrain, please wait...";
				}
				else
				{
					this.infoLabel.Caption = "Updating textures, patience...";
				}
			}
			else
			{
				TrayManager.RemoveWidgetFromTray( this.infoLabel );
				this.infoLabel.Hide();
				if ( this.terrainsImported )
				{
					SaveTerrains( true );
					this.terrainsImported = false;
				}
			}

			return base.FrameRenderingQueued( evt );
		}

		[OgreVersion( 1, 7, 2 )]
		public void SaveTerrains( bool onlyIfModified )
		{
			//TODO
			//terrainGroup.SaveAllTerrains( onlyIfModified );
		}

#if !( WINDOWS_PHONE || XBOX || XBOX360 || ANDROID || IOS )
		[OgreVersion( 1, 7, 2 )]
		public override bool KeyPressed( KeyEventArgs evt )
		{
			switch ( evt.Key )
			{
				case KeyCode.Key_S:
					// CTRL-S to save
					if ( Keyboard.IsKeyDown( KeyCode.Key_LCONTROL ) || Keyboard.IsKeyDown( KeyCode.Key_RCONTROL ) )
					{
						SaveTerrains( true );
					}
					else
					{
						return base.KeyPressed( evt );
					}
					break;

				case KeyCode.Key_F10:
					//dump
					int tkey = 0;
					foreach ( TerrainGroup.TerrainSlot ts in this.terrainGroup.TerrainSlots )
					{
						if ( ts.Instance != null && ts.Instance.IsLoaded )
						{
							ts.Instance.DumpTextures( "terrain_" + tkey, ".png" );
						}

						tkey++;
					}
					break;

				default:
					return base.KeyPressed( evt );
			}

			return true;
		}
#endif

		[OgreVersion( 1, 7, 2 )]
		private void _itemSelected( SelectMenu menu )
		{
			if ( menu == this.editMenu )
			{
				this.mode = (Mode)this.editMenu.SelectionIndex;
			}

			else if ( menu == this.shadowsMenu )
			{
				this.shadowMode = (ShadowMode)this.shadowsMenu.SelectionIndex;
				_changeShadows();
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public void _checkBoxToggled( CheckBox box )
		{
			if ( box == this.flyBox )
			{
				this.fly = this.flyBox.IsChecked;
			}
		}

		[OgreVersion( 1, 7, 2 )]
#if NET_40
        private void _defineTerrain( long x, long y, bool flat = false )
#else
		private void _defineTerrain( long x, long y, bool flat )
#endif
		{
			// if a file is available, use it
			// if not, generate file from import

			// Usually in a real project you'll know whether the compact terrain data is
			// available or not; I'm doing it this way to save distribution size

			if ( flat )
			{
				this.terrainGroup.DefineTerrain( x, y, 0 );
			}
			else
			{
				string filename = this.terrainGroup.GenerateFilename( x, y );
				if ( ResourceGroupManager.Instance.ResourceExists( this.terrainGroup.ResourceGroup, filename ) )
				{
					this.terrainGroup.DefineTerrain( x, y );
				}
				else
				{
					Image img = _getTerrainImage( x % 2 != 0, y % 2 != 0 );
					this.terrainGroup.DefineTerrain( x, y, img );
					this.terrainsImported = true;
				}
			}
		}

#if !NET_40
		private void _defineTerrain( long x, long y )
		{
			_defineTerrain( x, y, false );
		}
#endif

		[OgreVersion( 1, 7, 2 )]
		private Image _getTerrainImage( bool flipX, bool flipY )
		{
			Image img = Image.FromFile( "terrain.png", ResourceGroupManager.DefaultResourceGroupName );

			if ( flipX )
			{
				img.FlipAroundY();
			}

			if ( flipY )
			{
				img.FlipAroundX();
			}

			return img;
		}

		[OgreVersion( 1, 7, 2 )]
		private void _initBlendMaps( Components.Terrain.Terrain terrain )
		{
			TerrainLayerBlendMap blendMap0 = terrain.GetLayerBlendMap( 1 );
			TerrainLayerBlendMap blendMap1 = terrain.GetLayerBlendMap( 2 );
			Real minHeight0 = 70;
			Real fadeDist0 = 40;
			Real minHeight1 = 70;
			Real fadeDist1 = 15;

			float[] pBlend1 = blendMap1.BlendPointer;
			int blendIdx = 0;
			for ( int y = 0; y < terrain.LayerBlendMapSize; y++ )
			{
				for ( int x = 0; x < terrain.LayerBlendMapSize; x++ )
				{
					Real tx = 0;
					Real ty = 0;
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

		[OgreVersion( 1, 7, 2 )]
		private ImportData _configureTerrainDefaults( Light l )
		{
			// Configure global
			TerrainGlobalOptions.MaxPixelError = 8;
			// testing composite map
			TerrainGlobalOptions.CompositeMapDistance = 3000;
			TerrainGlobalOptions.LightMapDirection = l.DerivedDirection;
			TerrainGlobalOptions.CompositeMapAmbient = SceneManager.AmbientLight;
			TerrainGlobalOptions.CompositeMapDiffuse = l.Diffuse;

			// Configure default import settings for if we use imported image
			ImportData defaultImp = this.terrainGroup.DefaultImportSettings;
			defaultImp.TerrainSize = TerrainSize;
			defaultImp.WorldSize = TerrainWorldSize;
			defaultImp.InputScale = 600;
			defaultImp.MinBatchSize = 33;
			defaultImp.MaxBatchSize = 65;

			// textures
			defaultImp.LayerList = new List<LayerInstance>();
			var inst = new LayerInstance();
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
			inst.WorldSize = 200;
			inst.TextureNames = new List<string>();
			inst.TextureNames.Add( "growth_weirdfungus-03_diffusespecular.dds" );
			inst.TextureNames.Add( "growth_weirdfungus-03_normalheight.dds" );
			defaultImp.LayerList.Add( inst );

			return defaultImp;
		}

		[OgreVersion( 1, 7, 2 )]
		private void _addTextureDebugOverlay( TrayLocation loc, Texture tex, int i )
		{
			_addTextureDebugOverlay( loc, tex.Name, i );
		}

		[OgreVersion( 1, 7, 2 )]
		private void _addTextureDebugOverlay( TrayLocation loc, string texname, int i )
		{
			// Create material
			string matName = "Axiom/DebugTexture" + i;
			var debugMat = (Material)MaterialManager.Instance.GetByName( matName );
			if ( debugMat == null )
			{
				debugMat = (Material)MaterialManager.Instance.Create( matName, ResourceGroupManager.DefaultResourceGroupName );
			}

			Pass p = debugMat.GetTechnique( 0 ).GetPass( 0 );
			p.RemoveAllTextureUnitStates();
			p.LightingEnabled = false;
			TextureUnitState t = p.CreateTextureUnitState( texname );
			t.SetTextureAddressingMode( TextureAddressing.Clamp );

			// create template
			if ( OverlayManager.Instance.Elements.GetElement( "Axiom/DebugTexOverlay", true ) == null )
			{
				OverlayElement e = OverlayManager.Instance.Elements.CreateElement( "Panel", "Axiom/DebugTexOverlay", true );
				e.MetricsMode = MetricsMode.Pixels;
				e.Width = 128;
				e.Height = 128;
			}

			// add widget
			string widgetName = "DebugTex" + i;
			Widget w = TrayManager.GetWidget( widgetName );
			if ( w == null )
			{
				w = TrayManager.CreateDecorWidget( loc, widgetName, "", "Axiom/DebugTexOverlay" );
			}

			w.OverlayElement.MaterialName = matName;
		}

		private void _addTextureShadowDebugOverlay( TrayLocation loc, int num )
		{
			for ( int i = 0; i < num; ++i )
			{
				//TODO
				//Texture shadowTex = this.SceneManager.GetShadowTexture( i );
				//_addTextureDebugOverlay( loc, shadowTex, i );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private void _changeShadows()
		{
			_configureShadows( this.shadowMode != ShadowMode.None, this.shadowMode == ShadowMode.Depth );
		}

		private void _configureShadows( bool enabled, bool depthShadows )
		{
			//        TerrainMaterialGeneratorA::SM2Profile* matProfile = 
			//            static_cast<TerrainMaterialGeneratorA::SM2Profile*>(mTerrainGlobals->getDefaultMaterialGenerator()->getActiveProfile());
			//        matProfile->setReceiveDynamicShadowsEnabled(enabled);
			//#ifdef SHADOWS_IN_LOW_LOD_MATERIAL
			//        matProfile->setReceiveDynamicShadowsLowLod(true);
			//#else
			//        matProfile->setReceiveDynamicShadowsLowLod(false);
			//#endif

			//        // Default materials
			//        for (EntityList::iterator i = mHouseList.begin(); i != mHouseList.end(); ++i)
			//        {
			//            (*i)->setMaterialName("Examples/TudorHouse");
			//        }

			//        if (enabled)
			//        {
			//            // General scene setup
			//            mSceneMgr->setShadowTechnique(SHADOWTYPE_TEXTURE_ADDITIVE_INTEGRATED);
			//            mSceneMgr->setShadowFarDistance(3000);

			//            // 3 textures per directional light (PSSM)
			//            mSceneMgr->setShadowTextureCountPerLightType(Ogre::Light::LT_DIRECTIONAL, 3);

			//            if (mPSSMSetup.isNull())
			//            {
			//                // shadow camera setup
			//                PSSMShadowCameraSetup* pssmSetup = new PSSMShadowCameraSetup();
			//                pssmSetup->setSplitPadding(mCamera->getNearClipDistance());
			//                pssmSetup->calculateSplitPoints(3, mCamera->getNearClipDistance(), mSceneMgr->getShadowFarDistance());
			//                pssmSetup->setOptimalAdjustFactor(0, 2);
			//                pssmSetup->setOptimalAdjustFactor(1, 1);
			//                pssmSetup->setOptimalAdjustFactor(2, 0.5);

			//                mPSSMSetup.bind(pssmSetup);

			//            }
			//            mSceneMgr->setShadowCameraSetup(mPSSMSetup);

			//            if (depthShadows)
			//            {
			//                mSceneMgr->setShadowTextureCount(3);
			//                mSceneMgr->setShadowTextureConfig(0, 2048, 2048, PF_FLOAT32_R);
			//                mSceneMgr->setShadowTextureConfig(1, 1024, 1024, PF_FLOAT32_R);
			//                mSceneMgr->setShadowTextureConfig(2, 1024, 1024, PF_FLOAT32_R);
			//                mSceneMgr->setShadowTextureSelfShadow(true);
			//                mSceneMgr->setShadowCasterRenderBackFaces(true);
			//                mSceneMgr->setShadowTextureCasterMaterial("PSSM/shadow_caster");

			//                MaterialPtr houseMat = buildDepthShadowMaterial("fw12b.jpg");
			//                for (EntityList::iterator i = mHouseList.begin(); i != mHouseList.end(); ++i)
			//                {
			//                    (*i)->setMaterial(houseMat);
			//                }

			//            }
			//            else
			//            {
			//                mSceneMgr->setShadowTextureCount(3);
			//                mSceneMgr->setShadowTextureConfig(0, 2048, 2048, PF_X8B8G8R8);
			//                mSceneMgr->setShadowTextureConfig(1, 1024, 1024, PF_X8B8G8R8);
			//                mSceneMgr->setShadowTextureConfig(2, 1024, 1024, PF_X8B8G8R8);
			//                mSceneMgr->setShadowTextureSelfShadow(false);
			//                mSceneMgr->setShadowCasterRenderBackFaces(false);
			//                mSceneMgr->setShadowTextureCasterMaterial(StringUtil::BLANK);
			//            }

			//            matProfile->setReceiveDynamicShadowsDepth(depthShadows);
			//            matProfile->setReceiveDynamicShadowsPSSM(static_cast<PSSMShadowCameraSetup*>(mPSSMSetup.get()));

			//            //addTextureShadowDebugOverlay(TL_RIGHT, 3);


			//        }
			//        else
			//        {
			//            mSceneMgr->setShadowTechnique(SHADOWTYPE_NONE);
			//        }
		}

		/// <summary>
		/// Extends setupView to change some initial camera settings for this sample.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected override void SetupView()
		{
			base.SetupView();

			Camera.Position = this.terrainPos + new Vector3( 1683, 50, 2116 );
			Camera.LookAt( new Vector3( 1963, 50, 1660 ) );
			Camera.Near = 0.1f;
			Camera.Far = 50000;

			if ( Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.InfiniteFarPlane ) )
			{
				Camera.Far = 0; // enable infinite far clip distance if we can
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private void _setupControls()
		{
			TrayManager.ShowCursor();

			// make room for the controls
			TrayManager.ShowLogo( TrayLocation.TopRight );
			TrayManager.ShowFrameStats( TrayLocation.TopRight );
			TrayManager.ToggleAdvancedFrameStats();

			this.infoLabel = TrayManager.CreateLabel( TrayLocation.Top, "TInfo", "", 350 );

			this.editMenu = TrayManager.CreateLongSelectMenu( TrayLocation.Bottom, "EditMode", "Edit Mode", 370, 250, 3 );
			this.editMenu.AddItem( "None" );
			this.editMenu.AddItem( "Elevation" );
			this.editMenu.AddItem( "Blend" );
			this.editMenu.SelectItem( 0 ); // no edit mode
			this.editMenu.SelectedIndexChanged += _itemSelected;

			this.flyBox = TrayManager.CreateCheckBox( TrayLocation.Bottom, "Fly", "Fly" );
			this.flyBox.SetChecked( false, false );
			this.flyBox.CheckChanged += _checkBoxToggled;

			this.shadowsMenu = TrayManager.CreateLongSelectMenu( TrayLocation.Bottom, "Shadows", "Shadows", 370, 250, 3 );
			this.shadowsMenu.AddItem( "None" );
			this.shadowsMenu.AddItem( "Color Shadows" );
			this.shadowsMenu.AddItem( "Depth Shadows" );
			this.shadowsMenu.SelectItem( 0 );
			this.shadowsMenu.SelectedIndexChanged += _itemSelected;

			var names = new List<string>();
			names.Add( "Help" );
			//a friendly reminder
			TrayManager.CreateParamsPanel( TrayLocation.TopLeft, "Help", 100, names ).SetParamValue( 0, "H/F1" );
		}

		[OgreVersion( 1, 7, 2 )]
		protected override void SetupContent()
		{
			bool blankTerrain = false;

			this.editMarker = SceneManager.CreateEntity( "editMarker", "sphere.mesh" );
			this.editNode = SceneManager.RootSceneNode.CreateChildSceneNode();
			this.editNode.AttachObject( this.editMarker );
			this.editNode.Scale = new Vector3( 0.05f, 0.05f, 0.05f );

			_setupControls();

			CameraManager.TopSpeed = 50;

			DragLook = true;

			MaterialManager.Instance.SetDefaultTextureFiltering( TextureFiltering.Anisotropic );
			MaterialManager.Instance.DefaultAnisotropy = 7;

			SceneManager.SetFog( FogMode.Linear, new ColorEx( 0.07f, 0.07f, 0.08f ), 0, 10000, 25000 );

			var lightDir = new Vector3( 0.55f, 0.3f, 0.75f );
			lightDir.Normalize();

			Light l = SceneManager.CreateLight( "tsLight" );
			l.Type = LightType.Directional;
			l.Direction = lightDir;
			l.Diffuse = ColorEx.White;
			l.Specular = new ColorEx( 0.4f, 0.4f, 0.4f );

			SceneManager.AmbientLight = new ColorEx( 0.2f, 0.2f, 0.2f );

			this.terrainGroup = new TerrainGroup( SceneManager, Alignment.Align_X_Z, TerrainSize, TerrainWorldSize );
			this.terrainGroup.SetFilenameConvention( TerrainFilePrefix, TerrainFileSuffix );
			this.terrainGroup.Origin = this.terrainPos;

			_configureTerrainDefaults( l );
#if PAGING
    // Paging setup
            pageManager = new PageManager();
            // Since we're not loading any pages from .page files, we need a way just 
            // to say we've loaded them without them actually being loaded
            pageManager.PageProvider = dummyPageProvider;
            pageManager.AddCamera( Camera );
            terrainPaging = new TerrainPaging( pageManager );
            PagedWorld world = pageManager.CreateWorld();
            terrainPaging.CreateWorldSection( world, terrainGroup, 2000, 3000,
                TerrainPageMinX, TerrainPageMinY,
                TerrainPageMaxX, TerrainPageMaxY );
#else
			for ( long x = TerrainPageMinX; x <= TerrainPageMaxX; ++x )
			{
				for ( long y = TerrainPageMinY; y <= TerrainPageMaxY; ++y )
				{
					_defineTerrain( x, y, blankTerrain );
				}
			}
			// sync load since we want everything in place when we start
			this.terrainGroup.LoadAllTerrains( true );
#endif

			if ( this.terrainsImported )
			{
				foreach ( TerrainGroup.TerrainSlot ts in this.terrainGroup.TerrainSlots )
				{
					_initBlendMaps( ts.Instance );
				}
			}

			this.terrainGroup.FreeTemporaryResources();

			Entity e = SceneManager.CreateEntity( "TudoMesh", "tudorhouse.mesh" );
			var entPos = new Vector3( this.terrainPos.x + 2043, 0, this.terrainPos.z + 1715 );
			var rot = new Quaternion();
			entPos.y = this.terrainGroup.GetHeightAtWorldPosition( entPos ) + 65.5 + this.terrainPos.y;
			rot = Quaternion.FromAngleAxis( Utility.RangeRandom( -180, 180 ), Vector3.UnitY );
			SceneNode sn = SceneManager.RootSceneNode.CreateChildSceneNode( entPos, rot );
			sn.Scale = new Vector3( 0.12, 0.12, 0.12 );
			sn.AttachObject( e );
			this.houseList.Add( e );

			e = SceneManager.CreateEntity( "TudoMesh1", "tudorhouse.mesh" );
			entPos = new Vector3( this.terrainPos.x + 1850, 0, this.terrainPos.z + 1478 );
			entPos.y = this.terrainGroup.GetHeightAtWorldPosition( entPos ) + 65.5 + this.terrainPos.y;
			rot = Quaternion.FromAngleAxis( Utility.RangeRandom( -180, 180 ), Vector3.UnitY );
			sn = SceneManager.RootSceneNode.CreateChildSceneNode( entPos, rot );
			sn.Scale = new Vector3( 0.12, 0.12, 0.12 );
			sn.AttachObject( e );
			this.houseList.Add( e );

			e = SceneManager.CreateEntity( "TudoMesh2", "tudorhouse.mesh" );
			entPos = new Vector3( this.terrainPos.x + 1970, 0, this.terrainPos.z + 2180 );
			entPos.y = this.terrainGroup.GetHeightAtWorldPosition( entPos ) + 65.5 + this.terrainPos.y;
			rot = Quaternion.FromAngleAxis( Utility.RangeRandom( -180, 180 ), Vector3.UnitY );
			sn = SceneManager.RootSceneNode.CreateChildSceneNode( entPos, rot );
			sn.Scale = new Vector3( 0.12, 0.12, 0.12 );
			sn.AttachObject( e );
			this.houseList.Add( e );

			//SceneManager.SetSkyDome( true, "Examples/CloudyNoonSkyBox", 5000, 8 );
			SceneManager.SetSkyDome( true, "Examples/CloudySky", 5000, 8 );
		}

		[OgreVersion( 1, 7, 2 )]
		public override void Shutdown()
		{
			this.terrainPaging.SafeDispose();
			this.pageManager.SafeDispose();
			this.terrainGroup.SafeDispose();

			base.Shutdown();
		}
	};
}
