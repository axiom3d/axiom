#region LGPL License
/*
This file is part of the RealmForge GDK.
Copyright (C) 2003-2004 Daniel L. Moorehead

The RealmForge GDK is a cross-platform game development framework and toolkit written in Mono/C# and powered by the Axiom 3D engine. It will allow for the rapid development of cutting-edge software and MMORPGs with advanced graphics, audio, and networking capabilities.

dan@xeonxstudios.com
http://xeonxstudios.com
http://sf.net/projects/realmforge

If you have or intend to contribute any significant amount of code or changes to RealmForge you must go have completed the Xeonx Studios Copyright Assignment.

RealmForge is free software; you can redistribute it and/or modify it under the terms of  the GNU Lesser General Public License as published by the Free Software Foundation; either version 2 or (at your option) any later version.

RealmForge is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the accompanying RealmForge License and GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with RealmForge; if not, write to the Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA.
*/
#endregion


using System;
using System.IO;
using System.Collections;
using System.Xml;
using System.Text;
using System.Runtime;
using System.Reflection;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using Ser = System.Xml.Serialization;
using RealmForge.Serialization;
using RealmForge.FileSystem;
using RealmForge;
using RealmForge.Reflection;

namespace RealmForge.Serialization
{
    /// <summary>
    /// The enum of diffrent formats that object can be serialized into
    /// </summary>
    public enum SerializeFormat
    {
        Xml,
        Binary,
        DotNetSoap,
        DotNetXml,
        DotNetBinary
    }

    /// <summary>
    /// Allow serialization and deserailzation of diffrent types of object
    /// to and from diffrent formats (XML, Binary, Soap) as well as less verbose, 
    /// version aware Custom Binary and XML formats
    /// </summary>
    /// <remarks>
    /// For data types that can easily be converted to and from a string, implement IParsable
    /// or register a IParser class with the type
    /// For custom classes where there is access to the source, apply an Serailized attribute to each
    /// serialized property choose the overloaded constructor for classes with multple versions
    /// in which the older data formats should still be able to be deserailized
    /// </remarks>
    public class Serializer
    {
        #region Fields
        protected XmlWriter xmlOut = null;
        protected Stream file = null;
        protected XmlReader xmlIn = null;
        protected XmlFormatter xmlSer = null;
        /// <summary>
        /// About 2.2x as slow while about .8x the size (for large 400mb files only)
        /// GZip is the default
        /// </summary>
        public bool UseBZip2 = false;
        public static string RegisteredClassesConfigPath = null;
        #endregion

        #region Constructors
        public Serializer()
        {
            xmlSer = new XmlFormatter();
        }
        #endregion

        #region Public Methods

