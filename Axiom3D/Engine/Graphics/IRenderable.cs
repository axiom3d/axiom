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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using DotNet3D.Math;
using DotNet3D.Math.Collections;

#endregion Namespace Declarations

namespace Axiom
{
    /// <summary>
    ///		Interface defining the interface all renderable objects must implement.
    /// </summary>
    /// <remarks>
    ///		This interface abstracts renderable discrete objects which will be queued in the render pipeline,
    ///		grouped by material. Classes implementing this interface must be based on a single material, a single
    ///		world matrix (or a collection of world matrices which are blended by weights), and must be 
    ///		renderable via a single render operation.
    ///		<para />
    ///		Note that deciding whether to put these objects in the rendering pipeline is done from the more specific
    ///		classes e.g. entities. Only once it is decided that the specific class is to be rendered is the abstract version
    ///		created (could be more than one per visible object) and pushed onto the rendering queue.
    /// </remarks>
    /// <ogre name="Renderable">
    ///     <file name="OgreRenderable.h" revison="1.27.2.1" lastUpdated="6/5/06" lastUpdatedBy="Lehuvyterz" />
    /// </ogre>
    public interface IRenderable
    {
        #region Properties

        /// <summary>
        ///		Gets whether this renderable would normally cast a shadow. 
        /// </summary>
        /// <ogre name="getCastsShadows" />
        bool CastsShadows
        {
            get;
        }

        /// <summary>
        ///    Get the material associated with this renderable object.
        /// </summary>
        /// <ogre name="getMaterial" />
        Material Material
        {
            get;
        }

        /// <summary>
        ///    Technique being used to render this object according to the current hardware.
        /// </summary>
        /// <remarks>
        ///    This is to allow Renderables to use a chosen Technique if they wish, otherwise
        ///    they will use the best Technique available for the Material they are using.
        /// </remarks>
        /// <ogre name="getTechnique" />
        Technique Technique
        {
            get;
        }

        /// <ogre name="getClipPlanes" />
        PlaneList ClipPlanes
        {
            get;
        }

        /// <summary>
        ///    Gets a list of lights, ordered relative to how close they are to this renderable.
        /// </summary>
        /// <remarks>
        ///    Directional lights, which have no position, will always be first on this list.
        /// </remarks>
        /// <ogre name="getLights" />
        LightList Lights
        {
            get;
        }

        /// <summary>
        ///    Returns whether or not this Renderable wishes the hardware to normalize normals.
        /// </summary>
        /// <ogre name="getNormaliseNormalse" />
        bool NormalizeNormals
        {
            get;
        }

        /// <summary>
        ///    Gets the number of world transformations that will be used for this object.
        /// </summary>
        /// <remarks>
        ///    When a renderable uses vertex blending, it uses multiple world matrices instead of a single
        ///    one. Each vertex sent to the pipeline can reference one or more matrices in this list
        ///    with given weights.
        ///    If a renderable does not use vertex blending this method returns 1, which is the default for 
        ///    simplicity.
        /// </remarks>
        /// <ogre name="getNumWorldTransforms" />
        int WorldTransformCount
        {
            get;
        }

        /// <summary>
        ///    Returns whether or not to use an 'identity' projection.
        /// </summary>
        /// <remarks>
        ///    Usually IRenderable objects will use a projection matrix as determined
        ///    by the active camera. However, if they want they can cancel this out
        ///    and use an identity projection, which effectively projects in 2D using
        ///    a {-1, 1} view space. Useful for overlay rendering. Normal renderables need
        ///    not override this.
        /// </remarks>
        /// <ogre name="useIdentityProjection" />
        bool UseIdentityProjection
        {
            get;
        }

        /// <summary>
        ///    Returns whether or not to use an 'identity' projection.
        /// </summary>
        /// <remarks>
        ///    Usually IRenderable objects will use a view matrix as determined
        ///    by the active camera. However, if they want they can cancel this out
        ///    and use an identity matrix, which means all geometry is assumed
        ///    to be relative to camera space already. Useful for overlay rendering. 
        ///    Normal renderables need not override this.
        /// </remarks>
        /// <ogre name="useIdentityView" />
        bool UseIdentityView
        {
            get;
        }

        /// <summary>
        ///		Will allow for setting per renderable scene detail levels.
        /// </summary>
        /// <ogre name="getRenderDetail" />
        SceneDetailLevel RenderDetail
        {
            get;
        }

        /// <summary>
        /// Sets whether this renderable's chosen detail level can be overridden (downgraded) by the _camera setting. 
        /// </summary>
        /// <ogre name="getRenderDetailOverrideable" />
        /// <ogre name="setRenderDetailoverrideable" />
        bool RenderDetailOverrideable
        {
            get;
            set;
        }

