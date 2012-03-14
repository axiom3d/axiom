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
/*
 * Many thanks to the folks at Multiverse for providing the initial port for this class
 */

#endregion LGPL License

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Media;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	///<summary>
	///    Class representing a Compositor object. Compositors provide the means
	///    to flexibly "composite" the final rendering result from multiple scene renders
	///    and intermediate operations like rendering fullscreen quads. This makes
	///    it possible to apply postfilter effects, HDRI postprocessing, and shadow
	///    effects to a Viewport.
	///</summary>
	public class Compositor : Resource
	{
		#region Fields and Properties

		/// <summary>
		///    Auto incrementing number for creating unique names.
		/// </summary>
		protected static int autoNumber;

		private readonly ReadOnlyCollection<CompositionTechnique> readOnlySupportedTechniques;
		private readonly ReadOnlyCollection<CompositionTechnique> readOnlyTechniques;

		///<summary>
		///     This is set if the techniques change and the supportedness of techniques has to be
		///     re-evaluated.
		///</summary>
		protected bool compilationRequired;

		/// <summary>
		/// Store a list of MRTs we've created
		/// </summary>
		protected Dictionary<string, MultiRenderTarget> globalMRTs;

		/// <summary>
		/// Store a list of textures we've created
		/// </summary>
		protected Dictionary<string, Texture> globalTextures;

		protected List<CompositionTechnique> supportedTechniques;
		protected List<CompositionTechnique> techniques;

		/// <summary>
		/// List of all techniques
		/// </summary>
		public IList<CompositionTechnique> Techniques
		{
			get
			{
				return this.readOnlyTechniques;
			}
		}

		/// <summary>
		/// List of supported techniques
		/// </summary>
		///<remarks>
		///    The supported technique list is only available after this compositor has been compiled,
		///    which typically happens on loading it. Therefore, if this method returns
		///    an empty list, try calling <see>Compositor.Load</see>.
		///</remarks>
		public IList<CompositionTechnique> SupportedTechniques
		{
			get
			{
				return this.readOnlySupportedTechniques;
			}
		}

		#endregion Fields and Properties

		#region Constructors

		public Compositor( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			this.techniques = new List<CompositionTechnique>();
			this.readOnlyTechniques = new ReadOnlyCollection<CompositionTechnique>( this.techniques );
			this.supportedTechniques = new List<CompositionTechnique>();
			this.readOnlySupportedTechniques = new ReadOnlyCollection<CompositionTechnique>( this.supportedTechniques );
			this.globalTextures = new Dictionary<string, Texture>();
			this.globalMRTs = new Dictionary<string, MultiRenderTarget>();
			this.compilationRequired = true;
		}

		#endregion Constructors

		#region Implementation of Resource

		/// <summary>
		///		Overridden from Resource.
		/// </summary>
		/// <remarks>
		///		By default, Materials are not loaded, and adding additional textures etc do not cause those
		///		textures to be loaded. When the <code>Load</code> method is called, all textures are loaded (if they
		///		are not already), GPU programs are created if applicable, and Controllers are instantiated.
		///		Once a material has been loaded, all changes made to it are immediately loaded too
		/// </remarks>
		protected override void load()
		{
			if ( !IsLoaded )
			{
				// compile if needed
				if ( this.compilationRequired )
				{
					Compile();
				}

				CreateGlobalTextures();
			}
		}

		/// <summary>
		///		Unloads the material, frees resources etc.
		///		<see cref="Resource"/>
		/// </summary>
		protected override void unload()
		{
			FreeGlobalTextures();
		}

		/// <summary>
		///	    Disposes of any resources used by this object.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					RemoveAllTechniques();

					foreach ( var item in this.globalMRTs )
					{
						item.Value.Dispose();
					}
					this.globalMRTs.Clear();

					foreach ( var item in this.globalTextures )
					{
						item.Value.Dispose();
					}
					return;
					unload();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		/// <summary>
		///    Overridden to ensure a recompile occurs if needed before use.
		/// </summary>
		public override void Touch()
		{
			if ( this.compilationRequired )
			{
				Compile();
			}

			// call base class
			base.Touch();
		}

		#endregion Implementation of Resource

		#region Methods

		///<summary>
		/// Create a new technique, and return a pointer to it.
		///</summary>
		public CompositionTechnique CreateTechnique()
		{
			var t = new CompositionTechnique( this );
			this.techniques.Add( t );
			this.compilationRequired = true;
			return t;
		}

		///<summary>
		/// Remove a technique.
		///</summary>
		public void RemoveTechnique( int idx )
		{
			Debug.Assert( idx < this.techniques.Count, "Index out of bounds." );
			this.techniques[ idx ].Dispose();
			this.techniques[ idx ] = null;
			this.techniques.RemoveAt( idx );
			this.supportedTechniques.Clear();
			this.compilationRequired = true;
		}

		///<summary>
		///    Remove all techniques.
		///</summary>
		public void RemoveAllTechniques()
		{
			//for (int i = 0; i < techniques.Count; i++)
			//{
			//    techniques[i].Dispose();
			//    techniques[i] = null;
			//}
			this.techniques.Clear();
			this.supportedTechniques.Clear();
			this.compilationRequired = true;
		}

		/// <summary>
		/// Get a reference to a supported technique for a given scheme.
		/// </summary>
		/// <returns>the first supported technique with no specific scheme will be returned.</returns>
		public CompositionTechnique GetSupportedTechniqueByScheme()
		{
			return GetSupportedTechniqueByScheme( string.Empty );
		}

		/// <summary>
		/// Get a reference to a supported technique for a given scheme.
		/// </summary>
		/// <param name="schemeName"> The scheme name you are looking for.
		/// Blank means to look for techniques with no scheme associated
		/// </param>
		/// <returns></returns>
		/// <remarks>
		/// If there is no specific supported technique with this scheme name,
		/// then the first supported technique with no specific scheme will be returned.
		/// </remarks>
		public CompositionTechnique GetSupportedTechniqueByScheme( string schemeName )
		{
			foreach ( CompositionTechnique t in this.supportedTechniques )
			{
				if ( t.SchemeName == schemeName )
				{
					return t;
				}
			}
			// didn't find a matching one
			foreach ( CompositionTechnique t in this.supportedTechniques )
			{
				if ( String.IsNullOrEmpty( t.SchemeName ) )
				{
					return t;
				}
			}
			return null;
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
			if ( this.globalTextures.TryGetValue( name, out ret ) )
			{
				return ret;
			}

			//Try MRT
			string mrtName = GetMRTLocalName( name, mrtIndex );
			if ( !this.globalTextures.TryGetValue( name, out ret ) )
			{
				return ret;
			}

			throw new AxiomException( "Non-existent global texture name." );
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
			if ( this.globalTextures.TryGetValue( name, out ret ) )
			{
				return ret.GetBuffer().GetRenderTarget();
			}

			//Try MRT
			MultiRenderTarget mrt = null;
			if ( this.globalMRTs.TryGetValue( name, out mrt ) )
			{
				return mrt;
			}

			throw new AxiomException( "Non-existent global texture name." );
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

		///<summary>
		///    Check supportedness of techniques.
		///</summary>
		protected void Compile()
		{
			// Sift out supported techniques
			this.supportedTechniques.Clear();
			// Try looking for exact technique support with no texture fallback
			foreach ( CompositionTechnique t in this.techniques )
			{
				// Look for exact texture support first
				if ( t.IsSupported( false ) )
				{
					this.supportedTechniques.Add( t );
				}
			}

			if ( this.supportedTechniques.Count == 0 )
			{
				// Check again, being more lenient with textures
				foreach ( CompositionTechnique t in this.techniques )
				{
					// Allow texture support with degraded pixel format
					if ( t.IsSupported( true ) )
					{
						this.supportedTechniques.Add( t );
					}
				}
			}
			this.compilationRequired = false;
		}

		/// <summary>
		/// Create global rendertextures.
		/// </summary>
		private void CreateGlobalTextures()
		{
			if ( this.supportedTechniques.Count == 0 )
			{
				return;
			}

			//To make sure that we are consistent, it is demanded that all composition
			//techniques define the same set of global textures.
			var globalTextureNames = new List<string>();

			//Initialize global textures from first supported technique
			CompositionTechnique firstTechnique = this.supportedTechniques[ 0 ];

			foreach ( CompositionTechnique.TextureDefinition def in firstTechnique.TextureDefinitions )
			{
				if ( def.Scope == CompositionTechnique.TextureScope.Global )
				{
					//Check that this is a legit global texture
					if ( !string.IsNullOrEmpty( def.ReferenceCompositorName ) )
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
					if ( def.PixelFormats.Count > 1 )
					{
						string MRTBaseName = string.Format( "c{0}/{1}/{2}", autoNumber++.ToString(), _name, def.Name );
						MultiRenderTarget mrt = Root.Instance.RenderSystem.CreateMultiRenderTarget( MRTBaseName );
						this.globalMRTs.Add( def.Name, mrt );

						// create and bind individual surfaces
						int atch = 0;
						foreach ( PixelFormat p in def.PixelFormats )
						{
							string texName = string.Format( "{0}/{1}", MRTBaseName, atch.ToString() );
							Texture tex = TextureManager.Instance.CreateManual( texName, ResourceGroupManager.InternalResourceGroupName, TextureType.TwoD, def.Width, def.Height, 0, 0, p, TextureUsage.RenderTarget, null, def.HwGammaWrite && !PixelUtil.IsFloatingPoint( p ), def.Fsaa ? 1 : 0 );

							RenderTexture rt = tex.GetBuffer().GetRenderTarget();
							rt.IsAutoUpdated = false;
							mrt.BindSurface( atch, rt );
							// Also add to local textures so we can look up
							string mrtLocalName = GetMRTLocalName( def.Name, atch );
							this.globalTextures.Add( mrtLocalName, tex );
						}
						renderTarget = mrt;
					}
					else
					{
						string texName = "c" + autoNumber++.ToString() + "/" + _name + "/" + def.Name;
						// space in the name mixup the cegui in the compositor demo
						// this is an auto generated name - so no spaces can't hart us.
						texName = texName.Replace( " ", "_" );
						Texture tex = TextureManager.Instance.CreateManual( texName, ResourceGroupManager.InternalResourceGroupName, TextureType.TwoD, def.Width, def.Height, 0, def.PixelFormats[ 0 ], TextureUsage.RenderTarget, null, def.HwGammaWrite && !PixelUtil.IsFloatingPoint( def.PixelFormats[ 0 ] ), def.Fsaa ? 1 : 0 );

						renderTarget = tex.GetBuffer().GetRenderTarget();
						this.globalTextures.Add( def.Name, tex );
					}

					//Set DepthBuffer pool for sharing
					renderTarget.DepthBufferPool = def.DepthBufferId;
				}
			}

			//Validate that all other supported techniques expose the same set of global textures.
			foreach ( CompositionTechnique technique in this.supportedTechniques )
			{
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
			foreach ( Texture tex in this.globalTextures.Values )
			{
				TextureManager.Instance.Remove( tex.Name );
			}
			this.globalTextures.Clear();

			foreach ( MultiRenderTarget mrt in this.globalMRTs.Values )
			{
				Root.Instance.RenderSystem.DestroyRenderTarget( mrt.Name );
			}
			this.globalMRTs.Clear();
		}

		#endregion Methods
	}
}
