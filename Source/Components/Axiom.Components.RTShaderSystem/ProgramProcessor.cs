using System;
using System.Collections.Generic;
using Axiom.Graphics;

namespace Axiom.Components.RTShaderSystem
{
	/// <summary>
	///   Provides extra processing services on CPU based programs. The base classs only the generic processing. IN order to provide target langauge specific services and optimization one should dervie from this class and register its factory via the ProgramManger instance.
	/// </summary>
	public abstract class ProgramProcessor : IDisposable
	{
		#region Fields

		protected string targetLanguage;
		protected static int maxTexCoordSlots;
		protected static int maxTexCoordFloats;
		private static List<MergeCombination> paramMergeCombinations;
		private Dictionary<Function, string> functionMap;

		#endregion

		#region C'Tor

		public ProgramProcessor()
		{
			maxTexCoordSlots = 8;
			maxTexCoordFloats = maxTexCoordSlots*4;
		}

		#endregion

		#region Protected Virtual Methods

		/// <summary>
		///   Called before the creation of the GPU programs. Does several preperation operations such as validation, register compaction and specific target langauge optimizations.
		/// </summary>
		/// <param name="programSet"> The program set container </param>
		/// <returns> True on success </returns>
		internal virtual bool PreCreateGpuPrograms( ProgramSet programSet )
		{
			return false;
		}

		/// <summary>
		///   Called after cretaion of the GPU programs.
		/// </summary>
		/// <param name="programSet"> The program set container </param>
		/// <returns> True on success </returns>
		internal virtual bool PostCreateGpuPrograms( ProgramSet programSet )
		{
			return false;
		}

		/// <summary>
		///   Compact the vertex shader output registers.
		/// </summary>
		/// <param name="vsMain"> The Vertex Shader entry function </param>
		/// <param name="fsMain"> The Fragment shader entry function </param>
		/// <returns> True on success </returns>
		internal static bool CompactVsOutputs( Function vsMain, Function fsMain )
		{
			int outTexCoordSlots;
			int outTexCoordFloats;

			//Count vertex shader texcoords outputs
			CountVsTexcoordOutputs( vsMain, out outTexCoordSlots, out outTexCoordFloats );

			if ( outTexCoordFloats > maxTexCoordFloats )
			{
				return false;
			}

			//Only one slot used = nothing to compact.
			if ( outTexCoordFloats <= 1 )
			{
				return true;
			}

			//Case compact policy is low and output slots are enough = quit compacting process.
			if ( ShaderGenerator.Instance.VertexShaderOutputsCompactPolicy == ShaderGenerator.OutputsCompactPolicy.Low &&
			     outTexCoordSlots <= maxTexCoordSlots )
			{
				return true;
			}

			//build output parameter tables - each row represents different parameter type (FLOAT1-4)
			List<Parameter>[] vsOutParamsTable;
			List<Parameter>[] fsInParamsTable;


			BuildTexcoordTable( vsMain.OutputParameters, out vsOutParamsTable );
			BuildTexcoordTable( fsMain.InputParameters, out fsInParamsTable );

			//Create merge parameters entries using the predefined merge combinations.
			var vsMergedParamsList = new List<MergeParameter>();
			var fsMergedParamsList = new List<MergeParameter>();
			var vsSplitParams = new List<Parameter>();
			var fsSplitParams = new List<Parameter>();
			bool hasMergedParameters = false;

			MergeParameters( vsOutParamsTable, vsMergedParamsList, vsSplitParams );

			//Check if any parameter has been merged - means at least two parameters takes the same slot.
			for ( int i = 0; i < vsMergedParamsList.Count; i++ )
			{
				if ( vsMergedParamsList[ i ].SourceParameterCount > 1 )
				{
					hasMergedParameters = true;
					break;
				}
			}
			//Case no parameter has been merged = quit compacting process.
			if ( hasMergedParameters == false )
			{
				return true;
			}

			MergeParameters( fsInParamsTable, fsMergedParamsList, fsSplitParams );

			//Generates local params for split source paramters
			var vsLocalParamsMap = new Dictionary<Parameter, Parameter>();
			var fsLocalParamsMap = new Dictionary<Parameter, Parameter>();

			GenerateLocalSplitParameters( vsMain, GpuProgramType.Vertex, vsMergedParamsList, vsSplitParams,
			                              vsLocalParamsMap );
			GenerateLocalSplitParameters( fsMain, GpuProgramType.Fragment, fsMergedParamsList, fsSplitParams,
			                              fsLocalParamsMap );

			RebuildParameterList( vsMain, (int)Operand.OpSemantic.Out, vsMergedParamsList );
			RebuildParameterList( fsMain, (int)Operand.OpSemantic.In, fsMergedParamsList );

			//Adjust function onvocations operands to reference the new merged parameters.
			RebuildFunctionInvocations( vsMain.AtomInstances, vsMergedParamsList, vsLocalParamsMap );
			RebuildFunctionInvocations( fsMain.AtomInstances, fsMergedParamsList, fsLocalParamsMap );

			return true;
		}

