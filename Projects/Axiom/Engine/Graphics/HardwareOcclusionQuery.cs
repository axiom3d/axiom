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
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    ///		Interface specification for hardware queries that can be used to find the number
    ///		of fragments rendered by the last render operation.
    /// </summary>
    /// Original Author: Lee Sandberg.
    public abstract class HardwareOcclusionQuery : IDisposable
    {       
        /// <summary>
        /// Let's you get the last pixel count with out doing the hardware occlusion test
        /// </summary>
        /// <remarks>
        /// This function won't give you new values, just the old value.
        /// </remarks>
        public int LastFragmentCount
        {
            get; protected set;
        }

        /// <summary>
        /// Starts the hardware occlusion query
        /// </summary>
        public abstract void Begin();

        /// <summary>
        /// Ends the hardware occlusion test
        /// </summary>
        public abstract void End();

        /// <summary>
        /// Pulls the hardware occlusion query.
        /// </summary>
        /// <remarks>
        /// Waits until the query result is available; use <see cref="IsStillOutstanding"/>
        /// if just want to test if the result is available.
        /// </remarks>
        /// <returns>the resulting number of fragments.</returns>
        public abstract int PullResults();

        /// <summary>
        /// Lets you know when query is done, or still be processed by the Hardware
        /// </summary>
        /// <returns>true if query isn't finished.</returns>
        public abstract bool IsStillOutstanding();

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public virtual void Dispose()
        {
        }

        #endregion
    }
}
