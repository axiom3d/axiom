using System;
using System.IO;
using System.Text;

using Axiom.MathLib;

namespace Axiom
{
    /// <summary>
    /// 	Summary description for OgreSkeletonReader.
    /// </summary>
    public class OgreSkeletonReader : BinaryReader
    {
        #region Member variables

        const int CHUNK_OVERHEAD_SIZE = 6;
        const string VERSION = "[Serializer_v1.10]";

        private Skeleton skeleton;
        private int currentChunkLength;

        #endregion

        #region Constructors

        public OgreSkeletonReader( Stream data )
            : base( data )
        {
        }

        #endregion

        #region Methods

        public void Import( Skeleton skeleton )
        {
            // store a local reference to the mesh for modification
            this.skeleton = skeleton;

            // start off by taking a look at the header
            ReadFileHeader();

            try
            {
                SkeletonChunkID chunkID = 0;

                bool parse = true;

                while ( parse )
                {
                    chunkID = ReadChunk();

                    switch ( chunkID )
                    {
                        case SkeletonChunkID.Bone:
                            ReadBone();
                            break;

                        case SkeletonChunkID.BoneParent:
                            ReadBoneParent();
                            break;

                        case SkeletonChunkID.Animation:
                            ReadAnimation();
                            break;

                        default:
                            System.Diagnostics.Trace.Write( "Can only parse bones, parents, and animations at the top level during skeleton loading." );
                            parse = false;
                            break;
                    } // switch
                } // while
            }
            catch ( EndOfStreamException )
            {
                // assume bones are stored in binding pose
                skeleton.SetBindingPose();
            }
        }

        /// <summary>
        ///    Reads animation information from the file.
        /// </summary>
        protected void ReadAnimation()
        {
            // name of the animation
            string name = ReadString( '\n' );

            // length in seconds of the animation
            float length = ReadSingle();

            // create an animation from the skeleton
            Animation anim = skeleton.CreateAnimation( name, length );

            // read the first chunk
            SkeletonChunkID chunkId = ReadChunk();

            // continue while we still have animation tracks to read
            while ( chunkId == SkeletonChunkID.AnimationTrack )
            {
                // read the animation track
                ReadAnimationTrack( anim );

                // read the next chunk id
                chunkId = ReadChunk();
            } // while

            // move back to the beginning of last chunk read so it can be read by the calling method
            Seek( -CHUNK_OVERHEAD_SIZE );
        }

        /// <summary>
        ///    Reads an animation track.
        /// </summary>
        protected void ReadAnimationTrack( Animation anim )
        {
            // read the bone handle to apply this track to
            short boneHandle = ReadInt16();

            // get a reference to the target bone
            Bone targetBone = skeleton.GetBone( (ushort)boneHandle );

            // create an animation track for this bone
            AnimationTrack track = anim.CreateTrack( boneHandle, targetBone );

            // read the first chunkId
            SkeletonChunkID chunkId = ReadChunk();

            // keep reading all keyframes for this track
            while ( chunkId == SkeletonChunkID.KeyFrame )
            {
                // read the key frame
                ReadKeyFrame( track );

                // read the next chunk id
                chunkId = ReadChunk();
            }

            // move back to the beginning of last chunk read so it can be read by the calling method
            Seek( -CHUNK_OVERHEAD_SIZE );
        }

        /// <summary>
        ///    Reads bone information from the file.
        /// </summary>
        protected void ReadBone()
        {
            // bone name
            string name = ReadString( '\n' );

            short handle = ReadInt16();

            // create a new bone
            Bone bone = skeleton.CreateBone( name, (ushort)handle );

            // read and set the position of the bone
            Vector3 position = ReadVector3();
            bone.Position = position;

            // read and set the orientation of the bone
            Quaternion q = ReadQuat();
            bone.Orientation = q;
        }

        /// <summary>
        ///    Reads bone information from the file.
        /// </summary>
        protected void ReadBoneParent()
        {
            // all bones should have been created by this point, so this establishes the heirarchy
            Bone child, parent;
            short childHandle, parentHandle;

            // child bone
            childHandle = ReadInt16();

            // parent bone
            parentHandle = ReadInt16();

            // get references to father and son bones
            parent = skeleton.GetBone( (ushort)parentHandle );
            child = skeleton.GetBone( (ushort)childHandle );

            // attach the child to the parent
            parent.AddChild( child );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected SkeletonChunkID ReadChunk()
        {
            // get the chunk id
            short id = ReadInt16();

            // read the length for this chunk
            currentChunkLength = ReadInt32();

            return (SkeletonChunkID)id;
        }

        /// <summary>
        /// 
        /// </summary>
        protected void ReadFileHeader()
        {
            short headerID = 0;

            // read the header ID
            headerID = ReadInt16();

            // better hope this is the header
            if ( headerID == (short)SkeletonChunkID.Header )
            {
                string version = this.ReadString( '\n' );
            }
            else
                throw new Exception( "Invalid skeleton file, no header found." );
        }

        /// <summary>
        ///    Reads an animation track section.
        /// </summary>
        /// <param name="track"></param>
        protected void ReadKeyFrame( AnimationTrack track )
        {
            float time = ReadSingle();

            // create a new keyframe with the specified length
            KeyFrame keyFrame = track.CreateKeyFrame( time );

            // read orientation
            Quaternion rotate = ReadQuat();
            keyFrame.Rotation = rotate;

            // read translation
            Vector3 translate = ReadVector3();
            keyFrame.Translate = translate;

            // read scale?
            if ( currentChunkLength == 50 )
            {
                Vector3 scale = ReadVector3();
                keyFrame.Scale = scale;
            }
        }

        protected string ReadString( char delimiter )
        {
            StringBuilder sb = new StringBuilder();

            char c;

            // sift through each character until we hit the delimiter
            while ( ( c = base.ReadChar() ) != delimiter )
                sb.Append( c );

            // return the accumulated string
            return sb.ToString();
        }

        /// <summary>
        ///    Reads and returns a Quaternion.
        /// </summary>
        /// <returns></returns>
        protected Quaternion ReadQuat()
        {
            Quaternion quat = new Quaternion();

            quat.x = ReadSingle();
            quat.y = ReadSingle();
            quat.z = ReadSingle();
            quat.w = ReadSingle();

            return quat;
        }

        /// <summary>
        ///    Reads and returns a Vector3 structure.
        /// </summary>
        /// <returns></returns>
        protected Vector3 ReadVector3()
        {
            Vector3 vector = new Vector3();

            vector.x = ReadSingle();
            vector.y = ReadSingle();
            vector.z = ReadSingle();

            return vector;
        }

        /// <summary>
        ///    Moves forward (or backward if negative length is supplied) in the file.
        /// </summary>
        /// <param name="length"></param>
        protected void Seek( long length )
        {
            if ( base.BaseStream is FileStream )
            {
                FileStream fs = base.BaseStream as FileStream;

                fs.Seek( length, SeekOrigin.Current );
            }
            else if ( base.BaseStream is MemoryStream )
            {
                MemoryStream ms = base.BaseStream as MemoryStream;

                ms.Seek( length, SeekOrigin.Current );
            }
            else
                throw new Exception( "Unsupported stream type used to load a mesh." );
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
    }
}
