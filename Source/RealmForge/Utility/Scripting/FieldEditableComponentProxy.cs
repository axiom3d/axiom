using System;
using System.Reflection;
using System.ComponentModel;

namespace RealmForge.Scripting
{
    public class FieldEditableComponentProxy : ICustomTypeDescriptor
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
        private object target; // object to be described
        private PropertyDescriptorCollection propCache;
        private FilterCache filterCache;
        public object Target
        {
            get
            {
                return target;
            }
            set
            {
                target = value;
            }
        }
        #endregion

        #region Constructors

        public FieldEditableComponentProxy( object target )
        {
            if ( target == null )
                throw new ArgumentNullException( "target" );
            this.target = target;
        }
        #endregion

        #region Public Methods

        public object GetPropertyOwner( PropertyDescriptor pd )
        {
            return target; // properties belong to the target object
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
                target, attributes, true ) )
            {
                props.Add( prop );
            }
            foreach ( FieldInfo field in target.GetType().GetFields() )
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
            // Gets the attributes of the target object
            return TypeDescriptor.GetAttributes( target, true );
        }

        public string GetClassName()
        {
            // Gets the class name of the target object
            return TypeDescriptor.GetClassName( target, true );
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter( target );
        }

        public EventDescriptorCollection GetEvents( Attribute[] attributes )
        {
            return TypeDescriptor.GetEvents( target, attributes );
        }

        public EventDescriptorCollection GetEvents()
        {
            return GetEvents( null );
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName( target );
        }

        public object GetEditor( Type editorBaseType )
        {
            return TypeDescriptor.GetEditor( target, editorBaseType );
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty( target );
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent( target );
        }

        #endregion
        #endregion

    }
}
