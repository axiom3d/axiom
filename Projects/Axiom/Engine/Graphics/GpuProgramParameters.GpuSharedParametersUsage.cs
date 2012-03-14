using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Axiom.Graphics
{
	public partial class GpuProgramParameters
	{
		#region Nested type: GpuSharedParametersUsage

		/// <summary>
		/// This class records the usage of a set of shared parameters in a concrete
		/// set of GpuProgramParameters.
		/// </summary>
		public class GpuSharedParametersUsage
		{
			#region SharedParameters

			[OgreVersion( 1, 7, 2790, "SharedParams in OGRE" )]
			public GpuSharedParameters SharedParameters { get; private set; }

			#endregion

			#region _parameters

			[OgreVersion( 1, 7, 2790 )]
			private readonly GpuProgramParameters _parameters;

			#endregion

			#region TargetParameters

			/// <summary>
			/// Get's the target Gpu program parameters.
			/// </summary>
			[OgreVersion( 1, 7, 2790, "TargetParams in OGRE" )]
			public GpuProgramParameters TargetParameters
			{
				get
				{
					return this._parameters;
				}
			}

			#endregion

			#region CopyDataEntry

			[OgreVersion( 1, 7, 2790 )]
			protected struct CopyDataEntry
			{
				public GpuConstantDefinition DstDefinition;
				public GpuConstantDefinition SrcDefinition;
			}

			#endregion

			#region CopyDataList

			/// <summary>
			/// list of physical mappings that we are going to bring in
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			protected List<CopyDataEntry> CopyDataList = new List<CopyDataEntry>();

			#endregion

			#region CopyDataVersion

			/// <summary>
			/// Version of shared params we based the copydata on
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			protected uint CopyDataVersion;

			#endregion

			#region RenderSystemData

			/// <summary>
			/// Optional data the rendersystem might want to store
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			public object RenderSystemData { get; set; }

			#endregion

			#region Name

			/// <summary>
			/// Get the name of the shared parameter set
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			public string Name
			{
				get
				{
					return SharedParameters.Name;
				}
			}

			#endregion

			#region constructor

			/// <summary>
			/// Default Constructor.
			/// </summary>
			[OgreVersion( 1, 7, 2790 )]
			public GpuSharedParametersUsage( GpuSharedParameters sharedParams, GpuProgramParameters gparams )
			{
				SharedParameters = sharedParams;
				this._parameters = gparams;
				InitCopyData();
			}

			#endregion

			#region InitCopyData

			[OgreVersion( 1, 7, 2790 )]
			protected void InitCopyData()
			{
				this.CopyDataList.Clear();
				GpuConstantDefinitionMap sharedMap = SharedParameters.ConstantDefinitions.Map;
				foreach ( var i in sharedMap )
				{
					string name = i.Key;
					GpuConstantDefinition sharedDef = i.Value;

					GpuConstantDefinition instDef = this._parameters.FindNamedConstantDefinition( name, false );
					if ( instDef != null )
					{
						// Check that the definitions are the same
						if ( instDef.ConstantType == sharedDef.ConstantType && instDef.ArraySize == sharedDef.ArraySize )
						{
							var e = new CopyDataEntry();
							e.SrcDefinition = sharedDef;
							e.DstDefinition = instDef;
							this.CopyDataList.Add( e );
						}
					}
				}

				this.CopyDataVersion = SharedParameters.Version;
			}

			#endregion

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
				if ( this.CopyDataVersion != SharedParameters.Version )
				{
					InitCopyData();
				}

				foreach ( CopyDataEntry e in this.CopyDataList )
				{
					if ( e.DstDefinition.IsFloat )
					{
						FloatConstantList dst = SharedParameters.FloatConstants;
						FloatConstantList src = this._parameters.floatConstants;
						int pSrc = e.SrcDefinition.PhysicalIndex;
						int pDst = e.DstDefinition.PhysicalIndex;


						// Deal with matrix transposition here!!!
						// transposition is specific to the dest param set, shared params don't do it
						if ( this._parameters.TransposeMatrices && e.DstDefinition.ConstantType == GpuConstantType.Matrix_4X4 )
						{
							for ( int row = 0; row < 4; ++row )
							{
								for ( int col = 0; col < 4; ++col )
								{
									dst[ pDst + row * 4 + col ] = src[ pSrc + col * 4 + row ];
								}
							}
						}
						else
						{
							if ( e.DstDefinition.ElementSize == e.SrcDefinition.ElementSize )
							{
								// simple copy
								Array.Copy( src.Data, pSrc, dst.Data, pDst, e.DstDefinition.ElementSize * e.DstDefinition.ArraySize );
							}
							else
							{
								// target params may be padded to 4 elements, shared params are packed
								Debug.Assert( e.DstDefinition.ElementSize % 4 == 0 );
								int iterations = e.DstDefinition.ElementSize / 4 * e.DstDefinition.ArraySize;
								int valsPerIteration = e.SrcDefinition.ElementSize / iterations;
								for ( int l = 0; l < iterations; ++l )
								{
									Array.Copy( src.Data, pSrc, dst.Data, pDst, valsPerIteration );
									pSrc += valsPerIteration;
									pDst += 4;
								}
							}
						}
					}
					else
					{
						IntConstantList dst = SharedParameters.IntConstants;
						IntConstantList src = this._parameters.intConstants;
						int pSrc = e.SrcDefinition.PhysicalIndex;
						int pDst = e.DstDefinition.PhysicalIndex;

						if ( e.DstDefinition.ElementSize == e.SrcDefinition.ElementSize )
						{
							// simple copy
							Array.Copy( src.Data, pSrc, dst.Data, pDst, e.DstDefinition.ElementSize * e.DstDefinition.ArraySize );
						}
						else
						{
							// target params may be padded to 4 elements, shared params are packed
							Debug.Assert( e.DstDefinition.ElementSize % 4 == 0 );
							int iterations = e.DstDefinition.ElementSize / 4 * e.DstDefinition.ArraySize;
							int valsPerIteration = e.SrcDefinition.ElementSize / iterations;
							for ( int l = 0; l < iterations; ++l )
							{
								Array.Copy( src.Data, pSrc, dst.Data, pDst, valsPerIteration );
								pSrc += valsPerIteration;
								pDst += 4;
							}
						}
					}
				}
			}
		}

		#endregion

		#region Nested type: GpuSharedParametersUsageList

		public class GpuSharedParametersUsageList : List<GpuSharedParametersUsage> { }

		#endregion
	}
}
