using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using Axiom.Core.Collections;

namespace Axiom.SceneManagers.PortalConnected
{
    public class PCZAxisAlignedBoxSceneQuery : DefaultAxisAlignedBoxRegionSceneQuery
    {

        private PCZone startZone;
        private SceneNode excludeNode;
        private ulong queryTypeMask;

        /// <summary>
        ///     Creates a custom PCZ AAB query
        /// </summary>
        /// <param name="creator">
        ///     The SceneManager that creates the query.
        /// </param>
        public PCZAxisAlignedBoxSceneQuery(SceneManager creator)
            : base(creator)
        {
            startZone = null;
            excludeNode = null;
        }

        /// <summary>
        ///     Finds any entities that intersect the AAB for the query.
        /// </summary>
        /// <param name="listener">
        ///     The listener to call when we find the results.
        /// </param>
        public override void Execute(ISceneQueryListener listener)
        {
            List<PCZSceneNode> list = new List<PCZSceneNode>();
            //find the nodes that intersect the AAB
            ((PCZSceneManager)creator).FindNodesIn(box, ref list, startZone, (PCZSceneNode)excludeNode);

            //grab all moveables from the node that intersect...

            foreach (PCZSceneNode node in list)
            {
                foreach (MovableObject m in node.Objects)
                {
                    if ((m.QueryFlags & queryMask) != 0 &&
                        (m.TypeFlags & queryTypeMask) != 0 &&
                        m.IsAttached &&
                        box.Intersects(m.GetWorldBoundingBox()))
                    {

                        listener.OnQueryResult(m);
                        // deal with attached objects, since they are not directly attached to nodes
                        if (m.MovableType == "Entity")
                        {
                            //Check: not sure here...
                            Entity e = (Entity)m;
                            foreach (MovableObject c in e.SubEntities)
                            {
                                if ((c.QueryFlags & queryMask) > 0)
                                {
                                    listener.OnQueryResult(c);
                                }
                            }
                        }
                    }

                }
            }
            // reset startzone and exclude node
            startZone = null;
            excludeNode = null;
        }
    }

    public class PCZIntersectionSceneQuery : DefaultIntersectionSceneQuery
    {
        private ulong queryTypeMask;

        public PCZIntersectionSceneQuery(SceneManager creator)
            : base(creator)
        {
        }

        public override void Execute(IIntersectionSceneQueryListener listener)
        {
            Dictionary<MovableObject, MovableObject> set = new Dictionary<MovableObject, MovableObject>();

            // Iterate over all movable types
            foreach (Core.MovableObjectFactory factory in Root.Instance.MovableObjectFactories.Values)
            {
                MovableObjectCollection col = creator.GetMovableObjectCollection(factory.Type);
                foreach (MovableObject e in col)
                {
                    PCZone zone = ((PCZSceneNode)(e.ParentSceneNode)).HomeZone;
                    List<PCZSceneNode> list = new List<PCZSceneNode>();
                    //find the nodes that intersect the AAB
                    ((PCZSceneManager)creator).FindNodesIn(e.GetWorldBoundingBox(), ref list, zone, null);
                    //grab all moveables from the node that intersect...
                    foreach (PCZSceneNode node in list)
                    {
                        foreach (MovableObject m in node.Objects)
                        {
                           // MovableObject m = 
                            if (m != e &&
                                !set.ContainsKey(m) &&
                                !set.ContainsKey(e) &&
                                (m.QueryFlags & queryMask) != 0 &&
                                (m.TypeFlags & queryTypeMask) != 0 &&
                                m.IsAttached &&
                                e.GetWorldBoundingBox().Intersects(m.GetWorldBoundingBox()))
                            {
                                listener.OnQueryResult(e, m);
                                // deal with attached objects, since they are not directly attached to nodes
                                if (m.MovableType == "Entity")
                                {
                                    Entity e2 = (Entity)m;
                                    foreach (MovableObject c in e2.SubEntities)
                                    {
                                        if ((c.QueryFlags & queryMask) != 0 &&
                                            e.GetWorldBoundingBox().Intersects(c.GetWorldBoundingBox()))
                                        {
                                            listener.OnQueryResult(e, c);
                                        }
                                    }
                                }
                            }
                            set.Add(e, m);

                        }
                    }

                }
            }
        }
    }

    public class PCZSphereSceneQuery : DefaultSphereRegionSceneQuery
    {
        private ulong queryTypeMask;
        private PCZone startZone;
        private SceneNode excludeNode;

        protected internal PCZSphereSceneQuery(SceneManager creator)
            : base(creator)
        {
        }

