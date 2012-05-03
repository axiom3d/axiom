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

#endregion LGPL License

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
using System.IO;
using Axiom.Collections;
using Axiom.Math;
using Axiom.Core;
using Axiom.Serialization;
using ResourceHandle = System.UInt64;
using Axiom.Animating.Collections;

#endregion Namespace Declarations

#region Ogre Synchronization Information

// <ogresynchronization>
//     <file name="Skeleton.h"   revision="" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
//     <file name="Skeleton.cpp" revision="" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
// </ogresynchronization>

#endregion Ogre Synchronization Information

namespace Axiom.Animating
{
	/// <summary>
	///		A collection of Bone objects used to animate a skinned mesh.
	///	 </summary>
	///	 <remarks>
	///		Skeletal animation works by having a collection of 'bones' which are
	///		actually just joints with a position and orientation, arranged in a tree structure.
	///		For example, the wrist joint is a child of the elbow joint, which in turn is a
	///		child of the shoulder joint. Rotating the shoulder automatically moves the elbow
	///		and wrist as well due to this hierarchy.
	///		<p/>
	///		So how does this animate a mesh? Well every vertex in a mesh is assigned to one or more
	///		bones which affects it's position when the bone is moved. If a vertex is assigned to
	///		more than one bone, then weights must be assigned to determine how much each bone affects
	///		the vertex (actually a weight of 1.0 is used for single bone assignments).
	///		Weighted vertex assignments are especially useful around the joints themselves
	///		to avoid 'pinching' of the mesh in this region.
	///		<p/>
	///		Therefore by moving the skeleton using preset animations, we can animate the mesh. The
	///		advantage of using skeletal animation is that you store less animation data, especially
	///		as vertex counts increase. In addition, you are able to blend multiple animations together
	///		(e.g. walking and looking around, running and shooting) and provide smooth transitions
	///		between animations without incurring as much of an overhead as would be involved if you
	///		did this on the core vertex data.
	///		<p/>
	///		Skeleton definitions are loaded from datafiles, namely the .xsf file format. They
	///		are loaded on demand, especially when referenced by a Mesh.
	/// </remarks>
	public class Skeleton : Resource
	{
		#region Constants

		/// <summary>Maximum total available bone matrices that are available during blending.</summary>
		public const int MAX_BONE_COUNT = 256;

		#endregion Constants

		#region Fields and Properties

		#region BlendMode Property

		/// <summary>Mode of animation blending to use.</summary>
		private SkeletalAnimBlendMode _blendMode = SkeletalAnimBlendMode.Average;

		/// <summary>
		///    Gets/Sets the animation blending mode which this skeleton will use.
		/// </summary>
		public SkeletalAnimBlendMode BlendMode
		{
			get
			{
				return _blendMode;
			}
			set
			{
				_blendMode = value;
			}
		}

		#endregion BlendMode Property

		/// <summary>Internal list of bones attached to this skeleton, indexed by name.</summary>
		protected AxiomCollection<Bone> namedBoneList = new AxiomCollection<Bone>();

		#region BoneList Properties

		/// <summary>Internal list of bones attached to this skeleton, indexed by handle.</summary>
		protected BoneCollection boneList = new BoneCollection();

		/// <summary>
		/// Gets the bones.
		/// </summary>
		/// <value>The bones.</value>
		public IList<Bone> Bones
		{
			get
			{
				return boneList.Values;
			}
		}

		/// <summary>
		///    Gets the number of bones in this skeleton.
		/// </summary>
		public int BoneCount
		{
			get
			{
				return boneList.Count;
			}
		}

		#endregion BoneList Properties

		#region RootBones Properties

		/// <summary>Reference to the root bone of this skeleton.</summary>
		protected BoneList rootBones = new BoneList();

		/// <summary>
		///    Gets the root bone of the skeleton.
		/// </summary>
		/// <remarks>
		///    The system derives the root bone the first time you ask for it. The root bone is the
		///    only bone in the skeleton which has no parent. The system locates it by taking the
		///    first bone in the list and going up the bone tree until there are no more parents,
		///    and saves this top bone as the root. If you are building the skeleton manually using
		///    CreateBone then you must ensure there is only one bone which is not a child of
		///    another bone, otherwise your skeleton will not work properly. If you use CreateBone
		///    only once, and then use Bone.CreateChild from then on, then inherently the first
		///    bone you create will by default be the root.
		/// </remarks>
		public Bone RootBone
		{
			get
			{
				if ( rootBones.Count == 0 )
				{
					DeriveRootBone();
				}

				return rootBones[ 0 ];
			}
		}

