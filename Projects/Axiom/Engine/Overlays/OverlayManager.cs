#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.FileSystem;
using Axiom.Scripting;
using Axiom.Graphics;

using Real = System.Single;

#endregion Namespace Declarations

#region Ogre Synchronization Information
/// <ogresynchronization>
///     <file name="OgreOverlayManager.h"   revision="1.23.2.1" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
///     <file name="OgreOverlayManager.cpp" revision="1.39.2.3" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
/// </ogresynchronization>
#endregion Ogre Synchronization Information

namespace Axiom.Overlays
{
	/// <summary>
	///    Manages Overlay objects, parsing them from Ogre .overlay files and
	///    storing a lookup library of them. Also manages the creation of
	///    OverlayContainers and OverlayElements, used for non-interactive 2D
	///	   elements such as HUDs.
	/// </summary>
	public sealed class OverlayManager : Singleton<OverlayManager>, IScriptLoader
	{
		#region Fields and Properties

		private List<string> _loadedScripts = new List<string>();

		#region Overlays Property

		private Dictionary<string, Overlay> _overlays = new Dictionary<string, Overlay>();
		/// <summary>
		/// returns all existing overlays
		/// </summary>
		public IEnumerator<Overlay> Overlays
		{
			get
			{
				return _overlays.Values.GetEnumerator();
			}
		}

		#endregion Overlays Property

		#region HasViewportChanged Property

		private bool _viewportDimensionsChanged;
		/// <summary>
		///		Gets if the viewport has changed dimensions.
		/// </summary>
		/// <remarks>
		///		This is used by pixel-based GuiControls to work out if they need to reclaculate their sizes.
		///	</remarks>
		public bool HasViewportChanged
		{
			get
			{
				return _viewportDimensionsChanged;
			}
		}

		#endregion HasViewportChanged Property

		#region ViewportHeight Property

		private int _lastViewportHeight;
		/// <summary>
		///		Gets the height of the destination viewport in pixels.
		/// </summary>
		public int ViewportHeight
		{
			get
			{
				return _lastViewportHeight;
			}
		}

		#endregion ViewportHeight Property

		#region ViewportWidth Property

		private int _lastViewportWidth;
		/// <summary>
		///		Gets the width of the destination viewport in pixels.
		/// </summary>
		public int ViewportWidth
		{
			get
			{
				return _lastViewportWidth;
			}
		}

		#endregion ViewportWidth Property

		#region ViewportAspectRation Property

		public Real ViewportAspectRatio
		{
			get
			{
				return (Real)_lastViewportHeight / (Real)_lastViewportWidth;
			}
		}

		#endregion ViewportAspectRation Property

		#endregion Fields and Properties

		#region Construction and Destruction

		public OverlayManager()
		{
#if !AXIOM_USENEWCOMPILERS
			// Scripting is supported by this manager
			ScriptPatterns.Add( "*.overlay" );
			ResourceGroupManager.Instance.RegisterScriptLoader( this );
#endif // AXIOM_USENEWCOMPILERS
		}

		#endregion Construction and Destruction

		#region Methods

		#region Overlay Management

		/// <summary>
		///		Creates and return a new overlay.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Overlay Create( string name )
		{
			if ( _overlays.ContainsKey( name ) )
			{
				throw new Exception( "Overlay with the name '" + name + "' already exists." );
			}

			Overlay overlay = new Overlay( name );
			if ( overlay == null )
			{
				throw new Exception( "Overlay '" + name + "' could not be created." );
			}

			_overlays.Add( name, overlay );
			return overlay;
		}

		/// <summary>
		/// Retrieve an Overlay by name
		/// </summary>
		/// <param name="name">Name of Overlay to retrieve</param>
		/// <returns>The overlay or null if not found.</returns>
		public Overlay GetByName( string name )
		{
			return _overlays.ContainsKey( name ) ? _overlays[ name ] : null;
		}

		#region Destroy*

