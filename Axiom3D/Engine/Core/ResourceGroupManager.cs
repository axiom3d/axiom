#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#endregion Namespace Declarations

namespace Axiom
{

    /// <summary>
    /// This defines an interface which is called back during
    /// resource group loading to indicate the progress of the load. 
    /// </summary>
    /// <remarks>
    /// Resource group loading is in 2 phases - creating resources from 
    /// declarations (which includes parsing scripts), and loading
    /// resources. Note that you don't necessarily have to have both; it
    /// is quite possible to just parse all the scripts for a group (see
    /// ResourceGroupManager.InitialiseResourceGroup, but not to 
    /// load the resource group. 
    /// The sequence of events is (* signifies a repeating item):
    /// <ul>
    ///     <li>resourceGroupScriptingStarted</li>
    ///     <li>scriptParseStarted (*)</li>
    ///     <li>scriptParseEnded (*)</li>
    ///     <li>resourceGroupScriptingEnded</li>
    ///     <li>resourceGroupLoadStarted</li>
    ///     <li>resourceLoadStarted (*)</li>
    ///     <li>resourceLoadEnded (*)</li>
    ///     <li>worldGeometryStageStarted (*)</li>
    ///     <li>worldGeometryStageEnded (*)</li>
    ///     <li>resourceGroupLoadEnded</li>
    /// </ul>
    /// </remarks>
    /// <ogre name="ResourceGroupListener">
    ///     <file name="OgreResourceGroupManager.h"   revision="1.12.2.4" lastUpdated="5/14/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreResourceGroupManager.cpp" revision="1.16.2.10" lastUpdated="5/14/2006" lastUpdatedBy="Borrillis" />
    ///     <Borrillis>
    ///			Note: This has changed from a Class in OGRE to an interface here to better support derived classes
    ///     </Borrillis>
    /// </ogre> 
    public interface IResourceGroupListener
    {
        /// <summary>
        /// This event is fired when a resource group begins parsing scripts.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <param name="scriptCount">The number of scripts which will be parsed</param>
        void ResourceGroupScriptingStarted( string groupName, int scriptCount );

        /// <summary>
        /// This event is fired when a script is about to be parsed.
        /// </summary>
        /// <param name="scriptName">Name of the to be parsed</param>
        void ScriptParseStarted( string scriptName );

        /// <summary>
        /// This event is fired when the script has been fully parsed.
        /// </summary>
        void ScriptParseEnded();

        /// <summary>
        /// This event is fired when a resource group finished parsing scripts.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        void ResourceGroupScriptingEnded( string groupName );

        /// <summary>
        /// This event is fired  when a resource group begins loading.
        /// </summary>
        /// <param name="groupName">The name of the group being loaded</param>
        /// <param name="resourceCount">
        /// The number of resources which will be loaded, 
        /// including a number of stages required to load any linked world geometry
        /// </param>
        void ResourceGroupLoadStarted( string groupName, int resourceCount );

        /// <summary>
        /// This event is fired when a declared resource is about to be loaded. 
        /// </summary>
        /// <param name="resource">Weak reference to the resource loaded</param>
        void ResourceLoadStarted( Resource resource );

        /// <summary>
        /// This event is fired when the resource has been loaded. 
        /// </summary>
        void ResourceLoadEnded();

        /// <summary>
        /// This event is fired when a stage of loading linked world geometry 
        /// is about to start. The number of stages required will have been 
        /// included in the resourceCount passed in resourceGroupLoadStarted.
        /// </summary>
        /// <param name="description">Text description of what was just loaded</param>
        void WorldGeometryStageStarted( string description );

        /// <summary>
        /// This event is fired when a stage of loading linked world geometry 
        /// has been completed. The number of stages required will have been 
        /// included in the resourceCount passed in resourceGroupLoadStarted.
        /// </summary>
        /// <param name="description">Text description of what was just loaded</param>
        void WorldGeometryStageEnded();

        /// <summary>
        /// This event is fired when a resource group finished loading.
        /// </summary>
        void ResourceGroupLoadEnded( string groupName );
    }

    /// <summary>
    /// This singleton class manages the list of resource groups, and notifying
    /// the various resource managers of their obligations to load / unload
    /// resources in a group. It also provides facilities to monitor resource
    /// loading per group (to do progress bars etc), provided the resources 
    /// that are required are pre-registered.
    /// <para />
    /// Defining new resource groups,  and declaring the resources you intend to
    /// use in advance is optional, however it is a very useful feature. In addition, 
    /// if a ResourceManager supports the definition of resources through scripts, 
    ///	then this is the class which drives the locating of the scripts and telling
    ///	the ResourceManager to parse them. 
    /// @par
    ///	There are several states that a resource can be in (the concept, not the
    ///	object instance in this case):
    ///	<ol>
    ///	<li><b>Undefined</b>. Nobody knows about this resource yet. It might be
    ///	in the filesystem, but Ogre is oblivious to it at the moment - there 
    ///	is no Resource instance. This might be because it's never been declared
    ///	(either in a script, or using ResourceGroupManager::declareResource), or
    ///	it may have previously been a valid Resource instance but has been 
    ///	removed, either individually through ResourceManager::remove or as a group
    ///	through ResourceGroupManager::clearResourceGroup.</li>
    ///	<li><b>Declared</b>. Ogre has some forewarning of this resource, either
    ///	through calling ResourceGroupManager::declareResource, or by declaring
    ///	the resource in a script file which is on one of the resource locations
    ///	which has been defined for a group. There is still no instance of Resource,
    ///	but Ogre will know to create this resource when 
    ///	ResourceGroupManager::initialiseResourceGroup is called (which is automatic
    ///	if you declare the resource group before Root::initialise).</li>
    ///	<li><b>Unloaded</b>. There is now a Resource instance for this resource, 
    ///	although it is not loaded. This means that code which looks for this
    ///	named resource will find it, but the Resource is not using a lot of memory
    ///	because it is in an unloaded state. A Resource can get into this state
    ///	by having just been created by ResourceGroupManager::initialiseResourceGroup 
    ///	(either from a script, or from a call to declareResource), by 
    ///	being created directly from code (ResourceManager::create), or it may 
    ///	have previously been loaded and has been unloaded, either individually
    ///	through Resource::unload, or as a group through ResourceGroupManager::unloadResourceGroup.</li>
    ///	<li><b>Loaded</b>The Resource instance is fully loaded. This may have
    ///	happened implicitly because something used it, or it may have been 
    ///	loaded as part of a group.</li>
    ///	</ol>
    ///	<see>ResourceGroupManager.DeclareResource</see>
    ///	<see>ResourceGroupManager.InitialiseResourceGroup</see>
    ///	<see>ResourceGroupManager.LoadResourceGroup</see>
    ///	<see>ResourceGroupManager.UnloadResourceGroup</see>
    ///	<see>ResourceGroupManager.ClearResourceGroup</see>
    ///	</summary>
    /// <ogre name="ResourceGroupListener">
    ///     <file name="OgreResourceGroupManager.h"   revision="1.12.2.4" lastUpdated="5/14/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreResourceGroupManager.cpp" revision="1.16.2.10" lastUpdated="5/14/2006" lastUpdatedBy="Borrillis" />
    /// </ogre> 
    public class ResourceGroupManager : Singleton<ResourceGroupManager>
    {
        #region Delegates

        /// <summary>
        /// This event is fired when a resource group begins parsing scripts.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <param name="scriptCount">The number of scripts which will be parsed</param>
        private delegate void ResourceGroupScriptingStarted( string groupName, int scriptCount );
        private ResourceGroupScriptingStarted _resourceGroupScriptingStarted;

        /// <summary>
        /// This event is fired when a script is about to be parsed.
        /// </summary>
        /// <param name="scriptName">Name of the to be parsed</param>
        private delegate void ScriptParseStarted( string scriptName );
        private ScriptParseStarted _scriptParseStarted;

        /// <summary>
        /// This event is fired when the script has been fully parsed.
        /// </summary>
        private delegate void ScriptParseEnded();
        private ScriptParseEnded _scriptParseEnded;

