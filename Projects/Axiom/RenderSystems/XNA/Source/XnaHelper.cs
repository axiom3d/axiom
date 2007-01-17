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
using System.Collections;
using System.Collections.Generic;

using Axiom.Graphics;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

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

            foreach ( XFG.GraphicsAdapter adapterDetails in XFG.GraphicsAdapter.Adapters )
            {

                Driver driver = new Driver( adapterDetails );

                int lastWidth = 0, lastHeight = 0;
                XFG.SurfaceFormat lastFormat = 0;

                foreach ( XFG.DisplayMode mode in adapterDetails.SupportedDisplayModes )
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

        public static XFG.Color ConvertColorEx( Axiom.Core.ColorEx color )
        {
            System.Drawing.Color c = color.ToColor();
            return new XFG.Color( c.R, c.G, c.B, c.A );
        }

        #region Enumeration Conversions

        public static XFG.VertexElementFormat ConvertEnum( VertexElementType type )
        {
            // we only need to worry about a few types with D3D
            switch ( type )
            {
                case VertexElementType.Color:
                    return XFG.VertexElementFormat.Color;

                case VertexElementType.Float1:
                    return XFG.VertexElementFormat.Single;

                case VertexElementType.Float2:
                    return XFG.VertexElementFormat.Vector2;

                case VertexElementType.Float3:
                    return XFG.VertexElementFormat.Vector3;

                case VertexElementType.Float4:
                    return XFG.VertexElementFormat.Vector4;

                case VertexElementType.Short2:
                    return XFG.VertexElementFormat.Short2;

                case VertexElementType.Short4:
                    return XFG.VertexElementFormat.Short4;

                case VertexElementType.UByte4:
                    return XFG.VertexElementFormat.Byte4;

            } // switch

            // keep the compiler happy
            return XFG.VertexElementFormat.Vector3;
        }

        public static XFG.ResourceUsage ConvertEnum( BufferUsage usage )
        {
            XFG.ResourceUsage xnaUsage = 0;

            //if ( usage == BufferUsage.Dynamic || usage == BufferUsage.DynamicWriteOnly )
            //    xnaUsage |= XFG.ResourceUsage.ResolveTarget;

            //if ( usage == BufferUsage.WriteOnly || usage == BufferUsage.StaticWriteOnly || usage == BufferUsage.DynamicWriteOnly )
            //    xnaUsage |= XFG.ResourceUsage.WriteOnly;

            return xnaUsage;
        }

        public static XFG.VertexElementUsage ConvertEnum( VertexElementSemantic semantic )
        {
            switch ( semantic )
            {
                case VertexElementSemantic.BlendIndices:
                    return XFG.VertexElementUsage.BlendIndices;

                case VertexElementSemantic.BlendWeights:
                    return XFG.VertexElementUsage.BlendWeight;

                case VertexElementSemantic.Diffuse:
                    // index makes the difference (diffuse - 0)
                    return XFG.VertexElementUsage.Color;

                case VertexElementSemantic.Specular:
                    // index makes the difference (specular - 1)
                    return XFG.VertexElementUsage.Color;

                case VertexElementSemantic.Normal:
                    return XFG.VertexElementUsage.Normal;

                case VertexElementSemantic.Position:
                    return XFG.VertexElementUsage.Position;

                case VertexElementSemantic.TexCoords:
                    return XFG.VertexElementUsage.TextureCoordinate;

                case VertexElementSemantic.Binormal:
                    return XFG.VertexElementUsage.Binormal;

                case VertexElementSemantic.Tangent:
                    return XFG.VertexElementUsage.Tangent;
            } // switch

            // keep the compiler happy
            return XFG.VertexElementUsage.Position;
        }

        public static XnaTextureType ConvertEnum( TextureType type )
        {
            switch ( type )
            {
                case TextureType.OneD:
                case TextureType.TwoD:
                    return XnaTextureType.Normal;
                case TextureType.CubeMap:
                    return XnaTextureType.Cube;
                case TextureType.ThreeD:
                    return XnaTextureType.Volume;
            }

            return XnaTextureType.None;
        }

        /// <summary>
        ///    Converts our CompareFunction enum to the D3D.Compare equivalent.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static XFG.CompareFunction ConvertEnum( CompareFunction func )
        {
            switch ( func )
            {
                case CompareFunction.AlwaysFail:
                    return XFG.CompareFunction.Never;

                case CompareFunction.AlwaysPass:
                    return XFG.CompareFunction.Always;

                case CompareFunction.Equal:
                    return XFG.CompareFunction.Equal;

                case CompareFunction.Greater:
                    return XFG.CompareFunction.Greater;

                case CompareFunction.GreaterEqual:
                    return XFG.CompareFunction.GreaterEqual;

                case CompareFunction.Less:
                    return XFG.CompareFunction.Less;

                case CompareFunction.LessEqual:
                    return XFG.CompareFunction.LessEqual;

                case CompareFunction.NotEqual:
                    return XFG.CompareFunction.NotEqual;
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static XFG.FogMode ConvertEnum( FogMode mode )
        {
            // convert the fog mode value
            switch ( mode )
            {
                case FogMode.Exp:
                    return XFG.FogMode.Exponent;

                case FogMode.Exp2:
                    return XFG.FogMode.ExponentSquared;

                case FogMode.Linear:
                    return XFG.FogMode.Linear;
            } // switch

            return 0;
        }

        public static XFG.TextureAddressMode ConvertEnum( TextureAddressing type )
        {
            // convert from ours to D3D
            switch ( type )
            {
                case TextureAddressing.Wrap:
                    return XFG.TextureAddressMode.Wrap;

                case TextureAddressing.Mirror:
                    return XFG.TextureAddressMode.Mirror;

                case TextureAddressing.Clamp:
                    return XFG.TextureAddressMode.Clamp;
            } // end switch

            return 0;
        }

        /// <summary>
        ///		Helper method to convert Axiom scene blend factors to D3D
        /// </summary>
        /// <param name="factor"></param>
        /// <returns></returns>
        public static XFG.Blend ConvertEnum( SceneBlendFactor factor )
        {
            XFG.Blend xnaBlend = 0;

            switch ( factor )
            {
                case SceneBlendFactor.One:
                    xnaBlend = XFG.Blend.One;
                    break;
                case SceneBlendFactor.Zero:
                    xnaBlend = XFG.Blend.Zero;
                    break;
                case SceneBlendFactor.DestColor:
                    xnaBlend = XFG.Blend.DestinationColor;
                    break;
                case SceneBlendFactor.SourceColor:
                    xnaBlend = XFG.Blend.SourceColor;
                    break;
                case SceneBlendFactor.OneMinusDestColor:
                    xnaBlend = XFG.Blend.InverseDestinationColor;
                    break;
                case SceneBlendFactor.OneMinusSourceColor:
                    xnaBlend = XFG.Blend.InverseSourceColor;
                    break;
                case SceneBlendFactor.DestAlpha:
                    xnaBlend = XFG.Blend.DestinationAlpha;
                    break;
                case SceneBlendFactor.SourceAlpha:
                    xnaBlend = XFG.Blend.SourceAlpha;
                    break;
                case SceneBlendFactor.OneMinusDestAlpha:
                    xnaBlend = XFG.Blend.InverseDestinationAlpha;
                    break;
                case SceneBlendFactor.OneMinusSourceAlpha:
                    xnaBlend = XFG.Blend.InverseSourceAlpha;
                    break;
            }

            return xnaBlend;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <param name="caps"></param>
        /// <param name="texType"></param>
        /// <returns></returns>
        public static XFG.TextureFilter ConvertEnum( FilterType type, FilterOptions options, XFG.GraphicsDeviceCapabilities devCaps, XnaTextureType texType )
        {
            // setting a default val here to keep compiler from complaining about using unassigned value types
            XFG.GraphicsDeviceCapabilities.FilterCaps filterCaps = devCaps.TextureFilterCapabilities;

            switch ( texType )
            {
                case XnaTextureType.Normal:
                    filterCaps = devCaps.TextureFilterCapabilities;
                    break;
                case XnaTextureType.Cube:
                    filterCaps = devCaps.CubeTextureFilterCapabilities;
                    break;
                case XnaTextureType.Volume:
                    filterCaps = devCaps.VolumeTextureFilterCapabilities;
                    break;
            }

            switch ( type )
            {
                case FilterType.Min:
                    {
                        switch ( options )
                        {
                            case FilterOptions.Anisotropic:
                                if ( filterCaps.SupportsMinifyAnisotropic )
                                {
                                    return XFG.TextureFilter.Anisotropic;
                                }
                                else
                                {
                                    return XFG.TextureFilter.Linear;
                                }

                            case FilterOptions.Linear:
                                if ( filterCaps.SupportsMinifyLinear )
                                {
                                    return XFG.TextureFilter.Linear;
                                }
                                else
                                {
                                    return XFG.TextureFilter.Point;
                                }

                            case FilterOptions.Point:
                            case FilterOptions.None:
                                return XFG.TextureFilter.Point;
                        }
                        break;
                    }
                case FilterType.Mag:
                    {
                        switch ( options )
                        {
                            case FilterOptions.Anisotropic:
                                if ( filterCaps.SupportsMagnifyAnisotropic )
                                {
                                    return XFG.TextureFilter.Anisotropic;
                                }
                                else
                                {
                                    return XFG.TextureFilter.Linear;
                                }

                            case FilterOptions.Linear:
                                if ( filterCaps.SupportsMagnifyLinear )
                                {
                                    return XFG.TextureFilter.Linear;
                                }
                                else
                                {
                                    return XFG.TextureFilter.Point;
                                }

                            case FilterOptions.Point:
                            case FilterOptions.None:
                                return XFG.TextureFilter.Point;
                        }
                        break;
                    }
                case FilterType.Mip:
                    {
                        switch ( options )
                        {
                            case FilterOptions.Anisotropic:
                            case FilterOptions.Linear:
                                if ( filterCaps.SupportsMipMapLinear )
                                {
                                    return XFG.TextureFilter.Linear;
                                }
                                else
                                {
                                    return XFG.TextureFilter.Point;
                                }

                            case FilterOptions.Point:
                                if ( filterCaps.SupportsMipMapPoint )
                                {
                                    return XFG.TextureFilter.Point;
                                }
                                else
                                {
                                    return XFG.TextureFilter.None;
                                }

                            case FilterOptions.None:
                                return XFG.TextureFilter.None;
                        }
                        break;
                    }
            }

            // should never get here
            return 0;
        }

        #endregion

    }
}
