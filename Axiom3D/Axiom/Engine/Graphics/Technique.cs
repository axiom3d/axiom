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

// This is coming from RealmForge.Utility
using Axiom.Core;

namespace Axiom
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
        #region Fields

        /// <summary>
        ///    The material that owns this technique.
        /// </summary>
        protected Material parent;
        /// <summary>
        ///    The list of passes (fixed function or programmable) contained in this technique.
        /// </summary>
        protected PassList passes = new PassList();
        /// <summary>
        ///		List of derived passes, categorized (and ordered) into illumination stages.
        /// </summary>
        protected ArrayList illuminationPasses = new ArrayList();
        /// <summary>
        ///    Flag that states whether or not this technique is supported on the current hardware.
        /// </summary>
        protected bool isSupported;
        /// <summary>
        ///    Name of this technique.
        /// </summary>
        protected string name;
        /// <summary>
        ///		Level of detail index for this technique.
        /// </summary>
        protected int lodIndex;

        #endregion

        #region Constructors

        public Technique( Material parent )
        {
            this.parent = parent;
        }

        #endregion

        #region Methods

        /// <summary>
        ///		Internal method for clearing the illumination pass list.
        /// </summary>
        protected void ClearIlluminationPasses()
        {
            for ( int i = 0; i < illuminationPasses.Count; i++ )
            {
                IlluminationPass iPass = (IlluminationPass)illuminationPasses[i];

                if ( iPass.DestroyOnShutdown )
                {
                    iPass.Pass.QueueForDeletion();
                }
            }

            illuminationPasses.Clear();
        }

        /// <summary>
        ///    Clones this Technique.
        /// </summary>
        /// <param name="parent">Material that will own this technique.</param>
        /// <returns></returns>
        public Technique Clone( Material parent )
        {
            Technique newTechnique = new Technique( parent );

            CopyTo( newTechnique );

            return newTechnique;
        }

        /// <summary>
        ///		Copy the details of this Technique to another.
        /// </summary>
        /// <param name="target"></param>
        public void CopyTo( Technique target )
        {
            target.isSupported = isSupported;
            target.lodIndex = lodIndex;

            target.RemoveAllPasses();

            // clone each pass and add that to the new technique
            for ( int i = 0; i < passes.Count; i++ )
            {
                Pass pass = (Pass)passes[i];
                Pass newPass = pass.Clone( target, pass.Index );
                target.passes.Add( newPass );
            }

            // recompile illumination passes
            target.CompileIlluminationPasses();
        }

        /// <summary>
        ///    Compilation method for Techniques.  See <see cref="Axiom.Core.Material"/>
        /// </summary>
        /// <param name="autoManageTextureUnits">
        ///    Determines whether or not the engine should split up extra texture unit requests
        ///    into extra passes if the hardware does not have enough available units.
        /// </param>
        internal void Compile( bool autoManageTextureUnits )
        {
            // assume not supported unles it proves otherwise
            isSupported = false;

            // grab a ref to the current hardware caps
            HardwareCaps caps = Root.Instance.RenderSystem.Caps;
            int numAvailTexUnits = caps.TextureUnitCount;

            // check requirements for each pass
            for ( int i = 0; i < passes.Count; i++ )
            {
                Pass pass = (Pass)passes[i];

                int numTexUnitsRequested = pass.NumTextureUnitStages;

                if ( pass.HasFragmentProgram )
                {
                    // check texture units
                    if ( numTexUnitsRequested > numAvailTexUnits )
                    {
                        // can't do this, since programmable passes cannot be split automatically
                        return;
                    }

                    // check fragment program version
                    if ( !pass.FragmentProgram.IsSupported )
                    {
                        // can't do this one
                        return;
                    }
                }
                else
                {
                    // check support for a few fixed function options while we are here
                    for ( int j = 0; j < pass.NumTextureUnitStages; j++ )
                    {
                        TextureUnitState texUnit = pass.GetTextureUnitState( j );

                        // check to make sure we have some cube mapping support
                        if ( texUnit.Is3D && !caps.CheckCap( Capabilities.CubeMapping ) )
                        {
                            return;
                        }

                        // if this is a Dot3 blending layer, make sure we can support it
                        if ( texUnit.ColorBlendMode.operation == LayerBlendOperationEx.DotProduct && !caps.CheckCap( Capabilities.Dot3 ) )
                        {
                            return;
                        }
                    }

                    // keep splitting until the texture units required for this pass are available
                    while ( numTexUnitsRequested > numAvailTexUnits )
                    {
                        // split this pass up into more passes
                        pass = pass.Split( numAvailTexUnits );
                        numTexUnitsRequested = pass.NumTextureUnitStages;
                    }
                }

                // if this has a vertex program, check the syntax code to be sure the hardware supports it
                if ( pass.HasVertexProgram )
                {
                    // check vertex program version
                    if ( !pass.VertexProgram.IsSupported )
                    {
                        // can't do this one
                        return;
                    }
                }
            } // for

            // if we made it this far, we are good to go!
            isSupported = true;

            // Now compile for categorised illumination, in case we need it later
            CompileIlluminationPasses();
        }

        /// <summary>
        ///		Internal method for splitting the passes into illumination passes.
        /// </summary>
        public void CompileIlluminationPasses()
        {
            ClearIlluminationPasses();

            // don't need to split transparent passes since they are rendered seperately
            if ( this.IsTransparent )
            {
                return;
            }

            // start off with ambient passes
            IlluminationStage stage = IlluminationStage.Ambient;

            bool hasAmbient = false;

            for ( int i = 0; i < passes.Count; /* increment in logic */)
            {
                Pass pass = (Pass)passes[i];
                IlluminationPass iPass;

                switch ( stage )
                {
                    case IlluminationStage.Ambient:
                        // keep looking for ambient only
                        if ( pass.IsAmbientOnly )
                        {
                            iPass = new IlluminationPass();
                            iPass.OriginalPass = pass;
                            iPass.Pass = pass;
                            iPass.Stage = stage;
                            illuminationPasses.Add( iPass );
                            hasAmbient = true;

                            // progress to the next pass
                            i++;
                        }
                        else
                        {
                            // split off any ambient part
                            if ( pass.Ambient.CompareTo( ColorEx.Black ) != 0 ||
                                pass.Emissive.CompareTo( ColorEx.Black ) != 0 )
                            {

                                Pass newPass = new Pass( this, pass.Index );
                                pass.CopyTo( newPass );

                                // remove any texture units
                                newPass.RemoveAllTextureUnitStates();

                                // also remove any fragment program
                                if ( newPass.HasFragmentProgram )
                                {
                                    newPass.SetFragmentProgram( "" );
                                }

                                // We have to leave vertex program alone (if any) and
                                // just trust that the author is using light bindings, which 
                                // we will ensure there are none in the ambient pass
                                newPass.Diffuse = ColorEx.Black;
                                newPass.Specular = ColorEx.Black;

                                // if ambient and emissive are zero, then color write isn't needed
                                if ( newPass.Ambient.CompareTo( ColorEx.Black ) == 0 &&
                                    newPass.Emissive.CompareTo( ColorEx.Black ) == 0 )
                                {

                                    newPass.ColorWrite = false;
                                }

                                iPass = new IlluminationPass();
                                iPass.DestroyOnShutdown = true;
                                iPass.OriginalPass = pass;
                                iPass.Pass = newPass;
                                iPass.Stage = stage;

                                illuminationPasses.Add( iPass );
                                hasAmbient = true;
                            }

                            if ( !hasAmbient )
                            {
                                // make up a new basic pass
                                Pass newPass = new Pass( this, pass.Index );
                                pass.CopyTo( newPass );

                                newPass.Ambient = ColorEx.Black;
                                newPass.Diffuse = ColorEx.Black;

                                iPass = new IlluminationPass();
                                iPass.DestroyOnShutdown = true;
                                iPass.OriginalPass = pass;
                                iPass.Pass = newPass;
                                iPass.Stage = stage;
                                illuminationPasses.Add( iPass );
                                hasAmbient = true;
                            }

                            // this means we are done with ambients, progress to per-light
                            stage = IlluminationStage.PerLight;
                        }

                        break;

                    case IlluminationStage.PerLight:
                        if ( pass.RunOncePerLight )
                        {
                            // if this is per-light already, use it directly
                            iPass = new IlluminationPass();
                            iPass.DestroyOnShutdown = false;
                            iPass.OriginalPass = pass;
                            iPass.Pass = pass;
                            iPass.Stage = stage;
                            illuminationPasses.Add( iPass );

                            // progress to the next pass
                            i++;
                        }
                        else
                        {
                            // split off per-light details (can only be done for one)
                            if ( pass.LightingEnabled &&
                                ( pass.Diffuse.CompareTo( ColorEx.Black ) != 0 ||
                                pass.Specular.CompareTo( ColorEx.Black ) != 0 ) )
                            {

                                // copy existing pass
                                Pass newPass = new Pass( this, pass.Index );
                                pass.CopyTo( newPass );

                                newPass.RemoveAllTextureUnitStates();

                                // also remove any fragment program
                                if ( newPass.HasFragmentProgram )
                                {
                                    newPass.SetFragmentProgram( "" );
                                }

                                // Cannot remove vertex program, have to assume that
                                // it will process diffuse lights, ambient will be turned off
                                newPass.Ambient = ColorEx.Black;
                                newPass.Emissive = ColorEx.Black;

                                // must be additive
                                newPass.SetSceneBlending( SceneBlendFactor.One, SceneBlendFactor.One );

                                iPass = new IlluminationPass();
                                iPass.DestroyOnShutdown = true;
                                iPass.OriginalPass = pass;
                                iPass.Pass = newPass;
                                iPass.Stage = stage;

                                illuminationPasses.Add( iPass );
                            }

                            // This means the end of per-light passes
                            stage = IlluminationStage.Decal;
                        }

                        break;

                    case IlluminationStage.Decal:
                        // We just want a 'lighting off' pass to finish off
                        // and only if there are texture units
                        if ( pass.NumTextureUnitStages > 0 )
                        {
                            if ( !pass.LightingEnabled )
                            {
                                // we assume this pass already combines as required with the scene
                                iPass = new IlluminationPass();
                                iPass.DestroyOnShutdown = false;
                                iPass.OriginalPass = pass;
                                iPass.Pass = pass;
                                iPass.Stage = stage;
                                illuminationPasses.Add( iPass );
                            }
                            else
                            {
                                // Copy the pass and tweak away the lighting parts
                                Pass newPass = new Pass( this, pass.Index );
                                pass.CopyTo( newPass );
                                newPass.Ambient = ColorEx.Black;
                                newPass.Diffuse = ColorEx.Black;
                                newPass.Specular = ColorEx.Black;
                                newPass.Emissive = ColorEx.Black;
                                newPass.LightingEnabled = false;
                                // modulate
                                newPass.SetSceneBlending( SceneBlendFactor.DestColor, SceneBlendFactor.Zero );

                                // there is nothing we can do about vertex & fragment
                                // programs here, so people will just have to make their
                                // programs friendly-like if they want to use this technique
                                iPass = new IlluminationPass();
                                iPass.DestroyOnShutdown = true;
                                iPass.OriginalPass = pass;
                                iPass.Pass = newPass;
                                iPass.Stage = stage;
                                illuminationPasses.Add( iPass );
                            }
                        }

                        // always increment on decal, since nothing more to do with this pass
                        i++;

                        break;
                }
            }
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
        public Pass CreatePass()
        {
            Pass pass = new Pass( this, passes.Count );
            passes.Add( pass );
            return pass;
        }

        /// <summary>
        ///    Retreives the Pass at the specified index.
        /// </summary>
        /// <param name="index">Index of the Pass to retreive.</param>
        public Pass GetPass( int index )
        {
            Debug.Assert( index < passes.Count, "index < passes.Count" );

            return (Pass)passes[index];
        }

        /// <summary>
        ///    Retreives the IlluminationPass at the specified index.
        /// </summary>
        /// <param name="index">Index of the IlluminationPass to retreive.</param>
        public IlluminationPass GetIlluminationPass( int index )
        {
            Debug.Assert( index < illuminationPasses.Count, "index < illuminationPasses.Count" );

            return (IlluminationPass)illuminationPasses[index];
        }

        /// <summary>
        ///    Loads resources required by this Technique.
        /// </summary>
        public void Load()
        {
            Debug.Assert( isSupported, "This technique is not supported." );

            // load each pass
            for ( int i = 0; i < passes.Count; i++ )
            {
                ( (Pass)passes[i] ).Load();
            }
        }

        /// <summary>
        ///    Forces this Technique to recompile.
        /// </summary>
        /// <remarks>
        ///    The parent Material is asked to recompile to accomplish this.
        /// </remarks>
        internal void NotifyNeedsRecompile()
        {
            parent.NotifyNeedsRecompile();
        }

        /// <summary>
        ///    Removes the specified Pass from this Technique.
        /// </summary>
        /// <param name="pass">A reference to the Pass to be removed.</param>
        public void RemovePass( Pass pass )
        {
            Debug.Assert( pass != null, "pass != null" );

            pass.QueueForDeletion();

            passes.Remove( pass );
        }

        /// <summary>
        ///		Removes all passes from this technique and queues them for deletion.
        /// </summary>
        public void RemoveAllPasses()
        {
            // load each pass
            for ( int i = 0; i < passes.Count; i++ )
            {
                Pass pass = (Pass)passes[i];
                pass.QueueForDeletion();
            }

            passes.Clear();
        }

        public void SetSceneBlending( SceneBlendType blendType )
        {
            // load each pass
            for ( int i = 0; i < passes.Count; i++ )
            {
                ( (Pass)passes[i] ).SetSceneBlending( blendType );
            }
        }

        public void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
        {
            // load each pass
            for ( int i = 0; i < passes.Count; i++ )
            {
                ( (Pass)passes[i] ).SetSceneBlending( src, dest );
            }
        }

        /// <summary>
        ///    Unloads resources used by this Technique.
        /// </summary>
        public void Unload()
        {
            // load each pass
            for ( int i = 0; i < passes.Count; i++ )
            {
                ( (Pass)passes[i] ).Unload();
            }
        }

        #endregion

        #region Properties

        public ColorEx Ambient
        {
            set
            {
                for ( int i = 0; i < passes.Count; i++ )
                {
                    ( (Pass)passes[i] ).Ambient = value;
                }
            }
        }

        public CullingMode CullingMode
        {
            set
            {
                for ( int i = 0; i < passes.Count; i++ )
                {
                    ( (Pass)passes[i] ).CullMode = value;
                }
            }
        }

        public bool DepthCheck
        {
            set
            {
                for ( int i = 0; i < passes.Count; i++ )
                {
                    ( (Pass)passes[i] ).DepthCheck = value;
                }
            }
            get
            {
                if ( passes.Count == 0 )
                {
                    return false;
                }
                else
                {
                    // Base decision on the depth settings of the first pass
                    return ( (Pass)passes[0] ).DepthCheck;
                }
            }
        }

        public bool DepthWrite
        {
            set
            {
                for ( int i = 0; i < passes.Count; i++ )
                {
                    ( (Pass)passes[i] ).DepthWrite = value;
                }
            }
            get
            {
                if ( passes.Count == 0 )
                {
                    return false;
                }
                else
                {
                    // Base decision on the depth settings of the first pass
                    return ( (Pass)passes[0] ).DepthWrite;
                }
            }
        }

        public ColorEx Diffuse
        {
            set
            {
                for ( int i = 0; i < passes.Count; i++ )
                {
                    ( (Pass)passes[i] ).Diffuse = value;
                }
            }
        }

        /// <summary>
        ///    Returns true if this Technique has already been loaded.
        /// </summary>
        public bool IsLoaded
        {
            get
            {
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
        public bool IsSupported
        {
            get
            {
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
        public bool IsTransparent
        {
            get
            {
                if ( passes.Count == 0 )
                {
                    return false;
                }
                else
                {
                    // based on the transparency of the first pass
                    return ( (Pass)passes[0] ).IsTransparent;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Lighting
        {
            set
            {
                for ( int i = 0; i < passes.Count; i++ )
                {
                    ( (Pass)passes[i] ).LightingEnabled = value;
                }
            }
        }

        /// <summary>
        ///		Assigns a level-of-detail (LOD) index to this Technique.
        /// </summary>
        /// <remarks>
        ///		As noted previously, as well as providing fallback support for various
        ///		graphics cards, multiple Technique objects can also be used to implement
        ///		material LOD, where the detail of the material diminishes with distance to 
        ///		save rendering power.
        ///		<p/>
        ///		By default, all Techniques have a LOD index of 0, which means they are the highest
        ///		level of detail. Increasing LOD indexes are lower levels of detail. You can 
        ///		assign more than one Technique to the same LOD index, meaning that the best 
        ///		Technique that is supported at that LOD index is used. 
        ///		<p/>
        ///		You should not leave gaps in the LOD sequence; the engine will allow you to do this
        ///		and will continue to function as if the LODs were sequential, but it will 
        ///		confuse matters.
        /// </remarks>
        public int LodIndex
        {
            get
            {
                return lodIndex;
            }
            set
            {
                lodIndex = value;
                NotifyNeedsRecompile();
            }
        }

        public ManualCullingMode ManualCullMode
        {
            set
            {
                for ( int i = 0; i < passes.Count; i++ )
                {
                    ( (Pass)passes[i] ).ManualCullMode = value;
                }
            }
        }

        /// <summary>
        ///    Gets/Sets the name of this technique.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        /// <summary>
        ///    Gets the number of passes within this Technique.
        /// </summary>
        public int NumPasses
        {
            get
            {
                return passes.Count;
            }
        }

        /// <summary>
        ///		Gets the number of illumination passes compiled from this technique.
        /// </summary>
        public int IlluminationPassCount
        {
            get
            {
                return illuminationPasses.Count;
            }
        }

        /// <summary>
        ///    Gets a reference to the Material that owns this Technique.
        /// </summary>
        public Material Parent
        {
            get
            {
                return parent;
            }
        }

        public TextureFiltering TextureFiltering
        {
            set
            {
                for ( int i = 0; i < passes.Count; i++ )
                {
                    ( (Pass)passes[i] ).TextureFiltering = value;
                }
            }
        }

        #endregion
    }
}
