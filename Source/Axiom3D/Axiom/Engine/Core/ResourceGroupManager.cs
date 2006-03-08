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
using System.IO;
#endregion Namespace Declarations

namespace Axiom
{

    /// <summary>
    /// This abstract class defines an interface which is called back during
    /// resource group loading to indicate the progress of the load. 
    /// </summary>
    /// <remarks>
    /// Resource group loading is in 2 phases - creating resources from 
    /// declarations (which includes parsing scripts), and loading
    /// resources. Note that you don't necessarily have to have both; it
    /// is quite possible to just parse all the scripts for a group (see
    /// ResourceGroupManager::initialiseResourceGroup, but not to 
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
    public abstract class ResourceGroupListener
    {
        /// <summary>
        /// This event is fired when a resource group begins parsing scripts.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <param name="scriptCount">The number of scripts which will be parsed</param>
		public abstract void resourceGroupScriptingStarted( string groupName, int scriptCount);

        /// <summary>
        /// This event is fired when a script is about to be parsed.
        /// </summary>
        /// <param name="scriptName">Name of the to be parsed</param>
        public abstract void scriptParseStarted( string scriptName );

		/// <summary>
        /// This event is fired when the script has been fully parsed.
        /// </summary>
        public abstract void scriptParseEnded();

		/// <summary>
		/// This event is fired when a resource group finished parsing scripts.
		/// </summary>
		/// <param name="groupName">The name of the group</param>
        public abstract void resourceGroupScriptingEnded( string groupName );

		/// <summary>
		/// This event is fired  when a resource group begins loading.
		/// </summary>
		/// <param name="groupName">The name of the group being loaded</param>
		/// <param name="resourceCount">
        /// The number of resources which will be loaded, 
        /// including a number of stages required to load any linked world geometry
        /// </param>
        public abstract void resourceGroupLoadStarted( string groupName, int resourceCount );

        /// <summary>
        /// This event is fired when a declared resource is about to be loaded. 
        /// </summary>
        /// <param name="resource">Weak reference to the resource loaded</param>
        public abstract void resourceLoadStarted( Resource resource );

        /// <summary>
        /// This event is fired when the resource has been loaded. 
        /// </summary>
        public abstract void resourceLoadEnded();

        /// <summary>
        /// This event is fired when a stage of loading linked world geometry 
        /// is about to start. The number of stages required will have been 
        /// included in the resourceCount passed in resourceGroupLoadStarted.
        /// </summary>
        /// <param name="description">Text description of what was just loaded</param>
        public abstract void worldGeometryStageStarted( string description );

        /// <summary>
        /// This event is fired when a stage of loading linked world geometry 
        /// has been completed. The number of stages required will have been 
        /// included in the resourceCount passed in resourceGroupLoadStarted.
        /// </summary>
        /// <param name="description">Text description of what was just loaded</param>
        public abstract void worldGeometryStageEnded();

