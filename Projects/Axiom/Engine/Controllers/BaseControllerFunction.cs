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

using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Controllers
{
	/// <summary>
	///		Subclasses of this class are responsible for performing a function on an input value for a Controller.
	///	 </summary>
	///	 <remarks>
	///		This abstract class provides the interface that needs to be supported for a custom function which
	///		can be 'plugged in' to a Controller instance, which controls some object value based on an input value.
	///		For example, the WaveControllerFunction class provided by Ogre allows you to use various waveforms to
	///		translate an input value to an output value.
	///		<p/>
	///		This base class implements IControllerFunction, but leaves the implementation up to the subclasses.
	/// </remarks>
	public abstract class BaseControllerFunction : IControllerFunction<Real>
	{
		#region Member variables

		/// <summary>
		///		Value to be added during evaluation.
		/// </summary>
		protected Real deltaCount;

		/// <summary>
		///		If true, function will add input values together and wrap at 1.0 before evaluating.
		/// </summary>
		protected bool useDeltaInput;

		#endregion

		#region Constructors

		public BaseControllerFunction( bool useDeltaInput )
		{
			this.useDeltaInput = useDeltaInput;
			//deltaCount = 0; //[FXCop Optimization : Do not initialize unnecessarily], Defaults to 0, left here for clarity
		}

		#endregion

		#region Methods

		/// <summary>
		///		Adjusts the input value by a delta.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		protected virtual Real AdjustInput( Real input )
		{
			if ( this.useDeltaInput )
			{
				// wrap the value if it went past 1
				this.deltaCount = ( this.deltaCount + input ) % 1.0f;

				// return the adjusted input value
				return this.deltaCount;
			}
			else
			{
				// return the input value as is
				return input;
			}
		}

		#endregion

		#region IControllerFunction methods

		public abstract Real Execute( Real sourceValue );

		#endregion
	}
}
