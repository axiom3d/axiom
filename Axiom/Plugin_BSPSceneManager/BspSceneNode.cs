#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;

using Axiom;
using Axiom.Core;

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
		public BspSceneNode(SceneManager creator) : base(creator)
		{
		}

		public BspSceneNode(SceneManager creator, string name) : base(creator, name)
		{
		}
		#endregion

		#region Methods
		protected override void Update(bool updateChildren, bool parentHasChanged)
		{
			bool checkMovables = false;
	
			if(this.needParentUpdate || this.needParentUpdate)
				checkMovables = true;

			base.Update(updateChildren, parentHasChanged);

			if(checkMovables)
			{
				for(int i = 0; i < this.objectList.Count; i++)
					((BspSceneManager) this.Creator).NotifyObjectMoved(this.objectList[i], this.DerivedPosition);
			}
		}

		/// <summary>
		///		Detaches the indexed object from this scene node.
		///	</summary>
		///	<remarks>
		///		Detaches by index, see the alternate version to detach by name. Object indexes
		///		may change as other objects are added / removed.
		/// </remarks>
		public SceneObject DetachObject(ushort index)
		{
			SceneObject ret = this.objectList[index];
			
			base.DetachObject(index);
			((BspSceneManager) this.Creator).NotifyObjectDetached(ret);
		
			return ret;
		}

		public SceneObject DetachObject(string name)
		{
			SceneObject ret = this.objectList[name];
			
			base.DetachObject(ret);
			((BspSceneManager) this.Creator).NotifyObjectDetached(ret);

			return ret;
		}

		public new void DetachAllObjects()
		{
			BspSceneManager mgr = (BspSceneManager) this.Creator;

			for(int i = 0; i < this.objectList.Count; i++)
				mgr.NotifyObjectDetached(this.objectList[i]);

			base.DetachAllObjects();
		}
		#endregion
	}
}