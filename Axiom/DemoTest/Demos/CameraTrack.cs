using System;
using Axiom.Animating;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	/// 	Summary description for SplinePath.
	/// </summary>
	public class CameraTrack : TechDemo
	{
		#region Member variables
		
		private AnimationState animState = null;
		private SceneNode headNode = null;
	
		#endregion
		
		#region Constructors
		
		public CameraTrack()
		{
		}
		
		#endregion
		
		#region Methods
		
		protected override void CreateScene()
		{
			// set some ambient light
			sceneMgr.TargetRenderSystem.LightingEnabled = true;
			sceneMgr.AmbientLight = ColorEx.FromColor(System.Drawing.Color.Gray);

			// create a simple default point light
			Light light = sceneMgr.CreateLight("MainLight");
			light.Position = new Vector3(20, 80, 50);

			// create a plane for the plane mesh
			Plane p = new Plane();
			p.Normal = Vector3.UnitY;
			p.D = 200;

			// create a plane mesh
			MeshManager.Instance.CreatePlane("FloorPlane", p, 20000, 20000, 20, 20, true, 1, 50, 50, Vector3.UnitZ);

			// create an entity to reference this mesh
			Entity planeEnt = sceneMgr.CreateEntity("Floor", "FloorPlane");
			planeEnt.MaterialName = "Example/RustySteel";
			((SceneNode)sceneMgr.RootSceneNode.CreateChild()).Objects.Add(planeEnt);

			// create an entity to have follow the path
			Entity ogreHead = sceneMgr.CreateEntity("OgreHead", "ogrehead.mesh");

			// create a scene node for the entity and attach the entity
			headNode = (SceneNode)sceneMgr.RootSceneNode.CreateChild("OgreHeadNode", new Vector3(0, 50, 0), Quaternion.Identity);
			headNode.Objects.Add(ogreHead);

			// create a scene node to attach the camera to
			SceneNode camNode = (SceneNode)sceneMgr.RootSceneNode.CreateChild("CameraNode");
			camNode.Objects.Add(camera);
				
			// create new animation
			Animation anim = sceneMgr.CreateAnimation("OgreHeadAnimation", 10.0f);

			// nice smooth animation
			anim.InterpolationMode = InterpolationMode.Spline;

			// create the main animation track
			AnimationTrack track = anim.CreateTrack(0, camNode);

			// create a few keyframes to move the camera around
			KeyFrame frame = track.CreateKeyFrame(0.0f);

			frame = track.CreateKeyFrame(2.5f);
			frame.Translate = new Vector3(500, 500, -1000);

			frame = track.CreateKeyFrame(5.0f);
			frame.Translate = new Vector3(-1500, 1000, -600);

			frame = track.CreateKeyFrame(7.5f);
			frame.Translate = new Vector3(0, -100, 0);

			frame = track.CreateKeyFrame(10.0f);
			frame.Translate = Vector3.Zero;

			// create a new animation state to control the animation
			animState = sceneMgr.CreateAnimationState("OgreHeadAnimation");

			// enable the animation
			animState.IsEnabled = true; 

			// set a basic skybox
			sceneMgr.SetSkyBox(true, "Skybox/CloudyHills", 2000.0f);

			// always watch the ogre!
			camera.SetAutoTracking(true, headNode, Vector3.Zero);
		}

		protected override bool OnFrameStarted(object source, FrameEventArgs e)
		{
			base.OnFrameStarted (source, e);

			// add time to the animation which is driven off of rendering time per frame
			animState.AddTime(e.TimeSinceLastFrame);

			return true;
		}


		#endregion
		

	}
}
