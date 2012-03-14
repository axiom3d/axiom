using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;
using Axiom.Media;

namespace Axiom.Demos
{
	[Export( typeof( TechDemo ) )]
	public class Compositor : TechDemo
	{
		private readonly bool[] _compositorEnabled = new bool[ 2 ];

		private readonly string[] _compositorList = new[]
                                                    {
                                                        "Bloom", "Glass", "Old TV", "B&W", "Motion Blur", "Heat Vision", "Embossed", "Sharpen Edges", "Invert", "HDR"
                                                    };

		private int _compositorIndex = -1;
		private SceneNode _spinny;

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			if ( this._spinny != null )
			{
				this._spinny.Yaw( 10 * evt.TimeSinceLastFrame );
			}

			if ( input.IsKeyPressed( KeyCodes.Space ) )
			{
				if ( this._compositorIndex > 0 )
				{
					CompositorManager.Instance.SetCompositorEnabled( window.GetViewport( 0 ), this._compositorList[ this._compositorIndex ], false );
				}

				this._compositorIndex = ++this._compositorIndex % this._compositorList.Length;

				CompositorManager.Instance.SetCompositorEnabled( window.GetViewport( 0 ), this._compositorList[ this._compositorIndex ], true );
			}

			if ( input.IsKeyPressed( KeyCodes.D1 ) )
			{
				CompositorManager.Instance.SetCompositorEnabled( window.GetViewport( 0 ), this._compositorList[ 0 ], !this._compositorEnabled[ 0 ] );
				this._compositorEnabled[ 0 ] = !this._compositorEnabled[ 0 ];
				keypressDelay = 0.5f;
			}

			if ( input.IsKeyPressed( KeyCodes.D2 ) )
			{
				CompositorManager.Instance.SetCompositorEnabled( window.GetViewport( 0 ), this._compositorList[ 1 ], !this._compositorEnabled[ 1 ] );
				this._compositorEnabled[ 1 ] = !this._compositorEnabled[ 1 ];
				keypressDelay = 0.5f;
			}

			base.OnFrameStarted( source, evt );
		}

		public override void CreateScene()
		{
			// Register Compositor Logics
			CompositorManager.Instance.RegisterCompositorLogic( "HDR", new HdrLogic() );

			scene.ShadowTechnique = ShadowTechnique.TextureModulative;
			scene.ShadowFarDistance = 1000;

			scene.AmbientLight = new ColorEx( 0.3f, 0.3f, 0.2f );

			Light light = scene.CreateLight( "Light2" );
			var dir = new Vector3( -1, -1, 0 );
			dir.Normalize();
			light.Type = LightType.Directional;
			light.Direction = dir;
			light.Diffuse = new ColorEx( 1.0f, 1.0f, 0.8f );
			light.Specular = new ColorEx( 1.0f, 1.0f, 1.0f );

			Entity entity;

			// House
			entity = scene.CreateEntity( "1", "tudorhouse.mesh" );
			SceneNode n1 = scene.RootSceneNode.CreateChildSceneNode( new Vector3( 350, 450, -200 ) );
			n1.AttachObject( entity );

			entity = scene.CreateEntity( "2", "tudorhouse.mesh" );
			SceneNode n2 = scene.RootSceneNode.CreateChildSceneNode( new Vector3( -350, 450, -200 ) );
			n2.AttachObject( entity );

			entity = scene.CreateEntity( "3", "knot.mesh" );
			this._spinny = scene.RootSceneNode.CreateChildSceneNode( new Vector3( 0, 0, 300 ) );
			this._spinny.AttachObject( entity );
			entity.MaterialName = "Examples/MorningCubeMap";

			scene.SetSkyBox( true, "Examples/MorningSkyBox", 50 );

			var plane = new Plane();
			plane.Normal = Vector3.UnitY;
			plane.D = 100;
			MeshManager.Instance.CreatePlane( "Myplane", ResourceGroupManager.DefaultResourceGroupName, plane, 1500, 1500, 10, 10, true, 1, 5, 5, Vector3.UnitZ );
			Entity planeEntity = scene.CreateEntity( "plane", "Myplane" );
			planeEntity.MaterialName = "Examples/Rockwall";
			planeEntity.CastShadows = false;
			scene.RootSceneNode.CreateChildSceneNode().AttachObject( planeEntity );

			camera.Position = new Vector3( -400, 50, 900 );
			camera.LookAt( new Vector3( 0, 80, 0 ) );

			/// Create a couple of hard coded postfilter effects as an example of how to do it
			/// but the preferred method is to use compositor scripts.
			_createEffects();

			foreach ( string name in this._compositorList )
			{
				CompositorManager.Instance.AddCompositor( window.GetViewport( 0 ), name );
			}

			//CompositorManager.Instance.SetCompositorEnabled(this.window.GetViewport(0),
			//                                                 _compositorList[_compositorList.Length - 2],
			//                                                 true);
			CompositorManager.Instance.SetCompositorEnabled( window.GetViewport( 0 ), this._compositorList[ 0 ], true );
		}

