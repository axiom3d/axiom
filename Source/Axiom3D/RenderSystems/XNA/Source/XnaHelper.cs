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
//     <id value="$Id: $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;

using Axiom.Graphics;

using XnaF = Microsoft.Xna.Framework;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    static class XnaHelper
    {
        /// <summary>
        ///		Enumerates driver information and their supported display modes.
        /// </summary>
        public static DriverCollection GetDriverInfo()
        {
            DriverCollection driverList = new DriverCollection();

            foreach ( XnaF.Graphics.GraphicsAdapter adapterDetails in XnaF.Graphics.GraphicsAdapter.Adapters )
            {

                Driver driver = new Driver( adapterDetails );

                int lastWidth = 0, lastHeight = 0;
                XnaF.Graphics.SurfaceFormat lastFormat = 0;

                foreach ( XnaF.Graphics.DisplayMode mode in adapterDetails.SupportedDisplayModes )
                {
                    // filter out lower resolutions, and make sure this isnt a dupe (ignore variations on refresh rate)
                    if ( ( mode.Width >= 640 && mode.Height >= 480 ) &&
                        ( ( mode.Width != lastWidth ) || mode.Height != lastHeight || mode.Format != lastFormat ) )
                    {
                        // add the video mode to the list
                        driver.VideoModes.Add( new VideoMode( mode ) );

                        // save current mode settings for comparison on the next iteraion
                        lastWidth = mode.Width;
                        lastHeight = mode.Height;
                        lastFormat = mode.Format;
                    }
                }
                driverList.Add( driver );
            }
            return driverList;
        }

        #region Enumeration Conversions

        public static XnaF.Graphics.VertexElementFormat ConvertEnum( VertexElementType type )
        {
            // we only need to worry about a few types with D3D
            switch ( type )
            {
                case VertexElementType.Color:
                    return XnaF.Graphics.VertexElementFormat.Color;

                case VertexElementType.Float1:
                    return XnaF.Graphics.VertexElementFormat.Single;

                case VertexElementType.Float2:
                    return XnaF.Graphics.VertexElementFormat.Vector2;

                case VertexElementType.Float3:
                    return XnaF.Graphics.VertexElementFormat.Vector3;

                case VertexElementType.Float4:
                    return XnaF.Graphics.VertexElementFormat.Vector4;

                case VertexElementType.Short2:
                    return XnaF.Graphics.VertexElementFormat.Short2;

                case VertexElementType.Short4:
                    return XnaF.Graphics.VertexElementFormat.Short4;

                case VertexElementType.UByte4:
                    return XnaF.Graphics.VertexElementFormat.Byte4;

            } // switch

            // keep the compiler happy
            return XnaF.Graphics.VertexElementFormat.Vector3;
        }

        public static XnaF.Graphics.ResourceUsage ConvertEnum( BufferUsage usage )
        {
            XnaF.Graphics.ResourceUsage xnaUsage = 0;

            if ( usage == BufferUsage.Dynamic || usage == BufferUsage.DynamicWriteOnly )
                xnaUsage |= XnaF.Graphics.ResourceUsage.Dynamic;

            if ( usage == BufferUsage.WriteOnly || usage == BufferUsage.StaticWriteOnly || usage == BufferUsage.DynamicWriteOnly )
                xnaUsage |= XnaF.Graphics.ResourceUsage.WriteOnly;

            return xnaUsage;
        }

        public static XnaF.Graphics.VertexElementUsage ConvertEnum( VertexElementSemantic semantic )
        {
            switch ( semantic )
            {
                case VertexElementSemantic.BlendIndices:
                    return XnaF.Graphics.VertexElementUsage.BlendIndices;

                case VertexElementSemantic.BlendWeights:
                    return XnaF.Graphics.VertexElementUsage.BlendWeight;

                case VertexElementSemantic.Diffuse:
                    // index makes the difference (diffuse - 0)
                    return XnaF.Graphics.VertexElementUsage.Color;

                case VertexElementSemantic.Specular:
                    // index makes the difference (specular - 1)
                    return XnaF.Graphics.VertexElementUsage.Color;

                case VertexElementSemantic.Normal:
                    return XnaF.Graphics.VertexElementUsage.Normal;

                case VertexElementSemantic.Position:
                    return XnaF.Graphics.VertexElementUsage.Position;

                case VertexElementSemantic.TexCoords:
                    return XnaF.Graphics.VertexElementUsage.TextureCoordinate;

                case VertexElementSemantic.Binormal:
                    return XnaF.Graphics.VertexElementUsage.Binormal;

                case VertexElementSemantic.Tangent:
                    return XnaF.Graphics.VertexElementUsage.Tangent;
            } // switch

            // keep the compiler happy
            return XnaF.Graphics.VertexElementUsage.Position;
        }

        #endregion

    }
}
