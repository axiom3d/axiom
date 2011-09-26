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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using Axiom.Controllers;
using Axiom.Graphics;
using System.Reflection;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.ParticleSystems
{

	/// <summary>
	///     Particle system attribute method definition.
	/// </summary>
	/// <param name="values">Attribute values.</param>
	/// <param name="system">Target particle system.</param>
	delegate void ParticleSystemAttributeParser( string[] values, ParticleSystem system );

	/// <summary>
	///		Class defining particle system based special effects.
	/// </summary>
	/// <remarks>
	///     Particle systems are special effects generators which are based on a 
	///     number of moving points to create the impression of things like like 
	///     sparkles, smoke, blood spurts, dust etc.
	///		<p/>
	///		This class simply manages a single collection of particles in world space
	///     with a shared local origin for emission. The visual aspect of the 
	///     particles is handled by a ParticleSystemRenderer instance.
	///		<p/>
	///     Particle systems are created using the SceneManager, never directly.
	///     In addition, like all subclasses of MovableObject, the ParticleSystem 
	/// 	will only be considered for rendering once it has been attached to a 
	/// 	SceneNode. 
	/// </remarks>
	public class ParticleSystem : MovableObject
	{
		#region Fields and Properties

		const string PARTICLE = "Particle";

		/// <summary>List of emitters for this system.</summary>
		protected List<ParticleEmitter> emitterList = new List<ParticleEmitter>();
		public List<ParticleEmitter> Emitters
		{
			get
			{
				return emitterList;
			}
		}
		/// <summary>List of affectors for this system.</summary>
		protected List<ParticleAffector> affectorList = new List<ParticleAffector>();
		public List<ParticleAffector> Affectors
		{
			get
			{
				return affectorList;
			}
		}
		/// <summary>Cached for less memory usage during emitter processing.</summary>
		/// <note>EmitterList is a list of _counts_, not a list of emitters</note>
		protected Dictionary<ParticleEmitter, int> requested = new Dictionary<ParticleEmitter, int>();

		/** Pool of emitted emitters for use and reuse in the active emitted emitter list.
		@remarks
			The emitters in this pool act as particles and as emitters. The pool is a map containing lists 
			of emitters, identified by their name.
		@par
			The emitters in this pool are cloned using emitters that are kept in the main emitter list
			of the ParticleSystem.
		*/
		protected Dictionary<string, List<ParticleEmitter>> emittedEmitterPool = new Dictionary<string, List<ParticleEmitter>>();

		/** Free emitted emitter list.
			@remarks
				This contains a list of the emitters free for use as new instances as required by the set.
		*/
		protected Dictionary<string, List<ParticleEmitter>> freeEmittedEmitters = new Dictionary<string, List<ParticleEmitter>>();

		/** Active emitted emitter list.
			@remarks
				This is a linked list of pointers to emitters in the emitted emitter pool.
				Emitters that are used are stored (their pointers) in both the list with active particles and in 
				the list with active emitted emitters.        */
		protected List<ParticleEmitter> activeEmittedEmitters = new List<ParticleEmitter>();

		protected bool emittedEmitterPoolInitialized;
		protected int emittedEmitterPoolSize = 3;

		/// World AABB, only used to compare world-space positions to calc bounds
		protected AxisAlignedBox aab;
		protected float boundingRadius;
		protected bool boundsAutoUpdate = true;
		protected float boundsUpdateTime = 10.0f;
		protected float updateRemainTime = 0.0f;

		/// Name of the resource group to use to load materials
		protected string resourceGroupName;
		/// Name of the material to use
		protected string materialName = "BaseWhite";
		/// Have we set the material etc on the renderer?
		protected bool isRendererConfigured = false;
		/// Pointer to the material to use
		protected Material material;
		/// Default width of each particle
		protected float defaultWidth;
		/// Default height of each particle
		protected float defaultHeight;
		/// Speed factor
		protected float speedFactor = 1.0f;
		/// Iteration interval
		protected float iterationInterval = 0.0f;
		/// Iteration interval set? Otherwise track default
		protected bool iterationIntervalSet = false;
		/// Particles sorted according to camera?
		protected bool sorted = false;
		/// Particles in local space?
		protected bool localSpace = false;
		/// Update timeout when nonvisible (0 for no timeout)
		protected float nonvisibleTimeout = 0.0f;
		/// Update timeout when nonvisible set? Otherwise track default
		protected bool nonvisibleTimeoutSet = false;
		/// Amount of time non-visible so far
		protected float timeSinceLastVisible = 0;
		/// Last frame in which known to be visible
		protected ulong lastVisibleFrame = 0;
		/// Controller for time update
		protected Controller<float> timeController = null;

		// various collections for pooling billboards
		protected List<Particle> freeParticles = new List<Particle>();
		protected List<Particle> activeParticles = new List<Particle>();
		protected List<Particle> particlePool = new List<Particle>();

		/// The renderer used to render this particle system
		protected ParticleSystemRenderer renderer;

		/// Do we cull each particle individually?
		protected bool cullIndividual = false;

		/// The name of the type of renderer used to render this system
		protected string rendererType;

		/// The number of particles in the pool.
		protected int poolSize = 0;

		/// Optional origin of this particle system (eg script name)
		protected string origin;

		/// Default iteration interval
		protected static float defaultIterationInterval;
		/// Default nonvisible update timeout
		protected static float defaultNonvisibleTimeout;

		/// <summary>
		///     List of available attibute parsers for script attributes.
		/// </summary>
		private Dictionary<string, MethodInfo> attribParsers = new Dictionary<string, MethodInfo>();

		#endregion

		#region Constructors
		/// <summary>
		///		Creates a particle system with no emitters or affectors.
		/// </summary>
		/// <remarks>
		///		You should use the ParticleSystemManager to create systems, rather than doing it directly.
		/// </remarks>
		/// <param name="name"></param>
		internal ParticleSystem( string name )
			: this( name, ResourceGroupManager.DefaultResourceGroupName )
		{

		}

		/// <summary>
		///		Creates a particle system with no emitters or affectors.
		/// </summary>
		/// <remarks>
		///		You should use the ParticleSystemManager to create systems, rather than doing it directly.
		/// </remarks>
		internal ParticleSystem( string name, string resourceGroup )
			: base( name )
		{
			InitParameters();
			aab = new AxisAlignedBox();
			boundingRadius = 1.0f;
			resourceGroupName = resourceGroup;
			RendererName = "billboard";

			RegisterParsers();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposeManagedResources"></param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( timeController != null )
					{
						// timeController.Dispose();
						timeController = null;
					}
					RemoveAllEmitters();
					RemoveAllAffectors();

					DestroyVisualParticles( 0, particlePool.Count );
					
                    if ( renderer != null )
					{
                        if (!renderer.IsDisposed)
                            renderer.Dispose();

						renderer = null;
					}
				}
			}

            base.dispose(disposeManagedResources);
		}

		/// <summary>
		///		Adds an emitter to this particle system.
		///	 </summary>
		///	 <remarks>	
		///		Particles are created in a particle system by emitters - see the ParticleEmitter
		///		class for more details.
		/// </remarks>
		/// <param name="emitterType">
		///		string identifying the emitter type to create. Emitter types are defined
		///		by registering new factories with the manager - see ParticleEmitterFactory for more details.
		///		Emitter types can be extended by plugin authors.
		/// </param>
		/// <returns></returns>
		public ParticleEmitter AddEmitter( string emitterType )
		{
			var emitter = ParticleSystemManager.Instance.CreateEmitter( emitterType, this );
			emitterList.Add( emitter );
			return emitter;
		}

		public void RemoveEmitter( int index )
		{
			Debug.Assert( index < emitterList.Count, "Emitter index out of bounds!" );
			var emitter = emitterList[ index ];
			// ParticleSystemManager.Instance.DestroyEmitter(emitter);
			emitterList.RemoveAt( index );
		}

		public void RemoveAllEmitters()
		{
            //foreach (ParticleEmitter emitter in emitterList)
            //    ParticleSystemManager.Instance.DestroyEmitter(emitter);
			emitterList.Clear();
		}

		/// <summary>
		///		Adds an affector to this particle system.
		///	 </summary>
		///	 <remarks>	
		///		Particles are modified over time in a particle system by affectors - see the ParticleAffector
		///		class for more details.
		/// </remarks>
        /// <param name="affectorType">
		///		string identifying the affector type to create. Affector types are defined
		///		by registering new factories with the manager - see ParticleAffectorFactory for more details.
		///		Affector types can be extended by plugin authors.
		/// </param>
		/// <returns></returns>
		public ParticleAffector AddAffector( string affectorType )
		{
			var affector = ParticleSystemManager.Instance.CreateAffector( affectorType );
			affectorList.Add( affector );
			return affector;
		}

		public void RemoveAffector( int index )
		{
			Debug.Assert( index < affectorList.Count, "Affector index out of bounds!" );
			var affector = affectorList[ index ];
			// ParticleSystemManager.Instance.DestroyAffector(affector);
			affectorList.RemoveAt( index );
		}

		public void RemoveAllAffectors()
		{
			// foreach (ParticleAffector affector in AffectorList)
			//      ParticleSystemManager.Instance.DestroyEmitter(affector);
			affectorList.Clear();
		}

		/// <summary>
		///    Get a particle affector assigned to this particle system by index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ParticleAffector GetAffector( int index )
		{
			Debug.Assert( index < affectorList.Count, "index < affectorList.Count" );
			return affectorList[ index ];
		}

		/// <summary>
		///    Get a particle emitter assigned to this particle system by index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ParticleEmitter GetEmitter( int index )
		{
			Debug.Assert( index < emitterList.Count, "index < emitterList.Count" );

			return emitterList[ index ];
		}

		#endregion

		#region Methods



		/// <summary>
		///		Used to expire dead particles.
		/// </summary>
		/// <param name="timeElapsed"></param>
		protected void Expire( float timeElapsed )
		{
			var expiredList = new List<Particle>();
			for ( var index = 0; index < activeParticles.Count; index++ )
			//foreach ( Particle particle in activeParticles )
			{
				var particle = activeParticles[ index ];
				// is this particle dead?
				if ( particle.timeToLive < timeElapsed )
				{
					// notify renderer
					renderer.NotifyParticleExpired( particle );

					// Identify the particle type
					if ( particle.ParticleType == ParticleType.Emitter )
					{
						// For now, it can only be an emitted emitter
						var pParticleEmitter = (ParticleEmitter)particle;
						var fee = findFreeEmittedEmitter( pParticleEmitter.Name );
						fee.Add( pParticleEmitter );

						// Also erase from activeEmittedEmitters
						removeFromActiveEmittedEmitters( pParticleEmitter );

					}
					else
					{
						// add back to the free queue 
						freeParticles.Add( particle );
					}
					//and remove from active list
					expiredList.Add( particle );
				}
				else
				{
					// decrement TTL
					particle.timeToLive -= timeElapsed;
				}
			}
			for ( var index = 0; index < expiredList.Count; index++ )
			//foreach ( Particle p in expiredList )
			{
				var p = expiredList[ index ];
				activeParticles.Remove( p );
			}
		}

		private void removeFromActiveEmittedEmitters( ParticleEmitter pParticleEmitter )
		{
			if ( activeEmittedEmitters.Contains( pParticleEmitter ) )
			{
				activeEmittedEmitters.Remove( pParticleEmitter );
			}
		}

		/// <summary>
		///		Spawn new particles based on free quota and emitter requirements.
		/// </summary>
		/// <param name="timeElapsed"></param>
		protected void TriggerEmitters( float timeElapsed )
		{
			int index;
			int totalRequested, emissionAllowed;

			emissionAllowed = freeParticles.Count;
			totalRequested = 0;

			// Count up total requested emissions
			//foreach ( ParticleEmitter emitter in emitterList )
			for ( index = 0; index < emitterList.Count; index++ )
			{
				var emitter = emitterList[ index ];

				if ( !requested.ContainsKey( emitter ) )
				{
					requested.Add( emitter, 0 );
				}
				if ( !emitter.IsEmitted )
				{
					requested[ emitter ] = emitter.GetEmissionCount( timeElapsed );
					totalRequested += requested[ emitter ];
				}
			}

			//foreach ( ParticleEmitter activeEmitter in activeEmittedEmitters )
			for ( index = 0; index < activeEmittedEmitters.Count; index++ )
			{
				var activeEmitter = activeEmittedEmitters[ index ];
				if ( !requested.ContainsKey( activeEmitter ) )
				{
					requested.Add( activeEmitter, 0 );
				}
				totalRequested += requested[ activeEmitter ];
			}
			var ratio = 1.0f;
			// Check if the quota will be exceeded, if so reduce demand
			if ( totalRequested > emissionAllowed )
			{
				// Apportion down requested values to allotted values
				ratio = (float)emissionAllowed / (float)totalRequested;
				foreach ( var emitter in emitterList )
				{
					requested[ emitter ] = (int)( requested[ emitter ] * ratio );
				}
			}

			// Emit
			// For each emission, apply a subset of the motion for the frame
			// this ensures an even distribution of particles when many are
			// emitted in a single frame
			//foreach ( ParticleEmitter emitter in emitterList )
			for ( index = 0; index < emitterList.Count; index++ )
			{
				var emitter = emitterList[ index ];
				// Trigger the emitters, but exclude the emitters that are already in the emitted emitters list; 
				// they are handled in a separate loop
				if ( !emitter.IsEmitted )
				{
					executeTriggerEmitters( emitter, requested[ emitter ], timeElapsed );
				}
			}

			//foreach ( ParticleEmitter activeEmitter in activeEmittedEmitters )
			for ( index = 0; index < activeEmittedEmitters.Count; index++ )
			{
				var activeEmitter = activeEmittedEmitters[ index ];
				executeTriggerEmitters( activeEmitter, (int)( (float)activeEmitter.GetEmissionCount( timeElapsed ) * ratio ), timeElapsed );
			}
		}

		private void executeTriggerEmitters( ParticleEmitter emitter, int requested, float timeElapsed )
		{
			var timePoint = 0.0f;
			var timeInc = timeElapsed / requested;

			for ( var j = 0; j < requested; ++j )
			{
				// Create a new particle & init using emitter
				Particle p = null;
				var emitterName = emitter.EmittedEmitter;
				if ( emitterName == string.Empty )
				{
					p = CreateParticle();
				}
				else
				{
					p = CreateEmitterParticle( emitterName );
				}

				if ( p == null )
					return;

				emitter.InitParticle( p );

				if ( !localSpace )
				{
					p.Position = ( parentNode.DerivedOrientation * ( parentNode.DerivedScale * p.Position ) ) + parentNode.DerivedPosition;
					p.Direction = ( parentNode.DerivedOrientation * p.Direction );
				}

				// apply partial frame motion to this particle
				p.Position += ( p.Direction * timePoint );

				// apply particle initialization by the affectors
				foreach ( var affector in affectorList )
					affector.InitParticle( ref p );

				// Increment time fragment
				timePoint += timeInc;

				if ( p.ParticleType == ParticleType.Emitter )
				{
					// If the particle is an emitter, the position on the emitter side must also be initialised
					// Note, that position of the emitter becomes a position in worldspace if mLocalSpace is set 
					// to false (will this become a problem?)
					var pParticleEmitter = (ParticleEmitter)p;
					pParticleEmitter.Position = p.Position;
				}

				// Notify renderer
				renderer.NotifyParticleEmitted( p );

			}
		}



		/// <summary>
		///		Updates existing particles based on their momentum.
		/// </summary>
		/// <param name="timeElapsed"></param>
		protected void ApplyMotion( float timeElapsed )
		{
			foreach ( var p in activeParticles )
			{
				p.Position += p.Direction * timeElapsed;

				if ( p.ParticleType == ParticleType.Emitter )
				{
					// If the particle is an emitter, the position on the emitter side must also be initialised
					// Note, that position of the emitter becomes a position in worldspace if mLocalSpace is set 
					// to false (will this become a problem?)
					var pParticleEmitter = (ParticleEmitter)p;
					pParticleEmitter.Position = p.Position;
				}

				// notify renderer
				renderer.NotifyParticleMoved( activeParticles );
			}
		}

		/// <summary>
		///		Applies the effects of particle affectors.
		/// </summary>
		/// <param name="timeElapsed"></param>
		protected void TriggerAffectors( float timeElapsed )
		{
			foreach ( var affector in affectorList )
				affector.AffectParticles( this, timeElapsed );
		}

		/// <summary>
		///		Overriden from BillboardSet to create Particles instead of Billboards.
		///	 </summary>
		/// <param name="size"></param>
		protected void IncreasePool( int size )
		{
			var oldSize = particlePool.Count;

			// expand the capacity a bit
			particlePool.Capacity = size;

			// add fresh Billboard objects to the new slots
			for ( var i = oldSize; i < size; i++ )
				particlePool.Add( new Particle() );

			if ( isRendererConfigured )
				CreateVisualParticles( oldSize, size );
		}

		public Particle GetParticle( int index )
		{
			return activeParticles[ index ];
		}

		private Particle CreateParticle()
		{
			Particle newParticle = null;
			if ( freeParticles.Count != 0 )
			{
				// Fast creation (don't use superclass since emitter will init)
				newParticle = freeParticles[ 0 ];
				freeParticles.RemoveAt( 0 );

				// add the billboard to the active list
				activeParticles.Add( newParticle );

				newParticle.NotifyOwner( this );
			}
			return newParticle;
		}

		private Particle CreateEmitterParticle( string emitterName )
		{
			// Get the appropriate list and retrieve an emitter	
			Particle p = null;
			var fee = findFreeEmittedEmitter( emitterName );
			if ( fee != null && fee.Count != 0 )
			{
				p = fee[ 0 ];
				p.ParticleType = ParticleType.Emitter;
				fee.RemoveAt( 0 );
				activeParticles.Add( p );

				// Also add to mActiveEmittedEmitters. This is needed to traverse through all active emitters
				// that are emitted. Don't use mActiveParticles for that (although they are added to
				// mActiveParticles also), because it would take too long to traverse.
				activeEmittedEmitters.Add( (ParticleEmitter)p );

				p.NotifyOwner( this );
			}

			return p;
		}

		private List<ParticleEmitter> findFreeEmittedEmitter( string name )
		{
			if ( freeEmittedEmitters.ContainsKey( name ) )
			{
				return freeEmittedEmitters[ name ];
			}

			return null;
		}


		public override void UpdateRenderQueue( RenderQueue queue )
		{
			if ( renderer != null )
				renderer.UpdateRenderQueue( queue, activeParticles, cullIndividual );
		}

		protected void InitParameters()
		{
			// FIXME
			// throw new NotImplementedException();
		}

		protected void UpdateBounds()
		{
			if ( parentNode != null && ( boundsAutoUpdate || boundsUpdateTime > 0.0f ) )
			{
				Vector3 min;
				Vector3 max;
				if ( !boundsAutoUpdate )
				{
					// We're on a limit, grow rather than reset each time
					// so that we pick up the worst case scenario
					min = worldAABB.Minimum;
					max = worldAABB.Maximum;
				}
				else
				{
					min.x = min.y = min.z = float.PositiveInfinity;
					max.x = max.y = max.z = float.NegativeInfinity;
				}
				var halfScale = Vector3.UnitScale * 0.5f;
				var defaultPadding =
					halfScale * (float)Utility.Max( defaultHeight, defaultWidth );
				foreach ( var p in activeParticles )
				{
					if ( p.HasOwnDimensions )
					{
						var padding =
							halfScale * (float)Utility.Max( p.Width, p.Height );
						min.Floor( p.Position - padding );
						max.Ceil( p.Position + padding );
					}
					else
					{
						min.Floor( p.Position - defaultPadding );
						max.Ceil( p.Position + defaultPadding );
					}
				}
				worldAABB.SetExtents( min, max );


				if ( localSpace )
				{
					// Merge calculated box with current AABB to preserve any user-set AABB
					aab.Merge( worldAABB );
				}
				else
				{
					// We've already put particles in world space to decouple them from the
					// node transform, so reverse transform back since we're expected to 
					// provide a local AABB
					var newAABB = (AxisAlignedBox)worldAABB.Clone();
					newAABB.Transform( parentNode.FullTransform.Inverse() );

					// Merge calculated box with current AABB to preserve any user-set AABB
					aab.Merge( newAABB );
				}

				parentNode.NeedUpdate();
			}
		}


		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="time"></param>
		public void FastForward( float time )
		{
			FastForward( time, 0.1f );
		}

		/// <summary>
		///		Fast-forwards this system by the required number of seconds.
		///	 </summary>
		///	 <remarks>
		///		This method allows you to fast-forward a system so that it effectively looks like
		///		it has already been running for the time you specify. This is useful to avoid the
		///		'startup sequence' of a system, when you want the system to be fully populated right
		///		from the start.
		/// </remarks>
		/// <param name="time">The number of seconds to fast-forward by.</param>
		/// <param name="interval">
		///		The sampling interval used to generate particles, apply affectors etc. The lower this
		///		is the more realistic the fast-forward, but it takes more iterations to do it.
		/// </param>
		public void FastForward( float time, float interval )
		{
			for ( var t = 0.0f; t < time; t += interval )
			{
				Update( interval );
			}
		}

		public void NotifyParticleResized()
		{
			if ( renderer != null )
				renderer.NotifyParticleResized();
		}

		public void NotifyParticleRotated()
		{
			if ( renderer != null )
				renderer.NotifyParticleRotated();
		}

		protected void SetDefaultDimensions( float width, float height )
		{
			defaultWidth = width;
			defaultHeight = height;
			if ( renderer != null )
				renderer.NotifyDefaultDimensions( width, height );
		}

		public override void NotifyCurrentCamera( Camera cam )
		{
			// base.NotifyCurrentCamera(cam);

			// Record visible
			lastVisibleFrame = Root.Instance.CurrentFrameCount;
			timeSinceLastVisible = 0.0f;

			// TODO: Should I support sorting?
			//if (sorted)
			//{
			//    SortParticles(cam);
			//}

			if ( renderer != null )
			{
				if ( !isRendererConfigured )
					ConfigureRenderer();
				renderer.NotifyCurrentCamera( cam );
			}
		}

		internal override void NotifyAttached( Node parent, bool isTagPoint )
		{
			base.NotifyAttached( parent, isTagPoint );
			if ( renderer != null && isRendererConfigured )
			{
				renderer.NotifyAttached( parent, isTagPoint );
			}

			if ( parent != null && timeController == null )
			{
				// Assume visible
				timeSinceLastVisible = 0;
				lastVisibleFrame = Root.Instance.CurrentFrameCount;

				// Create time controller when attached
				var mgr = ControllerManager.Instance;
				IControllerValue<float> updValue = new ParticleSystemUpdateValue( this );
				timeController = ControllerManager.Instance.CreateFrameTimePassthroughController( updValue );
			}
			else if ( parent == null && timeController != null )
			{
				// Destroy controller
				ControllerManager.Instance.DestroyController( timeController );
				timeController = null;
			}
		}

		protected void Clear()
		{
			freeParticles.AddRange( activeParticles );
			activeParticles.Clear();
			updateRemainTime = 0.0f;
		}

		protected void ConfigureRenderer()
		{
			// Actual allocate particles
			var currSize = particlePool.Count;
			var size = poolSize;
			if ( currSize < size )
			{
				IncreasePool( size );

				for ( var i = currSize; i < size; ++i )
				{
					// Add new items to the queue
					freeParticles.Add( particlePool[ i ] );
				}

				// Tell the renderer, if already configured
				if ( renderer != null && isRendererConfigured )
				{
					renderer.NotifyParticleQuota( size );
				}
			}

			if ( renderer != null && !isRendererConfigured )
			{
				renderer.NotifyParticleQuota( particlePool.Count );
				renderer.NotifyAttached( parentNode, parentIsTagPoint );
				renderer.NotifyDefaultDimensions( defaultWidth, defaultHeight );
				CreateVisualParticles( 0, particlePool.Count );
				var mat = (Material)MaterialManager.Instance.Load( materialName, resourceGroupName );
				renderer.Material = mat;
				if ( renderQueueIDSet )
					renderer.RenderQueueGroup = renderQueueID;
				renderer.SetKeepParticlesInLocalSpace( localSpace );
				isRendererConfigured = true;
			}
		}

		protected void CreateVisualParticles( int poolstart, int poolend )
		{
			for ( var i = poolstart; i < poolend; ++i )
			{
				particlePool[ i ].NotifyVisualData( renderer.CreateVisualData() );
			}
		}

		//-----------------------------------------------------------------------
		protected void DestroyVisualParticles( int poolstart, int poolend )
		{
			for ( var i = poolstart; i < poolend; ++i )
			{
				renderer.DestroyVisualData( particlePool[ i ].VisualData );
				particlePool[ i ].NotifyVisualData( null );
			}
		}

		protected void NotifyOrigin( string origin )
		{
			this.origin = origin;
		}

		//-----------------------------------------------------------------------
		protected void SetBounds( AxisAlignedBox aabb )
		{
			aab = (AxisAlignedBox)aabb.Clone();
			var sqDist = (float)Utility.Max( aab.Minimum.LengthSquared,
										   aab.Maximum.LengthSquared );
			boundingRadius = (float)Utility.Sqrt( sqDist );
		}

		//-----------------------------------------------------------------------
		protected void SetBoundsAutoUpdated( bool autoUpdate, float stopIn )
		{
			boundsAutoUpdate = autoUpdate;
			boundsUpdateTime = stopIn;
		}
		//-----------------------------------------------------------------------
		protected void SetRenderQueueGroup( RenderQueueGroupID queueID )
		{
			base.RenderQueueGroup = queueID;
			if ( renderer != null )
			{
				renderer.RenderQueueGroup = queueID;
			}
		}
		//-----------------------------------------------------------------------
		protected void SetKeepParticlesInLocalSpace( bool keepLocal )
		{
			localSpace = keepLocal;
			if ( renderer != null )
			{
				renderer.SetKeepParticlesInLocalSpace( keepLocal );
			}
		}

		public void ScaleVelocity( float velocityMultiplier )
		{
			var emitterCount = emitterList.Count;
			for ( var i = 0; i < emitterCount; i++ )
			{
				var emitter = (ParticleEmitter)emitterList[ i ];
				emitter.ScaleVelocity( velocityMultiplier );
			}
		}

		public bool SetParameter( string attr, string val )
		{
			if ( attribParsers.ContainsKey( attr ) )
			{
				var args = new object[ 2 ];
				args[ 0 ] = val.Split( ' ' );
				args[ 1 ] = this;
				attribParsers[ attr ].Invoke( null, args );
				// attribParsers[attr].Invoke(this, val.Split(' '));
				//ParticleSystemAttributeParser parser =
				//        (ParticleSystemAttributeParser)attribParsers[attr];

				//// call the parser method
				//parser(val.Split(' '), this);
				return true;
			}
			return false;
		}

		#endregion

		#region Script parser methods

		/// <summary>
		///		Registers all attribute names with their respective parser.
		/// </summary>
		/// <remarks>
		///		Methods meant to serve as attribute parsers should use a method attribute to 
		/// </remarks>
		private void RegisterParsers()
		{
			var methods = this.GetType().GetMethods();

			// loop through all methods and look for ones marked with attributes
			for ( var i = 0; i < methods.Length; i++ )
			{
				// get the current method in the loop
				var method = methods[ i ];

				// see if the method should be used to parse one or more material attributes
				var parserAtts =
					(ParserCommandAttribute[])method.GetCustomAttributes( typeof( ParserCommandAttribute ), true );

				// loop through each one we found and register its parser
				for ( var j = 0; j < parserAtts.Length; j++ )
				{
					var parserAtt = parserAtts[ j ];

					switch ( parserAtt.ParserType )
					{
						// this method should parse a material attribute
						case PARTICLE:
							attribParsers.Add( parserAtt.Name, method );
							break;

					} // switch
				} // for
			} // for
		}

		[ParserCommand( "sorted", PARTICLE )]
		public static void ParseSorted( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "sorted", system.Name, "Wrong number of parameters." );
				return;
			}

			system.Sorted = StringConverter.ParseBool( values[ 0 ] );
		}

		[ParserCommand( "emit_emitter_quota", PARTICLE )]
		public static void ParseEmitEmitterQuota( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "emit_emitter_quota", system.Name, "Wrong number of parameters." );
				return;
			}

			//system.CullIndividual = StringConverter.ParseBool( values[ 0 ] );
		}


		[ParserCommand( "cull_each", PARTICLE )]
		public static void ParseCullEach( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "cull_each", system.Name, "Wrong number of parameters." );
				return;
			}

			system.CullIndividual = StringConverter.ParseBool( values[ 0 ] );
		}

		[ParserCommand( "particle_width", PARTICLE )]
		public static void ParseWidth( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "particle_width", system.Name, "Wrong number of parameters." );
				return;
			}

			system.DefaultWidth = StringConverter.ParseFloat( values[ 0 ] );
		}

		[ParserCommand( "particle_height", PARTICLE )]
		public static void ParseHeight( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "particle_height", system.Name, "Wrong number of parameters." );
				return;
			}

			system.DefaultHeight = StringConverter.ParseFloat( values[ 0 ] );
		}

		[ParserCommand( "material", PARTICLE )]
		public static void ParseMaterial( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "material", system.Name, "Wrong number of parameters." );
				return;
			}

			system.MaterialName = values[ 0 ];
		}

		[ParserCommand( "quota", PARTICLE )]
		public static void ParseQuota( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "quota", system.Name, "Wrong number of parameters." );
				return;
			}

			system.ParticleQuota = int.Parse( values[ 0 ] );
		}

		[ParserCommand( "local_space", PARTICLE )]
		public static void ParseLocalSpace( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "local_space", system.Name, "Wrong number of parameters." );
				return;
			}

			system.LocalSpace = StringConverter.ParseBool( values[ 0 ] );
		}

		[ParserCommand( "renderer", PARTICLE )]
		public static void ParseRenderer( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "renderer", system.Name, "Wrong number of parameters." );
				return;
			}

			system.RendererName = values[ 0 ];
		}

		[ParserCommand( "iteration_interval", PARTICLE )]
		public static void ParseIterationInterval( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "iteration_interval", system.Name, "Wrong number of parameters." );
				return;
			}

			system.IterationInterval = float.Parse( values[ 0 ] );
		}

		[ParserCommand( "nonvisible_update_timeout", PARTICLE )]
		public static void ParseNonvisibleUpdateTimeout( string[] values, ParticleSystem system )
		{
			if ( values.Length != 1 )
			{
				ParseHelper.LogParserError( "nonvisible_update_timeout", system.Name, "Wrong number of parameters." );
				return;
			}

			system.NonVisibleUpdateTimeout = float.Parse( values[ 0 ] );
		}

		#endregion

		#region Properties

		public bool LocalSpace
		{
			get
			{
				return localSpace;
			}
			set
			{
				this.localSpace = value;
			}
		}

		public float DefaultWidth
		{
			get
			{
				return defaultWidth;
			}
			set
			{
				defaultWidth = value;
				if ( renderer != null )
					renderer.NotifyDefaultDimensions( defaultWidth, defaultHeight );
			}
		}
		public float DefaultHeight
		{
			get
			{
				return defaultHeight;
			}
			set
			{
				defaultHeight = value;
				if ( renderer != null )
					renderer.NotifyDefaultDimensions( defaultWidth, defaultHeight );
			}
		}

		/// <summary>
		///		Gets the count of active particles currently in the system.
		/// </summary>
		public int ParticleCount
		{
			get
			{
				return activeParticles.Count;
			}
		}

		/// <summary>
		///		Returns the maximum number of particles this system is allowed to have active at once.
		/// </summary>
		/// <remarks>
		///		Particle systems all have a particle quota, i.e. a maximum number of particles they are 
		///		allowed to have active at a time. This allows the application to set a keep particle systems
		///		under control should they be affected by complex parameters which alter their emission rates
		///		etc. If a particle system reaches it's particle quota, none of the emitters will be able to 
		///		emit any more particles. As existing particles die, the spare capacity will be allocated
		///		equally across all emitters to be as consistent to the origina particle system style as possible.
		/// </remarks>
		public int ParticleQuota
		{
			get
			{
				return poolSize;
			}
			set
			{
				if ( poolSize < value )
					poolSize = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public List<Particle> Particles
		{
			get
			{
				return activeParticles;
			}
		}

		public string MaterialName
		{
			get
			{
				return materialName;
			}
			set
			{
				materialName = value;
				if ( isRendererConfigured )
				{
					var mat = (Material)MaterialManager.Instance.Load( materialName, resourceGroupName );
					renderer.Material = mat;
				}
			}
		}

		public string RendererName
		{
			get
			{
				if ( renderer != null )
					return renderer.Type;
				return string.Empty;
			}
			set
			{
				if ( renderer != null )
				{
					DestroyVisualParticles( 0, particlePool.Count );
					// ParticleSystemManager.Instance.DestroyRenderer(renderer);
					renderer = null;
				}
				if ( value != null && value != "" )
				{
					renderer = ParticleSystemManager.Instance.CreateRenderer( value );
					isRendererConfigured = false;
				}
			}
		}

		public bool CullIndividual
		{
			get
			{
				return cullIndividual;
			}
			set
			{
				cullIndividual = value;
			}
		}

		public bool Sorted
		{
			get
			{
				return sorted;
			}
			set
			{
				sorted = value;
			}
		}

		public float SpeedFactor
		{
			get
			{
				return speedFactor;
			}
			set
			{
				speedFactor = value;
			}
		}

		public float IterationInterval
		{
			get
			{
				return iterationInterval;
			}
			set
			{
				iterationInterval = value;
				iterationIntervalSet = true;
			}
		}

		public static float DefaultIterationInterval
		{
			get
			{
				return defaultIterationInterval;
			}
			set
			{
				defaultIterationInterval = value;
			}
		}

		public float NonVisibleUpdateTimeout
		{
			get
			{
				return nonvisibleTimeout;
			}
			set
			{
				nonvisibleTimeout = value;
				nonvisibleTimeoutSet = true;
			}
		}

		public static float DefaultNonVisibleUpdateTimeout
		{
			get
			{
				return defaultNonvisibleTimeout;
			}
			set
			{
				defaultNonvisibleTimeout = value;
			}
		}

		public string Origin
		{
			get
			{
				return origin;
			}
            set
            {
                origin = value;
            }
		}

		#endregion

		/// <summary>
		///		Cloning will deep copy all particle emitters and effectors, but not particles. The
		///		system's name is also not copied.
		/// </summary>
		/// <returns></returns>
		public void CopyTo( ParticleSystem system )
		{
			// remove the target's emitters and affectors
			system.RemoveAllEmitters();
			system.RemoveAllAffectors();

			// loop through emitter and affector lists and copy them over
			foreach ( var emitter in emitterList )
			{
				var newEmitter = system.AddEmitter( emitter.Type );
				emitter.CopyTo( newEmitter );
			}

			foreach ( var affector in affectorList )
			{
				var newAffector = system.AddAffector( affector.Type );
				affector.CopyTo( newAffector );
			}
			system.ParticleQuota = this.ParticleQuota;
			system.MaterialName = this.MaterialName;
			system.SetDefaultDimensions( this.defaultWidth, this.defaultHeight );
			system.cullIndividual = this.cullIndividual;
			system.sorted = this.sorted;
			system.localSpace = this.localSpace;
			system.iterationInterval = this.iterationInterval;
			system.iterationIntervalSet = this.iterationIntervalSet;
			system.nonvisibleTimeout = this.nonvisibleTimeout;
			system.nonvisibleTimeoutSet = this.nonvisibleTimeoutSet;
			// last frame visible and time since last visible should be left default
			system.RendererName = this.RendererName;
			// FIXME
			if ( system.renderer != null && renderer != null )
			{
				renderer.CopyParametersTo( system.renderer );
			}
		}

		/// <summary>
		///		Updates the particles in the system based on time elapsed.
		///	 </summary>
		///	 <remarks>	
		///		This is called automatically every frame by the engine.
		/// </remarks>
		/// <param name="timeElapsed">The amount of time (in seconds) since the last frame.</param>
		internal void Update( float timeElapsed )
		{
			// Only update if attached to a node
			if ( parentNode == null )
				return;

			var _nonvisibleTimeout = nonvisibleTimeoutSet ? nonvisibleTimeout : defaultNonvisibleTimeout;

			if ( _nonvisibleTimeout > 0 )
			{
				// Check whether it's been more than one frame (update is ahead of
				// camera notification by one frame because of the ordering)
				var frameDiff = Root.Instance.CurrentFrameCount - lastVisibleFrame;
				if ( frameDiff > 1 || frameDiff < 0 ) // < 0 if wrap only
				{
					timeSinceLastVisible += timeElapsed;
					if ( timeSinceLastVisible >= _nonvisibleTimeout )
					{
						// No update
						return;
					}
				}
			}

			// Scale incoming speed for the rest of the calculation
			timeElapsed *= speedFactor;

			// Init renderer if not done already
			if ( renderer != null )
			{
				if ( !isRendererConfigured )
					ConfigureRenderer();
			}

			// Initialize emitted emitters list if not done already
			initializeEmittedEmitters();

			var _iterationInterval = iterationIntervalSet ? iterationInterval : defaultIterationInterval;
			if ( _iterationInterval > 0 )
			{
				updateRemainTime += timeElapsed;

				while ( updateRemainTime >= _iterationInterval )
				{
					// Update existing particles
					Expire( _iterationInterval );
					TriggerAffectors( _iterationInterval );
					ApplyMotion( _iterationInterval );
					// Emit new particles
					TriggerEmitters( _iterationInterval );

					updateRemainTime -= _iterationInterval;
				}
			}
			else
			{
				// Update existing particles
				Expire( timeElapsed );
				TriggerAffectors( timeElapsed );
				ApplyMotion( timeElapsed );
				// Emit new particles
				TriggerEmitters( timeElapsed );
			}

			if ( !boundsAutoUpdate && boundsUpdateTime > 0.0f )
				boundsUpdateTime -= timeElapsed; // count down 
			UpdateBounds();
		}

		private void initializeEmittedEmitters()
		{
			// Initialize the pool if needed
			var currSize = 0;
			if ( emittedEmitterPool.Count == 0 )
			{
				if ( emittedEmitterPoolInitialized )
				{
					// It was already initialized, but apparently no emitted emitters were used
					return;
				}
				else
				{
					initializeEmittedEmitterPool();
				}
			}
			else
			{
				foreach ( var peList in emittedEmitterPool.Values )
				{
					currSize += peList.Count;
				}
			}

			var size = emittedEmitterPoolSize;
			if ( currSize < size && emittedEmitterPool.Count != 0 )
			{
				// Increase the pool. Equally distribute over all vectors in the map
				increaseEmittedEmitterPool( size );

				// Add new items to the free list
				addFreeEmittedEmitters();
			}
		}

		private void initializeEmittedEmitterPool()
		{
			if ( emittedEmitterPoolInitialized )
				return;

			// Run through mEmitters and add keys to the pool
			foreach ( var emitter in emitterList )
			{
				// Determine the names of all emitters that are emitted
				if ( emitter != null && emitter.EmittedEmitter != string.Empty )
				{
					// This one will be emitted, register its name and leave the vector empty!
					emittedEmitterPool.Add( emitter.EmittedEmitter, new List<ParticleEmitter>() );
				}

				// Determine whether the emitter itself will be emitted and set the 'IsEmitted' attribute
				foreach ( var emitterInner in emitterList )
				{
					if ( emitter != null &&
						emitterInner != null &&
						emitter.Name != string.Empty &&
						emitter.Name == emitterInner.EmittedEmitter )
					{
						emitter.IsEmitted = true;
						break;
					}
					else
					{
						// Set explicitly to 'false' although the default value is already 'false'
						emitter.IsEmitted = false;
					}
				}
			}

			emittedEmitterPoolInitialized = true;
		}

		private void increaseEmittedEmitterPool( int size )
		{
			// Don't proceed if the pool doesn't contain any keys of emitted emitters
			if ( emittedEmitterPool.Count == 0 )
				return;

			ParticleEmitter clonedEmitter = null;
			var name = string.Empty;
			List<ParticleEmitter> e = null;
			var maxNumberOfEmitters = size / emittedEmitterPool.Count; // equally distribute the number for each emitted emitter list
			var oldSize = 0;

			// Run through mEmittedEmitterPool and search for every key (=name) its corresponding emitter in mEmitters
			foreach ( var poolEntry in emittedEmitterPool )
			{
				name = poolEntry.Key;
				e = poolEntry.Value;

				// Search the correct emitter in the mEmitters vector
				foreach ( var emitter in emitterList )
				{
					if ( emitter != null &&
						name != string.Empty &&
						name == emitter.Name )
					{
						// Found the right emitter, clone each emitter a number of times
						oldSize = e.Count;
						for ( var t = oldSize; t < maxNumberOfEmitters; ++t )
						{
							clonedEmitter = ParticleSystemManager.Instance.CreateEmitter( emitter.Type, this );
							emitter.CopyTo( clonedEmitter );
							clonedEmitter.IsEmitted = emitter.IsEmitted; // is always 'true' by the way, but just in case

							// Initially deactivate the emitted emitter if duration/repeat_delay are set
							if ( clonedEmitter.Duration > 0.0f &&
								( clonedEmitter.RepeatDelay > 0.0f || clonedEmitter.MinRepeatDelay > 0.0f || clonedEmitter.MinRepeatDelay > 0.0f ) )
								clonedEmitter.IsEnabled = false;

							// Add cloned emitters to the pool
							e.Add( clonedEmitter );
						}
					}
				}
			}
		}

		private void addFreeEmittedEmitters()
		{
			// Don't proceed if the EmittedEmitterPool is empty
			if ( emittedEmitterPool.Count == 0 )
				return;

			// Copy all pooled emitters to the free list
			List<ParticleEmitter> emittedEmitters = null;
			List<ParticleEmitter> fee = null;
			var name = string.Empty;

			// Run through the emittedEmitterPool map
			foreach ( var poolEntry in emittedEmitterPool )
			{
				name = poolEntry.Key;
				emittedEmitters = poolEntry.Value;
				fee = findFreeEmittedEmitter( name );

				// If it´s not in the map, create an empty one
				if ( fee == null )
				{
					freeEmittedEmitters.Add( name, new List<ParticleEmitter>() );
					fee = findFreeEmittedEmitter( name );
				}

				// Check anyway if it´s ok now
				if ( fee == null )
					return; // forget it!

				// Add all emitted emitters from the pool to the free list
				foreach ( var emittedEmitter in emittedEmitters )
				{
					fee.Add( emittedEmitter );
				}
			}
		}

		public override AxisAlignedBox BoundingBox
		{
			get
			{
				return aab;
			}
		}

		public override float BoundingRadius
		{
			get
			{
				return boundingRadius;
			}
		}

		/// <summary>
		/// Get the 'type flags' for this <see cref="ParticleSystem"/>.
		/// </summary>
		/// <seealso cref="MovableObject.TypeFlags"/>
		public override uint TypeFlags
		{
			get
			{
				return (uint)SceneQueryTypeMask.Fx;
			}
		}

		internal ParticleSystemRenderer Renderer
		{
			get
			{
				return renderer;
			}
		}
	}

	public class ParticleSystemUpdateValue : IControllerValue<float>
	{
		protected ParticleSystem target;

		public ParticleSystemUpdateValue( ParticleSystem target )
		{
			this.target = target;
		}

		public float Value
		{
			get
			{
				return 0;
			}
			set
			{
				target.Update( value );
			}
		}
	}
}