        /// <summary>
        /// This event is fired when a resource group finished parsing scripts.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        private delegate void ResourceGroupScriptingEnded( string groupName );
        private ResourceGroupScriptingEnded _resourceGroupScriptingEnded;

        /// <summary>
        /// This event is fired  when a resource group begins loading.
        /// </summary>
        /// <param name="groupName">The name of the group being loaded</param>
        /// <param name="resourceCount">
        /// The number of resources which will be loaded, 
        /// including a number of stages required to load any linked world geometry
        /// </param>
        private delegate void ResourceGroupLoadStarted( string groupName, int resourceCount );
        private ResourceGroupLoadStarted _resourceGroupLoadStarted;

        /// <summary>
        /// This event is fired when a declared resource is about to be loaded. 
        /// </summary>
        /// <param name="resource">Weak reference to the resource loaded</param>
        private delegate void ResourceLoadStarted( Resource resource );
        private ResourceLoadStarted _resourceLoadStarted;

        /// <summary>
        /// This event is fired when the resource has been loaded. 
        /// </summary>
        private delegate void ResourceLoadEnded();
        private ResourceLoadEnded _resourceLoadEnded;

        /// <summary>
        /// This event is fired when a stage of loading linked world geometry 
        /// is about to start. The number of stages required will have been 
        /// included in the resourceCount passed in resourceGroupLoadStarted.
        /// </summary>
        /// <param name="description">Text description of what was just loaded</param>
        private delegate void WorldGeometryStageStarted( string description );
        private WorldGeometryStageStarted _worldGeometryStageStarted;
        
        /// <summary>
        /// This event is fired when a stage of loading linked world geometry 
        /// has been completed. The number of stages required will have been 
        /// included in the resourceCount passed in resourceGroupLoadStarted.
        /// </summary>
        /// <param name="description">Text description of what was just loaded</param>
        private delegate void WorldGeometryStageEnded();
        private WorldGeometryStageEnded _worldGeometryStageEnded;
        
        /// <summary>
        /// This event is fired when a resource group finished loading.
        /// </summary>
        private delegate void ResourceGroupLoadEnded( string groupName );
        private ResourceGroupLoadEnded _resourceGroupLoadEnded;

        #endregion

        #region Collection Declarations
        /// List of resource declarations
        //         typedef std::list<ResourceDeclaration> ResourceDeclarationList;
        public class ResourceDeclarationList : List<ResourceDeclaration>
        {
        };

        /// <summary>Map of resource types (strings) to ResourceManagers, used to notify them to load / unload group contents</summary>
        //          typedef std::map<String, ResourceManager*> ResourceManagerMap;
        public class ResourceManagerMap : Dictionary<String, ResourceManager>
        {
        };

        /// <summary>Map of loading order (Real) to ScriptLoader, used to order script parsing</summary>
        //          typedef std::multimap<Real, ScriptLoader*> ScriptLoaderOrderMap;
        public class ScriptLoaderOrderMap : AxiomCollection<float, IScriptLoader>
        {
        };

        /// <summary></summary>
        //          typedef std::vector<ResourceGroupListener*> ResourceGroupListenerList;
        public class ResourceGroupListenerList : List<IResourceGroupListener>
        {
        };

        /// <summary>Resource index entry, resourcename->location </summary>
        //          typedef std::map<String, Archive*> ResourceLocationIndex;
        public class ResourceLocationIndex : Dictionary<String, Archive>
        {
        };

        /// <summary>List of possible file locations</summary>
        //          typedef std::list<ResourceLocation*> LocationList;
        public class LocationList : List<ResourceLocation>
        {
        };

        /// <summary>List of resources which can be loaded / unloaded </summary>
        //          typedef std::list<ResourcePtr> LoadUnloadResourceList;
        public class LoadUnloadResourceList : List<Resource>
        {
        };

        /// <summary>Map from resource group names to groups </summary>
        //          typedef std::map<String, ResourceGroup*> ResourceGroupMap;
        public class ResourceGroupMap : Dictionary<String, ResourceGroup>
        {
        };

        //          typedef std::map<Real, LoadUnloadResourceList*> LoadResourceOrderMap;
        /// <summary>Map of loading order (float) to LoadUnLoadResourceList  used to order resource loading</summary>
        public class LoadResourceOrderMap : AxiomCollection<float, LoadUnloadResourceList>
        {
        };

        #endregion

        #region Nested Types

        /// Nested struct defining a resource declaration
        public struct ResourceDeclaration
        {
            public string ResourceName;
            public string ResourceType;
            public NameValuePairList Parameters;
        };

        /// <summary>Resource location entry</summary>
        public struct ResourceLocation : IDisposable
        {
            /// <summary>Pointer to the archive which is the destination</summary>
            public Archive Archive;
            /// Whether this location was added recursively
            public bool Recursive;

            #region IDisposable Members

            public void Dispose()
            {
                //Archive.Dispose();
                Archive = null;
                Recursive = false;
            }

            #endregion
        };

        /// Resource group entry
        public class ResourceGroup : IDisposable
        {

            //OGRE_AUTO_MUTEX
            /// <summary>Group name </summary>
            public string Name;

            /// <summary>Whether group has been initialised </summary>
            public bool Initialized;

            /// <summary>List of possible locations to search </summary>
            public LocationList LocationList;

            /// <summary>Index of resource names to locations, built for speedy access (case sensitive archives) </summary>
            public ResourceLocationIndex ResourceIndexCaseSensitive;

            /// <summary>Index of resource names to locations, built for speedy access (case insensitive archives) </summary>
            public ResourceLocationIndex ResourceIndexCaseInsensitive;

            /// <summary>Pre-declared resources, ready to be created </summary>
            public ResourceDeclarationList ResourceDeclarations;

            /// <summary>
            /// Created resources which are ready to be loaded / unloaded
            /// Group by loading order of the type (defined by ResourceManager)
            /// (e.g. skeletons and materials before meshes) 
            /// </summary>
            public LoadResourceOrderMap LoadResourceOrderMap;

            /// <summary>Linked world geometry, as passed to setWorldGeometry </summary>
            public string WorldGeometry;

            /// <summary>Scene manager to use with linked world geometry </summary>
            public SceneManager WorldGeometrySceneManager;

            #region IDisposable Members

            public void Dispose()
            {
                if ( Initialized )
                {
                    Initialized = false;
                    LocationList.Clear();
                    ResourceIndexCaseInsensitive.Clear();
                    ResourceIndexCaseSensitive.Clear();
                    ResourceDeclarations.Clear();
                    LoadResourceOrderMap.Clear();
                    WorldGeometrySceneManager = null;
                    WorldGeometry = "";
                    Name = "";
                }
            }

            #endregion
        };

        #endregion

        #region Fields and Properties
        //OGRE_AUTO_MUTEX // public to allow external locking

        #region DefaultResourceGroupName Property

        /// Default resource group name
        private static string _defaultResourceGroupName = "General";
        public static string DefaultResourceGroupName
        {
            get
            {
                return _defaultResourceGroupName;
            }
        }

        #endregion DefaultResourceGroupName Property

        #region BootstrapResourceGroupName Property

        /// Bootstrap resource group name (min OGRE resources)
        private static string _bootstrapResourceGroupName = "Bootstrap";
        public static string BootstrapResourceGroupName
        {
            get
            {
                return _bootstrapResourceGroupName;
            }
        }

        #endregion BootstrapResourceGroupName Property

        #region resourceManagerMap Property

        private ResourceManagerMap _resourceManagerMap;
        protected ResourceManagerMap resourceManagerMap
        {
            get
            {
                return _resourceManagerMap;
            }
        }

        #endregion resourceManagerMap Property

        #region scriptLoaderMap Property

        private ScriptLoaderOrderMap _scriptLoaderOrderMap;
        protected ScriptLoaderOrderMap scriptLoaderOrderMap
        {
            get
            {
                return _scriptLoaderOrderMap;
            }
        }

        #endregion scriptLoaderMap Property

        #region resourceGroupListenerList Property

