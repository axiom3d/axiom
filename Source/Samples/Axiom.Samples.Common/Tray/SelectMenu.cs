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
using Axiom.Math;
using Axiom.Overlays;
using Axiom.Overlays.Elements;

namespace Axiom.Samples
{
	/// <summary>
	/// Basic selection menu widget.
	/// </summary>
	public class SelectMenu : Widget
	{
		#region events

		/// <summary>
		/// Delegate used by SelectedIndexChanged event
		/// </summary>
		/// <param name="sender"></param>
		public delegate void SelectionChangedHandler( SelectMenu sender );

		/// <summary>
		/// Occours when the selcted index has changed.
		/// </summary>
		public event SelectionChangedHandler SelectedIndexChanged;

		/// <summary>
		/// Raises the Selected Index Changed event.
		/// </summary>
		/// <param name="sender"></param>
		public virtual void OnSelectedIndexChanged( SelectMenu sender )
		{
			if ( SelectedIndexChanged != null )
			{
				SelectedIndexChanged( sender );
			}
		}

		#endregion

		#region fields

		/// <summary>
		/// 
		/// </summary>
		protected BorderPanel smallBox;

		/// <summary>
		/// 
		/// </summary>
		protected BorderPanel expandedBox;

		/// <summary>
		/// 
		/// </summary>
		protected TextArea textArea;

		/// <summary>
		/// 
		/// </summary>
		protected TextArea smallTextArea;

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
		protected List<BorderPanel> itemElements;

		/// <summary>
		/// 
		/// </summary>
		protected int maxItemsShown;

		/// <summary>
		/// 
		/// </summary>
		protected int itemsShown;

		/// <summary>
		/// 
		/// </summary>
		protected bool IsCursorOver;

		/// <summary>
		/// 
		/// </summary>
		protected bool isExpanded;

		/// <summary>
		/// 
		/// </summary>
		protected bool isFitToContents;

		/// <summary>
		/// 
		/// </summary>
		protected bool isDragging;

		/// <summary>
		/// 
		/// </summary>
		protected List<String> items;

		/// <summary>
		/// 
		/// </summary>
		protected int sectionIndex;

		/// <summary>
		/// 
		/// </summary>
		protected int highlightIndex;

		/// <summary>
		/// 
		/// </summary>
		protected int displayIndex;

		/// <summary>
		/// 
		/// </summary>
		protected Real dragOffset;

		#endregion fields

		#region properties

		/// <summary>
		/// Internal method - sets which item goes at the top of the expanded menu.
		/// </summary>
		protected int DisplayIndex
		{
			set
			{
				int index = value;
				index = System.Math.Min( index, items.Count - itemElements.Count );
				displayIndex = index;
				BorderPanel ie;
				TextArea ta;

				for ( int i = 0; i < itemElements.Count; i++ )
				{
					ie = itemElements[ i ];
					ta = (TextArea)ie.Children[ ie.Name + "/MenuItemText" ];

					FitCaptionToArea( items[ displayIndex + i ], ta, ie.Width - 2*ta.Left );

					if ( displayIndex + i == highlightIndex )
					{
						ie.MaterialName = "SdkTrays/MiniTextBox/Over";
						ie.BorderMaterialName = "SdkTrays/MiniTextBox/Over";
					}
					else
					{
						ie.MaterialName = "SdkTrays/MiniTextBox";
						ie.BorderMaterialName = "SdkTrays/MiniTextBox";
					}
				}
			}
		}

		/// <summary>
		/// Indicates whether this menu is expanded or not.
		/// </summary>
		public bool IsExpanded
		{
			get
			{
				return isExpanded;
			}
		}