		/// <summary>
		/// Destroys an existing overlay by name
		/// </summary>
		/// <param name="name"></param>
		public void Destroy( string name )
		{
			if ( !_overlays.ContainsKey( name ) )
			{
				LogManager.Instance.Write( "No overlay with the name '" + name + "' found to destroy." );
				return;
			}

			_overlays[ name ].Dispose();
			_overlays.Remove( name );
		}

		/// <summary>
		/// Destroys an existing overlay
		/// </summary>
		/// <param name="overlay"></param>
		public void Destroy( Overlay overlay )
		{
			if ( !_overlays.ContainsValue( overlay ) )
			{
				LogManager.Instance.Write( "Overlay '" + overlay.Name + "' not found to destroy." );
                if (!overlay.IsDisposed)
				    overlay.Dispose();
				return;
			}

			_overlays.Remove( overlay.Name );
            if (!overlay.IsDisposed)
			    overlay.Dispose();
		}

		/// <summary>
		/// Destroys all existing overlays
		/// </summary>
		public void DestroyAll()
		{
			foreach ( KeyValuePair<string, Overlay> entry in _overlays )
			{
                if (!entry.Value.IsDisposed)
				    entry.Value.Dispose();
			}
			_overlays.Clear();
		}

		#endregion Destroy*

		#endregion Overlay Management

		#region OverlayElement Management

		public OverlayElementManager Elements
		{
			get
			{
				return OverlayElementManager.Instance;
			}
		}

		#endregion OverlayElement Management

		/// <summary>
		///		Internal method for queueing the visible overlays for rendering.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="queue"></param>
		/// <param name="viewport"></param>
		internal void QueueOverlaysForRendering( Camera camera, RenderQueue queue, Viewport viewport )
		{
			// Flag for update pixel-based OverlayElements if viewport has changed dimensions
			if ( _lastViewportWidth != viewport.ActualWidth ||
				_lastViewportHeight != viewport.ActualHeight )
			{

				_viewportDimensionsChanged = true;
				_lastViewportWidth = viewport.ActualWidth;
				_lastViewportHeight = viewport.ActualHeight;
			}
			else
			{
				_viewportDimensionsChanged = false;
			}

			foreach ( Overlay overlay in _overlays.Values )
			{
				overlay.FindVisibleObjects( camera, queue );
			}
		}

		#region Script Parsing

