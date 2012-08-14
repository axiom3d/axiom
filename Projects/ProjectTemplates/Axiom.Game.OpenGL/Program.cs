using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ >= 3.5)using System.Linq;
$endif$using System.Text;

namespace $safeprojectname$
{
	internal class Program
	{
		private static void Main()
		{
			try
			{
				System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US", false);
				(new Game()).Run();            
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(Axiom.Core.LogManager.BuildExceptionString(ex));
			}
		}
	}
}
