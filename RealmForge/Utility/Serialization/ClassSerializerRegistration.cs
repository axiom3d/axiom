using System;
using System.IO;
using System.Reflection;
using RealmForge;
using RealmForge.Reflection;

namespace RealmForge.Serialization
{
    /// <summary>
    /// Summary description for ClassSerializerRegistration.
    /// </summary>
    [SerializedClass( "classReg" )]
    public class ClassSerializerRegistration
    {

        #region Fields
        [Serialized( "type", true )]
        public string TargetClassName;
        [Serialized( "typeAssembly", true )]
        public string TargetClassAssembly;

        [Serialized( "helper", true )]
        public string HelperClassName;
        [Serialized( "helperMode", true )]
        public string SerializationMode;
        [Serialized( "helperAssembly", true )]
        public string HelperAssembly;
        #endregion

        #region Constructors

        public ClassSerializerRegistration()
            : this( null )
        {
        }

        public ClassSerializerRegistration( string type )
            : this( type, null )
        {
        }

        public ClassSerializerRegistration( string type, string assembly )
            : this( type, assembly, null, null, null )
        {
        }

        public ClassSerializerRegistration( string type, string assembly, string helperType, string helperAssembly, string helperMode )
        {
            this.TargetClassName = type;
            this.TargetClassAssembly = assembly;
            this.HelperClassName = helperType;
            this.HelperAssembly = helperAssembly;
            this.SerializationMode = helperMode;
        }
        #endregion

        #region Methods

        public object GetHelperClass()
        {

            if ( ( SerializationMode == "Parser" || SerializationMode == "Factory" )
                && HelperClassName != string.Empty && HelperClassName != null )
            {
                if ( HelperAssembly == string.Empty )
                {
                    HelperAssembly = null;
                }
                return Reflector.CreateClassInstance( HelperAssembly, HelperClassName );
            }
            return null;

        }

        public void Register( ClassSerializationInfoCollection classDefs )
        {
            bool helperFailed = false;
            Type type = Reflector.GetTypeFrom( this.TargetClassAssembly, this.TargetClassName );
            if ( type != null )
            {
                if ( SerializationMode == "Parser" )
                {
                    object h = GetHelperClass();
                    if ( h != null )
                    {
                        classDefs.RegisterType( type, (IObjectParser)h );
                    }
                    else
                    {
                        helperFailed = true;
                    }
                }
                else if ( SerializationMode == "Factory" )
                {
                    object h2 = GetHelperClass();
                    if ( h2 != null )
                    {
                        classDefs.RegisterType( type, (IObjectFactory)h2 );
                    }
                    else
                    {
                        helperFailed = true;
                    }
                }
                else
                {
                    classDefs.RegisterType( type );
                }
            }
            else
            {
                throw new GeneralException( "Failed to register serialized class information for type {0} in assembly {1}, type could not be found.", TargetClassName, TargetClassAssembly );
            }

            if ( helperFailed )
            {
                throw new GeneralException( "Could not find Helper class Type {0} in assembly {1} to register for the serialization of Type {2} in assembly {3}.", HelperClassName, this.HelperAssembly, TargetClassName, TargetClassAssembly );
            }
        }
        #endregion

    }
}
