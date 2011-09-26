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

using System.Windows.Graphics;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BufferUsage = Microsoft.Xna.Framework.Graphics.BufferUsage;
using CompareFunction = Microsoft.Xna.Framework.Graphics.CompareFunction;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using StencilOperation = Microsoft.Xna.Framework.Graphics.StencilOperation;
using Vector3 = Microsoft.Xna.Framework.Vector3;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	///		Helper class for Xna that includes conversion functions and things that are
	///		specific to XFG.
	/// </summary>
	internal static class XnaHelper
	{
		/// <summary>
		///		Enumerates driver information and their supported display modes.
		/// </summary>
		public static DriverCollection GetDriverInfo()
		{
			var driverList = new DriverCollection();

#if SILVERLIGHT
			driverList.Add(new Driver(GraphicsDeviceManager.Current.GraphicsDevice.Adapter));
#else
			foreach (var adapterInfo in GraphicsAdapter.Adapters)
			{
				var driver = new Driver( adapterInfo );

				int lastWidth = 0, lastHeight = 0;
				SurfaceFormat lastFormat = 0;

				foreach ( var mode in adapterInfo.SupportedDisplayModes )
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
#endif

			return driverList;
		}

		public static Color Convert( ColorEx color )
		{
			return new Color( (byte)( color.r*255 ), (byte)( color.g*255 ), (byte)( color.b*255 ), (byte)( color.a*255 ) );
		}

		public static ColorEx Convert( Color color )
		{
			return new ColorEx( color.R/255.0f, color.G/255.0f, color.B/255.0f, color.A/255.0f );
		}

		public static Matrix Convert( Matrix4 matrix )
		{
			var xnaMat = new Matrix();

			// set it to a transposed matrix since Xna uses row vectors
			xnaMat.M11 = matrix.m00;
			xnaMat.M12 = matrix.m10;
			xnaMat.M13 = matrix.m20;
			xnaMat.M14 = matrix.m30;
			xnaMat.M21 = matrix.m01;
			xnaMat.M22 = matrix.m11;
			xnaMat.M23 = matrix.m21;
			xnaMat.M24 = matrix.m31;
			xnaMat.M31 = matrix.m02;
			xnaMat.M32 = matrix.m12;
			xnaMat.M33 = matrix.m22;
			xnaMat.M34 = matrix.m32;
			xnaMat.M41 = matrix.m03;
			xnaMat.M42 = matrix.m13;
			xnaMat.M43 = matrix.m23;
			xnaMat.M44 = matrix.m33;

			return xnaMat;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="options"></param>
		/// <param name="caps"></param>
		/// <param name="texType"></param>
		/// <returns></returns>
		public static TextureFilter Convert( FilterType type, Graphics.FilterOptions options, XnaTextureType texType )
		{
			// setting a default val here to keep compiler from complaining about using unassigned value types

			switch ( type )
			{
				case FilterType.Min:
				{
					switch ( options )
					{
                        case Graphics.FilterOptions.Anisotropic:
							return TextureFilter.Anisotropic;

                        case Graphics.FilterOptions.Linear:
							return TextureFilter.Linear;

                        case Graphics.FilterOptions.Point:
                        case Graphics.FilterOptions.None:
							return TextureFilter.Point;
					}
					break;
				}
				case FilterType.Mag:
				{
					switch ( options )
					{
                        case Graphics.FilterOptions.Anisotropic:
							return TextureFilter.Anisotropic;

                        case Graphics.FilterOptions.Linear:
							return TextureFilter.Linear;

                        case Graphics.FilterOptions.Point:
                        case Graphics.FilterOptions.None:
							return TextureFilter.Point;
					}
					break;
				}
				case FilterType.Mip:
				{
					switch ( options )
					{
                        case Graphics.FilterOptions.Anisotropic:
                        case Graphics.FilterOptions.Linear:
							return TextureFilter.Linear;

                        case Graphics.FilterOptions.Point:
							return TextureFilter.Point;

                        case Graphics.FilterOptions.None:
							return TextureFilter.Point;
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
		public static BlendFunction Convert( LayerBlendOperationEx blendop )
		{
		    BlendFunction xnaTexOp = 0;


		    // figure out what is what
		    switch ( blendop )
		    {
                //case LayerBlendOperationEx.Source1:
                //    xnaTexOp = BlendFunction.SelectArg1;
                //    break;

                //case LayerBlendOperationEx.Source2:
                //    xnaTexOp = BlendFunction.SelectArg2;
                //    break;

                //case LayerBlendOperationEx.Modulate:
                //    xnaTexOp = BlendFunction.moXFG.TextureOperation.Modulate;
                //    break;

                //case LayerBlendOperationEx.ModulateX2:
                //    xnaTexOp = BlendFunction.Modulate2X;
                //    break;

                //case LayerBlendOperationEx.ModulateX4:
                //    xnaTexOp = BlendFunction.Modulate4X;
                //    break;

		        case LayerBlendOperationEx.Add:
		            xnaTexOp = BlendFunction.Add;
		            break;

		        case LayerBlendOperationEx.AddSigned:
		            xnaTexOp = BlendFunction.Add;
		            break;

		        case LayerBlendOperationEx.AddSmooth:
		            xnaTexOp = BlendFunction.Add;
		            break;

		        case LayerBlendOperationEx.Subtract:
		            xnaTexOp = BlendFunction.Subtract;
		            break;

                //case LayerBlendOperationEx.BlendDiffuseAlpha:
                //    xnaTexOp = BlendFunction.BlendDiffuseAlpha;
                //    break;

                //case LayerBlendOperationEx.BlendTextureAlpha:
                //    xnaTexOp = BlendFunction.BlendTextureAlpha;
                //    break;

                //case LayerBlendOperationEx.BlendCurrentAlpha:
                //    xnaTexOp = BlendFunction.BlendCurrentAlpha;
                //    break;

                //case LayerBlendOperationEx.BlendManual:
                //    xnaTexOp = BlendFunction.BlendFactorAlpha;
                //    break;

                //case LayerBlendOperationEx.DotProduct:
                //    if ( Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.Dot3 ))
                //        xnaTexOp = BlendFunction.DotProduct3;
                //    else
                //        xnaTexOp = BlendFunction.Modulate;
                //    break;

		        default:
		            xnaTexOp = BlendFunction.Add;
		            break;
		    } // end switch

		    return xnaTexOp;
		}

	    /*  public static XFG.TextureArgument Convert( LayerBlendSource blendSource )
		  {
			  XFG.TextureArgument d3dTexArg = 0;

			  switch ( blendSource )
			  {
				  case LayerBlendSource.Current:
					  d3dTexArg = XFG.TextureArgument.Current;
					  break;

				  case LayerBlendSource.Texture:
					  d3dTexArg = XFG.TextureArgument.TextureColor;
					  break;

				  case LayerBlendSource.Diffuse:
					  d3dTexArg = XFG.TextureArgument.Diffuse;
					  break;

				  case LayerBlendSource.Specular:
					  d3dTexArg = XFG.TextureArgument.Specular;
					  break;

				  case LayerBlendSource.Manual:
					  d3dTexArg = XFG.TextureArgument.TFactor;
					  break;
			  } // end switch

			  return d3dTexArg;
		  }*/

		/// <summary>
		///		Helper method to convert Axiom scene blend factors to Xna
		/// </summary>
		/// <param name="factor"></param>
		/// <returns></returns>
		public static Blend Convert( SceneBlendFactor factor )
		{
			Blend xnaBlend = 0;

			switch ( factor )
			{
				case SceneBlendFactor.One:
					xnaBlend = Blend.One;
					break;
				case SceneBlendFactor.Zero:
					xnaBlend = Blend.Zero;
					break;
				case SceneBlendFactor.DestColor:
					xnaBlend = Blend.DestinationColor;
					break;
				case SceneBlendFactor.SourceColor:
					xnaBlend = Blend.SourceColor;
					break;
				case SceneBlendFactor.OneMinusDestColor:
					xnaBlend = Blend.InverseDestinationColor;
					break;
				case SceneBlendFactor.OneMinusSourceColor:
					xnaBlend = Blend.InverseSourceColor;
					break;
				case SceneBlendFactor.DestAlpha:
					xnaBlend = Blend.DestinationAlpha;
					break;
				case SceneBlendFactor.SourceAlpha:
					xnaBlend = Blend.SourceAlpha;
					break;
				case SceneBlendFactor.OneMinusDestAlpha:
					xnaBlend = Blend.InverseDestinationAlpha;
					break;
				case SceneBlendFactor.OneMinusSourceAlpha:
					xnaBlend = Blend.InverseSourceAlpha;
					break;
			}

			return xnaBlend;
		}

		public static BlendFunction Convert(SceneBlendOperation op)
		{
			switch (op)
			{
				case SceneBlendOperation.Add:
					return BlendFunction.Add;
				case SceneBlendOperation.Subtract:
					return BlendFunction.Subtract;
				case SceneBlendOperation.Min:
					return BlendFunction.Min;
				case SceneBlendOperation.Max:
					return BlendFunction.Max;
				case SceneBlendOperation.ReverseSubtract:
					return BlendFunction.ReverseSubtract;
			}
			return 0;
		}

		public static VertexElementFormat Convert( VertexElementType type, bool tex )
		{
			// if (tex)
			//   return XFG.Graphics.VertexElementFormat.Unused;

			// we only need to worry about a few types with Xna
			switch ( type )
			{
				case VertexElementType.Color:
					return VertexElementFormat.Color;

				case VertexElementType.Float1:
					return VertexElementFormat.Single;

				case VertexElementType.Float2:
					return VertexElementFormat.Vector2;

				case VertexElementType.Float3:
					return VertexElementFormat.Vector3;

				case VertexElementType.Float4:
					return VertexElementFormat.Vector4;

				case VertexElementType.Short2:
					return VertexElementFormat.Short2;
					//case VertexElementType.Short3:
					//return XFG.VertexElementFormat.Short2;

				case VertexElementType.Short4:
					return VertexElementFormat.Short4;

				case VertexElementType.UByte4:
					return VertexElementFormat.Byte4;
			} // switch

			// keep the compiler happy
			return VertexElementFormat.Vector3; // Float3;
		}

		public static VertexElementUsage Convert( VertexElementSemantic semantic )
		{
			switch ( semantic )
			{
				case VertexElementSemantic.BlendIndices:
					return VertexElementUsage.BlendIndices;

				case VertexElementSemantic.BlendWeights:
					return VertexElementUsage.BlendWeight;

				case VertexElementSemantic.Diffuse:
					// index makes the difference (diffuse - 0)
					return VertexElementUsage.Color;

				case VertexElementSemantic.Specular:
					// index makes the difference (specular - 1)
					return VertexElementUsage.Color;

				case VertexElementSemantic.Normal:
					return VertexElementUsage.Normal;

				case VertexElementSemantic.Position:
					return VertexElementUsage.Position;

				case VertexElementSemantic.TexCoords:
					return VertexElementUsage.TextureCoordinate;

				case VertexElementSemantic.Binormal:
					return VertexElementUsage.Binormal;

				case VertexElementSemantic.Tangent:
					return VertexElementUsage.Tangent;
			} // switch

			// keep the compiler happy
			return VertexElementUsage.Position;
		}

		public static BufferUsage Convert( Graphics.BufferUsage usage )
		{
			BufferUsage xnaUsage = 0;
			if ( usage == Graphics.BufferUsage.Dynamic ||
				 usage == Graphics.BufferUsage.DynamicWriteOnly )
			{
				xnaUsage |= BufferUsage.WriteOnly;
			}

			if ( usage == Graphics.BufferUsage.WriteOnly ||
				 usage == Graphics.BufferUsage.StaticWriteOnly ||
				 usage == Graphics.BufferUsage.DynamicWriteOnly )
			{
				xnaUsage |= BufferUsage.WriteOnly;
			}

			return xnaUsage;
		}

/*
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
#if !(XBOX || XBOX360 || SILVERLIGHT)
		public static XFG.FogMode Convert( Axiom.Graphics.FogMode mode )
		{
			// convert the fog mode value
			switch ( mode )
			{
				case Axiom.Graphics.FogMode.Exp:
					return XFG.FogMode.Exponent;

				case Axiom.Graphics.FogMode.Exp2:
					return XFG.FogMode.ExponentSquared;

				case Axiom.Graphics.FogMode.Linear:
					return XFG.FogMode.Linear;
			} // switch

			return 0;
		}
#endif
*/
		/*     public static XFG.LockFlags Convert( BufferLocking locking )
			 {
				 //no lock in xna
				 XFG.LockFlags d3dLockFlags = 0;

				 if ( locking == BufferLocking.Discard )
					 d3dLockFlags |= XFG.LockFlags.Discard;
				 if ( locking == BufferLocking.ReadOnly )
					 d3dLockFlags |= XFG.LockFlags.ReadOnly;
				 if ( locking == BufferLocking.NoOverwrite )
					 d3dLockFlags |= XFG.LockFlags.NoOverwrite;
			
				 return 0;
			 }*/

		public static int Convert( TexCoordCalcMethod method )
		{
			/*
			switch ( method )
			{
				case  TexCoordCalcMethod.None:
					return (int)XFG.TextureCoordinateIndex.PassThru;

				case TexCoordCalcMethod.EnvironmentMapReflection:
					return TexCoordCalcMethod.EnvironmentMapPlanar;// (int)XFG.TextureCoordinateIndex.CameraSpaceReflectionVector;

				case TexCoordCalcMethod.EnvironmentMapPlanar:
					//return (int)XFG.TextureCoordinateIndex.CameraSpacePosition;
					if ( caps.VertexProcessingCaps.SupportsTextureGenerationSphereMap )
					{
						// use sphere map if available
						return TexCoordCalcMethod.EnvironmentMapPlanar;// (int)XFG.TextureCoordinateIndex.SphereMap;
					}
					else
					{
						// If not, fall back on camera space reflection vector which isn't as good
						return TexCoordCalcMethod.EnvironmentMapReflection;// (int)XFG.TextureCoordinateIndex.CameraSpaceReflectionVector;
					}

				case TexCoordCalcMethod.EnvironmentMapNormal:
					return TexCoordCalcMethod.EnvironmentMapNormal;// (int)XFG.TextureCoordinateIndex.CameraSpaceNormal;

				case TexCoordCalcMethod.EnvironmentMap:
					if ( caps.VertexProcessingCaps.SupportsTextureGenerationSphereMap )
					{
						return TexCoordCalcMethod.EnvironmentMap;// (int)XFG.TextureCoordinateIndex.SphereMap;
					}
					else
					{
						// fall back on camera space normal if sphere map isnt supported
						return TexCoordCalcMethod.None;// (int)XFG.TextureCoordinateIndex.CameraSpaceNormal;
					}

				case TexCoordCalcMethod.ProjectiveTexture:
					return TexCoordCalcMethod.None;// (int)XFG.TextureCoordinateIndex.CameraSpacePosition;
			} // switch
			*/
			return 1;
		}

		public static XnaTextureType Convert( TextureType type )
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

		public static TextureAddressMode Convert( TextureAddressing type )
		{
			// convert from ours to Xna
			switch ( type )
			{
				case TextureAddressing.Wrap:
					return TextureAddressMode.Wrap;

				case TextureAddressing.Mirror:
					return TextureAddressMode.Mirror;

				case TextureAddressing.Clamp:
					return TextureAddressMode.Clamp;
			} // end switch

			return 0;
		}

		/// <summary>
		///    Converts our CompareFunction enum to the XFG.Compare equivalent.
		/// </summary>
		/// <param name="func"></param>
		/// <returns></returns>
		public static CompareFunction Convert( Graphics.CompareFunction func )
		{
			switch ( func )
			{
				case Graphics.CompareFunction.AlwaysFail:
					return CompareFunction.Never;

				case Graphics.CompareFunction.AlwaysPass:
					return CompareFunction.Always;

				case Graphics.CompareFunction.Equal:
					return CompareFunction.Equal;

				case Graphics.CompareFunction.Greater:
					return CompareFunction.Greater;

				case Graphics.CompareFunction.GreaterEqual:
					return CompareFunction.GreaterEqual;

				case Graphics.CompareFunction.Less:
					return CompareFunction.Less;

				case Graphics.CompareFunction.LessEqual:
					return CompareFunction.LessEqual;

				case Graphics.CompareFunction.NotEqual:
					return CompareFunction.NotEqual;
			}

			return 0;
		}

		public static Graphics.CompareFunction Convert( CompareFunction func )
		{
			switch ( func )
			{
				case CompareFunction.Never:
					return Graphics.CompareFunction.AlwaysFail;

				case CompareFunction.Always:
					return Graphics.CompareFunction.AlwaysPass;

				case CompareFunction.Equal:
					return Graphics.CompareFunction.Equal;

				case CompareFunction.Greater:
					return Graphics.CompareFunction.Greater;

				case CompareFunction.GreaterEqual:
					return Graphics.CompareFunction.GreaterEqual;

				case CompareFunction.Less:
					return Graphics.CompareFunction.Less;

				case CompareFunction.LessEqual:
					return Graphics.CompareFunction.LessEqual;

				case CompareFunction.NotEqual:
					return Graphics.CompareFunction.NotEqual;
			}

			return 0;
		}

		/// <summary>
		///    Converts our Shading enum to the D3D ShadingMode equivalent.
		/// </summary>
		/// <param name="shading"></param>
		/// <returns></returns>
		/*public static XFG.ShadeMode Convert( Shading shading )
		{
			switch ( shading )
			{
				case Shading.Flat:
					return XFG.ShadeMode.Flat;
				case Shading.Gouraud:
					return XFG.ShadeMode.Gouraud;
				case Shading.Phong:
					return XFG.ShadeMode.Phong;
			}

			return 0;
		}*/
		/// <summary>
		///    Converts the D3D ShadingMode to our Shading enum equivalent.
		/// </summary>
		/// <param name="shading"></param>
		/// <returns></returns>
		/*public static Shading Convert( XFG.ShadeMode shading )
		{
			switch ( shading )
			{
				case XFG.ShadeMode.Flat:
					return Shading.Flat;
				case XFG.ShadeMode.Gouraud:
					return Shading.Gouraud;
				case XFG.ShadeMode.Phong:
					return Shading.Phong;
			}

			return 0;
		}*/
		public static StencilOperation Convert( Graphics.StencilOperation op )
		{
			return Convert( op, false );
		}

		/// <summary>
		///    Converts our StencilOperation enum to the D3D StencilOperation equivalent.
		/// </summary>
		/// <param name="op"></param>
		/// <returns></returns>
		public static StencilOperation Convert( Graphics.StencilOperation op, bool invert )
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
					return invert
							   ? StencilOperation.DecrementSaturation
							   : StencilOperation.IncrementSaturation;

				case Graphics.StencilOperation.Decrement:
					return invert
							   ? StencilOperation.IncrementSaturation
							   : StencilOperation.DecrementSaturation;

				case Graphics.StencilOperation.IncrementWrap:
					return invert
							   ? StencilOperation.Decrement
							   : StencilOperation.Increment;

				case Graphics.StencilOperation.DecrementWrap:
					return invert
							   ? StencilOperation.Increment
							   : StencilOperation.Decrement;

				case Graphics.StencilOperation.Invert:
					return StencilOperation.Invert;
			}

			return 0;
		}

		public static CullMode Convert( CullingMode mode, bool flip )
		{
			switch ( mode )
			{
				case CullingMode.None:
					return CullMode.None;

				case CullingMode.Clockwise:
					return flip ? CullMode.CullCounterClockwiseFace : CullMode.CullClockwiseFace;

				case CullingMode.CounterClockwise:
					return flip ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace;
			}

			return 0;
		}

		/// <summary>
		///    Checks Xna matrix to see if it an identity matrix.
		/// </summary>
		/// <remarks>
		///    For whatever reason, the equality operator overloads for the Xna Matrix
		///    struct are extremely slow....
		/// </remarks>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static bool IsIdentity( Matrix matrix )
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

		public static Rectangle ToRectangle( Core.Rectangle rectangle )
		{
			var retVal = new Rectangle();
			retVal.X = (int)rectangle.Left;
			retVal.Y = (int)rectangle.Top;
			retVal.Width = (int)rectangle.Width;
			retVal.Height = (int)rectangle.Height;
			return retVal;
		}

		public static Rectangle ToRectangle( BasicBox rectangle )
		{
			var retVal = new Rectangle();
			retVal.X = rectangle.Left;
			retVal.Y = rectangle.Top;
			retVal.Width = rectangle.Width;
			retVal.Height = rectangle.Height;
			return retVal;
		}

		public static PixelFormat Convert( SurfaceFormat semantic )
		{
			switch ( semantic )
			{
                case SurfaceFormat.Color:
                    return PixelFormat.A8B8G8R8;
                case SurfaceFormat.Bgr565:
                    return PixelFormat.R5G6B5;
                case SurfaceFormat.Bgra5551:
					return PixelFormat.A1R5G5B5;
				case SurfaceFormat.Bgra4444:
					return PixelFormat.A4R4G4B4;
                //case SurfaceFormat.NormalizedByte2:
                //    return PixelFormat.BYTE_LA;
                //case SurfaceFormat.NormalizedByte4:
                //    return PixelFormat.A8R8G8B8;
#if !SILVERLIGHT
                case SurfaceFormat.Dxt1:
                    return PixelFormat.DXT1;
                case SurfaceFormat.Dxt3:
                    return PixelFormat.DXT3;
                case SurfaceFormat.Dxt5:
                    return PixelFormat.DXT5;
                case SurfaceFormat.Rgba1010102:
                    return PixelFormat.A2B10G10R10;
                case SurfaceFormat.Rg32:
                    return PixelFormat.SHORT_GR;
                case SurfaceFormat.Rgba64:
                    return PixelFormat.SHORT_RGBA;
                case SurfaceFormat.Alpha8:
					return PixelFormat.A8;
                case SurfaceFormat.Single:
                    return PixelFormat.FLOAT32_R;
                case SurfaceFormat.Vector2:
                    return PixelFormat.FLOAT32_GR;
                case SurfaceFormat.Vector4:
                    return PixelFormat.FLOAT32_RGBA;
                //case SurfaceFormat.HalfSingle:
                //case SurfaceFormat.HalfVector2:
                //case SurfaceFormat.HalfVector4:
                //case SurfaceFormat.HdrBlendable:
#endif
                default:
					return PixelFormat.Unknown;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		public static SurfaceFormat Convert( PixelFormat format )
		{
			switch ( format )
			{
                case PixelFormat.A8B8G8R8:
                    return SurfaceFormat.Color;
                case PixelFormat.R5G6B5:
                    return SurfaceFormat.Bgr565;
                case PixelFormat.A1R5G5B5:
                    return SurfaceFormat.Bgra5551;
                case PixelFormat.A4R4G4B4:
                    return SurfaceFormat.Bgra4444;
                //case PixelFormat.BYTE_LA:
                //    return SurfaceFormat.NormalizedByte2;
                //case PixelFormat.A8R8G8B8:
                //    return SurfaceFormat.NormalizedByte4;
#if !SILVERLIGHT
                case PixelFormat.DXT1:
                    return SurfaceFormat.Dxt1;
                case PixelFormat.DXT3:
                    return SurfaceFormat.Dxt3;
                case PixelFormat.DXT5:
                    return SurfaceFormat.Dxt5;
                case PixelFormat.A2B10G10R10:
                    return SurfaceFormat.Rgba1010102;
                case PixelFormat.SHORT_GR:
                    return SurfaceFormat.Rg32;
                case PixelFormat.SHORT_RGBA:
                    return SurfaceFormat.Rgba64;
                case PixelFormat.A8:
                    return SurfaceFormat.Alpha8;
                case PixelFormat.FLOAT32_R:
                    return SurfaceFormat.Single;
                case PixelFormat.FLOAT32_GR:
                    return SurfaceFormat.Vector2;
                case PixelFormat.FLOAT32_RGBA:
                    return SurfaceFormat.Vector4;
                    //return SurfaceFormat.HalfSingle;
                    //return SurfaceFormat.HalfVector2;
                    //return SurfaceFormat.HalfVector4;
                    //return SurfaceFormat.HdrBlendable;
#endif
                default:
					return (SurfaceFormat)(-1);
            }
		}

		public static PixelFormat GetClosestSupported( PixelFormat format )
		{
			if ( Convert( format ) != (SurfaceFormat)( -1 ) )
				return format;
			switch ( format )
			{
                case PixelFormat.B8G8R8:
			        return PixelFormat.A8B8G8R8;
                    //return PixelFormat.A8R8G8B8; // Would be R8G8B8 normaly but MDX doesn't like that format.
                case PixelFormat.B5G6R5:
                    return PixelFormat.R5G6B5;
                    //return PixelFormat.A1R5G5B5;
                    //return PixelFormat.A4R4G4B4;
                    //return PixelFormat.DXT1;
                    //return PixelFormat.DXT3;
                    //return PixelFormat.DXT5;
                    //return PixelFormat.BYTE_LA;
                    //return PixelFormat.A8R8G8B8;
                    //return PixelFormat.A2B10G10R10;
                    //return PixelFormat.SHORT_GR;
                    //return PixelFormat.SHORT_RGBA;
                case PixelFormat.L8:
                    return PixelFormat.A8;
                    //return PixelFormat.FLOAT32_R;
                    //return PixelFormat.FLOAT32_GR;
                case PixelFormat.FLOAT32_RGB:
                    return PixelFormat.FLOAT32_RGBA;
				case PixelFormat.Unknown:
				default:
			        return PixelFormat.A8B8G8R8;
                    //return PixelFormat.B8G8R8A8; // Color	(Unsigned format) 32-bit ARGB pixel format with alpha, using 8 bits per channel.
                    //return PixelFormat.A8R8G8B8;
            }
		}
	}
}