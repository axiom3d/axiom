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

using Axiom.Math;
using Axiom.Overlays;
using Axiom.Overlays.Elements;

namespace Axiom.Samples
{
	/// <summary>
	/// Basic Button 
	/// </summary>
	public class Button : Widget
	{
		#region fields

		/// <summary>
		/// 
		/// </summary>
		protected ButtonState buttonState;

		/// <summary>
		/// 
		/// </summary>
		protected BorderPanel BorderPanel;

		/// <summary>
		/// 
		/// </summary>
		protected TextArea TextArea;

		/// <summary>
		/// 
		/// </summary>
		protected bool isFitToContents;

		#endregion

		#region properties

		/// <summary>
		/// 
		/// </summary>
		public ButtonState State
		{
			get { return buttonState; }
			protected set
			{
				if( value == ButtonState.Over )
				{
					this.BorderPanel.BorderMaterialName = "SdkTrays/Button/Over";
					this.BorderPanel.MaterialName = "SdkTrays/Button/Over";
				}
				else if( value == ButtonState.Up )
				{
					this.BorderPanel.BorderMaterialName = "SdkTrays/Button/Up";
					this.BorderPanel.MaterialName = "SdkTrays/Button/Up";
				}
				else
				{
					this.BorderPanel.BorderMaterialName = "SdkTrays/Button/Down";
					this.BorderPanel.MaterialName = "SdkTrays/Button/Down";
				}

				this.buttonState = value;
			}
		}

		/// <summary>
		/// Text of this Button
		/// </summary>
		public string Caption
		{
			get { return this.TextArea.Text; }
			set
			{
				this.TextArea.Text = value;
				if( this.isFitToContents )
				{
					this.element.Width = GetCaptionWidth( Caption, this.TextArea ) + element.Height - 12;
				}
			}
		}

		#endregion properties

		#region Construction and Destruction

		/// <summary>
		/// Creates a new instance of <see cref="Button" />
		/// </summary>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <remarks>
		/// Do not instantiate any widgets directly. Use SdkTrayManager.
		/// </remarks>
		public Button( String name, String caption, Real width )
		{
			element = OverlayManager.Instance.Elements.CreateElementFromTemplate( "SdkTrays/Button", "BorderPanel", name );
			this.BorderPanel = (BorderPanel)element;
			this.TextArea = (TextArea)this.BorderPanel.Children[ this.BorderPanel.Name + "/ButtonCaption" ];
			this.TextArea.Top = -( this.TextArea.CharHeight / 2 ); //
			if( width > 0 )
			{
				element.Width = width;
				this.isFitToContents = false;
			}
			else
			{
				this.isFitToContents = true;
			}

			this.Caption = caption;
			this.State = ButtonState.Up;
		}

		#endregion Construction and Destruction

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorPressed( Vector2 cursorPos )
		{
			if( IsCursorOver( element, cursorPos, 4 ) )
			{
				this.State = ButtonState.Down;
				base.OnCursorPressed( cursorPos );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorReleased( Vector2 cursorPos )
		{
			if( this.State == ButtonState.Down )
			{
				this.State = ButtonState.Over;
				if( this.listener != null )
				{
					listener.OnButtonHit( this, this );
				}
			}

			base.OnCursorReleased( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorMoved( Vector2 cursorPos )
		{
			if( IsCursorOver( element, cursorPos, 4 ) )
			{
				if( this.State == ButtonState.Up )
				{
					this.State = ButtonState.Over;
				}
			}
			else
			{
				if( this.State != ButtonState.Up )
				{
					this.State = ButtonState.Up;
				}
			}

			base.OnCursorMoved( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		public override void OnLostFocus()
		{
			this.State = ButtonState.Up; // reset button if cursor was lost

			base.OnLostFocus();
		}
	}
}
