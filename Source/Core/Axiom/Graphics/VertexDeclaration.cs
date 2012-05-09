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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	This class declares the format of a set of vertex inputs, which
	/// 	can be issued to the rendering API through a <see cref="RenderOperation"/>.
	/// </summary>
	/// <remarks>
	///		You should be aware that the ordering and structure of the
	///		VertexDeclaration can be very important on DirectX with older
	///		cards, so if you want to maintain maximum compatibility with
	///		all render systems and all cards you should be careful to follow these
	///		rules:<ol>
	///		<li>VertexElements should be added in the following order, and the order of the
	///		elements within a shared buffer should be as follows:
	///		position, blending weights, normals, diffuse colours, specular colours,
	///		texture coordinates (in order, with no gaps)</li>
	///		<li>You must not have unused gaps in your buffers which are not referenced
	///		by any <see cref="VertexElement"/></li>
	///		<li>You must not cause the buffer &amp; offset settings of 2 VertexElements to overlap</li>
	///		</ol>
	///		Whilst GL and more modern graphics cards in D3D will allow you to defy these rules,
	///		sticking to them will ensure that your buffers have the maximum compatibility.
	///		<p/>
	///		Like the other classes in this functional area, these declarations should be created and
	///		destroyed using the <see cref="HardwareBufferManager"/>.
	/// </remarks>
	public class VertexDeclaration : DisposableObject, ICloneable
	{
		#region Fields

		/// <summary>
		///     List of elements that make up this declaration.
		/// </summary>
		protected List<VertexElement> elements = new List<VertexElement>();

		#endregion Fields

		#region Construction and Destruction

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///     Adds a new VertexElement to this declaration.
		/// </summary>
		/// <remarks>
		///     This method adds a single element (positions, normals etc) to the
		///     vertex declaration. <b>Please read the information in <see cref="VertexDeclaration"/> about
		///     the importance of ordering and structure for compatibility with older D3D drivers</b>.
		/// </remarks>
		/// <param name="source">
		///     The binding index of HardwareVertexBuffer which will provide the source for this element.
		/// </param>
		/// <param name="offset">The offset in bytes where this element is located in the buffer.</param>
		/// <param name="type">The data format of the element (3 floats, a color etc).</param>
		/// <param name="semantic">The meaning of the data (position, normal, diffuse color etc).</param>
		public VertexElement AddElement( short source, int offset, VertexElementType type, VertexElementSemantic semantic )
		{
			return AddElement( source, offset, type, semantic, 0 );
		}

		/// <summary>
		///     Adds a new VertexElement to this declaration.
		/// </summary>
		/// <remarks>
		///     This method adds a single element (positions, normals etc) to the
		///     vertex declaration. <b>Please read the information in <see cref="VertexDeclaration"/> about
		///     the importance of ordering and structure for compatibility with older D3D drivers</b>.
		/// </remarks>
		/// <param name="source">
		///     The binding index of HardwareVertexBuffer which will provide the source for this element.
		/// </param>
		/// <param name="offset">The offset in bytes where this element is located in the buffer.</param>
		/// <param name="type">The data format of the element (3 floats, a color etc).</param>
		/// <param name="semantic">The meaning of the data (position, normal, diffuse color etc).</param>
		/// <param name="index">Optional index for multi-input elements like texture coordinates.</param>
		public virtual VertexElement AddElement( short source, int offset, VertexElementType type,
		                                         VertexElementSemantic semantic, int index )
		{
			var element = new VertexElement( source, offset, type, semantic, index );
			this.elements.Add( element );
			return element;
		}

		/// <summary>
		///     Finds a <see cref="VertexElement"/> with the given semantic, and index if there is more than
		///     one element with the same semantic.
		/// </summary>
		/// <param name="semantic">Semantic to search for.</param>
		/// <returns>If the element is not found, this method returns null.</returns>
		public VertexElement FindElementBySemantic( VertexElementSemantic semantic )
		{
			// call overload with a default of index 0
			return FindElementBySemantic( semantic, 0 );
		}

		/// <summary>
		///     Finds a <see cref="VertexElement"/> with the given semantic, and index if there is more than
		///     one element with the same semantic.
		/// </summary>
		/// <param name="semantic">Semantic to search for.</param>
		/// <param name="index">Index of item to looks for using the supplied semantic (applicable to tex coords and colors).</param>
		/// <returns>If the element is not found, this method returns null.</returns>
		public virtual VertexElement FindElementBySemantic( VertexElementSemantic semantic, short index )
		{
			for ( var i = 0; i < this.elements.Count; i++ )
			{
				var element = this.elements[ i ];

				// do they match?
				if ( element.Semantic == semantic && element.Index == index )
				{
					return element;
				}
			}

			// not found
			return null;
		}

		/// <summary>
		///     Gets a list of elements which use a given source.
		/// </summary>
		public virtual List<VertexElement> FindElementBySource( short source )
		{
			var rv = new List<VertexElement>();

			for ( var i = 0; i < this.elements.Count; i++ )
			{
				var element = this.elements[ i ];

				// do they match?
				if ( element.Source == source )
				{
					rv.Add( element );
				}
			}

			// return the list
			return rv;
		}

		/// <summary>
		///		Gets the <see cref="VertexElement"/> at the specified index.
		/// </summary>
		/// <param name="index">Index of the element to retrieve.</param>
		/// <returns>Element at the requested index.</returns>
		public VertexElement GetElement( int index )
		{
			Debug.Assert( index < this.elements.Count && index >= 0, "Element index out of bounds." );

			return this.elements[ index ];
		}

		/// <summary>
		///  Returns the entire vertexelement list
		/// </summary>
		public List<VertexElement> Elements
		{
			get
			{
				return this.elements;
			}
		}

		public class VertexElementLess : IComparer<Axiom.Graphics.VertexElement>
		{
			// Sort routine for VertexElement

			#region IComparer Members

			public int Compare( VertexElement e1, VertexElement e2 )
			{
				// Sort by source first

				if ( e1 == null && e2 == null )
				{
					return 0;
				}

				if ( e1 == null )
				{
					return -1;
				}
				else if ( e2 == null )
				{
					return 1;
				}

				if ( e1.Source < e2.Source )
				{
					return -1;
				}
				else if ( e1.Source == e2.Source )
				{
					// Use ordering of semantics to sort
					if ( e1.Semantic < e2.Semantic )
					{
						return -1;
					}
					else if ( e1.Semantic == e2.Semantic )
					{
						// Use index to sort
						if ( e1.Index < e2.Index )
						{
							return -1;
						}
						else if ( e1.Index == e2.Index )
						{
							return 0;
						}
					}
				}
				return 1;
			}

			#endregion IComparer Members
		}

		public void Sort()
		{
			var compareFunction = new VertexElementLess();
			this.elements.Sort( compareFunction );
		}

		public VertexDeclaration GetAutoOrganizedDeclaration( bool skeletalAnimation, bool vertexAnimation )
		{
			var newDecl = (VertexDeclaration)Clone();
			// Set all sources to the same buffer (for now)
			var elems = newDecl.Elements;

			var c = 0;

			for ( var i = 0; i < elems.Count; i++, ++c )
			{
				var elem = elems[ i ];
				newDecl.ModifyElement( c, 0, 0, elem.Type, elem.Semantic, elem.Index );
			}

			newDecl.Sort();

			// Now sort out proper buffer assignments and offsets
			var offset = 0;
			c = 0;
			short buffer = 0;
			var prevSemantic = VertexElementSemantic.Position;

			for ( var i = 0; i < elems.Count; i++, ++c )
			{
				var elem = elems[ i ];
				var splitWithPrev = false;
				var splitWithNext = false;
				switch ( elem.Semantic )
				{
					case VertexElementSemantic.Position:
						// For morph animation, we need positions on their own
						splitWithPrev = vertexAnimation;
						splitWithNext = vertexAnimation;
						break;
					case VertexElementSemantic.Normal:
						// Normals can't sharing with blend weights/indices
						splitWithPrev = ( prevSemantic == VertexElementSemantic.BlendWeights ||
						                  prevSemantic == VertexElementSemantic.BlendIndices );
						// All animated meshes have to split after normal
						splitWithNext = ( skeletalAnimation || vertexAnimation );
						break;
					case VertexElementSemantic.BlendWeights:
						// Blend weights/indices can be sharing with their own buffer only
						splitWithPrev = true;
						break;
					case VertexElementSemantic.BlendIndices:
						// Blend weights/indices can be sharing with their own buffer only
						splitWithNext = true;
						break;
				}

				if ( splitWithPrev && offset > 0 )
				{
					++buffer;
					offset = 0;
				}

				prevSemantic = elem.Semantic;

				newDecl.ModifyElement( c, buffer, offset, elem.Type, elem.Semantic, elem.Index );

				if ( splitWithNext )
				{
					++buffer;
					offset = 0;
				}
				else
				{
					offset += elem.Size;
				}
			}

			return newDecl;
		}

		/// <summary>
		/// Gets the vertex size defined by this declaration.
		/// </summary>
		public virtual int GetVertexSize()
		{
			var size = 0;

			for ( var i = 0; i < this.elements.Count; i++ )
			{
				var element = this.elements[ i ];
				size += element.Size;
			}
			return size;
		}

		/// <summary>
		///     Gets the vertex size defined by this declaration for a given source.
		/// </summary>
		/// <param name="source">The buffer binding index for which to get the vertex size.</param>
		public virtual int GetVertexSize( short source )
		{
			var size = 0;

			for ( var i = 0; i < this.elements.Count; i++ )
			{
				var element = this.elements[ i ];

				// do they match?
				if ( element.Source == source )
				{
					size += element.Size;
				}
			}

			// return the size
			return size;
		}

		/// <summary>
		///		Inserts a new <see cref="VertexElement"/> at a given position in this declaration.
		/// </summary>
		/// <remarks>
		///		This method adds a single element (positions, normals etc) at a given position in this
		///		vertex declaration. <b>Please read the information in VertexDeclaration about
		///		the importance of ordering and structure for compatibility with older D3D drivers</b>.
		/// </remarks>
		/// <param name="position">Position to insert into.</param>
		/// <param name="source">The binding index of HardwareVertexBuffer which will provide the source for this element.</param>
		/// <param name="offset">The offset in bytes where this element is located in the buffer.</param>
		/// <param name="type">The data format of the element (3 floats, a color, etc).</param>
		/// <param name="semantic">The meaning of the data (position, normal, diffuse color etc).</param>
		/// <returns>A reference to the newly created element.</returns>
		public VertexElement InsertElement( int position, short source, int offset, VertexElementType type,
		                                    VertexElementSemantic semantic )
		{
			return InsertElement( position, source, offset, type, semantic, 0 );
		}

		/// <summary>
		///		Inserts a new <see cref="VertexElement"/> at a given position in this declaration.
		/// </summary>
		/// <remarks>
		///		This method adds a single element (positions, normals etc) at a given position in this
		///		vertex declaration. <b>Please read the information in VertexDeclaration about
		///		the importance of ordering and structure for compatibility with older D3D drivers</b>.
		/// </remarks>
		/// <param name="position">Position to insert into.</param>
		/// <param name="source">The binding index of HardwareVertexBuffer which will provide the source for this element.</param>
		/// <param name="offset">The offset in bytes where this element is located in the buffer.</param>
		/// <param name="type">The data format of the element (3 floats, a color, etc).</param>
		/// <param name="semantic">The meaning of the data (position, normal, diffuse color etc).</param>
		/// <param name="index">Optional index for multi-input elements like texture coordinates.</param>
		/// <returns>A reference to the newly created element.</returns>
		public virtual VertexElement InsertElement( int position, short source, int offset, VertexElementType type,
		                                            VertexElementSemantic semantic, int index )
		{
			if ( position >= this.elements.Count )
			{
				return AddElement( source, offset, type, semantic, index );
			}

			var element = new VertexElement( source, offset, type, semantic, index );

			this.elements.Insert( position, element );

			return element;
		}

		/// <summary>
		///		Gets the <see cref="VertexElement"/> at the specified index.
		/// </summary>
		/// <param name="index">Index of the element to retrieve.</param>
		/// <returns>Element at the requested index.</returns>
		public virtual void RemoveElement( int index )
		{
			Debug.Assert( index < this.elements.Count && index >= 0, "Element index out of bounds." );

			this.elements.RemoveAt( index );
		}

		/// <summary>
		///		Removes all <see cref="VertexElement"/> from the declaration.
		/// </summary>
		public virtual void RemoveAllElements()
		{
			this.elements.Clear();
		}

		/// <summary>
		///		Modifies the definition of a <see cref="VertexElement"/>.
		/// </summary>
		/// <param name="elemIndex">Index of the element to modify.</param>
		/// <param name="source">Source of the element.</param>
		/// <param name="offset">Offset of the element.</param>
		/// <param name="type">Type of the element.</param>
		/// <param name="semantic">Semantic of the element.</param>
		public void ModifyElement( int elemIndex, short source, int offset, VertexElementType type,
		                           VertexElementSemantic semantic )
		{
			ModifyElement( elemIndex, source, offset, type, semantic, 0 );
		}

		/// <summary>
		///		Modifies the definition of a <see cref="VertexElement"/>.
		/// </summary>
		/// <param name="elemIndex">Index of the element to modify.</param>
		/// <param name="source">Source of the element.</param>
		/// <param name="offset">Offset of the element.</param>
		/// <param name="type">Type of the element.</param>
		/// <param name="semantic">Semantic of the element.</param>
		/// <param name="index">Usage index of the element.</param>
		public virtual void ModifyElement( int elemIndex, short source, int offset, VertexElementType type,
		                                   VertexElementSemantic semantic, int index )
		{
			this.elements[ elemIndex ] = new VertexElement( source, offset, type, semantic, index );
		}

		/// <summary>
		///		Remove the element with the given semantic.
		/// </summary>
		/// <remarks>
		///		For elements that have usage indexes, the default of 0 is used.
		/// </remarks>
		/// <param name="semantic">Semantic to remove.</param>
		public void RemoveElement( VertexElementSemantic semantic )
		{
			RemoveElement( semantic, 0 );
		}

		/// <summary>
		///		Remove the element with the given semantic and usage index.
		/// </summary>
		/// <param name="semantic">Semantic to remove.</param>
		/// <param name="index">Usage index to remove, typically only applies to tex coords.</param>
		public virtual void RemoveElement( VertexElementSemantic semantic, int index )
		{
			for ( var i = this.elements.Count - 1; i >= 0; i-- )
			{
				var element = this.elements[ i ];

				if ( element.Semantic == semantic && element.Index == index )
				{
					// we have a winner!
					this.elements.RemoveAt( i );
				}
			}
		}

		/// <summary>
		///     Tests equality of 2 <see cref="VertexElement"/> objects.
		/// </summary>
		/// <param name="left">A <see cref="VertexElement"/></param>
		/// <param name="right">A <see cref="VertexElement"/></param>
		/// <returns>true if equal, false otherwise.</returns>
		public static bool operator ==( VertexDeclaration left, VertexDeclaration right )
		{
			// If both are null, or both are same instance, return true.
			if ( System.Object.ReferenceEquals( left, right ) )
			{
				return true;
			}

			// If one is null, but not both, return false.
			if ( ( (object)left == null ) || ( (object)right == null ) )
			{
				return false;
			}

			// if element lists are different sizes, they can't be equal
			if ( left.elements.Count != right.elements.Count )
			{
				return false;
			}

			for ( var i = 0; i < right.elements.Count; i++ )
			{
				var a = left.elements[ i ];
				var b = right.elements[ i ];

				// if they are not equal, this declaration differs
				if ( !( a == b ) )
				{
					return false;
				}
			}

			// if we got thise far, they are equal
			return true;
		}

		/// <summary>
		///     Tests in-equality of 2 <see cref="VertexElement"/> objects.
		/// </summary>
		/// <param name="left">A <see cref="VertexElement"/></param>
		/// <param name="right">A <see cref="VertexElement"/></param>
		/// <returns>true if not equal, false otherwise.</returns>
		public static bool operator !=( VertexDeclaration left, VertexDeclaration right )
		{
			return !( left == right );
		}

		#endregion Methods

		#region Properties

		/// <summary>
		///     Gets the number of elements in the declaration.
		/// </summary>
		public int ElementCount
		{
			get
			{
				return this.elements.Count;
			}
		}

		public VertexElement this[ int index ]
		{
			get
			{
				return (VertexElement)this.elements[ index ];
			}
		}

		#endregion Properties

		#region Object overloads

		/// <summary>
		///    Override to determine equality between 2 VertexDeclaration objects,
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals( object obj )
		{
			var decl = obj as VertexDeclaration;

			return ( decl == this );
		}

		/// <summary>
		///    Override GetHashCode.
		/// </summary>
		/// <remarks>
		///    Done mainly to quash warnings, no real need for it.
		/// </remarks>
		/// <returns></returns>
		// TODO: Does this need to be implemented, dont think we are stuffing these into hashtables.
		public override int GetHashCode()
		{
			return 0;
		}

		#endregion Object overloads

		#region ICloneable Members

		/// <summary>
		///     Clones this declaration, including a copy of all <see cref="VertexElement"/> objects this declaration holds.
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			var clone = HardwareBufferManager.Instance.CreateVertexDeclaration();

			for ( var i = 0; i < this.elements.Count; i++ )
			{
				var element = (VertexElement)this.elements[ i ];
				clone.AddElement( element.Source, element.Offset, element.Type, element.Semantic, element.Index );
			}

			return clone;
		}

		/// <summary>
		/// Clones this declaration, including a copy of all <see cref="VertexElement"/> objects this declaration holds for the given source.
		/// </summary>
		/// <param name="source">the source elements to clone</param>
		/// <returns>a new <see cref="VertexDeclaration"/> containing only those <see cref="VertexElement"/>s</returns>
		/// <remarks>all elements in the cloned <see cref="VertexDeclaration"/> will have a source of 0.</remarks>
		public VertexDeclaration Clone( short source )
		{
			var clone = HardwareBufferManager.Instance.CreateVertexDeclaration();
			var sourceElements = FindElementBySource( source );

			for ( var i = 0; i < sourceElements.Count; i++ )
			{
				var element = (VertexElement)sourceElements[ i ];
				clone.AddElement( 0, element.Offset, element.Type, element.Semantic, element.Index );
			}

			return clone;
		}

		#endregion ICloneable Members

		#region IDisposable Implementation

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	}
}