#region MIT/X11 License
//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Utilities;
using D3D9 = SlimDX.Direct3D9;
using ResourceContainer = System.Collections.Generic.List<Axiom.RenderSystems.DirectX9.ID3D9Resource>;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    public class D3D9ResourceManager: ResourceManager
    {
        public enum ResourceCreationPolicy
        {
            CreateOnActiveDevice,
            CreateOnAllDevices
        };

        [OgreVersion(1, 7, 2790)]
        private static readonly object _resourcesMutex = new object();

        [OgreVersion(1, 7, 2790)]
        protected new ResourceContainer Resources = new ResourceContainer();

        [OgreVersion(1, 7, 2790)]
        private int _deviceAccessLockCount;

        [OgreVersion(1, 7, 2790)]
        public ResourceCreationPolicy CreationPolicy 
        { 
            get; 
            set; 
        }

        [OgreVersion( 1, 7, 2790 )]
        public bool AutoHardwareBufferManagement
        {
            get;
            set;
        }

        #region Constructor

        [OgreVersion(1, 7, 2790)]
        public D3D9ResourceManager()
            : base()
        {
            CreationPolicy = ResourceCreationPolicy.CreateOnAllDevices;
        }

        #endregion Constructor

        protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
        {
            throw new NotImplementedException( "Base class needs update to 1.7.2790" );
        }

        #region LockDeviceAccess

        [OgreVersion( 1, 7, 2790 )]
        public void LockDeviceAccess()
        {
            Contract.Requires( _deviceAccessLockCount >= 0 );
            _deviceAccessLockCount++;
            if ( _deviceAccessLockCount == 1 )
            {
#if AXIOM_THREAD_SUPPORT
                System.Threading.Monitor.Enter( _resourcesMutex );
#endif
                foreach ( var it in Resources )
                    it.LockDeviceAccess();

                D3D9HardwarePixelBuffer.LockDeviceAccess();
            }
        }

        #endregion LockDeviceAccess

        #region UnlockDeviceAccess

        [OgreVersion( 1, 7, 2790 )]
        public void UnlockDeviceAccess()
        {
            Contract.Requires( _deviceAccessLockCount > 0 );
            _deviceAccessLockCount--;
            if ( _deviceAccessLockCount == 0 )
            {
                // outermost recursive lock release, propagte unlock
                foreach ( var it in Resources )
                    it.UnlockDeviceAccess();

                D3D9HardwarePixelBuffer.UnlockDeviceAccess();
#if AXIOM_THREAD_SUPPORT
                System.Threading.Monitor.Exit( _resourcesMutex );
#endif
            }
        }

        #endregion UnlockDeviceAccess

        #region NotifyOnDeviceCreate

        [OgreVersion( 1, 7, 2790 )]
        public void NotifyOnDeviceCreate( D3D9.Device d3D9Device )
        {
            lock ( _resourcesMutex )
            {
                foreach ( var it in Resources )
                    it.NotifyOnDeviceCreate( d3D9Device );
            }
        }

        #endregion NotifyOnDeviceCreate

        #region NotifyOnDeviceDestroy

        [OgreVersion( 1, 7, 2790 )]
        public void NotifyOnDeviceDestroy( D3D9.Device d3D9Device )
        {
            lock ( _resourcesMutex )
            {
                foreach ( var it in Resources )
                    it.NotifyOnDeviceDestroy( d3D9Device );
            }
        }

        #endregion NotifyOnDeviceDestroy

        #region NotifyOnDeviceLost

        [OgreVersion( 1, 7, 2790 )]
        public void NotifyOnDeviceLost( D3D9.Device d3D9Device )
        {
            lock ( _resourcesMutex )
            {
                foreach ( var it in Resources )
                    it.NotifyOnDeviceLost( d3D9Device );
            }
        }

        #endregion NotifyOnDeviceLost

        #region NotifyOnDeviceReset

        [OgreVersion( 1, 7, 2790 )]
        public void NotifyOnDeviceReset( D3D9.Device d3D9Device )
        {
            lock ( _resourcesMutex )
            {
                foreach ( var it in Resources )
                    it.NotifyOnDeviceReset( d3D9Device );
            }
        }

        #endregion NotifyOnDeviceReset

        #region NotifyResourceCreated

        [OgreVersion( 1, 7, 2790 )]
        public void NotifyResourceCreated( ID3D9Resource pResource )
        {
            lock ( _resourcesMutex )
            {
                Resources.Add( pResource );
            }
        }

        #endregion NotifyResourceCreated

        #region NotifyResourceDestroyed

        [OgreVersion( 1, 7, 2790 )]
        public void NotifyResourceDestroyed( ID3D9Resource pResource )
        {
            lock ( _resourcesMutex )
            {
                if ( Resources.Contains( pResource ) )
                    Resources.Remove( pResource );
            }
        }

        #endregion NotifyResourceDestroyed
    };
}