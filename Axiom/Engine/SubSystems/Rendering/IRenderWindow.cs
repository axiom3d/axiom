using System;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	///		Represents an API independent interface for a rendering target, whether this be
	///		a form, picturebox, or anything else that has an underlying HWND.
	/// </summary>
	public interface IRenderWindow
	{
		#region Methods

		/// <summary>
		///		Updates the rendering target with all the rendering operations performed in
		///		the render system during this frame.
		/// </summary>
		/// <param name="vsync"></param>
		void SwapBuffers(bool vsync);

		#endregion

		#region Properties

		/// <summary>
		///		Gets a boolean that specifies whether the current window is active or not.
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		///		Gets a boolean that specifies whether or not rendering target is taking up the entire
		///		screen.
		/// </summary>
		bool IsFullscreen { get; }

		#endregion
	}
}
