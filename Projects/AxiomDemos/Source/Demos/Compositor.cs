using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;
using Axiom.Media;

namespace Axiom.Demos
{
	partial class Compositor : TechDemo
	{
		private SceneNode _spinny;
		private int _compositorIndex = -1;
		private string[] _compositorList = new string[] { "Bloom", "Glass", "Old TV", "B&W", "Motion Blur", "Heat Vision", "Embossed", "Sharpen Edges", "Invert", "HDR" };
		private bool[] _compositorEnabled = new bool[ 2 ];

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			if ( _spinny != null )
				_spinny.Yaw( 10 * evt.TimeSinceLastFrame );

			if ( input.IsKeyPressed( KeyCodes.Space ) )
			{
				if ( _compositorIndex > 0 )
					CompositorManager.Instance.SetCompositorEnabled( this.window.GetViewport( 0 ),
																	 _compositorList[ _compositorIndex ],
																	 false );

				_compositorIndex = ++_compositorIndex % _compositorList.Length;

				CompositorManager.Instance.SetCompositorEnabled( this.window.GetViewport( 0 ),
																 _compositorList[ _compositorIndex ],
																 true );
			}

			if ( input.IsKeyPressed( KeyCodes.D1 ) )
			{
				CompositorManager.Instance.SetCompositorEnabled( this.window.GetViewport( 0 ),
																 _compositorList[ 0 ],
																 !_compositorEnabled[ 0 ] );
				_compositorEnabled[ 0 ] = !_compositorEnabled[ 0 ];
				this.keypressDelay = 0.5f;
			}

			if ( input.IsKeyPressed( KeyCodes.D2 ) )
			{
				CompositorManager.Instance.SetCompositorEnabled( this.window.GetViewport( 0 ),
																 _compositorList[ 1 ],
																 !_compositorEnabled[ 1 ] );
				_compositorEnabled[ 1 ] = !_compositorEnabled[ 1 ];
				this.keypressDelay = 0.5f;
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
			Vector3 dir = new Vector3( -1, -1, 0 );
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
			_spinny = scene.RootSceneNode.CreateChildSceneNode( new Vector3( 0, 0, 300 ) );
			_spinny.AttachObject( entity );
			entity.MaterialName = "Examples/MorningCubeMap";

			scene.SetSkyBox( true, "Examples/MorningSkyBox", 50 );

			Plane plane = new Plane();
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

			foreach ( string name in _compositorList )
			{
				CompositorManager.Instance.AddCompositor( this.window.GetViewport( 0 ), name );
			}

			//CompositorManager.Instance.SetCompositorEnabled(this.window.GetViewport(0),
			//                                                 _compositorList[_compositorList.Length - 2],
			//                                                 true);
			CompositorManager.Instance.SetCompositorEnabled( this.window.GetViewport( 0 ),
															 _compositorList[ 0 ],
															 true );
		}

		internal class HdrLogic : CompositorLogic
		{
			private int vpWidth, vpHeight;
			int bloomSize;
			// Array params - have to pack in groups of 4 since this is how Cg generates them
			// also prevents dependent texture read problems if ops don't require swizzle
			float[] bloomTexWeights = new float[ 15 * 4 ];
			float[] bloomTexOffsetsHorz = new float[ 15 * 4 ];
			float[] bloomTexOffsetsVert = new float[ 15 * 4 ];

			public void SetViewport( Viewport viewport )
			{
				vpWidth = viewport.ActualWidth;
				vpHeight = viewport.ActualHeight;
			}

