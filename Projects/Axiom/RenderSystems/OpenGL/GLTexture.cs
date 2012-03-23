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

#endregion LGPL License

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
using System.Diagnostics;
using System.IO;

using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using Tao.OpenGl;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	///    OpenGL specialization of texture handling.
	/// </summary>
	public class GLTexture : Texture
	{
		#region Fields and Properties

		/// <summary>
		/// OpenGL Support
		/// </summary>
		private BaseGLSupport _glSupport;

		private List<HardwarePixelBuffer> _surfaceList = new List<HardwarePixelBuffer>();

		#region TextureID Property

		/// <summary>
		/// OpenGL texture ID.
		/// </summary>
		private int _glTextureID;

		/// <summary>
		///		OpenGL texture ID.
		/// </summary>
		public int TextureID
		{
			get
			{
				return _glTextureID;
			}
		}

		#endregion TextureID Property

		/// <summary>
		///    OpenGL texture format enum value.
		/// </summary>
		public int GLFormat
		{
			get
			{
				switch ( Format )
				{
					case PixelFormat.L8:
						return Gl.GL_LUMINANCE;
					case PixelFormat.R8G8B8:
						return Gl.GL_RGB;
					case PixelFormat.B8G8R8:
						return Gl.GL_BGR;
					case PixelFormat.B8G8R8A8:
						return Gl.GL_BGRA;
					case PixelFormat.A8R8G8B8:
						return Gl.GL_RGBA;
					case PixelFormat.DXT1:
						return Gl.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT;
					case PixelFormat.DXT3:
						return Gl.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT;
					case PixelFormat.DXT5:
						return Gl.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT;
				}

				// make the compiler happy
				return 0;
			}
		}

		/// <summary>
		///     Type of texture this represents, i.e. 2d, cube, etc.
		/// </summary>
		public int GLTextureType
		{
			get
			{
				switch ( TextureType )
				{
					case TextureType.OneD:
						return Gl.GL_TEXTURE_1D;
					case TextureType.TwoD:
						return Gl.GL_TEXTURE_2D;
					case TextureType.ThreeD:
						return Gl.GL_TEXTURE_3D;
					case TextureType.CubeMap:
						return Gl.GL_TEXTURE_CUBE_MAP;
				}

				return 0;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///    Constructor used when creating a manual texture.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <param name="handle"></param>
		/// <param name="group"></param>
		/// <param name="isManual"></param>
		/// <param name="loader"></param>
		/// <param name="glSupport"></param>
		internal GLTexture( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, BaseGLSupport glSupport )
			: base( parent, name, handle, group, isManual, loader )
		{
			_glSupport = glSupport;
			_glTextureID = 0;
		}

		~GLTexture()
		{
			dispose( false );
		}

		#endregion Construction and Destruction

		#region Methods

		protected override void load()
		{
			if ( IsLoaded )
			{
				return;
			}

			if ( Usage == TextureUsage.RenderTarget )
			{
				CreateRenderTexture();
				return;
			}

			if ( Name.IndexOf( '.' ) == -1 )
			{
				throw new Exception( "Unable to load image file '" + Name + "' - invalid extension." );
			}

			string baseName = Name.Substring( 0, Name.LastIndexOf( '.' ) );
			string ext = Name.Substring( Name.LastIndexOf( '.' ) + 1 );

			Image image;
			Stream stream = null;

			if ( TextureType == TextureType.TwoD || TextureType == TextureType.OneD || TextureType == TextureType.ThreeD )
			{
				// find & load resource data into stream to allow resource
				// group changes if required
				stream = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );
				image = Image.FromStream( stream, ext );

				if ( image == null )
				{
					return;
				}

				// If this is a cube map, set the texture type flag accordingly.
				if ( image.HasFlag( ImageFlags.CubeMap ) )
				{
					TextureType = TextureType.CubeMap;
				}

				// If this is a volumetric texture set the texture type flag accordingly.
				if ( image.Depth > 1 )
				{
					TextureType = TextureType.ThreeD;
				}

				// Call internal _loadImages, not loadImage since that's external and
				// will determine load status etc again
				LoadImages( new Image[]
				            {
				            	image
				            } );
			}
			else if ( TextureType == TextureType.CubeMap )
			{
				if ( Name.EndsWith( ".dds" ) )
				{
					// find & load resource data intro stream to allow resource
					// group changes if required
					stream = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );
					image = Image.FromStream( stream, ext );

					// Call internal _loadImages, not loadImage since that's external and
					// will determine load status etc again
					LoadImages( new Image[]
					            {
					            	image
					            } );
				}
				else
				{
					string[] postfixes = {
					                     	"_rt", "_lf", "_up", "_dn", "_fr", "_bk"
					                     };
					List<Image> images = new List<Image>();

					for ( int i = 0; i < 6; i++ )
					{
						string fullName = baseName + postfixes[ i ] + "." + ext;

						// load the image
						stream = ResourceGroupManager.Instance.OpenResource( fullName, Group, true, this );
						image = Image.FromStream( stream, ext );

						images.Add( image );
					} // for

					// load all 6 images
					LoadImages( images.ToArray() );
				}
			}
			else
			{
				throw new NotImplementedException( "Unknown texture type." );
			}

			if ( stream != null )
			{
				stream.Close();
			}
		}

		/// <summary>
		///    Deletes the texture memory.
		/// </summary>
		public override void Unload()
		{
			if ( IsLoaded )
			{
				Gl.glDeleteTextures( 1, ref _glTextureID );
			}
		}

		protected void GenerateMipMaps( byte[] data, bool useSoftware, bool isCompressed, int faceNum )
		{
			// use regular type, unless cubemap, then specify which face of the cubemap we
			// are dealing with here
			int type = ( TextureType == TextureType.CubeMap ) ? Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_X + faceNum : this.GLTextureType;

			if ( useSoftware && MipmapCount > 0 )
			{
				if ( TextureType == TextureType.OneD )
				{
					Glu.gluBuild1DMipmaps( type, HasAlpha ? Gl.GL_RGBA8 : Gl.GL_RGB8, Width, this.GLFormat, Gl.GL_UNSIGNED_BYTE, data );
				}
				else if ( TextureType == TextureType.ThreeD )
				{
					// TODO: Tao needs glTexImage3D
					Gl.glTexImage3DEXT( type, 0, HasAlpha ? Gl.GL_RGBA8 : Gl.GL_RGB8, SrcWidth, SrcHeight, Depth, 0, this.GLFormat, Gl.GL_UNSIGNED_BYTE, data );
				}
				else
				{
					// build the mipmaps
					Glu.gluBuild2DMipmaps( type, HasAlpha ? Gl.GL_RGBA8 : Gl.GL_RGB8, Width, Height, this.GLFormat, Gl.GL_UNSIGNED_BYTE, data );
				}
			}
			else
			{
				if ( TextureType == TextureType.OneD )
				{
					Gl.glTexImage1D( type, 0, HasAlpha ? Gl.GL_RGBA8 : Gl.GL_RGB8, Width, 0, this.GLFormat, Gl.GL_UNSIGNED_BYTE, data );
				}
				else if ( TextureType == TextureType.ThreeD )
				{
					// TODO: Tao needs glTexImage3D
					Gl.glTexImage3DEXT( type, 0, HasAlpha ? Gl.GL_RGBA8 : Gl.GL_RGB8, SrcWidth, SrcHeight, Depth, 0, this.GLFormat, Gl.GL_UNSIGNED_BYTE, data );
				}
				else
				{
					if ( isCompressed && Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.TextureCompressionDXT ) )
					{
						int blockSize = ( Format == PixelFormat.DXT1 ) ? 8 : 16;
						int size = ( ( Width + 3 ) / 4 ) * ( ( Height + 3 ) / 4 ) * blockSize;

						// load compressed image data
						Gl.glCompressedTexImage2DARB( type, 0, this.GLFormat, SrcWidth, SrcHeight, 0, size, data );
					}
					else
					{
						Gl.glTexImage2D( type, 0, HasAlpha ? Gl.GL_RGBA8 : Gl.GL_RGB8, Width, Height, 0, this.GLFormat, Gl.GL_UNSIGNED_BYTE, data );
					}
				}
			}
		}

		/// <summary>
		///    Used to generate a texture capable of serving as a rendering target.
		/// </summary>
		private void CreateRenderTexture()
		{
			// Create the GL texture
			// This already does everything neccessary
			CreateInternalResources();
		}

		private byte[] RescaleNPower2( Image src )
		{
			// Scale image to n^2 dimensions
			int newWidth = ( 1 << MostSignificantBitSet( SrcWidth ) );

			if ( newWidth != SrcWidth )
			{
				newWidth <<= 1;
			}

			int newHeight = ( 1 << MostSignificantBitSet( SrcHeight ) );
			if ( newHeight != SrcHeight )
			{
				newHeight <<= 1;
			}

			byte[] tempData;

			if ( newWidth != SrcWidth || newHeight != SrcHeight )
			{
				int newImageSize = newWidth * newHeight * ( HasAlpha ? 4 : 3 );

				tempData = new byte[ newImageSize ];

				if ( Glu.gluScaleImage( this.GLFormat, SrcWidth, SrcHeight, Gl.GL_UNSIGNED_BYTE, src.Data, newWidth, newHeight, Gl.GL_UNSIGNED_BYTE, tempData ) != 0 )
				{
					throw new AxiomException( "Error while rescaling image!" );
				}

				Image.ApplyGamma( tempData, Gamma, newImageSize, srcBpp );

				srcWidth = Width = newWidth;
				srcHeight = Height = newHeight;
			}
			else
			{
				tempData = new byte[ src.Size ];
				Array.Copy( src.Data, tempData, src.Size );
				Image.ApplyGamma( tempData, Gamma, src.Size, srcBpp );
			}

			return tempData;
		}

		/// <summary>
		///		Helper method for getting the next highest power of 2 value from the specified value.
		/// </summary>
		/// <remarks>Example: Input: 3 Result: 4, Input: 96 Output: 128</remarks>
		/// <param name="val">Integer value.</param>
		/// <returns></returns>
		private int MostSignificantBitSet( int val )
		{
			int result = 0;

			while ( val != 0 )
			{
				result++;
				val >>= 1;
			}

			return result - 1;
		}

		#endregion Methods

		private void _createSurfaceList()
		{
			_surfaceList.Clear();

			// For all faces and mipmaps, store surfaces as HardwarePixelBufferSharedPtr
			bool wantGeneratedMips = ( Usage & TextureUsage.AutoMipMap ) != 0;

			// Do mipmapping in software? (uses GLU) For some cards, this is still needed. Of course,
			// only when mipmap generation is desired.
			bool doSoftware = wantGeneratedMips && !MipmapsHardwareGenerated && MipmapCount != 0;

			for ( int face = 0; face < this.FaceCount; face++ )
			{
				for ( int mip = 0; mip <= MipmapCount; mip++ )
				{
					GLHardwarePixelBuffer buf = new GLTextureBuffer( Name, GLTextureType, _glTextureID, face, mip, (BufferUsage)Usage, doSoftware && mip == 0, _glSupport, HardwareGammaEnabled, FSAA );
					_surfaceList.Add( buf );

					/// Check for error
					if ( buf.Width == 0 || buf.Height == 0 || buf.Depth == 0 )
					{
						throw new Exception( String.Format( "Zero sized texture surface on texture {0} face {1} mipmap {2}. Probably, the GL driver refused to create the texture.", Name, face, mip ) );
					}
				}
			}
		}

		protected override void createInternalResources()
		{
			// Convert to nearest power-of-two size if required
			Width = GLPixelUtil.OptionalPO2( Width );
			Height = GLPixelUtil.OptionalPO2( Height );
			Depth = GLPixelUtil.OptionalPO2( Depth );


			// Adjust format if required
			this.format = TextureManager.Instance.GetNativeFormat( TextureType, Format, Usage );

			// Check requested number of mipmaps
			int maxMips = GLPixelUtil.GetMaxMipmaps( Width, Height, Depth, Format );
			MipmapCount = requestedMipmapCount;
			if ( MipmapCount > maxMips )
			{
				MipmapCount = maxMips;
			}

			// Generate texture name
			Gl.glGenTextures( 1, out _glTextureID );

			// Set texture type
			Gl.glBindTexture( GLTextureType, _glTextureID );

			// This needs to be set otherwise the texture doesn't get rendered
			Gl.glTexParameteri( GLTextureType, Gl.GL_TEXTURE_MAX_LEVEL, MipmapCount );

			// Set some misc default parameters so NVidia won't complain, these can of course be changed later
			Gl.glTexParameteri( GLTextureType, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST );
			Gl.glTexParameteri( GLTextureType, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST );
			Gl.glTexParameteri( GLTextureType, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE );
			Gl.glTexParameteri( GLTextureType, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE );

			// If we can do automip generation and the user desires this, do so
			mipmapsHardwareGenerated = Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.HardwareMipMaps );
			if ( ( ( Usage & TextureUsage.AutoMipMap ) == TextureUsage.AutoMipMap ) && requestedMipmapCount != 0 && MipmapsHardwareGenerated )
			{
				Gl.glTexParameteri( GLTextureType, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE );
			}

			// Allocate internal buffer so that glTexSubImageXD can be used
			// Internal format
			int format = GLPixelUtil.GetClosestGLInternalFormat( Format );
			int width = Width;
			int height = Height;
			int depth = Depth;

			{
				// Run through this process to pregenerate mipmap pyramid
				for ( int mip = 0; mip <= MipmapCount; mip++ )
				{
					// Normal formats
					switch ( TextureType )
					{
						case TextureType.OneD:
							Gl.glTexImage1D( Gl.GL_TEXTURE_1D, mip, format, width, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, IntPtr.Zero );

							break;
						case TextureType.TwoD:
							Gl.glTexImage2D( Gl.GL_TEXTURE_2D, mip, format, width, height, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, IntPtr.Zero );
							break;
						case TextureType.ThreeD:
							Gl.glTexImage3D( Gl.GL_TEXTURE_3D, mip, format, width, height, depth, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, IntPtr.Zero );
							break;
						case TextureType.CubeMap:
							for ( int face = 0; face < 6; face++ )
							{
								Gl.glTexImage2D( Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_X + face, mip, format, width, height, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, IntPtr.Zero );
							}
							break;
					}
					;
					if ( width > 1 )
					{
						width = width / 2;
					}
					if ( height > 1 )
					{
						height = height / 2;
					}
					if ( depth > 1 )
					{
						depth = depth / 2;
					}
				}
			}
			_createSurfaceList();
			// Get final internal format
			this.format = GetBuffer( 0, 0 ).Format;
		}

		protected override void freeInternalResources()
		{
			_surfaceList.Clear();
			try
			{
				Gl.glDeleteTextures( 1, ref _glTextureID );
			}
			catch ( AccessViolationException ave )
			{
				if ( LogManager.Instance != null )
				{
					LogManager.Instance.Write( "Failed to delete Texture[{0}]", _glTextureID );
				}
			}
		}

		public override HardwarePixelBuffer GetBuffer( int face, int mipmap )
		{
			if ( face >= this.FaceCount )
			{
				throw new IndexOutOfRangeException( "Face index out of range" );
			}
			if ( mipmap > MipmapCount )
			{
				throw new IndexOutOfRangeException( "MipMap index out of range" );
			}
			int idx = face * ( MipmapCount + 1 ) + mipmap;
			Debug.Assert( idx < _surfaceList.Count );
			return _surfaceList[ idx ];
		}

		/// <summary>
		///		Implementation of IDisposable to determine how resources are disposed of.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( IsLoaded )
					{
						Unload();
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.

				FreeInternalResources();
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}
	}
}
