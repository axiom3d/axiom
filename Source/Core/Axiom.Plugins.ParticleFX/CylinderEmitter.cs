#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;
using Axiom.ParticleSystems;
using Axiom.Math;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleFX
{
    /// <summary>
    /// Summary description for CylinderEmitter.
    /// </summary>
    public class CylinderEmitter : AreaEmitter
    {
        public CylinderEmitter(ParticleSystem ps)
            : base(ps)
        {
            InitDefaults("Cylinder");
        }

        public override void InitParticle(Particle particle)
        {
            float xOff, yOff, zOff;

            // First we create a random point inside a bounding cylinder with a
            // radius and height of 1 (this is easy to do). The distance of the
            // point from 0,0,0 must be <= 1 (== 1 means on the surface and we
            // count this as inside, too).

            while (true)
            {
                // three random values for one random point in 3D space
                xOff = Utility.SymmetricRandom();
                yOff = Utility.SymmetricRandom();
                zOff = Utility.SymmetricRandom();

                // the distance of x,y from 0,0 is sqrt(x*x+y*y), but
                // as usual we can omit the sqrt(), since sqrt(1) == 1 and we
                // use the 1 as boundary. z is not taken into account, since
                // all values in the z-direction are inside the cylinder:
                if (xOff * xOff + yOff * yOff <= 1)
                {
                    // found one valid point inside
                    break;
                }
            }

            // scale the found point to the cylinder's size and move it
            // relatively to the center of the emitter point

            particle.Position = Position + xOff * xRange + yOff * yRange * zOff * zRange;

            // Generate complex data by reference
            GenerateEmissionColor(ref particle.Color);
            GenerateEmissionDirection(ref particle.Direction);
            GenerateEmissionVelocity(ref particle.Direction);

            // Generate simpler data
            particle.timeToLive = GenerateEmissionTTL();
        }

        #region Command definition classes

        /// <summary>
        ///
        /// </summary>
        [ScriptableProperty("width", "Width of the cylinder emitter.", typeof(ParticleEmitter))]
        public class WidthCommand : IPropertyCommand
        {
            public void Set(object target, string val)
            {
                var emitter = target as CylinderEmitter;
                emitter.Width = StringConverter.ParseFloat(val);
            }

            public string Get(object target)
            {
                var emitter = target as CylinderEmitter;
                return StringConverter.ToString(emitter.Width);
            }
        }

        /// <summary>
        ///
        /// </summary>
        [ScriptableProperty("height", "Height of the cylinder emitter.", typeof(ParticleEmitter))]
        public class HeightCommand : IPropertyCommand
        {
            public void Set(object target, string val)
            {
                var emitter = target as CylinderEmitter;
                emitter.Height = StringConverter.ParseFloat(val);
            }

            public string Get(object target)
            {
                var emitter = target as CylinderEmitter;
                return StringConverter.ToString(emitter.Height);
            }
        }

        /// <summary>
        ///
        /// </summary>
        [ScriptableProperty("depth", "Depth of the cylinder emitter.", typeof(ParticleEmitter))]
        public class DepthCommand : IPropertyCommand
        {
            public void Set(object target, string val)
            {
                var emitter = target as CylinderEmitter;
                emitter.Depth = StringConverter.ParseFloat(val);
            }

            public string Get(object target)
            {
                var emitter = target as CylinderEmitter;
                return StringConverter.ToString(emitter.Depth);
            }
        }

        #endregion Command definition classes
    }
}