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
    public class ParticleSystemRenderer
    {
        /// Constructor
        public ParticleSystemRenderer() {}
        public ParticleSystemRenderer( string name )
        {
        }

		/// <summary>
		///  Delegated to by ParticleSystem::UpdateRenderQueue
		/// </summary>
        /// <remarks>
        /// The subclass must update the render queue using whichever Renderable instance(s) it wishes.
        /// </remarks>
        public virtual void UpdateRenderQueue( RenderQueue queue, ParticleList currentParticles, bool cullIndividually )
        {
        }

        /// <summary>
        /// Sets the material this renderer must use; called by ParticleSystem.
        /// </summary>
        public virtual Material Material
        {
            set
            {
            }
        }

        /// <summary>
        /// Delegated to by ParticleSystem.NotifyCurrentCamera
        /// </summary>
        /// <param name="cam"></param>
        public virtual void NotifyCurrentCamera( Camera cam )
        {
        }

        /// <summary>
        /// Delegated to by ParticleSystem.NotifyAttached
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="isTagPoint"></param>
        public virtual void NotifyAttached( Node parent )
        {
            NotifyAttached( parent, false );
        }

        public virtual void NotifyAttached( Node parent, bool isTagPoint )
        {
        }

        /// <summary>
        /// Optional callback notified when particles are rotated
        /// </summary>
        public virtual void NotifyParticleRotated()
        {
        }
        
        /// <summary>
        /// Optional callback notified when particles are resized individually
        /// </summary>
        public virtual void NotifyParticleResized()
        {
        }

        /// <summary>
        /// Tells the renderer that the particle quota has changed 
        /// </summary>
        /// <param name="quota"></param>
        public virtual void NotifyParticleQuota( int quota )
        {
        }

        /// <summary>
        /// Tells the renderer that the particle default size has changed
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public virtual void NotifyDefaultDimensions( float width, float height )
        {
        }

		/// <summary>
		/// Create a new ParticleVisualData instance for attachment to a particle.
		/// </summary>
        /// <remarks>
		///	If this renderer needs additional data in each particle, then this should
		///	be held in an instance of a subclass of ParticleVisualData, and this method
		///	should be overridden to return a new instance of it. The default
		///	behaviour is to return null.
        /// </remarks>
        public virtual ParticleVisualData CreateVisualData()
        {
            return null;
        }

		/// <summary>
		///  Destroy a ParticleVisualData instance.
		/// </summary>
        /// <remarks>
        /// If this renderer needs additional data in each particle, then this should
        /// be held in an instance of a subclass of ParticleVisualData, and this method
        /// should be overridden to destroy an instance of it. The default
        /// behaviour is to do nothing.
        /// </remarks>
        public virtual void DestroyVisualData( ParticleVisualData vis )
        { /* assert (vis == 0); */
        }

		/// <summary>
		/// Sets which render queue group this renderer should target with it's output.
		/// </summary>
        public virtual RenderQueueGroupID RenderQueueGroup
        {
            set
            {
            }
        }

    }
}
