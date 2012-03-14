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
using System.Collections.Generic;

using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

#region Ogre Synchronization Information

// <ogresynchronization>
//     <file name="KeyFrame.h"   revision="1.9" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
//     <file name="KeyFrame.cpp" revision="1.13" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
// </ogresynchronization>

#endregion

namespace Axiom.Animating
{
	/// <summary>
	///		A key frame in an animation sequence defined by an AnimationTrack.
	/// </summary>
	///	<remarks>
	///		This class can be used as a basis for all kinds of key frames. 
	///		The unifying principle is that multiple KeyFrames define an 
	///		animation sequence, with the exact state of the animation being an 
	///		interpolation between these key frames.
	/// </remarks>
	public class KeyFrame
	{
		#region Protected member variables

		/// <summary>
		///		Animation track that this key frame belongs to.
		/// </summary>
		protected AnimationTrack parentTrack;

		/// <summary>
		///		Time of this keyframe.
		/// </summary>
		protected float time;

		#endregion Protected member variables

		#region Constructors

		/// <summary>
		///		Creates a new keyframe with the specified time.  
		///		Should really be created by <see cref="AnimationTrack.CreateKeyFrame"/> instead.
		/// </summary>
		/// <param name="parent">Animation track that this keyframe belongs to.</param>
		/// <param name="time">Time at which this keyframe begins.</param>
		public KeyFrame( AnimationTrack parent, float time )
		{
			this.parentTrack = parent;
			this.time = time;
		}

		#endregion Constructors

		#region Public properties

		/// <summary>
		///		Gets the time of this keyframe in the animation sequence.
		/// </summary>
		public float Time
		{
			get
			{
				return this.time;
			}
			set
			{
				this.time = value;
			}
		}

		#endregion
	}

	/// <summary>Specialised KeyFrame which stores any numeric value.</summary>
	public class NumericKeyFrame : KeyFrame
	{
		#region Protected member variables

		/// <summary>
		///		Object holding the numeric value
		/// </summary>
		protected Object numericValue;

		#endregion Protected member variables

		#region Constructors

		/// <summary>
		///		Creates a new keyframe with the specified time.  
		///		Should really be created by <see cref="AnimationTrack.CreateKeyFrame"/> instead.
		/// </summary>
		/// <param name="parent">Animation track that this keyframe belongs to.</param>
		/// <param name="time">Time at which this keyframe begins.</param>
		public NumericKeyFrame( AnimationTrack parent, float time )
			: base( parent, time ) { }

		#endregion

		#region Public properties

		/// <summary>
		///		Gets the time of this keyframe in the animation sequence.
		/// </summary>
		public Object NumericValue
		{
			get
			{
				return this.numericValue;
			}
			set
			{
				// hack for python scripting, which insists on passing in a double
				var tmpParent = parentTrack as NumericAnimationTrack;
				if ( tmpParent != null )
				{
					if ( tmpParent.TargetAnimable.Type == AnimableType.Real )
					{
						if ( value is double )
						{
							var d = (double)value;
							var tmp = (float)d;
							value = tmp;
						}
						else if ( value is int )
						{
							var i = (int)value;
							var tmp = (float)i;
							value = tmp;
						}
					}
				}
				this.numericValue = value;
			}
		}

		#endregion Public Properties
	}

	/// <summary>Specialised KeyFrame which stores a full transform.</summary>
	public class TransformKeyFrame : KeyFrame
	{
		#region Protected member variables

		/// <summary>
		///		Rotation at this keyframe.
		/// </summary>
		protected Quaternion rotation;

		/// <summary>
		///		Scale factor at this keyframe.
		/// </summary>
		protected Vector3 scale;

		/// <summary>
		///		Translation at this keyframe.
		/// </summary>
		protected Vector3 translate;

		#endregion Protected member variables

		#region Constructors

		/// <summary>
		///		Creates a new keyframe with the specified time.  
		///		Should really be created by <see cref="AnimationTrack.CreateKeyFrame"/> instead.
		/// </summary>
		/// <param name="parent">Animation track that this keyframe belongs to.</param>
		/// <param name="time">Time at which this keyframe begins.</param>
		public TransformKeyFrame( AnimationTrack parent, float time )
			: base( parent, time )
		{
			this.translate = new Vector3();
			this.scale = Vector3.UnitScale;
			this.rotation = Quaternion.Identity;
		}

		#endregion Constructors

		#region Public properties

		/// <summary>
		///		Sets the rotation applied by this keyframe.
		///		Use Quaternion methods to convert from angle/axis or Matrix3 if
		///		you don't like using Quaternions directly.
		/// </summary>
		public Quaternion Rotation
		{
			get
			{
				return this.rotation;
			}
			set
			{
				this.rotation = value;

				if ( parentTrack != null )
				{
					parentTrack.OnKeyFrameDataChanged();
				}
			}
		}