		/// <summary>
		///		Gets the number of root bones in this skeleton.
		/// </summary>
		public int RootBoneCount
		{
			get
			{
				if ( rootBones.Count == 0 )
				{
					DeriveRootBone();
				}

				return rootBones.Count;
			}
		}

		#endregion RootBones Properties

		#region CurrentEntity Property

		/// <summary>
		///    Get/Set the entity that is currently updating this skeleton.
		/// </summary>
		public Entity CurrentEntity { get; set; }

		#endregion CurrentEntity Property

		#region nextAutoHandle Property

		/// <summary>Used for auto generated handles to ensure they are unique.</summary>
		private ushort _nextAutoHandle = 0;

		protected internal ushort nextAutoHandle
		{
			get
			{
				return _nextAutoHandle++;
			}
			set
			{
				_nextAutoHandle = value;
			}
		}

		#endregion nextAutoHandle Property

		#region Animations Property

		/// <summary>Lookup table for animations related to this skeleton.</summary>
		protected AnimationCollection animationList = new AnimationCollection();

		/// <summary>
		///     Gets the animations associated with this skeleton
		/// </summary>
		public virtual ICollection<Animation> Animations
		{
			get
			{
				return animationList.Values;
			}
		}

		#endregion Animations Property

		#region AttachmentPoints Property

		/// <summary>Internal list of bones attached to this skeleton, indexed by handle.</summary>
		protected List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();

		public List<AttachmentPoint> AttachmentPoints
		{
			get
			{
				return attachmentPoints;
			}
		}

		#endregion AttachmentPoints Property

		#endregion Fields and Properties

		#region Construction and Destruction

		internal Skeleton()
		{
		}

		/// <summary>
		/// Constructor, don't call directly, use SkeletonManager.
		/// </summary>
		/// <remarks>
		/// On creation, a Skeleton has a no bones, you should create them and link
		/// them together appropriately.
		/// </remarks>
		public Skeleton( ResourceManager parent, String name, ResourceHandle handle, string group )
			: this( parent, name, handle, group, false, null )
		{
		}

