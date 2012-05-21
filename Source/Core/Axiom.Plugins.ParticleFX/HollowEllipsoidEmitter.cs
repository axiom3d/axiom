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
	/// Summary description for HollowEllipsidEmitter.
	/// </summary>
	public class HollowEllipsoidEmitter : AreaEmitter
	{
		#region Fields

		protected float innerX;
		protected float innerY;
		protected float innerZ;

		#endregion Fields

		#region Properties

		public float InnerX
		{
			get
			{
				return this.innerX;
			}
			set
			{
				Debug.Assert( value > 0.0f && value < 1.0f );
				this.innerX = value;
			}
		}

		public float InnerY
		{
			get
			{
				return this.innerY;
			}
			set
			{
				Debug.Assert( value > 0.0f && value < 1.0f );
				this.innerY = value;
			}
		}

		public float InnerZ
		{
			get
			{
				return this.innerZ;
			}
			set
			{
				Debug.Assert( value > 0.0f && value < 1.0f );
				this.innerZ = value;
			}
		}

		#endregion Properties

		public HollowEllipsoidEmitter( ParticleSystem ps )
			: base( ps )
		{
			InitDefaults( "HollowEllipsoid" );
			this.innerX = 0.5f;
			this.innerY = 0.5f;
			this.innerZ = 0.5f;
		}

		public override void InitParticle( Particle particle )
		{
			float alpha, beta, a, b, c, x, y, z;

			particle.ResetDimensions();

			// create two random angles alpha and beta
			// with these two angles, we are able to select any point on an
			// ellipsoid's surface
			Utility.RangeRandom( durationMin, durationMax );

			alpha = Utility.RangeRandom( 0, Utility.TWO_PI );
			beta = Utility.RangeRandom( 0, Utility.PI );

			// create three random radius values that are bigger than the inner
			// size, but smaller/equal than/to the outer size 1.0 (inner size is
			// between 0 and 1)
			a = Utility.RangeRandom( InnerX, 1.0f );
			b = Utility.RangeRandom( InnerY, 1.0f );
			c = Utility.RangeRandom( InnerZ, 1.0f );

			// with a,b,c we have defined a random ellipsoid between the inner
			// ellipsoid and the outer sphere (radius 1.0)
			// with alpha and beta we select on point on this random ellipsoid
			// and calculate the 3D coordinates of this point
			x = a*Utility.Cos( alpha )*Utility.Sin( beta );
			y = b*Utility.Sin( alpha )*Utility.Sin( beta );
			z = c*Utility.Cos( beta );

			// scale the found point to the ellipsoid's size and move it
			// relatively to the center of the emitter point

			particle.Position = position + x*xRange + y*yRange*z*zRange;

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
		[ScriptableProperty( "width", "Width of the hollow ellipsoidal emitter.", typeof ( ParticleEmitter ) )]
		public class WidthCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as HollowEllipsoidEmitter;
				emitter.Width = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as HollowEllipsoidEmitter;
				return StringConverter.ToString( emitter.Width );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "height", "Height of the hollow ellipsoidal emitter.", typeof ( ParticleEmitter ) )]
		public class HeightCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as HollowEllipsoidEmitter;
				emitter.Height = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as HollowEllipsoidEmitter;
				return StringConverter.ToString( emitter.Height );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "depth", "Depth of the hollow ellipsoidal emitter.", typeof ( ParticleEmitter ) )]
		public class DepthCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as HollowEllipsoidEmitter;
				emitter.Depth = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as HollowEllipsoidEmitter;
				return StringConverter.ToString( emitter.Depth );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "inner_width", "Parametric value describing the proportion of the shape which is hollow.",
			typeof ( ParticleEmitter ) )]
		public class InnerWidthCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as HollowEllipsoidEmitter;
				emitter.InnerX = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as HollowEllipsoidEmitter;
				return StringConverter.ToString( emitter.InnerX );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "inner_height", "Parametric value describing the proportion of the shape which is hollow.",
			typeof ( ParticleEmitter ) )]
		public class InnerHeightCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as HollowEllipsoidEmitter;
				emitter.InnerY = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as HollowEllipsoidEmitter;
				return StringConverter.ToString( emitter.InnerY );
			}
		}

		/// <summary>
		///
		/// </summary>
		[ScriptableProperty( "inner_depth", "Parametric value describing the proportion of the shape which is hollow.",
			typeof ( ParticleEmitter ) )]
		public class InnerDepthCommand : IPropertyCommand
		{
			public void Set( object target, string val )
			{
				var emitter = target as HollowEllipsoidEmitter;
				emitter.InnerZ = StringConverter.ParseFloat( val );
			}

			public string Get( object target )
			{
				var emitter = target as HollowEllipsoidEmitter;
				return StringConverter.ToString( emitter.InnerZ );
			}
		}

		#endregion Command definition classes
	}
}