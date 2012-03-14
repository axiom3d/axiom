#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Octree
{
	public class TerrainRaySceneQuery : DefaultRaySceneQuery
	{
		protected WorldFragment WorldFragment;

		protected internal TerrainRaySceneQuery( SceneManager creator )
			: base( creator )
		{
			worldFragmentTypes = WorldFragmentType.SingleIntersection;
			this.WorldFragment = new WorldFragment();
		}

		public override void Execute( IRaySceneQueryListener listener )
		{
			Vector3 dir = ray.Direction;
			Vector3 origin = ray.Origin;
			// Straight up / down?
			if ( dir == Vector3.UnitY || dir == Vector3.NegativeUnitY )
			{
				float height = ( (TerrainSceneManager)creator ).GetHeightAt( origin, -1 );
				if ( height != -1 && ( height <= origin.y && dir.y < 0 ) || ( height >= origin.y && dir.y > 0 ) )
				{
					this.WorldFragment.SingleIntersection.x = origin.x;
					this.WorldFragment.SingleIntersection.z = origin.z;
					this.WorldFragment.SingleIntersection.y = height;
					if ( !listener.OnQueryResult( this.WorldFragment, ( this.WorldFragment.SingleIntersection - origin ).Length ) )
					{
						return;
					}
				}
			}
			else
			{
				var tsm = (TerrainSceneManager)creator;
				this.WorldFragment.SingleIntersection = tsm.IntersectSegment( origin, origin + ( dir * 100000 ) );
				if ( !listener.OnQueryResult( this.WorldFragment, ( this.WorldFragment.SingleIntersection - origin ).Length ) )
				{
					return;
				}
			}
		}
	}
}
