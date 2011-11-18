#region MIT/X11 License
//Copyright (c) 2009 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

using Axiom.Animating;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;

namespace Axiom.Samples.Extras.MousePicking
{
	/// <summary>
	/// Sample of selecting objects with a mouse
	/// </summary>
	class MousePickingSample : SdkSample
	{

		/// <summary>
		/// safety check for mouse picking sample calls
		/// </summary>
		private bool initialized = false;

		/// <summary>
		/// MouseSelector object
		/// </summary>
		private MouseSelector _MouseSelector = null;

		/// <summary>
		/// Sample menu GUI for setting selection mode
		/// </summary>
		private SelectMenu selectionModeMenu;

		/// <summary>
		/// sample label for displaying mouse coordinates
		/// </summary>
		protected Label MouseLocationLabel;

		/// <summary>
		/// Sample initialization
		/// </summary>
		public MousePickingSample()
		{
			Metadata[ "Title" ] = "Mouse Picking";
			Metadata[ "Description" ] = "Demonstrates selecting a node with a mouse.";
			Metadata[ "Thumbnail" ] = "thumb_picking.png";
			Metadata[ "Category" ] = "Interaction";
			Metadata[ "Help" ] = "Proof that Axiom is just the hottest thing. Bleh. So there. ^_^";

		}

		/// <summary>
		/// mouse pressed event override from SdkSample, calls MouseSelector's MousePressed method to start selection
		/// </summary>
		/// <param name="evt">MouseEventArgs</param>
		/// <param name="id">MouseButtonID</param>
		/// <returns>bool</returns>
		public override bool MousePressed( SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id )
		{
			if ( initialized )
			{
				_MouseSelector.MousePressed( evt, id );
			}
			return base.MousePressed( evt, id );
		}

		/// <summary>
		/// mouse moved event override from SdkSample, calls the MouseSelector's MouseMoved event for SelectionBox
		/// </summary>
		/// <param name="evt">MouseEventArgs</param>
		/// <returns>bool</returns>
		public override bool MouseMoved( SharpInputSystem.MouseEventArgs evt )
		{
			if ( initialized )
			{
				_MouseSelector.MouseMoved( evt );
				MouseLocationLabel.Caption = "(x:y) " + ( evt.State.X.Absolute / (float)Camera.Viewport.ActualWidth ).ToString() + ":" + ( evt.State.Y.Absolute / (float)Camera.Viewport.ActualHeight ).ToString();
			}
			return base.MouseMoved( evt );
		}

		/// <summary>
		/// mouse released event override from SdkSample, calls MouseSelector's MouseReleased method to end selection
		/// </summary>
		/// <param name="evt">MouseEventArgs</param>
		/// <param name="id">MouseButtonID</param>
		/// <returns>bool</returns>
		public override bool MouseReleased( SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id )
		{
			if ( initialized )
			{
				_MouseSelector.MouseReleased( evt, id );
			}
			return base.MouseReleased( evt, id );
		}

		/// <summary>
		/// key pressed selection event to have the ability to keep the current selection, sets the MouseSelector.KeepPreviousSelection
		/// if the right or left control key is pressed
		/// </summary>
		/// <param name="evt">KeyEventArgs</param>
		/// <returns>bool</returns>
		public override bool KeyPressed( SharpInputSystem.KeyEventArgs evt )
		{
			if ( initialized )
			{
				if ( evt.Key == SharpInputSystem.KeyCode.Key_LCONTROL || evt.Key == SharpInputSystem.KeyCode.Key_RCONTROL )
				{
					_MouseSelector.KeepPreviousSelection = true;
				}
			}
			return base.KeyPressed( evt );
		}

		/// <summary>
		/// key pressed selection event to have the ability to keep the current selection, sets the MouseSelector.KeepPreviousSelection
		/// if the right or left control key is pressed
		/// </summary>
		/// <param name="evt">KeyEventArgs</param>
		/// <returns>bool</returns>
		public override bool KeyReleased( SharpInputSystem.KeyEventArgs evt )
		{
			if ( initialized )
			{
				_MouseSelector.KeepPreviousSelection = false;
			}
			return base.KeyReleased( evt );
		}

