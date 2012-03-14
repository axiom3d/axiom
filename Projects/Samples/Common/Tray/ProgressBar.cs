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
	/// <summary>
	/// Basic progress bar widget.
	/// </summary>
	public class ProgressBar : Widget
	{
		#region fields

		/// <summary>
		/// 
		/// </summary>
		protected TextArea commentTextArea;

		/// <summary>
		/// 
		/// </summary>
		protected OverlayElement fill;

		/// <summary>
		/// 
		/// </summary>
		protected OverlayElement meter;

		/// <summary>
		/// 
		/// </summary>
		protected Real progress;

		/// <summary>
		/// 
		/// </summary>
		protected TextArea textArea;

		#endregion fields

		#region properties

		/// <summary>
		/// Gets or sets the progress as percentage
		/// </summary>
		public Real Progress
		{
			set
			{
				this.progress = value;
				this.progress = Utility.Clamp( this.progress, 1, 0 );
				this.fill.Width = System.Math.Max( (int)this.fill.Height, (int)( this.progress * ( this.meter.Width - 2 * this.fill.Left ) ) );
			}
			get
			{
				return this.progress;
			}
		}

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
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public string Comment
		{
			get
			{
				return this.commentTextArea.Text;
			}
			set
			{
				this.commentTextArea.Text = value;
			}
		}

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
			var c = (OverlayElementContainer)element;
			this.textArea = (TextArea)c.Children[ Name + "/ProgressCaption" ];
			var commentBox = (OverlayElementContainer)c.Children[ Name + "/ProgressCommentBox" ];
			commentBox.Width = ( commentBoxWidth );
			commentBox.Left = ( -( commentBoxWidth + 5 ) );
			this.commentTextArea = (TextArea)commentBox.Children[ commentBox.Name + "/ProgressCommentText" ];
			this.meter = c.Children[ Name + "/ProgressMeter" ];
			this.meter.Width = ( width - 10 );
			this.fill = ( (OverlayElementContainer)this.meter ).Children[ this.meter.Name + "/ProgressFill" ];
			Caption = caption;
		}
	};
}
