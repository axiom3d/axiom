using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace MeshPlugin
{
	/// <summary>
	/// Summary description for SubEntityPropertyTab.
	/// </summary>
	public class SubEntityPropertyTab : PropertyTab
	{
		public SubEntityPropertyTab()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public override string TabName 
		{
			get { return "Subentity Material Editor"; }
		}

		public override PropertyDescriptorCollection GetProperties(object component, Attribute[] attrs)
		{
			TypeConverter tc = TypeDescriptor.GetConverter(component);
			return TypeDescriptor.GetProperties(component, attrs);
				
			/*ArrayList propList = new ArrayList();
			propList.Add("Test 1");
			propList.Add("Test 2");

			// return the collection of PropertyDescriptors.
			PropertyDescriptor[] props = (PropertyDescriptor[])propList.ToArray(typeof(PropertyDescriptor));
			return new PropertyDescriptorCollection(props);*/
		}

	}
}
