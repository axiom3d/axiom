#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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
using System.Text;
using Axiom.Core;
using Tao.Cg;

#endregion Namespace Declarations

namespace Axiom.CgPrograms
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
        internal static void CheckCgError(string potentialError, IntPtr context)
        {
            // check for a Cg error
            int error = Cg.cgGetError();

            if (error != Cg.CG_NO_ERROR)
            {
                var sb = new StringBuilder();
                sb.Append(Environment.NewLine);
                sb.Append(potentialError);
                sb.Append(Environment.NewLine);

                sb.Append(Cg.cgGetErrorString(error));
                sb.Append(Environment.NewLine);

                // Check for compiler error, need CG_COMPILER_ERROR const
                if (error == Cg.CG_COMPILER_ERROR)
                {
                    sb.Append(Cg.cgGetLastListing(context));
                    sb.Append(Environment.NewLine);
                }

                LogManager.Instance.Write(sb.ToString());
            }
        }
    }
}