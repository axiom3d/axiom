#region Namespace Declarations

using System;
using System.ComponentModel.Composition;
using System.IO;

using Axiom.Animating;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;
using Axiom.Overlays;
using Axiom.ParticleSystems;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>Demonstrates simulation of Water mesh</summary>
#if !(WINDOWS_PHONE || XBOX || XBOX360)
	[Export( typeof( TechDemo ) )]
#endif
	public class Water : TechDemo
	{
		//najak: Change this back to TechDemo, if new controls are accepted by Axiom

		#region Fields

		// Demo Settings (tweak these and the recompile) - can't be adjusted run-time.
		protected static int CMPLX = 64; // watch out - number of polys is 2*ACCURACY*ACCURACY !
		protected static float PLANE_SIZE = 3000.0f;

		// General Fields
		protected OverlayElementManager GuiMgr;
		protected Overlay waterOverlay;
		protected ParticleSystem particleSystem;
		protected ParticleEmitter particleEmitter;

		// Fields Specific to Water Demo objects
		protected SceneNode headNode;
		protected WaterMesh waterMesh;
		protected Entity waterEntity;
		protected Entity planeEnt;
		protected float animTime;
		protected AnimationState animState;
		protected Plane reflectionPlane;
		protected SceneNode lightNode;
		protected BillboardSet lightSet;
		protected int lightModeIndex;
		protected string lightMode;

		// Demo Run-Time settings
		protected int materialNumber = 8;
		protected bool skyBoxOn;
		protected float headDepth = 2.5f;
		protected float headSpeed = 1.0f;
		protected bool rainOn;
		protected bool trackingOn;
		protected int changeSpeed; // scales speed of parameter adjustments
		protected int RAIN_HEIGHT_RANDOM = 5;
		protected int RAIN_HEIGHT_CONSTANT = 5;
		protected Random RAND; // random number generator

		// fields used by AnimateHead function (najak: were local statics)
		protected double[] adds = new double[ 4 ];
		protected double[] sines = new double[ 4 ];
		protected Vector3 oldPos = Vector3.UnitZ;

		// Input/Update Intervals (note: prevents some math rounding errors from extremely high update rates)
		protected float inputInterval = 0.01f, inputTimer = 0.01f;
		protected float modeInterval = 1f, modeTimer = 1f;
		protected float statsInterval = 1f, statsTimer = 1f;

		#endregion Fields

		#region Methods

		// Just override the mandatory create scene method
		public override void CreateScene()
		{
			this.RAND = new Random( 0 ); // najak: use a time-based seed
			this.GuiMgr = OverlayManager.Instance.Elements;
			scene.AmbientLight = new ColorEx( 0.75f, 0.75f, 0.75f ); // default Ambient Light
			// Customize Controls - speed up camera and slow down the input update rate
			camSpeed = 5.0f;
			this.inputInterval = this.inputTimer = 0.02f;

			// Create water mesh and entity, and attach to sceneNode
			this.waterMesh = new WaterMesh( "WaterMesh", PLANE_SIZE, CMPLX );
			this.waterEntity = scene.CreateEntity( "WaterEntity", "WaterMesh" );
			SceneNode waterNode = scene.RootSceneNode.CreateChildSceneNode();
			waterNode.AttachObject( this.waterEntity );

			// Add Ogre head, give it it's own node
			this.headNode = waterNode.CreateChildSceneNode();
			Entity ent = scene.CreateEntity( "head", "ogrehead.mesh" );
			this.headNode.AttachObject( ent );

			// Create the camera node, set its position & attach camera
			camera.Yaw( -45f );
			camera.Move( new Vector3( 1500f, 700f, PLANE_SIZE + 700f ) );
			camera.LookAt( new Vector3( PLANE_SIZE / 2f, 300f, PLANE_SIZE / 2f ) );
			camera.SetAutoTracking( false, this.headNode ); // Autotrack the head, but it isn't working right

			//scene.SetFog(FogMode.Exp, ColorEx.White, 0.000020f); // add Fog for fun, cuz we can

			// show overlay
			this.waterOverlay = OverlayManager.Instance.GetByName( "Example/WaterOverlay" );
			this.waterOverlay.Show();

			// Create Rain Emitter, but default Rain to OFF
			this.particleSystem = ParticleSystemManager.Instance.CreateSystem( "rain", "Examples/Water/Rain" );
			if ( this.particleSystem.Emitters.Count > 0 )
			{
				this.particleEmitter = this.particleSystem.GetEmitter( 0 );
				this.particleEmitter.EmissionRate = 0f;
			}

			// Attach Rain Emitter to SceneNode, and place it 3000f above the water surface
			SceneNode rNode = scene.RootSceneNode.CreateChildSceneNode();
			rNode.Translate( new Vector3( PLANE_SIZE / 2.0f, 3000, PLANE_SIZE / 2.0f ) );
			rNode.AttachObject( this.particleSystem );
			this.particleSystem.FastForward( 20 ); // Fastforward rain to make it look natural

			// It can't be set in .particle file, and we need it ;)
			//particleSystem.Origin = BillboardOrigin.BottomCenter;

			// Set Lighting
			this.lightNode = scene.RootSceneNode.CreateChildSceneNode();
			this.lightSet = scene.CreateBillboardSet( "Lights", 20 );
			this.lightSet.MaterialName = "Particles/Flare";
			this.lightNode.AttachObject( this.lightSet );
			SetLighting( "Ambient" ); // Add Lights - added by Najak to show lighted Water conditions - cool!

			#region STUBBED LIGHT ANIMATION

			// Create a new animation state to track this
			// TODO: Light Animation not working.
			//this.animState = scene.CreateAnimationState("WaterLight");
			//this.animState.Time = 0f;
			//this.animState.IsEnabled = false;

			// set up spline animation of light node.  Create random Spline
			Animation anim = scene.CreateAnimation( "WaterLight", 20 );
			AnimationTrack track = anim.CreateNodeTrack( 0, this.lightNode );
			var key = (TransformKeyFrame)track.CreateKeyFrame( 0 );
			for ( int ff = 1; ff <= 19; ff++ )
			{
				key = (TransformKeyFrame)track.CreateKeyFrame( ff );
				var rand = new Random( 0 );
				var lpos = new Vector3( (float)rand.NextDouble() % (int)PLANE_SIZE, //- PLANE_SIZE/2,
										(float)rand.NextDouble() % 300 + 100, (float)rand.NextDouble() % (int)PLANE_SIZE ); //- PLANE_SIZE/2
				key.Translate = lpos;
			}
			key = (TransformKeyFrame)track.CreateKeyFrame( 20 );

			#endregion STUBBED LIGHT ANIMATION

			// Initialize the Materials/Demo
			UpdateMaterial();
			UpdateInfoParamC();
			UpdateInfoParamD();
			UpdateInfoParamU();
			UpdateInfoParamT();
			UpdateInfoNormals();
			UpdateInfoHeadDepth();
			UpdateInfoSkyBox();
			UpdateInfoHeadSpeed();
			UpdateInfoLights();
			UpdateInfoTracking();

			// Init Head Animation:  Load adds[] elements - Ogre head animation
			this.adds[ 0 ] = 0.3f;
			this.adds[ 1 ] = -1.6f;
			this.adds[ 2 ] = 1.1f;
			this.adds[ 3 ] = 0.5f;
			this.sines[ 0 ] = 0;
			this.sines[ 1 ] = 100;
			this.sines[ 2 ] = 200;
			this.sines[ 3 ] = 300;
		}

		// end CreateScene()

		private void SetLighting( string mode )
		{
			// Clear Current Lights and start over
			// TODO: Add ClearLights
			//this.scene.ClearLights();
			this.lightSet.Clear();


			// Local Variable declarations
			var modeList = new[]
                           {
                               "Ambient", "SunLight", "Colors"
                           }; // add "Motion"
			Light l;
			scene.AmbientLight = new ColorEx( 0.05f, 0.05f, 0.05f ); // default is low ambient light


			// Set next Light Mode
			if ( mode == "next" )
			{
				this.lightModeIndex = ( ++this.lightModeIndex ) % modeList.Length;
			}
			this.lightMode = modeList[ this.lightModeIndex % modeList.Length ];

			switch ( this.lightMode )
			{
				case "SunLight":
					scene.RemoveAllLights();
					// Add Sun - Up and to the Left
					for ( int i = 0; i < 3; i++ )
					{
						l = AddLight( "DirLight" + i.ToString(), new Vector3( -PLANE_SIZE / 2f, PLANE_SIZE / 2f, PLANE_SIZE / 2f ), new ColorEx( 1f, 1f, 1f, 1f ), LightType.Directional );
						l.Direction = new Vector3( 1f, -0.5f, 0f );
						l.SetAttenuation( 10000f, 0, 0, 0 );
					}
					scene.AmbientLight = ColorEx.White; // default is low ambient light
					break;

				case "Colors":
					float lightScale = 1f;
					float lightDist = PLANE_SIZE; // / lightScale;
					float lightHeight = 300f / lightScale;
					this.lightNode.ScaleBy( new Vector3( lightScale, lightScale, lightScale ) );

					// Create a Light
					AddLight( "Lt1", new Vector3( lightDist, lightHeight, lightDist ), ColorEx.Red, LightType.Point );
					AddLight( "Lt2", new Vector3( lightDist, lightHeight, 0 ), ColorEx.Purple, LightType.Point );
					AddLight( "Lt3", new Vector3( 0, lightHeight, lightDist ), ColorEx.Blue, LightType.Point );
					AddLight( "Lt4", new Vector3( 0, lightHeight, 0 ), ColorEx.DarkOrange, LightType.Point );

					// Center Spotlight showing down on center of WaterMesh
					l = AddLight( "LtMid", new Vector3( 1500f, 1000f, 1500f ), ColorEx.Red, LightType.Spotlight );
					l.Direction = -1 * Vector3.UnitY;
					l.SetSpotlightRange( 60f, 70f, 90f );

					// Add Light to OgreHead coming out his forehead
					// TODO/BUG: Must alter Light name to avoid Axiom Exception.  Can't re-attach same light object to HeadNode
					string ltName = "LtHead" + this.RAND.NextDouble().ToString();
					l = scene.CreateLight( ltName );
					l.Position = new Vector3( 0, 20f, 0 );
					l.Type = LightType.Spotlight;
					l.SetSpotlightRange( 40f, 20f, 20f );
					l.Diffuse = ColorEx.Yellow;
					l.SetAttenuation( 1000000f, 0f, 0, 0.0000001f ); // Make lights go a long way
					l.Direction = new Vector3( 0, -0.1f, 1f );

					this.headNode.AttachObject( l );
					break;

				case "Motion":

					//l.SetAttenuation(5000f, 0f, 0f, 0f);
					//SceneNode lightNode = scene.RootSceneNode.CreateChildSceneNode();
					//lightNode.Lights.Add(l);
					break;

				default: // "Ambient" mode
					scene.AmbientLight = ColorEx.LightGray; // set Ambient Light
					break;
			}
		}

		private Light AddLight( string name, Vector3 pos, ColorEx color, LightType type )
		{
			Light l = scene.CreateLight( name );
			l.Position = pos;
			l.Type = type;
			l.Diffuse = color;
			l.SetAttenuation( 1000000f, 0f, 0, 0.0000001f ); // Make lights go a long way
			Billboard lightBoard = this.lightSet.CreateBillboard( pos, color );

			return l;
		}

		#endregion Methods

		#region Water Class EVENT HANDLERS - Custom Frame Update Functions

		#region RAPID UPDATE FUNCTIONS

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			base.OnFrameStarted( source, evt );
			if ( evt.StopRendering )
			{
				return;
			}

			// Limit user input update rate, to prevent math rounding errors from deltas too small
			//   Note: Slowing down input queries will speed up Frame Rates, not slow them down.
			if ( ( this.inputTimer += evt.TimeSinceLastFrame ) >= this.inputInterval )
			{
				//e.TimeSinceLastFrame = this.inputTimer;
				//base.OnFrameStarted(source, e); // do the normal demo frame processing first
				input.Capture(); // Read Keyboard and Mouse inputs
				RapidUpdate(); // Process rapid inputs, like camera motion or settings adjustments

				// Process User Requested Mode Changes
				if ( this.modeTimer > this.modeInterval )
				{
					ModeUpdate();
				}
				else
				{
					this.modeTimer += this.inputTimer;
				} // only increment when below, to save CPU

				// Update Performance Stats on Interval timer
				if ( ( this.statsTimer += this.inputTimer ) > this.statsInterval )
				{
					UpdateStats();
				}

				this.inputTimer = 0f;
			}
		}

		public void AnimateHead( float timeSinceLastFrame )
		{
			//animState.AddTime(timeSinceLastFrame);

			for ( int i = 0; i < 4; i++ )
			{
				this.sines[ i ] += this.adds[ i ] * this.headSpeed * timeSinceLastFrame;
			}
			float tx = (float)( ( System.Math.Sin( this.sines[ 0 ] ) + System.Math.Sin( this.sines[ 1 ] ) ) / 4 + 0.5 ) * ( CMPLX - 2 ) + 1;
			float ty = (float)( ( System.Math.Sin( this.sines[ 2 ] ) + System.Math.Sin( this.sines[ 3 ] ) ) / 4 + 0.5 ) * ( CMPLX - 2 ) + 1;

			// Push water down beneath the Ogre Head
			this.waterMesh.Push( tx, ty, this.headDepth, 150f, this.headSpeed, false );

			float step = PLANE_SIZE / CMPLX;
			this.headNode.ResetToInitialState();

			var newPos = new Vector3( step * tx, 80f - 40f * this.headDepth, step * ty );
			Vector3 diffPos = newPos - this.oldPos;
			Quaternion headRotation = Vector3.UnitZ.GetRotationTo( diffPos );
			this.oldPos = newPos;
			this.headNode.ScaleBy( new Vector3( 3.0f, 3.0f, 3.0f ) );
			this.headNode.Translate( newPos );
			this.headNode.Rotate( headRotation );
		}

		protected void ProcessRain()
		{
			foreach ( Particle p in this.particleSystem.Particles )
			{
				Vector3 ppos = p.Position;
				if ( ppos.y <= 0 && p.timeToLive > 0 )
				{
					// hits the water!
					p.timeToLive = 0.0f; // delete particle
					// push the water
					float x = ppos.x / PLANE_SIZE * CMPLX;
					float y = ppos.z / PLANE_SIZE * CMPLX;
					float h = (float)this.RAND.NextDouble() % this.RAIN_HEIGHT_RANDOM + this.RAIN_HEIGHT_CONSTANT * 2;
					if ( x < 1 )
					{
						x = 1;
					}
					if ( x > CMPLX - 1 )
					{
						x = CMPLX - 1;
					}
					if ( y < 1 )
					{
						y = 1;
					}
					if ( y > CMPLX - 1 )
					{
						y = CMPLX - 1;
					}
					this.waterMesh.PushDown( x, y, -h );
					//TODO: to implement WaterCircles, this is where you would create each new WaterCircle
				}
			}
		}

		/// <summary>Handle Inputs that Move and Turn the Camera.  Fast Update Rate. </summary>
		protected void RapidUpdate()
		{
			if ( !RapidUpdateCustom() )
			{
				return; // Give Demo first shot at making the update
			}

			camAccel = Vector3.Zero; // reset acceleration zero
			float scaleMove = 200 * this.inputTimer; // motion scalar
			float scaleTurn = 100 * this.inputTimer; // turn rate scalar

			// Disable Mouse Events if Right-Mouse clicked (control is given to the custom Demo)
			bool mouseEn = ( !input.IsMousePressed( MouseButtons.Right ) );

			// Keys that move camera.  Mouse-Wheel elevates camera
			if ( input.IsKeyPressed( KeyCodes.Left ) )
			{
				camAccel.x = -0.5f;
			} // move left
			if ( input.IsKeyPressed( KeyCodes.Right ) )
			{
				camAccel.x = 0.5f;
			} // move right
			if ( input.IsKeyPressed( KeyCodes.Up ) )
			{
				camAccel.z = -1;
			} // move forward
			if ( input.IsKeyPressed( KeyCodes.Down ) )
			{
				camAccel.z = 1;
			} // move backward
			if ( mouseEn )
			{
				camAccel.y += (float)( input.RelativeMouseZ * 0.1 ); // MouseWheel elevates camera
			}

			// When Mouse button pressed, Motion accelerates instead of turns camera
			if ( mouseEn && input.IsMousePressed( MouseButtons.Left ) )
			{
				camAccel.x += input.RelativeMouseX * 0.3f; // side motion
				camAccel.z += input.RelativeMouseY * 0.5f; // forward motion
			}

			// Calculate Camera Velocity and Location deltas
			camVelocity += ( camAccel * scaleMove * camSpeed );
			camera.MoveRelative( camVelocity * this.inputTimer );

			// Now dampen the Velocity - only if user is not accelerating
			if ( camAccel == Vector3.Zero )
			{
				camVelocity *= ( 1 - ( 4 * this.inputTimer ) );
			}

			// Keyboard arrows change Yaw/Pitch of camera
			if ( input.IsKeyPressed( KeyCodes.Left ) )
			{
				camera.Yaw( scaleTurn );
			}
			if ( input.IsKeyPressed( KeyCodes.Right ) )
			{
				camera.Yaw( -scaleTurn );
			}
			if ( input.IsKeyPressed( KeyCodes.Up ) )
			{
				camera.Pitch( scaleTurn );
			}
			if ( input.IsKeyPressed( KeyCodes.Down ) )
			{
				camera.Pitch( -scaleTurn );
			}

			// Mouse motion changes Yaw/Pitch of camera
			if ( mouseEn && !input.IsMousePressed( MouseButtons.Left ) )
			{
				camera.Yaw( -input.RelativeMouseX * 0.13f );
				camera.Pitch( -input.RelativeMouseY * 0.13f );
			}
		}

		// end ReadUserMotionInputs()


		/// <summary>Process User Inputs to change Axiom Render Mode or Print Screen.  Slow Update Rate.</summary>
		protected void ModeUpdate()
		{
			if ( !ModeUpdateCustom() )
			{
				return; // Give Demo first shot at making the update
			}

			// 'R' Toggles Render Mode
			if ( input.IsKeyPressed( KeyCodes.R ) )
			{
				switch ( camera.PolygonMode )
				{
					case PolygonMode.Points:
						camera.PolygonMode = PolygonMode.Solid;
						break;
					case PolygonMode.Solid:
						camera.PolygonMode = PolygonMode.Wireframe;
						break;
					case PolygonMode.Wireframe:
						camera.PolygonMode = PolygonMode.Points;
						break;
				}
				HandleUserModeInput( string.Format( "Rendering mode changed to '{0}'.", camera.PolygonMode ) );
			}

			// 'T' Toggles Texture Settings
			if ( input.IsKeyPressed( KeyCodes.T ) )
			{
				switch ( filtering )
				{
					case TextureFiltering.Bilinear:
						filtering = TextureFiltering.Trilinear;
						aniso = 1;
						break;
					case TextureFiltering.Trilinear:
						filtering = TextureFiltering.Anisotropic;
						aniso = 8;
						break;
					case TextureFiltering.Anisotropic:
						filtering = TextureFiltering.Bilinear;
						aniso = 1;
						break;
				}
				// set the new default texture settings
				MaterialManager.Instance.SetDefaultTextureFiltering( filtering );
				MaterialManager.Instance.DefaultAnisotropy = aniso;
				HandleUserModeInput( string.Format( "Texture Filtering changed to '{0}'.", filtering ) );
			}

			// Hide/Show Bounding Boxes and Overlays (besides debug overlay)
			if ( input.IsKeyPressed( KeyCodes.B ) )
			{
				scene.ShowBoundingBoxes = !scene.ShowBoundingBoxes;
				HandleUserModeInput( string.Format( "Show Bounding Boxes = {0}.", scene.ShowBoundingBoxes ) );
			}
			if ( input.IsKeyPressed( KeyCodes.F ) )
			{
				viewport.ShowOverlays = !viewport.ShowOverlays;
				HandleUserModeInput( string.Format( "Show Overlays = {0}.", viewport.ShowOverlays ) );
			}

#if !( SILVERLIGHT || XBOX || XBOX360 )
			// 'P' Captures Screenshot (like 'Print' command)
			if ( input.IsKeyPressed( KeyCodes.P ) )
			{
				// Save Screenshot to unique Filename (indexed)
				string[] temp = Directory.GetFiles( Environment.CurrentDirectory, "screenshot*.jpg" );
				string fileName = string.Format( "screenshot{0}.jpg", temp.Length + 1 );
				TakeScreenshot( fileName );
				HandleUserModeInput( string.Format( "Wrote screenshot '{0}'.", fileName ) );
			}
#endif
		}

		// end ReadUserModeInputs()

		// Process Rapid Inputs (adjust Demo settings)
		//protected override bool RapidUpdateCustom() {
		protected bool RapidUpdateCustom()
		{
			// Update Animations and Water Mesh
			AnimateHead( this.inputTimer ); // animate the Ogre Head
			ProcessRain(); // Process the Rain
			this.waterMesh.UpdateMesh( this.inputTimer ); // Update the Water Mesh (i.e. waves)

			// Press Left-SHIFT to speed up rate of change for adjust demo parameters
			this.changeSpeed = (int)( this.inputTimer * 1000 ); // round to nearest millisecond, use bit shift for speed
			if ( input.IsKeyPressed( KeyCodes.LeftShift ) )
			{
				this.changeSpeed *= 10;
			} // multiply by 8

			// Adjust Demo settings (mostly WaterMesh attributes) - Head height, and Water Properties
			if ( AdjustRange( ref this.headDepth, KeyCodes.J, KeyCodes.U, 0, 10, 0.0005f ) )
			{
				UpdateInfoHeadDepth();
			}
			if ( AdjustRange( ref this.waterMesh.PARAM_C, KeyCodes.D2, KeyCodes.D1, 0, 10, 0.0001f ) )
			{
				UpdateInfoParamC();
			}
			if ( AdjustRange( ref this.waterMesh.PARAM_D, KeyCodes.D4, KeyCodes.D3, 0.1f, 10, 0.0001f ) )
			{
				UpdateInfoParamD();
			}
			if ( AdjustRange( ref this.waterMesh.PARAM_U, KeyCodes.D6, KeyCodes.D5, -2f, 10, 0.0001f ) )
			{
				UpdateInfoParamU();
			}
			if ( AdjustRange( ref this.waterMesh.PARAM_T, KeyCodes.D8, KeyCodes.D7, 0, 10, 0.0001f ) )
			{
				UpdateInfoParamT();
			}
			if ( AdjustRange( ref this.headSpeed, KeyCodes.D0, KeyCodes.D9, 0, 3, 0.0001f ) )
			{
				UpdateInfoHeadSpeed();
			}
			return true;
		}

		// GUI Updaters
		private void UpdateInfoHeadDepth()
		{
			this.GuiMgr.GetElement( "Example/Water/Depth" ).Text = "[U/J]Head depth: " + this.headDepth.ToString();
		}

		private void UpdateInfoParamC()
		{
			this.GuiMgr.GetElement( "Example/Water/Param_C" ).Text = "[1/2]Ripple speed: " + this.waterMesh.PARAM_C.ToString();
		}

		private void UpdateInfoParamD()
		{
			this.GuiMgr.GetElement( "Example/Water/Param_D" ).Text = "[3/4]Distance: " + this.waterMesh.PARAM_D.ToString();
		}

		private void UpdateInfoParamU()
		{
			this.GuiMgr.GetElement( "Example/Water/Param_U" ).Text = "[5/6]Viscosity: " + this.waterMesh.PARAM_U.ToString();
		}

		private void UpdateInfoParamT()
		{
			this.GuiMgr.GetElement( "Example/Water/Param_T" ).Text = "[7/8]Frame time: " + this.waterMesh.PARAM_T.ToString();
		}

		private void UpdateInfoHeadSpeed()
		{
			this.GuiMgr.GetElement( "Example/Water/HeadSpeed" ).Text = "[9/0]Head Speed: " + this.headSpeed.ToString();
		}

		// Adjust Demo parameter value ('val')
		private bool AdjustRange( ref float val, KeyCodes plus, KeyCodes minus, float min, float max, float chg )
		{
			if ( input.IsKeyPressed( plus ) )
			{
				val += ( chg * this.changeSpeed );
				if ( val > max )
				{
					val = max;
				}
				return true;
			}
			if ( input.IsKeyPressed( minus ) )
			{
				val -= ( chg * this.changeSpeed );
				if ( val < min )
				{
					val = min;
				}
				return true;
			}
			return false;
		}

		#endregion RAPID UPDATE FUNCTIONS

		#region MODE UPDATE LOGIC

		// Mode Updates on Interval Timer
		// protected override bool ModeUpdateCustom() {
		protected bool ModeUpdateCustom()
		{
			// Process Mode Toggle Keys (use delay to avoid rapid flicker between modes)
			if ( input.IsKeyPressed( KeyCodes.Space ) )
			{
				ToggleMode( "Rain" );
			}
			if ( input.IsKeyPressed( KeyCodes.N ) )
			{
				ToggleMode( "Normals" );
			}
			if ( input.IsKeyPressed( KeyCodes.M ) )
			{
				ToggleMode( "Material" );
			}
			if ( input.IsKeyPressed( KeyCodes.K ) )
			{
				ToggleMode( "Skybox" );
			}
			if ( input.IsKeyPressed( KeyCodes.L ) )
			{
				ToggleMode( "Lights" );
			}
			if ( input.IsKeyPressed( KeyCodes.X ) )
			{
				ToggleMode( "Tracking" );
			}
			return true;
		}

		// Toggle Selected Mode and Reset the Timer
		protected void ToggleMode( string mode )
		{
			switch ( mode )
			{
				case "Rain":
					this.rainOn = !this.rainOn;
					this.particleEmitter.EmissionRate = ( ( this.rainOn ) ? 120.0f : 0.0f );
					UpdateInfoRain();
					HandleUserModeInput( string.Format( "Set Rain = '{0}'.", ( ( this.rainOn ) ? "On" : "Off" ) ) );
					break;
				case "Normals":
					this.waterMesh.useFakeNormals = !this.waterMesh.useFakeNormals;
					UpdateInfoNormals();
					HandleUserModeInput( string.Format( "Set Normal Calculations = '{0}'.", ( ( this.waterMesh.useFakeNormals ) ? "Fake" : "Real" ) ) );
					break;
				case "Material":
					this.materialNumber++;
					UpdateMaterial();
					break;
				case "Skybox":
					this.skyBoxOn = !this.skyBoxOn;
					scene.SetSkyBox( this.skyBoxOn, "Examples/SceneSkyBox2", 1000.0f );
					UpdateInfoSkyBox();
					HandleUserModeInput( string.Format( "Set SkyBox = '{0}'.", this.skyBoxOn.ToString() ) );
					break;
				case "Lights":
					SetLighting( "next" );
					UpdateInfoLights();
					HandleUserModeInput( string.Format( "Set Lighting Mode = '{0}'.", this.lightMode ) );
					break;
				case "Tracking":
					this.trackingOn = !this.trackingOn;
					camera.SetAutoTracking( this.trackingOn, this.headNode );
					UpdateInfoTracking();
					HandleUserModeInput( string.Format( "Set Camera Tracking = '{0}'.", this.trackingOn.ToString() ) );
					break;
			}
			this.modeTimer = 0f;
		}

		// GUI updaters
		private void UpdateInfoLights()
		{
			this.GuiMgr.GetElement( "Example/Water/Lights" ).Text = "[L]Lights: " + this.lightMode;
		}

		private void UpdateInfoNormals()
		{
			this.GuiMgr.GetElement( "Example/Water/Normals" ).Text = "[N]Normals: " + ( ( this.waterMesh.useFakeNormals ) ? "Fake" : "Real" );
		}

		private void UpdateInfoSkyBox()
		{
			this.GuiMgr.GetElement( "Example/Water/SkyBox" ).Text = "[K]SkyBox: " + this.skyBoxOn.ToString();
		}

		private void UpdateInfoRain()
		{
			this.GuiMgr.GetElement( "Example/Water/Rain" ).Text = "[SPACE]Rain: " + ( ( this.rainOn ) ? "On" : "Off" );
		}

		private void UpdateInfoTracking()
		{
			this.GuiMgr.GetElement( "Example/Water/Tracking" ).Text = "[X]Tracking: " + ( ( this.trackingOn ) ? "On" : "Off" );
		}

		// Sets the WaterMesh Material and Updates the GUI
		private void UpdateMaterial()
		{
			String materialName = "Examples/Water" + this.materialNumber.ToString();
			var material = (Material)MaterialManager.Instance.GetByName( materialName );

			if ( material == null )
			{
				if ( this.materialNumber != 0 )
				{
					this.materialNumber = 0;
					UpdateMaterial();
					return;
				}
				else
				{
					throw new Exception( String.Format( "Material '{0}' doesn't exist!", materialName ) );
				}
			}
			this.waterEntity.MaterialName = materialName;
			this.GuiMgr.GetElement( "Example/Water/Material" ).Text = "[M]Material: " + materialName.Substring( 9 );
			HandleUserModeInput( string.Format( "Set Water Material = '{0}'.", materialName ) );
		}

		#endregion MODE UPDATE LOGIC

		/// <summary>Prints output to screen/log, and resets Input Timer to avoid rapid flicker.</summary>
		protected void HandleUserModeInput( string logText )
		{
			debugText = logText;
			UpdateStats();
			LogManager.Instance.Write( logText );
			this.modeTimer = 0f;
		}

		#endregion Water Class EVENT HANDLERS - Custom Frame Update Functions
	}

	// end Water class


	/// <summary>WaterMesh implements the water simulation.</summary>
	public class WaterMesh
	{
		#region Fields

		protected static HardwareBufferManager HwBufMgr = HardwareBufferManager.Instance;
		protected float ANIMATIONS_PER_SECOND = 100.0f;
		public float PARAM_C = 0.3f; // ripple speed
		public float PARAM_D = 0.4f; // distance
		public float PARAM_T = 0.13f; // time
		public float PARAM_U = 0.05f; // viscosity
		protected int cmplx;
		protected float cmplxAdj;
		protected int curBufNum;
		protected Vector3[ , , ] fNorms; // Face Normals (for each triangle)
		protected float lastAnimationTimeStamp;
		protected float lastFrameTime;
		protected float lastTimeStamp;
		protected Mesh mesh;
		protected String meshName;
		protected HardwareVertexBuffer normVBuf; //	HardwareVertexBufferSharedPtr normVertexBuffer ;
		protected int numFaces;
		protected int numVertices;

		protected HardwareVertexBuffer posVBuf; //	HardwareVertexBufferSharedPtr posVertexBuffer ;
		protected float size;
		protected SubMesh subMesh;
		protected HardwareVertexBuffer tcVBuf; //	HardwareVertexBufferSharedPtr texcoordsVertexBuffer ;
		public bool useFakeNormals = true;
		protected Vector3[ , ] vBuf; // Current Vertex Buffer
		protected Vector3[][ , ] vBufs; // Vertex Buffers (current, plus last two frame snapshots)
		protected Vector3[ , ] vNorms; // Vertex Normals

		#endregion Fields

		public WaterMesh( String meshName, float planeSize, int cmplx )
		{
			// najak R-F
			// Assign Fields to the Initializer values
			this.meshName = meshName;
			this.size = planeSize;
			this.cmplx = cmplx; // Number of Rows/Columns in the Water Grid representation
			this.cmplxAdj = (float)System.Math.Pow( ( cmplx / 64f ), 1.4f ) * 2;
			this.numFaces = 2 * (int)System.Math.Pow( cmplx, 2 ); // Each square is split into 2 triangles.
			this.numVertices = (int)System.Math.Pow( ( cmplx + 1 ), 2 ); // Vertex grid is (Complexity+1) squared

			// Allocate and initialize space for calculated Normals
			this.vNorms = new Vector3[ cmplx + 1, cmplx + 1 ]; // vertex Normals for each grid point
			this.fNorms = new Vector3[ cmplx, cmplx, 2 ]; // face Normals for each triangle

			// Create mesh and submesh to represent the Water
			this.mesh = MeshManager.Instance.CreateManual( meshName, ResourceGroupManager.DefaultResourceGroupName, null );
			this.subMesh = this.mesh.CreateSubMesh();
			this.subMesh.useSharedVertices = false;

			// Construct metadata to describe the buffers associated with the water submesh
			this.subMesh.vertexData = new VertexData();
			this.subMesh.vertexData.vertexStart = 0;
			this.subMesh.vertexData.vertexCount = this.numVertices;

			// Define local variables to point to the VertexData Properties
			VertexDeclaration vdecl = this.subMesh.vertexData.vertexDeclaration; // najak: seems like metadata
			VertexBufferBinding vbind = this.subMesh.vertexData.vertexBufferBinding; // najak: pointer to actual buffer

			//najak: Set metadata to describe the three vertex buffers that will be accessed.
			vdecl.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			vdecl.AddElement( 1, 0, VertexElementType.Float3, VertexElementSemantic.Normal );
			vdecl.AddElement( 2, 0, VertexElementType.Float2, VertexElementSemantic.TexCoords );

			// Prepare buffer for positions - todo: first attempt, slow
			// Create the Position Vertex Buffer and Bind it index 0 - Write Only
			this.posVBuf = HwBufMgr.CreateVertexBuffer( vdecl.Clone( 0 ), this.numVertices, BufferUsage.DynamicWriteOnly );
			vbind.SetBinding( 0, this.posVBuf );

			// Prepare buffer for normals - write only
			// Create the Normals Buffer and Bind it to index 1 - Write only
			this.normVBuf = HwBufMgr.CreateVertexBuffer( vdecl.Clone( 1 ), this.numVertices, BufferUsage.DynamicWriteOnly );
			vbind.SetBinding( 1, this.normVBuf );

			// Prepare Texture Coordinates buffer (static, written only once)
			// Creates a 2D buffer of 2D coordinates: (Complexity X Complexity), pairs.
			//    Each pair indicates the normalized coordinates of the texture to map to.
			//    (0,1.00), (0.02, 1.00), (0.04, 1.00), ... (1.00,1.00)
			//    (0,0.98), (0.02, 0.98), (0.04, 1.00), ... (1.00,0.98)
			//    ...
			//    (0,0.00), (0.02, 0.00), (0.04, 0.00), ... (1.00,0.00)
			// This construct is simple and is used to calculate the Texture map.
			// Todo: Write directly to the buffer, when Axiom supports this in safe manner
			var tcBufDat = new float[ cmplx + 1, cmplx + 1, 2 ];
			for ( int i = 0; i <= cmplx; i++ )
			{
				// 2D column iterator for texture map
				for ( int j = 0; j <= cmplx; j++ )
				{
					// 2D row iterator for texture map
					// Define the normalized(0..1) X/Y-coordinates for this element of the 2D grid
					tcBufDat[ i, j, 0 ] = (float)i / cmplx;
					tcBufDat[ i, j, 1 ] = 1.0f - ( (float)j / ( cmplx ) );
				}
			}

			// Now Create the actual hardware buffer to contain the Texture Coordinate 2d map.
			//   and Bind it to buffer index 2
			this.tcVBuf = HwBufMgr.CreateVertexBuffer( vdecl.Clone( 2 ), this.numVertices, BufferUsage.StaticWriteOnly );
			this.tcVBuf.WriteData( 0, this.tcVBuf.Size, tcBufDat, true );
			vbind.SetBinding( 2, this.tcVBuf );

			// Create a Graphics Buffer on non-shared vertex indices (3 points for each triangle).
			//  Since the water grid consist of [Complexity x Complexity] squares, each square is
			//  split into 2 right triangles 45-90-45.  That is how the water mesh is constructed.
			//  Therefore the number of faces = 2 * Complexity * Complexity
			var idxBuf = new ushort[ cmplx, cmplx, 6 ];
			for ( int i = 0; i < cmplx; i++ )
			{
				// iterate the rows
				for ( int j = 0; j < cmplx; j++ )
				{
					// iterate the columns
					// Define 4 corners of each grid
					var p0 = (ushort)( i * ( cmplx + 1 ) + j ); // top left point on square
					var p1 = (ushort)( i * ( cmplx + 1 ) + j + 1 ); // top right
					var p2 = (ushort)( ( i + 1 ) * ( cmplx + 1 ) + j ); // bottom left
					var p3 = (ushort)( ( i + 1 ) * ( cmplx + 1 ) + j + 1 ); // bottom right

					// Split Square Grid element into 2 adjacent triangles.
					idxBuf[ i, j, 0 ] = p2;
					idxBuf[ i, j, 1 ] = p1;
					idxBuf[ i, j, 2 ] = p0; // top-left triangle
					idxBuf[ i, j, 3 ] = p2;
					idxBuf[ i, j, 4 ] = p3;
					idxBuf[ i, j, 5 ] = p1; // bottom-right triangle
				}
			}
			// Copy Index Buffer to the Hardware Index Buffer
			HardwareIndexBuffer hdwrIdxBuf = HwBufMgr.CreateIndexBuffer( IndexType.Size16, 3 * this.numFaces, BufferUsage.StaticWriteOnly, true );
			hdwrIdxBuf.WriteData( 0, this.numFaces * 3 * 2, idxBuf, true );

			// Set index buffer for this submesh
			this.subMesh.indexData.indexBuffer = hdwrIdxBuf;
			this.subMesh.indexData.indexStart = 0;
			this.subMesh.indexData.indexCount = 3 * this.numFaces;

			//Prepare Vertex Position Buffers (Note: make 3, since each frame is function of previous two)
			this.vBufs = new Vector3[ 3 ][ , ];
			for ( int b = 0; b < 3; b++ )
			{
				this.vBufs[ b ] = new Vector3[ cmplx + 1, cmplx + 1 ];
				for ( int y = 0; y <= cmplx; y++ )
				{
					for ( int x = 0; x <= cmplx; x++ )
					{
						this.vBufs[ b ][ y, x ].x = ( x ) / (float)( cmplx ) * this.size;
						this.vBufs[ b ][ y, x ].y = 0;
						this.vBufs[ b ][ y, x ].z = ( y ) / (float)( cmplx ) * this.size;
					}
				}
			}

			this.curBufNum = 0;
			this.vBuf = this.vBufs[ this.curBufNum ];
			this.posVBuf.WriteData( 0, this.posVBuf.Size, this.vBufs[ 0 ], true );

			var meshBounds = new AxisAlignedBox( new Vector3( 0, 0, 0 ), new Vector3( this.size, 0, this.size ) );
			this.mesh.BoundingBox = meshBounds; //	mesh->_setBounds(meshBounds); // najak: can't find _setBounds()

			this.mesh.Load();
			this.mesh.Touch();
		}

		// end WaterMesh Constructor

		public void UpdateMesh( float timeSinceLastFrame )
		{
			this.lastFrameTime = timeSinceLastFrame;
			this.lastTimeStamp += timeSinceLastFrame;

			// do rendering to get ANIMATIONS_PER_SECOND
			while ( this.lastAnimationTimeStamp <= this.lastTimeStamp )
			{
				// switch buffer numbers
				this.curBufNum = ( this.curBufNum + 1 ) % 3;
				Vector3[ , ] vbuf0 = this.vBufs[ this.curBufNum ]; // new frame
				Vector3[ , ] vbuf1 = this.vBufs[ ( this.curBufNum + 2 ) % 3 ]; // 1-frame ago
				Vector3[ , ] vbuf2 = this.vBufs[ ( this.curBufNum + 1 ) % 3 ]; // 2-frames ago

				// Algorithm from http://collective.valve-erc.com/index.php?go=water_simulation
				double C = this.PARAM_C; // ripple speed
				double D = this.PARAM_D; // distance
				double U = this.PARAM_U; // viscosity
				double T = this.PARAM_T; // time
				var TERM1 = (float)( ( 4.0f - 8.0f * C * C * T * T / ( D * D ) ) / ( U * T + 2 ) );
				var TERM2 = (float)( ( U * T - 2.0f ) / ( U * T + 2.0f ) );
				var TERM3 = (float)( ( 2.0f * C * C * T * T / ( D * D ) ) / ( U * T + 2 ) );
				for ( int i = 1; i < this.cmplx; i++ )
				{
					// don't do anything with border values
					for ( int j = 1; j < this.cmplx; j++ )
					{
						vbuf0[ i, j ].y = TERM1 * vbuf1[ i, j ].y + TERM2 * vbuf2[ i, j ].y + TERM3 * ( vbuf1[ i, j - 1 ].y + vbuf1[ i, j + 1 ].y + vbuf1[ i - 1, j ].y + vbuf1[ i + 1, j ].y );
					}
				}

				this.lastAnimationTimeStamp += ( 1.0f / this.ANIMATIONS_PER_SECOND );
			}

			this.vBuf = this.vBufs[ this.curBufNum ];

			if ( this.useFakeNormals )
			{
				CalculateFakeNormals();
			}
			else
			{
				CalculateNormals();
			}

			// set vertex buffer
			this.posVBuf.WriteData( 0, this.posVBuf.Size, this.vBufs[ this.curBufNum ], true );
		}


		/// <summary>Calculate WaterMesh precise Normals for each Vertex on Grid</summary>
		public void CalculateNormals()
		{
			Vector3 p0, p1, p2, p3, fn1, fn2;

			// Initialize Vertex Normals to ZERO
			for ( int i = 0; i < this.cmplx + 1; i++ )
			{
				for ( int j = 0; j < this.cmplx + 1; j++ )
				{
					this.vNorms[ i, j ] = Vector3.Zero;
				}
			}

			// Calculate Normal for each Face, and add it to the normal for each Vertex
			for ( int i = 0; i < this.cmplx; i++ )
			{
				for ( int j = 0; j < this.cmplx; j++ )
				{
					// Define 4-points of this grid square (top-left, top-right, bottom-left, bottom-right)
					p0 = this.vBuf[ i, j ];
					p1 = this.vBuf[ i, j + 1 ];
					p2 = this.vBuf[ i + 1, j ];
					p3 = this.vBuf[ i + 1, j + 1 ];

					// Calc Face Normals for both Triangles of each grid square
					fn1 = ( ( p2 - p0 ) ).Cross( p1 - p0 );
					fn2 = ( ( p1 - p3 ) ).Cross( p2 - p3 );

					// Add Face Normals to the adjacent Vertex Normals
					this.vNorms[ i, j ] += fn1; // top left
					this.vNorms[ i, j + 1 ] += ( fn1 + fn2 ); // top right (adjacent to both triangles)
					this.vNorms[ i + 1, j ] += ( fn1 + fn2 ); // bottom left (adjacent to both triangles)
					this.vNorms[ i + 1, j + 1 ] += fn2; // bottom right
				}
			}
			// Normalize the Vertex normals, and write it to the Normal Buffer
			for ( int i = 0; i <= this.cmplx; i++ )
			{
				for ( int j = 0; j <= this.cmplx; j++ )
				{
					this.vNorms[ i, j ].Normalize();
				}
			}
			this.normVBuf.WriteData( 0, this.normVBuf.Size, this.vNorms, true );
		}

		/// <summary>Calculate Fake Normals (close but not precise, but faster) </summary>
		/// Note: Algorithm from Ogre was improved by Najak to provide same speed improvment with more accurate results.
		public void CalculateFakeNormals()
		{
			float d1, d2; // diagonal slopes across 2 grid squares
			float ycomp = 6f * this.size / this.cmplx; // Fixed y-component of each vertex normal

			for ( int i = 1; i < this.cmplx; i++ )
			{
				for ( int j = 1; j < this.cmplx; j++ )
				{
					// Take average slopes across two grids
					d1 = this.vBuf[ i - 1, j - 1 ].y - this.vBuf[ i + 1, j + 1 ].y;
					d2 = this.vBuf[ i + 1, j - 1 ].y - this.vBuf[ i - 1, j + 1 ].y;
					this.vNorms[ i, j ].x = this.vBuf[ i, j - 1 ].y - this.vBuf[ i, j + 1 ].y + d1 + d2; // x-only component
					this.vNorms[ i, j ].z = this.vBuf[ i - 1, j ].y - this.vBuf[ i + 1, j ].y + d1 - d2; // z-only component
					this.vNorms[ i, j ].y = ycomp;
					this.vNorms[ i, j ].Normalize();
				}
			}
			// Create Unit-Y Normals for Water Edges (no angle for edges)
			for ( int i = 0; i <= this.cmplx; i++ )
			{
				this.vNorms[ i, 0 ] = this.vNorms[ i, this.cmplx ] = this.vNorms[ 0, i ] = this.vNorms[ this.cmplx, i ] = Vector3.UnitY;
			}

			// Write Normals to Hardware Buffer
			this.normVBuf.WriteData( 0, this.normVBuf.Size, this.vNorms, true );
		}

		/// <summary>Emulates an object pushing water out of its way (usually down)</summary>
		public void PushDown( float fx, float fy, float depth )
		{
			// Ogre Wave Generation Logic - scale pressure according to time passed
			for ( var addx = (int)fx; addx <= (int)fx + 1; addx++ )
			{
				for ( var addy = (int)fy; addy <= (int)fy + 1; addy++ )
				{
					float diffy = fy - (float)System.Math.Floor( fy + addy );
					float diffx = fx - (float)System.Math.Floor( fx + addx );
					var dist = (float)System.Math.Sqrt( diffy * diffy + diffx * diffx );
					float power = 1 - dist;

					if ( power < 0 )
					{
						power = 0;
					}

					this.vBuf[ addy, addx ].y -= depth * power;
				}
			}
		}

		/// <summary>Emulates an object pushing water out of its way (usually down)</summary>
		public void Push( float fx, float fy, float depth, float height, float speed, bool absolute )
		{
			// Ogre Wave Generation Logic - scale pressure according to time passed
			for ( var addx = (int)fx; addx <= (int)fx + 1; addx++ )
			{
				for ( var addy = (int)fy; addy <= (int)fy + 1; addy++ )
				{
					float diffy = fy - (float)System.Math.Floor( (double)addy );
					float diffx = fx - (float)System.Math.Floor( (double)addx );
					var dist = (float)System.Math.Sqrt( diffy * diffy + diffx * diffx );
					float power = ( 1 - dist ) * this.cmplxAdj * speed;

					if ( power < 0 )
					{
						power = 0;
					}

					if ( absolute )
					{
						this.vBuf[ addy, addx ].y = depth * power;
					}
					else
					{
						this.vBuf[ addy, addx ].y -= depth * power;
					}
				}
			}
		}
	}

	// WaterMesh class
}
