﻿#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2009 Axiom Project Team

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
//     <id value="$Id $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9.HLSL
{
    /// <summary>
    /// Shader optimization level
    /// </summary>
    public enum OptimizationLevel
    {
        /// <summary>
        /// Default optimization - no optimization in debug mode, LevelOne in release
        /// </summary>
        [ScriptEnum("default")]
        Default,
        /// <summary>
        /// No optimization
        /// </summary>
        [ScriptEnum( "none" )]
        None,
        /// <summary>
        /// Optimization level 0
        /// </summary>
        [ScriptEnum( "0" )]
        LevelZero,
        /// <summary>
        /// Optimization level 1
        /// </summary>
        [ScriptEnum( "1" )]
        LevelOne,
        /// <summary>
        /// Optimization level 2
        /// </summary>
        [ScriptEnum( "2" )]
        LevelTwo,
        /// <summary>
        /// Optimization level 3
        /// </summary>
        [ScriptEnum( "3" )]
        LevelThree
    }
}