        private ResourceGroupListenerList _resourceGroupListenerList;
        protected ResourceGroupListenerList resourceGroupListenerList
        {
            get
            {
                return _resourceGroupListenerList;
            }
        }

        #endregion resourceGroupListenerList Property

        #region resourceGroupMap Property

        private ResourceGroupMap _resourceGroupMap;
        protected ResourceGroupMap resourceGroupMap
        {
            get
            {
                return _resourceGroupMap;
            }
        }

        #endregion resourceGroupMap Property

        #region WorldResourceGroupName Property

        /// <summary>Group name for world resources</summary>
        private String _worldGroupName;
        /// <summary>
        /// Gets/Sets the resource group that 'world' resources will use.
        /// </summary>
        /// <remarks>
        ///    This is the group which should be used by SceneManagers implementing
        ///    world geometry when looking for their resources. Defaults to the 
        ///    DefaultResourceGroupName but this can be altered.
        /// </remarks>
        public String WorldResourceGroupName
        {
            get
            {
                return _worldGroupName;
            }
            set
            {
                _worldGroupName = value;
            }
        }

        #endregion WorldResourceGroupName Property

        #region currentGroup Property

        /// Stored current group - optimisation for when bulk loading a group
        private ResourceGroup _currentGroup;
        /// Stored current group - optimisation for when bulk loading a group
        protected ResourceGroup currentGroup
        {
            get
            {
                return _currentGroup;
            }
            set
            {
                _currentGroup = value;
            }
        }

        #endregion currentGroup Property

        #endregion Fields and Properties

        #region Constructors and Destructor

        /// <summary>
        ///     Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        private ResourceGroupManager()
        {

            // Create the 'General' group
            CreateResourceGroup( DefaultResourceGroupName );
            // default world group to the default group
            _worldGroupName = DefaultResourceGroupName;
            _currentGroup = null;

        }

        ~ResourceGroupManager()
        {
            // delete all resource groups
            foreach ( KeyValuePair<string,ResourceGroup> pair in resourceGroupMap )
            {
                ResourceGroup rg = pair.Value;
                _deleteGroup( rg );
            }
            resourceGroupMap.Clear();
            _currentGroup = null;
        }

        #endregion Constructors and Destructor

        #region Event Firing Methods

        /// <summary>Internal event firing method </summary>
        /// <param name="groupName"></param>
        /// <param name="scriptCount"></param>
        private void _fireResourceGroupScriptingStarted( string groupName, int scriptCount )
        {
            _resourceGroupScriptingStarted( groupName, scriptCount );
        }

        /// <summary>Internal event firing method </summary>
        /// <param name="scriptName"></param>
        private void _fireScriptStarted( string scriptName )
        {
            _scriptParseStarted( scriptName );
        }

        /// <summary>Internal event firing method</summary>
        private void _fireScriptEnded()
        {
            _scriptParseEnded();
        }

        /// <summary>Internal event firing method </summary>
        /// <param name="groupName"></param>
        private void _fireResourceGroupScriptingEnded( string groupName )
        {
            _resourceGroupScriptingEnded( groupName );
        }

        /// <summary>Internal event firing method </summary>
        /// <param name="groupName"></param>
        /// <param name="resourceCount"></param>
        private void _fireResourceGroupLoadStarted( string groupName, int resourceCount )
        {
            _resourceGroupLoadStarted( groupName, resourceCount );
        }

        /// <summary>Internal event firing method </summary>
        /// <param name="resource"></param>
        private void _fireResourceStarted( Resource resource )
        {
            _resourceLoadStarted( resource );
        }

        /// <summary>Internal event firing method </summary>
        private void _fireResourceEnded()
        {
            _resourceLoadEnded();
        }

        /// <summary>Internal event firing method </summary>
        /// <param name="groupName"></param>
        private void _fireResourceGroupLoadEnded( string groupName )
        {
            _resourceGroupLoadEnded( groupName );
        }

        #endregion Event Firing Methods

        #region Public Methods

        /// <summary>
        ///  Create a resource group.
        /// </summary>
        /// <remarks>
        ///    A resource group allows you to define a set of resources that can 
        ///    be loaded / unloaded as a unit. For example, it might be all the 
        ///    resources used for the level of a game. There is always one predefined
        ///    resource group called ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME, 
        ///	which is typically used to hold all resources which do not need to 
        ///	be unloaded until shutdown. You can create additional ones so that 
        ///	you can control the life of your resources in whichever way you wish.
        /// <para>
        ///    Once you have defined a resource group, resources which will be loaded
        ///	as part of it are defined in one of 3 ways:
        ///	<ol>
        ///	<li>Manually through declareResource(); this is useful for scripted
        ///		declarations since it is entirely generalised, and does not 
        ///		create Resource instances right away</li>
        ///	<li>Through the use of scripts; some ResourceManager subtypes have
        ///		script formats (e.g. .material, .overlay) which can be used
        ///		to declare resources</li>
        ///	<li>By calling ResourceManager::create to create a resource manually.
        ///	This resource will go on the list for it's group and will be loaded
        ///	and unloaded with that group</li>
        ///	</ol>
        ///	You must remember to call initialiseResourceGroup if you intend to use
        ///	the first 2 types.
        /// </para>
        /// </remarks>
        /// <param name="name">The name to give the resource group.</param>
        public void CreateResourceGroup( string name )
        {
            LogManager.Instance.Write( "Creating resource group " + name );
            if ( getResourceGroup( name ) == null )
            {
                throw new AxiomException( "Resource group with name '" + name + "' already exists!" );
            }
            ResourceGroup grp = new ResourceGroup();
            grp.Initialized = false;
            grp.Name = name;
            grp.WorldGeometrySceneManager = null;
            resourceGroupMap.Add( name, grp );
        }

        /// <summary>
        /// Initialises a resource group.
        /// </summary>
        /// <remarks>
        ///	After creating a resource group, adding some resource locations, and
        ///	perhaps pre-declaring some resources using declareResource(), but 
        ///	before you need to use the resources in the group, you 
        ///	should call this method to initialise the group. By calling this,
        ///	you are triggering the following processes:
        ///	<ol>
        ///	<li>Scripts for all resource types which support scripting are
        ///		parsed from the resource locations, and resources within them are
        ///		created (but not loaded yet).</li>
        ///	<li>Creates all the resources which have just pre-declared using
        ///	declareResource (again, these are not loaded yet)</li>
        ///	</ol>
        ///	So what this essentially does is create a bunch of unloaded Resource entries
        ///	in the respective ResourceManagers based on scripts, and resources
        ///	you've pre-declared. That means that code looking for these resources
        ///	will find them, but they won't be taking up much memory yet, until
        ///	they are either used, or they are loaded in bulk using loadResourceGroup.
        ///	Loading the resource group in bulk is entirely optional, but has the 
        ///	advantage of coming with progress reporting as resources are loaded.
        /// <para>
        ///	Failure to call this method means that loadResourceGroup will do 
        ///	nothing, and any resources you define in scripts will not be found.
        ///	Similarly, once you have called this method you won't be able to
        ///	pick up any new scripts or pre-declared resources, unless you
        ///	call clearResourceGroup, set up declared resources, and call this
        ///	method again.
        /// </para>
        /// <para>
        ///	When you call Root.Initialise, all resource groups that have already been
        ///	created are automatically initialised too. Therefore you do not need to 
        ///	call this method for groups you define and set up before you call 
        ///	Root.Initialise. However, since one of the most useful features of 
        ///	resource groups is to set them up after the main system initialisation
        ///	has occurred (e.g. a group per game level), you must remember to call this
        ///	method for the groups you create after this.
        /// </para>
        /// </remarks>
        /// <param name="name">The name of the resource group to initialise</param>
        public void InitialiseResourceGroup( string groupName )
        {
            LogManager.Instance.Write( "Initialising resource group {0}", groupName );
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::parseResourceGroupScripts", groupName );
            }

