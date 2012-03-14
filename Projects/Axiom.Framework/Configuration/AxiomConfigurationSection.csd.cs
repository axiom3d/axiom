//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Configuration;

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// The AxiomConfigurationSection Configuration Section.
	/// </summary>
	public class AxiomConfigurationSection : ConfigurationSection
	{
		#region Singleton Instance

		/// <summary>
		/// The XML name of the AxiomConfigurationSection Configuration Section.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string AxiomConfigurationSectionSectionName = "axiomConfigurationSection";

		/// <summary>
		/// Gets the AxiomConfigurationSection instance.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public static AxiomConfigurationSection Instance
		{
			get
			{
				return ( (AxiomConfigurationSection)( ConfigurationManager.GetSection( AxiomConfigurationSectionSectionName ) ) );
			}
		}

		#endregion

		#region Xmlns Property

		/// <summary>
		/// The XML name of the <see cref="Xmlns"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string XmlnsPropertyName = "xmlns";

		/// <summary>
		/// Gets the XML namespace of this Configuration Section.
		/// </summary>
		/// <remarks>
		/// This property makes sure that if the configuration file contains the XML namespace,
		/// the parser doesn't throw an exception because it encounters the unknown "xmlns" attribute.
		/// </remarks>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[ConfigurationProperty( XmlnsPropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public string Xmlns
		{
			get
			{
				return ( (string)( base[ XmlnsPropertyName ] ) );
			}
		}

		#endregion

		#region IsReadOnly override

		/// <summary>
		/// Gets a value indicating whether the element is read-only.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override bool IsReadOnly()
		{
			return false;
		}

		#endregion

		#region LogFilename Property

		/// <summary>
		/// The XML name of the <see cref="LogFilename"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string LogFilenamePropertyName = "logFilename";

		/// <summary>
		/// Gets or sets the LogFilename.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The LogFilename." )]
		[ConfigurationProperty( LogFilenamePropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public string LogFilename
		{
			get
			{
				return ( (string)( base[ LogFilenamePropertyName ] ) );
			}
			set
			{
				base[ LogFilenamePropertyName ] = value;
			}
		}

		#endregion

		#region Plugins Property

		/// <summary>
		/// The XML name of the <see cref="Plugins"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string PluginsPropertyName = "plugins";

		/// <summary>
		/// Gets or sets the Plugins.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Plugins." )]
		[ConfigurationProperty( PluginsPropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public PluginsElementCollection Plugins
		{
			get
			{
				return ( (PluginsElementCollection)( base[ PluginsPropertyName ] ) );
			}
			set
			{
				base[ PluginsPropertyName ] = value;
			}
		}

		#endregion

		#region ResourceLocations Property

		/// <summary>
		/// The XML name of the <see cref="ResourceLocations"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string ResourceLocationsPropertyName = "resourceLocations";

		/// <summary>
		/// Gets or sets the ResourceLocations.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The ResourceLocations." )]
		[ConfigurationProperty( ResourceLocationsPropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public ResourceLocationElementCollection ResourceLocations
		{
			get
			{
				return ( (ResourceLocationElementCollection)( base[ ResourceLocationsPropertyName ] ) );
			}
			set
			{
				base[ ResourceLocationsPropertyName ] = value;
			}
		}

		#endregion

		#region RenderSystems Property

		/// <summary>
		/// The XML name of the <see cref="RenderSystems"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string RenderSystemsPropertyName = "renderSystems";

		/// <summary>
		/// Gets or sets the RenderSystems.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The RenderSystems." )]
		[ConfigurationProperty( RenderSystemsPropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public RenderSystemElementCollection RenderSystems
		{
			get
			{
				return ( (RenderSystemElementCollection)( base[ RenderSystemsPropertyName ] ) );
			}
			set
			{
				base[ RenderSystemsPropertyName ] = value;
			}
		}

		#endregion
	}
}

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// The PluginElement Configuration Element.
	/// </summary>
	public class PluginElement : ConfigurationElement
	{
		#region IsReadOnly override

		/// <summary>
		/// Gets a value indicating whether the element is read-only.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override bool IsReadOnly()
		{
			return false;
		}

		#endregion

		#region Path Property

		/// <summary>
		/// The XML name of the <see cref="Path"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string PathPropertyName = "path";

		/// <summary>
		/// Gets or sets the Path.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Path." )]
		[ConfigurationProperty( PathPropertyName, IsRequired = true, IsKey = true, IsDefaultCollection = false )]
		public string Path
		{
			get
			{
				return ( (string)( base[ PathPropertyName ] ) );
			}
			set
			{
				base[ PathPropertyName ] = value;
			}
		}

		#endregion

		#region Enabled Property

		/// <summary>
		/// The XML name of the <see cref="Enabled"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string EnabledPropertyName = "enabled";

		/// <summary>
		/// Gets or sets the Enabled.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Enabled." )]
		[ConfigurationProperty( EnabledPropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false, DefaultValue = true )]
		public bool Enabled
		{
			get
			{
				return ( (bool)( base[ EnabledPropertyName ] ) );
			}
			set
			{
				base[ EnabledPropertyName ] = value;
			}
		}

		#endregion
	}
}

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// A collection of PluginElement instances.
	/// </summary>
	[ConfigurationCollection( typeof( PluginElement ), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate, AddItemName = PluginElementPropertyName )]
	public class PluginsElementCollection : ConfigurationElementCollection
	{
		#region Constants

		/// <summary>
		/// The XML name of the individual <see cref="global::Axiom.Framework.Configuration.PluginElement"/> instances in this collection.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string PluginElementPropertyName = "pluginElement";

		#endregion

		#region Overrides

		/// <summary>
		/// Gets the type of the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <returns>The <see cref="global::System.Configuration.ConfigurationElementCollectionType"/> of this collection.</returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.BasicMapAlternate;
			}
		}

		/// <summary>
		/// Gets the name used to identify this collection of elements
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override string ElementName
		{
			get
			{
				return PluginElementPropertyName;
			}
		}

		/// <summary>
		/// Indicates whether the specified <see cref="global::System.Configuration.ConfigurationElement"/> exists in the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="elementName">The name of the element to verify.</param>
		/// <returns>
		/// <see langword="true"/> if the element exists in the collection; otherwise, <see langword="false"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override bool IsElementName( string elementName )
		{
			return ( elementName == PluginElementPropertyName );
		}

		/// <summary>
		/// Gets the element key for the specified configuration element.
		/// </summary>
		/// <param name="element">The <see cref="global::System.Configuration.ConfigurationElement"/> to return the key for.</param>
		/// <returns>
		/// An <see cref="object"/> that acts as the key for the specified <see cref="global::System.Configuration.ConfigurationElement"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override object GetElementKey( ConfigurationElement element )
		{
			return ( (PluginElement)( element ) ).Path;
		}

		/// <summary>
		/// Creates a new <see cref="global::Axiom.Framework.Configuration.PluginElement"/>.
		/// </summary>
		/// <returns>
		/// A new <see cref="global::Axiom.Framework.Configuration.PluginElement"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override ConfigurationElement CreateNewElement()
		{
			return new PluginElement();
		}

		#endregion

		#region Indexer

		/// <summary>
		/// Gets the <see cref="global::Axiom.Framework.Configuration.PluginElement"/> at the specified index.
		/// </summary>
		/// <param name="index">The index of the <see cref="global::Axiom.Framework.Configuration.PluginElement"/> to retrieve.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public PluginElement this[ int index ]
		{
			get
			{
				return ( (PluginElement)( base.BaseGet( index ) ) );
			}
		}

		/// <summary>
		/// Gets the <see cref="global::Axiom.Framework.Configuration.PluginElement"/> with the specified key.
		/// </summary>
		/// <param name="path">The key of the <see cref="global::Axiom.Framework.Configuration.PluginElement"/> to retrieve.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public PluginElement this[ object path ]
		{
			get
			{
				return ( (PluginElement)( base.BaseGet( path ) ) );
			}
		}

		#endregion

		#region Add

		/// <summary>
		/// Adds the specified <see cref="global::Axiom.Framework.Configuration.PluginElement"/> to the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="pluginElement">The <see cref="global::Axiom.Framework.Configuration.PluginElement"/> to add.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public void Add( PluginElement pluginElement )
		{
			base.BaseAdd( pluginElement );
		}

		#endregion

		#region Remove

		/// <summary>
		/// Removes the specified <see cref="global::Axiom.Framework.Configuration.PluginElement"/> from the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="pluginElement">The <see cref="global::Axiom.Framework.Configuration.PluginElement"/> to remove.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public void Remove( PluginElement pluginElement )
		{
			base.BaseRemove( GetElementKey( pluginElement ) );
		}

		#endregion

		#region IsReadOnly override

		/// <summary>
		/// Gets a value indicating whether the element is read-only.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override bool IsReadOnly()
		{
			return false;
		}

		#endregion

		#region AutoScan Property

		/// <summary>
		/// The XML name of the <see cref="AutoScan"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string AutoScanPropertyName = "autoScan";

		/// <summary>
		/// Gets or sets the AutoScan.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The AutoScan." )]
		[ConfigurationProperty( AutoScanPropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false, DefaultValue = true )]
		public bool AutoScan
		{
			get
			{
				return ( (bool)( base[ AutoScanPropertyName ] ) );
			}
			set
			{
				base[ AutoScanPropertyName ] = value;
			}
		}

		#endregion

		#region Path Property

		/// <summary>
		/// The XML name of the <see cref="Path"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string PathPropertyName = "path";

		/// <summary>
		/// Gets or sets the Path.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Path." )]
		[ConfigurationProperty( PathPropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public string Path
		{
			get
			{
				return ( (string)( base[ PathPropertyName ] ) );
			}
			set
			{
				base[ PathPropertyName ] = value;
			}
		}

		#endregion
	}
}

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// A collection of ResourceLocationElement instances.
	/// </summary>
	[ConfigurationCollection( typeof( ResourceLocationElement ), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate, AddItemName = ResourceLocationElementPropertyName )]
	public class ResourceLocationElementCollection : ConfigurationElementCollection
	{
		#region Constants

		/// <summary>
		/// The XML name of the individual <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/> instances in this collection.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string ResourceLocationElementPropertyName = "resourceLocation";

		#endregion

		#region Overrides

		/// <summary>
		/// Gets the type of the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <returns>The <see cref="global::System.Configuration.ConfigurationElementCollectionType"/> of this collection.</returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.BasicMapAlternate;
			}
		}

		/// <summary>
		/// Gets the name used to identify this collection of elements
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override string ElementName
		{
			get
			{
				return ResourceLocationElementPropertyName;
			}
		}

		/// <summary>
		/// Indicates whether the specified <see cref="global::System.Configuration.ConfigurationElement"/> exists in the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="elementName">The name of the element to verify.</param>
		/// <returns>
		/// <see langword="true"/> if the element exists in the collection; otherwise, <see langword="false"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override bool IsElementName( string elementName )
		{
			return ( elementName == ResourceLocationElementPropertyName );
		}

		/// <summary>
		/// Gets the element key for the specified configuration element.
		/// </summary>
		/// <param name="element">The <see cref="global::System.Configuration.ConfigurationElement"/> to return the key for.</param>
		/// <returns>
		/// An <see cref="object"/> that acts as the key for the specified <see cref="global::System.Configuration.ConfigurationElement"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override object GetElementKey( ConfigurationElement element )
		{
			return ( (ResourceLocationElement)( element ) ).Path;
		}

		/// <summary>
		/// Creates a new <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/>.
		/// </summary>
		/// <returns>
		/// A new <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override ConfigurationElement CreateNewElement()
		{
			return new ResourceLocationElement();
		}

		#endregion

		#region Indexer

		/// <summary>
		/// Gets the <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/> at the specified index.
		/// </summary>
		/// <param name="index">The index of the <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/> to retrieve.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public ResourceLocationElement this[ int index ]
		{
			get
			{
				return ( (ResourceLocationElement)( base.BaseGet( index ) ) );
			}
		}

		/// <summary>
		/// Gets the <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/> with the specified key.
		/// </summary>
		/// <param name="path">The key of the <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/> to retrieve.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public ResourceLocationElement this[ object path ]
		{
			get
			{
				return ( (ResourceLocationElement)( base.BaseGet( path ) ) );
			}
		}

		#endregion

		#region Add

		/// <summary>
		/// Adds the specified <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/> to the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="resourceLocation">The <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/> to add.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public void Add( ResourceLocationElement resourceLocation )
		{
			base.BaseAdd( resourceLocation );
		}

		#endregion

		#region Remove

		/// <summary>
		/// Removes the specified <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/> from the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="resourceLocation">The <see cref="global::Axiom.Framework.Configuration.ResourceLocationElement"/> to remove.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public void Remove( ResourceLocationElement resourceLocation )
		{
			base.BaseRemove( GetElementKey( resourceLocation ) );
		}

		#endregion

		#region IsReadOnly override

		/// <summary>
		/// Gets a value indicating whether the element is read-only.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override bool IsReadOnly()
		{
			return false;
		}

		#endregion
	}
}

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// A collection of RenderSystem instances.
	/// </summary>
	[ConfigurationCollection( typeof( RenderSystem ), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate, AddItemName = RenderSystemPropertyName )]
	public class RenderSystemElementCollection : ConfigurationElementCollection
	{
		#region Constants

		/// <summary>
		/// The XML name of the individual <see cref="global::Axiom.Framework.Configuration.RenderSystem"/> instances in this collection.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string RenderSystemPropertyName = "renderSystem";

		#endregion

		#region Overrides

		/// <summary>
		/// Gets the type of the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <returns>The <see cref="global::System.Configuration.ConfigurationElementCollectionType"/> of this collection.</returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.BasicMapAlternate;
			}
		}

		/// <summary>
		/// Gets the name used to identify this collection of elements
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override string ElementName
		{
			get
			{
				return RenderSystemPropertyName;
			}
		}

		/// <summary>
		/// Indicates whether the specified <see cref="global::System.Configuration.ConfigurationElement"/> exists in the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="elementName">The name of the element to verify.</param>
		/// <returns>
		/// <see langword="true"/> if the element exists in the collection; otherwise, <see langword="false"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override bool IsElementName( string elementName )
		{
			return ( elementName == RenderSystemPropertyName );
		}

		/// <summary>
		/// Gets the element key for the specified configuration element.
		/// </summary>
		/// <param name="element">The <see cref="global::System.Configuration.ConfigurationElement"/> to return the key for.</param>
		/// <returns>
		/// An <see cref="object"/> that acts as the key for the specified <see cref="global::System.Configuration.ConfigurationElement"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override object GetElementKey( ConfigurationElement element )
		{
			return ( (RenderSystem)( element ) ).Name;
		}

		/// <summary>
		/// Creates a new <see cref="global::Axiom.Framework.Configuration.RenderSystem"/>.
		/// </summary>
		/// <returns>
		/// A new <see cref="global::Axiom.Framework.Configuration.RenderSystem"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override ConfigurationElement CreateNewElement()
		{
			return new RenderSystem();
		}

		#endregion

		#region Indexer

		/// <summary>
		/// Gets the <see cref="global::Axiom.Framework.Configuration.RenderSystem"/> at the specified index.
		/// </summary>
		/// <param name="index">The index of the <see cref="global::Axiom.Framework.Configuration.RenderSystem"/> to retrieve.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public RenderSystem this[ int index ]
		{
			get
			{
				return ( (RenderSystem)( base.BaseGet( index ) ) );
			}
		}

		/// <summary>
		/// Gets the <see cref="global::Axiom.Framework.Configuration.RenderSystem"/> with the specified key.
		/// </summary>
		/// <param name="name">The key of the <see cref="global::Axiom.Framework.Configuration.RenderSystem"/> to retrieve.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public RenderSystem this[ object name ]
		{
			get
			{
				return ( (RenderSystem)( base.BaseGet( name ) ) );
			}
		}

		#endregion

		#region Add

		/// <summary>
		/// Adds the specified <see cref="global::Axiom.Framework.Configuration.RenderSystem"/> to the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="renderSystem">The <see cref="global::Axiom.Framework.Configuration.RenderSystem"/> to add.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public void Add( RenderSystem renderSystem )
		{
			base.BaseAdd( renderSystem );
		}

		#endregion

		#region Remove

		/// <summary>
		/// Removes the specified <see cref="global::Axiom.Framework.Configuration.RenderSystem"/> from the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="renderSystem">The <see cref="global::Axiom.Framework.Configuration.RenderSystem"/> to remove.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public void Remove( RenderSystem renderSystem )
		{
			base.BaseRemove( GetElementKey( renderSystem ) );
		}

		#endregion

		#region IsReadOnly override

		/// <summary>
		/// Gets a value indicating whether the element is read-only.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override bool IsReadOnly()
		{
			return false;
		}

		#endregion

		#region DefaultRenderSystem Property

		/// <summary>
		/// The XML name of the <see cref="DefaultRenderSystem"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string DefaultRenderSystemPropertyName = "defaultRenderSystem";

		/// <summary>
		/// Gets or sets the DefaultRenderSystem.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The DefaultRenderSystem." )]
		[ConfigurationProperty( DefaultRenderSystemPropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public string DefaultRenderSystem
		{
			get
			{
				return ( (string)( base[ DefaultRenderSystemPropertyName ] ) );
			}
			set
			{
				base[ DefaultRenderSystemPropertyName ] = value;
			}
		}

		#endregion
	}
}

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// The ResourceLocationElement Configuration Element.
	/// </summary>
	public class ResourceLocationElement : ConfigurationElement
	{
		#region IsReadOnly override

		/// <summary>
		/// Gets a value indicating whether the element is read-only.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override bool IsReadOnly()
		{
			return false;
		}

		#endregion

		#region Path Property

		/// <summary>
		/// The XML name of the <see cref="Path"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string PathPropertyName = "path";

		/// <summary>
		/// Gets or sets the Path.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Path." )]
		[ConfigurationProperty( PathPropertyName, IsRequired = true, IsKey = true, IsDefaultCollection = false )]
		public string Path
		{
			get
			{
				return ( (string)( base[ PathPropertyName ] ) );
			}
			set
			{
				base[ PathPropertyName ] = value;
			}
		}

		#endregion

		#region Type Property

		/// <summary>
		/// The XML name of the <see cref="Type"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string TypePropertyName = "type";

		/// <summary>
		/// Gets or sets the Type.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Type." )]
		[ConfigurationProperty( TypePropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public string Type
		{
			get
			{
				return ( (string)( base[ TypePropertyName ] ) );
			}
			set
			{
				base[ TypePropertyName ] = value;
			}
		}

		#endregion

		#region Recurse Property

		/// <summary>
		/// The XML name of the <see cref="Recurse"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string RecursePropertyName = "recurse";

		/// <summary>
		/// Gets or sets the Recurse.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Recurse." )]
		[ConfigurationProperty( RecursePropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public string Recurse
		{
			get
			{
				return ( (string)( base[ RecursePropertyName ] ) );
			}
			set
			{
				base[ RecursePropertyName ] = value;
			}
		}

		#endregion

		#region Group Property

		/// <summary>
		/// The XML name of the <see cref="Group"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string GroupPropertyName = "group";

		/// <summary>
		/// Gets or sets the Group.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Group." )]
		[ConfigurationProperty( GroupPropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public string Group
		{
			get
			{
				return ( (string)( base[ GroupPropertyName ] ) );
			}
			set
			{
				base[ GroupPropertyName ] = value;
			}
		}

		#endregion
	}
}

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// The RenderSystem Configuration Element.
	/// </summary>
	public class RenderSystem : ConfigurationElement
	{
		#region IsReadOnly override

		/// <summary>
		/// Gets a value indicating whether the element is read-only.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override bool IsReadOnly()
		{
			return false;
		}

		#endregion

		#region Name Property

		/// <summary>
		/// The XML name of the <see cref="Name"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string NamePropertyName = "name";

		/// <summary>
		/// Gets or sets the Name.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Name." )]
		[ConfigurationProperty( NamePropertyName, IsRequired = true, IsKey = true, IsDefaultCollection = false )]
		public string Name
		{
			get
			{
				return ( (string)( base[ NamePropertyName ] ) );
			}
			set
			{
				base[ NamePropertyName ] = value;
			}
		}

		#endregion

		#region Options Property

		/// <summary>
		/// The XML name of the <see cref="Options"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string OptionsPropertyName = "options";

		/// <summary>
		/// Gets or sets the Options.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Options." )]
		[ConfigurationProperty( OptionsPropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = true )]
		public RenderSystemOptionElementCollection Options
		{
			get
			{
				return ( (RenderSystemOptionElementCollection)( base[ OptionsPropertyName ] ) );
			}
			set
			{
				base[ OptionsPropertyName ] = value;
			}
		}

		#endregion
	}
}

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// A collection of RenderSystemOption instances.
	/// </summary>
	[ConfigurationCollection( typeof( RenderSystemOption ), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate, AddItemName = RenderSystemOptionPropertyName )]
	public class RenderSystemOptionElementCollection : ConfigurationElementCollection
	{
		#region Constants

		/// <summary>
		/// The XML name of the individual <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/> instances in this collection.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string RenderSystemOptionPropertyName = "option";

		#endregion

		#region Overrides

		/// <summary>
		/// Gets the type of the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <returns>The <see cref="global::System.Configuration.ConfigurationElementCollectionType"/> of this collection.</returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.BasicMapAlternate;
			}
		}

		/// <summary>
		/// Gets the name used to identify this collection of elements
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override string ElementName
		{
			get
			{
				return RenderSystemOptionPropertyName;
			}
		}

		/// <summary>
		/// Indicates whether the specified <see cref="global::System.Configuration.ConfigurationElement"/> exists in the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="elementName">The name of the element to verify.</param>
		/// <returns>
		/// <see langword="true"/> if the element exists in the collection; otherwise, <see langword="false"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override bool IsElementName( string elementName )
		{
			return ( elementName == RenderSystemOptionPropertyName );
		}

		/// <summary>
		/// Gets the element key for the specified configuration element.
		/// </summary>
		/// <param name="element">The <see cref="global::System.Configuration.ConfigurationElement"/> to return the key for.</param>
		/// <returns>
		/// An <see cref="object"/> that acts as the key for the specified <see cref="global::System.Configuration.ConfigurationElement"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override object GetElementKey( ConfigurationElement element )
		{
			return ( (RenderSystemOption)( element ) ).Name;
		}

		/// <summary>
		/// Creates a new <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/>.
		/// </summary>
		/// <returns>
		/// A new <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/>.
		/// </returns>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		protected override ConfigurationElement CreateNewElement()
		{
			return new RenderSystemOption();
		}

		#endregion

		#region Indexer

		/// <summary>
		/// Gets the <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/> at the specified index.
		/// </summary>
		/// <param name="index">The index of the <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/> to retrieve.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public RenderSystemOption this[ int index ]
		{
			get
			{
				return ( (RenderSystemOption)( base.BaseGet( index ) ) );
			}
		}

		/// <summary>
		/// Gets the <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/> with the specified key.
		/// </summary>
		/// <param name="name">The key of the <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/> to retrieve.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public RenderSystemOption this[ object name ]
		{
			get
			{
				return ( (RenderSystemOption)( base.BaseGet( name ) ) );
			}
		}

		#endregion

		#region Add

		/// <summary>
		/// Adds the specified <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/> to the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="option">The <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/> to add.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public void Add( RenderSystemOption option )
		{
			base.BaseAdd( option );
		}

		#endregion

		#region Remove

		/// <summary>
		/// Removes the specified <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/> from the <see cref="global::System.Configuration.ConfigurationElementCollection"/>.
		/// </summary>
		/// <param name="option">The <see cref="global::Axiom.Framework.Configuration.RenderSystemOption"/> to remove.</param>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public void Remove( RenderSystemOption option )
		{
			base.BaseRemove( GetElementKey( option ) );
		}

		#endregion

		#region IsReadOnly override

		/// <summary>
		/// Gets a value indicating whether the element is read-only.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override bool IsReadOnly()
		{
			return false;
		}

		#endregion
	}
}

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// The RenderSystemOption Configuration Element.
	/// </summary>
	public class RenderSystemOption : ConfigurationElement
	{
		#region IsReadOnly override

		/// <summary>
		/// Gets a value indicating whether the element is read-only.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		public override bool IsReadOnly()
		{
			return false;
		}

		#endregion

		#region Name Property

		/// <summary>
		/// The XML name of the <see cref="Name"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string NamePropertyName = "name";

		/// <summary>
		/// Gets or sets the Name.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Name." )]
		[ConfigurationProperty( NamePropertyName, IsRequired = true, IsKey = true, IsDefaultCollection = false )]
		public string Name
		{
			get
			{
				return ( (string)( base[ NamePropertyName ] ) );
			}
			set
			{
				base[ NamePropertyName ] = value;
			}
		}

		#endregion

		#region Value Property

		/// <summary>
		/// The XML name of the <see cref="Value"/> property.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		internal const string ValuePropertyName = "value";

		/// <summary>
		/// Gets or sets the Value.
		/// </summary>
		[GeneratedCode( "ConfigurationSectionDesigner.CsdFileGenerator", "2.0.0.0" )]
		[Description( "The Value." )]
		[ConfigurationProperty( ValuePropertyName, IsRequired = false, IsKey = false, IsDefaultCollection = false )]
		public string Value
		{
			get
			{
				return ( (string)( base[ ValuePropertyName ] ) );
			}
			set
			{
				base[ ValuePropertyName ] = value;
			}
		}

		#endregion
	}
}
