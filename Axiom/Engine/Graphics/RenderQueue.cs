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
using System.Collections;
using System.Diagnostics;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;

namespace Axiom.Graphics {
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
    public class RenderQueue {
        #region Member variables

        /// <summary>Cached list of render groups, indexed by RenderQueueGroupID</summary>
        protected SortedList renderGroups = new SortedList();
        /// <summary>Default render group for this queue.</summary>
        protected RenderQueueGroupID defaultGroup;
        /// <summary>Default priority of items added to the render queue.</summary>
        public const int DEFAULT_PRIORITY = 100;

        #endregion

        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public RenderQueue() {
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
        public RenderQueueGroupID DefaultRenderGroup {
            get { 
				return defaultGroup; 
			}
            set { 
				defaultGroup = value; 
			}
        }

        /// <summary>
        ///    Gets the number of render queue groups contained within this queue.
        /// </summary>
        public int NumRenderQueueGroups {
            get {
                return renderGroups.Count;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        ///		Adds a renderable item to the queue.
        /// </summary>
        /// <param name="item">IRenderable object to add to the queue.</param>
        /// <param name="groupID">Group to add the item to.</param>
        /// <param name="priority"></param>
        public void AddRenderable(IRenderable renderable, ushort priority, RenderQueueGroupID groupID) {
            RenderQueueGroup group = GetQueueGroup(groupID);

            // let the material know it has been used, which also forces a recompile if required
            if(renderable.Material != null) {
                renderable.Material.Touch();
            }

            // add the renderable to the appropriate group
            group.AddRenderable(renderable, priority);
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="groupID"></param>
        public void AddRenderable(IRenderable item, ushort priority) {
            AddRenderable(item, priority, defaultGroup);
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="item"></param>
        public void AddRenderable(IRenderable item) {
            AddRenderable(item, DEFAULT_PRIORITY);
        }

        /// <summary>
        ///		Clears all 
        /// </summary>
        public void Clear() {
            // loop through each queue and clear it's items.  We don't wanna clear the group
            // list because it probably won't change frame by frame.
            for(int i = 0; i < renderGroups.Count; i++) {
                RenderQueueGroup group = (RenderQueueGroup)renderGroups.GetByIndex(i);

                // clear the RenderQueueGroup
                group.Clear();
            }

			// trigger the pending pass updates
			Pass.ProcessPendingUpdates();
        }

		/// <summary>
		///		Get a render queue group.
		/// </summary>
		/// <remarks>
		///		New queue groups are registered as they are requested, 
		///		therefore this method will always return a valid group.
		/// </remarks>
		/// <param name="queueID">ID of the queue group to retreive.</param>
		/// <returns></returns>
		public RenderQueueGroup GetQueueGroup(RenderQueueGroupID queueID) {
			RenderQueueGroup group = null;

			// see if there is a current queue group for this group id
			if(renderGroups[queueID] == null) {
				// create a new queue group for this group id
				group = new RenderQueueGroup();

				// add the new group to cached render group
				renderGroups.Add(queueID, group);
			}
			else {
				// retreive the existing queue group
				group = (RenderQueueGroup)renderGroups[queueID];
			}

			return group;
		}

        /// <summary>
        ///    
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal RenderQueueGroup GetQueueGroupByIndex(int index) {
            Debug.Assert(index < renderGroups.Count, "index < renderGroups.Count");

            return (RenderQueueGroup)renderGroups.GetByIndex(index);
        }

        internal RenderQueueGroupID GetRenderQueueGroupID(int index) {
            Debug.Assert(index < renderGroups.Count, "index < renderGroups.Count");

            return (RenderQueueGroupID)renderGroups.GetKey(index);
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
    public class RenderQueueGroup {
        #region Fields

        protected HashList priorityGroups = new HashList();

		/// <summary>
		///		Are shadows enabled for this group?
		/// </summary>
		protected bool shadowsEnabled;

        #endregion Fields

        #region Constructor

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public RenderQueueGroup() {
			// shadows enabled by default
			shadowsEnabled = true;
		}

        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="priority"></param>
        public void AddRenderable(IRenderable item, ushort priority) {
            RenderPriorityGroup group = null;

            // see if there is a current queue group for this group id
            if(!priorityGroups.ContainsKey(priority)) {
                // create a new queue group for this group id
                group = new RenderPriorityGroup();

                // add the new group to cached render group
                priorityGroups.Add(priority, group);
            }
            else {
                // retreive the existing queue group
                group = (RenderPriorityGroup)priorityGroups.GetByKey(priority);
            }

            // add the renderable to the appropriate group
            group.AddRenderable(item);			
        }

        /// <summary>
        ///		Clears all the priority groups within this group.
        /// </summary>
        public void Clear() {
            // loop through each priority group and clear it's items.  We don't wanna clear the group
            // list because it probably won't change frame by frame.
            for(int i = 0; i < priorityGroups.Count; i++) {
                RenderPriorityGroup group = (RenderPriorityGroup)priorityGroups[i];

                // clear the RenderPriorityGroup
                group.Clear();
            }
        }

        /// <summary>
        ///    Gets the hashlist entry for the priority group at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public RenderPriorityGroup GetPriorityGroup(int index) {
            Debug.Assert(index < priorityGroups.Count, "index < priorityGroups.Count");

            return (RenderPriorityGroup)priorityGroups[index];
        }

        #endregion

        #region Properties

        /// <summary>
        ///    Gets the number of priority groups within this queue group.
        /// </summary>
        public int NumPriorityGroups {
            get {
                return priorityGroups.Count;
            }
        }

		/// <summary>
		///		Indicate whether a given queue group will be doing any shadow setup.
		/// </summary>
		/// <remarks>
		///		This method allows you to inform the queue about a queue group, and to 
		///		indicate whether this group will require shadow processing of any sort.
		///		In order to preserve rendering order, Axiom/Ogre has to treat queue groups
		///		as very separate elements of the scene, and this can result in it
		///		having to duplicate shadow setup for each group. Therefore, if you
		///		know that a group which you are using will never need shadows, you
		///		should preregister the group using this method in order to improve
		///		the performance.
		/// </remarks>
		public bool ShadowsEnabled {
			get {
				return shadowsEnabled;
			}
			set {
				shadowsEnabled = value;
			}
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
    public class RenderPriorityGroup {
        #region Member variables
			
        protected internal ArrayList transparentPasses = new ArrayList();
        protected internal SortedList solidPassMap;

        #endregion

        /// <summary>
        ///    Default constructor.
        /// </summary>
        internal RenderPriorityGroup() {
            // sorted list, using Pass as a key (sorted based on hashcode), and IRenderable as the value
            solidPassMap = new SortedList(new SolidSort(), 50);
        }

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void AddRenderable(IRenderable item) {
            Technique t = null;
                
            // Check material & technique supplied (the former since the default implementation
            // of getTechnique is based on it for backwards compatibility
            if(item.Material == null || item.Technique == null) {
                // use default if not found
                t = MaterialManager.Instance.GetByName("BaseWhite").GetTechnique(0);
            }
            else {
                t = item.Technique;
            }

            // loop through each pass and queue it up
            if(t.IsTransparent) {
                for(int i = 0; i < t.NumPasses; i++) {
                    // add to transparent list
                    transparentPasses.Add(new RenderablePass(item, t.GetPass(i)));
                }
            }
            else {
                for(int i = 0; i < t.NumPasses; i++) {
                    Pass pass = t.GetPass(i);

                    if(solidPassMap[pass] == null) {
                        // add a new list to hold renderables for this pass
                        solidPassMap.Add(pass, new RenderableList());
                    }

                    // add to solid list for this pass
                    RenderableList solidList = (RenderableList)solidPassMap[pass];
                    solidList.Add(item);
                }
            }
        }

        /// <summary>
        ///		Clears all the internal lists.
        /// </summary>
        public void Clear() {
			PassList graveyardList = Pass.GraveyardList;

			// Delete queue groups which are using passes which are to be
			// deleted, we won't need these any more and they clutter up 
			// the list and can cause problems with future clones
			for(int i = 0; i < graveyardList.Count; i++) {
				RemoveSolidPassEntry((Pass)graveyardList[i]);
			}

			// Now remove any dirty passes, these will have their hashes recalculated
			// by the parent queue after all groups have been processed
			// If we don't do this, the std::map will become inconsistent for new insterts
			PassList dirtyList = Pass.DirtyList;

			// Delete queue groups which are using passes which are to be
			// deleted, we won't need these any more and they clutter up 
			// the list and can cause problems with future clones
			for(int i = 0; i < dirtyList.Count; i++) {
				RemoveSolidPassEntry((Pass)dirtyList[i]);
			}
           
			// We do NOT clear the graveyard or the dirty list here, because 
			// it needs to be acted on for all groups, the parent queue takes 
			// care of this afterwards

			// We do not clear the unchanged solid pass maps, only the contents of each list
			// This is because we assume passes are reused a lot and it saves resorting
            ClearSolidPassMap(solidPassMap);
			// TODO: Additional maps

			// Always empty the transparents list
			transparentPasses.Clear();
        }

		public void ClearSolidPassMap(SortedList list) {
			// loop through and clear the renderable containers for the stored passes
			for(int i = 0; i < list.Count; i++) {
				((RenderableList)list.GetByIndex(i)).Clear();
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Pass GetSolidPass(int index) {
            Debug.Assert(index < solidPassMap.Count, "index < solidPasses.Count");
            return (Pass)solidPassMap.GetKey(index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public RenderableList GetSolidPassRenderables(int index) {
            Debug.Assert(index < solidPassMap.Count, "index < solidPasses.Count");
            return (RenderableList)solidPassMap.GetByIndex(index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public RenderablePass GetTransparentPass(int index) {
            Debug.Assert(index < transparentPasses.Count, "index < transparentPasses.Count");
            return (RenderablePass)transparentPasses[index];
        }

        /// <summary>
        ///    Sorts the objects which have been added to the queue; transparent objects by their 
        ///    depth in relation to the passed in Camera, solid objects in order to minimize
        ///    render state changes.
        /// </summary>
        /// <remarks>
        ///    Solid passes are already stored in a sorted structure, so nothing extra needed here.
        /// </remarks>
        /// <param name="camera">Current camera to use for depth sorting.</param>
        public void Sort(Camera camera) {
            // sort the transparent objects using the custom IComparer
            transparentPasses.Sort(new TransparencySort(camera));
        }

		/// <summary>
		///		Remove a pass entry from all solid pass maps
		/// </summary>
		/// <param name="pass">Reference to the pass to remove.</param>
		public void RemoveSolidPassEntry(Pass pass) {
			if(solidPassMap[pass] != null) {
				solidPassMap.Remove(pass);
			}

			// TODO: Additional solid pass lists
		}

        #endregion

        #region Properties
            
        /// <summary>
        ///    Gets the number of non-transparent passes for this priority group.
        /// </summary>
        public int NumSolidPasses {
            get {
                return solidPassMap.Count;
            }
        }

        /// <summary>
        ///    Gets the number of transparent passes for this priority group.
        /// </summary>
        public int NumTransparentPasses {
            get {
                return transparentPasses.Count;
            }
        }

        #endregion

        #region Internal classes

        /// <summary>
        /// 
        /// </summary>
        class SolidSort : IComparer {
            #region IComparer Members

            public int Compare(object x, object y) {
                // TODO: Should these ever be null?
                if(x == null  || y == null)
                    return 0;

                // if they are the same, return 0
                if(x == y)
                    return 0;

                Pass a = x as Pass;
                Pass b = y as Pass;

                // sorting by pass hash
                if(a.GetHashCode() < b.GetHashCode()) {
                    return -1;
                }
                else {
                    return 1;
                }
            }

            #endregion            
        }

        /// <summary>
        ///		Nested class that implements IComparer for transparency sorting.
        /// </summary>
        class TransparencySort : IComparer {
            private Camera camera;

            public TransparencySort(Camera camera) {
                this.camera = camera;
            }

            #region IComparer Members

            public int Compare(object x, object y) {
                // TODO: Should these ever be null?
                if(x == null  || y == null)
                    return 0;

                // if they are the same, return 0
                if(x == y)
                    return 0;

                RenderablePass a = x as RenderablePass;
                RenderablePass b = y as RenderablePass;

                float adepth = a.renderable.GetSquaredViewDepth(camera);
                float bdepth = b.renderable.GetSquaredViewDepth(camera);

                if(adepth == bdepth) {
                    if(a.pass.GetHashCode() < b.pass.GetHashCode()) {
                        return 1;
                    }
                    else {
                        return -1;
                    }
                }
                else {
                    // sort descending by depth, meaning further objects get drawn first
                    if(adepth > bdepth)
                        return 1;
                    else
                        return -1;
                }
            }

            #endregion
        }

        #endregion
    }

    /// <summary>
    ///    Internal structure reflecting a single Pass for a Renderable
    /// </summary>
    public class RenderablePass {
        public IRenderable renderable;
        public Pass pass;

        public RenderablePass(IRenderable renderable, Pass pass) {
            this.renderable = renderable;
            this.pass = pass;
        }
    }
}