		/// <summary>
		///   Generates local parameters for the split parameters and perform packing/unpacking operation using them.
		/// </summary>
		/// <param name="fun"> </param>
		/// <param name="progType"> </param>
		/// <param name="mergedParams"> </param>
		/// <param name="splitParams"> </param>
		/// <param name="localParamsMap"> </param>
		internal static void GenerateLocalSplitParameters( Function func, GpuProgramType progType,
		                                                   List<MergeParameter> mergedParams,
		                                                   List<Parameter> splitParams,
		                                                   Dictionary<Parameter, Parameter> localParamsMap )
		{
			//No split params created.
			if ( splitParams.Count == 0 )
			{
				return;
			}

			//Create the local parameters + map from source to local
			for ( int i = 0; i < splitParams.Count; i++ )
			{
				Parameter srcParameter = splitParams[ i ];
				Parameter localParameter = func.ResolveLocalParameter( srcParameter.Semantic, srcParameter.Index,
				                                                       "lssplit_" + srcParameter.Name, srcParameter.Type );

				localParamsMap.Add( srcParameter, localParameter );
			}

			int invocationCounter = 0;

			//Establish link between the local parameter to the merged parameter.
			for ( int i = 0; i < mergedParams.Count; i++ )
			{
				var curMergeParameter = mergedParams[ i ];

				for ( int p = 0; p < curMergeParameter.SourceParameterCount; p++ )
				{
					Parameter srcMergedParameter = curMergeParameter.SourceParameter[ p ];

					if ( localParamsMap.ContainsKey( srcMergedParameter ) )
					{
						//Case it is the vertex shader -> assign the local parameter to the output merged parameter
						if ( progType == GpuProgramType.Vertex )
						{
							var curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
							                                                (int)
							                                                FFPRenderState.FFPVertexShaderStage.
							                                                	VSPostProcess, invocationCounter++ );

							curFuncInvocation.PushOperand( localParamsMap[ srcMergedParameter ], Operand.OpSemantic.In,
							                               curMergeParameter.GetSourceParameterMask( p ) );
							curFuncInvocation.PushOperand(
								curMergeParameter.GetDestinationParameter( (int)Operand.OpSemantic.Out, i ),
								Operand.OpSemantic.Out, curMergeParameter.GetDestinationParameterMask( p ) );
							func.AddAtomInstance( curFuncInvocation );
						}
						else if ( progType == GpuProgramType.Fragment )
						{
							var curFuncInvocation = new FunctionInvocation( FFPRenderState.FFPFuncAssign,
							                                                (int)
							                                                FFPRenderState.FFPFragmentShaderStage.
							                                                	PSPreProcess, invocationCounter++ );

							curFuncInvocation.PushOperand(
								curMergeParameter.GetDestinationParameter( (int)Operand.OpSemantic.In, i ),
								Operand.OpSemantic.In, curMergeParameter.GetDestinationParameterMask( p ) );
							curFuncInvocation.PushOperand( localParamsMap[ srcMergedParameter ], Operand.OpSemantic.Out,
							                               curMergeParameter.GetSourceParameterMask( p ) );
							func.AddAtomInstance( curFuncInvocation );
						}
					}
				}
			}
		}

		/// <summary>
		///   Rebuild the given parameters list using the merged parameters.
		/// </summary>
		internal static void RebuildParameterList( Function fun, int paramsUsage, List<MergeParameter> mergedParams )
		{
			//Delete the old merged paramters.
			for ( int i = 0; i < mergedParams.Count; ++i )
			{
				var curMergeParamter = mergedParams[ i ];

				for ( int j = 0; j < curMergeParamter.SourceParameterCount; ++j )
				{
					Parameter curSrcParam = curMergeParamter.SourceParameter[ j ];

					if ( paramsUsage == (int)Operand.OpSemantic.Out )
					{
						fun.DeleteOutputParameter( curSrcParam );
					}
					else if ( paramsUsage == (int)Operand.OpSemantic.In )
					{
						fun.DeleteInputParameter( curSrcParam );
					}
				}
			}

			//Add the new combined paramters.
			for ( int i = 0; i < mergedParams.Count; ++i )
			{
				MergeParameter curMergeParameter = mergedParams[ i ];

				if ( paramsUsage == (int)Operand.OpSemantic.Out )
				{
					fun.AddOutputParameter( curMergeParameter.GetDestinationParameter( paramsUsage, i ) );
				}
				else if ( paramsUsage == (int)Operand.OpSemantic.In )
				{
					fun.AddInputParameter( curMergeParameter.GetDestinationParameter( paramsUsage, i ) );
				}
			}
		}

