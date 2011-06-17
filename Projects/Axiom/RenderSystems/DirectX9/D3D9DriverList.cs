using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axiom.Core;
using SlimDX.Direct3D9;

namespace Axiom.RenderSystems.DirectX9
{
    public class D3D9DriverList : DisposableObject, IEnumerable<Driver>
    {
        #region _driverList

        [OgreVersion(1, 7, 2790)]
        private readonly List<Driver> _driverList = new List<Driver>();

        #endregion

        #region Constructor

        [OgreVersion(1, 7, 2790)]
        public D3D9DriverList()
        {
            Enumerate();
        }

        #endregion

        #region dispose

        [OgreVersion(1, 7, 2790)]
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
                _driverList.Clear();
        }

        #endregion

        #region Enumerate

        [OgreVersion(1, 7, 2790, "Update D3DDriver constructor")]
        public bool Enumerate()
        {
            var d3D = D3DRenderSystem.Direct3D9;

            LogManager.Instance.Write( "D3D9: Driver Detection Starts" );
            for (var iAdaptor = 0; iAdaptor < d3D.AdapterCount; iAdaptor++)
            {
                
                var adapterIdentifier = d3D.GetAdapterIdentifier( iAdaptor );
                var d3Ddm = d3D.GetAdapterDisplayMode( iAdaptor );
                var d3Dcaps9 = d3D.GetDeviceCaps( iAdaptor, DeviceType.Hardware );

                _driverList.Add(new Driver(iAdaptor, d3Dcaps9, adapterIdentifier, d3Ddm));
            }

            LogManager.Instance.Write( "D3D9: Driver Detection Ends" );
            return true;
        }

        #endregion

        #region Count

        [OgreVersion(1, 7, 2790)]
        public int Count
        {
            get
            {
                return _driverList.Count;
            }
        }

        #endregion

        #region Indexer

        [OgreVersion(1, 7, 2790)]
        public Driver this[int index]
        {
            get
            {
                return _driverList[ index ];
            }
        }

        [OgreVersion(1, 7, 2790)]
        public Driver this[string name]
        {
            get
            {
                return _driverList.FirstOrDefault(d => d.DriverDescription == name);
            }
        }

        #endregion

        #region IEnumerable

        [AxiomHelper(0, 8)]
        public IEnumerator<Driver> GetEnumerator()
        {
            return _driverList.GetEnumerator();
        }

        [AxiomHelper(0, 8)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
