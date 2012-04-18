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

using System.Text;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
    public struct TextureLayerState
    {
        #region Fields

        public TextureType TextureType;
        public TexCoordCalcMethod TexCoordCalcMethod;
        public LayerBlendModeEx LayerBlendMode;
        public int CoordIndex;

        #endregion Fields

        #region Object Implementation

        public override bool Equals( object obj )
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            int layerBlendHash;
            if ( LayerBlendMode == null )
                layerBlendHash = 0;
            else
                layerBlendHash = LayerBlendMode.GetHashCode();

            return TextureType.GetHashCode() ^ TexCoordCalcMethod.GetHashCode() ^ layerBlendHash ^
                   CoordIndex.GetHashCode();
        }

        public override string ToString()
        {
            return ( new StringBuilder() )
                .AppendFormat( "[ TextureType: {0}; TexCoordCalcMethod : {1}; LayerBlendMode : {2}; CoordIndex : {3} ]",
                               TextureType,
                               TexCoordCalcMethod,
                               LayerBlendMode,
                               CoordIndex ).ToString();
        }

        #endregion Object Implementation
    }
}