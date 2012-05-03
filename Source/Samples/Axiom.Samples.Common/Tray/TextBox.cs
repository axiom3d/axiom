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
			var container = (OverlayElementContainer)element;
			textArea = (TextArea)container.Children[ Name + "/TextBoxText" ];
			captionBar = (BorderPanel)container.Children[ Name + "/TextBoxCaptionBar" ];
			captionBar.Width = width - 4;
			captionTextArea = (TextArea)captionBar.Children[ captionBar.Name + "/TextBoxCaption" ];
			Caption = caption;
			scrollTrack = (BorderPanel)container.Children[ Name + "/TextBoxScrollTrack" ];
			scrollHandle = (Panel)scrollTrack.Children[ scrollTrack.Name + "/TextBoxScrollHandle" ];
			scrollHandle.Hide();
			isDragging = false;
			scrollPercentage = 0;
			startingLine = 0;
			padding = 15;
			text = "";
			RefitContents();
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
			get
			{
				return padding;
			}
		}

		/// <summary>
		/// Gets or sets the current caption.
		/// </summary>
		public string Caption
		{
			get
			{
				return captionTextArea.Text;
			}
			set
			{
				captionTextArea.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets the current text.
		/// </summary>
		public String Text
		{
			get
			{
				return text;
			}
			set
			{
				text = value;
				lines.Clear();

				var font = (Font)FontManager.Instance.GetByName( textArea.FontName );

				String current = text;
				bool firstWord = true;
				int lastSpace = 0;
				int lineBegin = 0;
				Real lineWidth = 0;
				Real rightBoundary = element.Width - 2*padding + scrollTrack.Left + 10;
				bool insert = true;
				for ( int i = 0; i < current.Length; i++ )
				{
					if ( current[ i ] == ' ' )
					{
						if ( textArea.SpaceWidth != 0 )
						{
							lineWidth += textArea.SpaceWidth;
						}
						else
						{
							lineWidth += font.GetGlyphAspectRatio( ' ' )*textArea.CharHeight;
						}
						firstWord = false;
						lastSpace = i;
						insert = true;
					}
					else if ( current[ i ] == '\n' )
					{
						firstWord = true;
						lineWidth = 0;
						lines.Add( current.Substring( lineBegin, i - lineBegin ) );
						lineBegin = i + 1;
						insert = true;
					}
					else
					{
						// use glyph information to calculate line width
						lineWidth += font.GetGlyphAspectRatio( current[ i ] )*textArea.CharHeight;
						if ( lineWidth > rightBoundary && insert )
						{
							if ( firstWord )
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
								for ( int letter = 0; letter < ll.Length; letter++ )
								{
									current += ll[ letter ];
								}
								i = lastSpace - 1;
								insert = false;
							}
						}
					}
				}

				lines.Add( current.Substring( lineBegin ) );

				int maxLines = HeightInLines;

				if ( lines.Count > maxLines ) // if too much text, filter based on scroll percentage
				{
					scrollHandle.Show();
					FilterLines();
				}
				else // otherwise just show all the text
				{
					textArea.Text = current;
					scrollHandle.Hide();
					scrollPercentage = 0;
					scrollHandle.Top = 0;
				}
			}
		}

		/// <summary>
		/// Gets or sets text box content horizontal alignment.
		/// </summary>
		public HorizontalAlignment TextAlignment
		{
			get
			{
				return textArea.HorizontalAlignment;
			}
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
				scrollPercentage = Math.Utility.Clamp<Real>( value, 1, 0 );
				scrollHandle.Top = (int)( value*( scrollTrack.Height - scrollHandle.Height ) );
				FilterLines();
			}
			get
			{
				return scrollPercentage;
			}
		}

		/// <summary>
		/// Gets how many lines of text can fit in this window.
		/// </summary>
		public int HeightInLines
		{
			get
			{
				return (int)( ( element.Height - 2*padding - captionBar.Height + 5 )/textArea.CharHeight );
			}
		}

		#endregion properties

		#region methods

		/// <summary>
		/// 
		/// </summary>
		public void ClearText()
		{
			Text = string.Empty;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		public void AppendText( String text )
		{
			Text = Text + text;
		}

		/// <summary>
		/// Makes adjustments based on new padding, size, or alignment info.
		/// </summary>
		public void RefitContents()
		{
			scrollTrack.Height = element.Height - captionBar.Height - 20;
			scrollTrack.Top = captionBar.Height + 10;

			textArea.Top = captionBar.Height + padding - 5;
			if ( textArea.HorizontalAlignment == HorizontalAlignment.Right )
			{
				textArea.Left = -padding + scrollTrack.Left;
			}
			else if ( textArea.HorizontalAlignment == HorizontalAlignment.Left )
			{
				textArea.Left = padding;
			}
			else
			{
				textArea.Left = scrollTrack.Left/2;
			}

			Text = Text;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorPressed( Vector2 cursorPos )
		{
			if ( !scrollHandle.IsVisible )
			{
				return; // don't care about clicks if text not scrollable
			}

			Vector2 co = Widget.CursorOffset( scrollHandle, cursorPos );

			if ( co.LengthSquared <= 81 )
			{
				isDragging = true;
				dragOffset = co.y;
			}
			else if ( Widget.IsCursorOver( scrollTrack, cursorPos ) )
			{
				Real newTop = scrollHandle.Top + co.y;
				Real lowerBoundary = scrollTrack.Height - scrollHandle.Height;
				scrollHandle.Top = Math.Utility.Clamp<Real>( newTop, lowerBoundary, 0 );

				// update text area contents based on new scroll percentage
				scrollPercentage = Math.Utility.Clamp<Real>( newTop/lowerBoundary, 1, 0 );
				FilterLines();
			}

			base.OnCursorPressed( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorReleased( Vector2 cursorPos )
		{
			isDragging = false;

			base.OnCursorReleased( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorMoved( Vector2 cursorPos )
		{
			if ( isDragging )
			{
				Vector2 co = Widget.CursorOffset( scrollHandle, cursorPos );
				Real newTop = scrollHandle.Top + co.y - dragOffset;
				Real lowerBoundary = scrollTrack.Height - scrollHandle.Height;
				scrollHandle.Top = Math.Utility.Clamp<Real>( newTop, lowerBoundary, 0 );

				// update text area contents based on new scroll percentage
				scrollPercentage = Math.Utility.Clamp<Real>( newTop/lowerBoundary, 1, 0 );
				FilterLines();
			}

			base.OnCursorMoved( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		public override void OnLostFocus()
		{
			isDragging = false; // stop dragging if cursor was lost
			base.OnLostFocus();
		}

		/// <summary>
		///  Decides which lines to show.
		/// </summary>
		protected void FilterLines()
		{
			String shown = "";
			int maxLines = HeightInLines;
			var newStart = (int)( scrollPercentage*( lines.Count - maxLines ) + 0.5f );

			startingLine = newStart;

			for ( int i = 0; i < maxLines; i++ )
			{
				shown += lines[ startingLine + i ] + "\n";
			}

			textArea.Text = shown; // show just the filtered lines
		}

		#endregion methods
	}
}