using System;
using System.Collections;
using Axiom.Animating;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos {
	/// <summary>
	/// Summary description for CelShading.
	/// </summary>
	public class CelShading : TechDemo {
        #region Fields

        SceneNode rotNode;

        #endregion Fields

        protected override void CreateScene() {
            if( !Root.Instance.RenderSystem.Caps.CheckCap(Capabilities.VertexPrograms) ||
                !Root.Instance.RenderSystem.Caps.CheckCap(Capabilities.FragmentPrograms)) {

                throw new Exception("Your hardware does not support vertex and fragment programs, so you cannot run this demo.");
            }

            // create a simple default point light
            Light light = scene.CreateLight("MainLight");
            light.Position = new Vector3(20, 80, 50);

            rotNode = scene.RootSceneNode.CreateChildSceneNode();
            rotNode.CreateChildSceneNode(new Vector3(20, 40, 50), Quaternion.Identity).AttachObject(light);

            Entity entity = scene.CreateEntity("Head", "ogrehead.mesh");

            camera.Position = new Vector3(20, 0, 100);
            camera.LookAt(Vector3.Zero);

            entity.GetSubEntity(0).MaterialName = "Examples/OgreCelShading/Eyes";
            entity.GetSubEntity(1).MaterialName = "Examples/OgreCelShading/Skin";
            entity.GetSubEntity(2).MaterialName = "Examples/OgreCelShading/Earring";
            entity.GetSubEntity(3).MaterialName = "Examples/OgreCelShading/Teeth";

            scene.RootSceneNode.CreateChildSceneNode().AttachObject(entity);

            window.GetViewport(0).BackgroundColor = ColorEx.White;
        }

        protected override void OnFrameStarted(object source, FrameEventArgs e) {
            rotNode.Yaw(e.TimeSinceLastFrame * 30);

            base.OnFrameStarted (source, e);
        }
	}
}
