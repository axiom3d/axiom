using System;
using Tao.DevIl;

namespace Axiom
{
    /// <summary>
    ///    JPG image file codec.
    /// </summary>
    public class JPGCodec : ILImageCodec
    {
        public JPGCodec()
        {
        }

        #region ILImageCodec Implementation

        /// <summary>
        ///    Passthrough implementation, no special code needed.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public override object Decode( System.IO.Stream input, System.IO.Stream output, params object[] args )
        {
            // nothing special needed, just pass through
            return base.Decode( input, output, args );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="args"></param>
        public override void Encode( System.IO.Stream source, System.IO.Stream dest, params object[] args )
        {
            throw new NotImplementedException( "JPG encoding is not yet implemented." );
        }

        /// <summary>
        ///    Returns the JPG file extension.
        /// </summary>
        public override String Type
        {
            get
            {
                return "jpg";
            }
        }


        /// <summary>
        ///    Returns JPG enum.
        /// </summary>
        public override int ILType
        {
            get
            {
                return Il.IL_JPG;
            }
        }

        #endregion ILImageCodec Implementation
    }
}
