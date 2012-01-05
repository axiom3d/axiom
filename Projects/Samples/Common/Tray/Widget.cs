#region MIT/X11 License

//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
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

using Axiom.Fonts;
using Axiom.Math;
using Axiom.Overlays;
using Axiom.Overlays.Elements;

using WidgetList = System.Collections.Generic.List<Axiom.Samples.Widget>;

namespace Axiom.Samples
{
	/// <summary>
	/// Abstract base class for all widgets.
	/// </summary>
	public class Widget
	{
		#region fields

		/// <summary>
		/// 
		/// </summary>
		protected OverlayElement element;

		/// <summary>
		/// 
		/// </summary>
		protected TrayLocation trayLoc;

		/// <summary>
		/// 
		/// </summary>
		protected ISdkTrayListener listener;

		#endregion fields

		#region properties

		/// <summary>
		/// Gets or sets the tray location where this widget is assigned to
		/// </summary>
		public TrayLocation AssigendTray { set { trayLoc = value; } get { return trayLoc; } }

		/// <summary>
		/// Gets or sets the assigned trayListener where this widget is assigned to
		/// </summary>
		public ISdkTrayListener AssignedTrayListener { get { return listener; } set { listener = value; } }

		/// <summary>
		/// Gets the underlying overlay element
		/// </summary>
		public OverlayElement OverlayElement { get { return element; } }

		/// <summary>
		/// Gets the name of this widget.
		/// </summary>
		public string Name { get { return element.Name; } }

		/// <summary>
		/// Gets the current tray location of this widget
		/// </summary>
		public TrayLocation TrayLocation { get { return this.trayLoc; } }

		/// <summary>
		/// Gets this widget is visible or not
		/// </summary>
		public bool IsVisible { get { return this.element.IsVisible; } }

		#endregion

		#region construction

		/// <summary>
		/// 
		/// </summary>
		public Widget()
		{
			this.trayLoc = TrayLocation.None;
			this.element = null;
			this.listener = null;
		}

		#endregion construction

		#region static helper methods

		/// <summary>
		/// Static utility method to recursively delete an overlay element plus<para></para>
		/// all of its children from the system.
		/// </summary>
		/// <param name="element"></param>
		public static void NukeOverlayElement( OverlayElement element )
		{
			OverlayElementContainer container = element as OverlayElementContainer;
			if( container != null )
			{
				List<OverlayElement> toDelete = new List<OverlayElement>();
				foreach( OverlayElement child in container.Children.Values )
				{
					toDelete.Add( child );
				}

				for( int i = 0; i < toDelete.Count; i++ )
				{
					NukeOverlayElement( toDelete[ i ] );
				}
			}
			if( element != null )
			{
				OverlayElementContainer parent = element.Parent;
				if( parent != null )
				{
					parent.RemoveChild( element.Name );
				}
				OverlayManager.Instance.Elements.DestroyElement( element.Name );
			}
		}

