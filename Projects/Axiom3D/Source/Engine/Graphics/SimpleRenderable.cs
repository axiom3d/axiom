#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;

using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    /// Summary description for SimpleRenderable.
    /// </summary>
    public abstract class SimpleRenderable : MovableObject, IRenderable
    {
        #region Fields

        protected Matrix4 worldTransform = Matrix4.Identity;
        protected AxisAlignedBox box;
        protected string materialName;
        protected Material material;
        protected SceneManager sceneMgr;
        protected Camera camera;
        static protected long nextAutoGenName;

        protected VertexData vertexData;
        protected IndexData indexData;

        /// <summary>
        ///    Empty light list to use when there is no parent for this renderable.
        /// </summary>
        protected LightList dummyLightList = new LightList();

        protected Hashtable customParams = new Hashtable();

        #endregion Fields

        #region Constructor

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public SimpleRenderable()
        {
            materialName = "BaseWhite";
            material = MaterialManager.Instance.GetByName( "BaseWhite" );
            name = "SimpleRenderable" + nextAutoGenName++;

            material.Load();
        }

        #endregion

        #region Implementation of SceneObject

        /// <summary>
        /// 
        /// </summary>
        public override AxisAlignedBox BoundingBox
        {
            get
            {
                return (AxisAlignedBox)box.Clone();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        public override void NotifyCurrentCamera( Camera camera )
        {
            this.camera = camera;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        public override void UpdateRenderQueue( RenderQueue queue )
        {
            // add ourself to the render queue
            queue.AddRenderable( this );
        }

        #endregion

        #region IRenderable Members

        public bool CastsShadows
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Material Material
        {
            get
            {
                return material;
            }
            set
            {
                material = value;
            }
        }

        public Technique Technique
        {
            get
            {
                return material.GetBestTechnique();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="op"></param>
        public abstract void GetRenderOperation( RenderOperation op );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrices"></param>
        public virtual void GetWorldTransforms( Matrix4[] matrices )
        {
            matrices[ 0 ] = worldTransform * parentNode.FullTransform;
        }

        public bool NormalizeNormals
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ushort NumWorldTransforms
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool UseIdentityProjection
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool UseIdentityView
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public SceneDetailLevel RenderDetail
        {
            get
            {
                return SceneDetailLevel.Solid;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public abstract float GetSquaredViewDepth( Camera camera );

        /// <summary>
        /// 
        /// </summary>
        public virtual Quaternion WorldOrientation
        {
            get
            {
                return parentNode.DerivedOrientation;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual Vector3 WorldPosition
        {
            get
            {
                return parentNode.DerivedPosition;
            }
        }

        public LightList Lights
        {
            get
            {
                if ( parentNode != null )
                {
                    return parentNode.Lights;
                }
                else
                {
                    return dummyLightList;
                }
            }
        }

        public Vector4 GetCustomParameter( int index )
        {
            if ( customParams[ index ] == null )
            {
                throw new Exception( "A parameter was not found at the given index" );
            }
            else
            {
                return (Vector4)customParams[ index ];
            }
        }

        public void SetCustomParameter( int index, Vector4 val )
        {
            customParams[ index ] = val;
        }

        public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
        {
            if ( customParams[ entry.data ] != null )
            {
                gpuParams.SetConstant( entry.index, (Vector4)customParams[ entry.data ] );
            }
        }

        #endregion
    }
}
