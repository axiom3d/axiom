using System;
using System.Reflection;
using System.ComponentModel;

namespace RealmForge.Scripting
{
    /// <summary>
    /// Represents the base class for components which provide their public fields as properties so that they can be edited by the ProperyGrid as well.
    /// </summary>
    public class FieldEditableComponent : ICustomTypeDescriptor
    {
        #region Inner Classes

        private class FilterCache
        {
            public Attribute[] Attributes;
            public PropertyDescriptorCollection FilteredProperties;
            public bool IsValid( Attribute[] other )
            {
                if ( other == null || Attributes == null )
                    return false;

                if ( Attributes.Length != other.Length )
                    return false;

                for ( int i = 0; i < other.Length; i++ )
                {
                    if ( !Attributes[i].Match( other[i] ) )
                        return false;
                }

                return true;
            }
        }
        #endregion

        #region Fields and Properties
        private PropertyDescriptorCollection propCache;
        private FilterCache filterCache;
        #endregion

        #region Constructors

        public FieldEditableComponent()
        {
        }
        #endregion

        #region Public Methods

        public object GetPropertyOwner( PropertyDescriptor pd )
        {
            return this; // properties belong to the this object
        }


        public PropertyDescriptorCollection GetProperties()
        {
            return GetProperties( null );
        }

        public PropertyDescriptorCollection GetProperties(
            Attribute[] attributes )
        {
            bool filtering = ( attributes != null && attributes.Length > 0 );
            PropertyDescriptorCollection props = propCache;
            FilterCache cache = filterCache;

            // Use a cached version if possible
            if ( filtering && cache != null && cache.IsValid( attributes ) )
                return cache.FilteredProperties;
            else if ( !filtering && props != null )
                return props;

            // Create the property collection and filter
            props = new PropertyDescriptorCollection( null );
            foreach ( PropertyDescriptor prop in
                TypeDescriptor.GetProperties(
                this, attributes, true ) )
            {
                props.Add( prop );
            }
            foreach ( FieldInfo field in this.GetType().GetFields() )
            {
                FieldPropertyDescriptor fieldDesc = new FieldPropertyDescriptor( field );
                if ( ( !filtering || fieldDesc.Attributes.Contains( attributes ) && fieldDesc.IsBrowsable ) )
                {
                    props.Add( fieldDesc );
                }
            }

            // Store the computed properties
            if ( filtering )
            {
                cache = new FilterCache();
                cache.Attributes = attributes;
                cache.FilteredProperties = props;
                filterCache = cache;
            }
            else
                propCache = props;

            return props;
        }


        #region Delegated to TypeDescriptor

        public AttributeCollection GetAttributes()
        {
            // Gets the attributes of the this object
            return TypeDescriptor.GetAttributes( this, true );
        }

        public string GetClassName()
        {
            // Gets the class name of the this object
            return TypeDescriptor.GetClassName( this, true );
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter( this );
        }

        public EventDescriptorCollection GetEvents( Attribute[] attributes )
        {
            return TypeDescriptor.GetEvents( this, attributes );
        }

        public EventDescriptorCollection GetEvents()
        {
            return GetEvents( null );
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName( this );
        }

        public object GetEditor( Type editorBaseType )
        {
            return TypeDescriptor.GetEditor( this, editorBaseType );
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty( this );
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent( this );
        }

        #endregion
        #endregion

    }
}
