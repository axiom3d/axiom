using System;

namespace Axiom.EventSystem
{
	public delegate void ScriptEventHandler(object source, ScriptEventArgs e);

	/// <summary>
	/// Summary description for ScriptEvent.
	/// </summary>
	public class ScriptEventArgs
	{
		protected String commandName;

		public ScriptEventArgs()
		{
		}
	}
}
