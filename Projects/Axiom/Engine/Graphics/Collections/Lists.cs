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
	///     Generics: List&lt;VertexElement&gt;
	/// </summary>
	public class VertexElementList : List<VertexElement> {}

	/// <summary>
	///     Generics: List&lt;TextureEffect&lt;
	/// </summary>
	public class TextureEffectList : List<TextureEffect> {}

	/// <summary>
	///     Generics: List&lt;RenderTexture&gt;
	/// </summary>
	public class RenderTextureList : List<RenderTexture> {}

	/// <summary>
	///     Generics: List&lt;Pass&gt;
	/// </summary>
	public class PassList : List<Pass> {}

	/// <summary>
	///     Generics: List&lt;Technique&gt;
	/// </summary>
	public class TechniqueList : List<Technique> {}

	/// <summary>
	///     Generics: List&lt;TextureUnitState&gt;
	/// </summary>
	public class TextureUnitStateList : List<TextureUnitState> {}

	/// <summary>
	///     Generics: List&lt;AutoConstantEntry&gt;
	/// </summary>
	public class AutoConstantEntryList : List<GpuProgramParameters.AutoConstantEntry> {}

	/// <summary>
	///     Generics: List&gt;AutoConstantEntry&lt;
	/// </summary>
	public class FloatConstantEntryList : List<GpuProgramParameters.FloatConstantEntry>
	{
		public void Resize( int size )
		{
			while( this.Count < size )
			{
				Add( new GpuProgramParameters.FloatConstantEntry() );
			}
		}
	}

	/// <summary>
	///     Generics: List&lt;AutoConstantEntry&gt;
	/// </summary>
	public class IntConstantEntryList : List<GpuProgramParameters.IntConstantEntry>
	{
		public void Resize( int size )
		{
			while( this.Count < size )
			{
				Add( new GpuProgramParameters.IntConstantEntry() );
			}
		}
	}

	/// <summary>
	///     Generics: List&lt;IRenderable&gt;
	/// </summary>
	public class RenderableList : List<IRenderable> {}

	/// <summary>
	///     Generics: List&lt;EdgeData.Triangle&gt;
	/// </summary>
	public class TriangleList : List<EdgeData.Triangle> {}

	/// <summary>
	///     Generics: List&lt;EdgeData.Edge&gt;
	/// </summary>
	public class EdgeList : List<EdgeData.Edge> {}

	/// <summary>
	///     Generics: List&lt;EdgeData.EdgeGroup&gt;
	/// </summary>
	public class EdgeGroupList : List<EdgeData.EdgeGroup> {}

	/// <summary>
	///     Generics: List&lt;VertexData&gt;
	/// </summary>
	public class VertexDataList : List<VertexData> {}

	/// <summary>
	///     Generics: List&lt;IndexData&gt;
	/// </summary>
	public class IndexDataList : List<IndexData> {}

	/// <summary>
	///     Generics: List&lt;ShadowRenderable&gt;
	/// </summary>
	public class ShadowRenderableList : List<ShadowRenderable> {}

	/// <summary>
	///		Generics: List&lt;RenderOperation>&gt;
	/// </summary>
	public class OperationTypeList : List<OperationType> {}
}
