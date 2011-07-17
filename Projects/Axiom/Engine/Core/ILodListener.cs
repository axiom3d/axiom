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
//     <id value="$Id: ILodListener.cs 1762 2009-09-13 19:00:22Z bostich $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Struct containing information about a lod change event for movable objects.
	/// </summary>
	public struct MovableObjectLodChangedEvent
	{
		/// <summary>
		/// The movable object whose level of detail has changed.
		/// </summary>
		public MovableObject MovableObject;
		/// <summary>
		/// The camera with respect to which the level of detail has changed.
		/// </summary>
		public Camera camera;
	}

	/// <summary>
	/// Struct containing information about a mesh lod change event for entities.
	/// </summary>
	public struct EntityMeshLodChangedEvent
	{
		/// <summary>
		/// The entity whose level of detail has changed.
		/// </summary>
		public Entity Entity;
		/// <summary>
		/// The camera with respect to which the level of detail has changed.
		/// </summary>
		public Camera Camera;
		/// <summary>
		/// Lod value as determined by lod strategy.
		/// </summary>
		public float LodValue;
		/// <summary>
		/// Previous level of detail index.
		/// </summary>
		public int PreviousLodIndex;
		/// <summary>
		/// New level of detail index.
		/// </summary>
		public int NewLodIndex;
	}

	/// <summary>
	/// Struct containing information about a material lod change event for entities.
	/// </summary>
	public struct EntityMaterialLodChangedEvent
	{
		/// <summary>
		/// The sub-entity whose material's level of detail has changed.
		/// </summary>
		public SubEntity SubEntity;
		/// <summary>
		/// The camera with respect to which the level of detail has changed.
		/// </summary>
		public Camera Camera;
		/// <summary>
		/// Lod value as determined by lod strategy.
		/// </summary>
		public float LodValue;
		/// <summary>
		/// Previous level of detail index.
		/// </summary>
		public int PreviousLodIndex;
		/// <summary>
		/// New level of detail index.
		/// </summary>
		public int NewLodIndex;
	}

	/// <summary>
	/// A interface class defining a listener which can be used to receive
	/// notifications of lod events.
	/// </summary>
	/// <remarks>
	/// A 'listener' is an interface designed to be called back when
	/// particular events are called. This class defines the
	/// interface relating to lod events. In order to receive
	/// notifications of lod events, you should create a subclass of
	/// LodListener and override the methods for which you would like
	/// to customise the resulting processing. You should then call
	/// SceneManager::addLodListener passing an instance of this class.
	/// There is no limit to the number of lod listeners you can register,
	/// allowing you to register multiple listeners for different purposes.
	///
	/// For some uses, it may be advantageous to also subclass
	/// <seealso name="RenderQueueListener"/> as this interface makes available information
	/// regarding render queue invocations.
	///
	/// It is important not to modify the scene graph during rendering, so,
	/// for each event, there are two methods, a prequeue method and a
	/// postqueue method.  The prequeue method is invoked during rendering,
	/// and as such should not perform any changes, but if the event is
	/// relevant, it may return true indicating the postqueue method should
	/// also be called.  The postqueue method is invoked at an appropriate
	/// time after rendering and scene changes may be safely made there.
	/// </remarks>
	public interface ILodListener
	{
		/// <summary>
		/// Called before a movable object's lod has changed.
		/// </summary>
		/// <remarks>
		/// Do not change the Axiom state from this method,
		/// instead return true and perform changes in
		/// PostqueueMovableObjectLodChanged.
		/// </remarks>
		/// <param name="evt"></param>
		/// <returns>True to indicate the event should be queued and
		/// PostqueueMovableObjectLodChanged called after
		/// rendering is complete.</returns>
		bool PrequeueMovableObjectLodChanged( MovableObjectLodChangedEvent evt );

		/// <summary>
		/// Called after a movable object's lod has changed.
		/// </summary>
		/// <remarks>
		/// May be called even if not requested from PrequeueMovableObjectLodChanged
		/// as only one event queue is maintained per SceneManger instance.
		/// </remarks>
		/// <param name="evt"></param>
		/// <returns></returns>
		bool PostqueueMovableObjectLodChanged( MovableObjectLodChangedEvent evt );

		/// <summary>
		/// Called before an entity's mesh lod has changed.
		/// </summary>
		/// <remarks>
		/// Do not change the Axiom state from this method,
		/// instead return true and perform changes in
		/// PostqueueEntityMeshLodChanged.
		///
		/// It is possible to change the event notification
		/// and even alter the newLodIndex field (possibly to
		/// prevent the lod from changing, or to skip an
		/// index).
		/// </remarks>
		/// <param name="evt"></param>
		/// <returns>True to indicate the event should be queued and
		/// PostqueueEntityMeshLodChanged called after
		/// rendering is complete.</returns>
		bool PrequeueEntityMeshLodChanged( EntityMeshLodChangedEvent evt );

		/// <summary>
		/// Called after an entity's mesh lod has changed.
		/// </summary>
		/// <remarks>
		/// May be called even if not requested from PrequeueEntityMeshLodChanged
		/// as only one event queue is maintained per SceneManger instance.
		/// </remarks>
		/// <param name="evt"></param>
		/// <returns></returns>
		bool PostqueueEntityMeshLodChanged( EntityMeshLodChangedEvent evt );

		/// <summary>
		/// Called before an entity's material lod has changed.
		/// </summary>
		/// <remarks>
		/// Do not change the Axiom state from this method,
		/// instead return true and perform changes in
		/// PostqueueMaterialLodChanged.
		///
		/// It is possible to change the event notification
		/// and even alter the newLodIndex field (possibly to
		/// prevent the lod from changing, or to skip an
		/// index).
		/// <returns>
		/// True to indicate the event should be queued and
		/// PostqueueMaterialLodChanged called after
		/// rendering is complete.
		/// </returns>
		/// </remarks>
		/// <param name="evt"></param>
		bool PrequeueEntityMaterialLodChanged( EntityMaterialLodChangedEvent evt );

		/// <summary>
		///  Called after an entity's material lod has changed.
		/// </summary>
		/// <remarks>
		/// May be called even if not requested from PrequeueEntityMaterialLodChanged
		/// as only one event queue is maintained per SceneManger instance.
		/// </remarks>
		/// <param name="evt"></param>
		/// <returns></returns>
		bool PostqueueEntityMaterialLodChanged( EntityMaterialLodChangedEvent evt );
	}
}