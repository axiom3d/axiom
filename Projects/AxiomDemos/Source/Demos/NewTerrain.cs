using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;

using Axiom.Components.Terrain;
using Axiom.Input;

namespace Axiom.Demos
{
    class NewTerrain : TechDemo
    {
        protected static Axiom.Components.Terrain.Terrain _Terrain = null;

        protected override void OnFrameStarted( object source, FrameEventArgs evt )
        {
            long y, x;
            x = y = (long)128 / 2;

            if ( input.IsKeyPressed( KeyCodes.Z ))
            {
                float n = _Terrain.GetHeightAtPoint( x, y );
                _Terrain.SetHeightAtPoint( x, y, 10 );
                float n2 = _Terrain.GetHeightAtPoint( x, y );
                if ( n == n2 )
                {
                }

            }
            if ( input.IsKeyPressed( KeyCodes.X ))
            {
            }
            
            base.OnFrameStarted( source, evt );
        }

        public override void CreateScene()
        {
            Vector3 lightDir = new Vector3( 0.55f, -0.3f, 0.75f );
            lightDir.Normalize();

            Light l = this.scene.CreateLight( "tstLight" );
            l.Type = LightType.Directional;
            l.Direction = lightDir;
            l.Diffuse = ColorEx.Black;
            l.Specular = new ColorEx( 0.4f, 0.4f, 0.4f );            
            this.scene.AmbientLight = new ColorEx( 0.2f, 0.2f, 0.2f );

            TerrainGlobalOptions.MaxPixelError = 8;
            TerrainGlobalOptions.CompositeMapDistance = 3000;

            TerrainGlobalOptions.LightMapDirection = lightDir;
            TerrainGlobalOptions.CompositeMapAmbient = this.scene.AmbientLight;

            _Terrain = CreateTerrain();
            Material mat = _Terrain.Material;
            _Terrain.Position = new Vector3( 10000, 0, 5000 );
            this.engine.FrameEnded += delegate( object source, Axiom.Core.FrameEventArgs evt )
                                      {
                                          _Terrain.Update( true );
                                      };

            this.camera.Position = _Terrain.Position + new Vector3( -4000, 300, 4000 );
            this.camera.LookAt( _Terrain.Position );
            this.camera.PolygonMode = PolygonMode.Solid;

            scene.SetSkyBox( true, "SkyBox/Morning", 5000 );
        }

        public Axiom.Components.Terrain.Terrain CreateTerrain()
        {
            Axiom.Components.Terrain.Terrain ret = new Axiom.Components.Terrain.Terrain( this.scene );
            ImportData data = new ImportData();
            data.TerrainSize = 129;
            data.InputImage = Image.FromFile("heightmap.jpg");
            data.WorldSize = 8000;
            data.InputScale = 600;
            data.MinBatchSize = 33;
            data.MaxBatchSize = 65;

            data.LayerList = new List<LayerInstance>();
            LayerInstance instance = new LayerInstance();
            instance.TextureNames = new List<string>();
            instance.WorldSize = 100;
            instance.TextureNames.Add( "dirt_grayrocky_diffusespecular.png" );
            instance.TextureNames.Add( "dirt_grayrocky_normalheight.png" );
            data.LayerList.Add( instance );

            instance = new LayerInstance();
            instance.TextureNames = new List<string>();
            instance.WorldSize = 30;
            instance.TextureNames.Add( "grass_green-01_diffusespecular.png" );
            instance.TextureNames.Add( "grass_green-01_normalheight.png" );
            data.LayerList.Add( instance );

            instance = new LayerInstance();
            instance.TextureNames = new List<string>();
            instance.WorldSize = 200;
            instance.TextureNames.Add( "growth_weirdfungus-03_diffusespecular.png" );
            instance.TextureNames.Add( "growth_weirdfungus-03_normalheight.png" );
            data.LayerList.Add( instance );

            ret.Prepare( data );
            ret.Load();

            TerrainLayerBlendMap blendMap0 = ret.GetLayerBlendMap( 1 );
            //TerrainLayerBlendMap blendMap1 = ret.GetLayerBlendMap( 2 );

            float minHeight0 = 70;
            float fadeDist0 = 40;
            float minHeight1 = 70;
            float fadeDist1 = 15;

            float[] blend0 = blendMap0.BlendPointer;
            //float[] blend1 = blendMap1.BlendPointer;
            for ( ushort y = 0; y < ret.LayerBlendMapSize; ++y )
            {
                for ( ushort x = 0; x < ret.LayerBlendMapSize; ++x )
                {
                    float tx = 0, ty = 0;

                    blendMap0.ConvertImageToTerrainSpace( x, y, ref tx, ref ty );
                    float height = ret.GetHeightAtTerrainPosition( tx, ty );
                    float val = ( height - minHeight0 ) / fadeDist0;
                    val = Utility.Clamp( val, 0, 1 );
                    //val = ( height - minHeight1 ) / fadeDist1;
                    //val = Clamp( val, 0, 1 );
                    unsafe
                    {
                        fixed ( float* pBlend1F = blend0 )
                        {
                            float* pBlend1 = pBlend1F;
                            *pBlend1++ = val;
                        }
                    }
                }
            }
            blendMap0.Dirty();
            //blendMap1.Dirty();
            blendMap0.Update();
            //blendMap1.Update();

            ret.FreeTemporaryResources();
            return ret;
        }

    }
}
