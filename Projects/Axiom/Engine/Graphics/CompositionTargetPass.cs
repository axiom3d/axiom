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

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    /// Object representing one render to a RenderTarget or Viewport in the Axiom Composition
    /// framework.
    /// </summary>
    public class CompositionTargetPass : IDisposable
    {
        /// <summary>
        /// Parent technique
        /// </summary>
        private CompositionTechnique _parent;

        /// <summary>
        /// Input name
        /// </summary>
        private CompositorInputMode _inputMode;

        /// <summary>
        /// (local) output texture
        /// </summary>
        private string _outputName;

        /// <summary>
        /// Passes
        /// </summary>
        private List<CompositionPass> _passes;

        /// <summary>
        /// This target pass is only executed initially after the effect
        /// has been enabled.
        /// </summary>
        private bool _onlyIntial;

        /// <summary>
        /// Visibility mask for this render
        /// </summary>
        private ulong _visibilityMask;

        /// <summary>
        /// LOD bias of this render
        /// </summary>
        private float _lodBias;

        /// <summary>
        /// Material scheme name
        /// </summary>
        private string _materialScheme;

        /// <summary>
        /// Shadows option
        /// </summary>
        private bool _shadowsEnabled;

        /// <summary>
        /// Get's or Set's input mode of this TargetPass
        /// </summary>
        public CompositorInputMode InputMode
        {
            get
            {
                return _inputMode;
            }
            set
            {
                _inputMode = value;
            }
        }

        /// <summary>
        /// Get's or Set's output local texture name
        /// </summary>
        public string OutputName
        {
            get
            {
                return _outputName;
            }
            set
            {
                _outputName = value;
            }
        }

        /// <summary>
        /// Get's or Set's "only initial" flag. This makes that this target pass is only executed initially
        /// after the effect has been enabled.
        /// </summary>
        public bool OnlyInitial
        {
            get
            {
                return _onlyIntial;
            }
            set
            {
                _onlyIntial = value;
            }
        }

        /// <summary>
        /// Get's or set's the scene visibility mask used by this pass
        /// </summary>
        public ulong VisibilityMask
        {
            get
            {
                return _visibilityMask;
            }
            set
            {
                _visibilityMask = value;
            }
        }

        /// <summary>
        /// Get's or Set's the material scheme used by this target pass.
        /// </summary>
        /// <remarks>
        /// Only applicable to targets that render the scene as
        /// one of their passes.
        /// </remarks>
        public string MaterialScheme
        {
            get
            {
                return _materialScheme;
            }
            set
            {
                _materialScheme = value;
            }
        }

        /// <summary>
        /// Get's or Set's  whether shadows are enabled in this target pass.
        /// </summary>
        public bool ShadowsEnabled
        {
            get
            {
                return _shadowsEnabled;
            }
            set
            {
                _shadowsEnabled = value;
            }
        }

        /// <summary>
        /// Get's or Set's the scene LOD bias used by this pass. The default is 1.0,
        /// everything below that means lower quality, higher means higher quality.
        /// </summary>
        public float LodBias
        {
            get
            {
                return _lodBias;
            }
            set
            {
                _lodBias = value;
            }
        }

        /// <summary>
        /// Get's the number of passes.
        /// </summary>
        public int NumPasses
        {
            get
            {
                return _passes.Count;
            }
        }

        /// <summary>
        /// Get's parent object
        /// </summary>
        public CompositionTechnique Parent
        {
            get
            {
                return _parent;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<CompositionPass> Passes
        {
            get
            {
                return _passes;
            }
            set
            {
                _passes = value;
            }
        }

        /// <summary>
        /// Determine if this target pass is supported on the current rendering device.
        /// </summary>
        public bool IsSupported
        {
            get
            {
                // A target pass is supported if all passes are supported
                foreach ( CompositionPass pass in _passes )
                {
                    if ( !pass.IsSupported )
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public CompositionTargetPass( CompositionTechnique parent )
        {
            _parent = parent;
            _inputMode = CompositorInputMode.None;
            _onlyIntial = false;
            _visibilityMask = 0xFFFFFFFF;
            _lodBias = 1.0f;
            _materialScheme = MaterialManager.DefaultSchemeName;
            _shadowsEnabled = true;
            _passes = new List<CompositionPass>();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            RemoveAllPasses();
        }

        /// <summary>
        /// Create a new pass, and return a pointer to it.
        /// </summary>
        /// <returns>new pass</returns>
        public CompositionPass CreatePass()
        {
            CompositionPass pass = new CompositionPass( this );
            _passes.Add( pass );
            return pass;
        }

        /// <summary>
        ///  Remove a pass. It will also be destroyed.
        /// </summary>
        /// <param name="index"></param>
        public void RemovePass( int index )
        {
            Debug.Assert( index < _passes.Count, "Index out of bounds." );
            _passes.RemoveAt( index );
        }

        /// <summary>
        /// Get a pass.
        /// </summary>
        /// <param name="index"> index of the pass</param>
        /// <returns>pass for given index</returns>
        public CompositionPass GetPass( int index )
        {
            Debug.Assert( index < _passes.Count, "Index out of bounds." );
            return _passes[ index ];
        }

        /// <summary>
        /// Remove all passes
        /// </summary>
        public void RemoveAllPasses()
        {
            _passes.Clear();
        }
    }
}