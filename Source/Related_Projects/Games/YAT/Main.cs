using System;

namespace YAT {
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class AppMain {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			using(TetrisApplication application = new TetrisApplication()) 
	{
				application.Start();
			}
		}
	}
}
