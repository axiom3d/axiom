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
using Axiom.Physics;
using Axiom.MathLib;

namespace Axiom.Scripting
{
	/// <summary>
	/// Summary description for GameObject.
	/// </summary>
	public abstract class GameObject
	{
		protected IRigidBody body;
		protected Entity entity;
		protected SceneNode node;
		protected SceneManager sceneMgr;

		public GameObject(SceneManager sceneManager)
		{
			this.sceneMgr = sceneManager;
		}

		public IRigidBody RigidBody
		{
			set 
			{ 
				body = value; 
				body.Position = node.Position;
			}
			get { return body; }
		}

		public void Move(float x, float y, float z)
		{
			node.Translate(new Vector3(x, y, z));
			//body.Position = node.Position;
			//body.AddTorque(120.0f, 0.0f, 0.0f);
		}

		public void Rotate(Vector3 axis, float angle)
		{
			node.Rotate(axis, angle);
		}

		public void Scale(float x, float y, float z)
		{
			node.Scale(new Vector3(x, y, z));
		}

		public Vector3 Position
		{
			get { return node.Position; }
			set { node.Position = value; }
		}

		public Quaternion Orientation
		{
			get { return node.Orientation; }
			set { 	node.Orientation = value;
			}
		}

		public Node Node
		{
			get { return node; }
		}

		public AxisAlignedBox BoundingBox
		{
			get { return entity.BoundingBox; }
		}

		public void UpdateFromDynamics()
		{
			this.Position = body.Position;
			this.Orientation = body.Orientation;
		}
	}
}