		/// <summary>
		///    Parses an attribute belonging to an Overlay.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="overlay"></param>
		private void ParseAttrib( string line, Overlay overlay )
		{
			string[] parms = line.Split( ' ' );

			if ( parms[ 0 ].ToLower() == "zorder" )
			{
				overlay.ZOrder = int.Parse( parms[ 1 ] );
			}
			else if ( parms[ 0 ].ToLower() == "visible" )
			{
				overlay.IsVisible = StringConverter.ParseBool( parms[ 1 ] );
			}
			else
			{
				ParseHelper.LogParserError( parms[ 0 ], overlay.Name, "Invalid overlay attribute." );
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="script"></param>
		/// <param name="line"></param>
		/// <param name="overlay"></param>
		/// <param name="isTemplate"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		private bool ParseChildren( TextReader script, string line, Overlay overlay, bool isTemplate, OverlayElementContainer parent )
		{
			bool ret = false;
			int skipParam = 0;

			string[] parms = line.Split( ' ', '(', ')' );

			// split on lines with a ) will have an extra blank array element, so lets get rid of it
			if ( parms[ parms.Length - 1 ].Length == 0 )
			{
				string[] tmp = new string[ parms.Length - 1 ];
				Array.Copy( parms, 0, tmp, 0, parms.Length - 1 );
				parms = tmp;
			}

			if ( isTemplate )
			{
				// the first param = 'template' on a new child element
				if ( parms[ 0 ] == "template" )
				{
					skipParam++;
				}
			}

			// top level component cannot be an element, it must be a container unless it is a template
			if ( parms[ 0 + skipParam ] == "container" || ( parms[ 0 + skipParam ] == "element" && ( isTemplate || parent != null ) ) )
			{
				string templateName = "";
				ret = true;

				// nested container/element
				if ( parms.Length > 3 + skipParam )
				{
					if ( parms.Length != 5 + skipParam )
					{
						LogManager.Instance.Write( "Bad element/container line: {0} in {1} - {2}, expecting ':' templateName", line, parent.GetType().Name, parent.Name );
						ParseHelper.SkipToNextCloseBrace( script );
						return ret;
					}
					if ( parms[ 3 + skipParam ] != ":" )
					{
						LogManager.Instance.Write( "Bad element/container line: {0} in {1} - {2}, expecting ':' for element inheritance.", line, parent.GetType().Name, parent.Name );
						ParseHelper.SkipToNextCloseBrace( script );
						return ret;
					}

					// get the template name
					templateName = parms[ 4 + skipParam ];
				}
				else if ( parms.Length != 3 + skipParam )
				{
					LogManager.Instance.Write( "Bad element/container line: {0} in {1} - {2}, expecting 'element type(name)'.", line, parent.GetType().Name, parent.Name );
					ParseHelper.SkipToNextCloseBrace( script );
					return ret;
				}

				ParseHelper.SkipToNextOpenBrace( script );
				bool isContainer = ( parms[ 0 + skipParam ] == "container" );
				ParseNewElement( script, parms[ 1 + skipParam ], parms[ 2 + skipParam ], isContainer, overlay, isTemplate, templateName, parent );
			}

			return ret;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="line"></param>
		/// <param name="overlay"></param>
		/// <param name="element"></param>
		private void ParseElementAttrib( string line, Overlay overlay, OverlayElement element )
		{
			string[] parms = line.Split( ' ' );

			// get a string containing only the params
			string paramLine = line.Substring( line.IndexOf( ' ', 0 ) + 1 );

			// set the param, and hopefully it exists
			if ( !element.SetParam( parms[ 0 ].ToLower(), paramLine ) )
			{
				LogManager.Instance.Write( "Bad element attribute line: {0} for element '{1}'", line, element.Name );
			}
		}

		/// <summary>
		///    Overloaded.  Calls overload with default of empty template name and null for the parent container.
		/// </summary>
		/// <param name="script"></param>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <param name="isContainer"></param>
		/// <param name="overlay"></param>
		/// <param name="isTemplate"></param>
		private void ParseNewElement( TextReader script, string type, string name, bool isContainer, Overlay overlay, bool isTemplate )
		{
			ParseNewElement( script, type, name, isContainer, overlay, isTemplate, "", null );
		}

		/// <summary>
		///    Parses a new element
		/// </summary>
		/// <param name="script"></param>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <param name="isContainer"></param>
		/// <param name="overlay"></param>
		/// <param name="isTemplate"></param>
		/// <param name="templateName"></param>
		/// <param name="parent"></param>
		private void ParseNewElement( TextReader script, string type, string name, bool isContainer, Overlay overlay, bool isTemplate,
			string templateName, OverlayElementContainer parent )
		{

			string line;
			OverlayElement element = OverlayElementManager.Instance.CreateElementFromTemplate( templateName, type, name, isTemplate );

			if ( parent != null )
			{
				// add this element to the parent container
				parent.AddChild( element );
			}
			else if ( overlay != null )
			{
				overlay.AddElement( (OverlayElementContainer)element );
			}

			while ( ( line = ParseHelper.ReadLine( script ) ) != null )
			{
				// inore blank lines and comments
				if ( line.Length > 0 && ( !line.StartsWith( "//" ) && !line.StartsWith( "# " ) ) )
				{
					if ( line == "}" )
					{
						// finished element
						break;
					}
					else
					{
						OverlayElementContainer container = null;
						if ( element is OverlayElementContainer )
						{
							container = (OverlayElementContainer)element;
						}
						if ( isContainer && ParseChildren( script, line, overlay, isTemplate, container ) )
						{
							// nested children, so don't reparse it
						}
						else
						{
							// element attribute
							ParseElementAttrib( line, overlay, element );
						}
					}
				}
			}
		}

		#endregion Script Parsing

		#endregion Methods

		#region Singleton<OverlayManager> Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					OverlayElementManager.Instance.DestroyAllElements( false );
					OverlayElementManager.Instance.DestroyAllElements( true );
					DestroyAll();

					// Unregister with resource group manager
					ResourceGroupManager.Instance.UnregisterScriptLoader( this );
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		public override bool Initialize( params object[] args )
		{
			return base.Initialize( args );
		}

		#endregion Singleton<OverlayManager> Implementation

		#region IScriptLoader Members

		private List<string> _scriptPatterns = new List<string>();
		public List<string> ScriptPatterns
		{
			get
			{
				return _scriptPatterns;
			}
		}

		public void ParseScript( Stream stream, string groupName, string fileName )
		{
			string line = "";
			Overlay overlay = null;
			bool skipLine;

			if ( _loadedScripts.Contains( fileName ) )
			{
				LogManager.Instance.Write( "Skipping load of overlay include: {0}, as it is already loaded.", fileName );
				return;
			}

			// parse the overlay script
			StreamReader script = new StreamReader( stream, System.Text.Encoding.UTF8 );

			// keep reading the file until we hit the end
			while ( ( line = ParseHelper.ReadLine( script ) ) != null )
			{
				bool isTemplate = false;
				skipLine = false;

				// ignore comments and blank lines
				if ( line.Length > 0 && ( !line.StartsWith( "//" ) && !line.StartsWith( "# " ) ) )
				{
					// does another overlay have to be included
					if ( line.StartsWith( "#include" ) )
					{

						string[] parms = line.Split( ' ', '(', ')', '<', '>' );
						// split on lines with a ) will have an extra blank array element, so lets get rid of it
						if ( parms[ parms.Length - 1 ].Length == 0 )
						{
							string[] tmp = new string[ parms.Length - 1 ];
							Array.Copy( parms, 0, tmp, 0, parms.Length - 1 );
							parms = tmp;
						}
						string includeFile = parms[ 2 ];

						Stream data = ResourceGroupManager.Instance.OpenResource( includeFile );
						ParseScript( data, groupName, includeFile );
						data.Close();

						continue;
					}

					if ( overlay == null )
					{
						// no current overlay
						// check to see if there is a template
						if ( line.StartsWith( "template" ) )
						{
							isTemplate = true;
						}
						else
						{
							// the line in this case should be the name of the overlay
							overlay = Create( line );
							//this is just telling the file name of the overlay
							overlay.Origin = fileName;
							// cause the next line (open brace) to be skipped
							ParseHelper.SkipToNextOpenBrace( script );
							skipLine = true;
						}
					}

					if ( ( overlay != null && !skipLine ) || isTemplate )
					{
						// already in overlay
						string[] parms = line.Split( ' ', '(', ')' );

						// split on lines with a ) will have an extra blank array element, so lets get rid of it
						if ( parms[ parms.Length - 1 ].Length == 0 )
						{
							string[] tmp = new string[ parms.Length - 1 ];
							Array.Copy( parms, 0, tmp, 0, parms.Length - 1 );
							parms = tmp;
						}

						if ( line == "}" )
						{
							// finished overlay
							overlay = null;
							isTemplate = false;
						}
						else if ( ParseChildren( script, line, overlay, isTemplate, null ) )
						{
						}
						else
						{
							// must be an attribute
							if ( !isTemplate )
							{
								ParseAttrib( line, overlay );
							}
						}
					}
				}
			}
		}

		public float LoadingOrder
		{
			get
			{
				// Load Late
				return 1100.0f;
			}
		}

		#endregion IScriptLoader Members

	}
}