using System;
using System.Collections.Generic;
using System.Threading;
using Axiom.Collections;
using Axiom.Core;
using SlimDX.Direct3D9;
using Resource = Axiom.Core.Resource;
using ResourceManager = Axiom.Core.ResourceManager;

namespace Axiom.RenderSystems.DirectX9
{
    public class D3D9ResourceManager: ResourceManager
    {
        #region inner classes

        protected class ResourceContainer : List<D3D9Resource>
        {
        }

        public enum ResourceCreationPolicy
        {
            CreateOnActiveDevice,
            CreateOnAllDevices
        }

        #endregion

        #region _resourcesMutex

        [OgreVersion(1, 7, 2790)]
        private readonly object _resourcesMutex = new object();

        #endregion

        #region Resources

        [OgreVersion(1, 7, 2790)]
        protected new readonly ResourceContainer Resources = new ResourceContainer();

        #endregion

        #region _deviceAccessLockCount

        [OgreVersion(1, 7, 2790)]
        private int _deviceAccessLockCount;

        #endregion

        #region CreationPolicy

        [OgreVersion(1, 7, 2790)]
        public ResourceCreationPolicy CreationPolicy 
        { 
            get; 
            set; 
        }

        #endregion

        #region Constructor

        [OgreVersion(1, 7, 2790)]
        public D3D9ResourceManager()
            : base()
        {
            CreationPolicy = ResourceCreationPolicy.CreateOnAllDevices;
        }

        #endregion

        #region AutoHardwareBufferManagement

        [OgreVersion(1, 7, 2790)]
        public bool AutoHardwareBufferManagement
        {
            get;
            set;
        }

        #endregion

        protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
        {
            throw new NotImplementedException("Base class needs update to 1.7.2790");
        }

        #region LockDeviceAccess

        [OgreVersion(1, 7, 2790)]
        public void LockDeviceAccess()
        {
            Monitor.Enter( _resourcesMutex );

            _deviceAccessLockCount++;
            if ( _deviceAccessLockCount != 1 )
                return; // recursive lock aquisition (dont propagate)

            D3D9Resource.LockDeviceAccess();
            D3DHardwarePixelBuffer.LockDeviceAccess();
        }

        #endregion

        #region UnlockDeviceAccess

        [OgreVersion(1, 7, 2790)]
        public void UnlockDeviceAccess()
        {
            _deviceAccessLockCount--;
            if (_deviceAccessLockCount == 0)
            {
                // outermost recursive lock release, propagte unlock
                D3D9Resource.UnlockDeviceAccess();
                D3DHardwarePixelBuffer.UnlockDeviceAccess();
            }
            Monitor.Exit( _resourcesMutex );
        }

        #endregion

        #region NotifyOnDeviceCreate

        [OgreVersion(1, 7, 2790)]
        public void NotifyOnDeviceCreate(Device d3D9Device)
        {
            lock ( _resourcesMutex )
            {
                foreach (var it in Resources)
                    it.NotifyOnDeviceCreate( d3D9Device );
            }
        }

        #endregion

        #region NotifyOnDeviceDestroy

        [OgreVersion(1, 7, 2790)]
        public void NotifyOnDeviceDestroy(Device d3D9Device)
        {
            lock (_resourcesMutex)
            {
                foreach (var it in Resources)
                    it.NotifyOnDeviceDestroy(d3D9Device);
            }
        }

        #endregion

        #region NotifyOnDeviceLost

        [OgreVersion(1, 7, 2790)]
        public void NotifyOnDeviceLost(Device d3D9Device)
        {
            lock ( _resourcesMutex )
            {
                foreach (var it in Resources)
                    it.NotifyOnDeviceLost( d3D9Device );
            }
        }

        #endregion

        #region NotifyOnDeviceReset

        [OgreVersion(1, 7, 2790)]
        public void NotifyOnDeviceReset(Device d3D9Device)
        {
            lock (_resourcesMutex)
            {
                foreach (var it in Resources)
                    it.NotifyOnDeviceReset(d3D9Device);
            }
        }

        #endregion

        #region NotifyResourceCreated

        [OgreVersion(1, 7, 2790)]
        public void NotifyResourceCreated( D3D9Resource pResource )
        {
            lock (_resourcesMutex)
            {
                Resources.Add( pResource );
            }
        }

        #endregion

        #region NotifyResourceDestroyed

        [OgreVersion(1, 7, 2790)]
        public void NotifyResourceDestroyed( D3D9Resource pResource )
        {
            lock (_resourcesMutex)
            {
                if (Resources.Contains(pResource))
                    Resources.Remove( pResource );
            }
        }

        #endregion
    }
}