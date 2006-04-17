#region LGPL License
/*
This file is part of the RealmForge GDK.
Copyright (C) 2003-2004 Daniel L. Moorehead

The RealmForge GDK is a cross-platform game development framework and toolkit written in Mono/C# and powered by the Axiom 3D engine. It will allow for the rapid development of cutting-edge software and MMORPGs with advanced graphics, audio, and networking capabilities.

dan@xeonxstudios.com
http://xeonxstudios.com
http://sf.net/projects/realmforge

If you have or intend to contribute any significant amount of code or changes to RealmForge you must go have completed the Xeonx Studios Copyright Assignment.

RealmForge is free software; you can redistribute it and/or modify it under the terms of  the GNU Lesser General Public License as published by the Free Software Foundation; either version 2 or (at your option) any later version.

RealmForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the accompanying RealmForge License and GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with RealmForge; if not, write to the Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA.
*/
#endregion

using System.Reflection;
using System.Runtime.CompilerServices;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//

[assembly: AssemblyTitle( "RealmForge Utility Library" )]
[assembly: AssemblyDescription( "Utility library for RealmForge, Axiom, and CEGUI# which provides logging, serialization, file system, and reflection services." )]
[assembly: AssemblyCompany( "RealmWare (http://realmforge.com)" )]
[assembly: AssemblyProduct( "RealmForge GDK" )]
[assembly: AssemblyCopyright( "Copyright © Dan Moorehead 2003-2005" )]

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersion( "0.6.3.*" )]

//
// In order to sign your assembly you must specify a key to use. Refer to the 
// Microsoft .NET Framework documentation for more information on assembly signing.
//
// Use the attributes below to control which key is used for signing. 
//
// Notes: 
//   (*) If no key is specified, the assembly is not signed.
//   (*) KeyName refers to a key that has been installed in the Crypto Service
//       Provider (CSP) on your machine. KeyFile refers to a file which contains
//       a key.
//   (*) If the KeyFile and the KeyName values are both specified, the 
//       following processing occurs:
//       (1) If the KeyName can be found in the CSP, that key is used.
//       (2) If the KeyName does not exist and the KeyFile does exist, the key 
//           in the KeyFile is installed into the CSP and used.
//   (*) In order to create a KeyFile, you can use the sn.exe (Strong Name) utility.
//       When specifying the KeyFile, the location of the KeyFile should be
//       relative to the project output directory which is
//       %Project Directory%\obj\<configuration>. For example, if your KeyFile is
//       located in the project directory, you would specify the AssemblyKeyFile 
//       attribute as [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) Delay Signing is an advanced option - see the Microsoft .NET Framework
//       documentation for more information on this.
//
[assembly: AssemblyDelaySign( false )]
[assembly: AssemblyKeyFile( "" )]
[assembly: AssemblyKeyName( "" )]