		/// <summary>
		///   Rebuild the given parameters list using the merged parameters.
		/// </summary>
		internal static void RebuildFunctionInvocations( List<FunctionAtom> funcAtomList,
		                                                 List<MergeParameter> mergedParams,
		                                                 Dictionary<Parameter, Parameter> localParamsMap )
		{
			var paramsRefMap = new Dictionary<Parameter, List<Operand>>();


			//Build reference map of source parametrs
			BuildParameterReferenceMap( funcAtomList, paramsRefMap );

			//Replace references to old parameters with references to new parameters.
			ReplaceParametersReferences( mergedParams, paramsRefMap );

			//Replace references to old parameters with references to new split parameters.
			ReplaceSplitParametersReferences( localParamsMap, paramsRefMap );
		}

		#endregion

		#region Protected Methods

		/// <summary>
		///   Build parameter mergin combinations.
		/// </summary>
		protected static void BuildMergeCombinations()
		{
			if ( paramMergeCombinations == null )
			{
				paramMergeCombinations = new List<MergeCombination>();
			}
			paramMergeCombinations.Add(
				new MergeCombination(
					1, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All,
					1, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All ) );

			paramMergeCombinations.Add(
				new MergeCombination(
					2, (int)Operand.OpMask.All,
					1, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All ) );

			paramMergeCombinations.Add(
				new MergeCombination(
					4, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All ) );

			paramMergeCombinations.Add(
				new MergeCombination(
					0, (int)Operand.OpMask.All,
					2, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All ) );

			paramMergeCombinations.Add(
				new MergeCombination(
					0, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All,
					0, (int)Operand.OpMask.All,
					1, (int)Operand.OpMask.All ) );
		}

		/// <summary>
		///   Counts Vertex shader texcoord output slots and output floats
		/// </summary>
		/// <param name="vsMain"> </param>
		/// <param name="outTexCoordSlots"> Will hold the number of used output texcoord slots </param>
		/// <param name="outTexCoordFloats"> Wiull hold the texcoord params sorted by types in each row </param>
		internal static void CountVsTexcoordOutputs( Function vsMain, out int outTexCoordSlots,
		                                             out int outTexCoordFloats )
		{
			outTexCoordFloats = 0;
			outTexCoordSlots = 0;

			List<Parameter> vsOutputs = vsMain.OutputParameters;
			for ( int i = 0; i < vsOutputs.Count; i++ )
			{
				Parameter curParam = vsOutputs[ i ];

				if ( curParam.Semantic == Parameter.SemanticType.TextureCoordinates )
				{
					outTexCoordSlots++;
					outTexCoordFloats += GetParameterFloatCount( curParam.Type );
				}
			}
		}

