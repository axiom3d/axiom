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
using Axiom.Media;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleFX
{
	/// <summary>
	/// Summary description for ColorFaderAffector.
	/// </summary>
	public class ColorImageAffector : ParticleAffector
	{
		protected Image colorImage;
		protected String colorImageName;
		protected bool colorImageLoaded;

		private const float div_255 = 1.0f / 255.0f;

		public ColorImageAffector( ParticleSystem psys )
			: base( psys )
		{
			this.type = "ColourImage";
		}

		public String ColorImageName
		{
			get
			{
				return colorImageName;
			}
			set
			{
				colorImageName = value;
			}
		}

		/// <see cref="ParticleAffector.InitParticle"/>
		public override void InitParticle( ref Particle particle )
		{
			if ( !colorImageLoaded )
			{
				loadImage();
			}

			particle.Color = colorImage.GetColorAt( 0, 0, 0 );
		}

		/// <see cref="ParticleAffector.AffectParticles"/>
		public override void AffectParticles( ParticleSystem system, Real timeElapsed )
		{
			if ( !colorImageLoaded )
			{
				loadImage();
			}

			int width = colorImage.Width - 1;
			float height = colorImage.Height - 1;

			// loop through the particles
			for ( int i = 0; i < system.Particles.Count; i++ )
			{
				Particle p = (Particle)system.Particles[ i ];

				// life_time, float_index, index and position are CONST in OGRE, but errors here

				// We do not have the concept of a total time to live!
				float life_time = p.totalTimeToLive;
				float particle_time = 1.0f - ( p.timeToLive / life_time );

				if ( particle_time > 1.0f )
				{
					particle_time = 1.0f;
				}
				if ( particle_time < 0.0f )
				{
					particle_time = 0.0f;
				}

				float float_index = particle_time * width;
				int index = (int)float_index;
				int position = index * 4;

				if ( index <= 0 )
				{
					p.Color = colorImage.GetColorAt( 0, 0, 0 );
				}
				else if ( index >= width )
				{
					p.Color = colorImage.GetColorAt( width, 0, 0 );
				}
				else
				{
					// fract, to_color and from_color are CONST in OGRE, but errors here
					float fract = float_index - (float)index;
					float toColor = fract;
					float fromColor = ( 1 - toColor );

					ColorEx from = colorImage.GetColorAt( index, 0, 0 ), to = colorImage.GetColorAt( index + 1, 0, 0 );

					p.Color.r = ( from.r * fromColor ) + ( to.r * toColor );
					p.Color.g = ( from.g * fromColor ) + ( to.g * toColor );
					p.Color.b = ( from.b * fromColor ) + ( to.b * toColor );
					p.Color.a = ( from.a * fromColor ) + ( to.a * toColor );
				}
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private void loadImage()
		{
			colorImage = Image.FromFile( colorImageName, parent.ResourceGroupName );

			var format = colorImage.Format;

			if ( !PixelUtil.IsAccessible( format ) )
			{
				throw new AxiomException( "Error: Image is not accessible (rgba) image." );
			}

			colorImageLoaded = true;
		}

		#region Command definition classes

		[ScriptableProperty( "image", "Image for color alterations.", typeof ( ParticleAffector ) )]
		public class ImageCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				ColorImageAffector affector = target as ColorImageAffector;
				return affector.ColorImageName;
			}

			public void Set( object target, string val )
			{
				ColorImageAffector affector = target as ColorImageAffector;
				affector.ColorImageName = val;
			}

			#endregion IPropertyCommand Members
		}

		#endregion Command definition classes
	}
}
