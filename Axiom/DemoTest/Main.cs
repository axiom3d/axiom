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

using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Axiom.Utility;

namespace Demos {

    /// <summary>
    ///     Demo browser entry point.
    /// </summary>
    public class DemoTest {
        [STAThread]
        private static void Main(string[] args) {
            try {
                using(Mutex mutex = new Mutex(false, "AxiomDemoBrowser")) {
                    if(!mutex.WaitOne(0, false)) {
                        Environment.Exit(-1);
                    }
                    else {
                        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("");
                        Application.Run(new DemoBrowser());
                    }
                }
            }
            catch(Exception ex) {
                // call the existing global exception handler
                TechDemo.GlobalErrorHandler(null, new System.Threading.ThreadExceptionEventArgs(ex));
            }
        }
    }
}