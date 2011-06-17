using System;
using System.Threading;
using SlimDX.Direct3D9;

namespace Axiom.RenderSystems.DirectX9
{
    public class D3D9Resource
    {
        internal static readonly object DeviceAccessMutex = new object();

        public static void LockDeviceAccess()
        {
            Monitor.Enter( DeviceAccessMutex );
        }

        public static void UnlockDeviceAccess()
        {
            Monitor.Exit( DeviceAccessMutex );
        }

        public virtual void NotifyOnDeviceCreate( Device d3D9Device )
        {
        }

        public virtual void NotifyOnDeviceLost( Device d3D9Device )
        {
        }

        public virtual void NotifyOnDeviceReset(Device d3D9Device)
        {
        }

        public virtual void NotifyOnDeviceDestroy(Device d3D9Device)
        {
        }
    }
}