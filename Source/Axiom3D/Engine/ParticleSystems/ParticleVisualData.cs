#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace Declarations
using System;
#endregion Namespace Declarations

namespace Axiom
{
    /// <summary>
    /// Abstract class containing any additional data required to be associated
    /// with a particle to perform the required rendering. 
    /// </summary>
	/// <remarks>
	/// Because you can specialise the way that particles are renderered by supplying
	/// custom ParticleSystemRenderer classes, you might well need some additional 
	/// data for your custom rendering routine which is not held on the default particle
	/// class. If that's the case, then you should define a subclass of this class, 
	/// and construct it when asked in your custom ParticleSystemRenderer class.
    /// </remarks>
	public abstract class ParticleVisualData
	{
        /// <summary>
        /// Default Constructor
        /// </summary>
	    public ParticleVisualData() {}
	};
}
