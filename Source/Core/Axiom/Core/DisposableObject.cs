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

using System;

#endregion Namespace Declarations

namespace Axiom.Core
{
    public abstract class DisposableObject : IDisposable
    {
        [AxiomHelper(0, 9)]
        protected DisposableObject()
        {
            IsDisposed = false;
#if DEBUG
            var stackTrace = string.Empty;
#if !(SILVERLIGHT || XBOX || XBOX360 || WINDOWS_PHONE || ANDROID) && AXIOM_ENABLE_LOG_STACKTRACE
			stackTrace = Environment.StackTrace;
#endif
            ObjectManager.Instance.Add(this, stackTrace);
#endif
        }

        [AxiomHelper(0, 9)]
        ~DisposableObject()
        {
            if (!IsDisposed)
            {
                dispose(false);
            }
        }

        #region IDisposable Implementation

        /// <summary>
        /// Determines if this instance has been disposed of already.
        /// </summary>
        [AxiomHelper(0, 9)]
        public bool IsDisposed { get; set; }

        /// <summary>
        /// Class level dispose method
        /// </summary>
        /// <remarks>
        /// When implementing this method in an inherited class the following template should be used;
        /// protected override void dispose( bool disposeManagedResources )
        /// {
        /// 	if ( !IsDisposed )
        /// 	{
        /// 		if ( disposeManagedResources )
        /// 		{
        /// 			// Dispose managed resources.
        /// 		}
        ///
        /// 		// There are no unmanaged resources to release, but
        /// 		// if we add them, they need to be released here.
        /// 	}
        ///
        /// 	// If it is available, make the call to the
        /// 	// base class's Dispose(Boolean) method
        /// 	base.dispose( disposeManagedResources );
        /// }
        /// </remarks>
        /// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
        [AxiomHelper(0, 9)]
        protected virtual void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    // Dispose managed resources.
#if DEBUG
                    ObjectManager.Instance.Remove(this);
#endif
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            IsDisposed = true;
        }

        [AxiomHelper(0, 9)]
        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Implementation
    };
}