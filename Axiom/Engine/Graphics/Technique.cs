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

using System;
using System.Collections;
using System.Diagnostics;
using Axiom.Core;

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Class representing an approach to rendering a particular Material. 
	/// </summary>
	/// <remarks>
    ///    The engine will attempt to use the best technique supported by the active hardware, 
    ///    unless you specifically request a lower detail technique (say for distant rendering)
	/// </remarks>
	public class Technique
	{
		#region Member variables

        /// <summary>
        ///    The material that owns this technique.
        /// </summary>
        protected Material parent;
        /// <summary>
        ///    The list of passes (fixed function or programmable) contained in this technique.
        /// </summary>
        protected ArrayList passes = new ArrayList();
        /// <summary>
        ///    Flag that states whether or not this technique is supported on the current hardware.
        /// </summary>
        protected bool isSupported;
		
		#endregion
		
		#region Constructors
		
		public Technique(Material parent) {
            this.parent = parent;
		}
		
		#endregion
		
		#region Methods

        /// <summary>
        ///    Compilation method for Techniques.  See <see cref="Axiom.Core.Material"/>
        /// </summary>
        /// <param name="autoManageTextureUnits">
        ///    Determines whether or not the engine should split up extra texture unit requests
        ///    into extra passes if the hardware does not have enough available units.
        /// </param>
        internal void Compile(bool autoManageTextureUnits) {
            // grab a ref to the current hardware caps
            HardwareCaps caps = Engine.Instance.RenderSystem.Caps;
            int numAvailTexUnits = caps.NumTextureUnits;

            // check requirements for each pass
            for(int i = 0; i < passes.Count; i++) {
                Pass pass = (Pass)passes[i];

                int numTexUnitsRequested = pass.NumTextureUnitStages;

                if(pass.IsProgrammable) {
                    // check texture units
                    if(numTexUnitsRequested > numAvailTexUnits) {
                        // can't do this, since programmable passes cannot be split automatically
                        isSupported = false;
                        return;
                    }
                    // check vertex program version
                    if(!GpuProgramManager.Instance.IsSyntaxSupported(pass.VertexProgram.SyntaxCode)) {
                        // can't do this one
                        isSupported = false;
                        return;
                    }
                    // check fragment program version
                    if(!GpuProgramManager.Instance.IsSyntaxSupported(pass.FragmentProgram.SyntaxCode)) {
                        // can't do this one
                        isSupported = false;
                        return;
                    }
                }
                else {
                    // keep splitting until the texture units required for this pass are available
                    while(numTexUnitsRequested > numAvailTexUnits) {
                        // split this pass up into more passes
                        pass = pass.Split(numAvailTexUnits);
                        numTexUnitsRequested = pass.NumTextureUnitStages;
                    }
                }
            } // for

            // if we made it this far, we are good to go!
            isSupported = true;
        }

        /// <summary>
        ///    Creates a new Pass for this technique.
        /// </summary>
        /// <remarks>
        ///    A Pass is a single rendering pass, ie a single draw of the given material.
        ///    Note that if you create a non-programmable pass, during compilation of the
        ///    material the pass may be split into multiple passes if the graphics card cannot
        ///    handle the number of texture units requested. For programmable passes, however, 
        ///    the number of passes you create will never be altered, so you have to make sure 
        ///    that you create an alternative fallback Technique for if a card does not have 
        ///    enough facilities for what you're asking for.
        /// </remarks>
        /// <param name="programmable">
        ///    True if programmable via vertex or fragment programs, false if fixed function.
        /// </param>
        /// <returns>A new Pass object reference.</returns>
        public Pass CreatePass(bool programmable) {
            Pass pass = new Pass(this, programmable);
            passes.Add(pass);
            return pass;
        }

        /// <summary>
        ///    Retreives the Pass at the specified index.
        /// </summary>
        /// <param name="index">Index of the Pass to retreive.</param>
        public Pass GetPass(int index) {
            Debug.Assert(index < passes.Count, "index < passes.Count");

            return (Pass)passes[index];
        }

        /// <summary>
        ///    Loads resources required by this Technique.
        /// </summary>
        public void Load() {
            Debug.Assert(isSupported, "This technique is not supported.");

            // load each pass
            for(int i = 0; i < passes.Count; i++) {
                ((Pass)passes[i]).Load();
            }
        }

        /// <summary>
        ///    Forces this Technique to recompile.
        /// </summary>
        /// <remarks>
        ///    The parent Material is asked to recompile to accomplish this.
        /// </remarks>
        internal void NotifyNeedsRecompile() {
            parent.NotifyNeedsRecompile();
        }

        /// <summary>
        ///    Removes the specified Pass from this Technique.
        /// </summary>
        /// <param name="pass">A reference to the Pass to be removed.</param>
        public void RemovePass(Pass pass) {
            Debug.Assert(pass != null, "pass != null");

            passes.Remove(pass);
        }

        /// <summary>
        ///    Unloads resources used by this Technique.
        /// </summary>
        public void Unload() {
            // load each pass
            for(int i = 0; i < passes.Count; i++) {
                ((Pass)passes[i]).Unload();
            }
        }
		
		#endregion
		
		#region Properties
		
        /// <summary>
        ///    Returns true if this Technique has already been loaded.
        /// </summary>
        public bool IsLoaded {
            get {
                return parent.IsLoaded;
            }
        }

        /// <summary>
        ///    Flag that states whether or not this technique is supported on the current hardware.
        /// </summary>
        /// <remarks>
        ///    This will only be correct after the Technique has been compiled, which is
        ///    usually triggered in Material.Compile.
        /// </remarks>
        public bool IsSupported {
            get {
                return isSupported;
            }
        }

        /// <summary>
        ///    Returns true if this Technique involves transparency.
        /// </summary>
        /// <remarks>
        ///    This basically boils down to whether the first pass
        ///    has a scene blending factor. Even if the other passes 
        ///    do not, the base color, including parts of the original 
        ///    scene, may be used for blending, therefore we have to treat
        ///    the whole Technique as transparent.
        /// </remarks>
        public bool IsTransparent {
            get {
                if(passes.Count == 0) {
                    return false;
                }
                else {
                    // based on the transparency of the first pass
                    return ((Pass)passes[0]).IsTransparent;
                }
            }
        }

        /// <summary>
        ///    Gets the number of passes within this Technique.
        /// </summary>
        public int NumPasses {
            get {
                return passes.Count;
            }
        }

        /// <summary>
        ///    Gets a reference to the Material that owns this Technique.
        /// </summary>
        public Material Parent {
            get {
                return parent;
            }
        }

		#endregion
	}
}
