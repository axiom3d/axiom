using System;
using System.Collections;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	/// 	Summary description for VertexDeclaration.
	/// </summary>
	/// DOC
	public class VertexDeclaration
	{
		#region Member variables

		protected ArrayList elements = new ArrayList();

		#endregion

		#region Methods
		
		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public virtual void AddElement(VertexElement element)
		{
			elements.Add(element);
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public virtual VertexElement FindElementBySemantic(VertexElementSemantic semantic, ushort index)
		{
			for(int i = 0; i < elements.Count; i++)
			{
				VertexElement element = (VertexElement)elements[i];

				// do they match?
				if(element.Semantic == semantic && element.Index == index)
					return element;
			}

			// not found
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public virtual ArrayList FindElementBySource(ushort source)
		{
			ArrayList elements = new ArrayList();

			for(int i = 0; i < elements.Count; i++)
			{
				VertexElement element = (VertexElement)elements[i];

				// do they match?
				if(element.Source == source)
					elements.Add(element);
			}

			// return the list
			return elements;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// DOC
		public virtual int GetVertexSize(ushort source)
		{
			int size = 0;

			for(int i = 0; i < elements.Count; i++)
			{
				VertexElement element = (VertexElement)elements[i];

				// do they match?
				if(element.Source == source)
					size += element.Size;
			}

			// return the size
			return size;
		}

		public static bool operator == (VertexDeclaration left, VertexDeclaration right)
		{
			// if element lists are different sizes, they can't be equal
			if(left.elements.Count != right.elements.Count)
				return false;

			for(int i = 0; i < right.elements.Count; i++)
			{
				VertexDeclaration a = (VertexDeclaration)left.elements[i];
				VertexDeclaration b = (VertexDeclaration)right.elements[i];

				// if they are not equal, this declaration differs
				if(!(a == b))
					return false;
			}

			// if we got thise far, they are equal
			return true;
		}

		public static bool operator != (VertexDeclaration left, VertexDeclaration right)
		{
			return !(left == right);
		}

		#endregion
		
		#region Properties
		
		/// <summary>
		/// 
		/// </summary>
		/// DOC
		public IList Elements
		{
			get { return elements; }
		}

		#endregion

	}
}
