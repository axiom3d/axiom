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

using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Controllers.Canned
{
    /// <summary>
    /// Summary description for TexCoordModifierControllerValue.
    /// </summary>
    public class TexCoordModifierControllerValue : IControllerValue<Real>
    {
        #region Member variables

        protected bool transU;
        protected bool transV;
        protected bool scaleU;
        protected bool scaleV;
        protected bool rotate;
        protected TextureUnitState texUnit;

        #endregion

        public TexCoordModifierControllerValue(TextureUnitState texUnit)
        {
            this.texUnit = texUnit;
        }

        public TexCoordModifierControllerValue(TextureUnitState texUnit, bool scrollU)
            : this(texUnit, scrollU, false)
        {
        }

        public TexCoordModifierControllerValue(TextureUnitState texUnit, bool scrollU, bool scrollV)
        {
            this.texUnit = texUnit;
            this.transU = scrollU;
            this.transV = scrollV;
        }

        public TexCoordModifierControllerValue(TextureUnitState texUnit, bool scrollU, bool scrollV, bool scaleU, bool scaleV,
                                                bool rotate)
        {
            this.texUnit = texUnit;
            this.transU = scrollU;
            this.transV = scrollV;
            this.scaleU = scaleU;
            this.scaleV = scaleV;
            this.rotate = rotate;
        }

        #region IControllerValue Members

        public Real Value
        {
            get
            {
                var trans = this.texUnit.TextureMatrix;

                if (this.transU)
                {
                    return trans.m03;
                }
                else if (this.transV)
                {
                    return trans.m13;
                }
                else if (this.scaleU)
                {
                    return trans.m00;
                }
                else if (this.scaleV)
                {
                    return trans.m11;
                }

                // should never get here
                return 0.0f;
            }
            set
            {
                if (this.transU)
                {
                    this.texUnit.SetTextureScrollU(value);
                }

                if (this.transV)
                {
                    this.texUnit.SetTextureScrollV(value);
                }

                if (this.scaleU)
                {
                    if (value >= 0)
                    {
                        this.texUnit.SetTextureScaleU(1 + value);
                    }
                    else
                    {
                        this.texUnit.SetTextureScaleU(1 / -value);
                    }
                }

                if (this.scaleV)
                {
                    if (value >= 0)
                    {
                        this.texUnit.SetTextureScaleV(1 + value);
                    }
                    else
                    {
                        this.texUnit.SetTextureScaleV(1 / -value);
                    }
                }

                if (this.rotate)
                {
                    this.texUnit.SetTextureRotate(value * 360);
                }
            }
        }

        #endregion
    }
}