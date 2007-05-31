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
			else
				BuildOptionMenu();
		}

		private void BuildMainMenu()
		{
			for ( int index = 0; index < _options.Count; index++ )
			{
				_menuItems.Add( _options[ index ] );
			}
		}

		private void BuildOptionMenu()
		{
			for ( int index = 0; index < _currentOption.PossibleValues.Count; index++ )
			{
				_menuItems.Add( _currentOption.PossibleValues.Values[ index ].ToString() );
			}
		}

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
			for ( int index = 0; index < _menuItems.Count; index++ )
			{
				System.Console.WriteLine( "{0:D2}      | {1}", index, _menuItems[ index ].ToString() );
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
					_currentOption = (ConfigOption)_menuItems[ key ];
				}
			}
			else
			{
				_currentOption.Value = _currentOption.PossibleValues.Values[ key ].ToString();

				if ( _currentOption.Name == "Render System" ) // About to change Renderers
				{
					_renderSystems = _currentOption;
					_currentSystem = Root.Instance.RenderSystems[ key ];
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