        /// <summary>
        ///    Gets the worldspace orientation of this renderable; this is used in order to
        ///    more efficiently update parameters to vertex & fragment programs, since inverting Quaterion
        ///    and Vector in order to derive object-space positions / directions for cameras and
        ///    lights is much more efficient than inverting a complete 4x4 matrix, and also 
        ///    eliminates problems introduced by scaling.
        /// </summary>
        /// <ogre name="getWorldOrientation" />
        Quaternion WorldOrientation
        {
            get;
        }

        /// <summary>
        ///    Gets the worldspace position of this renderable; this is used in order to
        ///    more efficiently update parameters to vertex & fragment programs, since inverting Quaterion
        ///    and Vector in order to derive object-space positions / directions for cameras and
        ///    lights is much more efficient than inverting a complete 4x4 matrix, and also 
        ///    eliminates problems introduced by scaling.
        /// </summary>
        /// <ogre name="getWorldPosition" />
        Vector3 WorldPosition
        {
            get;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///		Gets the render operation required to send this object to the frame buffer.
        /// </summary>
        /// <param name="op">The RenderOperation to populate.</param>
        /// <ogre name="getRenderOperation" />
        void GetRenderOperation( RenderOperation op );

        /// <summary>
        ///    Gets the world transform matrix / matrices for this renderable object.
        /// </summary>
        /// <remarks>
        ///    If the object has any derived transforms, these are expected to be up to date as long as
        ///    all the SceneNode structures have been updated before this is called.
        ///  <p/>
        ///    This will populate the parameter array with 1 matrix if it does not use vertex blending or otherwise with WorldTransformsCount matrices.
        /// </remarks>
        /// <ogre name="getWorldTransforms" />
        void GetWorldTransforms( Matrix4[] matrices );

        /// <summary>
        ///		Returns the camera-relative squared depth of this renderable.
        /// </summary>
        /// <remarks>
        ///		Used to sort transparent objects. Squared depth is used rather than
        ///		actual depth to avoid having to perform a square root on the result.	
        /// </remarks>
        /// <param name="camera"></param>
        /// <returns></returns>
        /// <ogre name="getSquaredViewDepth" />
        Real GetSquaredViewDepth( Camera camera );

        /// <summary>
        ///		Gets the custom value associated with this Renderable at the given index. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <ogre name="setCustomParameter" />
        Vector4 GetCustomParameter( int index );

        /// <summary>
        ///		Sets a custom parameter for this Renderable, which may be used to 
        ///		drive calculations for this specific Renderable, like GPU program parameters.
        /// </summary>
        /// <remarks>
        ///		Calling this method simply associates a numeric index with a 4-dimensional
        ///		value for this specific Renderable. This is most useful if the material
        ///		which this Renderable uses a vertex or fragment program, and has an 
        ///		AutoConstant.Custom parameter entry. This parameter entry can refer to the
        ///		index you specify as part of this call, thereby mapping a custom
        ///		parameter for this renderable to a program parameter.
        /// </remarks>
        /// <param name="index">
        ///		The index with which to associate the value. Note that this
        ///		does not have to start at 0, and can include gaps. It also has no direct
        ///		correlation with a GPU program parameter index - the mapping between the
        ///		two is performed by the AutoConstant.Custom entry, if that is used.
        /// </param>
        /// <param name="val">The value to associate.</param>
        /// <ogre name="setCustomparameter" />
        void SetCustomParameter( int index, Vector4 val );

        /// <summary>
        ///		Update a custom GpuProgramParameters constant which is derived from 
        ///		information only this Renderable knows.
        /// </summary>
        /// <remarks>
        ///		This method allows a Renderable to map in a custom GPU program parameter
        ///		based on it's own data. This is represented by a GPU auto parameter
        ///		of AutoConstants.Custom, and to allow there to be more than one of these per
        ///		Renderable, the 'data' field on the auto parameter will identify
        ///		which parameter is being updated. The implementation of this method
        ///		must identify the parameter being updated, and call a 'SetConstant' 
        ///		method on the passed in <see cref="GpuProgramParameters"/> object, using the details
        ///		provided in the incoming auto constant setting to identify the index
        ///		at which to set the parameter.
        /// </remarks>
        /// <param name="constant">The auto constant entry referring to the parameter being updated.</param>
        /// <param name="parameters">The parameters object which this method should call to set the updated parameters.</param>
        /// <ogre name="_updateCustomGpuParameter" />
        void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry constant, GpuProgramParameters parameters );

        #endregion
    }
}
