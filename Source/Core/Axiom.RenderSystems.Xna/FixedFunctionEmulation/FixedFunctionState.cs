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

using System.Collections.Generic;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
	/// <summary>
	/// Class defining a fixed function state.
	/// </summary>
	/// <remarks>
	/// The Fixed Function Pipeline (FFP abbreviated) is one of currently two methods of modifying 
	/// the graphic output. The other is the Programmable Pipeline also known as Shaders.
	/// With the FFP you can choose one of those algorithms and several ways to set or 
	/// modify the factors. There is only a handful of predefined algorithms and you cannot 
	/// add handcrafted ones. Hence the name Fixed Function Pipeline.
	/// One of the big differences of XNA from previous versions of DirectX and OpenGL is that it 
	/// doesn't have support for the FFP - the motivation for this class cames from the needs
	/// of the XNA render system to support the FFP functions using shaders.
	/// Usually you will get better performance if you use the PP and not the FFP shader emulation.
	/// The second common use for this class is to generate the base code for a new shader.
	/// </remarks>
	internal class FixedFunctionState
	{
		#region Fields and Properties

		/*protected bool materialEnabled;
		public bool MaterialEnabled
		{
			get
			{
				return materialEnabled;
			}
			set
			{
				materialEnabled = value;
			}
		}*/

		protected GeneralFixedFunctionState generalFFState = GeneralFixedFunctionState.Create();

		public GeneralFixedFunctionState GeneralFixedFunctionState
		{
			get
			{
				return generalFFState;
			}
			set
			{
				generalFFState = value;
			}
		}

		protected List<LightType> lights = new List<LightType>();

		public IList<LightType> Lights
		{
			get
			{
				return lights;
			}
			set
			{
				lights = (List<LightType>)value;
			}
		}

		protected List<TextureLayerState> textureLayerStates = new List<TextureLayerState>();

		public IList<TextureLayerState> TextureLayerStates
		{
			get
			{
				return textureLayerStates;
			}
			set
			{
				textureLayerStates = (List<TextureLayerState>)value;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		#endregion Construction and Destruction

		#region Methods

		#endregion Methods

		#region Object Overrides

		public override bool Equals( object obj )
		{
			return obj.GetHashCode() == GetHashCode();
		}

		public override int GetHashCode()
		{
			var hashCode = generalFFState.GetHashCode();
			foreach ( var tls in textureLayerStates )
				hashCode ^= tls.GetHashCode();
			foreach ( var light in lights )
				hashCode ^= light.GetHashCode();
			hashCode ^= textureLayerStates.Count;
			hashCode ^= lights.Count;
			//hashCode ^= materialEnabled.GetHashCode();
			return hashCode;
		}

		#endregion
	}
}