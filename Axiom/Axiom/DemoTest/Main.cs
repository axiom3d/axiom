#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

#region Namespace Declarations

using System;
using System.Collections;
using System.Reflection;
using RealmForge;

#endregion Namespace Declarations

namespace Axiom.Demos
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
                //print out the the available tech demos and their numbers
                ArrayList demoTypes = Reflector.GetTypesDerivedFrom( Assembly.GetExecutingAssembly(), typeof( TechDemo ) );
                for ( int i = 0; i < demoTypes.Count; i++ )
                {
                    Type type = (Type)demoTypes[i];
                    Console.WriteLine( "{0}) {1}", i + 1, type.Name );
                }
                //have the user enter the number of the demo to show
                Type demoType = null;
                Console.WriteLine( "Enter the number of the demo that you want to run and press enter." );
                while ( true )
                {
                    string line = Console.ReadLine();
                    int number = -1;
                    if ( line != string.Empty )
                    {
                        number = int.Parse( line.Trim() );
                    }
                    if ( number < 1 || number > demoTypes.Count )
                        Console.WriteLine( "The number of the demo game must be between 1 and {0}, the number of demos games available.", demoTypes.Count );
                    else
                    {
                        demoType = (Type)demoTypes[number - 1];
                        break;
                    }
                }

                Console.WriteLine( "Starting the {0} demo.", demoType.Name );
                //start it
                using ( TechDemo demo = (TechDemo)Activator.CreateInstance( demoType ) )
                {
                    demo.Start();//show and start rendering
                }//dispose of it when done
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