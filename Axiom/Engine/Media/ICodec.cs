using System;
using System.IO;

namespace Axiom.Media {
	/// <summary>
	///    Interface describing an object that can handle a form of media, be it
	///    a image, sound, video, etc.
	/// </summary>
	public interface ICodec {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="args"></param>
        void Decode(Stream source, Stream dest, params object[] args);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="args"></param>
        void Encode(Stream source, Stream dest, params object[] args);

        /// <summary>
        ///    Gets the type of data that this codec is meant to handle, typically a file extension.
        /// </summary>
        String Type {
            get;
        }
	}
}
