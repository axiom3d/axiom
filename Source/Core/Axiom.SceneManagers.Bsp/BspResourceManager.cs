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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;
using Axiom.Collections;
using ResourceHandle = System.UInt64;
using System.IO;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Bsp
{
	/// <summary>
	///		Manages the locating and loading of BSP-based indoor levels.
	/// </summary>
	/// <remarks>
	///		Like other ResourceManager specialisations it manages the location and loading
	///		of a specific type of resource, in this case files containing Binary
	///		Space Partition (BSP) based level files e.g. Quake3 levels.</p>
	///		However, note that unlike other ResourceManager implementations,
	///		only 1 BspLevel resource is allowed to be loaded at one time. Loading
	///		another automatically unloads the currently loaded level if any.
	/// </remarks>
	public class BspResourceManager : ResourceManager, ISingleton<BspResourceManager>
	{
		#region Fields and Properties

		protected Quake3ShaderManager shaderManager;

		#endregion Fields and Properties

		#region Methods

		/// <summary>
		///		Loads a BSP-based level from the named file.  Currently only supports loading of Quake3 .bsp files.
		/// </summary>
		public BspLevel Load( Stream stream, string group )
		{
			RemoveAll();

			var bsp = (BspLevel)Create( "bsplevel", ResourceGroupManager.Instance.WorldResourceGroupName, true, null, null );
			bsp.Load( stream );

			return bsp;
		}

		#endregion Methods

		#region ISingleton<BspResourceManager> Implementation

		/// <summary>
		/// 
		/// </summary>
		protected static BspResourceManager instance;

		/// <summary>
		/// 
		/// </summary>
		public static BspResourceManager Instance
		{
			get
			{
				return instance;
			}
		}

		internal BspResourceManager()
			: base()
		{
			if ( instance == null )
			{
				instance = this;
				ResourceType = "BspLevel";
				this.shaderManager = new Quake3ShaderManager();
				ResourceGroupManager.Instance.RegisterResourceManager( ResourceType, this );
			}
			else
			{
				throw new AxiomException( "Cannot create another instance of {0}. Use Instance property instead", GetType().Name );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool Initialize( params object[] args )
		{
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposeManagedResources"></param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					this.shaderManager.Dispose();
					ResourceGroupManager.Instance.UnregisterResourceManager( "BspLevel" );
					instance = null;
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion ISingleton<BspResourceManager> Implementation

		#region ResourceManager Implementation

		#region Load

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <param name="isManual"></param>
		/// <param name="loader"></param>
		/// <param name="loadParams"></param>
		/// <returns></returns>
		public override Resource Load( string name, string group, bool isManual, IManualResourceLoader loader, NameValuePairList loadParams, bool backgroundThread )
		{
			RemoveAll(); // Only one level at a time.

			return base.Load( name, group, isManual, loader, loadParams, backgroundThread );
		}

		#endregion Load

		/// <summary>
		///		Creates a BspLevel resource - mainly used internally.
		/// </summary>
		protected override Resource _create( string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
		{
			return new BspLevel( this, name, handle, group, isManual, loader, createParams );
		}

		#endregion ResourceManager Implementation
	}
}