		private void _createEffects()
		{
			#region /// Motion blur effect

			var comp3 = (Graphics.Compositor)CompositorManager.Instance.Create( "Motion Blur", ResourceGroupManager.DefaultResourceGroupName );
			{
				CompositionTechnique t = comp3.CreateTechnique();
				{
					CompositionTechnique.TextureDefinition def = t.CreateTextureDefinition( "scene" );
					def.Width = 0;
					def.Height = 0;
					def.PixelFormats.Add( PixelFormat.R8G8B8 );
				}
				{
					CompositionTechnique.TextureDefinition def = t.CreateTextureDefinition( "sum" );
					def.Width = 0;
					def.Height = 0;
					def.PixelFormats.Add( PixelFormat.R8G8B8 );
				}
				{
					CompositionTechnique.TextureDefinition def = t.CreateTextureDefinition( "temp" );
					def.Width = 0;
					def.Height = 0;
					def.PixelFormats.Add( PixelFormat.R8G8B8 );
				}
				/// Render scene
				{
					CompositionTargetPass tp = t.CreateTargetPass();
					tp.InputMode = CompositorInputMode.Previous;
					tp.OutputName = "scene";
				}
				/// Initialisation pass for sum texture
				{
					CompositionTargetPass tp = t.CreateTargetPass();
					tp.InputMode = CompositorInputMode.Previous;
					tp.OutputName = "sum";
					tp.OnlyInitial = true;
				}
				/// Do the motion blur
				{
					CompositionTargetPass tp = t.CreateTargetPass();
					tp.InputMode = CompositorInputMode.None;
					tp.OutputName = "temp";
					{
						CompositionPass pass = tp.CreatePass();
						pass.Type = CompositorPassType.RenderQuad;
						pass.MaterialName = "Ogre/Compositor/Combine";
						pass.SetInput( 0, "scene" );
						pass.SetInput( 1, "sum" );
					}
				}
				/// Copy back sum texture
				{
					CompositionTargetPass tp = t.CreateTargetPass();
					tp.InputMode = CompositorInputMode.None;
					tp.OutputName = "sum";
					{
						CompositionPass pass = tp.CreatePass();
						pass.Type = CompositorPassType.RenderQuad;
						pass.MaterialName = "Ogre/Compositor/Copyback";
						pass.SetInput( 0, "temp" );
					}
				}
				/// Display result
				{
					CompositionTargetPass tp = t.OutputTarget;
					tp.InputMode = CompositorInputMode.None;
					{
						CompositionPass pass = tp.CreatePass();
						pass.Type = CompositorPassType.RenderQuad;
						pass.MaterialName = "Ogre/Compositor/MotionBlur";
						pass.SetInput( 0, "sum" );
					}
				}
			}
			CompositorManager.Instance.AddCompositor( viewport, "Motion Blur" );

			#endregion /// Motion blur effect

			#region /// Heat vision effect

			var comp4 = (Graphics.Compositor)CompositorManager.Instance.Create( "Heat Vision", ResourceGroupManager.DefaultResourceGroupName );
			{
				CompositionTechnique t = comp4.CreateTechnique();
				{
					CompositionTechnique.TextureDefinition def = t.CreateTextureDefinition( "scene" );
					def.Width = 256;
					def.Height = 256;
					def.PixelFormats.Add( PixelFormat.R8G8B8 );
				}
				{
					CompositionTechnique.TextureDefinition def = t.CreateTextureDefinition( "temp" );
					def.Width = 256;
					def.Height = 256;
					def.PixelFormats.Add( PixelFormat.R8G8B8 );
				}
				/// Render scene
				{
					CompositionTargetPass tp = t.CreateTargetPass();
					tp.InputMode = CompositorInputMode.Previous;
					tp.OutputName = "scene";
				}
				/// Light to heat pass
				{
					CompositionTargetPass tp = t.CreateTargetPass();
					tp.InputMode = CompositorInputMode.None;
					tp.OutputName = "temp";
					{
						CompositionPass pass = tp.CreatePass();
						pass.Type = CompositorPassType.RenderQuad;
						pass.Identifier = 0xDEADBABE; /// Identify pass for use in listener
						pass.MaterialName = "Fury/HeatVision/LightToHeat";
						pass.SetInput( 0, "scene" );
					}
				}
				/// Display result
				{
					CompositionTargetPass tp = t.OutputTarget;
					tp.InputMode = CompositorInputMode.None;
					{
						CompositionPass pass = tp.CreatePass();
						pass.Type = CompositorPassType.RenderQuad;
						pass.MaterialName = "Fury/HeatVision/Blur";
						pass.SetInput( 0, "temp" );
					}
				}
			}
			CompositorManager.Instance.AddCompositor( viewport, "Heat Vision" );

			#endregion /// Heat vision effect
		}

