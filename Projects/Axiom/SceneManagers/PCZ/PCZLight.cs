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
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	public class PCZLight : Light
	{
		/** flag indicating if any of the zones in the affectedZonesList is
		*   visible in the current frame
		*/
		private bool affectsVisibleZone;

		/** List of PCZones which are affected by the light
		*/
		private readonly List<PCZone> affectedZonesList = new List<PCZone>();

		// flag recording if light has moved, therefore affected list needs updating
		bool needsUpdate;


		public PCZLight()
			: this( "" )
		{
		}

		public PCZLight( string name )
			: base( name )
		{
			needsUpdate = true;   // need to update the first time, regardless of attachment or movement
		}


		~PCZLight()
		{
			affectedZonesList.Clear();
		}

		//-----------------------------------------------------------------------
		/** Clear the affectedZonesList
		*/
		public void ClearAffectedZones()
		{
			affectedZonesList.Clear();
		}

		//-----------------------------------------------------------------------
		/** Add a zone to the zones affected list
		*/
		public void AddZoneToAffectedZonesList( PCZone zone )
		{
			affectedZonesList.Add( zone );
		}

		/** check if a zone is in the list of zones affected by the light */
		public bool AffectsZone( PCZone zone )
		{
			return affectedZonesList.Contains( zone );
		}

		public void UpdateZones( PCZone defaultZone, ulong frameCount )
		{
			//update the zones this light affects
			PCZone homeZone;
			affectedZonesList.Clear();
			affectsVisibleZone = false;
			PCZSceneNode sn = (PCZSceneNode)( this.ParentSceneNode );
			if ( null != sn )
			{
				// start with the zone the light is in
				homeZone = sn.HomeZone;
				if ( null != homeZone )
				{
					affectedZonesList.Add( homeZone );
					if ( homeZone.LastVisibleFrame == frameCount )
					{
						affectsVisibleZone = true;
					}
				}
				else
				{
					// error - scene node has no homezone!
					// just say it affects the default zone and leave it at that.
					affectedZonesList.Add( defaultZone );
					if ( defaultZone.LastVisibleFrame == frameCount )
					{
						affectsVisibleZone = true;
					}
					return;
				}
			}
			else
			{
				// ERROR! not connected to a scene node,
				// just say it affects the default zone and leave it at that.
				affectedZonesList.Add( defaultZone );
				if ( defaultZone.LastVisibleFrame == frameCount )
				{
					affectsVisibleZone = true;
				}
				return;
			}

			// now check visibility of each portal in the home zone.  If visible to
			// the light then add the target zone of the portal to the list of
			// affected zones and recurse into the target zone
			PCZFrustum portalFrustum = new PCZFrustum();
			Vector3 v = GetDerivedPosition();
			portalFrustum.SetOrigin( v );
			homeZone.CheckLightAgainstPortals( this, frameCount, portalFrustum, null );
		}
		//-----------------------------------------------------------------------
		public void RemoveZoneFromAffectedZonesList( PCZone zone )
		{
			if ( affectedZonesList.Contains( zone ) )
			{
				affectedZonesList.Remove( zone );
			}
		}
		//-----------------------------------------------------------------------
		public void NotifyMoved()
		{
			//TODO: Check implementation of this
			//_notifyMoved();   // inform ogre Light of movement
			localTransformDirty = true;
			needsUpdate = true;   // set need update flag
		}
		//-----------------------------------------------------------------------

		public bool NeedsUpdate
		{
			get
			{
				if ( needsUpdate ) // if this light has moved, return true immediately
					return true;

				// if any zones affected by this light have updated portals, then this light needs updating too
				foreach ( PCZone zone in affectedZonesList )
				{
					if ( zone.PortalsUpdated )
						return true; // return immediately to prevent further iterating
				}

				return false; // light hasnt moved, and no zones have updated portals. no light update.
			}

			set
			{
				needsUpdate = value;
			}
		}

		public bool AffectsVisibleZone
		{
			get
			{
				return affectsVisibleZone;
			}
			set
			{
				affectsVisibleZone = value;
			}
		}
	}

	public class PCZLightFactory : LightFactory
	{
		public const string TypeName = "PCZLight";

		public PCZLightFactory()
		{
			base.Type = PCZLightFactory.TypeName;
			base.TypeFlag = (uint)SceneQueryTypeMask.Light;
		}

		protected override MovableObject _createInstance( string name, NamedParameterList para )
		{
			return new PCZLight( name );
		}
	}

}