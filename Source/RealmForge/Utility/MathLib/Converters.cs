using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Collections;
using System.Globalization;
using System.Reflection;
using RealmForge.Scripting;

namespace Axiom.MathLib
{
    public class AxisAlignedBoxConverter : ParsingExpandingTypeConverterBase
    {

        #region Properties
        public override Type[] ConstructorArgumentTypes
        {
            get
            {
                return new Type[] { typeof( Vector3 ), typeof( Vector3 ) };
            }
        }
        public override string[] PropertyNames
        {
            get
            {
                return new string[] { "Center", "Minimum", "Maximum", "Corners", "IsNull" };
            }
        }
        public override Type TargetType
        {
            get
            {
                return typeof( AxisAlignedBox );
            }
        }
        #endregion

        #region Methods
        public override object[] GetConstructorArguments( object value )
        {
            AxisAlignedBox box = (AxisAlignedBox)value;
            return new object[] { box.Minimum, box.Maximum };
        }
        public override object Parse( string text, CultureInfo culture )
        {
            return AxisAlignedBox.Parse( text );
        }
        public override string GetParsableString( object value )
        {
            return ( (AxisAlignedBox)value ).ToString();
        }
        public override object CreateInstance( ITypeDescriptorContext context, IDictionary vals )
        {
            return new AxisAlignedBox( (Vector3)vals["Minimum"], (Vector3)vals["Maximum"] );
        }

        #endregion

    }

    public class Vector3Converter : ParsingExpandingTypeConverterBase
    {

        #region Properties
        public override Type[] ConstructorArgumentTypes
        {
            get
            {
                return new Type[] { typeof( float ), typeof( float ), typeof( float ) };
            }
        }
        public override string[] PropertyNames
        {
            get
            {
                return new string[] { "x", "y", "z" };
            }
        }
        public override Type TargetType
        {
            get
            {
                return typeof( Vector3 );
            }
        }
        #endregion

        #region Methods
        public override object[] GetConstructorArguments( object value )
        {
            return ( (Vector3)value ).ToObjectArray();
        }
        public override object Parse( string text, CultureInfo culture )
        {
            return Vector3.Parse( text );
        }
        public override string GetParsableString( object value )
        {
            return ( (Vector3)value ).ToString();
        }
        public override object CreateInstance( ITypeDescriptorContext context, IDictionary vals )
        {
            return new Vector3( (float)vals["x"], (float)vals["y"], (float)vals["z"] );
        }
        public override PropertyDescriptorCollection GetProperties( ITypeDescriptorContext context, object value, Attribute[] attributes )
        {
            return ( (ICustomTypeDescriptor)value ).GetProperties();
        }



        #endregion

    }
}
