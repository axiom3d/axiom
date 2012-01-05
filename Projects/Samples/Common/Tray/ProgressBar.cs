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
	/// Basic progress bar widget.
	/// </summary>
	public class ProgressBar : Widget
	{
		#region fields

		/// <summary>
		/// 
		/// </summary>
		protected TextArea textArea;

		/// <summary>
		/// 
		/// </summary>
		protected TextArea commentTextArea;

		/// <summary>
		/// 
		/// </summary>
		protected OverlayElement meter;

		/// <summary>
		/// 
		/// </summary>
		protected OverlayElement fill;

		/// <summary>
		/// 
		/// </summary>
		protected Real progress;

		#endregion fields

		#region properties

		/// <summary>
		/// Gets or sets the progress as percentage
		/// </summary>
		public Real Progress
		{
			set
			{
				progress = value;
				this.progress = Utility.Clamp<Real>( progress, 1, 0 );
				fill.Width = System.Math.Max( (int)fill.Height, (int)( progress * ( meter.Width - 2 * fill.Left ) ) );
			}
			get { return progress; }
		}

		/// <summary>
		/// Gets or sets the caption
		/// </summary>
		public string Caption { get { return textArea.Text; } set { textArea.Text = value; } }

		/// <summary>
		/// 
		/// </summary>
		public string Comment { get { return commentTextArea.Text; } set { commentTextArea.Text = value; } }

		#endregion properties

		/// <summary>
		/// Do not instantiate any widgets directly. Use SdkTrayManager.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="commentBoxWidth"></param>
		public ProgressBar( String name, String caption, Real width, Real commentBoxWidth )
		{
			element = OverlayManager.Instance.Elements.CreateElementFromTemplate( "SdkTrays/ProgressBar", "BorderPanel", name );
			element.Width = ( width );
			OverlayElementContainer c = (OverlayElementContainer)element;
			this.textArea = (TextArea)c.Children[ Name + "/ProgressCaption" ];
			OverlayElementContainer commentBox = (OverlayElementContainer)c.Children[ Name + "/ProgressCommentBox" ];
			commentBox.Width = ( commentBoxWidth );
			commentBox.Left = ( -( commentBoxWidth + 5 ) );
			this.commentTextArea = (TextArea)commentBox.Children[ commentBox.Name + "/ProgressCommentText" ];
			this.meter = c.Children[ Name + "/ProgressMeter" ];
			this.meter.Width = ( width - 10 );
			this.fill = ( (OverlayElementContainer)this.meter ).Children[ this.meter.Name + "/ProgressFill" ];
			this.Caption = caption;
		}
	};
}