		/// <summary>
		/// Sets up the samples content
		/// </summary>
		protected override void SetupContent()
		{

			// set some ambient light
			SceneManager.AmbientLight = new ColorEx( 1.0f, 0.2f, 0.2f, 0.2f );

			// create a skydome
			SceneManager.SetSkyDome( true, "Examples/CloudySky", 5, 8 );

			// create a simple default point light
			Light light = SceneManager.CreateLight( "MainLight" );
			light.Position = new Vector3( 20, 80, 50 );

			// create a plane for the plane mesh
			Plane plane = new Plane();
			plane.Normal = Vector3.UnitY;
			plane.D = 200;

			// create a plane mesh
			MeshManager.Instance.CreatePlane( "FloorPlane", ResourceGroupManager.DefaultResourceGroupName, plane, 200000, 200000, 20, 20, true, 1, 50, 50, Vector3.UnitZ );

			// create an entity to reference this mesh
			Entity planeEntity = SceneManager.CreateEntity( "Floor", "FloorPlane" );
			planeEntity.MaterialName = "Examples/Rockwall";
			SceneManager.RootSceneNode.CreateChildSceneNode().AttachObject( planeEntity );

			// create an entity to have follow the path
			Entity ogreHead = SceneManager.CreateEntity( "OgreHead", "ogrehead.mesh" );

			// Load muiltiple heads for box select and demonstrating single select with stacked objects
			// create a scene node for each entity and attach the entity
			SceneNode ogreHead1Node;
			ogreHead1Node = SceneManager.RootSceneNode.CreateChildSceneNode( "OgreHeadNode", Vector3.Zero, Quaternion.Identity );
			ogreHead1Node.AttachObject( ogreHead );

			SceneNode ogreHead2Node;
			Entity ogreHead2 = SceneManager.CreateEntity( "OgreHead2", "ogrehead.mesh" );
			ogreHead2Node = SceneManager.RootSceneNode.CreateChildSceneNode( "OgreHead2Node", new Vector3( -100, 0, 0 ), Quaternion.Identity );
			ogreHead2Node.AttachObject( ogreHead2 );

			SceneNode ogreHead3Node;
			Entity ogreHead3 = SceneManager.CreateEntity( "OgreHead3", "ogrehead.mesh" );
			ogreHead3Node = SceneManager.RootSceneNode.CreateChildSceneNode( "OgreHead3Node", new Vector3( +100, 0, 0 ), Quaternion.Identity );
			ogreHead3Node.AttachObject( ogreHead3 );

			// make sure the camera tracks this node
			// set initial camera position
			CameraManager.setStyle( CameraStyle.FreeLook );
			Camera.Position = new Vector3( 0, 0, 500 );
			Camera.SetAutoTracking( true, ogreHead1Node, Vector3.Zero );

			// create a scene node to attach the camera to
			SceneNode cameraNode = SceneManager.RootSceneNode.CreateChildSceneNode( "CameraNode" );
			cameraNode.AttachObject( Camera );

			// turn on some fog
			SceneManager.SetFog( FogMode.Exp, ColorEx.White, 0.0002f );

			_MouseSelector = new MouseSelector( "MouseSelector", Camera, Window );
			_MouseSelector.SelectionMode = MouseSelector.SelectionModeType.None;

			SetupGUI();
			initialized = true;
			base.SetupContent();
		}

		protected override void CleanupContent()
		{
			MeshManager.Instance.Remove( "FloorPlane" );

			base.CleanupContent();
		}

		/// <summary>
		/// Creates and initializes all the scene's GUI elements not defined in SdkSample
		/// </summary>
		protected void SetupGUI()
		{
			selectionModeMenu = TrayManager.CreateLongSelectMenu( TrayLocation.TopRight, "SelectionModeMenu", "Selection Mode", 300, 150, 3 );
			selectionModeMenu.AddItem( "None" );
			selectionModeMenu.AddItem( "Mouse Select" );
			selectionModeMenu.AddItem( "Selection Box" );
			selectionModeMenu.SelectItem( 0 );
			selectionModeMenu.SelectedIndexChanged += new System.EventHandler( selectionModeMenu_SelectedIndexChanged );

			MouseLocationLabel = TrayManager.CreateLabel( TrayLocation.TopLeft, "Mouse Location", "", 350 );

			TrayManager.ShowCursor();
		}

		/// <summary>
		/// Event for when the menu changes, sets the MouseSelectors SelectionMode
		/// </summary>
		/// <param name="sender">SelectMenu object</param>
		/// <param name="e">EventArgs</param>
		void selectionModeMenu_SelectedIndexChanged( object sender, System.EventArgs e )
		{
			SelectMenu menu = sender as SelectMenu;
			if ( menu != null )
			{
				_MouseSelector.SelectionMode = (MouseSelector.SelectionModeType)menu.SelectionIndex;
			}

		}
	}
}
