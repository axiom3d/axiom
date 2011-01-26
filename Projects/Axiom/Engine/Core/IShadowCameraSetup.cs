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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// This class allows you to plug in new ways to define the camera setup when
	///	rendering and projecting shadow textures.
	/// </summary>
	/// <remarks>
	/// The default projection used when rendering shadow textures is a uniform
	/// frustum. This is pretty straight forward but doesn't make the best use of
	/// the space in the shadow map since texels closer to the camera will be larger,
	/// resulting in 'jaggies'. There are several ways to distribute the texels
	/// in the shadow texture differently, and this class allows you to override
	/// that.
	/// <para />
	/// Axiom is provided with several alternative shadow camera setups, including
	/// LiSPSM (<see cref="LiSPSMShadowCameraSetup"/>) and Plane Optimal
	/// (<see cref="PlaneOptimalShadowCameraSetup"/>).
	/// Others can of course be written to incorporate other algorithms. All you
	/// have to do is instantiate one of these classes and enable it using
	/// <see cref="SceneManager.SetShadowCameraSetup"/> (global) or
	/// <see cref="Light.SetCustomShadowCameraSetup"/>
	/// (per light). In both cases the instance is be deleted automatically when
	/// no more references to it exist.
	/// <para />
	/// Shadow map matrices, being projective matrices, have 15 degrees of freedom.
	/// 3 of these degrees of freedom are fixed by the light's position.  4 are used to
	/// affinely affect z values.  6 affinely affect u,v sampling.  2 are projective
	/// degrees of freedom.  This class is meant to allow custom methods for
	/// handling optimization.
	/// </remarks>
	public interface IShadowCameraSetup
	{
		/// <summary>
		/// Gets a specific implementation of a ShadowCamera setup.
		/// </summary>
		/// <param name="sceneManager"></param>
		/// <param name="camera"></param>
		/// <param name="viewport"></param>
		/// <param name="light"></param>
		/// <param name="textureCamera"></param>
		/// <param name="iteration"></param>
		void GetShadowCamera( SceneManager sceneManager, Camera camera, Viewport viewport, Light light, Camera textureCamera, int iteration );
	}
}