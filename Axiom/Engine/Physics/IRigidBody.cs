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
using Axiom.MathLib;

namespace Axiom.Physics {
    /// <summary>
    /// Summary description for RigidBody.
    /// </summary>
    public interface IRigidBody {
        /// <summary>
        /// 
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// 
        /// </summary>
        Quaternion Orientation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aab"></param>
        void SetAABB(AxisAlignedBox aab);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        void AddTorque(float x, float y, float z);
		
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="x"></param>
        void AddForce(float x, float y, float z);
    }
}
