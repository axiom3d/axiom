#region Namespace Declarations

using Axiom;
using Axiom.Core;
using Axiom.Input;
using Axiom.MathLib;

#endregion Namespace Declarations

namespace Axiom.Demos
{
    /// <summary>
    /// Summary description for Shadows.
    /// </summary>
    public class Shadows : TechDemo
    {
        Entity athene;
        AnimationState animState;
        Light light;
        Light sunLight;
        SceneNode lightNode;
        ColorEx minLightColor = new ColorEx( 0.3f, 0, 0 );
        ColorEx maxLightColor = new ColorEx( 0.5f, 0.3f, 0.1f );
        float minFlareSize = 40;
        float maxFlareSize = 80;

        string[] atheneMaterials = new string[] { 
			"Examples/Athene/NormalMapped",
			"Examples/Athene/Basic" 
		};

        string[] shadowTechniqueDescriptions = new string[] { 
			"Stencil Shadows (Additive)",
			"Stencil Shadows (Modulative)",
			"Texture Shadows (Modulative)",
			"None"
		};

        ShadowTechnique[] shadowTechniques = new ShadowTechnique[] { 
			ShadowTechnique.StencilAdditive,
			ShadowTechnique.StencilModulative,
			ShadowTechnique.TextureModulative,
			ShadowTechnique.None
		};

        int currentShadowTechnique = 0;

        protected override void CreateScene()
        {
            scene.ShadowTechnique = ShadowTechnique.StencilAdditive;

            // set ambient light off
            scene.AmbientLight = ColorEx.Black;

            // TODO Check based on caps
            int currentAtheneMaterial = 0;

            // fixed light, dim
            sunLight = scene.CreateLight( "SunLight" );
            sunLight.Type = LightType.Spotlight;
            sunLight.Position = new Vector3( 1000, 1250, 500 );
            sunLight.SetSpotlightRange( 30, 50 );
            Vector3 dir = -sunLight.Position;
            dir.Normalize();
            sunLight.Direction = dir;
            sunLight.Diffuse = new ColorEx( 0.35f, 0.35f, 0.38f );
            sunLight.Specular = new ColorEx( 0.9f, 0.9f, 1 );

            // point light, movable, reddish
            light = scene.CreateLight( "Light2" );
            light.Diffuse = minLightColor;
            light.Specular = ColorEx.White;
            light.SetAttenuation( 8000, 1, .0005f, 0 );

            // create light node
            lightNode = scene.RootSceneNode.CreateChildSceneNode( "MovingLightNode" );
            lightNode.AttachObject( light );

            // create billboard set
            BillboardSet bbs = scene.CreateBillboardSet( "LightBBS", 1 );
            bbs.MaterialName = "Examples/Flare";
            Billboard bb = bbs.CreateBillboard( Vector3.Zero, minLightColor );
            // attach to the scene
            lightNode.AttachObject( bbs );

            // create controller, after this is will get updated on its own
            WaveformControllerFunction func =
                new WaveformControllerFunction( WaveformType.Sine, 0.75f, 0.5f );

            LightWibbler val = new LightWibbler( light, bb, minLightColor, maxLightColor, minFlareSize, maxFlareSize );
            ControllerManager.Instance.CreateController( val, func );

            lightNode.Position = new Vector3( 300, 250, -300 );

            // create a track for the light
            Animation anim = scene.CreateAnimation( "LightTrack", 20 );
            // spline it for nice curves
            anim.InterpolationMode = InterpolationMode.Spline;
            // create a track to animate the camera's node
            AnimationTrack track = anim.CreateTrack( 0, lightNode );
            // setup keyframes
            KeyFrame key = track.CreateKeyFrame( 0 );
            key.Translate = new Vector3( 300, 250, -300 );
            key = track.CreateKeyFrame( 2 );
            key.Translate = new Vector3( 150, 300, -250 );
            key = track.CreateKeyFrame( 4 );
            key.Translate = new Vector3( -150, 350, -100 );
            key = track.CreateKeyFrame( 6 );
            key.Translate = new Vector3( -400, 200, -200 );
            key = track.CreateKeyFrame( 8 );
            key.Translate = new Vector3( -200, 200, -400 );
            key = track.CreateKeyFrame( 10 );
            key.Translate = new Vector3( -100, 150, -200 );
            key = track.CreateKeyFrame( 12 );
            key.Translate = new Vector3( -100, 75, 180 );
            key = track.CreateKeyFrame( 14 );
            key.Translate = new Vector3( 0, 250, 300 );
            key = track.CreateKeyFrame( 16 );
            key.Translate = new Vector3( 100, 350, 100 );
            key = track.CreateKeyFrame( 18 );
            key.Translate = new Vector3( 250, 300, 0 );
            key = track.CreateKeyFrame( 20 );
            key.Translate = new Vector3( 300, 250, -300 );

            // create a new animation state to track this
            animState = scene.CreateAnimationState( "LightTrack" );
            animState.IsEnabled = true;

            // Make light node look at origin, this is for when we
            // change the moving light to a spotlight
            lightNode.SetAutoTracking( true, scene.RootSceneNode );

            Mesh mesh = MeshManager.Instance.Load( "athene.mesh" );

            short srcIdx, destIdx;

            // the athene mesh requires tangent vectors
            if ( !mesh.SuggestTangentVectorBuildParams( out srcIdx, out destIdx ) )
            {
                mesh.BuildTangentVectors( srcIdx, destIdx );
            }

            SceneNode node = scene.RootSceneNode.CreateChildSceneNode();
            athene = scene.CreateEntity( "Athene", "athene.mesh" );
            athene.MaterialName = atheneMaterials[currentAtheneMaterial];
            node.AttachObject( athene );
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

            Plane plane = new Plane( Vector3.UnitY, -100 );
            MeshManager.Instance.CreatePlane(
                "MyPlane", plane, 1500, 1500, 20, 20, true, 1, 5, 5, Vector3.UnitZ );

            Entity planeEnt = scene.CreateEntity( "Plane", "MyPlane" );
            planeEnt.MaterialName = "Examples/Rockwall";
            planeEnt.CastShadows = false;
            scene.RootSceneNode.CreateChildSceneNode().AttachObject( planeEnt );

            if ( Root.Instance.RenderSystem.Name.StartsWith( "Direct" ) )
            {
                // In D3D, use a 1024x1024 shadow texture
                scene.SetShadowTextureSettings( 1024, 2 );
            }
            else
            {
                // Use 512x512 texture in GL since we can't go higher than the window res
                scene.SetShadowTextureSettings( 512, 2 );
            }

            scene.ShadowColor = new ColorEx( 0.5f, 0.5f, 0.5f );

            // incase infinite far distance is not supported
            camera.Far = 100000;
        }

