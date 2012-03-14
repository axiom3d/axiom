#region Namespace Declarations

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using Axiom.Configuration;
using Axiom.Core;
using Axiom.Demos.Configuration;
using Axiom.Graphics;

using CommandLine;
using CommandLine.Text;

#endregion Namespace Declarations

namespace Axiom.Demos.Browser.WinForm
{
	/// <summary>
	/// Demo command line browser entry point.
	/// </summary>
	/// <remarks>
	/// This demo browser is implemented using a commandline interface.
	/// </remarks>
	public class Program : IDisposable
	{
		protected const string CONFIG_FILE = @"EngineConfig.xml";
		private static readonly HeadingInfo _headingInfo = new HeadingInfo( "Axiom Samples", "0.8" );

		private readonly string DemoAssembly = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) + Path.DirectorySeparatorChar + @"Axiom.Demos.dll";
		private EngineConfig config;
		private DemoConfigDialog dlg;
		private Root engine;

		#region IDisposable Members

		public void Dispose()
		{
			//throw new Exception( "The method or operation is not implemented." );
		}

		#endregion

		private bool _configure( Options options )
		{
			// instantiate the Root singleton
			this.engine = new Root( "AxiomDemos.log" );

			_setupResources();

			this.dlg = new DemoConfigDialog();
			this.dlg.LoadRenderSystemConfig += LoadRenderSystemConfiguration;
			this.dlg.SaveRenderSystemConfig += SaveRenderSystemConfiguration;
			this.dlg.LoadDemos( this.DemoAssembly );

			if ( String.IsNullOrEmpty( options.Sample ) )
			{
				DialogResult result = this.dlg.ShowDialog();

				if ( result == DialogResult.Cancel )
				{
					Root.Instance.Dispose();
					this.engine = null;
					return false;
				}
			}
			else
			{
				this.engine.RenderSystem = this.engine.RenderSystems[ 0 ];
				LoadRenderSystemConfiguration( this, this.engine.RenderSystems[ 0 ] );
			}

			return true;
		}

		private void SaveRenderSystemConfiguration( object sender, RenderSystem rs )
		{
			string renderSystemId = rs.GetType().FullName;

			var codt = ( (EngineConfig.ConfigOptionDataTable)this.config.Tables[ "ConfigOption" ] );
			foreach ( ConfigOption opt in rs.ConfigOptions )
			{
				EngineConfig.ConfigOptionRow coRow = codt.FindByNameRenderSystem( opt.Name, renderSystemId );
				if ( coRow == null )
				{
					coRow = codt.NewConfigOptionRow();
					coRow.RenderSystem = renderSystemId;
					coRow.Name = opt.Name;
					codt.AddConfigOptionRow( coRow );
				}
				coRow.Value = opt.Value;
			}
			this.config.AcceptChanges();
			this.config.WriteXml( CONFIG_FILE );
		}

		private void LoadRenderSystemConfiguration( object sender, RenderSystem rs )
		{
			string renderSystemId = rs.GetType().FullName;

			var codt = ( (EngineConfig.ConfigOptionDataTable)this.config.Tables[ "ConfigOption" ] );
			foreach ( EngineConfig.ConfigOptionRow row in codt )
			{
				if ( row.RenderSystem == renderSystemId )
				{
					if ( rs.ConfigOptions.ContainsKey( row.Name ) )
					{
						rs.ConfigOptions[ row.Name ].Value = row.Value;
					}
				}
			}
		}

		/// <summary>
		///		Loads default resource configuration if one exists.
		/// </summary>
		private void _setupResources()
		{
			string resourceConfigPath = Path.GetFullPath( CONFIG_FILE );

			if ( File.Exists( resourceConfigPath ) )
			{
				this.config = new EngineConfig();

				// load the config file
				// relative from the location of debug and releases executables
				this.config.ReadXml( CONFIG_FILE );

				// interrogate the available resource paths
				foreach ( EngineConfig.FilePathRow row in this.config.FilePath )
				{
					ResourceGroupManager.Instance.AddResourceLocation( row.src, row.type, row.group, false, true );
				}
			}
		}

		private void Run( Options options )
		{
			try
			{
				if ( _configure( options ) )
				{
					Type demoType = null;
					if ( !String.IsNullOrEmpty( options.Sample ) )
					{
						Assembly demos = Assembly.LoadFrom( this.DemoAssembly );
						Type[] demoTypes = demos.GetTypes();
						demoType = demos.GetType( "Axiom.Demos." + options.Sample );
					}
					else
					{
						demoType = this.dlg.Demo;
					}

					if ( demoType != null )
					{
						using ( var demo = (TechDemo)Activator.CreateInstance( demoType ) )
						{
							demo.SetupResources();
							demo.Start(); //show and start rendering
						}
					}
				}
			}
			catch ( Exception caughtException )
			{
				LogManager.Instance.Write( BuildExceptionString( caughtException ) );
			}
		}

		#region Main

		[STAThread]
		private static void Main( string[] args )
		{
			var options = new Options();
			ICommandLineParser parser = new CommandLineParser( new CommandLineParserSettings( Console.Error ) );
			if ( !parser.ParseArguments( args, options ) )
			{
				Environment.Exit( 1 );
			}

			ExecuteCoreTask( options );
		}

		private static void ExecuteCoreTask( Options options )
		{
			try
			{
				using ( var main = new Program() )
				{
					main.Run( options ); //show and start rendering
				} //dispose of it when done
			}
			catch ( Exception ex )
			{
				Console.WriteLine( BuildExceptionString( ex ) );
				Console.WriteLine( "An exception has occurred.  Press enter to continue..." );
				Console.ReadLine();
			}
		}

		private static string BuildExceptionString( Exception exception )
		{
			string errMessage = string.Empty;

			errMessage += exception.Message + Environment.NewLine + exception.StackTrace;

			while ( exception.InnerException != null )
			{
				errMessage += BuildInnerExceptionString( exception.InnerException );
				exception = exception.InnerException;
			}

			return errMessage;
		}

		private static string BuildInnerExceptionString( Exception innerException )
		{
			string errMessage = string.Empty;

			errMessage += Environment.NewLine + " InnerException ";
			errMessage += Environment.NewLine + innerException.Message + Environment.NewLine + innerException.StackTrace;

			return errMessage;
		}

		#endregion Main

		#region Nested type: Options

		private sealed class Options
		{
			[Option( "s", "sample", Required = false, HelpText = "Initial sample to run." )]
			public readonly string Sample = String.Empty;

			[HelpOption( HelpText = "Display this help screen." )]
			public string GetUsage()
			{
				var help = new HelpText( _headingInfo );
				help.AdditionalNewLineAfterOption = true;
				help.Copyright = new CopyrightInfo( "Axiom Engine Team", 2003, 2010 );
				help.AddPreOptionsLine( "This is free software. You may redistribute copies of it under the terms of" );
				help.AddPreOptionsLine( "the LGPL License <http://www.opensource.org/licenses/lgpl-license.php>." );
				help.AddPreOptionsLine( "Usage: SampleApp -sCompositor " );
				help.AddOptions( this );

				return help;
			}
		}

		#endregion
	}
}
