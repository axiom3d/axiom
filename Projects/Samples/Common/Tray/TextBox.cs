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

namespace Axiom.Samples
{
	/// <summary>
	/// Scrollable text box widget.
	/// </summary>
	public class TextBox : Widget
	{
		#region fields

		/// <summary>
		/// 
		/// </summary>
		protected TextArea textArea;

		/// <summary>
		/// 
		/// </summary>
		protected BorderPanel captionBar;

		/// <summary>
		/// 
		/// </summary>
		protected TextArea captionTextArea;

		/// <summary>
		/// 
		/// </summary>
		protected BorderPanel scrollTrack;

		/// <summary>
		/// 
		/// </summary>
		protected Panel scrollHandle;

		/// <summary>
		/// 
		/// </summary>
		protected String text;

		/// <summary>
		/// 
		/// </summary>
		protected List<String> lines;

		/// <summary>
		/// 
		/// </summary>
		protected Real padding;

		/// <summary>
		/// 
		/// </summary>
		protected bool isDragging;

		/// <summary>
		/// 
		/// </summary>
		protected Real scrollPercentage;

		/// <summary>
		/// 
		/// </summary>
		protected Real dragOffset;

		/// <summary>
		/// 
		/// </summary>
		protected int startingLine;

		#endregion fields

		#region construction

		/// <summary>
		/// Do not instantiate any widgets directly. Use SdkTrayManager.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public TextBox( String name, String caption, Real width, Real height )
		{
			lines = new List<string>();
			element = OverlayManager.Instance.Elements.CreateElementFromTemplate( "SdkTrays/TextBox", "BorderPanel", name );
			element.Width = width;
			element.Height = height;
			OverlayElementContainer container = (OverlayElementContainer)element;
			this.textArea = (TextArea)container.Children[ Name + "/TextBoxText" ];
			this.captionBar = (BorderPanel)container.Children[ Name + "/TextBoxCaptionBar" ];
			this.captionBar.Width = width - 4;
			this.captionTextArea = (TextArea)this.captionBar.Children[ this.captionBar.Name + "/TextBoxCaption" ];
			this.Caption = caption;
			this.scrollTrack = (BorderPanel)container.Children[ Name + "/TextBoxScrollTrack" ];
			this.scrollHandle = (Panel)this.scrollTrack.Children[ this.scrollTrack.Name + "/TextBoxScrollHandle" ];
			this.scrollHandle.Hide();
			this.isDragging = false;
			this.scrollPercentage = 0;
			this.startingLine = 0;
			this.padding = 15;
			this.text = "";
			this.RefitContents();
		}

		#endregion

		#region properties

		/// <summary>
		/// Gets or sets current padding.
		/// </summary>
		public Real Padding
		{
			set
			{
				padding = value;
				RefitContents();
			}
			get { return padding; }
		}

		/// <summary>
		/// Gets or sets the current caption.
		/// </summary>
		public string Caption { get { return captionTextArea.Text; } set { captionTextArea.Text = value; } }

		/// <summary>
		/// Gets or sets the current text.
		/// </summary>
		public String Text
		{
			get { return this.text; }
			set
			{
				this.text = value;
				this.lines.Clear();

				Font font = (Font)FontManager.Instance.GetByName( this.textArea.FontName );

				String current = text;
				bool firstWord = true;
				int lastSpace = 0;
				int lineBegin = 0;
				Real lineWidth = 0;
				Real rightBoundary = element.Width - 2 * this.padding + this.scrollTrack.Left + 10;
				bool insert = true;
				for( int i = 0; i < current.Length; i++ )
				{
					if( current[ i ] == ' ' )
					{
						if( this.textArea.SpaceWidth != 0 )
						{
							lineWidth += this.textArea.SpaceWidth;
						}
						else
						{
							lineWidth += font.GetGlyphAspectRatio( ' ' ) * this.textArea.CharHeight;
						}
						firstWord = false;
						lastSpace = i;
						insert = true;
					}
					else if( current[ i ] == '\n' )
					{
						firstWord = true;
						lineWidth = 0;
						this.lines.Add( current.Substring( lineBegin, i - lineBegin ) );
						lineBegin = i + 1;
						insert = true;
					}
					else
					{
						// use glyph information to calculate line width
						lineWidth += font.GetGlyphAspectRatio( current[ i ] ) * this.textArea.CharHeight;
						if( lineWidth > rightBoundary && insert )
						{
							if( firstWord )
							{
								current.Insert( i, "\n" );
								i = i - 1;
								insert = false;
								//i -= 1;
							}
							else
							{
								//current.in( lastSpace, '\n'.ToString() );
								char[] ll = current.ToCharArray();
								current = string.Empty;

								ll[ lastSpace ] = '\n';
								for( int letter = 0; letter < ll.Length; letter++ )
								{
									current += ll[ letter ];
								}
								i = lastSpace - 1;
								insert = false;
							}
						}
					}
				}

				this.lines.Add( current.Substring( lineBegin ) );

				int maxLines = this.HeightInLines;

				if( this.lines.Count > maxLines ) // if too much text, filter based on scroll percentage
				{
					this.scrollHandle.Show();
					this.FilterLines();
				}
				else // otherwise just show all the text
				{
					this.textArea.Text = current;
					this.scrollHandle.Hide();
					this.scrollPercentage = 0;
					this.scrollHandle.Top = 0;
				}
			}
		}

