#region MIT/X11 License

//Copyright � 2003-2012 Axiom 3D Rendering Engine Project
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

using Axiom.Math;
using Axiom.Overlays;
using Axiom.Overlays.Elements;

namespace Axiom.Samples
{
	public delegate void CheckChangedHandler( CheckBox box );

	/// <summary>
	/// Basic check box widget.
	/// </summary>
	public class CheckBox : Widget
	{
		#region events

		/// <summary>
		/// 
		/// </summary>
		public event CheckChangedHandler CheckChanged;

		#endregion

		#region fields

		/// <summary>
		/// 
		/// </summary>
		protected bool IsCursorOver;

		/// <summary>
		/// 
		/// </summary>
		protected bool isFitToContents;

		/// <summary>
		/// 
		/// </summary>
		protected BorderPanel square;

		/// <summary>
		/// 
		/// </summary>
		protected TextArea textArea;

		/// <summary>
		/// 
		/// </summary>
		protected OverlayElement x;

		#endregion fields

		#region construction

		/// <summary>
		/// Do not instantiate any widgets directly. Use SdkTrayManager.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		public CheckBox( String name, String caption, Real width )
		{
			this.IsCursorOver = false;
			this.isFitToContents = width <= 0;
			element = OverlayManager.Instance.Elements.CreateElementFromTemplate( "SdkTrays/CheckBox", "BorderPanel", name );
			var c = (OverlayElementContainer)element;
			this.textArea = (TextArea)c.Children[ Name + "/CheckBoxCaption" ];
			this.square = (BorderPanel)c.Children[ Name + "/CheckBoxSquare" ];
			this.x = this.square.Children[ this.square.Name + "/CheckBoxX" ];
			this.x.Hide();
			element.Width = width;
			Caption = caption;
		}

		#endregion

		#region properties

		/// <summary>
		/// Gets or sets the caption
		/// </summary>
		public string Caption
		{
			get
			{
				return this.textArea.Text;
			}
			set
			{
				this.textArea.Text = value;
				if ( this.isFitToContents )
				{
					element.Width = GetCaptionWidth( value, this.textArea ) + this.square.Width + 23;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool IsChecked
		{
			get
			{
				return this.x.IsVisible;
			}
			set
			{
				SetChecked( value, true );
			}
		}

		#endregion

		#region methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="check"></param>
		/// <param name="notifyListener"></param>
		public void SetChecked( bool check, bool notifyListener )
		{
			if ( check )
			{
				this.x.Show();
			}
			else
			{
				this.x.Hide();
			}
			if ( listener != null && notifyListener )
			{
				listener.CheckboxToggled( this );
			}

			OnCheckChanged( this );
		}

		/// <summary>
		/// 
		/// </summary>
		public void Check()
		{
			Check( true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="notifyListener"></param>
		public void Check( bool notifyListener )
		{
			SetChecked( true, notifyListener );
		}

		/// <summary>
		/// 
		/// </summary>
		public void Uncheck()
		{
			Uncheck( true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="notifyListener"></param>
		public void Uncheck( bool notifyListener )
		{
			SetChecked( false, notifyListener );
			OnCheckChanged( this );
		}

		/// <summary>
		/// 
		/// </summary>
		public void Toggle()
		{
			Toggle( true );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="notifyListener"></param>
		public void Toggle( bool notifyListener )
		{
			SetChecked( !IsChecked, notifyListener );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorPressed( Vector2 cursorPos )
		{
			if ( this.IsCursorOver )
			{
				Toggle();
				base.OnCursorPressed( cursorPos );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorMoved( Vector2 cursorPos )
		{
			if ( IsCursorOver( this.square, cursorPos, 5 ) )
			{
				if ( !this.IsCursorOver )
				{
					this.IsCursorOver = true;
					this.square.MaterialName = "SdkTrays/MiniTextBox/Over";
					this.square.BorderMaterialName = "SdkTrays/MiniTextBox/Over";
				}
			}
			else
			{
				if ( this.IsCursorOver )
				{
					this.IsCursorOver = false;
					this.square.MaterialName = "SdkTrays/MiniTextBox";
					this.square.BorderMaterialName = "SdkTrays/MiniTextBox";
				}
			}

			base.OnCursorMoved( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		public override void OnLostFocus()
		{
			this.square.MaterialName = "SdkTrays/MiniTextBox";
			this.square.BorderMaterialName = "SdkTrays/MiniTextBox";
			this.IsCursorOver = false;

			base.OnLostFocus();
		}

		public void OnCheckChanged( CheckBox sender )
		{
			if ( CheckChanged != null )
			{
				CheckChanged( sender );
			}
		}

		#endregion
	}
}
