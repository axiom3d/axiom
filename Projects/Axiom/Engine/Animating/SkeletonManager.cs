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

using Axiom.Collections;
using Axiom.Core;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Animating
{
	/// <summary>
	/// Summary description for SkeletonManager.
	/// </summary>
	public sealed class SkeletonManager : ResourceManager, ISingleton<SkeletonManager>
	{
		#region ISingleton<SkeletonManager> Implementation

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static SkeletonManager Instance
		{
			get
			{
				return Singleton<SkeletonManager>.Instance;
			}
		}

		/// <summary>
		/// Initializes the Skeleton Manager
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool Initialize( params object[] args )
		{
			return true;
		}

		#endregion ISingleton<SkeletonManager> Implementation

		#region Construction and Destruction

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		public SkeletonManager()
		{
			LoadingOrder = 300.0f;
			ResourceType = "Skeleton";

			ResourceGroupManager.Instance.RegisterResourceManager( ResourceType, this );
		}

		#endregion Construction and Destruction

		#region ResourceManager Implementation

		/// <summary>
		///    Creates a new skeleton object.
		/// </summary>
		protected override Resource _create( string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
		{
			return new Skeleton( this, name, handle, group, isManual, loader );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					ResourceGroupManager.Instance.UnregisterResourceManager( ResourceType );
					Singleton<SkeletonManager>.Destroy();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion ResourceManager Implementation
	}
}
