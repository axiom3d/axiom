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
//     <id value="$Id: AndroidResourceArchive.cs 2204 2010-09-19 00:22:17Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using Axiom.FileSystem;

#endregion Namespace Declarations

namespace Axiom.Platform.IPhone
{
	public class IPhoneResourceArchive : Archive
	{
		private string _type;

		public IPhoneResourceArchive( string name, string type )
			: base( name, type )
		{
			// TODO: Complete member initialization
			this.Name = name;
			this._type = type;
		}

		#region Archive Implementation

		public override bool IsCaseSensitive
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override void Load()
		{
			throw new NotImplementedException();
		}

		public override void Unload()
		{
			throw new NotImplementedException();
		}

		public override System.IO.Stream Open( string filename, bool readOnly )
		{
			throw new NotImplementedException();
		}

		public override List<string> List( bool recursive )
		{
			throw new NotImplementedException();
		}

		public override FileInfoList ListFileInfo( bool recursive )
		{
			throw new NotImplementedException();
		}

		public override List<string> Find( string pattern, bool recursive )
		{
			throw new NotImplementedException();
		}

		public override bool Exists( string fileName )
		{
			throw new NotImplementedException();
		}

		public override FileInfoList FindFileInfo( string pattern, bool recursive )
		{
			throw new NotImplementedException();
		}

		#endregion Archive Implementation
	}

	public class IPhoneArchiveFactory : ArchiveFactory
	{

		private const string _type = "IPhoneResource";

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
			return new IPhoneResourceArchive( name, _type );
		}

		public override void DestroyInstance( ref Archive obj )
		{
			obj.Dispose();
		}

		#endregion ArchiveFactory Implementation

	}
}
