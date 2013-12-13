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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Controllers.Canned
{
	/// <summary>
	/// A Controller representing a periodic waveform function ranging from Sine to InverseSawtooth
	/// </summary>
	/// <remarks>Function take to form of BaseValue + Amplitude * ( F(time * freq) / 2 + .5 )
	/// such as Base + A * ( Sin(t freq 2 pi) + .5) </remarks>
	public class WaveformControllerFunction : BaseControllerFunction
	{
		#region Member variables

		protected WaveformType type;
		protected Real baseVal; //[FXCop Optimization : Do not initialize unnecessarily]
		protected Real frequency = 1.0f;
		protected Real phase; //[FXCop Optimization : Do not initialize unnecessarily]
		protected Real amplitude = 1.0f;
		protected float dutyCycle = 0.5f;

		#endregion

		#region Constructors

		public WaveformControllerFunction( WaveformType type, Real baseVal, Real frequency, Real phase, Real amplitude,
		                                   bool useDelta )
			: base( useDelta )
		{
			this.type = type;
			this.baseVal = baseVal;
			this.frequency = frequency;
			this.phase = phase;
			this.amplitude = amplitude;
			deltaCount = phase;
		}

		public WaveformControllerFunction( WaveformType type, Real baseVal )
			: base( true )
		{
			this.type = type;
			this.baseVal = baseVal;
		}

		public WaveformControllerFunction( WaveformType type, Real baseVal, Real frequency )
			: base( true )
		{
			this.type = type;
			this.baseVal = baseVal;
			this.frequency = frequency;
		}

		public WaveformControllerFunction( WaveformType type, Real baseVal, Real frequency, Real phase )
			: base( true )
		{
			this.type = type;
			this.baseVal = baseVal;
			this.frequency = frequency;
			this.phase = phase;
		}

		public WaveformControllerFunction( WaveformType type, Real baseVal, Real frequency, Real phase, Real amplitude )
			: base( true )
		{
			this.type = type;
			this.baseVal = baseVal;
			this.frequency = frequency;
			this.phase = phase;
			this.amplitude = amplitude;
		}

		public WaveformControllerFunction( WaveformType type )
			: base( true )
		{
			this.type = type;
		}

		#endregion

		#region Methods

		public override Real Execute( Real sourceValue )
		{
			var input = AdjustInput( sourceValue*this.frequency )%1f;
			var output = 0.0f;

			//For simplicity, factor input down to {0,1}
			//Use looped subtract rather than divide / round
			while ( input >= 1.0 )
			{
				input -= 1.0f;
			}
			while ( input < 0.0 )
			{
				input += 1.0f;
			}

			// first, get output in range [-1,1] (typical for waveforms)
			switch ( this.type )
			{
				case WaveformType.Sine:
					output = Utility.Sin( input*Utility.TWO_PI );
					break;

				case WaveformType.Triangle:
					if ( input < 0.25f )
					{
						output = input*4;
					}
					else if ( input >= 0.25f && input < 0.75f )
					{
						output = 1.0f - ( ( input - 0.25f )*4 );
					}
					else
					{
						output = ( ( input - 0.75f )*4 ) - 1.0f;
					}

					break;

				case WaveformType.Square:
					if ( input <= 0.5f )
					{
						output = 1.0f;
					}
					else
					{
						output = -1.0f;
					}
					break;

				case WaveformType.Sawtooth:
					output = ( input*2 ) - 1;
					break;

				case WaveformType.InverseSawtooth:
					output = -( ( input*2 ) - 1 );
					break;
				case WaveformType.PulseWidthModulation:
					if ( input <= this.dutyCycle )
					{
						output = 1.0f;
					}
					else
					{
						output = -1.0f;
					}
					break;
			} // end switch

			// scale final output to range [0,1], and then by base and amplitude
			return this.baseVal + ( ( output + 1.0f )*0.5f*this.amplitude );
		}

		protected override Real AdjustInput( Real input )
		{
			var adjusted = base.AdjustInput( input );

			// if not using delta accumulation, adjust by phase value
			if ( !useDeltaInput )
			{
				adjusted += this.phase;
			}

			return adjusted;
		}

		#endregion

		#region Properties

		#endregion
	}
}