            if ( !grp.Initialized )
            {
                // Set current group
                _currentGroup = grp;
                _parseResourceGroupScripts( grp );
                _createDeclaredResources( grp );
                grp.Initialized = true;

                // Reset current group
                _currentGroup = null;
            }
        }

        /// <summary>
        /// Initialise all resource groups which are yet to be initialised.
        /// </summary>
        /// <see cref="ResourceGroupManager.initializeResourceGroup"/>
        public void InitialiseAllResourceGroups()
        {
            LogManager.Instance.Write( "Initialising all resource groups:" );
            // Intialise all declared resource groups
            foreach ( KeyValuePair<string, ResourceGroup> pair in resourceGroupMap )
            {
                ResourceGroup grp = pair.Value;
                if ( !grp.Initialized )
                {
                    // Set current group
                    _currentGroup = grp;
                    _parseResourceGroupScripts( grp );
                    _createDeclaredResources( grp );
                    grp.Initialized = true;

                    // Reset current group
                    _currentGroup = null;
                }
                LogManager.Instance.Write( "     {0} initialized.", grp.Name );
            }
        }

        #region LoadResourceGrouop Method

        /// <overloads>
        /// <summary>Loads a resource group.</summary>
        /// <remarks>            
        /// Loads any created resources which are part of the named group.
        /// Note that resources must have already been created by calling
        /// ResourceManager::create, or declared using declareResource() or
        /// in a script (such as .material and .overlay). The latter requires
        /// that initialiseResourceGroup has been called. 
        /// 
        /// When this method is called, this class will callback any ResourceGroupListeners
        /// which have been registered to update them on progress. 
        /// </remarks>
        /// <param name="name">The name to of the resource group to load.</param>
        /// </overloads>
        public void LoadResourceGroup( string name )
        {
            LoadResourceGroup( name, true, true );
        }

        /// <param name="loadMainResources">If true, loads normal resources associated 
        /// with the group (you might want to set this to false if you wanted
        /// to just load world geometry in bulk)</param>
        /// <param name="loadWorldGeom">If true, loads any linked world geometry <see>ResourceGroupManager.LinkWorldGeometryToResourceGroup</see></param>
        public void LoadResourceGroup( string name, bool loadMainResources, bool loadWorldGeom )
        {
            // Can only bulk-load one group at a time (reasonable limitation I think)
            //OGRE_LOCK_AUTO_MUTEX
            LogManager.Instance.Write( "Loading resource group '{0}' - Resources: {1} World Geometry: {2}", name, loadMainResources, loadWorldGeom );
            // load all created resources
            ResourceGroup grp = getResourceGroup( name );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::parseResourceGroupScripts", name );
            }

            // Set current group
            _currentGroup = grp;

            // Count up resources for starting event
            int resourceCount = 0;
            if ( loadMainResources )
            {
                foreach ( KeyValuePair<float,LoadUnloadResourceList> pair in grp.LoadResourceOrderMap )
                {
                    LoadUnloadResourceList lurl = pair.Value;
                    resourceCount += lurl.Count;
                }
            }
            // Estimate world geometry size
            if ( grp.WorldGeometrySceneManager != null && loadWorldGeom )
            {
                resourceCount += grp.WorldGeometrySceneManager.EstimateWorldGeometry( grp.WorldGeometry );
            }

            _fireResourceGroupLoadStarted( name, resourceCount );

            // Now load for real
            if ( loadMainResources )
            {
                foreach ( KeyValuePair<float, LoadUnloadResourceList> pair in grp.LoadResourceOrderMap )
                {
                    LoadUnloadResourceList lurl = pair.Value;
                    foreach ( Resource res in lurl )
                    {
                        // If loading one of these resources cascade-loads another resource, 
                        // the list will get longer! But these should be loaded immediately
                        if ( !res.IsLoaded )
                        {
                            _fireResourceStarted( res );
                            res.Load();
                            _fireResourceEnded();
                        }
                    }
                }
            }
            // Load World Geometry
            if ( grp.WorldGeometrySceneManager != null && loadWorldGeom )
            {
                grp.WorldGeometrySceneManager.SetWorldGeometry( grp.WorldGeometry );
            }
            _fireResourceGroupLoadEnded( name );

            // reset current group
            _currentGroup = null;

