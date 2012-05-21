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

using System.ComponentModel.Composition;
using Axiom.Core;
using Axiom.Media;
using RegisteredCodec = System.Collections.Generic.List<Axiom.Media.ImageCodec>;

#endregion Namespace Declarations

namespace Axiom.Plugins.SystemDrawingCodecs
{
	[Export( typeof ( IPlugin ) )]
	internal class Plugin : Axiom.Core.IPlugin
	{
		#region Implementation of IPlugin

		/// <summary>
		/// Unique name for the plugin
		/// </summary>
		private string Name
		{
			get
			{
				return "System.Drawing Media Codecs";
			}
		}

		private static RegisteredCodec _codecList;

		/// <summary>
		/// Perform any tasks the plugin needs to perform on full system initialization.
		/// </summary>
		/// <remarks>
		/// An implementation must be supplied for this method. It is called
		/// just after the system is fully initialized (either after Root.Initialize
		/// if a window is created then, or after the first window is created)
		/// and therefore all rendersystem functionality is available at this
		/// time. You can use this hook to create any resources which are
		/// dependent on a rendersystem or have rendersystem-specific implementations.
		/// </remarks>
		public void Initialize()
		{
			if ( _codecList == null )
			{
				_codecList = new RegisteredCodec();
				_codecList.Add( new SDImageLoader( "BMP" ) );
				_codecList.Add( new SDImageLoader( "JPG" ) );
				_codecList.Add( new SDImageLoader( "PNG" ) );

				foreach ( var i in _codecList )
				{
					if ( !CodecManager.Instance.IsCodecRegistered( i.Type ) )
					{
						CodecManager.Instance.RegisterCodec( i );
					}
				}
			}
		}

		/// <summary>
		/// Perform any tasks the plugin needs to perform when the system is shut down.
		/// </summary>
		/// <remarks>
		/// An implementation must be supplied for this method.
		/// This method is called just before key parts of the system are unloaded,
		/// such as rendersystems being shut down. You should use this hook to free up
		/// resources and decouple custom objects from the Axiom system, whilst all the
		/// instances of other plugins (e.g. rendersystems) still exist.
		/// </remarks>
		public void Shutdown()
		{
			if ( _codecList != null )
			{
				foreach ( var i in _codecList )
				{
					if ( CodecManager.Instance.IsCodecRegistered( i.Type ) )
					{
						CodecManager.Instance.UnregisterCodec( i );
					}
				}

				_codecList.Clear();
				_codecList = null;
			}
		}

		#endregion Implementation of IPlugin
	};
}