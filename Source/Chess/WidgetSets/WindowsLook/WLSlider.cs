using System;
using System.Drawing;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	/// 
	/// </summary>
	public class WLSlider : Slider
	{
		#region Constants

		/// <summary>
		/// 
		/// </summary>
		protected const string	ImagesetName				= "WindowsLook";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TrackLeftImageName			= "SliderTrackLeft";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TrackMiddleImageName		= "SlidertrackMiddle";
		/// <summary>
		/// 
		/// </summary>
		protected const string	TrackRightImageName			= "SliderTrackRight";
		/// <summary>
		/// 
		/// </summary>
		protected const string	CalibrationMarkImageName	= "SliderTick";
		/// <summary>
		/// 
		/// </summary>
		protected const string	MouseCursorImageName		= "MouseArrow";

		// window type stuff
		/// <summary>
		/// 
		/// </summary>
		protected const string	ThumbType = "WindowsLook.WLSliderThumb";

		// defaults
		/// <summary>
		/// 
		/// </summary>
		protected const float DefaultTickFrequency = 5.0f;

		#endregion

		#region Fields

		/// <summary>
		/// 
		/// </summary>
		protected Image trackLeftImage;
		/// <summary>
		/// 
		/// </summary>
		protected Image trackMiddleImage;
		/// <summary>
		/// 
		/// </summary>
		protected Image trackRightImage;
		/// <summary>
		/// 
		/// </summary>
		protected Image calibrationTickImage;

		/// <summary>
		/// 
		/// </summary>
		protected float calibrationFreq;

		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLSlider( string type, string name ) : base(type, name) {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			trackLeftImage = imageSet.GetImage(TrackLeftImageName);
			trackMiddleImage = imageSet.GetImage(TrackMiddleImageName);
			trackRightImage = imageSet.GetImage(TrackRightImageName);
			calibrationTickImage = imageSet.GetImage(CalibrationMarkImageName);

			SetMouseCursor(imageSet.GetImage(MouseCursorImageName));

			calibrationFreq = DefaultTickFrequency;
		}

		#endregion

		#region Slider Methods

		/// <summary>
		/// 
		/// </summary>
		protected override void UpdateThumb() {
			float slideExtent = AbsoluteWidth - thumb.AbsoluteWidth;

			thumb.SetHorizontalRange(0, slideExtent);
			thumb.Position = new Point(Value * (slideExtent / MaxValue), AbsoluteHeight - thumb.AbsoluteHeight);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override Thumb CreateThumb() {
			Imageset imageSet = ImagesetManager.Instance.GetImageset(ImagesetName);

			Thumb thumb = (Thumb)WindowManager.Instance.CreateWindow(ThumbType, Name + "__auto_thumb__" );
			thumb.Horizontal = true;

			thumb.MetricsMode = MetricsMode.Absolute;
			thumb.Size = imageSet.GetImage("SliderThumbNormal").Size;

			return thumb;
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void LayoutComponentWidgets() {
			UpdateThumb();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override float GetValueFromThumb() {
			return thumb.AbsoluteX / ((AbsoluteWidth - thumb.AbsoluteWidth) / MaxValue);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		protected override float GetAdjustDirectionFromPoint(Point point) {
			Rect absRect = thumb.UnclippedPixelRect;

			if(point.x < absRect.Left) {
				return -1;
			}
			else if(point.x > absRect.Right) {
				return 1;
			}
			else {
				return 0;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="z"></param>
		protected override void DrawSelf(float z) {
			Rect clipper = PixelRect;

			// do nothing if the widget is totally clipped
			if(clipper.Width == 0) {
				return;
			}

			// get the destination screen rect for this window
			Rect absRect = UnclippedPixelRect;

			// calculate the colors to use.
			ColorRect colors = new ColorRect(new Color(0xFFFFFF));
			colors.SetAlpha(EffectiveAlpha);

			// render track
			float leftWidth = trackLeftImage.Width;
			float rightWidth = trackRightImage.Width;
			float midWidth = absRect.Width - (leftWidth + rightWidth);
			float trackY = absRect.Bottom - (thumb.AbsoluteHeight * 0.5f);

			Vector3 pos = new Vector3(absRect.Left, trackY, z);
			SizeF size = new SizeF(leftWidth, trackMiddleImage.Height);
			trackLeftImage.Draw(pos, size, clipper, colors);

			pos.x += size.Width;
			size.Width = midWidth;
			trackMiddleImage.Draw(pos, size, clipper, colors);

			pos.x += size.Width;
			size.Width = rightWidth;
			trackRightImage.Draw(pos, size, clipper, colors);

			// render calibration / tick marks
			if(calibrationFreq > 0) {
				float spacing = (((AbsoluteWidth - thumb.AbsoluteWidth) / MaxValue) * Step) * calibrationFreq;
				int tickCount = (int)(AbsoluteWidth / spacing);

				pos.x = absRect.Left + (thumb.AbsoluteWidth * 0.5f);
				pos.y = absRect.Top + (thumb.AbsoluteY - calibrationTickImage.Height);

				for(int lc = 0; lc <= tickCount; ++lc) {
					calibrationTickImage.Draw(pos, clipper, colors);
					pos.x += spacing;
				}
			}
		}

		#endregion

	}
}
