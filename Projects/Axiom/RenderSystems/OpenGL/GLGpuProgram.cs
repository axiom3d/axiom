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

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;

using Tao.OpenGl;

using System.Diagnostics;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// 	Specialization of vertex/fragment programs for OpenGL.
	/// </summary>
	public class GLGpuProgram : GpuProgram
	{
		#region Fields

		/// <summary>
		///    Internal OpenGL id assigned to this program.
		/// </summary>
		protected int programId;

		/// <summary>
		///    Type of this program (vertex or fragment).
		/// </summary>
		protected int programType;

		/// <summary>
		///     For use internally to store temp values for passing constants, etc.
		/// </summary>
		protected float[] tempProgramFloats = new float[4];

		#endregion Fields

		#region Constructors

		/// <summary>
		///     Constructor.
		/// </summary>
		/// <param name="name">Name of the program.</param>
		/// <param name="type">Type of program (vertex or fragment).</param>
		/// <param name="syntaxCode">Syntax code (i.e. arbvp1, etc).</param>
		internal GLGpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader ) {}

		#endregion Constructors

		#region GpuProgram Methods

		/// <summary>
		///     Called when a program needs to be bound.
		/// </summary>
		virtual public void Bind()
		{
			// do nothing
		}

		/// <summary>
		///     Called when a program needs to be unbound.
		/// </summary>
		virtual public void Unbind()
		{
			// do nothing
		}

		/// <summary>
		///     Called to create the program from source.
		/// </summary>
		protected override void LoadFromSource()
		{
			// do nothing
		}

		/// <summary>
		///     Called when a program needs to bind the supplied parameters.
		/// </summary>
		/// <param name="parms"></param>
		virtual public void BindParameters( GpuProgramParameters parms )
		{
			// do nothing
		}

		/// <summary>
		/// Bind just the pass iteration parameters
		/// </summary>
		/// <param name="parms"></param>
		virtual public void BindProgramPassIterationParameters( GpuProgramParameters parms )
		{
			// do nothing
		}

		#endregion GpuProgram Methods

		#region Properties

		/// <summary>
		///    Access to the internal program id.
		/// </summary>
		public int ProgramID { get { return programId; } }

		/// <summary>
		///    Gets the program type (GL_VERTEX_PROGRAM_ARB, GL_FRAGMENT_PROGRAM_ARB, etc);
		/// </summary>
		public int GLProgramType { get { return programType; } }

		public override int SamplerCount
		{
			get
			{
				//TODO: SamplerCount is not implimented
				return 0;
			}
		}

		#endregion Properties

		internal bool IsAttributeValid( VertexElementSemantic semantic )
		{
			switch( semantic )
			{
				case VertexElementSemantic.Diffuse:
				case VertexElementSemantic.Normal:
				case VertexElementSemantic.Position:
				case VertexElementSemantic.Specular:
				case VertexElementSemantic.TexCoords:
				default:
					Debug.Assert( false, "Shouldn't be calling this for normal attributes" );
					break;
				case VertexElementSemantic.Binormal:
				case VertexElementSemantic.BlendIndices:
				case VertexElementSemantic.BlendWeights:
				case VertexElementSemantic.Tangent:
					return true;
			}
			return false; // keeps compiler happy
		}

		internal int AttributeIndex( VertexElementSemantic semantic )
		{
			switch( semantic )
			{
				case VertexElementSemantic.Diffuse:
				case VertexElementSemantic.Normal:
				case VertexElementSemantic.Position:
				case VertexElementSemantic.Specular:
				case VertexElementSemantic.TexCoords:
				default:
					Debug.Assert( false, "Shouldn't be calling this for normal attributes" );
					break;
				case VertexElementSemantic.Binormal:
					return 7;
				case VertexElementSemantic.BlendIndices:
					return 1;
				case VertexElementSemantic.BlendWeights:
					return 14;
				case VertexElementSemantic.Tangent:
					return 15;
			}
			return 0; // keeps compiler happy
		}
	}
}
