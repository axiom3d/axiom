#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Core;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations
			
namespace Axiom.Graphics
{
	/// <summary>
	/// Specialization of HighLevelGpuProgram which just delegates its implementation
	/// to one other high level program, allowing a single program definition
	/// to represent one supported program from a number of options
	/// </summary>
	/// <remarks>
	/// Whilst you can use Technique to implement several ways to render an object
	/// depending on hardware support, if the only reason to need multiple paths is
	/// because of the high-level shader language supported, this can be 
	/// cumbersome. For example you might want to implement the same shader 
	/// in HLSL and	GLSL for portability but apart from the implementation detail,
	/// the shaders do the same thing and take the same parameters. If the materials
	/// in question are complex, duplicating the techniques just to switch language
	/// is not optimal, so instead you can define high-level programs with a 
	/// syntax of 'unified', and list the actual implementations in order of
	/// preference via repeated use of the 'delegate' parameter, which just points
	/// at another program name. The first one which has a supported syntax
	/// will be used.
	/// </remarks>
	public class UnifiedHighLevelGpuProgram : HighLevelGpuProgram
	{
		#region Fields and Properties

		private List<String> _delegateNames = new List<string>();

		private HighLevelGpuProgram _chosenDelgate;
		public HighLevelGpuProgram Delegate
		{
			get
			{
				if ( _chosenDelgate == null )
					chooseDelegate();
				return _chosenDelgate;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		internal UnifiedHighLevelGpuProgram( ResourceManager creator, string name, ResourceHandle handle, string group )
			: this( creator, name, handle, group, false, null )
		{
		}

		internal UnifiedHighLevelGpuProgram( ResourceManager creator, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( creator, name, handle, group, isManual, loader )
		{
		}

		#endregion Construction and Destruction

		#region Methods

		/// Choose the delegate to use
		protected virtual void chooseDelegate()
		{
			_chosenDelgate = null;
			foreach ( string delegateName in _delegateNames )
			{
				HighLevelGpuProgram program = HighLevelGpuProgramManager.Instance[ delegateName ];
				if ( program != null && program.IsSupported )
				{
					_chosenDelgate = program;
					break;
				}
			}
		}

		protected virtual void buildConstantDefinitions()
		{
		}

		/// <summary>
		/// Adds a new delegate program to the list.
		/// </summary>
		/// <remarks>
		/// Delegates are tested in order so earlier ones are preferred.
		/// </remarks>
		/// <param name="delegateName"></param>
		public void AddDelegateProgram( string delegateName )
		{
			_delegateNames.Add( delegateName );
			// Invalidate current selection
			_chosenDelgate = null;
		}

		/// <summary>
		/// Remove all delegate programs
		/// </summary>
		public void ClearDelegatePrograms()
		{
			_delegateNames.Clear();
			// Invalidate current selection
			_chosenDelgate = null;
		}

		#endregion Methods

		#region HighLevelGpuProgram Implementation

		public override GpuProgram BindingDelegate
		{
			get
			{
				if ( _chosenDelgate != null )
				{
					return _chosenDelgate.BindingDelegate;
				}

				return null;
			}
		}

		public override bool IsMorphAnimationIncluded
		{
			get
			{
				if ( _chosenDelgate != null )
				{
					return _chosenDelgate.IsMorphAnimationIncluded;
				}

				return false;
			}
			set
			{
				if ( _chosenDelgate != null )
					_chosenDelgate.IsMorphAnimationIncluded = value;
			}
		}

		public override bool IsSkeletalAnimationIncluded
		{
			get
			{
				if ( _chosenDelgate != null )
				{
					return _chosenDelgate.IsSkeletalAnimationIncluded;
				}
				return false;
			}
			set
			{
				if ( _chosenDelgate != null )
					_chosenDelgate.IsSkeletalAnimationIncluded = value;
			}
		}

		public override ushort PoseAnimationCount
		{
			get
			{
				if ( _chosenDelgate != null )
				{
					return _chosenDelgate.PoseAnimationCount;
				}
				return 0;
			}
			set
			{
				if ( _chosenDelgate != null )
					_chosenDelgate.PoseAnimationCount = value;
			}
		}

		public override bool IsSupported
		{
			get
			{
				return _chosenDelgate != null;
			}
		}

		public override bool PassSurfaceAndLightStates
		{
			get
			{
				if ( _chosenDelgate != null )
				{
					return _chosenDelgate.PassSurfaceAndLightStates;
				}
				return false;
			}
			set
			{
				if ( _chosenDelgate != null )
					_chosenDelgate.PassSurfaceAndLightStates = value;
			}
		}

		public override GpuProgramParameters CreateParameters()
		{
			if ( IsSupported )
			{
				return Delegate.CreateParameters();
			}
			else
			{
				//return a default set
				GpuProgramParameters p = GpuProgramManager.Instance.CreateParameters();
				//TODO : p.IgnoreMissingParameters = true;
				return p;
			}
		}

		protected override void CreateLowLevelImpl()
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		protected override void UnloadImpl()
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		protected override void PopulateParameterNames( GpuProgramParameters parms )
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		public override bool SetParam( string name, string val )
		{
			switch ( name )
			{
				case "delegate":
					AddDelegateProgram( val );
					return true;
					break;
			}
			return false;
		}

		protected override void LoadFromSource()
		{
			throw new Exception( "The method or operation is not implemented." );
		}

		public override int SamplerCount
		{
			get
			{
				if ( _chosenDelgate != null )
				{
					return _chosenDelgate.SamplerCount;
				}
				return 0;
			}
		}

		#endregion HighLevelGpuProgram Implementation
	}

	public class UnifiedHighLevelGpuProgramFactory : HighLevelGpuProgramFactory
	{
		#region IHighLevelGpuProgramFactory Members

		public override string Language
		{
			get
			{
				return "unified";
			}
		}

		public override HighLevelGpuProgram  CreateInstance(ResourceManager creator, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader)
		{
			return new UnifiedHighLevelGpuProgram( creator, name, handle, group, isManual, loader );
		}

		#endregion
	}
}