		/// <summary>
		///   Builds parameters table.
		/// </summary>
		/// <param name="paramList"> The parameter list </param>
		/// <param name="outParamsTable"> Will hold the texcoord params sorted by types in each row. </param>
		private static void BuildTexcoordTable( List<Parameter> paramList, out List<Parameter>[] outParamsTable )
		{
			outParamsTable = new List<Parameter>[4];

			for ( int i = 0; i < paramList.Count; i++ )
			{
				Parameter curParam = paramList[ i ];
				if ( curParam.Semantic == Parameter.SemanticType.TextureCoordinates )
				{
					switch ( curParam.Type )
					{
						case GpuProgramParameters.GpuConstantType.Float1:
							outParamsTable[ 0 ].Add( curParam );
							break;
						case GpuProgramParameters.GpuConstantType.Float2:
							outParamsTable[ 1 ].Add( curParam );
							break;
						case GpuProgramParameters.GpuConstantType.Float3:
							outParamsTable[ 2 ].Add( curParam );
							break;
						case GpuProgramParameters.GpuConstantType.Float4:
							outParamsTable[ 3 ].Add( curParam );
							break;
						case GpuProgramParameters.GpuConstantType.Int1:
						case GpuProgramParameters.GpuConstantType.Int2:
						case GpuProgramParameters.GpuConstantType.Int3:
						case GpuProgramParameters.GpuConstantType.Int4:
						case GpuProgramParameters.GpuConstantType.Matrix_2X2:
						case GpuProgramParameters.GpuConstantType.Matrix_2X3:
						case GpuProgramParameters.GpuConstantType.Matrix_2X4:
						case GpuProgramParameters.GpuConstantType.Matrix_3X2:
						case GpuProgramParameters.GpuConstantType.Matrix_3X3:
						case GpuProgramParameters.GpuConstantType.Matrix_3X4:
						case GpuProgramParameters.GpuConstantType.Matrix_4X2:
						case GpuProgramParameters.GpuConstantType.Matrix_4X3:
						case GpuProgramParameters.GpuConstantType.Matrix_4X4:
						case GpuProgramParameters.GpuConstantType.Sampler1D:
						case GpuProgramParameters.GpuConstantType.Sampler1DShadow:
						case GpuProgramParameters.GpuConstantType.Sampler2D:
						case GpuProgramParameters.GpuConstantType.Sampler2DShadow:
						case GpuProgramParameters.GpuConstantType.Sampler3D:
						case GpuProgramParameters.GpuConstantType.SamplerCube:
						case GpuProgramParameters.GpuConstantType.Unknown:
						default:
							break;
					}
				}
			}
		}

		/// <summary>
		///   Merges parameters from the given table.
		/// </summary>
		/// <param name="paramsTable"> Source parameters table. </param>
		/// <param name="mergedParams"> Will hold the merged parameters list. </param>
		/// <param name="splitParams"> </param>
		internal static void MergeParameters( List<Parameter>[] paramsTable, List<MergeParameter> mergedParams,
		                                      List<Parameter> splitParams )
		{
			MergeParametersByPredefinedCombinations( paramsTable, mergedParams );

			//Merge the reminders paramters if such is left.
			if ( paramsTable[ 0 ].Count + paramsTable[ 1 ].Count + paramsTable[ 2 ].Count + paramsTable[ 3 ].Count > 0 )
			{
				MergeParametersReminders( paramsTable, mergedParams, splitParams );
			}
		}

		/// <summary>
		///   Creates merged parameters using pre-defined combinations.
		/// </summary>
		/// <param name="paramsTable"> Source parameters table. </param>
		/// <param name="mergedParams"> The merged parameters list </param>
		internal static void MergeParametersByPredefinedCombinations( List<Parameter>[] paramsTable,
		                                                              List<MergeParameter> mergedParams )
		{
			if ( paramMergeCombinations == null )
			{
				BuildMergeCombinations();
			}

			//Create the full used merged params - means FLOAT4 params that all of their components are used.
			for ( int i = 0; i < paramMergeCombinations.Count; ++i )
			{
				MergeCombination curCombination = paramMergeCombinations[ i ];

				//Case all parameters have been merged.
				if ( paramsTable[ 0 ].Count + paramsTable[ 1 ].Count +
				     paramsTable[ 2 ].Count + paramsTable[ 3 ].Count == 0 )
				{
					return;
				}

				MergeParameter curMergeParam;

				while ( MergeParametersByCombination( curCombination, paramsTable, out curMergeParam ) )
				{
					mergedParams.Add( curMergeParam );
					curMergeParam.Clear();
				}
			}

			//Case low/medium compactin policy = use these simplified combinations in order to prevent splits.
			if ( ShaderGenerator.Instance.VertexShaderOutputsCompactPolicy == ShaderGenerator.OutputsCompactPolicy.Low ||
			     ShaderGenerator.Instance.VertexShaderOutputsCompactPolicy ==
			     ShaderGenerator.OutputsCompactPolicy.Medium )
			{
				int curUsedSlots = mergedParams.Count;
				int float1ParamCount = paramsTable[ 0 ].Count;
				int float2ParamCount = paramsTable[ 1 ].Count;
				int float3ParamCount = paramsTable[ 2 ].Count;
				int reqSlots = 0;

				//Compute the required slots.

				//Add all float3 since each one of them requires one slot for himself.
				reqSlots += float3ParamCount;

				//Add the float2 count -> at max it will be 1 since all pairs have been merged previously.
				if ( float2ParamCount > 1 )
				{
					throw new Axiom.Core.AxiomException( "Invalid float2 reminder count." );
				}
				reqSlots += float2ParamCount;

				//Compute how much space needed for the float1(s) that left -> at max it will be 3.
				if ( float1ParamCount > 0 )
				{
					if ( float2ParamCount > 3 )
					{
						throw new Axiom.Core.AxiomException( "Invalid float1 reminder count." );
					}
					//No float2 -> we need one more slot for these float1(s).
					if ( float2ParamCount == 0 )
					{
						reqSlots++;
					}
					else
					{
						//float2 exists -> there must be at max 1 float1.
						if ( float1ParamCount > 1 )
						{
							throw new Axiom.Core.AxiomException( "Invalid float1 reminder count" );
						}
					}
				}
				//Case maximium slot count will be exceeded -> fall back to full compaction
				if ( curUsedSlots + reqSlots > maxTexCoordSlots )
				{
					return;
				}

				var simpleCombination = new MergeCombination[6]
				                        {
				                        	//Deal with the float3 paramters.
				                        	new MergeCombination(
				                        		0, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All,
				                        		1, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All ),
				                        	//Deal with the float2 + float1 combination
				                        	new MergeCombination(
				                        		1, (int)Operand.OpMask.All,
				                        		1, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All ),
				                        	//Deal with the float2 paramter.
				                        	new MergeCombination(
				                        		0, (int)Operand.OpMask.All,
				                        		1, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All ),
				                        	//Deal with the 3 float1 combination.
				                        	new MergeCombination(
				                        		3, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All ),
				                        	//Deal with the 2 float1 combination
				                        	new MergeCombination(
				                        		2, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All ),
				                        	//Deal with the 1 float1 combination
				                        	new MergeCombination(
				                        		1, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All,
				                        		0, (int)Operand.OpMask.All )
				                        };

				for ( int i = 0; i < 6; i++ )
				{
					MergeCombination curCombination = simpleCombination[ i ];

					//Case all parameters have been merged.
					if ( paramsTable[ 0 ].Count + paramsTable[ 1 ].Count + paramsTable[ 2 ].Count +
					     paramsTable[ 3 ].Count == 0 )
					{
						break;
					}

					MergeParameter curMergeParam;

					while ( MergeParametersByCombination( curCombination, paramsTable, out curMergeParam ) )
					{
						mergedParams.Add( curMergeParam );
						curMergeParam.Clear();
					}
				}
			}
		}

