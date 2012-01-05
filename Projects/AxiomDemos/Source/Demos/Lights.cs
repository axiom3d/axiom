#region Namespace Declarations

using System;

using Axiom.Animating;
using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;

using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Demos
{
	/// <summary>
	/// 	Summary description for Controllers.
	/// </summary>
	public class Lights : TechDemo
	{
		#region Member variables

		private SceneNode redYellowLightsNode;
		private SceneNode greenBlueLightsNode;
		private BillboardSet redYellowLights;
		private BillboardSet greenBlueLights;
		private Billboard redLightBoard;
		private Billboard greenLightBoard;
		private Billboard yellowLightBoard;
		private Billboard blueLightBoard;
		private Light redLight;
		private Light yellowLight;
		private Light greenLight;
		private Light blueLight;
		private IControllerFunction<float> redLightFunc;
		private IControllerFunction<float> yellowLightFunc;
		private IControllerFunction<float> greenLightFunc;
		private IControllerFunction<float> blueLightFunc;
		private IControllerValue<float> redLightFlasher;
		private IControllerValue<float> yellowLightFlasher;
		private IControllerValue<float> greenLightFlasher;
		private IControllerValue<float> blueLightFlasher;
		private List<AnimationState> animationStateList = new List<AnimationState>();

		#endregion Member variables

		#region Methods

		public override void CreateScene()
		{
			// set some ambient light
			scene.AmbientLight = new ColorEx( 1, 0.5f, 0.5f, 0.5f );

			// set a basic skybox
			scene.SetSkyBox( true, "Skybox/Space", 5000.0f );

			// create the ogre head
			Entity ogre = scene.CreateEntity( "OgreHead", "ogrehead.mesh" );

			// attach the ogre to the scene
			scene.RootSceneNode.AttachObject( ogre );

			setupLightTrails();
		}

		private void setupLightTrails()
		{
			Vector3 dir = new Vector3( -1.0f, -1.0f, 0.5f );
			dir.Normalize();

			Light l = scene.CreateLight( "light1" );
			l.Type = LightType.Directional;
			l.Direction = dir;

			RibbonTrail trail = scene.CreateRibbonTrail( "RibbonTrail" );
			trail.MaterialName = "Examples/LightRibbonTrail";
			trail.TrailLength = 400;
			trail.MaxChainElements = 80;
			trail.NumberOfChains = 2;
			scene.RootSceneNode.AttachObject( trail );

			// Create 3 nodes for trail to follow
			SceneNode animNode = scene.RootSceneNode.CreateChildSceneNode();
			animNode.Position = new Vector3( -50, -30, 0 ); //new Vector3(50,30,0);
			Animation anim = scene.CreateAnimation( "an1", 14 );
			anim.InterpolationMode = InterpolationMode.Spline;
			NodeAnimationTrack track = anim.CreateNodeTrack( 1, animNode );
			TransformKeyFrame kf = (TransformKeyFrame)track.CreateKeyFrame( 0 );
			kf.Translate = new Vector3( 50, 30, 0 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 2 );
			kf.Translate = new Vector3( 100, -30, 0 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 4 );
			kf.Translate = new Vector3( 120, -100, 150 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 6 );
			kf.Translate = new Vector3( 30, -100, 50 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 8 );
			kf.Translate = new Vector3( -50, 30, -50 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 10 );
			kf.Translate = new Vector3( -150, -20, -100 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 12 );
			kf.Translate = new Vector3( -50, -30, 0 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 14 );
			kf.Translate = new Vector3( 50, 30, 0 );

			AnimationState animState = scene.CreateAnimationState( "an1" );
			animState.IsEnabled = true;
			animationStateList.Add( animState );

			trail.SetInitialColor( 0, new ColorEx( 1.0f, 1.0f, 0.8f, 0f ) );
			trail.SetColorChange( 0, new ColorEx( 0.5f, 0.5f, 0.5f, 0.5f ) );
			trail.SetInitialWidth( 0, 5 );
			trail.AddNode( animNode );

			// Add light
			Light l2 = scene.CreateLight( "l2" );
			l2.Diffuse = trail.GetInitialColor( 0 );
			animNode.AttachObject( l2 );

			// Add billboard
			BillboardSet bbs = scene.CreateBillboardSet( "bb", 1 );
			bbs.MaterialName = "Examples/Flare";
			bbs.CreateBillboard( Vector3.Zero, trail.GetInitialColor( 0 ) );
			animNode.AttachObject( bbs );

			animNode = scene.RootSceneNode.CreateChildSceneNode();
			animNode.Position = new Vector3( -50, 100, 0 ); //new Vector3(50,30,0);
			anim = scene.CreateAnimation( "an2", 10 );
			anim.InterpolationMode = InterpolationMode.Spline;
			track = anim.CreateNodeTrack( 1, animNode );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 0 );
			kf.Translate = new Vector3( -50, 100, 0 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 2 );
			kf.Translate = new Vector3( -100, 150, -30 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 4 );
			kf.Translate = new Vector3( -200, 0, 40 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 6 );
			kf.Translate = new Vector3( 0, -150, 70 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 8 );
			kf.Translate = new Vector3( 50, 0, 30 );
			kf = (TransformKeyFrame)track.CreateKeyFrame( 10 );
			kf.Translate = new Vector3( -50, 100, 0 );

			animState = scene.CreateAnimationState( "an2" );
			animState.IsEnabled = true;
			animationStateList.Add( animState );

			trail.SetInitialColor( 1, new ColorEx( 1.0f, 0.0f, 1.0f, 0.4f ) );
			trail.SetColorChange( 1, new ColorEx( 0.5f, 0.5f, 0.5f, 0.5f ) );
			trail.SetInitialWidth( 1, 5 );
			trail.AddNode( animNode );

			// Add light
			l2 = scene.CreateLight( "l3" );
			l2.Diffuse = trail.GetInitialColor( 1 );
			animNode.AttachObject( l2 );

			// Add billboard
			bbs = scene.CreateBillboardSet( "bb2", 1 );
			bbs.MaterialName = "Examples/Flare";
			bbs.CreateBillboard( Vector3.Zero, trail.GetInitialColor( 1 ) );
			animNode.AttachObject( bbs );
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			// move the billboards around a bit
			foreach( AnimationState anim in animationStateList )
			{
				anim.AddTime( evt.TimeSinceLastFrame );
			}
			base.OnFrameStarted( source, evt );
		}

		#endregion Methods
	}

	public class LightFlasherControllerValue : IControllerValue<float>
	{
		private Billboard billboard;
		private Light light;
		private float intensity;
		private ColorEx maxColor;

		public LightFlasherControllerValue( Light light, Billboard billboard, ColorEx maxColor )
		{
			this.billboard = billboard;
			this.light = light;
			this.maxColor = maxColor;
		}

		#region IControllerValue Members

		public float Value
		{
			get { return intensity; }
			set
			{
				intensity = value;

				ColorEx newColor = new ColorEx();

				newColor.r = maxColor.r * intensity;
				newColor.g = maxColor.g * intensity;
				newColor.b = maxColor.b * intensity;

				billboard.Color = newColor;
				light.Diffuse = newColor;
			}
		}

		#endregion IControllerValue Members
	}
}