		/// <summary>
        /// This event is fired when a resource group finished loading.
        /// </summary>
        public abstract void resourceGroupLoadEnded( string groupName );

    }
    /** This singleton class manages the list of resource groups, and notifying
        the various resource managers of their obligations to load / unload
        resources in a group. It also provides facilities to monitor resource
        loading per group (to do progress bars etc), provided the resources 
        that are required are pre-registered.
    @par
        Defining new resource groups,  and declaring the resources you intend to
        use in advance is optional, however it is a very useful feature. In addition, 
		if a ResourceManager supports the definition of resources through scripts, 
		then this is the class which drives the locating of the scripts and telling
		the ResourceManager to parse them. 
	@par
		There are several states that a resource can be in (the concept, not the
		object instance in this case):
		<ol>
		<li><b>Undefined</b>. Nobody knows about this resource yet. It might be
		in the filesystem, but Ogre is oblivious to it at the moment - there 
		is no Resource instance. This might be because it's never been declared
		(either in a script, or using ResourceGroupManager::declareResource), or
		it may have previously been a valid Resource instance but has been 
		removed, either individually through ResourceManager::remove or as a group
		through ResourceGroupManager::clearResourceGroup.</li>
		<li><b>Declared</b>. Ogre has some forewarning of this resource, either
		through calling ResourceGroupManager::declareResource, or by declaring
		the resource in a script file which is on one of the resource locations
		which has been defined for a group. There is still no instance of Resource,
		but Ogre will know to create this resource when 
		ResourceGroupManager::initialiseResourceGroup is called (which is automatic
		if you declare the resource group before Root::initialise).</li>
		<li><b>Unloaded</b>. There is now a Resource instance for this resource, 
		although it is not loaded. This means that code which looks for this
		named resource will find it, but the Resource is not using a lot of memory
		because it is in an unloaded state. A Resource can get into this state
		by having just been created by ResourceGroupManager::initialiseResourceGroup 
		(either from a script, or from a call to declareResource), by 
		being created directly from code (ResourceManager::create), or it may 
		have previously been loaded and has been unloaded, either individually
		through Resource::unload, or as a group through ResourceGroupManager::unloadResourceGroup.</li>
		<li><b>Loaded</b>The Resource instance is fully loaded. This may have
		happened implicitly because something used it, or it may have been 
		loaded as part of a group.</li>
		</ol>
		@see ResourceGroupManager::declareResource
		@see ResourceGroupManager::initialiseResourceGroup
		@see ResourceGroupManager::loadResourceGroup
		@see ResourceGroupManager::unloadResourceGroup
		@see ResourceGroupManager::clearResourceGroup
    */
    public class ResourceGroupManager 
    {
		//OGRE_AUTO_MUTEX // public to allow external locking
		/// Default resource group name
		public static string DefaultResourceGroupName;
		/// Bootstrap resource group name (min OGRE resources)
		public static string BOOTSTRAP_RESOURCE_GROUP_NAME;
        /// Nested struct defining a resource declaration
        public struct ResourceDeclaration
        {
            string resourceName;
            string resourceType;
			NameValuePairList parameters;
        };
        /// List of resource declarations
        //         typedef std::list<ResourceDeclaration> ResourceDeclarationList;
        public class ResourceDeclarationList : ArrayList {};

        /// <summary>Map of resource types (strings) to ResourceManagers, used to notify them to load / unload group contents</summary>
		//          typedef std::map<String, ResourceManager*> ResourceManagerMap;
        protected class ResourceManagerMap : Map {};
        ResourceManagerMap mResourceManagerMap;

		/// <summary>Map of loading order (Real) to ScriptLoader, used to order script parsing</summary>
		//          typedef std::multimap<Real, ScriptLoader*> ScriptLoaderOrderMap;
        protected class ScriptLoaderOrderMap : Map {};
		ScriptLoaderOrderMap mScriptLoaderOrderMap;

        /// <summary></summary>
		//          typedef std::vector<ResourceGroupListener*> ResourceGroupListenerList;
        protected class ResourceGroupListenerList : ArrayList {};
        ResourceGroupListenerList mResourceGroupListenerList;

        /// <summary>Resource index entry, resourcename->location </summary>
        //          typedef std::map<String, Archive*> ResourceLocationIndex;
        protected class ResourceLocationIndex : Map {};

		/// <summary>Resource location entry</summary>
		struct ResourceLocation
		{
			/// <summary>Pointer to the archive which is the destination</summary>
			Archive archive;
			/// Whether this location was added recursively
			bool recursive;
		};

		/// <summary>List of possible file locations</summary>
		//          typedef std::list<ResourceLocation*> LocationList;
        protected class LocationList : ArrayList {};
		/// <summary>List of resources which can be loaded / unloaded </summary>
		//          typedef std::list<ResourcePtr> LoadUnloadResourceList;
        protected class LoadUnLoadResourceList : ArrayList {};

