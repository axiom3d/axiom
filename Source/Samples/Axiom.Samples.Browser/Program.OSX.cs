#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
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
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace Axiom.Samples.Browser
{
	class AppDelegate : NSApplicationDelegate
	{
		SampleBrowser sb;

		public override void FinishedLaunching( MonoMac.Foundation.NSObject notification )
		{
			System.IO.Directory.SetCurrentDirectory( System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location ) );
			sb = new SampleBrowser();
			sb.Go();
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed( NSApplication sender )
		{
			return true;
		}
	}

	internal static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main( string[] args )
		{
			NSApplication.Init ();

			using ( var p = new NSAutoreleasePool() ) 
			{
				NSApplication.SharedApplication.Delegate = new AppDelegate();
				NSApplication.Main (args);
			}
		}
	}
}

