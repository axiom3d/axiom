#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Drawing;
using Axiom.Core;
using Axiom.ParticleSystems;
using Axiom.MathLib;
using Axiom.Scripting;

namespace ParticleFX
{
	public enum ForceApplication
	{
		[ScriptEnum("average")]
		Average,

		[ScriptEnum("add")]
		Add
	}

	/// <summary>
	/// Summary description for LinearForceAffector.
	/// </summary>
	public class LinearForceAffector : ParticleAffector
	{
		protected ForceApplication forceApp = ForceApplication.Add;
		protected Vector3 forceVector = Vector3.Zero;

		const string LINEAR_FORCE = "LinearForce";

		public LinearForceAffector()
		{
			// HACK: See if there is better way to do this
			this.type = "LinearForce";
		}

		public override void AffectParticles(ParticleSystem system, float timeElapsed)
		{
			Vector3 scaledVector = Vector3.Zero;

			if(forceApp == ForceApplication.Add)
			{
				// scale force by time
				scaledVector = forceVector * timeElapsed;
			}

			// affect each particle
			for(int i = 0; i < system.Particles.Count; i++)
			{
				Particle p = (Particle)system.Particles[i];

				if(forceApp == ForceApplication.Add)
					p.Direction += scaledVector;
				else // Average
					p.Direction = (p.Direction + forceVector) / 2;
			}
		}

		public Vector3 Force
		{
			get { return forceVector; }
			set { forceVector = value; }
		}

		public ForceApplication ForceApplication
		{
			get { return forceApp; }
			set { forceApp = value; }
		}

		#region Script parser methods

		[AttributeParser("force_vector", LINEAR_FORCE)]
		public static void ParseForceVector(string[] values, params object[] objects) {
			LinearForceAffector affector = objects[0] as LinearForceAffector;

			affector.Force = ParseHelper.ParseVector3(values);
		}

		[AttributeParser("force_application", LINEAR_FORCE)]
		public static void ParseForceApplication(string[] values, params object[] objects) 
		{
			LinearForceAffector affector = objects[0] as LinearForceAffector;

			// lookup the real enum equivalent to the script value
			object val = ScriptEnumAttribute.Lookup(values[0], typeof(ForceApplication));

			// if a value was found, assign it
			if(val != null)
				affector.ForceApplication = ((ForceApplication)val);
			else
				ParseHelper.LogParserError(values[0], affector.Type, "Invalid enum value");
		}

		#endregion Script parser methods
	}
}
