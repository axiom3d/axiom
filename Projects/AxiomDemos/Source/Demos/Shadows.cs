#region Namespace Declarations

using System.ComponentModel.Composition;

using Axiom.Animating;
using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// Summary description for Shadows.
	/// </summary>
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class Shadows : TechDemo
	{
		private Entity athene;
		private AnimationState animState;
		private Light light;
		private Light sunLight;
		private SceneNode lightNode;
		private readonly ColorEx minLightColor = new ColorEx( 0.3f, 0, 0 );
		private readonly ColorEx maxLightColor = new ColorEx( 0.5f, 0.3f, 0.1f );
		private readonly Real minFlareSize = 40;
		private readonly Real maxFlareSize = 80;

		private readonly string[] atheneMaterials = new[]
                                                    {
                                                        "Examples/Athene/NormalMapped", "Examples/Athene/Basic"
                                                    };

		private readonly string[] shadowTechniqueDescriptions = new[]
                                                                {
                                                                    "Texture Shadows (Modulative)", "Texture Shadows (Additive)", "Stencil Shadows (Additive)", "Stencil Shadows (Modulative)", "None"
                                                                };

		private readonly ShadowTechnique[] shadowTechniques = new[]
                                                              {
                                                                  ShadowTechnique.TextureModulative, ShadowTechnique.TextureAdditive, ShadowTechnique.StencilAdditive, ShadowTechnique.StencilModulative, ShadowTechnique.None
                                                              };

		private int currentShadowTechnique = 3;

		public override void CreateScene()
		{
			// set ambient light off
			scene.AmbientLight = ColorEx.Black;

			// TODO: Check based on caps
			int currentAtheneMaterial = 1;

			// fixed light, dim
			this.sunLight = scene.CreateLight( "SunLight" );
			this.sunLight.Type = LightType.Directional;
			this.sunLight.Position = new Vector3( 1000, 1250, 500 );
			this.sunLight.SetSpotlightRange( 30, 50 );
			Vector3 dir = -this.sunLight.Position;
			dir.Normalize();
			this.sunLight.Direction = dir;
			this.sunLight.Diffuse = new ColorEx( 0.35f, 0.35f, 0.38f );
			this.sunLight.Specular = new ColorEx( 0.9f, 0.9f, 1 );

			// point light, movable, reddish
			this.light = scene.CreateLight( "Light2" );
			this.light.Diffuse = this.minLightColor;
			this.light.Specular = ColorEx.White;
			this.light.SetAttenuation( 8000, 1, .0005f, 0 );

			// create light node
			this.lightNode = scene.RootSceneNode.CreateChildSceneNode( "MovingLightNode" );
			this.lightNode.AttachObject( this.light );

			// create billboard set
			BillboardSet bbs = scene.CreateBillboardSet( "LightBBS", 1 );
			bbs.MaterialName = "Examples/Flare";
			Billboard bb = bbs.CreateBillboard( Vector3.Zero, this.minLightColor );
			// attach to the scene
			this.lightNode.AttachObject( bbs );

			// create controller, after this is will get updated on its own
			var func = new WaveformControllerFunction( WaveformType.Sine, 0.75f, 0.5f );

			var val = new LightWibbler( this.light, bb, this.minLightColor, this.maxLightColor, this.minFlareSize, this.maxFlareSize );
			ControllerManager.Instance.CreateController( val, func );

			this.lightNode.Position = new Vector3( 300, 250, -300 );

			// create a track for the light
			Animation anim = scene.CreateAnimation( "LightTrack", 20 );
			// spline it for nice curves
			anim.InterpolationMode = InterpolationMode.Spline;
			// create a track to animate the camera's node
			AnimationTrack track = anim.CreateNodeTrack( 0, this.lightNode );
			// setup keyframes
			var key = (TransformKeyFrame)track.CreateKeyFrame( 0 );
			key.Translate = new Vector3( 300, 250, -300 );
			key = (TransformKeyFrame)track.CreateKeyFrame( 2 );
			key.Translate = new Vector3( 150, 300, -250 );
			key = (TransformKeyFrame)track.CreateKeyFrame( 4 );
			key.Translate = new Vector3( -150, 350, -100 );
			key = (TransformKeyFrame)track.CreateKeyFrame( 6 );
			key.Translate = new Vector3( -400, 200, -200 );
			key = (TransformKeyFrame)track.CreateKeyFrame( 8 );
			key.Translate = new Vector3( -200, 200, -400 );
			key = (TransformKeyFrame)track.CreateKeyFrame( 10 );
			key.Translate = new Vector3( -100, 150, -200 );
			key = (TransformKeyFrame)track.CreateKeyFrame( 12 );
			key.Translate = new Vector3( -100, 75, 180 );
			key = (TransformKeyFrame)track.CreateKeyFrame( 14 );
			key.Translate = new Vector3( 0, 250, 300 );
			key = (TransformKeyFrame)track.CreateKeyFrame( 16 );
			key.Translate = new Vector3( 100, 350, 100 );
			key = (TransformKeyFrame)track.CreateKeyFrame( 18 );
			key.Translate = new Vector3( 250, 300, 0 );
			key = (TransformKeyFrame)track.CreateKeyFrame( 20 );
			key.Translate = new Vector3( 300, 250, -300 );

			// create a new animation state to track this
			this.animState = scene.CreateAnimationState( "LightTrack" );
			this.animState.IsEnabled = true;

			// Make light node look at origin, this is for when we
			// change the moving light to a spotlight
			this.lightNode.SetAutoTracking( true, scene.RootSceneNode );

			Mesh mesh = MeshManager.Instance.Load( "athene.mesh", ResourceGroupManager.DefaultResourceGroupName );

			short srcIdx, destIdx;

			// the athene mesh requires tangent vectors
			if ( !mesh.SuggestTangentVectorBuildParams( out srcIdx, out destIdx ) )
			{
				mesh.BuildTangentVectors( srcIdx, destIdx );
			}

			SceneNode node = scene.RootSceneNode.CreateChildSceneNode();
			this.athene = scene.CreateEntity( "Athene", "athene.mesh" );
			this.athene.MaterialName = this.atheneMaterials[ currentAtheneMaterial ];
			node.AttachObject( this.athene );
			node.Translate( new Vector3( 0, -20, 0 ) );
			node.Yaw( 90 );

			Entity ent = null;

			node = scene.RootSceneNode.CreateChildSceneNode();
			ent = scene.CreateEntity( "Column1", "column.mesh" );
			ent.MaterialName = "Examples/Rockwall";
			node.AttachObject( ent );
			node.Translate( new Vector3( 200, 0, -200 ) );

			node = scene.RootSceneNode.CreateChildSceneNode();
			ent = scene.CreateEntity( "Column2", "column.mesh" );
			ent.MaterialName = "Examples/Rockwall";
			node.AttachObject( ent );
			node.Translate( new Vector3( 200, 0, 200 ) );

			node = scene.RootSceneNode.CreateChildSceneNode();
			ent = scene.CreateEntity( "Column3", "column.mesh" );
			ent.MaterialName = "Examples/Rockwall";
			node.AttachObject( ent );
			node.Translate( new Vector3( -200, 0, -200 ) );

			node = scene.RootSceneNode.CreateChildSceneNode();
			ent = scene.CreateEntity( "Column4", "column.mesh" );
			ent.MaterialName = "Examples/Rockwall";
			node.AttachObject( ent );
			node.Translate( new Vector3( -200, 0, 200 ) );

			scene.SetSkyBox( true, "Skybox/Stormy", 3000 );

			var plane = new Plane( Vector3.UnitY, -100 );
			MeshManager.Instance.CreatePlane( "MyPlane", ResourceGroupManager.DefaultResourceGroupName, plane, 1500, 1500, 20, 20, true, 1, 5, 5, Vector3.UnitZ );

			Entity planeEnt = scene.CreateEntity( "Plane", "MyPlane" );
			planeEnt.MaterialName = "Examples/Rockwall";
			planeEnt.CastShadows = false;
			scene.RootSceneNode.CreateChildSceneNode().AttachObject( planeEnt );

			if ( Root.Instance.RenderSystem.Name.StartsWith( "Direct" ) )
			{
				// In D3D, use a 1024x1024 shadow texture
				scene.SetShadowTextureSettings( 1024, 2 );
			}
			else if ( Root.Instance.RenderSystem.Name.StartsWith( "Axiom Xna" ) )
			{
				// Use 512x512 texture in GL since we can't go higher than the window res
				scene.SetShadowTextureSettings( 1024, 2 );
			}

			scene.ShadowColor = new ColorEx( 0.5f, 0.5f, 0.5f );

			// incase infinite far distance is not supported
			camera.Far = 100000;

			ChangeShadowTechnique();
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
			{
				return;
			}

			if ( input.IsKeyPressed( KeyCodes.O ) && keypressDelay < 0 )
			{
				ChangeShadowTechnique();

				// show for 2 seconds
				debugTextDelay = 2.0f;

				keypressDelay = 1;
			}

			if ( input.IsKeyPressed( KeyCodes.M ) && keypressDelay < 0 )
			{
				scene.ShowDebugShadows = !scene.ShowDebugShadows;
				keypressDelay = 1;

				// show briefly on the screen
				debugText = string.Format( "Debug shadows {0}.", scene.ShowDebugShadows ? "on" : "off" );

				// show for 2 seconds
				debugTextDelay = 2.0f;
			}

			this.animState.AddTime( evt.TimeSinceLastFrame );
		}

		/// <summary>
		///		Method used to cycle through the shadow techniques.
		/// </summary>
		protected void ChangeShadowTechnique()
		{
			this.currentShadowTechnique = ++this.currentShadowTechnique % this.shadowTechniques.Length;

			scene.ShadowTechnique = this.shadowTechniques[ this.currentShadowTechnique ];

			var direction = new Vector3();

			switch ( this.shadowTechniques[ this.currentShadowTechnique ] )
			{
				case ShadowTechnique.StencilAdditive:
					// fixed light, dim
					this.sunLight.CastShadows = true;

					this.light.Type = LightType.Point;
					this.light.CastShadows = true;
					this.light.Diffuse = this.minLightColor;
					this.light.Specular = ColorEx.White;
					this.light.SetAttenuation( 8000, 1, 0.0005f, 0 );

					break;

				case ShadowTechnique.StencilModulative:
					// Multiple lights cause obvious silhouette edges in modulative mode
					// So turn off shadows on the direct light
					// Fixed light, dim

					this.sunLight.CastShadows = false;

					// point light, movable, reddish
					this.light.Type = LightType.Point;
					this.light.CastShadows = true;
					this.light.Diffuse = this.minLightColor;
					this.light.Specular = ColorEx.White;
					this.light.SetAttenuation( 8000, 1, 0.0005f, 0 );

					break;

				case ShadowTechnique.TextureModulative:
					// Change fixed point light to spotlight
					// Fixed light, dim
					//sunLight.CastShadows = true;

					this.light.Type = LightType.Spotlight;
					this.light.Direction = Vector3.NegativeUnitZ;
					this.light.CastShadows = true;
					this.light.Diffuse = this.minLightColor;
					this.light.Specular = ColorEx.White;
					this.light.SetAttenuation( 8000, 1, 0.0005f, 0 );
					this.light.SetSpotlightRange( 80, 90 );

					break;
			}

			// show briefly on the screen
			debugText = string.Format( "Using {0} Technique.", this.shadowTechniqueDescriptions[ this.currentShadowTechnique ] );
		}
	}

	/// <summary>
	///		This class 'wibbles' the light and billboard.
	/// </summary>
	public class LightWibbler : IControllerValue<Real>
	{
		#region Fields

		protected Billboard billboard;
		protected ColorEx colorRange;
		protected Real intensity;
		protected Light light;
		protected ColorEx minColor;
		protected Real minSize;
		protected Real sizeRange;

		#endregion Fields

		#region Constructor

		public LightWibbler( Light light, Billboard billboard, ColorEx minColor, ColorEx maxColor, Real minSize, Real maxSize )
		{
			this.light = light;
			this.billboard = billboard;
			this.minColor = minColor;
			this.colorRange.r = maxColor.r - minColor.r;
			this.colorRange.g = maxColor.g - minColor.g;
			this.colorRange.b = maxColor.b - minColor.b;
			this.minSize = minSize;
			this.sizeRange = maxSize - minSize;
		}

		#endregion Constructor

		#region IControllerValue<Real> Members

		public Real Value
		{
			get
			{
				return this.intensity;
			}
			set
			{
				this.intensity = value;

				var newColor = new ColorEx();

				// Attenuate the brightness of the light
				newColor.r = this.minColor.r + ( this.colorRange.r * this.intensity );
				newColor.g = this.minColor.g + ( this.colorRange.g * this.intensity );
				newColor.b = this.minColor.b + ( this.colorRange.b * this.intensity );

				this.light.Diffuse = newColor;
				this.billboard.Color = newColor;

				// set billboard size
				Real newSize = this.minSize + ( this.intensity * this.sizeRange );
				this.billboard.SetDimensions( newSize, newSize );
			}
		}

		#endregion
	}
}
