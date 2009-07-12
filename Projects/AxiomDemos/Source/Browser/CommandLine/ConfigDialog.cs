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
		private List<ConfigOption> _menuItems = new List<ConfigOption>();
        private List<ConfigOption> _options = new List<ConfigOption>();

		public ConfigDialog()
		{
			_currentSystem = Root.Instance.RenderSystems.Values[0];
			_renderSystems = new ConfigOption( "Render System", _currentSystem.Name, false );
			foreach ( RenderSystem rs in Root.Instance.RenderSystems )
			{
				_renderSystems.PossibleValues.Add( _renderSystems.PossibleValues.Count, rs.ToString() );
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
			if ( _currentOption == null )
				BuildMainMenu();
			//else
            //    BuildOptionMenu();
		}

		private void BuildMainMenu()
		{
			for ( int index = 0; index < _options.Count; index++ )
			{
				_menuItems.Add( _options[ index ] );
			}
		}

	        //private void BuildOptionMenu()
        //{
        //    foreach (string value in _currentOption.PossibleValues)
        //    {
        //        _menuItems.Add(value);
        //    }
        //}

		private void DisplayOptions()
		{
			Console.Clear();

			Console.WriteLine( "Axiom Engine Configuration" );
			Console.WriteLine( "==========================" );

			if ( _currentOption != null )
			{
				Console.WriteLine( "Available settings for {0}.\n", _currentOption.Name );
			}
			// Load Render Subsystem Options
            int index = 0;
            foreach ( object opt in _menuItems )
			{
                System.Console.WriteLine( "{0:D2}      | {1}", index++, opt.ToString() );
			}

            if ( _currentOption == null )
			{
				Console.WriteLine();
				Console.WriteLine( "Enter  | Saves changes." );
				Console.WriteLine( "ESC    | Exits." );
			}
			Console.Write( "\nSelect option : " );
		}

		private int GetInput()
		{
			int number = 0;
			int keyCount = 2;

			while ( keyCount > 0 )
			{
				ConsoleKeyInfo key = Console.ReadKey();

				if ( key.Key == ConsoleKey.Escape )
					return -1;
				if ( key.Key == ConsoleKey.Enter )
					return -2;

				if ( key.Key.ToString().Substring( 1 ).Length == 1 && key.Key.ToString().Substring( 1 ).ToCharArray()[ 0 ] >= '0' && key.Key.ToString().Substring( 1 ).ToCharArray()[ 0 ] <= '9' )
				{
					number += Int32.Parse( key.Key.ToString().Substring( 1 ) ) * ( (int)System.Math.Pow( 10, keyCount - 1 ) );
					keyCount--;
				}
			}
			return number;
		}

		private bool ProcessKey( int key )
		{
			int index;

			if ( _currentOption == null )
			{
				if ( key == -1 ) //ESCAPE
				{
					_result = DialogResult.Cancel;
					return false;
				}
				if ( key == -2 )
				{
					Root.Instance.RenderSystem = _currentSystem;

					//for ( index = 0; index < _options.Count; index++ )
					//{
					//    ConfigOption opt = (ConfigOption)_options[ index ];
					//    _currentSystem.ConfigOptions[ opt.Name ] = opt;
					//}

					_result = DialogResult.Ok;
					return false;
				}

				if ( key < _menuItems.Count )
				{
					_currentOption = _options.Find
					(
                        delegate(ConfigOption c)
                        {
                            return c == _menuItems[key];
						}
					);
				}
            }
			else
			{
				_currentOption.Value = _currentOption.PossibleValues[key].ToString();

				if ( _currentOption.Name == "Render System" ) // About to change Renderers
				{
					_renderSystems = _currentOption;
					_currentSystem = Root.Instance.RenderSystems[key.ToString()];
					BuildOptions();
					_currentOption = null;

					return true;
				}

				_currentOption = null;
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
				_continue = ProcessKey( value );
			} while ( _continue );
			Console.Clear();
			return _result;
		}
	}
}
