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

        #endregion
			
        #region Methods

        protected override void CreateScene() {

            // set some ambient light
            scene.TargetRenderSystem.LightingEnabled = true;
            scene.AmbientLight = ColorEx.Gray;

            // create the robot entity
            Entity entity = scene.CreateEntity("Robot", "robot.mesh");
            animState = entity.GetAnimationState("Walk");
            animState.IsEnabled = true;

            ((SceneNode)scene.RootSceneNode.CreateChild()).AttachObject(entity);

            Light light = scene.CreateLight("BlueLight");
            light.Position = new Vector3(-200, -80, -100);
            light.Diffuse = new ColorEx(1.0f, .5f, .5f, 1.0f);

            light = scene.CreateLight("GreenLight");
            light.Position = new Vector3(0, 0, -100);
            light.Diffuse = new ColorEx(1.0f, 0.5f, 1.0f, 0.5f);

            // setup the camera for a nice view of the robot
            camera.Position = new Vector3(100, 50, 100);
            camera.LookAt(new Vector3(0, 50, 0));
        }

        protected override void OnFrameStarted(object source, FrameEventArgs e) {
            // add time to the robot animation
            animState.AddTime(e.TimeSinceLastFrame);

            base.OnFrameStarted(source, e);
        }

        #endregion
    }
}
