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
using System.Text;

using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	partial class GpuProgramParameters
	{
		[Flags]
		public enum GpuParamVariability
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
		public class GpuSharedParameters
		{
			/// <summary>
			/// 
			/// </summary>
			protected GpuNamedConstants NamedConstants = new GpuNamedConstants();

			/// <summary>
			/// 
			/// </summary>
			protected List<byte[]> FloatConstants = new List<byte[]>();

			/// <summary>
			/// 
			/// </summary>
			protected List<byte[]> IntConstants = new List<byte[]>();

			/// <summary>
			/// 
			/// </summary>
			public GpuNamedConstants ConstantDefinitions
			{
				get
				{
					return this.NamedConstants;
				}
			}

			/// <summary>
			/// 
			/// </summary>
			private string _name;

			/// <summary>
			/// Get the name of this shared parameter set
			/// </summary>
			public string Name
			{
				get
				{
					return this._name;
				}
				protected set
				{
					this._name = value;
				}
			}

			/// <summary>
			/// Version number of the definitions in this buffer
			/// </summary>
			private ulong _version;

			/// <summary>
			/// Get the version number of this shared parameter set, can be used to identify when
			/// changes have occurred.
			/// </summary>
			public ulong Version
			{
				get
				{
					return this._version;
				}
				protected set
				{
					this._version = value;
				}
			}

			/// <summary>
			/// Not used when copying data, but might be useful to RS using shared buffers
			/// </summary>
			private int _frameLastUpdated;

			/// <summary>
			///  Get the frame in which this shared parameter set was last updated
			/// </summary>
			public int FrameLastUpdated
			{
				get
				{
					return this._frameLastUpdated;
				}
				set
				{
					this._frameLastUpdated = value;
				}
			}

			/// <summary>
			/// Optional data the rendersystem might want to store
			/// </summary>
			private object _renderSystemData;

			/// <summary>
			///  Internal method that the RenderSystem might use to store optional data.
			/// </summary>
			public object RenderSystemData
			{
				set
				{
					this._renderSystemData = value;
				}
				get
				{
					return this._renderSystemData;
				}
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			public GpuSharedParameters( string name )
			{
				this._name = name;
				this._frameLastUpdated = (int)Root.Instance.CurrentFrameCount;
				this._version = 0;
			}

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
			public void AddConstantDefinition( string name, GpuConstantType constType )
			{
				this.AddConstantDefinition( name, constType, 1 );
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
			public virtual void AddConstantDefinition( string name, GpuConstantType constType, int arrraySize )
			{
				if ( this.NamedConstants.GpuConstantDefinitions.ContainsKey( name ) )
				{
					throw new Exception( "Constant entry with name '" + name + "' allready exists, GpuSharedParameters.AddConstantDefinition" );
				}
				GpuConstantDefinition def = new GpuConstantDefinition();
				def.ArraySize = arrraySize;
				def.ConstantType = constType;
				// for compatibility we do not pad values to multiples of 4
				// when it comes to arrays, user is responsible for creating matching defs
				def.ElementSize = GpuConstantDefinition.GetElementSize( constType, false );

				//not used
				def.LogicalIndex = 0;
				def.Variability = GpuParamVariability.Global;
				byte[] b = new byte[ 1 ];
				if ( def.IsFloat )
				{
					def.PhysicalIndex = this.FloatConstants.Count;
					int amount = def.ArraySize * def.ElementSize;
					;
					this.FloatConstants.Capacity += amount;
					for ( int i = 0; i < amount; i++ )
					{
						this.FloatConstants.Add( b );
					}
				}
				else
				{
					def.PhysicalIndex = this.IntConstants.Count;
					int amount = def.ArraySize * def.ElementSize;
					this.IntConstants.Capacity += amount;
					for ( int i = 0; i < amount; i++ )
					{
						this.IntConstants.Add( b );
					}
				}
				this.NamedConstants.GpuConstantDefinitions.Add( name, def );

				++this._version;
			}

			/// <summary>
			/// Remove a constant definition from this shared set of parameters.
			/// </summary>
			/// <param name="name"></param>
			public virtual void RemoveConstantDefinition( string name )
			{
				GpuConstantDefinition i = null;
				if ( this.NamedConstants.GpuConstantDefinitions.TryGetValue( name, out i ) )
				{
					GpuConstantDefinition def = i;
					bool isFloat = def.IsFloat;
					foreach ( KeyValuePair<string, GpuConstantDefinition> j in this.NamedConstants.GpuConstantDefinitions )
					{
						GpuConstantDefinition otherDef = j.Value;
						bool otherIsFloat = otherDef.IsFloat;
						// same type, and comes after in the buffer
						if ( ( ( isFloat && otherIsFloat ) || ( !isFloat && !otherIsFloat ) ) &&
							 otherDef.PhysicalIndex > def.PhysicalIndex )
						{
							// adjust index
							otherDef.PhysicalIndex -= 1;
						}
					}

					// remove floats and reduce buffer
					if ( isFloat )
					{
						byte[] tmp = this.FloatConstants[ def.PhysicalIndex ];
						this.NamedConstants.FloatBufferSize -= sizeof( byte ) * tmp.Length;
						this.FloatConstants.RemoveAt( def.PhysicalIndex );
					}
					else
					{
						byte[] tmp = this.IntConstants[ def.PhysicalIndex ];
						this.NamedConstants.IntBufferSize -= sizeof( byte ) * tmp.Length;
						this.IntConstants.RemoveAt( def.PhysicalIndex );
					}

					++this._version;
				}
			}

			/// <summary>
			/// Remove a constant definition from this shared set of parameters.
			/// </summary>
			public virtual void RemoveAllConstantDefinitions()
			{
				this.NamedConstants.GpuConstantDefinitions.Clear();
				this.NamedConstants.FloatBufferSize = 0;
				this.NamedConstants.IntBufferSize = 0;
				this.FloatConstants.Clear();
				this.IntConstants.Clear();
			}

			/// <summary>
			/// Mark the shared set as being dirty (values modified).
			/// </summary>
			/// <remarks>
			/// You do not need to call this yourself, set is marked as dirty whenever
			/// setNamedConstant or (non const) getFloatPointer et al are called.
			/// </remarks>
			public virtual void MarkDirty()
			{
				this._frameLastUpdated = (int)Root.Instance.CurrentFrameCount;
			}

			/// <summary>
			/// Get a specific GpuConstantDefinition for a named parameter.
			/// </summary>
			/// <param name="name"></param>
			/// <returns></returns>
			public virtual GpuConstantDefinition GetConstantDefinition( string name )
			{
				GpuConstantDefinition def = null;
				if ( this.NamedConstants.GpuConstantDefinitions.TryGetValue( name, out def ) )
				{
					return def;
				}
				else
				{
					throw new Exception( "Constant entry with name '" + name + "' does not exists!\n" +
										 "GpuSharedParameters.GetConstantDefinition" );
				}
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			public void SetNamedConstant( string name, float value )
			{
				this.SetNamedConstant( name, BitConverterEx.GetBytes( value ), true );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			public void SetNamedConstant( string name, int value )
			{
				this.SetNamedConstant( name, BitConverterEx.GetBytes( value ), false );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			public void SetNamedConstant( string name, Vector4 value )
			{
				this.SetNamedConstant( name, BitConverterEx.GetBytes( value ), true );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			public void SetNamedConstant( string name, Vector3 value )
			{
				this.SetNamedConstant( name, BitConverterEx.GetBytes( value ), true );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			public void SetNamedConstant( string name, Matrix4 value )
			{
				this.SetNamedConstant( name, BitConverterEx.GetBytes( value ), true );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			public void SetNamedConstant( string name, Matrix3 value )
			{
				SetNamedConstant( name, BitConverterEx.GetBytes( value ), true );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			public void SetNamedConstant( string name, Matrix4 value, int numEntries )
			{
				this.SetNamedConstant( name, BitConverterEx.GetBytes( value ), true );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			public void SetNamedConstant( string name, double value )
			{
				this.SetNamedConstant( name, BitConverterEx.GetBytes( value ), true );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			public void SetNamedConstant( string name, ColorEx value )
			{
				this.SetNamedConstant( name, BitConverterEx.GetBytes( value ), true );
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="name"></param>
			/// <param name="value"></param>
			/// <param name="isFloat"></param>
			public virtual void SetNamedConstant( string name, byte[] value, bool isFloat )
			{
				GpuConstantDefinition def = null;
				if ( isFloat )
				{
					if ( this.NamedConstants.GpuConstantDefinitions.TryGetValue( name, out def ) )
					{
						this.FloatConstants[ def.PhysicalIndex ] = value;
					}
				}
				else
				{
					if ( this.NamedConstants.GpuConstantDefinitions.TryGetValue( name, out def ) )
					{
						this.IntConstants[ def.PhysicalIndex ] = value;
					}
				}

				MarkDirty();
			}

			/// <summary>
			/// Get a pointer to the 'nth' item in the float buffer
			/// </summary>
			/// <param name="pos"></param>
			/// <returns></returns>
			public byte[] GetFloatPointer( int pos )
			{
				return this.FloatConstants[ pos ];
			}

			/// <summary>
			/// Get a pointer to the 'nth' item in the int buffer
			/// </summary>
			/// <param name="pos"></param>
			/// <returns></returns>
			public byte[] GetIntPointer( int pos )
			{
				return this.IntConstants[ pos ];
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
					return this._sharedParameters.Name;
				}
			}

			/// <summary>
			/// 
			/// </summary>
			private GpuSharedParameters _sharedParameters;

			/// <summary>
			/// Get's the shared parameters.
			/// </summary>
			public GpuSharedParameters SharedParameters
			{
				get
				{
					return this._sharedParameters;
				}
			}

			/// <summary>
			/// 
			/// </summary>
			private GpuProgramParameters _parameters;

			/// <summary>
			/// Get's the target Gpu program parameters.
			/// </summary>
			public GpuProgramParameters TargetParameters
			{
				get
				{
					return this._parameters;
				}
			}

			/// <summary>
			/// Optional data the rendersystem might want to store
			/// </summary>
			private object _renderSystemData;

			/// <summary>
			/// Optional data the rendersystem might want to store
			/// </summary>
			public object RenderSystemData
			{
				set
				{
					this._renderSystemData = value;
				}
				get
				{
					return this._renderSystemData;
				}
			}

			/// <summary>
			/// Default Constructor.
			/// </summary>
			/// <param name="sharedParams"></param>
			/// <param name="gparams"></param>
			public GpuSharedParametersUsage( GpuSharedParameters sharedParams, GpuProgramParameters gparams )
			{
				this._sharedParameters = sharedParams;
				this._parameters = gparams;
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
				if ( this.CopyDataVersion != this._sharedParameters.Version )
				{
					InitCopyData();
				}

				foreach ( CopyDataEntry i in this.CopyDataList )
				{
					CopyDataEntry e = i;
					if ( e.DstDefinition.IsFloat )
					{
						unsafe
						{
							byte[] src = this._sharedParameters.GetFloatPointer( e.SrcDefinition.PhysicalIndex );
#warning implement: _parameters.GetFloatPointer(e.DstDefinition.PhysicalIndex);
							byte[] dst = null;

							// Deal with matrix transposition here!!!
							// transposition is specific to the dest param set, shared params don't do it
							if ( this._parameters.TransposeMatrices && e.DstDefinition.ConstantType == GpuConstantType.Matrix_4X4 )
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
							byte[] src = this._sharedParameters.GetIntPointer( e.SrcDefinition.PhysicalIndex );
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

									int* pfSrc = (int*)pSrc;
									int* pfDSt = (int*)pDst;
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
			/// 
			/// </summary>
			protected void InitCopyData()
			{
				this.CopyDataList.Clear();
				Dictionary<string, GpuConstantDefinition> sharedMap = this._sharedParameters.ConstantDefinitions.GpuConstantDefinitions;
				foreach ( KeyValuePair<string, GpuConstantDefinition> i in sharedMap )
				{
					string name = i.Key;
					GpuConstantDefinition sharedDef = i.Value;
#warning implement: _parameters.FindNamedConstantDefinition(name, false);
					GpuConstantDefinition instDef = null;
					if ( instDef != null )
					{
						// Check that the definitions are the same
						if ( instDef.ConstantType == sharedDef.ConstantType &&
							 instDef.ArraySize == sharedDef.ArraySize )
						{
							CopyDataEntry e = new CopyDataEntry();
							e.SrcDefinition = sharedDef;
							e.DstDefinition = instDef;
							this.CopyDataList.Add( e );
						}
					}
				}

				this.CopyDataVersion = this._sharedParameters.Version;
			}
		}
	}
}