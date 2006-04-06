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

#region Namespace Declarations

using System;
using System.Collections;

using Axiom;
using DotNet3D.Math;

using Axiom.SceneManagers.PagingLandscape.Collections;

#endregion Namespace Declarations
			
namespace Axiom.SceneManagers.PagingLandscape.Data2D
{
    public delegate Real[] GenerateHeightField(Real tileX, Real tileZ, long pageSize);
	public delegate Real GenerateHeightPoint(int x, int z, Real tileX, Real tileZ, long pageSize);

    /// <summary>
	/// A specialized class for loading 2D Data from a HeightField file.
	/// </summary>
	public class Data2D_Dynamic: Data2D
	{
		#region Fields

        static public GenerateHeightField generateHeightField;
		static public GenerateHeightPoint generateHeightPoint;

		protected int tileX;
		protected int tileZ;

		#endregion Fields

		#region Constructor

		public Data2D_Dynamic(int x, int z) : base()
		{
            maxheight = 256.0f * Options.Instance.Scale.y;
			dynamic = true;
			tileX = x;
			tileZ = z;
        }

		#endregion Constructor

		#region IDisposable Members

		public override void Dispose()
		{
			base.Dispose();
		}

		#endregion

		public override Real GetHeight( Real x, Real z ) 
		{
			if ( ( heightData == null ) && dynamic ) 
			{

				return generateHeightPoint((int)x, (int)z, tileX, tileZ, Options.Instance.PageSize);

			} 
			else 
			{
				return base.GetHeight(x, z);
			}
		}

		public override Real GetHeight( long x, long z ) 
		{
			if ( ( heightData == null ) && dynamic ) 
			{

				return generateHeightPoint((int)x, (int)z, tileX, tileZ, Options.Instance.PageSize);

			} 
			else 
			{
				return base.GetHeight(x, z);
			}
		}

		public override Real GetHeight( int x, int z ) 
		{
			if ( ( heightData == null ) && dynamic ) 
			{

				return generateHeightPoint((int)x, (int)z, tileX, tileZ, Options.Instance.PageSize);

			} 
			else 
			{
				return base.GetHeight(x, z);
			}
		}

		public override Vector3 GetNormalAt (Real X, Real Z)
		{
			Real X1 = X-1;
			Real X2 = X+1;
			if ( X1 < 0 ) 

			{
				X1 = 0;
			}
			if ( X2 > ( size - 1 )) 

			{
				X2 = size - 1;
			}

			Real Z1 = Z-1;
			Real Z2 = Z+1;
			if ( Z1 < 0 ) 

			{
				Z1 = 0;
			}
			if ( Z2 > ( size - 1 )) 

			{
				Z2 = size - 1;
			}
			Vector3 v =  new Vector3(GetHeight(X1, Z) - GetHeight(X2,Z), 2, GetHeight(X, Z1) - GetHeight(X, Z2));
			v.Normalize();

			return v;
		}


		public override ColorEx GetBase (Real X, Real Z)
		{
			return ColorEx.White;
		}


		public override ColorEx GetCoverage (Real X, Real Z)
		{
			return ColorEx.White;
		}


		protected override void load(Real X, Real Z)
		{

			this.size = Options.Instance.PageSize;

			maxArrayPos = (long) (this.size * this.size);
            this.max = maxArrayPos * 4;
    
            heightData = generateHeightField(X, Z, this.size);

            return;
        }


		protected override void load()
		{
            throw new AxiomException("Error: load()");
            //load(0, 0);
		}


		protected override void load(Image NewHeightMap )
		{
            throw new AxiomException("Error: load(image)");
		}


		protected override void unload()
		{
			this.Dispose();
		}


	}
}
