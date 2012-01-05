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

using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.PortalConnected
{
	/// <summary>
	/// Specialized version of Axiom.Light which caches which zones the light affects 
	/// </summary>
	public class PCZLight : Light
	{
		/// <summary>
		/// name generator
		/// </summary>
		private static NameGenerator<PCZLight> _nameGenerator = new NameGenerator<PCZLight>( "PCZLight" );

		/// <summary>
		/// flag indicating if any of the zones in the affectedZonesList is 
		/// visible in the current frame
		/// </summary>
		private bool _affectsVisibleZone = false;

		/// <summary>
		/// 
		/// </summary>
		private List<PCZone> _affectedZonesList = new List<PCZone>();

		/// <summary>
		/// List of PCZones which are affected by the light
		/// </summary>
		protected List<PCZone> AffectedZonesList { get { return _affectedZonesList; } set { _affectedZonesList = value; } }

		/// <summary>
		/// flag recording if light has moved, therefore affected list needs updating 
		/// </summary>
		private bool _needsUpdate = true;

		/// <summary>
		/// Update the list of zones the light affects 
		/// </summary>
		private PCZFrustum _portalFrustum = new PCZFrustum();

		/// <summary>
		/// Default constructor
		/// </summary>
		public PCZLight()
			: this( _nameGenerator.GetNextUniqueName() ) {}

		/// <summary>
		/// Normal constructor. Should not be called directly, but rather the SceneManager.createLight method should be used
		/// </summary>
		/// <param name="name">string</param>
		public PCZLight( string name )
			: base( name ) {}

		/// <summary>
		/// Standard destructor.
		/// </summary>
		~PCZLight()
		{
			AffectedZonesList.Clear();
		}

		/// <summary>
		/// Clear the affectedZonesList  
		/// </summary>
		public void ClearAffectedZones()
		{
			AffectedZonesList.Clear();
		}

		/// <summary>
		/// Add a zone to the zones affected list
		/// </summary>
		/// <param name="zone">PCZone</param>
		public void AddZoneToAffectedZonesList( PCZone zone )
		{
			AffectedZonesList.Add( zone );
		}

		/// <summary>
		/// check if a zone is in the list of zones affected by the light 
		/// </summary>
		/// <param name="zone">PCZone</param>
		/// <returns>bool</returns>
		public bool AffectsZone( PCZone zone )
		{
			return AffectedZonesList.Contains( zone );
		}

		/// <summary>
		/// returns flag indicating if the light affects a zone which is visible
		/// in the current frame
		/// </summary>
		/// <returns>bool</returns>
		public bool AffectsVisibleZone { get { return _affectsVisibleZone; } set { _affectsVisibleZone = value; } }

		/// <summary>
		/// UpdateZones
		/// </summary>
		/// <param name="defaultZone">PCZone</param>
		/// <param name="frameCount">ulong</param>
		public void UpdateZones( PCZone defaultZone, ulong frameCount )
		{
			//update the zones this light affects
			PCZone homeZone;
			AffectedZonesList.Clear();
			_affectsVisibleZone = false;
			PCZSceneNode sn = (PCZSceneNode)( this.ParentSceneNode );
			if( sn != null )
			{
				// start with the zone the light is in
				homeZone = sn.HomeZone;
				if( homeZone != null )
				{
					AffectedZonesList.Add( homeZone );
					if( homeZone.LastVisibleFrame == frameCount )
					{
						_affectsVisibleZone = true;
					}
				}
				else
				{
					// error - scene node has no home zone!
					// just say it affects the default zone and leave it at that.
					AffectedZonesList.Add( defaultZone );
					if( defaultZone.LastVisibleFrame == frameCount )
					{
						_affectsVisibleZone = true;
					}
					return;
				}
			}
			else
			{
				// ERROR! not connected to a scene node,
				// just say it affects the default zone and leave it at that.
				AffectedZonesList.Add( defaultZone );
				if( defaultZone.LastVisibleFrame == frameCount )
				{
					_affectsVisibleZone = true;
				}
				return;
			}

			// now check visibility of each portal in the home zone.  If visible to
			// the light then add the target zone of the portal to the list of
			// affected zones and recurse into the target zone
			//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
			//			static PCZFrustum portalFrustum;
			_portalFrustum.Origin = base.DerivedPosition;
			homeZone.CheckLightAgainstPortals( this, frameCount, _portalFrustum, null );
		}

		/// <summary>
		/// RemoveZoneFromAffectedZonesList
		/// </summary>
		/// <param name="zone">PCZone</param>
		public void RemoveZoneFromAffectedZonesList( PCZone zone )
		{
			AffectedZonesList.Remove( zone );
		}

		/// <summary>
		/// MovableObject notified when SceneNode changes
		/// </summary>
		public void NotifyMoved()
		{
			base.localTransformDirty = true;
			_needsUpdate = true; // set need update flag
		}

		/// <summary>
		/// clear update flag
		/// </summary>
		public void ClearNeedsUpdate()
		{
			_needsUpdate = false;
		}

		/// <summary>
		/// get status of need for update. this checks all affected zones
		/// </summary>
		/// <returns>bool</returns>
		public bool NeedsUpdate
		{
			get
			{
				if( _needsUpdate ) // if this light has moved, return true immediately
				{
					return true;
				}

				foreach( PCZone zone in AffectedZonesList )
				{
					if( zone.PortalsUpdated == true )
					{
						return true;
					}
				}

				return false; // light hasn't moved, and no zones have updated portals. no light update.
			}
			set { _needsUpdate = value; }
		}
	}

	//ORIGINAL LINE: class _OgrePCZPluginExport PCZLightFactory : public MovableObjectFactory
	/// <summary>
	/// Factory object for creating PCZLight instances 
	/// </summary>
	public class PCZLightFactory : LightFactory
	{
		/// <summary>
		/// TypeName
		/// </summary>
		new public const string TypeName = "PCZLight";

		/// <summary>
		/// Constructor
		/// </summary>
		public PCZLightFactory()
		{
			base.Type = PCZLightFactory.TypeName;
			base.TypeFlag = (uint)SceneQueryTypeMask.Light;
		}

		/// <summary>
		/// overridden MovableObject _createInstance
		/// </summary>
		/// <param name="name">string</param>
		/// <param name="para">NamedParameterList</param>
		/// <returns></returns>
		protected override MovableObject _createInstance( string name, NamedParameterList para )
		{
			return new PCZLight( name );
		}
	}
}

// Namespace namespace Axiom.SceneManagers.PortalConnected
