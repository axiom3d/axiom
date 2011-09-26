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
using System.Diagnostics;

using Axiom.Core;
using Axiom.ParticleSystems;
using Axiom.Math;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleFX
{
	/// <summary>
	/// Summary description for RingEmitter.
	/// </summary>
	public class RingEmitter : AreaEmitter
	{
		#region Fields

		protected float innerX;
		protected float innerY;

		#endregion Fields

		#region Properties

		public float InnerX
		{
			get
			{
				return innerX;
			}
			set
			{
				Debug.Assert( value > 0.0f && value < 1.0f );
				innerX = value;
			}
		}

		public float InnerY
		{
			get
			{
				return innerY;
			}
			set
			{
				Debug.Assert( value > 0.0f && value < 1.0f );
				innerY = value;
			}
		}

		#endregion Properties

		public RingEmitter( ParticleSystem ps )
			: base( ps )
		{
			InitDefaults( "Ring" );
			innerX = 0.5f;
			innerY = 0.5f;
		}

		public override void InitParticle( Particle particle )
		{
			float alpha, a, b, x, y, z;

			// create a random angle from 0 .. PI*2
			alpha = Utility.RangeRandom( 0, Utility.TWO_PI );

			// create two random radius values that are bigger than the inner size
			a = Utility.RangeRandom( InnerX, 1.0f );
			b = Utility.RangeRandom( InnerY, 1.0f );

			// with a and b we have defined a random ellipse inside the inner
			// ellipse and the outer circle (radius 1.0)
			// with alpha, and a and b we select a random point on this ellipse
			// and calculate it's coordinates
			x = a * Utility.Sin( alpha );
			y = b * Utility.Cos( alpha );
			// the height is simple running from 0 to 1
			z = Utility.UnitRandom();     // 0..1

			// scale the found point to the ring's size and move it
			// relatively to the center of the emitter point

			particle.Position = position + x * xRange + y * yRange + z * zRange;

			// Generate complex data by reference
			GenerateEmissionColor( ref particle.Color );
			GenerateEmissionDirection( ref particle.Direction );
			GenerateEmissionVelocity( ref particle.Direction );

			// Generate simpler data
			particle.timeToLive = GenerateEmissionTTL();
		}

		#region Command definition classes

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "width", "Width of the hollow ellipsoidal emitter.", typeof( ParticleEmitter ) )]
        public class WidthCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				RingEmitter emitter = target as RingEmitter;
				emitter.Width = StringConverter.ParseFloat( val );
			}
			public string Get( object target )
			{
				RingEmitter emitter = target as RingEmitter;
				return StringConverter.ToString( emitter.Width );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "height", "Height of the hollow ellipsoidal emitter.", typeof( ParticleEmitter ) )]
        public class HeightCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				RingEmitter emitter = target as RingEmitter;
				emitter.Height = StringConverter.ParseFloat( val );
			}
			public string Get( object target )
			{
				RingEmitter emitter = target as RingEmitter;
				return StringConverter.ToString( emitter.Height );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "depth", "Depth of the hollow ellipsoidal emitter.", typeof( ParticleEmitter ) )]
        public class DepthCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				RingEmitter emitter = target as RingEmitter;
				emitter.Depth = StringConverter.ParseFloat( val );
			}
			public string Get( object target )
			{
				RingEmitter emitter = target as RingEmitter;
				return StringConverter.ToString( emitter.Depth );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "inner_width", "Parametric value describing the proportion of the shape which is hollow.", typeof( ParticleEmitter ) )]
        public class InnerWidthCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				RingEmitter emitter = target as RingEmitter;
				emitter.InnerX = StringConverter.ParseFloat( val );
			}
			public string Get( object target )
			{
				RingEmitter emitter = target as RingEmitter;
				return StringConverter.ToString( emitter.InnerX );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "inner_height", "Parametric value describing the proportion of the shape which is hollow.", typeof( ParticleEmitter ) )]
        public class InnerHeightCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				RingEmitter emitter = target as RingEmitter;
				emitter.InnerY = StringConverter.ParseFloat( val );
			}
			public string Get( object target )
			{
				RingEmitter emitter = target as RingEmitter;
				return StringConverter.ToString( emitter.InnerY );
			}
		}

		#endregion Command definition classes
	}
}