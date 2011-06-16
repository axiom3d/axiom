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
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	partial class GpuProgramParameters
	{
		
        [OgreVersion(1,7)]
        [Flags]
		public enum GpuParamVariability: ushort
		{
			/// <summary>
			/// No variation except by manual setting - the default
			/// </summary>
			Global = 1,
			/// <summary>
			/// Varies per object (based on an auto param usually), but not per light setup
			/// </summary>
			PerObject = 2,
			/// <summary>
			/// Varies with light setup
			/// </summary>
			Lights = 4,
			/// <summary>
			/// Varies with pass iteration number
			/// </summary>
			PassIterationNumber = 8,
			/// <summary>
			/// Full mask (16-bit)
			/// </summary>
			All = 0xFFFF
		}

		/// <summary>
		/// Enumeration of the types of constant we may encounter in programs.
		/// </summary>
		/// <note>
		/// Low-level programs, by definition, will always use either
		/// float4 or int4 constant types since that is the fundamental underlying
		/// type in assembler.
		/// </note>
		[OgreVersion(1,7)]
		public enum GpuConstantType
		{
			Float1 = 1,
			Float2 = 2,
			Float3 = 3,
			Float4 = 4,
			Sampler1D = 5,
			Sampler2D = 6,
			Sampler3D = 7,
			SamplerCube = 8,
			Sampler1DShadow = 9,
			Sampler2DShadow = 10,
			Matrix_2X2 = 11,
			Matrix_2X3 = 12,
			Matrix_2X4 = 13,
			Matrix_3X2 = 14,
			Matrix_3X3 = 15,
			Matrix_3X4 = 16,
			Matrix_4X2 = 17,
			Matrix_4X3 = 18,
			Matrix_4X4 = 19,
			Int1 = 20,
			Int2 = 21,
			Int3 = 22,
			Int4 = 23,
			Unknown = 99
		}


        public class ConstantList: List<byte[]>
        {
            public void Resize(int sz)
            {
                Capacity = sz;
                while (Count < sz)
                    Add(null);
            }
        }

        /// <summary>
        /// </summary>
        [OgreVersion(1, 7)]
        public class FloatConstantList : ConstantList
        {
        }

        /// <summary>
        /// </summary>
        [OgreVersion(1, 7)]
        public class IntConstantList : ConstantList
        {
        }

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
		/// <note>
        /// Axiom specific: Data is serialized within FloatConstantList etc
        /// as a byte[] per constant entry and not to a large linear byte[] as in Ogre
		/// </note>
		public class GpuSharedParameters
		{
			/// <summary>
			/// </summary>
			[OgreVersion(1, 7)]
			protected GpuNamedConstants NamedConstants = new GpuNamedConstants();

			/// <summary>
			/// </summary>
            [OgreVersion(1, 7)]
			protected FloatConstantList FloatConstants = new FloatConstantList();

			/// <summary>
			/// </summary>
            [OgreVersion(1, 7)]
            protected IntConstantList IntConstants = new IntConstantList();

			/// <summary>
			/// </summary>
			public GpuNamedConstants ConstantDefinitions
			{
				get
				{
					return NamedConstants;
				}
			}

		    /// <summary>
		    /// Get the name of this shared parameter set
		    /// </summary>
		    [OgreVersion(1, 7)]
		    public string Name { get; protected set; }

		    /// <summary>
		    /// Get the version number of this shared parameter set, can be used to identify when
		    /// changes have occurred.
		    /// </summary>
            [OgreVersion(1, 7)]
            public ulong Version { get; protected set; }


		    /// <summary>
		    ///  Not used when copying data, but might be useful to RS using shared buffers
		    ///  Get the frame in which this shared parameter set was last updated
		    /// </summary>
            [OgreVersion(1, 7)]
		    public int FrameLastUpdated { get; protected set; }


		    /// <summary>
		    ///  Internal method that the RenderSystem might use to store optional data.
		    /// </summary>
            [OgreVersion(1, 7)]
            public object RenderSystemData { get; protected set; }

			/// <summary>
			/// </summary>
			public GpuSharedParameters( string name )
			{
				Name = name;
				FrameLastUpdated = (int)Root.Instance.CurrentFrameCount;
				Version = 0;
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
            [OgreVersion(1, 7, "will not expose ConstantDefinitionIterator")]
			public virtual void AddConstantDefinition( string name, GpuConstantType constType, int arrraySize = 1 )
			{
				if ( NamedConstants.Map.ContainsKey( name ) )
				{
					throw new Exception( "Constant entry with name '" + name + "' allready exists, GpuSharedParameters.AddConstantDefinition" );
				}
				var def = new GpuConstantDefinition
				          {
				              ArraySize = arrraySize,
				              ConstantType = constType,
                              // for compatibility we do not pad values to multiples of 4
                              // when it comes to arrays, user is responsible for creating matching defs
				              ElementSize = GpuConstantDefinition.GetElementSize( constType, false ),

                              // not used
                              LogicalIndex = 0,
				              Variability = GpuParamVariability.Global
				          };

				if ( def.IsFloat )
				{
					def.PhysicalIndex = FloatConstants.Count;
                    // TODO: Validate: is the resize really def.ArraySize * def.ElementSize or rather def.ArraySize?
                    FloatConstants.Resize(FloatConstants.Count + def.ArraySize);
				}
				else
				{
					def.PhysicalIndex = IntConstants.Count;
                    // TODO: Validate: is the resize really def.ArraySize * def.ElementSize or rather def.ArraySize?
                    IntConstants.Resize(IntConstants.Count + def.ArraySize);
				}
				NamedConstants.Map.Add( name, def );

				++Version;
			}

			/// <summary>
			/// Remove a constant definition from this shared set of parameters.
			/// </summary>
			[OgreVersion(1, 7)]
			public virtual void RemoveConstantDefinition( string name )
			{
				GpuConstantDefinition def;
				if ( NamedConstants.Map.TryGetValue( name, out def ) )
				{
					var isFloat = def.IsFloat;

					foreach ( var j in NamedConstants.Map )
					{
						var otherDef = j.Value;
						var otherIsFloat = otherDef.IsFloat;

						// same type, and comes after in the buffer
						if ( ( ( isFloat && otherIsFloat ) || ( !isFloat && !otherIsFloat ) ) &&
							 otherDef.PhysicalIndex > def.PhysicalIndex )
						{
							// adjust index
                            otherDef.PhysicalIndex -= 1; // Ogre: numElements
						}
					}

					// remove floats and reduce buffer
					if ( isFloat )
					{
                        var tmp = FloatConstants[def.PhysicalIndex];
                        NamedConstants.FloatBufferSize -= tmp.Length;
                        FloatConstants.RemoveAt(def.PhysicalIndex);
					}
					else
					{
                        var tmp = IntConstants[def.PhysicalIndex];
                        NamedConstants.IntBufferSize -= tmp.Length;
                        IntConstants.RemoveAt(def.PhysicalIndex);
					}

					++Version;
				}
			}

			/// <summary>
			/// Remove a constant definition from this shared set of parameters.
			/// </summary>
			[OgreVersion(1, 7)]
			public void RemoveAllConstantDefinitions()
			{
				NamedConstants.Map.Clear();
				NamedConstants.FloatBufferSize = 0;
				NamedConstants.IntBufferSize = 0;
				FloatConstants.Clear();
				IntConstants.Clear();
			}

			/// <summary>
			/// Mark the shared set as being dirty (values modified).
			/// </summary>
			/// <remarks>
			/// You do not need to call this yourself, set is marked as dirty whenever
			/// setNamedConstant or (non const) getFloatPointer et al are called.
			/// </remarks>
            [OgreVersion(1, 7)]
			public virtual void MarkDirty()
			{
				FrameLastUpdated = (int)Root.Instance.CurrentFrameCount;
			}

			/// <summary>
			/// Get a specific GpuConstantDefinition for a named parameter.
			/// </summary>
			[OgreVersion(1, 7)]
			public GpuConstantDefinition GetConstantDefinition( string name )
			{
				GpuConstantDefinition def;
				if ( NamedConstants.Map.TryGetValue( name, out def ) )
				{
					return def;
				}
				// else
				{
					throw new Exception( "Constant entry with name '" + name + "' does not exists!\n" +
										 "GpuSharedParameters.GetConstantDefinition" );
				}
			}

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7)]
            public void SetNamedConstant(string name, float value)
            {
                SetNamedConstant(name, BitConverterEx.GetBytes(value), true);
            }

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7)]
            public void SetNamedConstant(string name, int value)
            {
                SetNamedConstant(name, BitConverterEx.GetBytes(value), false);
            }

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7)]
            public void SetNamedConstant(string name, Vector4 value)
            {
                SetNamedConstant(name, BitConverterEx.GetBytes(value), true);
            }

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7)]
            public void SetNamedConstant(string name, Vector3 value)
            {
                SetNamedConstant(name, BitConverterEx.GetBytes(value), true);
            }

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7)]
            public void SetNamedConstant(string name, Matrix4 value)
            {
                SetNamedConstant(name, BitConverterEx.GetBytes(value), true);
            }

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7)]
            public void SetNamedConstant(string name, Matrix3 value)
            {
                SetNamedConstant(name, BitConverterEx.GetBytes(value), true);
            }

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7)]
            public void SetNamedConstant(string name, Matrix4 value, int numEntries)
            {
                SetNamedConstant(name, BitConverterEx.GetBytes(value), true);
            }

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7)]
            public void SetNamedConstant(string name, double value)
            {
                SetNamedConstant(name, BitConverterEx.GetBytes(value), true);
            }

            /// <summary>
            /// </summary>
            [OgreVersion(1, 7)]
            public void SetNamedConstant(string name, ColorEx value)
            {
                SetNamedConstant(name, BitConverterEx.GetBytes(value), true);
            }

            /// <summary>
            /// Associates serialized data with a named constant
            /// </summary>
            /// <remarks>
            /// The interface is not exactly as in Ogre as the internals in Axiom
            /// are different. Furthermore we cant differ serialized float -> byte[]
            /// from a serialized int -> byte[] so we need the additional hint
            /// </remarks>
            [OgreVersion(1, 7)]
            public virtual void SetNamedConstant(string name, byte[] value, bool isFloat)
            {
                GpuConstantDefinition def;
                if (isFloat)
                {
                    if (NamedConstants.Map.TryGetValue(name, out def))
                    {
                        FloatConstants[def.PhysicalIndex] = value;
                    }
                }
                else
                {
                    if (NamedConstants.Map.TryGetValue(name, out def))
                    {
                        IntConstants[def.PhysicalIndex] = value;
                    }
                }

                MarkDirty();
            }

			/// <summary>
			/// Get a pointer to the 'nth' item in the float buffer
			/// </summary>
			[OgreVersion(1,7, "different interface due to serialization")]
			public byte[] GetFloatPointer( int pos )
			{
				return FloatConstants[ pos ];
			}

			/// <summary>
			/// Get a pointer to the 'nth' item in the int buffer
			/// </summary>
            [OgreVersion(1, 7, "different interface due to serialization")]
			public byte[] GetIntPointer( int pos )
			{
				return IntConstants[ pos ];
			}
		}

		/// <summary>
		/// This class records the usage of a set of shared parameters in a concrete
		/// set of GpuProgramParameters.
		/// </summary>
		public class GpuSharedParametersUsage
		{
			protected struct CopyDataEntry
			{
				public GpuConstantDefinition SrcDefinition;
				public GpuConstantDefinition DstDefinition;
			}

			/// <summary>
			/// list of physical mappings that we are going to bring in
			/// </summary>
			protected List<CopyDataEntry> CopyDataList = new List<CopyDataEntry>();

			/// <summary>
			/// Version of shared params we based the copydata on
			/// </summary>
			protected ulong CopyDataVersion;

			/// <summary>
			/// Get the name of the shared parameter set
			/// </summary>
			public string Name
			{
				get
				{
					return _sharedParameters.Name;
				}
			}

			/// <summary>
			/// </summary>
			private readonly GpuSharedParameters _sharedParameters;

			/// <summary>
			/// Get's the shared parameters.
			/// </summary>
			public GpuSharedParameters SharedParameters
			{
				get
				{
					return _sharedParameters;
				}
			}

			/// <summary>
			/// </summary>
			private readonly GpuProgramParameters _parameters;

			/// <summary>
			/// Get's the target Gpu program parameters.
			/// </summary>
			public GpuProgramParameters TargetParameters
			{
				get
				{
					return _parameters;
				}
			}

		    /// <summary>
		    /// Optional data the rendersystem might want to store
		    /// </summary>
		    public object RenderSystemData { get; set; }

		    /// <summary>
			/// Default Constructor.
			/// </summary>
			/// <param name="sharedParams"></param>
			/// <param name="gparams"></param>
			public GpuSharedParametersUsage( GpuSharedParameters sharedParams, GpuProgramParameters gparams )
			{
				_sharedParameters = sharedParams;
				_parameters = gparams;
				InitCopyData();
			}

			/// <summary>
			/// Update the target parameters by copying the data from the shared
			/// parameters.
			/// </summary>
			/// <note>
			/// This method  may not actually be called if the RenderSystem
			/// supports using shared parameters directly in their own shared buffer; in
			/// which case the values should not be copied out of the shared area
			/// into the individual parameter set, but bound separately.
			/// </note>
			public void CopySharedParamsToTargetParams()
			{
				// check copy data version
				if ( CopyDataVersion != _sharedParameters.Version )
					InitCopyData();

				foreach ( var e in CopyDataList )
				{
					if ( e.DstDefinition.IsFloat )
					{
						unsafe
						{
							byte[] src = _sharedParameters.GetFloatPointer( e.SrcDefinition.PhysicalIndex );
#warning implement: _parameters.GetFloatPointer(e.DstDefinition.PhysicalIndex);
							byte[] dst = null;

							// Deal with matrix transposition here!!!
							// transposition is specific to the dest param set, shared params don't do it
							if ( _parameters.TransposeMatrices && e.DstDefinition.ConstantType == GpuConstantType.Matrix_4X4 )
							{
								for ( int row = 0; row < 4; ++row )
								{
									for ( int col = 0; col < 4; ++col )
									{
										dst[ row * 4 + col ] = src[ col * 4 + row ];
									}
								}
							}
							else
							{
								if ( e.DstDefinition.ElementSize == e.SrcDefinition.ElementSize )
								{
									// simple copy
									src.CopyTo( dst, 0 );
								}
								else
								{
									// target params may be padded to 4 elements, shared params are packed
									System.Diagnostics.Debug.Assert( e.DstDefinition.ElementSize % 4 == 0 );
									int iterations = e.DstDefinition.ElementSize / 4
													 * e.DstDefinition.ArraySize;
									int valsPerIteration = e.SrcDefinition.ElementSize / iterations;
									IntPtr pSrc = Memory.PinObject( src );
									IntPtr pDst = Memory.PinObject( dst );
									for ( int l = 0; l < iterations; ++l )
									{
										Memory.Copy( pSrc, pDst, sizeof( float ) * valsPerIteration );

										float* pfSrc = (float*)pSrc;
										float* pfDSt = (float*)pDst;
										pfSrc += valsPerIteration;
										pfDSt += 4;
									}
									Memory.UnpinObject( src );
									Memory.UnpinObject( dst );
								}
							}
						}
					}
					else
					{
						unsafe
						{
							byte[] src = _sharedParameters.GetIntPointer( e.SrcDefinition.PhysicalIndex );
#warning implement: _parameters.GetIntPointer(e.DstDefinition.PhysicalIndex);
							byte[] dst = null;
							if ( e.DstDefinition.ElementSize == e.SrcDefinition.ElementSize )
							{
								// simple copy
								src.CopyTo( dst, 0 );
							}
							else
							{
								// target params may be padded to 4 elements, shared params are packed
								System.Diagnostics.Debug.Assert( e.DstDefinition.ElementSize % 4 == 0 );
								int iterations = e.DstDefinition.ElementSize / 4
												 * e.DstDefinition.ArraySize;
								int valsPerIteration = e.SrcDefinition.ElementSize / iterations;
								IntPtr pSrc = Memory.PinObject( src );
								IntPtr pDst = Memory.PinObject( dst );
								for ( int l = 0; l < iterations; ++l )
								{
									Memory.Copy( pSrc, pDst, sizeof( int ) * valsPerIteration );

									var pfSrc = (int*)pSrc;
									var pfDSt = (int*)pDst;
									pfSrc += valsPerIteration;
									pfDSt += 4;
								}
								Memory.UnpinObject( src );
								Memory.UnpinObject( dst );
							}
						}
					}
				}
			}

			/// <summary>
			/// </summary>
			[OgreVersion(1, 7)]
			protected void InitCopyData()
			{
				CopyDataList.Clear();
				var sharedMap = _sharedParameters.ConstantDefinitions.Map;
				foreach (var i in sharedMap )
				{
					var name = i.Key;
					var sharedDef = i.Value;


                    var instDef = _parameters.FindNamedConstantDefinition(name, false);
					if ( instDef != null )
					{
						// Check that the definitions are the same
						if ( instDef.ConstantType == sharedDef.ConstantType &&
							 instDef.ArraySize == sharedDef.ArraySize )
						{
							var e = new CopyDataEntry();
							e.SrcDefinition = sharedDef;
							e.DstDefinition = instDef;
							CopyDataList.Add( e );
						}
					}
				}

				CopyDataVersion = _sharedParameters.Version;
			}
		}

	    public void CopySharedParams()
	    {
	        throw new NotImplementedException();
	    }
	}
}