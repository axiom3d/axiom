#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using System.Collections;
using System.Diagnostics;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.SubSystems.Rendering;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	///		Class to manage the scene object rendering queue.
	/// </summary>
	/// <remarks>
    ///		Objects are grouped by material to minimize rendering state changes. The map from
    ///		material to renderable object is wrapped in a class for ease of use.
    ///		<p/>
    ///		This class includes the concept of 'queue groups' which allows the application
    ///		adding the renderable to specifically schedule it so that it is included in 
    ///		a discrete group. Good for separating renderables into the main scene,
    ///		backgrounds and overlays, and also could be used in the future for more
    ///		complex multipass routines like stenciling.
	/// </remarks>
	// TESTME
	public class RenderQueue
	{
		#region Member variables

		/// <summary>Cached list of render groups, indexed by RenderQueueGroupID</summary>
		protected HashList renderGroups = new HashList();
		/// <summary>Default render group for this queue.</summary>
		protected RenderQueueGroupID defaultGroup;
		/// <summary>Default priority of items added to the render queue.</summary>
		public const int DEFAULT_PRIORITY = 100;

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		public RenderQueue()
		{
			// set the default queue group for this queue
			defaultGroup = RenderQueueGroupID.Main;

			// create the main queue group up front
			renderGroups.Add(RenderQueueGroupID.Main, new RenderQueueGroup());
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets/Sets the default priority for rendering objects in the queue.
		/// </summary>
		public RenderQueueGroupID DefaultRenderGroup
		{
			get { return defaultGroup; }
			set { defaultGroup = value; }
		}

		/// <summary>
		///		
		/// </summary>
		public HashList QueueGroups
		{
			get { return renderGroups; }
		}

		#endregion

		#region Public methods

		/// <summary>
		///		Adds a renderable item to the queue.
		/// </summary>
		/// <param name="item">IRenderable object to add to the queue.</param>
		/// <param name="groupID">Group to add the item to.</param>
		/// <param name="priority"></param>
		public void AddRenderable(IRenderable item, ushort priority, RenderQueueGroupID groupID)
		{
			RenderQueueGroup group = null;

			// see if there is a current queue group for this group id
			if(!renderGroups.ContainsKey(groupID))
			{
				// create a new queue group for this group id
				group = new RenderQueueGroup();

				// add the new group to cached render group
				renderGroups.Add(groupID, group);
			}
			else
			{
				// retreive the existing queue group
				group = (RenderQueueGroup)renderGroups[groupID];
			}

			// add the renderable to the appropriate group
			group.AddRenderable(item, priority);
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="groupID"></param>
		public void AddRenderable(IRenderable item, ushort priority)
		{
			AddRenderable(item, priority, defaultGroup);
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="item"></param>
		public void AddRenderable(IRenderable item)
		{
			AddRenderable(item, DEFAULT_PRIORITY);
		}

		/// <summary>
		///		Clears all 
		/// </summary>
		public void Clear()
		{
			// loop through each queue and clear it's items.  We don't wanna clear the group
			// list because it probably won't change frame by frame.
			for(int i = 0; i < renderGroups.Count; i++)
			{
				RenderQueueGroup group = (RenderQueueGroup)renderGroups[i];

				// clear the RenderQueueGroup
				group.Clear();
			}
		}

		#endregion
	}


	/// <summary>
	///		A grouping level underneath RenderQueue which groups renderables
	///		to be issued at coarsely the same time to the renderer.	
	/// </summary>
	/// <remarks>
	///		Each instance of this class itself hold RenderPriorityGroup instances, 
	///		which are the groupings of renderables by priority for fine control
	///		of ordering (not required for most instances).
	/// </remarks>
	internal class RenderQueueGroup
	{
		#region Member variables 

		protected HashList priorityGroups = new HashList();

		#endregion

		#region Constructor

		/// <summary>
		///		Default constructor.
		/// </summary>
		public RenderQueueGroup() {}

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <param name="priority"></param>
		public void AddRenderable(IRenderable item, ushort priority)
		{
			RenderPriorityGroup group = null;

			// see if there is a current queue group for this group id
			if(!priorityGroups.ContainsKey(priority))
			{
				// create a new queue group for this group id
				group = new RenderPriorityGroup();

				// add the new group to cached render group
				priorityGroups.Add(priority, group);
			}
			else
			{
				// retreive the existing queue group
				group = (RenderPriorityGroup)priorityGroups.GetByKey(priority);
			}

			// add the renderable to the appropriate group
			group.AddRenderable(item);			
		}

		/// <summary>
		///		Clears all the priority groups within this group.
		/// </summary>
		public void Clear()
		{
			// loop through each priority group and clear it's items.  We don't wanna clear the group
			// list because it probably won't change frame by frame.
			for(int i = 0; i < priorityGroups.Count; i++)
			{
				RenderPriorityGroup group = (RenderPriorityGroup)priorityGroups[i];

				// clear the RenderPriorityGroup
				group.Clear();
			}
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets an Enumerator that can be used to iterate through the priority groups.
		/// </summary>
		public HashList PriorityGroups
		{
			get { return priorityGroups; }
		}

		#endregion
	}

	/// <summary>
	///		IRenderables in the queue grouped by priority.
	/// </summary>
	/// <remarks>
	///		This class simply groups renderables for rendering. All the 
	///		renderables contained in this class are destined for the same
	///		RenderQueueGroup (coarse groupings like those between the main
	///		scene and overlays) and have the same priority (fine groupings
	///		for detailed overlap control).
	/// </remarks>
	internal class RenderPriorityGroup
	{
		#region Member variables
			
		protected ArrayList transparentObjectList = new ArrayList();
		/// <summary>List of renderable lists, indexed by material.</summary>
		protected Hashtable materialGroups = new Hashtable();

		#endregion

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		public void AddRenderable(IRenderable item)
		{
			Material material = item.Material;

			Debug.Assert(material != null, "Cannot add a IRenderable to the queue if it has a null Material.");

			// add transparent objects to the transparent object list
			if(material.IsTransparent)
				transparentObjectList.Add(item);
			else
			{
				ArrayList renderableList;
				
				// look material up by name
				// TODO: make sure using Material itself as a key works ok, may need to implement GetHashCode on Material if not
				if(materialGroups.ContainsKey(material))
				{
					// get the existing material group
					renderableList = (ArrayList)materialGroups[material];
				}
				else
				{
					// create a new list for the renderables and add it to the material list
					renderableList = new ArrayList();
					materialGroups.Add(material, renderableList);
				}

				// add the item to the renderable list
				renderableList.Add(item);
			}
		}

		/// <summary>
		///		Clears all the internal lists.
		/// </summary>
		public void Clear()
		{
			materialGroups.Clear();
			transparentObjectList.Clear();
		}

		public void SortTransparentObjects(Camera camera)
		{
			// sort the transparent objects using the custom IComparer
			transparentObjectList.Sort(new TransparencySort(camera));
		}

		#endregion

		#region Properties

		/// <summary>
		/// 
		/// </summary>
		public Hashtable MaterialGroups
		{
			get { return materialGroups; }
		}

		/// <summary>
		/// 
		/// </summary>
		public ArrayList TransparentObjects
		{
			get { return transparentObjectList; }
		}

		#endregion

		/// <summary>
		///		Nested class that implements IComparer for transparency sorting.
		/// </summary>
		class TransparencySort : IComparer
		{
			private Camera camera;

			public TransparencySort(Camera camera)
			{
				this.camera = camera;
			}

			#region IComparer Members

			public int Compare(object x, object y)
			{
				// TODO: Should these ever be null?
				if(x == null  || y == null)
					return 0;

				// if they are the same, return 0
				if(x == y)
					return 0;

				IRenderable a = x as IRenderable;
				IRenderable b = y as IRenderable;

				// sort descending by depth, meaning further objects get drawn first
				if(a.GetSquaredViewDepth(camera) > b.GetSquaredViewDepth(camera))
					return 1;
				else
					return -1;
			}

			#endregion
		}
	}
}
