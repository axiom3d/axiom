using System;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.MathLib;
using Axiom.Utility;

namespace Demos {
	/// <summary>
	/// Summary description for RenderToTexture.
	/// </summary>
	public class RenderToTexture : TechDemo {
        
        Camera rttCam;
        SceneNode headNode;
        Entity planeEnt;
        SceneNode planeNode;

        protected override void OnFrameStarted(object source, FrameEventArgs e) {
            base.OnFrameStarted (source, e);

            rttCam.Position = camera.DerivedPosition;
            rttCam.Direction = camera.DerivedDirection;

            Quaternion q = rttCam.DerivedOrientation;
            Vector3 vPos = rttCam.DerivedPosition;
            Vector3 vDir = rttCam.DerivedDirection;

            headNode.Orientation = q;
            vDir.Normalize();

            headNode.Position = vPos + vDir * -250.0f;
        }

        protected override void CreateScene() {

            // create a default point light
            Light light = scene.CreateLight("MainLight");
            light.Position = new Vector3(20, 80, 50);

            planeEnt = scene.CreateEntity("Plane", PrefabEntity.Plane);
            Entity knot = scene.CreateEntity("Knot", "knot.mesh");
            knot.MaterialName = "TextureFX/Knot";

            Entity head = scene.CreateEntity("Head", "ogrehead.mesh");

            SceneNode rootNode = scene.RootSceneNode;

            headNode = rootNode.CreateChildSceneNode("Head");
            headNode.AttachObject(head);

            planeNode = rootNode.CreateChildSceneNode();
            planeNode.AttachObject(planeEnt);

            rttCam = scene.CreateCamera("RttCam");
            rootNode.AttachObject(rttCam);
            rttCam.Position = new Vector3(0, 0, 200.0f);
            rttCam.Direction = new Vector3(0.0f, 0.0f, 100.0f);

            // create a render texture
            RenderTexture rttTex = Root.Instance.RenderSystem.CreateRenderTexture("RttTex", 512, 512);
            Viewport viewport = rttTex.AddViewport(rttCam, 0, 0, 1.0f, 1.0f, 0);
            viewport.ClearEveryFrame = true;
            viewport.OverlaysEnabled = false;
            viewport.BackgroundColor = ColorEx.White;
         
            Material mat = scene.CreateMaterial("RttMat");
            mat.GetTechnique(0).GetPass(0).CreateTextureUnitState("RttTex");

            planeEnt.MaterialName = "RttMat";

            Entity clone = null;

            for(int i = 0; i < 10; i++) {
                // create a new node under the root
                SceneNode node = scene.CreateSceneNode();

                // calculate a random position
                Vector3 nodePosition = new Vector3();
                nodePosition.x = MathUtil.SymmetricRandom() * 500.0f;
                nodePosition.y = MathUtil.SymmetricRandom() * 500.0f;
                nodePosition.z = MathUtil.SymmetricRandom() * 500.0f;

                // set the new position
                node.Position = nodePosition;

                // attach this node to the root node
                rootNode.AddChild(node);

                // clone the knot
                string cloneName = string.Format("Knot{0}", i);
                clone = knot.Clone(cloneName);

                // add the cloned knot to the scene
                node.AttachObject(clone);
            }
        }
    }
}
