#region Namespace Declarations

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Overlays;
using Axiom.Input;
using Axiom.Math;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	///     Offset mapping.
	/// </summary>
	/// <remarks>
	///     Original technique documented by Terry Welsh http://www.infiscape.com/doc/parallax_mapping.pdf.
	///     PS.1.4 assembler shader put together by NFZ (nfuzz@hotmail.com)
	///     Demo done by Randy Ridge http://www.randyridge.com
	/// </remarks>
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof ( TechDemo ) )]
#endif
	public class OffsetMapping : TechDemo
	{
		#region Fields

		private const int NUM_LIGHTS = 3;

		private float timeDelay = 0.0f;

		private Entity[] entities = new Entity[ NUM_LIGHTS ];

		private string[] entityMeshes = new string[]
		                                {
		                                	"knot.mesh", "ogrehead.mesh"
		                                };

		private Light[] lights = new Light[ NUM_LIGHTS ];
		private BillboardSet[] lightFlareSets = new BillboardSet[ NUM_LIGHTS ];
		private Billboard[] lightFlares = new Billboard[ NUM_LIGHTS ];

		private Vector3[] lightPositions = new Vector3[]
		                                   {
		                                   	new Vector3( 300, 0, 0 ), new Vector3( -200, 50, 0 ), new Vector3( 0, -300, -100 )
		                                   };

		private float[] lightRotationAngles = new float[]
		                                      {
		                                      	0, 30, 75
		                                      };

		private Vector3[] lightRotationAxes = new Vector3[]
		                                      {
		                                      	Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY
		                                      };

		private float[] lightSpeeds = new float[]
		                              {
		                              	30, 10, 50
		                              };

		private ColorEx[] diffuseLightColors = new ColorEx[]
		                                       {
		                                       	new ColorEx( 1, 1, 1, 1 ), new ColorEx( 1, 1, 0, 0 ), new ColorEx( 1, 1, 1, 0.5f )
		                                       };

		private ColorEx[] specularLightColors = new ColorEx[]
		                                        {
		                                        	new ColorEx( 1, 1, 1, 1 ), new ColorEx( 1, 0, 0.8f, 0.8f ), new ColorEx( 1, 1, 1, 0.8f )
		                                        };

		private bool[] lightState = new bool[]
		                            {
		                            	true, true, false
		                            };

		private string[] materialNames = new string[]
		                                 {
		                                 	"Examples/OffsetMapping/Specular"
		                                 };

		private int currentMaterial = 0;
		private int currentEntity = 0;

		private SceneNode mainNode;
		private SceneNode[] lightNodes = new SceneNode[ NUM_LIGHTS ];
		private SceneNode[] lightPivots = new SceneNode[ NUM_LIGHTS ];

		#endregion Fields

		public override void CreateScene()
		{
			scene.AmbientLight = ColorEx.Black;

			// create a scene node
			mainNode = scene.RootSceneNode.CreateChildSceneNode();

			// Load the meshes with non-default HBU options
			for ( int mn = 0; mn < entityMeshes.Length; mn++ )
			{
				Mesh mesh = MeshManager.Instance.Load( entityMeshes[ mn ], ResourceGroupManager.DefaultResourceGroupName, BufferUsage.DynamicWriteOnly, BufferUsage.StaticWriteOnly, true, true, 1 ); //so we can still read it

				short srcIdx, destIdx;

				if ( !mesh.SuggestTangentVectorBuildParams( out srcIdx, out destIdx ) )
				{
					mesh.BuildTangentVectors( srcIdx, destIdx );
				}

				// Create entity
				entities[ mn ] = scene.CreateEntity( "Ent" + mn.ToString(), entityMeshes[ mn ] );

				// Attach to child of root node
				mainNode.AttachObject( entities[ mn ] );

				// Make invisible, except for index 0
				if ( mn == 0 )
				{
					entities[ mn ].MaterialName = materialNames[ currentMaterial ];
				}
				else
				{
					entities[ mn ].IsVisible = false;
				}
			}

			for ( int i = 0; i < NUM_LIGHTS; i++ )
			{
				lightPivots[ i ] = scene.RootSceneNode.CreateChildSceneNode();
				lightPivots[ i ].Rotate( lightRotationAxes[ i ], lightRotationAngles[ i ] );

				// Create a light, use default parameters
				lights[ i ] = scene.CreateLight( "Light" + i.ToString() );
				lights[ i ].Position = lightPositions[ i ];
				lights[ i ].Diffuse = diffuseLightColors[ i ];
				lights[ i ].Specular = specularLightColors[ i ];
				lights[ i ].IsVisible = lightState[ i ];

				// Attach light
				lightPivots[ i ].AttachObject( lights[ i ] );

				// Create billboard for light
				lightFlareSets[ i ] = scene.CreateBillboardSet( "Flare" + i.ToString() );
				lightFlareSets[ i ].MaterialName = "Particles/Flare";
				lightPivots[ i ].AttachObject( lightFlareSets[ i ] );
				lightFlares[ i ] = lightFlareSets[ i ].CreateBillboard( lightPositions[ i ] );
				lightFlares[ i ].Color = diffuseLightColors[ i ];
				lightFlareSets[ i ].IsVisible = lightState[ i ];
			}
			// move the camera a bit right and make it look at the knot
			camera.MoveRelative( new Vector3( 50, 0, 20 ) );
			camera.LookAt( new Vector3( 0, 0, 0 ) );
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
			{
				return;
			}

			if ( timeDelay > 0.0f )
			{
				timeDelay -= evt.TimeSinceLastFrame;
			}
			else
			{
				if ( input.IsKeyPressed( KeyCodes.O ) )
				{
					entities[ currentEntity ].IsVisible = false;
					currentEntity = ( ++currentEntity ) % entityMeshes.Length;
					entities[ currentEntity ].IsVisible = true;
					entities[ currentEntity ].MaterialName = materialNames[ currentMaterial ];
				}

				if ( input.IsKeyPressed( KeyCodes.M ) )
				{
					currentMaterial = ( ++currentMaterial ) % materialNames.Length;
					entities[ currentEntity ].MaterialName = materialNames[ currentMaterial ];
				}

				if ( input.IsKeyPressed( KeyCodes.D1 ) )
				{
					FlipLightState( 0 );
				}

				if ( input.IsKeyPressed( KeyCodes.D2 ) )
				{
					FlipLightState( 1 );
				}

				if ( input.IsKeyPressed( KeyCodes.D3 ) )
				{
					FlipLightState( 2 );
				}

				timeDelay = 0.1f;
			}

			// animate the lights
			for ( int i = 0; i < NUM_LIGHTS; i++ )
			{
				lightPivots[ i ].Rotate( Vector3.UnitZ, lightSpeeds[ i ] * evt.TimeSinceLastFrame );
			}
		}


		/// <summary>
		///    Flips the light states for the light at the specified index.
		/// </summary>
		/// <param name="index"></param>
		private void FlipLightState( int index )
		{
			lightState[ index ] = !lightState[ index ];
			lights[ index ].IsVisible = lightState[ index ];
			lightFlareSets[ index ].IsVisible = lightState[ index ];
		}
	}
}
