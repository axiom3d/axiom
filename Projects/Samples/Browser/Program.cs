#region MIT/X11 License

//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.Security.Permissions;
using System.Threading;

using Axiom.Core;
using Axiom.Framework.Exceptions;

namespace Axiom.Samples
{
	internal static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
#if !(XBOX || XBOX360)
		[STAThread]
#endif
		private static void Main()
		{
			try
			{
#if !(XBOX || XBOX360)
				Thread.CurrentThread.CurrentCulture = new CultureInfo( "en-US", false );
				using( SampleBrowser sb = new SampleBrowser() )
#else
				using (SampleBrowser sb = new XBox.SampleBrowser())
#endif
				{
					sb.Go();
				}
			}
			catch( Exception ex )
			{
#if !(XBOX || XBOX360)
				IErrorDialog messageBox = new WinFormErrorDialog();
				messageBox.Show( ex );
#endif
				Debug.WriteLine( LogManager.BuildExceptionString( ex ) );
			}
		}
	}
}
