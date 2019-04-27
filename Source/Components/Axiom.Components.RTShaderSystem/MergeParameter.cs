using System;

namespace Axiom.Components.RTShaderSystem
{
    internal class MergeParameter
    {
        #region Fields

        protected Parameter dstParameter; //Destination merged parameter.
        protected Parameter[] srcParameter = new Parameter[4]; //Soure parameter - 4 source at max 1,1,1,1 -> 4

        protected int[] srcParameterMask = new int[4];
        //source parameter mask. OPM_ALL means all fields used, otherwise it is split source parameter.

        protected int[] dstParameterMask = new int[4];
        //destination parameters mask. OPM_ALL means all fields used, otherwise it is split source parameter.

        protected int srcParameterCount;
        protected int usedFloatcount;

        #endregion

        #region C'Tor

        public MergeParameter()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        ///   Add source parameter to this merged.
        /// </summary>
        /// <param name="srcParam"> </param>
        /// <param name="mask"> </param>
        public void AddSourceParameter(Parameter srcParam, int mask)
        {
        }

        /// <summary>
        ///   Returns the source parameter count.
        /// </summary>
        public int SourceParameterCount
        {
            get
            {
                return this.srcParameterCount;
            }
        }

        /// <summary>
        ///   Gets source parameter
        /// </summary>
        public Parameter[] SourceParameter
        {
            get
            {
                return this.srcParameter;
            }
        }

        /// <summary>
        ///   Gets the source parameter mask
        /// </summary>
        public int[] SourceParameterMask
        {
            get
            {
                return this.srcParameterMask;
            }
        }

        /// <summary>
        ///   Gets destination parameter mask by index.
        /// </summary>
        public int[] DestinationParameterMask
        {
            get
            {
                return this.dstParameterMask;
            }
        }

        /// <summary>
        ///   Gets the number of used floats
        /// </summary>
        public int UsedFloatCount { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///   Clears the state of this merge parameter
        /// </summary>
        public void Clear()
        {
            this.dstParameter = null;
            for (int i = 0; i < 4; i++)
            {
                this.srcParameter[i] = null;
                this.srcParameterMask[i] = 0;
                this.dstParameterMask[i] = 0;
            }
            this.srcParameterCount = 0;
            this.usedFloatcount = 0;
        }

        /// <summary>
        ///   Returns the destination parameter
        /// </summary>
        /// <param name="usage"> </param>
        /// <param name="index"> </param>
        /// <returns> </returns>
        public Parameter GetDestinationParameter(int usage, int index)
        {
            if (this.dstParameter == null)
            {
                CreateDestinationParamter(usage, index);
            }

            return this.dstParameter;
        }

        /// <summary>
        ///   Creates the destination parameter by a given class and index.
        /// </summary>
        /// <param name="usage"> </param>
        /// <param name="index"> </param>
        protected void CreateDestinationParamter(int usage, int index)
        {
            Axiom.Graphics.GpuProgramParameters.GpuConstantType dstParamType =
                Graphics.GpuProgramParameters.GpuConstantType.Unknown;

            switch (UsedFloatCount)
            {
                case 1:
                    dstParamType = Graphics.GpuProgramParameters.GpuConstantType.Float1;
                    break;
                case 2:
                    dstParamType = Graphics.GpuProgramParameters.GpuConstantType.Float2;
                    break;
                case 3:
                    dstParamType = Graphics.GpuProgramParameters.GpuConstantType.Float3;
                    break;
                case 4:
                    dstParamType = Graphics.GpuProgramParameters.GpuConstantType.Float4;
                    break;
            }

            if (usage == (int)Operand.OpSemantic.In)
            {
                this.dstParameter = ParameterFactory.CreateInTexcoord(dstParamType, index, Parameter.ContentType.Unknown);
            }
            else
            {
                this.dstParameter = ParameterFactory.CreateOutTexcoord(dstParamType, index, Parameter.ContentType.Unknown);
            }
        }

        #endregion

        internal int GetDestinationParameterMask(int index)
        {
            return this.dstParameterMask[index];
        }

        internal int GetSourceParameterMask(int index)
        {
            return this.srcParameterMask[index];
        }
    }
}