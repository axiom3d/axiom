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
using Axiom.Scripting;
using Axiom.MathLib;

namespace Axiom.Physics
{
	// TODO: Create our own event args
	public delegate void CollisionHandler(object source, System.EventArgs e);

	/// <summary>
	/// Summary description for IWorld.
	/// </summary>
	public interface IWorld
	{
		/// <summary>
		/// 
		/// </summary>
		//event CollisionHandler OnCollision;

		/// <summary>
		/// 
		/// </summary>
		Vector3 Gravity { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="bodyType"></param>
		/// <param name="position"></param>
		/// <param name="orientation"></param>
		/// <param name="aab"></param>
		/// <param name="massDensity"></param>
		/// <returns></returns>
		IRigidBody CreateBody(GameObject gameObject, DynamicsBodyType bodyType, float massDensity);

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		//IJoint CreateJoint();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stepsize"></param>
		void Step(float stepsize);
	}
}
