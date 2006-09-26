using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.WindowsLook {

	/// <summary>
	///		Close button widget for the Windows Gui Scheme.
	/// </summary>
	public class WLListHeader : ListHeader
	{
		#region Constants

		/// <summary>
		/// Name of the imageset to use for rendering.
		/// </summary>
		const string ImagesetName				= "WindowsLook";
		/// <summary>
		/// Name of the image to be used as a Mouse Cursor.
		/// </summary>
		const string MouseCursorImageName	= "MouseArrow";

		/// <summary>
		/// Name of the SegmentHeader widget
		/// </summary>
		const string SegmentWidgetType		= "WindowsLook.WLListHeaderSegment";

		#endregion

		#region Fields
		#endregion

		#region Constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public WLListHeader( string type, string name ) : base(type, name) {
			SetMouseCursor(ImagesetManager.Instance.GetImageset(ImagesetName).GetImage(MouseCursorImageName));
		}

		#endregion

		#region ListHeader Methods
		/// <summary>
		/// Create a ListHeaderSegment of an appropriate sub-class type.
		/// </summary>
		/// <param name="name">Unique name for the new segment widget</param>
		/// <returns></returns>
		protected override ListHeaderSegment CreateNewSegment(string name)
		{
			return (ListHeaderSegment)WindowManager.Instance.CreateWindow(SegmentWidgetType, name);
		}

		/// <summary>
		/// Destroy the given ListHeaderSegment.
		/// </summary>
		/// <param name="segment">ListHeaderSegment to be destroyed.</param>
		/// <returns></returns>
		protected override void DestroyListSegment(ListHeaderSegment segment)
		{
			WindowManager.Instance.DestroyWindow(segment);
		}

		#endregion

		#region Window Methods
		/// <summary>
		///     Perform the actual rendering for this Window.
		/// </summary>
		/// <param name="z">float value specifying the base Z co-ordinate that should be used when rendering.</param>
		protected override void DrawSelf(float z)
		{
			// No widget specific rendering to be done.
		}

		#endregion
	}
}