            LogManager.Instance.Write( "Finished loading resource group {0}.", name );
        }

        #endregion LoadResourceGroup Method

        /// <summary>Unloads a resource group.</summary>
        /// <remarks>
        /// This method unloads all the resources that have been declared as
        /// being part of the named resource group. Note that these resources
        /// will still exist in their respective ResourceManager classes, but
        /// will be in an unloaded state. If you want to remove them entirely,
        /// you should use ClearResourceGroup or DestroyResourceGroup.
        /// </remarks>
        /// <param name="name">The name to of the resource group to unload.</param>
        public void UnloadResourceGroup( string groupName )
        {

            LogManager.Instance.Write( "Unloading resource group {0}", groupName );
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::parseResourceGroupScripts", groupName );
            }
            // Set current group
            _currentGroup = grp;

            foreach ( KeyValuePair<float,LoadUnloadResourceList> pair in grp.LoadResourceOrderMap )
            {
                LoadUnloadResourceList lurl = pair.Value;
                foreach ( Resource res in lurl )
                {
                    res.Unload();
                }
            }

            // reset current group
            _currentGroup = null;
            LogManager.Instance.Write( "Finished unloading resource group {0}", groupName );
        }

        /// <summary>Clears a resource group.</summary>
        /// <remarks>            
        /// This method unloads all resources in the group, but in addition it
        /// removes all those resources from their ResourceManagers, and then 
        /// clears all the members from the list. That means after calling this
        /// method, there are no resources declared as part of the named group
        /// any more. Resource locations still persist though.
        /// </remarks>
        /// <param name="name">The name to of the resource group to clear.</param>
        public void ClearResourceGroup( string groupName )
        {
            LogManager.Instance.Write( "clearing resource group {0}", groupName );
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::ClearResourceGroup", groupName );
            }
            // set current group
            _currentGroup = grp;
            _dropGroupContents( grp );
            // clear initialised flag
            grp.Initialized = false;
            // reset current group
            _currentGroup = null;
            LogManager.Instance.Write( "Finished clearing resource group {0}", groupName );
        }

        /// <summary>
        /// Destroys a resource group, clearing it first, destroying the resources
        /// which are part of it, and then removing it from
        /// the list of resource groups.
        /// </summary>
        /// <param name="name">The name of the resource group to destroy.</param>
        public void DestroyResourceGroup( string groupName )
        {
            LogManager.Instance.Write( "Destroying resource group " + groupName );
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::DestroyResourceGroup", groupName );
            }
            // set current group
            _currentGroup = grp;
            UnloadResourceGroup( groupName ); // will throw an exception if name not valid
            _dropGroupContents( grp );
            _deleteGroup( grp );
            resourceGroupMap.Remove( groupName );
            // reset current group
            _currentGroup = null;
        }

        #region AddResourceLocation Method 

        /// <overloads>
        /// <summary>Method to add a resource location to for a given resource group.</summary>
        /// <remarks>
        /// Resource locations are places which are searched to load resource files.
        /// When you choose to load a file, or to search for valid files to load, 
        /// the resource locations are used.
        /// </remarks>
        /// <param name="name">The name of the resource location; probably a directory, zip file, URL etc.</param>
        /// <param name="locType">
        /// The codename for the resource type, which must correspond to the 
        /// Archive factory which is providing the implementation.
        /// </param>
        /// </overloads>
        public void AddResourceLocation( string name, string locType )
        {
            AddResourceLocation( name, locType, DefaultResourceGroupName, false );
        }

        /// <param name="resGroup">
        /// The name of the resource group for which this location is
        /// to apply. ResourceGroupManager.DefaultResourceGroupName is the 
        /// default group which always exists, and can
        /// be used for resources which are unlikely to be unloaded until application
        /// shutdown. Otherwise it must be the name of a group; if it
        /// has not already been created with createResourceGroup then it is created
        /// automatically.
        /// </param>
        public void AddResourceLocation( string name, string locType, string resGroup )
        {
            AddResourceLocation( name, locType, resGroup, false );
        }

        /// <param name="recursive"> 
        /// Whether subdirectories will be searched for files when using 
        /// a pattern match (such as *.material), and whether subdirectories will be
        /// indexed. This can slow down initial loading of the archive and searches.
        /// When opening a resource you still need to use the fully qualified name, 
        /// this allows duplicate names in alternate paths.
        /// </param>
        public void AddResourceLocation( string name, string locType, bool recursive )
        {
            AddResourceLocation( name, locType, DefaultResourceGroupName, recursive );
        }

        /// <param name="resGroup">
        /// The name of the resource group for which this location is
        /// to apply. ResourceGroupManager.DefaultResourceGroupName is the 
        /// default group which always exists, and can
        /// be used for resources which are unlikely to be unloaded until application
        /// shutdown. Otherwise it must be the name of a group; if it
        /// has not already been created with createResourceGroup then it is created
        /// automatically.
        /// </param>
        /// <param name="recursive"> 
        /// Whether subdirectories will be searched for files when using 
        /// a pattern match (such as *.material), and whether subdirectories will be
        /// indexed. This can slow down initial loading of the archive and searches.
        /// When opening a resource you still need to use the fully qualified name, 
        /// this allows duplicate names in alternate paths.
        /// </param>
        public void AddResourceLocation( string name, string locType, string resGroup, bool recursive )
        {
            ResourceGroup grp = getResourceGroup( resGroup );
            if ( grp != null )
            {
                CreateResourceGroup( resGroup );
                grp = getResourceGroup( resGroup );
            }


            // Get archive
            Archive arch = ArchiveManager.Instance.Load( name, locType );
            // Add to location list
            ResourceLocation loc = new ResourceLocation();
            loc.Archive = arch;
            loc.Recursive = recursive;
            grp.LocationList.Add( loc );
            // Index resources
            string[] vec = arch.Find( "*", recursive );
            foreach ( string it in vec )
            {
                // Index under full name, case sensitive
                grp.ResourceIndexCaseSensitive[ it ] = arch;
                if ( arch.IsCaseSensitive() )
                {
                    // Index under lower case name too for case insensitive match
                    grp.ResourceIndexCaseInsensitive[ it.ToLower() ] = arch;
                }
            }

            LogManager.Instance.Write( "Added resource location '{0}' of type '{1}' to resource group '{2}'{3}", name, locType, resGroup, recursive ? " with recursive option" : "" );
        }

        #endregion AddResourceLocation Method

        #region RemoveResourceLocation Method

        /// <overloads>
        /// <summary>
        /// Removes a resource location from the search path.
        /// </summary>
        /// <param name="locationName">the name of the ResourceLocation</param>
        /// </overloads>
        public void RemoveResourceLocation( string locationName )
        {
            RemoveResourceLocation( locationName, DefaultResourceGroupName );
        }

        /// <param name="groupName">the name of the ResourceGroup</param>
        public void RemoveResourceLocation( string locationName, string groupName )
        {
            LogManager.Instance.Write( "Remove Resource Location " + groupName );
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::RemoveResourceLocation", groupName );
            }

            // Remove from location list
            foreach ( ResourceLocation loc in grp.LocationList )
            {
                Archive arch = loc.Archive;
                if ( arch.Name == locationName )
                {
                    // Delete indexes
                    foreach ( string name in grp.ResourceIndexCaseInsensitive.Keys )
                    {
                        if ( grp.ResourceIndexCaseInsensitive[ name ] == arch )
                            grp.ResourceIndexCaseInsensitive.Remove( name );
                    }

                    foreach ( string name in grp.ResourceIndexCaseSensitive.Keys )
                    {
                        if ( grp.ResourceIndexCaseSensitive[ name ] == arch )
                            grp.ResourceIndexCaseSensitive.Remove( name );
                    }

                    loc.Dispose();

                    grp.LocationList.Remove( loc );
                    break;
                }

            }

            LogManager.Instance.Write( "Removed resource location " + locationName );

        }

        #endregion RemoveResourceLocation Method

        #region DeclareResource Method

        /// <overloads>
        /// <summary>
        /// Declares a resource to be a part of a resource group, allowing you to load and unload it as part of the group.
        /// </summary>
        /// <remarks>            
        /// By declaring resources before you attempt to use them, you can 
        /// more easily control the loading and unloading of those resources
        /// by their group. Declaring them also allows them to be enumerated, 
        /// which means events can be raised to indicate the loading progress
        /// <see>ResourceGroupListener</see>. Note that another way of declaring
        /// resources is to use a script specific to the resource type, if
        /// available (e.g. .material).
        /// <para>			
        /// Declared resources are not created as Resource instances (and thus
        /// are not available through their ResourceManager) until initialiseResourceGroup
        /// is called, at which point all declared resources will become created 
        /// (but unloaded) Resource instances, along with any resources declared
        /// in scripts in resource locations associated with the group.
        /// </para>
        /// </remarks>
        /// <param name="name">The resource name. </param>
        /// <param name="resourceType">
        /// The type of the resource. Axiom comes preconfigured with 
        /// a number of resource types: 
        /// <ul>
        /// <li>Font</li>
        /// <li>Material</li>
        /// <li>Mesh</li>
        /// <li>Overlay</li>
        /// <li>Skeleton</li>
        /// </ul>
        /// .. but more can be added by plugin ResourceManager classes.</param>
        /// </overloads>
        public void DeclareResource( string name, string resourceType )
        {
            DeclareResource( name, resourceType, DefaultResourceGroupName, new NameValuePairList() );
        }

        /// <param name="groupName">The name of the group to which it will belong.</param>
        public void DeclareResource( string name, string resourceType, string groupName )
        {
            DeclareResource( name, resourceType, groupName, new NameValuePairList() );
        }

        /// <param name="loadParameters">
        /// A list of name / value pairs which supply custom
        /// parameters to the resource which will be required before it can 
        /// be loaded. These are specific to the resource type.
        /// </param>
        public void DeclareResource( string name, string resourceType, NameValuePairList loadParameters )
        {
            DeclareResource( name, resourceType, DefaultResourceGroupName, loadParameters );
        }

        /// <param name="groupName">The name of the group to which it will belong.</param>
        /// <param name="loadParameters">
        /// A list of name / value pairs which supply custom
        /// parameters to the resource which will be required before it can 
        /// be loaded. These are specific to the resource type.
        /// </param>
        public void DeclareResource( string name, string resourceType, string groupName, NameValuePairList loadParameters )
        {
            ResourceGroup grp = getResourceGroup( name );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::DeclareResource", name );
            }

            ResourceDeclaration dcl;
            dcl.Parameters = loadParameters;
            dcl.ResourceName = name;
            dcl.ResourceType = resourceType;
            grp.ResourceDeclarations.Add( dcl );
        }

        #endregion DeclareResource Method

        /// <summary>Undeclare a resource.</summary>
        /// <remarks>
        /// Note that this will not cause it to be unloaded
        /// if it is already loaded, nor will it destroy a resource which has 
        /// already been created if InitialiseResourceGroup has been called already.
        /// Only UnloadResourceGroup / ClearResourceGroup / DestroyResourceGroup 
        /// will do that.
        /// </remarks>
        /// <param name="name">The name of the resource. </param>
        /// <param name="groupName">The name of the group this resource was declared in.</param>
        public void UndeclareResource( string name, string groupName )
        {
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::UndeclareResource", groupName );
            }

            foreach ( ResourceDeclaration resDec in grp.ResourceDeclarations )
            {
                if ( resDec.ResourceName == name )
                {
                    grp.ResourceDeclarations.Remove( resDec );
                    break;
                }
            }
        }

        #region OpenResource Method
        /** Open a single resource by name and return a DataStream
            pointing at the source of the data.
        @param resourceName The name of the resource to locate.
            Even if resource locations are added recursively, you
            must provide a fully qualified name to this method. You 
            can find out the matching fully qualified names by using the
            find() method if you need to.
        @param groupName The name of the resource group; this determines which 
            locations are searched. 
        @returns Shared pointer to data stream containing the data, will be
            destroyed automatically when no longer referenced
        */
        public Stream OpenResource( string resourceName )
        {
            return OpenResource( resourceName, DefaultResourceGroupName );
        }
        public Stream OpenResource( string resourceName, string groupName )
        {
            // Try to find in resource index first
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::OpenResource", groupName );
            }

            Archive arch = null;
            if ( grp.ResourceIndexCaseSensitive.ContainsKey( resourceName ) )
            {
                arch = grp.ResourceIndexCaseSensitive[ resourceName ];
                return arch.Open( resourceName );
            }
            else
            {
                string lc = resourceName.ToLower();
                if ( grp.ResourceIndexCaseInsensitive.ContainsKey( lc ) )
                {
                    arch = grp.ResourceIndexCaseInsensitive[ lc ];
                    return arch.Open( lc );
                }
                else
                {
                    foreach ( ResourceLocation rl in grp.LocationList )
                    {
                        arch = rl.Archive;
                        if ( arch.Exists( resourceName ) )
                        {
                            return arch.Open( resourceName );
                        }
                    }
                }
            }

            // Not found
            throw new AxiomException( "Cannot locate resource {0} in resource group {1}.", resourceName, groupName );

        }
        #endregion OpenResource Method

        #region OpenResources Method
        /// <overloads>
        /// <summary>
        /// Open all resources matching a given pattern (which can contain
        /// the character '*' as a wildcard), and return a collection of 
        /// DataStream objects on them.
        /// </summary>
        /// <param name="pattern">
        /// The pattern to look for. If resource locations have been
        /// added recursively, subdirectories will be searched too so this
        /// does not need to be fully qualified.
        /// </param>
        /// <returns>A list of Stream objects.</returns>
        /// </overloads>
        public List<Stream> OpenResources( string pattern )
        {
            return OpenResources( pattern, DefaultResourceGroupName );
        }

        /// <param name="groupName">
        /// The resource group; this determines which locations are searched.
        /// </param>
        public List<Stream> OpenResources( string pattern, string groupName )
        {
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::OpenResources", groupName );
            }

            // Iterate through all the archives and build up a combined list of
            // streams
            List<Stream> ret = new List<Stream>();

            foreach ( ResourceLocation li in grp.LocationList )
            {
                Archive arch = li.Archive;
                // Find all the names based on whether this archive is recursive
                string[] names = arch.Find( pattern, li.Recursive );

                // Iterate over the names and load a stream for each
                foreach ( string resource in names )
                {
                    Stream ptr = arch.Open( resource );
                    if ( ptr != null )
                    {
                        ret.Add( ptr );
                    }
                }
            }
            return ret;
        }

        #endregion OpenResources Method

        /// <summary>List all file names in a resource group.</summary>
        /// <remarks>
        /// This method only returns filenames, you can also retrieve other information using listFileInfo.
        /// </remarks>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A list of filenames matching the criteria, all are fully qualified</returns>
        public List<string> ListResourceNames( string groupName )
        {
            List<string> vec = new List<string>();

            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::ListResourceNames", groupName );
            }

            // Iterate over the archives
            foreach ( ResourceLocation rl in grp.LocationList )
            {
                string[] lst = rl.Archive.List( rl.Recursive );
                vec.AddRange( lst );
            }

            return vec;
        }

        /// <summary>List all files in a resource group with accompanying information.</summary>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A list of structures detailing quite a lot of information about all the files in the archive.</returns>
        public FileInfoList ListResourceFileInfo( string groupName )
        {
            FileInfoList vec = new FileInfoList();

            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::ListResourceFileInfo", groupName );
            }

            // Iterate over the archives
            foreach ( ResourceLocation rl in grp.LocationList )
            {
                FileInfoList lst = rl.Archive.ListFileInfo( rl.Recursive );
                vec.AddRange( lst );
            }

            return vec;

        }

        /// <summary>
        /// Find all file names matching a given pattern in a resource group.
        /// </summary>
        /// <remarks>
        /// This method only returns filenames, you can also retrieve other
        /// information using findFileInfo.
        /// </remarks>
        /// <param name="groupName">The name of the group</param>
        /// <param name="pattern">The pattern to search for; wildcards (*) are allowed</param>
        /// <returns>A list of filenames matching the criteria, all are fully qualified</returns>
        public List<string> FindResourceNames( string groupName, string pattern )
        {
            List<string> vec = new List<string>();

            // Try to find in resource index first
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::FindResourceNames", groupName );
            }


            // Iterate over the archives
            foreach ( ResourceLocation rl in grp.LocationList )
            {
                List<string> lst = rl.Archive.Find( pattern, rl.Recursive );
                vec.AddRange( lst );
            }

            return vec;
        }

        /// <summary>Find out if the named file exists in a group. /summary>
        /// <param name="group">The name of the resource group</param>
        /// <param name="filename">Fully qualified name of the file to test for</param>
        public bool ResourceExists( string group, string filename )
        {
            // Try to find in resource index first
            ResourceGroup grp = getResourceGroup( group );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::ResourceExists", group );
            }

            if ( grp.ResourceIndexCaseSensitive.ContainsKey( filename ) )
            {
                return true;
            }
            else
            {
                string lc = filename.ToLower();
                if ( grp.ResourceIndexCaseInsensitive.ContainsKey( lc ) )
                {
                    return true;
                }
                else
                {
                    foreach ( ResourceLocation rl in grp.LocationList )
                    {
                        Archive arch = rl.Archive;
                        if ( arch.Exists( filename ) )
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Find all files matching a given pattern in a group and get 
        /// some detailed information about them.
        /// </summary>
        /// <param name="groupName">The name of the resource group</param>
        /// <param name="pattern">The pattern to search for; wildcards (*) are allowed</param>
        /// <returns>A list of file information structures for all files matching the criteria.</returns>
        public FileInfoList FindResourceFileInfo( string groupName, string pattern )
        {
            FileInfoList vec = new FileInfoList();

            // Try to find in resource index first
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::FindResourceNames", groupName );
            }


            // Iterate over the archives
            foreach ( ResourceLocation rl in grp.LocationList )
            {
                FileInfoList lst = rl.Archive.FindFileInfo( pattern, rl.Recursive );
                vec.AddRange( lst );
            }

            return vec;        
        }

        /// <summary>
        /// Adds a ResourceGroupListener which will be called back during 
        /// resource loading events. 
        /// </summary>
        /// <param name="rgl"></param>
        public void AddResourceGroupListener( IResourceGroupListener rgl )
        {
            if ( rgl != null )
            {
                _resourceGroupListenerList.Add( rgl );
                this._resourceGroupScriptingStarted += new ResourceGroupScriptingStarted( rgl.ResourceGroupScriptingStarted );
                this._resourceGroupScriptingEnded += new ResourceGroupScriptingEnded( rgl.ResourceGroupScriptingEnded );
                this._resourceGroupLoadStarted += new ResourceGroupLoadStarted( rgl.ResourceGroupLoadStarted );
                this._resourceGroupLoadEnded += new ResourceGroupLoadEnded( rgl.ResourceGroupLoadEnded );
                this._resourceLoadStarted += new ResourceLoadStarted( rgl.ResourceLoadStarted );
                this._resourceLoadEnded += new ResourceLoadEnded( rgl.ResourceLoadEnded );
                this._scriptParseStarted += new ScriptParseStarted( rgl.ScriptParseStarted );
                this._scriptParseEnded += new ScriptParseEnded( rgl.ScriptParseEnded );
                this._worldGeometryStageStarted += new WorldGeometryStageStarted( rgl.WorldGeometryStageStarted );
                this._worldGeometryStageEnded += new WorldGeometryStageEnded( rgl.WorldGeometryStageEnded );

            }
        }

        /// <summary>
        /// Removes a ResourceGroupListener
        /// </summary>
        /// <param name="rgl"></param>
        public void RemoveResourceGroupListener( IResourceGroupListener rgl )
        {
            if ( rgl != null )
            {
                _resourceGroupListenerList.Remove( rgl );
                this._resourceGroupScriptingStarted -= new ResourceGroupScriptingStarted( rgl.ResourceGroupScriptingStarted );
                this._resourceGroupScriptingEnded -= new ResourceGroupScriptingEnded( rgl.ResourceGroupScriptingEnded );
                this._resourceGroupLoadStarted -= new ResourceGroupLoadStarted( rgl.ResourceGroupLoadStarted );
                this._resourceGroupLoadEnded -= new ResourceGroupLoadEnded( rgl.ResourceGroupLoadEnded );
                this._resourceLoadStarted -= new ResourceLoadStarted( rgl.ResourceLoadStarted );
                this._resourceLoadEnded -= new ResourceLoadEnded( rgl.ResourceLoadEnded );
                this._scriptParseStarted -= new ScriptParseStarted( rgl.ScriptParseStarted );
                this._scriptParseEnded -= new ScriptParseEnded( rgl.ScriptParseEnded );
                this._worldGeometryStageStarted -= new WorldGeometryStageStarted( rgl.WorldGeometryStageStarted );
                this._worldGeometryStageEnded -= new WorldGeometryStageEnded( rgl.WorldGeometryStageEnded );
            }
        }

        /// <summary>
        /// Associates some world geometry with a resource group, causing it to 
        /// be loaded / unloaded with the resource group.
        /// </summary>
        /// <remarks>
        /// You would use this method to essentially defer a call to 
        /// SceneManager::setWorldGeometry to the time when the resource group
        /// is loaded. The advantage of this is that compatible scene managers 
        /// will include the estimate of the number of loading stages for that
        /// world geometry when the resource group begins loading, allowing you
        /// to include that in a loading progress report.
        /// </remarks>
        /// <param name="groupName">The name of the resource group</param>
        /// <param name="worldGeometry">The parameter which should be passed to setWorldGeometry</param>
        /// <param name="sceneManager">The SceneManager which should be called</param>
        public void LinkWorldGeometryToResourceGroup( string groupName, string worldGeometry, SceneManager sceneManager )
        {
            // Try to find in resource index first
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::LinkWorldGeometryToResourceGroup", groupName );
            }

            grp.WorldGeometry = worldGeometry;
            grp.WorldGeometrySceneManager = sceneManager;
        }

        /// <summary>
        /// Clear any link to world geometry from a resource group.
        /// </summary>
        /// <remarks>Basically undoes a previous call to linkWorldGeometryToResourceGroup.</remarks>
        /// <param name="groupName">The name of the resource group</param>
        public void UnlinkWorldGeometryFromResourceGroup( string groupName )
        {
            // Try to find in resource index first
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::UnlinkWorldGeometryFromResourceGroup", groupName );
            }

            grp.WorldGeometry = "";
            grp.WorldGeometrySceneManager = null;
        }

        /// <summary>
        /// Shutdown all ResourceManagers, performed as part of clean-up. 
        /// </summary>
        public void ShutdownAll()
        {
            foreach ( KeyValuePair<string, ResourceManager> pair in _resourceManagerMap )
            {
                ResourceManager rm = pair.Value;
                rm.RemoveAll();
            }
        }


        /// <summary>Get a list of the currently defined resource groups.</summary>
        /// <remarks>
        /// This method intentionally returns a copy rather than a reference in
        /// order to avoid any contention issues in multithreaded applications.
        /// </remarks>
        /// <returns>A copy of list of currently defined groups.</returns>
        public List<string> GetResourceGroups()
        {
            List<string> vec = new List<string>();

            foreach ( KeyValuePair<string,ResourceGroup> pair in _resourceGroupMap )
            {
                ResourceGroup rg = pair.Value;
                vec.Add( rg.Name );
            }

            return vec;
        }

        /// <summary>Get the list of resource declarations for the specified group name.</summary>
        /// <remarks>
        /// This method intentionally returns a copy rather than a reference in
        /// order to avoid any contention issues in multithreaded applications.
        /// </remarks>
        /// /// <param name="groupName">The name of the group</param>
        /// <returns>A copy of list of currently defined resources.</returns>
        public ResourceDeclaration[] getResourceDeclarationList( string groupName )
        {
            // Try to find in resource index first
            ResourceGroup grp = getResourceGroup( groupName );
            if ( grp == null )
            {
                throw new AxiomException( "Cannot find a group named {0} : ResourceGroupManager::getResourceDeclarationList", groupName );
            }
            return grp.ResourceDeclarations.ToArray();
        }

#endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Get resource group
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected ResourceGroup getResourceGroup( string name )
        {
            if ( _resourceGroupMap.ContainsKey( name ) )
            {
                return _resourceGroupMap[ name ];
            }

            return null;

        }

        /// <summary>
        /// Internal method for registering a ResourceManager (which should be
        /// a singleton). Creators of plugins can register new ResourceManagers
        /// this way if they wish.
        /// </summary>
        /// <remarks>
        /// ResourceManagers that wish to parse scripts must also call registerScriptLoader.
        /// </remarks>
        /// <param name="resourceType">String identifying the resource type, must be unique.</param>
        /// <param name="rm">the ResourceManager instance.</param>
        internal void RegisterResourceManager( string resourceType, ResourceManager rm )
        {
            LogManager.Instance.Write( "Registering ResourceManager for type {0}", resourceType );
            _resourceManagerMap[ resourceType ] = rm;
        }

        /// <summary>
        /// Internal method for unregistering a ResourceManager.
        /// </summary>
        /// <remarks>
        /// ResourceManagers that wish to parse scripts must also call unregisterScriptLoader.
        /// </remarks>
        /// <param name="resourceType">String identifying the resource type.</param>
        internal void UnregisterResourceManager( string resourceType )
        {
            LogManager.Instance.Write( "Unregistering ResourceManager for type {0}", resourceType );
            if ( _resourceManagerMap.ContainsKey( resourceType ) )
            {
                _resourceManagerMap.Remove( resourceType );
            }
        }

        /// <summary>
        /// Internal method for registering a ScriptLoader. ScriptLoaders parse scripts when resource groups are initialised.
        /// </summary>
        /// <param name="su">ScriptLoader instance.</param>
        internal void RegisterScriptLoader( IScriptLoader su )
        {
            LogManager.Instance.Write( "Registering ScriptLoader for patterns {0}", su.ScriptPatterns );
            _scriptLoaderOrderMap.Add( su.LoadingOrder, su );
        }

        /// <summary>
        /// Internal method for unregistering a ScriptLoader.
        /// </summary>
        /// <param name="su">ScriptLoader instance.</param>
        internal void UnregisterScriptLoader( IScriptLoader su )
        {
            LogManager.Instance.Write( "Registering ScriptLoader for patterns {0}", su.ScriptPatterns );
            if ( _scriptLoaderOrderMap.ContainsValue( su ) )
            {
                _scriptLoaderOrderMap.Remove( su );
            }
        }

        /// <summary>
        /// Internal method for getting a registered ResourceManager.
        /// </summary>
        /// <param name="resourceType">String identifying the resource type.</param>
        internal ResourceManager GetResourceManager( string resourceType )
        {
            if ( _resourceManagerMap.ContainsKey( resourceType ) == true )
            {
                return _resourceManagerMap[ resourceType ];
            }
            throw new AxiomException( "Cannot locate resource manager for resource type '{0}' ResourceGroupManager::_getResourceManager", resourceType );
        }

        /// <summary>Internal method called by ResourceManager when a resource is created.</summary>
        /// <param name="res">reference to resource</param>
        internal void notifyResourceCreated( Resource res )
        {
            if ( _currentGroup != null )
            {
                // Use current group (batch loading)
                _addCreatedResource( res, _currentGroup );
            }
            else
            {
                // Find group
                ResourceGroup grp = getResourceGroup( res.Group );
                if ( grp != null )
                {
                    _addCreatedResource( res, grp );
                }
            }
        }

        /// <summary>Internal method called by ResourceManager when a resource is removed.</summary>
        /// <param name="res">reference to resource</param>
        internal void notifyResourceRemoved( Resource res )
        {
            if ( _currentGroup != null )
            {
                // Do nothing - we're batch unloading so list will be cleared
            }
            else
            {
                // Find group
                ResourceGroup grp = getResourceGroup( res.Group );
                if ( grp != null )
                {
                    if ( grp.LoadResourceOrderMap.ContainsKey( res.Parent.LoadingOrder ) )
                    {

                        // Iterate over the resource list and remove
                        LoadUnloadResourceList resList = grp.LoadResourceOrderMap[ res.Parent.LoadingOrder ];
                        foreach ( Resource r in resList )
                        {
                            if ( IntPtr.ReferenceEquals( r, res ) )
                            {
                                // this is the one
                                resList.Remove( r );
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Internal method called by ResourceManager when all resources for that manager are removed.</summary>
        /// <param name="manager">the manager for which all resources are being removed</param>
        internal void notifyAllResourcesRemoved( ResourceManager manager )
        {
            // Iterate over all groups
            foreach ( KeyValuePair<string, ResourceGroup> rgPair in _resourceGroupMap )
            {
                ResourceGroup rg = rgPair.Value;
                // Iterate over all priorities
                foreach ( KeyValuePair<float, LoadUnloadResourceList> rlPair in rg.LoadResourceOrderMap )
                {
                    LoadUnloadResourceList rl = rlPair.Value;
                    // Iterate over all resources
                    foreach ( Resource res in rl )
                    {
                        if ( res.Parent == manager )
                        {
                            // Increment first since iterator will be invalidated
                            //LoadUnloadResourceList::iterator del = l++;
                            rl.Remove( res );
                        }
                    }
                }

            }
        }

        /// <summary>Notify this manager that one stage of world geometry loading has been started.</summary>
        /// <remarks>            
        /// Custom SceneManagers which load custom world geometry should call this 
        /// method the number of times equal to the value they return from 
        /// SceneManager.estimateWorldGeometry while loading their geometry.
        /// </remarks>
        /// <param name="description"></param>
        internal void notifyWorldGeometryStageStarted( string description )
        {
            _worldGeometryStageStarted( description );
        }

        /// <summary>Notify this manager that one stage of world geometry loading has been completed.</summary>
        /// <remarks>            
        /// Custom SceneManagers which load custom world geometry should call this 
        /// method the number of times equal to the value they return from 
        /// SceneManager.estimateWorldGeometry while loading their geometry.
        /// </remarks>
        internal void notifyWorldGeometryStageEnded()
        {
            _worldGeometryStageEnded();
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Parses all the available scripts found in the resource locations
        /// for the given group, for all ResourceManagers. 
        /// </summary>
        /// <remarks>
        ///	Called as part of initializeResourceGroup
        /// </remarks>
        private void _parseResourceGroupScripts( ResourceGroup grp )
        {
            LogManager.Instance.Write( "Parsing scripts for resource group " + grp.Name );

            // Count up the number of scripts we have to parse
            List<Tuple<IScriptLoader, List<FileInfoList>>> scriptLoaderFileList = new List<Tuple<IScriptLoader, List<FileInfoList>>>();

            int scriptCount = 0;
            // Iterate over script users in loading order and get streams
            foreach ( KeyValuePair<float, IScriptLoader> pairsl in _scriptLoaderOrderMap )
            {
                IScriptLoader sl = pairsl.Value;
                List<FileInfoList> fileListList = new List<FileInfoList>();

                // Get all the patterns and search them
                List<string> patterns = sl.ScriptPatterns;
                foreach ( string p in patterns )
                {
                    FileInfoList fileList = FindResourceFileInfo( grp.Name, p );
                    scriptCount += fileList.Count;
                    fileListList.Add( fileList );
                }
                scriptLoaderFileList.Add( new Tuple<IScriptLoader, List<FileInfoList>>( sl, fileListList ) );
            }
            // Fire scripting event
            _fireResourceGroupScriptingStarted( grp.Name, scriptCount );

            // Iterate over scripts and parse
            // Note we respect original ordering
            foreach ( Tuple<IScriptLoader, List<FileInfoList>> slfli in scriptLoaderFileList )
            {
                IScriptLoader su = slfli.first;
                // Iterate over each list
                foreach ( FileInfoList flli in slfli.second )
                {
                    // Iterate over each item in the list
                    foreach ( FileInfo fii in flli )
                    {
                        LogManager.Instance.Write( "Parsing script " + fii.Filename );
                        _fireScriptStarted( fii.Filename );
                        {
                            Stream stream = fii.Archive.Open( fii.Filename );
                            if ( stream != null )
                            {
                                su.ParseScript( stream, grp.Name );
                            }
                        }
                        _fireScriptEnded();
                    }
                }
            }

            _fireResourceGroupScriptingEnded( grp.Name );
            LogManager.Instance.Write( "Finished parsing scripts for resource group " + grp.Name );
        }

        /// <summary>Create all the pre-declared resources.</summary>
        /// <remarks>Called as part of initialiseResourceGroup</remarks>
        private void _createDeclaredResources( ResourceGroup grp )
        {
            foreach ( ResourceDeclaration dcl in grp.ResourceDeclarations )
            {
                // Retrieve the appropriate manager
                ResourceManager mgr = _getResourceManager( dcl.resourceType );
                // Create the resource
                Resource res = mgr.Create( dcl.resourceName, grp.Name );
                // Set custom parameters
                res.SetParameterList( dcl.Parameters );
                // Add resource to load list
                LoadUnloadResourceList loadList;
                if ( grp.LoadResourceOrderMap.ContainsKey( mgr.LoadingOrder ) == true )
                {
                    loadList = new LoadUnloadResourceList();
                    grp.LoadResourceOrderMap.Add( mgr.LoadingOrder, loadList );
                }
                else
                {
                    loadList = grp.LoadResourceOrderMap[ mgr.LoadingOrder ];
                }
                loadList.Add( res );

            }
        }

        /** Adds a created resource to a group. */
        private void _addCreatedResource( Resource res, ResourceGroup group )
        {
            Real order = res.Creator.LoadingOrder;

            LoadUnloadResourceList loadList;
            if ( group.LoadResourceOrderMap.ContainsKey( order ) == true )
            {
                loadList = new LoadUnloadResourceList();
                group.LoadResourceOrderMap.Add( order, loadList );
            }
            else
            {
                loadList = group.LoadResourceOrderMap[ order ];
            }

            loadList.Add( res );
        }

        /// <summary>
        ///  Drops contents of a group, leave group there, notify ResourceManagers.
        /// </summary>
        /// <param name="grp"></param>
        private void _dropGroupContents( ResourceGroup grp )
        {

            bool groupSet = false;
            if ( _currentGroup != null )
            {
                // Set current group to indicate ignoring of notifications
                _currentGroup = grp;
                groupSet = true;
            }
            // delete all the load list entries
            foreach ( KeyValuePair<float, LoadUnloadResourceList> pair in grp.LoadResourceOrderMap )
            {
                LoadUnloadResourceList rl = pair.Value;
                foreach ( Resource res in rl )
                {
                    res.Parent.Remove( res );
                }
            }
            grp.LoadResourceOrderMap.Clear();

            if ( groupSet )
            {
                _currentGroup = null;
            }
        }

        /// <summary>
        /// Delete a group for shutdown - don't notify ResourceManagers. 
        /// </summary>
        /// <param name="grp"></param>
        private void _deleteGroup( ResourceGroup grp )
        {
            // delete all the load list entries
            foreach ( KeyValuePair<float, LoadUnloadResourceList> pair in grp.LoadResourceOrderMap )
            {
                // Don't iterate over resources to drop with ResourceManager
                // Assume this is being done anyway since this is a shutdown method
                LoadUnloadResourceList lurl = pair.Value;
                lurl.Clear();
            }

            // Drop location list
            foreach ( ResourceLocation loc in grp.LocationList )
            {
                loc.Dispose();
            }

            // delete ResourceGroup
            grp.Dispose();
        }

        #endregion Private Methods

    };
}
