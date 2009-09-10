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
//     <id value="$Id: Win32PlatformManager.cs 1663 2009-06-12 21:27:34Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;

using Axiom.Core;
using Axiom.Input;

#endregion Namespace Declarations

namespace Axiom.Platforms.SlimDX
{
    /// <summary>
    ///		Platform management specialization for Microsoft Windows (r) platform.
    /// </summary>
    public class SlimDXPlatformManager : IPlatformManager
    {
        #region Fields

        /// <summary>
        ///		Reference to the current input reader.
        /// </summary>
        private InputReader inputReader;

        #endregion Fields

        #region Construction and Destruction

        public SlimDXPlatformManager()
        {
            LogManager.Instance.Write( "SlimDX Platform Manager Loaded." );
        }

        #endregion Construction and Destruction

        #region IPlatformManager Members

        /// <summary>
        ///		Creates an InputReader implemented using Microsoft DirectInput (tm).
        /// </summary>
        /// <returns></returns>
        public InputReader CreateInputReader()
        {
            if ( inputReader == null )
                inputReader = new SlimDXInputReader();
            return inputReader;
        }

        /// <summary>
        ///     Called when the engine is being shutdown.
        /// </summary>
        public void Dispose()
        {
            if ( inputReader != null )
            {
                inputReader.Dispose();
            }
			LogManager.Instance.Write( "SlimDX Platform Manager Shutdown." );
        }

        #endregion
    }
}
