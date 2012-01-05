#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
//     <id value="$Id: ParticleSystemManager.cs 884 2006-09-14 06:32:07Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;

using Axiom.Core;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.ParticleSystems
{
	/// <summary>
	///     Particle system renderer attribute method definition.
	/// </summary>
	/// <param name="values">Attribute values.</param>
	/// <param name="renderer">Target particle system renderer.</param>
	internal delegate void ParticleSystemRendererAttributeParser( string[] values, ParticleSystemRenderer renderer );

	abstract public class ParticleSystemRenderer : DisposableObject
	{
		/// Constructor
		public ParticleSystemRenderer() {}

		/// <summary>
		/// Gets the type of this renderer - must be implemented by subclasses
		/// </summary>
		abstract public string Type { get; }

		/// <summary>
		///  Delegated to by ParticleSystem::UpdateRenderQueue
		/// </summary>
		/// <remarks>
		/// The subclass must update the render queue using whichever Renderable instance(s) it wishes.
		/// </remarks>
		virtual public void UpdateRenderQueue( RenderQueue queue, List<Particle> currentParticles, bool cullIndividually ) {}

		/// <summary>
		/// Sets the material this renderer must use; called by ParticleSystem.
		/// </summary>
		virtual public Material Material { set { } }

		/// <summary>
		/// Delegated to by ParticleSystem.NotifyCurrentCamera
		/// </summary>
		/// <param name="cam"></param>
		virtual public void NotifyCurrentCamera( Camera cam ) {}

		/// <summary>
		/// Delegated to by ParticleSystem.NotifyAttached
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="isTagPoint"></param>
		virtual public void NotifyAttached( Node parent )
		{
			NotifyAttached( parent, false );
		}

		virtual public void NotifyAttached( Node parent, bool isTagPoint ) {}

		/// <summary>
		/// Optional callback notified when particles are rotated
		/// </summary>
		virtual public void NotifyParticleRotated() {}

		/// <summary>
		/// Optional callback notified when particles are emitted
		/// </summary>
		virtual public void NotifyParticleEmitted( Particle particle ) {}

		/// <summary>
		/// Optional callback notified when particles are resized individually
		/// </summary>
		virtual public void NotifyParticleResized() {}

		/// <summary>
		/// Tells the renderer that the particle quota has changed 
		/// </summary>
		/// <param name="quota"></param>
		virtual public void NotifyParticleQuota( int quota ) {}

		/// <summary>
		/// Optional callback notified when particles are moved
		/// </summary>
		/// <param name="activeParticles"></param>
		virtual public void NotifyParticleMoved( List<Particle> activeParticles ) {}

		/// <summary>
		/// Optional callback notified when particles are moved
		/// </summary>
		/// <param name="activeParticles"></param>
		virtual public void NotifyParticleExpired( Particle particle ) {}

		/// <summary>
		/// Tells the renderer that the particle default size has changed
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		virtual public void NotifyDefaultDimensions( float width, float height ) {}

		/// <summary>
		/// Create a new ParticleVisualData instance for attachment to a particle.
		/// </summary>
		/// <remarks>
		///	If this renderer needs additional data in each particle, then this should
		///	be held in an instance of a subclass of ParticleVisualData, and this method
		///	should be overridden to return a new instance of it. The default
		///	behaviour is to return null.
		/// </remarks>
		virtual public ParticleVisualData CreateVisualData()
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
		virtual public void DestroyVisualData( ParticleVisualData vis )
		{
			/* assert (vis == 0); */
		}

		/// <summary>
		/// Sets which render queue group this renderer should target with it's output.
		/// </summary>
		virtual public RenderQueueGroupID RenderQueueGroup { set { } }

		abstract public void CopyParametersTo( ParticleSystemRenderer other );

		abstract public bool SetParameter( string attr, string val );

		/** Setting carried over from ParticleSystem.
		*/

		abstract public void SetKeepParticlesInLocalSpace( bool keepLocal );
	}

	public class ParticleSystemRendererFactory : AbstractFactory<ParticleSystemRenderer> {}
}
