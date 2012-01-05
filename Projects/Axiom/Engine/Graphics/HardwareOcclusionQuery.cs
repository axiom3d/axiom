#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///		Interface specification for hardware queries that can be used to find the number
	///		of fragments rendered by the last render operation.
	/// </summary>
	/// Original Author: Lee Sandberg.
	abstract public class HardwareOcclusionQuery : IDisposable
	{
		/// <summary>
		/// Let's you get the last pixel count with out doing the hardware occlusion test
		/// </summary>
		/// <remarks>
		/// This function won't give you new values, just the old value.
		/// </remarks>
		public int LastFragmentCount { get; protected set; }

		/// <summary>
		/// Starts the hardware occlusion query
		/// </summary>
		abstract public void Begin();

		/// <summary>
		/// Ends the hardware occlusion test
		/// </summary>
		abstract public void End();

		/// <summary>
		/// Pulls the hardware occlusion query.
		/// </summary>
		/// <remarks>
		/// Waits until the query result is available; use <see cref="IsStillOutstanding"/>
		/// if just want to test if the result is available.
		/// </remarks>
		/// <returns>the resulting number of fragments.</returns>
		abstract public int PullResults();

		/// <summary>
		/// Lets you know when query is done, or still be processed by the Hardware
		/// </summary>
		/// <returns>true if query isn't finished.</returns>
		abstract public bool IsStillOutstanding();

		#region IDisposable Implementation

		~HardwareOcclusionQuery()
		{
			dispose( false );
		}

		#region isDisposed Property

		private bool _disposed = false;

		/// <summary>
		/// Determines if this instance has been disposed of already.
		/// </summary>
		protected bool isDisposed { get { return _disposed; } set { _disposed = value; } }

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
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		virtual protected void dispose( bool disposeManagedResources )
		{
			if( !isDisposed )
			{
				if( disposeManagedResources )
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
