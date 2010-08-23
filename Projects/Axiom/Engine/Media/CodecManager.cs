#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

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
using System.IO;
using Axiom.Core;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Media
{
    /// <summary>
    ///    Manages registering/fulfilling requests for codecs that handle various types of media.
    /// </summary>
    public sealed class CodecManager : IDisposable
    {
        #region Singleton implementation

        /// <summary>
        ///     Singleton instance of this class.
        /// </summary>
        private static CodecManager instance;

        /// <summary>
        ///     Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        internal CodecManager()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static CodecManager Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion Singleton implementation

        public void Dispose()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        #region Fields

        /// <summary>
        ///    List of registered media codecs.
        /// </summary>
        private Dictionary<string, ICodec> codecs = new Dictionary<string, ICodec>(new CaseInsensitiveStringComparer());

        #endregion Fields

        /// <summary>
        ///     Register all default IL image codecs.
        /// </summary>
        public void RegisterCodecs()
        {
            // register codecs
            //RegisterCodec( new JPGCodec() );
            //RegisterCodec( new BMPCodec() );
            //RegisterCodec( new PNGCodec() );
            //RegisterCodec( new DDSCodec() );
            //RegisterCodec( new TGACodec() );
        }

        /// <summary>
        ///    Registers a new codec that can handle a particular type of media files.
        /// </summary>
        /// <param name="codec"></param>
        public void RegisterCodec(ICodec codec)
        {
            codecs[codec.Type] = codec;
        }

        /// <summary>
        ///    Gets the codec registered for the passed in file extension.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ICodec GetCodec(string extension)
        {
            if (!codecs.ContainsKey(extension))
            {
                LogManager.Instance.Write("No codec available for media with extension .{0}", extension);
                RegisterCodec(new NullCodec(extension));
            }

            return (ICodec)codecs[extension];
        }
    }

    public class NullCodec : ICodec
    {
        private string _type;
        public NullCodec(string extension)
        {
            _type = extension;
        }

        public object Decode(Stream input, Stream output, params object[] args)
        {
            return null;
        }

        public void Encode(Stream input, Stream output, params object[] args)
        {
            return;
        }

        public void EncodeToFile(Stream input, string fileName, object codecData)
        {
            return;
        }

        public string Type
        {
            get { return _type; }
        }
    }
}