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

using System;
using System.Collections.Generic;
using System.Configuration;
using Axiom.Core;
using Axiom.Configuration;

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// 
	/// </summary>
	public class DefaultConfigurationManager : ConfigurationManagerBase
	{
		#region Fields and Properties

		public static string DefaultLogFileName = "axiom.log";
		public static string DefaultSectionName = "axiom";

		protected AxiomConfigurationSection ConfigurationSection;
		protected System.Configuration.Configuration Configuration;
		protected IConfigurationDialogFactory ConfigurationFactory;

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		/// 
		/// </summary>
		public DefaultConfigurationManager()
			: this( new DefaultConfigurationDialogFactory(), null, DefaultSectionName )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dialog"></param>
		public DefaultConfigurationManager( IConfigurationDialogFactory dialog )
			: this( dialog, null, DefaultSectionName )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="configurationFile"></param>
		public DefaultConfigurationManager( string configurationFile )
			: this( new DefaultConfigurationDialogFactory(), configurationFile, DefaultSectionName )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dialog"></param>
		/// <param name="configurationFile"></param>
		public DefaultConfigurationManager( IConfigurationDialogFactory dialog, string configurationFile )
			: this( dialog, configurationFile, DefaultSectionName )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="configurationFile"></param>
		/// <param name="sectionName"></param>
		public DefaultConfigurationManager( string configurationFile, string sectionName )
			: this( new DefaultConfigurationDialogFactory(), configurationFile, DefaultSectionName )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dialog"></param>
		/// <param name="configurationFile"></param>
		/// <param name="sectionName"></param>
		public DefaultConfigurationManager( IConfigurationDialogFactory factory, string configurationFile, string sectionName )
			: base( configurationFile )
		{
			this.ConfigurationFactory = factory;
			LogFilename = DefaultLogFileName;

			if ( !String.IsNullOrEmpty( configurationFile ) )
			{
				// Get current configuration file.
				var map = new ExeConfigurationFileMap();
				map.ExeConfigFilename = configurationFile;
				this.Configuration = ConfigurationManager.OpenMappedExeConfiguration( map, ConfigurationUserLevel.None );
			}
			else
			{
				this.Configuration = ConfigurationManager.OpenExeConfiguration( ConfigurationUserLevel.None );
			}

			// Get the section.
			this.ConfigurationSection = this.Configuration.GetSection( sectionName ) as AxiomConfigurationSection;


			if ( this.ConfigurationSection != null && !String.IsNullOrEmpty( this.ConfigurationSection.LogFilename ) )
			{
				LogFilename = this.ConfigurationSection.LogFilename;
			}
		}

		#endregion Construction and Destruction

		#region ConfigurationManagerBase Implementation

		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		public override bool RestoreConfiguration( Root engine )
		{
			// Load Plugins

			// RenderSystem Selection
			if ( engine.RenderSystems.Count == 0 )
			{
				throw new AxiomException( "At least one RenderSystem must be loaded." );
			}

			if ( this.ConfigurationSection == null )
			{
				return false;
			}

			if ( engine.RenderSystems.ContainsKey( this.ConfigurationSection.RenderSystems.DefaultRenderSystem ) )
			{
				engine.RenderSystem = engine.RenderSystems[ this.ConfigurationSection.RenderSystems.DefaultRenderSystem ];
			}

			foreach ( RenderSystem renderSystemConfig in this.ConfigurationSection.RenderSystems )
			{
				if ( engine.RenderSystems.ContainsKey( renderSystemConfig.Name ) )
				{
					var renderSystem = engine.RenderSystems[ renderSystemConfig.Name ];

					foreach ( RenderSystemOption optionConfig in renderSystemConfig.Options )
					{
						if ( renderSystem.ConfigOptions.ContainsKey( optionConfig.Name ) )
						{
							renderSystem.ConfigOptions[ optionConfig.Name ].Value = optionConfig.Value;
						}
					}
				}
			}

			// Setup Resource Locations
			foreach ( ResourceLocationElement locationElement in this.ConfigurationSection.ResourceLocations )
			{
				ResourceGroupManager.Instance.AddResourceLocation( locationElement.Path, locationElement.Type, locationElement.Group,
				                                                   bool.Parse( locationElement.Recurse ), false );
			}
			return ( engine.RenderSystem != null );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		public override void SaveConfiguration( Root engine )
		{
			SaveConfiguration( engine, engine.RenderSystem.Name );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="defaultRenderer"></param>
		public override void SaveConfiguration( Root engine, string defaultRenderer )
		{
			for ( int index = 0; index < this.ConfigurationSection.RenderSystems.Count; index++ )
			{
				this.ConfigurationSection.RenderSystems.Remove( this.ConfigurationSection.RenderSystems[ 0 ] );
			}

			foreach ( var key in engine.RenderSystems.Keys )
			{
				this.ConfigurationSection.RenderSystems.Add( new RenderSystem
				                                             {
				                                             	Name = key,
				                                             	Options = new RenderSystemOptionElementCollection()
				                                             } );

				foreach ( ConfigOption item in engine.RenderSystems[ key ].ConfigOptions )
				{
					this.ConfigurationSection.RenderSystems[ key ].Options.Add( new RenderSystemOption
					                                                            {
					                                                            	Name = item.Name,
					                                                            	Value = item.Value
					                                                            } );
				}
			}

			if ( !string.IsNullOrEmpty( defaultRenderer ) &&
			     this.ConfigurationSection.RenderSystems.DefaultRenderSystem != defaultRenderer )
			{
				this.ConfigurationSection.RenderSystems.DefaultRenderSystem = defaultRenderer;
			}

			this.Configuration.Save( ConfigurationSaveMode.Modified );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="engine"></param>
		/// <returns></returns>
		public override bool ShowConfigDialog( Root engine )
		{
			IConfigurationDialog configDialog = this.ConfigurationFactory.CreateConfigurationDialog( engine,
			                                                                                         ResourceGroupManager.
			                                                                                         	Instance );
			DialogResult result = configDialog.Show();
			return result == DialogResult.Ok;
		}

		#endregion ConfigurationManagerBase Implementation
	}
}