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
            sceneMgr.AmbientLight = ColorEx.FromColor(System.Drawing.Color.White);

            // create the robot entity
            Entity entity = sceneMgr.CreateEntity("Robot", "robot.mesh");
            animState = entity.GetAnimationState("Idle");
            animState.Weight = 0.7f;
            animState2 = entity.GetAnimationState("Walk");
            animState2.Weight = 0.7f;
            animState.IsEnabled = true;
            animState2.IsEnabled = true;

            ((SceneNode)sceneMgr.RootSceneNode.CreateChild()).Objects.Add(entity);

            // setup the camera for a nice view of the robot
            camera.Position = new Vector3(100, 50, 100);
            camera.LookAt(new Vector3(-50, 50, 0));
        }

        protected override bool OnFrameStarted(object source, FrameEventArgs e) {
            // add time to the robot animation
            animState.AddTime(e.TimeSinceLastFrame);
            animState2.AddTime(e.TimeSinceLastFrame);

            return base.OnFrameStarted (source, e);
        }

        #endregion
    }
}