		/// <summary>
		///		Sets the scaling factor applied by this keyframe to the animable
		///		object at its time index.
		///		beware of supplying zero values for any component of this
		///		vector, it will scale the object to zero dimensions.
		/// </summary>
		public Vector3 Scale
		{
			get
			{
				return this.scale;
			}
			set
			{
				this.scale = value;

				if ( parentTrack != null )
				{
					parentTrack.OnKeyFrameDataChanged();
				}
			}
		}

		/// <summary>
		///		Sets the translation associated with this keyframe. 
		/// </summary>
		/// <remarks>
		///		The translation factor affects how much the keyframe translates (moves) its animable
		///		object at it's time index.
		///	</remarks>
		public Vector3 Translate
		{
			get
			{
				return this.translate;
			}
			set
			{
				this.translate = value;

				if ( parentTrack != null )
				{
					parentTrack.OnKeyFrameDataChanged();
				}
			}
		}

		#endregion Public Properties
	}


	/// <summary>Reference to a pose at a given influence level</summary>
	///	<remarks>
	///		Each keyframe can refer to many poses each at a given influence level.
	/// </remarks>
	public struct PoseRef
	{
		/// <summary>
		///     Influence level of the linked pose. 
		///     1.0 for full influence (full offset), 0.0 for no influence.
		/// </summary>
		public float influence;

		/// <summary>The linked pose index.</summary>
		///	<remarks>
		///	    The Mesh contains all poses for all vertex data in one list, both 
		///	    for the shared vertex data and the dedicated vertex data on submeshes.
		///	    The 'target' on the parent track must match the 'target' on the 
		///	    linked pose.
		/// </remarks>
		public ushort poseIndex;

		public PoseRef( ushort poseIndex, float influence )
		{
			this.poseIndex = poseIndex;
			this.influence = influence;
		}

		public ushort PoseIndex
		{
			get
			{
				return this.poseIndex;
			}
			set
			{
				this.poseIndex = value;
			}
		}

		public float Influence
		{
			get
			{
				return this.influence;
			}
			set
			{
				this.influence = value;
			}
		}
	}

	public class VertexMorphKeyFrame : KeyFrame
	{
		#region Protected member variables

		/// <summary>
		///		A list of the pose references for this frame
		/// </summary>
		protected HardwareVertexBuffer vertexBuffer;

		#endregion Protected member variables

		#region Constructors

		/// <summary>
		///		Creates a new keyframe with the specified time.  
		///		Should really be created by <see cref="AnimationTrack.CreateKeyFrame"/> instead.
		/// </summary>
		/// <param name="parent">Animation track that this keyframe belongs to.</param>
		/// <param name="time">Time at which this keyframe begins.</param>
		public VertexMorphKeyFrame( AnimationTrack parent, float time )
			: base( parent, time ) { }

		#endregion Constructors

		#region Public properties

		/// <summary>
		///		Gets or sets the vertex buffer
		/// </summary>
		public HardwareVertexBuffer VertexBuffer
		{
			get
			{
				return this.vertexBuffer;
			}
			set
			{
				this.vertexBuffer = value;
			}
		}

		#endregion
	}


	public class VertexPoseKeyFrame : KeyFrame
	{
		#region Protected member variables

		/// <summary>
		///		A list of the pose references for this frame
		/// </summary>
		protected List<PoseRef> poseRefs = new List<PoseRef>();

		#endregion Protected member variables

		#region Constructors

		/// <summary>
		///		Creates a new keyframe with the specified time.  
		///		Should really be created by <see cref="AnimationTrack.CreateKeyFrame"/> instead.
		/// </summary>
		/// <param name="parent">Animation track that this keyframe belongs to.</param>
		/// <param name="time">Time at which this keyframe begins.</param>
		public VertexPoseKeyFrame( AnimationTrack parent, float time )
			: base( parent, time ) { }

		#endregion Constructors

		#region Public properties

		/// <summary>
		///		Gets the time of this keyframe in the animation sequence.
		/// </summary>
		public List<PoseRef> PoseRefs
		{
			get
			{
				return this.poseRefs;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>Add a new pose reference.</summary>
		public void AddPoseReference( ushort poseIndex, float influence )
		{
			this.poseRefs.Add( new PoseRef( poseIndex, influence ) );
		}

		/// <summary>Update the influence of a pose reference.</summary>
		public void UpdatePoseReference( ushort poseIndex, float influence )
		{
			// Unfortunately, I can't modify PoseRef since it is a struct,
			// and when I access the list elements, I get a copy.
			// Because of this limitation, I remove and re-add the PoseRef
			RemovePoseReference( poseIndex );
			AddPoseReference( poseIndex, influence );
		}

		/// <summary>Remove reference to a given pose.</summary>
		/// <param name="poseIndex">The pose index (not the index of the reference)</param>
		public void RemovePoseReference( ushort poseIndex )
		{
			for ( int i = 0; i < this.poseRefs.Count; i++ )
			{
				if ( this.poseRefs[ i ].poseIndex == poseIndex )
				{
					this.poseRefs.RemoveAt( i );
					return;
				}
			}
		}

		/// <summary>Remove all pose references.</summary>
		public void RemoveAllPoseReferences()
		{
			this.poseRefs.Clear();
		}

		#endregion Public Methods
	}
}
