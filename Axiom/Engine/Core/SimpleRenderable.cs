#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for SimpleRenderable.
	/// </summary>
	public abstract class SimpleRenderable : SceneObject, IRenderable
	{
		#region Member variables

		protected Matrix4 worldTransform = Matrix4.Identity;
		protected AxisAlignedBox box;
		protected String materialName;
		protected Material material;
		protected SceneManager sceneMgr;
		protected Camera camera;
		static protected long nextAutoGenName;

		protected float[] vertexData;
		protected int[] colorData;

		#endregion

		#region Constructor

		/// <summary>
		///		Default constructor.
		/// </summary>
		public SimpleRenderable()
		{
			materialName = "BaseWhite";
			material = (Material)MaterialManager.Instance["BaseWhite"];
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
				return null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		internal override void NotifyCurrentCamera(Camera camera)
		{
			this.camera = camera;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="queue"></param>
		internal override void UpdateRenderQueue(RenderQueue queue)
		{
			// add ourself to the render queue
			queue.AddRenderable(this);
		}

		#endregion

		#region IRenderable Members

		/// <summary>
		/// 
		/// </summary>
		public Material Material
		{
			get { return material; }
		}

		public void GetRenderOperation(RenderOperation op)
		{
			// TODO: Implement GetRenderOperation
		}		

		/// <summary>
		/// 
		/// </summary>
		virtual public Axiom.MathLib.Matrix4[] WorldTransforms
		{
			get
			{
				return new Matrix4[] {worldTransform * parentNode.FullTransform};
			}
		}

		#endregion

		#region IRenderable Members

		public ushort NumWorldTransforms
		{
			get
			{
				// TODO:  Add SimpleRenderable.NumWorldTransforms getter implementation
				return 1;
			}
		}

		public bool UseIdentityProjection
		{
			get
			{
				// TODO:  Add SimpleRenderable.UseIdentityProjection getter implementation
				return false;
			}
		}

		public bool UseIdentityView
		{
			get
			{
				// TODO:  Add SimpleRenderable.UseIdentityView getter implementation
				return false;
			}
		}

		public SceneDetailLevel RenderDetail
		{
			get
			{
				// TODO:  Add SimpleRenderable.RenderDetail getter implementation
				return SceneDetailLevel.Solid;
			}
		}

		virtual public float GetSquaredViewDepth(Camera camera)
		{
			// TODO:  Add SimpleRenderable.GetSquaredViewDepth implementation
			return 0;
		}

		#endregion
	}
}
