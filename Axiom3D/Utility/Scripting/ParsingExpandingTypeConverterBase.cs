using System;
using System.ComponentModel;
using System.Globalization;
using System.Collections;
using System.Reflection;
using System.ComponentModel.Design.Serialization;

namespace RealmForge.Scripting
{

    /// <summary>
    /// Represents the base class for Type Converters that when associated with a type or property using the TypeConverter attribute will allow
    /// it to be edited both as parsable text and a expandable/collapsable tree of properties just like the how Point and Size properties are edited using the PropertyGrid
    /// </summary>
    /// <remarks>You must override CreateInstance if there the TargetType is not a struct and there is no parameterless constructor</remarks>
    public abstract class ParsingExpandingTypeConverterBase : TypeConverter
    {
        #region Abstract Properties
        public abstract Type TargetType
        {
            get;
        }
        public abstract string[] PropertyNames
        {
            get;
        }
        public abstract Type[] ConstructorArgumentTypes
        {
            get;
        }
        #endregion

        #region Abstract Methods

        public abstract object Parse( string text, CultureInfo culture );
        public abstract string GetParsableString( object value );
        public abstract object[] GetConstructorArguments( object value );

        #endregion

        #region Methods
        #region Checking
        public override bool CanConvertFrom( ITypeDescriptorContext context, Type sourceType )
        {
            if ( sourceType == typeof( string ) )
                return true;
            return base.CanConvertFrom( context, sourceType );	//true if InstanceDescriptor
        }
        public override bool CanConvertTo( ITypeDescriptorContext context, Type destinationType )
        {
            if ( destinationType == typeof( InstanceDescriptor ) )
                return true;
            return base.CanConvertTo( context, destinationType );	//true if string
        }

        public override bool GetCreateInstanceSupported( ITypeDescriptorContext context )
        {
            return true;
        }
        public override bool GetPropertiesSupported( ITypeDescriptorContext context )
        {
            return true;
        }
        #endregion

        #region Conversion
        public override object ConvertFrom( ITypeDescriptorContext context, CultureInfo culture, object value )
        {
            if ( !( value is string ) )
            {
                return base.ConvertFrom( context, culture, value );
            }
            string text = (string)value;
            if ( value == null || text == string.Empty )
                return null;

            if ( culture == null )
                culture = CultureInfo.CurrentCulture;
            return Parse( text, culture );
        }
        public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType )
        {
            if ( destinationType == null )
                throw new ArgumentNullException( "There destination type cannot be null" );
            Type type = TargetType;
            if ( value != null && value.GetType() == type )
            {
                if ( destinationType == typeof( string ) )
                    return GetParsableString( value );
                if ( destinationType == typeof( InstanceDescriptor ) )
                {
                    ConstructorInfo constructor = type.GetConstructor( ConstructorArgumentTypes );
                    if ( constructor != null ) //if that constructor exists
                    {
                        return new InstanceDescriptor( constructor, GetConstructorArguments( value ) );
                    }
                }
            }

            return base.ConvertTo( context, culture, value, destinationType );
        }

        #endregion

        #region Creation and Properties

        public override object CreateInstance( ITypeDescriptorContext context, IDictionary propertyValues )
        {
            Type type = TargetType;
            object instance = Activator.CreateInstance( type );
            foreach ( DictionaryEntry entry in propertyValues )
            {
                string propertyName = (string)entry.Key;

                BindingFlags instanceFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                PropertyInfo prop = type.GetProperty( propertyName, instanceFlags );
                if ( prop != null )
                    prop.SetValue( instance, entry.Value, null );
                else
                {
                    FieldInfo f = type.GetField( propertyName, instanceFlags );
                    if ( f != null )
                        f.SetValue( instance, entry.Value );
                }

            }
            return instance;
        }

        public override PropertyDescriptorCollection GetProperties( ITypeDescriptorContext context, object value, Attribute[] attributes )
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties( TargetType, attributes );
            return properties.Sort( PropertyNames );

        }

        #endregion

        #endregion
    }
}
