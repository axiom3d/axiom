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

using System;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.MathLib;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	///		Interface defining the interface all renderable objects must implement.
	/// </summary>
	/// <remarks>
	///		This interface abstracts renderable discrete objects which will be queued in the render pipeline,
	///		grouped by material. Classes implementing this interface must be based on a single material, a single
	///		world matrix (or a collection of world matrices which are blended by weights), and must be 
	///		renderable via a single render operation.
	///		<p/>
	///		Note that deciding whether to put these objects in the rendering pipeline is done from the more specific
	///		classes e.g. entities. Only once it is decided that the specific class is to be rendered is the abstract version
	///		created (could be more than one per visible object) and pushed onto the rendering queue.
	/// </remarks>
	public interface IRenderable
	{	
		#region Properties

		/// <summary>
		/// Get the material associated with this renderable object.
		/// </summary>
		Material Material
		{
			get;
		}

		/// <summary>
		/// Get the current render operation associated with this renderable object.
		/// </summary>
		void GetRenderOperation(RenderOperation op);

		/// <summary>
		/// Gets the world transform matrix / matrices for this renderable object.
		/// </summary>
		/// <remarks>
		///  If the object has any derived transforms, these are expected to be up to date as long as
		///  all the SceneNode structures have been updated before this is called.
		///  
		///  This method will populate xform with 1 matrix if it does not use vertex blending. If it
		///  does use vertex blending it will fill the passed in pointer with an array of matrices,
		///  the length being the value returned from getNumWorldTransforms.
		/// </remarks>
		Matrix4[] WorldTransforms
		{
			get;
		}

		/// <summary>
		/// Gets the number of world transformations that will be used for this object.
		/// </summary>
		/// <remarks>
		/// When a renderable uses vertex blending, it uses multiple world matrices instead of a single
		/// one. Each vertex sent to the pipeline can reference one or more matrices in this list
		/// with given weights.
		/// If a renderable does not use vertex blending this method returns 1, which is the default for 
		/// simplicity.
		/// </remarks>

		ushort NumWorldTransforms
		{
			get;
		}

		/// <summary>
		/// Returns whether or not to use an 'identity' projection.
		/// </summary>
		/// <remarks>
		/// Usually IRenderable objects will use a projection matrix as determined
		/// by the active camera. However, if they want they can cancel this out
		/// and use an identity projection, which effectively projects in 2D using
		/// a {-1, 1} view space. Useful for overlay rendering. Normal renderables need
		/// not override this.
		/// </remarks>
		bool UseIdentityProjection
		{
			get;
		}

		/// <summary>
		/// Returns whether or not to use an 'identity' projection.
		/// </summary>
		/// <remarks>
		/// Usually IRenderable objects will use a view matrix as determined
		/// by the active camera. However, if they want they can cancel this out
		/// and use an identity matrix, which means all geometry is assumed
		/// to be relative to camera space already. Useful for overlay rendering. 
		/// Normal renderables need not override this.
		/// </remarks>
		bool UseIdentityView
		{
			get;
		}

		/// <summary>
		///		Will allow for setting per renderable scene detail levels.
		/// </summary>
		SceneDetailLevel RenderDetail
		{
			get;
		}

		#endregion

		#region Public Methods

		/// <summary>
		///		Returns the camera-relative squared depth of this renderable.
		/// </summary>
		/// <remarks>
		///		Used to sort transparent objects. Squared depth is used rather than
		///		actual depth to avoid having to perform a square root on the result.	
		/// </remarks>
		/// <param name="camera"></param>
		/// <returns></returns>
		float GetSquaredViewDepth(Camera camera);

		#endregion
	}
}