		/// <summary>
		/// Static utility method to check if the cursor is over an overlay element.
		/// </summary>
		/// <param name="element"></param>
		/// <param name="cursorPos"></param>
		/// <returns></returns>
		public static bool IsCursorOver( OverlayElement element, Vector2 cursorPos )
		{
			return IsCursorOver( element, cursorPos, 0 );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="element"></param>
		/// <param name="cursorPos"></param>
		/// <param name="voidBorder"></param>
		/// <returns></returns>
		public static bool IsCursorOver( OverlayElement element, Vector2 cursorPos, Real voidBorder )
		{
			OverlayManager om = OverlayManager.Instance;
			int l = (int)( element.DerivedLeft * om.ViewportWidth );
			int t = (int)( element.DerivedTop * om.ViewportHeight );
			int r = l + (int)element.Width;
			int b = t + (int)element.Height;

			return ( cursorPos.x >= l + voidBorder && cursorPos.x <= r - voidBorder &&
			         cursorPos.y >= t + voidBorder && cursorPos.y <= b - voidBorder );
		}

		/// <summary>
		/// Static utility method used to get the cursor's offset from the center<para></para>
		/// of an overlay element in pixels.
		/// </summary>
		/// <param name="element"></param>
		/// <param name="cursorPos"></param>
		/// <returns></returns>
		public static Vector2 CursorOffset( OverlayElement element, Vector2 cursorPos )
		{
			OverlayManager om = OverlayManager.Instance;
			return new Vector2( cursorPos.x - ( element.DerivedLeft * om.ViewportWidth + element.Width / 2 ),
			                    cursorPos.y - ( element.DerivedTop * om.ViewportHeight + element.Height / 2 ) );
		}

		/// <summary>
		/// Static utility method used to get the width of a caption in a text area.
		/// </summary>
		/// <param name="caption"></param>
		/// <param name="area"></param>
		/// <returns></returns>
		public static Real GetCaptionWidth( String caption, TextArea area )
		{
			Font font = (Font)FontManager.Instance.GetByName( area.FontName );
			String current = caption;
			Real lineWidth = 0;

			for( int i = 0; i < current.Length; i++ )
			{
				// be sure to provide a line width in the text area
				if( current[ i ] == ' ' )
				{
					if( area.SpaceWidth != 0 )
					{
						lineWidth += area.SpaceWidth;
					}
					else
					{
						lineWidth += font.GetGlyphAspectRatio( ' ' ) * area.CharHeight;
					}
				}
				else if( current[ i ] == '\n' )
				{
					break;
				}
					// use glyph information to calculate line width
				else
				{
					lineWidth += font.GetGlyphAspectRatio( current[ i ] ) * area.CharHeight;
				}
			}

			return lineWidth;
		}

		/// <summary>
		/// Static utility method to cut off a string to fit in a text area.
		/// </summary>
		/// <param name="caption"></param>
		/// <param name="area"></param>
		/// <param name="maxWidth"></param>
		public static void FitCaptionToArea( String caption, TextArea area, Real maxWidth )
		{
			Font f = (Font)FontManager.Instance.GetByName( area.FontName );
			String s = caption;

			int nl = s.IndexOf( '\n' );
			if( nl != -1 )
			{
				s = s.Substring( 0, nl );
			}

			Real width = 0;

			for( int i = 0; i < s.Length; i++ )
			{
				if( s[ i ] == ' ' && area.SpaceWidth != 0 )
				{
					width += area.SpaceWidth;
				}
				else
				{
					width += f.GetGlyphAspectRatio( s[ i ] ) * area.CharHeight;
				}
				if( width > maxWidth )
				{
					s = s.Substring( 0, i );
					break;
				}
			}

			area.Text = s;
		}

		#endregion static helper methods

		#region methods

		/// <summary>
		/// 
		/// </summary>
		public void Cleanup()
		{
			if( this.element != null )
			{
				NukeOverlayElement( this.element );
			}
			this.element = null;
		}

		/// <summary>
		/// 
		/// </summary>
		public void Hide()
		{
			this.element.Hide();
		}

		/// <summary>
		/// 
		/// </summary>
		public void Show()
		{
			this.element.Show();
		}

		#endregion

		#region events

		/// <summary>
		/// Occurs when the cursor pointer is moved over the widget.
		/// </summary>
		public event CursorMovedHandler CursorMoved;

		/// <summary>
		/// Occurs when the cursor pointer is over the widget and a mouse button is pressed.
		/// </summary>
		public event CursorPressedHandler CursorPressed;

		/// <summary>
		/// Occurs when the cursor pointer is over the widget and a mouse button is released.
		/// </summary>
		public event CursorReleasedHandler CursorReleased;

		/// <summary>
		/// Occours when the widget loses focus.
		/// </summary>
		public event LostFocusHandler LostFocus;

		/// <summary>
		/// Raises the CursorMoved event.
		/// </summary>
		/// <param name="cursorPos">current cursor position</param>
		virtual public void OnCursorMoved( Vector2 cursorPos )
		{
			// Make a temporary copy of the event to avoid possibility of
			// a race condition if the last subscriber unsubscribes
			// immediately after the null check and before the event is raised.
			CursorMovedHandler handler = CursorMoved;
			if( handler != null )
			{
				handler( cursorPos );
			}
		}

		/// <summary>
		/// Raises the CursorPressed event.
		/// </summary>
		/// <param name="cursorPos">current cursor position</param>
		virtual public void OnCursorPressed( Vector2 cursorPos )
		{
			// Make a temporary copy of the event to avoid possibility of
			// a race condition if the last subscriber unsubscribes
			// immediately after the null check and before the event is raised.
			CursorPressedHandler handler = CursorPressed;
			if( handler != null )
			{
				handler( this, cursorPos );
			}
		}

		/// <summary>
		/// Raises the CursorReleased event.
		/// </summary>
		/// <param name="cursorPos">current cursor position</param>
		virtual public void OnCursorReleased( Vector2 cursorPos )
		{
			// Make a temporary copy of the event to avoid possibility of
			// a race condition if the last subscriber unsubscribes
			// immediately after the null check and before the event is raised.
			CursorReleasedHandler handler = CursorReleased;
			if( handler != null )
			{
				handler( cursorPos );
			}
		}

		/// <summary>
		/// Raises raises the LostFocus event.
		/// </summary>
		virtual public void OnLostFocus()
		{
			// Make a temporary copy of the event to avoid possibility of
			// a race condition if the last subscriber unsubscribes
			// immediately after the null check and before the event is raised.
			LostFocusHandler handler = LostFocus;
			if( handler != null )
			{
				handler();
			}
		}

		#endregion events
	}
}
