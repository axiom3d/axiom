#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
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
    /// Summary description for AreaEmitter.
    /// </summary>
    public abstract class AreaEmitter : ParticleEmitter
    {
        #region Fields

        protected Vector3 size = Vector3.Zero;
        protected Vector3 xRange;
        protected Vector3 yRange;
        protected Vector3 zRange;

        #endregion Fields

        public AreaEmitter() : base()
        {
        }

        #region Properties

        public override Axiom.Math.Vector3 Direction
        {
            get
            {
                return base.Direction;
            }
            set
            {
                base.Direction = value;

                // update the ranges
                GenerateAreaAxes();
            }
        }

        public Vector3 Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
                GenerateAreaAxes();
            }
        }

        public float Width
        {
            get
            {
                return size.x;
            }
            set
            {
                size.x = value;
                GenerateAreaAxes();
            }
        }

        public float Height
        {
            get
            {
                return size.y;
            }
            set
            {
                size.y = value;
                GenerateAreaAxes();
            }
        }

        public float Depth
        {
            get
            {
                return size.z;
            }
            set
            {
                size.z = value;
                GenerateAreaAxes();
            }
        }

        #endregion

        #region Methods

        protected void GenerateAreaAxes()
        {
            Vector3 left = up.Cross( direction );

            xRange = left * ( size.x * 0.5f );
            yRange = up * ( size.y * 0.5f );
            zRange = direction * ( size.z * 0.5f );
        }

        protected void InitDefaults( string type )
        {
            // TODO: Revisit this
            direction = Vector3.UnitZ;
            up = Vector3.UnitZ;
            this.Size = new Vector3( 50, 50, 0 );
            this.type = type;
        }

        #endregion

        #region Implementation of ParticleEmitter

        public override ushort GetEmissionCount( float timeElapsed )
        {
            // use basic constant emission
            return GenerateConstantEmissionCount( timeElapsed );
        }

        #endregion
    }
}
