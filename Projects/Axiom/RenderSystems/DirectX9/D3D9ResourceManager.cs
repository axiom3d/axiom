using System;
using Axiom.Collections;
using Axiom.Core;

namespace Axiom.RenderSystems.DirectX9
{
    internal class D3D9ResourceManager: ResourceManager
    {
        protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
        {
            throw new NotImplementedException();
        }

        public void LockDeviceAccess()
        {
            throw new NotImplementedException();
        }

        public void UnlockDeviceAccess()
        {
            throw new NotImplementedException();
        }
    }
}