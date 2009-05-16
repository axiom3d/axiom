using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.SceneManagers.PortalConnected;

namespace OctreeZone
{
    public class OctreeZonePlugin : IPlugin
    {
        public void Start()
        {
            mTerrainZoneFactory = new TerrainZoneFactory("ZoneType_Terrain");
            mOctreeZoneFactory = new OctreeZoneFactory("ZoneType_Octree");

            PCZoneFactoryManager.Instance.RegisterPCZoneFactory(mTerrainZoneFactory);
            PCZoneFactoryManager.Instance.RegisterPCZoneFactory(mOctreeZoneFactory);
        }

        public void Stop()
        {
            PCZoneFactoryManager.Instance.UnregisterPCZoneFactory(mOctreeZoneFactory);
            PCZoneFactoryManager.Instance.UnregisterPCZoneFactory(mTerrainZoneFactory);
        }

        OctreeZoneFactory mOctreeZoneFactory;
        TerrainZoneFactory mTerrainZoneFactory;
    }
}
