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
    public class GLHelper {
        private static StringCollection extensionList;
        private static string glVersion;
        private static string vendor;
        private static string videoCard;

        /// <summary>
        /// 
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
                for(int i = 0; i < splitExt.Length; i++)
                    extensionList.Add(splitExt[i]);
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

        /// <summary>
        ///		Find the GL int value for the StencilOperation enum.
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public static int ConvertEnum(StencilOperation op) {
            switch(op) {
                case StencilOperation.Keep:
                    return Gl.GL_KEEP;
                case StencilOperation.Zero:
                    return Gl.GL_ZERO;
                case StencilOperation.Replace:
                    return Gl.GL_REPLACE;
                case StencilOperation.Increment:
                    return Gl.GL_INCR;
                case StencilOperation.Decrement:
                    return Gl.GL_DECR;
                case StencilOperation.Invert:
                    return Gl.GL_INVERT;
            }

            // make the compiler happy
            return 0;
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
    }

    /// <summary>
    ///    Wrapper for Tao extension methods to cache the extension pointers and wrap methods eliminating
    ///    the need to pass them in manually.
    /// </summary>
    public class Ext {

        #region GL_ARB_multitexture

        private static IntPtr activeTextureARB;
        private static IntPtr clientActiveTextureARB;

        public static void glActiveTextureARB(int texture) {
            Gl.glActiveTextureARB(activeTextureARB, texture);
        }

        public static void glClientActiveTextureARB(int texture) {
            Gl.glClientActiveTextureARB(clientActiveTextureARB, texture);
        }

        #endregion GL_ARB_multitexture

        #region GL_ARB_vertex_buffer_object

        private static IntPtr bindBufferARBPtr;
        private static IntPtr bufferDataARBPtr;
        private static IntPtr bufferSubDataARBPtr;
        private static IntPtr deleteBuffersARBPtr;
        private static IntPtr genBuffersARBPtr;
        private static IntPtr getBufferSubDataARBPtr;      
        private static IntPtr mapBufferARBPtr;
        private static IntPtr unmapBufferARBPtr;
		private static IntPtr getBufferParameterivARBptr;

        public static void glBindBufferARB(int target, int buffer) {
            Gl.glBindBufferARB(bindBufferARBPtr, target, buffer);
        }

        public static void glBufferDataARB(int target, int size, IntPtr data, int usage) {
            Gl.glBufferDataARB(bufferDataARBPtr, target, size, data, usage);
        }

        public static void glBufferSubDataARB(int target, int offset, int size, IntPtr data) {
            Gl.glBufferSubDataARB(bufferSubDataARBPtr, target, offset, size, data);
        }

        public static void glDeleteBuffersARB(int number, ref int buffer) {
            // TODO: Fix, currently does nothing
			//Gl.glDeleteBuffersARB(deleteBuffersARBPtr, number, new int[]{buffer});
        }

        public static void glGenBuffersARB(int number, out int buffer) {
            Gl.glGenBuffersARB(genBuffersARBPtr, number, out buffer);
        }

        public static void glGetBufferSubDataARB(int target, int offset, int size, IntPtr data) {
            Gl.glGetBufferSubDataARB(getBufferSubDataARBPtr, target, offset, size, data);
        }

        public static IntPtr glMapBufferARB(int target, int access) {
            return Gl.glMapBufferARB(mapBufferARBPtr, target, access);
        }

        public static int glUnmapBufferARB(int target) {
			return Gl.glUnmapBufferARB(unmapBufferARBPtr, target);
        }

		public static void glGetBufferParameterivARB(int target, int name, int[] parameters) {
			Gl.glGetBufferParameterivARB(getBufferParameterivARBptr, target, name, parameters);
		}

        #endregion GL_ARB_vertex_buffer_object

        #region GL_ARB_vertex_program/GL_ARB_fragment_program

        private static IntPtr bindProgramARBPtr;
        private static IntPtr deleteProgramsARBPtr;
        private static IntPtr genProgramsARBPtr;
        private static IntPtr programStringARBPtr;
        private static IntPtr isProgramARBPtr;
        private static IntPtr programLocalParameter4fvARBPtr;

        public static void glGenProgramsARB(int number, out int program) {
            Gl.glGenProgramsARB(genProgramsARBPtr, number, out program);
        }

        public static void glBindProgramARB(int type, int programId) {
            Gl.glBindProgramARB(bindProgramARBPtr, type, programId);
        }

        public static void glDeleteProgramsARB(int number, ref int program) {
            // TODO: Fix
            Gl.glDeleteProgramsARB(deleteProgramsARBPtr, number, ref program);
        }

        public static void glProgramStringARB(int type, int format, int length, string source) {
            Gl.glProgramStringARB(programStringARBPtr, type, format, length, source);
        }

        public static bool glIsProgramARB(int programId) {
            return Gl.glIsProgramARB(isProgramARBPtr, programId) != 0;
        }

        public static void glProgramLocalParameter4vfARB(int type, int index, float[] values) {
            Gl.glProgramLocalParameter4fvARB(programLocalParameter4fvARBPtr, type, index, values);
        }

        #endregion GL_ARB_vertex_program/GL_ARB_fragment_program
        
        #region GL_EXT_texture3D
    
        private static IntPtr texImage3DEXTPtr;

        public static void glTexImage3DEXT(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, byte[] pixels) {
            Gl.glTexImage3DEXT(texImage3DEXTPtr, target, level, internalformat, width, height, depth, border, format, type, pixels);
        }

        #endregion GL_EXT_texture3D

        #region GL_ARB_texture_compression

        private static IntPtr compressedTexImage2DARB;

        public static void glCompressedTexImage2DARB(int target, int level, int internalformat, int width, int height, int border, int imageSize, byte[] data)  {
            Gl.glCompressedTexImage2DARB(compressedTexImage2DARB, target, level, internalformat, width, height, border, imageSize, data);
        }

        #endregion 

        #region GL_ATI_fragment_shader

        private static IntPtr genFragmentShadersATIptr;
        private static IntPtr bindFragmentShaderATIptr;
        private static IntPtr deleteFragmentShaderATIptr;
        private static IntPtr beginFragmentShaderATIptr;
        private static IntPtr endFragmentShaderATIptr;
        private static IntPtr setFragmentShaderConstantATIptr;
        private static IntPtr colorFragmentOp1ATIptr;
        private static IntPtr colorFragmentOp2ATIptr;
        private static IntPtr colorFragmentOp3ATIptr;
        private static IntPtr alphaFragmentOp1ATIptr;
        private static IntPtr alphaFragmentOp2ATIptr;
        private static IntPtr alphaFragmentOp3ATIptr;
        private static IntPtr passTexCoordATIptr;
        private static IntPtr sampleMapATIptr;

        public static int glGenFragmentShadersATI(int range) {
            return Gl.glGenFragmentShadersATI(genFragmentShadersATIptr, range);
        }

        public static void glBindFragmentShaderATI(int id) {
            Gl.glBindFragmentShaderATI(bindFragmentShaderATIptr, id);
        }

        public static void glDeleteFragmentShaderATI(int id) {
            Gl.glDeleteFragmentShaderATI(deleteFragmentShaderATIptr, id);
        }

        public static void glBeginFragmentShaderATI() {
            Gl.glBeginFragmentShaderATI(beginFragmentShaderATIptr);
        }

        public static void glEndFragmentShaderATI() {
            Gl.glEndFragmentShaderATI(endFragmentShaderATIptr);
        }

        public static void glSetFragmentShaderConstantATI(int index, float[] values) {
            Gl.glSetFragmentShaderConstantATI(setFragmentShaderConstantATIptr, index, values);
        }

        public static void glColorFragmentOp1ATI(int op, int dst, int dstMask, int dstMod, int arg1, int arg1Rep, int arg1Mod) {
            Gl.glColorFragmentOp1ATI(colorFragmentOp1ATIptr, op, dst, dstMask, dstMod, arg1, arg1Rep, arg1Mod);
        }

        public static void glColorFragmentOp2ATI(int op, int dst, int dstMask, int dstMod, int arg1, int arg1Rep, int arg1Mod, int arg2, int arg2Rep, int arg2Mod) {
            Gl.glColorFragmentOp2ATI(colorFragmentOp2ATIptr, op, dst, dstMask, dstMod, arg1, arg1Rep, arg1Mod, arg2, arg2Rep, arg2Mod);
        }

        public static void glColorFragmentOp3ATI(int op, int dst, int dstMask, int dstMod, int arg1, int arg1Rep, int arg1Mod, int arg2, int arg2Rep, int arg2Mod, int arg3, int arg3Rep, int arg3Mod) {
            Gl.glColorFragmentOp3ATI(colorFragmentOp3ATIptr, op, dst, dstMask, dstMod, arg1, arg1Rep, arg1Mod, arg2, arg2Rep, arg2Mod, arg3, arg3Rep, arg3Mod);
        }

        public static void glAlphaFragmentOp1ATI(int op, int dst, int dstMod, int arg1, int arg1Rep, int arg1Mod) {
            Gl.glAlphaFragmentOp1ATI(alphaFragmentOp1ATIptr, op, dst, dstMod, arg1, arg1Rep, arg1Mod);
        }

        public static void glAlphaFragmentOp2ATI(int op, int dst, int dstMod, int arg1, int arg1Rep, int arg1Mod, int arg2, int arg2Rep, int arg2Mod) {
            Gl.glAlphaFragmentOp2ATI(alphaFragmentOp2ATIptr, op, dst, dstMod, arg1, arg1Rep, arg1Mod, arg2, arg2Rep, arg2Mod);
        }

        public static void glAlphaFragmentOp3ATI(int op, int dst, int dstMod, int arg1, int arg1Rep, int arg1Mod, int arg2, int arg2Rep, int arg2Mod, int arg3, int arg3Rep, int arg3Mod) {
            Gl.glAlphaFragmentOp3ATI(alphaFragmentOp3ATIptr, op, dst, dstMod, arg1, arg1Rep, arg1Mod, arg2, arg2Rep, arg2Mod, arg3, arg3Rep, arg3Mod);
        }

        public static void glPassTexCoordATI(int dst, int coord, int swizzle) {
            Gl.glPassTexCoordATI(passTexCoordATIptr, dst, coord, swizzle);
        }

        public static void glSampleMapATI(int dst, int interp, int swizzle) {
            Gl.glSampleMapATI(sampleMapATIptr, dst, interp, swizzle);
        }

        #endregion GL_ATI_fragment_shader

        #region GL_NV_register_combiners

        private static IntPtr combinerStageParameterfvNVptr;

        public static void glCombinerStageParameterfvNV(int stage, int pname, float[] values) {
            Gl.glCombinerStageParameterfvNV(combinerStageParameterfvNVptr, stage, pname, values);
        }

        #endregion NV20 Crap

        #region GL_NV_fragment_program/GL_NV_vertex_program2

        private static IntPtr genProgramsNVptr;
        private static IntPtr bindProgramNVptr;
        private static IntPtr deleteProgramsNVptr;
        private static IntPtr loadProgramNVptr;
        private static IntPtr programNamedParameter4fNVptr;
        private static IntPtr programParameter4fvNVptr;

        public static void glGenProgramsNV(int num, out int id) {
            Gl.glGenProgramsNV(genProgramsNVptr, num, out id);
        }

        public static void glBindProgramNV(int target, int id) {
            Gl.glBindProgramNV(bindProgramNVptr, target, id);
        }

        public static void glDeleteProgramsNV(int num, ref int id) {
            Gl.glDeleteProgramsNV(deleteProgramsNVptr, num, ref id);
        }

        public static void glLoadProgramNV(int target, int id, int length, string program) {
            Gl.glLoadProgramNV(loadProgramNVptr, target, id, length, program);
        }

        public static void glProgramNamedParameter4fNV(int id, int length, string name, float x, float y, float z, float w) {
            Gl.glProgramNamedParameter4fNV(programNamedParameter4fNVptr, id, length, name, x, y, z, w);
        }
       
        public static void glProgramParameter4fvNV(int target, int index, float[] vals) {
            Gl.glProgramParameter4fvNV(programParameter4fvNVptr, target, index, vals);
        }

        #endregion GL_NV_fragment_program/GL_NV_vertex_program2

        #region GL_EXT_secondary_color

        private static IntPtr secondaryColorPointerEXTptr;

        public static void glSecondaryColorPointerEXT(int size, int type, int stride, IntPtr pointer) {
            Gl.glSecondaryColorPointerEXT(secondaryColorPointerEXTptr, size, type, stride, pointer);
        }

        #endregion GL_EXT_secondary_color

		#region Vertex Attributes

		private static IntPtr vertexAttribPointerARBptr;
		private static IntPtr enableVertexAttribArrayARBptr;
		private static IntPtr disableVertexAttribArrayARBptr;

		public static void glVertexAttribPointerARB(int index, int size, int type, int normalized, int stride, IntPtr pointer) {
			Gl.glVertexAttribPointerARB(vertexAttribPointerARBptr, index, size, type, normalized, stride, pointer);
		}

		public static void glEnableVertexAttribArrayARB(int index) {
			Gl.glEnableVertexAttribArrayARB(enableVertexAttribArrayARBptr, index);
		}

		public static void glDisableVertexAttribArrayARB(int index) {
			Gl.glDisableVertexAttribArrayARB(disableVertexAttribArrayARBptr, index);
		}

		#endregion Vertex Attributes

		#region GL_draw_range_elements

		private static IntPtr drawRangeElementsptr;

		public static void glDrawRangeElements(int mode, int start, int end, int count, int type, IntPtr indices) {
			Gl.glDrawRangeElements(drawRangeElementsptr, mode, start, end, count, type, indices);
		}

		#endregion GL_draw_range_elements

        /// <summary>
        ///    Must be fired up after a GL context has been created.
        /// </summary>
        public static void Init() {
            // ARB_vertex_buffer_object
            bindBufferARBPtr = Wgl.wglGetProcAddress("glBindBufferARB");
            bufferDataARBPtr = Wgl.wglGetProcAddress("glBufferDataARB");        
            bufferSubDataARBPtr = Wgl.wglGetProcAddress("glBufferSubDataARB"); 
            deleteBuffersARBPtr = Wgl.wglGetProcAddress("glDeleteBuffersARB");       
            genBuffersARBPtr = Wgl.wglGetProcAddress("glGenBuffersARB");
            getBufferSubDataARBPtr = Wgl.wglGetProcAddress("glGetBufferSubDataARB");        
            mapBufferARBPtr = Wgl.wglGetProcAddress("glMapBufferARB");
            unmapBufferARBPtr = Wgl.wglGetProcAddress("glUnmapBufferARB");
			getBufferParameterivARBptr = Wgl.wglGetProcAddress("glGetBufferParameterivARB");

            // ARB_multitexture
            activeTextureARB = Wgl.wglGetProcAddress("glActiveTextureARB");
            clientActiveTextureARB = Wgl.wglGetProcAddress("glClientActiveTextureARB");

            // ARB_vertex_program/ARB_fragment_program
            bindProgramARBPtr = Wgl.wglGetProcAddress("glBindProgramARB");
            genProgramsARBPtr = Wgl.wglGetProcAddress("glGenProgramsARB");
            deleteProgramsARBPtr = Wgl.wglGetProcAddress("glDeleteProgramsARB");
            programStringARBPtr = Wgl.wglGetProcAddress("glProgramStringARB");
            isProgramARBPtr = Wgl.wglGetProcAddress("glIsProgramARB");
            programLocalParameter4fvARBPtr = Wgl.wglGetProcAddress("glProgramLocalParameter4fvARB");

            // GL_EXT_texture3D
            texImage3DEXTPtr = Wgl.wglGetProcAddress("glTexImage3DEXT");

            // GL_ARB_texture_compression
            compressedTexImage2DARB = Wgl.wglGetProcAddress("glCompressedTexImage2DARB");

            // GL_ATI_fragment_shader
            genFragmentShadersATIptr = Wgl.wglGetProcAddress("glGenFragmentShadersATI");
            bindFragmentShaderATIptr = Wgl.wglGetProcAddress("glBindFragmentShaderATI");
            deleteFragmentShaderATIptr = Wgl.wglGetProcAddress("glDeleteFragmentShaderATI");
            beginFragmentShaderATIptr = Wgl.wglGetProcAddress("glBeginFragmentShaderATI");
            endFragmentShaderATIptr = Wgl.wglGetProcAddress("glEndFragmentShaderATI");
            setFragmentShaderConstantATIptr = Wgl.wglGetProcAddress("glSetFragmentShaderConstantATI");
            colorFragmentOp1ATIptr = Wgl.wglGetProcAddress("glColorFragmentOp1ATI");
            colorFragmentOp2ATIptr = Wgl.wglGetProcAddress("glColorFragmentOp2ATI");
            colorFragmentOp3ATIptr = Wgl.wglGetProcAddress("glColorFragmentOp3ATI");
            alphaFragmentOp1ATIptr = Wgl.wglGetProcAddress("glAlphaFragmentOp1ATI");
            alphaFragmentOp2ATIptr = Wgl.wglGetProcAddress("glAlphaFragmentOp2ATI");
            alphaFragmentOp3ATIptr = Wgl.wglGetProcAddress("glAlphaFragmentOp3ATI");
            passTexCoordATIptr = Wgl.wglGetProcAddress("glPassTexCoordATI");
            sampleMapATIptr = Wgl.wglGetProcAddress("glSampleMapATI");

            // GL_NV_register_combiner
            combinerStageParameterfvNVptr = Wgl.wglGetProcAddress("glCombinerStageParameterfvNV");

            // GL_NV_vertex_program2/GL_NV_fragment_program
            genProgramsNVptr = Wgl.wglGetProcAddress("glGenProgramsNV");
            bindProgramNVptr = Wgl.wglGetProcAddress("glBindProgramNV");
            deleteProgramsNVptr = Wgl.wglGetProcAddress("glDeleteProgramsNV");
            loadProgramNVptr = Wgl.wglGetProcAddress("glLoadProgramNV");
            programNamedParameter4fNVptr = Wgl.wglGetProcAddress("glProgramNamedParameter4fNV");
            programParameter4fvNVptr = Wgl.wglGetProcAddress("glProgramParameter4fvNV");

            // GL_EXT_secondary_color
            secondaryColorPointerEXTptr = Wgl.wglGetProcAddress("glSecondaryColorPointerEXT");

			// vertex attributes
			vertexAttribPointerARBptr = Wgl.wglGetProcAddress("glVertexAttribPointerARB");
			enableVertexAttribArrayARBptr = Wgl.wglGetProcAddress("glEnableVertexAttribArrayARB");
			disableVertexAttribArrayARBptr = Wgl.wglGetProcAddress("glDisableVertexAttribArrayARB");

			// GL_draw_range_elements
			drawRangeElementsptr = Wgl.wglGetProcAddress("glDrawRangeElements");
        }
    }

}
