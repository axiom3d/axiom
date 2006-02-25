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


using System;

namespace Axiom.Engine
{
    /// <summary>
    ///     Predefined controller value for getting/setting the frame number of a texture unit.
    /// </summary>
    public class TextureFrameControllerValue : IControllerValue
    {
        #region Fields

        /// <summary>
        ///     Reference to the texture unit state to target for the animation.
        /// </summary>
        protected TextureUnitState texUnit;

        #endregion Fields

        #region Constructor

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="textureUnit">Reference to the texture unit state to target for the animation.</param>
        public TextureFrameControllerValue( TextureUnitState textureUnit )
        {
            this.texUnit = textureUnit;
        }

        #endregion Constructor

        #region IControllerValue Members

        /// <summary>
        ///     Gets/Sets the frame of animation for a texture unit.
        /// </summary>
        /// <remarks>
        ///     Value is a parametric value in the range [0,1].
        /// </remarks>
        public float Value
        {
            get
            {
                return texUnit.CurrentFrame / texUnit.NumFrames;
            }
            set
            {
                texUnit.CurrentFrame = (int)( value * texUnit.NumFrames );
            }
        }


        #endregion IControllerValue Members
    }
}
