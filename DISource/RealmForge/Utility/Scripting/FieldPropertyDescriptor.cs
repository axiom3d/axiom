using System;
using System.ComponentModel;
using System.Reflection;
namespace RealmForge.Scripting
{
    /// <summary>
    /// Describes a field of a type to allow its use with the properties that are retrieved by TypeDescriptor for use by the PropertyGrid
    /// </summary>
    /// <remarks>This is used by the FieldAndPropertyProxyTypeDescriptor to which adds Fields to the list of described properties.</remarks>
    public class FieldPropertyDescriptor : PropertyDescriptor
    {
        #region Fields and Properties
        private FieldInfo field;

        public FieldInfo Field
        {
            get
            {
                return field;
            }
        }

        public override Type ComponentType
        {
            get
            {
                return field.DeclaringType;
            }
        }

        public override Type PropertyType
        {
            get
            {
                return field.FieldType;
            }
        }
        #endregion

        #region Constructors
        public FieldPropertyDescriptor( object instance, string fieldName )
            : this( instance, fieldName, BindingFlags.Default )
        {
        }
        public FieldPropertyDescriptor( object instance, string fieldName, BindingFlags flags )
            : this( instance.GetType().GetField( fieldName, flags ) )
        {
        }
        public FieldPropertyDescriptor( FieldInfo field )
            : base( field.Name,
            (Attribute[])field.GetCustomAttributes( typeof( Attribute ), true ) )
        {
            this.field = field;
        }

        #endregion

        #region Overriden Methods

        public override bool Equals( object obj )
        {
            FieldPropertyDescriptor other = obj as FieldPropertyDescriptor;
            return other != null && other.field.Equals( field );
        }

        public override int GetHashCode()
        {
            return field.GetHashCode();
        }

        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public override void ResetValue( object component )
        {
        }

        public override bool CanResetValue( object component )
        {
            return false;
        }

        public override bool ShouldSerializeValue( object component )
        {
            return true;
        }

        public override object GetValue( object component )
        {
            return field.GetValue( component );
        }

        public override void SetValue( object component, object value )
        {
            field.SetValue( component, value );
            OnValueChanged( component, EventArgs.Empty );
        }
        #endregion
    }
}
