#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    ///	Xna Fixed Function Emulation
    /// </summary>
    internal class XnaFixedFunctionEffect
    {
		#region Fields & Properties

		#region Projection Property

		private static XFG.EffectParameter _projection;

		public XNA.Matrix Projection
		{
			get
			{
				return _projection.GetValueMatrix();
			}
			set
			{
				_projection.SetValue( value );
			}
		}

		#endregion ProjectionMatrix Property
		#region View Property

		private static XFG.EffectParameter _view;

		public XNA.Matrix View
		{
			get
			{
				return _view.GetValueMatrix();
			}
			set
			{
				_view.SetValue( value );
			}
		}

		#endregion View Property
		#region World Property

		private static XFG.EffectParameter _world;

		public XNA.Matrix World
		{
			get
			{
				return _world.GetValueMatrix();
			}
			set
			{
				_world.SetValue( value );
			}
		}

		#endregion World Property
		#region LightColor Property

		private static XFG.EffectParameter _lightColorParameter;

		public XFG.EffectParameter LightColor
		{
			get
			{
				return XnaFixedFunctionEffect._lightColorParameter;
			}
		}

		#endregion LightColor Property
		#region LightDirection Property

		private static XFG.EffectParameter _lightDirectionParameter;

		public XFG.EffectParameter LightDirection
		{
			get
			{
				return XnaFixedFunctionEffect._lightDirectionParameter;
			}
		}

		#endregion LightDirection Property
		#region AmbientLightColor Property

		private static XFG.EffectParameter _ambientLightColor;

		public XNA.Vector4 AmbientLightColor
		{
			get
			{
				return _ambientLightColor.GetValueVector4();
			}
			set
			{
				if ( _ambientLightColor != null )
				{
					_ambientLightColor.SetValue( value );
				}
			}
		}

		#endregion AmbientLightColor Property
		#region ModelTexture Property

		private static XFG.EffectParameter _modelTextureParameter;

		public XFG.EffectParameter ModelTexture
		{
			get
			{
				return XnaFixedFunctionEffect._modelTextureParameter;
			}
		}

		#endregion ModelTexture Property
		#region ModelTexture2 Property

		private static XFG.EffectParameter _modelTextureParameter2;

		public XFG.EffectParameter ModelTexture2
		{
			get
			{
				return XnaFixedFunctionEffect._modelTextureParameter2;
			}
			set
			{
				XnaFixedFunctionEffect._modelTextureParameter2 = value;
			}
		}

		#endregion ModelTexture2 Property

		public XFG.EffectTechnique CurrentTechnique
		{
			get
			{
				return _effect.CurrentTechnique;
			}
		}

		private static bool _initialized;
		private static XFG.Effect _effect;

		#endregion Fields & Properties
			
		internal XnaFixedFunctionEffect( XFG.GraphicsDevice device )
		{
			_initialize( device );
		}

		private void _initialize( XFG.GraphicsDevice device )
		{
			lock ( this )
			{
				if ( !_initialized )
				{
					//create a simple effect to draw textured stuff
					string strEffect = System.IO.File.ReadAllText( "FixedFunction.fx" );
					XFG.CompiledEffect compeffect = XFG.Effect.CompileEffectFromSource( strEffect, null, null, XFG.CompilerOptions.Debug, XNA.TargetPlatform.Windows );
					System.Diagnostics.Debug.Assert( compeffect.Success == true, compeffect.ErrorsAndWarnings );
					byte[] effectCode = compeffect.GetEffectCode();

					_effect = new XFG.Effect( device, effectCode, XFG.CompilerOptions.Debug, null );

					_world = _effect.Parameters[ "World" ];
					_view = _effect.Parameters[ "View" ];
					_projection = _effect.Parameters[ "Projection" ];
					_lightColorParameter = _effect.Parameters[ "lightColor" ];
					_lightDirectionParameter = _effect.Parameters[ "lightDirection" ];
					_ambientLightColor = _effect.Parameters[ "ambientColor" ];
					_modelTextureParameter = _effect.Parameters[ "modelTexture" ];
					_modelTextureParameter2 = _effect.Parameters[ "modelTexture2" ];

					_effect.CurrentTechnique = _effect.Techniques[ 0 ];

					_initialized = true;
				}
			}
		}

		internal void CommitChanges()
		{
			_effect.CommitChanges();
		}

		internal void Begin()
		{
			_effect.Begin();
		}

		internal void End()
		{
			_effect.End();
		}
	}
}
