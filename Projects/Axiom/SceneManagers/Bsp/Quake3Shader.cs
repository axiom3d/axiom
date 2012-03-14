#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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

using System;
using System.Collections.Generic;

using Axiom.Controllers;
using Axiom.Core;
using Axiom.Graphics;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Bsp
{
	/// <summary>
	///		Class for recording Quake3 shaders.
	/// </summary>
	/// <remarks>
	///		This is a temporary holding area since shaders are actually converted into
	///		Material objects for use in the engine proper. However, because we have to read
	///		in shader definitions en masse (because they are stored in shared .shader files)
	///		without knowing which will actually be used, we store their definitions here
	///		temporarily since their instantiations as Materials would use precious resources
	///		because of the automatic loading of textures etc.
	/// </remarks>
	public class Quake3Shader : Resource
	{
		#region Fields and Properties

		#region Flags Property

		public uint Flags { get; set; }

		#endregion Flags Property

		private ManualCullingMode _cullingMode;

		public int PassCount
		{
			get
			{
				return this._pass.Count;
			}
		}

		public bool SkyDome { get; set; }

		public float CloudHeight { get; set; }

		public ShaderDeformFunc DeformFunc { get; set; }

		public float[] DeformParams { get; set; }

		public ManualCullingMode CullingMode
		{
			get
			{
				return this._cullingMode;
			}
			set
			{
				this._cullingMode = value;
			}
		}

		public bool Fog { get; set; }

		public ColorEx FogColor { get; set; }

		public float FogDistance { get; set; }

		#region Pass Property

		private readonly List<ShaderPass> _pass;

		public ICollection<ShaderPass> Pass
		{
			get
			{
				return this._pass;
			}
		}

		#endregion Pass Property

		#region Farbox Property

		public bool Farbox { get; set; }

		#endregion Farbox Property

		#region FarboxName Property

		public string FarboxName { get; set; }

		#endregion FarboxName Property

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///		Default constructor - used by <see cref="Quake3ShaderManager"/> (do not call directly)
		/// </summary>
		/// <param name="name">Shader name.</param>
		public Quake3Shader( ResourceManager parent, string name, ResourceHandle handle, string group )
			: base( parent, name, handle, group )
		{
			this.DeformFunc = ShaderDeformFunc.None;
			this.DeformParams = new float[ 5 ];
			this._cullingMode = ManualCullingMode.Back;
			this._pass = new List<ShaderPass>();
		}

		#endregion Construction and Destruction

		#region Methods

		protected string GetAlternateName( string textureName )
		{
			// Get alternative JPG to TGA and vice versa
			int pos;
			string ext, baseName;

			pos = textureName.LastIndexOf( "." );
			ext = textureName.Substring( pos, 4 ).ToLower();
			baseName = textureName.Substring( 0, pos );
			if ( ext == ".jpg" )
			{
				return baseName + ".tga";
			}
			else
			{
				return baseName + ".jpg";
			}
		}

		/// <summary>
		///		Creates this shader as an OGRE material.
		/// </summary>
		/// <remarks>
		///		Creates a new material based on this shaders settings and registers it with the
		///		SceneManager passed in.
		///		Material name is in the format of: shader#lightmap.
		/// </remarks>
		/// <param name="sm">SceneManager to register the material with.</param>
		/// <param name="lightmapNumber">Lightmap number</param>
		public Material CreateAsMaterial( int lightmapNumber )
		{
			string materialName = String.Format( "{0}#{1}", Name, lightmapNumber );
			string groupName = ResourceGroupManager.Instance.WorldResourceGroupName;

			var material = (Material)MaterialManager.Instance.Create( materialName, groupName );
			Pass pass = material.GetTechnique( 0 ).GetPass( 0 );

			LogManager.Instance.Write( "Using Q3 shader {0}", Name );

			for ( int p = 0; p < this._pass.Count; ++p )
			{
				TextureUnitState t;

				// Create basic texture
				if ( this._pass[ p ].textureName == "$lightmap" )
				{
					string lightmapName = String.Format( "@lightmap{0}", lightmapNumber );
					t = pass.CreateTextureUnitState( lightmapName );
				}

					// Animated texture support
				else if ( this._pass[ p ].animNumFrames > 0 )
				{
					float sequenceTime = this._pass[ p ].animNumFrames / this._pass[ p ].animFps;

					/* Pre-load textures
					We need to know if each one was loaded OK since extensions may change for each
					Quake3 can still include alternate extension filenames e.g. jpg instead of tga
					Pain in the arse - have to check for each frame as letters<n>.tga for example
					is different per frame!
					*/
					for ( uint alt = 0; alt < this._pass[ p ].animNumFrames; ++alt )
					{
						if ( !ResourceGroupManager.Instance.ResourceExists( groupName, this._pass[ p ].frames[ alt ] ) )
						{
							// Try alternate extension
							this._pass[ p ].frames[ alt ] = GetAlternateName( this._pass[ p ].frames[ alt ] );

							if ( !ResourceGroupManager.Instance.ResourceExists( groupName, this._pass[ p ].frames[ alt ] ) )
							{
								// stuffed - no texture
								continue;
							}
						}
					}

					t = pass.CreateTextureUnitState( "" );
					t.SetAnimatedTextureName( this._pass[ p ].frames, this._pass[ p ].animNumFrames, sequenceTime );

					if ( t.IsBlank )
					{
						for ( int alt = 0; alt < this._pass[ p ].animNumFrames; alt++ )
						{
							this._pass[ p ].frames[ alt ] = GetAlternateName( this._pass[ p ].frames[ alt ] );
						}

						t.SetAnimatedTextureName( this._pass[ p ].frames, this._pass[ p ].animNumFrames, sequenceTime );
					}
				}
				else
				{
					// Quake3 can still include alternate extension filenames e.g. jpg instead of tga
					// Pain in the arse - have to check for failure
					if ( !ResourceGroupManager.Instance.ResourceExists( groupName, this._pass[ p ].textureName ) )
					{
						// Try alternate extension
						this._pass[ p ].textureName = GetAlternateName( this._pass[ p ].textureName );

						if ( !ResourceGroupManager.Instance.ResourceExists( groupName, this._pass[ p ].textureName ) )
						{
							// stuffed - no texture
							continue;
						}
					}

					t = pass.CreateTextureUnitState( this._pass[ p ].textureName );
				}

				// Blending
				if ( p == 0 )
				{
					// scene blend
					material.SetSceneBlending( this._pass[ p ].blendSrc, this._pass[ p ].blendDest );

					if ( material.IsTransparent && ( this._pass[ p ].blendSrc != SceneBlendFactor.SourceAlpha ) )
					{
						material.DepthWrite = false;
					}

					t.SetColorOperation( LayerBlendOperation.Replace );

					// Alpha Settings
					pass.SetAlphaRejectSettings( this._pass[ p ].alphaFunc, this._pass[ p ].alphaVal );
				}
				else
				{
					if ( this._pass[ p ].customBlend )
					{
						// Fallback for now
						t.SetColorOperation( LayerBlendOperation.Modulate );
					}
					else
					{
						t.SetColorOperation( this._pass[ p ].blend );
					}

					// Alpha mode, prefer 'most alphary'
					CompareFunction currFunc = pass.AlphaRejectFunction;
					int currValue = pass.AlphaRejectValue;
					if ( this._pass[ p ].alphaFunc > currFunc || ( this._pass[ p ].alphaFunc == currFunc && this._pass[ p ].alphaVal < currValue ) )
					{
						pass.SetAlphaRejectSettings( this._pass[ p ].alphaFunc, this._pass[ p ].alphaVal );
					}
				}

				// Tex coords
				if ( this._pass[ p ].texGen == ShaderTextureGen.Base )
				{
					t.TextureCoordSet = 0;
				}
				else if ( this._pass[ p ].texGen == ShaderTextureGen.Lightmap )
				{
					t.TextureCoordSet = 1;
				}
				else if ( this._pass[ p ].texGen == ShaderTextureGen.Environment )
				{
					t.SetEnvironmentMap( true, EnvironmentMap.Planar );
				}

				// Tex mod
				// Scale
				t.SetTextureScaleU( this._pass[ p ].tcModScale[ 0 ] );
				t.SetTextureScaleV( this._pass[ p ].tcModScale[ 1 ] );

				// Procedural mods
				// Custom - don't use mod if generating environment
				// Because I do env a different way it look horrible
				if ( this._pass[ p ].texGen != ShaderTextureGen.Environment )
				{
					if ( this._pass[ p ].tcModRotate != 0.0f )
					{
						t.SetRotateAnimation( this._pass[ p ].tcModRotate );
					}

					if ( ( this._pass[ p ].tcModScroll[ 0 ] != 0.0f ) || ( this._pass[ p ].tcModScroll[ 1 ] != 0.0f ) )
					{
						if ( this._pass[ p ].tcModTurbOn )
						{
							// Turbulent scroll
							if ( this._pass[ p ].tcModScroll[ 0 ] != 0.0f )
							{
								t.SetTransformAnimation( TextureTransform.TranslateU, WaveformType.Sine, this._pass[ p ].tcModTurb[ 0 ], this._pass[ p ].tcModTurb[ 3 ], this._pass[ p ].tcModTurb[ 2 ], this._pass[ p ].tcModTurb[ 1 ] );
							}
							if ( this._pass[ p ].tcModScroll[ 1 ] != 0.0f )
							{
								t.SetTransformAnimation( TextureTransform.TranslateV, WaveformType.Sine, this._pass[ p ].tcModTurb[ 0 ], this._pass[ p ].tcModTurb[ 3 ], this._pass[ p ].tcModTurb[ 2 ], this._pass[ p ].tcModTurb[ 1 ] );
							}
						}
						else
						{
							// Constant scroll
							t.SetScrollAnimation( this._pass[ p ].tcModScroll[ 0 ], this._pass[ p ].tcModScroll[ 1 ] );
						}
					}

					if ( this._pass[ p ].tcModStretchWave != ShaderWaveType.None )
					{
						WaveformType wft = WaveformType.Sine;
						switch ( this._pass[ p ].tcModStretchWave )
						{
							case ShaderWaveType.Sin:
								wft = WaveformType.Sine;
								break;
							case ShaderWaveType.Triangle:
								wft = WaveformType.Triangle;
								break;
							case ShaderWaveType.Square:
								wft = WaveformType.Square;
								break;
							case ShaderWaveType.SawTooth:
								wft = WaveformType.Sawtooth;
								break;
							case ShaderWaveType.InverseSawtooth:
								wft = WaveformType.InverseSawtooth;
								break;
						}

						// Create wave-based stretcher
						t.SetTransformAnimation( TextureTransform.ScaleU, wft, this._pass[ p ].tcModStretchParams[ 3 ], this._pass[ p ].tcModStretchParams[ 0 ], this._pass[ p ].tcModStretchParams[ 2 ], this._pass[ p ].tcModStretchParams[ 1 ] );
						t.SetTransformAnimation( TextureTransform.ScaleV, wft, this._pass[ p ].tcModStretchParams[ 3 ], this._pass[ p ].tcModStretchParams[ 0 ], this._pass[ p ].tcModStretchParams[ 2 ], this._pass[ p ].tcModStretchParams[ 1 ] );
					}
				}
				// Address mode
				t.SetTextureAddressingMode( this._pass[ p ].addressMode );
			}

			// Do farbox (create new material)

			// Do skydome (use this material)
			//if ( _skyDome )
			//{
			//    float halfAngle = 0.5f * ( 0.5f * ( 4.0f * (float)System.Math.Atan( 1.0f ) ) );
			//    float sin = (float)Utility.Sin( halfAngle );

			//    // Quake3 is always aligned with Z upwards
			//    Quaternion q = new Quaternion(
			//        (float)Utility.Cos( halfAngle ),
			//        sin * Vector3.UnitX.x,
			//        sin * Vector3.UnitY.y,
			//        sin * Vector3.UnitX.z
			//        );

			//    // Also draw last, and make close to camera (far clip plane is shorter)
			//    sm.SetSkyDome( true, materialName, 20 - ( _cloudHeight / 256 * 18 ), 12, 2000, false, q );
			//}

			material.CullingMode = Graphics.CullingMode.None;
			material.ManualCullingMode = this._cullingMode;
			material.Lighting = false;
			material.Load();

			return material;
		}

		#endregion Methods

		#region Resource Implementation

		protected override void load()
		{
			// Do nothing.
		}

		protected override void unload()
		{
			// Do nothing.
		}

		#endregion Resource Implementation
	}

	public class ShaderPass
	{
		// Multitexture blend
		public TextureAddressing addressMode;
		public CompareFunction alphaFunc;
		public byte alphaVal;

		public float animFps;
		public int animNumFrames;
		public LayerBlendOperation blend;
		// Multipass blends (Quake3 only supports multipass?? Surely not?)
		public SceneBlendFactor blendDest;
		public SceneBlendFactor blendSrc;
		public bool customBlend;

		public CompareFunction depthFunc;
		public uint flags;
		public string[] frames = new string[ 32 ];

		// TODO - alphaFunc
		public ShaderGen rgbGenFunc;
		public float[] rgbGenParams = new float[ 4 ]; // base, amplitude, phase, frequency
		public ShaderWaveType rgbGenWave;
		public float tcModRotate;
		public float[] tcModScale = new float[ 2 ];
		public float[] tcModScroll = new float[ 2 ];
		public float[] tcModStretchParams = new float[ 4 ]; // base, amplitude, phase, frequency
		public ShaderWaveType tcModStretchWave;
		public float[] tcModTransform = new float[ 6 ];
		public float[] tcModTurb = new float[ 4 ];
		public bool tcModTurbOn;
		public ShaderTextureGen texGen;
		public string textureName;
	};

	[Flags]
	public enum ShaderFlags
	{
		NoCull = 1 << 0,
		Transparent = 1 << 1,
		DepthWrite = 1 << 2,
		Sky = 1 << 3,
		NoMipMaps = 1 << 4,
		NeedColors = 1 << 5,
		DeformVerts = 1 << 6
	}

	[Flags]
	public enum ShaderPassFlags
	{
		Lightmap = 1 << 0,
		Blend = 1 << 1,
		AlphaFunc = 1 << 2,
		TCMod = 1 << 3,
		AnimMap = 1 << 5,
		TCGenEnv = 1 << 6
	}

	public enum ShaderGen
	{
		Identity = 0,
		Wave,
		Vertex
	}

	public enum ShaderTextureGen
	{
		Base = 0,
		Lightmap,
		Environment
	}

	public enum ShaderWaveType
	{
		None = 0,
		Sin,
		Triangle,
		Square,
		SawTooth,
		InverseSawtooth
	}

	public enum ShaderDeformFunc
	{
		None = 0,
		Bulge,
		Wave,
		Normal,
		Move,
		AutoSprite,
		AutoSprite2
	}
}
