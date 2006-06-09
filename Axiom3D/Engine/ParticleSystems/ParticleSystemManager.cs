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

#region Namespace Declarations
using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
#endregion

#region Versioning Information
/// File								Revision
/// ===============================================
/// OgreBillboard.h		                1.13
/// OgreBillboard.cpp		            1.13
/// 
#endregion

namespace Axiom
{
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
    [Subsystem( "ParticleSystemManager" )]
    public sealed class ParticleSystemManager : IDisposable
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
        {
            if ( instance == null )
            {
                instance = this;
            }

            scriptPatterns.Add( "*.particle" );
            ResourceGroupManager.Instance.RegisterScriptLoader( this );
            
            // register the namespace
            Vfs.Instance.RegisterNamespace(new EmitterNamespaceExtender());
            Vfs.Instance.RegisterNamespace(new AffectorNamespaceExtender());

            // locate particle plugins
            List<PluginMetadataAttribute> particlePlugins =
                PluginManager.Instance.RequestSubsystemPlugins( this );

            foreach ( PluginMetadataAttribute meta in particlePlugins )
            {
                IPlugin particlePlugin = PluginManager.Instance.GetPlugin( meta.Name );
                particlePlugin.Start();
            }
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

        #region Delegates

        /// <summary>
        ///     Particle system attribute method definition.
        /// </summary>
        /// <param name="values">Attribute values.</param>
        /// <param name="system">Target particle system.</param>
        delegate void ParticleSystemAttributeParser( string[] values, ParticleSystem system );

        #endregion

        #region Fields

        /// <summary>
        ///     List of template particle systems.
        /// </summary>
        private Hashtable systemTemplateList = new Hashtable();

        /// <summary>
        ///     Actual instantiated particle systems (may be based on template, may be manual).
        /// </summary>
        private HashList systemList = new HashList();

        /// <summary>
        ///     Factories for named emitter type (can be extended using plugins).
        /// </summary>
        private Dictionary<string, ParticleEmitterFactory> emitterFactoryList = new Dictionary<string, ParticleEmitterFactory>();

        /// <summary>
        ///     Factories for named affector types (can be extended using plugins).
        /// </summary>
        private Dictionary<string, ParticleAffectorFactory> affectorFactoryList = new Dictionary<string, ParticleAffectorFactory>();

        /// <summary>
        ///     Map of renderer types to factories
        /// </summary>
        private Hashtable rendererFactoryList = new Hashtable();

        /// <summary>
        ///     List of available attibute parsers for script attributes.
        /// </summary>
        private Hashtable attribParsers = new Hashtable();

        private ArrayList scriptPatterns = new ArrayList();

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
        public ParticleSystem CreateTemplate( string name )
        {
            return CreateTemplate( name, "" );
        }
        public ParticleSystem CreateTemplate( string name, string resourceGroup )
        {
            ParticleSystem system = new ParticleSystem( name, resourceGroup );
            AddTemplate( name, system );

            return system;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the particle system to remove</param>
        public void RemoveTemplate( string name )
        {
            if ( !systemTemplateList.ContainsKey( name ) )
            {
                return;
            }
            ParticleSystem system = (ParticleSystem)systemTemplateList[ name ];
            systemTemplateList.Remove( name );
            //system.Dispose();
        }

        /// <summary>
        ///		Basic method for creating a blank particle system.
        ///	 </summary>
        ///	 <remarks>
        ///		This method creates a new, blank ParticleSystem instance and returns a reference to it.
        ///     The caller should not delete this object, it will be freed at system shutdown, or can
        ///     be released earlier using the destroySystem method.
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
            return CreateSystem( name, quota, ResourceGroupManager.DefaultResourceGroupName );
        }

        /// <summary>
        ///		Basic method for creating a blank particle system.
        ///	 </summary>
        ///	 <remarks>
        ///		This method creates a new, blank ParticleSystem instance and returns a reference to it.
        ///     The caller should not delete this object, it will be freed at system shutdown, or can
        ///     be released earlier using the destroySystem method.
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
        /// <param name="resourceGroup">The resource group which will be used to load dependent resources</param>
        /// <returns></returns>
        public ParticleSystem CreateSystem( string name, int quota, string resourceGroup )
        {
            ParticleSystem system = new ParticleSystem( name, resourceGroup );
            system.Quota = quota;
            systemList.Add( name, system );
            return system;
        }

        /// <summary>
        ///		Creates a particle system based on a template.
        ///	 </summary>
        ///	 <remarks>
        ///		This method creates a new ParticleSystem instance based on the named template and returns a 
        ///		reference to the caller. The caller should not delete this object, it will be freed at system shutdown, 
        ///    or can be released earlier using the destroySystem method.
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
        /// <returns></returns>
        public ParticleSystem CreateSystem( string name, string templateName )
        {
            if ( !systemTemplateList.ContainsKey( templateName ) )
            {
                throw new AxiomException( "Cannot create a particle system with template '{0}' because it does not exist.", templateName );
            }

            ParticleSystem templateSystem = (ParticleSystem)systemTemplateList[ templateName ];

            ParticleSystem system = CreateSystem( name, templateSystem.Quota, templateSystem.ResourceGroup );

            // copy template settings to the new system (do not return the template itself)
            templateSystem.CopyTo( system );

            return system;
        }

        /// <summary>
        /// Destroys a particle system, freeing it's memory and removing references to it in this class.
        /// </summary>
        /// <remarks>
        /// You should ensure that before calling this method, the particle system has been detached from 
        /// any SceneNode objects, and that no other objects are referencing it.
        /// </remarks>
        /// <param name="name">The name of the ParticleSystem to remove.</param>
        public void RemoveSystem( string name )
        {
            if ( !systemList.ContainsKey( name ) )
            {
                return;
            }
            ParticleSystem system = (ParticleSystem)systemList[ name ];
            systemList.Remove( name );
            //system.Dispose();
        }

        /// <summary>
        /// Destroys a particle system, freeing it's memory and removing references to it in this class.
        /// </summary>
        /// <remarks>
        /// You should ensure that before calling this method, the particle system has been detached from 
        /// any SceneNode objects, and that no other objects are referencing it.
        /// </remarks>
        /// <param name="sys">Reference to the ParticleSystem to be removed.</param>
        public void RemoveSystem( ParticleSystem sys )
        {
            if ( !systemList.ContainsKey( sys.Name ) )
            {
                return;
            }
            systemList.Remove( sys.Name );
            //sys.Dispose();
            sys = null;
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
        internal ParticleEmitter CreateEmitter( string emitterType )
        {
            ParticleEmitterFactory factory = (ParticleEmitterFactory)emitterFactoryList[ emitterType ];

            if ( factory == null )
            {
                throw new AxiomException( "Cannot find requested emitter '{0}'.", emitterType );
            }

            return factory.Create();
        }

        /// <summary>
        /// Internal method for removing an emitter.
        /// </summary>
        /// <remarks>
        /// This method is used to ask the factory to remove the instance passed in as a reference.
        /// </remarks>
        /// <param name="emitter">Reference to emitter to be removed. On return this reference will be null.</param>
        internal void RemoveEmitter( ParticleEmitter emitter )
        {
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
        /// Internal method for removing an affector.
        /// </summary>
        /// <remarks>
        /// This method is used to ask the factory to remove the instance passed in as a reference.
        /// </remarks>
        /// <param name="affector">Reference to affector to be removed. On return this reference will be null.</param>
        internal void RemoveAffector( ParticleAffector affector )
        {
        }

        /// <summary>
        /// Internal method for creating a new renderer from a factory.
        /// </summary>
        /// <param name="rendererType">rendererType String name of the renderer type to be created. A factory of this type must have been registered.</param>
        /// <returns>new ParticleSystemRenderer</returns>
        /// <remarks>
        ///    Used internally by the engine to create new ParticleSystemRenderer instances from named
        ///    factories. Applications should use the ParticleSystem.Renderer Property instead, 
        ///    which calls this method to create an instance.
        /// </remarks>
        internal ParticleSystemRenderer CreateRenderer( string rendererType )
        {
            ParticleSystemRendererFactory factory = (ParticleSystemRendererFactory)rendererFactoryList[ rendererType ];

            if ( factory == null )
            {
                throw new AxiomException( "Cannot find requested renderer '{0}'.", rendererType );
            }

            //return factory.Create();
            return null;
        }

        /// <summary>
        /// Internal method for removing an renderer.
        /// </summary>
        /// <remarks>
        /// This method is used to ask the factory to remove the instance passed in as a reference.
        /// </remarks>
        /// <param name="renderer">Reference to renderer to be removed. On return this reference will be null.</param>
        internal void RemoveRenderer( ParticleAffector renderer )
        {
        }


        /// <summary>
        ///		Internal method to init the particle systems.
        /// </summary>
        /// <remarks>
        ///		Since this method is dependent on other engine systems being started, this method will be called by the
        ///		engine when the render system is initialized.
        /// </remarks>
        internal void Initialize()
        {
            // add ourself as a listener for the frame started event
            Root.Instance.FrameStarted += new FrameEvent( RenderSystem_FrameStarted );

            // discover and register local attribute parsers
            RegisterParsers();

            ParseAllSources();
        }

        /// <summary>
        ///		Parses all particle system script files in resource folders and archives.
        /// </summary>
        private void ParseAllSources()
        {
            StringCollection particleFiles = ResourceManager.GetAllCommonNamesLike( "", ".particle" );

            foreach ( string file in particleFiles )
            {
                Stream data = ResourceManager.FindCommonResourceData( file );

                ParseScript( data );
            }
        }

        public void ParseScript( string script )
        {
            ParseScript( new StringReader( script ) );
        }

        public void ParseScript( Stream data )
        {
            ParseScript( new StreamReader( data, System.Text.Encoding.ASCII ) );
        }
        /// <summary>
        ///		Starts parsing an individual script file.
        /// </summary>
        /// <param name="data">Stream containing the script data.</param>
        public void ParseScript( TextReader script )
        {

            string line = "";
            ParticleSystem system = null;

            // parse through the data to the end
            while ( ( line = ParseHelper.ReadLine( script ) ) != null )
            {
                // ignore blank lines and comments
                if ( !( line.Length == 0 || line.StartsWith( "//" ) ) )
                {
                    if ( system == null )
                    {
                        system = CreateTemplate( line );

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

                        // read another line to skip the brace on the next line
                        script.ReadLine();

                        // new emitter
                        ParseEmitter( values[ 1 ], script, system );
                    }
                    else if ( line.StartsWith( "affector" ) )
                    {
                        string[] values = line.Split( ' ' );

                        // read another line to skip the brace on the next line
                        script.ReadLine();

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

        /// <summary>
        ///		Parses an attribute intended for the particle system itself.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="system"></param>
        private void ParseAttrib( string line, ParticleSystem system )
        {
            // split attribute line by spaces
            string[] values = line.Split( ' ' );

            // make sure this attribute exists
            if ( !attribParsers.ContainsKey( values[ 0 ] ) )
            {
                LogManager.Instance.Write( "Unknown particle system attribute: {0}", values[ 0 ] );
            }
            else
            {
                ParticleSystemAttributeParser parser =
                    (ParticleSystemAttributeParser)attribParsers[ values[ 0 ] ];

                // create a seperate parm list that has the command removed
                string[] parms = ParseHelper.GetParams( values );

                // call the parser method
                parser( parms, system );
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
            string[] values = line.Split( new char[] { ' ' }, 2 );

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
            string[] values = line.Split( new char[] { ' ' }, 2 );

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
            systemList.Clear();
            systemTemplateList.Clear();
        }

        #endregion

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
        public HashList ParticleSystems
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

        /// <summary>
        ///     List of available renderer factories.
        /// </summary>
        public Hashtable Renderers
        {
            get
            {
                return rendererFactoryList;
            }
        }

        /// <summary>
        ///     List of script patterns to load.
        /// </summary>
        public ArrayList ScriptPatterns
        {
            get
            {
                return scriptPatterns;
            }
        }

        /// <summary>
        ///     Loading Order
        /// </summary>
        public float LoadingOrder
        {
            get
            {
                return 1000.0F; // load late
            }
        }

        #endregion

        #region Script parser methods

        /// <summary>
        ///		Registers all attribute names with their respective parser.
        /// </summary>
        /// <remarks>
        ///		Methods meant to serve as attribute parsers should use a method attribute to 
        /// </remarks>
        private void RegisterParsers()
        {
            MethodInfo[] methods = this.GetType().GetMethods();

            // loop through all methods and look for ones marked with attributes
            for ( int i = 0; i < methods.Length; i++ )
            {
                // get the current method in the loop
                MethodInfo method = methods[ i ];

                // see if the method should be used to parse one or more material attributes
                AttributeParserAttribute[] parserAtts =
                    (AttributeParserAttribute[])method.GetCustomAttributes( typeof( AttributeParserAttribute ), true );

                // loop through each one we found and register its parser
                for ( int j = 0; j < parserAtts.Length; j++ )
                {
                    AttributeParserAttribute parserAtt = parserAtts[ j ];

                    switch ( parserAtt.ParserType )
                    {
                        // this method should parse a material attribute
                        case PARTICLE:
                            attribParsers.Add( parserAtt.Name, Delegate.CreateDelegate( typeof( ParticleSystemAttributeParser ), method ) );
                            break;

                    } // switch
                } // for
            } // for
        }

        [AttributeParser( "billboard_type", PARTICLE )]
        public static void ParseBillboardType( string[] values, ParticleSystem system )
        {
            if ( values.Length != 1 )
            {
                ParseHelper.LogParserError( "billboard_type", system.Name, "Wrong number of parameters." );
                return;
            }

            // lookup the real enum equivalent to the script value
            object val = ScriptEnumAttribute.Lookup( values[ 0 ], typeof( BillboardType ) );

            // if a value was found, assign it
            if ( val != null )
                system.BillboardType = (BillboardType)val;
            else
                ParseHelper.LogParserError( "billboard_type", system.Name, "Invalid enum value" );
        }

        [AttributeParser( "common_direction", PARTICLE )]
        public static void ParseCommonDirection( string[] values, ParticleSystem system )
        {
            if ( values.Length != 3 )
            {
                ParseHelper.LogParserError( "common_direction", system.Name, "Wrong number of parameters." );
                return;
            }

            system.CommonDirection = StringConverter.ParseVector3( values );
        }

        [AttributeParser( "cull_each", PARTICLE )]
        public static void ParseCullEach( string[] values, ParticleSystem system )
        {
            if ( values.Length != 1 )
            {
                ParseHelper.LogParserError( "cull_each", system.Name, "Wrong number of parameters." );
                return;
            }

            system.CullIndividually = StringConverter.ParseBool( values[ 0 ] );
        }

        [AttributeParser( "particle_height", PARTICLE )]
        public static void ParseHeight( string[] values, ParticleSystem system )
        {
            if ( values.Length != 1 )
            {
                ParseHelper.LogParserError( "particle_height", system.Name, "Wrong number of parameters." );
                return;
            }

            system.DefaultHeight = StringConverter.ParseFloat( values[ 0 ] );
        }

        [AttributeParser( "material", PARTICLE )]
        public static void ParseMaterial( string[] values, ParticleSystem system )
        {
            if ( values.Length != 1 )
            {
                ParseHelper.LogParserError( "material", system.Name, "Wrong number of parameters." );
                return;
            }

            system.MaterialName = values[ 0 ];
        }

        [AttributeParser( "quota", PARTICLE )]
        public static void ParseQuota( string[] values, ParticleSystem system )
        {
            if ( values.Length != 1 )
            {
                ParseHelper.LogParserError( "quota", system.Name, "Wrong number of parameters." );
                return;
            }

            system.Quota = int.Parse( values[ 0 ] );
        }

        [AttributeParser( "particle_width", PARTICLE )]
        public static void ParseWidth( string[] values, ParticleSystem system )
        {
            if ( values.Length != 1 )
            {
                ParseHelper.LogParserError( "particle_width", system.Name, "Wrong number of parameters." );
                return;
            }

            system.DefaultWidth = StringConverter.ParseFloat( values[ 0 ] );
        }

        #endregion

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
            for ( int i = 0; i < systemList.Count; i++ )
            {
                ParticleSystem system = (ParticleSystem)systemList[ i ];

                // ask the particle system to update itself based on the frame time
                system.Update( timeSinceLastFrame );
            }
        }

        /// <summary>
        ///		A listener that is added to the engine's render loop.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private void RenderSystem_FrameEnded( object source, FrameEventArgs e )
        {
            // Apply time factor
            float timeSinceLastFrame = timeFactor * e.TimeSinceLastFrame;

            // loop through and update each particle system
            for ( int i = 0; i < systemList.Count; i++ )
            {
                ParticleSystem system = (ParticleSystem)systemList[ i ];

                // ask the particle system to update itself based on the frame time
                system.Update( timeSinceLastFrame );
            }
        }

        #endregion Event Handlers

        #region IDisposable Members

        /// <summary>
        ///     Called when the engine is shutting down.
        /// </summary>
        public void Dispose()
        {
            //clear all the collections
            Clear();
            ResourceGroupManager.Instance.UnregisterScriptLoader( this );
            instance = null;
        }

        #endregion
    }
}
