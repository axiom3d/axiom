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

        private readonly object _resourcesMutex = new object();

        protected new readonly ResourceContainer Resources = new ResourceContainer();

        private int _deviceAccessLockCount;

        public ResourceCreationPolicy CreationPolicy;

        [OgreVersion(1, 7, 2790)]
        public bool AutoHardwareBufferManagement { get; set; }

        protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
        {
            throw new NotImplementedException();
        }

        public void LockDeviceAccess()
        {
            Monitor.Enter( _resourcesMutex );

            _deviceAccessLockCount++;
            if ( _deviceAccessLockCount != 1 )
                return; // recursive lock aquisition (dont propagate)

            D3D9Resource.LockDeviceAccess();
            D3DHardwarePixelBuffer.LockDeviceAccess();
        }

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

        public void NotifyOnDeviceCreate(Device d3d9Device)
        {
            lock ( _resourcesMutex )
            {
                foreach (var it in Resources)
                    it.NotifyOnDeviceCreate( d3d9Device );
            }
        }
    }
}