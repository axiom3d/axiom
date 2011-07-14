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
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	This class makes the usage of a vertex and fragment programs (low-level or high-level), 
	/// 	with a given set of parameters, explicit.
	/// </summary>
	/// <remarks>
	/// 	Using a vertex or fragment program can get fairly complex; besides the fairly rudimentary
	/// 	process of binding a program to the GPU for rendering, managing usage has few
	/// 	complications, such as:
	/// 	<ul>
	/// 	<li>Programs can be high level (e.g. Cg, GLSlang) or low level (assembler). Using
	/// 	either should be relatively seamless, although high-level programs give you the advantage
	/// 	of being able to use named parameters, instead of just indexed registers</li>
	/// 	<li>Programs and parameters can be shared between multiple usages, in order to save
	/// 	memory</li>
	/// 	<li>When you define a user of a program, such as a material, you often want to be able to
	/// 	set up the definition but not load / compile / assemble the program at that stage, because
	/// 	it is not needed just yet. The program should be loaded when it is first needed, or
	/// 	earlier if specifically requested. The program may not be defined at this time, you
	/// 	may want to have scripts that can set up the definitions independent of the order in which
	/// 	those scripts are loaded.</li>
	/// 	</ul>
	/// 	This class packages up those details so you don't have to worry about them. For example,
	/// 	this class lets you define a high-level program and set up the parameters for it, without
	/// 	having loaded the program (which you normally could not do). When the program is loaded and
	/// 	compiled, this class will then validate the parameters you supplied earlier and turn them
	/// 	into runtime parameters.
	/// 	<p/>
	/// 	Just incase it wasn't clear from the above, this class provides linkage to both 
	/// 	GpuProgram and HighLevelGpuProgram, despite its name.
	/// </remarks>
    public class GpuProgramUsage: DisposableObject
	{
        // TODO: Resource::Listener implementation!

		#region Member variables

        #region type

        /// <summary>
		///    Type of program (vertex or fragment) this usage is being specified for.
		/// </summary>
        [OgreVersion(1, 7, 2790)]
		protected GpuProgramType type;

        #endregion

        #region parent

        [OgreVersion(1, 7, 2790)]
	    protected Pass parent;

        #endregion

        #region recreateParams

        /// Whether to recreate parameters next load
        [OgreVersion(1, 7, 2790)]
        bool recreateParams;

        #endregion

        #region program

        /// <summary>
		///    Reference to the program whose usage is being specified within this class.
		/// </summary>
        [OgreVersion(1, 7, 2790)]
		protected GpuProgram program;

        #endregion

        #region parameters

        /// <summary>
		///    Low level GPU program parameters.
		/// </summary>
        [OgreVersion(1, 7, 2790)]
		protected GpuProgramParameters parameters;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
	    ///    Default constructor.
	    /// </summary>
	    /// <param name="type">Type of program to link to.</param>
	    /// <param name="parent"></param>
        [OgreVersion(1, 7, 2790)]
	    public GpuProgramUsage( GpuProgramType type, Pass parent )
		{
			this.type = type;
	        this.parent = parent;
            recreateParams = false;

		}

        /// <summary>
        /// Copy constructor
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public GpuProgramUsage(GpuProgramUsage oth, Pass parent)
        {
            type = oth.type;
            this.parent = parent;
            program = oth.Program;
            // nfz: parameters should be copied not just use a shared ptr to the original
            parameters = new GpuProgramParameters( oth.parameters );
            recreateParams = false;
        }

	    #endregion

        #region dispose

        [OgreVersion(1, 7, 2790)]
        protected override void dispose(bool disposeManagedResources)
        {
            if (disposeManagedResources)
            {
                // Listener not in place yet
                //if (program != null)
                //    program.RemoveListener( this );
            }
            base.dispose(disposeManagedResources);
        }

        #endregion

        #region Methods

        #region Load

        /// <summary>
		///    Load this usage (and ensure program is loaded).
		/// </summary>
        [OgreVersion(1, 7, 2790)]
        internal void Load()
		{
		    if ( !program.IsLoaded )
		        program.Load();

		    // check type
		    if ( program.IsLoaded && program.Type != type )
		    {
		        var myType = type.ToString();
		        var yourType = program.Type.ToString();
		        throw new AxiomException(
		            "{0} is a {1} program, but you are assigning it to a {2} program slot. This is invalid.",
		            program.Name, yourType, myType );
		    }

            // hackaround as Listener::loadingComplete is not in place, yet
            if (recreateParams)
                RecreateParameters();
		}

        #endregion

        #region Unload

        /// <summary>
		///    Unload this usage.
		/// </summary>
        [OgreVersion(1, 7, 2790)]
		internal void Unload()
		{
			// TODO?

            // hackaround as Listener::unloadingComplete is not in place, yet
            recreateParams = true;
		}

        #endregion

        #region RecreateParameters

        [OgreVersion(1, 7, 2790)]
        protected void RecreateParameters()
        {
            // Keep a reference to old ones to copy
            var savedParams = parameters;

            // Create new params
            parameters = program.CreateParameters();

            // Copy old (matching) values across
            // Don't use copyConstantsFrom since program may be different
            if (savedParams != null)
                parameters.CopyMatchingNamedConstantsFrom(savedParams);

            recreateParams = false;
        }

        #endregion

        #endregion

        #region Properties

        #region SetProgramName

        /// <summary>
        ///    Sets the name of the program we're trying to link to.
        /// </summary>
        /// <remarks>
        ///    Note that this will create a fresh set of parameters from the 
        ///    new program being linked, so if you had previously set parameters 
        ///    you will have to set them again. 
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public void SetProgramName(string name, bool resetParams = true)
        {

            if ( program != null )
            {
                // Listener not in place, yet
                //program.RemoveListener( this );
                //recreateParams = true;
            }

            // get a reference to the gpu program
            program = GpuProgramManager.Instance.GetByName(name);

            if ( program == null )
            {
                throw new Exception(string.Format("Unable to locate gpu program named '{0}'", name));
            }

            // Reset parameters 
            if ( resetParams || parameters == null || recreateParams )
            {
                RecreateParameters();
            }

            // Listener not in place, yet
            // Listen in on reload events so we can regenerate params
            //program.AddListener( this );

        }

        #endregion

        #region ProgramName

        /// <summary>
		///    Gets the name of the program we're trying to link to.
		/// </summary>
        [OgreVersion(1, 7, 2790)]
		public string ProgramName
		{
			get
			{
				return program.Name;
			}
		}

        #endregion

        #region Program

        /// <summary>
        ///    Gets the program this usage is linked to; only available after the usage has been
        ///    validated either via enableValidation or by enabling validation on construction.
        /// </summary>
        /// <remarks>
        ///    Note that this will create a fresh set of parameters from the
        ///    new program being linked, so if you had previously set parameters
        ///    you will have to set them again.
        /// </remarks>
        [OgreVersion(1, 7, 2790)]
        public GpuProgram Program
        {
            get
            {
                return program;
            }
            set
            {
                program = value;

                // create program specific parameters
                parameters = program.CreateParameters();
            }
        }

        #endregion

        #region Parameters

        /// <summary>
        ///    Gets/Sets the program parameters that should be used; because parameters can be
        ///    shared between multiple usages for efficiency, this method is here for you
        ///    to register externally created parameter objects.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public GpuProgramParameters Parameters
        {
            get
            {
                if (parameters == null)
                {
                    throw new Exception("A program must be loaded before its parameters can be retreived.");
                }

                return parameters;
            }
            set
            {
                parameters = value;
            }
        }

        #endregion

        #region Type

        /// <summary>
		///    Gets the type of program we're trying to link to.
		/// </summary>
        [OgreVersion(1, 7, 2790)]
		public GpuProgramType Type
		{
			get
			{
				return type;
			}
        }

        #endregion

        #endregion
    }
}
