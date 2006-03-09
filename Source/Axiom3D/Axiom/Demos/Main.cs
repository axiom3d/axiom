#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace Declarations

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using RealmForge;

using Axiom;

#endregion Namespace Declarations

namespace Axiom.Demos
{

    /// <summary>
    ///     Demo browser entry point.
    /// </summary>
    public class Program : IDisposable
    {
        protected const string CONFIG_FILE = @"EngineConfig.xml";

        private RenderWindow window;
        private Root engine;

        private bool _configure()
        {
            // instantiate the Root singleton
            engine = new Root( CONFIG_FILE, "AxiomEngine.log" );

            _setupResources();

            // HACK: Temporary
            ConfigDialog dlg = new ConfigDialog();
            DialogResult result = dlg.ShowDialog();
            if ( result == DialogResult.Cancel )
                return false;

            window = Root.Instance.Initialize( true, "Axiom Technical Demos" );

            return true;
        }

        /// <summary>
        ///		Loads default resource configuration if one exists.
        /// </summary>
        private void _setupResources()
        {
            string resourceConfigPath = Path.GetFullPath( CONFIG_FILE );

            if ( File.Exists( resourceConfigPath ) )
            {
                EngineConfig config = new EngineConfig();

                // load the config file
                // relative from the location of debug and releases executables
                config.ReadXml( CONFIG_FILE );

                // interrogate the available resource paths
                foreach ( EngineConfig.FilePathRow row in config.FilePath )
                {
                    ResourceManager.AddCommonArchive( row.src, row.type );
                }
            }
        }


        public void Run()
        {
            if ( _configure() )
            {

                string next = "";

                while ( next != "exit" )
                {
                    using ( DemoList mainDemo = new DemoList() )
                    {
                        next = mainDemo.Start( window );
                    }

                    if ( next != "exit" )
                    {
                        Type demoType = Assembly.GetExecutingAssembly().GetType( "Axiom.Demos." + next );

                        if ( demoType != null )
                        {
                            using ( TechDemo demo = (TechDemo)Assembly.GetExecutingAssembly().CreateInstance( "Axiom.Demos." + next ) )
                            {
                                demo.Start( window );//show and start rendering
                            }//dispose of it when done
                        }

                    }

                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Root.Instance.Dispose();
        }

        #endregion

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
                Console.WriteLine( ex.ToString() );
                Console.WriteLine( "An exception has occurred.  Press enter to continue..." );
                Console.ReadLine();
            }
        }
    

}
}
