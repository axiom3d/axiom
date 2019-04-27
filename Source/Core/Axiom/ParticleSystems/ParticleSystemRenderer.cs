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
using Axiom.Graphics;
using Axiom.Core;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.ParticleSystems
{
    /// <summary>
    ///     Particle system renderer attribute method definition.
    /// </summary>
    /// <param name="values">Attribute values.</param>
    /// <param name="renderer">Target particle system renderer.</param>
    internal delegate void ParticleSystemRendererAttributeParser(string[] values, ParticleSystemRenderer renderer);

    public abstract class ParticleSystemRenderer : DisposableObject
    {
        /// Constructor
        public ParticleSystemRenderer()
        {
        }

        public ParticleSystemRenderer(string name)
        {
        }

        /// <summary>
        /// Gets the type of this renderer - must be implemented by subclasses
        /// </summary>
        public abstract string Type { get; }

        /// <summary>
        ///  Delegated to by ParticleSystem::UpdateRenderQueue
        /// </summary>
        /// <remarks>
        /// The subclass must update the render queue using whichever Renderable instance(s) it wishes.
        /// </remarks>
        public virtual void UpdateRenderQueue(RenderQueue queue, List<Particle> currentParticles, bool cullIndividually)
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
        public virtual void NotifyCurrentCamera(Camera cam)
        {
        }

        /// <summary>
        /// Delegated to by ParticleSystem.NotifyAttached
        /// </summary>
        public virtual void NotifyAttached(Node parent)
        {
            NotifyAttached(parent, false);
        }

        public virtual void NotifyAttached(Node parent, bool isTagPoint)
        {
        }

        /// <summary>
        /// Optional callback notified when particles are rotated
        /// </summary>
        public virtual void NotifyParticleRotated()
        {
        }

        /// <summary>
        /// Optional callback notified when particles are emitted
        /// </summary>
        public virtual void NotifyParticleEmitted(Particle particle)
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
        public virtual void NotifyParticleQuota(int quota)
        {
        }

        /// <summary>
        /// Optional callback notified when particles are moved
        /// </summary>
        /// <param name="activeParticles"></param>
        public virtual void NotifyParticleMoved(List<Particle> activeParticles)
        {
        }

        /// <summary>
        /// Optional callback notified when particles are moved
        /// </summary>
        public virtual void NotifyParticleExpired(Particle particle)
        {
        }

        /// <summary>
        /// Tells the renderer that the particle default size has changed
        /// </summary>
        public virtual void NotifyDefaultDimensions(float width, float height)
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
        public virtual void DestroyVisualData(ParticleVisualData vis)
        {
            /* assert (vis == 0); */
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

        public abstract void CopyParametersTo(ParticleSystemRenderer other);

        public abstract bool SetParameter(string attr, string val);

        /** Setting carried over from ParticleSystem.
		*/
        public abstract void SetKeepParticlesInLocalSpace(bool keepLocal);
    }

    public class ParticleSystemRendererFactory : AbstractFactory<ParticleSystemRenderer>
    {
        public override void DestroyInstance(ref ParticleSystemRenderer obj)
        {
            if (!obj.IsDisposed)
            {
                obj.Dispose();
            }

            //base.DestroyInstance(ref obj);
        }
    }
}