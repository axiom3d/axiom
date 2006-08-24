using System;
using System.Text;
using Axiom.Core;
using Tao.Cg;

namespace Axiom.CgPrograms {
	/// <summary>
	/// 	Helper class with common methods for use in the Cg plugin.
	/// </summary>
	public class CgHelper {
        /// <summary>
        ///    Used to check for a recent Cg error and handle it accordingly.
        /// </summary>
        /// <param name="potentialError">Message to use if an error has indeed occurred.</param>
        /// <param name="context">Current Cg context.</param>
        internal static void CheckCgError(string potentialError, IntPtr context) {
            StringBuilder sb = new StringBuilder();

            // check for a Cg error
            int error = Cg.cgGetError();

            if(error != Cg.CG_NO_ERROR) {
                sb.Append(Environment.NewLine);
                sb.Append(potentialError);
                sb.Append(Environment.NewLine);

                sb.Append(Cg.cgGetErrorString(error));
                sb.Append(Environment.NewLine);

                // Check for compiler error, need CG_COMPILER_ERROR const
                if(error == Cg.CG_COMPILER_ERROR) {
                    sb.Append(Cg.cgGetLastListing(context));
                    sb.Append(Environment.NewLine);
                }

                throw new AxiomException(sb.ToString());
            }
        }
	}
}
