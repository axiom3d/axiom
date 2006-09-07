
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
[assembly: AssemblyVersion( "0.7.1.*" )]
[assembly: AssemblyFileVersion( "0.7.1.*" )]
#else
[assembly: AssemblyVersion( "0.7.1.0" )]
[assembly: AssemblyFileVersion( "0.7.1.0" )]
#endif

[assembly: SecurityPermission( SecurityAction.RequestMinimum )]

[assembly: AssemblyDelaySign( false )]
[assembly: AssemblyKeyFile( "" )]
[assembly: AssemblyKeyName( "" )]