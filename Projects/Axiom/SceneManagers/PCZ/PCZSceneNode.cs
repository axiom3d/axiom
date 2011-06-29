#region MIT/X11 License
//Copyright (c) 2009 Axiom 3D Rendering Engine Project
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
// <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
// <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom;
using Axiom.Core;
using Axiom.Math;
using Axiom.Collections;
using Axiom.Graphics;

using System.Collections.Generic;
#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{

    //ORIGINAL LINE: class _OgrePCZPluginExport PCZSceneNode : public SceneNode
    public class PCZSceneNode : SceneNode
    {

        /// <summary>
        /// name generator
        /// </summary>
        private static NameGenerator<PCZSceneNode> _nameGenerator = new NameGenerator<PCZSceneNode>("PCZSceneNode");

        protected Vector3 NewPosition = Vector3.Zero;
        protected PCZone mHomeZone = null;
        public bool Anchored = false;
        public bool AllowToVisit = true;
        public bool AllowedToVisit = true;
        protected Dictionary<string, PCZone> VisitingZones = new Dictionary<string, PCZone>();
        public Vector3 PrevPosition = Vector3.Zero;
        public ulong LastVisibleFrame = 0;
        public PCZCamera LastVisibleFromCamera;
        protected Dictionary<string, ZoneData> ZoneData = new Dictionary<string, ZoneData>();
        public bool Enabled = true;
        public bool Moved = false;

        //* Standard constructor 
        public PCZSceneNode(SceneManager creator)
            : this(creator, _nameGenerator.GetNextUniqueName())
        {
        }

        //* Standard constructor 
        public PCZSceneNode(SceneManager creator, string name)
            : base(creator, name)
        {

        }
        //* Standard destructor 
        ~PCZSceneNode()
        {
            // clear visiting zones list
            VisitingZones.Clear();
            ZoneData.Clear();
            base.Dispose();
        }

        protected override void Update(bool updateChildren, bool parentHasChanged)
        {
            base.Update(updateChildren, parentHasChanged);
            if (base.Parent != null) // skip bound update if it's root scene node. Saves a lot of CPU.
                UpdateBounds();

            PrevPosition = NewPosition;
            NewPosition = DerivedPosition;
        }

        //ORIGINAL LINE: void updateFromParentImpl() const
        public void updateFromParentImpl()
        {
            base.UpdateFromParent();
            Moved = true;
        }

        //    * Creates an unnamed new SceneNode as a child of this node.
        //        translate Initial translation offset of child relative to parent
        //        rotate Initial rotation relative to parent
        public SceneNode CreateChildSceneNode(Vector3 inTranslate)
        {
            return CreateChildSceneNode(inTranslate, Quaternion.Identity);
        }

        public SceneNode CreateChildSceneNode()
        {
            return CreateChildSceneNode(Vector3.Zero, Quaternion.Identity);
        }

        //ORIGINAL LINE: SceneNode* createChildSceneNode(const Vector3& inTranslate = Vector3::ZERO, const Quaternion& inRotate = Quaternion::IDENTITY)
        public SceneNode CreateChildSceneNode(Vector3 inTranslate, Quaternion inRotate)
        {
            PCZSceneNode childSceneNode = (PCZSceneNode)(this.CreateChild(inTranslate, inRotate));
            if (HomeZone != null)
            {
                childSceneNode.HomeZone = HomeZone;
                HomeZone.AddNode(childSceneNode);
            }
            return (SceneNode)(childSceneNode);
        }

        //    * Creates a new named SceneNode as a child of this node.
        //        This creates a child node with a given name, which allows you to look the node up from 
        //        the parent which holds this collection of nodes.
        //            translate Initial translation offset of child relative to parent
        //            rotate Initial rotation relative to parent
        public SceneNode CreateChildSceneNode(string name, Vector3 inTranslate)
        {
            return CreateChildSceneNode(name, inTranslate, Quaternion.Identity);
        }

        public SceneNode CreateChildSceneNode(string name)
        {
            return CreateChildSceneNode(name, Vector3.Zero, Quaternion.Identity);
        }

        //ORIGINAL LINE: SceneNode* createChildSceneNode(const string& name, const Vector3& inTranslate = Vector3::ZERO, const Quaternion& inRotate = Quaternion::IDENTITY)
        public SceneNode CreateChildSceneNode(string name, Vector3 inTranslate, Quaternion inRotate)
        {
            PCZSceneNode childSceneNode = (PCZSceneNode)(this.CreateChild(name, inTranslate, inRotate));
            if (HomeZone != null)
            {
                childSceneNode.HomeZone = HomeZone;
                HomeZone.AddNode(childSceneNode);
            }
            return (SceneNode)(childSceneNode);
        }

        public PCZone HomeZone
        {
            get
            {
                return mHomeZone;
            }
            set
            {
                // if the new home zone is different than the current, remove
                // the node from the current home zone's list of home nodes first
                if (value != mHomeZone && mHomeZone != null)
                {
                    mHomeZone.RemoveNode(this);
                }
                mHomeZone = value;
            }
        }

        public void AnchorToHomeZone(PCZone zone)
        {
            mHomeZone = zone;
            if (zone != null)
            {
                Anchored = true;
            }
            else
            {
                Anchored = false;
            }
        }


        public void AddZoneToVisitingZonesMap(PCZone zone)
        {
            VisitingZones[zone.Name] = zone;
        }

        public void ClearVisitingZonesMap()
        {
            VisitingZones.Clear();
        }

        // The following function does the following:
        // * 1) Remove references to the node from zones the node is visiting
        // * 2) Clear the node's list of zones it is visiting
        public void ClearNodeFromVisitedZones()
        {
            if (VisitingZones.Count > 0)
            {
                // first go through the list of zones this node is visiting
                // and remove references to this node

                foreach (KeyValuePair<string, PCZone> kvp in VisitingZones)
                {
                    PCZone zone = kvp.Value;
                    zone.RemoveNode(this);
                }

                // second, clear the visiting zones list
                VisitingZones.Clear();

            }
        }

        // Remove all references that the node has to the given zone
        //	
        public void RemoveReferencesToZone(PCZone zone)
        {
            if (HomeZone == zone)
            {
                HomeZone = null;
            }

            if (VisitingZones.ContainsKey(zone.Name))
            {
                VisitingZones.Remove(zone.Name);

            }

        }

        // returns true if zone is in the node's visiting zones map
        //   false otherwise.
        //	
        public bool IsVisitingZone(PCZone zone)
        {
            return VisitingZones.ContainsKey(zone.Name);
        }

        //* Adds the attached objects of this PCZSceneNode into the queue. 
        public void AddToRenderQueue(Camera cam, RenderQueue queue, bool onlyShadowCasters, VisibleObjectsBoundsInfo visibleBounds)
        {
            foreach (MovableObject mo in objectList)
            {

                mo.NotifyCurrentCamera(cam);
                if (mo.IsVisible && (!onlyShadowCasters || mo.CastShadows))
                {
                    mo.UpdateRenderQueue(queue);

                    //TODO: Check this
                    //if (visibleBounds != null)
                    //{
                    visibleBounds.Merge(mo.GetWorldBoundingBox(true), mo.GetWorldBoundingSphere(true), cam);
                    //}
                }
            }
        }

        //    * Save the node's current position as the previous position
        //	
        public void SavePrevPosition()
        {
            PrevPosition = DerivedPosition;
        }

        public void SetZoneData(PCZone zone, ZoneData zoneData)
        {

            // first make sure that the data doesn't already exist
            if (ZoneData.ContainsKey(zone.Name))
            {
                throw new AxiomException("A ZoneData associated with zone " + zone.Name + " already exists", "PCZSceneNode::setZoneData");
            }
            ZoneData[zone.Name] = zoneData;
        }

        // get zone data for this node for given zone
        // NOTE: This routine assumes that the zone data is present!
        public ZoneData GetZoneData(PCZone zone)
        {
            if (ZoneData.ContainsKey(zone.Name))
            {
                return ZoneData[zone.Name];
            }
            else
            {
                return null;
            }
        }

        // update zone-specific data for any zone that the node is touching
        public void UpdateZoneData()
        {
            ZoneData zoneData;
            PCZone zone;
            // make sure home zone data is updated
            zone = HomeZone;
            if (zone.RequiresZoneSpecificNodeData)
            {
                zoneData = GetZoneData(zone);
                //TODO: check this to get it to run but it should have something here right?
                if (zoneData != null)
                {
                    zoneData.Update();
                }
            }

        }
        public void Enable(bool yesno)
        {
            Enabled = yesno;
        }
        public bool IsEnabled()
        {
            return Enabled;
        }

    }
}
