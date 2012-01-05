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

using Axiom.Graphics;
using Axiom.RenderSystems.Xna.HLSL;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
	/// <summary>
	/// General State values which can be copied easily
	/// </summary>
	internal struct GeneralFixedFunctionState
	{
		#region Factory

		public static GeneralFixedFunctionState Create()
		{
			GeneralFixedFunctionState gffs;

			gffs.NormalizeNormals = false;
			gffs.EnableLighting = true;
			gffs.FogMode = FogMode.None;
			gffs.Shading = Shading.Gouraud;
			gffs.AlphaRejectFunction = CompareFunction.AlwaysPass;
			gffs.lightTypeCount = new uint[(uint)( LightType.Spotlight ) + 1];

			return gffs;
		}

		#endregion Factory

		#region Fields

		/// <summary>
		/// Determines if normals are to be normalized dynamically.
		/// </summary>
		/// <remarks>
		/// This option can be used to prevent lighting variations when scaling an
		/// object - normally because this scaling is hardware based, the normals 
		/// get scaled too which causes lighting to become inconsistent. By default the
		/// SceneManager detects scaled objects and does this for you, but 
		/// this has an overhead so you might want to turn that off by setting 
		/// <see name="SceneManager::NormalizeNormalsOnScale"/> to <code>false</code> 
		/// and only do it per-Pass when you need to.
		/// </remarks>
		public bool NormalizeNormals;

		/// <summary>
		/// The alpha reject function to use.
		/// </summary>
		public CompareFunction AlphaRejectFunction;

		/// <summary>
		/// The fogging mode applied to this pass.
		/// </summary>
		/// <remarks>
		/// Fogging is an effect that is applied as polys are rendered. Sometimes, you want
		/// fog to be applied to an entire scene. Other times, you want it to be applied to a few
		/// polygons only. This pass-level specification of fog parameters lets you easily manage
		/// both.
		/// <para/>
		/// The <see cref="SceneManager"/> class also has a <see cref="SceneManager.SetFog"/> method which applies scene-level fog. This method
		/// lets you change the fog behaviour for this pass compared to the standard scene-level fog.
		/// <para>
		/// <paramref name="overrideScene"/>
		/// If true, you authorize this pass to override the scene's fog params with it's own settings.
		/// If you specify false, so other parameters are necessary, and this is the default behaviour for passes.
		/// </para>
		/// <para>
		/// <paramref name="mode"/>Only applicable if overrideScene is true. You can disable fog which is turned on for the
		/// rest of the scene by specifying <see cref="FogeMode.None"/>. Otherwise, set a pass-specific fog mode as
		/// defined in the enum FogMode.
		/// </para>
		/// <para>
		/// <paramref name="color"/>
		/// The color of the fog. Either set this to the same as your viewport background colour,
		/// or to blend in with a skydome or skybox.
		/// </para>
		/// <para>
		/// <paramref name="expDensity"/>
		/// The density of the fog in <see cref="FogeMode.Exp"/> or <see cref="FogeMode.Exp2"/> mode, as a value between 0 and 1.
		/// The default is 0.001.
		/// </para>
		/// <para>
		/// <paramref name="linearStart"/>
		/// Distance in world units at which linear fog starts to encroach.
		/// Only applicable if mode is <see cref="FogeMode.Linear"/>.
		/// </para>
		/// <para>
		/// <paramref name="linearEnd"/>
		/// Distance in world units at which linear fog becomes completely opaque.
		/// Only applicable if mode is <see cref="FogeMode.Linear"/>.
		/// </para>
		/// </remarks>
		public FogMode FogMode;

		/// <summary>
		/// Deterimines whether or not dynamic lighting is enabled
		/// </summary>
		/// <remarks>
		/// If true, dynamic lighting is performed on geometry with normals supplied, 
		/// geometry without normals will not be displayed.
		/// <para/>
		/// If false, no lighting is applied and all geometry will be full brightness.
		/// </remarks>
		public bool EnableLighting;

		/// <summary>
		/// The type of light shading required
		/// </summary>
		/// <remarks>
		/// The default mode is Gouraud.
		/// </remarks>
		public Shading Shading;

		private uint[] lightTypeCount;

		#endregion Fields

		internal void ResetLightTypeCounts()
		{
			for( int index = 0; index < lightTypeCount.Length; index++ )
			{
				lightTypeCount[ index ] = 0;
			}
		}

		internal void IncrementLightTypeCount( LightType lightType )
		{
			lightTypeCount[ (uint)lightType ]++;
		}

		#region Object Implementation

		public override bool Equals( object obj )
		{
			return obj.GetHashCode() == GetHashCode();
		}

		public override int GetHashCode()
		{
			return NormalizeNormals.GetHashCode() ^ EnableLighting.GetHashCode() ^ FogMode.GetHashCode() ^ Shading.GetHashCode() ^ AlphaRejectFunction.GetHashCode() ^ lightTypeCount[ 1 ].GetHashCode() ^ lightTypeCount[ 2 ].GetHashCode();
		}

		#endregion
	}
}
