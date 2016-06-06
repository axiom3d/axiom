#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
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
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Overlays;
using DisplayString = System.String;
using SIS = SharpInputSystem;
using WidgetList = System.Collections.Generic.List<Axiom.Samples.Widget>;

namespace Axiom.Samples
{
	/// <summary>
	/// enumerator values for widget tray anchoring locations
	/// </summary>
	public enum TrayLocation
	{
		TopLeft,
		Top,
		TopRight,
		Left,
		Center,
		Right,
		BottomLeft,
		Bottom,
		BottomRight,
		None
	};

	/// <summary>
	/// enumerator values for button states
	/// </summary>
	public enum ButtonState
	{
		Up,
		Over,
		Down
	};

	/// <summary>
	/// Main class to manage a cursor, backdrop, trays and widgets.
	/// </summary>
	public class SdkTrayManager : ISdkTrayListener, IResourceGroupListener
	{
		#region events

		public event ButtonHitDelegate ButtonHit;

		#endregion

		#region fields

		protected String mName; // name of this tray system
		protected RenderWindow mWindow; // render window
		protected SIS.Mouse Mouse; // mouse device
		protected Overlay backdropLayer; // backdrop layer
		protected Overlay mTraysLayer; // widget layer
		protected Overlay mPriorityLayer; // top priority layer
		protected Overlay cursorLayer; // cursor layer
		protected OverlayElementContainer backdrop; // backdrop
		protected OverlayElementContainer[] mTrays = new OverlayElementContainer[10]; // widget trays
		protected WidgetList[] mWidgets = new WidgetList[10]; // widgets
		protected WidgetList mWidgetDeathRow = new WidgetList(); // widget queue for deletion
		protected OverlayElementContainer cursor; // cursor
		protected ISdkTrayListener listener; // tray listener
		protected Real mWidgetPadding; // widget padding
		protected Real mWidgetSpacing; // widget spacing
		protected Real mTrayPadding; // tray padding
		protected bool mTrayDrag; // a mouse press was initiated on a tray
		protected SelectMenu expandedMenu; // top priority expanded menu widget
		protected TextBox Dialog; // top priority dialog widget
		protected OverlayElementContainer mDialogShade; // top priority dialog shade
		protected Button mOk; // top priority OK button
		protected Button mYes; // top priority Yes button
		protected Button mNo; // top priority No button
		protected bool CursorWasVisible; // cursor state before showing dialog
		protected Label mFpsLabel; // FPS label
		protected ParamsPanel StatsPanel; // frame stats panel
		protected DecorWidget Logo; // logo
		protected ProgressBar LoadBar; // loading bar
		protected Real groupInitProportion; // proportion of load job assigned to initialising one resource group
		protected Real groupLoadProportion; // proportion of load job assigned to loading one resource group
		protected Real loadInc; // loading increment
		protected HorizontalAlignment[] trayWidgetAlign = new HorizontalAlignment[10]; // tray widget alignments

		#endregion

		#region properties

		public SelectMenu ExpandedMenu
		{
			get
			{
				return this.expandedMenu;
			}
			set
			{
				if ( this.expandedMenu == null && value != null )
				{
					var c = (OverlayElementContainer)value.OverlayElement;
					var eb = (OverlayElementContainer)c.Children[ value.Name + "/MenuExpandedBox" ];
					eb.Update();
					eb.SetPosition( (int)( eb.DerivedLeft*OverlayManager.Instance.ViewportWidth ),
					                (int)( eb.DerivedTop*OverlayManager.Instance.ViewportHeight ) );
					c.RemoveChild( eb.Name );
					this.mPriorityLayer.AddElement( eb );
				}
				else if ( this.expandedMenu != null && value == null )
				{
					OverlayElementContainer eb = this.mPriorityLayer.GetChild( this.expandedMenu.Name + "/MenuExpandedBox" );
					this.mPriorityLayer.RemoveElement( eb );
					( (OverlayElementContainer)this.expandedMenu.OverlayElement ).AddChild( eb );
				}

				this.expandedMenu = value;
			}
		}

		/// <summary>
		///  Gets the number of widgets in total.
		/// </summary>
		public int WidgetCount
		{
			get
			{
				int total = 0;

				for ( int i = 0; i < 10; i++ )
				{
					total += this.mWidgets[ i ].Count;
				}

				return total;
			}
		}