		/// <summary>
		/// Constructor, don't call directly, use SkeletonManager.
		/// </summary>
		/// <remarks>
		/// On creation, a Skeleton has a no bones, you should create them and link
		/// them together appropriately.
		/// </remarks>
		public Skeleton( ResourceManager parent, String name, ResourceHandle handle, string group, bool isManual,
		                 IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///    Creates a new Animation object for animating this skeleton.
		/// </summary>
		/// <param name="name">The name of this animation</param>
		/// <param name="length">The length of the animation in seconds</param>
		/// <returns></returns>
		public virtual Animation CreateAnimation( string name, float length )
		{
			// Check name not used
			if ( animationList.ContainsKey( name ) )
			{
				throw new Exception( "An animation with the name already exists" );
			}

			var anim = new Animation( name, length );

			animationList.Add( name, anim );

			return anim;
		}

		#region CreateBone Method

		/// <summary>
		///    Creates a brand new Bone owned by this Skeleton.
		/// </summary>
		/// <remarks>
		///    This method creates an unattached new Bone for this skeleton. Unless this is to
		///    be the root bone (there must only be one of these), you must
		///    attach it to another Bone in the skeleton using addChild for it to be any use.
		///    For this reason you will likely be better off creating child bones using the
		///    Bone.CreateChild method instead, once you have created the root bone.
		///    <p/>
		///    Note that this method automatically generates a handle for the bone, which you
		///    can retrieve using Bone.Handle. If you wish the new Bone to have a specific
		///    handle, use the alternate form of this method which takes a handle as a parameter,
		///    although you should note the restrictions.
		/// </remarks>
		public Bone CreateBone()
		{
			return CreateBone( nextAutoHandle );
		}

		/// <summary>
		///    Creates a brand new Bone owned by this Skeleton.
		/// </summary>
		/// <remarks>
		///    This method creates an unattached new Bone for this skeleton. Unless this is to
		///    be the root bone (there must only be one of these), you must
		///    attach it to another Bone in the skeleton using addChild for it to be any use.
		///    For this reason you will likely be better off creating child bones using the
		///    Bone.CreateChild method instead, once you have created the root bone.
		/// </remarks>
		/// <param name="name">
		///    The name to give to this new bone - must be unique within this skeleton.
		///    Note that the way the engine looks up bones is via a numeric handle, so if you name a
		///    Bone this way it will be given an automatic sequential handle. The name is just
		///    for your convenience, although it is recommended that you only use the handle to
		///    retrieve the bone in performance-critical code.
		/// </param>
		public virtual Bone CreateBone( string name )
		{
			if ( boneList.Count == MAX_BONE_COUNT )
			{
				throw new Exception( "Skeleton exceeded the maximum amount of bones." );
			}

			// create the new bone, and add it to both lookup lists
			var bone = new Bone( name, nextAutoHandle, this );
			boneList.Add( bone.Handle, bone );
			namedBoneList.Add( bone.Name, bone );

			return bone;
		}

		/// <summary>
		///    Creates a brand new Bone owned by this Skeleton.
		/// </summary>
		/// <param name="handle">
		///    The handle to give to this new bone - must be unique within this skeleton.
		///    You should also ensure that all bone handles are eventually contiguous (this is to simplify
		///    their compilation into an indexed array of transformation matrices). For this reason
		///    it is advised that you use the simpler createBone method which automatically assigns a
		///    sequential handle starting from 0.
		/// </param>
		public virtual Bone CreateBone( ushort handle )
		{
			if ( boneList.Count == MAX_BONE_COUNT )
			{
				throw new Exception( "Skeleton exceeded the maximum amount of bones." );
			}

			// create the new bone, and add it to both lookup lists
			var bone = new Bone( nextAutoHandle, this );
			boneList.Add( bone.Handle, bone );
			namedBoneList.Add( bone.Name, bone );

			return bone;
		}

		/// <summary>
		///    Creates a brand new Bone owned by this Skeleton.
		/// </summary>
		/// <param name="name">
		///    The name to give to this new bone - must be unique within this skeleton.
		///    Note that the way the engine looks up bones is via a numeric handle, so if you name a
		///    Bone this way it will be given an automatic sequential handle. The name is just
		///    for your convenience, although it is recommended that you only use the handle to
		///    retrieve the bone in performance-critical code.
		/// </param>
		/// <param name="handle">
		///    The handle to give to this new bone - must be unique within this skeleton.
		///    You should also ensure that all bone handles are eventually contiguous (this is to simplify
		///    their compilation into an indexed array of transformation matrices). For this reason
		///    it is advised that you use the simpler createBone method which automatically assigns a
		///    sequential handle starting from 0.
		/// </param>
		public virtual Bone CreateBone( string name, ushort handle )
		{
			if ( boneList.Count == MAX_BONE_COUNT )
			{
				throw new Exception( "Skeleton exceeded the maximum amount of bones." );
			}

			// create the new bone, and add it to both lookup lists
			var bone = new Bone( name, handle, this );
			boneList.Add( bone.Handle, bone );
			namedBoneList.Add( bone.Name, bone );

			return bone;
		}

		#endregion CreateBone Method

		/// <summary>
		///    Internal method which parses the bones to derive the root bone.
		/// </summary>
		protected void DeriveRootBone()
		{
			if ( boneList.Count == 0 )
			{
				throw new Exception( "Cannot derive the root bone for a skeleton that has no bones." );
			}

			rootBones.Clear();

			// get the first bone in the list
			var currentBone = boneList[ 0 ];

			foreach ( var bone in boneList.Values )
			{
				if ( bone.Parent == null )
				{
					rootBones.Add( bone );
				}
			}
		}

		#region GetAnimation Method

		/// <summary>
		///    Returns the animation with the specified name.
		/// </summary>
		/// <param name="name">Name of the animation to retrieve.</param>
		/// <returns></returns>
		public virtual Animation GetAnimation( string name )
		{
			if ( !animationList.ContainsKey( name ) )
			{
				return null;
				// throw new Exception("Animation named '" + name + "' is not part of this skeleton.");
			}

			return animationList[ name ];
		}

		#endregion GetAnimation Method

		public virtual bool ContainsAnimation( string name )
		{
			return animationList.ContainsKey( name );
		}

		#region GetBone Method

		/// <summary>
		///    Gets a bone by its handle.
		/// </summary>
		/// <param name="handle">Handle of the bone to retrieve.</param>
		/// <returns></returns>
		public virtual Bone GetBone( ushort handle )
		{
			if ( !boneList.ContainsKey( handle ) )
			{
				throw new Exception( "Bone with the handle " + handle + " not found." );
			}

			return (Bone)boneList[ handle ];
		}

		/// <summary>
		///    Gets a bone by its name.
		/// </summary>
		/// <param name="name">Name of the bone to retrieve.</param>
		/// <returns></returns>
		public virtual Bone GetBone( string name )
		{
			if ( !namedBoneList.ContainsKey( name ) )
			{
				//throw new Exception( "Bone with the name '" + name + "' not found." );
				return null;
			}

			return (Bone)namedBoneList[ name ];
		}

		#endregion GetBone Method

		/// <summary>
		///    Checks to see if a bone exists
		/// </summary>
		/// <param name="name">Name of the bone to check.</param>
		/// <returns></returns>
		public virtual bool ContainsBone( string name )
		{
			return namedBoneList.ContainsKey( name );
		}

		/// <summary>
		///		Gets the root bone at the specified index.
		/// </summary>
		/// <param name="index">Index of the root bone to return.</param>
		/// <returns>Root bone at the specified index, or null if the index is out of bounds.</returns>
		public virtual Bone GetRootBone( int index )
		{
			if ( index < rootBones.Count )
			{
				return rootBones[ index ];
			}

			return null;
		}

		/// <summary>
		///    Populates the passed in array with the bone matrices based on the current position.
		/// </summary>
		/// <remarks>
		///    Internal use only. The array passed in must
		///    be at least as large as the number of bones.
		///    Assumes animation has already been updated.
		/// </remarks>
		/// <param name="matrices"></param>
		internal virtual void GetBoneMatrices( Matrix4[] matrices )
		{
			// update derived transforms
			RootBone.Update( true, false );

			/*
				Calculating the bone matrices
				-----------------------------
				Now that we have the derived orientations & positions in the Bone nodes, we have
				to compute the Matrix4 to apply to the vertices of a mesh.
				Because any modification of a vertex has to be relative to the bone, we must first
				reverse transform by the Bone's original derived position/orientation, then transform
				by the new derived position / orientation.
			*/
			var i = 0;
			foreach ( var bone in boneList.Values )
			{
				matrices[ i++ ] = bone.FullTransform*bone.BindDerivedInverseTransform;
			}
		}

		/// <summary>
		///    Initialize an animation set suitable for use with this mesh.
		/// </summary>
		/// <remarks>
		///    Only recommended for use inside the engine, not by applications.
		/// </remarks>
		/// <param name="animSet"></param>
		public virtual void InitAnimationState( AnimationStateSet animSet )
		{
			animSet.RemoveAllAnimationStates();
			foreach ( var anim in animationList.Values )
			{
				animSet.CreateAnimationState( anim.Name, 0, anim.Length );
			}
		}

		/// <summary>
		///    Removes the animation with the specified name from this skeleton.
		/// </summary>
		/// <param name="name">Name of the animation to remove.</param>
		/// <returns></returns>
		public virtual void RemoveAnimation( string name )
		{
			animationList.Remove( name );
		}

		/// <summary>
		///    Resets the position and orientation of all bones in this skeleton to their original binding position.
		/// </summary>
		/// <remarks>
		///    A skeleton is bound to a mesh in a binding pose. Bone positions are then modified from this
		///    position during animation. This method returns all the bones to their original position and
		///    orientation.
		/// </remarks>
		public void Reset()
		{
			Reset( false );
		}

		/// <summary>
		///    Resets the position and orientation of all bones in this skeleton to their original binding position.
		/// </summary>
		/// <remarks>
		///    A skeleton is bound to a mesh in a binding pose. Bone positions are then modified from this
		///    position during animation. This method returns all the bones to their original position and
		///    orientation.
		/// </remarks>
		public virtual void Reset( bool resetManualBones )
		{
			// set all bones back to their binding pose
			for ( var i = 0; i < boneList.Count; i++ )
			{
				if ( !boneList.Values[ i ].IsManuallyControlled || resetManualBones )
				{
					boneList.Values[ i ].Reset();
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="animSet"></param>
		public virtual void SetAnimationState( AnimationStateSet animSet )
		{
			/*
			Algorithm:
			  1. Reset all bone positions
			  2. Iterate per AnimationState, if enabled get Animation and call Animation::apply
			*/

			// reset bones
			Reset();

			// per animation state
			foreach ( var animState in animSet.EnabledAnimationStates )
			{
				var anim = GetAnimation( animState.Name );
				// tolerate state entries for animations we're not aware of
				if ( anim != null )
				{
					anim.Apply( this, animState.Time, animState.Weight, _blendMode == SkeletalAnimBlendMode.Cumulative, 1.0f );
				}
			} // foreach
		}

		/// <summary>
		///    Sets the current position / orientation to be the 'binding pose' ie the layout in which
		///    bones were originally bound to a mesh.
		/// </summary>
		public virtual void SetBindingPose()
		{
			// update the derived transforms
			UpdateTransforms();

			// set all bones back to their binding pose
			for ( var i = 0; i < boneList.Count; i++ )
			{
				boneList.Values[ i ].SetBindingPose();
			}
		}

		/// <summary>
		///		Updates all the derived transforms in the skeleton.
		/// </summary>
		public virtual void UpdateTransforms()
		{
			for ( var i = 0; i < rootBones.Count; i++ )
			{
				rootBones[ i ].Update( true, false );
			}
		}

		/// <summary>
		///   TODO: should this replace an existing attachment point with the same name?
		/// </summary>
		/// <param name="name"></param>
		/// <param name="parentHandle"></param>
		/// <param name="rotation"></param>
		/// <param name="translation"></param>
		/// <returns></returns>
		public virtual AttachmentPoint CreateAttachmentPoint( string name, ushort parentHandle, Quaternion rotation,
		                                                      Vector3 translation )
		{
			var parentBone = boneList[ parentHandle ];
			var ap = new AttachmentPoint( name, parentBone.Name, rotation, translation );
			attachmentPoints.Add( ap );
			return ap;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fileName"></param>
		public void DumpContents( string fileName )
		{
			var fs = File.Open( fileName, FileMode.Create );
			var writer = new StreamWriter( fs );
			writer.AutoFlush = true;

			writer.WriteLine( "-= Debug output of skeleton  {0} =-", Name );
			writer.WriteLine( "" );
			writer.WriteLine( "== Bones ==" );
			writer.WriteLine( "Number of bones: {0}", boneList.Count );

			var q = new Quaternion();
			Real angle = 0;
			var axis = new Vector3();

			// write each bone out
			foreach ( var bone in boneList.Values )
			{
				writer.WriteLine( "-- Bone {0} --", bone.Handle );
				writer.Write( "Position: {0}", bone.Position );
				q = bone.Orientation;
				writer.Write( "Rotation: {0}", q );
				q.ToAngleAxis( ref angle, ref axis );
				writer.Write( " = {0} radians around axis {1}", angle, axis );
				writer.WriteLine( "" );
				writer.WriteLine( "" );
			}

			writer.WriteLine( "== Animations ==" );
			writer.WriteLine( "Number of animations: {0}", animationList.Count );

			// animations
			foreach ( var anim in animationList.Values )
			{
				writer.WriteLine( "-- Animation '{0}' (length {1}) --", anim.Name, anim.Length );
				writer.WriteLine( "Number of tracks: {0}", anim.NodeTracks.Count );

				// tracks
				foreach ( var track in anim.NodeTracks.Values )
				{
					writer.WriteLine( "  -- AnimationTrack {0} --", track.Handle );
					writer.WriteLine( "  Affects bone: {0}", ( (Bone)track.TargetNode ).Handle );
					writer.WriteLine( "  Number of keyframes: {0}", track.KeyFrames.Count );

					// key frames
					var kf = 0;
					for ( ushort i = 0; i < track.KeyFrames.Count; i++ )
					{
						var keyFrame = track.GetNodeKeyFrame( i );
						writer.WriteLine( "    -- KeyFrame {0} --", kf++ );
						writer.Write( "    Time index: {0}", keyFrame.Time );
						writer.WriteLine( "    Translation: {0}", keyFrame.Translate );
						q = keyFrame.Rotation;
						writer.Write( "    Rotation: {0}", q );
						q.ToAngleAxis( ref angle, ref axis );
						writer.WriteLine( " = {0} radians around axis {1}", angle, axis );
					}
				}
			}

			writer.Close();
			fs.Close();
		}

		#endregion Methods

		#region Implementation of Resource

		/// <summary>
		///    Generic load, called by SkeletonManager.
		/// </summary>
		protected override void load()
		{
			if ( IsLoaded )
			{
				return;
			}

			LogManager.Instance.Write( "Skeleton: Loading '{0}'...", Name );

			// load the skeleton file
			var data = ResourceGroupManager.Instance.OpenResource( Name, Group, true, this );

			// instantiate a new skeleton reader
			var reader = new OgreSkeletonSerializer();
			reader.ImportSkeleton( data, this );

			var extension = Path.GetExtension( Name );

			//TODO: Load any linked skeletons
			//LinkedSkeletonAnimSourceList::iterator i;
			//for (i = mLinkedSkeletonAnimSourceList.begin();
			//    i != mLinkedSkeletonAnimSourceList.end(); ++i)
			//{
			//    i->pSkeleton = SkeletonManager::getSingleton().load(
			//        i->skeletonName, mGroup);
			//}
		}

		/// <summary>
		///    Generic unload, called by SkeletonManager.
		/// </summary>
		protected override void unload()
		{
			// clear the internal lists
			animationList.Clear();
			boneList.Clear();
			namedBoneList.Clear();

			//base.Unload();
		}

		#endregion Implementation of Resource
	}
}