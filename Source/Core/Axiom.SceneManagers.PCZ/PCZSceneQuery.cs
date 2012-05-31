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
using System.Text;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	public class PCZAxisAlignedBoxSceneQuery : DefaultAxisAlignedBoxRegionSceneQuery
	{
		private PCZone startZone;
		private SceneNode excludeNode;

		/// <summary>
		///     Creates a custom PCZ AAB query
		/// </summary>
		/// <param name="creator">
		///     The SceneManager that creates the query.
		/// </param>
		public PCZAxisAlignedBoxSceneQuery( SceneManager creator )
			: base( creator )
		{
			this.startZone = null;
			this.excludeNode = null;
		}

		/// <summary>
		///     Finds any entities that intersect the AAB for the query.
		/// </summary>
		/// <param name="listener">
		///     The listener to call when we find the results.
		/// </param>
		public override void Execute( ISceneQueryListener listener )
		{
			var list = new List<PCZSceneNode>();
			//find the nodes that intersect the AAB
			( (PCZSceneManager)creator ).FindNodesIn( box, ref list, this.startZone, (PCZSceneNode)this.excludeNode );

			//grab all moveables from the node that intersect...

			foreach ( PCZSceneNode node in list )
			{
				foreach ( MovableObject m in node.Objects )
				{
					if ( ( m.QueryFlags & queryMask ) != 0 && ( m.TypeFlags & this.queryTypeMask ) != 0 && m.IsAttached &&
					     box.Intersects( m.GetWorldBoundingBox() ) )
					{
						listener.OnQueryResult( m );
						// deal with attached objects, since they are not directly attached to nodes
						if ( m.MovableType == "Entity" )
						{
							//Check: not sure here...
							var e = (Entity)m;
							foreach ( MovableObject c in e.SubEntities )
							{
								if ( ( c.QueryFlags & queryMask ) > 0 )
								{
									listener.OnQueryResult( c );
								}
							}
						}
					}
				}
			}
			// reset startzone and exclude node
			this.startZone = null;
			this.excludeNode = null;
		}
	}

	public class PCZIntersectionSceneQuery : DefaultIntersectionSceneQuery
	{

		public PCZIntersectionSceneQuery( SceneManager creator )
			: base( creator )
		{
		}

		public override void Execute( IIntersectionSceneQueryListener listener )
		{
			var set = new Dictionary<MovableObject, MovableObject>();

			// Iterate over all movable types
			foreach ( Core.MovableObjectFactory factory in Root.Instance.MovableObjectFactories.Values )
			{
				MovableObjectCollection col = creator.GetMovableObjectCollection( factory.Type );
				foreach ( MovableObject e in col.Values )
				{
					PCZone zone = ( (PCZSceneNode)( e.ParentSceneNode ) ).HomeZone;
					var list = new List<PCZSceneNode>();
					//find the nodes that intersect the AAB
					( (PCZSceneManager)creator ).FindNodesIn( e.GetWorldBoundingBox(), ref list, zone, null );
					//grab all moveables from the node that intersect...
					foreach ( PCZSceneNode node in list )
					{
						foreach ( MovableObject m in node.Objects )
						{
							// MovableObject m =
							if ( m != e && !set.ContainsKey( m ) && !set.ContainsKey( e ) && ( m.QueryFlags & queryMask ) != 0 &&
							     ( m.TypeFlags & this.queryTypeMask ) != 0 && m.IsAttached &&
							     e.GetWorldBoundingBox().Intersects( m.GetWorldBoundingBox() ) )
							{
								listener.OnQueryResult( e, m );
								// deal with attached objects, since they are not directly attached to nodes
								if ( m.MovableType == "Entity" )
								{
									var e2 = (Entity)m;
									foreach ( MovableObject c in e2.SubEntities )
									{
										if ( ( c.QueryFlags & queryMask ) != 0 && e.GetWorldBoundingBox().Intersects( c.GetWorldBoundingBox() ) )
										{
											listener.OnQueryResult( e, c );
										}
									}
								}
							}
							set.Add( e, m );
						}
					}
				}
			}
		}
	}

	public class PCZSphereSceneQuery : DefaultSphereRegionSceneQuery
	{
		private PCZone startZone;
		private SceneNode excludeNode;

		protected internal PCZSphereSceneQuery( SceneManager creator )
			: base( creator )
		{
		}

		public override void Execute( ISceneQueryListener listener )
		{
			var list = new List<PCZSceneNode>();
			//find the nodes that intersect the AAB
			( (PCZSceneManager)creator ).FindNodesIn( sphere, ref list, this.startZone, (PCZSceneNode)this.excludeNode );

			//grab all moveables from the node that intersect...

			foreach ( PCZSceneNode node in list )
			{
				foreach ( MovableObject m in node.Objects )
				{
					if ( ( m.QueryFlags & queryMask ) != 0 && ( m.TypeFlags & this.queryTypeMask ) != 0 && m.IsAttached &&
					     sphere.Intersects( m.GetWorldBoundingBox() ) )
					{
						listener.OnQueryResult( m );
						// deal with attached objects, since they are not directly attached to nodes
						if ( m.MovableType == "Entity" )
						{
							//Check: not sure here...
							var e = (Entity)m;
							foreach ( MovableObject c in e.SubEntities )
							{
								if ( ( c.QueryFlags & queryMask ) > 0 )
								{
									listener.OnQueryResult( c );
								}
							}
						}
					}
				}
			}
			// reset startzone and exclude node
			this.startZone = null;
			this.excludeNode = null;
		}
	}

	public class PCZRaySceneQuery : DefaultRaySceneQuery
	{
		private PCZone startZone;
		private SceneNode excludeNode;

		protected internal PCZRaySceneQuery( SceneManager creator )
			: base( creator )
		{
		}

		public override void Execute( IRaySceneQueryListener listener )
		{
			var list = new List<PCZSceneNode>();
			//find the nodes that intersect the AAB
			( (PCZSceneManager)creator ).FindNodesIn( ray, ref list, this.startZone, (PCZSceneNode)this.excludeNode );

			//grab all moveables from the node that intersect...

			foreach ( PCZSceneNode node in list )
			{
				foreach ( MovableObject m in node.Objects )
				{
					if ( ( m.QueryFlags & queryMask ) != 0 && ( m.TypeFlags & this.queryTypeMask ) != 0 && m.IsAttached )
					{
						IntersectResult result = ray.Intersects( m.GetWorldBoundingBox() );
						if ( result.Hit )
						{
							listener.OnQueryResult( m, result.Distance );
							// deal with attached objects, since they are not directly attached to nodes
							if ( m.MovableType == "Entity" )
							{
								//Check: not sure here...
								var e = (Entity)m;
								foreach ( MovableObject c in e.SubEntities )
								{
									if ( ( c.QueryFlags & queryMask ) > 0 )
									{
										result = ray.Intersects( c.GetWorldBoundingBox() );
										if ( result.Hit )
										{
											listener.OnQueryResult( c, result.Distance );
										}
									}
								}
							}
						}
					}
				}
			}
			// reset startzone and exclude node
			this.startZone = null;
			this.excludeNode = null;
		}

		public PCZone StartZone
		{
			get
			{
				return this.startZone;
			}
			set
			{
				this.startZone = value;
			}
		}

		public SceneNode ExcludeNode
		{
			get
			{
				return this.excludeNode;
			}

			set
			{
				this.excludeNode = value;
			}
		}
	}

	public class PCZPlaneBoundedVolumeListSceneQuery : DefaultPlaneBoundedVolumeListSceneQuery
	{
		private PCZone startZone;
		private SceneNode excludeNode;

		protected internal PCZPlaneBoundedVolumeListSceneQuery( SceneManager creator )
			: base( creator )
		{
		}

		public override void Execute( ISceneQueryListener listener )
		{
			var list = new List<PCZSceneNode>();
			var checkedNodes = new List<PCZSceneNode>();

			foreach ( PlaneBoundedVolume volume in volumes )
			{
				//find the nodes that intersect the AAB
				( (PCZSceneManager)creator ).FindNodesIn( volume, ref list, this.startZone, (PCZSceneNode)this.excludeNode );

				//grab all moveables from the node that intersect...
				foreach ( PCZSceneNode node in list )
				{
					// avoid double-check same scene node
					if ( !checkedNodes.Contains( node ) )
					{
						continue;
					}

					checkedNodes.Add( node );

					foreach ( MovableObject m in node.Objects )
					{
						if ( ( m.QueryFlags & queryMask ) != 0 && ( m.TypeFlags & this.queryTypeMask ) != 0 && m.IsAttached &&
						     volume.Intersects( m.GetWorldBoundingBox() ) )
						{
							listener.OnQueryResult( m );
							// deal with attached objects, since they are not directly attached to nodes
							if ( m.MovableType == "Entity" )
							{
								//Check: not sure here...
								var e = (Entity)m;
								foreach ( MovableObject c in e.SubEntities )
								{
									if ( ( c.QueryFlags & queryMask ) > 0 && volume.Intersects( c.GetWorldBoundingBox() ) )
									{
										listener.OnQueryResult( c );
									}
								}
							}
						}
					}
				}
			}
			// reset startzone and exclude node
			this.startZone = null;
			this.excludeNode = null;
		}
	}
}