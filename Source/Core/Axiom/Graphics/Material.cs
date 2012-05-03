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
using System.Diagnostics;
using System.Text;
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Graphics.Collections;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///    Class encapsulating the rendering properties of an object.
	/// </summary>
	/// <remarks>
	///    The Material class encapsulates ALL aspects of the visual appearance,
	///    of an object. It also includes other flags which
	///    might not be traditionally thought of as material properties such as
	///    culling modes and depth buffer settings, but these affect the
	///    appearance of the rendered object and are convenient to attach to the
	///    material since it keeps all the settings in one place. This is
	///    different to Direct3D which treats a material as just the color
	///    components (diffuse, specular) and not texture maps etc. This
	///    Material can be thought of as equivalent to a 'Shader'.
	///    <p/>
	///    A Material can be rendered in multiple different ways depending on the
	///    hardware available. You may configure a Material to use high-complexity
	///    fragment shaders, but these won't work on every card; therefore a Technique
	///    is an approach to creating the visual effect you are looking for. You are advised
	///    to create fallback techniques with lower hardware requirements if you decide to
	///    use advanced features. In addition, you also might want lower-detail techniques
	///    for distant geometry.
	///    <p/>
	///    Each technique can be made up of multiple passes. A fixed-function pass
	///    may combine multiple texture layers using multi-texturing, but they can
	///    break that into multiple passes automatically if the active card cannot
	///    handle that many simultaneous textures. Programmable passes, however, cannot
	///    be split down automatically, so if the active graphics card cannot handle the
	///    technique which contains these passes, the engine will try to find another technique
	///    which the card can do. If, at the end of the day, the card cannot handle any of the
	///    techniques which are listed for the material, the engine will render the
	///    geometry plain white, which should alert you to the problem.
	///    <p/>
	///    The engine comes configured with a number of default settings for a newly
	///    created material. These can be changed if you wish by retrieving the
	///    default material settings through
	///    SceneManager.DefaultMaterialSettings. Any changes you make to the
	///    Material returned from this method will apply to any materials created
	///    from this point onward.
	/// </remarks>
	public class Material : Resource, IComparable, ICopyable<Material>
	{
		#region Fields and Properties

		/// <summary>
		///    Auto incrementing number for creating unique names.
		/// </summary>
		/// <ogre name="" />
		protected static int autoNumber;

		/// <summary>
		/// A reference to a precreated Material that contains all the default settings.
		/// </summary>
		/// <ogre name="" />
		protected internal static Material defaultSettings;

		/// <summary>
		///
		/// </summary>
		/// <ogre name="mBestTechniqueList" />
		protected readonly Dictionary<ushort, Dictionary<int, Technique>> bestTechniquesByScheme =
			new Dictionary<ushort, Dictionary<int, Technique>>();

		#region techniques Property

		private TechniqueList _techniques = new TechniqueList();

		/// <summary>
		///    A list of techniques that exist within this Material.
		/// </summary>
		/// <ogre name="mTechniques" />
		protected TechniqueList techniques
		{
			get
			{
				return _techniques;
			}
			set
			{
				_techniques = value;
			}
		}

		#endregion techniques Property

		#region supportedTechniques Property

		private readonly TechniqueList _supportedTechniques = new TechniqueList();

		/// <summary>
		///    A list of the techniques of this material that are supported by the current hardware.
		/// </summary>
		/// <ogre name="mSupportedTechniques" />
		public TechniqueList SupportedTechniques
		{
			get
			{
				return _supportedTechniques;
			}
		}

		#endregion supportedTechniques Property

		#region compilationRequired Property

		private bool _compilationRequired;

		/// <summary>
		///    Flag noting whether or not this Material needs to be re-compiled.
		/// </summary>
		/// <ogre name="mCompilationRequired" />
		protected bool compilationRequired
		{
			get
			{
				return _compilationRequired;
			}
			set
			{
				_compilationRequired = value;
			}
		}

		#endregion compilationRequired Property

		#region ReceiveShadows Property

		/// <summary>
		///		Sets whether objects using this material will receive shadows.
		/// </summary>
		/// <remarks>
		///		This method allows a material to opt out of receiving shadows, if
		///		it would otherwise do so. Shadows will not be cast on any objects
		///		unless the scene is set up to support shadows and not all techniques
		///		cast shadows on all objects. In any case, if you have a need to prevent
		///		shadows being received by material, this is the method you call to do it.
		///		Note: Transparent materials never receive shadows despite this setting.
		///		The default is to receive shadows.
		///		<seealso cref="SceneManager.ShadowTechnique"/>
		/// </remarks>
		/// <ogre name="setRecieveShadows" />
		/// <ogre name="getRecieveShadows" />
		public bool ReceiveShadows { get; set; }

		#endregion ReceiveShadows Property

		#region TransparencyCastsShadows Property

		/// <summary>
		///		Gets/Sets whether objects using this material be classified as opaque to the shadow caster system.
		/// </summary>
		/// <remarks>
		///		This method allows a material to cast a shadow, even if it is transparent.
		///		By default, transparent materials neither cast nor receive shadows. Shadows
		///		will not be cast on any objects unless the scene is set up to support shadows
		///		<seealso cref="SceneManager.ShadowTechnique"/>, and not all techniques cast
		///		shadows on all objects.
		/// </remarks>
		/// <ogre name="setTransparentCastsShadows" />
		/// <ogre name="getTransparentCastsShadows" />
		public bool TransparencyCastsShadows { get; set; }

		#endregion TransparencyCastsShadows Property

		/// <summary>
		///		Determines if the material has any transparency with the rest of the scene (derived from
		///    whether any Techniques say they involve transparency).
		/// </summary>
		/// <ogre name="isTransparent" />
		public bool IsTransparent
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				// check each technique to see if it is transparent
				for ( var i = 0; i < techniques.Count; i++ )
				{
					if ( techniques[ i ].IsTransparent )
					{
						return true;
					}
				}

				// if we got this far, there are no transparent techniques
				return false;
			}
		}

		/// <summary>
		///    Gets the number of techniques within this Material.
		/// </summary>
		/// <ogre name="getNumTechniques" />
		public int TechniqueCount
		{
			get
			{
				return techniques.Count;
			}
		}

		#region Convience Properties

		// -------------------------------------------------------------------------------
		// The following methods are to make migration from previous versions simpler
		// and to make code easier to write when dealing with simple materials
		// They set the properties which have been moved to Pass for all Techniques and all Passes

		/// <summary>Sets the ambient colour reflectance properties for every Pass in every Technique.</summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.Ambient"></see>
		/// </remarks>
		/// <ogre name="setAmbient" />
		public ColorEx Ambient
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).Ambient = value;
				}
			}
		}

		/// <summary>
		/// Sets the diffuse colour reflectance properties of every Pass in every Technique.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.Diffuse"></see>
		/// </remarks>
		/// <ogre name="setDiffuse" />
		public ColorEx Diffuse
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).Diffuse = value;
				}
			}
		}

		/// <summary>
		/// Sets the specular colour reflectance properties of every Pass in every Technique.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.Specular"></see>
		/// </remarks>
		/// <ogre name="setSpecular" />
		public ColorEx Specular
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).Specular = value;
				}
			}
		}

		/// <summary>
		/// Sets the shininess properties of every Pass in every Technique.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.Shininess"></see>
		/// </remarks>
		/// <ogre name="setShininess" />
		public Single Shininess
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).Shininess = value;
				}
			}
		}

		/// <summary>
		/// Sets the amount of self-illumination of every Pass in every Technique.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.SelfIllumination"></see>
		/// </remarks>
		/// <ogre name="setSelfIllumination" />
		public ColorEx SelfIllumination
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).SelfIllumination = value;
				}
			}
		}

		/// <summary>
		/// Sets whether or not each Pass renders with depth-buffer checking on or not.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.DepthCheck"></see>
		/// </remarks>
		/// <ogre name="setDepthCheckEnabled" />
		public bool DepthCheck
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).DepthCheck = value;
				}
			}
		}

		/// <summary>
		/// Sets the function used to compare depth values when depth checking is on.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.DepthFunction"></see>
		/// </remarks>
		/// <ogre name="setDepthFunction" />
		public CompareFunction DepthFunction
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).DepthFunction = value;
				}
			}
		}

		/// <summary>
		/// Sets whether or not each Pass renders with depth-buffer writing on or not.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.DepthWrite"></see>
		/// </remarks>
		/// <ogre name="setDepthWriteEnabled" />
		public bool DepthWrite
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).DepthWrite = value;
				}
			}
		}

		/// <summary>
		/// Sets whether or not colour buffer writing is enabled for each Pass.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.ColorWriteEnabled"></see>
		/// </remarks>
		/// <ogre name="setColourWriteEnabled" />
		public bool ColorWriteEnabled
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).ColorWriteEnabled = value;
				}
			}
		}

		/// <summary>
		/// Sets the culling mode for each pass  based on the 'vertex winding'.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.CullingMode"></see>
		/// </remarks>
		/// <ogre name="setCullingMode" />
		public CullingMode CullingMode
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).CullingMode = value;
				}
			}
		}

		/// <summary>
		/// Sets the manual culling mode, performed by CPU rather than hardware.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.ManualCullingMode"></see>
		/// </remarks>
		/// <ogre name="setManualCullingMode" />
		public ManualCullingMode ManualCullingMode
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).ManualCullingMode = value;
				}
			}
		}

		/// <summary>
		/// Sets whether or not dynamic lighting is enabled for every Pass.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.Lighting"></see>
		/// </remarks>
		/// <ogre name="setLighting" />
		public bool Lighting
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).LightingEnabled = value;
				}
			}
		}

		/// <summary>
		/// Sets the depth bias to be used for each Pass.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.DepthBias"></see>
		/// </remarks>
		/// <ogre name="setDepthBias" />
		public int DepthBias
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).DepthBias = value;
				}
			}
		}

		/// <summary>
		/// Set texture filtering for every texture unit in every Technique and Pass
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.TextureFiltering"></see>
		/// </remarks>
		/// <ogre name="setTextureFiltering" />
		public TextureFiltering TextureFiltering
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).TextureFiltering = value;
				}
			}
		}

		/// <summary>
		/// Sets the anisotropy level to be used for all textures.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.TextureAnisotropy"></see>
		/// </remarks>
		/// <ogre name="setTextureAnisotropy" />
		public int TextureAnisotropy
		{
			set
			{
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).TextureAnisotropy = value;
				}
			}
		}

		/// <summary>
		/// Sets the type of light shading required
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.ShadingMode"></see>
		/// </remarks>
		public Shading ShadingMode
		{
			set
			{
				// load each technique
				for ( var i = 0; i < techniques.Count; i++ )
				{
					( (Technique)techniques[ i ] ).ShadingMode = value;
				}
			}
		}

		public void SetFog( bool overrideScene )
		{
			SetFog( overrideScene, FogMode.None, ColorEx.White, 0.001f, 0.0f, 1.0f );
		}

		/// <summary>
		/// Sets the fogging mode applied to each pass.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.SetFog"></see>
		/// </remarks>
		/// <ogre name="" />
		public void SetFog( bool overrideScene, FogMode mode, ColorEx color, Single expDensity, Single linearStart,
		                    Single linearEnd )
		{
			// load each technique
			for ( var i = 0; i < techniques.Count; i++ )
			{
				( (Technique)techniques[ i ] ).SetFog( overrideScene, mode, color, expDensity, linearStart, linearEnd );
			}
		}

		/// <summary>
		/// Sets the kind of blending every pass has with the existing contents of the scene.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.SetSceneBlending"></see>
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public void SetSceneBlending( SceneBlendType blendType )
		{
			// load each technique
			for ( var i = 0; i < techniques.Count; i++ )
			{
				techniques[ i ].SetSceneBlending( blendType );
			}
		}

		/// <summary>
		/// Sets the kind of blending every pass has with the existing contents of the scene, using individual factors for color and alpha channels
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the 
		/// Technique. For simplicity, this method allows you to set these properties for 
		/// every current Technique, and for every current Pass within those Techniques. If 
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// </remarks>
		/// <see cref="Pass.SetSeparateSceneBlending"/>
		[OgreVersion( 1, 7, 2 )]
		public void SetSeparateSceneBlending( SceneBlendType sbt, SceneBlendType sbta )
		{
			for ( var i = 0; i < techniques.Count; i++ )
			{
				techniques[ i ].SetSeparateSceneBlending( sbt, sbta );
			}
		}

		/// <summary>
		/// Allows very fine control of blending every Pass with the existing contents of the scene.
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the
		/// Technique. For simplicity, this method allows you to set these properties for
		/// every current Technique, and for every current Pass within those Techniques. If
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// <see ref="Pass.SetSceneBlending"></see>
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
		{
			// load each technique
			for ( var i = 0; i < techniques.Count; i++ )
			{
				techniques[ i ].SetSceneBlending( src, dest );
			}
		}

		/// <summary>
		/// Allows very fine control of blending every Pass with the existing contents of the scene, using individual factors for color and alpha channels
		/// </summary>
		/// <remarks>
		/// This property has been moved to the Pass class, which is accessible via the 
		/// Technique. For simplicity, this method allows you to set these properties for 
		/// every current Technique, and for every current Pass within those Techniques. If 
		/// you need more precision, retrieve the Technique and Pass instances and set the
		/// property there.
		/// </remarks>
		/// <see cref="Pass.SetSeparateSceneBlending"/>
		[OgreVersion( 1, 7, 2 )]
		public void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor,
		                                      SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha )
		{
			for ( var i = 0; i < techniques.Count; i++ )
			{
				techniques[ i ].SetSeparateSceneBlending( sourceFactor, destFactor, sourceFactorAlpha, destFactorAlpha );
			}
		}

		#endregion Convience Properties

		#endregion Fields and Properties

		#region Constructors and Destructor

		public Material( ResourceManager parent, string name, UInt64 handle, string group )
			: this( parent, name, handle, group, false, null )
		{
		}

		public Material( ResourceManager parent, string name, UInt64 handle, string group, bool isManual,
		                 IManualResourceLoader loader )
			: base( parent, name, handle, group )
		{
			ReceiveShadows = true;
			TransparencyCastsShadows = false;
			_compilationRequired = true;

			// Override isManual, not applicable for Material (we always want to call loadImpl)
			if ( isManual )
			{
				IsManuallyLoaded = false;
				LogManager.Instance.Write(
					"Material {0} was requested with isManual=true, but this is not applicable for materials; the flag has been reset to false.",
					name );
			}

			_lodValues.Add( 0.0f );

			ApplyDefaults();
		}

		#endregion Constructors and Destructor

		#region Methods

		#region Compile Method

		/// <overloads>
		/// <summary>
		///    'Compiles' this Material.
		/// </summary>
		/// <remarks>
		///    Compiling a material involves determining which Techniques are supported on the
		///    card on which the engine is currently running, and for fixed-function Passes within those
		///    Techniques, splitting the passes down where they contain more TextureUnitState
		///    instances than the current card has texture units.
		///    <p/>
		///    This process is automatically done when the Material is loaded, but may be
		///    repeated if you make some procedural changes.
		/// </remarks>
		/// <ogre name="compile" />
		/// </overloads>
		/// <remarks>
		///    By default, the engine will automatically split texture unit operations into multiple
		///    passes when the target hardware does not have enough texture units.
		/// </remarks>
		public void Compile()
		{
			Compile( true );
		}

		/// <param name="autoManageTextureUnits">
		///    If true, when a fixed function pass has too many TextureUnitState
		///    entries than the card has texture units, the Pass in question will be split into
		///    more than one Pass in order to emulate the Pass. If you set this to false and
		///    this situation arises, an Exception will be thrown.
		/// </param>
		[OgreVersion( 1, 7, 2 )]
		public void Compile( bool autoManageTextureUnits )
		{
			// Compile each technique, then add it to the list of supported techniques
			SupportedTechniques.Clear();
			ClearBestTechniqueList();
			var unSupportedReasons = new StringBuilder();

			// compile each technique, adding supported ones to the list of supported techniques
			for ( var i = 0; i < techniques.Count; i++ )
			{
				var t = techniques[ i ];

				// compile the technique, splitting texture passes if required
				var compileMessages = t.Compile( autoManageTextureUnits );

				// if supported, add it to the list
				if ( t.IsSupported )
				{
					InsertSupportedTechnique( t );
				}
				else
				{
					LogManager.Instance.Write( "Warning: Material '{0}' Technique {1}{2} is not supported.\n{3}", _name, i,
					                           !String.IsNullOrEmpty( t.Name ) ? "(" + t.Name + ")" : "", compileMessages );
					unSupportedReasons.Append( compileMessages );
				}
			}

			_compilationRequired = false;

			// Did we find any?
			if ( SupportedTechniques.Count == 0 )
			{
				LogManager.Instance.Write(
					"Warning: Material '{0}' has no supportable Techniques on this hardware.  Will be rendered blank. Explanation:",
					_name, unSupportedReasons.ToString() );
			}
		}

		/// <summary>
		/// Clear the best technique list.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		private void ClearBestTechniqueList()
		{
			foreach ( var pair in bestTechniquesByScheme )
			{
				pair.Value.Clear();
			}
			bestTechniquesByScheme.Clear();
		}

		private void InsertSupportedTechnique( Technique technique )
		{
			SupportedTechniques.Add( technique );
			// get scheme
			var schemeIndex = technique.SchemeIndex;
			Dictionary<int, Technique> lodTechniques;
			if ( !bestTechniquesByScheme.ContainsKey( schemeIndex ) )
			{
				lodTechniques = new Dictionary<int, Technique>();
				bestTechniquesByScheme.Add( schemeIndex, lodTechniques );
			}
			else
			{
				lodTechniques = bestTechniquesByScheme[ schemeIndex ];
			}

			// Insert won't replace if supported technique for this scheme/lod is
			// already there, which is what we want
			if ( !lodTechniques.ContainsKey( technique.LodIndex ) )
			{
				lodTechniques.Add( technique.LodIndex, technique );
			}
		}

		#endregion Compile Method

		/// <summary>
		///    Creates a new Technique for this Material.
		/// </summary>
		/// <remarks>
		///    A Technique is a single way of rendering geometry in order to achieve the effect
		///    you are intending in a material. There are many reason why you would want more than
		///    one - the main one being to handle variable graphics card abilities; you might have
		///    one technique which is impressive but only runs on 4th-generation graphics cards,
		///    for example. In this case you will want to create at least one fallback Technique.
		///    The engine will work out which Techniques a card can support and pick the best one.
		///    <p/>
		///    If multiple Techniques are available, the order in which they are created is
		///    important - the engine will consider lower-indexed Techniques to be preferable
		///    to higher-indexed Techniques, ie when asked for the 'best' technique it will
		///    return the first one in the technique list which is supported by the hardware.
		/// </remarks>
		/// <returns></returns>
		/// <ogre name="createTechnique" />
		public Technique CreateTechnique()
		{
			var t = new Technique( this );
			techniques.Add( t );
			_compilationRequired = true;
			return t;
		}


		/// <summary>
		///    Gets the technique at the specified index.
		/// </summary>
		/// <param name="index">Index of the technique to return.</param>
		/// <returns></returns>
		/// <ogre name="getTechnique" />
		public Technique GetTechnique( int index )
		{
			Debug.Assert( index < techniques.Count, "index < techniques.Count" );

			return techniques[ index ];
		}

		/// <summary>
		///    Tells the material that it needs recompilation.
		/// </summary>
		/// <ogre name="_notifyNeedsRecompile" />
		[OgreVersion( 1, 7, 2 )]
		internal void NotifyNeedsRecompile()
		{
			_compilationRequired = true;

			// Also need to unload to ensure we loaded any new items
			if ( IsLoaded ) // needed to stop this being called in 'loading' state
			{
				unload();
			}
		}

		/// <summary>
		///    Removes the specified Technique from this material.
		/// </summary>
		/// <param name="t">A reference to the technique to remove</param>
		/// <ogre name="removeTechnique" />
		public void RemoveTechnique( Technique t )
		{
			Debug.Assert( t != null, "t != null" );

			// remove from the list, and force a rebuild of supported techniques
			techniques.Remove( t );
			SupportedTechniques.Clear();
			ClearBestTechniqueList();
			_compilationRequired = true;
		}

		/// <summary>
		///		Removes all techniques from this material.
		/// </summary>
		/// <ogre name="removeAllTechniques" />
		public void RemoveAllTechniques()
		{
			techniques.Clear();
			SupportedTechniques.Clear();
			ClearBestTechniqueList();
			_compilationRequired = true;
		}


		/// <summary>
		///    Creates a copy of this Material with the specified name (must be unique).
		/// </summary>
		/// <param name="newName">The name that the cloned material will be known as.</param>
		/// <returns></returns>
		/// <ogre name="clone" />
		public Material Clone( string newName )
		{
			return Clone( newName, false, "" );
		}

		public Material Clone( string newName, bool changeGroup, string newGroup )
		{
			Material newMaterial;
			if ( changeGroup )
			{
				newMaterial = (Material)MaterialManager.Instance.Create( newName, newGroup );
			}
			else
			{
				newMaterial = (Material)MaterialManager.Instance.Create( newName, Group );
			}

			// Copy material preserving name, group and handle.
			CopyTo( newMaterial, false );

			return newMaterial;
		}

		public void ApplyDefaults()
		{
			if ( defaultSettings != null )
			{
				// copy properties from the default materials
				defaultSettings.CopyTo( this, false );
			}
			_compilationRequired = true;
		}

		public bool ApplyTextureAliases( Dictionary<string, string> aliasList )
		{
			return ApplyTextureAliases( aliasList, true );
		}

		/// <summary>
		/// Applies texture names to Texture Unit State with matching texture name aliases.
		/// All techniques, passes, and Texture Unit States within the material are checked.
		/// If matching texture aliases are found then true is returned.
		/// </summary>
		/// <param name="aliasList">is a map container of texture alias, texture name pairs</param>
		/// <param name="apply">set true to apply the texture aliases else just test to see if texture alias matches are found.</param>
		/// <returns>True if matching texture aliases were found in the material.</returns>
		public bool ApplyTextureAliases( Dictionary<string, string> aliasList, bool apply )
		{
			// iterate through all techniques and apply texture aliases
			var testResult = false;

			foreach ( var t in _techniques )
			{
				if ( t.ApplyTextureAliases( aliasList, apply ) )
				{
					testResult = true;
				}
			}

			return testResult;
		}

		#region Copy Method

		/// <overload>
		/// <summary>
		///		Copies the details from the supplied material.
		/// </summary>
		/// <param name="target">Material which will receive this material's settings.</param>
		/// <ogre name="copyDetailsTo" />
		/// <ogre name="operator =" />
		/// </overload>
		public void CopyTo( Material target )
		{
			CopyTo( target, true );
		}

		/// <param name="target"></param>
		/// <param name="copyUniqueInfo">preserves the target's handle, group, name, and loading properties (unlike operator=) but copying everything else.</param>
		public void CopyTo( Material target, bool copyUniqueInfo )
		{
			if ( copyUniqueInfo )
			{
				target.Name = Name;
				target.Handle = Handle;
				target.Group = Group;
				target.IsManuallyLoaded = IsManuallyLoaded;
				target.loader = loader;
			}

			// copy basic data
			target.Size = Size;
			target.LastAccessed = LastAccessed;
			target.ReceiveShadows = ReceiveShadows;
			target.TransparencyCastsShadows = TransparencyCastsShadows;

			target.RemoveAllTechniques();

			// clone a copy of all the techniques
			for ( var i = 0; i < techniques.Count; i++ )
			{
				var technique = techniques[ i ];
				var newTechnique = target.CreateTechnique();
				technique.CopyTo( newTechnique );

				// only add this technique to supported techniques if its...well....supported :-)
				if ( newTechnique.IsSupported )
				{
					target.InsertSupportedTechnique( newTechnique );
				}
			}

			// clear LOD distances
			target._lodValues.Clear();
			target.UserLodValues.Clear();

			// copy LOD distances
			target._lodValues.AddRange( _lodValues );
			target.UserLodValues.AddRange( UserLodValues );

			target.LodStrategy = LodStrategy;

			target.compilationRequired = compilationRequired;
		}

		#endregion Copy Method

		#region GetBestTechnique Method

		/// <overloads>
		/// <summary>
		///    Gets the best supported technique.
		/// </summary>
		/// <remarks>
		///    This method returns the lowest-index supported Technique in this material
		///    (since lower-indexed Techniques are considered to be better than higher-indexed
		///    ones).
		///    <p/>
		///    The best supported technique is only available after this material has been compiled,
		///    which typically happens on loading the material. Therefore, if this method returns
		///    null, try calling Material.Load.
		/// </remarks>
		/// <ogre name="getBestTechnique" />
		/// </overloads>
		public Technique GetBestTechnique()
		{
			return GetBestTechnique( 0 );
		}

		/// <param name="lodIndex"></param>
		public Technique GetBestTechnique( int lodIndex )
		{
			return GetBestTechnique( lodIndex, null );
		}

		public Technique GetBestTechnique( int lodIndex, IRenderable renderable )
		{
			Technique technique = null;
			Dictionary<int, Technique> lodTechniques;

			if ( SupportedTechniques.Count > 0 )
			{
				if ( !bestTechniquesByScheme.ContainsKey( MaterialManager.Instance.ActiveSchemeIndex ) )
				{
					technique = MaterialManager.Instance.ArbitrateMissingTechniqueForActiveScheme( this, lodIndex, renderable );
					if ( technique != null )
					{
						return technique;
					}

					// Nope, use default
					// get the first item, will be 0 (the default) if default
					// scheme techniques exist, otherwise the earliest defined
					var iter = bestTechniquesByScheme.GetEnumerator();
					if ( iter.Current.Value == null )
					{
						iter.MoveNext();
					}

					lodTechniques = iter.Current.Value;
				}
				else
				{
					lodTechniques = bestTechniquesByScheme[ MaterialManager.Instance.ActiveSchemeIndex ];
				}

				if ( !lodTechniques.ContainsKey( lodIndex ) )
				{
					while ( lodIndex >= 0 || !lodTechniques.ContainsKey( lodIndex ) )
					{
						lodIndex--;
					}

					if ( lodIndex >= 0 )
					{
						technique = lodTechniques[ lodIndex ];
					}
				}
				else
				{
					technique = lodTechniques[ lodIndex ];
				}
			}
			return technique;
		}

		#endregion GetBestTechnique Method

		#endregion Methods

		#region Material Schemes

		#endregion Material Schemes

		#region Material Level of Detail

		/// <summary>
		///	List of LOD distances specified for this material.
		/// </summary>
		/// <ogre name="mLodDistances" />
		private readonly LodValueList _lodValues = new LodValueList();

		/// <summary>
		/// Gets an iterator over the list of values at which each LOD comes into effect.
		/// </summary>
		/// <remarks>
		/// Note that the values returned from this method is not totally analogous to
		/// the one passed in by calling <see cref="SetLodLevels"/> - the list includes a zero
		/// entry at the start (since the highest LOD starts at value 0). Also, the
		/// values returned are after being transformed by <see name="LodStrategy.TransformUserValue"/>.
		/// </remarks>
		public LodValueList LodValues
		{
			get
			{
				return _lodValues;
			}
		}

		/// <summary>
		///	List of LOD distances specified for this material.
		/// </summary>
		/// <ogre name="mUserLodDistances" />
		protected LodValueList UserLodValues = new LodValueList();

		/// <summary>
		/// The LOD stategy for this material
		/// </summary>
		/// <ogre name="mLodStrategy" />
		private LodStrategy _lodStrategy;

		/// <summary>
		/// The LOD stategy for this material
		/// </summary>
		public LodStrategy LodStrategy
		{
			get
			{
				return _lodStrategy;
			}
			set
			{
				_lodStrategy = value;

				Debug.Assert( _lodValues.Count != 0 );

				_lodValues[ 0 ] = _lodStrategy.BaseValue;

				for ( var index = 0; index < UserLodValues.Count; index++ )
				{
					_lodValues[ index ] = _lodStrategy.TransformUserValue( UserLodValues[ index ] );
				}
			}
		}

		/// <summary>
		///		Gets the number of levels-of-detail this material has.
		/// </summary>
		/// <ogre name="getNumLodLevels" />
		public int LodLevelsCount
		{
			get
			{
				return bestTechniquesByScheme.Count;
			}
		}

		/// <summary>
		///		Sets the distance at which level-of-detail (LOD) levels come into effect.
		/// </summary>
		/// <remarks>
		///		You should only use this if you have assigned LOD indexes to the <see cref="Technique"/>
		///		instances attached to this <see cref="Material"/>. If you have done so, you should call this
		///		method to determine the distance at which the low levels of detail kick in.
		///		The decision about what distance is actually used is a combination of this
		///		and the LOD bias applied to both the current <see cref="Camera"/> and the current Entity.
		/// </remarks>
		/// <param name="lodDistanceList">
		///		A list of floats which indicate the distance at which to
		///		switch to lower details. They are listed in LOD index order, starting at index
		///		1 (ie the first level down from the highest level 0, which automatically applies
		///		from a distance of 0).
		/// </param>
		/// <ogre name="setLoadLevels" />
		public void SetLodLevels( LodValueList lodDistanceList )
		{
			// clear and add the 0 distance entry
			_lodValues.Clear();
			UserLodValues.Clear();
			UserLodValues.Add( float.NaN );
			_lodValues.Add( LodStrategy.BaseValue );

			foreach ( var lodValue in lodDistanceList )
			{
				UserLodValues.Add( lodValue );
				if ( LodStrategy != null )
				{
					_lodValues.Add( LodStrategy.TransformUserValue( lodValue ) );
				}
			}
		}

		/// <summary>
		///	Gets the LOD index to use at the given distance.
		/// </summary>
		/// <remarks>
		/// The value passed in is the 'transformed' value. If you are dealing with
		/// an original source value (e.g. distance), use <see name="LodStrategy.TransformUserValue"/>
		/// to turn this into a lookup value.
		/// </remarks>
		/// <param name="distance"></param>
		/// <returns></returns>
		/// <ogre name="getLodIndex" />
		public int GetLodIndex( Real distance )
		{
			return LodStrategy.GetIndex( distance, _lodValues );
		}

		#endregion Material Level of Detail

		#region IComparable Implementation

		/// <summary>
		///		Used for comparing 2 Material objects.
		/// </summary>
		/// <remarks>
		///		This comparison will be used in RenderQueue group sorting of Materials materials.
		///		If this object is transparent and the object being compared is not, this is greater that obj.
		///		If this object is not transparent and the object being compared is, obj is greater than this.
		/// </remarks>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo( object obj )
		{
			Debug.Assert( obj is Material, "Materials cannot be compared to objects of type '" + obj.GetType().Name );

			var material = obj as Material;

			// compare this Material with the incoming object to compare to.
			if ( IsTransparent && !material.IsTransparent )
			{
				return -1;
			}
			else if ( !IsTransparent && material.IsTransparent )
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}

		#endregion IComparable Implementation

		#region Resource Implementation

		/// <summary>
		///		Overridden from Resource.
		/// </summary>
		/// <remarks>
		///		By default, Materials are not loaded, and adding additional textures etc do not cause those
		///		textures to be loaded. When the <code>Load</code> method is called, all textures are loaded (if they
		///		are not already), GPU programs are created if applicable, and Controllers are instantiated.
		///		Once a material has been loaded, all changes made to it are immediately loaded too
		/// </remarks>
		/// <ogre name="loadImpl" />
		[OgreVersion( 1, 7, 2 )]
		protected override void load()
		{
			// load all the supported techniques
			for ( var i = 0; i < SupportedTechniques.Count; i++ )
			{
				SupportedTechniques[ i ].Load();
			}
		}

		/// <summary>
		///		Unloads the material, frees resources etc.
		///		<see cref="Resource"/>
		/// </summary>
		/// <ogre name="unloadImpl" />
		[OgreVersion( 1, 7, 2 )]
		protected override void unload()
		{
			// unload unsupported techniques
			for ( var i = 0; i < SupportedTechniques.Count; i++ )
			{
				SupportedTechniques[ i ].Unload();
			}
		}

		/// <summary>
		/// Calculate the size of a material; this will only be called after 'load'
		/// </summary>
		/// <returns></returns>
		/// <ogre name="calculateSize" />
		protected override int calculateSize()
		{
			return 0;
		}

		/// <summary>
		///    Overridden to ensure a recompile occurs if needed before use.
		/// </summary>
		public override void Touch()
		{
			if ( compilationRequired )
			{
				Compile();
			}

			// call base class
			base.Touch();
		}

		/// <summary>
		///    Overridden to ensure a release of techniques.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					unload();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Resource Implementation

		#region Object overloads

		/// <summary>
		///    Overridden to give Materials a meaningful hash code.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return _name.GetHashCode();
		}

		/// <summary>
		///    Overridden.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return _name;
		}

		#endregion Object overloads
	}
}