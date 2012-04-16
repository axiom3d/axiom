using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if (SILVERLIGHT && NET35)
[assembly: AssemblyTitle("SharpZipLib for Silverlight 3")]
#elif (SILVERLIGHT && NET4)
[assembly: AssemblyTitle("SharpZipLib for Silverlight 4")]
#elif (WINDOWS_PHONE)
[assembly: AssemblyTitle("SharpZipLib for Windows Phone 7")]
#else
[assembly: AssemblyTitle("SharpZipLibrary unlabelled version")]
#endif

[assembly: AssemblyDescription("A free C# compression library for Silverlight - http://slsharpziplib.codeplex.com/")]
[assembly: AssemblyProduct("#ZipLibrary")]
[assembly: AssemblyDefaultAlias("SharpZipLib")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("Salient Solutions")]
[assembly: AssemblyCopyright("Copyright 2001-2010 Mike Krueger, John Reilly, Sky Sanders")]
[assembly: AssemblyTrademark("Copyright 2001-2010 Mike Krueger, John Reilly, Sky Sanders")]

[assembly: AssemblyVersion("0.86.0.518")]
[assembly: AssemblyInformationalVersionAttribute("0.86.0")]

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

 


