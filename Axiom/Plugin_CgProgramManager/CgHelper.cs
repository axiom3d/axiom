using System;
using Tao.Cg;

namespace Plugin_CgProgramManager
{
	/// <summary>
	/// 	Helper class with common methods for use in the Cg plugin.
	/// </summary>
	public class CgHelper
	{
        /// <summary>
        ///    Used to check for a recent Cg error and handle it accordingly.
        /// </summary>
        /// <param name="potentialError">Message to use if an error has indeed occurred.</param>
        /// <param name="context">Current Cg context.</param>
        internal static void CheckCgError(string potentialError, int context) {
            // check for a Cg error
            int error = Cg.cgGetError();

            // TODO: CG_NO_ERROR const
            if(error != 0) {
                string msg = potentialError + Cg.cgGetErrorString(error);

                // TODO: Check for compiler error, need CG_COMPILER_ERROR const

                throw new Exception(msg);
            }
        }
	}
}
