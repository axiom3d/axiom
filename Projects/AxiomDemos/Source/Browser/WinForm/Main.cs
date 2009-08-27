#region Namespace Declarations

using System;
using System.IO;
using System.Globalization;
using System.Threading;

using Axiom.Demos;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Configuration;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using Axiom.Demos.Configuration;

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

        private Root engine;
		DemoConfigDialog dlg;
		EngineConfig config;

        private bool _configure( )
        {
            // instantiate the Root singleton
            engine = new Root( "AxiomDemos.log" );

            _setupResources();

			dlg = new DemoConfigDialog();
			dlg.LoadRenderSystemConfig += new ConfigDialog.LoadRenderSystemConfigEventHandler( LoadRenderSystemConfiguration );
			dlg.SaveRenderSystemConfig += new ConfigDialog.SaveRenderSystemConfigEventHandler( SaveRenderSystemConfiguration );
			dlg.LoadDemos( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) + System.IO.Path.DirectorySeparatorChar + @"Axiom.Demos.dll" );
            DialogResult result = dlg.ShowDialog();
			if ( result == DialogResult.Cancel )
			{
				Root.Instance.Dispose();
				engine = null;
				return false;
			}

            return true;
        }

		void SaveRenderSystemConfiguration( object sender, RenderSystem rs )
		{
			string renderSystemId = rs.GetType().FullName;

			EngineConfig.ConfigOptionDataTable codt = ( (EngineConfig.ConfigOptionDataTable)config.Tables[ "ConfigOption" ] );
			foreach ( ConfigOption opt in rs.ConfigOptions )
			{
				EngineConfig.ConfigOptionRow coRow = codt.FindByNameRenderSystem(opt.Name, renderSystemId );
				if ( coRow == null )
				{
					coRow = codt.NewConfigOptionRow();
					coRow.RenderSystem = renderSystemId;
					coRow.Name = opt.Name;
					codt.AddConfigOptionRow( coRow );
				}
				coRow.Value = opt.Value;
			}
			config.AcceptChanges();
			config.WriteXml( CONFIG_FILE );
		}

		void LoadRenderSystemConfiguration( object sender, RenderSystem rs )
		{
			string renderSystemId = rs.GetType().FullName;

			EngineConfig.ConfigOptionDataTable codt = ( (EngineConfig.ConfigOptionDataTable)config.Tables[ "ConfigOption" ] );
			foreach ( EngineConfig.ConfigOptionRow row in codt )
			{
				if ( row.RenderSystem == renderSystemId )
				{
					if ( rs.ConfigOptions.ContainsKey( row.Name ) )
						rs.ConfigOptions[ row.Name ].Value = row.Value;
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
				config = new EngineConfig();

                // load the config file
                // relative from the location of debug and releases executables
                config.ReadXml( CONFIG_FILE );

                // interrogate the available resource paths
				foreach ( EngineConfig.FilePathRow row in config.FilePath )
                {
                    ResourceGroupManager.Instance.AddResourceLocation( row.src, row.type, row.group, false, true );
                }
            }
        }

        public void Run( )
        {
            try
            {
                if ( _configure( ) )
                {

                    if ( dlg.Demo != null )
                    {
                        using ( TechDemo demo = (TechDemo)Activator.CreateInstance( dlg.Demo ) )
                        {
                            demo.SetupResources();
                            demo.Start();//show and start rendering
                        }//dispose of it when done
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
            try
            {
                using ( Program main = new Program() )
                {
                    main.Run();//show and start rendering
                }//dispose of it when done
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

        #region IDisposable Members

        public void Dispose()
        {
            //throw new Exception( "The method or operation is not implemented." );
        }

        #endregion
    }
}