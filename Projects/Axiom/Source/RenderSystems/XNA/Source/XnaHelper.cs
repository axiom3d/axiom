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
//     <id value="$Id: D3DHelper.cs 884 2006-09-14 06:32:07Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.RenderSystems.Xna;
using XNA = Microsoft.Xna.Framework.Graphics;
//using Axiom.Core.XnaRenderSystem;

#endregion Namespace Declarations

//big mess, tried to implement as much as possible but a lot of fixed pipeline function have disapeared.

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    ///		Helper class for Xna that includes conversion functions and things that are
    ///		specific to Xna.
    /// </summary>
    public class XnaHelper
    {
        public XnaHelper()
        {
        }

        /// <summary>
        ///		Enumerates driver information and their supported display modes.
        /// </summary>
        public static Driver GetDriverInfo()
        {
            ArrayList driverList = new ArrayList();

            // get the information for the default adapter (not checking secondaries)
            XNA.GraphicsAdapter adapterInfo = XNA.GraphicsAdapter.Adapters[ 0 ];
            
            Driver driver = new Driver( adapterInfo );

            int lastWidth = 0, lastHeight = 0;
            XNA.SurfaceFormat lastFormat = 0;

            foreach ( XNA.DisplayMode mode in adapterInfo.SupportedDisplayModes)
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

            return driver;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <param name="caps"></param>
        /// <param name="texType"></param>
        /// <returns></returns>
        public static XNA.TextureFilter ConvertEnum( FilterType type, FilterOptions options, XNA.GraphicsDeviceCapabilities devCaps, D3DTexType texType )
        {
            // setting a default val here to keep compiler from complaining about using unassigned value types
            XNA.GraphicsDeviceCapabilities.FilterCaps filterCaps = devCaps.TextureFilterCapabilities;

            switch ( texType )
            {
                case D3DTexType.Normal:
                    filterCaps = devCaps.TextureFilterCapabilities;
                    break;
                case D3DTexType.Cube:
                    filterCaps = devCaps.CubeTextureFilterCapabilities;
                    break;
                case D3DTexType.Volume:
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
                                if ( filterCaps.SupportsMinifyAnisotropic)
                                {
                                    return XNA.TextureFilter.Anisotropic;
                                }
                                else
                                {
                                    return XNA.TextureFilter.Linear;
                                }

                            case FilterOptions.Linear:
                                if ( filterCaps.SupportsMinifyLinear )
                                {
                                    return XNA.TextureFilter.Linear;
                                }
                                else
                                {
                                    return XNA.TextureFilter.Point;
                                }

                            case FilterOptions.Point:
                            case FilterOptions.None:
                                return XNA.TextureFilter.Point;
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
                                    return XNA.TextureFilter.Anisotropic;
                                }
                                else
                                {
                                    return XNA.TextureFilter.Linear;
                                }

                            case FilterOptions.Linear:
                                if ( filterCaps.SupportsMagnifyLinear )
                                {
                                    return XNA.TextureFilter.Linear;
                                }
                                else
                                {
                                    return XNA.TextureFilter.Point;
                                }

                            case FilterOptions.Point:
                            case FilterOptions.None:
                                return XNA.TextureFilter.Point;
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
                                    return XNA.TextureFilter.Linear;
                                }
                                else
                                {
                                    return XNA.TextureFilter.Point;
                                }

                            case FilterOptions.Point:
                                if ( filterCaps.SupportsMipMapPoint )
                                {
                                    return XNA.TextureFilter.Point;
                                }
                                else
                                {
                                    return XNA.TextureFilter.None;
                                }

                            case FilterOptions.None:
                                return XNA.TextureFilter.None;
                        }
                        break;
                    }
            }

            // should never get here
            return 0;
        }

        /// <summary>
        ///		Static method for converting LayerBlendOperationEx enum values to the Direct3D 
        ///		TextureOperation enum.
        /// </summary>
        /// <param name="blendop"></param>
        /// <returns></returns>
        public static XNA.BlendFunction ConvertEnum(LayerBlendOperationEx blendop)
        {
            XNA.BlendFunction d3dTexOp =  0;

                
                // figure out what is what
            switch ( blendop )
            {
                    
                /*case LayerBlendOperationEx.Source1:
                    d3dTexOp = XNA.Blend.BlendFunction.SelectArg1;
                    break;

                case LayerBlendOperationEx.Source2:
                    d3dTexOp = D3D.TextureOperation.SelectArg2;
                    break;
                
                case LayerBlendOperationEx.Modulate:
                    d3dTexOp = BlendFunction.moD3D.TextureOperation.Modulate;
                    break;

                case LayerBlendOperationEx.ModulateX2:
                    d3dTexOp = D3D.TextureOperation.Modulate2X;
                    break;

                case LayerBlendOperationEx.ModulateX4:
                    d3dTexOp = D3D.TextureOperation.Modulate4X;
                    break;*/

                case LayerBlendOperationEx.Add:
                    d3dTexOp = XNA.BlendFunction.Add;
                    break;

                case LayerBlendOperationEx.AddSigned:
                    d3dTexOp = XNA.BlendFunction.Add;
                    break;

                case LayerBlendOperationEx.AddSmooth:
                    d3dTexOp = XNA.BlendFunction.Add;
                    break;

                case LayerBlendOperationEx.Subtract:
                    d3dTexOp = XNA.BlendFunction.Subtract;
                    break;
                default:
                    d3dTexOp = XNA.BlendFunction.Add;
                    break;

              /*  case LayerBlendOperationEx.BlendDiffuseAlpha:
                    d3dTexOp = XNA.BlendFunction..BlendDiffuseAlpha;
                    break;

                case LayerBlendOperationEx.BlendTextureAlpha:
                    d3dTexOp = D3D.TextureOperation.BlendTextureAlpha;
                    break;

                case LayerBlendOperationEx.BlendCurrentAlpha:
                    d3dTexOp = D3D.TextureOperation.BlendCurrentAlpha;
                    break;

                case LayerBlendOperationEx.BlendManual:
                    d3dTexOp = D3D.TextureOperation.BlendFactorAlpha;
                    break;

                case LayerBlendOperationEx.DotProduct:
                    if ( Root.Instance.RenderSystem.Caps.CheckCap( Capabilities.Dot3 ) )
                    {
                        d3dTexOp = D3D.TextureOperation.DotProduct3;
                    }
                    else
                    {
                        d3dTexOp = D3D.TextureOperation.Modulate;
                    }
                    break;*/
            } // end switch

            return d3dTexOp;
        }

      /*  public static D3D.TextureArgument ConvertEnum( LayerBlendSource blendSource )
        {
            D3D.TextureArgument d3dTexArg = 0;

            switch ( blendSource )
            {
                case LayerBlendSource.Current:
                    d3dTexArg = D3D.TextureArgument.Current;
                    break;

                case LayerBlendSource.Texture:
                    d3dTexArg = D3D.TextureArgument.TextureColor;
                    break;

                case LayerBlendSource.Diffuse:
                    d3dTexArg = D3D.TextureArgument.Diffuse;
                    break;

                case LayerBlendSource.Specular:
                    d3dTexArg = D3D.TextureArgument.Specular;
                    break;

                case LayerBlendSource.Manual:
                    d3dTexArg = D3D.TextureArgument.TFactor;
                    break;
            } // end switch

            return d3dTexArg;
        }*/

        /// <summary>
        ///		Helper method to convert Axiom scene blend factors to D3D
        /// </summary>
        /// <param name="factor"></param>
        /// <returns></returns>
        public static XNA.Blend ConvertEnum( SceneBlendFactor factor )
        {
            XNA.Blend d3dBlend = 0;

            switch ( factor )
            {
                case SceneBlendFactor.One:
                    d3dBlend = XNA.Blend.One;
                    break;
                case SceneBlendFactor.Zero:
                    d3dBlend = XNA.Blend.Zero;
                    break;
                case SceneBlendFactor.DestColor:
                    d3dBlend = XNA.Blend.DestinationColor;
                    break;
                case SceneBlendFactor.SourceColor:
                    d3dBlend = XNA.Blend.SourceColor;
                    break;
                case SceneBlendFactor.OneMinusDestColor:
                    d3dBlend = XNA.Blend.InverseDestinationColor;
                    break;
                case SceneBlendFactor.OneMinusSourceColor:
                    d3dBlend = XNA.Blend.InverseSourceColor;
                    break;
                case SceneBlendFactor.DestAlpha:
                    d3dBlend = XNA.Blend.DestinationAlpha;
                    break;
                case SceneBlendFactor.SourceAlpha:
                    d3dBlend = XNA.Blend.SourceAlpha;
                    break;
                case SceneBlendFactor.OneMinusDestAlpha:
                    d3dBlend = XNA.Blend.InverseDestinationAlpha;
                    break;
                case SceneBlendFactor.OneMinusSourceAlpha:
                    d3dBlend = XNA.Blend.InverseSourceAlpha;
                    break;
            }

            return d3dBlend;
        }
        
        public static Microsoft.Xna.Framework.Graphics.ShaderProfile ConvertEnum(string shaderVersion)
        {
            switch (shaderVersion)
            {
                case "PS_1_1":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.PS_1_1;
                    break;
                case "PS_1_2":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.PS_1_2;
                    break;
                case "PS_1_3":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.PS_1_3;
                    break;
                case "PS_1_4":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.PS_1_4;
                    break;
                case "PS_2_0":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.PS_2_0;
                    break;
                case "PS_2_A":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.PS_2_A;
                    break;
                case "PS_2_B":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.PS_2_B;
                    break;
                case "PS_2_SW":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.PS_2_SW;
                    break;
                case "PS_3_0":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.PS_3_0;
                    break;
                case "Unknown":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.Unknown;
                    break;
                case "VS_1_1":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.VS_1_1;
                    break;
                case "VS_2_0":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.VS_2_0;
                    break;
                case "VS_2_A":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.VS_2_A;
                    break;
                case "VS_2_SW":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.VS_2_SW;
                    break;
                case "VS_3_0":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.VS_3_0;
                    break;
                case "XPS_3_0":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.XPS_3_0;
                    break;
                case "XVS_3_0":
                    return Microsoft.Xna.Framework.Graphics.ShaderProfile.XVS_3_0;
                    break; 
            }
            return Microsoft.Xna.Framework.Graphics.ShaderProfile.Unknown;
        }

        public static XNA.VertexElementFormat ConvertEnum( VertexElementType type,bool tex )
        {
           // if (tex)
             //   return Microsoft.Xna.Framework.Graphics.VertexElementFormat.Unused;

            // we only need to worry about a few types with D3D
            switch ( type )
            {
                case VertexElementType.Color:
                    return XNA.VertexElementFormat.Color;

                //case VertexElementType..Float1:
                 //   return XNA.VertexElementFormat.Float1;

                case VertexElementType.Float2:
                    return XNA.VertexElementFormat.Vector2;

                case VertexElementType.Float3:
                    return XNA.VertexElementFormat.Vector3;

                case VertexElementType.Float4:
                    return XNA.VertexElementFormat.Vector4;

                case VertexElementType.Short2:
                    return XNA.VertexElementFormat.Short2;
                //case VertexElementType.Short3:
                    //return XNA.VertexElementFormat.Short2;

                case VertexElementType.Short4:
                    return XNA.VertexElementFormat.Short4;

                case VertexElementType.UByte4:
                    return XNA.VertexElementFormat.Byte4;

            } // switch

            // keep the compiler happy
            return XNA.VertexElementFormat.Vector3;// Float3;
        }

        public static XNA.VertexElementUsage ConvertEnum( VertexElementSemantic semantic )
        {
            switch ( semantic )
            {
                case VertexElementSemantic.BlendIndices:
                    return XNA.VertexElementUsage.BlendIndices;

                case VertexElementSemantic.BlendWeights:
                    return XNA.VertexElementUsage.BlendWeight;

                case VertexElementSemantic.Diffuse:
                    // index makes the difference (diffuse - 0)
                    return XNA.VertexElementUsage.Color;

                case VertexElementSemantic.Specular:
                    // index makes the difference (specular - 1)
                    return XNA.VertexElementUsage.Color;

                case VertexElementSemantic.Normal:
                    return XNA.VertexElementUsage.Normal;

                case VertexElementSemantic.Position:
                    return XNA.VertexElementUsage.Position;

                case VertexElementSemantic.TexCoords:
                    return XNA.VertexElementUsage.TextureCoordinate;

                case VertexElementSemantic.Binormal:
                    return XNA.VertexElementUsage.Binormal;

                case VertexElementSemantic.Tangent:
                    return XNA.VertexElementUsage.Tangent;
            } // switch

            // keep the compiler happy
            return XNA.VertexElementUsage.Position;
        }

        public static XNA.BufferUsage ConvertEnum(BufferUsage usage)
        {
            XNA.BufferUsage d3dUsage = 0;
            /*
            if ( usage == BufferUsage.Dynamic ||
                usage == BufferUsage.DynamicWriteOnly )

                d3dUsage |= XNA.BufferUsage.WriteOnly;
             
            if ( usage == BufferUsage.WriteOnly ||
                usage == BufferUsage.StaticWriteOnly ||
                usage == BufferUsage.DynamicWriteOnly )
                */
                d3dUsage |= XNA.BufferUsage.WriteOnly;

            return d3dUsage;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static XNA.FogMode ConvertEnum(Axiom.Graphics.FogMode mode)
        {
            // convert the fog mode value
            switch ( mode )
            {
                case Axiom.Graphics.FogMode.Exp:
                    return Microsoft.Xna.Framework.Graphics.FogMode.Exponent;

                case Axiom.Graphics.FogMode.Exp2:
                    return Microsoft.Xna.Framework.Graphics.FogMode.ExponentSquared;

                case Axiom.Graphics.FogMode.Linear:
                    return Microsoft.Xna.Framework.Graphics.FogMode.Linear;
            } // switch

            return 0;
        }

   /*     public static D3D.LockFlags ConvertEnum( BufferLocking locking )
        {
            //no lock in xna
            D3D.LockFlags d3dLockFlags = 0;

            if ( locking == BufferLocking.Discard )
                d3dLockFlags |= D3D.LockFlags.Discard;
            if ( locking == BufferLocking.ReadOnly )
                d3dLockFlags |= D3D.LockFlags.ReadOnly;
            if ( locking == BufferLocking.NoOverwrite )
                d3dLockFlags |= D3D.LockFlags.NoOverwrite;
            
            return 0;
        }*/

        public static int ConvertEnum( TexCoordCalcMethod method, XNA.GraphicsDeviceCapabilities caps )
        {
            /*
            switch ( method )
            {
                case  TexCoordCalcMethod.None:
                    return (int)D3D.TextureCoordinateIndex.PassThru;

                case TexCoordCalcMethod.EnvironmentMapReflection:
                    return TexCoordCalcMethod.EnvironmentMapPlanar;// (int)D3D.TextureCoordinateIndex.CameraSpaceReflectionVector;

                case TexCoordCalcMethod.EnvironmentMapPlanar:
                    //return (int)D3D.TextureCoordinateIndex.CameraSpacePosition;
                    if ( caps.VertexProcessingCaps.SupportsTextureGenerationSphereMap )
                    {
                        // use sphere map if available
                        return TexCoordCalcMethod.EnvironmentMapPlanar;// (int)D3D.TextureCoordinateIndex.SphereMap;
                    }
                    else
                    {
                        // If not, fall back on camera space reflection vector which isn't as good
                        return TexCoordCalcMethod.EnvironmentMapReflection;// (int)D3D.TextureCoordinateIndex.CameraSpaceReflectionVector;
                    }

                case TexCoordCalcMethod.EnvironmentMapNormal:
                    return TexCoordCalcMethod.EnvironmentMapNormal;// (int)D3D.TextureCoordinateIndex.CameraSpaceNormal;

                case TexCoordCalcMethod.EnvironmentMap:
                    if ( caps.VertexProcessingCaps.SupportsTextureGenerationSphereMap )
                    {
                        return TexCoordCalcMethod.EnvironmentMap;// (int)D3D.TextureCoordinateIndex.SphereMap;
                    }
                    else
                    {
                        // fall back on camera space normal if sphere map isnt supported
                        return TexCoordCalcMethod.None;// (int)D3D.TextureCoordinateIndex.CameraSpaceNormal;
                    }

                case TexCoordCalcMethod.ProjectiveTexture:
                    return TexCoordCalcMethod.None;// (int)D3D.TextureCoordinateIndex.CameraSpacePosition;
            } // switch
            */
            return 1;
        }

        public static D3DTexType ConvertEnum( TextureType type )
        {
            switch ( type )
            {
                case TextureType.OneD:
                case TextureType.TwoD:
                    return D3DTexType.Normal;
                case TextureType.CubeMap:
                    return D3DTexType.Cube;
                case TextureType.ThreeD:
                    return D3DTexType.Volume;
            }

            return D3DTexType.None;
        }

        public static XNA.TextureAddressMode ConvertEnum( TextureAddressing type )
        {
            // convert from ours to D3D
            switch ( type )
            {
                case TextureAddressing.Wrap:
                    return XNA.TextureAddressMode.Wrap;

                case TextureAddressing.Mirror:
                    return XNA.TextureAddressMode.Mirror;

                case TextureAddressing.Clamp:
                    return XNA.TextureAddressMode.Clamp;
            } // end switch

            return 0;
        }

        /// <summary>
        ///    Converts our CompareFunction enum to the D3D.Compare equivalent.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static XNA.CompareFunction ConvertEnum( CompareFunction func )
        {
            switch ( func )
            {
                case CompareFunction.AlwaysFail:
                    return XNA.CompareFunction.Never;

                case CompareFunction.AlwaysPass:
                    return XNA.CompareFunction.Always;

                case CompareFunction.Equal:
                    return XNA.CompareFunction.Equal;

                case CompareFunction.Greater:
                    return XNA.CompareFunction.Greater;

                case CompareFunction.GreaterEqual:
                    return XNA.CompareFunction.GreaterEqual;

                case CompareFunction.Less:
                    return XNA.CompareFunction.Less;

                case CompareFunction.LessEqual:
                    return XNA.CompareFunction.LessEqual;

                case CompareFunction.NotEqual:
                    return XNA.CompareFunction.NotEqual;
            }

            return 0;
        }

        /// <summary>
        ///    Converts our Shading enum to the D3D ShadingMode equivalent.
        /// </summary>
        /// <param name="shading"></param>
        /// <returns></returns>
        /*public static XNA.ShadeMode ConvertEnum( Shading shading )
        {
            switch ( shading )
            {
                case Shading.Flat:
                    return D3D.ShadeMode.Flat;
                case Shading.Gouraud:
                    return D3D.ShadeMode.Gouraud;
                case Shading.Phong:
                    return D3D.ShadeMode.Phong;
            }

            return 0;
        }*/
        /// <summary>
        ///    Converts the D3D ShadingMode to our Shading enum equivalent.
        /// </summary>
        /// <param name="shading"></param>
        /// <returns></returns>
        /*public static Shading ConvertEnum( D3D.ShadeMode shading )
        {
            switch ( shading )
            {
                case D3D.ShadeMode.Flat:
                    return Shading.Flat;
                case D3D.ShadeMode.Gouraud:
                    return Shading.Gouraud;
                case D3D.ShadeMode.Phong:
                    return Shading.Phong;
            }

            return 0;
        }*/

        public static XNA.StencilOperation ConvertEnum( Axiom.Graphics.StencilOperation op )
        {
            return ConvertEnum( op, false );
        }

        /// <summary>
        ///    Converts our StencilOperation enum to the D3D StencilOperation equivalent.
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public static XNA.StencilOperation ConvertEnum(Axiom.Graphics.StencilOperation op, bool invert)
        {
            switch ( op )
            {
                case Axiom.Graphics.StencilOperation.Keep:
                    return XNA.StencilOperation.Keep;

                case Axiom.Graphics.StencilOperation.Zero:
                    return XNA.StencilOperation.Zero;

                case Axiom.Graphics.StencilOperation.Replace:
                    return XNA.StencilOperation.Replace;

                case Axiom.Graphics.StencilOperation.Increment:
                    return invert ?
                        XNA.StencilOperation.DecrementSaturation : XNA.StencilOperation.IncrementSaturation;

                case Axiom.Graphics.StencilOperation.Decrement:
                    return invert ?
                        XNA.StencilOperation.IncrementSaturation : XNA.StencilOperation.DecrementSaturation;

                case Axiom.Graphics.StencilOperation.IncrementWrap:
                    return invert ?
                        XNA.StencilOperation.Decrement : XNA.StencilOperation.Increment;

                case Axiom.Graphics.StencilOperation.DecrementWrap:
                    return invert ?
                        XNA.StencilOperation.Increment : XNA.StencilOperation.Decrement;

                case Axiom.Graphics.StencilOperation.Invert:
                    return XNA.StencilOperation.Invert;
            }

            return 0;
        }

        public static XNA.CullMode ConvertEnum( Axiom.Graphics.CullingMode mode, bool flip )
        {
            switch ( mode )
            {
                case CullingMode.None:
                    return XNA.CullMode.None;

                case CullingMode.Clockwise:
                    return flip ? XNA.CullMode.CullCounterClockwiseFace : XNA.CullMode.CullClockwiseFace;

                case CullingMode.CounterClockwise:
                    return flip ? XNA.CullMode.CullClockwiseFace : XNA.CullMode.CullCounterClockwiseFace;
            }

            return 0;
        }

        /// <summary>
        ///    Checks D3D matrix to see if it an identity matrix.
        /// </summary>
        /// <remarks>
        ///    For whatever reason, the equality operator overloads for the D3D Matrix
        ///    struct are extremely slow....
        /// </remarks>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static bool IsIdentity( ref Microsoft.Xna.Framework.Matrix matrix )
        {
            if ( matrix.M11 == 1.0f &&
                matrix.M12 == 0.0f &&
                matrix.M13 == 0.0f &&
                matrix.M14 == 0.0f &&
                matrix.M21 == 0.0f &&
                matrix.M22 == 1.0f &&
                matrix.M23 == 0.0f &&
                matrix.M24 == 0.0f &&
                matrix.M31 == 0.0f &&
                matrix.M32 == 0.0f &&
                matrix.M33 == 1.0f &&
                matrix.M34 == 0.0f &&
                matrix.M41 == 0.0f &&
                matrix.M42 == 0.0f &&
                matrix.M43 == 0.0f &&
                matrix.M44 == 1.0f )
            {

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
