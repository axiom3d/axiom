using System;
using System.Collections;
using Axiom.Exceptions;

namespace Axiom.Media {
	/// <summary>
	///    Manages registering/fulfilling requests for codecs that handle various types of media.
	/// </summary>
	public class CodecManager {
        #region Singleton implementation

        static CodecManager() { Init(); }
        private CodecManager() {}
        private static CodecManager instance;

        public static CodecManager Instance {
            get { 
                return instance; 
            }
        }

        public static void Init() {
            instance = new CodecManager();
        }
		
        #endregion

        #region Fields

        /// <summary>
        ///    List of registered codecs.
        /// </summary>
        private Hashtable codecs = new Hashtable();

        #endregion Fields

        /// <summary>
        ///    Registers a new codec that can handle a particular type of media files.
        /// </summary>
        /// <param name="codec"></param>
        public void RegisterCodec(ICodec codec) {
            codecs[codec.Type] = codec;
        }

        /// <summary>
        ///    Gets the codec registered for the passed in file extension.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ICodec GetCodec(string extension) {
            if(!codecs.ContainsKey(extension)) {
                throw new AxiomException("No codec available for media with extension .{0}", extension);
            }

            return (ICodec)codecs[extension];
        }
	}
}
