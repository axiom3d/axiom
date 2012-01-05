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

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using Microsoft.Xna.Framework.Graphics;

using BufferUsage = Axiom.Graphics.BufferUsage;
using CompareFunction = Axiom.Graphics.CompareFunction;
using FilterOptions = Axiom.Graphics.FilterOptions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

//big mess, tried to implement as much as possible but a lot of fixed pipeline function have disapeared.

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
			DriverCollection driverList = new DriverCollection();

			foreach( XFG.GraphicsAdapter adapterInfo in XFG.GraphicsAdapter.Adapters )
			{
				Driver driver = new Driver( adapterInfo );

				int lastWidth = 0, lastHeight = 0;
				XFG.SurfaceFormat lastFormat = 0;

				foreach( XFG.DisplayMode mode in adapterInfo.SupportedDisplayModes )
				{
					// filter out lower resolutions, and make sure this isnt a dupe (ignore variations on refresh rate)
					if( ( mode.Width >= 640 && mode.Height >= 480 ) &&
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

		public static XFG.Color Convert( Axiom.Core.ColorEx color )
		{
			return new XFG.Color( (byte)( color.r * 255 ), (byte)( color.g * 255 ), (byte)( color.b * 255 ), (byte)( color.a * 255 ) );
		}

		public static Axiom.Core.ColorEx Convert( XFG.Color color )
		{
			return new Axiom.Core.ColorEx( color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f );
		}

		public static XNA.Matrix Convert( Axiom.Math.Matrix4 matrix )
		{
			XNA.Matrix xnaMat = new XNA.Matrix();

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
		public static XFG.TextureFilter Convert( FilterType type, FilterOptions options, XFG.GraphicsDeviceCapabilities devCaps, XnaTextureType texType )
		{
			// setting a default val here to keep compiler from complaining about using unassigned value types
			XFG.GraphicsDeviceCapabilities.FilterCaps filterCaps = devCaps.TextureFilterCapabilities;

			switch( texType )
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

			switch( type )
			{
				case FilterType.Min:
				{
					switch( options )
					{
						case FilterOptions.Anisotropic:
							if( filterCaps.SupportsMinifyAnisotropic )
							{
								return XFG.TextureFilter.Anisotropic;
							}
							else
							{
								return XFG.TextureFilter.Linear;
							}

						case FilterOptions.Linear:
							if( filterCaps.SupportsMinifyLinear )
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
					switch( options )
					{
						case FilterOptions.Anisotropic:
							if( filterCaps.SupportsMagnifyAnisotropic )
							{
								return XFG.TextureFilter.Anisotropic;
							}
							else
							{
								return XFG.TextureFilter.Linear;
							}

						case FilterOptions.Linear:
							if( filterCaps.SupportsMagnifyLinear )
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
					switch( options )
					{
						case FilterOptions.Anisotropic:
						case FilterOptions.Linear:
							if( filterCaps.SupportsMipMapLinear )
							{
								return XFG.TextureFilter.Linear;
							}
							else
							{
								return XFG.TextureFilter.Point;
							}

						case FilterOptions.Point:
							if( filterCaps.SupportsMipMapPoint )
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

		/// <summary>
		///		Static method for converting LayerBlendOperationEx enum values to the Direct3D 
		///		TextureOperation enum.
		/// </summary>
		/// <param name="blendop"></param>
		/// <returns></returns>
		public static XFG.BlendFunction Convert( LayerBlendOperationEx blendop )
		{
			XFG.BlendFunction xnaTexOp = 0;

			// figure out what is what
			switch( blendop )
			{
					/*case LayerBlendOperationEx.Source1:
					d3dTexOp = XFG.Blend.BlendFunction.SelectArg1;
					break;

				case LayerBlendOperationEx.Source2:
					d3dTexOp = XFG.TextureOperation.SelectArg2;
					break;
				
				case LayerBlendOperationEx.Modulate:
					d3dTexOp = BlendFunction.moXFG.TextureOperation.Modulate;
					break;

				case LayerBlendOperationEx.ModulateX2:
					d3dTexOp = XFG.TextureOperation.Modulate2X;
					break;

				case LayerBlendOperationEx.ModulateX4:
					d3dTexOp = XFG.TextureOperation.Modulate4X;
					break;*/

				case LayerBlendOperationEx.Add:
					xnaTexOp = XFG.BlendFunction.Add;
					break;

				case LayerBlendOperationEx.AddSigned:
					xnaTexOp = XFG.BlendFunction.Add;
					break;

				case LayerBlendOperationEx.AddSmooth:
					xnaTexOp = XFG.BlendFunction.Add;
					break;

				case LayerBlendOperationEx.Subtract:
					xnaTexOp = XFG.BlendFunction.Subtract;
					break;
				default:
					xnaTexOp = XFG.BlendFunction.Add;
					break;

					/*  case LayerBlendOperationEx.BlendDiffuseAlpha:
					  d3dTexOp = XFG.BlendFunction..BlendDiffuseAlpha;
					  break;

				  case LayerBlendOperationEx.BlendTextureAlpha:
					  d3dTexOp = XFG.TextureOperation.BlendTextureAlpha;
					  break;

				  case LayerBlendOperationEx.BlendCurrentAlpha:
					  d3dTexOp = XFG.TextureOperation.BlendCurrentAlpha;
					  break;

				  case LayerBlendOperationEx.BlendManual:
					  d3dTexOp = XFG.TextureOperation.BlendFactorAlpha;
					  break;

				  case LayerBlendOperationEx.DotProduct:
					  if ( Root.Instance.RenderSystem.Caps.CheckCap( Capabilities.Dot3 ) )
					  {
						  d3dTexOp = XFG.TextureOperation.DotProduct3;
					  }
					  else
					  {
						  d3dTexOp = XFG.TextureOperation.Modulate;
					  }
					  break;*/
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
		public static XFG.Blend Convert( SceneBlendFactor factor )
		{
			XFG.Blend xnaBlend = 0;

			switch( factor )
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

		public static XFG.ShaderProfile Convert( string shaderVersion )
		{
			switch( shaderVersion )
			{
				case "PS_1_1":
					return XFG.ShaderProfile.PS_1_1;
				case "PS_1_2":
					return XFG.ShaderProfile.PS_1_2;
				case "PS_1_3":
					return XFG.ShaderProfile.PS_1_3;
				case "PS_1_4":
					return XFG.ShaderProfile.PS_1_4;
				case "PS_2_0":
					return XFG.ShaderProfile.PS_2_0;
				case "PS_2_A":
					return XFG.ShaderProfile.PS_2_A;
				case "PS_2_B":
					return XFG.ShaderProfile.PS_2_B;
				case "PS_2_SW":
					return XFG.ShaderProfile.PS_2_SW;
				case "PS_3_0":
					return XFG.ShaderProfile.PS_3_0;
				case "Unknown":
					return XFG.ShaderProfile.Unknown;
				case "VS_1_1":
					return XFG.ShaderProfile.VS_1_1;
				case "VS_2_0":
					return XFG.ShaderProfile.VS_2_0;
				case "VS_2_A":
					return XFG.ShaderProfile.VS_2_A;
				case "VS_2_SW":
					return XFG.ShaderProfile.VS_2_SW;
				case "VS_3_0":
					return XFG.ShaderProfile.VS_3_0;
				case "XPS_3_0":
					return XFG.ShaderProfile.XPS_3_0;
				case "XVS_3_0":
					return XFG.ShaderProfile.XVS_3_0;
			}
			return XFG.ShaderProfile.Unknown;
		}

		public static XFG.VertexElementFormat Convert( VertexElementType type, bool tex )
		{
			// if (tex)
			//   return XFG.Graphics.VertexElementFormat.Unused;

			// we only need to worry about a few types with Xna
			switch( type )
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
					//case VertexElementType.Short3:
					//return XFG.VertexElementFormat.Short2;

				case VertexElementType.Short4:
					return XFG.VertexElementFormat.Short4;

				case VertexElementType.UByte4:
					return XFG.VertexElementFormat.Byte4;
			} // switch

			// keep the compiler happy
			return XFG.VertexElementFormat.Vector3; // Float3;
		}

		public static XFG.VertexElementUsage Convert( VertexElementSemantic semantic )
		{
			switch( semantic )
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

		public static XFG.BufferUsage Convert( BufferUsage usage )
		{
			XFG.BufferUsage xnaUsage = 0;
			if( usage == BufferUsage.Dynamic ||
			    usage == BufferUsage.DynamicWriteOnly )
			{
				xnaUsage |= XFG.BufferUsage.WriteOnly;
			}

			if( usage == BufferUsage.WriteOnly ||
			    usage == BufferUsage.StaticWriteOnly ||
			    usage == BufferUsage.DynamicWriteOnly )
			{
				xnaUsage |= XFG.BufferUsage.WriteOnly;
			}

			return xnaUsage;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
#if !(XBOX || XBOX360 || SILVERLIGHT)
		public static XFG.FogMode Convert( Axiom.Graphics.FogMode mode )
		{
			// convert the fog mode value
			switch( mode )
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

		public static int Convert( TexCoordCalcMethod method, XFG.GraphicsDeviceCapabilities caps )
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
			switch( type )
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

		public static XFG.TextureAddressMode Convert( TextureAddressing type )
		{
			// convert from ours to Xna
			switch( type )
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
		///    Converts our CompareFunction enum to the XFG.Compare equivalent.
		/// </summary>
		/// <param name="func"></param>
		/// <returns></returns>
		public static XFG.CompareFunction Convert( CompareFunction func )
		{
			switch( func )
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
		public static XFG.StencilOperation Convert( Axiom.Graphics.StencilOperation op )
		{
			return Convert( op, false );
		}

		/// <summary>
		///    Converts our StencilOperation enum to the D3D StencilOperation equivalent.
		/// </summary>
		/// <param name="op"></param>
		/// <returns></returns>
		public static XFG.StencilOperation Convert( Axiom.Graphics.StencilOperation op, bool invert )
		{
			switch( op )
			{
				case Axiom.Graphics.StencilOperation.Keep:
					return XFG.StencilOperation.Keep;

				case Axiom.Graphics.StencilOperation.Zero:
					return XFG.StencilOperation.Zero;

				case Axiom.Graphics.StencilOperation.Replace:
					return XFG.StencilOperation.Replace;

				case Axiom.Graphics.StencilOperation.Increment:
					return invert ?
					              	XFG.StencilOperation.DecrementSaturation : XFG.StencilOperation.IncrementSaturation;

				case Axiom.Graphics.StencilOperation.Decrement:
					return invert ?
					              	XFG.StencilOperation.IncrementSaturation : XFG.StencilOperation.DecrementSaturation;

				case Axiom.Graphics.StencilOperation.IncrementWrap:
					return invert ?
					              	XFG.StencilOperation.Decrement : XFG.StencilOperation.Increment;

				case Axiom.Graphics.StencilOperation.DecrementWrap:
					return invert ?
					              	XFG.StencilOperation.Increment : XFG.StencilOperation.Decrement;

				case Axiom.Graphics.StencilOperation.Invert:
					return XFG.StencilOperation.Invert;
			}

			return 0;
		}

		public static XFG.CullMode Convert( Axiom.Graphics.CullingMode mode, bool flip )
		{
			switch( mode )
			{
				case CullingMode.None:
					return XFG.CullMode.None;

				case CullingMode.Clockwise:
					return flip ? XFG.CullMode.CullCounterClockwiseFace : XFG.CullMode.CullClockwiseFace;

				case CullingMode.CounterClockwise:
					return flip ? XFG.CullMode.CullClockwiseFace : XFG.CullMode.CullCounterClockwiseFace;
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
		public static bool IsIdentity( XNA.Matrix matrix )
		{
			if( matrix.M11 == 1.0f &&
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
			Rectangle retVal = new Rectangle();
			retVal.X = (int)rectangle.Left;
			retVal.Y = (int)rectangle.Top;
			retVal.Width = (int)rectangle.Width;
			retVal.Height = (int)rectangle.Height;
			return retVal;
		}

		public static Rectangle ToRectangle( BasicBox rectangle )
		{
			Rectangle retVal = new Rectangle();
			retVal.X = (int)rectangle.Left;
			retVal.Y = (int)rectangle.Top;
			retVal.Width = (int)rectangle.Width;
			retVal.Height = (int)rectangle.Height;
			return retVal;
		}

		public static PixelFormat Convert( SurfaceFormat semantic )
		{
			switch( semantic )
			{
				case SurfaceFormat.Alpha8:
					return Axiom.Media.PixelFormat.A8;
				case SurfaceFormat.Luminance8:
					return Axiom.Media.PixelFormat.L8;
				case SurfaceFormat.Luminance16:
					return Axiom.Media.PixelFormat.L16;
				case SurfaceFormat.LuminanceAlpha8:
					return Axiom.Media.PixelFormat.A4L4;
				case SurfaceFormat.LuminanceAlpha16: // Assume little endian here
					return Axiom.Media.PixelFormat.A8L8;
				case SurfaceFormat.Bgra5551:
					return Axiom.Media.PixelFormat.A1R5G5B5;
				case SurfaceFormat.Bgra4444:
					return Axiom.Media.PixelFormat.A4R4G4B4;
				case SurfaceFormat.Bgr565:
					return Axiom.Media.PixelFormat.R5G6B5;
				case XFG.SurfaceFormat.Bgr32:
					return Axiom.Media.PixelFormat.X8B8G8R8;
				case SurfaceFormat.Bgra1010102:
					return Axiom.Media.PixelFormat.A2R10G10B10;
				case SurfaceFormat.Rgba32:
				case SurfaceFormat.Color:
					return Axiom.Media.PixelFormat.A8R8G8B8;
				case XFG.SurfaceFormat.Bgr24:
					return Axiom.Media.PixelFormat.R8G8B8;
				case SurfaceFormat.Dxt1:
					return Axiom.Media.PixelFormat.DXT1;
				case SurfaceFormat.Dxt2:
					return Axiom.Media.PixelFormat.DXT2;
				case SurfaceFormat.Dxt3:
					return Axiom.Media.PixelFormat.DXT3;
				case SurfaceFormat.Dxt4:
					return Axiom.Media.PixelFormat.DXT4;
				case SurfaceFormat.Dxt5:
					return Axiom.Media.PixelFormat.DXT5;
				default:
					return Axiom.Media.PixelFormat.Unknown;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		public static XFG.SurfaceFormat Convert( PixelFormat format )
		{
			switch( format )
			{
				case PixelFormat.BYTE_LA:
					return XFG.SurfaceFormat.LuminanceAlpha16;
				case PixelFormat.L8:
					return XFG.SurfaceFormat.Luminance8;
				case PixelFormat.A8:
					return XFG.SurfaceFormat.Alpha8;
				case PixelFormat.R5G6B5:
					return XFG.SurfaceFormat.Bgr565;
				case PixelFormat.A4R4G4B4:
					return XFG.SurfaceFormat.Bgra4444;
				case PixelFormat.A8R8G8B8:
					return XFG.SurfaceFormat.Color;
					//case PixelFormat.L4A4:
				case PixelFormat.A4L4:
					return XFG.SurfaceFormat.LuminanceAlpha8;
					//case PixelFormat.B10G10R10A2:
				case PixelFormat.A2R10G10B10:
					return XFG.SurfaceFormat.Bgra1010102;
			}

			return XFG.SurfaceFormat.Unknown;
		}

		public static Axiom.Media.PixelFormat GetClosestSupported( Axiom.Media.PixelFormat format )
		{
			if( Convert( format ) != XFG.SurfaceFormat.Unknown )
			{
				return format;
			}
			switch( format )
			{
				case Axiom.Media.PixelFormat.B5G6R5:
					return Axiom.Media.PixelFormat.R5G6B5;
				case Axiom.Media.PixelFormat.B8G8R8:
					return Axiom.Media.PixelFormat.A8R8G8B8; // Would be R8G8B8 normaly but MDX doesn't like that format.
				case Axiom.Media.PixelFormat.B8G8R8A8:
					return Axiom.Media.PixelFormat.A8R8G8B8;
				case Axiom.Media.PixelFormat.FLOAT16_RGB:
					return Axiom.Media.PixelFormat.FLOAT16_RGBA;
				case Axiom.Media.PixelFormat.FLOAT32_RGB:
					return Axiom.Media.PixelFormat.FLOAT32_RGBA;
				case Axiom.Media.PixelFormat.Unknown:
				default:
					return Axiom.Media.PixelFormat.A8R8G8B8;
			}
		}
	}
}
