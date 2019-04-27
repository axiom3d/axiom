#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    public partial class GpuProgramParameters
    {
        /// <summary>
        /// A group of manually updated parameters that are shared between many parameter sets.
        /// </summary>
        /// <remarks>
        /// Sometimes you want to set some common parameters across many otherwise
        /// different parameter sets, and keep them all in sync together. This class
        /// allows you to define a set of parameters that you can share across many
        /// parameter sets and have the parameters that match automatically be pulled
        /// from the shared set, rather than you having to set them on all the parameter
        /// sets individually.
        /// </remarks>
        /// <par>
        /// Parameters in a shared set are matched up with instances in a GpuProgramParameters
        /// structure by matching names. It is up to you to define the named parameters
        /// that a shared set contains, and ensuring the definition matches.
        /// </par>
        /// <note>
        /// Shared parameter sets can be named, and looked up using the GpuProgramManager.
        /// </note>
        [OgreVersion(1, 7, 2790)]
        public class GpuSharedParameters
        {
            #region NamedConstants

            [OgreVersion(1, 7, 2790)] protected GpuNamedConstants NamedConstants = new GpuNamedConstants();

            #endregion

            #region FloatConstants

            [OgreVersion(1, 7, 2790)] protected internal FloatConstantList FloatConstants = new FloatConstantList();

            #endregion

            #region IntConstants

            [OgreVersion(1, 7, 2790)] protected internal IntConstantList IntConstants = new IntConstantList();

            #endregion

            #region Name

            /// <summary>
            /// Get the name of this shared parameter set
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public string Name { get; protected set; }

            #endregion

            #region ConstantDefinitions

            [OgreVersion(1, 7, 2790)]
            public GpuNamedConstants ConstantDefinitions
            {
                get
                {
                    return this.NamedConstants;
                }
            }

            #endregion

            #region Version

            /// <summary>
            /// Get the version number of this shared parameter set, can be used to identify when
            /// changes have occurred.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public uint Version { get; protected set; }

            #endregion

            #region FrameLastUpdated

            /// <summary>
            ///  Not used when copying data, but might be useful to RS using shared buffers
            ///  Get the frame in which this shared parameter set was last updated
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public int FrameLastUpdated { get; protected set; }

            #endregion

            #region RenderSystemData

            /// <summary>
            ///  Internal method that the RenderSystem might use to store optional data.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public object RenderSystemData { get; protected set; }

            #endregion

            #region constructor

            [OgreVersion(1, 7, 2790)]
            public GpuSharedParameters(string name)
            {
                Name = name;
                FrameLastUpdated = Root.Instance.NextFrameNumber;
                Version = 0;
            }

            #endregion

            #region AddConstantDefinition

            /// <summary>
            /// Add a new constant definition to this shared set of parameters.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="constType"></param>
            /// <remarks>
            /// Unlike GpuProgramParameters, where the parameter list is defined by the
            /// program being compiled, this shared parameter set is defined by the
            /// user. Only parameters which have been predefined here may be later
            /// updated.
            /// </remarks>
            [OgreVersion(1, 7, 2790, "will not expose ConstantDefinitionIterator")]
            public void AddConstantDefinition(string name, GpuConstantType constType)
            {
                AddConstantDefinition(name, constType, 1);
            }

            /// <summary>
            /// Add a new constant definition to this shared set of parameters.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="constType"></param>
            /// <param name="arrraySize"></param>
            /// <remarks>
            /// Unlike GpuProgramParameters, where the parameter list is defined by the
            /// program being compiled, this shared parameter set is defined by the
            /// user. Only parameters which have been predefined here may be later
            /// updated.
            /// </remarks>
            [OgreVersion(1, 7, 2790, "will not expose ConstantDefinitionIterator")]
            public void AddConstantDefinition(string name, GpuConstantType constType, int arrraySize)
            {
                if (this.NamedConstants.Map.ContainsKey(name))
                {
                    throw new Exception(string.Format("Constant entry with name '{0}' allready exists.", name));
                }

                var def = new GpuConstantDefinition
                {
                    ArraySize = arrraySize,
                    ConstantType = constType,
                    // for compatibility we do not pad values to multiples of 4
                    // when it comes to arrays, user is responsible for creating matching defs
                    ElementSize = GpuConstantDefinition.GetElementSize(constType, false),
                    // not used
                    LogicalIndex = 0,
                    Variability = GpuParamVariability.Global
                };

                if (def.IsFloat)
                {
                    def.PhysicalIndex = this.FloatConstants.Count;
                    this.FloatConstants.Resize(this.FloatConstants.Count + def.ArraySize * def.ElementSize);
                }
                else
                {
                    def.PhysicalIndex = this.IntConstants.Count;
                    this.IntConstants.Resize(this.IntConstants.Count + def.ArraySize * def.ElementSize);
                }
                this.NamedConstants.Map.Add(name, def);

                ++Version;
            }

            #endregion

            #region RemoveConstantDefinition

            /// <summary>
            /// Remove a constant definition from this shared set of parameters.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public virtual void RemoveConstantDefinition(string name)
            {
                GpuConstantDefinition def;
                if (!this.NamedConstants.Map.TryGetValue(name, out def))
                {
                    return;
                }

                var isFloat = def.IsFloat;
                var numElems = def.ElementSize * def.ArraySize;

                foreach (var otherDef in this.NamedConstants.Map.Values)
                {
                    // same type, and comes after in the buffer
                    if ((isFloat == otherDef.IsFloat) && otherDef.PhysicalIndex > def.PhysicalIndex)
                    {
                        // adjust index
                        otherDef.PhysicalIndex -= numElems;
                    }
                }

                // remove floats and reduce buffer
                if (isFloat)
                {
                    this.NamedConstants.FloatBufferSize -= numElems;
                    this.FloatConstants.RemoveRange(def.PhysicalIndex, numElems);
                }
                else
                {
                    this.NamedConstants.IntBufferSize -= numElems;
                    this.IntConstants.RemoveRange(def.PhysicalIndex, numElems);
                }

                ++Version;
            }

            #endregion

            #region RemoveAllConstantDefinitions

            /// <summary>
            /// Remove a constant definition from this shared set of parameters.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public void RemoveAllConstantDefinitions()
            {
                this.NamedConstants.Map.Clear();
                this.NamedConstants.FloatBufferSize = 0;
                this.NamedConstants.IntBufferSize = 0;
                this.FloatConstants.Clear();
                this.IntConstants.Clear();
            }

            #endregion

            #region GetConstantDefinition

            /// <summary>
            /// Get a specific GpuConstantDefinition for a named parameter.
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public GpuConstantDefinition GetConstantDefinition(string name)
            {
                return this.NamedConstants.Map[name];
            }

            #endregion

            #region SetNamedConstant overloads

            [OgreVersion(1, 7, 2790)]
            public void SetNamedConstant(string name, Real value)
            {
                SetNamedConstant(name, new float[]
                                        {
                                            value
                                        });
            }

            [OgreVersion(1, 7, 2790)]
            public void SetNamedConstant(string name, int value)
            {
                SetNamedConstant(name, new[]
                                        {
                                            value
                                        });
            }

            [OgreVersion(1, 7, 2790)]
            public void SetNamedConstant(string name, Vector4 value)
            {
                SetNamedConstant(name, new float[]
                                        {
                                            value.x, value.y, value.z, value.w
                                        });
            }

            [OgreVersion(1, 7, 2790)]
            public void SetNamedConstant(string name, Vector3 value)
            {
                SetNamedConstant(name, new float[]
                                        {
                                            value.x, value.y, value.z
                                        });
            }

            [OgreVersion(1, 7, 2790)]
            public void SetNamedConstant(string name, Matrix4 value)
            {
                var floats = new float[16];
                value.MakeFloatArray(floats);
                SetNamedConstant(name, floats);
            }

            [OgreVersion(1, 7, 2790)]
            public void SetNamedConstant(string name, Matrix4[] value)
            {
                var size = value.Length * 16;
                var floats = new float[size];
                for (var i = 0; i < value.Length; i++)
                {
                    value[i].MakeFloatArray(floats, i * 16);
                }
                SetNamedConstant(name, floats);
            }

            [OgreVersion(1, 7, 2790)]
            public void SetNamedConstant(string name, float[] value)
            {
                GpuConstantDefinition def;
                if (this.NamedConstants.Map.TryGetValue(name, out def))
                {
                    var count = Utility.Min(value.Length, def.ElementSize * def.ArraySize);

                    for (var v = 0; v < count; v++)
                    {
                        this.FloatConstants[def.PhysicalIndex + v] = value[v];
                    }
                }

                MarkDirty();
            }

            [OgreVersion(1, 7, 2790)]
            public void SetNamedConstant(string name, double[] value)
            {
                GpuConstantDefinition def;
                if (this.NamedConstants.Map.TryGetValue(name, out def))
                {
                    var count = Utility.Min(value.Length, def.ElementSize * def.ArraySize);

                    for (var v = 0; v < count; v++)
                    {
                        this.FloatConstants[def.PhysicalIndex + v] = (float)value[v];
                    }
                }

                MarkDirty();
            }

            [OgreVersion(1, 7, 2790)]
            public void SetNamedConstant(string name, ColorEx value)
            {
                var floats = new float[4];
                value.ToArrayRGBA(floats);
                SetNamedConstant(name, floats);
            }

            [OgreVersion(1, 7, 2790)]
            public void SetNamedConstant(string name, int[] value)
            {
                GpuConstantDefinition def;
                if (this.NamedConstants.Map.TryGetValue(name, out def))
                {
                    var count = Utility.Min(value.Length, def.ElementSize * def.ArraySize);

                    for (var v = 0; v < count; v++)
                    {
                        this.IntConstants[def.PhysicalIndex + v] = value[v];
                    }
                }

                MarkDirty();
            }

            #endregion

            #region MarkDirty

            /// <summary>
            /// Mark the shared set as being dirty (values modified).
            /// </summary>
            /// <remarks>
            /// You do not need to call this yourself, set is marked as dirty whenever
            /// setNamedConstant or (non const) getFloatPointer et al are called.
            /// </remarks>
            [OgreVersion(1, 7, 2790)]
            public virtual void MarkDirty()
            {
                FrameLastUpdated = Root.Instance.NextFrameNumber;
            }

            #endregion

            #region GetFloatPointer

            /// <summary>
            /// Get a pointer to the 'nth' item in the float buffer
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public OffsetArray<float>.FixedPointer GetFloatPointer(int pos)
            {
                return this.FloatConstants.Fix(pos);
            }

            #endregion

            #region GetIntPointer

            /// <summary>
            /// Get a pointer to the 'nth' item in the int buffer
            /// </summary>
            [OgreVersion(1, 7, 2790)]
            public OffsetArray<int>.FixedPointer GetIntPointer(int pos)
            {
                return this.IntConstants.Fix(pos);
            }

            #endregion
        }
    };
}