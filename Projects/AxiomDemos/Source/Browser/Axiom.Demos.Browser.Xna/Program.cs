using System;

namespace Axiom.Demos.Browser.Xna
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main( string[] args )
        {
            using ( Game game = new Game() )
            {
                game.Run();
            }
        }
    }
}

