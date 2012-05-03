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

using Axiom.Core;
using Axiom.Math;
using Axiom.ParticleSystems;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleFX
{
	/// <summary>
	/// Summary description for ColorFaderAffector.
	/// </summary>
	public class ColorFaderAffector : ParticleAffector
	{
		protected float alphaAdjust;
		protected float redAdjust;
		protected float greenAdjust;
		protected float blueAdjust;

		public ColorFaderAffector( ParticleSystem psys )
			: base( psys )
		{
			type = "ColourFader";
		}

		public float AlphaAdjust
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

		public float RedAdjust
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

		public float GreenAdjust
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

		public float BlueAdjust
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

		protected float AdjustWithClamp( float component, float adjust )
		{
			component += adjust;

			// limit to range [0,1]
			if ( component < 0.0f )
			{
				component = 0.0f;
			}
			else if ( component > 1.0f )
			{
				component = 1.0f;
			}

			return component;
		}

		public override void InitParticle( ref Particle particle )
		{
		}

		public override void AffectParticles( ParticleSystem system, Real timeElapsed )
		{
			float da, dr, dg, db;

			da = alphaAdjust*timeElapsed;
			dr = redAdjust*timeElapsed;
			dg = greenAdjust*timeElapsed;
			db = blueAdjust*timeElapsed;

			// loop through the particles

			for ( int i = 0; i < system.Particles.Count; i++ )
			{
				var p = (Particle)system.Particles[ i ];

				// adjust the values with clamping ([0,1] in this case)
				p.Color.a = AdjustWithClamp( p.Color.a, da );
				p.Color.r = AdjustWithClamp( p.Color.r, dr );
				p.Color.g = AdjustWithClamp( p.Color.g, dg );
				p.Color.b = AdjustWithClamp( p.Color.b, db );
			}
		}

		#region Command definition classes

		[ScriptableProperty( "red", "Red component.", typeof ( ParticleAffector ) )]
		public class RedCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				var affector = target as ColorFaderAffector;
				return StringConverter.ToString( affector.RedAdjust );
			}

			public void Set( object target, string val )
			{
				var affector = target as ColorFaderAffector;
				affector.RedAdjust = StringConverter.ParseFloat( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "green", "Green component.", typeof ( ParticleAffector ) )]
		public class GreenCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				var affector = target as ColorFaderAffector;
				return StringConverter.ToString( affector.GreenAdjust );
			}

			public void Set( object target, string val )
			{
				var affector = target as ColorFaderAffector;
				affector.GreenAdjust = StringConverter.ParseFloat( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "blue", "Blue component.", typeof ( ParticleAffector ) )]
		public class BlueCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				var affector = target as ColorFaderAffector;
				return StringConverter.ToString( affector.BlueAdjust );
			}

			public void Set( object target, string val )
			{
				var affector = target as ColorFaderAffector;
				affector.BlueAdjust = StringConverter.ParseFloat( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "alpha", "Alpha component.", typeof ( ParticleAffector ) )]
		public class AlphaCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				var affector = target as ColorFaderAffector;
				return StringConverter.ToString( affector.AlphaAdjust );
			}

			public void Set( object target, string val )
			{
				var affector = target as ColorFaderAffector;
				affector.AlphaAdjust = StringConverter.ParseFloat( val );
			}

			#endregion IPropertyCommand Members
		}

		#endregion Command definition classes
	}
}