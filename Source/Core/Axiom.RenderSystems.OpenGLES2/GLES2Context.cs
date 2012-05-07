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

#region SVN Version Information

// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: GLESContext.cs 2173 2010-09-09 15:27:05Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	/// <summary>
	///   Class that encapsulates an GL context. (IE a window/pbuffer). This is a virtual base class which should be implemented in a GLES2Support. This object can also be used to cache renderstate if we decide to do so in the future.
	/// </summary>
	[OgreVersion( 1, 8, 0, "It's from trunk rev.'b0d2092773fb'" )]
	public abstract class GLES2Context
	{
		protected bool _initialized;

		/// <summary>
		/// </summary>
		public GLES2Context()
		{
			this._initialized = false;
		}

		/// <summary>
		/// </summary>
		public bool IsInitialized
		{
			get { return this._initialized; }
			set { this._initialized = value; }
		}

		/// <summary>
		///   Enable the context. All subsequent rendering commands will go here.
		/// </summary>
		public abstract void SetCurrent();

		/// <summary>
		///   This is called before another context is made current. By default, nothing is done here.
		/// </summary>
		public abstract void EndCurrent();

		/// <summary>
		///   Create a new context based on the same window/pbuffer as this context - mostly useful for additional threads.
		/// </summary>
		/// <note>The caller is responsible for deleting the returned context.</note>
		/// <returns> Cloned GLESContext </returns>
		public abstract GLES2Context Clone();

		/// <summary>
		///   Release the render context
		/// </summary>
		public virtual void ReleaseContext() {}

		/// <summary>
		/// </summary>
		public abstract void Dispose();
	}
}
