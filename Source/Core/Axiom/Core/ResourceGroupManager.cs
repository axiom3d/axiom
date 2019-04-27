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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Axiom.Collections;
using Axiom.FileSystem;
using Axiom.Math;
using Axiom.Scripting;
using IO = System.IO;

#endregion Namespace Declarations

namespace Axiom.Core
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
    /// 	<li>resourceGroupPrepareStarted</li>
    ///	    <li>resourcePrepareStarted (*)</li>
    ///     <li>resourcePrepareEnded (*)</li>
    ///	    <li>resourceGroupPrepareEnded</li>
    /// </ul>
    /// </remarks>
    public interface IResourceGroupListener
    {
        /// <summary>
        /// This event is fired when a resource group begins parsing scripts.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <param name="scriptCount">The number of scripts which will be parsed</param>
        void ResourceGroupScriptingStarted(string groupName, int scriptCount);

        /// <summary>
        /// This event is fired when a script is about to be parsed.
        /// </summary>
        /// <param name="scriptName">Name of the to be parsed</param>
        /// <param name="skipThisScript">A boolean passed by reference which is by default set to 
        ///	false. If the event sets this to true, the script will be skipped and not
        ///	parsed. Note that in this case the scriptParseEnded event will not be raised
        ///	for this script.</param>
        void ScriptParseStarted(string scriptName, ref bool skipThisScript);

        /// <summary>
        /// This event is fired when the script has been fully parsed.
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="skipped"></param>
        void ScriptParseEnded(string scriptName, bool skipped);

        /// <summary>
        /// This event is fired when a resource group finished parsing scripts.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        void ResourceGroupScriptingEnded(string groupName);

        /// <summary>
        /// This event is fired  when a resource group begins preparing.
        /// </summary>
        /// <param name="groupName">The name of the group being prepared</param>
        /// <param name="resourceCount">The number of resources which will be prepared, including
        /// a number of stages required to prepare any linked world geometry.
        /// </param>
        void ResourceGroupPrepareStarted(string groupName, int resourceCount);

        /// <summary>
        /// This event is fired when a declared resource is about to be prepared. 
        /// </summary>
        /// <param name="resource">Weak reference to the resource prepared.</param>
        void ResourcePrepareStarted(Resource resource);

        /// <summary>
        /// This event is fired when the resource has been prepared. 
        /// </summary>
        void ResourcePrepareEnded();

        /// <summary>
        /// This event is fired when a resource group finished preparing.
        /// </summary>
        /// <param name="groupName">The name of the group has been prepared.</param>
        void ResourceGroupPrepareEnded(string groupName);

        /// <summary>
        /// This event is fired  when a resource group begins loading.
        /// </summary>
        /// <param name="groupName">The name of the group being loaded</param>
        /// <param name="resourceCount">
        /// The number of resources which will be loaded,
        /// including a number of stages required to load any linked world geometry
        /// </param>
        void ResourceGroupLoadStarted(string groupName, int resourceCount);

        /// <summary>
        /// This event is fired when a declared resource is about to be loaded.
        /// </summary>
        /// <param name="resource">Weak reference to the resource loaded</param>
        void ResourceLoadStarted(Resource resource);

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
        void WorldGeometryStageStarted(string description);

        /// <summary>
        /// This event is fired when a stage of loading linked world geometry
        /// has been completed. The number of stages required will have been
        /// included in the resourceCount passed in resourceGroupLoadStarted.
        /// </summary>
        void WorldGeometryStageEnded();

        /// <summary>
        /// This event is fired when a resource group finished loading.
        /// </summary>
        /// <param name="groupName">The name of the group has been loaded.</param>
        void ResourceGroupLoadEnded(string groupName);
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
    ///	ResourceGroupManager.InitializeResourceGroup is called (which is automatic
    ///	if you declare the resource group before Root.Initialize).</li>
    ///	<li><b>Unloaded</b>. There is now a Resource instance for this resource,
    ///	although it is not loaded. This means that code which looks for this
    ///	named resource will find it, but the Resource is not using a lot of memory
    ///	because it is in an unloaded state. A Resource can get into this state
    ///	by having just been created by ResourceGroupManager.InitializeResourceGroup
    ///	(either from a script, or from a call to declareResource), by
    ///	being created directly from code (ResourceManager.Create), or it may
    ///	have previously been loaded and has been unloaded, either individually
    ///	through Resource::unload, or as a group through ResourceGroupManager.UnloadResourceGroup.</li>
    ///	<li><b>Loaded</b>The Resource instance is fully loaded. This may have
    ///	happened implicitly because something used it, or it may have been
    ///	loaded as part of a group.</li>
    ///	</ol>
    ///	<see>ResourceGroupManager.DeclareResource</see>
    ///	<see>ResourceGroupManager.InitializeResourceGroup</see>
    ///	<see>ResourceGroupManager.LoadResourceGroup</see>
    ///	<see>ResourceGroupManager.UnloadResourceGroup</see>
    ///	<see>ResourceGroupManager.ClearResourceGroup</see>
    ///	</summary>
    public class ResourceGroupManager : DisposableObject, ISingleton<ResourceGroupManager>
    {
        #region Delegates

        /// <summary>
        /// This event is fired when a resource group begins parsing scripts.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <param name="scriptCount">The number of scripts which will be parsed</param>
        private delegate void ResourceGroupScriptingStarted(string groupName, int scriptCount);

        /// <summary>
        /// 
        /// </summary>
        private ResourceGroupScriptingStarted _resourceGroupScriptingStarted;

        /// <summary>
        /// This event is fired when a script is about to be parsed.
        /// </summary>
        /// <param name="scriptName">Name of the to be parsed</param>
        /// <param name="skipThisScript">A boolean passed by reference which is by default set to 
        ///	false. If the event sets this to true, the script will be skipped and not
        ///	parsed. Note that in this case the scriptParseEnded event will not be raised
        ///	for this script.</param>
        private delegate void ScriptParseStarted(string scriptName, ref bool skipThisScript);

        private ScriptParseStarted _scriptParseStarted;

        /// <summary>
        /// This event is fired when the script has been fully parsed.
        /// </summary>
        private delegate void ScriptParseEnded(string scriptName, bool skipped);

        private ScriptParseEnded _scriptParseEnded;

        /// <summary>
        /// This event is fired when a resource group finished parsing scripts.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        private delegate void ResourceGroupScriptingEnded(string groupName);

        private ResourceGroupScriptingEnded _resourceGroupScriptingEnded;

        /// <summary>
        /// This event is fired  when a resource group begins loading.
        /// </summary>
        /// <param name="groupName">The name of the group being loaded</param>
        /// <param name="resourceCount">
        /// The number of resources which will be loaded,
        /// including a number of stages required to load any linked world geometry
        /// </param>
        private delegate void ResourceGroupLoadStarted(string groupName, int resourceCount);

        private ResourceGroupLoadStarted _resourceGroupLoadStarted;

        /// <summary>
        /// This event is fired when a declared resource is about to be loaded.
        /// </summary>
        /// <param name="resource">Weak reference to the resource loaded</param>
        private delegate void ResourceLoadStarted(Resource resource);

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
        private delegate void WorldGeometryStageStarted(string description);

        private WorldGeometryStageStarted _worldGeometryStageStarted;

        /// <summary>
        /// This event is fired when a stage of loading linked world geometry
        /// has been completed. The number of stages required will have been
        /// included in the resourceCount passed in resourceGroupLoadStarted.
        /// </summary>
        private delegate void WorldGeometryStageEnded();

        private WorldGeometryStageEnded _worldGeometryStageEnded;

        /// <summary>
        /// This event is fired when a resource group finished loading.
        /// </summary>
        private delegate void ResourceGroupLoadEnded(string groupName);

        private ResourceGroupLoadEnded _resourceGroupLoadEnded;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="resourceCount"></param>
        private delegate void ResourceGroupPrepareStarted(string groupName, int resourceCount);

        // private ResourceGroupPrepareStarted _resourceGroupPrepareStarted;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resource"></param>
        private delegate void ResourcePrepareStarted(Resource resource);

        // private ResourcePrepareStarted _resourcePrepareStarted;

        /// <summary>
        /// 
        /// </summary>
        private delegate void ResourcePrepareEnded();

        //private ResourcePrepareEnded _resourcePrepareEnded;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupName"></param>
        private delegate void ResourceGroupPrepareEnded(string groupName);

        // private ResourceGroupPrepareEnded _resourceGroupPrepareEnded;

        #endregion Delegates

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
        public class ScriptLoaderOrderMap : AxiomSortedCollection<float, List<IScriptLoader>>
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
            public ResourceGroupMap()
                : base(new CaseInsensitiveStringComparer())
            {
            }
        };

        //          typedef std::map<Real, LoadUnloadResourceList*> LoadResourceOrderMap;
        /// <summary>Map of loading order (float) to LoadUnLoadResourceList  used to order resource loading</summary>
        public class LoadResourceOrderMap : Dictionary<float, LoadUnloadResourceList>
        {
        };

        #endregion Collection Declarations

        #region Nested Types

        /// Nested struct defining a resource declaration
        public struct ResourceDeclaration
        {
            public string ResourceName;
            public string ResourceType;
            public IManualResourceLoader Loader;
            public NameValuePairList Parameters;
        };

        /// <summary>Resource location entry</summary>
        public struct ResourceLocation
        {
            /// <summary>Pointer to the archive which is the destination</summary>
            public Archive Archive;

            /// <summary>Pointer to the watcher which is monitoring the archive location</summary>
            public Watcher Watcher;

            /// <summary>Whether this location and it's children are searched for files</summary>
            public bool Recursive;

            /// <summary>Whether this location is be monitored for new files</summary>
            public bool Monitor;
        };

        /// Resource group entry
        public class ResourceGroup : DisposableObject
        {
            //OGRE_AUTO_MUTEX
            /// <summary>Group name </summary>
            public string Name;

            /// <summary>Whether group has been initialised </summary>
            public bool Initialized;

            /// <summary>List of possible locations to search </summary>
            public LocationList LocationList = new LocationList();

            /// <summary>Index of resource names to locations, built for speedy access (case sensitive archives) </summary>
            public ResourceLocationIndex ResourceIndexCaseSensitive = new ResourceLocationIndex();

            /// <summary>Index of resource names to locations, built for speedy access (case insensitive archives) </summary>
            public ResourceLocationIndex ResourceIndexCaseInsensitive = new ResourceLocationIndex();

            /// <summary>Pre-declared resources, ready to be created </summary>
            public ResourceDeclarationList ResourceDeclarations = new ResourceDeclarationList();

            /// <summary>
            /// Created resources which are ready to be loaded / unloaded
            /// Group by loading order of the type (defined by ResourceManager)
            /// (e.g. skeletons and materials before meshes)
            /// </summary>
            public LoadResourceOrderMap LoadResourceOrders = new LoadResourceOrderMap();

            /// <summary>Linked world geometry, as passed to setWorldGeometry </summary>
            public string WorldGeometry;

            /// <summary>Scene manager to use with linked world geometry </summary>
            public SceneManager WorldGeometrySceneManager;

            public void Add(string filename, Archive arch)
            {
                // internal, assumes mutex lock has already been obtained
                this.ResourceIndexCaseSensitive[filename] = arch;

                if (!arch.IsCaseSensitive)
                {
                    this.ResourceIndexCaseInsensitive[filename.ToLower()] = arch;
                }
            }

            public void Remove(string filename, Archive arch)
            {
                // internal, assumes mutex lock has already been obtained
                if (this.ResourceIndexCaseSensitive.ContainsKey(filename))
                {
                    this.ResourceIndexCaseSensitive.Remove(filename);
                }

                if (!arch.IsCaseSensitive)
                {
                    var lcase = filename.ToLower();
                    if (this.ResourceIndexCaseInsensitive.ContainsKey(filename))
                    {
                        this.ResourceIndexCaseInsensitive.Remove(filename);
                    }
                }
            }

            public void Remove(Archive arch)
            {
                var keys = new List<string>();
                // Delete indexes
                foreach (var kvp in this.ResourceIndexCaseSensitive)
                {
                    if (kvp.Value == arch)
                    {
                        keys.Add(kvp.Key);
                    }
                }
                foreach (var key in keys)
                {
                    this.ResourceIndexCaseSensitive.Remove(key);
                }

                keys.Clear();
                foreach (var kvp in this.ResourceIndexCaseInsensitive)
                {
                    if (kvp.Value == arch)
                    {
                        keys.Add(kvp.Key);
                    }
                }
                foreach (var key in keys)
                {
                    this.ResourceIndexCaseInsensitive.Remove(key);
                }
            }

            #region IDisposable Members

            protected override void dispose(bool disposeManagedResources)
            {
                if (!IsDisposed)
                {
                    if (disposeManagedResources)
                    {
                        this.Initialized = false;
                        this.LocationList.Clear();
                        this.ResourceIndexCaseInsensitive.Clear();
                        this.ResourceIndexCaseSensitive.Clear();
                        this.ResourceDeclarations.Clear();
                        this.LoadResourceOrders.Clear();
                        this.WorldGeometrySceneManager = null;
                        this.WorldGeometry = "";
                        this.Name = "";
                    }
                }

                base.dispose(disposeManagedResources);
            }

            #endregion IDisposable Members
        };

        #endregion Nested Types

        #region Constants

        /// <summary>
        /// Default resource group name
        /// </summary>
        public const string DefaultResourceGroupName = "General";

        /// <summary>
        /// Internal resource group name (should be used by Axiom internal only)
        /// </summary>
        public const string InternalResourceGroupName = "Internal";

        /// <summary>
        /// Bootstrap resource group name (min Axiom resources)
        /// </summary>
        public const string BootstrapResourceGroupName = "Bootstrap";

        /// <summary>
        ///  Special resource group name which causes resource group to be automatically determined based on searching for the resource in all groups.
        /// </summary>
        public const string AutoDetectResourceGroupName = "AutoDetect";

        /// <summary>
        /// The number of reference counts held per resource by the resource system
        /// </summary>
        public const int ResourceSystemNumReferenceCount = 3;

        #endregion Constants

        #region Fields and Properties

        #region ResourceManagers Property

        private readonly ResourceManagerMap _resourceManagers = new ResourceManagerMap();

        public ResourceManagerMap ResourceManagers
        {
            get
            {
                return this._resourceManagers;
            }
        }

        #endregion ResourceManagers Property

        #region scriptLoaders Property

        private readonly ScriptLoaderOrderMap _scriptLoaderOrders = new ScriptLoaderOrderMap();

        protected ScriptLoaderOrderMap scriptLoaderOrders
        {
            get
            {
                return this._scriptLoaderOrders;
            }
        }

        #endregion scriptLoaders Property

        #region resourceGroupListeners Property

        private readonly ResourceGroupListenerList _resourceGroupListeners = new ResourceGroupListenerList();

        protected ResourceGroupListenerList resourceGroupListeners
        {
            get
            {
                return this._resourceGroupListeners;
            }
        }

        #endregion resourceGroupListeners Property

        #region resourceGroups Property

        private readonly ResourceGroupMap _resourceGroups = new ResourceGroupMap();

        protected ResourceGroupMap resourceGroups
        {
            get
            {
                return this._resourceGroups;
            }
        }

        #endregion resourceGroups Property

        #region WorldResourceGroupName Property

        /// <summary>
        /// Gets/Sets the resource group that 'world' resources will use.
        /// </summary>
        /// <remarks>
        ///    This is the group which should be used by SceneManagers implementing
        ///    world geometry when looking for their resources. Defaults to the
        ///    DefaultResourceGroupName but this can be altered.
        /// </remarks>
        public String WorldResourceGroupName { get; set; }

        #endregion WorldResourceGroupName Property

        #region currentGroup Property

        /// Stored current group - optimization for when bulk loading a group
        private ResourceGroup _currentGroup = null;

        /// <summary>
        /// Stored current group - optimization for when bulk loading a group
        /// </summary>
        protected ResourceGroup currentGroup
        {
            get
            {
                return this._currentGroup;
            }
            set
            {
                this._currentGroup = value;
            }
        }

        #endregion currentGroup Property

        #endregion Fields and Properties

        #region Constructors and Destructor

        /// <summary>
        ///     Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        public ResourceGroupManager()
        {
            if (instance == null)
            {
                instance = this;
            }

            // default world group to the default group
            WorldResourceGroupName = DefaultResourceGroupName;
        }

        ~ResourceGroupManager()
        {
            dispose(false);
        }

        #endregion Constructors and Destructor

        #region Event Firing Methods

        /// <summary>Internal event firing method </summary>
        /// <param name="groupName"></param>
        /// <param name="scriptCount"></param>
        private void _fireResourceGroupScriptingStarted(string groupName, int scriptCount)
        {
            if (this._resourceGroupScriptingStarted != null)
            {
                this._resourceGroupScriptingStarted(groupName, scriptCount);
            }
        }

        /// <summary>
        /// Internal event firing method 
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="skipThisScript"></param>
        private void _fireScriptStarted(string scriptName, ref bool skipThisScript)
        {
            if (this._scriptParseStarted != null)
            {
                this._scriptParseStarted(scriptName, ref skipThisScript);
            }
        }

        /// <summary>
        /// Internal event firing method 
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="skipped"></param>
        private void _fireScriptEnded(string scriptName, bool skipped)
        {
            if (this._scriptParseEnded != null)
            {
                this._scriptParseEnded(scriptName, skipped);
            }
        }

        /// <summary>
        /// Internal event firing method 
        /// </summary>
        /// <param name="groupName"></param>
        private void _fireResourceGroupScriptingEnded(string groupName)
        {
            if (this._resourceGroupScriptingEnded != null)
            {
                this._resourceGroupScriptingEnded(groupName);
            }
        }

        /// <summary>Internal event firing method </summary>
        /// <param name="groupName"></param>
        /// <param name="resourceCount"></param>
        private void _fireResourceGroupLoadStarted(string groupName, int resourceCount)
        {
            if (this._resourceGroupLoadStarted != null)
            {
                this._resourceGroupLoadStarted(groupName, resourceCount);
            }
        }

        /// <summary>
        /// Internal event firing method 
        /// </summary>
        /// <param name="resource"></param>
        private void _fireResourceStarted(Resource resource)
        {
            if (this._resourceLoadStarted != null)
            {
                this._resourceLoadStarted(resource);
            }
        }

        /// <summary>
        /// Internal event firing method 
        /// </summary>
        private void _fireResourceEnded()
        {
            if (this._resourceLoadEnded != null)
            {
                this._resourceLoadEnded();
            }
        }

        /// <summary>
        /// Internal event firing method 
        /// </summary>
        /// <param name="groupName"></param>
        private void _fireResourceGroupLoadEnded(string groupName)
        {
            if (this._resourceGroupLoadEnded != null)
            {
                this._resourceGroupLoadEnded(groupName);
            }
        }

        /// <summary>
        /// Internal event firing method 
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="resourceCount"></param>
        private void _fireResourceGroupPrepareStarted(string groupName, int resourceCount)
        {
            /*
			if ( _resourceGroupPrepareStarted != null )
			{
				_resourceGroupPrepareStarted( groupName, resourceCount );
			}
			 */
        }

        /// <summary>
        /// Internal event firing method 
        /// </summary>
        /// <param name="resource"></param>
        private void _fireResourcePrepareStarted(Resource resource)
        {
            /*
			if ( _resourcePrepareStarted != null )
			{
				_resourcePrepareStarted( resource );
			}
			 */
        }

        /// <summary>
        /// Internal event firing method 
        /// </summary>
        private void _fireResourcePrepareEnded()
        {
            /*
			if ( _resourcePrepareEnded != null )
			{
				_resourcePrepareEnded();
			}
			 */
        }

        /// <summary>
        /// Internal event firing method 
        /// </summary>
        /// <param name="groupName"></param>
        private void _fireResourceGroupPrepareEnded(string groupName)
        {
            /*
			if ( _resourceGroupPrepareEnded != null )
			{
				_resourceGroupPrepareEnded( groupName );
			}
			 */
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
        public void CreateResourceGroup(string name)
        {
            LogManager.Instance.Write("Creating resource group " + name);
            if (getResourceGroup(name) != null)
            {
                throw new AxiomException("Resource group with name '" + name + "' already exists!");
            }
            var grp = new ResourceGroup();
            grp.Initialized = false;
            grp.Name = name;
            grp.WorldGeometrySceneManager = null;
            resourceGroups.Add(name, grp);
        }

        /// <summary>
        /// Initializes a resource group.
        /// </summary>
        /// <remarks>
        ///	After creating a resource group, adding some resource locations, and
        ///	perhaps pre-declaring some resources using <see cref="DeclareResource(string, string)"/> , but
        ///	before you need to use the resources in the group, you
        ///	should call this method to initialise the group. By calling this,
        ///	you are triggering the following processes:
        ///	<ol>
        ///	<li>Scripts for all resource types which support scripting are
        ///		parsed from the resource locations, and resources within them are
        ///		created (but not loaded yet).</li>
        ///	<li>Creates all the resources which have just pre-declared using
        ///	<see cref="DeclareResource(string, string)"/> (again, these are not loaded yet)</li>
        ///	</ol>
        ///	So what this essentially does is create a bunch of unloaded <see cref="Resource"/>  entries
        ///	in the respective ResourceManagers based on scripts, and resources
        ///	you've pre-declared. That means that code looking for these resources
        ///	will find them, but they won't be taking up much memory yet, until
        ///	they are either used, or they are loaded in bulk using <see cref="LoadResourceGroup(string)"/>.
        ///	Loading the resource group in bulk is entirely optional, but has the
        ///	advantage of coming with progress reporting as resources are loaded.
        /// <para>
        ///	Failure to call this method means that <see cref="LoadResourceGroup(string)"/>  will do
        ///	nothing, and any resources you define in scripts will not be found.
        ///	Similarly, once you have called this method you won't be able to
        ///	pick up any new scripts or pre-declared resources, unless you
        ///	call <see cref="ClearResourceGroup"/> , set up declared resources, and call this
        ///	method again.
        /// </para>
        /// <para>
        ///	When you call <see cref="Root.Initialize(bool)"/> , all resource groups that have already been
        ///	created are automatically initialised too. Therefore you do not need to
        ///	call this method for groups you define and set up before you call
        ///	<see cref="Root.Initialize(bool)"/>. However, since one of the most useful features of
        ///	resource groups is to set them up after the main system initialization
        ///	has occurred (e.g. a group per game level), you must remember to call this
        ///	method for the groups you create after this.
        /// </para>
        /// </remarks>
        /// <param name="groupName">The name of the resource group to initialise</param>
        public void InitializeResourceGroup(string groupName)
        {
            LogManager.Instance.Write("Initializing resource group {0}.", groupName);
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}.", groupName);
            }

            if (!grp.Initialized)
            {
                // Set current group
                _parseResourceGroupScripts(grp);
                this._currentGroup = grp;
                _createDeclaredResources(grp);
                grp.Initialized = true;

                // Reset current group
                this._currentGroup = null;
            }
            LogManager.Instance.Write("\t{0} initialized.", grp.Name);
        }

        /// <summary>
        /// Initialize all resource groups which are yet to be initialised.
        /// </summary>
        /// <see cref="ResourceGroupManager.InitializeResourceGroup"/>
        public void InitializeAllResourceGroups()
        {
            LogManager.Instance.Write("Initializing all resource groups:");

            // Initialize Built-in groups first
            InitializeResourceGroup(ResourceGroupManager.AutoDetectResourceGroupName);
            if (resourceGroups.ContainsKey(ResourceGroupManager.BootstrapResourceGroupName))
            {
                InitializeResourceGroup(ResourceGroupManager.BootstrapResourceGroupName);
            }

            // Intialize all declared resource groups
            foreach (var pair in resourceGroups)
            {
                var grp = pair.Value;

                if (!grp.Initialized)
                {
                    LogManager.Instance.Write("Initializing resource group {0}.", grp.Name);
                    // Set current group
                    _parseResourceGroupScripts(grp);
                    this._currentGroup = grp;
                    _createDeclaredResources(grp);
                    grp.Initialized = true;

                    // Reset current group
                    this._currentGroup = null;
                    LogManager.Instance.Write("\t{0} initialized.", grp.Name);
                }
            }
        }

        #region PrepareResourceGroup

        /// <summary>
        /// Prepares a resource group.
        /// </summary>
        /// <see cref="ResourceGroupManager.PrepareResourceGroup(string, bool, bool)"/>
        public void PrepareResourceGroup(string name)
        {
            PrepareResourceGroup(name, true, true);
        }

        /// <summary>
        /// Prepares a resource group.
        /// </summary>
        /// <see cref="ResourceGroupManager.PrepareResourceGroup(string, bool, bool)"/>
        public void PrepareResourceGroup(string name, bool prepareMainResources)
        {
            PrepareResourceGroup(name, prepareMainResources, true);
        }

        /// <summary>
        /// Prepares a resource group.
        /// </summary>
        /// <remarks>Prepares any created resources which are part of the named group.
        ///	Note that resources must have already been created by calling
        ///	ResourceManager::create, or declared using declareResource() or
        ///	in a script (such as .material and .overlay). The latter requires
        ///	that initialiseResourceGroup has been called. 
        ///
        ///	When this method is called, this class will callback any ResourceGroupListeners
        ///	which have been registered to update them on progress.
        ///	</remarks>
        /// <param name="name">The name of the resource group to prepare.</param>
        /// <param name="prepareMainResources">If true, prepares normal resources associated 
        ///	with the group (you might want to set this to false if you wanted
        ///	to just prepare world geometry in bulk)</param>
        /// <param name="prepareWorldGeom">If true, prepares any linked world geometry
        ///	<see cref="ResourceGroupManager.LinkWorldGeometryToResourceGroup"/></param>
        public void PrepareResourceGroup(string name, bool prepareMainResources, bool prepareWorldGeom)
        {
            LogManager.Instance.Write("Preparing resource group '{0}' - Resources: {1}, World Geometry: {2}", name,
                                       prepareMainResources, prepareWorldGeom);

            // load all created resources
            var grp = getResourceGroup(name);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", name);
            }

            // Set current group
            this._currentGroup = grp;

            // Count up resources for starting event
            var resourceCount = 0;
            if (prepareMainResources)
            {
                foreach (var pair in grp.LoadResourceOrders)
                {
                    var lurl = pair.Value;
                    resourceCount += lurl.Count;
                }
            }
            // Estimate world geometry size
            if (grp.WorldGeometrySceneManager != null && prepareWorldGeom)
            {
                resourceCount += grp.WorldGeometrySceneManager.EstimateWorldGeometry(grp.WorldGeometry);
            }

            _fireResourceGroupPrepareStarted(name, resourceCount);

            // Now load for real
            if (prepareMainResources)
            {
                var keys = new float[grp.LoadResourceOrders.Count];
                grp.LoadResourceOrders.Keys.CopyTo(keys, 0);

                for (ushort i = 0; i < keys.Length; i++)
                {
                    var lurl = grp.LoadResourceOrders[keys[i]];
                    foreach (var res in lurl)
                    {
                        // Fire resource events no matter whether resource needs preparing
                        // or not. This ensures that the number of callbacks
                        // matches the number originally estimated, which is important
                        // for progress bars.
                        _fireResourcePrepareStarted(res);

                        // If preparing one of these resources cascade-prepares another resource, 
                        // the list will get longer! But these should be prepared immediately
                        // Call prepare regardless, already prepared or loaded resources will be skipped
                        res.Prepare();

                        _fireResourcePrepareEnded();
                    }
                }
            }
            // Load World Geometry
            if (grp.WorldGeometrySceneManager != null && prepareWorldGeom)
            {
                grp.WorldGeometrySceneManager.PrepareWorldGeometry(grp.WorldGeometry);
            }
            _fireResourceGroupPrepareEnded(name);

            // reset current group
            this._currentGroup = null;

            LogManager.Instance.Write("Finished preparing resource group " + name);
        }

        #endregion PrepareResourceGroup

        #region LoadResourceGroup Method

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
        public void LoadResourceGroup(string name)
        {
            LoadResourceGroup(name, true, true);
        }

        /// <param name="loadMainResources">If true, loads normal resources associated
        /// with the group (you might want to set this to false if you wanted
        /// to just load world geometry in bulk)</param>
        /// <param name="name"></param>
        /// <param name="loadWorldGeom">If true, loads any linked world geometry <see>ResourceGroupManager.LinkWorldGeometryToResourceGroup</see></param>
        public void LoadResourceGroup(string name, bool loadMainResources, bool loadWorldGeom)
        {
            // Can only bulk-load one group at a time (reasonable limitation I think)
            //OGRE_LOCK_AUTO_MUTEX
            LogManager.Instance.Write("Loading resource group '{0}' - Resources: {1} World Geometry: {2}", name,
                                       loadMainResources, loadWorldGeom);
            // load all created resources
            var grp = getResourceGroup(name);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", name);
            }

            // Set current group
            this._currentGroup = grp;

            // Count up resources for starting event
            var resourceCount = 0;
            if (loadMainResources)
            {
                foreach (var pair in grp.LoadResourceOrders)
                {
                    var lurl = pair.Value;
                    resourceCount += lurl.Count;
                }
            }
            // Estimate world geometry size
            if (grp.WorldGeometrySceneManager != null && loadWorldGeom)
            {
                resourceCount += grp.WorldGeometrySceneManager.EstimateWorldGeometry(grp.WorldGeometry);
            }

            _fireResourceGroupLoadStarted(name, resourceCount);

            // Now load for real
            if (loadMainResources)
            {
                var keys = new float[grp.LoadResourceOrders.Count];
                grp.LoadResourceOrders.Keys.CopyTo(keys, 0);

                for (ushort i = 0; i < keys.Length; i++)
                {
                    var lurl = grp.LoadResourceOrders[keys[i]];
                    foreach (var res in lurl)
                    {
                        // Fire resource events no matter whether resource is already
                        // loaded or not. This ensures that the number of callbacks
                        // matches the number originally estimated, which is important
                        // for progress bars.
                        _fireResourceStarted(res);

                        // If loading one of these resources cascade-loads another resource,
                        // the list will get longer! But these should be loaded immediately
                        // Call load regardless, already loaded resources will be skipped
                        res.Load();

                        _fireResourceEnded();
                    }
                }
            }
            // Load World Geometry
            if (grp.WorldGeometrySceneManager != null && loadWorldGeom)
            {
                grp.WorldGeometrySceneManager.SetWorldGeometry(grp.WorldGeometry);
            }
            _fireResourceGroupLoadEnded(name);

