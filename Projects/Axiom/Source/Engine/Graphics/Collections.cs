#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    ///     Generics: List<VertexElement>
    /// </summary>
    public class VertexElementList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<TextureEffect>
    /// </summary>
    public class TextureEffectList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<RenderTarget>
    /// </summary>
    public class RenderTargetList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<RenderTexture>
    /// </summary>
    public class RenderTextureList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<Pass>
    /// </summary>
    public class PassList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<Technique>
    /// </summary>
    public class TechniqueList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<TextureUnitState>
    /// </summary>
    public class TextureUnitStateList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<AutoConstantEntry>
    /// </summary>
    public class AutoConstantEntryList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<AutoConstantEntry>
    /// </summary>
    public class FloatConstantEntryList : ArrayList
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
    public class IntConstantEntryList : ArrayList
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
    public class RenderableList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<EdgeData.Triangle>
    /// </summary>
    public class TriangleList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<EdgeData.Edge>
    /// </summary>
    public class EdgeList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<EdgeGroup>
    /// </summary>
    public class EdgeGroupList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<VertexData>
    /// </summary>
    public class VertexDataList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<IndexData>
    /// </summary>
    public class IndexDataList : ArrayList
    {
    }

    /// <summary>
    ///     Generics: List<ShadowRenderable>
    /// </summary>
    public class ShadowRenderableList : ArrayList
    {
    }

    /// <summary>
    ///		Generics: List<RenderOperation>
    /// </summary>
    public class OperationTypeList : ArrayList
    {
        public void Add( OperationType type )
        {
            base.Add( type );
        }

        public new OperationType this[ int index ]
        {
            get
            {
                return (OperationType)base[ index ];
            }
            set
            {
                base[ index ] = value;
            }
        }
    }
}