        public override void Execute(ISceneQueryListener listener)
        {
            List<PCZSceneNode> list = new List<PCZSceneNode>();
            //find the nodes that intersect the AAB
            ((PCZSceneManager)creator).FindNodesIn(sphere, ref list, startZone, (PCZSceneNode)excludeNode);

            //grab all moveables from the node that intersect...

            foreach (PCZSceneNode node in list)
            {
                foreach (MovableObject m in node.Objects)
                {
                    if ((m.QueryFlags & queryMask) != 0 &&
                        (m.TypeFlags & queryTypeMask) != 0 &&
                        m.IsAttached &&
                        sphere.Intersects(m.GetWorldBoundingBox()))
                    {

                        listener.OnQueryResult(m);
                        // deal with attached objects, since they are not directly attached to nodes
                        if (m.MovableType == "Entity")
                        {
                            //Check: not sure here...
                            Entity e = (Entity)m;
                            foreach (MovableObject c in e.SubEntities)
                            {
                                if ((c.QueryFlags & queryMask) > 0)
                                {
                                    listener.OnQueryResult(c);
                                }
                            }
                        }
                    }

                }
            }
            // reset startzone and exclude node
            startZone = null;
            excludeNode = null;
        }
    
    }

    public class PCZRaySceneQuery : DefaultRaySceneQuery
    {
        private ulong queryTypeMask;
        private PCZone startZone;
        private SceneNode excludeNode;

        protected internal PCZRaySceneQuery(SceneManager creator) : base(creator)
        {
        }

        public override void Execute(IRaySceneQueryListener listener)
        {
            List<PCZSceneNode> list = new List<PCZSceneNode>();
            //find the nodes that intersect the AAB
            ((PCZSceneManager)creator).FindNodesIn(ray, ref list, startZone, (PCZSceneNode)excludeNode);

            //grab all moveables from the node that intersect...

            foreach (PCZSceneNode node in list)
            {
                foreach (MovableObject m in node.Objects)
                {
                    if ( (m.QueryFlags & queryMask) != 0 &&
                         (m.TypeFlags & queryTypeMask) != 0 &&
                         m.IsAttached )
                    {
                        IntersectResult result = ray.Intersects(m.GetWorldBoundingBox());
                        if (result.Hit)
                        {
                            listener.OnQueryResult(m, result.Distance);
                            // deal with attached objects, since they are not directly attached to nodes
                            if (m.MovableType == "Entity")
                            {
                                //Check: not sure here...
                                Entity e = (Entity) m;
                                foreach (MovableObject c in e.SubEntities)
                                {
                                    if ((c.QueryFlags & queryMask) > 0)
                                    {
                                        result = ray.Intersects(c.GetWorldBoundingBox());
                                        if (result.Hit)
                                        {
                                            listener.OnQueryResult(c, result.Distance);
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
            // reset startzone and exclude node
            startZone = null;
            excludeNode = null;
        }

        public PCZone StartZone
        {
            get
            {
                return startZone;
            }
            set
            {
                startZone = value;
            }
        }

        public SceneNode ExcludeNode
        {
            get
            {
                return excludeNode;
            }

            set
            {
                excludeNode = value;
            }
        }
    }

    public class PCZPlaneBoundedVolumeListSceneQuery : DefaultPlaneBoundedVolumeListSceneQuery
    {
        private ulong queryTypeMask;
        private PCZone startZone;
        private SceneNode excludeNode;

        protected internal PCZPlaneBoundedVolumeListSceneQuery(SceneManager creator) : base(creator)
        {
        }

        public override void Execute(ISceneQueryListener listener)
        {
            List<PCZSceneNode> list = new List<PCZSceneNode>();
            List<PCZSceneNode> checkedNodes = new List<PCZSceneNode>();

            foreach (PlaneBoundedVolume volume in volumes)
            {
                //find the nodes that intersect the AAB
                ((PCZSceneManager)creator).FindNodesIn(volume, ref list, startZone, (PCZSceneNode)excludeNode);

                //grab all moveables from the node that intersect...
                foreach (PCZSceneNode node in list)
                {
                    // avoid double-check same scene node
                    if (!checkedNodes.Contains(node))
                        continue;

                    checkedNodes.Add(node);

                    foreach (MovableObject m in node.Objects)
                    {
                        if ((m.QueryFlags & queryMask) != 0 &&
                            (m.TypeFlags & queryTypeMask) != 0 &&
                            m.IsAttached && volume.Intersects(m.GetWorldBoundingBox()))
                        {
                            listener.OnQueryResult(m);
                            // deal with attached objects, since they are not directly attached to nodes
                            if (m.MovableType == "Entity")
                            {
                                //Check: not sure here...
                                Entity e = (Entity)m;
                                foreach (MovableObject c in e.SubEntities)
                                {
                                    if ((c.QueryFlags & queryMask) > 0 && 
                                        volume.Intersects(c.GetWorldBoundingBox()))
                                    {
                                        listener.OnQueryResult(c);
                                    }
                                }
                            }
                        }

                    }
                }
            }
            // reset startzone and exclude node
            startZone = null;
            excludeNode = null;
        }
    }
}