		/// <summary>
		/// Gets or sets the caption of this menu.
		/// </summary>
		public string Caption
		{
			get
			{
				return textArea.Text;
			}
			set
			{
				textArea.Text = value;
				if ( isFitToContents )
				{
					element.Width = GetCaptionWidth( value, textArea ) + smallBox.Width + 23;
					smallBox.Left = element.Width - smallBox.Width - 5;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public IList<string> Items
		{
			get
			{
				return items;
			}
			set
			{
				items = (List<String>)value;
				sectionIndex = -1;

				for ( int i = 0; i < itemElements.Count; i++ ) // destroy all the item elements
				{
					NukeOverlayElement( itemElements[ i ] );
				}
				itemElements.Clear();

				itemsShown = System.Math.Max( 2, System.Math.Min( maxItemsShown, items.Count ) );

				for ( int i = 0; i < itemsShown; i++ ) // create all the item elements
				{
					var e =
						(BorderPanel)
						OverlayManager.Instance.Elements.CreateElementFromTemplate( "SdkTrays/SelectMenuItem", "BorderPanel",
						                                                            expandedBox.Name + "/Item" + ( i + 1 ) );

					e.Top = 6 + i*( smallBox.Height - 8 );
					e.Width = expandedBox.Width - 32;

					expandedBox.AddChild( e );
					itemElements.Add( e );
				}

				if ( !( items.Count == 0 ) )
				{
					SelectItem( 0, false );
				}
				else
				{
					smallTextArea.Text = "";
				}
			}
		}

		/// <summary>
		/// Gets ammount of current items
		/// </summary>
		public int ItemsCount
		{
			get
			{
				return items.Count;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public string SelectedItem
		{
			get
			{
				if ( sectionIndex == -1 )
				{
					String desc = "Menu \"" + Name + "\" has no item selected.";
					throw new AxiomException( desc + ", SelectMenu.getSelectedItem" );
					throw new Exception();
				}
				else
				{
					return items[ sectionIndex ];
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public int SelectionIndex
		{
			get
			{
				return sectionIndex;
			}
		}

		#endregion

		#region construction

		/// <summary>
		///  Do not instantiate any widgets directly. Use SdkTrayManager.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="boxWidth"></param>
		/// <param name="maxItemsShown"></param>
		public SelectMenu( String name, String caption, Real width, Real boxWidth, int maxItemsShown )
		{
			items = new List<string>();
			itemElements = new List<BorderPanel>();

			sectionIndex = -1;
			isFitToContents = false;
			IsCursorOver = false;
			isExpanded = false;
			isDragging = false;
			this.maxItemsShown = maxItemsShown;
			itemsShown = 0;
			element =
				(BorderPanel)
				OverlayManager.Instance.Elements.CreateElementFromTemplate( "SdkTrays/SelectMenu", "BorderPanel", name );
			textArea = (TextArea)( (OverlayElementContainer)element ).Children[ name + "/MenuCaption" ];
			smallBox = (BorderPanel)( (OverlayElementContainer)element ).Children[ name + "/MenuSmallBox" ];
			smallBox.Width = width - 10;
			smallTextArea = (TextArea)smallBox.Children[ name + "/MenuSmallBox/MenuSmallText" ];
			element.Width = width;

			if ( boxWidth > 0 ) // long style
			{
				if ( width <= 0 )
				{
					isFitToContents = true;
				}
				smallBox.Width = boxWidth;
				smallBox.Top = 2;
				smallBox.Left = width - boxWidth - 5;
				element.Height = smallBox.Height + 4;
				textArea.HorizontalAlignment = HorizontalAlignment.Left;
				textArea.TextAlign = HorizontalAlignment.Left;
				textArea.Left = 12;
				textArea.Top = 10;
			}

			expandedBox = (BorderPanel)( (OverlayElementContainer)element ).Children[ name + "/MenuExpandedBox" ];
			expandedBox.Width = smallBox.Width + 10;
			expandedBox.Hide();
			scrollTrack = (BorderPanel)expandedBox.Children[ expandedBox.Name + "/MenuScrollTrack" ];
			scrollHandle = (Panel)scrollTrack.Children[ scrollTrack.Name + "/MenuScrollHandle" ];

			Caption = caption;
		}

		#endregion construction

		#region methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		public void AddItem( String item )
		{
			items.Add( item );
			Items = items;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		public void RemoveItem( String item )
		{
			if ( items.Contains( item ) )
			{
				items.Remove( item );
				if ( items.Count < itemsShown )
				{
					itemsShown = items.Count;
					NukeOverlayElement( itemElements[ itemElements.Count - 1 ] );
					itemElements.RemoveAt( itemElements.Count - 1 );
				}
			}
			else
			{
				String desc = "Menu \"" + Name + "\" contains no item \"" + item + "\".";
				throw new AxiomException( desc + "SelectMenu.removeItem" );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		public void RemoveItem( int index )
		{
			try
			{
				items.RemoveAt( index );
			}
			catch ( ArgumentOutOfRangeException ex )
			{
				String desc = "Menu \"" + Name + "\" contains no item at position " + index + ".";
				throw new AxiomException( desc + ", SelectMenu.RemoveItem" );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void ClearItems()
		{
			items.Clear();
			sectionIndex = -1;
			smallTextArea.Text = "";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		public void SelectItem( int index )
		{
			SelectItem( index, true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="notifyListener"></param>
		public void SelectItem( int index, bool notifyListener )
		{
			if ( index < 0 || index >= items.Count )
			{
				String desc = "Menu \"" + Name + "\" contains no item at position " + index + ".";
				throw new AxiomException( desc + ", SelectMenu.SelectItem" );
			}
			sectionIndex = index;
			FitCaptionToArea( items[ index ], smallTextArea, smallBox.Width - smallTextArea.Left*2 );

			if ( listener != null && notifyListener )
			{
				listener.ItemSelected( this );
			}

			OnSelectedIndexChanged( this );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <param name="notifyListener"></param>
		public void SelectItem( String item )
		{
			SelectItem( item, true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <param name="notifyListener"></param>
		public void SelectItem( String item, bool notifyListener )
		{
			for ( int i = 0; i < items.Count; i++ )
			{
				if ( item == items[ i ] )
				{
					SelectItem( i, notifyListener );
					return;
				}
			}

			String desc = "Menu \"" + Name + "\" contains no item \"" + item + "\".";
			throw new AxiomException( desc + ", SelectMenu.SelectItem" );
			throw new Exception();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorPressed( Vector2 cursorPos )
		{
			OverlayManager om = OverlayManager.Instance;

			if ( isExpanded )
			{
				if ( scrollHandle.IsVisible ) // check for scrolling
				{
					Vector2 co = Widget.CursorOffset( scrollHandle, cursorPos );

					if ( co.LengthSquared <= 81 )
					{
						isDragging = true;
						dragOffset = co.y;
						return;
					}
					else if ( Widget.IsCursorOver( scrollTrack, cursorPos ) )
					{
						Real newTop = scrollHandle.Top + co.y;
						Real lowerBoundary = scrollTrack.Height - scrollHandle.Height;
						scrollHandle.Top = Math.Utility.Clamp<Real>( newTop, lowerBoundary, 0 );

						var scrollPercentage = Math.Utility.Clamp<Real>( newTop/lowerBoundary, 1, 0 );
						DisplayIndex = (int)( scrollPercentage*( items.Count - itemElements.Count ) + 0.5 );
						return;
					}
				}

				if ( !IsCursorOver( expandedBox, cursorPos, 3 ) )
				{
					Retract();
				}
				else
				{
					Real l = itemElements[ 0 ].DerivedLeft*om.ViewportWidth + 5;
					Real t = itemElements[ 0 ].DerivedTop*om.ViewportHeight + 5;
					Real r = l + itemElements[ itemElements.Count - 1 ].Width - 10;
					Real b = itemElements[ itemElements.Count - 1 ].DerivedTop*om.ViewportHeight +
					         itemElements[ itemElements.Count - 1 ].Height - 5;

					if ( cursorPos.x >= l && cursorPos.x <= r && cursorPos.y >= t && cursorPos.y <= b )
					{
						if ( highlightIndex != sectionIndex )
						{
							SelectItem( highlightIndex );
						}
						Retract();
					}
				}
			}
			else
			{
				if ( items.Count < 2 )
				{
					return; // don't waste time showing a menu if there's no choice
				}

				if ( IsCursorOver( smallBox, cursorPos, 4 ) )
				{
					expandedBox.Show();
					smallBox.Hide();

					// calculate how much vertical space we need
					Real idealHeight = itemsShown*( smallBox.Height - 8 ) + 20;
					expandedBox.Height = idealHeight;
					scrollTrack.Height = expandedBox.Height - 20;

					expandedBox.Left = smallBox.Left - 4;

					// if the expanded menu goes down off the screen, make it go up instead
					if ( smallBox.DerivedTop*om.ViewportHeight + idealHeight > om.ViewportHeight )
					{
						expandedBox.Top = smallBox.Top + smallBox.Height - idealHeight + 3;
						// if we're in thick style, hide the caption because it will interfere with the expanded menu
						if ( textArea.HorizontalAlignment == HorizontalAlignment.Center )
						{
							textArea.Hide();
						}
					}
					else
					{
						expandedBox.Top = smallBox.Top + 3;
					}

					isExpanded = true;
					highlightIndex = sectionIndex;
					DisplayIndex = highlightIndex;

					if ( itemsShown < items.Count ) // update scrollbar position
					{
						scrollHandle.Show();
						Real lowerBoundary = scrollTrack.Height - scrollHandle.Height;
						scrollHandle.Top = (int)( displayIndex*lowerBoundary/( items.Count - itemElements.Count ) );
					}
					else
					{
						scrollHandle.Hide();
					}
				}
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
			OverlayManager om = OverlayManager.Instance;

			if ( isExpanded )
			{
				if ( isDragging )
				{
					Vector2 co = Widget.CursorOffset( scrollHandle, cursorPos );
					Real newTop = scrollHandle.Top + co.y - dragOffset;
					Real lowerBoundary = scrollTrack.Height - scrollHandle.Height;
					scrollHandle.Top = Math.Utility.Clamp<Real>( newTop, lowerBoundary, 0 );

					var scrollPercentage = Math.Utility.Clamp<Real>( newTop/lowerBoundary, 0, 1 );
					var newIndex = (int)( scrollPercentage*( items.Count - itemElements.Count ) + 0.5 );
					if ( newIndex != displayIndex )
					{
						DisplayIndex = newIndex;
					}
					return;
				}

				Real l = itemElements[ 0 ].DerivedLeft*om.ViewportWidth + 5;
				Real t = itemElements[ 0 ].DerivedTop*om.ViewportHeight + 5;
				Real r = l + itemElements[ itemElements.Count - 1 ].Width - 10;
				Real b = itemElements[ itemElements.Count - 1 ].DerivedTop*om.ViewportHeight +
				         itemElements[ itemElements.Count - 1 ].Height - 5;

				if ( cursorPos.x >= l && cursorPos.x <= r && cursorPos.y >= t && cursorPos.y <= b )
				{
					var newIndex = (int)( displayIndex + ( cursorPos.y - t )/( b - t )*itemElements.Count );
					if ( highlightIndex != newIndex )
					{
						highlightIndex = newIndex;
						DisplayIndex = displayIndex;
					}
				}
			}
			else
			{
				if ( IsCursorOver( smallBox, cursorPos, 4 ) )
				{
					smallBox.MaterialName = "SdkTrays/MiniTextBox/Over";
					smallBox.BorderMaterialName = "SdkTrays/MiniTextBox/Over";
					IsCursorOver = true;
				}
				else
				{
					if ( IsCursorOver )
					{
						smallBox.MaterialName = "SdkTrays/MiniTextBox";
						smallBox.BorderMaterialName = "SdkTrays/MiniTextBox";
						IsCursorOver = false;
					}
				}
			}

			base.OnCursorMoved( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		public override void OnLostFocus()
		{
			if ( expandedBox.IsVisible )
			{
				Retract();
			}

			base.OnLostFocus();
		}

		/// <summary>
		/// Internal method - cleans up an expanded menu.
		/// </summary>
		protected void Retract()
		{
			isDragging = false;
			isExpanded = false;
			expandedBox.Hide();
			textArea.Show();
			smallBox.Show();
			smallBox.MaterialName = "SdkTrays/MiniTextBox";
			smallBox.BorderMaterialName = "SdkTrays/MiniTextBox";
		}

		#endregion
	}
}