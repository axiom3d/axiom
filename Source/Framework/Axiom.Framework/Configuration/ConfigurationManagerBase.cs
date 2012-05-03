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

using Axiom.Core;

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class ConfigurationManagerBase : IConfigurationManager
	{
		#region Fields and Properties

		/// <summary>
		/// 
		/// </summary>
		protected string ConfigurationFile { get; set; }

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		/// 
		/// </summary>
		/// <param name="configurationFilename"></param>
		protected ConfigurationManagerBase( string configurationFilename )
		{
			ConfigurationFile = configurationFilename;
		}

		#endregion Construction and Destruction

		#region IConfigurationManager Implementation

		/// <summary>
		/// 
		/// </summary>
		public virtual string LogFilename { get; protected set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		public abstract bool RestoreConfiguration( Root engine );


		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		public abstract void SaveConfiguration( Root engine );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="defaultRenderer"></param>
		public abstract void SaveConfiguration( Root engine, string defaultRenderer );

		/// <summary>
		/// 
		/// </summary>
		public abstract bool ShowConfigDialog( Root engine );

		#endregion IConfigurationManager Implementation
	}
}