        protected override void OnFrameStarted( object source, FrameEventArgs e )
        {
            base.OnFrameStarted( source, e );

            if ( input.IsKeyPressed( KeyCodes.O ) )
            {
                ChangeShadowTechnique();
            }

            animState.AddTime( e.TimeSinceLastFrame );
        }

        /// <summary>
        ///		Method used to cycle through the shadow techniques.
        /// </summary>
        protected void ChangeShadowTechnique()
        {
            currentShadowTechnique = ++currentShadowTechnique % shadowTechniques.Length;

            scene.ShadowTechnique = shadowTechniques[currentShadowTechnique];

            Vector3 direction = new Vector3();

            switch ( shadowTechniques[currentShadowTechnique] )
            {
                case ShadowTechnique.StencilAdditive:
                    // fixed light, dim
                    sunLight.CastShadows = true;

                    light.Type = LightType.Point;
                    light.CastShadows = true;
                    light.Diffuse = minLightColor;
                    light.Specular = ColorEx.White;
                    light.SetAttenuation( 8000, 1, 0.0005f, 0 );

                    break;

                case ShadowTechnique.StencilModulative:
                    // Multiple lights cause obvious silhouette edges in modulative mode
                    // So turn off shadows on the direct light
                    // Fixed light, dim

                    sunLight.CastShadows = false;

                    // point light, movable, reddish
                    light.Type = LightType.Point;
                    light.CastShadows = true;
                    light.Diffuse = minLightColor;
                    light.Specular = ColorEx.White;
                    light.SetAttenuation( 8000, 1, 0.0005f, 0 );

                    break;

                case ShadowTechnique.TextureModulative:
                    // Change fixed point light to spotlight
                    // Fixed light, dim
                    sunLight.CastShadows = true;

                    light.Type = LightType.Spotlight;
                    light.Direction = Vector3.NegativeUnitZ;
                    light.CastShadows = true;
                    light.Diffuse = minLightColor;
                    light.Specular = ColorEx.White;
                    light.SetAttenuation( 8000, 1, 0.0005f, 0 );
                    light.SetSpotlightRange( 80, 90 );

                    break;
            }
        }
    }

    /// <summary>
    ///		This class 'wibbles' the light and billboard.
    /// </summary>
    public class LightWibbler : IControllerValue
    {
        #region Fields

        protected Light light;
        protected Billboard billboard;
        protected ColorEx colorRange = new ColorEx();
        protected ColorEx minColor;
        protected float minSize;
        protected float sizeRange;
        protected float intensity;

        #endregion Fields

        #region Constructor

        public LightWibbler( Light light, Billboard billboard, ColorEx minColor,
            ColorEx maxColor, float minSize, float maxSize )
        {

            this.light = light;
            this.billboard = billboard;
            this.minColor = minColor;
            colorRange.r = maxColor.r - minColor.r;
            colorRange.g = maxColor.g - minColor.g;
            colorRange.b = maxColor.b - minColor.b;
            this.minSize = minSize;
            sizeRange = maxSize - minSize;
        }

        #endregion Constructor

        #region IControllerValue Members

        public float Value
        {
            get
            {
                return intensity;
            }
            set
            {
                intensity = value;

                ColorEx newColor = new ColorEx();

                // Attenuate the brightness of the light
                newColor.r = minColor.r + ( colorRange.r * intensity );
                newColor.g = minColor.g + ( colorRange.g * intensity );
                newColor.b = minColor.b + ( colorRange.b * intensity );

                light.Diffuse = newColor;
                billboard.Color = newColor;

                // set billboard size
                float newSize = minSize + ( intensity * sizeRange );
                billboard.SetDimensions( newSize, newSize );
            }
        }

        #endregion
    }
}

