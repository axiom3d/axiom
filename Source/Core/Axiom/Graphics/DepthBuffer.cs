using System;
using System.Collections.Generic;
using System.Diagnostics;
using Axiom.Core;

namespace Axiom.Graphics
{
	public enum PoolId
	{
		NoDepth = 0,
		ManualUsage = 0,
		Default = 1
	}

	/// <summary>
	/// An abstract class that contains a depth/stencil buffer.
	/// Depth Buffers can be attached to render targets. Note we handle Depth &amp; Stencil together.
	/// DepthBuffer sharing is handled automatically for you. However, there are times where you want
	/// to specifically control depth buffers to achieve certain effects or increase performance.
	/// You can control this by hinting Ogre with POOL IDs. Created depth buffers can live in different
	/// pools, or alltoghether in the same one.
	/// Usually, a depth buffer can only be attached to a RenderTarget only if it's dimensions are bigger
	/// and have the same bit depth and same multisample settings. Depth Buffers are created automatically
	/// for new RTs when needed, and stored in the pool where the RenderTarget should have drawn from.
	/// By default, all RTs have the Id POOL_DEFAULT, which means all depth buffers are stored by default
	/// in that pool. By chosing a different Pool Id for a specific RenderTarget, that RT will only
	/// retrieve depth buffers from _that_ pool, therefore not conflicting with sharing depth buffers
	/// with other RTs (such as shadows maps).
	/// Setting an RT to POOL_MANUAL_USAGE means Ogre won't manage the DepthBuffer for you (not recommended)
	/// RTs with POOL_NO_DEPTH are very useful when you don't want to create a DepthBuffer for it. You can
	/// still manually attach a depth buffer though as internally POOL_NO_DEPTH &amp; POOL_MANUAL_USAGE are
	/// handled in the same way.
	/// 
	/// Behavior is consistent across all render systems, if, and only if, the same RSC flags are set
	/// RSC flags that affect this class are:
	/// * RSC_RTT_SEPARATE_DEPTHBUFFER:
	/// The RTT can create a custom depth buffer different from the main depth buffer. This means,
	/// an RTT is able to not share it's depth buffer with the main window if it wants to.
	/// * RSC_RTT_MAIN_DEPTHBUFFER_ATTACHABLE:
	/// When RSC_RTT_SEPARATE_DEPTHBUFFER is set, some APIs (ie. OpenGL w/ FBO) don't allow using
	/// the main depth buffer for offscreen RTTs. When this flag is set, the depth buffer can be
	/// shared between the main window and an RTT.
	/// * RSC_RTT_DEPTHBUFFER_RESOLUTION_LESSEQUAL:
	/// When this flag isn't set, the depth buffer can only be shared across RTTs who have the EXACT
	/// same resolution. When it's set, it can be shared with RTTs as long as they have a
	/// resolution less or equal than the depth buffer's.
	/// </summary>
	/// <remarks>
	/// Design discussion <a href="http://www.ogre3d.org/forums/viewtopic.php?f=4&amp;t=53534&amp;p=365582" />
	/// </remarks>    
	public class DepthBuffer : DisposableObject
	{
		[OgreVersion( 1, 7, 2790 )]
		public DepthBuffer( PoolId poolId, ushort bitDepth, int width, int height, int fsaa, string fsaaHint, bool manual )
		{
			this.poolId = poolId;
			this.bitDepth = bitDepth;
			this.width = width;
			this.height = height;
			this.fsaa = fsaa;
			this.fsaaHint = fsaaHint;
			this.manual = manual;
		}

		[OgreVersion( 1, 7, 2790 )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				DetachFromAllRenderTargets();
			}
			base.dispose( disposeManagedResources );
		}

		#region PoolId

		[OgreVersion( 1, 7, 2790 )] protected PoolId poolId;

		/// <summary>
		/// Gets the pool id in which this DepthBuffer lives
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public virtual PoolId PoolId
		{
			get
			{
				return this.poolId;
			}

			set
			{
				//Change the pool Id
				this.poolId = value;

				//Render Targets were attached to us, but they have a different pool Id,
				//so detach ourselves from them
				DetachFromAllRenderTargets();
			}
		}

		#endregion

