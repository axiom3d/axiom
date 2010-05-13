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

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    /// Class representing a Compositor object. Compositors provide the means
    /// to flexibly "composite" the final rendering result from multiple scene renders
    /// and intermediate operations like rendering fullscreen quads. This makes
    /// it possible to apply postfilter effects, HDRI postprocessing, and shadow
    /// effects to a Viewport.
    /// </summary>
    public class Compositor : Resource
    {

        /// <summary>
        /// List of added techniques
        /// </summary>
        private List<CompositionTechnique> _techniques;

        /// <summary>
        /// list of supported techniques;
        /// </summary>
        private List<CompositionTechnique> _supportedTechniques;

        /// <summary>
        /// Compilation required
        /// This is set if the techniques change and the supportedness of techniques has to be
        /// re-evaluated.
        /// </summary>
        private bool _compilationRequired;

        /// <summary>
        /// Store a list of textures we've created
        /// </summary>
        private Dictionary<string, Texture> _globalTextures;

        /// <summary>
        /// Store a list of MRTs we've created
        /// </summary>
        private Dictionary<string, MultiRenderTarget> _globalMRTs;

        /// <summary>
        /// 
        /// </summary>
        private static int _dummyCounter = 0;

        /// <summary>
        /// Get's the amount of techiques.
        /// </summary>
        public int NumTechniques
        {
            get
            {
                return _techniques.Count;
            }
        }

        /// <summary>
        /// Get's access to all techniques of this compositor.
        /// </summary>
        public List<CompositionTechnique> Techniques
        {
            get
            {
                return _techniques;
            }
        }

        /// <summary>
        /// Get's the amount of supported techniques.
        /// </summary>
        /// <remarks>
        /// The supported technique list is only available after this compositor has been compiled,
        /// which typically happens on loading it. Therefore, if this method returns
        /// an empty list, try calling Compositor.Load();
        /// </remarks>
        public int NumSupportedTechniques
        {
            get
            {
                return _supportedTechniques.Count;
            }
        }

        /// <summary>
        /// Get's access to all supported techniques of this compositor.
        /// </summary>
        public List<CompositionTechnique> SupportedTechniques
        {
            get
            {
                return _supportedTechniques;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creator"></param>
        /// <param name="name"></param>
        /// <param name="handle"></param>
        /// <param name="group"></param>
        public Compositor( ResourceManager creator, string name, ulong handle,
                           string group )
            : this( creator, name, handle, group, false, null )
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creator"></param>
        /// <param name="name"></param>
        /// <param name="handle"></param>
        /// <param name="group"></param>
        /// <param name="isManual"></param>
        /// <param name="loader"></param>
        public Compositor( ResourceManager creator, string name, ResourceHandle handle,
                           string group, bool isManual, IManualResourceLoader loader )
            : base( creator, name, handle, group, isManual, loader )
        {
            _compilationRequired = true;
            _techniques = new List<CompositionTechnique>();
            _supportedTechniques = new List<CompositionTechnique>();
            _globalMRTs = new Dictionary<string, MultiRenderTarget>();
            _globalTextures = new Dictionary<string, Texture>();
        }

        /// <summary>
        /// Create's a new technique, and return a pointer to it.
        /// </summary>
        /// <returns>new created composition technique</returns>
        public CompositionTechnique CreateTechnique()
        {
            CompositionTechnique t = new CompositionTechnique( this );
            _techniques.Add( t );
            _compilationRequired = true;
            return t;
        }

        /// <summary>
        /// Remove's a technique, it will be also destroyed.
        /// </summary>
        /// <param name="index">index of the technique to remove</param>
        public void RemoveTechnique( int index )
        {
            Debug.Assert( index < _techniques.Count, "Index out of bounds, Compositor.RemoveTechnique" );
            _techniques.RemoveAt( index );
            _compilationRequired = true;
            _supportedTechniques.Clear();
        }

        /// <summary>
        /// Get's a technique by index
        /// </summary>
        /// <param name="index">index for the technique to get</param>
        /// <returns>technique given by index</returns>
        private CompositionTechnique GetTechnique( int index )
        {
            Debug.Assert( index < _techniques.Count, "Index out of bounds, Compositor.GetTechnique" );
            return _techniques[ index ];
        }

        /// <summary>
        /// Remove's all techniques.
        /// </summary>
        public void RemoveAllTechniques()
        {
            _techniques.Clear();
            _supportedTechniques.Clear();
            _compilationRequired = true;
        }

        /// <summary>
        /// Get' a supported technique by index.
        /// </summary>
        /// <param name="index">index of the technique</param>
        /// <returns>technique for given index</returns>
        /// <remarks>
        /// The supported technique list is only available after this compositor has been compiled,
        /// which typically happens on loading it. Therefore, if this method returns
        /// an empty list, try calling Compositor.Load();
        /// </remarks>
        public CompositionTechnique GetSupportedTechnique( int index )
        {
            Debug.Assert( index < _supportedTechniques.Count, "Index out of bounds, Compositor.GetSupportedTechnique" );
            return _supportedTechniques[ index ];
        }

        /// <summary>
        /// Get a pointer to a supported technique for a given scheme.
        /// </summary>
        /// <returns>the first supported technique with no specific scheme will be returned.</returns>
        public CompositionTechnique GetSupportedTechnique()
        {
            return GetSupportedTechnique( string.Empty );
        }

        /// <summary>
        /// Get a pointer to a supported technique for a given scheme.
        /// </summary>
        /// <param name="name"> The scheme name you are looking for.
        /// Blank means to look for techniques with no scheme associated
        /// </param>
        /// <returns></returns>
        /// <remarks>
        /// If there is no specific supported technique with this scheme name,
        /// then the first supported technique with no specific scheme will be returned.
        /// </remarks>
        public CompositionTechnique GetSupportedTechnique( string schemeName )
        {
            foreach ( CompositionTechnique t in _supportedTechniques )
            {
                if ( t.SchemeName == schemeName )
                {
                    return t;
                }
            }
            // didn't find a matching one
            foreach ( CompositionTechnique t in _supportedTechniques )
            {
                if ( t.SchemeName == string.Empty )
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Get's the instance name for a global texture.
        /// </summary>
        /// <param name="name">The name of the texture in the original compositor definition</param>
        /// <param name="mrtIndex">If name identifies a MRT, which texture attachment to retrieve</param>
        /// <returns>The instance name for the texture, corresponds to a real texture</returns>
        public string GetTextureInstanceName( string name, int mrtIndex )
        {
            return GetTextureInstance( name, mrtIndex ).Name;
        }

        /// <summary>
        /// Get's the instance of a global texture.
        /// </summary>
        /// <param name="name">The name of the texture in the original compositor definition</param>
        /// <param name="mrtIndex">If name identifies a MRT, which texture attachment to retrieve</param>
        /// <returns>The texture pointer, corresponds to a real texture</returns>
        public Texture GetTextureInstance( string name, int mrtIndex )
        {
            //Try simple texture
            Texture ret = null;
            if ( !_globalTextures.TryGetValue( name, out ret ) )
            {
                //Try MRT
                string mrtName = GetMRTLocalName( name, mrtIndex );
                if ( !_globalTextures.TryGetValue( name, out ret ) )
                {
                    throw new AxiomException( "Non-existent global texture name, " +
                                              "Compositor.GetTextureInstance", new Object
                                                                               {
                                                                               } );
                }
            }
            return ret;
        }

        /// <summary>
        /// Get's the render target for a given render texture name.
        /// </summary>
        /// <param name="name">name of the texture</param>
        /// <returns>rendertarget</returns>
        /// <remarks>
        /// You can use this to add listeners etc, but do not use it to update the
        /// targets manually or any other modifications, the compositor instance
        /// is in charge of this.
        /// </remarks>
        public RenderTarget GetRenderTarget( string name )
        {
            //Try simple texture
            Texture ret = null;
            if ( !_globalTextures.TryGetValue( name, out ret ) )
            {
                //Try MRT
                MultiRenderTarget mrt = null;
                if ( _globalMRTs.TryGetValue( name, out mrt ) )
                {
                    return mrt;
                }
                else
                {
                    throw new AxiomException( "Non-existent global texture name, " +
                                              "Compositor.GetTextureInstance", new Object
                                                                               {
                                                                               } );
                }
            }
            return ret.GetBuffer().GetRenderTarget();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseName"></param>
        /// <param name="attachment"></param>
        /// <returns></returns>
        public string GetMRTLocalName( string baseName, int attachment )
        {
            return baseName + "/" + attachment.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void load()
        {
            if ( _compilationRequired )
            {
                Compile();
            }

            CreateGlobalTextures();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void unload()
        {
            FreeGlobalTextures();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override int calculateSize()
        {
            return 0;
        }

        /// <summary>
        /// Check supportedness of techniques.
        /// </summary>
        protected void Compile()
        {
            // Sift out supported techniques
            _supportedTechniques.Clear();
            // Try looking for exact technique support with no texture fallback
            foreach ( CompositionTechnique t in _techniques )
            {
                if ( t.IsSupported( false ) )
                {
                    _supportedTechniques.Add( t );
                }
            }
            if ( _supportedTechniques.Count == 0 )
            {
                // Check again, being more lenient with textures
                foreach ( CompositionTechnique t in _techniques )
                {
                    if ( t.IsSupported( true ) )
                    {
                        _supportedTechniques.Add( t );
                    }
                }
            }

            _compilationRequired = false;
        }

        /// <summary>
        /// Create global rendertextures.
        /// </summary>
        private void CreateGlobalTextures()
        {
            if ( _supportedTechniques.Count == 0 )
            {
                return;
            }

            //To make sure that we are consistent, it is demanded that all composition
            //techniques define the same set of global textures.
            List<string> globalTextureNames = new List<string>();

            //Initialize global textures from first supported technique
            CompositionTechnique firstTechnique = _supportedTechniques[ 0 ];

            foreach ( CompositionTechnique.TextureDefinition def in firstTechnique.TextureDefinitions )
            {
                if ( def.Scope == CompositionTechnique.TextureScope.Global )
                {
                    //Check that this is a legit global texture
                    if ( !( def.ReferenceCompositorName == string.Empty ) )
                    {
                        throw new AxiomException( "Global compositor texture definition can not be a reference." );
                    }
                    if ( def.Width == 0 || def.Height == 0 )
                    {
                        throw new AxiomException( "Global compositor texture definition must have absolute size." );
                    }
                    if ( def.Pooled )
                    {
                        LogManager.Instance.Write( "Pooling global compositor textures has no effect", null );
                    }
                    globalTextureNames.Add( def.Name );

                    //TODO GSOC : Heavy copy-pasting from CompositorInstance. How to we solve it?
                    // Make the tetxure
                    RenderTarget renderTarget = null;
                    if ( def.FormatList.Count > 1 )
                    {
                        string MRTBaseName = "c" + _dummyCounter++.ToString() + "/" + _name + "/" + def.Name;
                        MultiRenderTarget mrt =
                            Root.Instance.RenderSystem.CreateMultiRenderTarget( MRTBaseName );
                        _globalMRTs.Add( def.Name, mrt );

                        // create and bind individual surfaces
                        int atch = 0;
                        foreach ( PixelFormat p in def.FormatList )
                        {
#warning TextureManager.CreateManual does not accept params like "FSAA" or HWGammWrite, add this!
                            string texName = MRTBaseName + "/" + atch.ToString();
                            Texture tex =
                                TextureManager.Instance.CreateManual( texName, ResourceGroupManager.InternalResourceGroupName,
                                                                      TextureType.TwoD, def.Width, def.Height, 0, p, TextureUsage.RenderTarget, null );

                            RenderTexture rt = tex.GetBuffer().GetRenderTarget();
                            rt.IsAutoUpdated = false;
                            mrt.BindSurface( atch, rt );
                            // Also add to local textures so we can look up
                            string mrtLocalName = GetMRTLocalName( def.Name, atch );
                            _globalTextures.Add( mrtLocalName, tex );
                        }
                        renderTarget = mrt;
                    }
                    else
                    {
                        string texName = "c" + _dummyCounter++.ToString() + "/" + _name + "/" + def.Name;
                        // space in the name mixup the cegui in the compositor demo
                        // this is an auto generated name - so no spaces can't hart us.
                        texName = texName.Replace( " ", "_" );
                        Texture tex =
                            TextureManager.Instance.CreateManual( texName, ResourceGroupManager.InternalResourceGroupName,
                                                                  TextureType.TwoD, def.Width, def.Height, 0, def.FormatList[ 0 ], TextureUsage.RenderTarget, null );

                        renderTarget = tex.GetBuffer().GetRenderTarget();
                        _globalTextures.Add( def.Name, tex );
                    }
                    //Set DepthBuffer pool for sharing
#warning rendTarget->setDepthBufferPool( def->depthBufferId );
                }
            } //end foreach

            //Validate that all other supported techniques expose the same set of global textures.
            for ( int i = 0; i < _supportedTechniques.Count; i++ )
            {
                CompositionTechnique technique = _supportedTechniques[ i ];
                bool isConsistent = true;
                int numGlobals = 0;
                foreach ( CompositionTechnique.TextureDefinition texDef in technique.TextureDefinitions )
                {
                    if ( texDef.Scope == CompositionTechnique.TextureScope.Global )
                    {
                        if ( !globalTextureNames.Contains( texDef.Name ) )
                        {
                            isConsistent = false;
                            break;
                        }
                        numGlobals++;
                    }
                }
                if ( numGlobals != globalTextureNames.Count )
                {
                    isConsistent = false;
                }
                if ( !isConsistent )
                {
                    throw new AxiomException( "Different composition techniques define different global textures." );
                }
            }
        }

        /// <summary>
        /// Destroy global rendertextures.
        /// </summary>
        private void FreeGlobalTextures()
        {
            foreach ( Texture tex in _globalTextures.Values )
            {
                TextureManager.Instance.Remove( tex.Name );
            }

            _globalTextures.Clear();

            foreach ( MultiRenderTarget mrt in _globalMRTs.Values )
            {
                Root.Instance.RenderSystem.DestroyRenderTarget( mrt.Name );
            }

            _globalMRTs.Clear();
        }
    }
}