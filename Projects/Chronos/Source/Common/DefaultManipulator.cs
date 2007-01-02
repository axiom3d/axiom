using System;
using Axiom.Core;
using Axiom.Math;

using Log = Chronos.Diagnostics.Log;

namespace Chronos.Core
{
	/// <summary>
	/// Summary description for DefaultManipulator.
	/// </summary>
	public class DefaultManipulator : IManipulator
	{
		private enum handleOps { None,
			MoveX, MoveY, MoveZ, MoveAll,
			RotateX, RotateY, RotateZ, RotateAll,
			ScaleX, ScaleY, ScaleZ, ScaleAll }

		private SceneNode selectedNode;
		private SceneNode multiToolNode;
		SceneNode xNodes, yNodes, zNodes;
		private bool flipX, flipY, flipZ;
		private Vector3 camXAxis = new Vector3(0,0,0);
		private Vector3 camYAxis = new Vector3(0,0,0);
		private Vector3 camZAxis = new Vector3(0,0,0);

		private handleOps activeHandleOp;
		private float camDistance;

		public DefaultManipulator() {}

		public SceneNode GetManipulatorNode(SceneNode node) {
			selectedNode = node;
			int handleSpread = 17;

			// Create the multitool
			multiToolNode = Root.Instance.SceneManager.RootSceneNode.CreateChildSceneNode("__editor_multiToolNode");
			multiToolNode.InheritScale = false;

			xNodes = multiToolNode.CreateChildSceneNode("__editor_xHandles");
			yNodes = multiToolNode.CreateChildSceneNode("__editor_yHandles");
			zNodes = multiToolNode.CreateChildSceneNode("__editor_zHandles");

			Line3D line = new Line3D(true);
			line.RenderQueueGroup = RenderQueueGroupID.Overlay;
			line.DrawLine(new Vector3(0,0,0), new Vector3(handleSpread*3, 0, 0), ColorEx.Red);
			xNodes.AttachObject(line);

			line = new Line3D(true);
			line.RenderQueueGroup = RenderQueueGroupID.Overlay;
			line.DrawLine(new Vector3(0,0,0), new Vector3(0, handleSpread*3, 0), ColorEx.Yellow);
			yNodes.AttachObject(line);

			line = new Line3D(true);
			line.RenderQueueGroup = RenderQueueGroupID.One;
			line.DrawLine(new Vector3(0,0,0), new Vector3(0, 0, handleSpread*3), ColorEx.Blue);
			zNodes.AttachObject(line);

			xNodes.AddChild(createEntity("__editor_handle_move_x", "Editor/scalecrystal.mesh", new Vector3(handleSpread*3,0,0), Vector3.UnitX, "__editorRed"));
			yNodes.AddChild(createEntity("__editor_handle_move_y", "Editor/scalecrystal.mesh", new Vector3(0,handleSpread*3,0), Vector3.UnitY, "__editorYellow"));
			zNodes.AddChild(createEntity("__editor_handle_move_z", "Editor/scalecrystal.mesh", new Vector3(0,0,handleSpread*3), Vector3.UnitZ, "__editorBlue"));

			SceneNode scaleNodes = multiToolNode.CreateChildSceneNode("__editor_scaleNodes");
			xNodes.AddChild(createEntity("__editor_handle_scale_x", "Editor/editorcube.mesh", new Vector3(handleSpread,0,0), "__editorRed"));
			yNodes.AddChild(createEntity("__editor_handle_scale_y", "Editor/editorcube.mesh", new Vector3(0,handleSpread,0), "__editorYellow"));
			zNodes.AddChild(createEntity("__editor_handle_scale_z", "Editor/editorcube.mesh", new Vector3(0,0,handleSpread), "__editorBlue"));
			multiToolNode.AddChild(createEntity("__editor_handle_scale_all", "Editor/editorcube.mesh", new Vector3(0,0,0),"__editorWhite"));

			SceneNode sn;
			sn = createEntity("__editor_handle_rotate_x", "Editor/rotator.mesh", new Vector3(handleSpread*2,0,0), Vector3.NegativeUnitY, "__editorRed");
			sn.Rotate(new Vector3(0,0,1), 90);
			xNodes.AddChild(sn);
			sn = createEntity("__editor_handle_rotate_y", "Editor/rotator.mesh", new Vector3(0,handleSpread*2,0), "__editorYellow");
			sn.Orientation = Quaternion.FromAngleAxis(0, new Vector3(0,1,0));
			yNodes.AddChild(sn);
			zNodes.AddChild(createEntity("__editor_handle_rotate_z", "Editor/rotator.mesh", new Vector3(0,0,handleSpread*2), Vector3.NegativeUnitY, "__editorBlue"));
			return multiToolNode;
		}

