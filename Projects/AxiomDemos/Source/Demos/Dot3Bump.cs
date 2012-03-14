#region Namespace Declarations

using System.ComponentModel.Composition;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;
using Axiom.Overlays;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	///     Demonstrates dotproduct blending operation and normalization cube map
	///     usage for achieving bump mapping effect.
	/// </summary>
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class Dot3Bump : TechDemo
	{
		#region Fields

		private const int NUM_ENTITIES = 3;
		private const int NUM_MATERIALS = 6;
		private const int NUM_LIGHTS = 3;

		private float timeDelay;

		private readonly Entity[] entities = new Entity[ NUM_LIGHTS ];

		private readonly string[] entityMeshes = new[]
                                                 {
                                                     "athene.mesh", "knot.mesh", "ogrehead.mesh"
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

		private readonly string[ , ] materialNames = new[ , ]
                                                     {
                                                         // athene
                                                         {
                                                             "Examples/Athene/NormalMapped", "Examples/Athene/NormalMappedSpecular", "Examples/Athene/NormalMapped", "Examples/ShowUV", "Examples/ShowNormals", "Examples/ShowTangents"
                                                         }, // knot
                                                         {
                                                             "Examples/BumpMapping/MultiLight", "Examples/BumpMapping/MultiLightSpecular", "Examples/OffsetMapping/Specular", "Examples/ShowUV", "Examples/ShowNormals", "Examples/ShowTangents"
                                                         }, // ogre head
                                                         {
                                                             "Examples/BumpMapping/MultiLight", "Examples/BumpMapping/MultiLightSpecular", "Examples/OffsetMapping/Specular", "Examples/ShowUV", "Examples/ShowNormals", "Examples/ShowTangents"
                                                         }
                                                     };

		private int currentMaterial;
		private int currentEntity;

		private SceneNode mainNode;
		private SceneNode[] lightNodes = new SceneNode[ NUM_LIGHTS ];
		private readonly SceneNode[] lightPivots = new SceneNode[ NUM_LIGHTS ];

		private OverlayElement objectInfo, materialInfo, info;

		#endregion Fields

		public override void CreateScene()
		{
			// Check prerequisites first
			RenderSystemCapabilities caps = Root.Instance.RenderSystem.Capabilities;
			if ( !caps.HasCapability( Capabilities.VertexPrograms ) || !caps.HasCapability( Capabilities.FragmentPrograms ) )
			{
				throw new AxiomException( "Your card does not support vertex and fragment programs, so cannot run this demo. Sorry!" );
			}
			else
			{
				if ( !GpuProgramManager.Instance.IsSyntaxSupported( "arbfp1" ) && !GpuProgramManager.Instance.IsSyntaxSupported( "ps_2_0" ) )
				{
					throw new AxiomException( "Your card does not support shader model 2, so cannot run this demo. Sorry!" );
				}
			}

			scene.AmbientLight = ColorEx.Black;

			// create scene node
			this.mainNode = scene.RootSceneNode.CreateChildSceneNode();

			// Load the meshes with non-default HBU options
			for ( int mn = 0; mn < NUM_ENTITIES; mn++ )
			{
				Mesh mesh = MeshManager.Instance.Load( this.entityMeshes[ mn ], ResourceGroupManager.DefaultResourceGroupName, BufferUsage.DynamicWriteOnly, BufferUsage.StaticWriteOnly, true, true, 1 ); //so we can still read it

				// Build tangent vectors, all our meshes use only 1 texture coordset
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
					this.entities[ mn ].MaterialName = this.materialNames[ this.currentEntity, this.currentMaterial ];
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

			// show overlay
			Overlay pOver = OverlayManager.Instance.GetByName( "Example/DP3Overlay" );
			this.objectInfo = OverlayManager.Instance.Elements.GetElement( "Example/DP3/ObjectInfo" );
			this.materialInfo = OverlayManager.Instance.Elements.GetElement( "Example/DP3/MaterialInfo" );
			this.info = OverlayManager.Instance.Elements.GetElement( "Example/DP3/Info" );

			this.objectInfo.Text = "Current: " + this.entityMeshes[ this.currentEntity ];
			this.materialInfo.Text = "Current: " + this.materialNames[ this.currentEntity, this.currentMaterial ];
			if ( !caps.HasCapability( Capabilities.FragmentPrograms ) )
			{
				this.info.Text = "NOTE: Light colours and specular highlights are not supported by your card.";
			}
			pOver.Show();
		}

		protected override void OnFrameStarted( object source, FrameEventArgs e )
		{
			base.OnFrameStarted( source, e );

			this.timeDelay -= e.TimeSinceLastFrame;

			IfKeyPressed( KeyCodes.O, delegate
									  {
										  this.entities[ this.currentEntity ].IsVisible = false;
										  this.currentEntity = ( ++this.currentEntity ) % NUM_ENTITIES;
										  this.entities[ this.currentEntity ].IsVisible = true;
										  this.entities[ this.currentEntity ].MaterialName = this.materialNames[ this.currentEntity, this.currentMaterial ];

										  this.objectInfo.Text = "Current: " + this.entityMeshes[ this.currentEntity ];
										  this.materialInfo.Text = "Current: " + this.materialNames[ this.currentEntity, this.currentMaterial ];
									  } );

			IfKeyPressed( KeyCodes.M, delegate
									  {
										  this.currentMaterial = ( ++this.currentMaterial ) % NUM_MATERIALS;
										  this.entities[ this.currentEntity ].MaterialName = this.materialNames[ this.currentEntity, this.currentMaterial ];

										  this.materialInfo.Text = "Current: " + this.materialNames[ this.currentEntity, this.currentMaterial ];
									  } );

			IfKeyPressed( KeyCodes.D1, delegate { FlipLightState( 0 ); } );

			IfKeyPressed( KeyCodes.D2, delegate { FlipLightState( 1 ); } );

			IfKeyPressed( KeyCodes.D3, delegate { FlipLightState( 2 ); } );

			// animate the lights
			for ( int i = 0; i < NUM_LIGHTS; i++ )
			{
				this.lightPivots[ i ].Rotate( Vector3.UnitZ, this.lightSpeeds[ i ] * e.TimeSinceLastFrame );
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
