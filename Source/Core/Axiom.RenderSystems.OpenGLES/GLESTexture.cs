#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using OpenTK.Graphics.ES11;

using ResourceHandle = System.UInt64;
using OpenGL = OpenTK.Graphics.ES11.GL;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	public class GLESTexture : Texture
	{
		/// <summary>
		/// </summary>
		private GLESSupport _support;

		/// <summary>
		/// </summary>
		private int _textureID;

		/// <summary>
		///   List of subsurfaces
		/// </summary>
		private readonly List<HardwarePixelBuffer> _surfaceList;

		/// <summary>
		///   List of images that were pulled from disk by prepareLoad but have yet to be pushed into texture memory by loadImpl. Images should be deleted by loadImpl and unprepareImpl.
		/// </summary>
		protected List<Image> _loadedImages;

		/// <summary>
		///   Unique ID of the texture (assigned by OpenGL)
		/// </summary>
		public int TextureID
		{
			get { return this._textureID; }
		}

		/// <summary>
		/// </summary>
		public All GLESTextureTarget
		{
			get { return All.Texture2D; }
		}

		/// <summary>
		/// </summary>
		/// <param name="creator"> </param>
		/// <param name="name"> </param>
		/// <param name="handle"> </param>
		/// <param name="group"> </param>
		/// <param name="isManual"> </param>
		/// <param name="loader"> </param>
		/// <param name="support"> </param>
		public GLESTexture( ResourceManager creator, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, GLESSupport support )
			: base( creator, name, handle, group, isManual, loader )
		{
			this._support = support;
			this._surfaceList = new List<HardwarePixelBuffer>();
		}

		/// <summary>
		/// </summary>
		/// <param name="name"> </param>
		/// <param name="group"> </param>
		/// <param name="ext"> </param>
		/// <param name="images"> </param>
		/// <param name="r"> </param>
		public static void DoImageIO( string name, string group, string ext, ref List<Image> images, Resource r )
		{
			int imgIdx = images.Count;
			images.Add( new Image() );

			Stream stream = ResourceGroupManager.Instance.OpenResource( name, group, true, r );

			images[ imgIdx ] = Image.FromStream( stream, ext );
		}

		/// <summary>
		/// </summary>
		/// <param name="face"> </param>
		/// <param name="mipmap"> </param>
		/// <returns> </returns>
		public override HardwarePixelBuffer GetBuffer( int face, int mipmap )
		{
			if ( face >= faceCount )
			{
				throw new IndexOutOfRangeException( string.Format( "Face index is out of range. Face : {0}, Facecount : {1}.", face, faceCount ) );
			}
			if ( mipmap > MipmapCount )
			{
				throw new IndexOutOfRangeException( string.Format( "Mipmap index is out of range. Mipmap : {0}, Mipmapcount : {1}.", mipmap, MipmapCount ) );
			}

			int idx = face * ( MipmapCount + 1 ) + mipmap;
			Utilities.Contract.Requires( idx < this._surfaceList.Count, String.Format( "[GLESTexture( Name={0} ) ] Index( {1} ) > Surfacelist.Count( {2} )", Name, idx, this._surfaceList.Count ) );
			return this._surfaceList[ idx ];
		}

		/// <summary>
		/// </summary>
		public void CreateRenderTexture()
		{
			// Create the GL texture
			// This already does everything neccessary
			createInternalResources();
		}

		/// <summary>
		/// </summary>
		protected void CreateSurfaceList()
		{
			this._surfaceList.Clear();
			// For all faces and mipmaps, store surfaces as HardwarePixelBufferSharedPtr
			bool wantGenerateMips = ( Usage & TextureUsage.AutoMipMap ) == TextureUsage.AutoMipMap;
			// Do mipmapping in software? (uses GLU) For some cards, this is still needed. Of course,
			// only when mipmap generation is desired.
			bool doSoftware = wantGenerateMips && !MipmapsHardwareGenerated && MipmapCount > 0;

			for ( int face = 0; face < faceCount; face++ )
			{
				int width = Width;
				int height = Height;
				for ( int mip = 0; mip <= MipmapCount; mip++ )
				{
					HardwarePixelBuffer buf = new GLESTextureBuffer( Name, GLESTextureTarget, this._textureID, width, height, (int) GLESPixelUtil.GetClosestGLInternalFormat( Format, HardwareGammaEnabled ), face, mip, (BufferUsage) Usage, doSoftware && mip == 0, HardwareGammaEnabled, FSAA );

					this._surfaceList.Add( buf );

					// If format is PVRTC then every mipmap is a custom one so to allow the upload of the compressed data 
					// provided by the file we need to adjust the current mip level's dimention
#warning Compressed Textures are not yet supported by axiom
					/*
					if (mFormat == PF_PVRTC_RGB2 || mFormat == PF_PVRTC_RGBA2 ||
					mFormat == PF_PVRTC_RGB4 || mFormat == PF_PVRTC_RGBA4)
					{
						if (width > 1)
						{
							width = width / 2;
						}
						if (height > 1)
						{
							height = height / 2;
						}
					}*/
					/// Check for error
					if ( buf.Width == 0 || buf.Height == 0 || buf.Depth == 0 )
					{
						throw new AxiomException( string.Format( "Zero sized texture surface on texture: {0} face: {1} mipmap : {2}. The GL driver probably refused to create the texture.", Name, face, mip ) );
					}
				}
			}
		}

		/// <summary>
		/// </summary>
		protected override void createInternalResources()
		{
			// Convert to nearest power-of-two size if required
			Width = GLESPixelUtil.OptionalPO2( Width );
			Height = GLESPixelUtil.OptionalPO2( Height );
			Depth = GLESPixelUtil.OptionalPO2( Depth );

			//adjust format if required
			Format = TextureManager.Instance.GetNativeFormat( Graphics.TextureType.TwoD, Format, Usage );
			// Check requested number of mipmaps
			int maxMips = GLESPixelUtil.GetMaxMipmaps( Width, Height, Depth, Format );

			if ( PixelUtil.IsCompressed( Format ) && _mipmapCount == 0 )
			{
				RequestedMipmapCount = 0;
			}

			_mipmapCount = RequestedMipmapCount;
			if ( _mipmapCount > maxMips )
			{
				_mipmapCount = maxMips;
			}

			// Generate texture name
			OpenGL.GenTextures( 1, ref this._textureID );
			GLESConfig.GlCheckError( this );
			// Set texture type
			OpenGL.BindTexture( All.Texture2D, this._textureID );
			GLESConfig.GlCheckError( this );
			// Set some misc default parameters, these can of course be changed later
			OpenGL.TexParameter( All.Texture2D, All.TextureMinFilter, (int) All.LinearMipmapNearest );
			GLESConfig.GlCheckError( this );
			OpenGL.TexParameter( All.Texture2D, All.TextureMagFilter, (int) All.Nearest );
			GLESConfig.GlCheckError( this );
			OpenGL.TexParameter( All.Texture2D, All.TextureWrapS, (int) All.ClampToEdge );
			GLESConfig.GlCheckError( this );
			OpenGL.TexParameter( All.Texture2D, All.TextureWrapT, (int) All.ClampToEdge );
			GLESConfig.GlCheckError( this );
			// If we can do automip generation and the user desires this, do so
			MipmapsHardwareGenerated = Root.Instance.RenderSystem.HardwareCapabilities.HasCapability( Capabilities.HardwareMipMaps ) && !PixelUtil.IsCompressed( Format );

			if ( ( Usage & TextureUsage.AutoMipMap ) == TextureUsage.AutoMipMap && RequestedMipmapCount > 0 && MipmapsHardwareGenerated )
			{
				OpenGL.TexParameter( All.Texture2D, All.GenerateMipmap, (int) All.True );
				GLESConfig.GlCheckError( this );
			}

			// Allocate internal buffer so that TexSubImageXD can be used
			// Internal format
			All format = GLESPixelUtil.GetClosestGLInternalFormat( Format, HardwareGammaEnabled );
			int width = Width;
			int height = Height;
			int depth = Depth;

			if ( PixelUtil.IsCompressed( Format ) )
			{
				// Compressed formats
				int size = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

				// Provide temporary buffer filled with zeroes as glCompressedTexImageXD does not
				// accept a 0 pointer like normal glTexImageXD
				// Run through this process for every mipmap to pregenerate mipmap pyramid

				var tmpData = new byte[ size ];
				IntPtr tmpDataptr = Memory.PinObject( tmpData );
				for ( int mip = 0; mip <= MipmapCount; mip++ )
				{
					size = PixelUtil.GetMemorySize( Width, Height, Depth, Format );
					OpenGL.CompressedTexImage2D( All.Texture2D, mip, format, width, height, 0, size, tmpDataptr );

					GLESConfig.GlCheckError( this );

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
				Memory.UnpinObject( tmpData );
			}
			else
			{
				// Run through this process to pregenerate mipmap pyramid
				for ( int mip = 0; mip <= MipmapCount; mip++ )
				{
					OpenGL.TexImage2D( All.Texture2D, mip, (int) format, width, height, 0, format, All.UnsignedByte, IntPtr.Zero );
					GLESConfig.GlCheckError( this );

					if ( width > 1 )
					{
						width = width / 2;
					}
					if ( height > 1 )
					{
						height = height / 2;
					}
				}
			}

			CreateSurfaceList();

			// Get final internal format
			Format = GetBuffer( 0, 0 ).Format;
		}

		/// <summary>
		/// </summary>
		protected override void freeInternalResources()
		{
			this._surfaceList.Clear();
			OpenGL.DeleteTextures( 1, ref this._textureID );
			GLESConfig.GlCheckError( this );
		}

		/// <summary>
		/// </summary>
		protected override void load()
		{
			if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
			{
				return;
			}

			string baseName, ext = string.Empty;
			int pos = Name.LastIndexOf( '.' );
			baseName = Name.Substring( 0, pos );
			if ( pos != -1 )
			{
				ext = Name.Substring( pos + 1 );
			}

			var loadedImages = new List<Image>();

			if ( TextureType == Graphics.TextureType.TwoD )
			{
				DoImageIO( Name, Group, ext, ref loadedImages, this );
				// If this is a volumetric texture set the texture type flag accordingly.
				if ( loadedImages[ 0 ].Depth > 1 )
				{
					throw new AxiomException( "**** Unsupported 3D texture type ****" );
				}
#warning Axiom did not have Compressed Textureformat yet
				/*
				// If PVRTC and 0 custom mipmap disable auto mip generation and disable software mipmap creation
				PixelFormat imageFormat = (*loadedImages)[0].getFormat();
				if (imageFormat == PF_PVRTC_RGB2 || imageFormat == PF_PVRTC_RGBA2 ||
					imageFormat == PF_PVRTC_RGB4 || imageFormat == PF_PVRTC_RGBA4)
				{
					size_t imageMips = (*loadedImages)[0].getNumMipmaps();
					if (imageMips == 0)
					{
						mNumMipmaps = mNumRequestedMipmaps = imageMips;
						// Disable flag for auto mip generation
						mUsage &= ~TU_AUTOMIPMAP;
					}
				}*/
			}
			else
			{
				throw new AxiomException( "**** Unknown texture type ****" );
			}

			this._loadedImages = loadedImages;


			if ( ( Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
			{
				CreateRenderTexture();
				return;
			}

			Image[] images = this._loadedImages.ToArray();
			base.LoadImages( images );
		}

		protected override void unload()
		{
			base.unload();
			foreach ( var image in this._loadedImages )
			{
				if ( !image.IsDisposed )
				{
					image.Dispose();
				}
			}
			this._loadedImages.Clear();
		}

		/// <summary>
		/// </summary>
		/// <param name="disposeManagedResources"> </param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( IsLoaded )
			{
				unload();
			}
			else
			{
				freeInternalResources();
			}
			base.dispose( disposeManagedResources );
		}
	}
}
