using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;


namespace TestNamespace
{

    /// <summary> 
    /// Tests whether sandcastle can handle all c# tags as defined at http://msdn2.microsoft.com/en-us/library/5ast78ax.aspx.
    /// Comments of method "Increment (int step)" include almost all tags.
    /// Method "Swap" is used to test generics tags, such as "typeparam".
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    [Serializable()]
    public class StoredNumber
    {

        /// <summary>Initializes the stored number class with a starting value.</summary>
        public StoredNumber(int value)
        {
            number = value;
        }

        private int number;

        /// <preliminary>
        /// <para>This method is just for testing right now. It might be removed in the near future</para>
        /// </preliminary>
        public void PreliminaryTest()
        {
        }

        /// <overloads>
        /// <summary>dec operation</summary>
        /// </overloads>
        public void Dec()
        {
            number--;
        }

        public void Dec(int step)
        {
            number -= step;
        }

        /// <summary><c>Increment</c> method incriments the stored number by one. 
        /// <note type="caution">
        /// note description here
        ///</note>
        /// <preliminary/>
        /// </summary>        
        public void Increment()
        {
            number++;
        }

        /// <summary><c>Increment</c> method incriments the stored number by a specified <paramref name="step"/>. 
        /// <list type="number">
        /// <item>
        /// <description>Item 1.</description>
        /// </item>
        /// <item>
        /// <description>Item 2.</description>
        /// </item>
        /// </list>
        /// <para>see <see cref="System.Int32"/></para>
        /// <para>seealso <seealso cref="System.Int64"/></para>
        /// </summary>		
        /// <remarks>
        /// You may have some additional information about this class.
        /// </remarks>
        /// <example> This sample shows how to call the GetZero method.
        /// <code>
        /// class TestClass 
        /// {
        ///     static int Main() 
        ///     {
        ///         return GetZero();
        ///     }
        /// }
        /// </code>
        /// </example>
        ///
        /// <exception cref="System.Exception">Thrown when...</exception>
        /// <param name="step"> specified step</param>
        /// <permission cref="System.Security.PermissionSet">Everyone can access this method.</permission>
        /// <returns>Returns nothing</returns>
        /// <value>The Name property gets/sets the _name data member.</value>
        public void Increment(int step)
        {
            number = number + step;
        }


        /// <summary>
        /// <para><typeparam name="T">The element type to swap</typeparam></para>
        /// <para>Swap data of type <typeparamref name="T"/></para>
        /// </summary>
        public void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }



        /// <summary>Gets the stored number.</summary>
        public int Value
        {
            get
            {
                return (number);
            }
        }


        private int _myProp;


        ///<summary>Gets or sets a <see cref="T:System.Windows.Media.Brush"></see> used to fill the area between the borders of a 
        ///<see cref="T:System.Windows.Controls.InkCanvas"></see>.
        ///see <see langword="null"/> as reference
        ///</summary>
        public int MyProp
        {
            get { return _myProp; }
            set { _myProp = value; }
        }

        /// <include file='mydocs.xml' path='MyDocs/MyMembers[@name="test"]/*' />
        public void IncludeTest()
        {
        }

    }




}
