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

    /// <summary>
    /// Command line configuration dialog
    /// </summary>
    class ConfigDialog
    {
        private ConfigOption _renderSystems;
        private RenderSystem _currentSystem;
        private List<RenderSystem> _renderSystemList;
        private DialogResult _result;
        private ConfigOption _currentOption;
        private ArrayList _menuItems = new ArrayList();
        private List<ConfigOption> _options = new List<ConfigOption>();
        private int _numericInput;

        public ConfigDialog()
        {
            Axiom.Core.Root root = Axiom.Core.Root.Instance;
            _renderSystemList = new List<RenderSystem>(root.RenderSystems.Values);
            _currentSystem = _renderSystemList[ 0 ];
            _renderSystems = new ConfigOption("Render System", _currentSystem.Name, false);

            foreach ( RenderSystem rs in root.RenderSystems )
            {
                _renderSystems.PossibleValues.Add(_renderSystems.PossibleValues.Count, rs.ToString());
            }

            BuildOptions();
        }

        private void BuildOptions()
        {
            _options.Clear();
            _options.Add(_renderSystems);

            // Load Render Subsystem Options
            foreach ( ConfigOption option in _currentSystem.ConfigOptions )
            {
                _options.Add(option);
            }
        }

        private void BuildMenu()
        {
            _menuItems.Clear();
            if ( _currentOption == null )
                BuildMainMenu();
            else
                BuildOptionMenu();
        }

        private void BuildMainMenu()
        {
            for ( int index = 0; index < _options.Count; index++ )
            {
                _menuItems.Add(_options[ index ]);
            }
        }

        private void BuildOptionMenu()
        {
            for ( int index = 0; index < _currentOption.PossibleValues.Count; index++ )
            {
                _menuItems.Add(_currentOption.PossibleValues.Values[ index ].ToString());
            }
        }

        private void DisplayOptions()
        {
            Console.Clear();

            Console.WriteLine("Axiom Engine Configuration");
            Console.WriteLine("==========================");

            if ( _currentOption != null )
            {
                Console.WriteLine("Available settings for {0}.\n", _currentOption.Name);
            }
            // Load Render Subsystem Options
            for ( int index = 0; index < _menuItems.Count; index++ )
            {
                System.Console.WriteLine("{0:D2}      | {1}", index, _menuItems[ index ].ToString());
            }

            if ( _currentOption == null )
            {
                Console.WriteLine();
                Console.WriteLine("Enter  | Save changes");
                Console.WriteLine("ESC    | Exit");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("ESC    | Back to main");
            }

            Console.Write("\nSelect option : ");
        }

        private int GetInput()
        {
            _numericInput = -1;
            string stringInput = String.Empty;
            int keyCount = 2;

            while ( keyCount > 0 )
            {
                ConsoleKeyInfo key = Console.ReadKey();

                if ( key.Key == ConsoleKey.Escape )
                    return -1;

                if ( key.Key == ConsoleKey.Enter )
                {
                    if ( stringInput.Length > 0 )
                        _numericInput = Int32.Parse(stringInput);

                    return -2;
                }

                if ( key.KeyChar >= '0' && key.KeyChar <= '9' )
                {
                    stringInput += key.KeyChar.ToString();
                    keyCount--;
                }
                else if ( key.Key == ConsoleKey.Backspace )
                {
                    if ( stringInput.Length > 0 )
                    {
                        stringInput = stringInput.Substring(0, stringInput.Length - 1);
                        keyCount++;
                        Console.Write(" \b"); // clear last char
                    }
                    else
                        Console.Write(" "); // back to position
                }
                else
                {
                    Console.Write("\b \b"); // clear invalid char
                }
            }
            _numericInput = Int32.Parse(stringInput);

            return -2; // force ENTER
        }

        private bool ProcessKey( int key )
        {

            if ( key == -1 ) //ESCAPE
            {
                if ( _currentOption == null )
                {
                    // ESC at main menu
                    _result = DialogResult.Cancel;
                    return false;
                }
                else
                {
                    // go back to main menu
                    _currentOption = null;
                    return true;
                }
            }

            if ( key == -2 ) //ENTER
            {
                if ( _currentOption == null )
                {
                    // at main menu
                    if ( _numericInput == -1 )
                    {
                        Axiom.Core.Root.Instance.RenderSystem = _currentSystem;

                        _result = DialogResult.Ok;
                        return false;
                    }
                    else if ( _numericInput >= 0 && _numericInput < _menuItems.Count )
                    {
                        _currentOption = (ConfigOption)_menuItems[ _numericInput ];
                        return true;
                    }

                    return true;
                }
                else if ( _numericInput >= 0 && _numericInput < _currentOption.PossibleValues.Count )
                {
                    // at options menu and having an entered number
                    _currentOption.Value = _currentOption.PossibleValues.Values[ _numericInput ].ToString();

                    if ( _currentOption.Name == "Render System" ) // About to change Renderers
                    {
                        _renderSystems = _currentOption;
                        _currentSystem = _renderSystemList[ _numericInput ];

                        BuildOptions();

                        _currentOption = null;

                        return true;
                    }

                    _currentOption = null;
                }

                return true;
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
                int value = GetInput();
                _continue = ProcessKey(value);
            } while ( _continue );

            Console.Clear();

            return _result;
        }
    }
}