		#region Mouse event handlers
		public void ManipulatorHandleMouseDown(Axiom.Core.MovableObject handle, Axiom.Input.InputReader input) {
			Log.WriteLine(this.camXAxis.ToString() + " " +
						this.camYAxis.ToString() + " " +
						this.camZAxis.ToString());
			//if(input.IsMousePressed(Axiom.Input.MouseButtons.Button0)) {
				string[] bits = handle.Name.ToLower().Split('_');
			if(bits[bits.Length-2] == "move") {
				string op = bits[bits.Length-1];
				if(op == "x") {
					activeHandleOp = handleOps.MoveX;
				} else if(op == "y") {
					activeHandleOp = handleOps.MoveY;
				} else if(op == "z") {
					activeHandleOp = handleOps.MoveZ;
				} else if(op == "all") {
					activeHandleOp = handleOps.MoveAll;
				}
				switchOff(op);
			} else if (bits[bits.Length-2] == "scale") {
				string op = bits[bits.Length-1];
				if(op == "x") {
					activeHandleOp = handleOps.ScaleX;
				} else if(op == "x") {
					activeHandleOp = handleOps.ScaleY;
				} else if(op == "z") {
					activeHandleOp = handleOps.ScaleZ;
				} else if(op == "all") {
					activeHandleOp = handleOps.ScaleAll;
				}
				switchOff(op);
			} else if (bits[bits.Length-2] == "rotate") {
				string op = bits[bits.Length-1];
				if(op == "x") {
					activeHandleOp = handleOps.RotateX;
				} else if(op == "y") {
					activeHandleOp = handleOps.RotateY;
				} else if(op == "z") {
					activeHandleOp = handleOps.RotateZ;
				} else if(op == "all") {
					activeHandleOp = handleOps.RotateAll;
				}
				switchOff(op);
			}
			//}
		}

		// This is ugly, but it gets the job done.
		private void switchOff(string keepAxis) {
			if(keepAxis != "x" && keepAxis != "y" && keepAxis != "z") return;
			if(keepAxis != "x")
				xNodes.Visible = false;
			if(keepAxis != "y")
				yNodes.Visible = false;
			if(keepAxis != "z")
				zNodes.Visible = false;
		}

		public void switchOn() {
			xNodes.Visible = true;
			yNodes.Visible = true;
			zNodes.Visible = true;
		}

		public void ManipulatorHandleMoved(Axiom.Input.InputReader input) {
			if(activeHandleOp != handleOps.None) {
				doNodeTranslate(input);
				doNodeRotate(input);
				doNodeScale(input);
			}
		}

		public void ManipulatorHandleMouseUp(Axiom.Input.InputReader input) {
			activeHandleOp = handleOps.None;
			switchOn();
			for(int i=0; i<multiToolNode.ChildCount; i++) {
				multiToolNode.GetChild(i).ResetToInitialState();
			}
		}
		#endregion

		public void ManipulatorHandleKeyPressed(Axiom.Input.InputReader input) {
			// Nothing to do for keys
		}

		public void Tick(Axiom.Core.Camera camera) {
			// Synch tool scale to node-camera distance.
			camDistance = (camera.DerivedPosition - selectedNode.DerivedPosition).Length;
			float sf = camDistance / 250;
			this.multiToolNode.ScaleFactor = new Vector3(sf, sf, sf);
			multiToolNode.Position = this.selectedNode.DerivedPosition;

			camera.DerivedOrientation.ToAxes(out camXAxis, out camYAxis, out camZAxis);
			flipX = camXAxis.x < 0;
			flipY = camYAxis.y < 0;
			flipZ = camZAxis.z < 0;
		}