		/// <summary>
		///   Creates merged parameter from given combination.
		/// </summary>
		/// <param name="combination"> The merge combination to try. </param>
		/// <param name="paramsTable"> The params table sorted by tpes in each row. </param>
		/// <param name="mergedParameter"> Will hold the merged parameter </param>
		/// <returns> </returns>
		internal static bool MergeParametersByCombination( MergeCombination combination, List<Parameter>[] paramsTable,
		                                                   out MergeParameter mergedParameter )
		{
			mergedParameter = new MergeParameter();
			//Make sure we have enough parameters to combine.
			if ( combination.srcParameterTypeCount[ 0 ] > paramsTable[ 0 ].Count ||
			     combination.srcParameterTypeCount[ 1 ] > paramsTable[ 1 ].Count ||
			     combination.srcParameterTypeCount[ 2 ] > paramsTable[ 2 ].Count ||
			     combination.srcParameterTypeCount[ 3 ] > paramsTable[ 3 ].Count )
			{
				return false;
			}

			//Create the new output parameter.
			for ( int i = 0; i < 4; ++i )
			{
				var curParamList = paramsTable[ i ];
				int srcParameterTypeCount = combination.srcParameterTypeCount[ i ];
				int srcParameterCount = 0;

				while ( srcParameterCount > 0 )
				{
					mergedParameter.AddSourceParameter( curParamList[ curParamList.Count - 1 ],
					                                    combination.srcParameterMask[ srcParameterCount ] );
					curParamList.RemoveAt( curParamList.Count - 1 );
					srcParameterCount++;
					--srcParameterTypeCount;
				}
			}

			return true;
		}

