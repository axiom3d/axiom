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
using Axiom.Core;

using Axiom.Scripting;
using Axiom.MathLib;

namespace Axiom.Physics {
    // TODO: Create our own event args
    public delegate void CollisionHandler(object source, System.EventArgs e);

	public delegate void CollisionNotifier(PhysicalObject object1, PhysicalObject object2, Vector3 position);
	
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

		IRigidBody CreateDynamicBox(DynamicObject dynaObject, Vector3 size);
		DynamicObject CreateDynamicBox(SceneManager scene, string name, string mesh, string material, Vector3 position, Quaternion orientation, string surface, Vector3 size, float mass, bool createDebugObject);
		IRigidBody CreateDynamicSphere(DynamicObject dynaObject, float radius);
		DynamicObject CreateDynamicSphere(SceneManager scene, string name, string mesh, string material, Vector3 position, Quaternion orientation, string surface, float diameter, float mass, bool createDebugObject);
		IRigidBody CreateDynamicCapsule(DynamicObject dynaObject, float radius, float height);
		DynamicObject CreateDynamicCapsule(SceneManager scene, string name, string mesh, string material, Vector3 position, Quaternion orientation, string surface, float diameter, float height, float mass, bool createDebugObject);
		IRigidBody CreateDynamicMesh(DynamicObject dynaObject, Mesh mesh, float radius);
		IRigidBody CreateDynamicMesh(DynamicObject dynaObject, Mesh mesh, Vector3 size);

		// Functions to create static objects

		IPhysicalObject CreateStaticPlane(StaticObject statObject, Plane plane);
		IPhysicalObject CreateStaticBox(StaticObject statObject, Vector3 size);
		IPhysicalObject CreateStaticBox(SceneManager scene, string name, string mesh, string material, Vector3 position, Quaternion orientation, string surface, Vector3 size, bool createDebugObject);
		IPhysicalObject CreateStaticSphere(StaticObject statObject, float radius);
		IPhysicalObject CreateStaticSphere(SceneManager scene, string name, string mesh, string material, Vector3 position, Quaternion orientation, string surface, float diameter, bool createDebugObject);
		IPhysicalObject CreateStaticCapsule(StaticObject statObject, float radius, float height);
		IPhysicalObject CreateStaticCapsule(SceneManager scene, string name, string mesh, string material, Vector3 position, Quaternion orientation, string surface, float diameter, float height, bool createDebugObject);
		IPhysicalObject CreateTriMesh(StaticObject statObject, Mesh mesh);
		IPhysicalObject CreateTriMesh(SceneManager scene, string name, string mesh, string material, Vector3 position, Quaternion orientation, string surface);

		IJoint CreateBallSocket(SceneManager scene, string name, DynamicObject o1, DynamicObject o2, Vector3 postion, Quaternion orientation);
		IJoint CreateHinge(SceneManager scene, string name, DynamicObject o1, DynamicObject o2, float length, Vector3 position, Quaternion orientation);
		IJoint CreateFixed(SceneManager scene, string name, DynamicObject o1, DynamicObject o2);
 
		int GetDynamicObjectCount();
		int GetStaticObjectCount();
		int GetJointCount();

		DynamicObject GetDynamicObject(string name);

		event CollisionNotifier Collision;
		
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

		void Update (float stepsize);

		void Shutdown(SceneManager scene);

		void createSurface (string name, float friction, float restitution);
    }
}
