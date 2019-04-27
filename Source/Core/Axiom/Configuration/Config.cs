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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

#endregion Namespace Declarations

namespace Axiom.Configuration
{
    /// <summary>
    /// Summary description for Config.
    /// </summary>
    public class Config
    {
        public const int MaxTextureCoordSets = 6;
        public const int MaxTextureLayers = 8;
        public const int MaxBlendWeights = 4;
        public const int MaxSimultaneousLights = 8;
        public const int MaxMultipleRenderTargets = 8;

#if AXIOM_THREAD_SUPPORT
		/// <summary>
		/// Get the multithreading level to use.
		/// </summary>
		/// <remarks>
		/// In Ogre, OGRE_THREAD_SUPPORT preprocessor directive and multithreading work as follow:
		/// 
		/// OGRE_THREAD_SUPPORT = 0 No support for threading.
		/// 
		/// OGRE_THREAD_SUPPORT = 1 Thread support for background loading, 
		///     by both loading and constructing resources in a background thread.
		///     Resource management and SharedPtr handling becomes thread-safe,
		///     and resources may be completely loaded in the background.
		///     The places where threading is available are clearly marked,
		///     you should assume state is NOT thread safe unless otherwise
		///     stated in relation to this flag.
		///     
		/// OGRE_THREAD_SUPPORT = 2 Thread support for background resource preparation.
		///     This means that resource data can streamed into memory in the background,
		///     but the final resource construction (including RenderSystem dependencies)
		///     is still done in the primary thread. Has a lower synchronisation primitive
		///     overhead than full threading while still allowing the major blocking aspects
		///     of resource management (I/O) to be done in the background.
		///     
		/// In Axiom, we use the conditional compilation symbol AXIOM_THREAD_SUPPORT, as Ogre threading level 0,
		/// to enable or disable multithreading features.
		/// When enabled, the behaviour of Axiom (or multithreading level) can be controlled by AxiomThreadLevel.
		/// </remarks>
		public static readonly int AxiomThreadLevel = 2;
#endif

        public Config()
        {
        }
    }
}