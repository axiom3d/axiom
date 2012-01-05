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
		/// Occours when the selcted index has changed.
		/// </summary>
		public event EventHandler SelectedIndexChanged;

		/// <summary>
		/// Raises the Selected Index Changed event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		virtual public void OnSelectedIndexChanged( object sender, EventArgs e )
		{
			if( SelectedIndexChanged != null )
			{
				SelectedIndexChanged( sender, e );
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
				index = System.Math.Min( index, this.items.Count - this.itemElements.Count );
				this.displayIndex = index;
				BorderPanel ie;
				TextArea ta;

				for( int i = 0; i < this.itemElements.Count; i++ )
				{
					ie = this.itemElements[ i ];
					ta = (TextArea)ie.Children[ ie.Name + "/MenuItemText" ];

					FitCaptionToArea( this.items[ this.displayIndex + i ], ta, ie.Width - 2 * ta.Left );

					if( this.displayIndex + i == this.highlightIndex )
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
		public bool IsExpanded { get { return isExpanded; } }

		/// <summary>
		/// Gets or sets the caption of this menu.
		/// </summary>
		public string Caption
		{
			get { return textArea.Text; }
			set
			{
				this.textArea.Text = value;
				if( this.isFitToContents )
				{
					element.Width = GetCaptionWidth( value, this.textArea ) + this.smallBox.Width + 23;
					this.smallBox.Left = element.Width - this.smallBox.Width - 5;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public IList<string> Items
		{
			get { return items; }
			set
			{
				this.items = (List<String>)value;
				this.sectionIndex = -1;

				for( int i = 0; i < this.itemElements.Count; i++ ) // destroy all the item elements
				{
					NukeOverlayElement( this.itemElements[ i ] );
				}
				this.itemElements.Clear();

				this.itemsShown = System.Math.Max( 2, System.Math.Min( this.maxItemsShown, this.items.Count ) );

				for( int i = 0; i < this.itemsShown; i++ ) // create all the item elements
				{
					BorderPanel e =
						(BorderPanel)OverlayManager.Instance.Elements.CreateElementFromTemplate
						             	( "SdkTrays/SelectMenuItem", "BorderPanel",
						             	  this.expandedBox.Name + "/Item" + ( i + 1 ) );

					e.Top = 6 + i * ( this.smallBox.Height - 8 );
					e.Width = this.expandedBox.Width - 32;

					this.expandedBox.AddChild( e );
					this.itemElements.Add( e );
				}

				if( !( items.Count == 0 ) )
				{
					this.SelectItem( 0, false );
				}
				else
				{
					this.smallTextArea.Text = "";
				}
			}
		}

		/// <summary>
		/// Gets ammount of current items
		/// </summary>
		public int ItemsCount { get { return items.Count; } }

		/// <summary>
		/// 
		/// </summary>
		public string SelectedItem
		{
			get
			{
				if( this.sectionIndex == -1 )
				{
					String desc = "Menu \"" + Name + "\" has no item selected.";
					throw new AxiomException( desc + ", SelectMenu.getSelectedItem" );
					throw new Exception();
				}
				else
				{
					return this.items[ this.sectionIndex ];
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public int SelectionIndex { get { return sectionIndex; } }

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

			this.sectionIndex = -1;
			this.isFitToContents = false;
			this.IsCursorOver = false;
			this.isExpanded = false;
			this.isDragging = false;
			this.maxItemsShown = maxItemsShown;
			this.itemsShown = 0;
			element = (BorderPanel)OverlayManager.Instance.Elements.CreateElementFromTemplate
			                       	( "SdkTrays/SelectMenu", "BorderPanel", name );
			this.textArea = (TextArea)( (OverlayElementContainer)element ).Children[ name + "/MenuCaption" ];
			this.smallBox = (BorderPanel)( (OverlayElementContainer)element ).Children[ name + "/MenuSmallBox" ];
			this.smallBox.Width = width - 10;
			this.smallTextArea = (TextArea)this.smallBox.Children[ name + "/MenuSmallBox/MenuSmallText" ];
			element.Width = width;

			if( boxWidth > 0 ) // long style
			{
				if( width <= 0 )
				{
					this.isFitToContents = true;
				}
				this.smallBox.Width = boxWidth;
				this.smallBox.Top = 2;
				this.smallBox.Left = width - boxWidth - 5;
				element.Height = this.smallBox.Height + 4;
				this.textArea.HorizontalAlignment = HorizontalAlignment.Left;
				this.textArea.TextAlign = HorizontalAlignment.Left;
				this.textArea.Left = 12;
				this.textArea.Top = 10;
			}

			this.expandedBox = (BorderPanel)( (OverlayElementContainer)element ).Children[ name + "/MenuExpandedBox" ];
			this.expandedBox.Width = this.smallBox.Width + 10;
			this.expandedBox.Hide();
			this.scrollTrack = (BorderPanel)this.expandedBox.Children[ this.expandedBox.Name + "/MenuScrollTrack" ];
			this.scrollHandle = (Panel)this.scrollTrack.Children[ this.scrollTrack.Name + "/MenuScrollHandle" ];

			this.Caption = caption;
		}

		#endregion construction

		#region methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		public void AddItem( String item )
		{
			this.items.Add( item );
			this.Items = this.items;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		public void RemoveItem( String item )
		{
			if( items.Contains( item ) )
			{
				items.Remove( item );
				if( this.items.Count < this.itemsShown )
				{
					this.itemsShown = this.items.Count;
					NukeOverlayElement( this.itemElements[ this.itemElements.Count - 1 ] );
					this.itemElements.RemoveAt( itemElements.Count - 1 );
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
			catch( ArgumentOutOfRangeException ex )
			{
				String desc = "Menu \"" + Name + "\" contains no item at position " +
				              index + ".";
				throw new AxiomException( desc + ", SelectMenu.RemoveItem" );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void ClearItems()
		{
			this.items.Clear();
			this.sectionIndex = -1;
			this.smallTextArea.Text = "";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		public void SelectItem( int index )
		{
			this.SelectItem( index, true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="notifyListener"></param>
		public void SelectItem( int index, bool notifyListener )
		{
			if( index < 0 || index >= this.items.Count )
			{
				String desc = "Menu \"" + Name + "\" contains no item at position " +
				              index + ".";
				throw new AxiomException( desc + ", SelectMenu.SelectItem" );
			}
			this.sectionIndex = index;
			FitCaptionToArea( this.items[ index ], this.smallTextArea, this.smallBox.Width - this.smallTextArea.Left * 2 );

			if( listener != null && notifyListener )
			{
				listener.ItemSelected( this );
			}

			OnSelectedIndexChanged( this, new EventArgs() );
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
			for( int i = 0; i < this.items.Count; i++ )
			{
				if( item == this.items[ i ] )
				{
					this.SelectItem( i, notifyListener );
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

			if( this.isExpanded )
			{
				if( this.scrollHandle.IsVisible ) // check for scrolling
				{
					Vector2 co = Widget.CursorOffset( this.scrollHandle, cursorPos );

					if( co.LengthSquared <= 81 )
					{
						this.isDragging = true;
						this.dragOffset = co.y;
						return;
					}
					else if( Widget.IsCursorOver( this.scrollTrack, cursorPos ) )
					{
						Real newTop = this.scrollHandle.Top + co.y;
						Real lowerBoundary = this.scrollTrack.Height - this.scrollHandle.Height;
						this.scrollHandle.Top = Math.Utility.Clamp<Real>( newTop, lowerBoundary, 0 );

						Real scrollPercentage = Math.Utility.Clamp<Real>( newTop / lowerBoundary, 1, 0 );
						DisplayIndex = (int)( scrollPercentage * ( this.items.Count - this.itemElements.Count ) + 0.5 );
						return;
					}
				}

				if( !IsCursorOver( this.expandedBox, cursorPos, 3 ) )
				{
					this.Retract();
				}
				else
				{
					Real l = this.itemElements[ 0 ].DerivedLeft * om.ViewportWidth + 5;
					Real t = this.itemElements[ 0 ].DerivedTop * om.ViewportHeight + 5;
					Real r = l + this.itemElements[ itemElements.Count - 1 ].Width - 10;
					Real b = this.itemElements[ itemElements.Count - 1 ].DerivedTop * om.ViewportHeight +
					         this.itemElements[ itemElements.Count - 1 ].Height - 5;

					if( cursorPos.x >= l && cursorPos.x <= r && cursorPos.y >= t && cursorPos.y <= b )
					{
						if( this.highlightIndex != this.sectionIndex )
						{
							this.SelectItem( this.highlightIndex );
						}
						this.Retract();
					}
				}
			}
			else
			{
				if( this.items.Count < 2 )
				{
					return; // don't waste time showing a menu if there's no choice
				}

				if( IsCursorOver( this.smallBox, cursorPos, 4 ) )
				{
					this.expandedBox.Show();
					this.smallBox.Hide();

					// calculate how much vertical space we need
					Real idealHeight = this.itemsShown * ( this.smallBox.Height - 8 ) + 20;
					this.expandedBox.Height = idealHeight;
					this.scrollTrack.Height = this.expandedBox.Height - 20;

					this.expandedBox.Left = this.smallBox.Left - 4;

					// if the expanded menu goes down off the screen, make it go up instead
					if( this.smallBox.DerivedTop * om.ViewportHeight + idealHeight > om.ViewportHeight )
					{
						this.expandedBox.Top = this.smallBox.Top + this.smallBox.Height - idealHeight + 3;
						// if we're in thick style, hide the caption because it will interfere with the expanded menu
						if( this.textArea.HorizontalAlignment == HorizontalAlignment.Center )
						{
							this.textArea.Hide();
						}
					}
					else
					{
						this.expandedBox.Top = this.smallBox.Top + 3;
					}

					this.isExpanded = true;
					this.highlightIndex = this.sectionIndex;
					DisplayIndex = this.highlightIndex;

					if( this.itemsShown < this.items.Count ) // update scrollbar position
					{
						this.scrollHandle.Show();
						Real lowerBoundary = this.scrollTrack.Height - this.scrollHandle.Height;
						this.scrollHandle.Top = (int)( this.displayIndex * lowerBoundary / ( this.items.Count - this.itemElements.Count ) );
					}
					else
					{
						this.scrollHandle.Hide();
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
			this.isDragging = false;

			base.OnCursorReleased( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorMoved( Vector2 cursorPos )
		{
			OverlayManager om = OverlayManager.Instance;

			if( this.isExpanded )
			{
				if( this.isDragging )
				{
					Vector2 co = Widget.CursorOffset( this.scrollHandle, cursorPos );
					Real newTop = this.scrollHandle.Top + co.y - this.dragOffset;
					Real lowerBoundary = this.scrollTrack.Height - this.scrollHandle.Height;
					this.scrollHandle.Top = Math.Utility.Clamp<Real>( newTop, lowerBoundary, 0 );

					Real scrollPercentage = Math.Utility.Clamp<Real>( newTop / lowerBoundary, 0, 1 );
					int newIndex = (int)( scrollPercentage * ( this.items.Count - this.itemElements.Count ) + 0.5 );
					if( newIndex != this.displayIndex )
					{
						DisplayIndex = newIndex;
					}
					return;
				}

				Real l = this.itemElements[ 0 ].DerivedLeft * om.ViewportWidth + 5;
				Real t = this.itemElements[ 0 ].DerivedTop * om.ViewportHeight + 5;
				Real r = l + this.itemElements[ itemElements.Count - 1 ].Width - 10;
				Real b = this.itemElements[ itemElements.Count - 1 ].DerivedTop * om.ViewportHeight +
				         this.itemElements[ itemElements.Count - 1 ].Height - 5;

				if( cursorPos.x >= l && cursorPos.x <= r && cursorPos.y >= t && cursorPos.y <= b )
				{
					int newIndex = (int)( this.displayIndex + ( cursorPos.y - t ) / ( b - t ) * this.itemElements.Count );
					if( this.highlightIndex != newIndex )
					{
						this.highlightIndex = newIndex;
						DisplayIndex = this.displayIndex;
					}
				}
			}
			else
			{
				if( IsCursorOver( this.smallBox, cursorPos, 4 ) )
				{
					this.smallBox.MaterialName = "SdkTrays/MiniTextBox/Over";
					this.smallBox.BorderMaterialName = "SdkTrays/MiniTextBox/Over";
					this.IsCursorOver = true;
				}
				else
				{
					if( this.IsCursorOver )
					{
						this.smallBox.MaterialName = "SdkTrays/MiniTextBox";
						this.smallBox.BorderMaterialName = "SdkTrays/MiniTextBox";
						this.IsCursorOver = false;
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
			if( this.expandedBox.IsVisible )
			{
				this.Retract();
			}

			base.OnLostFocus();
		}

		/// <summary>
		/// Internal method - cleans up an expanded menu.
		/// </summary>
		protected void Retract()
		{
			this.isDragging = false;
			this.isExpanded = false;
			this.expandedBox.Hide();
			this.textArea.Show();
			this.smallBox.Show();
			this.smallBox.MaterialName = "SdkTrays/MiniTextBox";
			this.smallBox.BorderMaterialName = "SdkTrays/MiniTextBox";
		}

		#endregion
	}
}
