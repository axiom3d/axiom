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
	/// Summary description for ColorInterpolatorAffector.
	/// </summary>
	public class ColorInterpolatorAffector : ParticleAffector
	{
		protected const int MAX_STAGES = 6;

		internal ColorEx[] colorAdj = new ColorEx[ MAX_STAGES ];
		internal float[] timeAdj = new float[ MAX_STAGES ];

		public ColorInterpolatorAffector( ParticleSystem psys )
			: base( psys )
		{
			this.type = "ColourInterpolator";
			ColorEx init;
			init.a = init.r = init.g = 0.5f;
			init.b = 0.0f;

			for ( int i = 0; i < MAX_STAGES; ++i )
			{
				colorAdj[ i ] = init;
				timeAdj[ i ] = 1.0f;
			}
		}

		public override void AffectParticles( ParticleSystem system, Real timeElapsed )
		{
			// loop through the particles
			for ( int i = 0; i < system.Particles.Count; i++ )
			{
				Particle p = (Particle)system.Particles[ i ];

				float lifeTime = p.totalTimeToLive;
				float particleTime = 1.0f - ( p.timeToLive / lifeTime );

				if ( particleTime <= timeAdj[ 0 ] )
				{
					p.Color = colorAdj[ 0 ];
				}
				else if ( particleTime >= timeAdj[ MAX_STAGES - 1 ] )
				{
					p.Color = colorAdj[ MAX_STAGES - 1 ];
				}
				else
				{
					for ( int k = 0; k < MAX_STAGES - 1; k++ )
					{
						if ( particleTime >= timeAdj[ k ] && particleTime < timeAdj[ k + 1 ] )
						{
							particleTime -= timeAdj[ k ];
							particleTime /= ( timeAdj[ k + 1 ] - timeAdj[ k ] );
							p.Color.r = ( ( colorAdj[ k + 1 ].r * particleTime ) + ( colorAdj[ k ].r * ( 1.0f - particleTime ) ) );
							p.Color.g = ( ( colorAdj[ k + 1 ].g * particleTime ) + ( colorAdj[ k ].g * ( 1.0f - particleTime ) ) );
							p.Color.b = ( ( colorAdj[ k + 1 ].b * particleTime ) + ( colorAdj[ k ].b * ( 1.0f - particleTime ) ) );
							p.Color.a = ( ( colorAdj[ k + 1 ].a * particleTime ) + ( colorAdj[ k ].a * ( 1.0f - particleTime ) ) );

							break;
						}
					}
				}
			}
		}

		#region Command definition classes

		[ScriptableProperty( "colour0", "Initial 'keyframe' color.", typeof ( ParticleAffector ) )]
		public class Color0Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				return StringConverter.ToString( affector.colorAdj[ 0 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.colorAdj[ 0 ] = StringConverter.ParseColor( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "colour1", "1st 'keyframe' color.", typeof ( ParticleAffector ) )]
		public class Color1Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				// TODO: Common way for writing color.
				return StringConverter.ToString( affector.colorAdj[ 1 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.colorAdj[ 1 ] = StringConverter.ParseColor( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "colour2", "2nd 'keyframe' color.", typeof ( ParticleAffector ) )]
		public class Color2Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				// TODO: Common way for writing color.
				return StringConverter.ToString( affector.colorAdj[ 2 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.colorAdj[ 2 ] = StringConverter.ParseColor( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "colour3", "3rd 'keyframe' color.", typeof ( ParticleAffector ) )]
		public class Color3Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				return StringConverter.ToString( affector.colorAdj[ 3 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.colorAdj[ 3 ] = StringConverter.ParseColor( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "colour4", "4th 'keyframe' color.", typeof ( ParticleAffector ) )]
		public class Color4Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				return StringConverter.ToString( affector.colorAdj[ 4 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.colorAdj[ 4 ] = StringConverter.ParseColor( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "colour5", "5th 'keyframe' color.", typeof ( ParticleAffector ) )]
		public class Color5Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				return StringConverter.ToString( affector.colorAdj[ 5 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.colorAdj[ 5 ] = StringConverter.ParseColor( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "time0", "Initial 'keyframe' time.", typeof ( ParticleAffector ) )]
		public class Time0Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				return StringConverter.ToString( affector.timeAdj[ 0 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.timeAdj[ 0 ] = StringConverter.ParseFloat( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "time1", "1st 'keyframe' time.", typeof ( ParticleAffector ) )]
		public class Time1Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				return StringConverter.ToString( affector.timeAdj[ 1 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.timeAdj[ 1 ] = StringConverter.ParseFloat( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "time2", "2nd 'keyframe' time.", typeof ( ParticleAffector ) )]
		public class Time2Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				return StringConverter.ToString( affector.timeAdj[ 2 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.timeAdj[ 2 ] = StringConverter.ParseFloat( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "time3", "3rd 'keyframe' time.", typeof ( ParticleAffector ) )]
		public class Time3Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				return StringConverter.ToString( affector.timeAdj[ 3 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.timeAdj[ 3 ] = StringConverter.ParseFloat( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "time4", "4th 'keyframe' time.", typeof ( ParticleAffector ) )]
		public class Time4Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				return StringConverter.ToString( affector.timeAdj[ 4 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.timeAdj[ 4 ] = StringConverter.ParseFloat( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "time5", "5th 'keyframe' time.", typeof ( ParticleAffector ) )]
		public class Time5Command : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				return StringConverter.ToString( affector.timeAdj[ 5 ] );
			}

			public void Set( object target, string val )
			{
				ColorInterpolatorAffector affector = target as ColorInterpolatorAffector;

				affector.timeAdj[ 5 ] = StringConverter.ParseFloat( val );
			}

			#endregion IPropertyCommand Members
		}

		#endregion Command definition classes
	}
}
