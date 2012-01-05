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

using Axiom.Core;

namespace Axiom.Samples
{
	/// <summary>
	/// Utility class used to hold a set of samples in an Axiom plugin.
	/// </summary>
	public class SamplePlugin : IPlugin
	{
		#region Fields and Properties

		/// <summary>
		/// 
		/// </summary>
		public string Name { get; protected set; }

		/// <summary>
		/// 
		/// </summary>
		public readonly SampleSet Samples = new SampleSet();

		#endregion Fields and Properties

		#region Construction and Destruction

		public SamplePlugin()
		{
			//this ctor is for axiom's plugin manager
			this.Name = string.Empty;
		}

		public SamplePlugin( string name )
		{
			this.Name = name;
		}

		#endregion Construction and Destruction

		#region Methods

		public void AddSample( Sample s )
		{
			Samples.Add( s );
		}

		#endregion Methods

		#region IPlugin Implementation

		virtual public void Initialize() {}

		virtual public void Shutdown() {}

		#endregion
	}
}
