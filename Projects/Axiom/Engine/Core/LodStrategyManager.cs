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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id: LodStrategyManager.cs 1762 2009-09-13 17:56:22Z bostich $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Manager for lod strategies.
	/// </summary>
	public class LodStrategyManager : Singleton<LodStrategyManager>
	{
		#region Fields and Properties

		/// <summary>
		/// Internal map of strategies.
		/// </summary>
		private Dictionary<string, LodStrategy> _strategies = new Dictionary<string, LodStrategy>();

		#region DefaultStrategy

		/// <summary>
		/// Default strategy.
		/// </summary>
		private LodStrategy _defaultStrategy;

		/// <summary>
		/// Get's or set's the default strategy.
		/// </summary>
		public LodStrategy DefaultStrategy { set { this._defaultStrategy = value; } get { return this._defaultStrategy; } }

		/// <summary>
		/// Set the default strategy by name.
		/// </summary>
		/// <param name="name"></param>
		public void SetDefaultStrategy( string name )
		{
			DefaultStrategy = GetStrategy( name );
		}

		#endregion DefaultStrategy

		#endregion Fields and Properties

		/// <summary>
		/// Default constructor.
		/// </summary>
		public LodStrategyManager()
			: base()
		{
			// Add default (distance) strategy
			DistanceLodStrategy distanceStrategy = new DistanceLodStrategy();
			AddStrategy( distanceStrategy );

			// Add new pixel-count strategy
			PixelCountStrategy pixelCountStrategy = new PixelCountStrategy();
			AddStrategy( pixelCountStrategy );

			// Set the default strategy
			DefaultStrategy = distanceStrategy;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if( !isDisposed )
			{
				if( disposeManagedResources )
				{
					// Dispose managed resources.
					RemoveAllStrategies();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		/// <summary>
		/// Add a strategy to the manager.
		/// </summary>
		/// <param name="strategy"></param>
		public void AddStrategy( LodStrategy strategy )
		{
			// Check for invalid strategy name
			if( strategy.Name.ToLower() == "default" )
			{
				throw new AxiomException( "Lod strategy name must not be 'default'", new object[] {} );
			}

			// Insert the strategy into the map with its name as the key
			this._strategies.Add( strategy.Name, strategy );
		}

		/// <summary>
		/// Remove a strategy from the manager with a specified name.
		/// </summary>
		/// <remarks>
		/// The removed strategy is returned so the user can control
		/// how it is destroyed.
		/// </remarks>
		/// <param name="name"></param>
		/// <returns></returns>
		public LodStrategy RemoveStrategy( string name )
		{
			LodStrategy ret = null;
			if( this._strategies.TryGetValue( name, out ret ) )
			{
				this._strategies.Remove( name );
				return ret;
			}
			return ret;
		}

		/// <summary>
		///  Remove and delete all strategies from the manager.
		/// </summary>
		/// <remarks>
		/// All strategies are deleted.  If finer control is required
		/// over strategy destruction, use removeStrategy.
		/// </remarks>
		public void RemoveAllStrategies()
		{
			this._strategies.Clear();
		}

		/// <summary>
		/// Get the strategy with the specified name.
		/// </summary>
		/// <param name="name">name of the strategy</param>
		/// <returns>strategy with the given name</returns>
		public LodStrategy GetStrategy( string name )
		{
			// If name is "default", return the default strategy instead of performing a lookup
			if( name.ToLower() == "default" )
			{
				return DefaultStrategy;
			}

			LodStrategy ret = null;
			this._strategies.TryGetValue( name, out ret );

			return ret;
		}
	}
}