		/// <summary>
		///   Merge reminders parameters that could not be merged into one slot using the predefined combinations.
		/// </summary>
		/// <param name="paramsTable"> The params table sorted by types in each row. </param>
		/// <param name="mergedParams"> The merged parameters list </param>
		/// <param name="splitParams"> The split parameters list </param>
		internal static void MergeParametersReminders( List<Parameter>[] paramsTable, List<MergeParameter> mergedParams,
		                                               List<Parameter> splitParams )
		{
			//Handle reminders parameters - All of the parameters that could not be packed perfectly.
			int mergedParamsBaseIndex = mergedParams.Count;
			int remindersFloatCount = ( 1*paramsTable[ 0 ].Count ) + ( 2*paramsTable[ 1 ].Count ) +
			                          ( 3*paramsTable[ 2 ].Count ) + ( 4*paramsTable[ 3 ].Count );
			int reminderFloatMod = remindersFloatCount%4;
			int remindersFullSlotCount = remindersFloatCount/4;
			int remindersPartialSlotCount = reminderFloatMod > 0 ? 1 : 0;
			int remindersTotalSlotCount = remindersFullSlotCount + remindersPartialSlotCount;

			//First pass -> fill the slots with the largest reminders paramters.
			for ( int slot = 0; slot < remindersTotalSlotCount; ++slot )
			{
				MergeParameter curMergeParam;

				for ( int row = 0; row < 4; ++row )
				{
					List<Parameter> curParamList = paramsTable[ 3 - row ];

					//Case this list contains parameters - remove it and add to merged params.
					if ( curParamList.Count > 0 )
					{
						curMergeParam = new MergeParameter();
						curMergeParam.AddSourceParameter( curParamList[ curParamList.Count - 1 ],
						                                  (int)Operand.OpMask.All );
						curParamList.RemoveAt( curParamList.Count - 1 );
						mergedParams.Add( curMergeParam );
					}
				}
			}

			//Second pass -> merge the reminders parameters.
			for ( int row = 0; row < 4; ++row )
			{
				var curParamList = paramsTable[ 3 - row ];

				//Merge the all the paramters of the current list.
				while ( curParamList.Count > 0 )
				{
					Parameter srcParameter = curParamList[ curParamList.Count - 1 ];
					int splitCount = 0; // How many times the source paramter has been split.
					int srcParamterComponents;
					int srcParameterFloats;
					int curSrcParameterFloats;

					srcParameterFloats = GetParameterFloatCount( srcParameter.Type );
					curSrcParameterFloats = srcParameterFloats;
					srcParamterComponents = GetParameterMaskByType( srcParameter.Type );

					//while this parameter has remaining components -> split it.
					while ( curSrcParameterFloats > 0 )
					{
						for ( int slot = 0; slot < remindersTotalSlotCount && curSrcParameterFloats > 0; ++slot )
						{
							MergeParameter curMergeParam = mergedParams[ mergedParamsBaseIndex + slot ];
							int freeFloatCount = 4 - curMergeParam.UsedFloatCount;

							//Case this slot has free space.
							if ( freeFloatCount > 0 )
							{
								//Case current components of source paramter can go all into this slot without split.
								if ( srcParameterFloats < freeFloatCount && splitCount == 0 )
								{
									curMergeParam.AddSourceParameter( srcParameter, (int)Operand.OpMask.All );
								}
								//case we have to split the current source parameter
							}
							else
							{
								int srcComponentMask;

								//Create the mask that tell us which part of the source componetn is added to the merged paramter.
								srcComponentMask = GetParameterMaskByFloatCount( freeFloatCount ) << splitCount;

								//Add the partial source paramter to merged paramter.
								curMergeParam.AddSourceParameter( srcParameter, srcComponentMask & srcParamterComponents );
							}
							splitCount++;

							//Update left floats count
							if ( srcParameterFloats < freeFloatCount )
							{
								curSrcParameterFloats -= srcParameterFloats;
							}
							else
							{
								curSrcParameterFloats -= freeFloatCount;
							}
						}
					}
					//Add to split params list
					if ( splitCount > 1 )
					{
						splitParams.Add( srcParameter );
					}

					curParamList.RemoveAt( curParamList.Count - 1 );
				}
			}
		}

		/// <summary>
		///   Builds a map between parameter and all the references to it
		/// </summary>
		/// <param name="funcAtomList"> </param>
		/// <param name="paramsRefMap"> </param>
		internal static void BuildParameterReferenceMap( List<FunctionAtom> funcAtomList,
		                                                 Dictionary<Parameter, List<Operand>> paramsRefMap )
		{
			for ( int i = 0; i < funcAtomList.Count; i++ )
			{
				FunctionAtom curAtom = funcAtomList[ i ];

				//Deal only with the function invocations.
				if ( curAtom is FunctionInvocation )
				{
					var curFuncInvocation = curAtom as FunctionInvocation;
					var funcOperands = curFuncInvocation.OperandList;

					for ( int op = 0; op < funcOperands.Count; op++ )
					{
						Operand curOperand = funcOperands[ op ];

						paramsRefMap[ curOperand.Parameter ].Add( curOperand );
					}
				}
			}
		}

