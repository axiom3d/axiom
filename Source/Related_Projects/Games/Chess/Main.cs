using System;
using Chess.Main;

namespace Chess {
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class AppMain {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			using(ChessApplication application = new ChessApplication()) 
	{
				application.Run();
			}
		}
	}
}
