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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Axiom.Core;
using Axiom.Media;
using Tao.DevIl;

#endregion Namespace Declarations

namespace Axiom.Plugins.DevILCodecs
{
	/// <summary>
	/// Main plugin class.
	/// </summary>
	[Export( typeof ( IPlugin ) )]
	public class DevILPlugin : IPlugin
	{
		private static readonly List<ILImageCodec> codecList = new List<ILImageCodec>();

		[OgreVersion( 1, 7, 2 )]
		private static int _IlTypeFromExt( string ext )
		{
			if ( ext == "tga" || ext == "vda" || ext == "icb" || ext == "vst" )
			{
				return Il.IL_TGA;
			}

			if ( ext == "jpg" || ext == "jpe" || ext == "jpeg" )
			{
				return Il.IL_JPG;
			}

			//if (ext=="dds")
			//    return Il.IL_DDS;

			if ( ext == "png" )
			{
				return Il.IL_PNG;
			}

			if ( ext == "bmp" || ext == "dib" )
			{
				return Il.IL_BMP;
			}

			if ( ext == "gif" )
			{
				return Il.IL_GIF;
			}

			if ( ext == "cut" )
			{
				return Il.IL_CUT;
			}

			if ( ext == "hdr" )
			{
				return Il.IL_HDR;
			}

			if ( ext == "ico" || ext == "cur" )
			{
				return Il.IL_ICO;
			}

			if ( ext == "jng" )
			{
				return Il.IL_JNG;
			}

			if ( ext == "lif" )
			{
				return Il.IL_LIF;
			}

			if ( ext == "mdl" )
			{
				return Il.IL_MDL;
			}

			if ( ext == "mng" || ext == "jng" )
			{
				return Il.IL_MNG;
			}

			if ( ext == "pcd" )
			{
				return Il.IL_PCD;
			}

			if ( ext == "pcx" )
			{
				return Il.IL_PCX;
			}

			if ( ext == "pic" )
			{
				return Il.IL_PIC;
			}

			if ( ext == "pix" )
			{
				return Il.IL_PIX;
			}

			if ( ext == "pbm" || ext == "pgm" || ext == "pnm" || ext == "ppm" )
			{
				return Il.IL_PNM;
			}

			if ( ext == "psd" || ext == "pdd" )
			{
				return Il.IL_PSD;
			}

			if ( ext == "psp" )
			{
				return Il.IL_PSP;
			}

			if ( ext == "pxr" )
			{
				return Il.IL_PXR;
			}

			if ( ext == "sgi" || ext == "bw" || ext == "rgb" || ext == "rgba" )
			{
				return Il.IL_SGI;
			}

			if ( ext == "tif" || ext == "tiff" )
			{
				return Il.IL_TIF;
			}

			if ( ext == "wal" )
			{
				return Il.IL_WAL;
			}

			if ( ext == "xpm" )
			{
				return Il.IL_XPM;
			}

			return Il.IL_TYPE_UNKNOWN;
		}

		/// <summary>
		/// Called when the plugin is started.
		/// Register all codecs provided by this plugin.
		/// </summary>
		/// <remarks>
		/// Differently from Ogre, we check if a codec is already registered.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public void Initialize()
		{
			var ilVersion = Il.ilGetString( Il.IL_VERSION_NUM );
			if ( Il.ilGetError() != Il.IL_NO_ERROR )
			{
				// IL defined the version number as IL_VERSION in older versions, so we have to scan for it
				for ( var ver = 150; ver < 170; ver++ )
				{
					ilVersion = Il.ilGetString( ver );
					if ( Il.ilGetError() == Il.IL_NO_ERROR )
					{
						break;
					}
					else
					{
						ilVersion = "Unknown";
					}
				}
			}
			LogManager.Instance.Write( "DevIL version: {0}", ilVersion );
			var ilExtensions = Il.ilGetString( Il.IL_LOAD_EXT );
			if ( Il.ilGetError() != Il.IL_NO_ERROR )
			{
				ilExtensions = string.Empty;
			}

			var ext = ilExtensions.Split( new char[]
			                              {
			                              	' '
			                              }, StringSplitOptions.RemoveEmptyEntries );

			foreach ( var str in ext )
			{
				if ( CodecManager.Instance.IsCodecRegistered( str ) )
				{
					continue;
				}

				var ilType = _IlTypeFromExt( str );
				var codec = new ILImageCodec( str, ilType );
				CodecManager.Instance.RegisterCodec( codec );
				codecList.Add( codec );
			}

			// Raw format, missing in image formats string
			if ( !CodecManager.Instance.IsCodecRegistered( "raw" ) )
			{
				var cod = new ILImageCodec( "raw", Il.IL_RAW );
				CodecManager.Instance.RegisterCodec( cod );
				codecList.Add( cod );
				ilExtensions += "raw";
			}

			LogManager.Instance.Write( "DevIL image formats: {0}", ilExtensions );
		}

		/// <summary>
		/// Called when the plugin is stopped.
		/// Delete all codecs provided by this plugin.
		/// </summary>
		[OgreVersion( 1, 7, 2, "Original reference method was ILCodecs::deleteCodecs" )]
		public void Shutdown()
		{
			foreach ( var i in codecList )
			{
				CodecManager.Instance.UnregisterCodec( i );
			}

			codecList.Clear();
		}
	};
}