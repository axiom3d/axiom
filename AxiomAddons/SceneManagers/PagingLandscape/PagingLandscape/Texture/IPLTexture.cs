#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Using Statements

using System;
using System.Collections;

using Axiom.Core;
using Axiom.MathLib;
using Axiom.Graphics;

using Axiom.SceneManagers.IPLSceneManager;
using Axiom.SceneManagers.IPLSceneManager.Page;

#endregion;

namespace Axiom.SceneManagers.IPLSceneManager.Texture
{
	/// <summary>
	/// A simple class for encapsulating Texture generation.
	/// </summary>
	public abstract class IPLTexture
	{
		#region Fields

		/// <summary>
		/// Flag to indicate if this texture is loaded
		/// </summary>
		protected bool isLoaded;

		
		/// <summary>
		/// Maeterial in use for this texture
		/// </summary>
		protected Material material;

		/// <summary>
		/// X and Z coordinates for this texture
		/// </summary>
		protected float dataX, dataZ;

		/// <summary>
		/// Creator of this Texture
		/// </summary>
		protected IPLPage page;
		
		#endregion Fields

		#region Constructor

		public IPLTexture( IPLPage creator )
		{
			page = creator;
			isLoaded = false;
			dataX = -1;
			dataZ = -1;
			// get default material
			material = (Material)(MaterialManager.Instance.GetByName("BaseWhite"));
		}

		#endregion Constructor
		//virtual ~IPLTexture( void );

		#region Methods

		public void Load(  float X,  float Z )
		{
			dataX = X;
			dataZ = Z;
			loadMaterial( );
			isLoaded = true;
		}

		public void Unload( )
		{
			isLoaded = false;
			unloadMaterial( );
		}

		
		/// <summary>
		/// This will create the template material so we can reuse it
		/// </summary>
		/// <param name="pageCount"></param>
		/// <param name="vw"></param>
		public abstract void setupMaterial( int pageCount, IPLVirtualPageWindow vw );

		#endregion Methods

		#region Propeties

		public bool IsLoaded
		{
			get
			{
				return isLoaded;
			}
		}

		Material Material
		{
			get
			{
				return material;
			}
		}

		#endregion Properties

		#region Operations

		protected abstract void loadMaterial( );
		protected abstract void unloadMaterial( );

		#endregion Operations


	}
}
