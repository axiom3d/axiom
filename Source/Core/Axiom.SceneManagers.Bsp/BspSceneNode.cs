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

using Axiom;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Bsp
{
	/// <summary>
	///		Specialisation of <see cref="Axiom.Core.SceneNode"/> for the <see cref="Plugin_BSPSceneManager.BspSceneManager"/>.
	///	</summarny>
	/// <remarks>
	///		This specialisation of <see cref="Axiom.Core.SceneNode"/> is to enable information about the
	///		leaf node in which any attached objects are held is stored for
	///		use in the visibility determination.
	///		<p/>
	///		Do not confuse this class with <see cref="Plugin_BSPSceneManager.BspNode"/>, which reflects nodes in the
	///		BSP tree itself. This class is just like a regular <see cref="Axiom.Core.SceneNode"/>, except that
	///		it should be locating <see cref="Plugin_BSPSceneManager.BspNode"/> leaf elements which objects should be included
	///		in. Note that because objects are movable, and thus may very well be overlapping
	///		the boundaries of more than one leaf, that it is possible that an object attached
	///		to one <see cref="Plugin_BSPSceneManager.BspSceneNode"/> may actually be associated with more than one BspNode.
	/// </remarks>
	public class BspSceneNode : SceneNode
	{
		#region Constructors

		public BspSceneNode( SceneManager creator )
			: base( creator ) {}

		public BspSceneNode( SceneManager creator, string name )
			: base( creator, name ) {}

		#endregion Constructors

		#region Methods

		protected override void Update( bool updateChildren, bool parentHasChanged )
		{
			bool checkMovables = false;

			//needChildUpdate is more appropriate than needParentUpdate. needParentUpdate
			//is set to false when there is a DerivedPosition/DerivedOrientation.
			if ( this.needChildUpdate || parentHasChanged )
			{
				checkMovables = true;
			}

			base.Update( updateChildren, parentHasChanged );

			if ( checkMovables )
			{
				foreach ( MovableObject obj in this.objectList.Values )
				{
					if ( obj is TextureLight )
					{
						// the notification of BspSceneManager when the position of
						// the light is changed, is taken care of at TextureLight.Update()
						continue;
					}
					( (BspSceneManager)this.Creator ).NotifyObjectMoved( obj, this.DerivedPosition );
				}
			}
		}

		public override void DetachObject( MovableObject obj )
		{
			// TextureLights are detached only when removed at the BspSceneManager
			if ( !( obj is TextureLight ) )
			{
				( (BspSceneManager)this.Creator ).NotifyObjectDetached( obj );
			}

			base.DetachObject( obj );
		}

		public override void DetachAllObjects()
		{
			BspSceneManager mgr = (BspSceneManager)this.Creator;

			foreach ( MovableObject obj in this.objectList.Values )
			{
				// TextureLights are detached only when removed at the BspSceneManager
				if ( obj is TextureLight )
				{
					continue;
				}

				mgr.NotifyObjectDetached( obj );
			}

			base.DetachAllObjects();
		}

		#endregion Methods
	}
}