		/// <summary>
		/// Gets or sets text box content horizontal alignment.
		/// </summary>
		public HorizontalAlignment TextAlignment
		{
			get { return textArea.HorizontalAlignment; }
			set
			{
				textArea.HorizontalAlignment = value;
				RefitContents();
			}
		}

		/// <summary>
		/// Gets or sets how far scrolled down the text is as a percentage.
		/// </summary>
		public Real ScrollPercentage
		{
			set
			{
				this.scrollPercentage = Math.Utility.Clamp<Real>( value, 1, 0 );
				this.scrollHandle.Top = (int)( value * ( this.scrollTrack.Height - this.scrollHandle.Height ) );
				this.FilterLines();
			}
			get { return scrollPercentage; }
		}

		/// <summary>
		/// Gets how many lines of text can fit in this window.
		/// </summary>
		public int HeightInLines { get { return (int)( ( element.Height - 2 * this.padding - this.captionBar.Height + 5 ) / this.textArea.CharHeight ); } }

		#endregion properties

		#region methods

		/// <summary>
		/// 
		/// </summary>
		public void ClearText()
		{
			this.Text = string.Empty;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		public void AppendText( String text )
		{
			this.Text = this.Text + text;
		}

		/// <summary>
		/// Makes adjustments based on new padding, size, or alignment info.
		/// </summary>
		public void RefitContents()
		{
			this.scrollTrack.Height = element.Height - this.captionBar.Height - 20;
			this.scrollTrack.Top = this.captionBar.Height + 10;

			this.textArea.Top = this.captionBar.Height + this.padding - 5;
			if( this.textArea.HorizontalAlignment == HorizontalAlignment.Right )
			{
				this.textArea.Left = -this.padding + this.scrollTrack.Left;
			}
			else if( this.textArea.HorizontalAlignment == HorizontalAlignment.Left )
			{
				this.textArea.Left = this.padding;
			}
			else
			{
				this.textArea.Left = this.scrollTrack.Left / 2;
			}

			Text = this.Text;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorPressed( Vector2 cursorPos )
		{
			if( !this.scrollHandle.IsVisible )
			{
				return; // don't care about clicks if text not scrollable
			}

			Vector2 co = Widget.CursorOffset( this.scrollHandle, cursorPos );

			if( co.LengthSquared <= 81 )
			{
				this.isDragging = true;
				this.dragOffset = co.y;
			}
			else if( Widget.IsCursorOver( this.scrollTrack, cursorPos ) )
			{
				Real newTop = this.scrollHandle.Top + co.y;
				Real lowerBoundary = this.scrollTrack.Height - this.scrollHandle.Height;
				this.scrollHandle.Top = Math.Utility.Clamp<Real>( newTop, lowerBoundary, 0 );

				// update text area contents based on new scroll percentage
				this.scrollPercentage = Math.Utility.Clamp<Real>( newTop / lowerBoundary, 1, 0 );
				this.FilterLines();
			}

			base.OnCursorPressed( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorReleased( Vector2 cursorPos )
		{
			this.isDragging = false;

			base.OnCursorReleased( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorMoved( Vector2 cursorPos )
		{
			if( this.isDragging )
			{
				Vector2 co = Widget.CursorOffset( this.scrollHandle, cursorPos );
				Real newTop = this.scrollHandle.Top + co.y - this.dragOffset;
				Real lowerBoundary = this.scrollTrack.Height - this.scrollHandle.Height;
				this.scrollHandle.Top = Math.Utility.Clamp<Real>( newTop, lowerBoundary, 0 );

				// update text area contents based on new scroll percentage
				this.scrollPercentage = Math.Utility.Clamp<Real>( newTop / lowerBoundary, 1, 0 );
				this.FilterLines();
			}

			base.OnCursorMoved( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		public override void OnLostFocus()
		{
			this.isDragging = false; // stop dragging if cursor was lost
			base.OnLostFocus();
		}

		/// <summary>
		///  Decides which lines to show.
		/// </summary>
		protected void FilterLines()
		{
			String shown = "";
			int maxLines = this.HeightInLines;
			int newStart = (int)( this.scrollPercentage * ( this.lines.Count - maxLines ) + 0.5f );

			this.startingLine = newStart;

			for( int i = 0; i < maxLines; i++ )
			{
				shown += this.lines[ this.startingLine + i ] + "\n";
			}

			this.textArea.Text = shown; // show just the filtered lines
		}

		#endregion methods
	}
}
