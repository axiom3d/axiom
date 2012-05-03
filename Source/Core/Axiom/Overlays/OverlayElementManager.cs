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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Overlays.Elements;
using Axiom.Utilities;

#endregion Namespace Declarations

#region Ogre Synchronization Information

// <ogresynchronization>
//     <file name="OgreOverlayManager.h"   revision="1.23.2.1" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
//     <file name="OgreOverlayManager.cpp" revision="1.39.2.3" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
// </ogresynchronization>

#endregion

namespace Axiom.Overlays
{
	/// <summary>
	///    This class acts as a repository and regitrar of overlay components.
	/// </summary>
	/// <remarks>
	///    OverlayElementManager's job is to manage the lifecycle of OverlayElement (subclass)
	///    instances, and also to register plugin suppliers of new components.
	/// </remarks>
	public sealed class OverlayElementManager : DisposableObject
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static OverlayElementManager instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		internal OverlayElementManager()
			: base()
		{
			if ( instance == null )
			{
				instance = this;

				// register the default overlay element factories
				instance.AddElementFactory( new BorderPanelFactory() );
				instance.AddElementFactory( new TextAreaFactory() );
				instance.AddElementFactory( new PanelFactory() );
			}
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		internal static OverlayElementManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

		#region Fields & Properties

		private readonly Dictionary<string, IOverlayElementFactory> _elementFactories =
			new Dictionary<string, IOverlayElementFactory>();

		#region Instances Property

		private readonly Dictionary<string, OverlayElement> _elementInstances = new Dictionary<string, OverlayElement>();

		/// <summary>
		/// returns all elemnt instances
		/// </summary>
		public IEnumerable<OverlayElement> Instances
		{
			get
			{
				return _elementInstances.Values;
			}
		}

		#endregion Instances Property

		#region Templates Property

		private readonly Dictionary<string, OverlayElement> _elementTemplates = new Dictionary<string, OverlayElement>();

		/// <summary>
		/// returns all element templates
		/// </summary>
		public IEnumerable<OverlayElement> Templates
		{
			get
			{
				return _elementTemplates.Values;
			}
		}

		#endregion Templates Property

		#endregion Fields & Properties

		#region Methods

		/// <summary>
		///     Registers a new OverlayElementFactory with this manager.
		/// </summary>
		/// <remarks>
		///    Should be used by plugins or other apps wishing to provide
		///    a new OverlayElement subclass.
		/// </remarks>
		/// <param name="factory"></param>
		public void AddElementFactory( IOverlayElementFactory factory )
		{
			_elementFactories.Add( factory.Type, factory );

			LogManager.Instance.Write( "OverlayElementFactory for type '{0}' registered.", factory.Type );
		}

		#region Creat* Methods

		/// <summary>
		///    Creates a new OverlayElement of the type requested.
		/// </summary>
		/// <param name="typeName">The type of element to create is passed in as a string because this
		///    allows plugins to register new types of component.</param>
		/// <param name="instanceName">The type of element to create.</param>
		/// <returns></returns>
		public OverlayElement CreateElement( string typeName, string instanceName )
		{
			return CreateElement( typeName, instanceName, false );
		}

		/// <summary>
		///    Creates a new OverlayElement of the type requested.
		/// </summary>
		/// <param name="typeName">The type of element to create is passed in as a string because this
		///    allows plugins to register new types of component.</param>
		/// <param name="instanceName">The type of element to create.</param>
		/// <param name="isTemplate"></param>
		/// <returns></returns>
		public OverlayElement CreateElement( string typeName, string instanceName, bool isTemplate )
		{
			var elements = GetElementTable( isTemplate );

			if ( elements.ContainsKey( instanceName ) )
			{
				//throw new AxiomException( "OverlayElement with the name '{0}' already exists.", instanceName );
				return (OverlayElement)elements[ instanceName ];
			}

			var element = CreateElementFromFactory( typeName, instanceName );

			// register
			elements.Add( instanceName, element );

			return element;
		}

		/// <summary>
		///    Creates an element of the specified type, with the specified name
		/// </summary>
		/// <remarks>
		///    A factory must be available to handle the requested type, or an exception will be thrown.
		/// </remarks>
		/// <param name="typeName"></param>
		/// <param name="instanceName"></param>
		/// <returns></returns>
		public OverlayElement CreateElementFromFactory( string typeName, string instanceName )
		{
			if ( !_elementFactories.ContainsKey( typeName ) )
			{
				throw new AxiomException( "Cannot locate factory for element type '{0}'", typeName );
			}

			// create the element
			return ( (IOverlayElementFactory)_elementFactories[ typeName ] ).Create( instanceName );
		}

		/// <summary>
		/// </summary>
		public OverlayElement CreateElementFromTemplate( string templateName, string typeName, string instanceName )
		{
			return CreateElementFromTemplate( templateName, typeName, instanceName, false );
		}

		/// <summary>
		/// </summary>
		public OverlayElement CreateElementFromTemplate( string templateName, string typeName, string instanceName,
		                                                 bool isTemplate )
		{
			OverlayElement element = null;

			if ( String.IsNullOrEmpty( templateName ) )
			{
				element = CreateElement( typeName, instanceName, isTemplate );
			}
			else
			{
				var template = GetElement( templateName, true );

				var typeToCreate = "";
				if ( String.IsNullOrEmpty( typeName ) )
				{
					typeToCreate = template.GetType().Name;
				}
				else
				{
					typeToCreate = typeName;
				}

				element = CreateElement( typeToCreate, instanceName, isTemplate );

				// Copy settings from template
				element.CopyFromTemplate( template );
			}

			return element;
		}

		#endregion Creat* Methods

		/// <summary>
		/// Clones an overlay element from a template
		/// </summary>
		/// <param name="template">template to clone</param>
		/// <param name="name">name of the new element</param>
		/// <returns></returns>
		public OverlayElement CloneOverlayElementFromTemplate( string template, string name )
		{
			var element = GetElement( template, true );
			return element.Clone( name );
		}

		/// <summary>
		///    Gets a reference to an existing element.
		/// </summary>
		/// <param name="name">Name of the element to retrieve.</param>
		/// <returns></returns>
		public OverlayElement GetElement( string name )
		{
			return GetElement( name, false );
		}

		/// <summary>
		///    Gets a reference to an existing element.
		/// </summary>
		/// <param name="name">Name of the element to retrieve.</param>
		/// <param name="isTemplate"></param>
		/// <returns></returns>
		public OverlayElement GetElement( string name, bool isTemplate )
		{
			Contract.RequiresNotEmpty( name, "name" );

			var elements = GetElementTable( isTemplate );

			if ( !elements.ContainsKey( name ) )
			{
				LogManager.Instance.Write( "OverlayElement with the name'{0}' was not found.", name );
				return null;
			}
			else
			{
				return (OverlayElement)elements[ name ];
			}
		}

		/// <summary>
		///    Quick helper method to return the lookup table for the right element type.
		/// </summary>
		/// <param name="isTemplate"></param>
		/// <returns></returns>
		private Dictionary<string, OverlayElement> GetElementTable( bool isTemplate )
		{
			return isTemplate ? _elementTemplates : _elementInstances;
		}

		#region Destroy*OverlayElement

		/// <summary>
		/// Destroys the specified OverlayElement
		/// </summary>
		/// <param name="name"></param>
		public void DestroyElement( string name )
		{
			DestroyElement( name, false );
		}

		/// <summary>
		/// Destroys the specified OverlayElement
		/// </summary>
		/// <param name="name"></param>
		/// <param name="isTemplate"></param>
		public void DestroyElement( string name, bool isTemplate )
		{
			var elements = isTemplate ? _elementTemplates : _elementInstances;
			if ( !elements.ContainsKey( name ) )
			{
				throw new Exception( "OverlayElement with the name '" + name + "' not found to destroy." );
			}
			elements[ name ].Dispose();
			elements.Remove( name );
		}

		/// <summary>
		/// Destroys the supplied OvelayElement
		/// </summary>
		/// <param name="element"></param>
		public void DestroyElement( OverlayElement element )
		{
			DestroyElement( element, false );
		}

		/// <summary>
		/// Destroys the supplied OvelayElement
		/// </summary>
		/// <param name="element"></param>
		/// <param name="isTemplate"></param>
		public void DestroyElement( OverlayElement element, bool isTemplate )
		{
			var elements = isTemplate ? _elementTemplates : _elementInstances;
			if ( !elements.ContainsValue( element ) )
			{
				throw new Exception( "OverlayElement with the name '" + element.Name + "' not found to destroy." );
			}

			elements.Remove( element.Name );
		}

		/// <summary>
		/// destroys all OverlayElements
		/// </summary>
		public void DestroyAllElements()
		{
			DestroyAllElements( false );
		}

		/// <summary>
		/// destroys all OverlayElements
		/// </summary>
		public void DestroyAllElements( bool isTemplate )
		{
			( isTemplate ? _elementTemplates : _elementInstances ).Clear();
		}

		#endregion Destroy*OverlayElement

		#endregion Methods

		#region IDisposable Implementation

		/// <summary>
		///     Called when the engine is shutting down.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					instance = null;
				}
			}

			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	}
}