		/// Resource group entry
		struct ResourceGroup
		{
			//OGRE_AUTO_MUTEX
			/// <summary>Group name </summary>
			string name;
			/// <summary>Whether group has been initialised </summary>
			bool initialised;
			/// <summary>List of possible locations to search </summary>
			LocationList locationList;
			/// <summary>Index of resource names to locations, built for speedy access (case sensitive archives) </summary>
			ResourceLocationIndex resourceIndexCaseSensitive;
            /// <summary>Index of resource names to locations, built for speedy access (case insensitive archives) </summary>
            ResourceLocationIndex resourceIndexCaseInsensitive;
			/// <summary>Pre-declared resources, ready to be created </summary>
            ResourceDeclarationList resourceDeclarations;
			/// <summary>
            /// Created resources which are ready to be loaded / unloaded
			/// Group by loading order of the type (defined by ResourceManager)
			/// (e.g. skeletons and materials before meshes) 
            /// </summary>
			//          typedef std::map<Real, LoadUnloadResourceList*> LoadResourceOrderMap;
            class LoadResourceOrderMap : Map {};

			LoadResourceOrderMap loadResourceOrderMap;
            /// <summary>Linked world geometry, as passed to setWorldGeometry </summary>
            string worldGeometry;
            /// <summary>Scene manager to use with linked world geometry </summary>
            SceneManager worldGeometrySceneManager;
		};
        /// <summary>Map from resource group names to groups </summary>
        //          typedef std::map<String, ResourceGroup*> ResourceGroupMap;
        protected class ResourceGroupMap : Map {}
        ResourceGroupMap mResourceGroupMap;

        /// <summary>Group name for world resources </summary>
        String mWorldGroupName;

		/// <summary>
        /// Parses all the available scripts found in the resource locations
		/// for the given group, for all ResourceManagers. 
        /// </summary>
		/// <remarks>
		///	Called as part of initialiseResourceGroup
		/// </remarks>
        void parseResourceGroupScripts( ResourceGroup grp )
        {
        }

		/// <summary>Create all the pre-declared resources.</summary>
		/// <remarks>Called as part of initialiseResourceGroup</remarks>
        void createDeclaredResources( ResourceGroup grp )
        {
        }

		/** Adds a created resource to a group. */
        void addCreatedResource( Resource res, ResourceGroup group )
        {
        }

		/** Get resource group */
        ResourceGroup getResourceGroup( string name )
        {
            return new ResourceGroup();
        }

		/** Drops contents of a group, leave group there, notify ResourceManagers. */
        void dropGroupContents( ResourceGroup grp )
        {
        }

		/** Delete a group for shutdown - don't notify ResourceManagers. */
        void deleteGroup( ResourceGroup grp )
        {
        }

		/// <summary>Internal event firing method </summary>
        void fireResourceGroupScriptingStarted( string groupName, int scriptCount )
        {
        }

		/// <summary>Internal event firing method </summary>
        void fireScriptStarted( string scriptName )
        {
        }

        /// Internal event firing method
        void fireScriptEnded()
        {
        }

		/// Internal event firing method
        void fireResourceGroupScriptingEnded( string groupName )
        {
        }

		/// Internal event firing method
        void fireResourceGroupLoadStarted( string groupName, int resourceCount )
        {
        }

        /// Internal event firing method
        void fireResourceStarted( Resource resource )
        {
        }

		/// Internal event firing method
        void fireResourceEnded()
        {
        }

		/// Internal event firing method
        void fireResourceGroupLoadEnded( string groupName )
        {
        }




		/// Stored current group - optimisation for when bulk loading a group
		ResourceGroup mCurrentGroup;

        #region Singleton implementation

        /// <summary>
        ///     Singleton instance of this class.
        /// </summary>
        private static ResourceGroupManager instance;

        /// <summary>
        ///     Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        internal ResourceGroupManager()
        {

        }

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static ResourceGroupManager Instance
        {
            get
            {
                if ( instance == null )
                {
                    instance = new ResourceGroupManager();
                }
                return instance;
            }
        }

        #endregion Singleton implementation

