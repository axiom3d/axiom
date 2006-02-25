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
using System.Threading;
using RealmForge;

#endregion Namespace Declarations

namespace Axiom.Demos
{

    /// <summary>
    ///     Demo browser entry point.
    /// </summary>
    public class DemoMain
    {
        [STAThread]
        private static void Main( string[] args )
        {

            try
            {
                using ( TechDemo demo = (TechDemo)new DemoList() )
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