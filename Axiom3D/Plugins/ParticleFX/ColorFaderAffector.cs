#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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

#region Namespace Declarations

using System;

using Axiom;

using DotNet3D.Math;

#endregion Namespace Declarations
			
namespace Axiom.ParticleFX
{
    /// <summary>
    /// Summary description for ColorFaderAffector.
    /// </summary>
    public class ColorFaderAffector : ParticleAffector
    {
        protected Real alphaAdjust;
        protected Real redAdjust;
        protected Real greenAdjust;
        protected Real blueAdjust;

        public ColorFaderAffector()
        {
            this.type = "ColourFader";
        }

        public Real AlphaAdjust
        {
            get
            {
                return alphaAdjust;
            }
            set
            {
                alphaAdjust = value;
            }
        }

        public Real RedAdjust
        {
            get
            {
                return redAdjust;
            }
            set
            {
                redAdjust = value;
            }
        }

        public Real GreenAdjust
        {
            get
            {
                return greenAdjust;
            }
            set
            {
                greenAdjust = value;
            }
        }

        public Real BlueAdjust
        {
            get
            {
                return blueAdjust;
            }
            set
            {
                blueAdjust = value;
            }
        }

        protected void AdjustWithClamp( ref Real component, Real adjust )
        {
            component += adjust;

            // limit to range [0,1]
            if ( component < 0.0f )
                component = 0.0f;
            else if ( component > 1.0f )
                component = 1.0f;
        }

        public override void InitParticle( ref Particle particle )
        {
        }

        public override void AffectParticles( ParticleSystem system, Real timeElapsed )
        {
            Real da, dr, dg, db;

            da = alphaAdjust * timeElapsed;
            dr = redAdjust * timeElapsed;
            dg = greenAdjust * timeElapsed;
            db = blueAdjust * timeElapsed;

            // loop through the particles

            for ( int i = 0; i < system.Particles.Count; i++ )
            {
                Particle p = (Particle)system.Particles[i];

                // adjust the values with clamping ([0,1] in this case)
                AdjustWithClamp( ref p.Color.a, da );
                AdjustWithClamp( ref p.Color.r, dr );
                AdjustWithClamp( ref p.Color.g, dg );
                AdjustWithClamp( ref p.Color.b, db );

            }
        }

        #region Command definition classes

        [Command( "red", "Red component.", typeof( ParticleAffector ) )]
        class RedCommand : ICommand
        {
            #region ICommand Members

            public string Get( object target )
            {
                ColorFaderAffector affector = target as ColorFaderAffector;
                return StringConverter.ToString( affector.RedAdjust );
            }
            public void Set( object target, string val )
            {
                ColorFaderAffector affector = target as ColorFaderAffector;
                affector.RedAdjust = Real.Parse( val );
            }

            #endregion
        }

        [Command( "green", "Green component.", typeof( ParticleAffector ) )]
        class GreenCommand : ICommand
        {
            #region ICommand Members

            public string Get( object target )
            {
                ColorFaderAffector affector = target as ColorFaderAffector;
                return StringConverter.ToString( affector.GreenAdjust );
            }
            public void Set( object target, string val )
            {
                ColorFaderAffector affector = target as ColorFaderAffector;
                affector.GreenAdjust = Real.Parse( val );
            }

            #endregion
        }

        [Command( "blue", "Blue component.", typeof( ParticleAffector ) )]
        class BlueCommand : ICommand
        {
            #region ICommand Members

            public string Get( object target )
            {
                ColorFaderAffector affector = target as ColorFaderAffector;
                return StringConverter.ToString( affector.BlueAdjust );
            }
            public void Set( object target, string val )
            {
                ColorFaderAffector affector = target as ColorFaderAffector;
                affector.BlueAdjust = Real.Parse( val );
            }

            #endregion
        }

        [Command( "alpha", "Alpha component.", typeof( ParticleAffector ) )]
        class AlphaCommand : ICommand
        {
            #region ICommand Members

            public string Get( object target )
            {
                ColorFaderAffector affector = target as ColorFaderAffector;
                return StringConverter.ToString( affector.AlphaAdjust );
            }
            public void Set( object target, string val )
            {
                ColorFaderAffector affector = target as ColorFaderAffector;
                affector.AlphaAdjust = Real.Parse( val );
            }

            #endregion
        }

        #endregion Command definition classes
    }
}