		/// <summary>
		///   Replace references to old parameters with the new merged parameters.
		/// </summary>
		/// <param name="mergedParams"> </param>
		/// <param name="paramsRefMap"> </param>
		internal static void ReplaceParametersReferences( List<MergeParameter> mergedParams,
		                                                  Dictionary<Parameter, List<Operand>> paramsRefMap )
		{
			for ( int i = 0; i < mergedParams.Count; i++ )
			{
				MergeParameter curMergeParameter = mergedParams[ i ];
				int paramBitMaskOffset = 0;

				for ( int j = 0; j < curMergeParameter.SourceParameterCount; j++ )
				{
					Parameter curSrcParam = curMergeParameter.SourceParameter[ j ];

					//Case the source parameter has some references
					if ( paramsRefMap.ContainsKey( curSrcParam ) )
					{
						List<Operand> srcParamRefs = paramsRefMap[ curSrcParam ];
						Parameter dstParameter;

						//Case the source paramter is fully contained within the destination merged parameter.
						if ( curMergeParameter.GetSourceParameterMask( j ) == (int)Operand.OpMask.All )
						{
							dstParameter = curMergeParameter.GetDestinationParameter( (int)Operand.OpSemantic.InOut, i );

							for ( int op = 0; op < srcParamRefs.Count; op++ )
							{
								Operand srcOperand = srcParamRefs[ op ];
								int dstOpMask;

								if ( srcOperand.Mask == (int)Operand.OpMask.All )
								{
									//Case the merged parameter contains only one source - no point in adding special mask.
									if ( curMergeParameter.SourceParameterCount == 1 )
									{
										dstOpMask = (int)Operand.OpMask.All;
									}
									else
									{
										dstOpMask = GetParameterMaskByType( curSrcParam.Type ) << paramBitMaskOffset;
									}
								}
								else
								{
									dstOpMask = srcOperand.Mask << paramBitMaskOffset;
								}
								// Replace the original source operand with a new operand the reference the new merged parameter.						
								srcOperand = new Operand( dstParameter, srcOperand.Semantic, dstOpMask, 0 );
							}
						}
					}

					//Update the bit mask offset
					paramBitMaskOffset += GetParameterFloatCount( curSrcParam.Type );
				}
			}
		}

		/// <summary>
		///   Replace references to old parameters that have been split with the new local parameters that represents them.
		/// </summary>
		/// <param name="mergedParams"> </param>
		/// <param name="paramsRefMap"> </param>
		internal static void ReplaceSplitParametersReferences( Dictionary<Parameter, Parameter> localParamsMap,
		                                                       Dictionary<Parameter, List<Operand>> paramsRefMap )
		{
			foreach ( var it in localParamsMap )
			{
				Parameter curSrcParam = it.Key;

				if ( paramsRefMap.ContainsKey( curSrcParam ) )
				{
					Parameter dstParameter = it.Value;
					var srcParamRefs = paramsRefMap[ curSrcParam ];

					for ( int i = 0; i < srcParamRefs.Count; i++ )
					{
						Operand srcOperand = srcParamRefs[ i ];
						int dstOpMask;

						if ( srcOperand.Mask == (int)Operand.OpMask.All )
						{
							dstOpMask = GetParameterMaskByType( curSrcParam.Type );
						}
						else
						{
							dstOpMask = srcOperand.Mask;
						}

						//Replace the original source operand with a new operand the reference the new merged parameter
						srcOperand = new Operand( dstParameter, srcOperand.Semantic, dstOpMask, 0 );
					}
				}
			}
		}

