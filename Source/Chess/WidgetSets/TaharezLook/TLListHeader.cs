using System;
using CeGuiSharp;
using CeGuiSharp.Widgets;

namespace CeGuiSharp.WidgetSets.TaharezLook 
{
	/// <summary>
	/// ListHeader widget for the Taharez Gui Scheme.
	/// </summary>
	public class TLListHeader: ListHeader
	{

		#region Constants
		/// <summary>
		/// Type of window/widget to create for the segments.
		/// </summary>
		const string SegmentWidgetType = "TaharezLook.TLListHeaderSegment";
		#endregion

		#region Fields
		#endregion

		#region Constructor
		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="name"></param>
		public TLListHeader(string type, string name) : base(type, name) 
		{
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
