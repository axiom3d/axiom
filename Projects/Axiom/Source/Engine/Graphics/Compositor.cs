#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team
 
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
/*
 * Many thanks to the folks at Multiverse for providing the initial port for this class
 */
#endregion

#region SVN Version Information
// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
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
using Axiom.Configuration;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{

	///<summary>
	///    Class representing a Compositor object. Compositors provide the means 
	///    to flexibly "composite" the final rendering result from multiple scene renders
	///    and intermediate operations like rendering fullscreen quads. This makes 
	///    it possible to apply postfilter effects, HDRI postprocessing, and shadow 
	///    effects to a Viewport.
	///</summary>
	public class Compositor : Resource
	{

		#region Fields

		protected List<CompositionTechnique> techniques;

		protected List<CompositionTechnique> supportedTechniques;

		///<summary>
		///     This is set if the techniques change and the supportedness of techniques has to be
		///     re-evaluated.
		///</summary>
		protected bool compilationRequired;

		/// <summary>
		///    Auto incrementing number for creating unique names.
		/// </summary>
		static protected int autoNumber;

		#endregion Fields

		#region Constructors

		public Compositor( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			techniques = new List<CompositionTechnique>();
			supportedTechniques = new List<CompositionTechnique>();
			this.compilationRequired = true;
		}

		#endregion Constructors

		#region Properties

		public List<CompositionTechnique> Techniques
		{
			get
			{
				return techniques;
			}
		}

		public List<CompositionTechnique> SupportedTechniques
		{
			get
			{
				return supportedTechniques;
			}
		}

		#endregion Properties

		#region Implementation of Resource

		//public override void Preload()
		//{
		//    if ( !isLoaded )
		//    {
		//        // compile if needed
		//        if ( compilationRequired )
		//            Compile();
		//    }
		//}

		/// <summary>
		///		Overridden from Resource.
		/// </summary>
		/// <remarks>
		///		By default, Materials are not loaded, and adding additional textures etc do not cause those
		///		textures to be loaded. When the <code>Load</code> method is called, all textures are loaded (if they
		///		are not already), GPU programs are created if applicable, and Controllers are instantiated.
		///		Once a material has been loaded, all changes made to it are immediately loaded too
		/// </remarks>
		protected override void load()
		{
			if ( !IsLoaded )
			{
				// compile if needed
				if ( compilationRequired )
				{
					Compile();
				}
			}
		}

		/// <summary>
		///		Unloads the material, frees resources etc.
		///		<see cref="Resource"/>
		/// </summary>
		protected override void unload()
		{
		}

		/// <summary>
		///	    Disposes of any resources used by this object.	
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( IsLoaded )
						Unload();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			isDisposed = true;

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}


		/// <summary>
		///    Overridden to ensure a recompile occurs if needed before use.
		/// </summary>
		public override void Touch()
		{
			if ( compilationRequired )
			{
				Compile();
			}

			// call base class
			base.Touch();
		}

		#endregion

		#region Methods

		///<summary>
		///    Create a new technique, and return a pointer to it.
		///</summary
		public CompositionTechnique CreateTechnique()
		{
			CompositionTechnique t = new CompositionTechnique( this );
			techniques.Add( t );
			compilationRequired = true;
			return t;
		}

		///<summary>
		///    Remove a technique.
		///</summary
		public void RemoveTechnique( int idx )
		{
			techniques.RemoveAt( idx );
			supportedTechniques.Clear();
			compilationRequired = true;
		}

		///<summary>
		///    Get a technique.
		///</summary
		public CompositionTechnique GetTechnique( int idx )
		{
			return techniques[ idx ];
		}

		///<summary>
		///    Get a supported technique.
		///</summary
		///<remarks>
		///    The supported technique list is only available after this compositor has been compiled,
		///    which typically happens on loading it. Therefore, if this method returns
		///    an empty list, try calling Compositor.Load.
		///</remarks>
		public CompositionTechnique GetSupportedTechnique( int idx )
		{
			return supportedTechniques[ idx ];
		}

		///<summary>
		///    Remove all techniques.
		///</summary
		public void RemoveAllTechniques()
		{
			techniques.Clear();
			supportedTechniques.Clear();
			compilationRequired = true;
		}

		///<summary>
		///    Check supportedness of techniques.
		///</summary
		protected void Compile()
		{
			/// Sift out supported techniques
			supportedTechniques.Clear();
			// Try looking for exact technique support with no texture fallback
			foreach ( CompositionTechnique t in techniques )
			{
				// Look for exact texture support first
				if ( t.IsSupported( false ) )
					supportedTechniques.Add( t );
			}

			if ( supportedTechniques.Count == 0 )
			{
				// Check again, being more lenient with textures
				foreach ( CompositionTechnique t in techniques )
				{
					// Allow texture support with degraded pixel format
					if ( t.IsSupported( true ) )
						supportedTechniques.Add( t );
				}
			}
			compilationRequired = false;
		}

		#endregion Methods

	}
}
