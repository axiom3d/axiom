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

namespace Axiom.Graphics {
	/// <summary>
	/// Summary description for ShadowRenderable.
	/// </summary>
	public class ShadowRenderable : IRenderable {
		public ShadowRenderable() {
		}
	
        #region IRenderable Members

        public Material Material {
            get {
                // TODO:  Add ShadowRenderable.Material getter implementation
                return null;
            }
        }

        public Technique Technique {
            get {
                // TODO:  Add ShadowRenderable.Technique getter implementation
                return null;
            }
        }

        public void GetRenderOperation(RenderOperation op) {
            // TODO:  Add ShadowRenderable.GetRenderOperation implementation
        }

        public void GetWorldTransforms(Axiom.MathLib.Matrix4[] matrices) {
            // TODO:  Add ShadowRenderable.GetWorldTransforms implementation
        }

        public LightList Lights {
            get {
                // TODO:  Add ShadowRenderable.Lights getter implementation
                return null;
            }
        }

        public bool NormalizeNormals {
            get {
                // TODO:  Add ShadowRenderable.NormalizeNormals getter implementation
                return false;
            }
        }

        public ushort NumWorldTransforms {
            get {
                // TODO:  Add ShadowRenderable.NumWorldTransforms getter implementation
                return 0;
            }
        }

        public bool UseIdentityProjection {
            get {
                // TODO:  Add ShadowRenderable.UseIdentityProjection getter implementation
                return false;
            }
        }

        public bool UseIdentityView {
            get {
                // TODO:  Add ShadowRenderable.UseIdentityView getter implementation
                return false;
            }
        }

        public SceneDetailLevel RenderDetail {
            get {
                return SceneDetailLevel.Solid;
            }
        }

        public Axiom.MathLib.Quaternion WorldOrientation {
            get {
                // TODO:  Add ShadowRenderable.WorldOrientation getter implementation
                return new Axiom.MathLib.Quaternion ();
            }
        }

        public Axiom.MathLib.Vector3 WorldPosition {
            get {
                // TODO:  Add ShadowRenderable.WorldPosition getter implementation
                return new Axiom.MathLib.Vector3 ();
            }
        }

        public float GetSquaredViewDepth(Camera camera) {
            return 0;
        }

        #endregion
    }
}
