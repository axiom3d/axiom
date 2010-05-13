#region LGPL License

/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team
 
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
/*
 * Many thanks to the folks at Multiverse for providing the initial port for this class
 */

#endregion

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Configuration;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    ///<summary>
    /// Base composition technique, can be subclassed in plugins.
    ///</summary>
    public class CompositionTechnique : IDisposable
    {
        ///<summary>
        /// The scope of a texture defined by the compositor.
        ///</summary>
        public enum TextureScope
        {
            ///<summary>
            /// Local texture - only available to the compositor passes in this technique
            ///</summary>
            Local,
            ///<summary>
            /// Chain texture - available to the other compositors in the chain
            ///</summary>
            Chain,
            ///<summary>
            /// Global texture - available to everyone in every scope
            ///</summary>
            Global
        }

        //end enum TextureScope

        ///<summary>
        /// Local texture definitions
        ///</summary>
        public class TextureDefinition
        {
            ///<summary>
            /// Name of the texture definition.
            ///</summary>
            public string Name { get; set; }

            /// <summary>
            /// If a reference, the name of the compositor being referenced
            /// </summary>
            public string ReferenceCompositorName { get; set; }

            /// <summary>
            /// If a reference, the name of the texture in the compositor being referenced
            /// </summary>
            public string ReferenceTextureName { get; set; }

            /// <summary>
            /// 0 means adapt to target width
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// 0 means adapt to target height
            /// </summary>
            public int Height { get; set; }

            /// <summary>
            /// multiple of target width to use (if width = 0)
            /// </summary>
            public float WidthFactor { get; set; }

            /// <summary>
            /// multiple of target height to use (if height = 0)
            /// </summary>
            public float HeightFactor { get; set; }

            /// <summary>
            /// more than one means MRT
            /// </summary>
            public List<PixelFormat> FormatList { get; set; }

            /// <summary>
            /// FSAA enabled; 
            /// true = determine from main target (if render_scene), false = disable
            /// </summary>
            public bool FSAA { get; set; }

            /// <summary>
            /// Do sRGB gamma correction on write (only 8-bit per channel formats)
            /// </summary>
            public bool HWGammaWrite { get; set; }

            /// <summary>
            /// Depth Buffer's pool ID. (unrelated to "pool" variable below)
            /// </summary>
            public ushort DepthBufferID { get; set; }

            /// <summary>
            /// whether to use pooled textures for this one
            /// </summary>
            public bool Pooled { get; set; }

            /// <summary>
            /// Which scope has access to this texture
            /// </summary>
            public TextureScope Scope { get; set; }

            /// <summary>
            /// Creates a new local texture definition.
            /// </summary>
            public TextureDefinition()
            {
                WidthFactor = 1.0f;
                HeightFactor = 1.0f;
                FSAA = true;
                DepthBufferID = 1;
                Scope = TextureScope.Local;
                ReferenceCompositorName = string.Empty;
                ReferenceTextureName = string.Empty;
                FormatList = new List<PixelFormat>();
            }
        }

        /// <summary>
        /// local texture definitions.
        /// </summary>
        private List<TextureDefinition> _textureDefinitions;

        /// <summary>
        /// Intermediate target passes
        /// </summary>
        private List<CompositionTargetPass> _targetPasses;

        /// <summary>
        /// Output target pass (can be only one)
        /// </summary>
        private CompositionTargetPass _outputTarget;

        /// <summary>
        /// Optional scheme name
        /// </summary>
        private string _schemeName;

        /// <summary>
        /// Optional compositor logic name
        /// </summary>
        private string _compositorLogicName;

        /// <summary>
        /// Parent compositor.
        /// </summary>
        private Compositor _parent;

        /// <summary>
        /// Create's a new Composition technique
        /// </summary>
        /// <param name="parent">parent of this technique</param>
        public CompositionTechnique( Compositor parent )
        {
            _parent = parent;
            _outputTarget = new CompositionTargetPass( this );
            _textureDefinitions = new List<TextureDefinition>();
            _targetPasses = new List<CompositionTargetPass>();
            _schemeName = string.Empty;
            _compositorLogicName = string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            RemoveAllTextureDefinitions();
            RemoveAllTargetPasses();
            _outputTarget = null;
        }

        /// <summary>
        /// Get's the amount of local texture definitions.
        /// </summary>
        public int NumTextureDefinitions
        {
            get
            {
                return _textureDefinitions.Count;
            }
        }

        /// <summary>
        /// Get's a list of all texture definitions.
        /// </summary>
        public List<TextureDefinition> TextureDefinitions
        {
            get
            {
                return _textureDefinitions;
            }
        }

        /// <summary>
        /// Get's the number of target passes.
        /// </summary>
        public int NumTargetPasses
        {
            get
            {
                return _targetPasses.Count;
            }
        }

        /// <summary>
        /// Get's a list of all target passes of this technique.
        /// </summary>
        public List<CompositionTargetPass> TargetPasses
        {
            get
            {
                return _targetPasses;
            }
        }

        /// <summary>
        /// Get's or set's a scheme name for this technique,
        /// used to switch between multiple techniques by choice,
        /// rather than for hardware compatibility.
        /// </summary>
        public virtual string SchemeName
        {
            get
            {
                return _schemeName;
            }
            set
            {
                _schemeName = value;
            }
        }

        /// <summary>
        /// Get's or set's the logic name, assigned to this technique.
        /// Instances if this technique will be auto-coupled with the matching logic.
        /// </summary>
        public string CompositorLogicName
        {
            get
            {
                return _compositorLogicName;
            }
            set
            {
                _compositorLogicName = value;
            }
        }

        /// <summary>
        /// Get's the parent of this technique.
        /// </summary>
        public Compositor Parent
        {
            get
            {
                return _parent;
            }
        }

        /// <summary>
        /// Get's the output (final) target pass.
        /// </summary>
        public CompositionTargetPass OutputTarget
        {
            get
            {
                return _outputTarget;
            }
        }

        /// <summary>
        /// Create a new local texture definition, and return a pointer to it.
        /// </summary>
        /// <param name="name">name of the local texture</param>
        /// <returns>pointer to a texture definition</returns>
        public TextureDefinition CreateTextureDefinition( string name )
        {
            TextureDefinition t = new TextureDefinition();
            t.Name = name;
            _textureDefinitions.Add( t );
            return t;
        }

        /// <summary>
        /// Remove and destroy a local texture definition.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveTextureDefinition( int index )
        {
            Debug.Assert( index < _textureDefinitions.Count, "Index out of bounds, CompositionTechnqiuq.RemoveTextureDefinition" );
            _textureDefinitions.RemoveAt( index );
        }

        /// <summary>
        /// Get's a local texture definition by index.
        /// </summary>
        /// <param name="index">index of the texture definition</param>
        /// <returns>texture definition for the given index</returns>
        public TextureDefinition GetTextureDefinition( int index )
        {
            Debug.Assert( index < _textureDefinitions.Count, "Index out of bounds, CompositionTechnqiuq.GetTextureDefinition" );
            return _textureDefinitions[ index ];
        }

        /// <summary>
        /// Get's a local texture definition by name.
        /// </summary>
        /// <param name="name">name of the texture definition</param>
        /// <returns>texture definition for the given name.if noone exists, null</returns>
        public TextureDefinition GetTextureDefinition( string name )
        {
            foreach ( TextureDefinition t in _textureDefinitions )
            {
                if ( t.Name == name )
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Remove's all texture definitions.
        /// </summary>
        public void RemoveAllTextureDefinitions()
        {
            _textureDefinitions.Clear();
        }

        /// <summary>
        /// Create's a new target pass.
        /// </summary>
        /// <returns>pointer to a new target pass</returns>
        public CompositionTargetPass CreateTargetPass()
        {
            CompositionTargetPass t = new CompositionTargetPass( this );
            _targetPasses.Add( t );
            return t;
        }

        /// <summary>
        /// Remove's and destroys a target pass.
        /// </summary>
        /// <param name="index">index of the target pass to remove to.</param>
        public void RemoveTargetPass( int index )
        {
            Debug.Assert( index < _targetPasses.Count, "Index out of bounds, CompositionTechnqiuqe.RemoveTargetPass" );
            _targetPasses.RemoveAt( index );
        }

        /// <summary>
        /// Get's a target passs by index.
        /// </summary>
        /// <param name="index">index of the target pass</param>
        /// <returns>target pass for the given index</returns>
        public CompositionTargetPass GetTargetPass( int index )
        {
            Debug.Assert( index < _targetPasses.Count, "Index out of bounds, CompositionTechnqiuqe.GetTargetPass" );
            return _targetPasses[ index ];
        }

        /// <summary>
        /// Remove's all target passes from this technique.
        /// </summary>
        public void RemoveAllTargetPasses()
        {
            _targetPasses.Clear();
        }

        /// <summary>
        /// Determine if this technique is supported on the current rendering device.
        /// </summary>
        /// <param name="allowTextureDegradation">True to accept a reduction in texture depth</param>
        /// <returns>true if supported, otherwise false</returns>
        /// <remarks>
        /// A technique is supported if all materials referenced have a supported
        /// technique, and the intermediate texture formats requested are supported
        /// Material support is a cast-iron requirement, but if no texture formats
        /// are directly supported we can let the rendersystem create the closest
        /// match for the least demanding technique
        /// </remarks>
        public virtual bool IsSupported( bool allowTextureDegradation )
        {
            // Check output target pass is supported
            if ( !_outputTarget.IsSupported )
            {
                return false;
            }
            // Check all target passes is supported
            foreach ( CompositionTargetPass targetPass in _targetPasses )
            {
                if ( !targetPass.IsSupported )
                {
                    return false;
                }
            }

            TextureManager texMgr = TextureManager.Instance;
            // Check all Texture Definitions is supported
            foreach ( TextureDefinition td in _textureDefinitions )
            {
                // Firstly check MRTs
                if ( td.FormatList.Count > Root.Instance.RenderSystem.HardwareCapabilities.MultiRenderTargetCount )
                {
                    return false;
                }

                foreach ( PixelFormat pf in td.FormatList )
                {
                    // Check whether equivalent supported
                    if ( allowTextureDegradation )
                    {
                        // Don't care about exact format so long as something is supported
                        if ( texMgr.GetNativeFormat( TextureType.TwoD, pf, TextureUsage.RenderTarget ) == PixelFormat.Unknown )
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // Need a format which is the same number of bits to pass
                        if ( !texMgr.IsEquivalentFormatSupported( TextureType.TwoD, pf, TextureUsage.RenderTarget ) )
                        {
                            return false;
                        }
                    }
                }
            }

            // must be ok
            return true;
        }
    }
}