		/// <summary>
		/// Determines if any dialog is currently visible.
		/// </summary>
		public bool IsDialogVisible
		{
			get
			{
				return this.Dialog != null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool IsLoadingBarVisible
		{
			get
			{
				return this.LoadBar != null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool IsLogoVisible
		{
			get
			{
				return this.Logo != null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public bool IsFrameStatsVisible
		{
			get
			{
				return this.mFpsLabel != null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Real WidgetPadding
		{
			set
			{
				this.mWidgetPadding = System.Math.Max( value, 0 );
				AdjustTrays();
			}
			get
			{
				return this.mWidgetPadding;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Real WidgetSpacing
		{
			set
			{
				this.mWidgetSpacing = System.Math.Max( value, 0 );
				AdjustTrays();
			}
			get
			{
				return this.mWidgetSpacing;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Real TrayPadding
		{
			set
			{
				this.mTrayPadding = System.Math.Max( value, 0 );
				AdjustTrays();
			}
			get
			{
				return this.mTrayPadding;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool IsCursorVisible
		{
			get
			{
				return this.cursorLayer.IsVisible;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool IsBackdropVisible
		{
			get
			{
				return BackdropLayer.IsVisible;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool AreTraysVisible
		{
			get
			{
				return this.mTraysLayer.IsVisible;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public ISdkTrayListener Listener
		{
			get
			{
				return this.listener;
			}
			set
			{
				this.listener = value;
			}
		}

		/// <summary>
		///  these methods get the underlying overlays and overlay elements
		/// </summary>
		public OverlayElementContainer[] TrayContainer
		{
			get
			{
				return this.mTrays;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Overlay BackdropLayer
		{
			get
			{
				return this.backdropLayer;
			}
			protected set
			{
				this.backdropLayer = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Overlay TraysLayer
		{
			get
			{
				return this.mTraysLayer;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Overlay CursorLayer
		{
			get
			{
				return this.cursorLayer;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public OverlayElementContainer BackdropContainer
		{
			get
			{
				return this.backdrop;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public OverlayElementContainer CursorContainer
		{
			get
			{
				return this.cursor;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public OverlayElement CursorImage
		{
			get
			{
				return this.cursor.Children[ this.cursor.Name + "/CursorImage" ];
			}
		}

		#endregion properties

		/// <summary>
		/// Creates backdrop, cursor, and trays.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="window"></param>
		/// <param name="mouse"></param>
		public SdkTrayManager( String name, RenderWindow window, SIS.Mouse mouse )
			: this( name, window, mouse, null )
		{
		}

		/// <summary>
		/// Creates backdrop, cursor, and trays.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="window"></param>
		/// <param name="mouse"></param>
		/// <param name="listener"></param>
		public SdkTrayManager( String name, RenderWindow window, SIS.Mouse mouse, ISdkTrayListener listener )
		{
			this.mName = name;
			this.mWindow = window;
			this.Mouse = mouse;
			Listener = listener;

			this.mWidgetPadding = 8;
			this.mWidgetSpacing = 2;

			OverlayManager om = OverlayManager.Instance;

			String nameBase = this.mName + "/";
			nameBase.Replace( ' ', '_' );

			// create overlay layers for everything

			BackdropLayer = om.Create( nameBase + "BackdropLayer" );
			this.mTraysLayer = om.Create( nameBase + "WidgetsLayer" );
			this.mPriorityLayer = om.Create( nameBase + "PriorityLayer" );
			this.cursorLayer = om.Create( nameBase + "CursorLayer" );
			BackdropLayer.ZOrder = 100;
			this.mTraysLayer.ZOrder = 200;
			this.mPriorityLayer.ZOrder = 300;
			this.cursorLayer.ZOrder = 400;

			// make backdrop and cursor overlay containers

			this.cursor =
				(OverlayElementContainer)om.Elements.CreateElementFromTemplate( "SdkTrays/Cursor", "Panel", nameBase + "Cursor" );
			this.cursorLayer.AddElement( this.cursor );
			this.backdrop = (OverlayElementContainer)om.Elements.CreateElement( "Panel", nameBase + "Backdrop" );
			BackdropLayer.AddElement( this.backdrop );
			this.mDialogShade = (OverlayElementContainer)om.Elements.CreateElement( "Panel", nameBase + "DialogShade" );
			this.mDialogShade.MaterialName = "SdkTrays/Shade";
			this.mDialogShade.Hide();
			this.mPriorityLayer.AddElement( this.mDialogShade );

			String[] trayNames = {
			                     	"TopLeft", "Top", "TopRight", "Left", "Center", "Right", "BottomLeft", "Bottom", "BottomRight"
			                     };

			for ( int i = 0; i < 9; i++ ) // make the real trays
			{
				this.mTrays[ i ] =
					(OverlayElementContainer)
					om.Elements.CreateElementFromTemplate( "SdkTrays/Tray", "BorderPanel", nameBase + trayNames[ i ] + "Tray" );

				this.mTraysLayer.AddElement( this.mTrays[ i ] );

				this.trayWidgetAlign[ i ] = HorizontalAlignment.Center;

				// align trays based on location
				if ( i == (int)TrayLocation.Top || i == (int)TrayLocation.Center || i == (int)TrayLocation.Bottom )
				{
					this.mTrays[ i ].HorizontalAlignment = HorizontalAlignment.Center;
				}
				if ( i == (int)TrayLocation.Left || i == (int)TrayLocation.Center || i == (int)TrayLocation.Right )
				{
					this.mTrays[ i ].VerticalAlignment = VerticalAlignment.Center;
				}
				if ( i == (int)TrayLocation.TopRight || i == (int)TrayLocation.Right || i == (int)TrayLocation.BottomRight )
				{
					this.mTrays[ i ].HorizontalAlignment = HorizontalAlignment.Right;
				}
				if ( i == (int)TrayLocation.BottomLeft || i == (int)TrayLocation.Bottom || i == (int)TrayLocation.BottomRight )
				{
					this.mTrays[ i ].VerticalAlignment = VerticalAlignment.Bottom;
				}
			}

			// create the null tray for free-floating widgets
			this.mTrays[ 9 ] = (OverlayElementContainer)om.Elements.CreateElement( "Panel", nameBase + "NullTray" );
			this.trayWidgetAlign[ 9 ] = HorizontalAlignment.Left;
			this.mTraysLayer.AddElement( this.mTrays[ 9 ] );

			for ( int i = 0; i < this.mWidgets.Length; i++ )
			{
				this.mWidgets[ i ] = new WidgetList();
			}

			AdjustTrays();

			ShowTrays();
			ShowCursor();
		}

		/// <summary>
		/// Destroys background, cursor, widgets, and trays.
		/// </summary>
		public void Dispose()
		{
			OverlayManager om = OverlayManager.Instance;

			DestroyAllWidgets();

			for ( int i = 0; i < this.mWidgetDeathRow.Count; i++ ) // delete widgets queued for destruction
			{
				this.mWidgetDeathRow[ i ] = null;
			}
			this.mWidgetDeathRow.Clear();
			if ( om != null )
			{
				om.Destroy( BackdropLayer );
				om.Destroy( this.mTraysLayer );
				om.Destroy( this.mPriorityLayer );
				om.Destroy( this.cursorLayer );

				//CloseDialog();
				//hideLoadingBar();

				Widget.NukeOverlayElement( this.backdrop );
				Widget.NukeOverlayElement( this.cursor );
				Widget.NukeOverlayElement( this.mDialogShade );

				for ( int i = 0; i < 10; i++ )
				{
					Widget.NukeOverlayElement( this.mTrays[ i ] );
				}
			}
		}

		/// <summary>
		/// Converts a 2D screen coordinate (in pixels) to a 3D ray into the scene.
		/// </summary>
		/// <param name="cam"></param>
		/// <param name="pt"></param>
		/// <returns></returns>
		public static Ray ScreenToScene( Camera cam, Vector2 pt )
		{
			return cam.GetCameraToViewportRay( pt.x, pt.y );
		}

		/// <summary>
		/// Converts a 3D scene position to a 2D screen coordinate (in pixels).
		/// </summary>
		/// <param name="cam"></param>
		/// <param name="?"></param>
		/// <returns></returns>
		public static Vector2 sceneToScreen( Camera cam, Vector3 pt )
		{
			Vector3 result = cam.ProjectionMatrix*cam.ViewMatrix*pt;
			return new Vector2( ( result.x + 1 )/2, -( result.y + 1 )/2 );
		}

		#region ISdkTrayListerner implementation

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="yesHit"></param>
		public void YesNoDialogClosed( string text, bool yesHit )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		public void OkDialogClosed( string text )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="slider"></param>
		public void SliderMoved( Slider slider )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="box"></param>
		public void CheckboxToggled( CheckBox box )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="menu"></param>
		public void ItemSelected( SelectMenu menu )
		{
		}

		#endregion ISdkTrayListerner implementation

		/// <summary>
		/// 
		/// </summary>
		public void ShowAll()
		{
			ShowBackdrop();
			ShowTrays();
			ShowCursor();
		}

		/// <summary>
		/// 
		/// </summary>
		public void HideAll()
		{
			HideBackdrop();
			HideTrays();
			HideCursor();
		}

		/// <summary>
		/// Displays specified material on backdrop, or the last material used if<para></para>
		/// none specified. Good for pause menus like in the browser.
		/// </summary>
		public void ShowBackdrop()
		{
			ShowBackdrop( String.Empty );
		}

		/// <summary>
		/// Displays specified material on backdrop, or the last material used if<para></para>
		/// none specified. Good for pause menus like in the browser.
		/// </summary>
		/// <param name="materialName"></param>
		public void ShowBackdrop( String materialName )
		{
			if ( materialName != String.Empty )
			{
				this.backdrop.MaterialName = materialName;
			}
			BackdropLayer.Show();
		}

		/// <summary>
		/// 
		/// </summary>
		public void HideBackdrop()
		{
			BackdropLayer.Hide();
		}

		/// <summary>
		/// Displays specified material on cursor, or the last material used if<para></para>
		/// none specified. Used to change cursor type.
		/// </summary>
		public void ShowCursor()
		{
			ShowCursor( String.Empty );
		}

		/// <summary>
		/// Displays specified material on cursor, or the last material used if<para></para>
		/// none specified. Used to change cursor type.
		/// </summary>
		/// <param name="materialName"></param>
		public void ShowCursor( String materialName )
		{
			if ( materialName != String.Empty )
			{
				CursorImage.MaterialName = materialName;
			}

			if ( !this.cursorLayer.IsVisible )
			{
				this.cursorLayer.Show();
				RefreshCursor();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void HideCursor()
		{
			this.cursorLayer.Hide();

			// give widgets a chance to reset in case they're in the middle of something
			for ( int i = 0; i < 10; i++ )
			{
				if ( this.mWidgets[ i ] != null )
				{
					for ( int j = 0; j < this.mWidgets[ i ].Count; j++ )
					{
						this.mWidgets[ i ][ j ].OnLostFocus();
					}
				}
			}

			//SetExpandedMenu( 0 );
		}

		/// <summary>
		/// Updates cursor position based on unbuffered mouse state. This is necessary
		/// because if the tray manager has been cut off from mouse events for a time,
		/// the cursor position will be out of date.
		/// </summary>
		public void RefreshCursor()
		{
			int cursorX = 0, cursorY = 0;
			if ( this.Mouse != null )
			{
				cursorX = this.Mouse.MouseState.X.Absolute;
				cursorY = this.Mouse.MouseState.Y.Absolute;
			}
			this.cursor.SetPosition( cursorX, cursorY );
		}

		/// <summary>
		/// 
		/// </summary>
		public void ShowTrays()
		{
			this.mTraysLayer.Show();
			this.mPriorityLayer.Show();
		}

		/// <summary>
		/// 
		/// </summary>
		public void HideTrays()
		{
			this.mTraysLayer.Hide();
			this.mPriorityLayer.Hide();

			// give widgets a chance to reset in case they're in the middle of something
			for ( int i = 0; i < 10; i++ )
			{
				for ( int j = 0; j < this.mWidgets[ i ].Count; j++ )
				{
					this.mWidgets[ i ][ j ].OnLostFocus();
				}
			}

			ExpandedMenu = null;
		}

		/// <summary>
		/// Sets horizontal alignment of a tray's contents.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="gha"></param>
		public void SetTrayWidgetAlignment( TrayLocation trayLoc, HorizontalAlignment gha )
		{
			this.trayWidgetAlign[ (int)trayLoc ] = gha;

			for ( int i = 0; i < this.mWidgets[ (int)trayLoc ].Count; i++ )
			{
				this.mWidgets[ (int)trayLoc ][ i ].OverlayElement.HorizontalAlignment = gha;
			}
		}

		/// <summary>
		/// Fits trays to their contents and snaps them to their anchor locations.
		/// </summary>
		public virtual void AdjustTrays()
		{
			for ( int i = 0; i < 9; i++ ) // resizes and hides trays if necessary
			{
				Real trayWidth = 0;
				Real trayHeight = this.mWidgetPadding;
				var labelsAndSeps = new List<OverlayElement>();

				if ( this.mWidgets[ i ] == null || this.mWidgets[ i ].Count == 0 ) // hide tray if empty
				{
					this.mTrays[ i ].Hide();
					continue;
				}
				else
				{
					this.mTrays[ i ].Show();
				}

				// arrange widgets and calculate final tray size and position
				for ( int j = 0; j < this.mWidgets[ i ].Count; j++ )
				{
					OverlayElement e = this.mWidgets[ i ][ j ].OverlayElement;

					if ( j != 0 )
					{
						trayHeight += this.mWidgetSpacing; // don't space first widget
					}

					e.VerticalAlignment = VerticalAlignment.Top;
					e.Top = trayHeight;

					switch ( e.HorizontalAlignment )
					{
						case HorizontalAlignment.Left:
							e.Left = this.mWidgetPadding;
							break;
						case HorizontalAlignment.Right:
							e.Left = -( e.Width + this.mWidgetPadding );
							break;
						default:
							e.Left = ( -( e.Width/2 ) );
							break;
					}

					// prevents some weird texture filtering problems (just some)
					e.SetPosition( (int)e.Left, (int)e.Top );
					e.SetDimensions( (int)e.Width, (int)e.Height );

					trayHeight += e.Height;

					var l = this.mWidgets[ i ][ j ] as Label;
					if ( l != null && l.IsFitToTray )
					{
						labelsAndSeps.Add( e );
						continue;
					}
					var s = this.mWidgets[ i ][ j ] as Separator;
					if ( s != null && s.IsFitToTray )
					{
						labelsAndSeps.Add( e );
						continue;
					}

					if ( e.Width > trayWidth )
					{
						trayWidth = e.Width;
					}
				}

				// add paddings and resize trays
				this.mTrays[ i ].Width = trayWidth + 2*this.mWidgetPadding;
				this.mTrays[ i ].Height = trayHeight + this.mWidgetPadding;

				for ( int k = 0; k < labelsAndSeps.Count; k++ )
				{
					labelsAndSeps[ k ].Width = (int)trayWidth;
					labelsAndSeps[ k ].Left = -(int)( trayWidth/2 );
				}
			}

			for ( int i = 0; i < 9; i++ ) // snap trays to anchors
			{
				if ( i == (int)TrayLocation.TopLeft || i == (int)TrayLocation.Left || i == (int)TrayLocation.BottomLeft )
				{
					this.mTrays[ i ].Left = this.mTrayPadding;
				}
				if ( i == (int)TrayLocation.Top || i == (int)TrayLocation.Center || i == (int)TrayLocation.Bottom )
				{
					this.mTrays[ i ].Left = -this.mTrays[ i ].Width/2;
				}
				if ( i == (int)TrayLocation.TopRight || i == (int)TrayLocation.Right || i == (int)TrayLocation.BottomRight )
				{
					this.mTrays[ i ].Left = -( this.mTrays[ i ].Width + this.mTrayPadding );
				}

				if ( i == (int)TrayLocation.TopLeft || i == (int)TrayLocation.Top || i == (int)TrayLocation.TopRight )
				{
					this.mTrays[ i ].Top = this.mTrayPadding;
				}
				if ( i == (int)TrayLocation.Left || i == (int)TrayLocation.Center || i == (int)TrayLocation.Right )
				{
					this.mTrays[ i ].Top = -this.mTrays[ i ].Height/2;
				}
				if ( i == (int)TrayLocation.BottomLeft || i == (int)TrayLocation.Bottom || i == (int)TrayLocation.BottomRight )
				{
					this.mTrays[ i ].Top = -this.mTrays[ i ].Height - this.mTrayPadding;
				}

				// prevents some weird texture filtering problems (just some)
				this.mTrays[ i ].SetPosition( (int)this.mTrays[ i ].Left, (int)this.mTrays[ i ].Top );
				this.mTrays[ i ].SetDimensions( (int)this.mTrays[ i ].Width, (int)this.mTrays[ i ].Height );
			}
		}

		/// <summary>
		/// Returns a 3D ray into the scene that is directly underneath the cursor.
		/// </summary>
		/// <param name="cam"></param>
		/// <returns></returns>
		public Ray GetCursorRay( Camera cam )
		{
			return ScreenToScene( cam, new Vector2( this.cursor.DerivedLeft, this.cursor.DerivedTop ) );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <returns></returns>
		public Button CreateButton( TrayLocation trayLoc, String name, String caption )
		{
			return CreateButton( trayLoc, name, caption, 0 );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public Button CreateButton( TrayLocation trayLoc, String name, String caption, Real width )
		{
			var b = new Button( name, caption, width );
			MoveWidgetToTray( b, trayLoc );
			b.AssignedTrayListener = this.listener;
			return b;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		public TextBox CreateTextBox( TrayLocation trayLoc, String name, DisplayString caption, Real width, Real height )
		{
			var tb = new TextBox( name, caption, width, height );
			MoveWidgetToTray( tb, trayLoc );
			tb.AssignedTrayListener = this.listener;
			return tb;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="maxItemsShown"></param>
		/// <returns></returns>
		public SelectMenu CreateThickSelectMenu( TrayLocation trayLoc, String name, DisplayString caption, Real width,
		                                         int maxItemsShown )
		{
			return CreateThickSelectMenu( trayLoc, name, caption, width, maxItemsShown, new List<String>() );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="maxItemsShown"></param>
		/// <param name="items"></param>
		/// <returns></returns>
		public SelectMenu CreateThickSelectMenu( TrayLocation trayLoc, String name, DisplayString caption, Real width,
		                                         int maxItemsShown, IList<String> items )
		{
			var sm = new SelectMenu( name, caption, width, 0, maxItemsShown );
			MoveWidgetToTray( sm, trayLoc );
			sm.AssignedTrayListener = this.listener;
			if ( !( items.Count == 0 ) )
			{
				sm.Items = items;
			}
			return sm;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="boxWidth"></param>
		/// <param name="maxItemsShown"></param>
		/// <returns></returns>
		public SelectMenu CreateLongSelectMenu( TrayLocation trayLoc, String name, DisplayString caption, Real width,
		                                        Real boxWidth, int maxItemsShown )
		{
			return CreateLongSelectMenu( trayLoc, name, caption, width, boxWidth, maxItemsShown, new List<string>() );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="boxWidth"></param>
		/// <param name="maxItemsShown"></param>
		/// <returns></returns>
		public SelectMenu createLongSelectMenu( TrayLocation trayLoc, String name, DisplayString caption, Real boxWidth,
		                                        int maxItemsShown )
		{
			return CreateLongSelectMenu( trayLoc, name, caption, 0, boxWidth, maxItemsShown, new List<string>() );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="boxWidth"></param>
		/// <param name="maxItemsShown"></param>
		/// <param name="items"></param>
		/// <returns></returns>
		public SelectMenu CreateLongSelectMenu( TrayLocation trayLoc, String name, DisplayString caption, Real width,
		                                        Real boxWidth, int maxItemsShown, IList<String> items )
		{
			var sm = new SelectMenu( name, caption, width, boxWidth, maxItemsShown );
			MoveWidgetToTray( sm, trayLoc );
			sm.AssignedTrayListener = this.listener;
			if ( !( items.Count == 0 ) )
			{
				sm.Items = items;
			}
			return sm;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <returns></returns>
		public Label CreateLabel( TrayLocation trayLoc, String name, DisplayString caption )
		{
			return CreateLabel( trayLoc, name, caption, 0 );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public Label CreateLabel( TrayLocation trayLoc, String name, DisplayString caption, Real width )
		{
			var l = new Label( name, caption, width );
			MoveWidgetToTray( l, trayLoc );
			l.AssignedTrayListener = this.listener;
			return l;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public Separator CreateSeparator( TrayLocation trayLoc, String name )
		{
			return CreateSeparator( trayLoc, name, 0 );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public Separator CreateSeparator( TrayLocation trayLoc, String name, Real width )
		{
			var s = new Separator( name, width );
			MoveWidgetToTray( s, trayLoc );
			return s;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="valueBoxWidth"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		/// <param name="snaps"></param>
		/// <returns></returns>
		public Slider CreateThickSlider( TrayLocation trayLoc, String name, DisplayString caption, Real width,
		                                 Real valueBoxWidth, Real minValue, Real maxValue, int snaps )
		{
			var s = new Slider( name, caption, width, 0, valueBoxWidth, minValue, maxValue, snaps );
			MoveWidgetToTray( s, trayLoc );
			s.AssignedTrayListener = this.listener;
			return s;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="trackWidth"></param>
		/// <param name="valueBoxWidth"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		/// <param name="snaps"></param>
		/// <returns></returns>
		public Slider CreateLongSlider( TrayLocation trayLoc, String name, DisplayString caption, Real width, Real trackWidth,
		                                Real valueBoxWidth, Real minValue, Real maxValue, int snaps )
		{
			if ( trackWidth <= 0 )
			{
				trackWidth = 1;
			}
			var s = new Slider( name, caption, width, trackWidth, valueBoxWidth, minValue, maxValue, snaps );
			MoveWidgetToTray( s, trayLoc );
			s.AssignedTrayListener = this.listener;
			return s;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="trackWidth"></param>
		/// <param name="valueBoxWidth"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		/// <param name="snaps"></param>
		/// <returns></returns>
		public Slider CreateLongSlider( TrayLocation trayLoc, String name, DisplayString caption, Real trackWidth,
		                                Real valueBoxWidth, Real minValue, Real maxValue, int snaps )
		{
			return CreateLongSlider( trayLoc, name, caption, 0, trackWidth, valueBoxWidth, minValue, maxValue, snaps );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="lines"></param>
		/// <returns></returns>
		public ParamsPanel CreateParamsPanel( TrayLocation trayLoc, String name, Real width, int lines )
		{
			var pp = new ParamsPanel( name, width, lines );
			MoveWidgetToTray( pp, trayLoc );
			return pp;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="paramNames"></param>
		/// <returns></returns>
		public ParamsPanel CreateParamsPanel( TrayLocation trayLoc, String name, Real width, IList<String> paramNames )
		{
			var pp = new ParamsPanel( name, width, paramNames.Count );
			pp.ParamNames = paramNames;
			MoveWidgetToTray( pp, trayLoc );
			return pp;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <returns></returns>
		public CheckBox CreateCheckBox( TrayLocation trayLoc, String name, DisplayString caption )
		{
			return CreateCheckBox( trayLoc, name, caption, 0 );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public CheckBox CreateCheckBox( TrayLocation trayLoc, String name, DisplayString caption, Real width )
		{
			var cb = new CheckBox( name, caption, width );
			MoveWidgetToTray( cb, trayLoc );
			cb.AssignedTrayListener = this.listener;
			return cb;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="typeName"></param>
		/// <param name="templateName"></param>
		/// <returns></returns>
		public DecorWidget CreateDecorWidget( TrayLocation trayLoc, String name, String typeName, String templateName )
		{
			var dw = new DecorWidget( name, typeName, templateName );
			MoveWidgetToTray( dw, trayLoc );
			return dw;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="typeName"></param>
		/// <param name="templateName"></param>
		/// <returns></returns>
		public DecorWidget CreateLogoWidget( TrayLocation trayLoc, String name, String typeName, String templateName )
		{
			DecorWidget dw = new LogoWidget( name, typeName, templateName );
			MoveWidgetToTray( dw, trayLoc );
			return dw;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="commentBoxWidth"></param>
		/// <returns></returns>
		public ProgressBar CreateProgressBar( TrayLocation trayLoc, String name, DisplayString caption, Real width,
		                                      Real commentBoxWidth )
		{
			var pb = new ProgressBar( name, caption, width, commentBoxWidth );
			MoveWidgetToTray( pb, trayLoc );
			return pb;
		}

		/// <summary>
		/// Shows frame statistics widget set in the specified location.
		/// </summary>
		/// <param name="trayLoc"></param>
		public void ShowFrameStats( TrayLocation trayLoc )
		{
			ShowFrameStats( trayLoc, -1 );
		}

		/// <summary>
		/// Shows frame statistics widget set in the specified location.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="place"></param>
		public void ShowFrameStats( TrayLocation trayLoc, int place )
		{
			if ( !IsFrameStatsVisible )
			{
				var stats = new List<string>();
				stats.Add( "Average FPS" );
				stats.Add( "Best FPS" );
				stats.Add( "Worst FPS" );
				stats.Add( "Triangles" );
				stats.Add( "Batches" );

				this.mFpsLabel = CreateLabel( TrayLocation.None, this.mName + "/FpsLabel", "FPS:", 180 );
				this.mFpsLabel.AssignedTrayListener = this;
				this.StatsPanel = CreateParamsPanel( TrayLocation.None, this.mName + "/StatsPanel", 180, stats );
			}

			MoveWidgetToTray( this.mFpsLabel, trayLoc, place );
			MoveWidgetToTray( this.StatsPanel, trayLoc, LocateWidgetInTray( this.mFpsLabel ) + 1 );
		}

		/// <summary>
		/// Hides frame statistics widget set.
		/// </summary>
		public void HideFrameStats()
		{
			if ( IsFrameStatsVisible )
			{
				DestroyWidget( this.mFpsLabel );
				DestroyWidget( this.StatsPanel );
				this.mFpsLabel = null;
				this.StatsPanel = null;
			}
		}

		/// <summary>
		/// Toggles visibility of advanced statistics.
		/// </summary>
		public void ToggleAdvancedFrameStats()
		{
			if ( this.mFpsLabel != null )
			{
				LabelHit( this.mFpsLabel );
			}
		}

		/// <summary>
		/// Shows logo in the specified location.
		/// </summary>
		/// <param name="trayLoc"></param>
		public void ShowLogo( TrayLocation trayLoc )
		{
			ShowLogo( trayLoc, -1 );
		}

		/// <summary>
		/// Shows logo in the specified location.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="place"></param>
		public void ShowLogo( TrayLocation trayLoc, int place )
		{
			if ( !IsLogoVisible )
			{
				this.Logo = CreateDecorWidget( trayLoc, this.mName + "/Logo", "Panel", "SdkTrays/Logo" );
			}
			MoveWidgetToTray( this.Logo, trayLoc, place );
		}

		/// <summary>
		/// 
		/// </summary>
		public void HideLogo()
		{
			if ( IsLogoVisible )
			{
				DestroyWidget( this.Logo );
				this.Logo = null;
			}
		}

		///<summary>
		/// Shows loading bar. Also takes job settings: the number of resource groups
		/// to be initialised, the number of resource groups to be loaded, and the
		/// proportion of the job that will be taken up by initialization. Usually,
		/// script parsing takes up most time, so the default value is 0.7.
		/// </summary>
		public void ShowLoadingBar()
		{
			ShowLoadingBar( 1, 1, 0.7f );
		}

		///<summary>
		/// Shows loading bar. Also takes job settings: the number of resource groups
		/// to be initialised, the number of resource groups to be loaded, and the
		/// proportion of the job that will be taken up by initialization. Usually,
		/// script parsing takes up most time, so the default value is 0.7.
		/// </summary>
		/// <param name="numGroupsInit"></param>
		/// <param name="numGroupsLoad"></param>
		public void ShowLoadingBar( int numGroupsInit, int numGroupsLoad )
		{
			ShowLoadingBar( numGroupsInit, numGroupsLoad, 0.7f );
		}

		///<summary>
		/// Shows loading bar. Also takes job settings: the number of resource groups
		/// to be initialised, the number of resource groups to be loaded, and the
		/// proportion of the job that will be taken up by initialization. Usually,
		/// script parsing takes up most time, so the default value is 0.7.
		/// </summary>
		/// <param name="numGroupsInit"></param>
		/// <param name="numGroupsLoad"></param>
		/// <param name="initProportion"></param>
		public void ShowLoadingBar( int numGroupsInit, int numGroupsLoad, Real initProportion )
		{
			if ( this.LoadBar != null )
			{
				HideLoadingBar();
				return;
			}
			this.LoadBar = new ProgressBar( this.mName + "/LoadingBar", "Loading...", 400, 308 );
			OverlayElement e = this.LoadBar.OverlayElement;
			this.mDialogShade.AddChild( e );
			e.VerticalAlignment = VerticalAlignment.Center;
			e.Left = ( -( e.Width/2 ) );
			e.Top = ( -( e.Height/2 ) );
			ResourceGroupManager.Instance.AddResourceGroupListener( this );
			this.CursorWasVisible = IsCursorVisible;
			HideCursor();
			this.mDialogShade.Show();
			// calculate the proportion of job required to init/load one group
			if ( numGroupsInit == 0 && numGroupsLoad != 0 )
			{
				this.groupInitProportion = 0;
				this.groupLoadProportion = 1;
			}
			else if ( numGroupsLoad == 0 && numGroupsInit != 0 )
			{
				this.groupLoadProportion = 0;
				if ( numGroupsInit != 0 )
				{
					this.groupInitProportion = 1;
				}
			}
			else if ( numGroupsInit == 0 && numGroupsLoad == 0 )
			{
				this.groupInitProportion = 0;
				this.groupLoadProportion = 0;
			}
			else
			{
				this.groupInitProportion = initProportion/numGroupsInit;
				this.groupLoadProportion = ( 1 - initProportion )/numGroupsLoad;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void HideLoadingBar()
		{
			if ( this.LoadBar != null )
			{
				this.LoadBar.Cleanup();
				this.LoadBar = null;
				ResourceGroupManager.Instance.RemoveResourceGroupListener( this );
				if ( this.CursorWasVisible )
				{
					ShowCursor();
				}
				this.mDialogShade.Hide();
			}
		}

		/// <summary>
		/// Pops up a message dialog with an OK button.
		/// </summary>
		/// <param name="caption"></param>
		/// <param name="message"></param>
		public void ShowOkDialog( DisplayString caption, DisplayString message )
		{
			OverlayElement e;
			if ( this.Dialog != null )
			{
				this.Dialog.Caption = caption;
				this.Dialog.Text = message;
				if ( this.mOk != null )
				{
					return;
				}
				else
				{
					if ( this.mYes != null )
					{
						this.mYes.Cleanup();
					}
					if ( this.mNo != null )
					{
						this.mNo.Cleanup();
					}

					this.mYes = null;
					this.mNo = null;
				}
			}
			else
			{
				// give widgets a chance to reset in case they're in the middle of something
				for ( int i = 0; i < 10; i++ )
				{
					for ( int j = 0; j < this.mWidgets[ i ].Count; j++ )
					{
						this.mWidgets[ i ][ j ].OnLostFocus();
					}
				}
				this.mDialogShade.Show();
				this.Dialog = new TextBox( this.mName + "/DialogBox", caption, 300, 208 );
				this.Dialog.Text = message;
				e = this.Dialog.OverlayElement;
				this.mDialogShade.AddChild( e );
				e.VerticalAlignment = VerticalAlignment.Center;
				e.Left = -( e.Width/2 );
				e.Top = -( e.Height/2 );
				this.CursorWasVisible = IsCursorVisible;
				ShowCursor();
			}
			this.mOk = new Button( this.mName + "/OkButton", "OK", 60 );
			this.mOk.AssignedTrayListener = this;
			e = this.mOk.OverlayElement;
			this.mDialogShade.AddChild( e );
			e.VerticalAlignment = VerticalAlignment.Center;
			e.Left = -( e.Width/2 );
			e.Top = this.Dialog.OverlayElement.Top + this.Dialog.OverlayElement.Height + 5;
		}

		/// <summary>
		/// Pops up a question dialog with Yes and No buttons.
		/// </summary>
		/// <param name="caption"></param>
		/// <param name="question"></param>
		public void ShowYesNoDialog( DisplayString caption, DisplayString question )
		{
			OverlayElement e;
			if ( this.Dialog != null )
			{
				this.Dialog.Caption = caption;
				this.Dialog.Text = question;
				if ( this.mOk != null )
				{
					if ( this.mOk != null )
					{
						this.mOk.Cleanup();
					}

					this.mOk = null;
				}
				else
				{
					return;
				}
			}
			else
			{
				// give widgets a chance to reset in case they're in the middle of something
				for ( int i = 0; i < 10; i++ )
				{
					for ( int j = 0; j < this.mWidgets[ i ].Count; j++ )
					{
						this.mWidgets[ i ][ j ].OnLostFocus();
					}
				}
				this.mDialogShade.Show();
				this.Dialog = new TextBox( this.mName + "/DialogBox", caption, 300, 208 );
				this.Dialog.Text = question;
				e = this.Dialog.OverlayElement;
				this.mDialogShade.AddChild( e );
				e.VerticalAlignment = VerticalAlignment.Center;
				e.Left = -( e.Width/2 );
				e.Top = -( e.Height/2 );
				this.CursorWasVisible = IsCursorVisible;
				ShowCursor();
			}
			this.mYes = new Button( this.mName + "/YesButton", "Yes", 58 );
			this.mYes.AssignedTrayListener = this;
			e = this.mYes.OverlayElement;
			this.mDialogShade.AddChild( e );
			e.VerticalAlignment = VerticalAlignment.Center;
			e.Left = -( e.Width + 2 );
			e.Top = this.Dialog.OverlayElement.Top + this.Dialog.OverlayElement.Height + 5;
			this.mNo = new Button( this.mName + "/NoButton", "No", 50 );
			this.mNo.AssignedTrayListener = this;
			e = this.mNo.OverlayElement;
			this.mDialogShade.AddChild( e );
			e.VerticalAlignment = VerticalAlignment.Center;
			e.Left = 3;
			e.Top = this.Dialog.OverlayElement.Top + this.Dialog.OverlayElement.Height + 5;
		}

		/// <summary>
		/// Hides whatever dialog is currently showing.
		/// </summary>
		public void CloseDialog()
		{
			if ( this.Dialog != null )
			{
				if ( this.mOk != null )
				{
					this.mOk.Cleanup();
					this.mOk = null;
				}
				else
				{
					if ( this.mYes != null )
					{
						this.mYes.Cleanup();
					}
					if ( this.mNo != null )
					{
						this.mNo.Cleanup();
					}

					this.mYes = null;
					this.mNo = null;
				}
				this.mDialogShade.Hide();
				this.Dialog.Cleanup();
				this.Dialog = null;
				if ( !this.CursorWasVisible )
				{
					HideCursor();
				}
			}
		}

		/// <summary>
		/// Gets a widget from a tray by place.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="place"></param>
		/// <returns></returns>
		public Widget GetWidget( TrayLocation trayLoc, int place )
		{
			if ( place >= 0 && place < this.mWidgets[ (int)trayLoc ].Count )
			{
				return this.mWidgets[ (int)trayLoc ][ place ];
			}
			return null;
		}

		/// <summary>
		///  Gets a widget from a tray by name.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public Widget GetWidget( TrayLocation trayLoc, String name )
		{
			for ( int i = 0; i < this.mWidgets[ (int)trayLoc ].Count; i++ )
			{
				if ( this.mWidgets[ (int)trayLoc ][ i ].Name == name )
				{
					return this.mWidgets[ (int)trayLoc ][ i ];
				}
			}
			return null;
		}

		/// <summary>
		/// Gets a widget by name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Widget GetWidget( String name )
		{
			for ( int i = 0; i < 10; i++ )
			{
				for ( int j = 0; j < this.mWidgets[ i ].Count; j++ )
				{
					if ( this.mWidgets[ i ][ j ].Name == name )
					{
						return this.mWidgets[ i ][ j ];
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the number of widgets in a tray.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <returns></returns>
		public int GetWidgetCount( TrayLocation trayLoc )
		{
			return this.mWidgets[ (int)trayLoc ].Count;
		}

		/// <summary>
		/// Gets all the widgets of a specific tray.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <returns></returns>
		public IEnumerator<Widget> GetWidgetEnumerator( TrayLocation trayLoc )
		{
			return this.mWidgets[ (int)trayLoc ].GetEnumerator();
		}

		/// <summary>
		/// Gets a widget's position in its tray.
		/// </summary>
		/// <param name="widget"></param>
		/// <returns></returns>
		public int LocateWidgetInTray( Widget widget )
		{
			for ( int i = 0; i < this.mWidgets[ (int)widget.TrayLocation ].Count; i++ )
			{
				if ( this.mWidgets[ (int)widget.TrayLocation ][ i ] == widget )
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Destroys a widget.
		/// </summary>
		/// <param name="widget"></param>
		public void DestroyWidget( Widget widget )
		{
			if ( widget == null )
			{
				new AxiomException( "Widget does not exist,TrayManager.DestroyWidget" );
			}

			// in case special widgets are destroyed manually, set them to 0
			if ( widget == this.Logo )
			{
				this.Logo = null;
			}
			else if ( widget == this.StatsPanel )
			{
				this.StatsPanel = null;
			}
			else if ( widget == this.mFpsLabel )
			{
				this.mFpsLabel = null;
			}

			this.mTrays[ (int)widget.TrayLocation ].RemoveChild( widget.Name );

			WidgetList wList = this.mWidgets[ (int)widget.TrayLocation ];
			wList.Remove( widget );
			if ( widget == ExpandedMenu )
			{
				ExpandedMenu = null;
			}

			widget.Cleanup();

			this.mWidgetDeathRow.Add( widget );

			AdjustTrays();
		}

		/// <summary>
		/// Destroys a widget.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="place"></param>
		public void DestroyWidget( TrayLocation trayLoc, int place )
		{
			DestroyWidget( GetWidget( trayLoc, place ) );
		}

		/// <summary>
		/// Destroys a widget.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		public void DestroyWidget( TrayLocation trayLoc, String name )
		{
			DestroyWidget( GetWidget( trayLoc, name ) );
		}

		/// <summary>
		/// Destroys a widget.
		/// </summary>
		/// <param name="name"></param>
		public void DestroyWidget( String name )
		{
			DestroyWidget( GetWidget( name ) );
		}

		/// <summary>
		/// Destroys all widgets in a tray.
		/// </summary>
		/// <param name="trayLoc"></param>
		public void DestroyAllWidgetsInTray( TrayLocation trayLoc )
		{
			while ( !( this.mWidgets[ (int)trayLoc ].Count == 0 ) )
			{
				DestroyWidget( this.mWidgets[ (int)trayLoc ][ 0 ] );
			}
		}

		/// <summary>
		/// Destroys all widgets.
		/// </summary>
		public void DestroyAllWidgets()
		{
			for ( int i = 0; i < 10; i++ ) // destroy every widget in every tray (including null tray)
			{
				DestroyAllWidgetsInTray( (TrayLocation)i );
			}
		}

		/// <summary>
		/// Adds a widget to a specified tray.
		/// </summary>
		/// <param name="widget"></param>
		/// <param name="trayLoc"></param>
		public void MoveWidgetToTray( Widget widget, TrayLocation trayLoc )
		{
			MoveWidgetToTray( widget, trayLoc, -1 );
		}

		/// <summary>
		/// Adds a widget to a specified tray.
		/// </summary>
		/// <param name="widget"></param>
		/// <param name="trayLoc"></param>
		/// <param name="place"></param>
		public void MoveWidgetToTray( Widget widget, TrayLocation trayLoc, int place )
		{
			if ( widget == null )
			{
				throw new ArgumentNullException( "widget", "Widget deos not exist." );
			}

			// remove widget from old tray
			WidgetList wList = this.mWidgets[ (int)widget.TrayLocation ];
			if ( wList == null )
			{
				wList = new WidgetList();
				this.mWidgets[ (int)widget.TrayLocation ] = wList;
			}

			if ( wList.Contains( widget ) )
			{
				wList.Remove( widget );
				this.mTrays[ (int)widget.TrayLocation ].RemoveChild( widget.Name );
			}

			// insert widget into new tray at given position, or at the end if unspecified or invalid
			if ( place == -1 || place > this.mWidgets[ (int)trayLoc ].Count )
			{
				place = this.mWidgets[ (int)trayLoc ].Count;
			}
			this.mWidgets[ (int)trayLoc ].Insert( place, widget );
			// mWidgets[ (int)trayLoc ].Add( widget );
			this.mTrays[ (int)trayLoc ].AddChild( widget.OverlayElement );

			widget.OverlayElement.HorizontalAlignment = this.trayWidgetAlign[ (int)trayLoc ];

			// adjust trays if necessary
			if ( widget.TrayLocation != TrayLocation.None || trayLoc != TrayLocation.None )
			{
				AdjustTrays();
			}

			widget.AssigendTray = trayLoc;
			//widget.Show();
			//mTraysLayer.AddElement( (OverlayElementContainer)widget.OverlayElement);
		}

		/// <summary>
		/// Adds a widget to a specified tray.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="trayLoc"></param>
		public void MoveWidgetToTray( String name, TrayLocation trayLoc )
		{
			MoveWidgetToTray( GetWidget( name ), trayLoc, -1 );
		}

		/// <summary>
		/// Adds a widget to a specified tray.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="trayLoc"></param>
		/// <param name="place"></param>
		public void MoveWidgetToTray( String name, TrayLocation trayLoc, int place )
		{
			MoveWidgetToTray( GetWidget( name ), trayLoc, place );
		}

		/// <summary>
		/// Adds a widget to a specified tray.
		/// </summary>
		/// <param name="currentTrayLoc"></param>
		/// <param name="name"></param>
		/// <param name="targetTrayLoc"></param>
		public void MoveWidgetToTray( TrayLocation currentTrayLoc, String name, TrayLocation targetTrayLoc )
		{
			MoveWidgetToTray( GetWidget( currentTrayLoc, name ), targetTrayLoc, -1 );
		}

		/// <summary>
		/// Adds a widget to a specified tray.
		/// </summary>
		/// <param name="currentTrayLoc"></param>
		/// <param name="name"></param>
		/// <param name="targetTrayLoc"></param>
		/// <param name="place"></param>
		public void MoveWidgetToTray( TrayLocation currentTrayLoc, String name, TrayLocation targetTrayLoc, int place )
		{
			MoveWidgetToTray( GetWidget( currentTrayLoc, name ), targetTrayLoc, place );
		}

		/// <summary>
		/// Adds a widget to a specified tray.
		/// </summary>
		/// <param name="currentTrayLoc"></param>
		/// <param name="currentPlace"></param>
		/// <param name="targetTrayLoc"></param>
		public void MoveWidgetToTray( TrayLocation currentTrayLoc, int currentPlace, TrayLocation targetTrayLoc )
		{
			MoveWidgetToTray( GetWidget( currentTrayLoc, currentPlace ), targetTrayLoc, -1 );
		}

		/// <summary>
		/// Adds a widget to a specified tray.
		/// </summary>
		/// <param name="currentTrayLoc"></param>
		/// <param name="currentPlace"></param>
		/// <param name="targetTrayLoc"></param>
		/// <param name="targetPlace"></param>
		public void MoveWidgetToTray( TrayLocation currentTrayLoc, int currentPlace, TrayLocation targetTrayLoc,
		                              int targetPlace )
		{
			MoveWidgetToTray( GetWidget( currentTrayLoc, currentPlace ), targetTrayLoc, targetPlace );
		}

		/// <summary>
		/// Removes a widget from its tray. Same as moving it to the null tray.
		/// </summary>
		/// <param name="widget"></param>
		public void RemoveWidgetFromTray( Widget widget )
		{
			MoveWidgetToTray( widget, TrayLocation.None );
		}

		/// <summary>
		/// Removes a widget from its tray. Same as moving it to the null tray.
		/// </summary>
		/// <param name="name"></param>
		public void RemoveWidgetFromTray( String name )
		{
			RemoveWidgetFromTray( GetWidget( name ) );
		}

		/// <summary>
		/// Removes a widget from its tray. Same as moving it to the null tray.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="name"></param>
		public void RemoveWidgetFromTray( TrayLocation trayLoc, String name )
		{
			RemoveWidgetFromTray( GetWidget( trayLoc, name ) );
		}

		/// <summary>
		/// Removes a widget from its tray. Same as moving it to the null tray.
		/// </summary>
		/// <param name="trayLoc"></param>
		/// <param name="place"></param>
		public void RemoveWidgetFromTray( TrayLocation trayLoc, int place )
		{
			RemoveWidgetFromTray( GetWidget( trayLoc, place ) );
		}

		/// <summary>
		/// Removes all widgets from a widget tray.
		/// </summary>
		/// <param name="trayLoc"></param>
		public void ClearTray( TrayLocation trayLoc )
		{
			if ( trayLoc == TrayLocation.None )
			{
				return; // can't clear the null tray
			}

			while ( !( this.mWidgets[ (int)trayLoc ].Count == 0 ) ) // remove every widget from given tray
			{
				RemoveWidgetFromTray( this.mWidgets[ (int)trayLoc ][ 0 ] );
			}
		}

		/// <summary>
		/// Removes all widgets from all widget trays.
		/// </summary>
		public void ClearAllTrays()
		{
			for ( int i = 0; i < 9; i++ )
			{
				ClearTray( (TrayLocation)i );
			}
		}

		/// <summary>
		/// Process frame events. Updates frame statistics widget set and deletes
		/// all widgets queued for destruction.
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public bool FrameRenderingQueued( FrameEventArgs evt )
		{
			for ( int i = 0; i < this.mWidgetDeathRow.Count; i++ )
			{
				this.mWidgetDeathRow[ i ] = null;
			}
			this.mWidgetDeathRow.Clear();

			RenderTarget.FrameStatistics stats = this.mWindow.Statistics;

			if ( IsFrameStatsVisible )
			{
				String s;

				s = String.Format( "{0:#.##}", stats.LastFPS );
				this.mFpsLabel.Caption = s;

				if ( this.StatsPanel.OverlayElement.IsVisible )
				{
					var values = new List<string>();

					s = String.Format( "{0:#.##}", stats.AverageFPS );
					values.Add( s );

					s = String.Format( "{0:#.##}", stats.BestFPS );
					values.Add( s );

					s = String.Format( "{0:#.##}", stats.WorstFPS );
					values.Add( s );

					s = String.Format( "{0:#.##}", stats.TriangleCount );
					values.Add( s );

					s = String.Format( "{0:#.##}", stats.BatchCount );
					values.Add( s );

					this.StatsPanel.ParamValues = values;
				}
			}

			return true;
		}

		#region Implementation of IResourceGroupListener

		/// <summary>
		/// This event is fired when a resource group begins parsing scripts.
		/// </summary>
		/// <param name="groupName">The name of the group</param>
		/// <param name="scriptCount">The number of scripts which will be parsed</param>
		public void ResourceGroupScriptingStarted( string groupName, int scriptCount )
		{
			this.loadInc = this.groupInitProportion/scriptCount;
			this.LoadBar.Caption = "Parsing...";
			this.mWindow.Update();
			// allow OS events to process (if the platform requires it
			if ( WindowEventMonitor.Instance.MessagePump != null )
			{
				WindowEventMonitor.Instance.MessagePump();
			}
		}

		/// <summary>
		/// This event is fired when a script is about to be parsed.
		/// </summary>
		/// <param name="scriptName">Name of the to be parsed</param>
		public void ScriptParseStarted( string scriptName, ref bool skipThisScript )
		{
			this.LoadBar.Comment = System.IO.Path.GetFileName( scriptName );
			this.mWindow.Update();
			// allow OS events to process (if the platform requires it
			if ( WindowEventMonitor.Instance.MessagePump != null )
			{
				WindowEventMonitor.Instance.MessagePump();
			}
		}

		/// <summary>
		/// This event is fired when the script has been fully parsed.
		/// </summary>
		public void ScriptParseEnded( string scriptName, bool skipped )
		{
			this.LoadBar.Progress = this.LoadBar.Progress + this.loadInc;
			this.mWindow.Update();
			// allow OS events to process (if the platform requires it
			if ( WindowEventMonitor.Instance.MessagePump != null )
			{
				WindowEventMonitor.Instance.MessagePump();
			}
		}

		/// <summary>
		/// This event is fired when a resource group finished parsing scripts.
		/// </summary>
		/// <param name="groupName">The name of the group</param>
		public void ResourceGroupScriptingEnded( string groupName )
		{
		}

		/// <summary>
		/// This event is fired  when a resource group begins loading.
		/// </summary>
		/// <param name="groupName">The name of the group being loaded</param>
		/// <param name="resourceCount">
		/// The number of resources which will be loaded, 
		/// including a number of stages required to load any linked world geometry
		/// </param>
		public void ResourceGroupLoadStarted( string groupName, int resourceCount )
		{
			this.loadInc = this.groupLoadProportion/resourceCount;
			this.LoadBar.Caption = "Loading...";
			this.mWindow.Update();
			// allow OS events to process (if the platform requires it
			if ( WindowEventMonitor.Instance.MessagePump != null )
			{
				WindowEventMonitor.Instance.MessagePump();
			}
		}

		/// <summary>
		/// This event is fired when a declared resource is about to be loaded. 
		/// </summary>
		/// <param name="resource">Weak reference to the resource loaded</param>
		public void ResourceLoadStarted( Resource resource )
		{
			this.LoadBar.Comment = resource.Name;
			this.mWindow.Update();
			// allow OS events to process (if the platform requires it
			if ( WindowEventMonitor.Instance.MessagePump != null )
			{
				WindowEventMonitor.Instance.MessagePump();
			}
		}

		/// <summary>
		/// This event is fired when the resource has been loaded. 
		/// </summary>
		public void ResourceLoadEnded()
		{
			this.LoadBar.Progress = this.LoadBar.Progress + this.loadInc;
			this.mWindow.Update();
			// allow OS events to process (if the platform requires it
			if ( WindowEventMonitor.Instance.MessagePump != null )
			{
				WindowEventMonitor.Instance.MessagePump();
			}
		}

		/// <summary>
		/// This event is fired when a stage of loading linked world geometry 
		/// is about to start. The number of stages required will have been 
		/// included in the resourceCount passed in resourceGroupLoadStarted.
		/// </summary>
		/// <param name="description">Text description of what was just loaded</param>
		public void WorldGeometryStageStarted( string description )
		{
			this.LoadBar.Comment = description;
			this.mWindow.Update();
			// allow OS events to process (if the platform requires it
			if ( WindowEventMonitor.Instance.MessagePump != null )
			{
				WindowEventMonitor.Instance.MessagePump();
			}
		}

		/// <summary>
		/// This event is fired when a stage of loading linked world geometry 
		/// has been completed. The number of stages required will have been 
		/// included in the resourceCount passed in resourceGroupLoadStarted.
		/// </summary>
		/// <param name="description">Text description of what was just loaded</param>
		public void WorldGeometryStageEnded()
		{
			this.LoadBar.Progress = this.LoadBar.Progress + this.loadInc;
			this.mWindow.Update();
			// allow OS events to process (if the platform requires it
			if ( WindowEventMonitor.Instance.MessagePump != null )
			{
				WindowEventMonitor.Instance.MessagePump();
			}
		}

		/// <summary>
		/// This event is fired when a resource group finished loading.
		/// </summary>
		public void ResourceGroupLoadEnded( string groupName )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="groupName"></param>
		public void ResourceGroupPrepareEnded( string groupName )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		public void ResourcePrepareEnded()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="resource"></param>
		public void ResourcePrepareStarted( Resource resource )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="groupName"></param>
		/// <param name="resourceCount"></param>
		public void ResourceGroupPrepareStarted( string groupName, int resourceCount )
		{
		}

		#endregion

		/// <summary>
		/// Toggles visibility of advanced statistics.
		/// </summary>
		/// <param name="label"></param>
		public void LabelHit( Label label )
		{
			if ( this.StatsPanel.OverlayElement.IsVisible )
			{
				this.StatsPanel.OverlayElement.Hide();
				this.mFpsLabel.OverlayElement.Width = 150;
				RemoveWidgetFromTray( this.StatsPanel );
			}
			else
			{
				this.StatsPanel.OverlayElement.Show();
				this.mFpsLabel.OverlayElement.Width = 180;
				MoveWidgetToTray( this.StatsPanel, this.mFpsLabel.TrayLocation, LocateWidgetInTray( this.mFpsLabel ) + 1 );
			}
		}

		/// <summary>
		/// Destroys dialog widgets, notifies listener, and ends high priority session.
		/// </summary>
		/// <param name="button"></param>
		public void OnButtonHit( object sender, Button button )
		{
			if ( this.listener != null )
			{
				if ( button == this.mOk )
				{
					this.listener.OkDialogClosed( this.Dialog.Text );
				}
				else
				{
					this.listener.YesNoDialogClosed( this.Dialog.Text, button == this.mYes );
				}
			}
			CloseDialog();

			if ( ButtonHit != null )
			{
				ButtonHit( sender, button );
			}
		}

		/// <summary>
		/// Processes mouse button down events. Returns true if the event was<para></para>
		/// consumed and should not be passed on to other handlers.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool InjectMouseDown( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			// only process left button when stuff is visible
			if ( !this.cursorLayer.IsVisible || id != SIS.MouseButtonID.Left )
			{
				return false;
			}

			var cursorPos = new Vector2( this.cursor.Left, this.cursor.Top );

			this.mTrayDrag = false;

			if ( ExpandedMenu != null ) // only check top priority widget until it passes on
			{
				ExpandedMenu.OnCursorPressed( cursorPos );
				if ( !ExpandedMenu.IsExpanded )
				{
					ExpandedMenu = null;
				}
				return true;
			}

			if ( this.Dialog != null ) // only check top priority widget until it passes on
			{
				this.Dialog.OnCursorPressed( cursorPos );
				if ( this.mOk != null )
				{
					this.mOk.OnCursorPressed( cursorPos );
				}
				else
				{
					this.mYes.OnCursorPressed( cursorPos );
					this.mNo.OnCursorPressed( cursorPos );
				}
				return true;
			}

			for ( int i = 0; i < 9; i++ ) // check if mouse is over a non-null tray
			{
				if ( this.mTrays[ i ].IsVisible && Widget.IsCursorOver( this.mTrays[ i ], cursorPos, 2 ) )
				{
					this.mTrayDrag = true; // initiate a drag that originates in a tray
					break;
				}
			}

			for ( int i = 0; i < this.mWidgets[ 9 ].Count; i++ ) // check if mouse is over a non-null tray's widgets
			{
				if ( this.mWidgets[ 9 ][ i ].OverlayElement.IsVisible &&
				     Widget.IsCursorOver( this.mWidgets[ 9 ][ i ].OverlayElement, cursorPos ) )
				{
					this.mTrayDrag = true; // initiate a drag that originates in a tray
					break;
				}
			}

			if ( !this.mTrayDrag )
			{
				return false; // don't process if mouse press is not in tray
			}

			Widget w;

			for ( int i = 0; i < 10; i++ )
			{
				if ( !this.mTrays[ i ].IsVisible )
				{
					continue;
				}

				for ( int j = 0; j < this.mWidgets[ i ].Count; j++ )
				{
					w = this.mWidgets[ i ][ j ];
					if ( !w.OverlayElement.IsVisible )
					{
						continue;
					}
					w.OnCursorPressed( cursorPos ); // send event to widget

					var m = w as SelectMenu;
					if ( m != null && m.IsExpanded ) // a menu has begun a top priority session
					{
						ExpandedMenu = m;
						return true;
					}
				}
			}

			return true; // a tray click is not to be handled by another party
		}

		/// <summary>
		/// Processes mouse button up events. Returns true if the event was <para></para>
		/// consumed and should not be passed on to other handlers.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool InjectMouseUp( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			// only process left button when stuff is visible
			if ( !this.cursorLayer.IsVisible || id != SIS.MouseButtonID.Left )
			{
				return false;
			}

			var cursorPos = new Vector2( this.cursor.Left, this.cursor.Top );

			if ( ExpandedMenu != null ) // only check top priority widget until it passes on
			{
				ExpandedMenu.OnCursorReleased( cursorPos );
				return true;
			}

			if ( this.Dialog != null ) // only check top priority widget until it passes on
			{
				this.Dialog.OnCursorReleased( cursorPos );
				if ( this.mOk != null )
				{
					this.mOk.OnCursorReleased( cursorPos );
				}
				else
				{
					this.mYes.OnCursorReleased( cursorPos );
					// very important to check if second button still exists, because first button could've closed the popup
					if ( this.mNo != null )
					{
						this.mNo.OnCursorReleased( cursorPos );
					}
				}
				return true;
			}

			if ( !this.mTrayDrag )
			{
				return false; // this click did not originate in a tray, so don't process
			}

			Widget w;

			for ( int i = 0; i < 10; i++ )
			{
				if ( !this.mTrays[ i ].IsVisible )
				{
					continue;
				}

				for ( int j = 0; j < this.mWidgets[ i ].Count; j++ )
				{
					w = this.mWidgets[ i ][ j ];
					if ( !w.OverlayElement.IsVisible )
					{
						continue;
					}
					w.OnCursorReleased( cursorPos ); // send event to widget
				}
			}

			this.mTrayDrag = false; // stop this drag
			return true; // this click did originate in this tray, so don't pass it on
		}

		/// <summary>
		/// Updates cursor position. Returns true if the event was <para></para>
		/// consumed and should not be passed on to other handlers.
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public bool InjectMouseMove( SIS.MouseEventArgs evt )
		{
			if ( !this.cursorLayer.IsVisible )
			{
				return false; // don't process if cursor layer is invisible
			}

			this.cursor.SetPosition( evt.State.X.Absolute, evt.State.Y.Absolute );

			var cursorPos = new Vector2( this.cursor.Left, this.cursor.Top );

			if ( ExpandedMenu != null ) // only check top priority widget until it passes on
			{
				ExpandedMenu.OnCursorMoved( cursorPos );
				return true;
			}

			if ( this.Dialog != null ) // only check top priority widget until it passes on
			{
				this.Dialog.OnCursorMoved( cursorPos );
				if ( this.mOk != null )
				{
					this.mOk.OnCursorMoved( cursorPos );
				}
				else
				{
					this.mYes.OnCursorMoved( cursorPos );
					this.mNo.OnCursorMoved( cursorPos );
				}
				return true;
			}

			Widget w;

			for ( int i = 0; i < this.mTrays.Length; i++ )
			{
				if ( !this.mTrays[ i ].IsVisible )
				{
					continue;
				}

				for ( int j = 0; j < this.mWidgets[ i ].Count; j++ )
				{
					w = this.mWidgets[ i ][ j ];
					if ( !w.OverlayElement.IsVisible )
					{
						continue;
					}
					w.OnCursorMoved( cursorPos ); // send event to widget
				}
			}

			if ( this.mTrayDrag )
			{
				return true; // don't pass this event on if we're in the middle of a drag
			}
			return false;
		}
	};
}