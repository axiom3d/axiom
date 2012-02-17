#region MIT/X11 License
//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using SlimDX.Direct3D9;
using D3D = SlimDX.Direct3D9;
using DX = SlimDX;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// Should we ask D3D to manage vertex/index buffers automatically?
    /// Doing so avoids lost devices, but also has a performance impact
    /// which is unacceptably bad when using very large buffers
    /// </summary>
    /// AXIOM_D3D_MANAGE_BUFFERS

	/// <summary>
	///	Helper class for Direct3D that includes conversion functions and things that are
	///	specific to D3D.
	/// </summary>
	public class D3DHelper
	{
        #region ConvertEnum overloads

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom ShadeOptions value
        /// </summary>
        [OgreVersion( 1, 7, 2, "ShadeMode.Phong is missing in SlimDX implementation" )]
        public static D3D.ShadeMode ConvertEnum( ShadeOptions opt )
        {
            switch ( opt )
            {
                case ShadeOptions.Flat:
                    return D3D.ShadeMode.Flat;

                case ShadeOptions.Gouraud:
                    return D3D.ShadeMode.Gouraud;

                case ShadeOptions.Phong:
                    return (D3D.ShadeMode)3;//D3D.ShadeMode.Phong;
            };

            return 0;
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom LightType value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.LightType ConvertEnum( Graphics.LightType lightType )
        {
            switch ( lightType )
            {
                case Graphics.LightType.Point:
                    return D3D.LightType.Point;

                case Graphics.LightType.Directional:
                    return D3D.LightType.Directional;

                case Graphics.LightType.Spotlight:
                    return D3D.LightType.Spot;
            };

            return (D3D.LightType)0x7fffffff; // D3DLIGHT_FORCE_DWORD
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom TexCoordCalsMethod value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static int ConvertEnum( TexCoordCalcMethod method, D3D.Capabilities caps )
        {
            switch ( method )
            {
                case TexCoordCalcMethod.None:
                    return (int)D3D.TextureCoordIndex.PassThru;

                case TexCoordCalcMethod.EnvironmentMapReflection:
                    return (int)D3D.TextureCoordIndex.CameraSpaceReflectionVector;

                case TexCoordCalcMethod.EnvironmentMapPlanar:
                    if ( ( caps.VertexProcessingCaps & D3D.VertexProcessingCaps.TexGenSphereMap ) == D3D.VertexProcessingCaps.TexGenSphereMap )
                    {
                        // use sphere map if available
                        return (int)D3D.TextureCoordIndex.SphereMap;
                    }
                    else
                    {
                        // If not, fall back on camera space reflection vector which isn't as good
                        return (int)D3D.TextureCoordIndex.CameraSpaceReflectionVector;
                    }

                case TexCoordCalcMethod.EnvironmentMapNormal:
                    return (int)D3D.TextureCoordIndex.CameraSpaceNormal;

                case TexCoordCalcMethod.EnvironmentMap:
                    if ( ( caps.VertexProcessingCaps & D3D.VertexProcessingCaps.TexGenSphereMap ) == D3D.VertexProcessingCaps.TexGenSphereMap )
                    {
                        return (int)D3D.TextureCoordIndex.SphereMap;
                    }
                    else
                    {
                        // fall back on camera space normal if sphere map isnt supported
                        return (int)D3D.TextureCoordIndex.CameraSpaceNormal;
                    }

                case TexCoordCalcMethod.ProjectiveTexture:
                    return (int)D3D.TextureCoordIndex.CameraSpacePosition;
            } // switch

            return 0;
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom TextureAddressing value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.TextureAddress ConvertEnum( TextureAddressing type, D3D.Capabilities caps )
        {
            // convert from ours to D3D
            switch ( type )
            {
                case TextureAddressing.Wrap:
                    return D3D.TextureAddress.Wrap;

                case TextureAddressing.Mirror:
                    return D3D.TextureAddress.Mirror;

                case TextureAddressing.Clamp:
                    return D3D.TextureAddress.Clamp;

                case TextureAddressing.Border:
                    if ( ( caps.TextureAddressCaps & D3D.TextureAddressCaps.Border ) == D3D.TextureAddressCaps.Border )
                        return D3D.TextureAddress.Border;
                    else
                        return D3D.TextureAddress.Clamp;
            } // end switch

            return (D3D.TextureAddress)0x7fffffff; //D3DTADDRESS_FORCE_DWORD
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom LayerBlendType value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.TextureStage ConvertEnum( LayerBlendType lbt )
        {
            switch ( lbt )
            {
                case LayerBlendType.Color:
                    return D3D.TextureStage.ColorOperation;

                case LayerBlendType.Alpha:
                    return D3D.TextureStage.AlphaOperation;
            };

            return (D3D.TextureStage)0x7fffffff;// D3DTSS_FORCE_DWORD
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom LayerBlendSource value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static TextureArgument ConvertEnum( LayerBlendSource lbs, bool perStageConstants )
        {
            switch ( lbs )
            {
                case LayerBlendSource.Current:
                    return TextureArgument.Current;

                case LayerBlendSource.Texture:
                    return TextureArgument.Texture;

                case LayerBlendSource.Diffuse:
                    return TextureArgument.Diffuse;

                case LayerBlendSource.Specular:
                    return TextureArgument.Specular;

                case LayerBlendSource.Manual:
                    return perStageConstants ? TextureArgument.Constant : TextureArgument.TFactor;
            }
            return 0;
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom LayerBlendOperationEx value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static TextureOperation ConvertEnum( LayerBlendOperationEx lbo, D3D.Capabilities devCaps )
        {
            switch ( lbo )
            {
                case LayerBlendOperationEx.Source1:
                    return TextureOperation.SelectArg1;

                case LayerBlendOperationEx.Source2:
                    return TextureOperation.SelectArg2;

                case LayerBlendOperationEx.Modulate:
                    return TextureOperation.Modulate;

                case LayerBlendOperationEx.ModulateX2:
                    return TextureOperation.Modulate2X;

                case LayerBlendOperationEx.ModulateX4:
                    return TextureOperation.Modulate4X;

                case LayerBlendOperationEx.Add:
                    return TextureOperation.Add;

                case LayerBlendOperationEx.AddSigned:
                    return TextureOperation.AddSigned;

                case LayerBlendOperationEx.AddSmooth:
                    return TextureOperation.AddSmooth;

                case LayerBlendOperationEx.Subtract:
                    return TextureOperation.Subtract;

                case LayerBlendOperationEx.BlendDiffuseAlpha:
                    return TextureOperation.BlendDiffuseAlpha;

                case LayerBlendOperationEx.BlendTextureAlpha:
                    return TextureOperation.BlendTextureAlpha;

                case LayerBlendOperationEx.BlendCurrentAlpha:
                    return TextureOperation.BlendCurrentAlpha;

                case LayerBlendOperationEx.BlendManual:
                    return TextureOperation.BlendFactorAlpha;

                case LayerBlendOperationEx.DotProduct:
                    return ( devCaps.TextureOperationCaps & TextureOperationCaps.DotProduct3 ) != 0 ? TextureOperation.DotProduct3 : TextureOperation.Modulate;
                
                case LayerBlendOperationEx.BlendDiffuseColor:
                    return ( devCaps.TextureOperationCaps & TextureOperationCaps.Lerp ) != 0 ? TextureOperation.Lerp : TextureOperation.Modulate;
            }

            return 0;
        }

        /// <summary>
        ///	Return a D3D9 equivalent for a Axiom SceneBlendFactor value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.Blend ConvertEnum( SceneBlendFactor factor )
        {
            switch ( factor )
            {
                case SceneBlendFactor.One:
                    return D3D.Blend.One;

                case SceneBlendFactor.Zero:
                    return D3D.Blend.Zero;

                case SceneBlendFactor.DestColor:
                    return D3D.Blend.DestinationColor;

                case SceneBlendFactor.SourceColor:
                    return D3D.Blend.SourceColor;

                case SceneBlendFactor.OneMinusDestColor:
                    return D3D.Blend.InverseDestinationColor;

                case SceneBlendFactor.OneMinusSourceColor:
                    return D3D.Blend.InverseSourceColor;

                case SceneBlendFactor.DestAlpha:
                    return D3D.Blend.DestinationAlpha;

                case SceneBlendFactor.SourceAlpha:
                    return D3D.Blend.SourceAlpha;

                case SceneBlendFactor.OneMinusDestAlpha:
                    return D3D.Blend.InverseDestinationAlpha;

                case SceneBlendFactor.OneMinusSourceAlpha:
                    return D3D.Blend.InverseSourceAlpha;
            };

            return (D3D.Blend)0x7fffffff; //D3DBLEND_FORCE_DWORD
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom SceneBlendOperation value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.BlendOperation ConvertEnum( SceneBlendOperation op )
        {
            switch ( op )
            {
                case SceneBlendOperation.Add:
                    return D3D.BlendOperation.Add;

                case SceneBlendOperation.Subtract:
                    return D3D.BlendOperation.Subtract;

                case SceneBlendOperation.ReverseSubtract:
                    return D3D.BlendOperation.ReverseSubtract;

                case SceneBlendOperation.Min:
                    return D3D.BlendOperation.Minimum;

                case SceneBlendOperation.Max:
                    return D3D.BlendOperation.Maximum;
            };

            return (D3D.BlendOperation)0x7fffffff; //D3DBLENDOP_FORCE_DWORD
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom CompareFunction value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.Compare ConvertEnum( CompareFunction func )
        {
            switch ( func )
            {
                case CompareFunction.AlwaysFail:
                    return D3D.Compare.Never;

                case CompareFunction.AlwaysPass:
                    return D3D.Compare.Always;

                case CompareFunction.Less:
                    return D3D.Compare.Less;

                case CompareFunction.LessEqual:
                    return D3D.Compare.LessEqual;

                case CompareFunction.Equal:
                    return D3D.Compare.Equal;

                case CompareFunction.NotEqual:
                    return D3D.Compare.NotEqual;

                case CompareFunction.GreaterEqual:
                    return D3D.Compare.GreaterEqual;

                case CompareFunction.Greater:
                    return D3D.Compare.Greater;
            };

            return 0;
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom CullingMode value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.Cull ConvertEnum( Graphics.CullingMode mode, bool flip )
        {
            switch ( mode )
            {
                case CullingMode.None:
                    return D3D.Cull.None;

                case CullingMode.Clockwise:
                    return flip ? D3D.Cull.Counterclockwise : D3D.Cull.Clockwise;

                case CullingMode.CounterClockwise:
                    return flip ? D3D.Cull.Clockwise : D3D.Cull.Counterclockwise;
            }

            return 0;
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom FogMode value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.FogMode ConvertEnum( Graphics.FogMode mode )
        {
            // convert the fog mode value
            switch ( mode )
            {
                case Axiom.Graphics.FogMode.Exp:
                    return D3D.FogMode.Exponential;

                case Axiom.Graphics.FogMode.Exp2:
                    return D3D.FogMode.ExponentialSquared;

                case Axiom.Graphics.FogMode.Linear:
                    return D3D.FogMode.Linear;
            }; // switch

            return (D3D.FogMode)0x7fffffff; //D3DFOG_FORCE_DWORD
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom PolygonMode value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.FillMode ConvertEnum( PolygonMode mode )
        {
            switch ( mode )
            {
                case PolygonMode.Points:
                    return D3D.FillMode.Point;

                case PolygonMode.Wireframe:
                    return D3D.FillMode.Wireframe;

                case PolygonMode.Solid:
                    return D3D.FillMode.Solid;
            };

            return (D3D.FillMode)0x7fffffff; //D3DFILL_FORCE_DWORD
        }

        /// <summary>
        /// Return a D3D9 equivalent for a Axiom StencilOperation value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
#if NET_40
        public static D3D.StencilOperation ConvertEnum( Graphics.StencilOperation op, bool invert = false )
#else
        public static D3D.StencilOperation ConvertEnum( Graphics.StencilOperation op, bool invert )
#endif
        {
            switch ( op )
            {
                case Axiom.Graphics.StencilOperation.Keep:
                    return D3D.StencilOperation.Keep;

                case Axiom.Graphics.StencilOperation.Zero:
                    return D3D.StencilOperation.Zero;

                case Axiom.Graphics.StencilOperation.Replace:
                    return D3D.StencilOperation.Replace;

                case Axiom.Graphics.StencilOperation.Increment:
                    return invert ? D3D.StencilOperation.DecrementSaturate : D3D.StencilOperation.IncrementSaturate;

                case Axiom.Graphics.StencilOperation.Decrement:
                    return invert ? D3D.StencilOperation.IncrementSaturate : D3D.StencilOperation.DecrementSaturate;

                case Axiom.Graphics.StencilOperation.IncrementWrap:
                    return invert ? D3D.StencilOperation.Decrement : D3D.StencilOperation.Increment;

                case Axiom.Graphics.StencilOperation.DecrementWrap:
                    return invert ? D3D.StencilOperation.Increment : D3D.StencilOperation.Decrement;

                case Axiom.Graphics.StencilOperation.Invert:
                    return D3D.StencilOperation.Invert;
            }

            return 0;
        }

#if !NET_40
        /// <see cref="D3DHelper.ConvertEnum(Graphics.StencilOperation, bool)"/>
        public static D3D.StencilOperation ConvertEnum( Graphics.StencilOperation op )
        {
            return ConvertEnum( op, false );
        }
#endif
        /// <summary>
        /// Return a D3D9 state type for Axiom FilterType value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.SamplerState ConvertEnum( FilterType type )
        {
            switch ( type )
            {
                case FilterType.Min:
                    return D3D.SamplerState.MinFilter;

                case FilterType.Mag:
                    return D3D.SamplerState.MagFilter;

                case FilterType.Mip:
                    return D3D.SamplerState.MipFilter;
            };

            // to keep compiler happy
            return D3D.SamplerState.MinFilter;
        }

        /// <summary>
        /// Return a D3D9 filter option for Axiom FilterType & FilterOption value
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.TextureFilter ConvertEnum( FilterType type, FilterOptions options, D3D.Capabilities devCaps, D3D9TextureType texType )
        {
            // Assume normal
            D3D.FilterCaps capsType = devCaps.TextureFilterCaps;

            switch ( texType )
            {
                case D3D9TextureType.Normal:
                    capsType = devCaps.TextureFilterCaps;
                    break;

                case D3D9TextureType.Cube:
                    capsType = devCaps.CubeTextureFilterCaps;
                    break;

                case D3D9TextureType.Volume:
                    capsType = devCaps.VolumeTextureFilterCaps;
                    break;
            }

            switch ( type )
            {
                #region FilterType.Min
                case FilterType.Min:
                    {
                        switch ( options )
                        {
                            // NOTE: Fall through if device doesn't support requested type
                            case FilterOptions.Anisotropic:
                                if ( ( capsType & D3D.FilterCaps.MinAnisotropic ) == D3D.FilterCaps.MinAnisotropic )
                                {
                                    return D3D.TextureFilter.Anisotropic;
                                }
                                break;

                            case FilterOptions.Linear:
                                if ( ( capsType & D3D.FilterCaps.MinLinear ) == D3D.FilterCaps.MinLinear )
                                {
                                    return D3D.TextureFilter.Linear;
                                }
                                break;

                            case FilterOptions.Point:
                            case FilterOptions.None:
                                return D3D.TextureFilter.Point;
                        }
                        break;
                    }
                #endregion FilterType.Min

                #region FilterType.Mag
                case FilterType.Mag:
                    {
                        switch ( options )
                        {
                            // NOTE: Fall through if device doesn't support requested type
                            case FilterOptions.Anisotropic:
                                if ( ( capsType & D3D.FilterCaps.MagAnisotropic ) == D3D.FilterCaps.MagAnisotropic )
                                {
                                    return D3D.TextureFilter.Anisotropic;
                                }
                                break;

                            case FilterOptions.Linear:
                                if ( ( capsType & D3D.FilterCaps.MagLinear ) == D3D.FilterCaps.MagLinear )
                                {
                                    return D3D.TextureFilter.Linear;
                                }
                                break;

                            case FilterOptions.Point:
                            case FilterOptions.None:
                                return D3D.TextureFilter.Point;
                        }
                        break;
                    }
                #endregion FilterType.Mag

                #region FilterType.Mip
                case FilterType.Mip:
                    {
                        switch ( options )
                        {
                            case FilterOptions.Anisotropic:
                            case FilterOptions.Linear:
                                if ( ( capsType & D3D.FilterCaps.MipLinear ) == D3D.FilterCaps.MipLinear )
                                {
                                    return D3D.TextureFilter.Linear;
                                }
                                break;

                            case FilterOptions.Point:
                                if ( ( capsType & D3D.FilterCaps.MipPoint ) == D3D.FilterCaps.MipPoint )
                                {
                                    return D3D.TextureFilter.Point;
                                }
                                break;

                            case FilterOptions.None:
                                return D3D.TextureFilter.None;
                        }
                        break;
                    }
                #endregion FilterType.Mip
            }

            // should never get here
            return 0;
        }

        /// <summary>
        /// Return the D3DtexType equivalent of a Axiom texture type
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D9TextureType ConvertEnum( TextureType type )
        {
            switch ( type )
            {
                case TextureType.OneD:
                case TextureType.TwoD:
                    return D3D9TextureType.Normal;

                case TextureType.CubeMap:
                    return D3D9TextureType.Cube;

                case TextureType.ThreeD:
                    return D3D9TextureType.Volume;
            }

            return D3D9TextureType.None;
        }

        /// <summary>
        /// Return the combination of BufferUsage values for Axiom buffer usage
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.Usage ConvertEnum( BufferUsage usage )
        {
            D3D.Usage ret = 0;

            if ( ( usage & BufferUsage.Dynamic ) != 0 )
            {
#if AXIOM_D3D_MANAGE_BUFFERS
                // Only add the dynamic flag for the default pool, and
                // we use default pool when buffer is discardable
                if ( ( usage & BufferUsage.Discardable ) != 0 )
                    ret |= D3D.Usage.Dynamic;
#else
				ret |= D3D.Usage.Dynamic;
#endif
            }
            if ( ( usage & BufferUsage.WriteOnly ) != 0 )
            {
                ret |= D3D.Usage.WriteOnly;
            }
            return ret;
        }

        /// <summary>
        /// Get lock options
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.LockFlags ConvertEnum( BufferLocking locking, BufferUsage usage )
        {
            D3D.LockFlags ret = 0;
            if ( locking == BufferLocking.Discard )
            {
#if AXIOM_D3D_MANAGE_BUFFERS
                // Only add the discard flag for dynamic usgae and default pool
                if ( ( usage & BufferUsage.Dynamic ) != 0 &&
                    ( usage & BufferUsage.Discardable ) != 0 )
                    ret |= D3D.LockFlags.Discard;
#else
				// D3D doesn't like discard or no_overwrite on non-dynamic buffers
				if ((usage & BufferUsage.Dynamic) != 0)
					ret |= D3D.LockFlags.Discard;
#endif
            }
            if ( locking == BufferLocking.ReadOnly )
            {
                // D3D debug runtime doesn't like you locking managed buffers readonly
                // when they were created with write-only (even though you CAN read
                // from the software backed version)
                if ( ( usage & BufferUsage.WriteOnly ) == 0 )
                    ret |= D3D.LockFlags.ReadOnly;
            }
            if ( locking == BufferLocking.NoOverwrite )
            {
#if AXIOM_D3D_MANAGE_BUFFERS
                // Only add the nooverwrite flag for dynamic usgae and default pool
                if ( ( usage & BufferUsage.Dynamic ) != 0 &&
                    ( usage & BufferUsage.Discardable ) != 0 )
                    ret |= D3D.LockFlags.NoOverwrite;
#else
				// D3D doesn't like discard or no_overwrite on non-dynamic buffers
				if ((usage & BufferUsage.Dynamic) != 0)
					ret |= D3D.LockFlags.NoOverwrite;
#endif
            }

            return ret;
        }

        /// <summary>
        /// Get index type
        /// </summary>
        /// <remarks>
        /// SlimDX accepts boolean values.
        /// True to create a buffer of 16-bit indices, false otherwise.
        /// </remarks>
        [OgreVersion( 1, 7, 2 )]
        public static bool ConvertEnum( IndexType itype )
        {
            return itype == IndexType.Size16;
        }

        /// <summary>
        /// Get vertex data type
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.DeclarationType ConvertEnum( VertexElementType type )
        {
            // we only need to worry about a few types with D3D
            switch ( type )
            {
                case VertexElementType.Color_ABGR:
                case VertexElementType.Color_ARGB:
                case VertexElementType.Color:
                    return D3D.DeclarationType.Color;

                case VertexElementType.Float1:
                    return D3D.DeclarationType.Float1;

                case VertexElementType.Float2:
                    return D3D.DeclarationType.Float2;

                case VertexElementType.Float3:
                    return D3D.DeclarationType.Float3;

                case VertexElementType.Float4:
                    return D3D.DeclarationType.Float4;

                case VertexElementType.Short2:
                    return D3D.DeclarationType.Short2;

                case VertexElementType.Short4:
                    return D3D.DeclarationType.Short4;

                case VertexElementType.UByte4:
                    return D3D.DeclarationType.Ubyte4;

            }; // switch

            // keep the compiler happy
            return D3D.DeclarationType.Float3;
        }

        /// <summary>
        /// Get vertex semantic
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.DeclarationUsage ConvertEnum( VertexElementSemantic semantic )
        {
            switch ( semantic )
            {
                case VertexElementSemantic.BlendIndices:
                    return D3D.DeclarationUsage.BlendIndices;

                case VertexElementSemantic.BlendWeights:
                    return D3D.DeclarationUsage.BlendWeight;

                case VertexElementSemantic.Diffuse:
                    // index makes the difference (diffuse - 0)
                    return D3D.DeclarationUsage.Color;

                case VertexElementSemantic.Specular:
                    // index makes the difference (specular - 1)
                    return D3D.DeclarationUsage.Color;

                case VertexElementSemantic.Normal:
                    return D3D.DeclarationUsage.Normal;

                case VertexElementSemantic.Position:
                    return D3D.DeclarationUsage.Position;

                case VertexElementSemantic.TexCoords:
                    return D3D.DeclarationUsage.TextureCoordinate;

                case VertexElementSemantic.Binormal:
                    return D3D.DeclarationUsage.Binormal;

                case VertexElementSemantic.Tangent:
                    return D3D.DeclarationUsage.Tangent;
            }; // switch

            // keep the compiler happy
            return D3D.DeclarationUsage.Position;
        }

        /// <summary>
        /// Utility method, convert D3D9 pixel format to Axiom pixel format
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        [OgreVersion( 1, 7, 2 )]
        public static Media.PixelFormat ConvertEnum( D3D.Format format )
        {
            switch ( format )
            {
                case D3D.Format.A8:
                    return Media.PixelFormat.A8;

                case D3D.Format.L8:
                    return Media.PixelFormat.L8;

                case D3D.Format.L16:
                    return Media.PixelFormat.L16;

                case D3D.Format.A4L4:
                    return Media.PixelFormat.A4L4;

                case D3D.Format.A8L8:	// Assume little endian here
                    return Media.PixelFormat.A8L8;

                case D3D.Format.R3G3B2:
                    return Media.PixelFormat.R3G3B2;

                case D3D.Format.A1R5G5B5:
                    return Media.PixelFormat.A1R5G5B5;

                case D3D.Format.A4R4G4B4:
                    return Media.PixelFormat.A4R4G4B4;

                case D3D.Format.R5G6B5:
                    return Media.PixelFormat.R5G6B5;

                case D3D.Format.R8G8B8:
                    return Media.PixelFormat.R8G8B8;

                case D3D.Format.X8R8G8B8:
                    return Media.PixelFormat.X8R8G8B8;

                case D3D.Format.A8R8G8B8:
                    return Media.PixelFormat.A8R8G8B8;

                case D3D.Format.X8B8G8R8:
                    return Media.PixelFormat.X8B8G8R8;

                case D3D.Format.A8B8G8R8:
                    return Media.PixelFormat.A8B8G8R8;

                case D3D.Format.A2R10G10B10:
                    return Media.PixelFormat.A2R10G10B10;

                case D3D.Format.A2B10G10R10:
                    return Media.PixelFormat.A2B10G10R10;

                case D3D.Format.R16F:
                    return Media.PixelFormat.FLOAT16_R;

                case D3D.Format.A16B16G16R16F:
                    return Media.PixelFormat.FLOAT16_RGBA;

                case D3D.Format.R32F:
                    return Media.PixelFormat.FLOAT32_R;

                case D3D.Format.A32B32G32R32F:
                    return Media.PixelFormat.FLOAT32_RGBA;

                case D3D.Format.A16B16G16R16:
                    return Media.PixelFormat.SHORT_RGBA;

                case D3D.Format.Dxt1:
                    return Media.PixelFormat.DXT1;

                case D3D.Format.Dxt2:
                    return Media.PixelFormat.DXT2;

                case D3D.Format.Dxt3:
                    return Media.PixelFormat.DXT3;

                case D3D.Format.Dxt4:
                    return Media.PixelFormat.DXT4;

                case D3D.Format.Dxt5:
                    return Media.PixelFormat.DXT5;

                default:
                    return Media.PixelFormat.Unknown;
            }
        }

        /// <summary>
        /// Utility method, convert Axiom pixel format to D3D9 pixel format
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static D3D.Format ConvertEnum( Media.PixelFormat format )
        {
            switch ( format )
            {
                case Media.PixelFormat.A8:
                    return D3D.Format.A8;

                case Media.PixelFormat.L8:
                    return D3D.Format.L8;

                case Media.PixelFormat.L16:
                    return D3D.Format.L16;

                case Media.PixelFormat.A4L4:
                    return D3D.Format.A4L4;

                case Media.PixelFormat.A8L8:
                    return D3D.Format.A8L8;	// Assume little endian here
                
                case Media.PixelFormat.R3G3B2:
                    return D3D.Format.R3G3B2;

                case Media.PixelFormat.A1R5G5B5:
                    return D3D.Format.A1R5G5B5;

                case Media.PixelFormat.A4R4G4B4:
                    return D3D.Format.A4R4G4B4;

                case Media.PixelFormat.R5G6B5:
                    return D3D.Format.R5G6B5;

                case Media.PixelFormat.R8G8B8:
                    return D3D.Format.R8G8B8;

                case Media.PixelFormat.X8R8G8B8:
                    return D3D.Format.X8R8G8B8;

                case Media.PixelFormat.A8R8G8B8:
                    return D3D.Format.A8R8G8B8;

                case Media.PixelFormat.X8B8G8R8:
                    return D3D.Format.X8B8G8R8;

                case Media.PixelFormat.A8B8G8R8:
                    return D3D.Format.A8B8G8R8;

                case Media.PixelFormat.A2R10G10B10:
                    return D3D.Format.A2R10G10B10;

                case Media.PixelFormat.A2B10G10R10:
                    return D3D.Format.A2B10G10R10;

                case Media.PixelFormat.FLOAT16_R:
                    return D3D.Format.R16F;

                case Media.PixelFormat.FLOAT16_GR:
                    return D3D.Format.G16R16F;

                case Media.PixelFormat.FLOAT16_RGBA:
                    return D3D.Format.A16B16G16R16F;

                case Media.PixelFormat.FLOAT32_R:
                    return D3D.Format.R32F;

                case Media.PixelFormat.FLOAT32_GR:
                    return D3D.Format.G32R32F;

                case Media.PixelFormat.FLOAT32_RGBA:
                    return D3D.Format.A32B32G32R32F;

                case Media.PixelFormat.SHORT_RGBA:
                    return D3D.Format.A16B16G16R16;

                case Media.PixelFormat.SHORT_GR:
                    return D3D.Format.G16R16;

                case Media.PixelFormat.DXT1:
                    return D3D.Format.Dxt1;

                case Media.PixelFormat.DXT2:
                    return D3D.Format.Dxt2;

                case Media.PixelFormat.DXT3:
                    return D3D.Format.Dxt3;

                case Media.PixelFormat.DXT4:
                    return D3D.Format.Dxt4;

                case Media.PixelFormat.DXT5:
                    return D3D.Format.Dxt5;

                default:
                    return D3D.Format.Unknown;
            };
        }

        #endregion ConvertEnum overloads

        #region Matrix
        
        /// <summary>
        /// Convert matrix to D3D style
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static DX.Matrix MakeD3DMatrix( Matrix4 matrix )
        {
            // Transpose matrix
            // D3D9 uses row vectors i.e. V*M
            // Ogre, OpenGL and everything else uses column vectors i.e. M*V

            var dxMat = new DX.Matrix();

            // set it to a transposed matrix since DX uses row vectors
            dxMat.M11 = matrix.m00;
            dxMat.M12 = matrix.m10;
            dxMat.M13 = matrix.m20;
            dxMat.M14 = matrix.m30;
            dxMat.M21 = matrix.m01;
            dxMat.M22 = matrix.m11;
            dxMat.M23 = matrix.m21;
            dxMat.M24 = matrix.m31;
            dxMat.M31 = matrix.m02;
            dxMat.M32 = matrix.m12;
            dxMat.M33 = matrix.m22;
            dxMat.M34 = matrix.m32;
            dxMat.M41 = matrix.m03;
            dxMat.M42 = matrix.m13;
            dxMat.M43 = matrix.m23;
            dxMat.M44 = matrix.m33;

            return dxMat;
        }

        /// <summary>
        /// Convert matrix from D3D style
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public static Matrix4 ConvertD3DMatrix( ref DX.Matrix d3DMat )
        {
            var mat = Matrix4.Zero;

            mat.m00 = d3DMat.M11;
            mat.m10 = d3DMat.M12;
            mat.m20 = d3DMat.M13;
            mat.m30 = d3DMat.M14;

            mat.m01 = d3DMat.M21;
            mat.m11 = d3DMat.M22;
            mat.m21 = d3DMat.M23;
            mat.m31 = d3DMat.M24;

            mat.m02 = d3DMat.M31;
            mat.m12 = d3DMat.M32;
            mat.m22 = d3DMat.M33;
            mat.m32 = d3DMat.M34;

            mat.m03 = d3DMat.M41;
            mat.m13 = d3DMat.M42;
            mat.m23 = d3DMat.M43;
            mat.m33 = d3DMat.M44;

            return mat;
        }

        #endregion Matrix

        [AxiomHelper( 0, 9 )]
		public static System.Drawing.Color ToColor( ColorEx color )
		{
			return System.Drawing.Color.FromArgb(
                (int)( color.a < 1.0f ? color.a * 255.0f : color.a ),
                (int)( color.r * 255.0f ),
                (int)( color.g * 255.0f ),
                (int)( color.b * 255.0f )
                );
		}

        /// <summary>
		/// Checks D3D matrix to see if it an identity matrix.
		/// </summary>
		/// <remarks>
		/// For whatever reason, the equality operator overloads for the D3D Matrix
		/// struct are extremely slow....
		/// </remarks>
        [AxiomHelper( 0, 9 )]
		public static bool IsIdentity( ref DX.Matrix matrix )
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

        /// <summary>
        /// Utility method, find closest Ogre pixel format that D3D9 can support
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
		public static Media.PixelFormat GetClosestSupported( Media.PixelFormat format )
		{
			if ( ConvertEnum( format ) != D3D.Format.Unknown )
				return format;

			switch ( format )
			{
				case Media.PixelFormat.B5G6R5:
					return Media.PixelFormat.R5G6B5;

				case Media.PixelFormat.B8G8R8:
                    return Media.PixelFormat.R8G8B8;
				
                case Media.PixelFormat.B8G8R8A8:
					return Media.PixelFormat.A8R8G8B8;

				case Media.PixelFormat.FLOAT16_RGB:
					return Media.PixelFormat.FLOAT16_RGBA;

				case Media.PixelFormat.FLOAT32_RGB:
					return Media.PixelFormat.FLOAT32_RGBA;

				case Media.PixelFormat.Unknown:
				default:
					return Media.PixelFormat.A8R8G8B8;
			}
		}
	};
}