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
using Ode;
using Axiom.Core;
using Axiom.Physics;

namespace DynamicsSystem_ODE
{
	/// <summary>
	/// Summary description for OdeDynamics.
	/// </summary>
	public class OdeDynamics : DynamicsSystem, IPlugin
	{
		#region Constructors

		/// <summary>
		///		Default constructor.
		/// </summary>
		/// <remarks>
		///		Upon creation, the inherited constructor will register this instance as the singleton instance.
		/// </remarks>
		public OdeDynamics()
		{
		}

		#endregion

		#region IDynamicsSystem Members

		/// <summary>
		///		
		/// </summary>
		/// <returns></returns>
		public override IWorld CreateWorld()
		{
			return new OdeWorld();
		}

		#endregion

		#region IPlugin Members

		public void Start()
		{
		}

		public void Stop()
		{
			// TODO:  Add ODEDynamics.Stop implementation
		}

		#endregion

		#region Static methods


		static internal Ode.Vector3 MakeOdeVector(Axiom.MathLib.Vector3 vec)
		{
			return new Ode.Vector3(vec.x, vec.y, vec.z);
		}

		static internal Axiom.MathLib.Vector3 MakeDIVector(Ode.Vector3 vec)
		{
			return new Axiom.MathLib.Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
		}

		static internal Ode.Quaternion MakeOdeQuat(Axiom.MathLib.Quaternion quat)
		{
			// convert the quat
			Ode.Quaternion odeQuat = new Ode.Quaternion();
			odeQuat.W = (float)quat.w;
			odeQuat.X = (float)quat.x;
			odeQuat.Y = (float)quat.y;
			odeQuat.Z = (float)quat.z;

			return odeQuat;
		}

		static internal Axiom.MathLib.Quaternion MakeDIQuat(Ode.Quaternion quat)
		{
			return new Axiom.MathLib.Quaternion((float)quat.W, (float)quat.X, (float)quat.Y, (float)quat.Z);
		}

		#endregion
	}
}
