using System;
using System.Collections.Specialized;
using Axiom.SubSystems.Rendering;

namespace RenderSystem_OpenGL
{
	/// <summary>
	/// Summary description for GLHelper.
	/// </summary>
	public class GLHelper
	{
		static StringCollection extensionList;

		/// <summary>
		/// 
		/// </summary>
		public static StringCollection Extensions
		{
			get 
			{
				// lazy load, first time load the extensions
				LoadExtensionList();

				return extensionList; 
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="extention"></param>
		/// <returns></returns>
		public static bool SupportsExtension(string extention)
		{
			// lazy load, first time load the extensions
			LoadExtensionList();

			// check if the extension is supported
			return extensionList.Contains(extention);
		}

		/// <summary>
		/// 
		/// </summary>
		private static void LoadExtensionList()
		{
			if(extensionList == null)
			{
				extensionList = new StringCollection();

				string allExt = OpenGLRenderer.glGetString(OpenGLRenderer.GL_EXTENSIONS);
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
		public static uint ConvertEnum(BufferUsage usage)
		{
			switch(usage)
			{
				case BufferUsage.Static:
				//case BufferUsage.StaticWriteOnly:
					return OpenGLExtensions.GL_STATIC_DRAW_ARB;

				case BufferUsage.Dynamic:
				case BufferUsage.DynamicWriteOnly:
				default:
					return OpenGLExtensions.GL_DYNAMIC_DRAW_ARB;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static uint ConvertEnum(VertexElementType type)
		{
			switch(type)
			{
				case VertexElementType.Float1:
				case VertexElementType.Float2:
				case VertexElementType.Float3:
				case VertexElementType.Float4:
					return OpenGLExtensions.GL_FLOAT;

				case VertexElementType.Short1:
				case VertexElementType.Short2:
				case VertexElementType.Short3:
				case VertexElementType.Short4:
					return OpenGLExtensions.GL_SHORT;

				case VertexElementType.Color:
					return OpenGLExtensions.GL_UNSIGNED_BYTE;
			}

			// should never reach this
			return 0;
		}

	}
}
