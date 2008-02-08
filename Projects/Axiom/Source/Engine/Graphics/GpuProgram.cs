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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;
using System.Collections;

using Axiom.Core;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Defines a program which runs on the GPU such as a vertex or fragment program.
	/// </summary>
	public abstract class GpuProgram : Resource
	{
		#region Fields and Properties

		#region BindingDelegate Property

		/// <summary>
		///    Returns the GpuProgram which should be bound to the pipeline.
		/// </summary>
		/// <remarks>
		///    This method is simply to allow some subclasses of GpuProgram to delegate
		///    the program which is bound to the pipeline to a delegate, if required.
		/// </remarks>
		public virtual GpuProgram BindingDelegate
		{
			get
			{
				return this;
			}
		}

		#endregion BindingDelegate Property

		/// <summary>
		///    Whether this source is being loaded from file or not.
		/// </summary>
		protected bool loadFromFile;

		/// <summary>
		/// Did we encounter a compilation error?
		/// </summary>
		protected bool _compileError = false;

		#region SourceFile Property

		/// <summary>
		///    The name of the file to load from source (may be blank).
		/// </summary>
		protected string fileName;
		/// <summary>
		///    Gets/Sets the source file for this program.
		/// </summary>
		/// <remarks>
		///    Setting this will have no effect until you (re)load the program.
		/// </remarks>
		public string SourceFile
		{
			get
			{
				return fileName;
			}
			set
			{
				fileName = value;
				source = "";
				loadFromFile = true;
				_compileError = false;
			}
		}

		#endregion SourceFile Property

		#region Source Property

		/// <summary>
		///    The assembler source of this program.
		/// </summary>
		protected string source;
		/// <summary>
		///    Gets/Sets the source assembler code for this program.
		/// </summary>
		/// <remarks>
		///    Setting this will have no effect until you (re)load the program.
		/// </remarks>
		public string Source
		{
			get
			{
				return source;
			}
			set
			{
				source = value;
				fileName = "";
				loadFromFile = false;
				_compileError = false;
			}
		}

		#endregion Source Property

		#region SyntaxCode Property

		/// <summary>
		///    Syntax code (i.e. arbvp1, vs_2_0, etc.)
		/// </summary>
		protected string syntaxCode;
		/// <summary>
		///    Gets the syntax code of this program (i.e. arbvp1, vs_1_1, etc).
		/// </summary>
		public string SyntaxCode
		{
			get
			{
				return syntaxCode;
			}
			set
			{
				syntaxCode = value;
			}
		}

		#endregion SyntaxCode Property

		#region Type Property

		/// <summary>
		///    Type of program this represents (vertex or fragment).
		/// </summary>
		protected GpuProgramType type;
		/// <summary>
		///    Gets the type of GPU program this represents (vertex or fragment).
		/// </summary>
		public virtual GpuProgramType Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
			}
		}

		#endregion Type Property

		#region IsSkeletalAnimationIncluded Property

		/// <summary>
		///		Flag indicating whether this program is being used for hardware skinning.
		/// </summary>
		protected bool isSkeletalAnimationSupported;
		/// <summary>
		///		Gets/Sets whether a vertex program includes the required instructions
		///		to perform skeletal animation. 
		/// </summary>
		public virtual bool IsSkeletalAnimationIncluded
		{
			get
			{
				return isSkeletalAnimationSupported;
			}
			set
			{
				isSkeletalAnimationSupported = value;
			}
		}

		#endregion IsSkeletalAnimationIncluded Property

		#region IsMorphAninimationIncluded Property

		/// <summary>
		///		Does this (vertex) program include morph animation?
		/// </summary>
		protected bool isMorphAnimationSupported;
		/// <summary>
		///		Gets/Sets whether a vertex program includes the required instructions
		///		to perform morph animation. 
		/// </summary>
		public virtual bool IsMorphAnimationIncluded
		{
			get
			{
				return isMorphAnimationSupported;
			}
			set
			{
				isMorphAnimationSupported = value;
			}
		}

		#endregion IsMorphAninimationIncluded Property

		#region IsVertexTextureFetchRequired Property

		/// <summary>
		///		Does this (vertex) program require vertex texture fetch?
		/// </summary>
		protected bool _isVertexTextureFetchRequired;
		/// <summary>
		///		Gets/Sets whether this vertex program requires support for vertex 
		///		texture fetch from the hardware. 
		/// </summary>
		public virtual bool IsVertexTextureFetchRequired
		{
			get
			{
				return _isVertexTextureFetchRequired;
			}
			set
			{
				_isVertexTextureFetchRequired = value;
			}
		}

		#endregion IsVertexTextureFetchRequired Property

		#region PoseAnimationCount Property

		/// <summary>
		///		Does this (vertex) program include morph animation?
		/// </summary>
		protected ushort poseAnimationCount;
		/// <summary>
		///		Gets/Sets whether a vertex program includes the required instructions
		///		to perform pose animation. 
		/// </summary>
		public virtual ushort PoseAnimationCount
		{
			get
			{
				return poseAnimationCount;
			}
			set
			{
				poseAnimationCount = value;
			}
		}

		#endregion PoseAnimationCount Property

		#region DefaultParameters Property

		/// <summary>
		///		List of default parameters, as gathered from the program definition.
		/// </summary>
		protected GpuProgramParameters defaultParams;
		/// <summary>
		///		List of default parameters, as gathered from the program definition.
		/// </summary>
		public virtual GpuProgramParameters DefaultParameters
		{
			get
			{
				if ( defaultParams == null )
				{
					defaultParams = this.CreateParameters();
				}
				return defaultParams;
			}
		}

		#endregion DefaultParameters Property

		#region PassSurfaceAndLightStates Property

		/// <summary>
		///		Does this program want light states passed through fixed pipeline?
		/// </summary>
		protected bool passSurfaceAndLightStates;
		/// <summary>
		///		Sets whether a vertex program requires light and material states to be passed
		///		to through fixed pipeline low level API rendering calls.
		/// </summary>
		/// <remarks>
		///		If this is set to true, Axiom will pass all active light states to the fixed function
		///		pipeline.  This is useful for high level shaders like GLSL that can read the OpenGL
		///		light and material states.  This way the user does not have to use autoparameters to 
		///		pass light position, color etc.
		/// </remarks>
		public virtual bool PassSurfaceAndLightStates
		{
			get
			{
				return passSurfaceAndLightStates;
			}
			set
			{
				passSurfaceAndLightStates = value;
			}
		}

		#endregion PassSurfaceAndLightStates Property

		#region IsSupported Property

		/// <summary>
		///    Returns whether this program can be supported on the current renderer and hardware.
		/// </summary>
		public virtual bool IsSupported
		{
			get
			{
				if ( _compileError || !isRequiredCapabilitiesSupported())
				{

					return false;
				}

				return GpuProgramManager.Instance.IsSyntaxSupported( syntaxCode );
			}
		}

		#endregion IsSupported Property

		#region SamplerCount Property

		/// <summary>
		/// Returns the maximum number of samplers that this fragment program has access
		/// to, based on the fragment program profile it uses.
		/// </summary>
		public abstract int SamplerCount
		{
			get;
		}

		#endregion SamplerCount Property

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///    Constructor for creating
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		public GpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			this.Name = name;

			this.type = GpuProgramType.Vertex;
			this.loadFromFile = true;
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///    Creates a new parameters object compatible with this program definition.
		/// </summary>
		/// <remarks>
		///    It is recommended that you use this method of creating parameters objects
		///    rather than going direct to GpuProgramManager, because this method will
		///    populate any implementation-specific extras (like named parameters) where
		///    they are appropriate.
		/// </remarks>
		/// <returns></returns>
		public virtual GpuProgramParameters CreateParameters()
		{
			GpuProgramParameters newParams = GpuProgramManager.Instance.CreateParameters();

			// copy the default parameters if they exist
			if ( defaultParams != null )
			{
				newParams.CopyConstantsFrom( defaultParams );
			}

			return newParams;
		}

		/// <summary>
		///    Loads this Gpu Program.
		/// </summary>
		protected override void load()
		{
			// load from file and get the source string from it
			if ( loadFromFile )
			{
				Stream stream = ResourceGroupManager.Instance.OpenResource( fileName, this.Group );
				StreamReader reader = new StreamReader( stream, System.Text.Encoding.ASCII );
				source = reader.ReadToEnd();
			}

			try
			{
				// call polymorphic load to read source
				LoadFromSource();
			}
			catch ( Exception ex )
			{
				LogManager.Instance.Write( "Gpu program {0} encountered an error during loading and is thus not supported.", Name );
				_compileError = true;
			}
		}

        protected override void unload()
        {
        }

		/// <summary>
		///    Method which must be implemented by subclasses, loads the program from source.
		/// </summary>
		protected abstract void LoadFromSource();

		protected bool isRequiredCapabilitiesSupported()
		{
			RenderSystemCapabilities caps = Root.Instance.RenderSystem.HardwareCapabilities;
			// If skeletal animation is being done, we need support for UBYTE4
			if ( this.IsSkeletalAnimationIncluded &&
				!caps.HasCapability( Capabilities.VertexFormatUByte4 ) )
			{
				return false;
			}

			// Vertex texture fetch required?
			if ( this.IsVertexTextureFetchRequired &&
				!caps.HasCapability( Capabilities.VertexTextureFetch ) )
			{
				return false;
			}

			return true;
		}

		#endregion

	}
}
