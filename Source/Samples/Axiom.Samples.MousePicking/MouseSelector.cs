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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Axiom;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Animating;
using Axiom.Math.Collections;

namespace Axiom.Samples.MousePicking
{
	/// <summary>
	/// Object to assist in selecting and manipulating in game objects
	/// </summary>
	public class MouseSelector
	{
		#region Public Properties and Enums

		/// <summary>
		/// Selection mode types
		/// </summary>
		public enum SelectionModeType
		{
			None,
			MouseClick,
			SelectionBox
		}

		/// <summary>
		/// Selection mode type property
		/// </summary>
		public SelectionModeType SelectionMode = SelectionModeType.MouseClick;

		/// <summary>
		/// Selected Objects
		/// </summary>
		public List<MovableObject> Selection = new List<MovableObject>();

		/// <summary>
		/// Toggles logging to standard axiom log file
		/// </summary>
		public bool VerboseLogging = true;

		/// <summary>
		/// Keep previous selection
		/// </summary>
		public bool KeepPreviousSelection = false;

		#endregion

		#region Private Variables

		/// <summary>
		/// Private variable for the Main Camera from application, set on object creation
		/// </summary>
		private readonly Camera _Camera = null;

		/// <summary>
		/// Private variable for the Main Window from application, set on object creation
		/// </summary>
		private RenderWindow _Window = null;

		/// <summary>
		/// Private variable for when the object is in selection mode
		/// </summary>
		private bool Selecting = false;

		/// <summary>
		/// Private variable for the name of the object's selection box and scenenode
		/// </summary>
		private static readonly NameGenerator<MouseSelector> _nameGenerator =
			new NameGenerator<MouseSelector>( "MouseSelector" );

		private readonly string _name;

		/// <summary>
		/// Private Variable for the Selection Rectangle 
		/// </summary>
		private readonly SelectionRectangle _rect;

		/// <summary>
		/// Start vector for when the MouseSelector is in SelectionBox mode. Set on mouse down
		/// </summary>
		private Vector2 _start;

		/// <summary>
		/// Stop vector for when the MouseSelector is in SelectionBox mode. set by current position of mouse as it is moved till the mouse 
		/// button is released
		/// </summary>
		private Vector2 _stop;

		#endregion Private Variables

		#region Constructors / Destructors

		/// <summary>
		/// This is the Constructor for the MouseSelection object, scene must be fully initialized and 
		/// valid camera and window must be passed in, generic name will be created
		/// </summary>
		/// <param name="camera">Camera</param>
		/// <param name="window">RenderWindow</param>
		public MouseSelector( Camera camera, RenderWindow window )
			: this( _nameGenerator.GetNextUniqueName(), camera, window )
		{
		}

		/// <summary>
		/// This is the Constructor for the MouseSelection object, scene must be fully initialized and 
		/// valid camera and window must be passed in, name must be passed
		/// </summary>
		/// <param name="name">string</param>
		/// <param name="camera">Camera</param>
		/// <param name="window">RenderWindow</param>
		public MouseSelector( string name, Camera camera, RenderWindow window )
		{
			this._Camera = camera;
			this._Window = window;
			this._name = name;
			this._rect = new SelectionRectangle( this._name );
			this._Camera.ParentSceneNode.CreateChildSceneNode( this._name + "_node" ).AttachObject( this._rect );
			Log( this._name + " created, and attached to " + this._name + "_node." );
		}

		/// <summary>
		/// Destructor for the object
		/// </summary>
		~MouseSelector()
		{
			this.Selection = null;
			this.Selecting = false;
		}

		#endregion

		/// <summary>
		/// private method for selection object that creates a box from a single mouse click
		/// </summary>
		private void PerformSelectionWithMouseClick()
		{
			Log( "MouseSelector: " + this._name + " single click selecting at (" + this._start.x.ToString() + ";" +
			     this._start.y.ToString() +
			     ")" );
			Ray mouseRay = this._Camera.GetCameraToViewportRay( this._start.x, this._start.y );
			RaySceneQuery RayScnQuery = Root.Instance.SceneManager.CreateRayQuery( mouseRay );
			RayScnQuery.SortByDistance = true;
			foreach ( RaySceneQueryResultEntry re in RayScnQuery.Execute() )
			{
				SelectObject( re.SceneObject );
			}
		}