        /** Create a resource group.
        @remarks
            A resource group allows you to define a set of resources that can 
            be loaded / unloaded as a unit. For example, it might be all the 
            resources used for the level of a game. There is always one predefined
            resource group called ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME, 
			which is typically used to hold all resources which do not need to 
			be unloaded until shutdown. You can create additional ones so that 
			you can control the life of your resources in whichever way you wish.
        @par
            Once you have defined a resource group, resources which will be loaded
			as part of it are defined in one of 3 ways:
			<ol>
			<li>Manually through declareResource(); this is useful for scripted
				declarations since it is entirely generalised, and does not 
				create Resource instances right away</li>
			<li>Through the use of scripts; some ResourceManager subtypes have
				script formats (e.g. .material, .overlay) which can be used
				to declare resources</li>
			<li>By calling ResourceManager::create to create a resource manually.
			This resource will go on the list for it's group and will be loaded
			and unloaded with that group</li>
			</ol>
			You must remember to call initialiseResourceGroup if you intend to use
			the first 2 types.
        @param name The name to give the resource group.
        */
        void createResourceGroup( string name )
        {
        }


        /** Initialises a resource group.
		@remarks
			After creating a resource group, adding some resource locations, and
			perhaps pre-declaring some resources using declareResource(), but 
			before you need to use the resources in the group, you 
			should call this method to initialise the group. By calling this,
			you are triggering the following processes:
			<ol>
			<li>Scripts for all resource types which support scripting are
				parsed from the resource locations, and resources within them are
				created (but not loaded yet).</li>
			<li>Creates all the resources which have just pre-declared using
			declareResource (again, these are not loaded yet)</li>
			</ol>
			So what this essentially does is create a bunch of unloaded Resource entries
			in the respective ResourceManagers based on scripts, and resources
			you've pre-declared. That means that code looking for these resources
			will find them, but they won't be taking up much memory yet, until
			they are either used, or they are loaded in bulk using loadResourceGroup.
			Loading the resource group in bulk is entirely optional, but has the 
			advantage of coming with progress reporting as resources are loaded.
		@par
			Failure to call this method means that loadResourceGroup will do 
			nothing, and any resources you define in scripts will not be found.
			Similarly, once you have called this method you won't be able to
			pick up any new scripts or pre-declared resources, unless you
			call clearResourceGroup, set up declared resources, and call this
			method again.
		@note 
			When you call Root::initialise, all resource groups that have already been
			created are automatically initialised too. Therefore you do not need to 
			call this method for groups you define and set up before you call 
			Root::initialise. However, since one of the most useful features of 
			resource groups is to set them up after the main system initialisation
			has occurred (e.g. a group per game level), you must remember to call this
			method for the groups you create after this.

		@param name The name of the resource group to initialise
		*/
        void initialiseResourceGroup( string name )
        {
        }

		/** Initialise all resource groups which are yet to be initialised.
		@see ResourceGroupManager::intialiseResourceGroup
		*/
        void initialiseAllResourceGroups()
        {
        }

		/** Loads a resource group.
        @remarks
			Loads any created resources which are part of the named group.
			Note that resources must have already been created by calling
			ResourceManager::create, or declared using declareResource() or
			in a script (such as .material and .overlay). The latter requires
			that initialiseResourceGroup has been called. 
		
			When this method is called, this class will callback any ResourceGroupListeners
			which have been registered to update them on progress. 
        @param name The name to of the resource group to load.
		@param loadMainResources If true, loads normal resources associated 
			with the group (you might want to set this to false if you wanted
			to just load world geometry in bulk)
		@param loadWorldGeom If true, loads any linked world geometry
			@see ResourceGroupManager::linkWorldGeometryToResourceGroup
        */
        void loadResourceGroup( string name )
        {
            loadResourceGroup( name, true, true );
        }
        void loadResourceGroup(string name, bool loadMainResources, bool loadWorldGeom)
        {
        }

        /** Unloads a resource group.
        @remarks
            This method unloads all the resources that have been declared as
            being part of the named resource group. Note that these resources
            will still exist in their respective ResourceManager classes, but
            will be in an unloaded state. If you want to remove them entirely,
            you should use clearResourceGroup or destroyResourceGroup.
        @param name The name to of the resource group to unload.
        */
        void unloadResourceGroup( string name )
        {
        }

