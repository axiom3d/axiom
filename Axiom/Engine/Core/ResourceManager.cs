#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using Axiom.Configuration;
using Axiom.FileSystem;
using Axiom.Patterns;

namespace Axiom.Core
{
	/// <summary>
	///		Defines a generic resource handler.
	/// </summary>
	/// <remarks>
	///		A resource manager is responsible for managing a pool of
	///		resources of a particular type. It must index them, look
	///		them up, load and destroy them. It may also need to stay within
	///		a defined memory budget, and temporaily unload some resources
	///		if it needs to to stay within this budget.
	///		<p/>
	///		Resource managers use a priority system to determine what can
	///		be unloaded, and a Least Recently Used (LRU) policy within
	///		resources of the same priority.
	/// </remarks>
	public abstract class ResourceManager : IDisposable
	{
		#region Member variables

		protected ulong memoryBudget;
		protected ulong memoryUsage;
		/// <summary>A cached list of all resources in memory.</summary>
		protected Hashtable resourceList									= new Hashtable();
		/// <summary>A lookup table used to find a common archive associated with a filename.</summary>
		protected Hashtable filePaths										= new Hashtable();
		/// <summary>A cached list of archives specific to a resource type.</summary>
		protected ArrayList archives											= new ArrayList();
		/// <summary>A lookup table used to find a archive associated with a filename.</summary>
		static protected Hashtable commonFilePaths				= new Hashtable();
		/// <summary>A cached list of archives common to all resource types.</summary>
		static protected ArrayList commonArchives				= new ArrayList();

		#endregion

		#region Constructors

