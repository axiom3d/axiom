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
//     <id value="$Id: WebArchive.cs 1537 2009-03-30 19:25:01Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Linq;

using Axiom.Core;

using XNA = Microsoft.Xna.Framework;

#endregion Namespace Declarations

namespace Axiom.FileSystem
{
	/// <summary>
	/// </summary>
	public class TitleContainerArchive : FileSystemArchive
	{
		#region Fields and Properties

		List<string> _files = new List<string>();

		/// <summary>
		/// Is this archive capable of being monitored for additions, changes and deletions
		/// </summary>
		public override bool IsMonitorable
		{
			get
			{
				return false;
			}
		}

		#endregion Fields and Properties

		#region Utility Methods

		protected override bool DirectoryExists( string directory )
		{
			return true;
		}

		public override List<string> Find(string pattern, bool recursive)
		//protected override string[] getFiles( string dir, string pattern, bool recurse )
		{
			var searchResults = new List<string>();

			foreach ( var file in _files )
			{
				var ext = Path.GetExtension( file );

				if ( pattern == "*" || pattern.Contains( ext ) )
				{
					searchResults.Add( file );
				}
			}

			return searchResults;
		}

		public override FileInfoList FindFileInfo( string pattern, bool recursive )
		{
			var fil = new FileInfoList();
			_files.ForEach( ( file ) =>
			{
				var ext = Path.GetExtension( file );

				if ( pattern == "*" || pattern.Contains( ext ) )
				{
					fil.Add( new FileInfo
					{
						Archive = this,
						CompressedSize = 0,
						Basename = file,
						Filename = file,
						ModifiedTime = DateTime.Now,
						Path = _basePath,
						UncompressedSize = 0
					} );
				}
			} );

			return fil;
			//return base.FindFileInfo( pattern, recursive );
		}
		#endregion Utility Methods

		#region Constructors and Destructors

		public TitleContainerArchive( string name, string archType )
			: base( name, archType )
		{
			// Cache manifest file contents
			var manifestStream = XNA.TitleContainer.OpenStream(  name + @"\files.manifest" );
			var reader = new StreamReader( manifestStream );

			while ( !reader.EndOfStream )
			{
				var file = reader.ReadLine();
				_files.Add( file );
			}
		}

		#endregion Constructors and Destructors

		#region Archive Implementation

		public override bool IsCaseSensitive
		{
			get
			{
				return true;
			}
		}

		public override void Load()
		{
			_basePath = Name + "/";
			IsReadOnly = true;
			SafeDirectoryChange( _basePath, () => IsReadOnly = true );
		}

		public override Stream Create( string filename, bool overwrite )
		{
			throw new AxiomException( "Cannot create a file in a read-only archive." );
		}

		public override Stream Open( string filename, bool readOnly )
		{
			if ( !readOnly )
			{
				throw new AxiomException( "Cannot create a file in a read-only archive." );
			}
			if ( _files.Contains( filename ) )
			{
				return XNA.TitleContainer.OpenStream( _basePath + filename );
			}
			return null;
		}

		public override bool Exists( string fileName )
		{
			return Open( fileName, true ) != null;
		}

		#endregion Archive Implementation
	}

	/// <summary>
	/// Specialization of IArchiveFactory for Web files.
	/// </summary>
	public class TitleContainerArchiveFactory : ArchiveFactory
	{
		private const string _type = "TitleContainer";

		#region ArchiveFactory Implementation

		public override string Type
		{
			get
			{
				return _type;
			}
		}

		public override Archive CreateInstance( string name )
		{
			return new TitleContainerArchive( name, _type );
		}

		public override void DestroyInstance( ref Archive obj )
		{
			obj.Dispose();
		}

		#endregion ArchiveFactory Implementation
	} ;
}