		/** Clears a resource group. 
		@remarks
			This method unloads all resources in the group, but in addition it
			removes all those resources from their ResourceManagers, and then 
			clears all the members from the list. That means after calling this
			method, there are no resources declared as part of the named group
			any more. Resource locations still persist though.
        @param name The name to of the resource group to clear.
		*/
        void clearResourceGroup( string name )
        {
        }
        
        /** Destroys a resource group, clearing it first, destroying the resources
            which are part of it, and then removing it from
            the list of resource groups. 
        @param name The name of the resource group to destroy.
        */
        void destroyResourceGroup( string name )
        {
        }


        /** Method to add a resource location to for a given resource group. 
        @remarks
            Resource locations are places which are searched to load resource files.
            When you choose to load a file, or to search for valid files to load, 
            the resource locations are used.
        @param name The name of the resource location; probably a directory, zip file, URL etc.
        @param locType The codename for the resource type, which must correspond to the 
            Archive factory which is providing the implementation.
        @param resGroup The name of the resource group for which this location is
            to apply. ResourceGroupManager::DEFAULT_RESOURCE_GROUP_NAME is the 
			default group which always exists, and can
            be used for resources which are unlikely to be unloaded until application
            shutdown. Otherwise it must be the name of a group; if it
            has not already been created with createResourceGroup then it is created
            automatically.
        @param recursive Whether subdirectories will be searched for files when using 
			a pattern match (such as *.material), and whether subdirectories will be
			indexed. This can slow down initial loading of the archive and searches.
			When opening a resource you still need to use the fully qualified name, 
			this allows duplicate names in alternate paths.
        */
        void addResourceLocation(string name, string locType) { addResourceLocation( name, locType, DefaultResourceGroupName, false ); }
        void addResourceLocation(string name, string locType, string resGroup ) { addResourceLocation( name, locType, resGroup, false ); }
        void addResourceLocation(string name, string locType, bool recursive) { addResourceLocation( name, locType, DefaultResourceGroupName, recursive ); }
        void addResourceLocation(string name, string locType, string resGroup, bool recursive)
        {
        }

        /** Removes a resource location from the search path. */ 
        void removeResourceLocation(string name) { removeResourceLocation( name, DefaultResourceGroupName); }
        void removeResourceLocation(string name, string resGroup)
        {
        }

        /** Declares a resource to be a part of a resource group, allowing you 
            to load and unload it as part of the group.
        @remarks
            By declaring resources before you attempt to use them, you can 
            more easily control the loading and unloading of those resources
            by their group. Declaring them also allows them to be enumerated, 
            which means events can be raised to indicate the loading progress
            (@see ResourceGroupListener). Note that another way of declaring
			resources is to use a script specific to the resource type, if
			available (e.g. .material).
		@par
			Declared resources are not created as Resource instances (and thus
			are not available through their ResourceManager) until initialiseResourceGroup
			is called, at which point all declared resources will become created 
			(but unloaded) Resource instances, along with any resources declared
			in scripts in resource locations associated with the group.
        @param name The resource name. 
        @param resourceType The type of the resource. Ogre comes preconfigured with 
            a number of resource types: 
            <ul>
            <li>Font</li>
            <li>Material</li>
            <li>Mesh</li>
            <li>Overlay</li>
            <li>Skeleton</li>
            </ul>
            .. but more can be added by plugin ResourceManager classes.
        @param groupName The name of the group to which it will belong.
		@param loadParameters A list of name / value pairs which supply custom
			parameters to the resource which will be required before it can 
			be loaded. These are specific to the resource type.
        */
        void declareResource(string name, string resourceType) { declareResource( name, resourceType, DefaultResourceGroupName, new NameValuePairList() ); }
        void declareResource( string name, string resourceType, string groupName ) { declareResource( name, resourceType, groupName, new NameValuePairList() ); }
        void declareResource( string name, string resourceType, NameValuePairList loadParameters ) { declareResource( name, resourceType, DefaultResourceGroupName, loadParameters ); }
        void declareResource( string name, string resourceType, string groupName, NameValuePairList loadParameters )
        {
        }

