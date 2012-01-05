#region MIT/X11 License

//Copyright (c) 2009 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
// <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
// <id value="$Id:$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom;
using Axiom.Core;
using Axiom.Math;
using Axiom.Collections;
using Axiom.Graphics;

using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	//ORIGINAL LINE: class _OgrePCZPluginExport PCZSceneNode : public SceneNode
	public class PCZSceneNode : SceneNode
	{
		/// <summary>
		/// name generator
		/// </summary>
		private static NameGenerator<PCZSceneNode> _nameGenerator = new NameGenerator<PCZSceneNode>( "PCZSceneNode" );

		private Vector3 _newPosition = Vector3.Zero;

		/// <summary>
		/// NewPosition
		/// </summary>
		protected Vector3 NewPosition { get { return _newPosition; } set { _newPosition = value; } }

		private PCZone _homeZone = null;

		private bool _anchored = false;

		/// <summary>
		/// Anchored
		/// </summary>
		public bool Anchored { get { return _anchored; } set { _anchored = value; } }

		private bool _allowToVisit = true;

		/// <summary>
		/// AllowToVisit
		/// </summary>
		public bool AllowToVisit { get { return _allowToVisit; } set { _allowToVisit = value; } }

		private bool allowedToVisit = true;

		/// <summary>
		/// AllowedToVisit
		/// </summary>
		public bool AllowedToVisit { get { return allowedToVisit; } set { allowedToVisit = value; } }

		private Dictionary<string, PCZone> _visitingZones = new Dictionary<string, PCZone>();

		/// <summary>
		/// VisitingZones
		/// </summary>
		protected Dictionary<string, PCZone> VisitingZones { get { return _visitingZones; } set { _visitingZones = value; } }

		private Vector3 _prevPosition = Vector3.Zero;

		/// <summary>
		/// PrevPosition
		/// </summary>
		public Vector3 PrevPosition { get { return _prevPosition; } set { _prevPosition = value; } }

		private ulong _lastVisibleFrame = 0;

		/// <summary>
		/// LastVisibleFrame
		/// </summary>
		public ulong LastVisibleFrame { get { return _lastVisibleFrame; } set { _lastVisibleFrame = value; } }

		private PCZCamera _lastVisibleFromCamera = null;

		/// <summary>
		/// LastVisibleFromCamera
		/// </summary>
		public PCZCamera LastVisibleFromCamera { get { return _lastVisibleFromCamera; } set { _lastVisibleFromCamera = value; } }

		private Dictionary<string, ZoneData> _zoneData = new Dictionary<string, ZoneData>();

		/// <summary>
		/// ZoneData
		/// </summary>
		protected Dictionary<string, ZoneData> ZoneData { get { return _zoneData; } set { _zoneData = value; } }

		private bool _enabled = true;

		/// <summary>
		/// Enabled
		/// </summary>
		public bool Enabled { get { return _enabled; } set { _enabled = value; } }

		private bool _moved = false;

		/// <summary>
		/// Moved
		/// </summary>
		public bool Moved { get { return _moved; } set { _moved = value; } }

		/// <summary>
		///  Standard constructor 
		/// </summary>
		/// <param name="creator">SceneManager</param>
		public PCZSceneNode( SceneManager creator )
			: this( creator, _nameGenerator.GetNextUniqueName() ) {}

		/// <summary>
		/// Standard constructor
		/// </summary>
		/// <param name="creator">SceneManager</param>
		/// <param name="name">string</param> 
		public PCZSceneNode( SceneManager creator, string name )
			: base( creator, name ) {}

		//* Standard destructor 
		~PCZSceneNode()
		{
			// clear visiting zones list
			_visitingZones.Clear();
			ZoneData.Clear();
			base.Dispose();
		}

		/// <summary>
		/// Update
		/// </summary>
		/// <param name="updateChildren">bool</param>
		/// <param name="parentHasChanged">bool</param>
		protected override void Update( bool updateChildren, bool parentHasChanged )
		{
			base.Update( updateChildren, parentHasChanged );
			if( base.Parent != null ) // skip bound update if it's root scene node. Saves a lot of CPU.
			{
				UpdateBounds();
			}

			_prevPosition = NewPosition;
			NewPosition = DerivedPosition;
		}

		/// <summary>
		/// UpdateFromParentImpl
		/// </summary>
		public void UpdateFromParentImpl()
		{
			base.UpdateFromParent();
			_moved = true;
		}

		/// <summary>
		/// Creates an unnamed new SceneNode as a child of this node.
		/// translate Initial translation offset of child relative to parent
		/// rotate Initial rotation relative to parent
		/// </summary>
		/// <param name="inTranslate">Vector3</param>
		/// <returns>SceneNode</returns>
		public SceneNode CreateChildSceneNode( Vector3 inTranslate )
		{
			return CreateChildSceneNode( inTranslate, Quaternion.Identity );
		}

		/// <summary>
		/// Creates an unnamed new SceneNode as a child of this node.
		/// translate Initial translation offset of child relative to parent
		/// rotate Initial rotation relative to parent
		/// </summary>
		/// <returns>SceneNode</returns>
		public SceneNode CreateChildSceneNode()
		{
			return CreateChildSceneNode( Vector3.Zero, Quaternion.Identity );
		}

		/// <summary>
		/// Creates an unnamed new SceneNode as a child of this node.
		/// translate Initial translation offset of child relative to parent
		/// rotate Initial rotation relative to parent
		/// </summary>
		/// <param name="inTranslate">Vector3</param>
		/// <param name="inRotate">Quaternion</param>
		/// <returns>SceneNode</returns>       
		public SceneNode CreateChildSceneNode( Vector3 inTranslate, Quaternion inRotate )
		{
			PCZSceneNode childSceneNode = (PCZSceneNode)( this.CreateChild( inTranslate, inRotate ) );
			if( HomeZone != null )
			{
				childSceneNode.HomeZone = HomeZone;
				HomeZone.AddNode( childSceneNode );
			}
			return (SceneNode)( childSceneNode );
		}

		/// <summary>
		/// Creates a new named SceneNode as a child of this node.
		///     This creates a child node with a given name, which allows you to look the node up from 
		///     the parent which holds this collection of nodes.
		///     translate Initial translation offset of child relative to parent
		///     rotate Initial rotation relative to parent
		/// </summary>
		/// <param name="name">string</param>
		/// <param name="inTranslate">Vector3</param>
		/// <returns>SceneNode</returns>
		public SceneNode CreateChildSceneNode( string name, Vector3 inTranslate )
		{
			return CreateChildSceneNode( name, inTranslate, Quaternion.Identity );
		}

		/// <summary>
		/// Creates a new named SceneNode as a child of this node.
		/// </summary>
		/// <param name="name">string</param>
		/// <returns>SceneNode</returns>
		public SceneNode CreateChildSceneNode( string name )
		{
			return CreateChildSceneNode( name, Vector3.Zero, Quaternion.Identity );
		}

		/// <summary>
		/// Creates a new named SceneNode as a child of this node.
		/// </summary>
		/// <param name="name">string</param>
		/// <param name="inTranslate">Vector3</param>
		/// <param name="inRotate">Quaternion</param>
		/// <returns>SceneNode</returns>
		public SceneNode CreateChildSceneNode( string name, Vector3 inTranslate, Quaternion inRotate )
		{
			PCZSceneNode childSceneNode = (PCZSceneNode)( this.CreateChild( name, inTranslate, inRotate ) );
			if( HomeZone != null )
			{
				childSceneNode.HomeZone = HomeZone;
				HomeZone.AddNode( childSceneNode );
			}
			return (SceneNode)( childSceneNode );
		}

		/// <summary>
		/// HomeZone
		/// </summary>
		public PCZone HomeZone
		{
			get { return _homeZone; }
			set
			{
				// if the new home zone is different than the current, remove
				// the node from the current home zone's list of home nodes first
				if( value != _homeZone && _homeZone != null )
				{
					_homeZone.RemoveNode( this );
				}
				_homeZone = value;
			}
		}

		/// <summary>
		/// AnchorToHomeZone
		/// </summary>
		/// <param name="zone"></param>
		public void AnchorToHomeZone( PCZone zone )
		{
			_homeZone = zone;
			if( zone != null )
			{
				Anchored = true;
			}
			else
			{
				Anchored = false;
			}
		}

		/// <summary>
		/// Add Zone To Visiting Zones Map
		/// </summary>
		/// <param name="zone">PCZone</param>
		public void AddZoneToVisitingZonesMap( PCZone zone )
		{
			_visitingZones[ zone.Name ] = zone;
		}

		/// <summary>
		/// ClearVisitingZonesMap
		/// </summary>
		public void ClearVisitingZonesMap()
		{
			_visitingZones.Clear();
		}

		/// <summary>
		/// The following function does the following:
		///  1) Remove references to the node from zones the node is visiting
		///  2) Clear the node's list of zones it is visiting
		/// </summary>
		public void ClearNodeFromVisitedZones()
		{
			if( _visitingZones.Count > 0 )
			{
				// first go through the list of zones this node is visiting
				// and remove references to this node

				foreach( KeyValuePair<string, PCZone> kvp in _visitingZones )
				{
					PCZone zone = kvp.Value;
					zone.RemoveNode( this );
				}

				// second, clear the visiting zones list
				_visitingZones.Clear();
			}
		}

		/// <summary>
		/// Remove all references that the node has to the given zone
		/// </summary>
		/// <param name="zone">PCZone</param> 
		public void RemoveReferencesToZone( PCZone zone )
		{
			if( HomeZone == zone )
			{
				HomeZone = null;
			}

			if( VisitingZones.ContainsKey( zone.Name ) )
			{
				VisitingZones.Remove( zone.Name );
			}
		}

		/// <summary>
		/// returns true if zone is in the node's visiting zones map
		///   false otherwise.
		/// </summary>
		/// <param name="zone">PCZone</param>
		/// <returns>bool</returns>
		public bool IsVisitingZone( PCZone zone )
		{
			return VisitingZones.ContainsKey( zone.Name );
		}

		/// <summary>
		/// Adds the attached objects of this PCZSceneNode into the queue. 
		/// </summary>
		/// <param name="camera">Camera</param>
		/// <param name="queue">RenderQueue</param>
		/// <param name="onlyShadowCasters">bool</param>
		/// <param name="visibleBounds">VisibleObjectsBoundsInfo</param>
		public void AddToRenderQueue( Camera camera, RenderQueue queue, bool onlyShadowCasters, VisibleObjectsBoundsInfo visibleBounds )
		{
			foreach( MovableObject mo in objectList )
			{
				mo.NotifyCurrentCamera( camera );
				if( mo.IsVisible && ( !onlyShadowCasters || mo.CastShadows ) )
				{
					mo.UpdateRenderQueue( queue );

					//TODO: Check this
					//if (visibleBounds != null)
					//{
					visibleBounds.Merge( mo.GetWorldBoundingBox( true ), mo.GetWorldBoundingSphere( true ), camera );
					//}
				}
			}
		}

		/// <summary>
		/// Save the node's current position as the previous position
		/// </summary>
		public void SavePrevPosition()
		{
			PrevPosition = DerivedPosition;
		}

		/// <summary>
		/// SetZoneData
		/// </summary>
		/// <param name="zone">PCZone</param>
		/// <param name="zoneData">ZoneData</param>
		public void SetZoneData( PCZone zone, ZoneData zoneData )
		{
			// first make sure that the data doesn't already exist
			if( ZoneData.ContainsKey( zone.Name ) )
			{
				throw new AxiomException( "A ZoneData associated with zone " + zone.Name + " already exists", "PCZSceneNode::setZoneData" );
			}
			ZoneData[ zone.Name ] = zoneData;
		}

		/// <summary>
		/// get zone data for this node for given zone
		/// NOTE: This routine assumes that the zone data is present!
		/// </summary>
		/// <param name="zone">PCZone</param>
		/// <returns>ZoneData</returns>
		public ZoneData GetZoneData( PCZone zone )
		{
			if( ZoneData.ContainsKey( zone.Name ) )
			{
				return ZoneData[ zone.Name ];
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// update zone-specific data for any zone that the node is touching
		/// </summary>
		public void UpdateZoneData()
		{
			ZoneData zoneData;
			PCZone zone;
			// make sure home zone data is updated
			zone = HomeZone;
			if( zone.RequiresZoneSpecificNodeData )
			{
				zoneData = GetZoneData( zone );
				//TODO: check this to get it to run but it should have something here right?
				if( zoneData != null )
				{
					zoneData.Update();
				}
			}
		}
	}
}