		/// <summary>
		/// Sets the pool id in which this DepthBuffer lives
		/// Note this will detach any render target from this depth buffer
		/// </summary>
		[OgreVersion( 1, 7, 2790, "The setter is nonvirtual, thus cant be part of the PoolId property" )]
		public void SetPoolId( PoolId id )
		{
			//Change the pool Id
			this.poolId = id;

			//Render Targets were attached to us, but they have a different pool Id,
			//so detach ourselves from them
			DetachFromAllRenderTargets();
		}

		[OgreVersion( 1, 7, 2790 )]
		public virtual ushort BitDepth
		{
			get
			{
				return this.bitDepth;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public virtual int Width
		{
			get
			{
				return this.width;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public virtual int Height
		{
			get
			{
				return this.height;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public virtual int Fsaa
		{
			get
			{
				return this.fsaa;
			}
		}

		[OgreVersion( 1, 7, 2790 )]
		public virtual string FsaaHint
		{
			get
			{
				return this.fsaaHint;
			}
		}

		/// <summary>
		/// Manual DepthBuffers are cleared in RenderSystem's destructor. Non-manual ones are released
		/// with it's render target (aka, a backbuffer or similar)
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public bool IsManual
		{
			get
			{
				return this.manual;
			}
		}

		/// <summary>
		/// Returns whether the specified RenderTarget is compatible with this DepthBuffer
		/// That is, this DepthBuffer can be attached to that RenderTarget
		/// </summary>
		/// <remarks>
		/// Most APIs impose the following restrictions:
		/// Width &amp; height must be equal or higher than the render target's
		/// They must be of the same bit depth.
		/// They need to have the same FSAA setting
		/// </remarks>
		/// <param name="renderTarget">The render target to test against</param>
		/// <returns>true if compatible</returns>
		[OgreVersion( 1, 7, 2790 )]
		public virtual bool IsCompatible( RenderTarget renderTarget )
		{
			return Width >= renderTarget.Width && Height >= renderTarget.Height && Fsaa == renderTarget.FSAA;
		}

		/// <summary>
		/// Called when a RenderTarget is attaches this DepthBuffer
		/// </summary>
		/// <remarks>
		/// This function doesn't actually attach. It merely informs the DepthBuffer
		/// which RenderTarget did attach. The real attachment happens in
		/// RenderTarget::attachDepthBuffer()
		/// </remarks>
		/// <param name="renderTarget">The RenderTarget that has just been attached</param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void NotifyRenderTargetAttached( RenderTarget renderTarget )
		{
			Debug.Assert( !this.attachedRenderTargets.Contains( renderTarget ) );
			this.attachedRenderTargets.Add( renderTarget );
		}

		/// <summary>
		/// Called when a RenderTarget is detaches from this DepthBuffer
		/// </summary>
		/// <remarks>
		/// Same as <see cref="NotifyRenderTargetAttached"/>
		/// </remarks>        
		/// <param name="renderTarget">The RenderTarget that has just been attached</param>
		[OgreVersion( 1, 7, 2790 )]
		public virtual void NotifyRenderTargetDetached( RenderTarget renderTarget )
		{
			var success = this.attachedRenderTargets.Remove( renderTarget );
			Debug.Assert( success );
		}

		protected class RenderTargetSet : List<RenderTarget>
		{
		}

		[OgreVersion( 1, 7, 2790 )] protected ushort bitDepth;

		[OgreVersion( 1, 7, 2790 )] protected int width;

		[OgreVersion( 1, 7, 2790 )] protected int height;

		[OgreVersion( 1, 7, 2790 )] protected int fsaa;

		[OgreVersion( 1, 7, 2790 )] protected string fsaaHint;

		[OgreVersion( 1, 7, 2790 )] protected bool manual;

		[OgreVersion( 1, 7, 2790 )] protected RenderTargetSet attachedRenderTargets = new RenderTargetSet();

		[OgreVersion( 1, 7, 2790 )]
		protected void DetachFromAllRenderTargets()
		{
			foreach ( var itor in this.attachedRenderTargets )
			{
				itor._DetachDepthBuffer();
			}
			this.attachedRenderTargets.Clear();
		}
	}
}