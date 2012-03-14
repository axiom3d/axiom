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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;

using Axiom.Collections;
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Serialization;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///     Class for managing Material settings.
	/// </summary>
	/// <remarks>
	///     Materials control the eventual surface rendering properties of geometry. This class
	///     manages the library of materials, dealing with programmatic registrations and lookups,
	///     as well as loading predefined Material settings from scripts.
	///     <p/>
	///     When loaded from a script, a Material is in an 'unloaded' state and only stores the settings
	///     required. It does not at that stage load any textures. This is because the material settings may be
	///     loaded 'en masse' from bulk material script files, but only a subset will actually be required.
	///     <p/>
	///     Because this is a subclass of ResourceManager, any files loaded will be searched for in any path or
	///     archive added to the resource paths/archives. See ResourceManager for details.
	///     <p/>
	///     For a definition of the material script format, see <a href="http://www.ogre3d.org/docs/manual/manual_16.html#SEC25">here</a>.
	/// </remarks>
	/// 
	/// <ogre name="MaterialManager">
	///     <file name="OgreMaterialManager.h"   revision="" lastUpdated="6/19/2006" lastUpdatedBy="Borrillis" />
	///     <file name="OgreMaterialManager.cpp" revision="" lastUpdated="6/19/2006" lastUpdatedBy="Borrillis" />
	/// </ogre> 
	/// 
	public class MaterialManager : ResourceManager, ISingleton<MaterialManager>
	{
		#region Delegates

		public delegate Technique SchemeNotFoundHandler( SchemeNotFoundEventArgs args );

		#endregion

		#region Fields and Properties

		/// <summary>
		///		Used for parsing material scripts.
		/// </summary>
		private readonly MaterialSerializer _serializer = new MaterialSerializer();

		/// <summary>
		///     Default Texture filtering - magnification.
		/// </summary>
		private FilterOptions _defaultMagFilter;

		/// <summary>
		///     Default Texture filtering - minification.
		/// </summary>
		private FilterOptions _defaultMinFilter;

		/// <summary>
		///     Default Texture filtering - mipmapping.
		/// </summary>
		private FilterOptions _defaultMipFilter;

		private TextureFiltering _filtering;

		#region DefaultAnisotropy Property

		/// <summary>
		///    Sets the default anisotropy level to be used for loaded textures, for when textures are
		///    loaded automatically (e.g. by Material class) or when 'Load' is called with the default
		///    parameters by the application.
		/// </summary>
		public int DefaultAnisotropy { get; set; }

		#endregion DefaultAnisotropy Property

		#endregion Fields and Properties

		#region Constructors and Destructor

		/// <summary>
		/// private constructor.  This class cannot be instantiated externally.
		/// </summary>
		public MaterialManager()
		{
			if ( instance == null )
			{
				instance = this;
			}

			SetDefaultTextureFiltering( TextureFiltering.Bilinear );
			this.DefaultAnisotropy = 1;

			// Loading order
			LoadingOrder = 100.0f;

#if !AXIOM_USENEWCOMPILERS
			// Scripting is supported by this manager
			ScriptPatterns.Add( "*.program" );
			ScriptPatterns.Add( "*.material" );
			ResourceGroupManager.Instance.RegisterScriptLoader( this );
#endif
			// AXIOM_USENEWCOMPILERS
			// Material Schemes
			ActiveScheme = DefaultSchemeName;
			ActiveSchemeIndex = 0;
			//_schemes.Add(_activeSchemeName, _activeSchemeIndex);
			GetSchemeIndex( ActiveScheme );

			// Resource type
			ResourceType = "Material";

			// Register with resource group manager
			ResourceGroupManager.Instance.RegisterResourceManager( ResourceType, this );
		}

		#endregion Constructors and Destructor

		#region Methods

		/// <summary>
		///     Sets up default materials and parses all material scripts.
		/// </summary>
		public void Initialize()
		{
			// Set up default material - don't use name constructor as we want to avoid applying defaults
			Material.defaultSettings = (Material)Create( "DefaultSettings", ResourceGroupManager.DefaultResourceGroupName );
			// Add a single technique and pass, non-programmable
			Material.defaultSettings.CreateTechnique().CreatePass();

			// Set the default lod strategy
			Material.defaultSettings.LodStrategy = LodStrategyManager.Instance.DefaultStrategy;

			// create the default BaseWhite materials
			Create( "BaseWhite", ResourceGroupManager.DefaultResourceGroupName );

			// create the default BaseWhiteNoLighting Material
			( (Material)Create( "BaseWhiteNoLighting", ResourceGroupManager.DefaultResourceGroupName ) ).Lighting = false;
		}

		#region SetDefaultTextureFiltering Method

		/// <overload>
		/// <summary>
		///     Sets the default texture filtering to be used for loaded textures, for when textures are
		///     loaded automatically (e.g. by Material class) or when 'load' is called with the default
		///     parameters by the application.
		/// </summary>
		/// </overload> 
		/// <param name="filtering"></param>
		public virtual void SetDefaultTextureFiltering( TextureFiltering filtering )
		{
			this._filtering = filtering;
			switch ( filtering )
			{
				case TextureFiltering.None:
					SetDefaultTextureFiltering( FilterOptions.Point, FilterOptions.Point, FilterOptions.None );
					break;
				case TextureFiltering.Bilinear:
					SetDefaultTextureFiltering( FilterOptions.Linear, FilterOptions.Linear, FilterOptions.Point );
					break;
				case TextureFiltering.Trilinear:
					SetDefaultTextureFiltering( FilterOptions.Linear, FilterOptions.Linear, FilterOptions.Linear );
					break;
				case TextureFiltering.Anisotropic:
					SetDefaultTextureFiltering( FilterOptions.Anisotropic, FilterOptions.Anisotropic, FilterOptions.Linear );
					break;
			}
		}

		/// <param name="type">Type to configure.</param>
		/// <param name="options">Options to set for the specified type.</param>
		public virtual void SetDefaultTextureFiltering( FilterType type, FilterOptions options )
		{
			switch ( type )
			{
				case FilterType.Min:
					this._defaultMinFilter = options;
					break;

				case FilterType.Mag:
					this._defaultMagFilter = options;
					break;

				case FilterType.Mip:
					this._defaultMipFilter = options;
					break;
			}
		}

		/// <param name="minFilter">Minification filter.</param>
		/// <param name="magFilter">Magnification filter.</param>
		/// <param name="mipFilter">Map filter.</param>
		public virtual void SetDefaultTextureFiltering( FilterOptions minFilter, FilterOptions magFilter, FilterOptions mipFilter )
		{
			this._defaultMinFilter = minFilter;
			this._defaultMagFilter = magFilter;
			this._defaultMipFilter = mipFilter;
		}

		#endregion SetDefaultTextureFiltering Method

		#region GetDefaultTextureFiltering Method

		/// <summary>
		///     Gets the default texture filtering options for the specified filter type.
		/// </summary>
		/// <param name="type">Filter type to get options for.</param>
		/// <returns></returns>
		public virtual FilterOptions GetDefaultTextureFiltering( FilterType type )
		{
			switch ( type )
			{
				case FilterType.Min:
					return this._defaultMinFilter;

				case FilterType.Mag:
					return this._defaultMagFilter;

				case FilterType.Mip:
					return this._defaultMipFilter;
			}

			// make the compiler happy
			return FilterOptions.None;
		}

		/// <summary>
		///     Gets the default texture filtering options.
		/// </summary>
		public virtual TextureFiltering GetDefaultTextureFiltering()
		{
			return this._filtering;
		}

		#endregion GetDefaultTextureFiltering Method

		#endregion Methods

		#region Material Schemes

		public static string DefaultSchemeName = "Default";
		private readonly MultiMap<string, SchemeNotFoundHandler> _listenerMap = new MultiMap<string, SchemeNotFoundHandler>();

		protected readonly Dictionary<String, ushort> _schemes = new Dictionary<String, ushort>();
		protected ushort _activeSchemeIndex;
		protected String _activeSchemeName;

		/// <summary>
		/// The active scheme index. 
		/// </summary>
		/// <seealso ref="Technique.SchemeIndex"/>
		public ushort ActiveSchemeIndex { get; protected set; }

		/// <summary>
		/// The name of the active material scheme. 
		/// </summary>
		/// <seealso ref="Technique.SchemeName"/>
		public String ActiveScheme
		{
			get
			{
				return this._activeSchemeName;
			}
			set
			{
				ActiveSchemeIndex = GetSchemeIndex( value );
				this._activeSchemeName = value;
			}
		}

		/// <summary>
		/// The index for the given material scheme name. 
		/// </summary>
		/// <seealso ref="Technique.SchemeName"/>
		public ushort GetSchemeIndex( String name )
		{
			if ( !this._schemes.ContainsKey( name ) )
			{
				this._schemes.Add( name, (ushort)this._schemes.Count );
			}

			return this._schemes[ name ];
		}


		/// <summary>
		/// The name for the given material scheme index. 
		/// </summary>
		/// <seealso ref="Technique.SchemeName"/>
		public String GetSchemeName( ushort index )
		{
			if ( this._schemes.ContainsValue( index ) )
			{
				foreach ( var item in this._schemes )
				{
					if ( item.Value == index )
					{
						return item.Key;
					}
				}
			}
			return DefaultSchemeName;
		}


		/// <summary>
		/// Add a listener to handle material events. 
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Using delegate rather than an Listener interface" )]
		public virtual void AddListener( SchemeNotFoundHandler l )
		{
			this._listenerMap.Add( string.Empty, l );
		}

		/// <summary>
		/// Add a listener to handle material events. 
		/// If schemeName is supplied, the listener will only receive events for that certain scheme.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Using delegate rather than an Listener interface" )]
		public virtual void AddListener( SchemeNotFoundHandler l, string schemeName )
		{
			this._listenerMap.Add( schemeName ?? string.Empty, l );
		}

		/// <summary>
		/// Remove a listener handling material events. 
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Using delegate rather than an Listener interface" )]
		public virtual void RemoveListener( SchemeNotFoundHandler l )
		{
			this._listenerMap.RemoveWhere( ( x, y ) => x == String.Empty && y == l );
		}

		/// <summary>
		/// Remove a listener handling material events. 
		/// If the listener was added with a custom scheme name, it needs to be supplied here as well.
		/// </summary>
		[OgreVersion( 1, 7, 2790, "Using delegate rather than an Listener interface" )]
		public virtual void RemoveListener( SchemeNotFoundHandler l, string schemeName )
		{
			this._listenerMap.RemoveWhere( ( x, y ) => x == schemeName && y == l );
		}


		/// <summary>Internal method for sorting out missing technique for a scheme</summary>
		public Technique ArbitrateMissingTechniqueForActiveScheme( Material mat, int lodIndex, IRenderable rend )
		{
			var args = new SchemeNotFoundEventArgs( this._activeSchemeIndex, this._activeSchemeName, mat, lodIndex, rend );

			//First, check the scheme specific listeners
			List<SchemeNotFoundHandler> handlers;
			if ( this._listenerMap.TryGetValue( this._activeSchemeName, out handlers ) )
			{
				foreach ( SchemeNotFoundHandler i in handlers )
				{
					Technique t = i( args );
					if ( t != null )
					{
						return t;
					}
				}
			}

			//If no success, check generic listeners
			if ( this._listenerMap.TryGetValue( string.Empty, out handlers ) )
			{
				foreach ( SchemeNotFoundHandler i in handlers )
				{
					Technique t = i( args );
					if ( t != null )
					{
						return t;
					}
				}
			}

			return null;
		}

		#endregion

		#region ResourceManager Implementation

		protected override Resource _create( string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams )
		{
			return new Material( this, name, handle, group, isManual, loader );
		}

		#endregion ResourceManager Implementation

		#region ISingleton<MaterialManager> implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		protected static MaterialManager instance;

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static MaterialManager Instance
		{
			get
			{
				return instance;
			}
		}

		public virtual bool Initialize( params object[] args )
		{
			return true;
		}

		#endregion ISingleton<MaterialManager> implementation

		#region IScriptLoader Implementation

		/// <summary>
		///    Parse a .material script passed in as a chunk.
		/// </summary>
		public override void ParseScript( Stream stream, string groupName, string fileName )
		{
#if AXIOM_USENEWCOMPILERS
			Axiom.Scripting.Compiler.ScriptCompilerManager.Instance.ParseScript( stream, groupName, fileName );
#else
			this._serializer.ParseScript( stream, groupName, fileName );
#endif
		}

		#endregion IScriptLoader Implementation

		#region IDisposable Implementation

		/// <summary>
		/// Dispose of this object 
		/// </summary>
		/// <ogre name="~MaterialManager" />
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Unregister with resource group manager
					ResourceGroupManager.Instance.UnregisterResourceManager( ResourceType );
					// Unegister scripting with resource group manager
					ResourceGroupManager.Instance.UnregisterScriptLoader( this );
					Singleton<MaterialManager>.Destroy();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation

		#region Nested type: PassAttributeParser

		private delegate void PassAttributeParser( string[] values, Pass pass );

		#endregion

		#region Nested type: SchemeNotFoundEventArgs

		public class SchemeNotFoundEventArgs : EventArgs
		{
			public SchemeNotFoundEventArgs( ushort schemeIndex, string schemeName, Material originalMaterial, int lodIndex, IRenderable renderable )
			{
				SchemeIndex = schemeIndex;
				Renderable = renderable;
				LodIndex = lodIndex;
				OriginalMaterial = originalMaterial;
				SchemeName = schemeName;
			}

			public ushort SchemeIndex { get; private set; }

			public string SchemeName { get; private set; }

			public Material OriginalMaterial { get; private set; }

			public int LodIndex { get; private set; }

			public IRenderable Renderable { get; private set; }
		}

		#endregion

		#region Nested type: TextureUnitAttributeParser

		private delegate void TextureUnitAttributeParser( string[] values, TextureUnitState texUnit );

		#endregion
	}
}
