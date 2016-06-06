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
	internal class XBoxConfigurationManager : ConfigurationManagerBase
	{
		#region Fields and Properties

		public static string DefaultLogFileName = "axiom.log";

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		/// 
		/// </summary>
		public XBoxConfigurationManager()
			: base( DefaultLogFileName )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="configurationFile"></param>
		public XBoxConfigurationManager( string configurationFile )
			: base( configurationFile )
		{
		}

		#endregion Construction and Destruction

		#region ConfigurationManagerBase Implementation

		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		/// <returns></returns>
		public override bool RestoreConfiguration( Core.Root engine )
		{
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		public override void SaveConfiguration( Core.Root engine )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="defaultRenderer"></param>
		public override void SaveConfiguration( Core.Root engine, string defaultRenderer )
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		/// <returns></returns>
		public override bool ShowConfigDialog( Core.Root engine )
		{
			return true;
		}

		#endregion ConfigurationManagerBase Implementation
	}
}