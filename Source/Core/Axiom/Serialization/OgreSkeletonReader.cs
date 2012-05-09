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
using System.IO;
using System.Text;
using Axiom.Core;
using Axiom.Animating;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Serialization
{
	/// <summary>
	/// 	Summary description for OgreSkeletonSerializer.
	/// </summary>
	public class OgreSkeletonSerializer : Serializer
	{
		#region Member variables

		private Skeleton skeleton;

		#endregion

		#region Constructors

		public OgreSkeletonSerializer()
		{
			version = "[Serializer_v1.10]";
		}

		#endregion

		#region Methods

		public void ImportSkeleton( Stream stream, Skeleton skeleton )
		{
			// store a local reference to the mesh for modification
			this.skeleton = skeleton;

			var reader = new BinaryReader( stream, System.Text.Encoding.UTF8 );

			// start off by taking a look at the header
			ReadFileHeader( reader );

			SkeletonChunkID chunkID = 0;

			while ( !IsEOF( reader ) )
			{
				chunkID = ReadChunk( reader );

				switch ( chunkID )
				{
					case SkeletonChunkID.Bone:
						ReadBone( reader );
						break;

					case SkeletonChunkID.BoneParent:
						ReadBoneParent( reader );
						break;

					case SkeletonChunkID.Animation:
						ReadAnimation( reader );
						break;

					case SkeletonChunkID.AttachmentPoint:
						ReadAttachmentPoint( reader );
						break;

					default:
						LogManager.Instance.Write(
							"Can only parse bones, parents, and animations at the top level during skeleton loading." );
						LogManager.Instance.Write( "Unexpected chunk: " + chunkID.ToString() );
						break;
				} // switch
			} // while

			// assume bones are stored in binding pose
			skeleton.SetBindingPose();
		}

		protected SkeletonChunkID ReadChunk( BinaryReader reader )
		{
			return (SkeletonChunkID)ReadFileChunk( reader );
		}

		/// <summary>
		///    Reads animation information from the file.
		/// </summary>
		protected void ReadAnimation( BinaryReader reader )
		{
			// name of the animation
			var name = ReadString( reader );

			// length in seconds of the animation
			var length = ReadFloat( reader );

			// create an animation from the skeleton
			var anim = this.skeleton.CreateAnimation( name, length );

			// keep reading all keyframes for this track
			if ( !IsEOF( reader ) )
			{
				var chunkID = ReadChunk( reader );
				while ( !IsEOF( reader ) && ( chunkID == SkeletonChunkID.AnimationTrack ) )
				{
					// read the animation track
					ReadAnimationTrack( reader, anim );
					// read the next chunk id
					// If we're not end of file get the next chunk ID
					if ( !IsEOF( reader ) )
					{
						chunkID = ReadChunk( reader );
					}
				}
				// backpedal to the start of the chunk
				if ( !IsEOF( reader ) )
				{
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		/// <summary>
		///    Reads an animation track.
		/// </summary>
		protected void ReadAnimationTrack( BinaryReader reader, Animation anim )
		{
			// read the bone handle to apply this track to
			var boneHandle = ReadUShort( reader );

			// get a reference to the target bone
			var targetBone = this.skeleton.GetBone( boneHandle );

			// create an animation track for this bone
			var track = anim.CreateNodeTrack( boneHandle, targetBone );

			// keep reading all keyframes for this track
			if ( !IsEOF( reader ) )
			{
				var chunkID = ReadChunk( reader );
				while ( !IsEOF( reader ) && ( chunkID == SkeletonChunkID.KeyFrame ) )
				{
					// read the key frame
					ReadKeyFrame( reader, track );
					// read the next chunk id
					// If we're not end of file get the next chunk ID
					if ( !IsEOF( reader ) )
					{
						chunkID = ReadChunk( reader );
					}
				}
				// backpedal to the start of the chunk
				if ( !IsEOF( reader ) )
				{
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		/// <summary>
		///    Reads bone information from the file.
		/// </summary>
		protected void ReadBone( BinaryReader reader )
		{
			// bone name
			var name = ReadString( reader );

			var handle = ReadUShort( reader );

			// create a new bone
			var bone = this.skeleton.CreateBone( name, handle );

			// read and set the position of the bone
			var position = ReadVector3( reader );
			bone.Position = position;

			// read and set the orientation of the bone
			var q = ReadQuat( reader );
			bone.Orientation = q;
		}

		/// <summary>
		///    Reads bone information from the file.
		/// </summary>
		protected void ReadBoneParent( BinaryReader reader )
		{
			// all bones should have been created by this point, so this establishes the heirarchy
			Bone child, parent;
			ushort childHandle, parentHandle;

			// child bone
			childHandle = ReadUShort( reader );

			// parent bone
			parentHandle = ReadUShort( reader );

			// get references to father and son bones
			parent = this.skeleton.GetBone( parentHandle );
			child = this.skeleton.GetBone( childHandle );

			// attach the child to the parent
			parent.AddChild( child );
		}

		/// <summary>
		///    Reads an animation track section.
		/// </summary>
		protected void ReadKeyFrame( BinaryReader reader, NodeAnimationTrack track )
		{
			var time = ReadFloat( reader );

			// create a new keyframe with the specified length
			var keyFrame = track.CreateNodeKeyFrame( time );

			// read orientation
			var rotate = ReadQuat( reader );
			keyFrame.Rotation = rotate;

			// read translation
			var translate = ReadVector3( reader );
			keyFrame.Translate = translate;

			// read scale if it is in there
			if ( currentChunkLength >= 50 )
			{
				var scale = ReadVector3( reader );
				keyFrame.Scale = scale;
			}
			else
			{
				keyFrame.Scale = Vector3.UnitScale;
			}
		}

		/// <summary>
		///    Reads bone information from the file.
		/// </summary>
		protected void ReadAttachmentPoint( BinaryReader reader )
		{
			// bone name
			var name = ReadString( reader );

			var boneHandle = ReadUShort( reader );

			// read and set the position of the bone
			var position = ReadVector3( reader );

			// read and set the orientation of the bone
			var q = ReadQuat( reader );

			// create the attachment point
			var ap = this.skeleton.CreateAttachmentPoint( name, boneHandle, q, position );
		}

		public void ExportSkeleton( Skeleton skeleton, string fileName )
		{
			this.skeleton = skeleton;
			var stream = new FileStream( fileName, FileMode.Create );
			try
			{
				var writer = new BinaryWriter( stream );
				WriteFileHeader( writer, version );
				WriteSkeleton( writer );
			}
			finally
			{
				if ( stream != null )
				{
					stream.Close();
				}
			}
		}

		protected void WriteSkeleton( BinaryWriter writer )
		{
			for ( ushort i = 0; i < this.skeleton.BoneCount; ++i )
			{
				var bone = this.skeleton.GetBone( i );
				WriteBone( writer, bone );
			}

			for ( ushort i = 0; i < this.skeleton.BoneCount; ++i )
			{
				var bone = this.skeleton.GetBone( i );
				if ( bone.Parent != null )
				{
					WriteBoneParent( writer, bone, (Bone)bone.Parent );
				}
			}

			foreach ( var anim in this.skeleton.Animations )
			{
				WriteAnimation( writer, anim );
			}

			for ( var i = 0; i < this.skeleton.AttachmentPoints.Count; ++i )
			{
				var ap = this.skeleton.AttachmentPoints[ i ];
				WriteAttachmentPoint( writer, ap, this.skeleton.GetBone( ap.ParentBone ) );
			}
		}

		protected void WriteBone( BinaryWriter writer, Bone bone )
		{
			var start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, SkeletonChunkID.Bone, 0 );

			WriteString( writer, bone.Name );
			WriteUShort( writer, bone.Handle );
			WriteVector3( writer, bone.Position );
			WriteQuat( writer, bone.Orientation );
			if ( bone.Scale != Vector3.UnitScale )
			{
				WriteVector3( writer, bone.Scale );
			}

			var end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, SkeletonChunkID.Bone, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteBoneParent( BinaryWriter writer, Bone bone, Bone parent )
		{
			var start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, SkeletonChunkID.BoneParent, 0 );

			WriteUShort( writer, bone.Handle );
			WriteUShort( writer, parent.Handle );

			var end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, SkeletonChunkID.BoneParent, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteAnimation( BinaryWriter writer, Animation anim )
		{
			var start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, SkeletonChunkID.Animation, 0 );

			WriteString( writer, anim.Name );
			WriteFloat( writer, anim.Length );

			foreach ( var track in anim.NodeTracks.Values )
			{
				WriteAnimationTrack( writer, track );
			}

			var end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, SkeletonChunkID.Animation, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteAnimationTrack( BinaryWriter writer, NodeAnimationTrack track )
		{
			var start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, SkeletonChunkID.AnimationTrack, 0 );

			WriteUShort( writer, (ushort)track.Handle );
			for ( ushort i = 0; i < track.KeyFrames.Count; i++ )
			{
				var keyFrame = track.GetNodeKeyFrame( i );
				WriteKeyFrame( writer, keyFrame );
			}
			var end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, SkeletonChunkID.AnimationTrack, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteKeyFrame( BinaryWriter writer, TransformKeyFrame keyFrame )
		{
			var start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, SkeletonChunkID.KeyFrame, 0 );

			WriteFloat( writer, keyFrame.Time );
			WriteQuat( writer, keyFrame.Rotation );
			WriteVector3( writer, keyFrame.Translate );
			if ( keyFrame.Scale != Vector3.UnitScale )
			{
				WriteVector3( writer, keyFrame.Scale );
			}

			var end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, SkeletonChunkID.KeyFrame, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteAttachmentPoint( BinaryWriter writer, AttachmentPoint ap, Bone bone )
		{
			var start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, SkeletonChunkID.AttachmentPoint, 0 );

			WriteString( writer, ap.Name );
			WriteUShort( writer, bone.Handle );
			WriteVector3( writer, ap.Position );
			WriteQuat( writer, ap.Orientation );

			var end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, SkeletonChunkID.AttachmentPoint, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		#endregion Methods
	}

	/// <summary>
	///    Chunk ID's that can be found within the Ogre .skeleton format.
	/// </summary>
	public enum SkeletonChunkID
	{
		Header = 0x1000,
		Bone = 0x2000,
		BoneParent = 0x3000,
		Animation = 0x4000,
		AnimationTrack = 0x4100,
		KeyFrame = 0x4110,
		// TODO: AnimationLink = 0x5000,
		// Multiverse Addition
		AttachmentPoint = 0x6000,
	}
}