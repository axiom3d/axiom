using System;
using Tao.OpenGl;

namespace Axiom.RenderSystems.OpenGL {
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

		#region GL_EXT_stencil_two_side

		private static IntPtr activeStencilFaceEXTptr;

		public static void glActiveStencilFaceEXT(int face) {
			Gl.glActiveStencilFaceEXT(activeStencilFaceEXTptr, face);
		}

		#endregion GL_EXT_stencil_two_side

		#region NV_occlusion_query

		private static IntPtr glGenOcclusionQueriesNVptr;
		private static IntPtr glDeleteOcclusionQueriesNVptr;
		private static IntPtr glBeginOcclusionQueryNVptr;
		private static IntPtr glEndOcclusionQueryNVptr;
		private static IntPtr glGetOcclusionQueryivNVptr;

		public static void glGenOcclusionQueriesNV(int n, out int id) {
			Gl.glGenOcclusionQueriesNV(glGenOcclusionQueriesNVptr, n, out id);
		}

		public static void glDeleteOcclusionQueriesNV(int n, ref int id) {
			Gl.glDeleteOcclusionQueriesNV(glDeleteOcclusionQueriesNVptr, n, ref id);
		}

		public static void glBeginOcclusionQueriesNV(int id) {
			Gl.glBeginOcclusionQueryNV(glBeginOcclusionQueryNVptr, id);
		}

		public static void glEndOcclusionQueriesNV() {
			Gl.glEndOcclusionQueryNV(glEndOcclusionQueryNVptr);
		}

		public static void glGetOcclusionQueryivNV(int id, int pname, out int val) {
			Gl.glGetOcclusionQueryivNV(glGetOcclusionQueryivNVptr, id, pname, out val);
		}

		#endregion NV_occlusion_query

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

			// GL_EXT_stencil_two_side
			activeStencilFaceEXTptr = Wgl.wglGetProcAddress("glActiveStencilFaceEXT");

			// NV_occlusion_query
			glGenOcclusionQueriesNVptr = Wgl.wglGetProcAddress("glGenOcclusionQueriesNV");
			glDeleteOcclusionQueriesNVptr = Wgl.wglGetProcAddress("glDeleteOcclusionQueriesNV");
			glBeginOcclusionQueryNVptr = Wgl.wglGetProcAddress("glBeginOcclusionQueryNV");
			glEndOcclusionQueryNVptr = Wgl.wglGetProcAddress("glEndOcclusionQueryNV");
			glGetOcclusionQueryivNVptr = Wgl.wglGetProcAddress("glGetOcclusionQueryivNV");
		}
	}
}
