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

namespace Axiom.Framework.Configuration
{
	public class ConfigurationManagerFactory
	{
		public static IConfigurationManager CreateDefault()
		{
			var platform = Environment.OSVersion.Platform;
			switch ( platform )
			{
				case PlatformID.Xbox:
					return new XBoxConfigurationManager();
#if !(XBOX || XBOX360 || WINDOWS_PHONE)
				case PlatformID.MacOSX:
#endif
				case PlatformID.Unix:
#if SILVERLIGHT && WINDOWS_PHONE
				case PlatformID.NokiaS60:
#endif
				case PlatformID.WinCE:
					return new XBoxConfigurationManager();
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				default:
#if !(XBOX || XBOX360 || WINDOWS_PHONE || SILVERLIGHT || ANDROID)
					return new DefaultConfigurationManager();
#else
					return null;
#endif
			}
		}
	}
}