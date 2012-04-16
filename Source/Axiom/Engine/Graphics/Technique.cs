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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Axiom.Core;
using Axiom.Math;
using Axiom.Scripting;

#endregion Namespace Declarations

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
		#region Constants and Enumerations

		/// <summary>
		/// illumination pass state type
		/// </summary>
		protected enum IlluminationPassesCompilationPhase
		{
			Disabled = -1,
			NotCompiled = 0,
			Compiled = 1
		}

		/// <summary>
		/// Rule controlling whether technique is deemed supported based on GPU vendor
		/// </summary>
		public struct GPUVendorRule
		{
			public GPUVendor Vendor { get; set; }
			public bool Include { get; set; }

			public GPUVendorRule( GPUVendor v, bool ie )
				: this()
			{
				Vendor = v;
				Include = ie;
			}

			#region System.Object overrides

			public override bool Equals( object obj )
			{
				if ( obj == null )
				{
					return false;
				}

				if ( !( obj is GPUVendorRule ) )
				{
					return false;
				}

				var v = (GPUVendorRule)obj;

				return ( v.Vendor == Vendor ) && ( v.Include == Include );
			}

			public override int GetHashCode()
			{
				return Vendor.GetHashCode();
			}

			#endregion System.Object overrides

			public static bool operator ==( GPUVendorRule a, GPUVendorRule b )
			{
				if ( Object.ReferenceEquals( a, b ) )
				{
					return true;
				}

				return ( a.Vendor == b.Vendor ) && ( a.Include == b.Include );
			}

			public static bool operator !=( GPUVendorRule a, GPUVendorRule b )
			{
				return !( a == b );
			}
		};

		/// <summary>
		/// Rule controlling whether technique is deemed supported based on GPU device name
		/// </summary>
		public struct GPUDeviceNameRule
		{
			public string DevicePattern;

			public bool Include;

			public bool CaseSensitive;

			public GPUDeviceNameRule( string pattern, bool ie, bool caseSen )
				: this()
			{
				DevicePattern = pattern;
				Include = ie;
				CaseSensitive = caseSen;
			}

			#region System.Object overrides

			public override bool Equals( object obj )
			{
				if ( obj == null )
				{
					return false;
				}

				if ( !( obj is GPUDeviceNameRule ) )
				{
					return false;
				}

				var d = (GPUDeviceNameRule)obj;

				return ( d.DevicePattern == DevicePattern ) && ( d.Include == Include ) && ( d.CaseSensitive == CaseSensitive );
			}

			public override int GetHashCode()
			{
				if ( DevicePattern != null )
				{
					return DevicePattern.GetHashCode();
				}

				return base.GetHashCode();
			}

			#endregion System.Object overrides

			public static bool operator ==( GPUDeviceNameRule a, GPUDeviceNameRule b )
			{
				if ( Object.ReferenceEquals( a, b ) )
				{
					return true;
				}

				return ( a.DevicePattern == b.DevicePattern ) && ( a.Include == b.Include ) && ( a.CaseSensitive == b.CaseSensitive );
			}

			public static bool operator !=( GPUDeviceNameRule a, GPUDeviceNameRule b )
			{
				return !( a == b );
			}
		};

		#endregion Constants and Enumerations

		#region Fields and Properties

		/// <summary>
		///    The list of passes (fixed function or programmable) contained in this technique.
		/// </summary>
		private List<Pass> _passes = new List<Pass>();

		protected List<GPUVendorRule> _GPUVendorRules = new List<GPUVendorRule>();
		protected List<GPUDeviceNameRule> _GPUDeviceNameRules = new List<GPUDeviceNameRule>();

		#region IlluminationPasses Property

		private IlluminationPassesCompilationPhase _illuminationPassesCompilationPhase = IlluminationPassesCompilationPhase.NotCompiled;

		/// <summary>
		///		List of derived passes, categorized (and ordered) into illumination stages.
		/// </summary>
		private List<IlluminationPass> _illuminationPasses = new List<IlluminationPass>();

		public IEnumerable<IlluminationPass> IlluminationPasses
		{
			get
			{
				var targetState = IlluminationPassesCompilationPhase.Compiled;
				if ( _illuminationPassesCompilationPhase != targetState )
				{
					// prevents parent->_notifyNeedsRecompile() call during compile
					_illuminationPassesCompilationPhase = IlluminationPassesCompilationPhase.Disabled;
					// Splitting the passes into illumination passes
					CompileIlluminationPasses();
					// Mark that illumination passes compilation finished
					_illuminationPassesCompilationPhase = targetState;
				}

				return _illuminationPasses;
			}
		}

		#endregion IlluminationPasses Property

		#region Parent Property

		/// <summary>
		///    The material that owns this technique.
		/// </summary>
		private Material _parent;

		/// <summary>
		///    Gets a reference to the Material that owns this Technique.
		/// </summary>
		public Material Parent
		{
			get
			{
				return _parent;
			}
			protected set
			{
				_parent = value;
			}
		}

		#endregion Parent Property

		#region IsSupported Property

		/// <summary>
		///    Flag that states whether or not this technique is supported on the current hardware.
		/// </summary>
		private bool _isSupported;

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
				return _isSupported;
			}
			protected set
			{
				_isSupported = value;
			}
		}

		#endregion IsSupported Property

		#region Name Property

		/// <summary>
		///    Name of this technique.
		/// </summary>
		private string _name;

		/// <summary>
		///    Gets/Sets the name of this technique.
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		#endregion Name Property

		#region LodIndex Property

		/// <summary>
		///		Level of detail index for this technique.
		/// </summary>
		private int _lodIndex;

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
				return _lodIndex;
			}
			set
			{
				_lodIndex = value;
				NotifyNeedsRecompile();
			}
		}

		#endregion LodIndex Property

		#region compiledIlluminationPasses Property

		private bool _compiledIlluminationPasses;

		protected bool compiledIlluminationPasses
		{
			get
			{
				return _compiledIlluminationPasses;
			}
			set
			{
				_compiledIlluminationPasses = value;
			}
		}

		#endregion compiledIlluminationPasses Property

		#region IlluminationPassCount Property

		/// <summary>
		///		Gets the number of illumination passes compiled from this technique.
		/// </summary>
		public int IlluminationPassCount
		{
			get
			{
				if ( !_compiledIlluminationPasses )
				{
					CompileIlluminationPasses();
				}
				return _illuminationPasses.Count;
			}
		}

		#endregion IlluminationPassCount Property

		#region Pass Convienence Properties

		/// <summary>
		/// Sets the point size properties for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.PointSize"/>
		/// </remarks>
		public Real PointSize
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.PointSize = value;
				}
			}
		}

		/// <summary>
		/// Sets the ambient size properties for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.Ambient"/>
		/// </remarks>
		public ColorEx Ambient
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.Ambient = value;
				}
			}
		}

		/// <summary>
		/// Sets the Diffuse property for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.Diffuse"/>
		/// </remarks>
		public ColorEx Diffuse
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.Diffuse = value;
				}
			}
		}

		/// <summary>
		/// Sets the Specular property for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.Specular"/>
		/// </remarks>
		public ColorEx Specular
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.Specular = value;
				}
			}
		}

		/// <summary>
		/// Sets the Shininess property for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.Shininess"/>
		/// </remarks>
		public Real Shininess
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.Shininess = value;
				}
			}
		}

		/// <summary>
		/// Sets the SelfIllumination property for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.SelfIllumination"/>
		/// </remarks>
		public ColorEx SelfIllumination
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.SelfIllumination = value;
				}
			}
		}

		/// <summary>
		/// Sets the Emissive property for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.Emissive"/>
		/// </remarks>
		public ColorEx Emissive
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.Emissive = value;
				}
			}
		}

		/// <summary>
		/// Sets the CullingMode property for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.CullingMode"/>
		/// </remarks>
		public CullingMode CullingMode
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.CullingMode = value;
				}
			}
		}

		/// <summary>
		/// Sets the ManualCullingMode property for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.ManualCullingMode"/>
		/// </remarks>
		public ManualCullingMode ManualCullingMode
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.ManualCullingMode = value;
				}
			}
		}

		/// <summary>
		/// Sets whether or not dynamic lighting is enabled for every Pass.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see ref="Pass.LightingEnabled"></see>
		/// </remarks>
		public bool LightingEnabled
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.LightingEnabled = value;
				}
			}
		}

		/// <summary>
		/// Sets the depth bias to be used for each Pass.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see ref="Pass.DepthBias"></see>
		/// </remarks>
		public int DepthBias
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.SetDepthBias( value );
				}
			}
		}

		/// <summary>
		/// Sets the type of light shading required
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see ref="Pass.ShadingMode"></see>
		/// </remarks>
		public Shading ShadingMode
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.ShadingMode = value;
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
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see ref="Pass.SetFog"></see>
		/// </remarks>
		public void SetFog( bool overrideScene, FogMode mode, ColorEx color, Real expDensity, Real linearStart, Real linearEnd )
		{
			// load each technique
			foreach ( var p in _passes )
			{
				p.SetFog( overrideScene, mode, color, expDensity, linearStart, linearEnd );
			}
		}

		/// <summary>
		/// Sets the DepthCheck property for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.DepthCheck"/>
		/// </remarks>
		public bool DepthCheck
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.DepthCheck = value;
				}
			}
			get
			{
				if ( _passes.Count == 0 )
				{
					return false;
				}
				else
				{
					// Base decision on the depth settings of the first pass
					return _passes[ 0 ].DepthCheck;
				}
			}
		}

		/// <summary>
		/// Sets the function used to compare depth values when depth checking is on.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see ref="Pass.DepthFunction"></see>
		/// </remarks>
		public CompareFunction DepthFunction
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.DepthFunction = value;
				}
			}
		}

		/// <summary>
		/// Sets the DepthWrite property for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.DepthWrite"/>
		/// </remarks>
		public bool DepthWrite
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.DepthWrite = value;
				}
			}
			get
			{
				if ( _passes.Count == 0 )
				{
					return false;
				}
				else
				{
					// Base decision on the depth settings of the first pass
					return _passes[ 0 ].DepthWrite;
				}
			}
		}

		/// <summary>
		/// Sets whether or not colour buffer writing is enabled for each Pass.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see ref="Pass.ColorWriteEnabled"></see>
		/// </remarks>
		public bool ColorWriteEnabled
		{
			get
			{
				if ( _passes.Count == 0 )
				{
					return true;
				}
				else
				{
					return _passes[ 0 ].ColorWriteEnabled;
				}
			}
			set
			{
				foreach ( var p in _passes )
				{
					p.ColorWriteEnabled = value;
				}
			}
		}

		/// <summary>
		/// Sets the anisotropy level to be used for all textures.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see ref="Pass.TextureAnisotropy"></see>
		/// </remarks>
		public int TextureAnisotropy
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.TextureAnisotropy = value;
				}
			}
		}

		/// <summary>
		/// Sets the TextureFiltering property for every Pass in this Technique.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// <see cref="Pass.TextureFiltering"/>
		/// </remarks>
		public TextureFiltering TextureFiltering
		{
			set
			{
				foreach ( var p in _passes )
				{
					p.TextureFiltering = value;
				}
			}
		}

		#endregion Pass Convienence Properties

		#region Scheme Property

		private ushort _schemeIndex;
		private string _schemeName;

		public String Scheme
		{
			get
			{
				return _schemeName;
			}

			set
			{
				_schemeName = value;
				_schemeIndex = MaterialManager.Instance.GetSchemeIndex( _schemeName );
			}
		}

		#endregion Scheme Property

		/// <summary>
		///    Returns true if this Technique has already been loaded.
		/// </summary>
		public bool IsLoaded
		{
			get
			{
				return _parent.IsLoaded;
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
				if ( _passes.Count == 0 )
				{
					return false;
				}
				else
				{
					// based on the transparency of the first pass
					return ( (Pass)_passes[ 0 ] ).IsTransparent;
				}
			}
		}

		/// <summary>
		///    Gets the number of passes within this Technique.
		/// </summary>
		public int PassCount
		{
			get
			{
				return _passes.Count;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		public Technique( Material parent )
		{
			this._parent = parent;
			this._compiledIlluminationPasses = false;
		}

		#endregion

		#region Methods

		/// <summary>
		///		Internal method for clearing the illumination pass list.
		/// </summary>
		protected void ClearIlluminationPasses()
		{
			for ( var i = 0; i < _illuminationPasses.Count; i++ )
			{
				var iPass = (IlluminationPass)_illuminationPasses[ i ];

				if ( iPass.DestroyOnShutdown )
				{
					iPass.Pass.QueueForDeletion();
				}
			}

			_illuminationPasses.Clear();
		}

		/// <summary>
		///    Clones this Technique.
		/// </summary>
		/// <param name="parent">Material that will own this technique.</param>
		/// <returns></returns>
		public Technique Clone( Material parent )
		{
			var newTechnique = new Technique( parent );

			CopyTo( newTechnique );

			return newTechnique;
		}

		/// <summary>
		///		Copy the details of this Technique to another.
		/// </summary>
		/// <param name="target"></param>
		public void CopyTo( Technique target )
		{
			target._isSupported = _isSupported;
			target.SchemeIndex = SchemeIndex;
			target._lodIndex = _lodIndex;

			target.RemoveAllPasses();

			// clone each pass and add that to the new technique
			for ( var i = 0; i < _passes.Count; i++ )
			{
				var pass = _passes[ i ];
				var newPass = pass.Clone( target, pass.Index );
				target._passes.Add( newPass );
			}

			// Compile for categorized illumination on demand
			ClearIlluminationPasses();
			_illuminationPassesCompilationPhase = IlluminationPassesCompilationPhase.NotCompiled;
		}

		/// <summary>
		///    Compilation method for Techniques.  See <see cref="Axiom.Graphics.Material"/>
		/// </summary>
		/// <param name="autoManageTextureUnits">
		///    Determines whether or not the engine should split up extra texture unit requests
		///    into extra passes if the hardware does not have enough available units.
		/// </param>
		[OgreVersion( 1, 7, 2 )]
		internal String Compile( bool autoManageTextureUnits )
		{
			var compileErrors = new StringBuilder();

			_isSupported = CheckGPURules( ref compileErrors );
			if ( _isSupported )
			{
				_isSupported = CheckHardwareSupport( autoManageTextureUnits, ref compileErrors );
			}

			// Compile for categorised illumination on demand
			ClearIlluminationPasses();
			_illuminationPassesCompilationPhase = IlluminationPassesCompilationPhase.NotCompiled;

			return compileErrors.ToString();
		}

		/// <summary>
		/// Internal method for checking hardware support
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		internal bool CheckHardwareSupport( bool autoManageTextureUnits, ref StringBuilder compileErrors )
		{
			// Go through each pass, checking requirements
			ushort passNum = 0;
			RenderSystemCapabilities caps = Root.Instance.RenderSystem.Capabilities;
			int numTexUnits = caps.TextureUnitCount;
			for ( int i = 0; i < _passes.Count; ++i )
			{
				// Adjust pass index
				_passes[ i ].Index = passNum;

				//Check for advanced blending operation support
				if ( ( _passes[ i ].SceneBlendingOperation != SceneBlendOperation.Add || _passes[ i ].SceneBlendingOperationAlpha != SceneBlendOperation.Add ) && !caps.HasCapability( Capabilities.AdvancedBlendOperations ) )
				{
					return false;
				}

				// Check texture unit requirements
				int numTexUnitsRequested = _passes[ i ].TextureUnitStatesCount;
				// Don't trust getNumTextureUnits for programmable
				if ( !_passes[ i ].HasFragmentProgram )
				{
#if OGRE_PRETEND_TEXTURE_UNITS //&& OGRE_PRETEND_TEXTURE_UNITS > 0
				if (numTexUnits > OGRE_PRETEND_TEXTURE_UNITS)
					numTexUnits = OGRE_PRETEND_TEXTURE_UNITS;
#endif
					if ( numTexUnitsRequested > numTexUnits )
					{
						if ( !autoManageTextureUnits )
						{
							// The user disabled auto pass split
							compileErrors.AppendLine( "Pass " + passNum + ": Too many texture units for the current hardware and no splitting allowed." );
							return false;
						}
						else if ( _passes[ i ].HasVertexProgram )
						{
							// Can't do this one, and can't split a programmable pass
							compileErrors.AppendLine( "Pass " + passNum + ": Too many texture units for the current hardware and cannot split programmable passes." );
							return false;
						}
					}
				}

				if ( _passes[ i ].HasVertexProgram )
				{
					// Check vertex program version
					if ( !_passes[ i ].VertexProgram.IsSupported )
					{
						// Can't do this one
						compileErrors.Append( "Pass " + passNum + ": Vertex program " + _passes[ i ].VertexProgram.Name + " cannot be used - " );

						if ( _passes[ i ].VertexProgram.HasCompileError )
						{
							compileErrors.Append( "compile error." );
						}
						else
						{
							compileErrors.Append( "not supported." );
						}

						compileErrors.AppendLine();
						return false;
					}
				}

				if ( _passes[ i ].HasGeometryProgram )
				{
					// Check geometry program version
					if ( !_passes[ i ].GeometryProgram.IsSupported )
					{
						// Can't do this one
						compileErrors.Append( "Pass " + passNum + ": Geometry program " + _passes[ i ].GeometryProgram.Name + " cannot be used - " );
						if ( _passes[ i ].GeometryProgram.HasCompileError )
						{
							compileErrors.Append( "compile error." );
						}
						else
						{
							compileErrors.Append( "not supported." );
						}

						compileErrors.AppendLine();
						return false;
					}
				}

				if ( _passes[ i ].HasFragmentProgram )
				{
					// Check fragment program version
					if ( !_passes[ i ].FragmentProgram.IsSupported )
					{
						// Can't do this one
						compileErrors.Append( "Pass " + passNum + ": Fragment program " + _passes[ i ].FragmentProgram.Name + " cannot be used - " );
						if ( _passes[ i ].FragmentProgram.HasCompileError )
						{
							compileErrors.Append( "compile error." );
						}
						else
						{
							compileErrors.Append( "not supported." );
						}

						compileErrors.AppendLine();
						return false;
					}
				}
				else
				{
					// Check a few fixed-function options in texture layers
					int texUnit = 0;

					for ( int t = 0; t < _passes[ i ].TextureUnitStatesCount; ++t )
					{
						TextureUnitState tex = _passes[ i ].GetTextureUnitState( t );
						// Any Cube textures? NB we make the assumption that any
						// card capable of running fragment programs can support
						// cubic textures, which has to be true, surely?
						if ( tex.Is3D && !caps.HasCapability( Capabilities.CubeMapping ) )
						{
							// Fail
							compileErrors.AppendLine( "Pass " + passNum + " Tex " + texUnit + ": Cube maps not supported by current environment." );
							return false;
						}
						// Any 3D textures? NB we make the assumption that any
						// card capable of running fragment programs can support
						// 3D textures, which has to be true, surely?
						if ( tex.TextureType == TextureType.ThreeD && !caps.HasCapability( Capabilities.Texture3D ) )
						{
							// Fail
							compileErrors.AppendLine( "Pass " + passNum + " Tex " + texUnit + ": Volume textures not supported by current environment." );
							return false;
						}
						// Any Dot3 blending?
						if ( tex.ColorBlendMode.operation == LayerBlendOperationEx.DotProduct && !caps.HasCapability( Capabilities.Dot3 ) )
						{
							// Fail
							compileErrors.AppendLine( "Pass " + passNum + " Tex " + texUnit + ": DOT3 blending not supported by current environment." );
							return false;
						}
						++texUnit;
					}

					// We're ok on operations, now we need to check # texture units
					if ( !_passes[ i ].HasFragmentProgram )
					{
						// Keep splitting this pass so long as units requested > gpu units
						while ( numTexUnitsRequested > numTexUnits )
						{
							// chop this pass into many passes
							_passes[ i ] = _passes[ i ].Split( numTexUnits );
							numTexUnitsRequested = _passes[ i ].TextureUnitStatesCount;
							// Advance pass number
							++passNum;
							// Reset iterator
							i = passNum;
							// Move the new pass to the right place (will have been created
							// at the end, may be other passes in between)
							Debug.Assert( _passes[ _passes.Count - 1 ] == _passes[ i ] );

							//std::copy_backward(i, (mPasses.end()-1), mPasses.end());
							int result = _passes.Count;
							int last = result - 1;

							while ( last != i )
							{
								_passes[ --result ] = _passes[ --last ];
							}

							// Adjust pass index
							_passes[ i ].Index = passNum;
						}
					}
				}
			}
			// If we got this far, we're ok
			return true;
		}

		/// <summary>
		/// Internal method for checking GPU vendor / device rules
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		internal bool CheckGPURules( ref StringBuilder errors )
		{
			RenderSystemCapabilities caps = Root.Instance.RenderSystem.Capabilities;

			var includeRules = new StringBuilder();
			bool includeRulesPresent = false;
			bool includeRuleMatched = false;

			// Check vendors first
			foreach ( var i in _GPUVendorRules )
			{
				var vendorName = ScriptEnumAttribute.GetScriptAttribute( (int)caps.Vendor, typeof ( GPUVendor ) );

				if ( i.Include )
				{
					includeRulesPresent = true;
					includeRules.Append( vendorName + " " );
					if ( i.Vendor == caps.Vendor )
					{
						includeRuleMatched = true;
					}
				}
				else // EXCLUDE
				{
					if ( i.Vendor == caps.Vendor )
					{
						errors.AppendLine( "Excluded GPU vendor: " + vendorName );
						return false;
					}
				}
			}

			if ( includeRulesPresent && !includeRuleMatched )
			{
				errors.AppendLine( "Failed to match GPU vendor: " + includeRules.ToString() );
				return false;
			}

			// now check device names
			includeRules.Length = 0;
			includeRulesPresent = false;
			includeRuleMatched = false;

			foreach ( var i in _GPUDeviceNameRules )
			{
				//StringUtil::match(caps->getDeviceName(), i->devicePattern, i->caseSensitive)
				bool checkCondition = i.CaseSensitive ? i.DevicePattern.Contains( caps.DeviceName ) : i.DevicePattern.ToLower().Contains( caps.DeviceName.ToLower() );

				if ( i.Include )
				{
					includeRulesPresent = true;
					includeRules.Append( i.DevicePattern + " " );
					if ( checkCondition )
					{
						includeRuleMatched = true;
					}
				}
				else // EXCLUDE
				{
					if ( checkCondition )
					{
						errors.AppendLine( "Excluded GPU device: " + i.DevicePattern );
						return false;
					}
				}
			}

			if ( includeRulesPresent && !includeRuleMatched )
			{
				errors.AppendLine( "Failed to match GPU device: " + includeRules.ToString() );
				return false;
			}

			// passed
			return true;
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
			var stage = IlluminationStage.Ambient;

			var hasAmbient = false;

			for ( var i = 0; i < _passes.Count; /* increment in logic */ )
			{
				var pass = (Pass)_passes[ i ];
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
							_illuminationPasses.Add( iPass );
							hasAmbient = true;

							// progress to the next pass
							i++;
						}
						else
						{
							// split off any ambient part
							if ( pass.Ambient.CompareTo( ColorEx.Black ) != 0 || pass.Emissive.CompareTo( ColorEx.Black ) != 0 )
							{
								var newPass = new Pass( this, pass.Index );
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
								if ( newPass.Ambient.CompareTo( ColorEx.Black ) == 0 && newPass.Emissive.CompareTo( ColorEx.Black ) == 0 )
								{
									newPass.ColorWriteEnabled = false;
								}

								iPass = new IlluminationPass();
								iPass.DestroyOnShutdown = true;
								iPass.OriginalPass = pass;
								iPass.Pass = newPass;
								iPass.Stage = stage;

								_illuminationPasses.Add( iPass );
								hasAmbient = true;
							}

							if ( !hasAmbient )
							{
								// make up a new basic pass
								var newPass = new Pass( this, pass.Index );
								pass.CopyTo( newPass );

								newPass.Ambient = ColorEx.Black;
								newPass.Diffuse = ColorEx.Black;

								iPass = new IlluminationPass();
								iPass.DestroyOnShutdown = true;
								iPass.OriginalPass = pass;
								iPass.Pass = newPass;
								iPass.Stage = stage;
								_illuminationPasses.Add( iPass );
								hasAmbient = true;
							}

							// this means we are done with ambients, progress to per-light
							stage = IlluminationStage.PerLight;
						}

						break;

					case IlluminationStage.PerLight:
						if ( pass.IteratePerLight )
						{
							// if this is per-light already, use it directly
							iPass = new IlluminationPass();
							iPass.DestroyOnShutdown = false;
							iPass.OriginalPass = pass;
							iPass.Pass = pass;
							iPass.Stage = stage;
							_illuminationPasses.Add( iPass );

							// progress to the next pass
							i++;
						}
						else
						{
							// split off per-light details (can only be done for one)
							if ( pass.LightingEnabled && ( pass.Diffuse.CompareTo( ColorEx.Black ) != 0 || pass.Specular.CompareTo( ColorEx.Black ) != 0 ) )
							{
								// copy existing pass
								var newPass = new Pass( this, pass.Index );
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

								_illuminationPasses.Add( iPass );
							}

							// This means the end of per-light passes
							stage = IlluminationStage.Decal;
						}

						break;

					case IlluminationStage.Decal:
						// We just want a 'lighting off' pass to finish off
						// and only if there are texture units
						if ( pass.TextureUnitStatesCount > 0 )
						{
							if ( !pass.LightingEnabled )
							{
								// we assume this pass already combines as required with the scene
								iPass = new IlluminationPass();
								iPass.DestroyOnShutdown = false;
								iPass.OriginalPass = pass;
								iPass.Pass = pass;
								iPass.Stage = stage;
								_illuminationPasses.Add( iPass );
							}
							else
							{
								// Copy the pass and tweak away the lighting parts
								var newPass = new Pass( this, pass.Index );
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
								_illuminationPasses.Add( iPass );
							}
						}

						// always increment on decal, since nothing more to do with this pass
						i++;

						break;
				}
			}

			_compiledIlluminationPasses = true;
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
		/// <returns>A new Pass object reference.</returns>
		public Pass CreatePass()
		{
			var pass = new Pass( this, _passes.Count );
			_passes.Add( pass );
			return pass;
		}

		/// <summary>
		///    Retreives the Pass by name.
		/// </summary>
		/// <param name="passName">Name of the Pass to retreive.</param>
		public Pass GetPass( string passName )
		{
			foreach ( var pass in _passes )
			{
				if ( pass.Name == passName )
				{
					return pass;
				}
			}
			return null;
		}

		/// <summary>
		///    Retreives the Pass at the specified index.
		/// </summary>
		/// <param name="index">Index of the Pass to retreive.</param>
		public Pass GetPass( int index )
		{
			Debug.Assert( index < _passes.Count, "index < passes.Count" );

			return (Pass)_passes[ index ];
		}

		/// <summary>
		///    Retreives the IlluminationPass at the specified index.
		/// </summary>
		/// <param name="index">Index of the IlluminationPass to retreive.</param>
		public IlluminationPass GetIlluminationPass( int index )
		{
			if ( !_compiledIlluminationPasses )
			{
				CompileIlluminationPasses();
			}

			Debug.Assert( index < _illuminationPasses.Count, "index < illuminationPasses.Count" );

			return (IlluminationPass)_illuminationPasses[ index ];
		}

		/// <summary>
		///    Preloads resources required by this Technique.  This is the 
		///    portion that is safe to do from a thread other than the 
		///    main render thread.
		/// </summary>
		public void Preload()
		{
			Debug.Assert( _isSupported, "This technique is not supported." );

			// load each pass
			for ( var i = 0; i < _passes.Count; i++ )
			{
				( (Pass)_passes[ i ] ).Load();
			}
		}

		/// <summary>
		///    Loads resources required by this Technique.
		/// </summary>
		public void Load()
		{
			Debug.Assert( _isSupported, "This technique is not supported." );

			// load each pass
			for ( var i = 0; i < _passes.Count; i++ )
			{
				( (Pass)_passes[ i ] ).Load();
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
			if ( _illuminationPassesCompilationPhase != IlluminationPassesCompilationPhase.Disabled )
			{
				_parent.NotifyNeedsRecompile();
			}
		}

		/// <summary>
		///    Removes the specified Pass from this Technique.
		/// </summary>
		/// <param name="pass">A reference to the Pass to be removed.</param>
		public void RemovePass( Pass pass )
		{
			Debug.Assert( pass != null, "pass != null" );

			pass.QueueForDeletion();

			_passes.Remove( pass );
		}

		/// <summary>
		///		Removes all passes from this technique and queues them for deletion.
		/// </summary>
		public void RemoveAllPasses()
		{
			// load each pass
			for ( var i = 0; i < _passes.Count; i++ )
			{
				var pass = (Pass)_passes[ i ];
				pass.QueueForDeletion();
			}

			_passes.Clear();
		}

		/// <summary>
		/// Sets the kind of blending every pass has with the existing contents of the scene.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// </remarks>
		/// <see cref="Pass.SetSceneBlending"/>
		[OgreVersion( 1, 7, 2 )]
		public void SetSceneBlending( SceneBlendType blendType )
		{
			// load each pass
			for ( var i = 0; i < _passes.Count; i++ )
			{
				_passes[ i ].SetSceneBlending( blendType );
			}
		}

		/// <summary>
		/// Sets the kind of blending every pass has with the existing contents of the scene, using individual factors both color and alpha channels
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// </remarks>
		/// <see cref="Pass.SetSeparateSceneBlending"/>
		[OgreVersion( 1, 7, 2 )]
		public void SetSeparateSceneBlending( SceneBlendType sbt, SceneBlendType sbta )
		{
			for ( var i = 0; i < _passes.Count; i++ )
			{
				_passes[ i ].SetSeparateSceneBlending( sbt, sbta );
			}
		}

		/// <summary>
		/// Allows very fine control of blending every Pass with the existing contents of the scene.
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		///</remarks>
		/// <see cref="Pass.SetSceneBlending"/>
		[OgreVersion( 1, 7, 2 )]
		public void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
		{
			// load each pass
			for ( var i = 0; i < _passes.Count; i++ )
			{
				_passes[ i ].SetSceneBlending( src, dest );
			}
		}

		/// <summary>
		/// Allows very fine control of blending every Pass with the existing contents of the scene, using individual factors both color and alpha channels
		/// </summary>
		/// <remarks>
		/// This property actually exists on the Pass class. For simplicity, this method allows 
		/// you to set these properties for every current Pass within this Technique. If 
		/// you need more precision, retrieve the Pass instance and set the
		/// property there.
		/// </remarks>
		/// <see cref="Pass.SetSeparateSceneBlending"/>
		[OgreVersion( 1, 7, 2 )]
		public void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha )
		{
			for ( var i = 0; i < _passes.Count; i++ )
			{
				_passes[ i ].SetSeparateSceneBlending( sourceFactor, destFactor, sourceFactorAlpha, destFactorAlpha );
			}
		}

		public bool ApplyTextureAliases( Dictionary<string, string> aliasList, bool apply )
		{
			// iterate through all passes and apply texture aliases
			var testResult = false;

			foreach ( var p in _passes )
			{
				if ( p.ApplyTextureAliases( aliasList, apply ) )
				{
					testResult = true;
				}
			}

			return testResult;
		}

		/// <summary>
		///    Unloads resources used by this Technique.
		/// </summary>
		public void Unload()
		{
			// load each pass
			for ( var i = 0; i < _passes.Count; i++ )
			{
				( (Pass)_passes[ i ] ).Unload();
			}
		}

		#endregion

		#region Material Schemes

		/// <summary>
		/// Get/Set the 'scheme name' for this technique. 
		/// </summary>
		/// <remarks>
		/// Material schemes are used to control top-level switching from one
		/// set of techniques to another. For example, you might use this to 
		/// define 'high', 'medium' and 'low' complexity levels on materials
		/// to allow a user to pick a performance / quality ratio. Another
		/// possibility is that you have a fully HDR-enabled pipeline for top
		/// machines, rendering all objects using unclamped shaders, and a 
		/// simpler pipeline for others; this can be implemented using 
		/// schemes.
		/// <para>
		/// Every technique belongs to a scheme - if you don't specify one, the
		/// Technique belongs to the scheme called 'Default', which is also the
		/// scheme used to render by default. The active scheme is set one of
		/// two ways - either by calling <see ref="Viewport.MaterialScheme" />, or
		/// by manually calling <see ref="MaterialManager.ActiveScheme" />.
		/// </para>
		/// </remarks>
		public String SchemeName
		{
			get
			{
				return MaterialManager.Instance.GetSchemeName( SchemeIndex );
			}
			set
			{
				SchemeIndex = MaterialManager.Instance.GetSchemeIndex( value );
				this.NotifyNeedsRecompile();
			}
		}

		/// <summary>
		/// The material scheme index
		/// </summary>
		public ushort SchemeIndex { get; protected set; }

		#endregion

		#region Shadow Materials

		private Material _shadowCasterMaterial;
		private string _shadowCasterMaterialName = string.Empty;

		public Material ShadowCasterMaterial
		{
			get
			{
				return _shadowCasterMaterial;
			}
			set
			{
				if ( value != null )
				{
					_shadowCasterMaterial = value;
					_shadowCasterMaterialName = _shadowCasterMaterial.Name;
				}
				else
				{
					_shadowCasterMaterial = null;
					_shadowCasterMaterialName = String.Empty;
				}
			}
		}

		public void SetShadowCasterMaterial( string name )
		{
			_shadowCasterMaterialName = name;
			_shadowCasterMaterial = (Material)MaterialManager.Instance[ name ];
		}


		private Material _shadowReceiverMaterial;
		private string _shadowReceiverMaterialName = string.Empty;

		public Material ShadowReceiverMaterial
		{
			get
			{
				return _shadowReceiverMaterial;
			}
			set
			{
				if ( value != null )
				{
					_shadowReceiverMaterial = value;
					_shadowReceiverMaterialName = _shadowReceiverMaterial.Name;
				}
				else
				{
					_shadowReceiverMaterial = null;
					_shadowReceiverMaterialName = String.Empty;
				}
			}
		}

		public void SetShadowReceiverMaterial( string name )
		{
			_shadowReceiverMaterialName = name;
			_shadowReceiverMaterial = (Material)MaterialManager.Instance[ name ];
		}

		#endregion

		/// <summary>
		/// Add a rule which manually influences the support for this technique based
		/// on a GPU vendor.
		/// </summary>
		/// <remarks>
		/// You can use this facility to manually control whether a technique is
		/// considered supported, based on a GPU vendor. You can add inclusive
		/// or exclusive rules, and you can add as many of each as you like. If
		///	at least one inclusive rule is added, a	technique is considered 
		///	unsupported if it does not match any of those inclusive rules. If exclusive rules are
		///	added, the technique is considered unsupported if it matches any of
		///	those inclusive rules.
		///	Note that any rule for the same vendor will be removed before adding this one.
		///	/// </remarks>
		/// <param name="rule"></param>
		internal void AddGPUVenderRule( GPUVendorRule rule )
		{
			// remove duplicates
			RemoveGPUVendorRule( rule );
			_GPUVendorRules.Add( rule );
		}

		/// <summary>
		/// Removes a matching vendor rule.
		/// </summary>
		/// <see cref="AddGPUVenderRule"/>
		/// <param name="rule"></param>
		internal void RemoveGPUVendorRule( GPUVendorRule rule )
		{
			if ( _GPUVendorRules.Contains( rule ) )
			{
				_GPUVendorRules.Remove( rule );
			}
		}

		/// <summary>
		/// Add a rule which manually influences the support for this technique based
		///	on a pattern that matches a GPU device name (e.g. '*8800*').
		/// </summary>
		/// <remarks>
		/// You can use this facility to manually control whether a technique is
		///	considered supported, based on a GPU device name pattern. You can add inclusive
		///	or exclusive rules, and you can add as many of each as you like. If
		///	at least one inclusive rule is added, a	technique is considered 
		///	unsupported if it does not match any of those inclusive rules. If exclusive rules are
		///	added, the technique is considered unsupported if it matches any of
		///	those inclusive rules. The pattern you supply can include wildcard
		///	characters ('*') if you only want to match part of the device name.
		///	Note that any rule for the same device pattern will be removed before adding this one.
		/// </remarks>
		/// <param name="rule"></param>
		internal void AddGPUDeviceNameRule( GPUDeviceNameRule rule )
		{
			// remove duplicates
			RemoveGPUDeviceNameRule( rule );
			_GPUDeviceNameRules.Add( rule );
		}

		/// <summary>
		/// Removes a matching device name rule.
		/// </summary>
		/// <see cref="AddGPUDeviceNameRule"/>
		/// <param name="rule"></param>
		internal void RemoveGPUDeviceNameRule( GPUDeviceNameRule rule )
		{
			if ( _GPUDeviceNameRules.Contains( rule ) )
			{
				_GPUDeviceNameRules.Remove( rule );
			}
		}
	}
}
