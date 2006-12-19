using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook {
	/// <summary>
	/// 
	/// </summary>
	public class TLButton : PushButton {
		#region Constants

		/// <summary>
		///		Name of the imageset to use for rendering.
		/// </summary>
		const string	ImagesetName				= "TaharezLook";
		/// <summary>
		///		Name of the image to use for the left end of the button (normal).
		/// </summary>
		const string	LeftNormalImageName			= "ButtonLeftNormal";
		/// <summary>
		///		Name of the image to use for the middle of the button (normal).
		/// </summary>
		const string	MiddleNormalImageName		= "ButtonMiddleNormal";
		/// <summary>
		///		Name of the image to use for the right end of the button (normal).
		/// </summary>
		const string	RightNormalImageName		= "ButtonRightNormal";
		/// <summary>
		///		Name of the image to use for the left end of the button (hover / highlighted).
		/// </summary>
		const string	LeftHighlightImageName		= "ButtonLeftHighlight";
		/// <summary>
		///		Name of the image to use for the middle of the button (hover / highlighted).
		/// </summary>
		const string	MiddleHighlightImageName	= "ButtonMiddleHighlight";
		/// <summary>
		///		Name of the image to use for the right end of the button (hover / highlighted).
		/// </summary>
		const string	RightHighlightImageName		= "ButtonRightHighlight";
		/// <summary>
		///		Name of the image to use for the left end of the button (pushed state).
		/// </summary>
		const string	LeftPushedImageName			= "ButtonLeftPushed";
		/// <summary>
		///		Name of the image to use for the middle of the button (pushed state).
		/// </summary>
		const string	MiddlePushedImageName		= "ButtonMiddlePushed";
		/// <summary>
		///		Name of the image to use for the right end of the button (pushed state).
		/// </summary>
		const string	RightPushedImageName		= "ButtonRightPushed";

		const string	MouseCursorImageName		= "MouseArrow";

		#endregion Constants

		#region Fields

		/// <summary>
		///		When true custom images will be scaled to the same size as the button.
		/// </summary>
		protected bool autoscaleImages;
		/// <summary>
		///		true if button standard imagery should be drawn.
		/// </summary>
		protected bool useStandardImagery;
		/// <summary>
		///		true if an image should be drawn for the normal state.
		/// </summary>
		protected bool useNormalImage;
		/// <summary>
		///		true if an image should be drawn for the highlighted state.
		/// </summary>
		protected bool useHoverImage;
		/// <summary>
		///		true if an image should be drawn for the pushed state.
		/// </summary>
		protected bool usePushedImage;
		/// <summary>
		///		true if an image should be drawn for the disabled state.
		/// </summary>
		protected bool useDisabledImage;

		/// <summary>
		///		RenderableImage used when rendering an image in the normal state.
		/// </summary>
		protected RenderableImage normalImage = new RenderableImage();
		/// <summary>
		///		RenderableImage used when rendering an image in the highlighted state.
		/// </summary>
		protected RenderableImage hoverImage = new RenderableImage();
		/// <summary>
		///		RenderableImage used when rendering an image in the pushed state.
		/// </summary>
		protected RenderableImage pushedImage = new RenderableImage();
		/// <summary>
		///		RenderableImage used when rendering an image in the disabled state.
		/// </summary>
		protected RenderableImage disabledImage = new RenderableImage();

		/// <summary>
		///		Image to use when rendering the button left section (normal state).
		/// </summary>
		protected Image leftSectionNormal;
		/// <summary>
		///		Image to use when rendering the button middle section (normal state).
		/// </summary>
		protected Image middleSectionNormal;
		/// <summary>
		///		Image to use when rendering the button right section (normal state).
		/// </summary>
		protected Image rightSectionNormal;
		/// <summary>
		///		Image to use when rendering the button left section (hover state).
		/// </summary>
		protected Image leftSectionHover;
		/// <summary>
		///		Image to use when rendering the button middle section (hover state).
		/// </summary>
		protected Image middleSectionHover;
		/// <summary>
		///		Image to use when rendering the button right section (hover state).
		/// </summary>
		protected Image rightSectionHover;
		/// <summary>
		///		Image to use when rendering the button left section (pushed state).
		/// </summary>
		protected Image leftSectionPushed;
		/// <summary>
		///		Image to use when rendering the button middle section (pushed state).
		/// </summary>
		protected Image middleSectionPushed;
		/// <summary>
		///		Image to use when rendering the button right section (pushed state).
		/// </summary>
		protected Image rightSectionPushed;

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name"></param>
		public TLButton(string type, string name) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			autoscaleImages = true;
			useStandardImagery = true;

			// setup images
			leftSectionNormal = imageSet.GetImage(LeftNormalImageName);
			middleSectionNormal = imageSet.GetImage(MiddleNormalImageName);
			rightSectionNormal = imageSet.GetImage(RightNormalImageName);

			leftSectionHover = imageSet.GetImage(LeftHighlightImageName);
			middleSectionHover = imageSet.GetImage(MiddleHighlightImageName);
			rightSectionHover = imageSet.GetImage(RightHighlightImageName);

			leftSectionPushed = imageSet.GetImage(LeftPushedImageName);
			middleSectionPushed = imageSet.GetImage(MiddlePushedImageName);
			rightSectionPushed = imageSet.GetImage(RightPushedImageName);

			SetMouseCursor(imageSet.GetImage(MouseCursorImageName));
		}

		#endregion Constructor

		#region Base Members

		#region Properties

		/// <summary>
		///		Get/Set whether of not custom button image areas are auto-scaled to the size of the button.
		/// </summary>
		/// <value>
		///		true if client specified custom image areas are re-sized when the button size changes.  
		///		false if image areas will remain unchanged when the button is sized.
		/// </value>
		public bool CustomImageryAutoSized {
			get {
				return autoscaleImages;
			}
			set {
				// if we are enabling auto-sizing, scale images for current size
				if ((value == true) && (value != autoscaleImages)) {
					Rect area = new Rect(0, 0, absArea.Width, absArea.Height);

					normalImage.Rect = area;
					hoverImage.Rect = area;
					pushedImage.Rect = area;
					disabledImage.Rect = area;

					RequestRedraw();
				}

				autoscaleImages = value;
			}
		}

		/// <summary>
		///		Get/Set whether or not rendering of the standard imagery is enabled.
		/// </summary>
		/// <value>true if the standard button imagery will be rendered, false if no standard rendering will be performed.</value>
		public bool StandardImageryEnabled {
			get {
				return useStandardImagery;
			}
			set {
				if(useStandardImagery != value) {
					useStandardImagery = value;
					RequestRedraw();
				}
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///		Set the details of the image to render for the button in the normal state.
		/// </summary>
		/// <param name="image">
		///		RenderableImage object with all the details for the image.  Note that an internal copy of the Renderable image is made and
		///		ownership of <paramref name="image"/> remains with client code.  If this parameter is NULL, rendering of an image for this 
		///		button state is disabled.
		/// </param>
		public void SetNormalImage(RenderableImage image) {
			if(image == null) {
				useNormalImage = false;
			}
			else {
				useNormalImage = true;
				normalImage = image;
			}
		}

		/// <summary>
		///		Set the details of the image to render for the button in the hover state.
		/// </summary>
		/// <param name="image">
		///		RenderableImage object with all the details for the image.  Note that an internal copy of the Renderable image is made and
		///		ownership of <paramref name="image"/> remains with client code.  If this parameter is NULL, rendering of an image for this 
		///		button state is disabled.
		/// </param>
		public void SetHoverImage(RenderableImage image) {
			if(image == null) {
				useHoverImage = false;
			}
			else {
				useHoverImage = true;
				hoverImage = image;
			}
		}

		/// <summary>
		///		Set the details of the image to render for the button in the pushed state.
		/// </summary>
		/// <param name="image">
		///		RenderableImage object with all the details for the image.  Note that an internal copy of the Renderable image is made and
		///		ownership of <paramref name="image"/> remains with client code.  If this parameter is NULL, rendering of an image for this 
		///		button state is disabled.
		/// </param>
		public void SetPushedImage(RenderableImage image) {
			if(image == null) {
				usePushedImage = false;
			}
			else {
				usePushedImage = true;
				pushedImage = image;
			}
		}

		/// <summary>
		///		Set the details of the image to render for the button in the disabled state.
		/// </summary>
		/// <param name="image">
		///		RenderableImage object with all the details for the image.  Note that an internal copy of the Renderable image is made and
		///		ownership of <paramref name="image"/> remains with client code.  If this parameter is NULL, rendering of an image for this 
		///		button state is disabled.
		/// </param>
		public void SetDisabledImage(RenderableImage image) {
			if(image == null) {
				useDisabledImage = false;
			}
			else {
				useDisabledImage = true;
				disabledImage = image;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawDisabled(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			Color colorVal = new Color(EffectiveAlpha, 0.7f, 0.7f, 0.7f);
			ColorRect colors = new ColorRect(colorVal, colorVal, colorVal, colorVal);

			// render standard button imagery if required
			if(useStandardImagery) {
				// calculate widths for the title bar segments
				float leftWidth		= leftSectionNormal.Width;
				float rightWidth	= rightSectionNormal.Width;
				float midWidth		= absRect.Width - leftWidth - rightWidth;

				//
				// draw the images
				//
				Vector3 pos = new Vector3(absRect.Left, absRect.Top, z);
				SizeF size = new SizeF(leftWidth, absRect.Height);
				leftSectionNormal.Draw(pos, size, clipper, colors);

				pos.x = absRect.Right - rightWidth;
				size.Width = rightWidth;
				rightSectionNormal.Draw(pos, size, clipper, colors);

				pos.x = absRect.Left + leftWidth;
				size.Width = midWidth;
				middleSectionNormal.Draw(pos, size, clipper, colors);
			}

			// render clients custom image if that is required.
			if(useDisabledImage) {
				Vector3 imagePos = new Vector3(absRect.Left, absRect.Top, GuiSystem.Instance.Renderer.GetZLayer(1));
				disabledImage.Draw(imagePos, clipper);
			}

			// Draw label text
			absRect.Top		+= (absRect.Height - this.Font.LineSpacing) * 0.5f;

			this.Font.DrawText(
				this.Text, absRect, 
				GuiSystem.Instance.Renderer.GetZLayer(1), clipper, HorizontalTextFormat.Center, colors);
		}

		#endregion Methods

		#endregion Base Members

		#region Window Members

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawNormal(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			Color colorVal = new Color(EffectiveAlpha, 1, 1, 1);
			ColorRect colors = new ColorRect(colorVal, colorVal, colorVal, colorVal);

			// render standard button imagery if required
			if(useStandardImagery) {
				// calculate widths for the title bar segments
				float leftWidth		= leftSectionNormal.Width;
				float rightWidth	= rightSectionNormal.Width;
				float midWidth		= absRect.Width - leftWidth - rightWidth;

				//
				// draw the images
				//
				Vector3 pos = new Vector3(absRect.Left, absRect.Top, z);
				SizeF size = new SizeF(leftWidth, absRect.Height);
				leftSectionNormal.Draw(pos, size, clipper, colors);

				pos.x = absRect.Right - rightWidth;
				size.Width = rightWidth;
				rightSectionNormal.Draw(pos, size, clipper, colors);

				pos.x = absRect.Left + leftWidth;
				size.Width = midWidth;
				middleSectionNormal.Draw(pos, size, clipper, colors);
			}

			// render clients custom image if that is required.
			if(useNormalImage) {
				ColorRect imageColors = normalImage.Colors;
				imageColors.SetAlpha(EffectiveAlpha);
				Vector3 imagePos = new Vector3(absRect.Left, absRect.Top, GuiSystem.Instance.Renderer.GetZLayer(1));
				normalImage.Draw(imagePos, clipper);
			}

			// Draw label text
			absRect.Top		+= (absRect.Height - this.Font.LineSpacing) * 0.5f;

			this.Font.DrawText(
				this.Text, absRect, 
				GuiSystem.Instance.Renderer.GetZLayer(1), clipper, HorizontalTextFormat.Center, colors);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawHover(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			Color colorVal = new Color(EffectiveAlpha, 1, 1, 1);
			ColorRect colors = new ColorRect(colorVal, colorVal, colorVal, colorVal);

			// render standard button imagery if required
			if(useStandardImagery) {
				// calculate widths for the title bar segments
				float leftWidth		= leftSectionHover.Width;
				float rightWidth	= rightSectionHover.Width;
				float midWidth		= absRect.Width - leftWidth - rightWidth;

				//
				// draw the images
				//
				Vector3 pos = new Vector3(absRect.Left, absRect.Top, z);
				SizeF size = new SizeF(leftWidth, absRect.Height);
				leftSectionHover.Draw(pos, size, clipper, colors);

				pos.x = absRect.Right - rightWidth;
				size.Width = rightWidth;
				rightSectionHover.Draw(pos, size, clipper, colors);

				pos.x = absRect.Left + leftWidth;
				size.Width = midWidth;
				middleSectionHover.Draw(pos, size, clipper, colors);
			}

			// render clients custom image if that is required.
			if(useHoverImage) {
				ColorRect imageColors = hoverImage.Colors;
				imageColors.SetAlpha(EffectiveAlpha);
				Vector3 imagePos = new Vector3(absRect.Left, absRect.Top, GuiSystem.Instance.Renderer.GetZLayer(1));
				hoverImage.Draw(imagePos, clipper);
			}

			// Draw label text
			absRect.Top		+= (absRect.Height - this.Font.LineSpacing) * 0.5f;

			this.Font.DrawText(
				this.Text, absRect, 
				GuiSystem.Instance.Renderer.GetZLayer(1), clipper, HorizontalTextFormat.Center, colors);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawPushed(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped.
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			Color colorVal = new Color(EffectiveAlpha, 1, 1, 1);
			ColorRect colors = new ColorRect(colorVal, colorVal, colorVal, colorVal);

			// render standard button imagery if required
			if(useStandardImagery) {
				// calculate widths for the title bar segments
				float leftWidth		= leftSectionPushed.Width;
				float rightWidth	= rightSectionPushed.Width;
				float midWidth		= absRect.Width - leftWidth - rightWidth;

				//
				// draw the images
				//
				Vector3 pos = new Vector3(absRect.Left, absRect.Top, z);
				SizeF size = new SizeF(leftWidth, absRect.Height);
				leftSectionPushed.Draw(pos, size, clipper, colors);

				pos.x = absRect.Right - rightWidth;
				size.Width = rightWidth;
				rightSectionPushed.Draw(pos, size, clipper, colors);

				pos.x = absRect.Left + leftWidth;
				size.Width = midWidth;
				middleSectionPushed.Draw(pos, size, clipper, colors);
			}

			// render clients custom image if that is required.
			if(usePushedImage) {
				ColorRect imageColors = pushedImage.Colors;
				imageColors.SetAlpha(EffectiveAlpha);
				Vector3 imagePos = new Vector3(absRect.Left, absRect.Top, GuiSystem.Instance.Renderer.GetZLayer(1));
				pushedImage.Draw(imagePos, clipper);
			}

			// Draw label text
			absRect.Top		+= (absRect.Height - this.Font.LineSpacing) * 0.5f;

			this.Font.DrawText(
				this.Text, absRect, 
				GuiSystem.Instance.Renderer.GetZLayer(1), clipper, HorizontalTextFormat.Center, colors);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSized(GuiEventArgs e) {
			// default processing
			base.OnSized (e);

			if(autoscaleImages) {
				Rect area = new Rect(0, 0, absArea.Width, absArea.Height);

				normalImage.Rect = area;
				hoverImage.Rect = area;
				pushedImage.Rect = area;
				disabledImage.Rect = area;

				e.Handled = true;
			}
		}


		#endregion Window Members
	}
}
