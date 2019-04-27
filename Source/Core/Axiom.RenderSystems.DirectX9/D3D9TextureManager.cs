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

using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;
using D3D9 = SharpDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    public class D3D9TextureManager : TextureManager
    {
        [OgreVersion(1, 7, 2)]
        public D3D9TextureManager()
            : base()
        {
            // register with group manager
            ResourceGroupManager.Instance.RegisterResourceManager(ResourceType, this);
        }

        [OgreVersion(1, 7, 2, "~D3D9TextureManager")]
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    // unregister with group manager
                    ResourceGroupManager.Instance.UnregisterResourceManager(ResourceType);
                }
            }

            base.dispose(disposeManagedResources);
        }

        [OgreVersion(1, 7, 2)]
        protected override Resource _create(string name, ulong handle, string group, bool isManual,
                                             IManualResourceLoader loader, NameValuePairList createParams)
        {
            return new D3D9Texture(this, name, handle, group, isManual, loader);
        }

        // This ends up just discarding the format passed in; the C# methods don't let you supply
        // a "recommended" format.  Ah well.
        [OgreVersion(1, 7, 2)]
        public override Axiom.Media.PixelFormat GetNativeFormat(TextureType ttype, PixelFormat format, TextureUsage usage)
        {
            // Basic filtering
            var d3dPF = D3D9Helper.ConvertEnum(D3D9Helper.GetClosestSupported(format));

            // Calculate usage
            var d3dusage = D3D9.Usage.None;
            var pool = D3D9.Pool.Managed;
            if ((usage & TextureUsage.RenderTarget) != 0)
            {
                d3dusage |= D3D9.Usage.RenderTarget;
                pool = D3D9.Pool.Default;
            }
            if ((usage & TextureUsage.Dynamic) != 0)
            {
                d3dusage |= D3D9.Usage.Dynamic;
                pool = D3D9.Pool.Default;
            }

            var curDevice = D3D9RenderSystem.ActiveD3D9Device;

            // Use D3DX to adjust pixel format
            switch (ttype)
            {
                case TextureType.OneD:
                case TextureType.TwoD:
                    var tReqs = D3D9.Texture.CheckRequirements(curDevice, 0, 0, 0, d3dusage, D3D9Helper.ConvertEnum(format), pool);
                    d3dPF = tReqs.Format;
                    break;

                case TextureType.ThreeD:
                    var volReqs = D3D9.VolumeTexture.CheckRequirements(curDevice, 0, 0, 0, 0, d3dusage,
                                                                        D3D9Helper.ConvertEnum(format), pool);
                    d3dPF = volReqs.Format;
                    break;

                case TextureType.CubeMap:
                    var cubeReqs = D3D9.CubeTexture.CheckRequirements(curDevice, 0, 0, d3dusage, D3D9Helper.ConvertEnum(format),
                                                                       pool);
                    d3dPF = cubeReqs.Format;
                    break;
            }

            return D3D9Helper.ConvertEnum(d3dPF);
        }

        /// <see cref="Axiom.Core.TextureManager.IsHardwareFilteringSupported(TextureType, PixelFormat, TextureUsage, bool)"/>
        [OgreVersion(1, 7, 2)]
        public override bool IsHardwareFilteringSupported(TextureType ttype, PixelFormat format, TextureUsage usage,
                                                           bool preciseFormatOnly)
        {
            if (!preciseFormatOnly)
            {
                format = GetNativeFormat(ttype, format, usage);
            }

            var rs = D3D9RenderSystem.Instance;
            return rs.CheckTextureFilteringSupported(ttype, format, usage);
        }
    };
}