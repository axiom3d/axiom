#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using System.Collections.Specialized;
using Axiom.Exceptions;
using Axiom.Graphics;
using Tao.OpenGl;
using Tao.Platform.Windows;

namespace Axiom.RenderSystems.OpenGL {
    /// <summary>
    /// Summary description for GLHelper.
    /// </summary>
    public abstract class BaseGLHelper {
		#region Fields

		/// <summary>
		///		Collection of extensions supported by the current hardware.
		/// </summary>
        private static StringCollection extensionList;
		/// <summary>
		///		OpenGL version string.
		/// </summary>
        private static string glVersion;
		/// <summary>
		///		Vendor of the current hardware.
		/// </summary>
        private static string vendor;
		/// <summary>
		///		Name of the video card in use.
		/// </summary>
        private static string videoCard;

		#endregion Fields

        /// <summary>
        ///		Gets a collection of strings listing all the available extensions.
        /// </summary>
        public static StringCollection Extensions {
            get {
                return extensionList; 
            }
        }

        /// <summary>
        ///		Handy check to see if the current GL version is at least what is supplied.
        /// </summary>
        /// <param name="version">What you want to check for, i.e. "1.3" </param>
        /// <returns></returns>
        public static bool CheckMinVersion(string version) {
            return glVersion.StartsWith(version);
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="extention"></param>
        /// <returns></returns>
        public static bool SupportsExtension(string extention) {
            // check if the extension is supported
            return extensionList.Contains(extention);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void InitializeExtensions() {
            if(extensionList == null) {
                // load GL extensions
                Ext.Init();

                // get the OpenGL version string and vendor name
                glVersion = Gl.glGetString(Gl.GL_VERSION);
                videoCard = Gl.glGetString(Gl.GL_RENDERER);
                vendor = Gl.glGetString(Gl.GL_VENDOR);

                // parse out the first piece of the vendor string if there are spaces in it
                if(vendor.IndexOf(" ") != -1) {
                    vendor = vendor.Substring(0, vendor.IndexOf(" "));
                }

                // create a new extension list
                extensionList = new StringCollection();

                string allExt = Gl.glGetString(Gl.GL_EXTENSIONS);
                string[] splitExt = allExt.Split(Char.Parse(" "));

                // store the parsed extension list
				for(int i = 0; i < splitExt.Length; i++) {
					extensionList.Add(splitExt[i]);
				}
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usage"></param>
        /// <returns></returns>
        public static int ConvertEnum(BufferUsage usage) {
            switch(usage) {
                case BufferUsage.Static:
                case BufferUsage.StaticWriteOnly:
                    return (int)Gl.GL_STATIC_DRAW_ARB;

                case BufferUsage.Dynamic:
                case BufferUsage.DynamicWriteOnly:
                default:
                    return (int)Gl.GL_DYNAMIC_DRAW_ARB;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int ConvertEnum(VertexElementType type) {
            switch(type) {
                case VertexElementType.Float1:
                case VertexElementType.Float2:
                case VertexElementType.Float3:
                case VertexElementType.Float4:
                    return Gl.GL_FLOAT;

                case VertexElementType.Short1:
                case VertexElementType.Short2:
                case VertexElementType.Short3:
                case VertexElementType.Short4:
                    return Gl.GL_SHORT;

                case VertexElementType.Color:
				case VertexElementType.UByte4:
                    return Gl.GL_UNSIGNED_BYTE;
            }

            // should never reach this
            return 0;
        }

        /// <summary>
        ///		Find the GL int value for the CompareFunction enum.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static int ConvertEnum(CompareFunction func) {
            switch(func) {
                case CompareFunction.AlwaysFail:
                    return Gl.GL_NEVER;
                case CompareFunction.AlwaysPass:
                    return Gl.GL_ALWAYS;
                case CompareFunction.Less:
                    return Gl.GL_LESS;
                case CompareFunction.LessEqual:
                    return Gl.GL_LEQUAL;
                case CompareFunction.Equal:
                    return Gl.GL_EQUAL;
                case CompareFunction.NotEqual:
                    return Gl.GL_NOTEQUAL;
                case CompareFunction.GreaterEqual:
                    return Gl.GL_GEQUAL;
                case CompareFunction.Greater:
                    return Gl.GL_GREATER;
            } // switch

            // make the compiler happy
            return 0;
        }

		public static int ConvertEnum(StencilOperation op) {
			return ConvertEnum(op, false);
		}

        /// <summary>
        ///		Find the GL int value for the StencilOperation enum.
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public static int ConvertEnum(StencilOperation op, bool invert) {
            switch(op) {
                case StencilOperation.Keep:
                    return Gl.GL_KEEP;

                case StencilOperation.Zero:
                    return Gl.GL_ZERO;

                case StencilOperation.Replace:
                    return Gl.GL_REPLACE;

                case StencilOperation.Increment:
                    return invert ? Gl.GL_DECR : Gl.GL_INCR;

                case StencilOperation.Decrement:
                    return invert ? Gl.GL_INCR : Gl.GL_DECR;

				case StencilOperation.IncrementWrap:
					return invert ? Gl.GL_DECR_WRAP_EXT : Gl.GL_INCR_WRAP_EXT;

				case StencilOperation.DecrementWrap:
					return invert ? Gl.GL_INCR_WRAP_EXT : Gl.GL_DECR_WRAP_EXT;

                case StencilOperation.Invert:
                    return Gl.GL_INVERT;
            }

            // make the compiler happy
            return Gl.GL_KEEP;
        }

        public static int ConvertEnum(GpuProgramType type) {
            switch(type) {
                case GpuProgramType.Vertex:
                    return Gl.GL_VERTEX_PROGRAM_ARB;

                case GpuProgramType.Fragment:
                    return Gl.GL_FRAGMENT_PROGRAM_ARB;
            }

            // make the compiler happy
            return 0;
        }

        public static string Vendor {
            get { 
                return vendor; 
            }
        }

        public static string VideoCard {
            get { 
                return videoCard; 
            }
        }

        public static string Version {
            get { 
                return glVersion; 
            }
        }

		public 
    }
}
