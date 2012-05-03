#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
	/// <summary>
	/// Enumeration of types of data that can be read from textures that are
	/// specific to a given layer. Notice that global texture information 
	/// such as shadows and terrain normals are not represented
	/// here because they are not a per-layer attribute, and blending
	/// is stored in packed texture structures which are stored separately.
	/// </summary>
	public enum TerrainLayerSamplerSemantic
	{
		/// <summary>
		///  Albedo colour (diffuse reflectance colour)
		/// </summary>
		Albedo = 0,

		/// <summary>
		/// Tangent-space normal information from a detail texture
		/// </summary>
		Normal = 1,

		/// <summary>
		/// Height information for the detail texture
		/// </summary>
		Height = 2,

		/// <summary>
		/// Specular reflectance
		/// </summary>
		Specular = 3,
	}

	/// <summary>
	/// Information about one element of a sampler / texture within a layer.
	/// </summary>
	public struct TerrainLayerSamplerElement
	{
		/// <summary>
		/// The source sampler index of this element relative to LayerDeclaration's list
		/// </summary>
		public byte Source;

		/// <summary>
		/// The semantic this element represents
		/// </summary>
		public TerrainLayerSamplerSemantic Semantic;

		/// <summary>
		/// The colour element at which this element starts
		/// </summary>
		public byte ElementStart;

		/// <summary>
		/// The number of colour elements this semantic uses (usually standard per semantic)
		/// </summary>
		public byte ElementCount;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public static bool operator ==( TerrainLayerSamplerElement left, TerrainLayerSamplerElement right )
		{
			return left.Source == right.Source && left.Semantic == right.Semantic && left.ElementStart == right.ElementStart &&
			       left.ElementCount == right.ElementCount;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=( TerrainLayerSamplerElement left, TerrainLayerSamplerElement right )
		{
			return left.Source != right.Source && left.Semantic != right.Semantic && left.ElementStart != right.ElementStart &&
			       left.ElementCount != right.ElementCount;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="src"></param>
		/// <param name="sem"></param>
		/// <param name="elemStart"></param>
		/// <param name="elemCount"></param>
		public TerrainLayerSamplerElement( byte src, TerrainLayerSamplerSemantic sem, byte elemStart, byte elemCount )
		{
			Source = src;
			Semantic = sem;
			ElementStart = elemStart;
			ElementCount = elemCount;
		}
	}

	/// <summary>
	/// Description of a sampler that will be used with each layer.
	/// </summary>
	public struct TerrainLayerSampler
	{
		/// <summary>
		/// A descriptive name that is merely used to assist in recognition
		/// </summary>
		public string Alias;

		/// <summary>
		/// The format required of this texture
		/// </summary>
		public PixelFormat Format;

		public static bool operator ==( TerrainLayerSampler left, TerrainLayerSampler right )
		{
			return left.Alias == right.Alias && left.Format == right.Format;
		}

		public static bool operator !=( TerrainLayerSampler left, TerrainLayerSampler right )
		{
			return left.Alias != right.Alias && left.Format != right.Format;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="aliasName"></param>
		/// <param name="format"></param>
		public TerrainLayerSampler( string aliasName, PixelFormat format )
		{
			Alias = aliasName;
			Format = format;
		}
	}

	/// <summary>
	/// The definition of the information each layer will contain in this terrain.
	/// All layers must contain the same structure of information, although the
	/// input textures can be different per layer instance. 
	/// </summary>
	public struct TerrainLayerDeclaration
	{
		/// <summary>
		/// 
		/// </summary>
		public List<TerrainLayerSampler> Samplers;

		/// <summary>
		/// 
		/// </summary>
		public List<TerrainLayerSamplerElement> Elements;

		public static bool operator ==( TerrainLayerDeclaration left, TerrainLayerDeclaration right )
		{
			return left.Samplers == right.Samplers && left.Elements == right.Elements;
		}

		public static bool operator !=( TerrainLayerDeclaration left, TerrainLayerDeclaration right )
		{
			return left.Samplers != right.Samplers && left.Elements != right.Elements;
		}
	}

	/// <summary>
	/// Class that provides functionality to generate materials for use with a terrain.
	/// </summary>
	/// <remarks>
	/// Terrains are composed of one or more layers of texture information, and
	///	require that a material is generated to render them. There are various approaches
	///	to rendering the terrain, which may vary due to:
	///	<ul><li>Hardware support (static)</li>
	///	<li>Texture instances assigned to a particular terrain (dynamic in an editor)</li>
	///	<li>User selection (e.g. changing to a cheaper option in order to increase performance, 
	///	or in order to test how the material might look on other hardware (dynamic)</li>
	///	</ul>
	///	Subclasses of this class are responsible for responding to these factors and
	///	to generate a terrain material. 
	///	@par
	///	In order to cope with both hardware support and user selection, the generator
	///	must expose a number of named 'profiles'. These profiles should function on
	///	a known range of hardware, and be graded by quality. At runtime, the user 
	///	should be able to select the profile they wish to use (provided hardware
	///	support is available). 
	/// </remarks>
	public class TerrainMaterialGenerator : IDisposable
	{
		#region - fields -

#warning: mb sorted list?
		/// <summary>
		/// List of profiles - NB should be ordered in descending complexity
		/// </summary>
		protected List<Profile> mProfiles = new List<Profile>();

		/// <summary>
		/// the currently active profile
		/// </summary>
		protected Profile mActiveProfile;

		/// <summary>
		/// 
		/// </summary>
		protected int mChangeCounter;

		/// <summary>
		/// 
		/// </summary>
		protected TerrainLayerDeclaration mLayerDecl;

		/// <summary>
		/// 
		/// </summary>
		protected uint mDebugLevel;

		/// <summary>
		/// 
		/// </summary>
		protected SceneManager mCompositeMapSM;

		/// <summary>
		/// 
		/// </summary>
		protected Camera mCompositeMapCam;

		/// <summary>
		/// deliberately holding this by raw pointer to avoid shutdown issues
		/// </summary>
		protected Texture mCompositeMapRTT;

		/// <summary>
		/// 
		/// </summary>
		protected ManualObject mCompositeMapPlane;

		/// <summary>
		/// 
		/// </summary>
		protected Light mCompositeMapLight;

		#endregion

		#region - properties -

		/// <summary>
		/// List of profiles - NB should be ordered in descending complexity
		/// </summary>
		public List<Profile> Profiles
		{
			get
			{
				return mProfiles;
			}
		}

		/// <summary>
		/// Get's or Set's the active profile
		/// </summary>
		public Profile ActiveProfile
		{
			set
			{
				if ( mActiveProfile != value )
				{
					mActiveProfile = value;
					MarkChanged();
				}
			}
			get
			{
				// default if not chosen yet
				if ( mActiveProfile == null && mProfiles.Count > 0 )
				{
					mActiveProfile = mProfiles[ 0 ];
				}

				return mActiveProfile;
			}
		}

		/// <summary>
		/// Returns the number of times the generator has undergone a change which 
		///	would require materials to be regenerated.
		/// </summary>
		public int ChangeCount
		{
			get
			{
				return mChangeCounter;
			}
		}

		/// <summary>
		/// Get the layer declaration that this material generator operates with.
		/// </summary>
		public virtual TerrainLayerDeclaration LayerDeclaration
		{
			get
			{
				return mLayerDecl;
			}
		}

		/// <summary>
		/// Get's or set's the debug level of the material. 
		/// </summary>
		/// <remarks>
		/// Sets the level of debug display for this material.
		///	What this debug level means is entirely depdendent on the generator, 
		///	the only constant is that 0 means 'no debug' and non-zero means 
		///	'some level of debugging', with any graduations in non-zero values
		///	being generator-specific.
		/// </remarks>
		public virtual uint DebugLevel
		{
			set
			{
				if ( mDebugLevel != value )
				{
					mDebugLevel = value;
					MarkChanged();
				}
			}
			get
			{
				return mDebugLevel;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Texture CompositeMapRTT
		{
			get
			{
				return mCompositeMapRTT;
			}
		}

		#endregion

		#region - inner class Pofile -

		/// <summary>
		/// Inner class which should also be subclassed to provide profile-specific 
		///	material generation.
		/// </summary>
		public abstract class Profile
		{
			#region - fields -

			protected TerrainMaterialGenerator mParent;

			/// <summary>
			/// 
			/// </summary>
			protected string mName;

			/// <summary>
			/// 
			/// </summary>
			protected string mDesc;

			#endregion

			#region - properties -

			/// <summary>
			/// Get's the generator which owns this profile
			/// </summary>
			public TerrainMaterialGenerator Parent
			{
				get
				{
					return mParent;
				}
			}

			/// <summary>
			/// Get's the name of this profile
			/// </summary>
			public string Name
			{
				get
				{
					return mName;
				}
			}

			/// <summary>
			/// Get's the description of this profile
			/// </summary>
			public string Description
			{
				get
				{
					return mDesc;
				}
			}

			#endregion

			#region - constructor -

			/// <summary>
			/// 
			/// </summary>
			/// <param name="parent"></param>
			/// <param name="name"></param>
			/// <param name="description"></param>
			public Profile( TerrainMaterialGenerator parent, string name, string description )
			{
				mParent = parent;
				mName = name;
				mDesc = description;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="profile"></param>
			public Profile( Profile profile )
			{
				mParent = profile.mParent;
				mName = profile.mName;
				mDesc = profile.mDesc;
			}

			#endregion

			#region - abstract -

			/// <summary>
			/// Generate / reuse a material for the terrain
			/// </summary>
			/// <param name="terrain"></param>
			/// <returns></returns>
			public abstract Material Generate( Terrain terrain );

			/// <summary>
			/// Generate / reuse a material for the terrain
			/// </summary>
			/// <param name="terrain"></param>
			/// <returns></returns>
			public abstract Material GenerateForCompositeMap( Terrain terrain );

			/// <summary>
			/// Get's the number of layers supported
			/// </summary>
			/// <param name="terrain"></param>
			/// <returns></returns>
			public abstract byte GetMaxLayers( Terrain terrain );

			/// <summary>
			/// Update params for a terrain
			/// </summary>
			/// <param name="mat"></param>
			/// <param name="terrain"></param>
			public abstract void UpdateParams( Material mat, Terrain terrain );

			/// <summary>
			/// Update params for a terrain
			/// </summary>
			/// <param name="mat"></param>
			/// <param name="terrain"></param>
			public abstract void UpdateParamsForCompositeMap( Material mat, Terrain terrain );

			/// <summary>
			/// Request the options needed from the terrain
			/// </summary>
			/// <param name="terrain"></param>
			public abstract void RequestOption( Terrain terrain );

			#endregion

			/// <summary>
			/// Update the composite map for a terrain
			/// </summary>
			/// <param name="terrain"></param>
			/// <param name="rect"></param>
			public virtual void UpdateCompositeMap( Terrain terrain, Rectangle rect )
			{
				// convert point-space rect into image space
				int compSize = terrain.CompositeMap.Width;
				var imgRect = new Rectangle();
				Vector3 inVec = Vector3.Zero, outVec = Vector3.Zero;
				inVec.x = rect.Left;
				inVec.y = rect.Bottom - 1; // this is 'top' in image space, also make inclusive
				terrain.ConvertPosition( Space.PointSpace, inVec, Space.TerrainSpace, ref outVec );
				float left = ( outVec.x*compSize );
				float top = ( ( 1.0f - outVec.y )*compSize );
				;
				imgRect.Left = (long)left;
				imgRect.Top = (long)top;
				inVec.x = rect.Right - 1;
				inVec.y = rect.Top; // this is 'bottom' in image space
				terrain.ConvertPosition( Space.PointSpace, inVec, Space.TerrainSpace, ref outVec );
				float right = ( outVec.x*(float)compSize + 1 );
				imgRect.Right = (long)right;
				float bottom = ( ( 1.0f - outVec.y )*compSize + 1 );
				imgRect.Bottom = (long)bottom;

				imgRect.Left = System.Math.Max( 0L, imgRect.Left );
				imgRect.Top = System.Math.Max( 0L, imgRect.Top );
				imgRect.Right = System.Math.Min( (long)compSize, imgRect.Right );
				imgRect.Bottom = System.Math.Min( (long)compSize, imgRect.Bottom );

#warning enable rendercompositemap
#if false
mParent.RenderCompositeMap(compSize, imgRect,
terrain.CompositeMapMaterial,
terrain.CompositeMap);
#endif
				update = true;
			}

			private static bool update = false;
		}

		#endregion

		#region - functions -

		/// <summary>
		/// Set the active profile by name.
		/// </summary>
		/// <param name="name">name of the profile</param>
		public virtual void SetActiveProfile( string name )
		{
			if ( mActiveProfile == null || mActiveProfile.Name != name )
			{
				for ( int i = 0; i < mProfiles.Count; ++i )
				{
					if ( mProfiles[ i ].Name == name )
					{
						ActiveProfile = mProfiles[ i ];
						break;
					}
				}
			}
		}

		/// <summary>
		/// Whether this generator can generate a material for a given declaration. 
		///	By default this only returns true if the declaration is equal to the 
		///	standard one returned from getLayerDeclaration, but if a subclass wants
		///	to be flexible to generate materials for other declarations too, it 
		///	can specify here. 
		/// </summary>
		/// <param name="decl"></param>
		/// <returns></returns>
		public virtual bool CanGenerateUsingDeclaration( TerrainLayerDeclaration decl )
		{
			return decl == mLayerDecl;
		}

		/// <summary>
		/// Triggers the generator to request the options that it needs.
		/// </summary>
		/// <param name="terrain"></param>
		public virtual void RequestOption( Terrain terrain )
		{
			Profile p = ActiveProfile;
			if ( p != null )
			{
				p.RequestOption( terrain );
			}
		}

		/// <summary>
		/// Generate a material for the given terrain using the active profile.
		/// </summary>
		/// <param name="terrain"></param>
		public virtual Material Generate( Terrain terrain )
		{
#warning: check return value null here
			Profile p = ActiveProfile;
			if ( p == null )
			{
				return null;
			}
			else
			{
				return p.Generate( terrain );
			}
		}

		/// <summary>
		/// Generate a material for the given composite map of the terrain using the active profile.
		/// </summary>
		/// <param name="terrain"></param>
		/// <returns></returns>
		public virtual Material GenerateForCompositeMap( Terrain terrain )
		{
			Profile p = ActiveProfile;
#warning: check return value null here
			if ( p == null )
			{
				return null;
			}
			else
			{
				return p.GenerateForCompositeMap( terrain );
			}
		}

		/// <summary>
		/// Internal method - indicates that a change has been made that would require material regeneration
		/// </summary>
		public void MarkChanged()
		{
			++mChangeCounter;
		}

		/// <summary>
		/// Get the maximum number of layers supported with the given terrain. 
		/// </summary>
		/// <value>
		/// When you change the options on the terrain, this value can change.
		/// </value>
		/// <param name="terrain"></param>
		/// <returns></returns>
		public virtual byte GetMaxLayers( Terrain terrain )
		{
			Profile p = ActiveProfile;
			if ( p != null )
			{
				return p.GetMaxLayers( terrain );
			}
			else
			{
				return 0;
			}
		}

		/// <summary>
		/// Update the composite map for a terrain.
		/// The composite map for a terrain must match what the terrain should look like
		/// at distance. This method will only be called in the render thread so the
		/// generator is free to render into a texture to support this, so long as 
		/// the results are blitted into the Terrain's own composite map afterwards.
		/// </summary>
		/// <param name="terrain"></param>
		/// <param name="rect"></param>
		public virtual void UpdateCompositeMap( Terrain terrain, Rectangle rect )
		{
			Profile p = ActiveProfile;
			if ( p == null )
			{
				return;
			}
			else
			{
				p.UpdateCompositeMap( terrain, rect );
			}
		}

		/// <summary>
		/// Update parameters for the given terrain using the active profile.
		/// </summary>
		/// <param name="mat"></param>
		/// <param name="terrain"></param>
		public virtual void UpdateParams( Material mat, Terrain terrain )
		{
			Profile p = ActiveProfile;
			if ( p != null )
			{
				p.UpdateParams( mat, terrain );
			}
		}

		/// <summary>
		/// Update parameters for the given terrain using the active profile.
		/// </summary>
		/// <param name="mat"></param>
		/// <param name="terrain"></param>
		public virtual void UpdateParamsForCompositeMap( Material mat, Terrain terrain )
		{
			Profile p = ActiveProfile;
			if ( p != null )
			{
				p.UpdateParamsForCompositeMap( mat, terrain );
			}
		}

		/// <summary>
		/// Helper method to render a composite map.
		/// </summary>
		/// <param name="size"> The requested composite map size</param>
		/// <param name="rect"> The region of the composite map to update, in image space</param>
		/// <param name="mat">The material to use to render the map</param>
		/// <param name="destCompositeMap"></param>
		public virtual void RenderCompositeMap( int size, Rectangle rect, Material mat, Texture destCompositeMap )
		{
			//return;
			if ( mCompositeMapSM == null )
			{
				//dedicated SceneManager

				mCompositeMapSM = Root.Instance.CreateSceneManager( SceneType.ExteriorClose, "TerrainMaterialGenerator_SceneManager" );
				float camDist = 100;
				float halfCamDist = camDist*0.5f;
				mCompositeMapCam = mCompositeMapSM.CreateCamera( "TerrainMaterialGenerator_Camera" );
				mCompositeMapCam.Position = new Vector3( 0, 0, camDist );
				//mCompositeMapCam.LookAt(Vector3.Zero);
				mCompositeMapCam.ProjectionType = Projection.Orthographic;
				mCompositeMapCam.Near = 10;
				mCompositeMapCam.Far = 999999*3;
				//mCompositeMapCam.AspectRatio = camDist / camDist;
				mCompositeMapCam.SetOrthoWindow( camDist, camDist );
				// Just in case material relies on light auto params
				mCompositeMapLight = mCompositeMapSM.CreateLight( "TerrainMaterialGenerator_Light" );
				mCompositeMapLight.Type = LightType.Directional;

				RenderSystem rSys = Root.Instance.RenderSystem;
				float hOffset = rSys.HorizontalTexelOffset/(float)size;
				float vOffset = rSys.VerticalTexelOffset/(float)size;

				//setup scene
				mCompositeMapPlane = mCompositeMapSM.CreateManualObject( "TerrainMaterialGenerator_ManualObject" );
				mCompositeMapPlane.Begin( mat.Name, OperationType.TriangleList );
				mCompositeMapPlane.Position( -halfCamDist, halfCamDist, 0 );
				mCompositeMapPlane.TextureCoord( 0 - hOffset, 0 - vOffset );
				mCompositeMapPlane.Position( -halfCamDist, -halfCamDist, 0 );
				mCompositeMapPlane.TextureCoord( 0 - hOffset, 1 - vOffset );
				mCompositeMapPlane.Position( halfCamDist, -halfCamDist, 0 );
				mCompositeMapPlane.TextureCoord( 1 - hOffset, 1 - vOffset );
				mCompositeMapPlane.Position( halfCamDist, halfCamDist, 0 );
				mCompositeMapPlane.TextureCoord( 1 - hOffset, 0 - vOffset );
				mCompositeMapPlane.Quad( 0, 1, 2, 3 );
				mCompositeMapPlane.End();
				mCompositeMapSM.RootSceneNode.AttachObject( mCompositeMapPlane );
			} //end if

			// update
			mCompositeMapPlane.SetMaterialName( 0, mat.Name );
			mCompositeMapLight.Direction = TerrainGlobalOptions.LightMapDirection;
			mCompositeMapLight.Diffuse = TerrainGlobalOptions.CompositeMapDiffuse;
			mCompositeMapSM.AmbientLight = TerrainGlobalOptions.CompositeMapAmbient;


			//check for size change (allow smaller to be reused)
			if ( mCompositeMapRTT != null && size != mCompositeMapRTT.Width )
			{
				TextureManager.Instance.Remove( mCompositeMapRTT );
				mCompositeMapRTT = null;
			}
			if ( mCompositeMapRTT == null )
			{
				mCompositeMapRTT = TextureManager.Instance.CreateManual( mCompositeMapSM.Name + "/compRTT",
				                                                         ResourceGroupManager.DefaultResourceGroupName,
				                                                         TextureType.TwoD, size, size, 0, PixelFormat.BYTE_RGBA,
				                                                         TextureUsage.RenderTarget );

				RenderTarget rtt = mCompositeMapRTT.GetBuffer().GetRenderTarget();
				// don't render all the time, only on demand
				rtt.IsAutoUpdated = false;
				Viewport vp = rtt.AddViewport( mCompositeMapCam );
				// don't render overlays
				vp.ShowOverlays = false;
			}

			// calculate the area we need to update
			float vpleft = (float)rect.Left/(float)size;
			float vptop = (float)rect.Top/(float)size;
			float vpright = (float)rect.Right/(float)size;
			float vpbottom = (float)rect.Bottom/(float)size;
			float vpwidth = (float)rect.Width/(float)size;
			float vpheight = (float)rect.Height/(float)size;

			RenderTarget rtt2 = mCompositeMapRTT.GetBuffer().GetRenderTarget();
			Viewport vp2 = rtt2.GetViewport( 0 );
			mCompositeMapCam.SetWindow( vpleft, vptop, vpright, vpbottom );
			rtt2.Update();
			vp2.Update();
			// We have an RTT, we want to copy the results into a regular texture
			// That's because in non-update scenarios we don't want to keep an RTT
			// around. We use a single RTT to serve all terrain pages which is more
			// efficient.
			var box = new BasicBox( (int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom );
			destCompositeMap.GetBuffer().Blit( mCompositeMapRTT.GetBuffer(), box, box );
		}

		public void Dispose()
		{
			if ( mProfiles != null )
			{
				mProfiles.Clear();
				mProfiles = null;
			}
			if ( mCompositeMapRTT != null && TextureManager.Instance != null )
			{
				TextureManager.Instance.Remove( mCompositeMapRTT );
				mCompositeMapRTT = null;
			}
			if ( mCompositeMapSM != null && Root.Instance != null )
			{
				// will also delete cam and objects etc
				Root.Instance.DestroySceneManager( mCompositeMapSM );
				mCompositeMapSM = null;
				mCompositeMapCam = null;
				mCompositeMapPlane = null;
				mCompositeMapLight = null;
			}
		}

		#endregion
	}
}