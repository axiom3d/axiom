#region License
/*
MIT License
Copyright ©2003-2007 Tao Framework Team
http://www.taoframework.com
All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

#region --- Using Directives ---

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.Reflection.Emit;
using System.IO;

#endregion

namespace Tao.Platform.Windows
{
    public static partial class Wgl
    {
        #region --- Fields ---

        static StringBuilder sb = new StringBuilder();
        static object gl_lock = new object();

        internal const string Library = "opengl32.dll";

        private static SortedList<string, bool> AvailableExtensions = new SortedList<string, bool>();
        private static bool rebuildExtensionList;

        private static Type glClass;
        private static Type delegatesClass;
        private static Type importsClass;
        private static FieldInfo[] delegates;

        #endregion

        #region --- Static Constructor ---

        static Wgl()
        {
            glClass = typeof(Wgl);
            delegatesClass = glClass.GetNestedType("Delegates", BindingFlags.Static | BindingFlags.NonPublic);
            importsClass = glClass.GetNestedType("Imports", BindingFlags.Static | BindingFlags.NonPublic);
            // 'Touch' Imports class to force initialization. We don't want anything yet, just to have
            // this class ready.
            if (Imports.FunctionMap != null) { }
            ReloadFunctions();
        }

        #endregion

        #region --- Methods ---

        #region internal static partial class Imports

        /// <summary>
        /// Contains DllImports for the core OpenGL functions.
        /// </summary>
        internal static partial class Imports
        {
            /// <summary>
            ///  Build a string->MethodInfo map to speed up extension loading.
            /// </summary>
            internal static SortedList<string, MethodInfo> FunctionMap;  // This is faster than either Dictionary or SortedDictionary
            static Imports()
            {
                MethodInfo[] methods = importsClass.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
                FunctionMap = new SortedList<string, MethodInfo>(methods.Length);
                foreach (MethodInfo m in methods)
                    FunctionMap.Add(m.Name, m);
            }
        }

        #endregion

        #region public static Delegate GetDelegate(string name, Type signature)

        /// <summary>
        /// Creates a System.Delegate that can be used to call an OpenGL function, core or extension.
        /// </summary>
        /// <param name="name">The name of the OpenGL function (eg. "glNewList")</param>
        /// <param name="signature">The signature of the OpenGL function.</param>
        /// <returns>
        /// A System.Delegate that can be used to call this OpenGL function, or null if the specified
        /// function name did not correspond to an OpenGL function.
        /// </returns>
        public static Delegate GetDelegate(string name, Type signature)
        {
            MethodInfo m;
            Delegate r = GetExtensionDelegate(name, signature) ??
                  (Imports.FunctionMap.TryGetValue((name.Substring(3)), out m) ?
                   Delegate.CreateDelegate(signature, m) : null);

            return r;
        }

        #endregion

        #region public static void ReloadFunctions()

        /// <summary>
        /// Loads all OpenGL functions (core and extensions).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This function will be automatically called the first time you use any opengl function. There is 
        /// </para>
        /// <para>
        /// Call this function manually whenever you need to update OpenGL entry points.
        /// This need may arise if you change the pixelformat/visual, or in case you cannot
        /// (or do not want) to use the automatic initialization of the GL class.
        /// </para>
        /// </remarks>
        public static void ReloadFunctions()
        {
            // Using reflection is more than 3 times faster than directly loading delegates on the first
            // run, probably due to code generation overhead. Subsequent runs are faster with direct loading
            // than with reflection, but the first time is more significant.

            if (delegates == null)
                delegates = delegatesClass.GetFields(BindingFlags.Static | BindingFlags.NonPublic);

            foreach (FieldInfo f in delegates)
                f.SetValue(null, GetDelegate(f.Name, f.FieldType));

            rebuildExtensionList = true;
        }

        static void set(object d, Delegate value)
        {
            d = value;
        }

        #endregion

        #region public static bool ReloadFunction(string function)

        /// <summary>
        /// Tries to reload the given OpenGL function (core or extension).
        /// </summary>
        /// <param name="function">The name of the OpenGL function (i.e. glShaderSource)</param>
        /// <returns>True if the function was found and reloaded, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Use this function if you require greater granularity when loading OpenGL entry points.
        /// </para>
        /// <para>
        /// While the automatic initialisation will load all OpenGL entry points, in some cases
        /// the initialisation can take place before an OpenGL Context has been established.
        /// In this case, use this function to load the entry points for the OpenGL functions
        /// you will need, or use ReloadFunctions() to load all available entry points.
        /// </para>
        /// <para>
        /// This function returns true if the given OpenGL function is supported, false otherwise.
        /// </para>
        /// <para>
        /// To query for supported extensions use the IsExtensionSupported() function instead.
        /// </para>
        /// </remarks>
        public static bool Load(string function)
        {
            FieldInfo f = delegatesClass.GetField(function, BindingFlags.Static | BindingFlags.NonPublic);
            if (f == null)
                return false;

            Delegate old = f.GetValue(null) as Delegate;
            Delegate @new = GetDelegate(f.Name, f.FieldType);
            if (old.Target != @new.Target)
            {
                f.SetValue(null, @new);
            }
            return @new != null;
        }

        #endregion

        #region internal static Delegate GetExtensionDelegate(string name, Type signature)

        /// <summary>
        /// Creates a System.Delegate that can be used to call a dynamically exported OpenGL function.
        /// </summary>
        /// <param name="name">The name of the OpenGL function (eg. "glNewList")</param>
        /// <param name="signature">The signature of the OpenGL function.</param>
        /// <returns>
        /// A System.Delegate that can be used to call this OpenGL function or null
        /// if the function is not available in the current OpenGL context.
        /// </returns>
        internal static Delegate GetExtensionDelegate(string name, Type signature)
        {
            IntPtr address = Imports.GetProcAddress(name);

            if (address == IntPtr.Zero ||
                address == new IntPtr(1) ||     // Workaround for buggy nvidia drivers which return
                address == new IntPtr(2))       // 1 or 2 instead of IntPtr.Zero for some extensions.
            {
                return null;
            }
            else
            {
                return Marshal.GetDelegateForFunctionPointer(address, signature);
            }
        }

        #endregion

        #region public static bool IsExtensionSupported(string name)

        /// <summary>
        /// Determines whether the specified OpenGL extension category is available in
        /// the current OpenGL context. Equivalent to IsExtensionSupported(name, true)
        /// </summary>
        /// <param name="name">The string for the OpenGL extension category (eg. "GL_ARB_multitexture")</param>
        /// <returns>True if the specified extension is available, false otherwise.</returns>
        public static bool IsExtensionSupported(string name)
        {
            if (rebuildExtensionList)
                BuildExtensionList();

            lock (gl_lock)
            {
                sb.Remove(0, sb.Length);
                if (!name.StartsWith("WGL_"))
                    sb.Append("wgl_");
                sb.Append(name.ToLower());

                // Search the cache for the string.
                return AvailableExtensions.ContainsKey(sb.ToString());
            }
        }

        #endregion


        #region BuildExtensionList
        internal static void BuildExtensionList()
        {
            AvailableExtensions.Clear();

            string extension_string = "";
            try
            {
                extension_string = wglGetExtensionsStringARB(wglGetCurrentDC());
            }
            catch (NullReferenceException)
            { }

            if (String.IsNullOrEmpty(extension_string))
                return;               // no extensions are available

            string[] extensions = extension_string.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string ext in extensions)
                AvailableExtensions.Add(ext.ToLower(), true);
        }
        #endregion

        #endregion
    }
}
