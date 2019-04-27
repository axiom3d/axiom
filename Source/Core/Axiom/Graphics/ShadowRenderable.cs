#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    ///		Class which represents the renderable aspects of a set of shadow volume faces.
    /// </summary>
    /// <remarks>
    ///		Note that for casters comprised of more than one set of vertex buffers (e.g. SubMeshes each
    ///		using their own geometry), it will take more than one <see cref="ShadowRenderable"/> to render the
    ///		shadow volume. Therefore for shadow caster geometry, it is best to stick to one set of
    ///		vertex buffers (not necessarily one buffer, but the positions for the entire geometry
    ///		should come from one buffer if possible)
    /// </remarks>
    public abstract class ShadowRenderable : DisposableObject, IRenderable
    {
        #region Fields and Properties

        protected Material material;

        /// <summary>
        ///		Used only if IsLightCapSeparate == true.
        /// </summary>
        protected ShadowRenderable lightCap;

        protected LightList dummyLightList = new LightList();
        protected List<Vector4> customParams = new List<Vector4>();

        /// <summary>
        ///		Does this renderable require a separate light cap?
        /// </summary>
        /// <remarks>
        ///		If possible, the light cap (when required) should be contained in the
        ///		usual geometry of the shadow renderable. However, if for some reason
        ///		the normal depth function (less than) could cause artefacts, then a
        ///		separate light cap with a depth function of 'always fail' can be used
        ///		instead. The primary example of this is when there are floating point
        ///		inaccuracies caused by calculating the shadow geometry separately from
        ///		the real geometry.
        /// </remarks>
        public bool IsLightCapSeperate
        {
            get
            {
                return this.lightCap != null;
            }
        }

        /// <summary>
        ///		Get the light cap version of this renderable.
        /// </summary>
        public ShadowRenderable LightCapRenderable
        {
            get
            {
                return this.lightCap;
            }
        }

        /// <summary>
        ///		Should this ShadowRenderable be treated as visible?
        /// </summary>
        public virtual bool IsVisible
        {
            get
            {
                return true;
            }
        }

        #endregion Fields and Properties

        #region Construction and Destruction

        protected ShadowRenderable()
        {
            this.renderOperation = new RenderOperation();
            this.renderOperation.useIndices = true;
            this.renderOperation.operationType = OperationType.TriangleList;
        }

        #endregion Construction and Destruction

        #region Methods

        /// <summary>
        ///		Gets the internal render operation for setup.
        /// </summary>
        /// <returns></returns>
        public RenderOperation GetRenderOperationForUpdate()
        {
            return this.renderOperation;
        }

        #endregion Methods

        #region IRenderable Members

        public bool CastsShadows
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///		Gets/Sets the material to use for this shadow renderable.
        /// </summary>
        /// <remarks>
        ///		Should be set by the caller before adding to a render queue.
        /// </remarks>
        public Material Material
        {
            get
            {
                return this.material;
            }
            set
            {
                this.material = value;
            }
        }

        public Technique Technique
        {
            get
            {
                return Material.GetBestTechnique();
            }
        }

        protected RenderOperation renderOperation;

        /// <summary>
        ///		Gets the render operation for this shadow renderable.
        /// </summary>
        /// <param name="value"></param>
        public RenderOperation RenderOperation
        {
            get
            {
                return this.renderOperation;
            }
        }

        public abstract void GetWorldTransforms(Axiom.Math.Matrix4[] matrices);

        public LightList Lights
        {
            get
            {
                return this.dummyLightList;
            }
        }

        public bool NormalizeNormals
        {
            get
            {
                return false;
            }
        }

        public virtual ushort NumWorldTransforms
        {
            get
            {
                return 1;
            }
        }

        public virtual bool UseIdentityProjection
        {
            get
            {
                return false;
            }
        }

        public virtual bool UseIdentityView
        {
            get
            {
                return false;
            }
        }

        public virtual bool PolygonModeOverrideable
        {
            get
            {
                return true;
            }
        }

        public abstract Axiom.Math.Quaternion WorldOrientation { get; }

        public abstract Axiom.Math.Vector3 WorldPosition { get; }

        public virtual Real GetSquaredViewDepth(Camera camera)
        {
            return 0;
        }

        public Vector4 GetCustomParameter(int index)
        {
            if (this.customParams[index] == null)
            {
                throw new Exception("A parameter was not found at the given index");
            }
            else
            {
                return (Vector4)this.customParams[index];
            }
        }

        public void SetCustomParameter(int index, Vector4 val)
        {
            while (this.customParams.Count <= index)
            {
                this.customParams.Add(Vector4.Zero);
            }
            this.customParams[index] = val;
        }

        public void UpdateCustomGpuParameter(GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams)
        {
            if (this.customParams[entry.Data] != null)
            {
                gpuParams.SetConstant(entry.PhysicalIndex, (Vector4)this.customParams[entry.Data]);
            }
        }

        #endregion IRenderable Members

        #region IDisposable Implementation

        /// <summary>
        /// Class level dispose method
        /// </summary>
        /// <remarks>
        /// When implementing this method in an inherited class the following template should be used;
        /// protected override void dispose( bool disposeManagedResources )
        /// {
        /// 	if ( !isDisposed )
        /// 	{
        /// 		if ( disposeManagedResources )
        /// 		{
        /// 			// Dispose managed resources.
        /// 		}
        ///
        /// 		// There are no unmanaged resources to release, but
        /// 		// if we add them, they need to be released here.
        /// 	}
        ///
        /// 	// If it is available, make the call to the
        /// 	// base class's Dispose(Boolean) method
        /// 	base.dispose( disposeManagedResources );
        /// }
        /// </remarks>
        /// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    // Dispose managed resources.
                    if (this.renderOperation != null)
                    {
                        if (!this.renderOperation.IsDisposed)
                        {
                            this.renderOperation.Dispose();
                        }

                        this.renderOperation = null;
                    }

                    if (this.material != null)
                    {
                        if (!this.material.IsDisposed)
                        {
                            this.material.Dispose();
                        }

                        this.material = null;
                    }
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            base.dispose(disposeManagedResources);
        }

        #endregion IDisposable Implementation
    }
}