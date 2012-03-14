using System;

using Axiom.Math;

namespace Axiom.Samples
{
	public delegate void CursorMovedHandler( Vector2 cursorPosition );

	public delegate void CursorPressedHandler( object sender, Vector2 cursorPosition );

	public delegate void CursorReleasedHandler( Vector2 cursorPosition );

	public delegate void LostFocusHandler();

	/// <summary>
	/// 
	/// </summary>
	public class WidgetEventArgs : EventArgs
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="widget"></param>
		public WidgetEventArgs( Widget widget )
		{
			Widget = widget;
		}

		/// <summary>
		/// 
		/// </summary>
		public Widget Widget { get; set; }
	}
}
