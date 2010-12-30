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
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Core;
using Axiom.Graphics;
using System.Collections.Generic;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// Visitor interface for items in a QueuedRenderableCollection.
	/// </summary>
	/// <remarks>
	/// Those wishing to iterate over the items in a 
	/// QueuedRenderableCollection should implement this visitor pattern,
	/// since internal organization of the collection depends on the 
	/// sorting method in use.
	/// </remarks>
	public interface IQueuedRenderableVisitor
	{
		/// <summary>
		/// Called when visiting a RenderablePass, ie items in a
		/// sorted collection where items are not grouped by pass.
		/// </summary>
		/// <remarks>
		/// If this is called, neither of the other 2 visit methods	will be called.
		/// </remarks>
		/// <param name="renderablePass"></param>
		void Visit( RenderablePass renderablePass );

		/// <summary>
		/// When visiting a collection grouped by pass, this is	called when the grouping pass changes.
		/// </summary>
		/// <remarks>
		/// If this method is called, the RenderablePass visit 
		/// method will not be called for this collection. The 
		/// Renderable visit method will be called for each item
		/// underneath the pass grouping level.
		/// </remarks>
		/// <param name="pass"></param>
		/// <returns>True to continue, false to skip the Renderables underneath</returns>
		bool Visit( Pass pass );

		/// <summary>
		/// Visit method called once per Renderable on a grouped collection.
		/// </summary>
		/// <remarks>
		/// If this method is called, the RenderablePass visit 
		/// method will not be called for this collection.
		/// </remarks>
		/// <param name="renderable"></param>
		void Visit( IRenderable renderable );


	};


	///<summary>Lowest level collection of renderables.</summary>
	///<remarks>
	///To iterate over items in this collection, you must call
	///the accept method and supply a <see cref="QueuedRenderableVisitor"/>,
	///the order of the iteration, and whether that iteration is
	///over a <see cref="RenderablePass"/> list or a 2-level grouped list which 
	///causes a visit call at the <see cref="Pass"/> level, and a call for each
	///<see cref="Renderable"/> underneath.
	///</remarks>
	public class QueuedRenderableCollection
	{
		#region Constants and Enumerations

		/// <summary>
		/// Organization modes required for this collection.
		/// </summary>
		/// <remarks>
		/// This affects the internal placement of the items added to this collection;
		/// if only one type of sorting / grouping is to be required, then renderables
		/// can be stored only once, whilst if multiple types are going to be needed
		/// then internally there will be multiple organizations. Changing the organization
		/// needs to be done when the collection is empty.
		/// </remarks>
		[Flags()]
		public enum OrganizationMode
		{
			/// Group by pass
			GroupByPass = 1,
			/// Sort descending camera distance
			Descending = 2,
			/// <summary>
			/// Sort ascending camera distance 
			/// </summary>
			/// <remarks>
			/// Note value overlaps with descending since both use same sort
			/// </remarks>
			Ascending = 6
		};

		#endregion Constants and Enumerations

		#region Classes and Structures

		class PassGroupComparer : IComparer<Pass>
		{
			#region IComparer<Pass> Members

			public int Compare( Pass a, Pass b )
			{
				if ( a == b )
					return 0;

				// sorting by pass hash
				if ( a.GetHashCode() == b.GetHashCode() )
					return ( a.passId < b.passId ) ? -1 : 1;
				return ( a.GetHashCode() < b.GetHashCode() ) ? -1 : 1;
			}

			#endregion

		}

		class DepthSortDescendingComparer : IComparer<RenderablePass>
		{

			#region Fields and Properties

			public Camera Camera;

			#endregion Fields and Properties

			#region Construction and Destruction

			public DepthSortDescendingComparer()
			{
			}

			#endregion Construction and Destruction

			#region IComparer<RenderablePass> Members

			public int Compare( RenderablePass x, RenderablePass y )
			{
				if ( x == null || y == null )
					return 0;

				// if they are the same, return 0
				if ( x == y )
					return 0;

				float adepth = x.renderable.GetSquaredViewDepth( Camera );
				float bdepth = y.renderable.GetSquaredViewDepth( Camera );

				if ( adepth == bdepth )
				{
					return ( x.pass.GetHashCode() < y.pass.GetHashCode() ) ? 1 : -1;
				}
				else
				{
					// sort descending by depth, meaning further objects get drawn first
					return ( adepth < bdepth ) ? 1 : -1;
				}
			}

			#endregion
		}

		#endregion Classes and Structures

		#region Fields and Properties

		/// Radix sorter for accessing sort value 1 (Pass)
		private static RadixSortUInt32<List<RenderablePass>, RenderablePass> _radixSorter1 = new RadixSortUInt32<List<RenderablePass>, RenderablePass>();
		/// Radix sorter for sort value 2 (distance)
		private static RadixSortSingle<List<RenderablePass>, RenderablePass> _radixSorter2 = new RadixSortSingle<List<RenderablePass>, RenderablePass>();

		/// Bitmask of the organization modes requested
		OrganizationMode _organizationMode;

		/// Grouped 
		AxiomSortedCollection<Pass, List<IRenderable>> _grouped = new AxiomSortedCollection<Pass, List<IRenderable>>( new PassGroupComparer() );

		/// Sorted descending (can iterate backwards to get ascending)
		List<RenderablePass> _sortedDescending = new List<RenderablePass>();
		DepthSortDescendingComparer _defaultDepthSortComparer = new DepthSortDescendingComparer();

		#endregion Fields and Properties

		#region Construction and Destruction

		public QueuedRenderableCollection()
		{

		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Empty the collection
		/// </summary>
		public void Clear()
		{
			foreach ( KeyValuePair<Pass, List<IRenderable>> item in _grouped )
			{
				// Clear the list associated with this pass, but leave the pass entry
				item.Value.Clear();
			}
			// Clear sorted list
			_sortedDescending.Clear();
		}

		/// <summary>
		/// Remove the group entry (if any) for a given Pass.
		/// </summary>
		/// <remarks>
		/// To be used when a pass is destroyed, such that any
		/// grouping level for it becomes useless.
		/// </remarks>
		/// <param name="pass"></param>
		public void RemovePassGroup( Pass pass )
		{
			if ( _grouped.ContainsKey( pass ) )
			{
				_grouped[ pass ].Clear();
				_grouped.Remove( pass );
			}
		}

		/// <summary>
		/// Reset the organisation modes required for this collection.
		/// </summary>
		/// <remarks>
		/// You can only do this when the collection is empty.
		/// </remarks>
		/// <see cref="OrganizationMode"/>
		public void ResetOrganizationModes()
		{
			_organizationMode = 0;
		}

		/// <summary>
		/// Add a required sorting / grouping mode to this collection when next used.
		/// </summary>
		/// <remarks>
		/// You can only do this when the collection is empty.
		/// </remarks>
		/// <see cref="OrganizationMode"/>
		/// <param name="om"></param>
		public void AddOrganizationMode( OrganizationMode om )
		{
			_organizationMode |= om;
		}

		/// <summary>
		/// Add a renderable to the collection using a given pass
		/// </summary>
		public void AddRenderable( Pass pass, IRenderable rend )
		{
			// ascending and descending sort both set bit 1
			if ( (int)( _organizationMode & OrganizationMode.Descending ) != 0 )
			{
				_sortedDescending.Add( new RenderablePass( rend, pass ) );
			}

			if ( (int)( _organizationMode & OrganizationMode.GroupByPass ) != 0 )
			{
				if ( !_grouped.ContainsKey( pass ) )
				{
					// Create new pass entry, build a new list
					// Note that this pass and list are never destroyed until the 
					// engine shuts down, or a pass is destroyed or has it's hash
					// recalculated, although the lists will be cleared
					_grouped.Add( pass, new List<IRenderable>() );
				}
				_grouped[ pass ].Add( rend );
			}
		}

		/// <summary>
		/// Perform any sorting that is required on this collection.
		/// </summary>
		/// <param name="camera"></param>
		public void Sort( Camera camera )
		{
			// ascending and descending sort both set bit 1
			if ( (int)( _organizationMode & OrganizationMode.Descending ) != 0 )
			{

				// We can either use a built-in_sort and the 'less' implementation,
				// or a 2-pass radix sort (once by pass, then by distance, since
				// radix sorting is inherently stable this will work)
				// We use built-in sort if the number of items is 512 or less, since
				// the complexity of the radix sort is approximately O(10N), since 
				// each sort is O(5N) (1 pass histograms, 4 passes sort)
				// Since built-in_sort has a worst-case performance of O(N(logN)^2)
				// the performance tipping point is from about 1500 items, but in
				// built-in_sorts best-case scenario O(NlogN) it would be much higher.
				// Take a stab at 2000 items.

				if ( _sortedDescending.Count > 2000 )
				{
					// sort by pass
					_radixSorter1.Sort( _sortedDescending, delegate( RenderablePass value )
														   {
															   return (uint)value.pass.GetHashCode();
														   } );
					// sort by depth
					_radixSorter2.Sort( _sortedDescending, delegate( RenderablePass value )
														   {
															   // negated to force descending order
															   return -value.renderable.GetSquaredViewDepth( camera );
														   } );
				}
				else
				{
					_defaultDepthSortComparer.Camera = camera;
					_sortedDescending.Sort( _defaultDepthSortComparer );
				}
			}

			// Nothing needs to be done for pass groups, they auto-organize
		}

		/// <summary>
		/// Accept a visitor over the collection contents.
		/// </summary>
		/// <param name="visitor">Visitor class which should be called back</param>
		/// <param name="organizationMode">
		/// The organization mode which you want to iterate over.
		/// Note that this must have been included in an AddOrganizationMode
		/// call before any renderables were added.
		/// </param>
		public void AcceptVisitor( IQueuedRenderableVisitor visitor, OrganizationMode organizationMode )
		{
			if ( (int)( organizationMode & _organizationMode ) == 0 )
			{
				throw new ArgumentException( "Organization mode requested in AcceptVistor was not notified " +
											 "to this class ahead of time, therefore may not be supported.", "organizationMode" );
			}

			switch ( organizationMode )
			{
				case OrganizationMode.GroupByPass:
					acceptVisitorGrouped( visitor );
					break;
				case OrganizationMode.Descending:
					acceptVisitorDescending( visitor );
					break;
				case OrganizationMode.Ascending:
					acceptVisitorAscending( visitor );
					break;
			}

		}

		/// Internal visitor implementation
		private void acceptVisitorGrouped( IQueuedRenderableVisitor visitor )
		{
			foreach ( KeyValuePair<Pass, List<IRenderable>> item in _grouped )
			{
				// Fast bypass if this group is now empty
				if ( item.Value.Count == 0 )
					continue;

				// Visit Pass - allow skip
				if ( !visitor.Visit( item.Key ) )
					continue;

				foreach ( IRenderable renderable in item.Value )
				{
					// Visit Renderable
					visitor.Visit( renderable );
				}
			}

		}

		/// Internal visitor implementation
		private void acceptVisitorDescending( IQueuedRenderableVisitor visitor )
		{
			// List is already in descending order, so iterate forward
			foreach ( RenderablePass renderablePass in _sortedDescending )
			{
				visitor.Visit( renderablePass );
			}
		}

		/// Internal visitor implementation
		private void acceptVisitorAscending( IQueuedRenderableVisitor visitor )
		{
			// List is in descending order, so iterate in reverse
			for ( int index = _sortedDescending.Count - 1; index >= 0; ++index )
			{
				visitor.Visit( _sortedDescending[ index ] );
			}
		}

		#endregion Methods

	}
}