		#region Transformation methods
		private void doNodeTranslate(Axiom.Input.InputReader input) {
			Vector3 v = new Vector3(0,0,0);
			if(activeHandleOp == handleOps.MoveX) {
				int xyplane = (camZAxis.y < 0 ? -1 : 1);
				int yaxis = (camXAxis.z < 0 ? 1 : -1);
				v.x += input.RelativeMouseX * camXAxis.x;
				v.x += input.RelativeMouseY * (1 - camXAxis.x) * xyplane * yaxis;
			} else if(activeHandleOp == handleOps.MoveY) {
				v.y += -input.RelativeMouseY * (camYAxis.y > 0 ? 1 : -1);
			} else if(activeHandleOp == handleOps.MoveZ) {
				int xyplane = (camZAxis.y < 0 ? -1 : 1);
				int yaxis = (camZAxis.z < 0 ? -1 : 1);
				v.z += input.RelativeMouseX * camXAxis.z;
				v.z += input.RelativeMouseY * (1 - camZAxis.z) * yaxis;
			} else if(activeHandleOp == handleOps.MoveAll) {
				v.x += input.RelativeMouseX;
				v.y += -input.RelativeMouseY;
			}

			selectedNode.Translate(v * (camDistance / 900));
		}

		/// <summary>
		/// Perform node rotation based on key and mouse input.
		/// </summary>
		private void doNodeRotate(Axiom.Input.InputReader input) {

			float x = (float)input.RelativeMouseX;
			float y = (float)input.RelativeMouseY;
			if(x == 0) return;

			if(activeHandleOp == handleOps.RotateX) {
				selectedNode.Orientation = Quaternion.FromAngleAxis(Utility.DegreesToRadians(-x), new Vector3(1,0,0)) * selectedNode.Orientation;
				SceneNode n = multiToolNode.GetChild("__editor_xHandles") as SceneNode;
				n.Orientation = Quaternion.FromAngleAxis(Utility.DegreesToRadians(-x), new Vector3(1,0,0)) * n.Orientation;
			} else if(activeHandleOp == handleOps.RotateY) {
				selectedNode.Orientation = Quaternion.FromAngleAxis(Utility.DegreesToRadians(x), new Vector3(0,1,0)) * selectedNode.Orientation;
				SceneNode n = multiToolNode.GetChild("__editor_yHandles") as SceneNode;
				n.Orientation = Quaternion.FromAngleAxis(Utility.DegreesToRadians(x), new Vector3(0,1,0)) * n.Orientation;
			} else if(activeHandleOp == handleOps.RotateZ) {
				selectedNode.Orientation = Quaternion.FromAngleAxis(Utility.DegreesToRadians(-x), new Vector3(0,0,1)) * selectedNode.Orientation;
				SceneNode n = multiToolNode.GetChild("__editor_zHandles") as SceneNode;
				n.Orientation = Quaternion.FromAngleAxis(Utility.DegreesToRadians(-x), new Vector3(0,0,1)) * n.Orientation;
			}
		}

		/// <summary>
		/// Perform node scaling based on mouse input
		/// </summary>
		private void doNodeScale(Axiom.Input.InputReader input) {
			float div = (float)camDistance / 4.0f;
			float x = (float)input.RelativeMouseX / div;
			float y = (float)input.RelativeMouseY / div;
			Vector3 v = new Vector3(1,1,1);
			if(activeHandleOp == handleOps.ScaleX) {
				v.x += x;
			} else if(activeHandleOp == handleOps.ScaleY) {
				v.y += -y;
			} else if(activeHandleOp == handleOps.ScaleZ) {
				v.z += -x;
			} else if(activeHandleOp == handleOps.ScaleAll) {
				v.x += x;
				v.y += x;
				v.z += x;
			}

			selectedNode.Scale(v);
		}

		#endregion

		#region Private helper methods
		private static SceneNode createEntity(string nodeName, string meshName,  Vector3 translate) {
			return createEntity(nodeName, meshName, translate, Vector3.NegativeUnitZ, "default", false);
		}

		private static SceneNode createEntity(string nodeName, string meshName, Vector3 translate, string materialName) {
			return createEntity(nodeName, meshName, translate, Vector3.NegativeUnitZ, materialName, false);
		}

		private static SceneNode createEntity(string nodeName, string meshName, Vector3 translate, Vector3 direction, string materialName) {
			return createEntity(nodeName, meshName, translate, direction, materialName, false);
		}

		private static SceneNode createEntity(string nodeName, string meshName, Vector3 translate, Vector3 direction, string materialName, bool castShadows) {
			SceneNode node = Root.Instance.SceneManager.CreateSceneNode(nodeName);
			Entity e = Root.Instance.SceneManager.CreateEntity(nodeName, meshName);
			e.RenderQueueGroup = RenderQueueGroupID.Overlay;
			e.MaterialName = materialName;
			node.AttachObject(e);
			node.SetDirection(direction, TransformSpace.Local, Vector3.NegativeUnitZ);
			node.Translate(translate);
			return node;
		}
		#endregion
	}
}
