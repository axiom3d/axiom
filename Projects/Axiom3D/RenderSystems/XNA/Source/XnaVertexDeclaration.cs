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
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;

using Axiom.Core;
using Axiom.Graphics;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    class XnaVertexDeclaration: VertexDeclaration
    {
        #region Member variables

        protected XFG.GraphicsDevice device;
        protected XFG.VertexDeclaration xnaVertexDecl;
        protected bool needsRebuild;

        #endregion

        #region Constructors

        public XnaVertexDeclaration( XFG.GraphicsDevice device )
        {
            this.device = device;
        }

        #endregion

        #region Methods

        public override VertexElement AddElement( short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index )
        {
            VertexElement element = base.AddElement( source, offset, type, semantic, index );

            needsRebuild = true;

            return element;
        }

        public override VertexElement InsertElement( int position, short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index )
        {
            VertexElement element = base.InsertElement( position, source, offset, type, semantic, index );

            needsRebuild = true;

            return element;
        }

        public override void ModifyElement( int elemIndex, short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index )
        {
            base.ModifyElement( elemIndex, source, offset, type, semantic, index );

            needsRebuild = true;
        }


        public override void RemoveElement( VertexElementSemantic semantic, int index )
        {
            base.RemoveElement( semantic, index );

            needsRebuild = true;
        }

        public override void RemoveElement( int index )
        {
            base.RemoveElement( index );

            needsRebuild = true;
        }


        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public XFG.VertexDeclaration XnaVertexDecl
        {
            get
            {
                // rebuild declaration if things have changed
                if ( needsRebuild )
                {
                    if ( xnaVertexDecl != null )
                        xnaVertexDecl.Dispose();

                    // create elements array
                    XFG.VertexElement[] xnaElements = new XFG.VertexElement[ elements.Count ];

                    // loop through and configure each element for XFG
                    for ( int i = 0; i < elements.Count; i++ )
                    {
                        VertexElement element = (VertexElement)elements[ i ];

                        xnaElements[i].Offset = (short)element.Offset;
                        xnaElements[i].Stream = (short)element.Source;
                        xnaElements[i].VertexElementFormat = XnaHelper.ConvertEnum( element.Type );
                        xnaElements[i].VertexElementMethod = XFG.VertexElementMethod.Default;
                        xnaElements[i].VertexElementUsage = XnaHelper.ConvertEnum( element.Semantic );

                        // set usage index explicitly for diffuse and specular, use index for the rest (i.e. texture coord sets)
                        switch ( element.Semantic )
                        {
                            case VertexElementSemantic.Diffuse:
                                xnaElements[ i ].UsageIndex = 0;
                                break;

                            case VertexElementSemantic.Specular:
                                xnaElements[ i ].UsageIndex = 1;
                                break;

                            default:
                                xnaElements[ i ].UsageIndex = (byte)element.Index;
                                break;
                        } //  switch

                    } // for

                    // configure the last element to be the end
                    //xnaElements[elements.Count] = XFG.VertexElement.VertexDeclarationEnd;

                    // create the new declaration
                    xnaVertexDecl = new XFG.VertexDeclaration( device, xnaElements );

                    // reset the flag
                    needsRebuild = false;
                }

                return xnaVertexDecl;
            }
        }

        #endregion

    }
}
