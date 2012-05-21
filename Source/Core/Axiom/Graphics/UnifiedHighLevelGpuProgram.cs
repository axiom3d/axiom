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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Scripting;
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

		private readonly List<String> _delegateNames = new List<string>();

		private HighLevelGpuProgram _chosenDelegate;

		public HighLevelGpuProgram Delegate
		{
			get
			{
				if ( this._chosenDelegate == null )
				{
					chooseDelegate();
				}
				return this._chosenDelegate;
			}
		}

		#endregion Fields and Properties

		#region Construction and Destruction

		internal UnifiedHighLevelGpuProgram( ResourceManager creator, string name, ResourceHandle handle, string group )
			: this( creator, name, handle, group, false, null )
		{
		}

		internal UnifiedHighLevelGpuProgram( ResourceManager creator, string name, ResourceHandle handle, string group,
		                                     bool isManual, IManualResourceLoader loader )
			: base( creator, name, handle, group, isManual, loader )
		{
		}

		#endregion Construction and Destruction

		#region Methods

		/// Choose the delegate to use
		protected virtual void chooseDelegate()
		{
			this._chosenDelegate = null;
			foreach ( var delegateName in this._delegateNames )
			{
				var program = HighLevelGpuProgramManager.Instance[ delegateName ];
				if ( program != null && program.IsSupported )
				{
					this._chosenDelegate = program;
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
			this._delegateNames.Add( delegateName );
			// Invalidate current selection
			this._chosenDelegate = null;
		}

		/// <summary>
		/// Remove all delegate programs
		/// </summary>
		public void ClearDelegatePrograms()
		{
			this._delegateNames.Clear();
			// Invalidate current selection
			this._chosenDelegate = null;
		}

		#endregion Methods

		#region Command objects

		#region DelegateCommand

		[OgreVersion( 1, 7, 2 )]
		[ScriptableProperty( "delegate", "Additional delegate programs containing implementations." )]
		public class DelegateCommand : IPropertyCommand
		{
			#region IPropertyCommand Members

			[OgreVersion( 1, 7, 2 )]
			public string Get( object target )
			{
				// Can't do this (not one delegate), shouldn't matter
				return string.Empty;
			}

			[OgreVersion( 1, 7, 2 )]
			public void Set( object target, string val )
			{
				( (UnifiedHighLevelGpuProgram)target ).AddDelegateProgram( val );
			}

			#endregion IPropertyCommand Members
		}

		#endregion DelegateCommand

		#endregion Command objects

		#region HighLevelGpuProgram Implementation

		public override GpuProgram BindingDelegate
		{
			get
			{
				if ( Delegate != null )
				{
					return Delegate.BindingDelegate;
				}

				return null;
			}
		}

		public override bool IsMorphAnimationIncluded
		{
			get
			{
				if ( Delegate != null )
				{
					return Delegate.IsMorphAnimationIncluded;
				}

				return false;
			}
			set
			{
				if ( Delegate != null )
				{
					Delegate.IsMorphAnimationIncluded = value;
				}
			}
		}

		public override bool IsSkeletalAnimationIncluded
		{
			get
			{
				if ( Delegate != null )
				{
					return Delegate.IsSkeletalAnimationIncluded;
				}
				return false;
			}
			set
			{
				if ( Delegate != null )
				{
					Delegate.IsSkeletalAnimationIncluded = value;
				}
			}
		}

		public override ushort PoseAnimationCount
		{
			get
			{
				if ( Delegate != null )
				{
					return Delegate.PoseAnimationCount;
				}
				return 0;
			}
			set
			{
				if ( Delegate != null )
				{
					Delegate.PoseAnimationCount = value;
				}
			}
		}

		public override bool IsSupported
		{
			get
			{
				return Delegate != null;
			}
		}

		public override bool PassSurfaceAndLightStates
		{
			get
			{
				if ( Delegate != null )
				{
					return Delegate.PassSurfaceAndLightStates;
				}
				return false;
			}
		}

		public override bool HasDefaultParameters
		{
			get
			{
				if ( Delegate != null )
				{
					return Delegate.HasDefaultParameters;
				}
				return false;
			}
		}

		public override GpuProgramParameters DefaultParameters
		{
			get
			{
				if ( Delegate != null )
				{
					return Delegate.DefaultParameters;
				}
				return null;
			}
		}

		protected override void BuildConstantDefinitions()
		{
			throw new NotImplementedException();
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
				var p = GpuProgramManager.Instance.CreateParameters();
				p.IgnoreMissingParameters = true;
				return p;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public override void Load( bool background )
		{
			if ( Delegate != null )
			{
				Delegate.Load( background );
			}
		}

		[OgreVersion( 1, 7, 2 )]
		protected override void CreateLowLevelImpl()
		{
			throw new AxiomException( "This method should never get called!" );
		}

		[OgreVersion( 1, 7, 2 )]
		protected override void UnloadHighLevelImpl()
		{
			throw new AxiomException( "This method should never get called!" );
		}

		[OgreVersion( 1, 7, 2 )]
		protected override void LoadFromSource()
		{
			throw new AxiomException( "This method should never get called!" );
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

		public override HighLevelGpuProgram CreateInstance( ResourceManager creator, string name, ResourceHandle handle,
		                                                    string group, bool isManual, IManualResourceLoader loader )
		{
			return new UnifiedHighLevelGpuProgram( creator, name, handle, group, isManual, loader );
		}

		#endregion
	}
}