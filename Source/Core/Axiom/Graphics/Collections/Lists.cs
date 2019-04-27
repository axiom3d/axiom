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
using System.Collections;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Graphics.Collections
{
    /// <summary>
    /// </summary>
    public class VertexElementList : List<VertexElement>
    {
    }

    /// <summary>
    /// </summary>
    public class TextureEffectList : List<TextureEffect>
    {
    }

    /// <summary>
    /// </summary>
    public class RenderTextureList : List<RenderTexture>
    {
    }

    /// <summary>
    /// </summary>
    public class PassList : List<Pass>
    {
    }

    /// <summary>
    /// </summary>
    public class TechniqueList : List<Technique>
    {
    }

    /// <summary>
    /// </summary>
    public class TextureUnitStateList : List<TextureUnitState>
    {
    }

    /// <summary>
    /// </summary>
    public class RenderableList : List<IRenderable>
    {
    }

    /// <summary>
    /// </summary>
    public class TriangleList : List<EdgeData.Triangle>
    {
    }

    /// <summary>
    /// </summary>
    public class EdgeList : List<EdgeData.Edge>
    {
    }

    /// <summary>
    /// </summary>
    public class EdgeGroupList : List<EdgeData.EdgeGroup>
    {
    }

    /// <summary>
    /// </summary>
    public class VertexDataList : List<VertexData>
    {
    }

    /// <summary>
    /// </summary>
    public class IndexDataList : List<IndexData>
    {
    }

    /// <summary>
    /// </summary>
    public class ShadowRenderableList : List<ShadowRenderable>
    {
    }

    /// <summary>
    /// </summary>
    public class OperationTypeList : List<OperationType>
    {
    }
}