		#region Nested type: HdrLogic

		internal class HdrLogic : CompositorLogic
		{
			private readonly float[] bloomTexOffsetsHorz = new float[ 15 * 4 ];
			private readonly float[] bloomTexOffsetsVert = new float[ 15 * 4 ];
			private readonly float[] bloomTexWeights = new float[ 15 * 4 ];
			private int bloomSize;
			private int vpHeight;
			private int vpWidth;

			public void SetViewport( Viewport viewport )
			{
				this.vpWidth = viewport.ActualWidth;
				this.vpHeight = viewport.ActualHeight;
			}

			public void SetCompositor( CompositorInstance compositor )
			{
				// Get some RTT dimensions for later calculations
				foreach ( CompositionTechnique.TextureDefinition textureDefinition in compositor.Technique.TextureDefinitions )
				{
					if ( textureDefinition.Name == "rt_bloom0" )
					{
						this.bloomSize = textureDefinition.Width; // should be square
						// Calculate gaussian texture offsets & weights
						float deviation = 3.0f;
						float texelSize = 1.0f / this.bloomSize;

						// central sample, no offset
						this.bloomTexOffsetsHorz[ 0 ] = 0.0f;
						this.bloomTexOffsetsHorz[ 1 ] = 0.0f;
						this.bloomTexOffsetsVert[ 0 ] = 0.0f;
						this.bloomTexOffsetsVert[ 1 ] = 0.0f;
						this.bloomTexWeights[ 0 ] = this.bloomTexWeights[ 1 ] = this.bloomTexWeights[ 2 ] = Utility.GaussianDistribution( 0, 0, deviation );
						this.bloomTexWeights[ 3 ] = 1.0f;

						// 'pre' samples
						for ( int i = 1; i < 8; ++i )
						{
							int offset = i * 4;
							this.bloomTexWeights[ offset + 0 ] = this.bloomTexWeights[ offset + 1 ] = this.bloomTexWeights[ offset + 2 ] = 1.25f * Utility.GaussianDistribution( i, 0, deviation );
							this.bloomTexWeights[ offset + 3 ] = 1.0f;
							this.bloomTexOffsetsHorz[ offset + 0 ] = i * texelSize;
							this.bloomTexOffsetsHorz[ offset + 1 ] = 0.0f;
							this.bloomTexOffsetsVert[ offset + 0 ] = 0.0f;
							this.bloomTexOffsetsVert[ offset + 1 ] = i * texelSize;
						}
						// 'post' samples
						for ( int i = 8; i < 15; ++i )
						{
							int offset = i * 4;
							this.bloomTexWeights[ offset + 0 ] = this.bloomTexWeights[ offset + 1 ] = this.bloomTexWeights[ offset + 2 ] = this.bloomTexWeights[ offset - 7 * 4 + 0 ];
							this.bloomTexWeights[ offset + 3 ] = 1.0f;

							this.bloomTexOffsetsHorz[ offset + 0 ] = -this.bloomTexOffsetsHorz[ offset - 7 * 4 + 0 ];
							this.bloomTexOffsetsHorz[ offset + 1 ] = 0.0f;
							this.bloomTexOffsetsVert[ offset + 0 ] = 0.0f;
							this.bloomTexOffsetsVert[ offset + 1 ] = -this.bloomTexOffsetsVert[ offset - 7 * 4 + 1 ];
						}
					}
				}
			}

