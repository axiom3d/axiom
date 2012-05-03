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
using System.Reflection;
using Axiom.Collections;
using Axiom.Math;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleSystems
{
	///<summary>
	///  Abstract class defining the interface to be implemented by particle affectors.
	///</summary>
	///<remarks>
	///  Particle affectors modify particles in a particle system over their lifetime. They can be grouped into types, e.g. 'vector force' affectors, 'fader' affectors etc; each type will modify particles in a different way, using different parameters. <para /> Because there are so many types of affectors you could use, the engine chooses not to dictate the available types. It comes with some in-built, but allows plugins or applications to extend the affector types available. This is done by subclassing ParticleAffector to have the appropriate emission behavior you want, and also creating a subclass of ParticleAffectorFactory which is responsible for creating instances of your new affector type. You register this factory with the ParticleSystemManager using AddAffectorFactory, and from then on affectors of this type can be created either from code or through .particle scripts by naming the type. <para /> This same approach is used for ParticleEmitters (which are the source of particles in a system). This means that the engine is particularly flexible when it comes to creating particle system effects, with literally infinite combinations of affector and affector types, and parameters within those types.
	///</remarks>
	public abstract class ParticleAffector
	{
		#region Fields

		/// <summary>
		///   Name of the affector type. Must be initialized by subclasses.
		/// </summary>
		protected string type;

		protected AxiomCollection<IPropertyCommand> commandTable = new AxiomCollection<IPropertyCommand>();
		protected ParticleSystem parent;

		#endregion Fields

		#region Constructors

		///<summary>
		///  Default constructor
		///</summary>
		public ParticleAffector( ParticleSystem parent )
		{
			this.parent = parent;
			RegisterCommands();
		}

		#endregion Constructors

		#region Properties

		///<summary>
		///  Gets the type name of this affector.
		///</summary>
		public string Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
			}
		}

		#endregion Properties

		#region Methods

		///<summary>
		///  Method called to allow the affector to 'do it's stuff' on all active particles in the system.
		///</summary>
		///<remarks>
		///  This is where the affector gets the chance to apply it's effects to the particles of a system. The affector is expected to apply it's effect to some or all of the particles in the system passed to it, depending on the affector's approach.
		///</remarks>
		///<param name="system"> Reference to a ParticleSystem to affect. </param>
		///<param name="timeElapsed"> The number of seconds which have elapsed since the last call. </param>
		public abstract void AffectParticles( ParticleSystem system, Real timeElapsed );

		public virtual void CopyTo( ParticleAffector affector )
		{
			// loop through all registered commands and copy from this instance to the target instance
			foreach ( var key in commandTable.Keys )
			{
				// get the value of the param from this instance
				var val = ( (IPropertyCommand)commandTable[ key ] ).Get( this );

				// set the param on the target instance
				affector.SetParam( key, val );
			}
		}

		///<summary>
		///  Method called to allow the affector to 'do it's stuff' on all active particles in the system.
		///</summary>
		///<remarks>
		///  This is where the affector gets the chance to apply it's effects to the particles of a system. The affector is expected to apply it's effect to some or all of the particles in the system passed to it, depending on the affector's approach.
		///</remarks>
		///<param name="particle"> Reference to a ParticleSystem to affect. </param>
		public virtual void InitParticle( ref Particle particle )
		{
			// do nothing by default
		}

		#endregion Methods

		#region Script parser methods

		///<summary>
		///</summary>
		public bool SetParam( string name, string val )
		{
			if ( commandTable.ContainsKey( name ) )
			{
				var command = (IPropertyCommand)commandTable[ name ];

				command.Set( this, val );

				return true;
			}

			return false;
		}

		///<summary>
		///  Registers all attribute names with their respective parser.
		///</summary>
		///<remarks>
		///  Methods meant to serve as attribute parsers should use a method attribute to
		///</remarks>
		protected void RegisterCommands()
		{
			var baseType = GetType();

			do
			{
				var types = baseType.GetNestedTypes( BindingFlags.NonPublic | BindingFlags.Public );

				// loop through all methods and look for ones marked with attributes
				for ( var i = 0; i < types.Length; i++ )
				{
					// get the current method in the loop
					var type = types[ i ];

					// get as many command attributes as there are on this type
					var commandAtts =
						(ScriptablePropertyAttribute[])type.GetCustomAttributes( typeof ( ScriptablePropertyAttribute ), true );

					// loop through each one we found and register its command
					for ( var j = 0; j < commandAtts.Length; j++ )
					{
						var commandAtt = commandAtts[ j ];

						commandTable.Add( commandAtt.ScriptPropertyName, (IPropertyCommand)Activator.CreateInstance( type ) );
					} // for
				} // for

				// get the base type of the current type
				baseType = baseType.BaseType;
			}
			while ( baseType != typeof ( object ) );
		}

		#endregion Script parser methods
	}
}