		/// <summary>
		///		Default constructor
		/// </summary>
		public ResourceManager()
		{
			memoryBudget = Int64.MaxValue;
			memoryUsage = 0;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Sets a limit on the amount of memory this resource handler may use.	
		/// </summary>
		/// <remarks>
		///		If, when asked to load a new resource, the manager believes it will exceed this memory
		///		budget, it will temporarily unload a resource to make room for the new one. This unloading
		///		is not permanent and the Resource is not destroyed; it simply needs to be reloaded when
		///		next used.
		/// </remarks>
		public ulong MemoryBudget
		{
			//get { return memoryBudget; }
			set 
			{ 
				memoryBudget = value;

				CheckUsage();
			}
		}

		/// <summary>
		///		Gets/Sets the current memory usages by all resource managers.
		/// </summary>
		public ulong MemoryUsage
		{
			get { return memoryUsage; }
			set { memoryUsage = value; }
		}

		#endregion

		#region Virtual/Abstract methods

		/// <summary>
		///		Loads a resource.  Resource will be subclasses of Resource.
		/// </summary>
		/// <param name="resource">Resource to load.</param>
		/// <param name="priority"></param>
		virtual public void Load(Resource resource, int priority)
		{
			// load and touch the resource
			resource.Load();
			resource.Touch();

			// cache the resource
			resourceList.Add(resource.Name, resource);
		}

		/// <summary>
		///		Unloads a Resource from the managed resources list, calling it's Unload() method.
		/// </summary>
		/// <remarks>
		///		This method removes a resource from the list maintained by this manager, and unloads it from
		///		memory. It does NOT destroy the resource itself, although the memory used by it will be largely
		///		freed up. This would allow you to reload the resource again if you wished. 
		/// </remarks>
		/// <param name="resource"></param>
		virtual public void Unload(Resource resource)
		{
			// unload the resource
			resource.Unload();

			// remove the resource 
			resourceList.Remove(resource.Name);

			// update memory usage
			memoryUsage -= resource.Size;
		}

		/// <summary>
		///		
		/// </summary>
		virtual public void UnloadAndDestroyAll()
		{
			foreach(Resource resource in resourceList.Values)
			{
				// unload and dispose of resource
				resource.Unload();
				resource.Dispose();
			}

			// empty the resource list
			resourceList.Clear();
			filePaths.Clear();
			commonArchives.Clear();
			commonFilePaths.Clear();
			archives.Clear();
		}

		#endregion
		
		#region Public methods

		/// <summary>
		///		Adds a relative path to search for resources of this type.
		/// </summary>
		/// <remarks>
		///		This method adds the supplied path to the list of relative locations that that will be searched for
		///		a single type of resource only. Each subclass of ResourceManager will maintain it's own list of
		///		specific subpaths, which it will append to the current path as it searches for matching files.
		/// </remarks>
		/// <param name="path"></param>
		public void AddSearchPath(String path)
		{
			AddArchive(path, "Folder");
		}

		/// <summary>
		///		Adds a relative search path for resources of ALL types.
		/// </summary>
		/// <remarks>
		///		This method has the same effect as ResourceManager.AddSearchPath, except that the path added
		///		applies to ALL resources, not just the one managed by the subclass in question.
		/// </remarks>
		/// <param name="path"></param>
		static public void AddCommonSearchPath(String path)
		{
			// record the common file path
			AddCommonArchive(path, "Folder");
		}

		static public StringCollection GetAllCommonNamesLike(string startPath, string extension)
		{
			StringCollection allFiles = new StringCollection();

			for(int i = 0; i < commonArchives.Count; i++)
			{
				Archive archive = (Archive)commonArchives[i];
				string[] files = archive.GetFileNamesLike(startPath, extension);

				// add each one to the final list
				foreach(String fileName in files)
				{
					allFiles.Add(fileName);
				}
			}

			return allFiles;
		}

		/// <summary>
		///		Adds an archive to 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		public void AddArchive(string name, string type)
		{
			Archive archive = null;

			switch(type)
			{
				case "Folder":
					archive = new Folder(name);
					break;
				// TODO: Implement this
				case "ZipFile":
					archive = new Zip(name);
					break;
				default: 
					throw new Axiom.Exceptions.AxiomException(String.Format("Archive type {0} is not a valid archive type.", type));
			}

			// get a list of all files in the archive
			string[] files = archive.GetFileNamesLike("", "");

			// add a lookup for all these files so they know what archive they are in
			for(int i = 0; i < files.Length; i++)
			{
				// HACK: Ignore .scc (SoureSafe) files, remove when put into CVS
				if(!files[i].EndsWith(".scc"))
					filePaths.Add(files[i], archive);
			}

			// add the archive to the common archives
			archives.Add(archive);
		}

		/// <summary>
		///		Adds an archive to 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		static public void AddCommonArchive(string name, string type)
		{
			Archive archive = null;

			switch(type)
			{
				case "Folder":
					archive = new Folder(name);
					break;
					// TODO: Implement this
				case "ZipFile":
					archive = new Zip(name);
					break;
				default: 
					throw new Axiom.Exceptions.AxiomException(String.Format("Archive type {0} is not a valid archive type.", type));
			}

			// get a list of all files in the archive
			string[] files = archive.GetFileNamesLike("", "");

			// add a lookup for all these files so they know what archive they are in
			for(int i = 0; i < files.Length; i++)
			{
				// HACK: Ignore .scc (SoureSafe) files, remove when put into CVS
				if(!files[i].EndsWith(".scc"))
					commonFilePaths.Add(files[i], archive);
			}

			// add the archive to the common archives
			commonArchives.Add(archive);
		}

		#endregion

		#region Protected methods

		/// <summary>
		///		Makes sure we are still within budget.
		/// </summary>
		protected void CheckUsage()
		{
			// TODO: Implementation of CheckUsage.
			// Keep a sorted list of resource by LastAccessed for easy removal of oldest?
		}

		/// <summary>
		///		Locates resource data within the archives known to the ResourceManager.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public Stream FindResourceData(string fileName)
		{
			// look in local file cache first
			if(filePaths.ContainsKey(fileName))
			{
				Archive archive = (Archive)filePaths[fileName];
				return archive.ReadFile(fileName);
			}

			// search common file cache			
			if(commonFilePaths.ContainsKey(fileName))
			{
				Archive archive = (Archive)commonFilePaths[fileName];
				return archive.ReadFile(fileName);
			}

			// not found in the cache, load the resource manually
			
			// TODO: Load resources manually
			throw new Axiom.Exceptions.AxiomException(String.Format("Resource '{0}' could not be found.  Be sure it is located in a known directory.", fileName));

			//return null;
		}

		/// <summary>
		///		Locates resource data within the archives known to the ResourceManager.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		static public Stream FindCommonResourceData(string fileName)
		{
			// search common file cache			
			if(commonFilePaths.ContainsKey(fileName))
			{
				Archive archive = (Archive)commonFilePaths[fileName];
				return archive.ReadFile(fileName);
			}

			// not found in the cache, load the resource manually
			
			// TODO: Load resources manually
			throw new Axiom.Exceptions.AxiomException(String.Format("Resource '{0}' could not be found.  Be sure it is located in a known directory.", fileName));

			//return null;
		}

		#endregion

		#region Operator overloads

		/// <summary>
		///		
		/// </summary>
		public virtual Resource this[string name]
		{
			get
			{
				Debug.Assert(resourceList != null, "A resource was being retreived, but the list of Resources is null.", "");

				// find the resource in the Hashtable and return it
				if(resourceList.ContainsKey(name))
					return (Resource)resourceList[name];
				else
					return null;
			}
		}

		#endregion

		/// <summary>
		///		Creates a new blank resource, compatible with this manager.
		/// </summary>
		/// <remarks>
		///		Resource managers handle disparate types of resources. This method returns a pointer to a
		///		valid new instance of the kind of resource managed here. The caller should  complete the
		///		details of the returned resource and call ResourceManager.Load to load the resource. Note
		///		that it is the CALLERS responsibility to destroy this object when it is no longer required
		///		(after calling ResourceManager.Unload if it had been loaded).
		/// </remarks>
		/// <param name="name"></param>
		/// <returns></returns>
		abstract public Resource Create(string name);

		#region Implementation of IDisposable

		public void Dispose()
		{
			// unload and destroy all resources
			UnloadAndDestroyAll();
		}

		#endregion
	}
}
