#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using Axiom.Collections;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Graphics;

namespace Axiom.Graphics {
    /// <summary>
    /// Summary description for SimpleRenderable.
    /// </summary>
    public abstract class SimpleRenderable : SceneObject, IRenderable {
        #region Member variables

        protected Matrix4 worldTransform = Matrix4.Identity;
        protected AxisAlignedBox box;
        protected string materialName;
        protected Material material;
        protected SceneManager sceneMgr;
        protected Camera camera;
        static protected long nextAutoGenName;

        protected VertexData vertexData;
        protected IndexData indexData;

        #endregion

        #region Constructor

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public SimpleRenderable() {
            materialName = "BaseWhite";
            material = MaterialManager.Instance.GetByName("BaseWhite");
            name = "SimpleRenderable" + nextAutoGenName++;

            material.Load();
        }

        #endregion

        #region Implementation of SceneObject

        /// <summary>
        /// 
        /// </summary>
        public override AxisAlignedBox BoundingBox {
            get {
                return box;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        internal override void NotifyCurrentCamera(Camera camera) {
            this.camera = camera;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        internal override void UpdateRenderQueue(RenderQueue queue) {
            // add ourself to the render queue
            queue.AddRenderable(this);
        }

        #endregion

        #region IRenderable Members

        /// <summary>
        /// 
        /// </summary>
        public Material Material {
            get { 
                return material; 
            }
            set {
                material = value;
            }
        }

        public Technique Technique {
            get {
                return material.BestTechnique;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="op"></param>
        public abstract void GetRenderOperation(RenderOperation op);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrices"></param>
        public virtual void GetWorldTransforms(Matrix4[] matrices) {
            matrices[0] = worldTransform * parentNode.FullTransform;
        }

        /// <summary>
        /// 
        /// </summary>
        public ushort NumWorldTransforms {
            get {
                return 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool UseIdentityProjection {
            get {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool UseIdentityView {
            get {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public SceneDetailLevel RenderDetail {
            get {
                return SceneDetailLevel.Solid;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public abstract float GetSquaredViewDepth(Camera camera);

        /// <summary>
        /// 
        /// </summary>
        public Quaternion WorldOrientation {
            get {
                return parentNode.DerivedOrientation;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 WorldPosition {
            get {
                return parentNode.DerivedPosition;
            }
        }

        public LightList Lights {
            get {
                return parentNode.Lights;
            }
        }

        #endregion
    }
}
