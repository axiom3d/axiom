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
using Axiom;

namespace Axiom
{
    /// <summary>
    ///		An extension of Billboard that allows for each particle to have particle system specific info.
    /// </summary>
    public class Particle : Billboard
    {

        #region Member variables

        /// <summary>Time (in seconds) before this particle is destroyed.</summary>
        public float timeToLive;
        /// <summary>Total Time to live, number of seconds of particles natural life</summary>
        public float totalTimeToLive;
        /// <summary>Speed of rotation in radians</summary>
        float rotationSpeed;

        #endregion

        #region Properties

        public float RotationSpeed
        {
            get
            {
                return rotationSpeed;
            }
            set
            {
                rotationSpeed = value;
            }
        }

        #endregion

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public Particle()
        {
            timeToLive = 10;
            totalTimeToLive = 10;
            rotationSpeed = 0;
        }
    }
}
