using System;
using Axiom.Animating;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos {
    /// <summary>
    /// 	Summary description for SkeletalAnimation.
    /// </summary>
    public class SkeletalAnimation : TechDemo {
        #region Member variables
		
        private AnimationState animState;
        private AnimationState animState2;

        #endregion
			
        #region Methods

        protected override void CreateScene() {

            // set some ambient light
            sceneMgr.TargetRenderSystem.LightingEnabled = true;
            sceneMgr.AmbientLight = ColorEx.FromColor(System.Drawing.Color.Gray);

            // create the robot entity
            Entity entity = sceneMgr.CreateEntity("Robot", "robot.mesh");
            animState = entity.GetAnimationState("Walk");
            animState.IsEnabled = true;

            ((SceneNode)sceneMgr.RootSceneNode.CreateChild()).Objects.Add(entity);

            Light light = sceneMgr.CreateLight("BlueLight");
            light.Position = new Vector3(-200, -80, -100);
            light.Diffuse = new ColorEx(1.0f, .5f, .5f, 1.0f);

            light = sceneMgr.CreateLight("GreenLight");
            light.Position = new Vector3(0, 0, -100);
            light.Diffuse = new ColorEx(1.0f, 0.5f, 1.0f, 0.5f);

            // setup the camera for a nice view of the robot
            camera.Position = new Vector3(100, 50, 100);
            camera.LookAt(new Vector3(0, 50, 0));
        }

        protected override bool OnFrameStarted(object source, FrameEventArgs e) {
            // add time to the robot animation
            animState.AddTime(e.TimeSinceLastFrame);

            return base.OnFrameStarted (source, e);
        }

        #endregion
    }
}
