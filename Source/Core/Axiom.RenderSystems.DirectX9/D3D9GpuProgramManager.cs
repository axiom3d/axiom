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
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// Summary description for D3DGpuProgramManager.
    /// </summary>
    public class D3D9GpuProgramManager : GpuProgramManager
    {
        [OgreVersion(1, 7, 2)]
        internal D3D9GpuProgramManager()
            : base()
        {
            // Superclass sets up members 

            // Register with resource group manager
            ResourceGroupManager.Instance.RegisterResourceManager(ResourceType, this);
        }

        [OgreVersion(1, 7, 2, "~D3D9GpuProgramManager")]
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    // Unregister with resource group manager
                    ResourceGroupManager.Instance.UnregisterResourceManager(ResourceType);
                }
            }

            base.dispose(disposeManagedResources);
        }

        #region GpuProgramManager Implementation

        /// <see cref="Axiom.Core.ResourceManager._create(string, ResourceHandle, string, bool, IManualResourceLoader, NameValuePairList)"/>
        [OgreVersion(1, 7, 2)]
        protected override Resource _create(string name, ResourceHandle handle, string group, bool isManual,
                                             IManualResourceLoader loader, NameValuePairList createParams)
        {
            if (createParams == null || !createParams.ContainsKey("type"))
            {
                throw new AxiomException("You must supply a 'type' parameter.");
            }

            if (createParams["type"] == "vertex_program")
            {
                return new D3D9GpuVertexProgram(this, name, handle, group, isManual, loader);
            }
            else
            {
                return new D3D9GpuFragmentProgram(this, name, handle, group, isManual, loader);
            }
        }

        /// <summary>
        /// Specialised create method with specific parameters
        /// </summary>
        [OgreVersion(1, 7, 2)]
        protected override Resource _create(string name, ResourceHandle handle, string group, bool isManual,
                                             IManualResourceLoader loader, GpuProgramType type, string syntaxCode)
        {
            if (type == GpuProgramType.Vertex)
            {
                return new D3D9GpuVertexProgram(this, name, handle, group, isManual, loader);
            }
            else
            {
                return new D3D9GpuFragmentProgram(this, name, handle, group, isManual, loader);
            }
        }

        #endregion GpuProgramManager Implementation
    };
}