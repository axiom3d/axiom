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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;
using System.Linq;

#endregion Namespace Declarations

namespace Axiom.Core
{
	///<summary>
	///  Class which manages the platform settings required to run.
	///</summary>
	public sealed class PlatformManager
	{
		/// <summary>
		///   Gets if the operating system that this is running on is Windows (as opposed to a Unix-based one such as Linux or Mac OS X)
		/// </summary>
		/// <remarks>
		///   The Windows version strings start with "Microsoft Windows" followed by CE, NT, or 98 and the version number, however Microsoft Win32S is used with the 32-bit simulation layer on 16-bit systems so we should just check for the presence of Microsoft Unix-based operating systems start with Unix The Environment.OSVersion.Platform is 128 for Unix-based platforms (an additional enum value added that by the name Unix), however under .NET 2.0 Unix is supposed to be 3 but may still be 128 under Mono Additionally, GNU Portable .NET likely doesn't provide this same value, so just check for the presence of Windows in the string name
		/// </remarks>
		public static bool IsWindowsOS
		{
			get
			{
				//return ((int)Environment.OSVersion.Platform) == 128;	//if is a unix-based operating system (running Mono), not sure if this will work for GNU Portable .NET
				var os = Environment.OSVersion.ToString();
				return os.IndexOf( "Microsoft" ) != -1;
			}
		}

		#region Singleton implementation

		/// <summary>
		///   Singleton instance of this class.
		/// </summary>
		private static IPlatformManager instance;

#if NET_40  && !( XBOX || XBOX360 || WINDOWS_PHONE )
		[ImportMany(typeof(IPlatformManager))]
		public IEnumerable<IPlatformManager> platforms { private get; set; }
#endif

		/// <summary>
		///   Internal constructor. This class cannot be instantiated externally.
		/// </summary>
		internal PlatformManager()
		{
			// First look in current Executing assembly for a PlatformManager
			if ( instance == null )
			{
				var platformMgr = new DynamicLoader();
				var platforms = platformMgr.Find( typeof ( IPlatformManager ) );
				if ( platforms.Count != 0 )
				{
					instance = platformMgr.Find( typeof ( IPlatformManager ) )[ 0 ].CreateInstance<IPlatformManager>();
				}
			}

#if NET_40 && !( XBOX || XBOX360 || WINDOWS_PHONE )
			if (instance == null)
			{
				this.SatisfyImports(".");
				if (platforms != null && platforms.Count() != 0)
				{
					instance = platforms.First();
					System.Diagnostics.Debug.WriteLine(String.Format("MEF IPlatformManager: {0}.", instance));
				}
			}
#endif

#if !( SILVERLIGHT || WINDOWS_PHONE || XBOX || XBOX360 )
			// Then look in loaded assemblies
			if ( instance == null )
			{
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				for ( var index = 0; index < assemblies.Length && instance == null; index++ )
				{
					//TODO: NRSC Added: Deal with Dynamic Assemblies not having a Location
					//if (assemblies[index].IsDynamic)
					//    continue;
					try
					{
						var platformMgr = new DynamicLoader( assemblies[ index ].Location );
						var platforms = platformMgr.Find( typeof ( IPlatformManager ) );
						if ( platforms.Count != 0 )
						{
							instance = platformMgr.Find( typeof ( IPlatformManager ) )[ 0 ].CreateInstance<IPlatformManager>();
						}
					}
					catch ( Exception )
					{
						System.Diagnostics.Debug.WriteLine( String.Format( "Failed to load assembly: {0}.", assemblies[ index ].FullName ) );
					}
				}
			}

			// Then look in external assemblies
			if ( instance == null )
			{
				// find and load a platform manager assembly
				var files = Directory.GetFiles( ".", "Axiom.Platforms.*.dll" ).ToArray();
				var file = "";

				// make sure there is 1 platform manager available
				if ( files.Length == 0 )
				{
					throw new PluginException( "A PlatformManager was not found in the execution path, and is required." );
				}
				else
				{
					var isWindows = IsWindowsOS;
					var platform = IsWindowsOS ? "Win32" : "OpenTK";

					if ( files.Length == 1 )
					{
						file = files[ 0 ];
					}
					else
					{
						for ( var i = 0; i < files.Length; i++ )
						{
							if ( ( files[ i ].IndexOf( platform ) != -1 ) == true )
							{
								file = files[ i ];
							}
						}
					}

					System.Diagnostics.Debug.WriteLine( String.Format( "Selected the PlatformManager contained in {0}.", file ) );
				}

				var path = Path.Combine( System.IO.Directory.GetCurrentDirectory(), file );

				var platformMgr = new DynamicLoader( path );
				var platforms = platformMgr.Find( typeof ( IPlatformManager ) );
				if ( platforms.Count != 0 )
				{
					instance = platformMgr.Find( typeof ( IPlatformManager ) )[ 0 ].CreateInstance<IPlatformManager>();
				}
			}
#endif

			// All else fails, yell loudly
			if ( instance == null )
			{
				throw new PluginException(
					"The available Platform assembly did not contain any subclasses of PlatformManager, which is required." );
			}
		}

		/// <summary>
		///   Gets the singleton instance of this class.
		/// </summary>
		public static IPlatformManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation
	}
}