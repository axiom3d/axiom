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

using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;

#endregion Namespace Declarations


namespace Axiom.SceneManagers.PortalConnected
{
    public abstract class PCZoneFactory
    {
        /// Factory type name
        protected string _factoryTypeName;

        public PCZoneFactory(string typeName)
        {
            _factoryTypeName = typeName;
        }

        public abstract bool SupportsPCZoneType(string zoneType);
        public abstract PCZone CreatePCZone(PCZSceneManager pczsm, string zoneName);

        public string FactoryTypeName
        {
            get
            {
                return _factoryTypeName;
            }
        }
    }

    public class DefaultZoneFactory : PCZoneFactory
    {
        public DefaultZoneFactory(string typeName)
            : base(typeName)
        {
            _factoryTypeName = typeName;
        }

        public override bool SupportsPCZoneType(string zoneType)
        {
            return zoneType == _factoryTypeName;
        }

        public override PCZone CreatePCZone(PCZSceneManager pczsm, string zoneName)
        {
            return new DefaultZone(pczsm, zoneName);
        }
    }

    public class PCZoneFactoryManager
    {
        private static PCZoneFactoryManager _instance;
        private Dictionary<string, PCZoneFactory> _pCZoneFactories = new Dictionary<string, PCZoneFactory>();
        private DefaultZoneFactory _defaultFactory = new DefaultZoneFactory("ZoneType_Default");

        private PCZoneFactoryManager()
        {
            RegisterPCZoneFactory(_defaultFactory);
        }

        public static PCZoneFactoryManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PCZoneFactoryManager();

                return _instance;
            }
        }

        public void RegisterPCZoneFactory(PCZoneFactory factory)
        {
            String name = factory.FactoryTypeName;
            _pCZoneFactories.Add(name, factory);
            LogManager.Instance.Write("PCZone Factory Type '" + name + "' registered");
        }

        public void UnregisterPCZoneFactory(PCZoneFactory factory)
        {
            if (null != factory)
            {
                //find and remove factory from mPCZoneFactories
                // Note that this does not free the factory from memory, just removes from the factory manager
                string name = factory.FactoryTypeName;
                if (_pCZoneFactories.ContainsKey(name))
                {
                    _pCZoneFactories.Remove(name);
                    LogManager.Instance.Write("PCZone Factory Type '" + name + "' unregistered");
                }
            }
        }


        public PCZone CreatePCZone(PCZSceneManager pczsm, string zoneType, string zoneName)
        {
            //find a factory that supports this zone type and then call createPCZone() on it
            PCZone inst = null;
            foreach (PCZoneFactory factory in _pCZoneFactories.Values)
            {
                if (factory.SupportsPCZoneType(zoneType))
                {
                    // use this factory
                    inst = factory.CreatePCZone(pczsm, zoneName);
                }
            }
            if (null == inst)
            {
                // Error!
                throw new AxiomException("No factory found for zone of type '" + zoneType +
                                         "' PCZoneFactoryManager.CreatePCZone");
            }
            return inst;
        }
    }
}