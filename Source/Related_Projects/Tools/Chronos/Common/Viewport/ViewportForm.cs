#region LGPL License
/*
Chronos World Editor
Copyright (C) 2004 Chris "Antiarc" Heald [antiarc@antiarc.net]

This application is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This application is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion


using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

using Axiom.Core;
using Axiom.Input;
using Axiom.Math;
using Axiom.Math.Collections;
using Axiom.Graphics;

using Chronos.Core;

using MouseButtons = Axiom.Input.MouseButtons;

namespace Chronos.Core {
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class RenderingWindow : UserControl {
		private static int __windowCount = 0;
		private TD.SandBar.ToolBar toolBar1;
		private TD.SandBar.ButtonItem buttonItem1;
		private TD.SandBar.ButtonItem buttonItem2;
		private TD.SandBar.ButtonItem buttonItem3;
		private TD.SandBar.DropDownMenuItem dropDownMenuItem1;
		private TD.SandBar.MenuButtonItem menuButtonItem1;
		private TD.SandBar.MenuButtonItem menuButtonItem2;
		private TD.SandBar.MenuButtonItem menuButtonItem3;
		private System.Windows.Forms.ImageList imageList1;
		private System.ComponentModel.IContainer components;
		private static SceneNode lastCamNode;

		private SceneNode manipNode;
		private ArrayList manipulatorHandleList = new ArrayList();

		private IManipulator manipulator;

		public delegate void CmdKeyDelegate(Keys key);
		public event CmdKeyDelegate CmdKeyPressed;

		#region Private fields
		private System.Windows.Forms.StatusBar statusBar1;
		private System.Windows.Forms.StatusBarPanel statusBarPanel1;

		private double timeSinceLastKey;
		private Axiom.Graphics.RenderWindow renderWindow;
		
		private SceneNode axesNode, axesRootNode, cameraNode, cameraTrackerNode;
		private string defaultStatusText = "LMB = pan; RMB = zoom; MMB = look";
		private string defaultSelStatusText = "RMB = zoom; MMB = tumble";

		private float pickObjectDepth;
		private float cameraScale = 100;
		private float toggleDelay = 1.0f;

		private bool invalidateInput = true;

		private float camPitch = 0.0f;
		private float camYaw = 0.0f;

		private MovableObject pickedObject;
		private EditorNode selectedNode;
		
		private SceneNode camNodes;
		
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.MenuItem menuItem4;
		private int windowID;

		private System.Windows.Forms.StatusBarPanel entityX;
		private System.Windows.Forms.StatusBarPanel entityY;
		private System.Windows.Forms.StatusBarPanel entityZ;
		private System.Windows.Forms.StatusBarPanel fpsPanel;
		private System.Windows.Forms.StatusBarPanel statusBarPanel2;
		private System.Windows.Forms.Panel panel1;
		public System.Windows.Forms.PictureBox renderBox;

		private Vector3 cameraVector = new Vector3(0,0,0);
		private InputReader input;
		private Camera camera;
		private Viewport viewport;

		#endregion

		#region Structs and Enums
		public enum Ops {
			None, Translate, Rotate, Scale
		};
		#endregion

		#region Constructors & Destructors

		public RenderingWindow() {
			InitializeComponent();

			// set the global error handler for this applications thread of execution.
			Application.ThreadException += new ThreadExceptionEventHandler(GlobalErrorHandler);

			SceneGraph.Instance.SelectedObjectChanged += new Chronos.Core.SceneGraph.SelectedObjectChangedDelegate(Instance_SelectedObjectChanged);

			this.statusBar1.Panels[0].Text = defaultStatusText;
			this.SetStyle(ControlStyles.DoubleBuffer, true);
			windowID = ++__windowCount;
			Setup();
			renderWindow.BeforeViewportUpdate +=new ViewportUpdateEventHandler(renderWindow_BeforeViewportUpdate);
			renderWindow.AfterViewportUpdate += new ViewportUpdateEventHandler(renderWindow_AfterViewportUpdate);

		}

		#endregion Constructors & Destructors

		#region Root Setup

		private void CreateScene() {
			Root.Instance.SceneManager.AmbientLight = ColorEx.White;

			camNodes = Root.Instance.SceneManager.CreateSceneNode();
			if(lastCamNode == null)
				lastCamNode = camNodes;
			cameraTrackerNode = camNodes.CreateChildSceneNode();

			camera.LookAt(new Vector3(0,0,-1));
			cameraNode = cameraTrackerNode.CreateChildSceneNode();
			cameraNode.AttachObject(camera);
			cameraNode.Translate(new Vector3(0,0,500));
			
			axesRootNode = cameraNode.CreateChildSceneNode(new Vector3(-70, -50, -150), camera.Orientation);
			axesNode = axesRootNode.CreateChildSceneNode();

			Entity ent = Root.Instance.SceneManager.CreateEntity("__editorAxes_" + windowID, "Editor/axes.mesh");
			ent.MaterialName = "Editor/editor/Axes";
			ent.CastShadows = false;
			ent.RenderQueueGroup = RenderQueueGroupID.Overlay;
			axesNode.AttachObject(ent);

			Entity ent2 = Root.Instance.SceneManager.CreateEntity("__editorAxes2_" + windowID, "Editor/axes.mesh");
			ent2.MaterialName = "Editor/editor/Axes";
			ent2.CastShadows = false;
			ent.RenderQueueGroup = RenderQueueGroupID.Overlay;
			cameraTrackerNode.AttachObject(ent2);

			cameraTrackerNode.SetFixedYawAxis(true);
		}

		#endregion Root Setup

		#region Public static methods
		public static void GlobalErrorHandler(Object source, ThreadExceptionEventArgs e) {
			// show the error

			MessageBox.Show("An exception has occured.  Please check the log file for more information.\n\nError:\n" + e.Exception.ToString(), "Exception!");
			RootManager.Instance.RequestStart();

			// log the error
			System.Diagnostics.Trace.WriteLine(e.Exception.ToString());
		}
		#endregion

		#region Public Methods

		public static Vector3 DropPosition {
			get { return lastCamNode.DerivedPosition; }
		}

		private void Setup() {
			// add event handlers for frame events
			this.renderWindow = RootManager.Instance.CreateRenderWindow(renderBox);
			input = RootManager.Instance.InputReader;

			camera = Root.Instance.SceneManager.CreateCamera("MainCamera" + windowID);
			camera.Position = new Vector3(0, 0, 0);
			camera.LookAt(new Vector3(0, 0, -1));
			camera.Near = 5;	// set the near clipping plane to be very close

			viewport = renderWindow.AddViewport(camera, 0, 0, 100, 100, 100);
			viewport.BackgroundColor = ColorEx.Black;

			CreateScene();

			Root.Instance.FrameStarted += new FrameEvent(OnFrameStarted);
			Root.Instance.FrameStarted +=new FrameEvent(UpdateRootStats);
		}

		#endregion Public Methods
	
		#region Protected methods
		protected override void Dispose(bool disposing) {
			base.Dispose (disposing);
			Root.Instance.FrameStarted -= new FrameEvent(OnFrameStarted);
		}
		#endregion

		#region Private Methods

		private void Pick3D(float clickX, float clickY) {
			Ray ray = camera.GetCameraToViewportRay(clickX/renderBox.Width, clickY/renderBox.Height);
			RaySceneQuery pickRayQuery = Root.Instance.SceneManager.CreateRayQuery(ray);
			pickRayQuery.SortByDistance = true;
			pickRayQuery.MaxResults = 255;		// We need to be sure we can get the handles

			ArrayList objects = pickRayQuery.Execute();

			pickObjectDepth = -1.0f;
			bool gotHandle = false;
			bool gotObject = false;
			foreach(RaySceneQueryResultEntry e in objects) {
				MovableObject o = e.SceneObject;
				// This is a entity belonging to the manipulator
				if(this.manipulatorHandleList.Contains(o.Name)) {
					manipulator.ManipulatorHandleMouseDown(o, input);
					gotHandle = true;
					break;
				} else if(!o.Name.StartsWith("__")) {		
					// Anything starting with double underscores is assumed to be non-pickable.
					if(e.Distance < pickObjectDepth || pickObjectDepth == -1.0f) {
						pickedObject = o;
						pickObjectDepth = e.Distance;
						gotObject = true;
					}
				}
			}
			if (!gotObject) {
				pickedObject = null;
				if(!gotHandle)
					ViewportPlugin.Instance.FireObjectPicked(this, pickedObject);
			} else if (!gotHandle) {
				ViewportPlugin.Instance.FireObjectPicked(this, pickedObject);
			}
		}

		#endregion

		#region Event Handlers

		private void panel1_Resize(object sender, System.EventArgs e) {
			int w = (sender as Panel).Width;
			int h = (sender as Panel).Height;
			if(w > h * 1.25) {
				renderBox.Height = h;
				renderBox.Width = (int)((double)h * 1.25);
				renderBox.Top = 0;
				renderBox.Left = (w-renderBox.Width)/2;
				renderWindow.Resize((int)((double)h * 1.25), h);
				(viewport as Viewport).SetDimensions(0, 0, 100, 100);
			} 
			else {
				renderBox.Width = w;
				renderBox.Height = (int)((double)w * 0.75);
				renderBox.Top = (h-renderBox.Height)/2;
				renderBox.Left = 0;
				renderWindow.Resize(w, (int)((double)w * 0.75));
				(viewport as Viewport).SetDimensions(0, 0, 100, 100);
			}
		}

		private void renderBox_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
			this.Focus();
			renderBox.Focus();
			if((e.Button == System.Windows.Forms.MouseButtons.Left))
				Pick3D(e.X, e.Y);		
		}

		private void UpdateRootStats(object source, FrameEventArgs e) {
			//return;
			statusBar1.Panels[1].Text = "FPS: " + Root.Instance.CurrentFPS.ToString();
			if(selectedNode != null) {
				statusBar1.Panels[2].Text = String.Format("X: {0:0.00}", selectedNode.DerivedPosition.x);
				statusBar1.Panels[3].Text = String.Format("Y: {0:0.00}", selectedNode.DerivedPosition.y);
				statusBar1.Panels[4].Text = String.Format("Z: {0:0.00}", selectedNode.DerivedPosition.z);
			} 
			else {
				statusBar1.Panels[2].Text = String.Format("X: ---");
				statusBar1.Panels[3].Text = String.Format("Y: ---");
				statusBar1.Panels[4].Text = String.Format("Z: ---");
			}
		}

		protected virtual void OnFrameStarted(Object source, FrameEventArgs e) {
			// reset the camera
			cameraVector.x = 0;
			cameraVector.y = 0;
			cameraVector.z = 0;

			// set the scaling of camera motion
			this.timeSinceLastKey += e.TimeSinceLastFrame;

			// TODO: Move this into an event queueing mechanism that is processed every frame
			if(!this.renderBox.Focused || !this.renderWindow.IsActive) {
				invalidateInput = true;
				return;
			}

			// If we are coming back from having lost focus, we want to invalidate
			// the first input capture, since otherwise the app thinks we had a huge
			// mouse movement, and causes all kinds of fun bugs.

			if(invalidateInput) {
				input.Capture();		// Capture input and throw it away.
				invalidateInput = false;
			}
			input.Capture();
			lastCamNode = camNodes;


			if(input.IsKeyPressed(KeyCodes.A)) cameraVector.x = -cameraScale;
			if(input.IsKeyPressed(KeyCodes.D)) cameraVector.x = cameraScale;
			if(input.IsKeyPressed(KeyCodes.W)) cameraVector.z = -cameraScale;
			if(input.IsKeyPressed(KeyCodes.S)) cameraVector.z = cameraScale;
			if(input.IsKeyPressed(KeyCodes.Left)) cameraVector.x = -cameraScale;
			if(input.IsKeyPressed(KeyCodes.Right)) cameraVector.x = cameraScale;
			if(input.IsKeyPressed(KeyCodes.Up)) cameraVector.z = -cameraScale;
			if(input.IsKeyPressed(KeyCodes.Down)) cameraVector.z = cameraScale;
			cameraScale = cameraNode.Position.Length * e.TimeSinceLastFrame * 0.5f;
			camNodes.Translate(cameraNode.DerivedOrientation * cameraVector);

			// subtract the time since last frame to delay specific key presses
			toggleDelay -= e.TimeSinceLastFrame;

			if(manipulator != null && 
				(input.RelativeMouseX != 0 || input.RelativeMouseY != 0 || input.RelativeMouseZ != 0)) {
				manipulator.ManipulatorHandleMoved(input);
			}
			if(input.IsMousePressed(Axiom.Input.MouseButtons.Left) && manipulator != null) {
				manipulator.ManipulatorHandleMoved(input);
			} 
			else if(input.IsMousePressed(Axiom.Input.MouseButtons.Right)) {
				if(input.IsKeyPressed(KeyCodes.LeftControl) || input.IsKeyPressed(KeyCodes.RightControl)) {
					if(selectedNode == null) {
						float cameraYaw = -input.RelativeMouseX * 0.3f;
						float cameraPitch = -input.RelativeMouseY * 0.3f;
						cameraNode.Yaw(cameraYaw);
						cameraNode.Pitch(cameraPitch);
					} 
				} 
				else {
					if(selectedNode != null) {
						Vector3 distance = cameraNode.Position;
						float d = input.RelativeMouseY * distance.Length / 125;
						cameraNode.Translate(new Vector3(0,0, -d));
					} 
					else {
						camNodes.Translate(new Vector3(0,0,0 -input.RelativeMouseY * 2));
					}
				}
			}
			else if(input.IsMousePressed(Axiom.Input.MouseButtons.Middle)) {
				camPitch += -input.RelativeMouseY;
				camYaw += -input.RelativeMouseX;
				if(camPitch > 360) camPitch = camPitch % 360;
				if(camYaw > 360) camYaw = camYaw % 360;
				cameraTrackerNode.Orientation = Quaternion.FromAngleAxis(0, Vector3.NegativeUnitZ);
				cameraTrackerNode.Yaw(camYaw);
				cameraTrackerNode.Pitch(camPitch);
			} 
			axesRootNode.Orientation = cameraNode.DerivedOrientation;

			if(manipulator != null)
				manipulator.Tick(camera);

			/*float toolDistance = (cameraNode.DerivedPosition - multiToolNode.DerivedPosition).Length / (float)handleScale;
			if(selectedObject != null && selectedObject.ParentNode!= null) {
				multiToolNode.Position = selectedObject.ParentNode.DerivedPosition;
				multiToolNode.ScaleFactor = new Vector3(toolDistance, toolDistance, toolDistance);
			} else if (multiToolNode.Parent != null) {
				multiToolNode.Parent.RemoveChild(multiToolNode);
			}*/
		}

		// Ripped off from the Camera.Direction property :)
		private Quaternion getQuatTo(Vector3 destination, SceneNode srcNode, bool fixedYaw) {
			Vector3 direction = destination - srcNode.DerivedPosition;
			Vector3 zAdjustVector = -direction;
			zAdjustVector.Normalize();

			Quaternion r = new Quaternion(1,0,0,0);
			if( fixedYaw ) {
				Vector3 xVector = new Vector3(0,1,0).Cross( zAdjustVector );
				xVector.Normalize();

				Vector3 yVector = zAdjustVector.Cross( xVector );
				yVector.Normalize();

				r.FromAxes( xVector, yVector, zAdjustVector );
			}
			else {

				// Get axes from current quaternion
				Vector3 xAxis, yAxis, zAxis;

				// get the vector components of the derived orientation vector
				srcNode.DerivedOrientation.ToAxes(out xAxis, out yAxis, out zAxis);

				Quaternion rotationQuat;

				if (-zAdjustVector == zAxis) {
					// Oops, a 180 degree turn (infinite possible rotation axes)
					// Default to yaw i.e. use current UP
					rotationQuat = Quaternion.FromAngleAxis(Utility.PI, yAxis);
				}
				else {
					// Derive shortest arc to new direction
					rotationQuat = zAxis.GetRotationTo(zAdjustVector);
				}
				r = rotationQuat * srcNode.Orientation;
			}
			return r;
		}

		#endregion Event Handlers

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(RenderingWindow));
			this.statusBar1 = new System.Windows.Forms.StatusBar();
			this.statusBarPanel1 = new System.Windows.Forms.StatusBarPanel();
			this.fpsPanel = new System.Windows.Forms.StatusBarPanel();
			this.entityX = new System.Windows.Forms.StatusBarPanel();
			this.entityY = new System.Windows.Forms.StatusBarPanel();
			this.entityZ = new System.Windows.Forms.StatusBarPanel();
			this.statusBarPanel2 = new System.Windows.Forms.StatusBarPanel();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			this.menuItem4 = new System.Windows.Forms.MenuItem();
			this.panel1 = new System.Windows.Forms.Panel();
			this.renderBox = new System.Windows.Forms.PictureBox();
			this.toolBar1 = new TD.SandBar.ToolBar();
			this.buttonItem1 = new TD.SandBar.ButtonItem();
			this.buttonItem2 = new TD.SandBar.ButtonItem();
			this.buttonItem3 = new TD.SandBar.ButtonItem();
			this.dropDownMenuItem1 = new TD.SandBar.DropDownMenuItem();
			this.menuButtonItem1 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem2 = new TD.SandBar.MenuButtonItem();
			this.menuButtonItem3 = new TD.SandBar.MenuButtonItem();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanel1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.fpsPanel)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.entityX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.entityY)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.entityZ)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanel2)).BeginInit();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// statusBar1
			// 
			this.statusBar1.Location = new System.Drawing.Point(0, 518);
			this.statusBar1.Name = "statusBar1";
			this.statusBar1.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
																						  this.statusBarPanel1,
																						  this.fpsPanel,
																						  this.entityX,
																						  this.entityY,
																						  this.entityZ,
																						  this.statusBarPanel2});
			this.statusBar1.ShowPanels = true;
			this.statusBar1.Size = new System.Drawing.Size(638, 16);
			this.statusBar1.SizingGrip = false;
			this.statusBar1.TabIndex = 0;
			// 
			// statusBarPanel1
			// 
			this.statusBarPanel1.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
			this.statusBarPanel1.Text = "statusBarPanel1";
			this.statusBarPanel1.Width = 243;
			// 
			// fpsPanel
			// 
			this.fpsPanel.Alignment = System.Windows.Forms.HorizontalAlignment.Right;
			this.fpsPanel.Text = "0.0";
			this.fpsPanel.Width = 70;
			// 
			// entityX
			// 
			this.entityX.Alignment = System.Windows.Forms.HorizontalAlignment.Right;
			this.entityX.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.entityX.MinWidth = 75;
			this.entityX.Text = "0.00";
			this.entityX.Width = 75;
			// 
			// entityY
			// 
			this.entityY.Alignment = System.Windows.Forms.HorizontalAlignment.Right;
			this.entityY.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.entityY.MinWidth = 75;
			this.entityY.Text = "0.00";
			this.entityY.Width = 75;
			// 
			// entityZ
			// 
			this.entityZ.Alignment = System.Windows.Forms.HorizontalAlignment.Right;
			this.entityZ.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.entityZ.MinWidth = 75;
			this.entityZ.Text = "0.00";
			this.entityZ.Width = 75;
			// 
			// statusBarPanel2
			// 
			this.statusBarPanel2.Text = "debug";
			// 
			// menuItem3
			// 
			this.menuItem3.Index = -1;
			this.menuItem3.Text = "";
			// 
			// menuItem4
			// 
			this.menuItem4.Index = -1;
			this.menuItem4.Text = "";
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.Color.FromArgb(((System.Byte)(64)), ((System.Byte)(64)), ((System.Byte)(64)));
			this.panel1.Controls.Add(this.renderBox);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 26);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(638, 492);
			this.panel1.TabIndex = 3;
			this.panel1.Resize += new System.EventHandler(this.panel1_Resize);
			// 
			// renderBox
			// 
			this.renderBox.BackColor = System.Drawing.Color.Black;
			this.renderBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.renderBox.Location = new System.Drawing.Point(56, 24);
			this.renderBox.Name = "renderBox";
			this.renderBox.Size = new System.Drawing.Size(344, 312);
			this.renderBox.TabIndex = 3;
			this.renderBox.TabStop = false;
			this.renderBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.renderBox_MouseUp);
			this.renderBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.renderBox_MouseDown);
			// 
			// toolBar1
			// 
			this.toolBar1.Buttons.AddRange(new TD.SandBar.ToolbarItemBase[] {
																				this.buttonItem1,
																				this.buttonItem2,
																				this.buttonItem3,
																				this.dropDownMenuItem1});
			this.toolBar1.Guid = new System.Guid("cf00879b-eed4-4f1a-88a5-33fbfb86ee13");
			this.toolBar1.ImageList = this.imageList1;
			this.toolBar1.IsOpen = true;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.Size = new System.Drawing.Size(638, 26);
			this.toolBar1.TabIndex = 7;
			this.toolBar1.Text = "toolBar1";
			this.toolBar1.ButtonClick += new TD.SandBar.ToolBar.ButtonClickEventHandler(this.toolBar1_ButtonClick);
			// 
			// buttonItem1
			// 
			this.buttonItem1.ImageIndex = 0;
			// 
			// buttonItem2
			// 
			this.buttonItem2.ImageIndex = 1;
			// 
			// buttonItem3
			// 
			this.buttonItem3.ImageIndex = 2;
			// 
			// dropDownMenuItem1
			// 
			this.dropDownMenuItem1.MenuItems.AddRange(new TD.SandBar.MenuButtonItem[] {
																						  this.menuButtonItem1,
																						  this.menuButtonItem2,
																						  this.menuButtonItem3});
			this.dropDownMenuItem1.Text = "Render Mode";
			// 
			// menuButtonItem1
			// 
			this.menuButtonItem1.Text = "Textured";
			// 
			// menuButtonItem2
			// 
			this.menuButtonItem2.Text = "Wireframe";
			// 
			// menuButtonItem3
			// 
			this.menuButtonItem3.Text = "Points";
			// 
			// imageList1
			// 
			this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// RenderingWindow
			// 
			this.AllowDrop = true;
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.statusBar1);
			this.Controls.Add(this.toolBar1);
			this.Name = "RenderingWindow";
			this.Size = new System.Drawing.Size(638, 534);
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanel1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.fpsPanel)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.entityX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.entityY)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.entityZ)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanel2)).EndInit();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		public EditorNode SelectedNode {
			get { return selectedNode; }
			set { 
				selectedNode = null;
				if(manipNode != null) {
					// Destroy the existing manipulator
					Root.Instance.SceneManager.DestroySceneNode(manipNode.Name);
					manipNode = null;
				}
				selectedNode = value;

				manipulatorHandleList.Clear();

				if(selectedNode != null) {
					this.statusBar1.Panels[0].Text = this.defaultSelStatusText;
					foreach(MovableObject o in selectedNode.AttachedObjects) {
						manipulator = selectedNode.GetOwner().GetManipulator(o);
						if(manipulator != null)
							break;
					}
					if(manipulator != null) {
						manipNode = manipulator.GetManipulatorNode(selectedNode);
						manipulatorHandleList = buildManipulatorHandleList(manipNode);
					}
				} else {
					this.statusBar1.Panels[0].Text = this.defaultStatusText;
				}
			}
		}

		private ArrayList buildManipulatorHandleList(SceneNode sn) {
			ArrayList result = new ArrayList();
			for(int i=0; i<sn.ObjectCount; i++) {
				result.Add(sn.GetObject(i).Name);
			}
			for(int i=0; i<sn.ChildCount; i++) {
				result.AddRange(buildManipulatorHandleList(sn.GetChild(i) as SceneNode));
			}
			return result;
	}

		private void Instance_SelectedObjectChanged(object sender, EditorNode obj) {
			this.SelectedNode = obj;
			Root.Instance.RenderOneFrame();
		}

		private void renderWindow_BeforeViewportUpdate(object sender, ViewportUpdateEventArgs e) {
			Root.Instance.SceneManager.RootSceneNode.AddChild(camNodes);
			if(selectedNode != null) {
				selectedNode.HighlightNode();
			}
		}

		private void renderWindow_AfterViewportUpdate(object sender, ViewportUpdateEventArgs e) {
			Root.Instance.SceneManager.RootSceneNode.RemoveChild(camNodes);
			if(selectedNode != null) {
				selectedNode.UnhighlightNode();
			}		
		}

		private void toolBar1_ButtonClick(object sender, TD.SandBar.ToolBarItemEventArgs e) {
			bool uncheck = false;
			if(e.Item is TD.SandBar.ButtonItem)
				uncheck = (e.Item as TD.SandBar.ButtonItem).Checked;
			for(int i=0; i<3; i++) {
				((sender as TD.SandBar.ToolBar).Buttons[i] as TD.SandBar.ButtonItem).Checked = false;
			}
			if(!uncheck && (e.Item is TD.SandBar.ButtonItem)) {
				(e.Item as TD.SandBar.ButtonItem).Checked = true;
			}
		}

		private void renderBox_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
			if(manipulator != null)
				manipulator.ManipulatorHandleMouseUp(input);
		}
	}
}
