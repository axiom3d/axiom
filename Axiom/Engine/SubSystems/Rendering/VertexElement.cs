using System;
using System.Runtime.InteropServices;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	/// 	Summary description for VertexElement.
	/// </summary>
	/// DOC
	public class VertexElement
	{
		#region Member variables
		
		protected ushort source;
		protected int offset;
		protected VertexElementType type;
		protected VertexElementSemantic semantic;
		protected ushort index;

		#endregion
		
		#region Constructors
		
		public VertexElement(ushort source, int offset, VertexElementType type, VertexElementSemantic semantic)
		{
			this.source = source;
			this.offset = offset;
			this.type = type;
			this.semantic = semantic;
			this.index = 0; 
		}

		public VertexElement(ushort source, int offset, VertexElementType type, VertexElementSemantic semantic, ushort index)
		{
			this.source = source;
			this.offset = offset;
			this.type = type;
			this.semantic = semantic;
			this.index = index;			
		}
		
		#endregion
		
		#region Methods
		
		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public static int GetTypeSize(VertexElementType type)
		{
			unsafe
			{
				switch(type)
				{
					case VertexElementType.Color:
						return sizeof(int);

					case VertexElementType.Float1:
						return sizeof(float);						

					case VertexElementType.Float2:
						return sizeof(float) * 2;

					case VertexElementType.Float3:
						return sizeof(float) * 3;

					case VertexElementType.Float4:
						return sizeof(float) * 4;

					case VertexElementType.Short1:
						return sizeof(short);

					case VertexElementType.Short2:
						return sizeof(short) * 2;

					case VertexElementType.Short3:
						return sizeof(short) * 3;

					case VertexElementType.Short4:
						return sizeof(short) * 4;
				} // end switch

				// keep the compiler happy
				return 0;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public static int GetTypeCount(VertexElementType type)
		{
			switch(type)
			{
				case VertexElementType.Color:
					return 1;

				case VertexElementType.Float1:
					return 1;						

				case VertexElementType.Float2:
					return 2;

				case VertexElementType.Float3:
					return 3;

				case VertexElementType.Float4:
					return 4;

				case VertexElementType.Short1:
					return 1;

				case VertexElementType.Short2:
					return 2;

				case VertexElementType.Short3:
					return 3;

				case VertexElementType.Short4:
					return 4;
			} // end switch			

			// keep the compiler happy
			return 0;
		}

		/// <summary>
		///		Returns proper enum for a base type multiplied by a value.  This is helpful
		///		when working with tex coords especially since you might not know the number
		///		of texture dimensions at runtime, and when creating the VertexBuffer you will
		///		have to get a VertexElementType based on that amount to creating the VertexElement.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public static VertexElementType MultiplyTypeCount(VertexElementType type, int count)
		{
			switch(type)
			{
				case VertexElementType.Float1:
					return (VertexElementType)Enum.Parse(type.GetType(), "Float" + count, true);

				case VertexElementType.Short1:
					return (VertexElementType)Enum.Parse(type.GetType(), "Short" + count, true);
			}

			throw new Exception("Cannot multiply base vertex element type: " + type.ToString());
		}

		#endregion
		
		#region Properties

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public ushort Source
		{
			get { return source; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public int Offset
		{
			get { return offset; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public VertexElementType Type
		{
			get { return type; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public VertexElementSemantic Semantic
		{
			get { return semantic; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public ushort Index
		{
			get { return index; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public int Size
		{
			get { return GetTypeSize(type); }
		}

		#endregion

	}
}