        public object DeserializeFromString( string text )
        {
            return xmlSer.Deserialize( new XmlTextReader( new StringReader( text ) ) );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="file"></param>
        /// <param name="format"></param>
        public void SerializeData( object data, Stream file, SerializeFormat format, string password, bool compress )
        {
            if ( data == null || file == null )
                return;
            Stream file2 = null;
            Stream memStream = file;
            if ( password != null && password != string.Empty )
            {//if we are encrypting it
                //dont write it to file first
                file2 = file;//backup the actual file to write to it later
                memStream = file = new MemoryStream();//write the memory stream for now
            }

            //writes to the output stream which in turn write the compressed data to the underlying stream
            //compresses before encrypts

            //NOTE: you MUST compress before encryption becuase it will be near worthless afterwards
            //this may create some plain text or a header, but it removes other redundent text and causes less to proccess for the encryption algorithem
            if ( compress )
            {//NOTE: doesnt replace memStream
                Compressor.AssertExists();
                if ( this.UseBZip2 )
                    file = Compressor.Instance.WriteBZip2( file );
                else
                    file = Compressor.Instance.WriteGZip( file );
            }

            this.file = file;
            Type type = data.GetType();
            switch ( format )
            {
                case SerializeFormat.Binary:
                //TODO
                case SerializeFormat.Xml:
                    xmlSer.Serialize( file, data );
                    break;
                case SerializeFormat.DotNetBinary:
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.TypeFormat = FormatterTypeStyle.TypesWhenNeeded;
                    bin.Serialize( file, data );
                    break;
                case SerializeFormat.DotNetXml:
                    Ser.XmlSerializer xser = new Ser.XmlSerializer( type );
                    XmlTextWriter xw = new XmlTextWriter( file, null );
                    xw.Formatting = Formatting.Indented;
                    xw.Indentation = 1;
                    xw.IndentChar = '\t';
                    xser.Serialize( xw, data );
                    break;
                case SerializeFormat.DotNetSoap:
                    Errors.NotSupported( "Soap formatting is not supported as it is rarely used." );
                    /*
						SoapFormatter soap = new SoapFormatter();
						soap.TypeFormat = FormatterTypeStyle.TypesWhenNeeded;//No verbose type info
						//XsdString would be better, but requires an XSD file
						soap.Serialize(file,data);
						*/
                    break;
            }

            //if need to encrypt
            if ( file2 != null )
            {//now write the memory stream to file while encrypting it
                memStream.Position = 0;//go to startu
                Encrypter.Encrypt( memStream, file2, password, false );//write to file and close it
                            memStream.Close();
                            file = file2;
                    }

                    file.Flush();
            file.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="file"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public object DeserializeData( Type dataType, Stream file, SerializeFormat format, string password, bool decompress )
        {
            if ( file == null )
                return null;
            this.file = file;
            //decrypts first
            if ( password != null && password != string.Empty )
            {
                MemoryStream stream = new MemoryStream( (int)file.Length );//NOTE: MAY BE TO LARGE!!
                Encrypter.Decrypt( file, stream, password, false );
                file.Close();//close the file
                stream.Position = 0;
                file = stream;//now read from the decrypted stream
                StreamWriter sw = new StreamWriter( "ttt.txt" );
                sw.Write( new StreamReader( stream ).ReadToEnd() );
                sw.Flush();
                sw.Close();
            }
            if ( decompress )
            {//read indirectly so decompresses as it goes
                Compressor.AssertExists();
                if ( this.UseBZip2 )
                    file = Compressor.Instance.ReadBZip2( file );
                else
                    file = Compressor.Instance.ReadGZip( file );
            }
            object result = null;
            switch ( format )
            {
                case SerializeFormat.Binary:
                //TODO
                case SerializeFormat.Xml:
                    if ( dataType != null )
                    {
                        ClassSerializationInfoCollection.Instance.RegisterType( dataType );
                    }

                    result = xmlSer.Deserialize( file );
                    //TODO
                    break;
                case SerializeFormat.DotNetBinary:
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.TypeFormat = FormatterTypeStyle.TypesWhenNeeded;
                    result = bin.Deserialize( file );
                    break;
                case SerializeFormat.DotNetXml:
                    Ser.XmlSerializer xser = new Ser.XmlSerializer( dataType );
                    XmlReader xr = new XmlTextReader( file, null );
                    result = xser.Deserialize( xr );
                    break;
                case SerializeFormat.DotNetSoap:
                    Errors.NotSupported( "Soap formatting is not supported as it is rarely used." );
                    /*
					SoapFormatter soap = new SoapFormatter();
					soap.TypeFormat = FormatterTypeStyle.TypesWhenNeeded;//No verbose type info
					//XsdString would be better, but requires an XSD file
					result = soap.Deserialize(file);
					*/
                    break;
            }
            file.Close();
            return result;
        }

        public void Close()
        {
            if ( file != null )
            {
                file.Close();
            }
        }

        #endregion

                #region Singleton Implementation
                protected static Serializer instance = new Serializer();
                /// <summary>
                /// Gets the Singleton instance
        /// </summary>
        public static Serializer Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        #region Static Methods

        #region Deserialize
        public static object Deserialize( Type type, Stream data, SerializeFormat format, string password, bool decompress )
        {
            if ( data == null )
                Errors.ArgumentNull( "Cannot deserialize from a null stream" );
            object result = null;
            try
            {
                result = instance.DeserializeData( type, data, format, password, decompress );
            }
            catch ( Exception e )
            {
                try
                {
                    instance.Close();
                }
                catch ( Exception ex )
                {
                    Log.Warn( "Failed to close stream after deserialization has failed and it aborting it" );
                    Log.Write( ex );
                }
                Log.Warn( "Deserialization failed: Exception of type '{0}' occured while deserializing file in the {1} format.", e.GetType(), format );
                Log.Write( e );
            }
            return result;
        }

        public static object Deserialize( Type type, Stream data )
        {
            return Deserialize( type, data, SerializeFormat.Xml, null, false );
        }

        public static object Deserialize( Type type, string fileName )
        {
            return Deserialize( type, File.OpenRead( fileName ), SerializeFormat.Xml, null, false );
        }

        public static object Deserialize( Type type, string fileName, SerializeFormat format )
        {
            return Deserialize( type, File.OpenRead( fileName ), format, null, false );
        }


        public static object Deserialize( Type type, string fileName, SerializeFormat format, string password )
        {
            return Deserialize( type, File.OpenRead( fileName ), format, password, false );
        }

        public static object Deserialize( Type type, string fileName, SerializeFormat format, bool decompress )
        {
            return Deserialize( type, File.OpenRead( fileName ), format, null, decompress );
        }

        public static object Deserialize( Type type, string fileName, SerializeFormat format, string password, bool decompress )
        {
            return Deserialize( type, File.OpenRead( fileName ), format, password, decompress );
        }

        #endregion

        #region Serialize
        public static void Serialize( Stream stream, object data, SerializeFormat format, string password, bool compress )
        {
            try
            {
                ClassSerializationInfoCollection.Instance.RegisterType( data.GetType() );
                instance.SerializeData( data, stream, format, password, compress );
            }
            catch ( Exception e )
            {
                instance.Close();
                Log.Write( e );
                Log.Write( "Serialization failed: Exception of type '{0}' occured while serializing data to a stream using the {1} format.", e.GetType(), format );
            }
        }

        public static void Serialize( Stream stream, object data )
        {
            Serialize( stream, data, SerializeFormat.Xml );
        }

        public static void Serialize( Stream stream, object data, SerializeFormat format )
        {
            Serialize( stream, data, format, null, false );
        }

        public static void Serialize( string fileName, object data )
        {
            Serialize( File.Create( fileName ), data, SerializeFormat.Xml, null, false );
        }

        public static void Serialize( string fileName, object data, SerializeFormat format )
        {
            Serialize( File.Create( fileName ), data, format, null, false );
        }
        public static void Serialize( string fileName, object data, SerializeFormat format, string password )
        {
            Serialize( File.Create( fileName ), data, format, password, false );
        }

        public static void Serialize( string fileName, object data, SerializeFormat format, bool compress )
        {
            Serialize( File.Create( fileName ), data, format, null, compress );
        }

        public static void Serialize( string fileName, object data, SerializeFormat format, string password, bool compress )
        {
            Serialize( File.Create( fileName ), data, format, password, compress );
        }


        #endregion

        /// <summary>Registers all Serializable classes listed in the Serializer Config File.</summary>
        public static void RegisterClasses()
        {
            if ( RegisteredClassesConfigPath == null || RegisteredClassesConfigPath == string.Empty )
                return;
            // Get reference to global collection of Serializable classes
            ClassSerializationInfoCollection defs = ClassSerializationInfoCollection.Instance;

            // Register these two class manually, since they'll be needed for the operations below
            Log.StartDebugTaskGroup( "Registering critical classes for serialization" );
            defs.RegisterTypeIfNeeded( typeof( ArrayList ) );
            defs.RegisterTypeIfNeeded( typeof( ClassSerializerRegistration ) );
            Log.EndDebugTaskGroup();

            // Read List of Serializable class from XML file (deserialized ArrayList!)
            if ( RegisteredClassesConfigPath != null && RegisteredClassesConfigPath != string.Empty )
            {
                Log.StartDebugTaskGroup( "Registering classes for serialization as defined in {0}", RegisteredClassesConfigPath );

                ArrayList regs = (ArrayList)Deserialize( typeof( ArrayList ), RegisteredClassesConfigPath );
                if ( regs == null )
                {
                    throw new ApplicationException( "Failed to register to classes for serialization" );
                }
                // Add each class to the registered collection
                foreach ( ClassSerializerRegistration reg in regs )
                {
                    try
                    {
                        reg.Register( defs );
                        Log.DebugWrite( "Registered Serializable Class '{0}' in assembly '{1}'.", reg.TargetClassName, reg.TargetClassAssembly );
                    }
                    catch ( Exception e )
                    {
                        Log.Write( e );
                    }
                }
                Log.EndDebugTaskGroup();
            }

        }

        #endregion

    }
}
