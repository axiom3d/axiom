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
//     <id value="$Id: Lists.cs -1   $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Graphics.Collections
{
	/// <summary>
	///     Generics: List<VertexElement>
	/// </summary>
	public class VertexElementList : List<VertexElement>
	{
	}

	/// <summary>
	///     Generics: List<TextureEffect>
	/// </summary>
	public class TextureEffectList : List<TextureEffect>
	{
	}

	/// <summary>
	///     Generics: List<RenderTexture>
	/// </summary>
	public class RenderTextureList : List<RenderTexture>
	{
	}

	/// <summary>
	///     Generics: List<Pass>
	/// </summary>
	public class PassList : List<Pass>
	{
	}

	/// <summary>
	///     Generics: List<Technique>
	/// </summary>
	public class TechniqueList : List<Technique>
	{
	}

	/// <summary>
	///     Generics: List<TextureUnitState>
	/// </summary>
	public class TextureUnitStateList : List<TextureUnitState>
	{
	}

	/// <summary>
	///     Generics: List<AutoConstantEntry>
	/// </summary>
	public class AutoConstantEntryList : List<GpuProgramParameters.AutoConstantEntry>
	{
	}

	/// <summary>
	///     Generics: List<AutoConstantEntry>
	/// </summary>
	public class FloatConstantEntryList : List<GpuProgramParameters.FloatConstantEntry>
	{
		public void Resize( int size )
		{
			while ( this.Count < size )
			{
				Add( new GpuProgramParameters.FloatConstantEntry() );
			}
		}
	}

	/// <summary>
	///     Generics: List<AutoConstantEntry>
	/// </summary>
	public class IntConstantEntryList : List<GpuProgramParameters.IntConstantEntry>
	{
		public void Resize( int size )
		{
			while ( this.Count < size )
			{
				Add( new GpuProgramParameters.IntConstantEntry() );
			}
		}
	}

	/// <summary>
	///     Generics: List<IRenderable>
	/// </summary>
	public class RenderableList : List<IRenderable>
	{
	}

	/// <summary>
	///     Generics: List<EdgeData.Triangle>
	/// </summary>
	public class TriangleList : List<EdgeData.Triangle>
	{
	}

	/// <summary>
	///     Generics: List<EdgeData.Edge>
	/// </summary>
	public class EdgeList : List<EdgeData.Edge>
	{
	}

	/// <summary>
	///     Generics: List<EdgeData.EdgeGroup>
	/// </summary>
	public class EdgeGroupList : List<EdgeData.EdgeGroup>
	{
	}

	/// <summary>
	///     Generics: List<VertexData>
	/// </summary>
	public class VertexDataList : List<VertexData>
	{
	}

	/// <summary>
	///     Generics: List<IndexData>
	/// </summary>
	public class IndexDataList : List<IndexData>
	{
	}

	/// <summary>
	///     Generics: List<ShadowRenderable>
	/// </summary>
	public class ShadowRenderableList : List<ShadowRenderable>
	{
	}

	/// <summary>
	///		Generics: List<RenderOperation>
	/// </summary>
	public class OperationTypeList : List<OperationType>
	{
	}
}
