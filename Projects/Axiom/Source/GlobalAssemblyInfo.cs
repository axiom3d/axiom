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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;

#endregion Namespace Declarations

#if DEBUG
[assembly: AssemblyConfiguration( "Debug" )]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyProduct( "Axiom Rendering Engine" )]
[assembly: AssemblyCompany( "Axiom Rendering Engine Project Team." )]
[assembly: AssemblyCopyright( "© 2003-2006 Axiom Rendering Engine Project Team." )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

#if DEBUG
[assembly: AssemblyVersion( "0.7.2.*" )]
#else
[assembly: AssemblyVersion( "0.7.2.0" )]
#endif

[assembly: AssemblyFileVersion( "0.7.2.0" )]

[assembly: SecurityPermission( SecurityAction.RequestMinimum )]
[assembly: ComVisible( false )]

[assembly: AssemblyDelaySign( false )]
[assembly: AssemblyKeyFile( "" )]
[assembly: AssemblyKeyName( "" )]