        /** Undeclare a resource.
		@remarks
			Note that this will not cause it to be unloaded
            if it is already loaded, nor will it destroy a resource which has 
			already been created if initialiseResourceGroup has been called already.
			Only unloadResourceGroup / clearResourceGroup / destroyResourceGroup 
			will do that. 
        @param name The name of the resource. 
		@param groupName The name of the group this resource was declared in. 
        */
        void undeclareResource( string name, string groupName )
        {
        }

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
        Stream openResource( string resourceName )
        {
            return openResource( resourceName, DefaultResourceGroupName );
        }
        Stream openResource( string resourceName, string groupName )
        {
            return null;
        }

		/** Open all resources matching a given pattern (which can contain
			the character '*' as a wildcard), and return a collection of 
			DataStream objects on them.
		@param pattern The pattern to look for. If resource locations have been
			added recursively, subdirectories will be searched too so this
			does not need to be fully qualified.
		@param groupName The resource group; this determines which locations
			are searched.
		@returns Shared pointer to a data stream list , will be
			destroyed automatically when no longer referenced
		*/
        public class StreamList : ArrayList {}
        StreamList openResources( string pattern )
        {
            return openResources( pattern, DefaultResourceGroupName );
        }
        StreamList openResources( string pattern, string groupName )
        {
            return null;
        }
		
        /** List all file names in a resource group.
        @note
        This method only returns filenames, you can also retrieve other
        information using listFileInfo.
        @param groupName The name of the group
        @returns A list of filenames matching the criteria, all are fully qualified
        */
        string[] listResourceNames( string groupName )
        {
            return null;
        }

        /** List all files in a resource group with accompanying information.
        @param groupName The name of the group
        @returns A list of structures detailing quite a lot of information about
        all the files in the archive.
        */
        protected class FileInfoList : ArrayList {}
        FileInfoList listResourceFileInfo( string groupName )
        {
            return null;
        }

        /** Find all file names matching a given pattern in a resource group.
        @note
        This method only returns filenames, you can also retrieve other
        information using findFileInfo.
        @param groupName The name of the group
        @param pattern The pattern to search for; wildcards (*) are allowed
        @returns A list of filenames matching the criteria, all are fully qualified
        */
        string[] findResourceNames( string groupName, string pattern )
        {
            return null;
        }

        /** Find out if the named file exists in a group. 
        @param group The name of the resource group
        @param filename Fully qualified name of the file to test for
        */
        bool resourceExists( string group, string filename )
        {
            return false;
        }

        /** Find all files matching a given pattern in a group and get 
        some detailed information about them.
        @param group The name of the resource group
        @param pattern The pattern to search for; wildcards (*) are allowed
        @returns A list of file information structures for all files matching 
        the criteria.
        */
        FileInfoList findResourceFileInfo( string group, string pattern )
        {
            return null;
        }

        
        /** Adds a ResourceGroupListener which will be called back during 
            resource loading events. 
        */
        void addResourceGroupListener( ResourceGroupListener l )
        {
        }
        /** Removes a ResourceGroupListener */
        void removeResourceGroupListener( ResourceGroupListener l )
        {
        }

        /** Sets the resource group that 'world' resources will use.
        @remarks
            This is the group which should be used by SceneManagers implementing
            world geometry when looking for their resources. Defaults to the 
            DEFAULT_RESOURCE_GROUP_NAME but this can be altered.
        */
        void setWorldResourceGroupName( string groupName )
        {
            mWorldGroupName = groupName;
        }

        /// Sets the resource group that 'world' resources will use.
        string getWorldResourceGroupName()
        {
            return mWorldGroupName;
        }

