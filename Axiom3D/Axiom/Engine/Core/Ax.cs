#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

namespace Axiom
{
    /// <summary>
    /// Static singleton aggregator utility class
    /// </summary>
    public sealed class Ax
    {
        /// <summary>
        /// Private constructor to prevent instantiation
        /// </summary>
        private Ax()
        {
        }

        #region Static Fields and Properties
        public static RenderSystem Renderer
        {
            get
            {
                return Root.RenderSystem;
            }
        }
        public static SceneManager Scene
        {
            get
            {
                return Root.SceneManager;
            }
        }
        public static Root Root
        {
            get
            {
                return Root.Instance;
            }
        }
        public static LogManager Log
        {
            get
            {
                return LogManager.Instance;
            }
        }
        public static MaterialManager Materials
        {
            get
            {
                return MaterialManager.Instance;
            }
        }
        public static ArchiveManager Archives
        {
            get
            {
                return ArchiveManager.Instance;
            }
        }
        public static MeshManager Meshes
        {
            get
            {
                return MeshManager.Instance;
            }
        }
        public static SkeletonManager Skeletons
        {
            get
            {
                return SkeletonManager.Instance;
            }
        }
        public static ParticleSystemManager Particles
        {
            get
            {
                return ParticleSystemManager.Instance;
            }
        }
        public static IPlatformManager Platform
        {
            get
            {
                return PlatformManager.Instance;
            }
        }
        public static OverlayManager Overlays
        {
            get
            {
                return OverlayManager.Instance;
            }
        }
        public static OverlayElementManager OverlayElements
        {
            get
            {
                return OverlayElementManager.Instance;
            }
        }
        public static FontManager Fonts
        {
            get
            {
                return FontManager.Instance;
            }
        }
        public static CodecManager Codecs
        {
            get
            {
                return CodecManager.Instance;
            }
        }
        public static HighLevelGpuProgramManager GpuPrograms
        {
            get
            {
                return HighLevelGpuProgramManager.Instance;
            }
        }
        public static PluginManager Plugins
        {
            get
            {
                return PluginManager.Instance;
            }
        }
        public static TextureManager Textures
        {
            get
            {
                return TextureManager.Instance;
            }
        }
        public static ControllerManager Controllers
        {
            get
            {
                return ControllerManager.Instance;
            }
        }
        public static HardwareBufferManager Buffers
        {
            get
            {
                return HardwareBufferManager.Instance;
            }
        }
        #endregion
    }
}
