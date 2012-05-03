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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	///<summary>
	///  Interface specification for hardware queries that can be used to find the number of fragments rendered by the last render operation.
	///</summary>
	///Original Author: Lee Sandberg.
	public abstract class HardwareOcclusionQuery : DisposableObject
	{
		/// <summary>
		///   is query hasn't yet returned a result.
		/// </summary>
		[OgreVersion( 1, 7, 2 )] protected bool isQueryResultStillOutstanding;

		/// <summary>
		///   Let's you get the last pixel count with out doing the hardware occlusion test
		/// </summary>
		/// <remarks>
		///   This function won't give you new values, just the old value.
		/// </remarks>
		[OgreVersion( 1, 7, 2, "getLastQuerysPixelcount() / mPixelCount" )]
		public int LastFragmentCount { get; protected set; }

		/// <summary>
		///   Starts the hardware occlusion query
		/// </summary>
		[OgreVersion( 1, 7, 2, "beginOcclusionQuery" )]
		public abstract void Begin();

		/// <summary>
		///   Ends the hardware occlusion test
		/// </summary>
		[OgreVersion( 1, 7, 2, "endOcclusionQuery" )]
		public abstract void End();

		/// <summary>
		///   Pulls the hardware occlusion query.
		/// </summary>
		/// <remarks>
		///   Waits until the query result is available; use <see cref="IsStillOutstanding" /> if just want to test if the result is available.
		/// </remarks>
		/// <param name="NumOfFragments"> NumOfFragments will get the resulting number of fragments. </param>
		/// <returns> True if success or false if not. </returns>
		[OgreVersion( 1, 7, 2, "pullOcclusionQuery" )]
		public abstract bool PullResults( out int NumOfFragments );

		/// <summary>
		///   Lets you know when query is done, or still be processed by the Hardware
		/// </summary>
		/// <returns> true if query isn't finished. </returns>
		[OgreVersion( 1, 7, 2 )]
		public abstract bool IsStillOutstanding();

		#region IDisposable Implementation

		///<summary>
		///  Class level dispose method
		///</summary>
		///<remarks>
		///  When implementing this method in an inherited class the following template should be used; protected override void dispose( bool disposeManagedResources ) { if ( !isDisposed ) { if ( disposeManagedResources ) { // Dispose managed resources. } // There are no unmanaged resources to release, but // if we add them, they need to be released here. } // If it is available, make the call to the // base class's Dispose(Boolean) method base.dispose( disposeManagedResources ); }
		///</remarks>
		///<param name="disposeManagedResources"> True if Unmanaged resources should be released. </param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	};
}