using System;
using System.Windows.Forms;

namespace Axiom.EventSystem
{
	/// <summary>
	/// Summary description for IKeyTarget.
	/// </summary>
	public interface IKeyTarget
	{
		/// <summary>
		/// 
		/// </summary>
		event KeyEventHandler KeyUp;

		/// <summary>
		///		
		/// </summary>
		event KeyPressEventHandler KeyDown;
	}
}
