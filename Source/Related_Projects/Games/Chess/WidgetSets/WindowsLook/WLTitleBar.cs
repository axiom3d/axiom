using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLTitleBar : TitleBar
	{
		#region Constants

		/// <summary>
		/// 
		/// </summary>
		protected const string	ImagesetName				= "WindowsLook";
		/// <summary>
		/// 
		/// </summary>
		protected const string	LeftEndSectionImageName	= "TitlebarLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	MiddleSectionImageName	= "TitlebarMiddle";
		/// <summary>
		/// 
		/// </summary>
		protected const string	RightEndSectionImageName	= "TitlebarRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NormalCursorImageName		= "MouseMoveCursor";
		/// <summary>
		/// 
		/// </summary>
		protected const string	NoDragCursorImageName		= "MouseArrow";

		// Colors
		/// <summary>
		/// 
		/// </summary>
		protected static Color ActiveColor		= new Color(0xFFA7C7FF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color InactiveColor		= new Color(0xFFEFEFEF);
		/// <summary>
		/// 
		/// </summary>
		protected static Color CaptionColor		= new Color(0xFF000000);

		/// <summary>
		/// 
		/// </summary>
		protected const int TextLayer = 1;
		
		#endregion

		#region Fields

		/// <summary>
		/// 
		/// </summary>
		protected Image leftImage;
		/// <summary>
		/// 
		/// </summary>
		protected Image middleImage;
		/// <summary>
		/// 
		/// </summary>
		protected Image rightImage;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLTitleBar( string type, string name ) : base(type, name) {
			// get images
			Imageset imageSet = ImagesetManager.Instance.GetImageset( ImagesetName );

			leftImage = imageSet.GetImage(LeftEndSectionImageName);
			middleImage = imageSet.GetImage(MiddleSectionImageName);
			rightImage = imageSet.GetImage(RightEndSectionImageName);

			SetMouseCursor( imageSet.GetImage(NormalCursorImageName) );

			AlwaysOnTop = false;
		}

		#endregion

		#region Window Members


		/// <summary>
		/// 
		/// </summary>
		public override Rect PixelRect {
			get {
				// clip to screen if we have no grand-parent
				if(parent == null || parent.Parent == null) {
					return GuiSystem.Instance.Renderer.Rect.GetIntersection(UnclippedPixelRect);
				}
					// clip to grand parent
				else {
					return parent.Parent.InnerRect.GetIntersection(UnclippedPixelRect);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawSelf(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if( clipper.Width == 0 ) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate the colors to use
			Color color = Parent.IsActive ? ActiveColor : InactiveColor;
			ColorRect colors = new ColorRect( color );
			colors.SetAlpha( EffectiveAlpha );

			// calculate widths for the title bar segments
			float leftWidth = leftImage.Width;
			float rightWidth = rightImage.Width;
			float midWidth = absRect.Width - (leftWidth + rightWidth);

			// draw the titlebar images
			Vector3 pos = new Vector3( absRect.Left, absRect.Top, z );
			SizeF sz = new SizeF( leftWidth, absRect.Height );
			leftImage.Draw( pos, sz, clipper, colors );
			pos.x += sz.Width;

			sz.Width = midWidth;
			middleImage.Draw( pos, sz, clipper, colors );
			pos.x += sz.Width;

			sz.Width = rightWidth;
			rightImage.Draw( pos, sz, clipper, colors );

			// draw the title text
			colors = new ColorRect( CaptionColor );
			colors.SetAlpha( EffectiveAlpha );

			Rect textClipper = new Rect( clipper.Left, clipper.Top, clipper.Right, clipper.Bottom );
			textClipper.Width = midWidth;
			textClipper = clipper.GetIntersection(textClipper);

			pos.x = absRect.Left + leftWidth;
			pos.y = absRect.Top + ((absRect.Height - Font.LineSpacing) / 2);
			pos.z = GuiSystem.Instance.Renderer.GetZLayer( TextLayer );

			Font.DrawText( parent.Text, pos, textClipper, colors );
		}

		#endregion

	}

}