			public void SetCompositor( CompositorInstance compositor )
			{
				// Get some RTT dimensions for later calculations
				foreach ( CompositionTechnique.TextureDefinition textureDefinition in compositor.Technique.TextureDefinitions )
				{
					if ( textureDefinition.Name == "rt_bloom0" )
					{
						bloomSize = (int)textureDefinition.Width; // should be square
						// Calculate gaussian texture offsets & weights
						float deviation = 3.0f;
						float texelSize = 1.0f / (float)bloomSize;

						// central sample, no offset
						bloomTexOffsetsHorz[ 0 ] = 0.0f;
						bloomTexOffsetsHorz[ 1 ] = 0.0f;
						bloomTexOffsetsVert[ 0 ] = 0.0f;
						bloomTexOffsetsVert[ 1 ] = 0.0f;
						bloomTexWeights[ 0 ] = bloomTexWeights[ 1 ] = bloomTexWeights[ 2 ] = Utility.GaussianDistribution( 0, 0, deviation );
						bloomTexWeights[ 3 ] = 1.0f;

						// 'pre' samples
						for ( int i = 1; i < 8; ++i )
						{
							int offset = i * 4;
							bloomTexWeights[ offset + 0 ] = bloomTexWeights[ offset + 1 ] = bloomTexWeights[ offset + 2 ] = 1.25f * Utility.GaussianDistribution( i, 0, deviation );
							bloomTexWeights[ offset + 3 ] = 1.0f;
							bloomTexOffsetsHorz[ offset + 0 ] = i * texelSize;
							bloomTexOffsetsHorz[ offset + 1 ] = 0.0f;
							bloomTexOffsetsVert[ offset + 0 ] = 0.0f;
							bloomTexOffsetsVert[ offset + 1 ] = i * texelSize;
						}
						// 'post' samples
						for ( int i = 8; i < 15; ++i )
						{
							int offset = i * 4;
							bloomTexWeights[ offset + 0 ] = bloomTexWeights[ offset + 1 ] =
															bloomTexWeights[ offset + 2 ] = bloomTexWeights[ offset - 7 * 4 + 0 ];
							bloomTexWeights[ offset + 3 ] = 1.0f;

							bloomTexOffsetsHorz[ offset + 0 ] = -bloomTexOffsetsHorz[ offset - 7 * 4 + 0 ];
							bloomTexOffsetsHorz[ offset + 1 ] = 0.0f;
							bloomTexOffsetsVert[ offset + 0 ] = 0.0f;
							bloomTexOffsetsVert[ offset + 1 ] = -bloomTexOffsetsVert[ offset - 7 * 4 + 1 ];
						}
					}
				}
			}

			void OnMaterialSetup( CompositorInstance source, CompositorInstanceMaterialEventArgs e )
			{
				this.SetViewport( source.Chain.Viewport );
				this.SetCompositor( source );

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
							var fparams = e.Material.GetBestTechnique().GetPass( 0 ).FragmentProgramParameters;
							fparams.SetNamedConstant( "sampleOffsets", bloomTexOffsetsHorz, 15 );
							fparams.SetNamedConstant( "sampleWeights", bloomTexWeights, 15 );

							break;
						}
					case 700: // rt_bloom0
						{
							// vertical bloom
							e.Material.Load();
							var fparams = e.Material.GetTechnique( 0 ).GetPass( 0 ).FragmentProgramParameters;
							fparams.SetNamedConstant( "sampleOffsets", bloomTexOffsetsVert, 15 );
							fparams.SetNamedConstant( "sampleWeights", bloomTexWeights, 15 );

							break;
						}
				}
			}

			void OnMaterialRender( CompositorInstance source, CompositorInstanceMaterialEventArgs e )
			{
			}

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
				newInstance.MaterialRender += new CompositorInstanceMaterialEventHandler( OnMaterialRender );
				newInstance.MaterialSetup += new CompositorInstanceMaterialEventHandler( OnMaterialSetup );
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
				destroyedInstance.MaterialRender -= new CompositorInstanceMaterialEventHandler( OnMaterialRender );
				destroyedInstance.MaterialSetup -= new CompositorInstanceMaterialEventHandler( OnMaterialSetup );
			}

			#endregion Implementation of ICompositorLogicFactory
		}

		private void _createEffects()
		{
			#region /// Motion blur effect

			Axiom.Graphics.Compositor comp3 = (Axiom.Graphics.Compositor)CompositorManager.Instance.Create( "Motion Blur", ResourceGroupManager.DefaultResourceGroupName );
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
			CompositorManager.Instance.AddCompositor( this.viewport, "Motion Blur" );

			#endregion /// Motion blur effect

			#region /// Heat vision effect

			Axiom.Graphics.Compositor comp4 = (Axiom.Graphics.Compositor)CompositorManager.Instance.Create( "Heat Vision", ResourceGroupManager.DefaultResourceGroupName );
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
			CompositorManager.Instance.AddCompositor( this.viewport, "Heat Vision" );

			#endregion /// Heat vision effect
		}
	}
}