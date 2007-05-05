#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

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

#region SVN Version Information
// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Class that encapsulates an GL context. (IE a window/pbuffer). This is a 
	/// virtual base class which should be implemented in a GLSupport.
	/// This object can also be used to cache renderstate if we decide to do so
	/// in the future.
	/// </summary>
	internal abstract class GLContext : IDisposable
	{
		#region Fields and Properties

		#region Initialized Property
		private bool _initialized;
		/// <summary>
		/// 
		/// </summary>
		public bool Initialized
		{
			get
			{
				return _initialized;
			}
			set
			{
				_initialized = value;
			}
		}
		#endregion Initialized Property

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLContext()
		{
		}

		~GLContext()
		{
			dispose( true );
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Enable the context. All subsequent rendering commands will go here.
		/// </summary>
        public abstract void SetCurrent();

		/// <summary>
		/// This is called before another context is made current. By default,
		/// nothing is done here.
		/// </summary>
		public virtual void EndCurrent()
		{
		}

        /// <summary>
        /// Create a new context based on the same window/pbuffer as this
		/// context - mostly useful for additional threads.
        /// </summary>
		/// <remarks>
		///	The caller is responsible for deleting the returned context.
		/// </remarks>
        /// <returns></returns>
		public abstract GLContext Clone();

		#endregion Methods

		#region IDisposable Implementation

		#region isDisposed Property

		private bool _disposed = false;
		/// <summary>
		/// Determines if this instance has been disposed of already.
		/// </summary>
		protected bool isDisposed
		{
			get
			{
				return _disposed;
			}
			set
			{
				_disposed = value;
			}
		}

		#endregion isDisposed Property

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		/// 
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		/// 	isDisposed = true;
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected virtual void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			isDisposed = true;
		}

		public void Dispose()
		{
			dispose( true );
			GC.SuppressFinalize( this );
		}

		#endregion IDisposable Implementation

	}
}
