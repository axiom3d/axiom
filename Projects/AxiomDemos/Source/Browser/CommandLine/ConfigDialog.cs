#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Collections;
using System.Collections;

#endregion Namespace Declarations
			
namespace Axiom.Demos.Browser.CommandLine
{
    public enum DialogResult
    {
        Ok,
        Cancel
    }

    class ConfigDialog
    {
        private ConfigOption _renderSystems;
        private RenderSystem _currentSystem;
        private DialogResult _result;
        private ConfigOption _currentOption;
        private ArrayList _menuItems = new ArrayList();
        private ArrayList _options = new ArrayList();

        public ConfigDialog()
        {
            _currentSystem = Root.Instance.RenderSystems[ 0 ];
            _renderSystems = new ConfigOption( "Render System", _currentSystem.Name, false );
            foreach ( RenderSystem rs in Root.Instance.RenderSystems )
            {
                _renderSystems.PossibleValues.Add( rs );
            }
            BuildOptions();
        }

        private void BuildOptions()
        {
            int index = 1;

            _options.Clear();
            _options.Add( _renderSystems );

            // Load Render Subsystem Options
            foreach ( ConfigOption option in _currentSystem.ConfigOptions )
            {
                _options.Add( option );
            }
        }

        private void BuildMenu()
        {
            _menuItems.Clear();
            if ( _currentOption.Name == null )
                BuildMainMenu();
            else
                BuildOptionMenu();
        }

        private void BuildMainMenu()
        {
            for( int index =0 ; index < _options.Count; index++ )
            {
                _menuItems.Add( _options[ index ] );
            }
        }

        private void BuildOptionMenu()
        {
            foreach ( object value in _currentOption.PossibleValues )
            {
                _menuItems.Add( value.ToString() );
            }
        }

        private void DisplayOptions()
        {
            Console.Clear();

            Console.WriteLine( "Axiom Engine Configuration" );
            Console.WriteLine( "==========================" );

            if ( _currentOption.Name != null )
            {
                Console.WriteLine( "Available settings for {0}.\n", _currentOption.Name );
            }
            // Load Render Subsystem Options
            int index = 0;
            foreach( object opt in _menuItems )
            {
                System.Console.WriteLine( "{0}      | {1}", index++, opt.ToString() );
            }

            if ( _currentOption.Name == null )
            {
                Console.WriteLine();
                Console.WriteLine( "Enter  | Saves changes." );
                Console.WriteLine( "ESC    | Exits." );
            }
            Console.Write( "\nSelect option : " );
        }

        private ConsoleKey GetInput()
        {
            ConsoleKeyInfo key = Console.ReadKey();
            return key.Key;
        }

        private bool ProcessKey( ConsoleKey key )
        {
            int index;

            if ( _currentOption.Name == null )
            {
                if ( key == ConsoleKey.Escape )
                {
                    _result = DialogResult.Cancel;
                    return false;
                }
                if ( key == ConsoleKey.Enter )
                {
                    Root.Instance.RenderSystem = _currentSystem;

                    for ( index = 0; index < _options.Count; index++ )
                    {
                        ConfigOption opt = (ConfigOption)_options[ index ];
                        _currentSystem.ConfigOptions[ opt.Name ] = opt;
                    }

                    _result = DialogResult.Ok;
                    return false;
                }

                if ( key.ToString().Substring( 1 ).Length == 1 && key.ToString().Substring( 1 ).ToCharArray()[ 0 ] >= '0' && key.ToString().Substring( 1 ).ToCharArray()[ 0 ] <= '9' )
                {
                    index = Int32.Parse( key.ToString().Substring( 1 ) );

                    if ( index < _menuItems.Count )
                    {
                        _currentOption = (ConfigOption)_menuItems[ index ];
                    }
                }
            }
            else
            {

                if ( key.ToString().Substring( 1 ).Length == 1 && key.ToString().Substring( 1 ).ToCharArray()[ 0 ] >= '0' && key.ToString().Substring( 1 ).ToCharArray()[ 0 ] <= '9' )
                {
                    index = Int32.Parse( key.ToString().Substring( 1 ) );
                    _currentOption.Value = _currentOption.PossibleValues[ index ].ToString();

                    if ( _currentOption.Name == "Render System" ) // About to change Renderers
                    {
                        _currentSystem = (RenderSystem)_currentOption.PossibleValues[ index ];
                        _renderSystems = _currentOption;
                        BuildOptions();
                        _currentOption.Name = null;

                        return true;
                    }


                    for ( index = 0; index < _options.Count; index++ )
                    {
                        if ( ( (ConfigOption)_options[ index ] ).Name == _currentOption.Name )
                        {
                            _options[ index ] = _currentOption; 
                        }
                    }
                }
                _currentOption.Name = null;
            }
            return true;
        }

        public DialogResult ShowDialog()
        {
            bool _continue = false;
            do
            {
                BuildMenu();
                DisplayOptions();
                ConsoleKey value = GetInput();
                _continue = ProcessKey( value );
            } while ( _continue );
            Console.Clear();
            return _result;
        }
    }
}
