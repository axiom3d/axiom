//#region LGPL License
///*
//Axiom Game Engine Library
//Copyright (C) 2003  Axiom Project Team
//
//The overall design, and a majority of the core engine and rendering code 
//contained within this library is a derivative of the open source Object Oriented 
//Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
//Many thanks to the OGRE team for maintaining such a high quality project.
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//*/
//#endregion
//
//using System;
//using Axiom.Core;
//using Axiom.MathLib;
//
//namespace Axiom.Scripting {
//    /// <summary>
//    /// Summary description for GameObject.
//    /// </summary>
//    public abstract class GameObject {
//        /// <summary>
//        ///    Globally unique ID of this GameObject.
//        /// </summary>
//        protected int id;
//        protected SceneObject sceneObject;
//        protected SceneNode node;
//        protected SceneManager sceneMgr;
//
//        public GameObject(SceneManager sceneManager) {
//            this.sceneMgr = sceneManager;
//        }
//
//        public int ID {
//            get {
//                return id;
//            }
//            set {
//                id = value;
//            }
//        }
//
//        protected void NotifySceneObject(SceneObject sceneObject) {
//            sceneObject.GameObject = this;
//        }
//
//        public void Move(float x, float y, float z) {
//            node.Translate(new Vector3(x, y, z));
//        }
//
//        public void MoveRelative(float x, float y, float z) {
//            // Transform the axes of the relative vector by camera's local axes
//            Vector3 transform = node.Orientation * new Vector3(x, y, z);
//
//            node.Position += transform;
//        }
//
//        public void Rotate(Vector3 axis, float angle) {
//            node.Rotate(axis, angle);
//        }
//
//        public void Scale(float x, float y, float z) {
//            node.Scale(new Vector3(x, y, z));
//        }
//
//        /// <summary>
//        ///    Called on every frame to allow the object to update its state, perform actions, etc.
//        /// </summary>
//        /// <param name="time">Time elapsed since the last frame.</param>
//        public virtual void OnTick(float time) {
//        }
//
//        public Vector3 Position {
//            get { 
//                return node.DerivedPosition; 
//            }
//            set { 
//                node.Position = value; 
//            }
//        }
//
//        public Quaternion Orientation {
//            get { 
//                return node.DerivedOrientation; 
//            }
//            set { 
//                node.Orientation = value;
//            }
//        }
//
//        /// <summary>
//        /// Gets/Sets the object's direction vector.
//        /// </summary>
//        public Vector3 Direction {
//            get {
//                return node.Orientation * Vector3.UnitZ;
//            }
//            set {
//                Vector3 direction = value;
//
//                // Do nothing if given a zero vector
//                // (Replaced assert since this could happen with auto tracking camera and
//                //  camera passes through the lookAt point)
//                if (direction == Vector3.Zero) 
//                    return;
//
//                // Remember, camera points down -Z of local axes!
//                // Therefore reverse direction of direction vector before determining local Z
//                Vector3 zAdjustVector = direction;
//                zAdjustVector.Normalize();
//
//                // Get axes from current quaternion
//                Vector3 xAxis, yAxis, zAxis;
//
//                // get the vector components of the derived orientation vector
//                node.Orientation.ToAxes(out xAxis, out yAxis, out zAxis);
//
//                Quaternion rotationQuat;
//
//                if (-zAdjustVector == zAxis) {
//                    // Oops, a 180 degree turn (infinite possible rotation axes)
//                    // Default to yaw i.e. use current UP
//                    rotationQuat = Quaternion.FromAngleAxis(MathUtil.PI, yAxis);
//                }
//                else {
//                    // Derive shortest arc to new direction
//                    rotationQuat = zAxis.GetRotationTo(zAdjustVector);
//                }
//
//                node.Orientation = rotationQuat * node.Orientation;
//            }
//        }
//
//        public Node Node {
//            get { 
//                return node; 
//            }
//        }
//
//        public AxisAlignedBox BoundingBox {
//            get { 
//                return sceneObject.BoundingBox; 
//            }
//        }
//    }
//}
