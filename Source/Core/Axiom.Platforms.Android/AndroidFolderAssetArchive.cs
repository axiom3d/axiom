#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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

using System.Collections.Generic;

using Android.Content.Res;

using Axiom.FileSystem;

#endregion Namespace Declarations

namespace Axiom.Platform.Android
{
	public class AndroidFolderAssetArchive : FileSystemArchive
	{
		private string _type;
		private readonly AssetManager _assets;

		public AndroidFolderAssetArchive( AssetManager assets, string name, string type )
			: base( name, type )
		{
			this._assets = assets;
		}

		#region Archive Implementation

		public override bool IsCaseSensitive
		{
			get { return true; }
		}

		public override void Load() {}

		public override void Unload() {}

		public override System.IO.Stream Open( string filename, bool readOnly )
		{
			return null;
		}

		public override List<string> List( bool recursive )
		{
			this._assets.List( Name );
			return new List<string>();
		}

		public override FileInfoList ListFileInfo( bool recursive )
		{
			return new FileInfoList();
		}

		public override List<string> Find( string pattern, bool recursive )
		{
			return new List<string>();
		}

		public override bool Exists( string fileName )
		{
			return false;
		}

		public override FileInfoList FindFileInfo( string pattern, bool recursive )
		{
			return new FileInfoList();
		}

		#endregion Archive Implementation
	}

	public class AndroidFolderAssetArchiveFactory : ArchiveFactory
	{
		private const string _type = "AndroidFolderAsset";

		private readonly AssetManager _assets;

		public AndroidFolderAssetArchiveFactory( AssetManager assets )
		{
			this._assets = assets;
		}

		#region ArchiveFactory Implementation

		public override string Type
		{
			get { return _type; }
		}

		public override Archive CreateInstance( string name )
		{
			return new AndroidFolderAssetArchive( this._assets, name, _type );
		}

		public override void DestroyInstance( ref Archive obj )
		{
			if ( obj is AndroidFolderAssetArchive )
			{
				obj.Dispose();
				obj = null;
			}
		}

		#endregion ArchiveFactory Implementation
	}
}