		/// <summary>
		///   Bind the auto parameters for a given CPU and GPU program set.
		/// </summary>
		/// <param name="pCpuProgram"> </param>
		/// <param name="pGpuProgram"> </param>
		internal void BindAutoParameters( Program pCpuProgram, GpuProgram pGpuProgram )
		{
			var gpuParams = pGpuProgram.DefaultParameters;
			var progParams = pCpuProgram.Parameters;

			for ( int itParams = 0; itParams < progParams.Count; ++itParams )
			{
				UniformParameter curParam = progParams[ itParams ];

				var gpuConstDef = gpuParams.FindNamedConstantDefinition( curParam.Name );

				if ( gpuConstDef != null )
				{
					if ( curParam.IsAutoConstantParameter )
					{
						if ( curParam.IsAutoConstantRealParameter )
						{
							gpuParams.SetNamedAutoConstantReal( curParam.Name,
							                                    curParam.AutoConstantType,
							                                    curParam.AutoConstantRealData );
						}
						else if ( curParam.IsAutoConstantIntParameter )
						{
							gpuParams.SetNamedAutoConstant( curParam.Name,
							                                curParam.AutoConstantType,
							                                curParam.AutoConstantIntData );
						}
					}
				}
					//Case this is not auto constant - we have to update its variablity ourself.
				else
				{
					gpuConstDef.Variability |= (GpuProgramParameters.GpuParamVariability)curParam.Variablity;

					//update variability in the float map.
					if ( gpuConstDef.IsSampler == false )
					{
						var floatLogical = gpuParams.FloatLogicalBufferStruct;
						if ( floatLogical != null )
						{
							for ( int i = 0; i < floatLogical.Map.Count; i++ )
							{
								if ( floatLogical.Map[ i ].PhysicalIndex == gpuConstDef.PhysicalIndex )
								{
									floatLogical.Map[ i ].Variability |= gpuConstDef.Variability;
									break;
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		///   Returns the number of floats needed by the given type.
		/// </summary>
		/// <param name="type"> </param>
		/// <returns> </returns>
		protected static int GetParameterFloatCount( Axiom.Graphics.GpuProgramParameters.GpuConstantType type )
		{
			int floatCount = 0;
			switch ( type )
			{
				case GpuProgramParameters.GpuConstantType.Float1:
					floatCount = 1;
					break;
				case GpuProgramParameters.GpuConstantType.Float2:
					floatCount = 2;
					break;
				case GpuProgramParameters.GpuConstantType.Float3:
					floatCount = 3;
					break;
				case GpuProgramParameters.GpuConstantType.Float4:
					floatCount = 4;
					break;
				default:
					throw new Axiom.Core.AxiomException( "Invalid parameter float type." );
			}

			return floatCount;
		}

		/// <summary>
		///   Returns the parameter mask of by the given parameter type.
		///   <example>
		///     X|Y for FLOAT2 etc..
		///   </example>
		/// </summary>
		/// <param name="type"> </param>
		/// <returns> </returns>
		protected static int GetParameterMaskByType( Axiom.Graphics.GpuProgramParameters.GpuConstantType type )
		{
			int paramMask = 0;

			switch ( type )
			{
				case GpuProgramParameters.GpuConstantType.Float1:
					paramMask = (int)Operand.OpMask.X;
					break;
				case GpuProgramParameters.GpuConstantType.Float2:
					paramMask = ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y );
					break;
				case GpuProgramParameters.GpuConstantType.Float3:
					paramMask = ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z );
					break;
				case GpuProgramParameters.GpuConstantType.Float4:
					paramMask = ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z |
					              (int)Operand.OpMask.W );
					break;
				default:
					throw new Axiom.Core.AxiomException( "Invalid paramter float type." );
			}

			return paramMask;
		}

		#endregion

		#region Public Method

		public static int GetParameterMaskByFloatCount( int count )
		{
			int paramMask = 0;
			switch ( count )
			{
				case 1:
					paramMask = (int)Operand.OpMask.X;
					break;
				case 2:
					paramMask = ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y );
					break;
				case 3:
					paramMask = ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z );
					break;
				case 4:
					paramMask = ( (int)Operand.OpMask.X | (int)Operand.OpMask.Y | (int)Operand.OpMask.Z |
					              (int)Operand.OpMask.W );
					break;
				default:
					throw new Axiom.Core.AxiomException( "Invalid parameter float type" );
			}

			return paramMask;
		}

		#endregion

		#region Structs

		public struct MergeCombination
		{
			public int[] srcParameterTypeCount;
			//The count of each source type. I.E. (1 FLOAT1, 0 FLOAT2, 1 FLOAT3, 0 FLOAT4).

			public int[] srcParameterMask;
			// source parameters mask. OPM_ALL means all fields used, otherwise it is split source parameter.

			public MergeCombination(
				int float1Count, int float1Mask,
				int float2Count, int float2Mask,
				int float3Count, int float3Mask,
				int float4Count, int float4Mask )
			{
				srcParameterTypeCount = new int[4];
				srcParameterMask = new int[4];

				srcParameterTypeCount[ 0 ] = float1Count;
				srcParameterTypeCount[ 1 ] = float2Count;
				srcParameterTypeCount[ 2 ] = float3Count;
				srcParameterTypeCount[ 3 ] = float4Count;
				srcParameterMask[ 0 ] = float1Mask;
				srcParameterMask[ 1 ] = float2Mask;
				srcParameterMask[ 2 ] = float3Mask;
				srcParameterMask[ 3 ] = float4Mask;
			}
		}

		#endregion

		public abstract string TargetLanguage { get; }

		public virtual void Dispose()
		{
		}
	}
}