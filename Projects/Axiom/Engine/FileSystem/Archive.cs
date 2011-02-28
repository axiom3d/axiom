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
using System.Collections.Generic;
using System.IO;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.FileSystem
{
	#region FileInfo Class and Collection

	/// <summary>Information about a file/directory within the archive will be returned using a FileInfo struct.</summary>
	/// <see cref="Archive"/>
	/// <ogre name="FileInfo">
	///     <file name="OgreArchive.h"   revision="1.7" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	/// </ogre> 
	public struct FileInfo
	{
		/// The archive in which the file has been found (for info when performing
		/// multi-Archive searches, note you should still open through ResourceGroupManager)
		public Archive Archive;
		/// The file's fully qualified name
		public String Filename;
		/// Path name; separated by '/' and ending with '/'
		public String Path;
		/// Base filename
		public String Basename;
		/// Compressed size
		public long CompressedSize;
		/// Uncompressed size
		public long UncompressedSize;
        /// Last modification time
        public DateTime ModifiedTime;
	};

	/// <ogre name="FileInfoList">
	///     <file name="OgreArchive.h"   revision="1.7" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	/// </ogre> 
	public class FileInfoList : List<FileInfo>
	{
	}

	#endregion FileInfo Class and Collection

	#region Archive Abstract Class and Factory

	/// <summary>Archive-handling class.</summary>
	/// <remarks>
	/// An archive is a generic term for a container of files. This may be a
	/// filesystem folder, it may be a compressed archive, it may even be 
	/// a remote location shared on the web. This class is designed to be 
	/// subclassed to provide access to a range of file locations. 
	/// <para/>
	/// Instances of this class are never constructed or even handled by end-user
	/// applications. They are constructed by custom ArchiveFactory classes, 
	/// which plugins can register new instances of using ArchiveManager. 
	/// End-user applications will typically use ResourceManager or 
	/// ResourceGroupManager to manage resources at a higher level, rather than 
	/// reading files directly through this class. Doing it this way allows you
	/// to benefit from OGRE's automatic searching of multiple file locations 
	/// for the resources you are looking for.
	/// </remarks>
	/// <ogre name="FileInfo">
	///     <file name="OgreArchive.h"   revision="1.7" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	/// </ogre> 
	public abstract class Archive : DisposableObject
	{
		#region Fields and Properties

		#region Name Property

		private string _name;
		/// Archive name
		public string Name
		{
			get
			{
				return _name;
			}
			protected set
			{
				_name = value;
			}
		}

		#endregion Name Property

		#region Type Property

		private string _type;
		/// Archive type code
		public string Type
		{
			get
			{
				return _type;
			}
			protected set
			{
				_type = value;
			}
		}

		#endregion Type Property

		#region IsReadOnly Property
		private bool _isReadOnly;
		public bool IsReadOnly
		{
			get
			{
				return _isReadOnly;
			}
			protected set
			{
				_isReadOnly = value;
			}
		}
		#endregion IsReadOnly Property

		/// Is this archive case sensitive in the way it matches files
		public abstract bool IsCaseSensitive
		{
			get;
		}

		/// <summary>
		/// Is this archive capable of being monitored for additions, changes and deletions
		/// </summary>
		public virtual bool IsMonitorable
		{
			get
			{
				return false;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		/// Constructor - don't call direct, used by IArchiveFactory.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="archType"></param>
		public Archive( string name, string archType )
            : base()
		{
			_name = name;
			_type = archType;
			_isReadOnly = true;
		}

		#endregion Constructors

		#region Methods

		/// <summary>
		/// Loads the archive
		/// </summary>
		/// <remarks>
		/// This initializes all the internal data of the class.
		/// <para/>
		/// Do not call this function directly, it is meant to be used
		/// only by the ArchiveManager class.
		/// </remarks>
		public abstract void Load();

		/// <summary>
		/// Unloads the archive
		/// </summary>
		/// <remarks>
		/// Do not call this function directly, it is meant to be used
		/// only by the ArchiveManager class.
		/// </remarks>
		public abstract void Unload();

		/// <summary>
		/// Open a stream on a given file. 
		/// </summary>
		/// <remarks>
		/// There is no equivalent 'close' method; the returned stream
		/// controls the lifecycle of this file operation.
		/// </remarks>
		/// <param name="filename">The fully qualified name of the file</param>
		/// <param name="readOnly">True to open for Read Access, false for Read/Write </param>
		/// <returns>A reference to a Stream which can be used to read / write
		///  the file. If the file is not present, returns null.
		/// </returns>
		public abstract Stream Open( string filename, bool readOnly );

		/// <summary>
		/// Open a stream on a given file. 
		/// </summary>
		/// <remarks>
		/// There is no equivalent 'close' method; the returned stream
		/// controls the lifecycle of this file operation.
		/// </remarks>
		/// <param name="filename">The fully qualified name of the file</param>
		/// <returns>
		/// A reference to a Stream which can be used to read the file. If the file is not present, returns null.
		/// </returns>
		public Stream Open( string filename )
		{
			return Open( filename, true );
		}

		/// <summary>
		/// Create a new file (or overwrite one already there).
		/// </summary>
		/// <remarks>If the archive is read-only then this method will fail.</remarks>
		/// <param name="filename">The fully qualified name of the file</param>
		/// <returns>A Stream which can be used to read / write the file.</returns>
		public Stream Create( string filename )
		{
			return Create( filename, false );
		}

		/// <summary>
		/// Create a new file (or overwrite one already there).
		/// </summary>
		/// <remarks>If the archive is read-only then this method will fail.</remarks>
		/// <param name="filename">The fully qualified name of the file</param>
		/// <param name="overwrite">True to overwrite an existing file.</param>
		/// <returns>A Stream which can be used to read / write the file.</returns>
		public virtual Stream Create( string filename, bool overwrite )
		{
			throw new AxiomException( "This archive does not support creation of files." );
		}

		/// <summary>
		/// Delete a named file.
		/// </summary>
		/// <remarks>If the archive is read-only then this method will fail.</remarks>
		/// <param name="filename">The fully qualified name of the file</param>
		public virtual void Remove( string filename )
		{
			throw new AxiomException( "This archive does not support removal of files." );
		}

		#region List Method

		/// <overloads>
		/// <summary>
		/// List all file names in the archive.
		/// </summary>
		/// <remarks>    
		/// This method only returns filenames, you can also retrieve other
		/// information using listFileInfo.
		/// </remarks>
		/// <returns>A list of filenames matching the criteria, all are fully qualified</returns>
		/// </overloads>
		public List<string> List()
		{
			return List( true );
		}

		/// <param name="recursive">Whether all paths of the archive are searched (if the archive has a concept of that)</param>
		public abstract List<string> List( bool recursive );

		#endregion List Method

		#region ListFileInfo Method

		/// <summary>
		/// List all files in the archive with accompanying information.
		/// </summary>
		/// <returns>A list of structures detailing quite a lot of information about all the files in the archive.</returns>
		public FileInfoList ListFileInfo()
		{
			return ListFileInfo( true );
		}

		/// <param name="recursive">Whether all paths of the archive are searched (if the archive has a concept of that)</param>
		public abstract FileInfoList ListFileInfo( bool recursive );

		#endregion ListFileInfo Method

		#region Find Method

		/// <overloads>
		/// <summary>
		/// Find all file names matching a given pattern in this archive.
		/// </summary>
		/// <remarks> 
		/// This method only returns filenames, you can also retrieve other
		/// information using findFileInfo.
		/// </remarks>
		/// <param name="pattern">The pattern to search for; wildcards (*) are allowed</param>
		/// <returns>A list of filenames matching the criteria, all are fully qualified</returns>
		/// </overloads>
		public List<string> Find( string pattern )
		{
			return Find( pattern );
		}

		/// <param name="recursive">Whether all paths of the archive are searched (if the archive has a concept of that)</param>
		public abstract List<string> Find( string pattern, bool recursive );

		#endregion Find Method

		/// <summary>
		/// Find out if the named file exists
		/// </summary>
        /// <param name="fileName">fully qualified filename</param>
		/// <returns></returns>
		public abstract bool Exists( string fileName );

        /// <summary>
        /// Retrieve the modification time of a given file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public virtual DateTime GetModifiedTime(string fileName)
        {
            FileInfoList list = this.ListFileInfo();

            foreach (FileInfo currentInfo in list)
            {
                if (currentInfo.Basename.ToLower() != fileName.ToLower())
                    continue;

                return currentInfo.ModifiedTime;
            }

            return DateTime.MinValue;
        }

		#region FindFileInfo Method

		/// <overloads>
		/// <summary>
		/// Find all files matching a given pattern in this archive and get 
		/// some detailed information about them.
		/// </summary>
		/// <param name="pattern">The pattern to search for; wildcards (*) are allowed</param>
		/// <returns>A list of file information structures for all files matching the criteria.</returns>
		/// </overloads>
		public FileInfoList FindFileInfo( string pattern )
		{
			return FindFileInfo( pattern, true );
		}

		/// <param name="recursive">Whether all paths of the archive are searched (if the archive has a concept of that)</param>
		public abstract FileInfoList FindFileInfo( string pattern, bool recursive );

		#endregion FindFileInfo Method

		#endregion Methods

		#region IDisposable Implementation
		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		/// 
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

            base.dispose(disposeManagedResources);
		}

        #endregion IDisposable Implementation
    }

	/// <summary>
	/// 	abstract class for plugin developers to override to create 
	///		new types of archive to load resources from.
	/// </summary>
	/// <remarks>            
	/// All access to 'archives' (collections of files, compressed or
	/// just folders, maybe even remote) is managed via the abstract
	/// Archive class. Plugins are expected to provide the
	/// implementation for the actual codec itself, but because a
	/// subclass of Archive has to be created for every archive, a
	/// factory class is required to create the appropriate subclass.
	/// <para/>
	/// So archive plugins create a subclass of Archive AND a subclass
	/// of ArchiveFactory which creates instances of the Archive
	/// subclass. See the 'Zip' and 'FileSystem' plugins for examples.
	/// Each Archive and ArchiveFactory subclass pair deal with a
	/// single archive type (identified by a string).
	/// </remarks>
	/// <ogre name="ArchiveFactory">
	///     <file name="OgreArchiveFactory.h"   revision="1.11" lastUpdated="5/18/2006" lastUpdatedBy="Borrillis" />
	/// </ogre> 
	public class ArchiveFactory : AbstractFactory<Archive>
	{
	}

	#endregion Archive Abstract Class and Factory
}
