using System;
using System.Windows.Forms;

namespace Axiom.EventSystem
{
	/// <summary>
	/// Summary description for IMouseTarget.
	/// </summary>
	public interface IMouseTarget
	{
		/// <summary>
		/// 
		/// </summary>
		event MouseEventHandler MouseMoved;

		/// <summary>
		/// 
		/// </summary>
		event MouseEventHandler MouseEnter;

		/// <summary>
		/// 
		/// </summary>
		event MouseEventHandler MouseLeave;

		/// <summary>
		/// 
		/// </summary>
		event MouseEventHandler MouseDown;

		/// <summary>
		/// 
		/// </summary>
		event MouseEventHandler MouseUp;
	}
}
