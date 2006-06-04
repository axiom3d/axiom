#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
using System.Collections;
using System.IO;

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    ///		Class for serialising mesh data to/from an OGRE .mesh file.
    /// </summary>
    /// <remarks>
    ///		This class allows exporters to write OGRE .mesh files easily, and allows the
    ///		OGRE engine to import .mesh files into instatiated OGRE Meshes.
    ///		<p/>
    ///		It's important to realize that this exporter uses OGRE terminology. In this context,
    ///		'Mesh' means a top-level mesh structure which can actually contain many SubMeshes, each
    ///		of which has only one Material. Modelling packages may refer to these differently, for
    ///		example in Milkshape, it says 'Model' instead of 'Mesh' and 'Mesh' instead of 'SubMesh', 
    ///		but the theory is the same.
    /// </remarks>
    public sealed class MeshSerializer : Serializer
    {
        #region Fields

        /// <summary>
        ///		Lookup table holding the various mesh serializer versions.
        /// </summary>
        private Hashtable implementations = new Hashtable();

        /// <summary>
        ///		Current version string.
        /// </summary>
        private static string currentVersion = "[MeshSerializer_v1.30]";

        #endregion Fields

        #region Constructor

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public MeshSerializer()
        {
            // add the supported .mesh versions
            implementations.Add( "[MeshSerializer_v1.10]", new MeshSerializerImplv11() );
            implementations.Add( "[MeshSerializer_v1.20]", new MeshSerializerImplv12() );
            implementations.Add( currentVersion, new MeshSerializerImpl() );
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        ///		Exports a mesh to the file specified.
        /// </summary>
        /// <param name="mesh">Reference to the mesh to export.</param>
        /// <param name="fileName">The destination filename.</param>
        public void ExportMesh( Mesh mesh, string fileName )
        {
            throw new NotImplementedException( "Mesh exporting is currently not implemented." );
        }

        /// <summary>
        ///		Imports mesh data from a .mesh file.
        /// </summary>
        /// <param name="stream">The stream holding the .mesh data. Must be initialised (pos at the start of the buffer).</param>
        /// <param name="mesh">Reference to the Mesh object which will receive the data. Should be blank already.</param>
        public void ImportMesh( Stream stream, Mesh mesh )
        {
            BinaryReader reader = new BinaryReader( stream );

            // read the header ID
            ushort headerID = ReadUShort( reader );

            if ( headerID != (ushort)MeshChunkID.Header )
            {
                throw new AxiomException( "File header not found." );
            }

            // read version
            string fileVersion = ReadString( reader );

            // set jump back to the start of the reader
            Seek( reader, 0, SeekOrigin.Begin );

            // barf if there specified version is not supported
            if ( !implementations.ContainsKey( fileVersion ) )
            {
                throw new AxiomException( "Cannot find serializer implementation for version '{0}'.", fileVersion );
            }

            LogManager.Instance.Write( "Mesh: Loading '{0}'...", mesh.Name );

            // call implementation
            MeshSerializerImpl serializer = (MeshSerializerImpl)implementations[fileVersion];
            serializer.ImportMesh( stream, mesh );

            // warn on old version of mesh
            if ( fileVersion != currentVersion )
            {
                LogManager.Instance.Write( "WARNING: {0} is an older format ({1}); you should upgrade it as soon as possible using the OgreMeshUpdate tool.", mesh.Name, fileVersion );
            }
        }

        #endregion Methods
    }
}
