using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Axiom.Graphics
{
    public partial class GpuProgramParameters
    {
        /// <summary>
        /// class collecting together the information for named constants.
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public class GpuNamedConstants
        {
            #region FloatBufferSize

            /// <summary>
            /// Total size of the float buffer required
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public int FloatBufferSize;

            #endregion

            #region IntBufferSize

            /// <summary>
            /// Total size of the int buffer required
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public int IntBufferSize;

            #endregion

            #region Map

            /// <summary>
            /// Dictionary of parameter names to GpuConstantDefinition
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public GpuConstantDefinitionMap Map = new GpuConstantDefinitionMap();

            #endregion

            #region GenerateAllConstantDefinitionArrayEntries

            /// <summary>
            /// Indicates whether all array entries will be generated and added to the definitions map
            /// </summary>
            /// <remarks>
            /// Normally, the number of array entries added to the definitions map is capped at 16
            /// to save memory. Setting this value to <code>true</code> allows all of the entries
            /// to be generated and added to the map.
            /// </remarks>
            [OgreVersion(1, 7, 2790)]
            protected static bool GenerateAllConstantDefinitionArrayEntries;

            #endregion

            #region GenerateConstantDefinitionArrayEntries

            /// <summary>
            /// Generate additional constant entries for arrays based on a base definition.
            /// </summary>
            /// <param name="paramName"></param>
            /// <param name="baseDef"></param>
            /// <remarks>
            /// Array uniforms will be added just with their base name with no array
            /// suffix. This method will add named entries for array suffixes too
            ///	so individual array entries can be addressed. Note that we only
            ///	individually index array elements if the array size is up to 16
            ///	entries in size. Anything larger than that only gets a [0] entry
            ///	as well as the main entry, to save cluttering up the name map. After
            ///	all, you can address the larger arrays in a bulk fashion much more
            ///	easily anyway.
            /// </remarks>
            [OgreVersion(1, 7, 2790)]
            public void GenerateConstantDefinitionArrayEntries(String paramName, GpuConstantDefinition baseDef)
            {
                // Copy definition for use with arrays
                var arrayDef = baseDef.Clone();
                arrayDef.ArraySize = 1;

                // Add parameters for array accessors
                // [0] will refer to the same location, [1+] will increment
                // only populate others individually up to 16 array slots so as not to get out of hand,
                // unless the system has been explicitly configured to allow all the parameters to be added

                // paramName[0] version will always exist
                var maxArrayIndex = 1;
                if (baseDef.ArraySize <= 16 || GenerateAllConstantDefinitionArrayEntries)
                {
                    maxArrayIndex = baseDef.ArraySize;
                }

                for (var i = 0; i < maxArrayIndex; i++)
                {
                    var arrayName = string.Format("{0}[{1}]", paramName, i);
                    Map.Add(arrayName, arrayDef);
                    // increment location
                    arrayDef.PhysicalIndex += arrayDef.ElementSize;
                }
                // note no increment of buffer sizes since this is shared with main array def
            }

            #endregion

            #region Save

            /// <summary>
            /// Saves constant definitions to a file, compatible with GpuProgram::setManualNamedConstantsFile.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public void Save(string filename)
            {
                var ser = new GpuNamedConstantsSerializer();
                ser.ExportNamedConstants(this, filename);
            }

            #endregion

            #region Load

            /// <summary>
            /// Loads constant definitions from a stream, compatible with GpuProgram::setManualNamedConstantsFile.
            /// </summary>
            /// <param name="stream"></param>
            [OgreVersion(1, 7, 2790)]
            public void Load(Stream stream)
            {
                var ser = new GpuNamedConstantsSerializer();
                ser.ImportNamedConstants(stream, this);
            }

            #endregion

            public GpuNamedConstants Clone()
            {
                var p = new GpuNamedConstants();
                p.FloatBufferSize = FloatBufferSize;
                p.IntBufferSize = IntBufferSize;
                foreach (var i in Map)
                    p.Map.Add( i.Key, i.Value );

                return p;
            }
        }
    }
}
