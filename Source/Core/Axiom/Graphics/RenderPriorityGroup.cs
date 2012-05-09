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
//     <id value="$Id $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;
using Axiom.Core;
using Axiom.Graphics;
using System.Collections.Generic;
using Axiom.Graphics.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///		IRenderables in the queue grouped by priority.
	/// </summary>
	/// <remarks>
	///		This class simply groups renderables for rendering. All the 
	///		renderables contained in this class are destined for the same
	///		RenderQueueGroup (coarse groupings like those between the main
	///		scene and overlays) and have the same priority (fine groupings
	///		for detailed overlap control).
	/// </remarks>
	public class RenderPriorityGroup
	{
		#region Fields

		private readonly RenderQueueGroup _parent;

		/// <summary>
		/// 
		/// </summary>
		protected internal List<RenderablePass> transparentPasses = new List<RenderablePass>();

		/// <summary>
		///		Solid pass list, used when no shadows, modulative shadows, or ambient passes for additive.
		/// </summary>
		protected internal SortedList solidPasses;

		/// <summary>
		///		Solid per-light pass list, used with additive shadows.
		/// </summary>
		protected internal SortedList solidPassesDiffuseSpecular;

		/// <summary>
		///		Solid decal (texture) pass list, used with additive shadows.
		/// </summary>
		protected internal SortedList solidPassesDecal;

		/// <summary>
		///		Solid pass list, used when shadows are enabled but shadow receive is turned off for these passes.
		/// </summary>
		protected internal SortedList solidPassesNoShadow;

		/// <summary>
		///		Should passes be split by their lighting stage?
		/// </summary>
		protected bool splitPassesByLightingType;

		protected bool splitNoShadowPasses;
		protected bool shadowCastersCannotBeReceivers;

		#endregion Fields

		#region Constructor

		/// <summary>
		///    Default constructor.
		/// </summary>
		internal RenderPriorityGroup( RenderQueueGroup parent, bool splitPassesByLightingType, bool splitNoShadowPasses,
		                              bool shadowCastersCannotBeReceivers )
		{
			this._parent = parent;
			// sorted list, using Pass as a key (sorted based on hashcode), and IRenderable as the value
			this.solidPasses = new SortedList( new SolidSort(), 50 );
			this.solidPassesDiffuseSpecular = new SortedList( new SolidSort(), 50 );
			this.solidPassesDecal = new SortedList( new SolidSort(), 50 );
			this.solidPassesNoShadow = new SortedList( new SolidSort(), 50 );
			this.splitPassesByLightingType = splitPassesByLightingType;
			this.splitNoShadowPasses = splitNoShadowPasses;
			this.shadowCastersCannotBeReceivers = shadowCastersCannotBeReceivers;
		}

		#endregion Constructor

		#region Methods

		/// <summary>
		///		Add a renderable to this group.
		/// </summary>
		/// <param name="renderable">Renderable to add to the queue.</param>
		/// <param name="technique"></param>
		public void AddRenderable( IRenderable renderable, Technique technique )
		{
			// Transparent and depth/colour settings mean depth sorting is required?
			// Note: colour write disabled with depth check/write enabled means
			//       setup depth buffer for other passes use.

			if ( technique.IsTransparent && ( !technique.DepthWrite || !technique.DepthCheck || technique.ColorWriteEnabled ) )
			{
				AddTransparentRenderable( technique, renderable );
			}
			else
			{
				if ( this.splitNoShadowPasses && this._parent.ShadowsEnabled &&
				     ( !technique.Parent.ReceiveShadows || renderable.CastsShadows && this.shadowCastersCannotBeReceivers ) )
				{
					// Add solid renderable and add passes to no-shadow group
					AddSolidRenderable( technique, renderable, true );
				}
				else
				{
					if ( this.splitPassesByLightingType && this._parent.ShadowsEnabled )
					{
						AddSolidRenderableSplitByLightType( technique, renderable );
					}
					else
					{
						AddSolidRenderable( technique, renderable, false );
					}
				}
			}
		}

		/// <summary>
		///		Internal method for adding a solid renderable
		/// </summary>
		/// <param name="technique">Technique to use for this renderable.</param>
		/// <param name="renderable">Renderable to add to the queue.</param>
		/// <param name="noShadows">True to add to the no shadow group, false otherwise.</param>
		protected void AddSolidRenderable( Technique technique, IRenderable renderable, bool noShadows )
		{
			SortedList passMap = null;

			if ( noShadows )
			{
				passMap = this.solidPassesNoShadow;
			}
			else
			{
				passMap = this.solidPasses;
			}

			for ( var i = 0; i < technique.PassCount; i++ )
			{
				var pass = technique.GetPass( i );

				if ( passMap[ pass ] == null )
				{
					// add a new list to hold renderables for this pass
					passMap.Add( pass, new RenderableList() );
				}

				// add to solid list for this pass
				var solidList = (RenderableList)passMap[ pass ];

				solidList.Add( renderable );
			}
		}

		/// <summary>
		///		Internal method for adding a solid renderable ot the group based on lighting stage.
		/// </summary>
		/// <param name="technique">Technique to use for this renderable.</param>
		/// <param name="renderable">Renderable to add to the queue.</param>
		protected void AddSolidRenderableSplitByLightType( Technique technique, IRenderable renderable )
		{
			// Divide the passes into the 3 categories
			for ( var i = 0; i < technique.IlluminationPassCount; i++ )
			{
				// Insert into solid list
				var illpass = technique.GetIlluminationPass( i );
				SortedList passMap = null;

				switch ( illpass.Stage )
				{
					case IlluminationStage.Ambient:
						passMap = this.solidPasses;
						break;
					case IlluminationStage.PerLight:
						passMap = this.solidPassesDiffuseSpecular;
						break;
					case IlluminationStage.Decal:
						passMap = this.solidPassesDecal;
						break;
				}

				var solidList = (RenderableList)passMap[ illpass.Pass ];

				if ( solidList == null )
				{
					// add a new list to hold renderables for this pass
					solidList = new RenderableList();
					passMap.Add( illpass.Pass, solidList );
				}

				solidList.Add( renderable );
			}
		}

		/// <summary>
		///		Internal method for adding a transparent renderable.
		/// </summary>
		/// <param name="technique">Technique to use for this renderable.</param>
		/// <param name="renderable">Renderable to add to the queue.</param>
		protected void AddTransparentRenderable( Technique technique, IRenderable renderable )
		{
			for ( var i = 0; i < technique.PassCount; i++ )
			{
				// add to transparent list
				this.transparentPasses.Add( new RenderablePass( renderable, technique.GetPass( i ) ) );
			}
		}

		/// <summary>
		///		Clears all the internal lists.
		/// </summary>
		public void Clear()
		{
			var graveyardList = Pass.GraveyardList;

			// Delete queue groups which are using passes which are to be
			// deleted, we won't need these any more and they clutter up 
			// the list and can cause problems with future clones
			for ( var i = 0; i < graveyardList.Count; i++ )
			{
				RemoveSolidPassEntry( (Pass)graveyardList[ i ] );
			}

			// Now remove any dirty passes, these will have their hashes recalculated
			// by the parent queue after all groups have been processed
			// If we don't do this, the std::map will become inconsistent for new insterts
			var dirtyList = Pass.DirtyList;

			// Delete queue groups which are using passes which are to be
			// deleted, we won't need these any more and they clutter up 
			// the list and can cause problems with future clones
			for ( var i = 0; i < dirtyList.Count; i++ )
			{
				RemoveSolidPassEntry( (Pass)dirtyList[ i ] );
			}

			// We do NOT clear the graveyard or the dirty list here, because 
			// it needs to be acted on for all groups, the parent queue takes 
			// care of this afterwards

			// We do not clear the unchanged solid pass maps, only the contents of each list
			// This is because we assume passes are reused a lot and it saves resorting
			ClearSolidPassMap( this.solidPasses );
			ClearSolidPassMap( this.solidPassesDiffuseSpecular );
			ClearSolidPassMap( this.solidPassesDecal );
			ClearSolidPassMap( this.solidPassesNoShadow );

			// Always empty the transparents list
			this.transparentPasses.Clear();
		}

		public void ClearSolidPassMap( SortedList list )
		{
			// loop through and clear the renderable containers for the stored passes
			for ( var i = 0; i < list.Count; i++ )
			{
				( (RenderableList)list.GetByIndex( i ) ).Clear();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Pass GetSolidPass( int index )
		{
			Debug.Assert( index < this.solidPasses.Count, "index < solidPasses.Count" );
			return (Pass)this.solidPasses.GetKey( index );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public RenderableList GetSolidPassRenderables( int index )
		{
			Debug.Assert( index < this.solidPasses.Count, "index < solidPasses.Count" );
			return (RenderableList)this.solidPasses.GetByIndex( index );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public RenderablePass GetTransparentPass( int index )
		{
			Debug.Assert( index < this.transparentPasses.Count, "index < transparentPasses.Count" );
			return (RenderablePass)this.transparentPasses[ index ];
		}

		/// <summary>
		///    Sorts the objects which have been added to the queue; transparent objects by their 
		///    depth in relation to the passed in Camera, solid objects in order to minimize
		///    render state changes.
		/// </summary>
		/// <remarks>
		///    Solid passes are already stored in a sorted structure, so nothing extra needed here.
		/// </remarks>
		/// <param name="camera">Current camera to use for depth sorting.</param>
		public void Sort( Camera camera )
		{
			// sort the transparent objects using the custom IComparer
			this.transparentPasses.Sort( new TransparencySort( camera ) );
		}

		/// <summary>
		///		Remove a pass entry from all solid pass maps
		/// </summary>
		/// <param name="pass">Reference to the pass to remove.</param>
		public void RemoveSolidPassEntry( Pass pass )
		{
			if ( this.solidPasses[ pass ] != null )
			{
				this.solidPasses.Remove( pass );
			}

			if ( this.solidPassesDecal[ pass ] != null )
			{
				this.solidPassesDecal.Remove( pass );
			}

			if ( this.solidPassesDiffuseSpecular[ pass ] != null )
			{
				this.solidPassesDiffuseSpecular.Remove( pass );
			}

			if ( this.solidPassesNoShadow[ pass ] != null )
			{
				this.solidPassesNoShadow.Remove( pass );
			}
		}

		#endregion

		#region Properties

		/// <summary>
		///    Gets the number of non-transparent passes for this priority group.
		/// </summary>
		public int NumSolidPasses
		{
			get
			{
				return this.solidPasses.Count;
			}
		}

		/// <summary>
		///    Gets the number of transparent passes for this priority group.
		/// </summary>
		public int NumTransparentPasses
		{
			get
			{
				return this.transparentPasses.Count;
			}
		}

		/// <summary>
		///		Gets/Sets whether or not the queue will split passes by their lighting type,
		///		ie ambient, per-light and decal. 
		/// </summary>
		public bool SplitPassesByLightingType
		{
			get
			{
				return this.splitPassesByLightingType;
			}
			set
			{
				this.splitPassesByLightingType = value;
			}
		}

		/// <summary>
		///		Gets/Sets whether or not the queue will split passes which have shadow receive
		///		turned off (in their parent material), which is needed when certain shadow
		///		techniques are used.
		/// </summary>
		public bool SplitNoShadowPasses
		{
			get
			{
				return this.splitNoShadowPasses;
			}
			set
			{
				this.splitNoShadowPasses = value;
			}
		}

		/// <summary>
		///		Gets/Sets whether or not the queue will disallow receivers when certain shadow
		///		techniques are used.
		/// </summary>
		public bool ShadowCastersCannotBeReceivers
		{
			get
			{
				return this.shadowCastersCannotBeReceivers;
			}
			set
			{
				this.shadowCastersCannotBeReceivers = value;
			}
		}

		#endregion

		#region Internal classes

		/// <summary>
		/// 
		/// </summary>
		private class SolidSort : IComparer
		{
			#region IComparer Members

			public int Compare( object x, object y )
			{
				if ( x == null || y == null )
				{
					return 0;
				}

				// if they are the same, return 0
				if ( x == y )
				{
					return 0;
				}

				var a = x as Pass;
				var b = y as Pass;

				if ( a == null || b == null )
				{
					return 0;
				}

				// sorting by pass hash
				if ( a.GetHashCode() == b.GetHashCode() )
				{
					return ( a.passId < b.passId ) ? -1 : 1;
				}
				return ( a.GetHashCode() < b.GetHashCode() ) ? -1 : 1;
			}

			#endregion
		}

		/// <summary>
		///		Nested class that implements IComparer for transparency sorting.
		/// </summary>
		private class TransparencySort : IComparer<RenderablePass>
		{
			private readonly Camera camera;

			public TransparencySort( Camera camera )
			{
				this.camera = camera;
			}

			#region IComparer<RenderablePass> Members

			public int Compare( RenderablePass x, RenderablePass y )
			{
				if ( x == null || y == null )
				{
					return 0;
				}

				// if they are the same, return 0
				if ( x == y )
				{
					return 0;
				}

				var adepth = x.renderable.GetSquaredViewDepth( this.camera );
				var bdepth = y.renderable.GetSquaredViewDepth( this.camera );

				if ( adepth == bdepth )
				{
					if ( x.pass.GetHashCode() < y.pass.GetHashCode() )
					{
						return 1;
					}
					else
					{
						return -1;
					}
				}
				else
				{
					// sort descending by depth, meaning further objects get drawn first
					if ( adepth < bdepth )
					{
						return 1;
					}
					else
					{
						return -1;
					}
				}
			}

			#endregion
		}

		#endregion
	}
}