using System;
using System.Drawing;
using Axiom.Animating;
using Axiom.Controllers;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos
{
	/// <summary>
	/// 	Summary description for Controllers.
	/// </summary>
	public class Lights : TechDemo
	{
		#region Member variables
		
		private Controller skullRotator;
		private SceneNode ogreNode;
		private SceneNode billboardNode;
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
		private IControllerFunction redLightFunc;
		private IControllerFunction yellowLightFunc;
		private IControllerFunction greenLightFunc;
		private IControllerFunction blueLightFunc;
		private IControllerValue redLightFlasher;
		private IControllerValue yellowLightFlasher;
		private IControllerValue greenLightFlasher;
		private IControllerValue blueLightFlasher;

		#endregion
	
		#region Methods
		
		protected override void CreateScene()
		{
			// set some ambient light
			sceneMgr.TargetRenderSystem.LightingEnabled = true;
			sceneMgr.AmbientLight = ColorEx.FromColor(System.Drawing.Color.Gray);

			// create the ogre head
			Entity ogre = sceneMgr.CreateEntity("OgreHead", "ogrehead.mesh");

			// attach the ogre to the scene
			sceneMgr.RootSceneNode.Objects.Add(ogre);

			// create nodes for the billboard sets
			redYellowLightsNode = (SceneNode)sceneMgr.RootSceneNode.CreateChild();
			greenBlueLightsNode = (SceneNode)sceneMgr.RootSceneNode.CreateChild();

			// create a billboard set for creating billboards
			redYellowLights = new BillboardSet("RedYellowLights", 5);
			redYellowLights.MaterialName = "Particles/Flare";
			redYellowLightsNode.Objects.Add(redYellowLights);

			greenBlueLights = new BillboardSet("GreenBlueLights", 5);
			greenBlueLights.MaterialName = "Particles/Flare";
			greenBlueLightsNode.Objects.Add(greenBlueLights);

			// red light billboard in off set
			Vector3 redLightPos = new Vector3(78, -8, -70);
			redLightBoard = redYellowLights.CreateBillboard(redLightPos, ColorEx.FromColor(Color.Black));

			// yellow light billboard in off set
			Vector3 yellowLightPos = new Vector3(-4.5f, 30, -80);
			yellowLightBoard = redYellowLights.CreateBillboard(yellowLightPos, ColorEx.FromColor(Color.Black));

			// blue light billboard in off set
			Vector3 blueLightPos = new Vector3(90, -8, -70);
			blueLightBoard = greenBlueLights.CreateBillboard(blueLightPos, ColorEx.FromColor(Color.Black));

			// green light billboard in off set
			Vector3 greenLightPos = new Vector3(50, 70, 80);
			greenLightBoard = greenBlueLights.CreateBillboard(redLightPos, ColorEx.FromColor(Color.Black));

			// red light in off state
			redLight = sceneMgr.CreateLight("RedLight");
			redLight.Position = redLightPos;
			redLight.Diffuse = ColorEx.FromColor(Color.Black);
			redYellowLightsNode.Objects.Add(redLight);

			// yellow light in off state
			yellowLight = sceneMgr.CreateLight("YellowLight");
			yellowLight.Position = yellowLightPos;
			yellowLight.Diffuse = ColorEx.FromColor(Color.Black);
			redYellowLightsNode.Objects.Add(yellowLight);

			// green light in off state
			greenLight = sceneMgr.CreateLight("GreenLight");
			greenLight.Position = greenLightPos;
			greenLight.Diffuse = ColorEx.FromColor(Color.Black);
			greenBlueLightsNode.Objects.Add(greenLight);

			// blue light in off state
			blueLight = sceneMgr.CreateLight("BlueLight");
			blueLight.Position = blueLightPos;
			blueLight.Diffuse = ColorEx.FromColor(Color.Black);
			greenBlueLightsNode.Objects.Add(blueLight);

			// create controller function
			IControllerValue redLightFlasher = 
				new LightFlasherControllerValue(redLight, redLightBoard, ColorEx.FromColor(System.Drawing.Color.Red));
			IControllerValue yellowLightFlasher = 
				new LightFlasherControllerValue(yellowLight, yellowLightBoard, ColorEx.FromColor(System.Drawing.Color.Yellow));
			IControllerValue greenLightFlasher = 
				new LightFlasherControllerValue(greenLight, greenLightBoard, ColorEx.FromColor(System.Drawing.Color.Green));
			IControllerValue blueLightFlasher = 
				new LightFlasherControllerValue(blueLight, blueLightBoard, ColorEx.FromColor(System.Drawing.Color.Blue));

			// set up the controller value and function for flashing
			redLightFunc = new WaveformControllerFunction(WaveformType.Sine, 0, .75f, 0, 1);
			yellowLightFunc = new WaveformControllerFunction(WaveformType.Sawtooth, 0, .3f, 0, 1);
			greenLightFunc = new WaveformControllerFunction(WaveformType.InverseSawtooth, 0, .55f, 0, 1);
			blueLightFunc = new WaveformControllerFunction(WaveformType.Sine, 0, .5f, 0, 1);

			// set up the controllers
			ControllerManager.Instance.CreateController(redLightFlasher, redLightFunc);
			ControllerManager.Instance.CreateController(yellowLightFlasher, yellowLightFunc);
			ControllerManager.Instance.CreateController(greenLightFlasher, greenLightFunc);
			ControllerManager.Instance.CreateController(blueLightFlasher, blueLightFunc);

			// set a basic skybox
			sceneMgr.SetSkyBox(true, "Skybox/Space", 2000.0f);
		}

		protected override bool OnFrameStarted(object source, FrameEventArgs e)
		{
			base.OnFrameStarted (source, e);

			// move the billboards around a bit
			redYellowLightsNode.Yaw(10 * e.TimeSinceLastFrame);
			greenBlueLightsNode.Pitch(20 * e.TimeSinceLastFrame);

			return true;
		}

		#endregion
		
	}

	public class LightFlasherControllerValue : IControllerValue
	{
		private Billboard billboard;
		private Light light;
		private float intensity;
		private ColorEx maxColor;

		public LightFlasherControllerValue(Light light, Billboard billboard, ColorEx maxColor)
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

		#endregion

	}

}
