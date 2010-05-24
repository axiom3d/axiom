using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;
using Axiom.Media;

namespace Axiom.Demos
{
	class Compositor : TechDemo
	{
		private SceneNode _spinny;
	    private int _compositorIndex = -1;
		private string[] _compositorList = new string[] { "Bloom", "Glass", "Old TV", "B&W", "Motion Blur", "Heat Vision", "Embossed", "Sharpen Edges", "Invert" };
		private bool[] _compositorEnabled = new bool[2];

		protected override void OnFrameStarted( object source, Axiom.Core.FrameEventArgs evt )
		{
			if (_spinny !=null)
				_spinny.Yaw( 10 * evt.TimeSinceLastFrame );

            if (input.IsKeyPressed( KeyCodes.Space ))
            {
                if ( _compositorIndex > 0)
                    CompositorManager.Instance.SetCompositorEnabled( this.window.GetViewport( 0 ),
                                                                     _compositorList[ _compositorIndex ],
                                                                     false );

                _compositorIndex = ++_compositorIndex % _compositorList.Length;

                CompositorManager.Instance.SetCompositorEnabled( this.window.GetViewport( 0 ),
                                                                 _compositorList[ _compositorIndex ],
                                                                 true );
            }

			if ( input.IsKeyPressed( KeyCodes.D1 ))
			{
				CompositorManager.Instance.SetCompositorEnabled(this.window.GetViewport( 0 ),
																 _compositorList[0],
																 !_compositorEnabled[0]);
				_compositorEnabled[ 0 ] = !_compositorEnabled[ 0 ];
			}

			if ( input.IsKeyPressed( KeyCodes.D2 ) )
			{
				CompositorManager.Instance.SetCompositorEnabled( this.window.GetViewport( 0 ),
																 _compositorList[ 1 ],
																 !_compositorEnabled[ 1 ] );
				_compositorEnabled[ 1 ] = !_compositorEnabled[ 1 ];
			}
	
			base.OnFrameStarted( source, evt );
		}

		public override void CreateScene()
		{
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

			CompositorManager.Instance.SetCompositorEnabled( this.window.GetViewport( 0 ),
															 _compositorList[ _compositorList.Length - 1 ],
			                                                 true );

		}

		private void _createEffects()
		{
			#region /// Motion blur effect
			Axiom.Graphics.Compositor comp3 = (Axiom.Graphics.Compositor)CompositorManager.Instance.Create("Motion Blur", ResourceGroupManager.DefaultResourceGroupName);
			{
				CompositionTechnique t = comp3.CreateTechnique();
				{
					CompositionTechnique.TextureDefinition def = t.CreateTextureDefinition("scene");
					def.Width = 0;
					def.Height = 0;
					def.PixelFormats.Add(PixelFormat.R8G8B8);
				}
				{
					CompositionTechnique.TextureDefinition def = t.CreateTextureDefinition("sum");
					def.Width = 0;
					def.Height = 0;
					def.PixelFormats.Add(PixelFormat.R8G8B8);
				}
				{
					CompositionTechnique.TextureDefinition def = t.CreateTextureDefinition("temp");
					def.Width = 0;
					def.Height = 0;
					def.PixelFormats.Add(PixelFormat.R8G8B8);
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
						pass.SetInput(0, "scene");
						pass.SetInput(1, "sum");
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
						pass.SetInput(0, "temp");
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
						pass.SetInput(0, "sum");
					}
				}
			}
			CompositorManager.Instance.AddCompositor( this.viewport, "Motion Blur" );
			#endregion

			#region /// Heat vision effect
			Axiom.Graphics.Compositor comp4 = (Axiom.Graphics.Compositor)CompositorManager.Instance.Create("Heat Vision", ResourceGroupManager.DefaultResourceGroupName);
			{
				CompositionTechnique t = comp4.CreateTechnique();
				{
					CompositionTechnique.TextureDefinition def = t.CreateTextureDefinition("scene");
					def.Width = 256;
					def.Height = 256;
					def.PixelFormats.Add(PixelFormat.R8G8B8);
				}
				{
					CompositionTechnique.TextureDefinition def = t.CreateTextureDefinition("temp");
					def.Width = 256;
					def.Height = 256;
					def.PixelFormats.Add(PixelFormat.R8G8B8);
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
						pass.SetInput(0, "scene");
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
						pass.SetInput(0, "temp");
					}
				}
			}
			CompositorManager.Instance.AddCompositor(this.viewport, "Heat Vision");
			#endregion
		}
	}
}
