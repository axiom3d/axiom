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

using System.Collections.Generic;
using System.IO;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Media;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	///<summary>
	///  Class for managing Compositor settings for Ogre. Compositors provide the means to flexibly "composite" the final rendering result from multiple scene renders and intermediate operations like rendering fullscreen quads. This makes it possible to apply postfilter effects, HDRI postprocessing, and shadow effects to a Viewport. When loaded from a script, a Compositor is in an 'unloaded' state and only stores the settings required. It does not at that stage load any textures. This is because the material settings may be loaded 'en masse' from bulk material script files, but only a subset will actually be required. Because this is a subclass of ResourceManager, any files loaded will be searched for in any path or archive added to the resource paths/archives. See ResourceManager for details.
	///</summary>
	public class CompositorManager : ResourceManager, ISingleton<CompositorManager>
	{
		#region ISingleton<CompositorManager> Implementation

		/// <summary>
		///   Gets the singleton instance of this class.
		/// </summary>
		public static CompositorManager Instance
		{
			get
			{
				return Singleton<CompositorManager>.Instance;
			}
		}

		/// <summary>
		///   Initializes the Compositor Manager
		/// </summary>
		/// <param name="args"> </param>
		/// <returns> </returns>
		public bool Initialize( params object[] args )
		{
			var scene = (Compositor)Create( "Axiom/Scene", ResourceGroupManager.InternalResourceGroupName );
			var t = scene.CreateTechnique();

			var tp = t.OutputTarget;
			tp.VisibilityMask = 0xFFFFFFFF;
			{
				var pass = tp.CreatePass();
				pass.Type = CompositorPassType.Clear;
			}
			{
				var pass = tp.CreatePass();
				pass.Type = CompositorPassType.RenderScene;
				// Render everything, including skies
				pass.FirstRenderQueue = RenderQueueGroupID.Background;
				pass.LastRenderQueue = RenderQueueGroupID.SkiesLate;
			}
			chains = new Dictionary<Viewport, CompositorChain>();

			return true;
		}

		#endregion ISingleton<CompositorManager> Implementation

		#region Construction and Destruction

		/// <summary>
		///   Internal constructor. This class cannot be instantiated externally.
		/// </summary>
		public CompositorManager()
			: base()
		{
			// OGRE initializes this manager here in the constructor. For consistency Axiom calls Initialize() directly
			// in Root.ctor(), this does change the order in which things are initialized.
			//Initialize();

			customCompositionPassesIndex = new ReadOnlyDictionary<string, ICustomCompositionPass>( customCompositionPasses );
			compositorLogicIndex = new ReadOnlyDictionary<string, ICompositorLogic>( compositorLogics );
			//Load right after materials
			LoadingOrder = 110.0f;

			ScriptPatterns.Add( "*.compositor" );
			ResourceGroupManager.Instance.RegisterScriptLoader( this );

			ResourceType = "Compositor";
			ResourceGroupManager.Instance.RegisterResourceManager( ResourceType, this );
		}

		#endregion Construction and Destruction

		#region TextureDefinition

		///<summary>
		///</summary>
		public struct TextureDefinition
		{
			///<summary>
			///</summary>
			public int Width;

			///<summary>
			///</summary>
			public int Height;

			///<summary>
			///</summary>
			public PixelFormat Format;

			///<summary>
			///</summary>
			public int FSAA;

			///<summary>
			///</summary>
			public string FSAAHint;

			///<summary>
			///</summary>
			public bool SRGBWrite;

			///<summary>
			///</summary>
			///<param name="width"> </param>
			///<param name="height"> </param>
			///<param name="format"> </param>
			///<param name="aa"> </param>
			///<param name="aaHint"> </param>
			///<param name="srgb"> </param>
			public TextureDefinition( int width, int height, PixelFormat format, int aa, string aaHint, bool srgb )
			{
				Width = width;
				Height = height;
				Format = format;
				FSAA = aa;
				FSAAHint = aaHint;
				SRGBWrite = srgb;
			}
		}

		///<summary>
		///</summary>
		public struct TextureDefLess : IComparer<TextureDefinition>
		{
			public int Compare( TextureDefinition x, TextureDefinition y )
			{
				if ( x.Format < y.Format )
				{
					return -1;
				}
				else if ( x.Format == y.Format )
				{
					if ( x.Width < y.Width )
					{
						return -1;
					}
					else if ( x.Width == y.Width )
					{
						if ( x.FSAA < y.FSAA )
						{
							return -1;
						}
						else if ( x.FSAA == y.FSAA )
						{
							if ( x.FSAAHint != y.FSAAHint )
							{
								return -1;
							}
							else if ( !x.SRGBWrite && y.SRGBWrite )
							{
								return -1;
							}
						}
					}
				}
				return 1;
			}

			//public static bool operator!=(TextureDef x, TextureDef y)
			//{
			//}
		}

		#endregion TextureDefinition

		#region Fields and Properties

		///<summary>
		///  Mapping from viewport to compositor chain
		///</summary>
		protected Dictionary<Viewport, CompositorChain> chains;

		///<summary>
		///</summary>
		private readonly Dictionary<TextureDefinition, List<Texture>> texturesByDef =
			new Dictionary<TextureDefinition, List<Texture>>();

		///<summary>
		///</summary>
		private readonly Dictionary<Pair<string>, SortedList<TextureDefinition, Texture>> chainTexturesByRef =
			new Dictionary<Pair<string>, SortedList<TextureDefinition, Texture>>();

		///<summary>
		///</summary>
		protected Rectangle2D rectangle = null;

		/// <summary>
		///   List of registered compositor logics
		/// </summary>
		private readonly Dictionary<string, ICompositorLogic> compositorLogics = new Dictionary<string, ICompositorLogic>();

		private readonly ReadOnlyDictionary<string, ICompositorLogic> compositorLogicIndex;

		/// <summary>
		///   List of registered compositor logics
		/// </summary>
		public ReadOnlyDictionary<string, ICompositorLogic> CompositorLogics
		{
			get
			{
				return compositorLogicIndex;
			}
		}

		/// <summary>
		///   List of registered custom composition passes
		/// </summary>
		private readonly Dictionary<string, ICustomCompositionPass> customCompositionPasses =
			new Dictionary<string, ICustomCompositionPass>();

		private readonly ReadOnlyDictionary<string, ICustomCompositionPass> customCompositionPassesIndex;

		/// <summary>
		///   List of registered custom composition passes
		/// </summary>
		public ReadOnlyDictionary<string, ICustomCompositionPass> CustomCompositionPasses
		{
			get
			{
				return customCompositionPassesIndex;
			}
		}

		///<summary>
		///  Get a textured fullscreen 2D rectangle, for internal use.
		///</summary>
		internal IRenderable TexturedRectangle2D
		{
			get
			{
				if ( rectangle == null )
				{
					rectangle = new Rectangle2D( true );
				}

				var rs = Root.Instance.RenderSystem;
				var vp = rs.Viewport;
				float hOffset = rs.HorizontalTexelOffset/( 0.5f*vp.ActualWidth );
				float vOffset = rs.VerticalTexelOffset/( 0.5f*vp.ActualHeight );
				rectangle.SetCorners( -1f + hOffset, 1f - vOffset, 1f + hOffset, -1f - vOffset );
				return rectangle;
			}
		}

		#endregion Fields and Properties

		#region Methods

		///<summary>
		///  Get the compositor chain for a Viewport. If there is none yet, a new compositor chain is registered. XXX We need a _notifyViewportRemoved to find out when this viewport disappears, so we can destroy its chain as well.
		///</summary>
		public CompositorChain GetCompositorChain( Viewport vp )
		{
			CompositorChain chain;
			if ( chains.TryGetValue( vp, out chain ) )
			{
				// Make sure we have the right viewport
				// It's possible that this chain may have outlived a viewport and another
				// viewport was created at the same physical address, meaning we find it again but the viewport is gone
				chain.Viewport = vp;
			}
			else
			{
				chain = new CompositorChain( vp );
				chains[ vp ] = chain;
			}
			return chain;
		}

		///<summary>
		///  Returns whether exists compositor chain for a viewport.
		///</summary>
		public bool HasCompositorChain( Viewport vp )
		{
			return chains.ContainsKey( vp );
		}

		///<summary>
		///  Remove the compositor chain from a viewport if exists.
		///</summary>
		public void RemoveCompositorChain( Viewport vp )
		{
			chains.Remove( vp );
		}

		///<summary>
		///  Overridden from ResourceManager since we have to clean up chains too.
		///</summary>
		public override void RemoveAll()
		{
			FreeChains();
			base.RemoveAll();
		}

		///<summary>
		///  Clear composition chains for all viewports
		///</summary>
		protected void FreeChains()
		{
			//foreach (var item in chains.Values)
			//{
			//    item.Dispose();
			//}

			chains.Clear();
		}

		///<summary>
		///  Add a compositor to a viewport. By default, it is added to end of the chain, after the other compositors.
		///</summary>
		///<param name="vp"> Viewport to modify </param>
		///<param name="compositor"> The name of the compositor to apply </param>
		///<param name="addPosition"> At which position to add, defaults to the end (-1). </param>
		///<returns> pointer to instance, or null if it failed. </returns>
		public CompositorInstance AddCompositor( Viewport vp, string compositor, int addPosition )
		{
			var comp = (Compositor)this[ compositor ];
			if ( comp == null )
			{
				return null;
			}
			var chain = GetCompositorChain( vp );
			return chain.AddCompositor( comp, addPosition == -1 ? CompositorChain.LastCompositor : addPosition );
		}

		public CompositorInstance AddCompositor( Viewport vp, string compositor )
		{
			return AddCompositor( vp, compositor, -1 );
		}

		///<summary>
		///  Remove a compositor from a viewport
		///</summary>
		public void RemoveCompositor( Viewport vp, string compositor )
		{
			var chain = GetCompositorChain( vp );
			for ( var i = 0; i < chain.Instances.Count; i++ )
			{
				var instance = chain.GetCompositor( i );
				if ( instance.Compositor.Name == compositor )
				{
					chain.RemoveCompositor( i );
					break;
				}
			}
		}

		/// <summary>
		///   another overload to remove a compositor instance from its chain
		/// </summary>
		/// <param name="remInstance"> </param>
		public void RemoveCompositor( CompositorInstance remInstance )
		{
			var chain = remInstance.Chain;

			for ( var i = 0; i < chain.Instances.Count; i++ )
			{
				var instance = chain.GetCompositor( i );
				if ( instance == remInstance )
				{
					chain.RemoveCompositor( i );
					break;
				}
			}
		}

		///<summary>
		///  Set the state of a compositor on a viewport to enabled or disabled. Disabling a compositor stops it from rendering but does not free any resources. This can be more efficient than using removeCompositor and addCompositor in cases the filter is switched on and off a lot.
		///</summary>
		public void SetCompositorEnabled( Viewport vp, string compositor, bool value )
		{
			var chain = GetCompositorChain( vp );
			for ( var i = 0; i < chain.Instances.Count; i++ )
			{
				var instance = chain.GetCompositor( i );
				if ( instance.Compositor.Name == compositor )
				{
					chain.SetCompositorEnabled( i, value );
					break;
				}
			}
		}

		public void RegisterCompositorLogic( string name, ICompositorLogic compositorLogic )
		{
			if ( string.IsNullOrEmpty( name ) )
			{
				throw new AxiomException( "Compositor logic name must not be empty." );
			}
			if ( compositorLogics.ContainsKey( name ) )
			{
				throw new AxiomException( "Compositor logic '" + name + "' already exists." );
			}

			compositorLogics.Add( name, compositorLogic );
		}

		public void RegisterCustomCompositorPass( string name, ICustomCompositionPass compositionPass )
		{
			if ( string.IsNullOrEmpty( name ) )
			{
				throw new AxiomException( "Pass name must not be empty." );
			}
			if ( compositorLogics.ContainsKey( name ) )
			{
				throw new AxiomException( "Pass '" + name + "' already exists." );
			}

			customCompositionPasses.Add( name, compositionPass );
		}

		#region Pooled Textures

		///<summary>
		///</summary>
		///<param name="name"> </param>
		///<param name="localName"> </param>
		///<param name="width"> </param>
		///<param name="height"> </param>
		///<param name="format"> </param>
		///<param name="aa"> </param>
		///<param name="aaHint"> </param>
		///<param name="srgb"> </param>
		///<param name="textureAllreadyAssigned"> </param>
		///<param name="instance"> </param>
		///<param name="scope"> </param>
		///<returns> </returns>
		public Texture GetPooledTexture( string name, string localName, int width, int height, PixelFormat format, int aa,
		                                 string aaHint, bool srgb, List<Texture> textureAllreadyAssigned,
		                                 CompositorInstance instance, CompositionTechnique.TextureScope scope )
		{
			if ( scope == CompositionTechnique.TextureScope.Global )
			{
				throw new AxiomException( "Global scope texture can not be pooled." );
			}

			var def = new TextureDefinition( width, height, format, aa, aaHint, srgb );
			if ( scope == CompositionTechnique.TextureScope.Chain )
			{
				var pair = new Pair<string>( instance.Compositor.Name, localName );
				SortedList<TextureDefinition, Texture> defMap = null;
				if ( chainTexturesByRef.TryGetValue( pair, out defMap ) )
				{
					Texture tex;
					if ( defMap.TryGetValue( def, out tex ) )
					{
						return tex;
					}
				}
				// ok, we need to create a new one
				if ( defMap == null )
				{
					defMap = new SortedList<TextureDefinition, Texture>( new TextureDefLess() );
				}

				var newTex = TextureManager.Instance.CreateManual( name, ResourceGroupManager.InternalResourceGroupName,
				                                                   TextureType.TwoD, width, height, 0, format,
				                                                   TextureUsage.RenderTarget, null, srgb, aa, aaHint );

				defMap.Add( def, newTex );

				if ( chainTexturesByRef.ContainsKey( pair ) )
				{
					chainTexturesByRef[ pair ] = defMap;
				}
				else
				{
					chainTexturesByRef.Add( pair, defMap );
				}

				return newTex;
			} //end if scope

			List<Texture> i = null;
			if ( !texturesByDef.TryGetValue( def, out i ) )
			{
				i = new List<Texture>();
				texturesByDef.Add( def, i );
			}

			var previous = instance.Chain.GetPreviousInstance( instance );
			var next = instance.Chain.GetNextInstance( instance );

			Texture ret = null;
			// iterate over the existing textures and check if we can re-use
			foreach ( var tex in i )
			{
				// check not already used
				if ( !textureAllreadyAssigned.Contains( tex ) )
				{
					var allowReuse = true;
					// ok, we didn't use this one already
					// however, there is an edge case where if we re-use a texture
					// which has an 'input previous' pass, and it is chained from another
					// compositor, we can end up trying to use the same texture for both
					// so, never allow a texture with an input previous pass to be
					// shared with its immediate predecessor in the chain
					if ( IsInputPreviousTarget( instance, localName ) )
					{
						// Check whether this is also an input to the output target of previous
						// can't use CompositorInstance._previousInstance, only set up
						// during compile
						if ( previous != null && IsInputToOutputTarget( previous, tex ) )
						{
							allowReuse = false;
						}
					}
					// now check the other way around since we don't know what order they're bound in
					if ( IsInputToOutputTarget( instance, localName ) )
					{
						if ( next != null && IsInputPreviousTarget( next, tex ) )
						{
							allowReuse = false;
						}
					}
					if ( allowReuse )
					{
						ret = tex;
						break;
					}
				}
			}

			if ( ret == null )
			{
				// ok, we need to create a new one
				ret = TextureManager.Instance.CreateManual( name, ResourceGroupManager.InternalResourceGroupName, TextureType.TwoD,
				                                            width, height, 0, format, TextureUsage.RenderTarget, null, srgb, aa,
				                                            aaHint );
				i.Add( ret );
				texturesByDef[ def ] = i;
			}

			// record that we used this one in the requester's list
			textureAllreadyAssigned.Add( ret );

			return ret;
		}

		///<summary>
		///</summary>
		public void FreePooledTextures()
		{
			FreePooledTextures( true );
		}

		///<summary>
		///</summary>
		///<param name="onlyIfUnreferenced"> </param>
		public void FreePooledTextures( bool onlyIfUnreferenced )
		{
			if ( onlyIfUnreferenced )
			{
				foreach ( var i in texturesByDef )
				{
					var texList = i.Value;
					// if the resource system, plus this class, are the only ones to have a reference..
					// NOTE: any material references will stop this texture getting freed (e.g. compositor demo)
					// until this routine is called again after the material no longer references the texture
					for ( var j = 0; j < texList.Count; j++ )
					{
						if ( texList[ j ].UseCount == ResourceGroupManager.ResourceSystemNumReferenceCount + 1 )
						{
							TextureManager.Instance.Remove( texList[ j ].Handle );
							texList.Remove( texList[ j ] );
						}
					}
				}
				foreach ( var i in chainTexturesByRef )
				{
					var texMap = i.Value;
					foreach ( var j in texMap )
					{
						var tex = j.Value;
						if ( tex.UseCount == ResourceGroupManager.ResourceSystemNumReferenceCount + 1 )
						{
							TextureManager.Instance.Remove( tex.Handle );
							texMap.Remove( j.Key );
						}
					}
				}
			}
			else
			{
				// destroy all
				foreach ( var i in texturesByDef )
				{
					var texList = i.Value;
					for ( var j = 0; j < texList.Count; j++ )
					{
						TextureManager.Instance.Remove( texList[ 0 ].Handle );
						texList.Remove( texList[ 0 ] );
					}
				}
				texturesByDef.Clear();
				chainTexturesByRef.Clear();
			}
		}

		///<summary>
		///</summary>
		///<param name="instance"> </param>
		///<param name="localName"> </param>
		///<returns> </returns>
		private bool IsInputPreviousTarget( CompositorInstance instance, string localName )
		{
			foreach ( var tp in instance.Technique.TargetPasses )
			{
				if ( tp.InputMode == CompositorInputMode.Previous && tp.OutputName == localName )
				{
					return true;
				}
			}
			return false;
		}

		///<summary>
		///</summary>
		///<param name="instance"> </param>
		///<param name="texture"> </param>
		///<returns> </returns>
		private bool IsInputPreviousTarget( CompositorInstance instance, Texture texture )
		{
			foreach ( var tp in instance.Technique.TargetPasses )
			{
				if ( tp.InputMode == CompositorInputMode.Previous )
				{
					// Don't have to worry about an MRT, because no MRT can be input previous
					var t = instance.GetTextureInstance( tp.OutputName, 0 );
					if ( t != null && t == texture )
					{
						return true;
					}
				}
			}
			return false;
		}

		///<summary>
		///</summary>
		///<param name="instance"> </param>
		///<param name="localName"> </param>
		///<returns> </returns>
		private bool IsInputToOutputTarget( CompositorInstance instance, string localName )
		{
			var tp = instance.Technique.OutputTarget;
			foreach ( var p in tp.Passes )
			{
				for ( var i = 0; i < p.Inputs.Length; i++ )
				{
					if ( p.GetInput( i ).Name == localName )
					{
						return true;
					}
				}
			}
			return false;
		}

		///<summary>
		///</summary>
		///<param name="instance"> </param>
		///<param name="texture"> </param>
		///<returns> </returns>
		private bool IsInputToOutputTarget( CompositorInstance instance, Texture texture )
		{
			var tp = instance.Technique.OutputTarget;
			foreach ( var p in tp.Passes )
			{
				for ( var i = 0; i < p.Inputs.Length; i++ )
				{
					var t = instance.GetTextureInstance( p.GetInput( i ).Name, 0 );
					if ( t != null && t == texture )
					{
						return true;
					}
				}
			}
			return false;
		}

		#endregion Pooled Textures

		#endregion Methods

		#region ResourceManager Implementation

		protected override Resource _create( string name, ResourceHandle handle, string group, bool isManual,
		                                     IManualResourceLoader loader, Axiom.Collections.NameValuePairList createParams )
		{
			return new Compositor( this, name, handle, group, isManual, loader );
		}

		///<summary>
		///  Starts parsing an individual script file.
		///</summary>
		public override void ParseScript( Stream data, string groupName, string fileName )
		{
#if AXIOM_USENEWCOMPILERS
            Axiom.Scripting.Compiler.ScriptCompilerManager.Instance.ParseScript( data, groupName, fileName );
#else
			CompositorScriptLoader.ParseScript( this, data, groupName, fileName );
#endif
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					foreach ( var item in chains )
					{
						item.Value.RemoveAllCompositors();
					}

					if ( ResourceGroupManager.Instance != null )
					{
						ResourceGroupManager.Instance.UnregisterScriptLoader( this );
						ResourceGroupManager.Instance.UnregisterResourceManager( ResourceType );
					}
					FreeChains();
					FreePooledTextures( false );
					Singleton<CompositorManager>.Destroy();

					return;
					rectangle.Dispose();
					rectangle = null;
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion ResourceManager Implementation

		public void ReconstructAllCompositorResources()
		{
			var instancesToReenable = new List<CompositorInstance>();
			foreach ( var i in chains )
			{
				var chain = i.Value;
				foreach ( var inst in chain.Instances )
				{
					if ( !inst.IsEnabled )
					{
						continue;
					}
					inst.IsEnabled = false;
					instancesToReenable.Add( inst );
				}
			}

			// TODO: reset default UVs
			//UVs are lost, and will never be reconstructed unless we do them again, now
			//if (!rectangle.IsDisposed)
			//    rectangle.SetDefaultUVs();

			foreach ( var inst in instancesToReenable )
			{
				inst.IsEnabled = true;
			}
		}
	}
}