#region Namespace Declarations

using System;
using System.Globalization;
using System.Threading;

#endregion Namespace Declarations

namespace Demos
{

    /// <summary>
    ///     Demo browser entry point.
    /// </summary>
    public class DemoTest
    {
        [STAThread]
        private static void Main( string[] args )
        {
            try
            {
                //using(DemoBrowser browser = new DemoBrowser()) {
                //	browser.Start();
                //}

                // Change me to whatever demo you want to run for the meantime until the new browser is done
                Type demoType = typeof( Demos.EnvMapping );

                using ( TechDemo demo = (TechDemo)Activator.CreateInstance( demoType ) )
                {
                    demo.Start();
                }
            }
            catch ( Exception ex )
            {
                Console.WriteLine( ex.ToString() );
                Console.WriteLine( "An exception has occurred.  Press enter to continue..." );
                Console.ReadLine();
            }
        }
    }
}