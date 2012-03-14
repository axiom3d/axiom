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

using System.Drawing;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;

using SharpDX;
using SharpDX.Direct3D9;

using Capabilities = SharpDX.Direct3D9.Capabilities;
using D3D9 = SharpDX.Direct3D9;
using DX = SharpDX;
using FogMode = SharpDX.Direct3D9.FogMode;
using LightType = SharpDX.Direct3D9.LightType;
using StencilOperation = SharpDX.Direct3D9.StencilOperation;

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
	public class D3D9Helper
	{
		#region ConvertEnum overloads

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom ShadeOptions value
		/// </summary>
		[OgreVersion( 1, 7, 2, "ShadeMode.Phong is missing in SlimDX implementation" )]
		public static ShadeMode ConvertEnum( ShadeOptions opt )
		{
			switch ( opt )
			{
				case ShadeOptions.Flat:
					return ShadeMode.Flat;

				case ShadeOptions.Gouraud:
					return ShadeMode.Gouraud;

				case ShadeOptions.Phong:
					return (ShadeMode)3; //D3D.ShadeMode.Phong;
			}
			;

			return 0;
		}

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom LightType value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static LightType ConvertEnum( Graphics.LightType lightType )
		{
			switch ( lightType )
			{
				case Graphics.LightType.Point:
					return LightType.Point;

				case Graphics.LightType.Directional:
					return LightType.Directional;

				case Graphics.LightType.Spotlight:
					return LightType.Spot;
			}
			;

			return (LightType)0x7fffffff; // D3DLIGHT_FORCE_DWORD
		}

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom TexCoordCalsMethod value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static int ConvertEnum( TexCoordCalcMethod method, Capabilities caps )
		{
			switch ( method )
			{
				case TexCoordCalcMethod.None:
					return (int)TextureCoordIndex.PassThru;

				case TexCoordCalcMethod.EnvironmentMapReflection:
					return (int)TextureCoordIndex.CameraSpaceReflectionVector;

				case TexCoordCalcMethod.EnvironmentMapPlanar:
					if ( ( caps.VertexProcessingCaps & VertexProcessingCaps.TexGenSphereMap ) == VertexProcessingCaps.TexGenSphereMap )
					{
						// use sphere map if available
						return (int)TextureCoordIndex.SphereMap;
					}
					else
					{
						// If not, fall back on camera space reflection vector which isn't as good
						return (int)TextureCoordIndex.CameraSpaceReflectionVector;
					}

				case TexCoordCalcMethod.EnvironmentMapNormal:
					return (int)TextureCoordIndex.CameraSpaceNormal;

				case TexCoordCalcMethod.EnvironmentMap:
					if ( ( caps.VertexProcessingCaps & VertexProcessingCaps.TexGenSphereMap ) == VertexProcessingCaps.TexGenSphereMap )
					{
						return (int)TextureCoordIndex.SphereMap;
					}
					else
					{
						// fall back on camera space normal if sphere map isnt supported
						return (int)TextureCoordIndex.CameraSpaceNormal;
					}

				case TexCoordCalcMethod.ProjectiveTexture:
					return (int)TextureCoordIndex.CameraSpacePosition;
			} // switch

			return 0;
		}

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom TextureAddressing value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static TextureAddress ConvertEnum( TextureAddressing type, Capabilities caps )
		{
			// convert from ours to D3D
			switch ( type )
			{
				case TextureAddressing.Wrap:
					return TextureAddress.Wrap;

				case TextureAddressing.Mirror:
					return TextureAddress.Mirror;

				case TextureAddressing.Clamp:
					return TextureAddress.Clamp;

				case TextureAddressing.Border:
					if ( ( caps.TextureAddressCaps & TextureAddressCaps.Border ) == TextureAddressCaps.Border )
					{
						return TextureAddress.Border;
					}
					else
					{
						return TextureAddress.Clamp;
					}
			} // end switch

			return (TextureAddress)0x7fffffff; //D3DTADDRESS_FORCE_DWORD
		}

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom LayerBlendType value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static TextureStage ConvertEnum( LayerBlendType lbt )
		{
			switch ( lbt )
			{
				case LayerBlendType.Color:
					return TextureStage.ColorOperation;

				case LayerBlendType.Alpha:
					return TextureStage.AlphaOperation;
			}
			;

			return (TextureStage)0x7fffffff; // D3DTSS_FORCE_DWORD
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
		public static TextureOperation ConvertEnum( LayerBlendOperationEx lbo, Capabilities devCaps )
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
		public static Blend ConvertEnum( SceneBlendFactor factor )
		{
			switch ( factor )
			{
				case SceneBlendFactor.One:
					return Blend.One;

				case SceneBlendFactor.Zero:
					return Blend.Zero;

				case SceneBlendFactor.DestColor:
					return Blend.DestinationColor;

				case SceneBlendFactor.SourceColor:
					return Blend.SourceColor;

				case SceneBlendFactor.OneMinusDestColor:
					return Blend.InverseDestinationColor;

				case SceneBlendFactor.OneMinusSourceColor:
					return Blend.InverseSourceColor;

				case SceneBlendFactor.DestAlpha:
					return Blend.DestinationAlpha;

				case SceneBlendFactor.SourceAlpha:
					return Blend.SourceAlpha;

				case SceneBlendFactor.OneMinusDestAlpha:
					return Blend.InverseDestinationAlpha;

				case SceneBlendFactor.OneMinusSourceAlpha:
					return Blend.InverseSourceAlpha;
			}
			;

			return (Blend)0x7fffffff; //D3DBLEND_FORCE_DWORD
		}

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom SceneBlendOperation value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static BlendOperation ConvertEnum( SceneBlendOperation op )
		{
			switch ( op )
			{
				case SceneBlendOperation.Add:
					return BlendOperation.Add;

				case SceneBlendOperation.Subtract:
					return BlendOperation.Subtract;

				case SceneBlendOperation.ReverseSubtract:
					return BlendOperation.ReverseSubtract;

				case SceneBlendOperation.Min:
					return BlendOperation.Minimum;

				case SceneBlendOperation.Max:
					return BlendOperation.Maximum;
			}
			;

			return (BlendOperation)0x7fffffff; //D3DBLENDOP_FORCE_DWORD
		}

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom CompareFunction value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static Compare ConvertEnum( CompareFunction func )
		{
			switch ( func )
			{
				case CompareFunction.AlwaysFail:
					return Compare.Never;

				case CompareFunction.AlwaysPass:
					return Compare.Always;

				case CompareFunction.Less:
					return Compare.Less;

				case CompareFunction.LessEqual:
					return Compare.LessEqual;

				case CompareFunction.Equal:
					return Compare.Equal;

				case CompareFunction.NotEqual:
					return Compare.NotEqual;

				case CompareFunction.GreaterEqual:
					return Compare.GreaterEqual;

				case CompareFunction.Greater:
					return Compare.Greater;
			}
			;

			return 0;
		}

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom CullingMode value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static Cull ConvertEnum( CullingMode mode, bool flip )
		{
			switch ( mode )
			{
				case CullingMode.None:
					return Cull.None;

				case CullingMode.Clockwise:
					return flip ? Cull.Counterclockwise : Cull.Clockwise;

				case CullingMode.CounterClockwise:
					return flip ? Cull.Clockwise : Cull.Counterclockwise;
			}

			return 0;
		}

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom FogMode value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static FogMode ConvertEnum( Graphics.FogMode mode )
		{
			// convert the fog mode value
			switch ( mode )
			{
				case Graphics.FogMode.Exp:
					return FogMode.Exponential;

				case Graphics.FogMode.Exp2:
					return FogMode.ExponentialSquared;

				case Graphics.FogMode.Linear:
					return FogMode.Linear;
			}
			; // switch

			return (FogMode)0x7fffffff; //D3DFOG_FORCE_DWORD
		}

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom PolygonMode value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static FillMode ConvertEnum( PolygonMode mode )
		{
			switch ( mode )
			{
				case PolygonMode.Points:
					return FillMode.Point;

				case PolygonMode.Wireframe:
					return FillMode.Wireframe;

				case PolygonMode.Solid:
					return FillMode.Solid;
			}
			;

			return (FillMode)0x7fffffff; //D3DFILL_FORCE_DWORD
		}

		/// <summary>
		/// Return a D3D9 equivalent for a Axiom StencilOperation value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public static D3D9.StencilOperation ConvertEnum( Graphics.StencilOperation op, bool invert = false )
#else
		public static StencilOperation ConvertEnum( Graphics.StencilOperation op, bool invert )
#endif
		{
			switch ( op )
			{
				case Graphics.StencilOperation.Keep:
					return StencilOperation.Keep;

				case Graphics.StencilOperation.Zero:
					return StencilOperation.Zero;

				case Graphics.StencilOperation.Replace:
					return StencilOperation.Replace;

				case Graphics.StencilOperation.Increment:
					return invert ? StencilOperation.DecrementSaturate : StencilOperation.IncrementSaturate;

				case Graphics.StencilOperation.Decrement:
					return invert ? StencilOperation.IncrementSaturate : StencilOperation.DecrementSaturate;

				case Graphics.StencilOperation.IncrementWrap:
					return invert ? StencilOperation.Decrement : StencilOperation.Increment;

				case Graphics.StencilOperation.DecrementWrap:
					return invert ? StencilOperation.Increment : StencilOperation.Decrement;

				case Graphics.StencilOperation.Invert:
					return StencilOperation.Invert;
			}

			return 0;
		}

#if !NET_40
		/// <see cref="D3D9Helper.ConvertEnum(Axiom.Graphics.StencilOperation, bool)"/>
		public static StencilOperation ConvertEnum( Graphics.StencilOperation op )
		{
			return ConvertEnum( op, false );
		}
#endif

		/// <summary>
		/// Return a D3D9 state type for Axiom FilterType value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static SamplerState ConvertEnum( FilterType type )
		{
			switch ( type )
			{
				case FilterType.Min:
					return SamplerState.MinFilter;

				case FilterType.Mag:
					return SamplerState.MagFilter;

				case FilterType.Mip:
					return SamplerState.MipFilter;
			}
			;

			// to keep compiler happy
			return SamplerState.MinFilter;
		}

		/// <summary>
		/// Return a D3D9 filter option for Axiom FilterType & FilterOption value
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static TextureFilter ConvertEnum( FilterType type, FilterOptions options, Capabilities devCaps, D3D9TextureType texType )
		{
			// Assume normal
			FilterCaps capsType = devCaps.TextureFilterCaps;

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
								if ( ( capsType & FilterCaps.MinAnisotropic ) == FilterCaps.MinAnisotropic )
								{
									return TextureFilter.Anisotropic;
								}
								break;

							case FilterOptions.Linear:
								if ( ( capsType & FilterCaps.MinLinear ) == FilterCaps.MinLinear )
								{
									return TextureFilter.Linear;
								}
								break;

							case FilterOptions.Point:
							case FilterOptions.None:
								return TextureFilter.Point;
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
								if ( ( capsType & FilterCaps.MagAnisotropic ) == FilterCaps.MagAnisotropic )
								{
									return TextureFilter.Anisotropic;
								}
								break;

							case FilterOptions.Linear:
								if ( ( capsType & FilterCaps.MagLinear ) == FilterCaps.MagLinear )
								{
									return TextureFilter.Linear;
								}
								break;

							case FilterOptions.Point:
							case FilterOptions.None:
								return TextureFilter.Point;
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
								if ( ( capsType & FilterCaps.MipLinear ) == FilterCaps.MipLinear )
								{
									return TextureFilter.Linear;
								}
								break;

							case FilterOptions.Point:
								if ( ( capsType & FilterCaps.MipPoint ) == FilterCaps.MipPoint )
								{
									return TextureFilter.Point;
								}
								break;

							case FilterOptions.None:
								return TextureFilter.None;
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
		public static Usage ConvertEnum( BufferUsage usage )
		{
			Usage ret = 0;

			if ( ( usage & BufferUsage.Dynamic ) != 0 )
			{
#if AXIOM_D3D_MANAGE_BUFFERS
				// Only add the dynamic flag for the default pool, and
				// we use default pool when buffer is discardable
				if ( ( usage & BufferUsage.Discardable ) != 0 )
				{
					ret |= Usage.Dynamic;
				}
#else
				ret |= D3D9.Usage.Dynamic;
#endif
			}
			if ( ( usage & BufferUsage.WriteOnly ) != 0 )
			{
				ret |= Usage.WriteOnly;
			}

			return ret;
		}

		/// <summary>
		/// Get lock options
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static LockFlags ConvertEnum( BufferLocking locking, BufferUsage usage )
		{
			LockFlags ret = 0;
			if ( locking == BufferLocking.Discard )
			{
#if AXIOM_D3D_MANAGE_BUFFERS
				// Only add the discard flag for dynamic usgae and default pool
				if ( ( usage & BufferUsage.Dynamic ) != 0 && ( usage & BufferUsage.Discardable ) != 0 )
				{
					ret |= LockFlags.Discard;
				}
#else
    // D3D doesn't like discard or no_overwrite on non-dynamic buffers
				if ((usage & BufferUsage.Dynamic) != 0)
					ret |= D3D9.LockFlags.Discard;
#endif
			}
			if ( locking == BufferLocking.ReadOnly )
			{
				// D3D debug runtime doesn't like you locking managed buffers readonly
				// when they were created with write-only (even though you CAN read
				// from the software backed version)
				if ( ( usage & BufferUsage.WriteOnly ) == 0 )
				{
					ret |= LockFlags.ReadOnly;
				}
			}
			if ( locking == BufferLocking.NoOverwrite )
			{
#if AXIOM_D3D_MANAGE_BUFFERS
				// Only add the nooverwrite flag for dynamic usgae and default pool
				if ( ( usage & BufferUsage.Dynamic ) != 0 && ( usage & BufferUsage.Discardable ) != 0 )
				{
					ret |= LockFlags.NoOverwrite;
				}
#else
    // D3D doesn't like discard or no_overwrite on non-dynamic buffers
				if ((usage & BufferUsage.Dynamic) != 0)
					ret |= D3D9.LockFlags.NoOverwrite;
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
		public static DeclarationType ConvertEnum( VertexElementType type )
		{
			// we only need to worry about a few types with D3D
			switch ( type )
			{
				case VertexElementType.Color_ABGR:
				case VertexElementType.Color_ARGB:
				case VertexElementType.Color:
					return DeclarationType.Color;

				case VertexElementType.Float1:
					return DeclarationType.Float1;

				case VertexElementType.Float2:
					return DeclarationType.Float2;

				case VertexElementType.Float3:
					return DeclarationType.Float3;

				case VertexElementType.Float4:
					return DeclarationType.Float4;

				case VertexElementType.Short2:
					return DeclarationType.Short2;

				case VertexElementType.Short4:
					return DeclarationType.Short4;

				case VertexElementType.UByte4:
					return DeclarationType.Ubyte4;
			}
			; // switch

			// keep the compiler happy
			return DeclarationType.Float3;
		}

		/// <summary>
		/// Get vertex semantic
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static DeclarationUsage ConvertEnum( VertexElementSemantic semantic )
		{
			switch ( semantic )
			{
				case VertexElementSemantic.BlendIndices:
					return DeclarationUsage.BlendIndices;

				case VertexElementSemantic.BlendWeights:
					return DeclarationUsage.BlendWeight;

				case VertexElementSemantic.Diffuse:
					// index makes the difference (diffuse - 0)
					return DeclarationUsage.Color;

				case VertexElementSemantic.Specular:
					// index makes the difference (specular - 1)
					return DeclarationUsage.Color;

				case VertexElementSemantic.Normal:
					return DeclarationUsage.Normal;

				case VertexElementSemantic.Position:
					return DeclarationUsage.Position;

				case VertexElementSemantic.TexCoords:
					return DeclarationUsage.TextureCoordinate;

				case VertexElementSemantic.Binormal:
					return DeclarationUsage.Binormal;

				case VertexElementSemantic.Tangent:
					return DeclarationUsage.Tangent;
			}
			; // switch

			// keep the compiler happy
			return DeclarationUsage.Position;
		}

		/// <summary>
		/// Utility method, convert D3D9 pixel format to Axiom pixel format
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		[OgreVersion( 1, 7, 2 )]
		public static PixelFormat ConvertEnum( Format format )
		{
			switch ( format )
			{
				case Format.A8:
					return PixelFormat.A8;

				case Format.L8:
					return PixelFormat.L8;

				case Format.L16:
					return PixelFormat.L16;

				case Format.A4L4:
					return PixelFormat.A4L4;

				case Format.A8L8: // Assume little endian here
					return PixelFormat.A8L8;

				case Format.R3G3B2:
					return PixelFormat.R3G3B2;

				case Format.A1R5G5B5:
					return PixelFormat.A1R5G5B5;

				case Format.A4R4G4B4:
					return PixelFormat.A4R4G4B4;

				case Format.R5G6B5:
					return PixelFormat.R5G6B5;

				case Format.R8G8B8:
					return PixelFormat.R8G8B8;

				case Format.X8R8G8B8:
					return PixelFormat.X8R8G8B8;

				case Format.A8R8G8B8:
					return PixelFormat.A8R8G8B8;

				case Format.X8B8G8R8:
					return PixelFormat.X8B8G8R8;

				case Format.A8B8G8R8:
					return PixelFormat.A8B8G8R8;

				case Format.A2R10G10B10:
					return PixelFormat.A2R10G10B10;

				case Format.A2B10G10R10:
					return PixelFormat.A2B10G10R10;

				case Format.R16F:
					return PixelFormat.FLOAT16_R;

				case Format.A16B16G16R16F:
					return PixelFormat.FLOAT16_RGBA;

				case Format.R32F:
					return PixelFormat.FLOAT32_R;

				case Format.A32B32G32R32F:
					return PixelFormat.FLOAT32_RGBA;

				case Format.A16B16G16R16:
					return PixelFormat.SHORT_RGBA;

				case Format.Dxt1:
					return PixelFormat.DXT1;

				case Format.Dxt2:
					return PixelFormat.DXT2;

				case Format.Dxt3:
					return PixelFormat.DXT3;

				case Format.Dxt4:
					return PixelFormat.DXT4;

				case Format.Dxt5:
					return PixelFormat.DXT5;

				default:
					return PixelFormat.Unknown;
			}
		}

		/// <summary>
		/// Utility method, convert Axiom pixel format to D3D9 pixel format
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static Format ConvertEnum( PixelFormat format )
		{
			switch ( format )
			{
				case PixelFormat.A8:
					return Format.A8;

				case PixelFormat.L8:
					return Format.L8;

				case PixelFormat.L16:
					return Format.L16;

				case PixelFormat.A4L4:
					return Format.A4L4;

				case PixelFormat.A8L8:
					return Format.A8L8; // Assume little endian here

				case PixelFormat.R3G3B2:
					return Format.R3G3B2;

				case PixelFormat.A1R5G5B5:
					return Format.A1R5G5B5;

				case PixelFormat.A4R4G4B4:
					return Format.A4R4G4B4;

				case PixelFormat.R5G6B5:
					return Format.R5G6B5;

				case PixelFormat.R8G8B8:
					return Format.R8G8B8;

				case PixelFormat.X8R8G8B8:
					return Format.X8R8G8B8;

				case PixelFormat.A8R8G8B8:
					return Format.A8R8G8B8;

				case PixelFormat.X8B8G8R8:
					return Format.X8B8G8R8;

				case PixelFormat.A8B8G8R8:
					return Format.A8B8G8R8;

				case PixelFormat.A2R10G10B10:
					return Format.A2R10G10B10;

				case PixelFormat.A2B10G10R10:
					return Format.A2B10G10R10;

				case PixelFormat.FLOAT16_R:
					return Format.R16F;

				case PixelFormat.FLOAT16_GR:
					return Format.G16R16F;

				case PixelFormat.FLOAT16_RGBA:
					return Format.A16B16G16R16F;

				case PixelFormat.FLOAT32_R:
					return Format.R32F;

				case PixelFormat.FLOAT32_GR:
					return Format.G32R32F;

				case PixelFormat.FLOAT32_RGBA:
					return Format.A32B32G32R32F;

				case PixelFormat.SHORT_RGBA:
					return Format.A16B16G16R16;

				case PixelFormat.SHORT_GR:
					return Format.G16R16;

				case PixelFormat.DXT1:
					return Format.Dxt1;

				case PixelFormat.DXT2:
					return Format.Dxt2;

				case PixelFormat.DXT3:
					return Format.Dxt3;

				case PixelFormat.DXT4:
					return Format.Dxt4;

				case PixelFormat.DXT5:
					return Format.Dxt5;

				default:
					return Format.Unknown;
			}
			;
		}

		#endregion ConvertEnum overloads

		#region Matrix

		/// <summary>
		/// Convert matrix to D3D style
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static Matrix MakeD3DMatrix( Matrix4 matrix )
		{
			// Transpose matrix
			// D3D9 uses row vectors i.e. V*M
			// Ogre, OpenGL and everything else uses column vectors i.e. M*V

			var dxMat = new Matrix();

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
		public static Matrix4 ConvertD3DMatrix( ref Matrix d3DMat )
		{
			Matrix4 mat = Matrix4.Zero;

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
		public static Color ToColor( ColorEx color )
		{
			return Color.FromArgb( (int)( color.a < 1.0f ? color.a * 255.0f : color.a ), (int)( color.r * 255.0f ), (int)( color.g * 255.0f ), (int)( color.b * 255.0f ) );
		}

		/// <summary>
		/// Checks D3D matrix to see if it an identity matrix.
		/// </summary>
		/// <remarks>
		/// For whatever reason, the equality operator overloads for the D3D Matrix
		/// struct are extremely slow....
		/// </remarks>
		[AxiomHelper( 0, 9 )]
		public static bool IsIdentity( ref Matrix matrix )
		{
			if ( matrix.M11 == 1.0f && matrix.M12 == 0.0f && matrix.M13 == 0.0f && matrix.M14 == 0.0f && matrix.M21 == 0.0f && matrix.M22 == 1.0f && matrix.M23 == 0.0f && matrix.M24 == 0.0f && matrix.M31 == 0.0f && matrix.M32 == 0.0f && matrix.M33 == 1.0f && matrix.M34 == 0.0f && matrix.M41 == 0.0f && matrix.M42 == 0.0f && matrix.M43 == 0.0f && matrix.M44 == 1.0f )
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
		public static PixelFormat GetClosestSupported( PixelFormat format )
		{
			if ( ConvertEnum( format ) != Format.Unknown )
			{
				return format;
			}

			switch ( format )
			{
				case PixelFormat.B5G6R5:
					return PixelFormat.R5G6B5;

				case PixelFormat.B8G8R8:
					return PixelFormat.R8G8B8;

				case PixelFormat.B8G8R8A8:
					return PixelFormat.A8R8G8B8;

				case PixelFormat.FLOAT16_RGB:
					return PixelFormat.FLOAT16_RGBA;

				case PixelFormat.FLOAT32_RGB:
					return PixelFormat.FLOAT32_RGBA;

				case PixelFormat.Unknown:
				default:
					return PixelFormat.A8R8G8B8;
			}
		}
	};
}
