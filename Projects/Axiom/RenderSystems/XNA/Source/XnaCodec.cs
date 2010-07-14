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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;
using Axiom.Core;
using Axiom.Media;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    class XnaCodec : ImageCodec
    {
        private string imageExtension;

        public XnaCodec( string extension )
        {
            this.imageExtension = extension;
        }

        #region Overrides of ImageCodec

        /// <summary>
        /// Codes the data from the input chunk into the output chunk.
        /// </summary>
        /// <param name="input">Input stream (encoded data).</param><param name="output">Output stream (decoded data).</param><param name="args">Variable number of extra arguments.</param>
        /// <returns>
        /// An object that holds data specific to the media format which this codec deal with.
        ///     For example, an image codec might return a structure that has image related details,
        ///     such as height, width, etc.
        /// </returns>
        public override object Decode( Stream input, Stream output, params object[] args )
        {
            byte[] data = new byte[ input.Length ];
            input.Read( data, 0, (int)input.Length );
            output.Write( data, 0, (int)input.Length );
            if ( input.GetType() == typeof( XnaImageCodecStream ) )
            {
                return ( (XnaImageCodecStream)input ).ImageData;
            }
            return null;
        }

        /// <summary>
        /// Encodes the data in the input stream and saves the result in the output stream.
        /// </summary>
        /// <param name="input">Input stream (decoded data).</param><param name="output">Output stream (encoded data).</param><param name="args">Variable number of extra arguments.</param>
        public override void Encode( Stream input, Stream output, params object[] args )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Encodes data to a file.
        /// </summary>
        /// <param name="input">Stream containing data to write.</param><param name="fileName">Filename to output to.</param><param name="codecData">Extra data to use in order to describe the codec data.</param>
        public override void EncodeToFile( Stream input, string fileName, object codecData )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the type of data that this codec is meant to handle, typically a file extension.
        /// </summary>
        public override string Type
        {
            get { return imageExtension; }
        }

        #endregion Overrides of ImageCodec
    }

    public class XnaCodecPlugin : IPlugin
    {
        #region Implementation of IPlugin

        /// <summary>
        /// Unique name for the plugin
        /// </summary>
        string Name
        {
            get { return "XNACodecs"; }
        }

        /// <summary>
        /// Perform the plugin initial installation sequence.
        /// </summary>
        /// <remarks>
        /// An implementation must be supplied for this method. It must perform
        /// the startup tasks necessary to install any rendersystem customizations
        /// or anything else that is not dependent on system initialization, ie
        /// only dependent on the core of Axiom. It must not perform any
        /// operations that would create rendersystem-specific objects at this stage,
        /// that should be done in Initialize().
        /// </remarks>
        //void Install();

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
#if ( XBOX || XBOX360 )
            CodecManager.Instance.RegisterCodec( new XnaCodec( "png" ) );
            CodecManager.Instance.RegisterCodec( new XnaCodec( "jpg" ) );
            CodecManager.Instance.RegisterCodec( new XnaCodec( "gif" ) );
            CodecManager.Instance.RegisterCodec( new XnaCodec( "dds" ) );
            CodecManager.Instance.RegisterCodec( new XnaCodec( "bmp" ) );
#endif
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
        }

        #endregion Implementation of IPlugin
    }
}