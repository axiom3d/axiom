using System;
using System.Threading;
using Axiom.Core;
using SlimDX.Direct3D9;

namespace Axiom.RenderSystems.DirectX9
{
    [OgreVersion(1, 7, 2790)]
    public class D3D9Resource: DisposableObject
    {
        #region DeviceAccessMutex

        [OgreVersion(1, 7, 2790)]
        internal static readonly object DeviceAccessMutex = new object();

        #endregion

        #region Constructor

        [OgreVersion(1, 7, 2790)]
        public D3D9Resource()
        {
            D3DRenderSystem.ResourceManager.NotifyResourceCreated(this);	
        }

        #endregion

        #region dispose

        [OgreVersion(1, 7, 2790)]
        protected override void dispose(bool disposeManagedResources)
        {
            D3DRenderSystem.ResourceManager.NotifyResourceDestroyed(this);
        }

        #endregion

        #region LockDeviceAccess

        /// <summary>
        /// Called when device state is changing. Access to any device should be locked.
        /// Relevant for multi thread application.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public static void LockDeviceAccess()
        {
            Monitor.Enter( DeviceAccessMutex );
        }

        #endregion

        #region UnlockDeviceAccess

        /// <summary>
        /// Called when device state change completed. Access to any device is allowed.
        /// Relevant for multi thread application.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public static void UnlockDeviceAccess()
        {
            Monitor.Exit( DeviceAccessMutex );
        }

        #endregion

        #region NotifyOnDeviceCreate

        /// <summary>
        /// Called immediately after the Direct3D device has been created.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual void NotifyOnDeviceCreate( Device d3D9Device )
        {
        }

        #endregion

        #region NotifyOnDeviceDestroy

        /// <summary>
        /// Called before the Direct3D device is going to be destroyed.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual void NotifyOnDeviceDestroy(Device d3D9Device)
        {
        }

        #endregion

        #region NotifyOnDeviceLost

        /// <summary>
        /// Called immediately after the Direct3D device has entered a lost state.
        /// This is the place to release non-managed resources.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual void NotifyOnDeviceLost( Device d3D9Device )
        {
        }

        #endregion

        #region NotifyOnDeviceReset

        /// <summary>
        /// Called immediately after the Direct3D device has been reset.
        /// This is the place to create non-managed resources.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public virtual void NotifyOnDeviceReset(Device d3D9Device)
        {
        }

        #endregion
    }
}