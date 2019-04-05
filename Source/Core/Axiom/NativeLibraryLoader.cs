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

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Axiom
{
    public class NativeLibraryLoader
    {
        public static void CopyNativeDll(String sourceLibraryName, string destinationLibraryName)
        {
            string executingFolder = GetExecutingFolder();

            string runtimesFolder = Path.Combine(executingFolder, "runtimes");


            string libraryPath = GetPlatformLibraryPath(runtimesFolder, sourceLibraryName);
            string libraryFileExtension = Path.GetExtension(libraryPath);

            if (false == File.Exists(libraryPath))
            {
                throw new FileNotFoundException(libraryPath);
            }

            string targetLibraryPath = Path.Combine(executingFolder, $"{destinationLibraryName}{libraryFileExtension}");

            if (File.Exists(targetLibraryPath))
            {
                File.Delete(targetLibraryPath);
            }

            File.Copy(libraryPath, targetLibraryPath, false);
        }

        private static string GetPlatformLibraryPath(string runtimesFolder, string libraryName)
        {
            string runtimeFolderName;
            string libraryExtension;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                runtimeFolderName = GetWindowsRuntimeFolder();
                libraryExtension = ".dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                runtimeFolderName = "osx.10.10-x64";
                libraryExtension = ".dylib";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                runtimeFolderName = "linux-x64";
                libraryExtension = ".so";
            }
            else
            {
                throw new Exception($"Unsupported platform");
            }

            return FindNativeLibrary(Path.Combine(runtimesFolder, runtimeFolderName, "native"), libraryName, libraryExtension);
        }

        private static string FindNativeLibrary(string nativeFolder, string libraryName, string libraryExtension)
        {
            var files = Directory.GetFiles(nativeFolder, $"*{libraryName}*{libraryExtension}");
            if (files.Length > 0)
            {
                return files[0];
            }
            return String.Empty;
        }

        private static string GetWindowsRuntimeFolder()
        {
            int ptrSize = Marshal.SizeOf<IntPtr>();
            return (ptrSize == 4) ? "win-x86" : "win-x64";
        }

        private static string GetExecutingFolder()
        {
            return Path.GetDirectoryName(typeof(NativeLibraryLoader).Assembly.Locati‌​on);
        }
    }
}