        /** Associates some world geometry with a resource group, causing it to 
            be loaded / unloaded with the resource group.
        @remarks
            You would use this method to essentially defer a call to 
            SceneManager::setWorldGeometry to the time when the resource group
            is loaded. The advantage of this is that compatible scene managers 
            will include the estimate of the number of loading stages for that
            world geometry when the resource group begins loading, allowing you
            to include that in a loading progress report. 
        @param group The name of the resource group
        @param worldGeometry The parameter which should be passed to setWorldGeometry
        @param sceneManager The SceneManager which should be called
        */
        void linkWorldGeometryToResourceGroup(string group, string worldGeometry, SceneManager sceneManager )
        {
        }

        /** Clear any link to world geometry from a resource group.
        @remarks
            Basically undoes a previous call to linkWorldGeometryToResourceGroup.
        */
        void unlinkWorldGeometryFromResourceGroup( string group )
        {
        }

        /** Shutdown all ResourceManagers, performed as part of clean-up. */
        void shutdownAll()
        {
        }


        /** Internal method for registering a ResourceManager (which should be
            a singleton). Creators of plugins can register new ResourceManagers
            this way if they wish.
		@remarks
			ResourceManagers that wish to parse scripts must also call 
			_registerScriptLoader.
        @param resourceType String identifying the resource type, must be unique.
        @param rm Pointer to the ResourceManager instance.
        */
        void _registerResourceManager( string resourceType, ResourceManager rm )
        {
        }

        /** Internal method for unregistering a ResourceManager.
		@remarks
			ResourceManagers that wish to parse scripts must also call 
			_unregisterScriptLoader.
        @param resourceType String identifying the resource type.
        */
        void _unregisterResourceManager( string resourceType )
        {
        }


        /** Internal method for registering a ScriptLoader.
		@remarks ScriptLoaders parse scripts when resource groups are initialised.
        @param su Pointer to the ScriptLoader instance.
        */
        public class ScriptLoader {}
        public void RegisterScriptLoader(object su)
        {
        }

        /** Internal method for unregistering a ScriptLoader.
        @param su Pointer to the ScriptLoader instance.
        */
        public void UnregisterScriptLoader( object su )
        {
        }

		/** Internal method for getting a registered ResourceManager.
		@param resourceType String identifying the resource type.
		*/
        ResourceManager _getResourceManager( string resourceType )
        {
            return null;
        }

		/** Internal method called by ResourceManager when a resource is created.
		@param res Weak reference to resource
		*/
        void _notifyResourceCreated( Resource res )
        {
        }

		/** Internal method called by ResourceManager when a resource is removed.
		@param res Weak reference to resource
		*/
        void _notifyResourceRemoved( Resource res )
        {
        }

		/** Internal method called by ResourceManager when all resources 
			for that manager are removed.
		@param manager Pointer to the manager for which all resources are being removed
		*/
        void _notifyAllResourcesRemoved( ResourceManager manager )
        {
        }

        /** Notify this manager that one stage of world geometry loading has been 
            started.
        @remarks
            Custom SceneManagers which load custom world geometry should call this 
            method the number of times equal to the value they return from 
            SceneManager::estimateWorldGeometry while loading their geometry.
        */
        void _notifyWorldGeometryStageStarted( string description )
        {
        }
        /** Notify this manager that one stage of world geometry loading has been 
            completed.
        @remarks
            Custom SceneManagers which load custom world geometry should call this 
            method the number of times equal to the value they return from 
            SceneManager::estimateWorldGeometry while loading their geometry.
        */
        void _notifyWorldGeometryStageEnded()
        {
        }

		/** Get a list of the currently defined resource groups. 
		@note This method intentionally returns a copy rather than a reference in
			order to avoid any contention issues in multithreaded applications.
		@returns A copy of list of currently defined groups.
		*/
        string[] getResourceGroups()
        {
            return null;
        }
		/** Get the list of resource declarations for the specified group name. 
		@note This method intentionally returns a copy rather than a reference in
			order to avoid any contention issues in multithreaded applications.
		@param groupName The name of the group
		@returns A copy of list of currently defined resources.
		*/
        ResourceDeclarationList getResourceDeclarationList( string groupName )
        {
            return null;
        }

    };}
