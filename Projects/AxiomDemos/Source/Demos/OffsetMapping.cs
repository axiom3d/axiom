#region Namespace Declarations

using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;

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
	[Export( typeof( TechDemo ) )]
#endif
	public class OffsetMapping : TechDemo
	{
		#region Fields

		private const int NUM_LIGHTS = 3;

		private float timeDelay;

		private readonly Entity[] entities = new Entity[ NUM_LIGHTS ];

		private readonly string[] entityMeshes = new[]
                                                 {
                                                     "knot.mesh", "ogrehead.mesh"
                                                 };

		private readonly Light[] lights = new Light[ NUM_LIGHTS ];
		private readonly BillboardSet[] lightFlareSets = new BillboardSet[ NUM_LIGHTS ];
		private readonly Billboard[] lightFlares = new Billboard[ NUM_LIGHTS ];

		private readonly Vector3[] lightPositions = new[]
                                                    {
                                                        new Vector3( 300, 0, 0 ), new Vector3( -200, 50, 0 ), new Vector3( 0, -300, -100 )
                                                    };

		private readonly float[] lightRotationAngles = new float[]
                                                       {
                                                           0, 30, 75
                                                       };

		private readonly Vector3[] lightRotationAxes = new[]
                                                       {
                                                           Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY
                                                       };

		private readonly float[] lightSpeeds = new float[]
                                               {
                                                   30, 10, 50
                                               };

		private readonly ColorEx[] diffuseLightColors = new[]
                                                        {
                                                            new ColorEx( 1, 1, 1, 1 ), new ColorEx( 1, 1, 0, 0 ), new ColorEx( 1, 1, 1, 0.5f )
                                                        };

		private readonly ColorEx[] specularLightColors = new[]
                                                         {
                                                             new ColorEx( 1, 1, 1, 1 ), new ColorEx( 1, 0, 0.8f, 0.8f ), new ColorEx( 1, 1, 1, 0.8f )
                                                         };

		private readonly bool[] lightState = new[]
                                             {
                                                 true, true, false
                                             };

		private readonly string[] materialNames = new[]
                                                  {
                                                      "Examples/OffsetMapping/Specular"
                                                  };

		private int currentMaterial;
		private int currentEntity;

		private SceneNode mainNode;
		private SceneNode[] lightNodes = new SceneNode[ NUM_LIGHTS ];
		private readonly SceneNode[] lightPivots = new SceneNode[ NUM_LIGHTS ];

		#endregion Fields

		public override void CreateScene()
		{
			scene.AmbientLight = ColorEx.Black;

			// create a scene node
			this.mainNode = scene.RootSceneNode.CreateChildSceneNode();

			// Load the meshes with non-default HBU options
			for ( int mn = 0; mn < this.entityMeshes.Length; mn++ )
			{
				Mesh mesh = MeshManager.Instance.Load( this.entityMeshes[ mn ], ResourceGroupManager.DefaultResourceGroupName, BufferUsage.DynamicWriteOnly, BufferUsage.StaticWriteOnly, true, true, 1 ); //so we can still read it

				short srcIdx, destIdx;

				if ( !mesh.SuggestTangentVectorBuildParams( out srcIdx, out destIdx ) )
				{
					mesh.BuildTangentVectors( srcIdx, destIdx );
				}

				// Create entity
				this.entities[ mn ] = scene.CreateEntity( "Ent" + mn.ToString(), this.entityMeshes[ mn ] );

				// Attach to child of root node
				this.mainNode.AttachObject( this.entities[ mn ] );

				// Make invisible, except for index 0
				if ( mn == 0 )
				{
					this.entities[ mn ].MaterialName = this.materialNames[ this.currentMaterial ];
				}
				else
				{
					this.entities[ mn ].IsVisible = false;
				}
			}

			for ( int i = 0; i < NUM_LIGHTS; i++ )
			{
				this.lightPivots[ i ] = scene.RootSceneNode.CreateChildSceneNode();
				this.lightPivots[ i ].Rotate( this.lightRotationAxes[ i ], this.lightRotationAngles[ i ] );

				// Create a light, use default parameters
				this.lights[ i ] = scene.CreateLight( "Light" + i.ToString() );
				this.lights[ i ].Position = this.lightPositions[ i ];
				this.lights[ i ].Diffuse = this.diffuseLightColors[ i ];
				this.lights[ i ].Specular = this.specularLightColors[ i ];
				this.lights[ i ].IsVisible = this.lightState[ i ];

				// Attach light
				this.lightPivots[ i ].AttachObject( this.lights[ i ] );

				// Create billboard for light
				this.lightFlareSets[ i ] = scene.CreateBillboardSet( "Flare" + i.ToString() );
				this.lightFlareSets[ i ].MaterialName = "Particles/Flare";
				this.lightPivots[ i ].AttachObject( this.lightFlareSets[ i ] );
				this.lightFlares[ i ] = this.lightFlareSets[ i ].CreateBillboard( this.lightPositions[ i ] );
				this.lightFlares[ i ].Color = this.diffuseLightColors[ i ];
				this.lightFlareSets[ i ].IsVisible = this.lightState[ i ];
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

			if ( this.timeDelay > 0.0f )
			{
				this.timeDelay -= evt.TimeSinceLastFrame;
			}
			else
			{
				if ( input.IsKeyPressed( KeyCodes.O ) )
				{
					this.entities[ this.currentEntity ].IsVisible = false;
					this.currentEntity = ( ++this.currentEntity ) % this.entityMeshes.Length;
					this.entities[ this.currentEntity ].IsVisible = true;
					this.entities[ this.currentEntity ].MaterialName = this.materialNames[ this.currentMaterial ];
				}

				if ( input.IsKeyPressed( KeyCodes.M ) )
				{
					this.currentMaterial = ( ++this.currentMaterial ) % this.materialNames.Length;
					this.entities[ this.currentEntity ].MaterialName = this.materialNames[ this.currentMaterial ];
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

				this.timeDelay = 0.1f;
			}

			// animate the lights
			for ( int i = 0; i < NUM_LIGHTS; i++ )
			{
				this.lightPivots[ i ].Rotate( Vector3.UnitZ, this.lightSpeeds[ i ] * evt.TimeSinceLastFrame );
			}
		}


		/// <summary>
		///    Flips the light states for the light at the specified index.
		/// </summary>
		/// <param name="index"></param>
		private void FlipLightState( int index )
		{
			this.lightState[ index ] = !this.lightState[ index ];
			this.lights[ index ].IsVisible = this.lightState[ index ];
			this.lightFlareSets[ index ].IsVisible = this.lightState[ index ];
		}
	}
}
