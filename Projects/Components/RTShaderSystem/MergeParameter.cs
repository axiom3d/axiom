using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axiom.Components.RTShaderSystem
{
    class MergeParameter
    {
        #region Fields
        protected Parameter dstParameter; //Destination merged parameter.
        protected Parameter[] srcParameter = new Parameter[4]; //Soure parameter - 4 source at max 1,1,1,1 -> 4
        protected int[] srcParameterMask = new int[4]; //source parameter mask. OPM_ALL means all fields used, otherwise it is split source parameter.
        protected int[] dstParameterMask = new int[4]; //destination parameters mask. OPM_ALL means all fields used, otherwise it is split source parameter.
        protected int srcParameterCount;
        protected int usedFloatcount; 
        #endregion

        #region C'Tor
        public MergeParameter()
        { }
        #endregion

        #region Properties
        /// <summary>
        /// Add source parameter to this merged.
        /// </summary>
        /// <param name="srcParam"></param>
        /// <param name="mask"></param>
        public void AddSourceParameter(Parameter srcParam, int mask)
        { }
        /// <summary>
        /// Returns the source parameter count.
        /// </summary>
        public int SourceParameterCount
        {
            get { return srcParameterCount; }
        }
        /// <summary>
        /// Gets source parameter
        /// </summary>
        public Parameter[] SourceParameter
        {
            get { return srcParameter; }
        }
        /// <summary>
        /// Gets the source parameter mask
        /// </summary>
        public int[] SourceParameterMask
        {
            get { return srcParameterMask; }
        }
        /// <summary>
        /// Gets destination parameter mask by index.
        /// </summary>
        public int[] DestinationParameterMask
        {
            get { return dstParameterMask; }
        }
        /// <summary>
        /// Gets the number of used floats
        /// </summary>
        public int UsedFloatCount
        {
            get;
            set;
        } 
        #endregion

        #region Methods
        /// <summary>
        /// Clears the state of this merge parameter
        /// </summary>
        public void Clear()
        { }
        /// <summary>
        /// Returns the destination parameter
        /// </summary>
        /// <param name="usage"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Parameter GetDestinationParameter(int usage, int index)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Creates the destination parameter by a given class and index.
        /// </summary>
        /// <param name="usage"></param>
        /// <param name="index"></param>
        protected void CreateDestinationParamter(int usage, int index)
        { } 
        #endregion


        internal int GetSourceParameterMask(int p)
        {
            throw new NotImplementedException();
        }

        internal int GetDestinationParameterMask(int p)
        {
            throw new NotImplementedException();
        }
    }
}
