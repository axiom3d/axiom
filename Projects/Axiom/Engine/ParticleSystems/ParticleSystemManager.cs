#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Axiom.Collections;
using Axiom.Core;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleSystems
{

	#region Delegates


	#endregion Delegates
	/// <summary>
	///		Manages particle systems, particle system scripts (templates) and the available emitter & affector factories.
	///	 </summary>
	///	 <remarks>
	///		This singleton class is responsible for creating and managing particle systems. All particle
	///		systems must be created and destroyed using this object. Remember that like all other SceneObject
	///		subclasses, ParticleSystems do not get rendered until they are attached to a SceneNode object.
	///		<p/>
	///		This class also manages factories for ParticleEmitter and ParticleAffector classes. To enable easy
	///		extensions to the types of emitters (particle sources) and affectors (particle modifiers), the
	///		ParticleSystemManager lets plugins or applications register factory classes which submit new
	///		subclasses to ParticleEmitter and ParticleAffector. The engine comes with a number of them already provided,
	///		such as cone, sphere and box-shaped emitters, and simple affectors such as constant directional force
	///		and color faders. However using this registration process, a plugin can create any behavior
	///		required.
	///		<p/>
	///		This class also manages the loading and parsing of particle system scripts, which are XML files
	///		describing named particle system templates. Instances of particle systems using these templates can
	///		then be created easily through the CreateParticleSystem method.
	/// </remarks>
	public sealed class ParticleSystemManager : DisposableObject, IScriptLoader
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static ParticleSystemManager instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		internal ParticleSystemManager()
            : base()
		{
			if ( instance == null )
			{
				instance = this;
			}

#if !AXIOM_USENEWCOMPILERS
			_scriptPatterns.Add( "*.particle" );
			ResourceGroupManager.Instance.RegisterScriptLoader( this );
#endif // AXIOM_USENEWCOMPILERS

			//TODO : MovableObjectFactory : _psFactory = new new ParticleSystemFactory();
			//TODO : MovableObjectFactory : Root.Instance.RegisterMovableObjectFactory( _psFactory );
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static ParticleSystemManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

		#region Fields

		// In Ogre, this is actually a global!
		private static BillboardParticleRendererFactory billboardRendererFactory;

		//TODO : MovableObjectFactory : // MovalbeObjectFactory for Particle Systems
		//TODO : MovableObjectFactory : private static ParticleSystemFactory _psFactory;

		/// <summary>
		///     List of template particle systems.
		/// </summary>
		private Dictionary<string, ParticleSystem> systemTemplateList = new Dictionary<string, ParticleSystem>();
		/// <summary>
		///     Actual instantiated particle systems (may be based on template, may be manual).
		/// </summary>
		private Dictionary<string, ParticleSystem> systemList = new Dictionary<string, ParticleSystem>();
		/// <summary>
		///     Factories for named emitter type (can be extended using plugins).
		/// </summary>
		private Dictionary<string, ParticleEmitterFactory> emitterFactoryList = new Dictionary<string, ParticleEmitterFactory>();
		/// <summary>
		///     Factories for named affector types (can be extended using plugins).
		/// </summary>
		private Dictionary<string, ParticleAffectorFactory> affectorFactoryList = new Dictionary<string, ParticleAffectorFactory>();
		/// <summary>
		///     Factories for named renderer types (can be extended using plugins).
		/// </summary>
		private Dictionary<string, ParticleSystemRendererFactory> rendererFactoryList = new Dictionary<string, ParticleSystemRendererFactory>();


		/// <summary>
		///     Controls time. (1.0 is real time)
		/// </summary>
		private float timeFactor = 1.0f;

		/// <summary>
		///     Default param constants.
		/// </summary>
		const int DefaultQuota = 500;

		/// <summary>
		///     Script parsing constants.
		/// </summary>
		const string PARTICLE = "Particle";

		#endregion Fields

		#region Methods

		/// <summary>
		///		Adds a new 'factory' object for emitters to the list of available emitter types.
		///	 </summary>
		///	 <remarks>
		///		This method allows plugins etc to add new particle emitter types. Particle emitters
		///		are sources of particles, and generate new particles with their start positions, colors and
		///		momentums appropriately. Plugins would create new subclasses of ParticleEmitter which
		///		emit particles a certain way, and register a subclass of ParticleEmitterFactory to create them (since multiple
		///		emitters can be created for different particle systems).
		///		<p/>
		///		All particle emitter factories have an assigned name which is used to identify the emitter
		///		type. This must be unique.
		/// </remarks>
		/// <param name="factory"></param>
		public void AddEmitterFactory( ParticleEmitterFactory factory )
		{
			emitterFactoryList.Add( factory.Name, factory );

			LogManager.Instance.Write( "Particle Emitter type '{0}' registered.", factory.Name );
		}

		/// <summary>
		///		Adds a new 'factory' object for affectors to the list of available affector types.
		///	 </summary>
		///	  <remarks>
		///		This method allows plugins etc to add new particle affector types. Particle
		///		affectors modify the particles in a system a certain way such as affecting their direction
		///		or changing their color, lifespan etc. Plugins would
		///		create new subclasses of ParticleAffector which affect particles a certain way, and register
		///		a subclass of ParticleAffectorFactory to create them.
		///		<p/>
		///		All particle affector factories have an assigned name which is used to identify the affector
		///		type. This must be unique.
		/// </remarks>
		/// <param name="factory"></param>
		public void AddAffectorFactory( ParticleAffectorFactory factory )
		{
			affectorFactoryList.Add( factory.Name, factory );

			LogManager.Instance.Write( "Particle Affector type '{0}' registered.", factory.Name );
		}

		/// <summary>
		/// Registers a factory class for creating ParticleSystemRenderer instances.
		/// </summary>
		/// <param name="factory">
		/// factory Pointer to a ParticleSystemRendererFactory subclass created by the plugin or application code.
		/// </param>
		/// <remarks>
		/// Note that the object passed to this function will not be destroyed by the ParticleSystemManager,
		/// since it may have been allocted on a different heap in the case of plugins. The caller must
		/// destroy the object later on, probably on plugin shutdown.
		/// </remarks>
		public void AddRendererFactory( ParticleSystemRendererFactory factory )
		{
			rendererFactoryList.Add( factory.Type, factory );

			LogManager.Instance.Write( "Particle Renderer type '{0}' registered.", factory.Type );
		}

		public ParticleSystemRenderer CreateRenderer( string rendererType )
		{
			ParticleSystemRendererFactory factory;
			if ( rendererFactoryList.TryGetValue( rendererType, out factory ) )
				return factory.CreateInstance( rendererType );
			throw new Exception( "Cannot find requested renderer type." );
		}

		/// <summary>
		///		Adds a new particle system template to the list of available templates.
		///	 </summary>
		///	 <remarks>
		///		Instances of particle systems in a scene are not normally unique - often you want to place the
		///		same effect in many places. This method allows you to register a ParticleSystem as a named template,
		///		which can subsequently be used to create instances using the CreateSystem method.
		///		<p/>
		///		Note that particle system templates can either be created programmatically by an application
		///		and registered using this method, or they can be defined in a XML script file which is
		///		loaded by the engine at startup, very much like Material scripts.
		/// </remarks>
		/// <param name="name">The name of the template. Must be unique across all templates.</param>
		/// <param name="system">A reference to a particle system to be used as a template.</param>
		public void AddTemplate( string name, ParticleSystem system )
		{
			systemTemplateList.Add( name, system );
		}

		/// <summary>
		///		Create a new particle system template.
		/// </summary>
		/// <remarks>
		///		This method is similar to the AddTemplate method, except this just creates a new template
		///		and returns a reference to it to be populated. Use this when you don't already have a system
		///		to add as a template and just want to create a new template which you will build up at runtime.
		/// </remarks>
		/// <param name="name"></param>
		/// <param name="resourceGroup">The name of the resource group which will be used to load any dependent resources.</param>
		/// <returns>returns a reference to a ParticleSystem template to be populated</returns>
		public ParticleSystem CreateTemplate( string name, string resourceGroup )
		{
			if ( systemTemplateList.ContainsKey( name ) )
			{
				throw new Exception( "ParticleSystem template with name '" + name + "' already exists." );
			}
			ParticleSystem system = new ParticleSystem( name, resourceGroup );
			AddTemplate( name, system );

			return system;
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public ParticleSystem CreateSystem( string name )
		{
			// create a system with a default quota
			return CreateSystem( name, DefaultQuota );
		}

		public ParticleSystem CreateSystem( string name, string templateName )
		{
			return CreateSystem( name, templateName, DefaultQuota );
		}

		/// <summary>
		///		Basic method for creating a blank particle system.
		///	 </summary>
		///	 <remarks>
		///		This method creates a new, blank ParticleSystem instance and returns a reference to it.
		///		<p/>
		///		The instance returned from this method won't actually do anything because on creation a
		///		particle system has no emitters. The caller should manipulate the instance through it's
		///		ParticleSystem methods to actually create a real particle effect.
		///		<p/>
		///		Creating a particle system does not make it a part of the scene. As with other SceneObject
		///		subclasses, a ParticleSystem is not rendered until it is attached to a SceneNode.
		/// </remarks>
		/// <param name="name">The name to give the ParticleSystem.</param>
		/// <param name="quota">The maximum number of particles to allow in this system.</param>
		/// <returns></returns>
		public ParticleSystem CreateSystem( string name, int quota )
		{
			ParticleSystem system = new ParticleSystem( name );
			system.ParticleQuota = quota;
			systemList.Add( name, system );

			return system;
		}

		public void RemoveSystem( string name )
		{
			systemList.Remove( name );
		}

		/// <summary>
		///		Creates a particle system based on a template.
		///	 </summary>
		///	 <remarks>
		///		This method creates a new ParticleSystem instance based on the named template and returns a
		///		reference to the caller.
		///		<p/>
		///		Each system created from a template takes the template's settings at the time of creation,
		///		but is completely separate from the template from there on.
		///		<p/>
		///		Creating a particle system does not make it a part of the scene. As with other SceneObject
		///		subclasses, a ParticleSystem is not rendered until it is attached to a SceneNode.
		///		<p/>
		///		This is probably the more useful particle system creation method since it does not require manual
		///		setup of the system.
		/// </remarks>
		/// <param name="name">The name to give the new particle system instance.</param>
		/// <param name="templateName">The name of the template to base the new instance on.</param>
		/// <param name="quota">The maximum number of particles to allow in this system (can be changed later).</param>
		/// <returns></returns>
		public ParticleSystem CreateSystem( string name, string templateName, int quota )
		{
			if ( !systemTemplateList.ContainsKey( templateName ) )
			{
				LogManager.Instance.Write( "Cannot create a particle system with template '{0}' because it does not exist, using NullParticleSystem.", templateName );
				return CreateSystem( name, "NullParticleSystem" );
			}

			ParticleSystem templateSystem = systemTemplateList[ templateName ];

			ParticleSystem system = CreateSystem( name, quota );

			// copy template settings to the new system (do not return the template itself)
			templateSystem.CopyTo( system );

			return system;
		}

		/// <summary>
		///		Internal method for creating a new emitter from a factory.
		/// </summary>
		/// <remarks>
		///		Used internally by the engine to create new ParticleEmitter instances from named
		///		factories. Applications should use the ParticleSystem.AddEmitter method instead,
		///		which calls this method to create an instance.
		/// </remarks>
		/// <param name="emitterType">string name of the emitter type to be created. A factory of this type must have been registered.</param>
		internal ParticleEmitter CreateEmitter( string emitterType, ParticleSystem ps )
		{
			ParticleEmitterFactory factory = emitterFactoryList[ emitterType ];

			if ( factory == null )
			{
				throw new AxiomException( "Cannot find requested emitter '{0}'.", emitterType );
			}

			return factory.Create( ps );
		}

		/// <summary>
		///		Internal method for creating a new affector from a factory.
		/// </summary>
		/// <remarks>
		///		Used internally by the engine to create new ParticleAffector instances from named
		///		factories. Applications should use the ParticleSystem.AddAffector method instead,
		///		which calls this method to create an instance.
		/// </remarks>
		/// <param name="emitterType">string name of the affector type to be created. A factory of this type must have been registered.</param>
		internal ParticleAffector CreateAffector( string affectorType )
		{
			ParticleAffectorFactory factory = (ParticleAffectorFactory)affectorFactoryList[ affectorType ];

			if ( factory == null )
			{
				throw new AxiomException( "Cannot find requested affector '{0}'.", affectorType );
			}

			return factory.Create();
		}

		/// <summary>
		///		Internal method to init the particle systems.
		/// </summary>
		/// <remarks>
		///		Since this method is dependent on other engine systems being started, this method will be called by the
		///		engine when the render system is initialized.
		/// </remarks>
		public void Initialize()
		{

			// Create Billboard renderer factory
			billboardRendererFactory = new BillboardParticleRendererFactory();
			AddRendererFactory( billboardRendererFactory );

			CreateTemplate( "NullParticleSystem", ResourceGroupManager.DefaultResourceGroupName );

		}

		/// <summary>
		///		Parses an attribute intended for the particle system itself.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="system"></param>
		private void ParseAttrib( string line, ParticleSystem system )
		{
			// Split params on space or tab
			char[] delims = { '\t', ' ' };
			string[] values = StringConverter.Split( line, delims, 2 );
			// Look up first param (command setting)
			if ( !system.SetParameter( values[ 0 ], values[ 1 ] ) )
			{
				// Attribute not supported by particle system, try the renderer
				ParticleSystemRenderer renderer = system.Renderer;
				if ( renderer != null )
				{
					if ( !renderer.SetParameter( values[ 0 ], values[ 1 ] ) )
					{
						LogManager.Instance.Write( "Bad particle system attribute line: '{0}' in {1} (tried renderer)",
												  line, system.Name );
					}
				}
				else
				{
					// BAD command. BAD!
					LogManager.Instance.Write( "Bad particle system attribute line: '{0}' in {1} (no renderer)",
											  line, system.Name );
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="line"></param>
		/// <param name="system"></param>
		private void ParseEmitter( string type, TextReader script, ParticleSystem system )
		{
			ParticleEmitter emitter = system.AddEmitter( type );

			string line = "";

			while ( line != null )
			{
				line = ParseHelper.ReadLine( script );

				if ( !( line.Length == 0 || line.StartsWith( "//" ) ) )
				{
					if ( line == "}" )
					{
						// finished with this emitter
						break;
					}
					else
					{
						ParseEmitterAttrib( line.ToLower(), emitter );
					}
				} // if
			} // while
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="line"></param>
		/// <param name="system"></param>
		private void ParseAffector( string type, TextReader script, ParticleSystem system )
		{
			ParticleAffector affector = system.AddAffector( type );

			string line = "";

			while ( line != null )
			{
				line = ParseHelper.ReadLine( script );

				if ( !( line.Length == 0 || line.StartsWith( "//" ) ) )
				{
					if ( line == "}" )
					{
						// finished with this affector
						break;
					}
					else
					{
						ParseAffectorAttrib( line.ToLower(), affector );
					}
				} // if
			} // while
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="line"></param>
		/// <param name="emitter"></param>
		private void ParseEmitterAttrib( string line, ParticleEmitter emitter )
		{
			string[] values = StringConverter.Split( line, new char[] { ' ' }, 2 );

			if ( !( emitter.SetParam( values[ 0 ], values[ 1 ] ) ) )
			{
				ParseHelper.LogParserError( values[ 0 ], emitter.Type, "Command not found." );
			}
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="line"></param>
		/// <param name="affector"></param>
		private void ParseAffectorAttrib( string line, ParticleAffector affector )
		{
			string[] values = StringConverter.Split( line, new char[] { ' ' }, 2 );

			if ( !( affector.SetParam( values[ 0 ], values[ 1 ] ) ) )
			{
				ParseHelper.LogParserError( values[ 0 ], affector.Type, "Command not found." );
			}
		}

		public void Clear()
		{
			// clear all collections
			emitterFactoryList.Clear();
			affectorFactoryList.Clear();

            foreach (ParticleSystem system in this.systemList.Values)
            {
                if (!system.IsDisposed)
                    system.Dispose();
            }
			systemList.Clear();
            systemList = null;

            foreach (ParticleSystem system in this.systemTemplateList.Values)
            {
                if (!system.IsDisposed)
                    system.Dispose();
            }
			systemTemplateList.Clear();
            systemTemplateList = null;
		}

		#endregion Methods

		#region Properties

		/// <summary>
		///		Get/Set the relative speed of time as perceived by particle systems.
		///	 </summary>
		///	 <remarks>
		///		Normally particle systems are updated automatically in line with the real
		///		passage of time. This method allows you to change that, so that
		///		particle systems are told that the time is passing slower or faster than it
		///		actually is. Use this to globally speed up / slow down particle systems.
		/// </remarks>
		public float TimeFactor
		{
			get
			{
				return timeFactor;
			}
			set
			{
				timeFactor = value;
			}
		}

		/// <summary>
		///		List of available particle systems.
		/// </summary>
		public Dictionary<string, ParticleSystem> ParticleSystems
		{
			get
			{
				return systemList;
			}
		}

		/// <summary>
		///     List of available affector factories.
		/// </summary>
		public Dictionary<string, ParticleAffectorFactory> Affectors
		{
			get
			{
				return affectorFactoryList;
			}
		}

		/// <summary>
		///     List of available emitter factories.
		/// </summary>
		public Dictionary<string, ParticleEmitterFactory> Emitters
		{
			get
			{
				return emitterFactoryList;
			}
		}

		#endregion Properties

		#region Event Handlers

		/// <summary>
		///		A listener that is added to the engine's render loop.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		/// <returns></returns>
		private void RenderSystem_FrameStarted( object source, FrameEventArgs e )
		{
			// Apply time factor
			float timeSinceLastFrame = timeFactor * e.TimeSinceLastFrame;

			// loop through and update each particle system
			foreach ( ParticleSystem system in systemList.Values )
			{
				// ask the particle system to update itself based on the frame time
				system.Update( timeSinceLastFrame );
			}
		}

		#endregion Event Handlers

		#region IDisposable Members

        /// <summary>
        /// Called when the engine is shutting down.
        /// </summary>
        /// <param name="disposeManagedResources"></param>
        protected override void dispose(bool disposeManagedResources)
        {
            if (!this.IsDisposed)
            {
                if (disposeManagedResources)
                {
                    //clear all the collections
                    Clear();

                    //TODO : MovableObjectFactory : Root.Instance.UnregisterMovableObjectFactory( _psFactory );
                    //TODO : MovableObjectFactory : _psFactory = null;
                    instance = null;
                }
            }

            base.dispose(disposeManagedResources);
        }

		#endregion IDisposable Members

		#region IScriptLoader Implementation

		private List<string> _scriptPatterns = new List<string>();
		public List<string> ScriptPatterns
		{
			get
			{
				return _scriptPatterns;
			}
		}

		public void ParseScript( Stream stream, string groupName, string fileName )
		{
			string line = "";
			ParticleSystem system = null;

			TextReader script = new StreamReader( stream, System.Text.Encoding.UTF8 );

			// parse through the data to the end
			while ( ( line = ParseHelper.ReadLine( script ) ) != null )
			{
				// ignore blank lines and comments
				if ( !( line.Length == 0 || line.StartsWith( "//" ) ) )
				{
					if ( system == null )
					{
						system = CreateTemplate( line, groupName );
						// read another line to skip the beginning brace of the current particle system
						script.ReadLine();
					}
					else if ( line == "}" )
					{
						// end of current particle template
						system = null;
					}
					else if ( line.StartsWith( "emitter" ) )
					{
						string[] values = line.Split( ' ' );

						if ( values.Length < 2 )
						{
							// Oops, bad emitter
							LogManager.Instance.Write( "Bad particle system emitter line: '" + line + "' in " + system.Name );
							script.ReadLine();
						}
						// read another line to skip the brace on the next line
						script.ReadLine();
						// new emitter
						ParseEmitter( values[ 1 ], script, system );
					}
					else if ( line.StartsWith( "affector" ) )
					{
						string[] values = line.Split( ' ' );
						if ( values.Length < 2 )
						{
							// Oops, bad affector
							LogManager.Instance.Write( "Bad particle system affector line: '" + line + "' in " + system.Name );
							script.ReadLine();
						}

						// read another line to skip the brace on the next line
						script.ReadLine();
						// new affector
						ParseAffector( values[ 1 ], script, system );
					}
					else
					{
						// attribute line
						ParseAttrib( line.ToLower(), system );
					} // if
				} // if
			} // while
		}

		public float LoadingOrder
		{
			get
			{
				// load late
				return 1000.0f;
			}
		}

		#endregion IScriptLoader Implementation
	}

	#region MovableObjectFactory implementation

	//TODO: This class is not an inheritor of MovableObject (it is in Ogre)

	//public class ParticleSystemManagerFactory : MovableObjectFactory
	//{
	//    public static string Factory_Type_Name = "ParticleSystemManager";

	//    protected override MovableObject _createInstance(string name, NamedParameterList param)
	//    {
	//        return new ManualObject(name);
	//    }

	//    public override void DestroyInstance(MovableObject obj)
	//    {
	//        obj = null;
	//    }
	//}

	#endregion MovableObjectFactory implementation

}