#warning TODO
            // group is loaded
            //grp->groupStatus = ResourceGroup::LOADED;

            // reset current group
            this._currentGroup = null;

            LogManager.Instance.Write("Finished loading resource group {0}.", name);
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
        /// <param name="groupName">The name to of the resource group to unload.</param>
        public void UnloadResourceGroup(string groupName)
        {
            UnloadResourceGroup(groupName, true);
        }

        /// <summary>Unloads a resource group.</summary>
        /// <remarks>
        /// This method unloads all the resources that have been declared as
        /// being part of the named resource group. Note that these resources
        /// will still exist in their respective ResourceManager classes, but
        /// will be in an unloaded state. If you want to remove them entirely,
        /// you should use ClearResourceGroup or DestroyResourceGroup.
        /// </remarks>
        /// <param name="groupName">The name to of the resource group to unload.</param>
        /// <param name="reloadableOnly"></param>
        public void UnloadResourceGroup(string groupName, bool reloadableOnly)
        {
            LogManager.Instance.Write("Unloading resource group {0}.", groupName);
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }
            // Set current group
            this._currentGroup = grp;

            var grpKeys = new float[grp.LoadResourceOrders.Count];
            grp.LoadResourceOrders.Keys.CopyTo(grpKeys, 0);

            for (var i = grp.LoadResourceOrders.Count - 1; i >= 0; i--)
            {
                var lurl = grp.LoadResourceOrders[grpKeys[i]];
                foreach (var res in lurl)
                {
                    if (!reloadableOnly || res.IsReloadable)
                    {
                        res.Unload();
                        res.Dispose();
                    }
                }
            }

            // reset current group
            this._currentGroup = null;
            LogManager.Instance.Write("Finished unloading resource group {0}.", groupName);
        }

        /// <summary>
        /// Unload all resources which are not referenced by any other object.
        /// </summary>
        /// <remarks>
        /// This method behaves like unloadResourceGroup, except that it only
        /// unloads resources in the group which are not in use, ie not referenced
        /// by other objects. This allows you to free up some memory selectively
        /// whilst still keeping the group around (and the resources present,
        /// just not using much memory).
        /// </remarks>
        /// <param name="groupName">The name of the group to check for unreferenced resources</param>
        public void UnloadUnreferencedResourcesInGroup(string groupName)
        {
            UnloadUnreferencedResourcesInGroup(groupName, true);
        }

        /// <summary>
        /// Unload all resources which are not referenced by any other object.
        /// </summary>
        /// <remarks>
        /// This method behaves like unloadResourceGroup, except that it only
        /// unloads resources in the group which are not in use, ie not referenced
        /// by other objects. This allows you to free up some memory selectively
        /// whilst still keeping the group around (and the resources present,
        /// just not using much memory).
        /// </remarks>
        /// <param name="groupName">The name of the group to check for unreferenced resources</param>
        /// <param name="reloadableOnly">If true (the default), only unloads resources
        /// which can be subsequently automatically reloaded</param>
        public void UnloadUnreferencedResourcesInGroup(string groupName, bool reloadableOnly)
        {
            throw new NotImplementedException();
        }

        /// <summary>Clears a resource group.</summary>
        /// <remarks>
        /// This method unloads all resources in the group, but in addition it
        /// removes all those resources from their ResourceManagers, and then
        /// clears all the members from the list. That means after calling this
        /// method, there are no resources declared as part of the named group
        /// any more. Resource locations still persist though.
        /// </remarks>
        /// <param name="groupName">The name to of the resource group to clear.</param>
        public void ClearResourceGroup(string groupName)
        {
            LogManager.Instance.Write("Clearing resource group {0}", groupName);
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }
            // set current group
            this._currentGroup = grp;
            _dropGroupContents(grp);
            // clear initialised flag
            grp.Initialized = false;
            // reset current group
            this._currentGroup = null;
            LogManager.Instance.Write("Finished clearing resource group {0}", groupName);
        }

        /// <summary>
        /// Destroys a resource group, clearing it first, destroying the resources
        /// which are part of it, and then removing it from
        /// the list of resource groups.
        /// </summary>
        /// <param name="groupName">The name of the resource group to destroy.</param>
        public void DestroyResourceGroup(string groupName)
        {
            LogManager.Instance.Write("Destroying resource group " + groupName);
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }
            // set current group
            this._currentGroup = grp;
            UnloadResourceGroup(groupName, false); // will throw an exception if name not valid
            _dropGroupContents(grp);
            _deleteGroup(grp);
            resourceGroups.Remove(groupName);
            // reset current group
            this._currentGroup = null;
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
        public void AddResourceLocation(string name, string locType)
        {
            AddResourceLocation(name, locType, DefaultResourceGroupName, false, false);
        }

        /// <param name="locType"></param>
        /// <param name="resGroup">
        /// The name of the resource group for which this location is
        /// to apply. ResourceGroupManager.DefaultResourceGroupName is the
        /// default group which always exists, and can
        /// be used for resources which are unlikely to be unloaded until application
        /// shutdown. Otherwise it must be the name of a group; if it
        /// has not already been created with createResourceGroup then it is created
        /// automatically.
        /// </param>
        /// <param name="name"></param>
        public void AddResourceLocation(string name, string locType, string resGroup)
        {
            AddResourceLocation(name, locType, resGroup, false, false);
        }

        /// <param name="locType"></param>
        /// <param name="recursive">
        /// Whether subdirectories will be searched for files when using
        /// a pattern match (such as *.material), and whether subdirectories will be
        /// indexed. This can slow down initial loading of the archive and searches.
        /// When opening a resource you still need to use the fully qualified name,
        /// this allows duplicate names in alternate paths.
        /// </param>
        /// <param name="name"></param>
        public void AddResourceLocation(string name, string locType, bool recursive)
        {
            AddResourceLocation(name, locType, DefaultResourceGroupName, recursive, false);
        }

        /// <param name="locType"></param>
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
        /// <param name="name"></param>
        /// <param name="monitor"></param>
        public void AddResourceLocation(string name, string locType, string resGroup, bool recursive, bool monitor)
        {
            var grp = getResourceGroup(resGroup);
            if (grp == null)
            {
                CreateResourceGroup(resGroup);
                grp = getResourceGroup(resGroup);
            }

            // Get archive
            var arch = ArchiveManager.Instance.Load(name, locType);
            // Add to location list
            var loc = new ResourceLocation();
            loc.Archive = arch;
            loc.Recursive = recursive;
            grp.LocationList.Add(loc);
            // Index resources
            var vec = arch.Find("*", recursive);
            foreach (var it in vec)
            {
                grp.Add(it, arch);
            }

            if (arch.IsMonitorable && monitor)
            {
                loc.Watcher = new Watcher(name, recursive);
            }

            LogManager.Instance.Write("Added resource location '{0}' of type '{1}' to resource group '{2}'{3}", name, locType,
                                       resGroup, recursive ? " with recursive option" : "");
        }

        #endregion AddResourceLocation Method

        #region RemoveResourceLocation Method

        /// <overloads>
        /// <summary>
        /// Removes a resource location from the search path.
        /// </summary>
        /// <param name="locationName">the name of the ResourceLocation</param>
        /// </overloads>
        public void RemoveResourceLocation(string locationName)
        {
            RemoveResourceLocation(locationName, DefaultResourceGroupName);
        }

        /// <param name="locationName"></param>
        /// <param name="groupName">the name of the ResourceGroup</param>
        public void RemoveResourceLocation(string locationName, string groupName)
        {
            LogManager.Instance.Write("Remove Resource Location " + groupName);
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }

            // Remove from location list
            foreach (var loc in grp.LocationList)
            {
                var arch = loc.Archive;
                if (arch.Name == locationName)
                {
                    // Remove from index
                    grp.Remove(arch);

                    grp.LocationList.Remove(loc);
                    break;
                }
            }
            LogManager.Instance.Write("Removed resource location " + locationName);
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
        /// are not available through their ResourceManager) until <see cref="InitializeResourceGroup"/>
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
        public void DeclareResource(string name, string resourceType)
        {
            DeclareResource(name, resourceType, DefaultResourceGroupName, null, new NameValuePairList());
        }

        /// <param name="resourceType"></param>
        /// <param name="groupName">The name of the group to which it will belong.</param>
        /// <param name="name"></param>
        public void DeclareResource(string name, string resourceType, string groupName)
        {
            DeclareResource(name, resourceType, groupName, null, new NameValuePairList());
        }

        /// <param name="loader"></param>
        /// <param name="loadParameters">
        /// A list of name / value pairs which supply custom
        /// parameters to the resource which will be required before it can
        /// be loaded. These are specific to the resource type.
        /// </param>
        /// <param name="name"></param>
        /// <param name="resourceType"></param>
        public void DeclareResource(string name, string resourceType, IManualResourceLoader loader,
                                     NameValuePairList loadParameters)
        {
            DeclareResource(name, resourceType, DefaultResourceGroupName, loader, loadParameters);
        }

        /// <param name="resourceType"></param>
        /// <param name="groupName">The name of the group to which it will belong.</param>
        /// <param name="loader"></param>
        /// <param name="loadParameters">
        /// A list of name / value pairs which supply custom
        /// parameters to the resource which will be required before it can
        /// be loaded. These are specific to the resource type.
        /// </param>
        /// <param name="name"></param>
        public void DeclareResource(string name, string resourceType, string groupName, IManualResourceLoader loader,
                                     NameValuePairList loadParameters)
        {
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", name);
            }

            ResourceDeclaration dcl;
            dcl.Parameters = loadParameters;
            dcl.ResourceName = name;
            dcl.ResourceType = resourceType;
            dcl.Loader = loader;
            grp.ResourceDeclarations.Add(dcl);
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
        public void UndeclareResource(string name, string groupName)
        {
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }

            foreach (var resDec in grp.ResourceDeclarations)
            {
                if (resDec.ResourceName == name)
                {
                    grp.ResourceDeclarations.Remove(resDec);
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

        public IO.Stream OpenResource(string resourceName)
        {
            return OpenResource(resourceName, DefaultResourceGroupName);
        }

        public IO.Stream OpenResource(string resourceName, string groupName)
        {
            return OpenResource(resourceName, groupName, true);
        }

        public IO.Stream OpenResource(string resourceName, string groupName, bool searchGroupsIfNotFound)
        {
            return OpenResource(resourceName, groupName, searchGroupsIfNotFound, null);
        }

        public virtual IO.Stream OpenResource(string resourceName, string groupName, bool searchGroupsIfNotFound,
                                               Resource resourceBeingLoaded)
        {
            // Try to find in resource index first
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0} : ResourceGroupManager::OpenResource", groupName);
            }

            Archive arch = null;
            if (grp.ResourceIndexCaseSensitive.ContainsKey(resourceName))
            {
                // Found in the index
                arch = grp.ResourceIndexCaseSensitive[resourceName];
                var stream = arch.Open(resourceName);

                //Maybe, the stream is null 'cause we added a resource location recursively, so 
                //we try to find the wanted resource with the following search.
                if (stream == null)
                {
                    var fileList = FindResourceFileInfo(groupName, "*" + System.IO.Path.GetExtension(resourceName));
                    foreach (var info in fileList)
                    {
                        if (!info.Basename.Contains(resourceName))
                        {
                            continue;
                        }

                        stream = arch.Open(info.Basename);
                        break;
                    }
                }

                return stream;
            }
            else
            {
                // try case insensitive
                var lc = resourceName.ToLower();
                if (grp.ResourceIndexCaseInsensitive.ContainsKey(lc))
                {
                    // Found in the index
                    arch = grp.ResourceIndexCaseInsensitive[lc];
                    return arch.Open(lc);
                }
                else
                {
                    // Search the hard way
                    foreach (var rl in grp.LocationList)
                    {
                        arch = rl.Archive;
                        if (arch.Exists(resourceName))
                        {
                            return arch.Open(resourceName);
                        }
                    }
                }
            }

            // Not found
            if (searchGroupsIfNotFound)
            {
                grp = _findGroupContainingResourceImpl(resourceName);
                if (grp != null)
                {
                    if (resourceBeingLoaded != null)
                    {
                        resourceBeingLoaded.Group = grp.Name;
                    }
                    return OpenResource(resourceName, grp.Name, false);
                }
                else
                {
                    throw new IO.FileNotFoundException("Cannot locate resource " + resourceName + " in resource group " + groupName +
                                                        " or any other group.");
                }
            }
            throw new IO.FileNotFoundException("Cannot locate resource " + resourceName + " in resource group " + groupName +
                                                ".");
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
        public List<IO.Stream> OpenResources(string pattern)
        {
            return OpenResources(pattern, DefaultResourceGroupName);
        }

        /// <param name="pattern"></param>
        /// <param name="groupName">
        /// The resource group; this determines which locations are searched.
        /// </param>
        public List<IO.Stream> OpenResources(string pattern, string groupName)
        {
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }

            // Iterate through all the archives and build up a combined list of
            // streams
            var ret = new List<IO.Stream>();

            foreach (var li in grp.LocationList)
            {
                var arch = li.Archive;
                // Find all the names based on whether this archive is recursive
                var names = arch.Find(pattern, li.Recursive);

                // Iterate over the names and load a stream for each
                foreach (var resource in names)
                {
                    var ptr = arch.Open(resource);
                    if (ptr != null)
                    {
                        ret.Add(ptr);
                    }
                }
            }
            return ret;
        }

        #endregion OpenResources Method

        #region CreateResource Method

        /// <summary>
        /// Create a new resource file in a given group.
        /// </summary>
        /// <remarks>This method creates a new file in a resource group and passes you back a writeable stream</remarks>
        /// <param name="filename">The name of the file to create</param>
        /// <returns>An open Stream</returns>
        public IO.Stream CreateResource(string filename)
        {
            return CreateResource(filename, ResourceGroupManager.DefaultResourceGroupName, false, String.Empty);
        }

        /// <summary>
        /// Create a new resource file in a given group.
        /// </summary>
        /// <remarks>This method creates a new file in a resource group and passes you back a writeable stream</remarks>
        /// <param name="filename">The name of the file to create</param>
        /// <param name="groupName">The name of the group in which to create the file</param>
        /// <returns>An open Stream</returns>
        public IO.Stream CreateResource(string filename, string groupName)
        {
            return CreateResource(filename, groupName, false, String.Empty);
        }

        /// <summary>
        /// Create a new resource file in a given group.
        /// </summary>
        /// <remarks>This method creates a new file in a resource group and passes you back a writeable stream</remarks>
        /// <param name="filename">The name of the file to create</param>
        /// <param name="groupName">The name of the group in which to create the file</param>
        /// <param name="overwrite">If true, an existing file will be overwritten, if false
        /// an error will occur if the file already exists</param>
        /// <returns>An open Stream</returns>
        public IO.Stream CreateResource(string filename, string groupName, bool overwrite)
        {
            return CreateResource(filename, groupName, overwrite, String.Empty);
        }

        /// <summary>
        /// Create a new resource file in a given group.
        /// </summary>
        /// <remarks>This method creates a new file in a resource group and passes you back a writeable stream</remarks>
        /// <param name="filename">The name of the file to create</param>
        /// <param name="groupName">The name of the group in which to create the file</param>
        /// <param name="overwrite">If true, an existing file will be overwritten, if false
        /// an error will occur if the file already exists</param>
        /// <param name="locationPattern">If the resource group contains multiple locations,
        /// then usually the file will be created in the first writable location. If you
        /// want to be more specific, you can include a location pattern here and
        /// only locations which match that pattern (as determined by <seealso cref="Regex.IsMatch(string)"/>)
        /// will be considered candidates for creation.</param>
        /// <returns>An open Stream</returns>
        public IO.Stream CreateResource(string filename, string groupName, bool overwrite, string locationPattern)
        {
            //OGRE_LOCK_AUTO_MUTEX
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}.", groupName);
            }

            //OGRE_LOCK_MUTEX(grp->OGRE_AUTO_MUTEX_NAME) // lock group mutex

            foreach (var rl in grp.LocationList)
            {
                var arch = rl.Archive;

                if (!arch.IsReadOnly &&
                     (String.IsNullOrEmpty(locationPattern) || (new Regex(locationPattern)).IsMatch(arch.Name)))
                {
                    if (!overwrite && arch.Exists(filename))
                    {
                        throw new AxiomException("Cannot overwrite existing file " + filename);
                    }

                    // create it
                    var ret = arch.Create(filename);
                    grp.Add(filename, arch);

                    return ret;
                }
            }
            throw new AxiomException("Cannot find a writable location in group " + groupName);
        }

        #endregion CreateResource Method

        #region DeleteResource Method

        /// <summary>
        /// Delete a single resource file.
        /// </summary>
        /// <param name="filename">The name of the file to delete</param>
        public void DeleteResource(string filename)
        {
            DeleteResource(filename, ResourceGroupManager.DefaultResourceGroupName, String.Empty);
        }

        /// <summary>
        /// Delete a single resource file.
        /// </summary>
        /// <param name="filename">The name of the file to delete</param>
        /// <param name="groupName">The name of the group in which to search</param>
        public void DeleteResource(string filename, string groupName)
        {
            DeleteResource(filename, groupName, String.Empty);
        }

        /// <summary>
        /// Delete a single resource file.
        /// </summary>
        /// <param name="filename">The name of the file to delete</param>
        /// <param name="groupName">The name of the group in which to search</param>
        /// <param name="locationPattern">If the resource group contains multiple locations,
        /// then usually first matching file found in any location will be deleted. If you
        /// want to be more specific, you can include a location pattern here and
        /// only locations which match that pattern (as determined by <seealso cref="Regex.IsMatch(string)"/>)
        /// will be considered candidates for deletion.</param>
        public void DeleteResource(string filename, string groupName, string locationPattern)
        {
            //OGRE_LOCK_AUTO_MUTEX
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}.", groupName);
            }

            //OGRE_LOCK_MUTEX(grp->OGRE_AUTO_MUTEX_NAME) // lock group mutex

            foreach (var rl in grp.LocationList)
            {
                var arch = rl.Archive;

                if (!arch.IsReadOnly &&
                     (String.IsNullOrEmpty(locationPattern) || (new Regex(locationPattern)).IsMatch(arch.Name)))
                {
                    if (arch.Exists(filename))
                    {
                        arch.Remove(filename);
                        grp.Remove(filename, arch);

                        // only remove one file
                        break;
                    }
                }
            }
        }

        #endregion DeleteResource Method

        #region DeleteMatchingResources Method

        /// <summary>
        /// Delete all matching resource files.
        /// </summary>
        /// <param name="filePattern">The pattern (see <seealso cref="Regex.IsMatch(string)"/>) of the files to delete. </param>
        public void DeleteMatchingResources(string filePattern)
        {
            DeleteMatchingResources(filePattern, ResourceGroupManager.DefaultResourceGroupName, String.Empty);
        }

        /// <summary>
        /// Delete all matching resource files.
        /// </summary>
        /// <param name="filePattern">The pattern (see <seealso cref="Regex.IsMatch(string)"/>) of the files to delete. </param>
        /// <param name="groupName">The name of the group in which to search</param>
        public void DeleteMatchingResources(string filePattern, string groupName)
        {
            DeleteMatchingResources(filePattern, groupName, String.Empty);
        }

        /// <summary>
        /// Delete all matching resource files.
        /// </summary>
        /// <param name="filePattern">The pattern (see <seealso cref="Regex.IsMatch(string)"/>) of the files to delete. </param>
        /// <param name="groupName">The name of the group in which to search</param>
        /// <param name="locationPattern">If the resource group contains multiple locations,
        /// then usually all matching files in any location will be deleted. If you
        /// want to be more specific, you can include a location pattern here and
        /// only locations which match that pattern (as determined by <seealso cref="Regex.IsMatch(string)"/>)
        /// will be considered candidates for deletion.</param>
        public void DeleteMatchingResources(string filePattern, string groupName, string locationPattern)
        {
            //OGRE_LOCK_AUTO_MUTEX
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}.", groupName);
            }

            //OGRE_LOCK_MUTEX(grp->OGRE_AUTO_MUTEX_NAME) // lock group mutex

            foreach (var rl in grp.LocationList)
            {
                var arch = rl.Archive;

                if (!arch.IsReadOnly &&
                     (String.IsNullOrEmpty(locationPattern) || (new Regex(locationPattern)).IsMatch(arch.Name)))
                {
                    foreach (var f in arch.Find(filePattern))
                    {
                        arch.Remove(f);
                        grp.Remove(f, arch);
                    }
                }
            }
        }

        #endregion DeleteMatchingResources Method

        /// <summary>List all file names in a resource group.</summary>
        /// <remarks>
        /// This method only returns filenames, you can also retrieve other information using listFileInfo.
        /// </remarks>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A list of filenames matching the criteria, all are fully qualified</returns>
        public List<string> ListResourceNames(string groupName)
        {
            var vec = new List<string>();

            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }

            // Iterate over the archives
            foreach (var rl in grp.LocationList)
            {
                var lst = rl.Archive.List(rl.Recursive);
                vec.AddRange(lst);
            }

            return vec;
        }

        /// <summary>List all files in a resource group with accompanying information.</summary>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A list of structures detailing quite a lot of information about all the files in the archive.</returns>
        public FileInfoList ListResourceFileInfo(string groupName)
        {
            var vec = new FileInfoList();

            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }

            // Iterate over the archives
            foreach (var rl in grp.LocationList)
            {
                var lst = rl.Archive.ListFileInfo(rl.Recursive);
                vec.AddRange(lst);
            }

            return vec;
        }

        /// <summary>
        /// Retrieve the modification time of a given file
        /// </summary>
        /// <see cref="ResourceGroupManager.ResourceModifiedTime(ResourceGroup, string)"/>
        public DateTime ResourceModifiedTime(string groupName, string resourceName)
        {
            // Try to find in resource index first
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }

            return ResourceModifiedTime(grp, resourceName);
        }

        /// <summary>
        /// Retrieve the modification time of a given file
        /// </summary>
        /// <param name="group"></param>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        public DateTime ResourceModifiedTime(ResourceGroup group, string resourceName)
        {
            if (group.ResourceIndexCaseSensitive.ContainsKey(resourceName))
            {
                return group.ResourceIndexCaseSensitive[resourceName].GetModifiedTime(resourceName);
            }
            else
            {
                // try case insensitive
                var lcResourceName = resourceName.ToLower();
                if (group.ResourceIndexCaseInsensitive.ContainsKey(lcResourceName))
                {
                    return group.ResourceIndexCaseInsensitive[lcResourceName].GetModifiedTime(resourceName);
                }
                else
                {
                    // Search the hard way
                    foreach (var rl in group.LocationList)
                    {
                        var arch = rl.Archive;
                        var testTime = arch.GetModifiedTime(resourceName);

                        if (testTime > DateTime.MinValue)
                        {
                            return testTime;
                        }
                    }
                }
            }

            return DateTime.MinValue;
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
        public List<string> FindResourceNames(string groupName, string pattern)
        {
            var vec = new List<string>();

            // Try to find in resource index first
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }

            // Iterate over the archives
            foreach (var rl in grp.LocationList)
            {
                var lst = rl.Archive.Find(pattern, rl.Recursive);
                vec.AddRange(lst);
            }

            return vec;
        }

        /// <summary>Find out if the named file exists in a group. </summary>
        /// <param name="group">The name of the resource group</param>
        /// <param name="filename">Fully qualified name of the file to test for</param>
        public bool ResourceExists(string group, string filename)
        {
            // Try to find in resource index first
            var grp = getResourceGroup(group);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", group);
            }

            return ResourceExists(grp, filename);
        }

        /// <summary>Find out if the named file exists in a group. </summary>
        /// <param name="group">the resource group</param>
        /// <param name="filename">Fully qualified name of the file to test for</param>
        public bool ResourceExists(ResourceGroup group, string filename)
        {
            if (group.ResourceIndexCaseSensitive.ContainsKey(filename))
            {
                return true;
            }
            else
            {
                var lc = filename.ToLower();
                if (group.ResourceIndexCaseInsensitive.ContainsKey(lc))
                {
                    return true;
                }
                else
                {
                    foreach (var rl in group.LocationList)
                    {
                        var arch = rl.Archive;
                        if (arch.Exists(filename))
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
        public FileInfoList FindResourceFileInfo(string groupName, string pattern)
        {
            var vec = new FileInfoList();

            // Try to find in resource index first
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }

            // Iterate over the archives
            foreach (var rl in grp.LocationList)
            {
                var lst = rl.Archive.FindFileInfo(pattern, rl.Recursive);
                vec.AddRange(lst);
            }

            return vec;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public Math.Tuple<bool, string> FindGroupContainingResource(string filename)
        {
            var grp = _findGroupContainingResourceImpl(filename);

            if (grp == null)
            {
                return new Math.Tuple<bool, string>(false, null);
            }
            return new Math.Tuple<bool, string>(true, grp.Name);
        }

        /// <summary>
        /// Adds a ResourceGroupListener which will be called back during
        /// resource loading events.
        /// </summary>
        /// <param name="rgl"></param>
        public void AddResourceGroupListener(IResourceGroupListener rgl)
        {
            if (rgl != null)
            {
                this._resourceGroupListeners.Add(rgl);
                this._resourceGroupScriptingStarted += new ResourceGroupScriptingStarted(rgl.ResourceGroupScriptingStarted);
                this._resourceGroupScriptingEnded += new ResourceGroupScriptingEnded(rgl.ResourceGroupScriptingEnded);
                this._resourceGroupLoadStarted += new ResourceGroupLoadStarted(rgl.ResourceGroupLoadStarted);
                this._resourceGroupLoadEnded += new ResourceGroupLoadEnded(rgl.ResourceGroupLoadEnded);
                this._resourceLoadStarted += new ResourceLoadStarted(rgl.ResourceLoadStarted);
                this._resourceLoadEnded += new ResourceLoadEnded(rgl.ResourceLoadEnded);
                this._scriptParseStarted += new ScriptParseStarted(rgl.ScriptParseStarted);
                this._scriptParseEnded += new ScriptParseEnded(rgl.ScriptParseEnded);
                this._worldGeometryStageStarted += new WorldGeometryStageStarted(rgl.WorldGeometryStageStarted);
                this._worldGeometryStageEnded += new WorldGeometryStageEnded(rgl.WorldGeometryStageEnded);
            }
        }

        /// <summary>
        /// Removes a ResourceGroupListener
        /// </summary>
        /// <param name="rgl"></param>
        public void RemoveResourceGroupListener(IResourceGroupListener rgl)
        {
            if (rgl != null)
            {
                this._resourceGroupListeners.Remove(rgl);
                this._resourceGroupScriptingStarted -= new ResourceGroupScriptingStarted(rgl.ResourceGroupScriptingStarted);
                this._resourceGroupScriptingEnded -= new ResourceGroupScriptingEnded(rgl.ResourceGroupScriptingEnded);
                this._resourceGroupLoadStarted -= new ResourceGroupLoadStarted(rgl.ResourceGroupLoadStarted);
                this._resourceGroupLoadEnded -= new ResourceGroupLoadEnded(rgl.ResourceGroupLoadEnded);
                this._resourceLoadStarted -= new ResourceLoadStarted(rgl.ResourceLoadStarted);
                this._resourceLoadEnded -= new ResourceLoadEnded(rgl.ResourceLoadEnded);
                this._scriptParseStarted -= new ScriptParseStarted(rgl.ScriptParseStarted);
                this._scriptParseEnded -= new ScriptParseEnded(rgl.ScriptParseEnded);
                this._worldGeometryStageStarted -= new WorldGeometryStageStarted(rgl.WorldGeometryStageStarted);
                this._worldGeometryStageEnded -= new WorldGeometryStageEnded(rgl.WorldGeometryStageEnded);
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
        public void LinkWorldGeometryToResourceGroup(string groupName, string worldGeometry, SceneManager sceneManager)
        {
            // Try to find in resource index first
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }

            grp.WorldGeometry = worldGeometry;
            grp.WorldGeometrySceneManager = sceneManager;
        }

        /// <summary>
        /// Clear any link to world geometry from a resource group.
        /// </summary>
        /// <remarks>Basically undoes a previous call to linkWorldGeometryToResourceGroup.</remarks>
        /// <param name="groupName">The name of the resource group</param>
        public void UnlinkWorldGeometryFromResourceGroup(string groupName)
        {
            // Try to find in resource index first
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
            }

            grp.WorldGeometry = "";
            grp.WorldGeometrySceneManager = null;
        }

        /// <summary>
        /// Shutdown all ResourceManagers, performed as part of clean-up.
        /// </summary>
        public void ShutdownAll()
        {
            foreach (var pair in this._resourceManagers)
            {
                var rm = pair.Value;
                rm.UnloadAll();
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
            var vec = new List<string>();

            foreach (var pair in this._resourceGroups)
            {
                var rg = pair.Value;
                vec.Add(rg.Name);
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
        public ResourceDeclaration[] getResourceDeclarationList(string groupName)
        {
            // Try to find in resource index first
            var grp = getResourceGroup(groupName);
            if (grp == null)
            {
                throw new AxiomException("Cannot find a group named {0}", groupName);
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
        protected ResourceGroup getResourceGroup(string name)
        {
            if (this._resourceGroups.ContainsKey(name))
            {
                return this._resourceGroups[name];
            }

            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="group"></param>
        protected void addCreatedResource(Resource resource, ResourceGroup group)
        {
            //OGRE_LOCK_MUTEX(grp.OGRE_AUTO_MUTEX_NAME)
            var order = resource.Creator.LoadingOrder;
            LoadUnloadResourceList loadList = null;

            if (!group.LoadResourceOrders.TryGetValue(order, out loadList))
            {
                loadList = new LoadUnloadResourceList();
                group.LoadResourceOrders.Add(order, loadList);
            }
            loadList.Add(resource);
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
        public void RegisterResourceManager(string resourceType, ResourceManager rm)
        {
            LogManager.Instance.Write("Registering ResourceManager for type {0}", resourceType);
            this._resourceManagers[resourceType] = rm;
        }

        /// <summary>
        /// Internal method for unregistering a ResourceManager.
        /// </summary>
        /// <remarks>
        /// ResourceManagers that wish to parse scripts must also call unregisterScriptLoader.
        /// </remarks>
        /// <param name="resourceType">String identifying the resource type.</param>
        public void UnregisterResourceManager(string resourceType)
        {
            LogManager.Instance.Write("Unregistering ResourceManager for type {0}", resourceType);
            if (this._resourceManagers.ContainsKey(resourceType))
            {
                this._resourceManagers.Remove(resourceType);
            }
        }

        /// <summary>
        /// Internal method for registering a ScriptLoader. ScriptLoaders parse scripts when resource groups are initialised.
        /// </summary>
        /// <param name="su">ScriptLoader instance.</param>
        public void RegisterScriptLoader(IScriptLoader su)
        {
            var patterns = new StringBuilder();
            foreach (var pattern in su.ScriptPatterns)
            {
                patterns.Append(pattern + " ");
            }
            LogManager.Instance.Write("Registering ScriptLoader for patterns {0}", patterns);
            if (!this._scriptLoaderOrders.ContainsKey(su.LoadingOrder))
            {
                this._scriptLoaderOrders.Add(su.LoadingOrder, new List<IScriptLoader>());
            }

            this._scriptLoaderOrders[su.LoadingOrder].Add(su);
        }

        /// <summary>
        /// Internal method for unregistering a ScriptLoader.
        /// </summary>
        /// <param name="su">ScriptLoader instance.</param>
        public void UnregisterScriptLoader(IScriptLoader su)
        {
            var patterns = new StringBuilder();
            foreach (var pattern in su.ScriptPatterns)
            {
                patterns.Append(pattern + " ");
            }
            LogManager.Instance.Write("Unregistering ScriptLoader for patterns {0}", patterns.ToString());
            if (this._scriptLoaderOrders.ContainsKey(su.LoadingOrder))
            {
                this._scriptLoaderOrders[su.LoadingOrder].Remove(su);
            }
        }

        /// <summary>Internal method called by ResourceManager when a resource is created.</summary>
        /// <param name="res">reference to resource</param>
        public void notifyResourceCreated(Resource res)
        {
            if (this._currentGroup != null)
            {
                // Use current group (batch loading)
                _addCreatedResource(res, this._currentGroup);
            }
            else
            {
                // Find group
                var grp = getResourceGroup(res.Group);
                if (grp != null)
                {
                    _addCreatedResource(res, grp);
                }
            }
        }

        /// <summary>Internal method called by ResourceManager when a resource is removed.</summary>
        /// <param name="res">reference to resource</param>
        public void notifyResourceRemoved(Resource res)
        {
            if (this._currentGroup != null)
            {
                // Do nothing - we're batch unloading so list will be cleared
            }
            else
            {
                // Find group
                var grp = getResourceGroup(res.Group);
                if (grp != null)
                {
                    if (grp.LoadResourceOrders.ContainsKey(res.Creator.LoadingOrder))
                    {
                        // Iterate over the resource list and remove
                        var resList = grp.LoadResourceOrders[res.Creator.LoadingOrder];
                        foreach (var r in resList)
                        {
                            if (ReferenceEquals(r, res))
                            {
                                // this is the one
                                resList.Remove(r);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void notifyResourceGroupChanged(string oldGroup, Resource resource)
        {
            //OGRE_LOCK_AUTO_MUTEX
            ResourceGroup oldGrp = null;
            // New group
            var newGrp = getResourceGroup(resource.Group);
            // find old entry
            if (this._resourceGroups.TryGetValue(oldGroup, out oldGrp))
            {
                var order = resource.Creator.LoadingOrder;
                LoadUnloadResourceList loadList = null;
                if (oldGrp.LoadResourceOrders.TryGetValue(order, out loadList))
                {
                    foreach (var item in loadList)
                    {
                        if (item == resource)
                        {
                            addCreatedResource(item, newGrp);
                            loadList.Remove(item);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>Internal method called by ResourceManager when all resources for that manager are removed.</summary>
        /// <param name="manager">the manager for which all resources are being removed</param>
        public void notifyAllResourcesRemoved(ResourceManager manager)
        {
            int index;
            // Iterate over all groups
            foreach (var rgPair in this._resourceGroups)
            {
                var rg = rgPair.Value;
                // Iterate over all priorities
                foreach (var rlPair in rg.LoadResourceOrders)
                {
                    var rl = rlPair.Value;
                    index = 0;
                    // Iterate over all resources
                    while (rl.Count != 0 && index != rl.Count)
                    {
                        var res = rl[index];

                        if (res.Creator == manager)
                        {
                            // Increment first since iterator will be invalidated
                            rl.Remove(res);
                        }
                        else
                        {
                            index++;
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
        public void notifyWorldGeometryStageStarted(string description)
        {
            if (this._worldGeometryStageStarted != null)
            {
                this._worldGeometryStageStarted(description);
            }
        }

        /// <summary>Notify this manager that one stage of world geometry loading has been completed.</summary>
        /// <remarks>
        /// Custom SceneManagers which load custom world geometry should call this
        /// method the number of times equal to the value they return from
        /// SceneManager.estimateWorldGeometry while loading their geometry.
        /// </remarks>
        public void notifyWorldGeometryStageEnded()
        {
            if (this._worldGeometryStageStarted != null)
            {
                this._worldGeometryStageEnded();
            }
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
        private void _parseResourceGroupScripts(ResourceGroup grp)
        {
            LogManager.Instance.Write("Parsing scripts for resource group " + grp.Name);

            // Count up the number of scripts we have to parse
            var scriptLoaderFileList = new List<Axiom.Math.Tuple<IScriptLoader, List<FileInfoList>>>();

            var scriptCount = 0;
            // Iterate over script users in loading order and get streams
            foreach (var pairsl in this._scriptLoaderOrders)
            {
                var sl = pairsl.Value;

                foreach (var isl in sl)
                {
                    var fileListList = new List<FileInfoList>();
                    // Get all the patterns and search them
                    var patterns = isl.ScriptPatterns;
                    foreach (var p in patterns)
                    {
                        var fileList = FindResourceFileInfo(grp.Name, p);
                        scriptCount += fileList.Count;
                        fileListList.Add(fileList);
                    }
                    scriptLoaderFileList.Add(new Axiom.Math.Tuple<IScriptLoader, List<FileInfoList>>(isl, fileListList));
                }
            }
            // Fire scripting event
            _fireResourceGroupScriptingStarted(grp.Name, scriptCount);

            // Iterate over scripts and parse
            // Note we respect original ordering
            foreach (var slfli in scriptLoaderFileList)
            {
                var su = slfli.First;
                // Iterate over each list
                foreach (var flli in slfli.Second)
                {
                    // Iterate over each item in the list
                    foreach (var fii in flli)
                    {
                        var skipScript = false;
                        _fireScriptStarted(fii.Basename, ref skipScript);
                        if (skipScript)
                        {
                            LogManager.Instance.Write("Skipping script " + fii.Basename);
                        }
                        else
                        {
                            var stream = fii.Archive.Open(fii.Basename);
                            if (stream != null)
                            {
                                LogManager.Instance.Write("Parsing script " + fii.Basename);
#warning TODO
                                //if ( mLoadingListener )
                                //    mLoadingListener->resourceStreamOpened( fii->filename, grp->name, 0, stream );
                                su.ParseScript(stream, grp.Name, fii.Filename);
                                stream.Close();
                            }
                        }
                        _fireScriptEnded(fii.Basename, skipScript);
                    }
                }
            }
            _fireResourceGroupScriptingEnded(grp.Name);
            LogManager.Instance.Write("Finished parsing scripts for resource group " + grp.Name);
        }

        /// <summary>Create all the pre-declared resources.</summary>
        /// <remarks>Called as part of initializeResourceGroup</remarks>
        private void _createDeclaredResources(ResourceGroup grp)
        {
            foreach (var dcl in grp.ResourceDeclarations)
            {
                // Retrieve the appropriate manager
                var mgr = _getResourceManager(dcl.ResourceType);
                // Create the resource
                var res = mgr.Create(dcl.ResourceName, grp.Name, dcl.Loader != null, dcl.Loader, dcl.Parameters);
                // Add resource to load list
                LoadUnloadResourceList loadList;
                if (grp.LoadResourceOrders.ContainsKey(mgr.LoadingOrder) != true)
                {
                    loadList = new LoadUnloadResourceList();
                    grp.LoadResourceOrders.Add(mgr.LoadingOrder, loadList);
                }
                else
                {
                    loadList = grp.LoadResourceOrders[mgr.LoadingOrder];
                }
                loadList.Add(res);
            }
        }

        /** Adds a created resource to a group. */

        private void _addCreatedResource(Resource res, ResourceGroup group)
        {
            var order = res.Creator.LoadingOrder;

            LoadUnloadResourceList loadList;
            if (!group.LoadResourceOrders.ContainsKey(order))
            {
                loadList = new LoadUnloadResourceList();
                group.LoadResourceOrders.Add(order, loadList);
            }
            else
            {
                loadList = group.LoadResourceOrders[order];
            }

            loadList.Add(res);
        }

        /// <summary>
        ///  Drops contents of a group, leave group there, notify ResourceManagers.
        /// </summary>
        /// <param name="grp"></param>
        private void _dropGroupContents(ResourceGroup grp)
        {
            var groupSet = false;
            if (this._currentGroup != null)
            {
                // Set current group to indicate ignoring of notifications
                this._currentGroup = grp;
                groupSet = true;
            }
            // delete all the load list entries
            foreach (var pair in grp.LoadResourceOrders)
            {
                var rl = pair.Value.ToArray(); // avoid modification while enumerating
                foreach (var res in rl)
                {
                    res.Creator.Remove(res); // will result in call to notifyResourceRemoved()
                }
            }
            grp.LoadResourceOrders.Clear();

            if (groupSet)
            {
                this._currentGroup = null;
            }
        }

        /// <summary>
        /// Delete a group for shutdown - don't notify ResourceManagers.
        /// </summary>
        /// <param name="grp"></param>
        private void _deleteGroup(ResourceGroup grp)
        {
            // delete all the load list entries
            foreach (var pair in grp.LoadResourceOrders)
            {
                // Don't iterate over resources to drop with ResourceManager
                // Assume this is being done anyway since this is a shutdown method
                var lurl = pair.Value;
                lurl.Clear();
            }

            // Drop location list
            grp.LocationList.Clear();

            // delete ResourceGroup
            grp.Dispose();
        }

        /// <summary>
        /// Internal method for getting a registered ResourceManager.
        /// </summary>
        /// <param name="resourceType">identifies the resource type.</param>
        /// <returns></returns>
        private ResourceManager _getResourceManager(string resourceType)
        {
            //OGRE_LOCK_AUTO_MUTEX
            if (!ResourceManagers.ContainsKey(resourceType))
            {
                throw new Exception("Cannot locate resource manager for resource type '" + resourceType + "'.");
            }
            return ResourceManagers[resourceType];
        }

        private ResourceGroup _findGroupContainingResourceImpl(string filename)
        {
            //OGRE_LOCK_AUTO_MUTEX

            // Iterate over resource groups and find
            foreach (var grp in this._resourceGroups.Values)
            {
                //OGRE_LOCK_MUTEX(grp->OGRE_AUTO_MUTEX_NAME) // lock group mutex

                if (ResourceExists(grp, filename))
                {
                    return grp;
                }
            }
            // Not found
            return null;
        }

        #endregion Private Methods

        #region ISingleton<ResourceGroupManager> implementation

        /// <summary>
        ///     Singleton instance of this class.
        /// </summary>
        protected static ResourceGroupManager instance = new Lazy<ResourceGroupManager>().Value;

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static ResourceGroupManager Instance
        {
            get
            {
                return instance;
            }
        }

        public virtual bool Initialize(params object[] args)
        {
            if (args.Length == 0)
            {
                // Create the 'General' group
                CreateResourceGroup(DefaultResourceGroupName);
                // Create the 'Internal' group
                CreateResourceGroup(InternalResourceGroupName);
                // Create the 'Bootstrap' group
                CreateResourceGroup(BootstrapResourceGroupName);
                // Create the 'AutoDetect' group
                CreateResourceGroup(AutoDetectResourceGroupName);
            }
            else
            {
                // TODO something with args?
            }
            return true;
        }

        #endregion ISingleton<ResourceGroupManager> implementation

        #region IDisposable Members

        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    // delete all resource groups
                    foreach (var pair in resourceGroups)
                    {
                        var rg = pair.Value;
                        _deleteGroup(rg);
                    }
                    resourceGroups.Clear();
                    this._currentGroup = null;

                    ResourceGroupManager.instance = null;
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose(disposeManagedResources);
        }

        #endregion IDisposable Members
    };
}