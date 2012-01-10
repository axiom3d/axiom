#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2012 Axiom Project Team

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
using System.ComponentModel.Composition;
using Axiom.Core;
using Axiom.ParticleSystems;
using Axiom.Math;
using Axiom.Scripting;
using Axiom.ParticleFX.Factories;

#endregion Namespace Declarations

namespace Axiom.ParticleFX
{
	/// <summary>
	/// Summary description for ParticleFX.
	/// </summary>
#if ( WINDOWS_PHONE || SILVERLIGHT )
	[Export(typeof(IPlugin))]
#endif
	public class ParticleFX : IPlugin
	{
		#region IPlugin Members

		public void Initialize()
		{
			ParticleEmitterFactory emitterFactory;
			ParticleAffectorFactory affectorFactory;

			// box emitter
			emitterFactory = new BoxEmitterFactory();
			ParticleSystemManager.Instance.AddEmitterFactory( emitterFactory );

			// point emitter
			emitterFactory = new PointEmitterFactory();
			ParticleSystemManager.Instance.AddEmitterFactory( emitterFactory );

			// cylinder emitter
			emitterFactory = new CylinderEmitterFactory();
			ParticleSystemManager.Instance.AddEmitterFactory( emitterFactory );

			// ellipsoid emitter
			emitterFactory = new EllipsoidEmitterFactory();
			ParticleSystemManager.Instance.AddEmitterFactory( emitterFactory );

			// hollow ellipsoid emitter
			emitterFactory = new HollowEllipsoidEmitterFactory();
			ParticleSystemManager.Instance.AddEmitterFactory( emitterFactory );

			// ring emitter
			emitterFactory = new RingEmitterFactory();
			ParticleSystemManager.Instance.AddEmitterFactory( emitterFactory );

			// linear force affector
			affectorFactory = new LinearForceAffectorFactory();
			ParticleSystemManager.Instance.AddAffectorFactory( affectorFactory );

			// color fader affector
			affectorFactory = new ColorFaderAffectorFactory();
			ParticleSystemManager.Instance.AddAffectorFactory( affectorFactory );

			// color fader 2 affector
			affectorFactory = new ColorFaderAffector2Factory();
			ParticleSystemManager.Instance.AddAffectorFactory( affectorFactory );

			// color image affector
			affectorFactory = new ColorImageAffectorFactory();
			ParticleSystemManager.Instance.AddAffectorFactory( affectorFactory );

			// color interpolator affector
			affectorFactory = new ColorInterpolatorAffectorFactory();
			ParticleSystemManager.Instance.AddAffectorFactory( affectorFactory );

			// scale affector
			affectorFactory = new ScaleAffectorFactory();
			ParticleSystemManager.Instance.AddAffectorFactory( affectorFactory );

			// scale affector
			affectorFactory = new RotationAffectorFactory();
			ParticleSystemManager.Instance.AddAffectorFactory( affectorFactory );

			// deflector plane affector
			affectorFactory = new DeflectorPlaneAffectorFactory();
			ParticleSystemManager.Instance.AddAffectorFactory( affectorFactory );

			//direction randomizer affector
			affectorFactory = new DirectionRandomizerAffectorFactory();
			ParticleSystemManager.Instance.AddAffectorFactory( affectorFactory );
		}

		public void Shutdown()
		{
			// TODO:  Add ParticleFX.Stop implementation
		}

		#endregion IPlugin Members
	}
}