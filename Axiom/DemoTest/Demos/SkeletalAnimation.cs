using System;
using Axiom.Animating;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos {
    /// <summary>
    /// 	Summary description for SkeletalAnimation.
    /// </summary>
    public class SkeletalAnimation : TechDemo {
        #region Fields
		
		const int NUM_ROBOTS = 10;
		AnimationState[] animState = new AnimationState[NUM_ROBOTS];		
		float[] animationSpeed = new float[NUM_ROBOTS];

        #endregion Fields
			
        #region Methods

        protected override void CreateScene() {
            // set some ambient light
            scene.TargetRenderSystem.LightingEnabled = true;
            scene.AmbientLight = ColorEx.Gray;

			Entity entity = null;

            // create the robot entity
			for(int i = 0; i < NUM_ROBOTS; i++) { 
				string robotName = string.Format("Robot{0}", i);
				entity = scene.CreateEntity(robotName, "robot.mesh");
				scene.RootSceneNode.CreateChildSceneNode(
					new Vector3(0, 0, (i * 50) - (NUM_ROBOTS * 50 / 2))).AttachObject(entity);
				animState[i] = entity.GetAnimationState("Walk");
				animState[i].IsEnabled = true;
				animationSpeed[i] = MathUtil.RangeRandom(0.5f, 1.5f);
			}

            Light light = scene.CreateLight("BlueLight");
            light.Position = new Vector3(-200, -80, -100);
            light.Diffuse = new ColorEx(1.0f, .5f, .5f, 1.0f);

            light = scene.CreateLight("GreenLight");
            light.Position = new Vector3(0, 0, -100);
            light.Diffuse = new ColorEx(1.0f, 0.5f, 1.0f, 0.5f);

            // setup the camera for a nice view of the robot
            camera.Position = new Vector3(100, 50, 100);
            camera.LookAt(new Vector3(0, 50, 0));

			Technique t = entity.GetSubEntity(0).Material.GetBestTechnique();
			Pass p = t.GetPass(0);
			
			if(p.HasVertexProgram && p.VertexProgram.IsSkeletalAnimationIncluded) {
				window.DebugText = "Hardware skinning is enabled.";
			}
			else {
				window.DebugText = "Software skinning is enabled.";
			}
        }

        protected override void OnFrameStarted(object source, FrameEventArgs e) {
			for(int i = 0; i < NUM_ROBOTS; i++) {
				// add time to the robot animation
				animState[i].AddTime(e.TimeSinceLastFrame * animationSpeed[i]);
			}

            base.OnFrameStarted(source, e);
        }

        #endregion
    }
}