		/// <summary>
		/// private method for selection object that creates a box from the SelectionRectangle, stop variable is passed in
		/// </summary>
		/// <param name="first">Vector2</param>
		/// <param name="second">Vector2</param>
		private void PerformSelectionWithSelectionBox( Math.Vector2 first, Math.Vector2 second )
		{
			Log( "MouseSelector: " + this._name + " performing selection." );

			float left = first.x, right = second.x, top = first.y, bottom = second.y;

			if ( left > right )
			{
				Utility.Swap( ref left, ref right );
			}

			if ( top > bottom )
			{
				Utility.Swap( ref top, ref bottom );
			}

			if ( ( right - left )*( bottom - top ) < 0.0001 )
			{
				return;
			}

			Ray topLeft = this._Camera.GetCameraToViewportRay( left, top );
			Ray topRight = this._Camera.GetCameraToViewportRay( right, top );
			Ray bottomLeft = this._Camera.GetCameraToViewportRay( left, bottom );
			Ray bottomRight = this._Camera.GetCameraToViewportRay( right, bottom );

			var vol = new PlaneBoundedVolume();
			vol.planes.Add( new Math.Plane( topLeft.GetPoint( 3 ), topRight.GetPoint( 3 ), bottomRight.GetPoint( 3 ) ) );
			// front plane
			vol.planes.Add( new Math.Plane( topLeft.Origin, topLeft.GetPoint( 100 ), topRight.GetPoint( 100 ) ) ); // top plane
			vol.planes.Add( new Math.Plane( topLeft.Origin, bottomLeft.GetPoint( 100 ), topLeft.GetPoint( 100 ) ) );
			// left plane
			vol.planes.Add( new Math.Plane( bottomLeft.Origin, bottomRight.GetPoint( 100 ), bottomLeft.GetPoint( 100 ) ) );
			// bottom plane
			vol.planes.Add( new Math.Plane( topRight.Origin, topRight.GetPoint( 100 ), bottomRight.GetPoint( 100 ) ) );
			// right plane

			var volList = new PlaneBoundedVolumeList();
			volList.Add( vol );

			PlaneBoundedVolumeListSceneQuery volQuery;

			volQuery = Root.Instance.SceneManager.CreatePlaneBoundedVolumeQuery( new PlaneBoundedVolumeList() );
			volQuery.Volumes = volList;
			SceneQueryResult result = volQuery.Execute();

			foreach ( MovableObject obj in result.objects )
			{
				SelectObject( obj );
			}
		}

		/// <summary>
		/// Public Method for deselecting all objects previously selected.
		/// once they are selected they stay selected till the object is cleared or another selection begins.
		/// </summary>
		public void DeselectObjects()
		{
			if ( !this.KeepPreviousSelection )
			{
				foreach ( MovableObject iter in this.Selection )
				{
					iter.ParentSceneNode.ShowBoundingBox = false;
				}
				this.Selection.Clear();
			}
		}

		/// <summary>
		/// Private method that adds any object not currently selected into the selection list
		/// </summary>
		/// <param name="obj">MovableObject</param>
		private void SelectObject( MovableObject obj )
		{
			if ( !this.Selection.Contains( obj ) )
			{
				Log( "MouseSelector: " + this._name + " selecting object " + obj.Name );
				obj.ParentSceneNode.ShowBoundingBox = true;
				this.Selection.Add( obj );
			}
		}

		/// <summary>
		/// public method to call when the mouse is pressed
		/// </summary>
		/// <param name="evt">MouseEventArgs</param>
		/// <param name="id">MouseButtonID</param>
		public void MousePressed( SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id )
		{
			if ( id == SharpInputSystem.MouseButtonID.Left )
			{
				this.Selecting = true;
				Clear();
				switch ( this.SelectionMode )
				{
					case SelectionModeType.SelectionBox:
						Log( "MouseSelector: Selection starting for " + this._name );
						Clear();
						this._start = new Vector2( evt.State.X.Absolute/(float)this._Camera.Viewport.ActualWidth,
						                           evt.State.Y.Absolute/(float)this._Camera.Viewport.ActualHeight );
						this._stop = this._start;
						this._rect.IsVisible = true;
						Log( "MouseSelector: " + this._name + " selecting from top(" + this._start.x.ToString() + ";" +
						     this._start.y.ToString() + ")" );
						this._rect.SetCorners( this._start, this._stop );
						break;

					case SelectionModeType.MouseClick:
						this._start = new Vector2( evt.State.X.Absolute/(float)this._Camera.Viewport.ActualWidth,
						                           evt.State.Y.Absolute/(float)this._Camera.Viewport.ActualHeight );
						break;
				}
			}
		}

		/// <summary>
		/// public method to call when the mouse is moved
		/// </summary>
		/// <param name="evt"></param>
		public void MouseMoved( SharpInputSystem.MouseEventArgs evt )
		{
			if ( ( this.SelectionMode == SelectionModeType.SelectionBox ) && this.Selecting == true )
			{
				this._stop = new Vector2( evt.State.X.Absolute/(float)this._Camera.Viewport.ActualWidth,
				                          evt.State.Y.Absolute/(float)this._Camera.Viewport.ActualHeight );
				this._rect.SetCorners( this._start, this._stop );
			}
		}

		/// <summary>
		/// public method to call when the mouse button is released
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="id"></param>
		public void MouseReleased( SharpInputSystem.MouseEventArgs evt, SharpInputSystem.MouseButtonID id )
		{
			if ( id == SharpInputSystem.MouseButtonID.Left && this.Selecting == true )
			{
				switch ( this.SelectionMode )
				{
					case SelectionModeType.SelectionBox:
						Log( "MouseSelector: " + this._name + " selecting to bottom(" + this._stop.x.ToString() + ";" +
						     this._stop.y.ToString() + ")" );
						PerformSelectionWithSelectionBox( this._start, this._stop );
						this.Selecting = false;
						this._rect.IsVisible = false;
						Log( "MouseSelector: " + this._name + " selection complete." );
						break;

					case SelectionModeType.MouseClick:
						PerformSelectionWithMouseClick();
						break;
				}
				this.Selecting = false;
			}
		}

		/// <summary>
		/// public method to clear and reset the MouseSelector object
		/// </summary>
		public void Clear()
		{
			this._rect.Clear();
			DeselectObjects();
		}

		/// <summary>
		/// Logging via the LogManager
		/// </summary>
		/// <param name="message">string</param>
		private void Log( string message )
		{
			if ( this.VerboseLogging == true )
			{
				LogManager.Instance.Write( message );
			}
		}
	}
}