			private void OnMaterialSetup( CompositorInstance source, CompositorInstanceMaterialEventArgs e )
			{
				SetViewport( source.Chain.Viewport );
				SetCompositor( source );

				// Prepare the fragment params offsets
				switch ( e.PassID )
				{
					//case 994: // rt_lum4
					case 993: // rt_lum3
					case 992: // rt_lum2
					case 991: // rt_lum1
					case 990: // rt_lum0
						break;
					case 800: // rt_brightpass
						break;
					case 701: // rt_bloom1
						{
							// horizontal bloom
							e.Material.Load();
							GpuProgramParameters fparams = e.Material.GetBestTechnique().GetPass( 0 ).FragmentProgramParameters;
							fparams.SetNamedConstant( "sampleOffsets", this.bloomTexOffsetsHorz, 15 );
							fparams.SetNamedConstant( "sampleWeights", this.bloomTexWeights, 15 );

							break;
						}
					case 700: // rt_bloom0
						{
							// vertical bloom
							e.Material.Load();
							GpuProgramParameters fparams = e.Material.GetTechnique( 0 ).GetPass( 0 ).FragmentProgramParameters;
							fparams.SetNamedConstant( "sampleOffsets", this.bloomTexOffsetsVert, 15 );
							fparams.SetNamedConstant( "sampleWeights", this.bloomTexWeights, 15 );

							break;
						}
				}
			}

			private void OnMaterialRender( CompositorInstance source, CompositorInstanceMaterialEventArgs e ) { }

			#region Implementation of ICompositorLogicFactory

			/// <summary>
			/// Called when a compositor instance has been created.
			/// </summary>
			/// <remarks>
			/// This happens after its setup was finished, so the chain is also accessible.
			/// This is an ideal method to automatically attach a compositor listener.
			/// </remarks>
			/// <param name="newInstance"></param>
			public override void CompositorInstanceCreated( CompositorInstance newInstance )
			{
				newInstance.MaterialRender += OnMaterialRender;
				newInstance.MaterialSetup += OnMaterialSetup;
			}

			/// <summary>
			/// Called when a compositor instance has been destroyed
			/// </summary>
			/// <remarks>
			/// The chain that contained the compositor is still alive during this call.
			/// </remarks>
			/// <param name="destroyedInstance"></param>
			public override void CompositorInstanceDestroyed( CompositorInstance destroyedInstance )
			{
				destroyedInstance.MaterialRender -= OnMaterialRender;
				destroyedInstance.MaterialSetup -= OnMaterialSetup;
			}

			#endregion Implementation of ICompositorLogicFactory
		}

		#endregion
	}
}
