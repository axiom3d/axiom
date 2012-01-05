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
	/// Basic label widget.
	/// </summary>
	public class Label : Widget
	{
		#region fields

		/// <summary>
		/// 
		/// </summary>
		public OverlayElement textArea;

		/// <summary>
		/// 
		/// </summary>
		protected bool isFitToTray;

		#endregion

		#region properties

		/// <summary>
		/// 
		/// </summary>
		public string Caption { get { return this.textArea.Text; } set { this.textArea.Text = value; } }

		/// <summary>
		/// 
		/// </summary>
		public bool IsFitToTray { get { return isFitToTray; } }

		#endregion properties

		/// <summary>
		/// Do not instantiate any widgets directly. Use SdkTrayManager.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		public Label( String name, String caption, Real width )
		{
			element = OverlayManager.Instance.Elements.CreateElementFromTemplate( "SdkTrays/Label", "BorderPanel", name );
			this.textArea = (TextArea)( (OverlayElementContainer)element ).Children[ Name + "/LabelCaption" ];
			this.textArea.Text = caption;
			this.Caption = caption;
			if( width <= 0 )
			{
				this.isFitToTray = true;
			}
			else
			{
				this.isFitToTray = false;
				element.Width = width;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorPressed( Vector2 cursorPos )
		{
			if( listener != null && IsCursorOver( element, cursorPos, 3 ) )
			{
				listener.LabelHit( this );
			}
			base.OnCursorPressed( cursorPos );
		}
	}
}
