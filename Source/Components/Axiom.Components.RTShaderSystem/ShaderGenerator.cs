using System;
using System.Collections.Generic;
using System.Linq;
using Axiom.Core;
using Axiom.Core.Collections;
using Axiom.Graphics;
using Axiom.Scripting.Compiler;
using Axiom.Scripting.Compiler.AST;
using Axiom.Serialization;

namespace Axiom.Components.RTShaderSystem
{
	public class ShaderGenerator
	{
		#region Fields

		public enum OutputsCompactPolicy
		{
			Low,
			Medium
		}

		public static string DefaultSchemeName;
		private List<SGPass> SGPassList = new List<SGPass>();
		private Dictionary<SGTechnique, SGTechnique> SGTechniqueMap;
		private Tuple<MatGroupPair, SGMaterial> SGMaterialMap;
		private Dictionary<string, SGScheme> SGSchemeMap;
		private Dictionary<string, SGScriptTranslator> SGScriptTranslatorMap;

		private static ShaderGenerator instance;
		private SceneManager activeSceneMgr;
		private Dictionary<string, SceneManager> sceneManagerMap;
		private SgScriptTranslatorManager scriptTranslatorManager;
		private Dictionary<string, SGScriptTranslator> scriptTranslatorMap;
		private SGScriptTranslator coreScriptTranslator;
		private string shaderLanguage;
		private string vertexShaderProfiles;
		private string[] vertexShaderProfilesList;
		private string fragmentShaderProfiles;
		private string[] fragmentShaderProfilesList;
		private string shaderCachePath;
		private ProgramManager programManager;
		private ProgramWriterManager programWriteManager;
		private FFPRenderStateBuilder ffpRenderStateBuilder;
		private List<Tuple<MatGroupPair, SGMaterial>> materialEntriesMap;
		private Dictionary<string, SGScheme> schemeEntriesMap;
		//List<Tuple<SGTechnique, SGTechnique>> techniqueEntriesMap;
		private Dictionary<SGTechnique, SGTechnique> techniqueEntriesMap;
		private Dictionary<string, SubRenderStateFactory> subRenderStateFactories;
		private Dictionary<string, SubRenderStateFactory> subRenderStateExFactories;
		private bool activeViewportValid;
		private readonly int[] lightCount = new int[3];
		private bool isFinalizing;
		private string GeneratedShadersGroupName = "ShaderGeneratorResourceGroup";

		#endregion

		#region C'tor

		public ShaderGenerator()
		{
			ShaderGenerator.DefaultSchemeName = "ShaderGeneratorDefaultScheme";
			ShaderGenerator.SGPass.UserKey = "SGPass";
			ShaderGenerator.SGTechnique.UserKey = "SGTechnique";
			programWriteManager = null;
			programManager = null;
			ffpRenderStateBuilder = null;
			activeSceneMgr = null;
			scriptTranslatorManager = null;
			activeViewportValid = false;
			lightCount[ 0 ] = 0;
			lightCount[ 1 ] = 0;
			lightCount[ 2 ] = 0;
			VertexShaderOutputsCompactPolicy = OutputsCompactPolicy.Low;
			CreateShaderOverProgrammablePass = false;
			isFinalizing = false;
			shaderLanguage = string.Empty;

			HighLevelGpuProgramManager hmgr = HighLevelGpuProgramManager.Instance;

			if ( hmgr.IsLanguageSupported( "cg" ) )
			{
				shaderLanguage = "cg";
			}
			else if ( hmgr.IsLanguageSupported( "glsl" ) )
			{
				shaderLanguage = "glsl";
			}
			else if ( hmgr.IsLanguageSupported( "glsles" ) )
			{
				shaderLanguage = "glsles";
			}
			else if ( hmgr.IsLanguageSupported( "hlsl" ) )
			{
				shaderLanguage = "hlsl";
			}
			else
			{
				// ASSAF: This is disabled for now - to stop an exception on the iOS
				// when running with the OpenGL ES 1.x that doesn't support shaders...
				/*
         * throw new Axiom.Core.AxiomException("ShaderGeneration creation enrror: None of the profiles is supported");
         */
				shaderLanguage = "cg";
			}

			VertexShaderProfiles = ( "gpu_vp gp4vp vp40 vp30 arbvp1 vs_4_0 vs_3_0 vs_2_x vs_2_a vs_2_0 vs_1_1" );
			FragmentShaderProfiles =
				( "ps_4_0 ps_3_x ps_3_0 fp40 fp30 fp20 arbfp1 ps_2_x ps_2_a ps_2_b ps_2_0 ps_1_4 ps_1_3 ps_1_2 ps_1_1" );
		}

		#endregion

		#region Public Methods

		/// <summary>
		///   Initializes the Shader Generator System.
		/// </summary>
		/// <returns> True upon succcess </returns>
		public static bool Initialize()
		{
			if ( ShaderGenerator.instance == null )
			{
				ShaderGenerator.instance = new ShaderGenerator();
				if ( ShaderGenerator.instance._initialize() )
				{
					ShaderGenerator.instance = null;
					return false;
				}
			}
			return true;
		}

		/// <summary>
		///   finalize the Shader Generator instance
		/// </summary>
		public static void Finalize()
		{
			if ( instance != null )
			{
				instance = null;
			}
		}

		/// <summary>
		///   Add a scene manager to the shader generator scene managers list
		/// </summary>
		/// <param name="sceneMgr"> The scene manager to add to the list. </param>
		public void AddSceneManager( SceneManager sceneMgr )
		{
			//make sure this doesn't already exist in the map
			if ( sceneManagerMap.ContainsKey( sceneMgr.Name ) )
			{
				return;
			}

			//TODO hook events

			sceneManagerMap.Add( sceneMgr.Name, sceneMgr );

			if ( activeSceneMgr == null )
			{
				activeSceneMgr = sceneMgr;
			}
		}

		/// <summary>
		///   Removes a scene manager from the shader generator scene managers list.
		/// </summary>
		/// <param name="sceneMgr"> The scene manager to remove from the list </param>
		public void RemoveSceneManager( SceneManager sceneMgr )
		{
			//make sure this scene manager exists in the map
			if ( sceneManagerMap.ContainsKey( sceneMgr.Name ) == false )
			{
				return;
			}

			//TODO unhook events

			sceneManagerMap.Remove( sceneMgr.Name );

			//Update the active scene mangager.
			if ( activeSceneMgr == sceneMgr )
			{
				activeSceneMgr = null;
			}
		}

		/// <summary>
		///   Flush the shader cache. this operation will cause all active sachems to be invalidated and will destroy any CPU/GPU program that was creted by this shader geneartor.
		/// </summary>
		public void FlushShaderCache()
		{
			//release all programs
			foreach ( var key in techniqueEntriesMap.Keys )
			{
				techniqueEntriesMap[ key ].ReleasePrograms();
			}

			ProgramManager.Instance.FlushGpuProgramsCache();

			//invalidate all schemes
			foreach ( var key in schemeEntriesMap.Keys )
			{
				schemeEntriesMap[ key ].Invalidate();
			}
		}

		/// <summary>
		///   Returns a requested render state. If the render state does not exist, this function creates it
		/// </summary>
		/// <param name="schemeName"> The scheme name to retrieve </param>
		/// <returns> </returns>
		public Tuple<RenderState, bool> CreateOrRetrieveRenderState( string schemeName )
		{
			var res = CreateOrRetrieveScheme( schemeName );
			return new Tuple<RenderState, bool>( res.Item1.RenderState, res.Item2 );
		}

		/// <summary>
		///   Tells if a given render state exists.
		/// </summary>
		/// <param name="schemeName"> The scheme name to check. </param>
		/// <returns> </returns>
		public bool HasRenderState( string schemeName )
		{
			return schemeEntriesMap.ContainsKey( schemeName );
		}

		/// <summary>
		///   Returns a global render state associated with the given scheme name. Modifying this render state will affect all techniques that belongs to that scheme. This is the best way to apply global chagnes to all techniques. After altering the render state one should call invalidateScheme method in order to regerenarte shaders.
		/// </summary>
		/// <param name="schemeName"> </param>
		/// <returns> The destination scheme name </returns>
		public RenderState GetRenderState( string schemeName )
		{
			if ( schemeEntriesMap.ContainsKey( schemeName ) == false )
			{
				throw new Axiom.Core.AxiomException( "A scheme named " + schemeName + "doesn't exist" );
			}

			return schemeEntriesMap[ schemeName ].RenderState;
		}

		public RenderState GetRenderState( string schemeName, string materialName, ushort passIndex )
		{
			return GetRenderState( schemeName, materialName, ResourceGroupManager.AutoDetectResourceGroupName, passIndex );
		}

		/// <summary>
		///   Gets render state of specific pass. Using this method allows the user to customize the behavior of a specific pass.
		/// </summary>
		/// <param name="schemeName"> The destination scheme name. </param>
		/// <param name="materialName"> The specific material name. </param>
		/// <param name="groupName"> The specific group name </param>
		/// <param name="passIndex"> The pass index. </param>
		/// <returns> </returns>
		public RenderState GetRenderState( string schemeName, string materialName, string groupName, ushort passIndex )
		{
			if ( schemeEntriesMap.ContainsKey( schemeName ) == false )
			{
				throw new Axiom.Core.AxiomException( "A scheme named " + schemeName + " doesn't exist." );
			}
			var schemeEntry = schemeEntriesMap[ schemeName ];

			return schemeEntry.GetRenderState( materialName, groupName, passIndex );
		}

		/// <summary>
		///   Add sub render state factory. Plugins or 3rd party applications may implement sub classes of subRenderState interface. Add the matching factory will allow the application to create instances of these sub classes.
		/// </summary>
		/// <param name="factory"> The factory to add </param>
		public void AddSubRenderStateFactory( SubRenderStateFactory factory )
		{
			if ( subRenderStateFactories.Keys.Contains( factory.Type ) )
			{
				throw new Axiom.Core.AxiomException( "A factory of type " + factory.Type + " already exists." );
			}

			subRenderStateFactories.Add( factory.Type, factory );
		}

		/// <summary>
		///   Returns a sub render state factory by index
		/// </summary>
		/// <param name="index"> </param>
		/// <returns> </returns>
		public SubRenderStateFactory GetSubRenderStateFactory( int index )
		{
			int i = 0;
			foreach ( var key in subRenderStateFactories.Keys )
			{
				if ( i == index )
				{
					return subRenderStateFactories[ key ];
					break;
				}
				i++;
			}

			throw new AxiomException( "A factory on index " + index.ToString() + " does not exist." );
		}

		/// <summary>
		///   Returns a sub render state factory by name
		/// </summary>
		/// <param name="type"> </param>
		/// <returns> </returns>
		public SubRenderStateFactory GetSubRenderStateFactory( string type )
		{
			return subRenderStateFactories[ type ];
		}

		/// <summary>
		///   Remove sub render state factory.
		/// </summary>
		/// <param name="factory"> The factory to remove </param>
		public void RemoveSubRenderStateFactory( SubRenderStateFactory factory )
		{
			subRenderStateFactories.Remove( factory.Type );
		}

		/// <summary>
		///   Creates an instance of sub render state from a given type
		/// </summary>
		/// <param name="type"> The type of sub render state to create </param>
		/// <returns> </returns>
		public SubRenderState CreateSubRenderState( string type )
		{
			foreach ( var key in subRenderStateFactories.Keys )
			{
				return subRenderStateFactories[ key ].CreateInstance();
			}

			throw new Axiom.Core.AxiomException( "A factory of type " + type + " doesn't exist" );
		}

		/// <summary>
		///   Destoys an instance of sub render state.
		/// </summary>
		/// <param name="subRenderState"> The instance to destroy </param>
		public void DestroySubRenderState( SubRenderState subRenderState )
		{
			var factoryThatCreatedSRS = subRenderStateFactories[ subRenderState.Type ];
			factoryThatCreatedSRS.DestroyInstance( subRenderState );
		}

		public bool HasShaderBasedTechnique( string materialName, string srcTechniqueSchemeName,
		                                     string dstTechniqueSchemeName )
		{
			return HasShaderBasedTechnique( materialName, ResourceGroupManager.AutoDetectResourceGroupName,
			                                srcTechniqueSchemeName, dstTechniqueSchemeName );
		}

		/// <summary>
		///   Checks if a shader based techniuqe has been created for a given technique.
		/// </summary>
		/// <param name="materialName"> The source material name </param>
		/// <param name="groupName"> The source group name. </param>
		/// <param name="srcTechnqiueSchemeName"> The source techniuqe scheme name </param>
		/// <param name="dstTechniqueSchemeName"> The destination shader based technique scheme name. </param>
		/// <returns> True if exist. False if not. </returns>
		public bool HasShaderBasedTechnique( string materialName, string groupName, string srcTechnqiueSchemeName,
		                                     string dstTechniqueSchemeName )
		{
			if ( MaterialManager.Instance.ResourceExists( materialName ) == false )
			{
				return false;
			}

			foreach ( var matEntryIt in materialEntriesMap )
			{
				var techniqueEntries = matEntryIt.Item2.TechniqueList;

				for ( int i = 0; i < techniqueEntries.Count; i++ )
				{
					var techEntry = techniqueEntries[ i ];
					//check requested mapping already exists
					if ( techEntry.SourceTechnique.SchemeName == srcTechnqiueSchemeName &&
					     techEntry.DestinationTechniqueSchemeName == dstTechniqueSchemeName )
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool CreateShaderBasedTechnique( string materialName, string srcTechniqueSchemeName,
		                                        string dstTechniqueSchemeName, bool overProgrammable )
		{
			return HasShaderBasedTechnique( materialName, ResourceGroupManager.AutoDetectResourceGroupName,
			                                srcTechniqueSchemeName, dstTechniqueSchemeName );
		}

		/// <summary>
		///   Create shader based technique from a given technique.
		/// </summary>
		/// <param name="materialName"> The source material name </param>
		/// <param name="groupName"> The source group name </param>
		/// <param name="srcTechniqueSchemeName"> The source technique scheme name </param>
		/// <param name="dstTechniqueSchemeName"> The destination shader based technique scheme name. </param>
		/// <param name="overProgrammable"> </param>
		/// <returns> True on success. Failure may occur if the source technique is not FFP pure, or different source technique is mapped to the requested destination scheme. </returns>
		public bool CreateShaderBasedTechnique( string materialName, string groupName, string srcTechniqueSchemeName,
		                                        string dstTechniqueSchemeName, bool overProgrammable )
		{
			var srcMat = (Material)MaterialManager.Instance.GetByName( materialName );
			if ( srcMat == null )
			{
				return false;
			}

			string trueGroupName = srcMat.Group;

			//Case the requested material belongs to a different group and it is not autodetect_resource_group
			if ( trueGroupName != groupName && groupName != ResourceGroupManager.AutoDetectResourceGroupName )
			{
				return false;
			}

			foreach ( var itMatEntry in materialEntriesMap )
			{
				var techniqueEntries = itMatEntry.Item2.TechniqueList;

				foreach ( var techEntry in techniqueEntries )
				{
					if ( techEntry.SourceTechnique.SchemeName == srcTechniqueSchemeName &&
					     techEntry.DestinationTechniqueSchemeName == dstTechniqueSchemeName )
					{
						return true;
					}
						//Case a shader based technique with the same scheme name already defined based
						//on a different source technique.
						//This state might lead to conflicts during shader generartion - we prevent it by returning false here.
					else if ( techEntry.DestinationTechniqueSchemeName == dstTechniqueSchemeName )
					{
						return false;
					}
				}
			}
			//no technique created => check if one can be crated from the given source technique scheme.
			Technique srcTechnique = null;
			srcTechnique = FindSourceTechnique( materialName, trueGroupName, srcTechniqueSchemeName );

			//No appropriate source technique found.
			if ( srcTechnique == null || ( ( overProgrammable == false ) && ( IsProgrammable( srcTechnique ) == true ) ) )
			{
				return false;
			}

			//Create shader based technique from the given source technique
			SGMaterial matEntry = null;


			foreach ( var itMatEntry in materialEntriesMap )
			{
				if ( itMatEntry == materialEntriesMap[ materialEntriesMap.Count - 1 ] )
				{
					matEntry = new SGMaterial( materialName, trueGroupName );
					materialEntriesMap.Add(
						new Tuple<MatGroupPair, SGMaterial>( new MatGroupPair( materialName, trueGroupName ), matEntry ) );
				}
				else
				{
					matEntry = matEntry = itMatEntry.Item2;
				}
			}
			//create the new technique entry
			var sgTechEntry = new SGTechnique( matEntry, srcTechnique, dstTechniqueSchemeName );

			//Add to material entry map.
			matEntry.TechniqueList.Add( sgTechEntry );

			//Add to all technique map
			techniqueEntriesMap.Add( sgTechEntry, sgTechEntry );

			//add to scheme
			SGScheme schemeEntry = CreateOrRetrieveScheme( dstTechniqueSchemeName ).Item1;
			schemeEntry.AddTechniqueEntry( sgTechEntry );

			return true;
		}

		private bool IsProgrammable( Technique srcTechnique )
		{
			if ( srcTechnique != null )
			{
				for ( int i = 0; i < srcTechnique.PassCount; i++ )
				{
					if ( srcTechnique.GetPass( i ).IsProgrammable == true )
					{
						return true;
					}
				}
			}

			return false;
		}

		public Technique FindSourceTechnique( string materialName, string groupName, string srcTechniqueSchemeName )
		{
			var mat = (Material)MaterialManager.Instance.GetByName( materialName );

			for ( int i = 0; i < mat.TechniqueCount; i++ )
			{
				Technique curTechnique = mat.GetTechnique( i );

				if ( curTechnique.SchemeName == srcTechniqueSchemeName )
				{
					return curTechnique;
				}
			}
			return null;
		}

		/// <summary>
		///   Remove shader based technique from a given technique.
		/// </summary>
		/// <param name="materialName"> The source material name. </param>
		/// <param name="groupName"> The source group name. </param>
		/// <param name="srcTechniqueSchemeName"> The source technique scheme name. </param>
		/// <param name="dstTechniqueSchemeName"> The destination shader based technique scheme name. </param>
		/// <returns> True on success. Failure may occur if given source technique was not previously registered successfully using the CreateShaderBasedTechnique method.>> </returns>
		public bool RemoveShaderBasedTechnique( string materialName, string groupName, string srcTechniqueSchemeName,
		                                        string dstTechniqueSchemeName )
		{
			SGScheme schemeEntry = null;

			//make sure scheme exists
			if ( schemeEntriesMap.ContainsKey( dstTechniqueSchemeName ) == false )
			{
				return false;
			}
			else
			{
				schemeEntry = schemeEntriesMap[ dstTechniqueSchemeName ];
			}

			SGTechnique dstTechnique = null;

			foreach ( var itMatEntry in materialEntriesMap )
			{
				var techniqueEntries = itMatEntry.Item2.TechniqueList;


				//Remove destination technique entry from material techiniques list
				foreach ( var itTechEntry in techniqueEntries )
				{
					if ( itTechEntry.SourceTechnique.SchemeName == srcTechniqueSchemeName &&
					     itTechEntry.DestinationTechniqueSchemeName == dstTechniqueSchemeName )
					{
						dstTechnique = itTechEntry;
						techniqueEntries.Remove( itTechEntry );
						break;
					}
				}
			}
			//Technique not found.
			if ( dstTechnique == null )
			{
				return false;
			}

			schemeEntry.RemoveTechniqueEntry( dstTechnique );
			var itTechMap = techniqueEntriesMap[ dstTechnique ];
			techniqueEntriesMap.Remove( itTechMap );
			dstTechnique.Dispose();
			dstTechnique = null;

			return true;
		}

		public bool RemoveShaderBasedTechnique( string materialName, string srcTechniqueSchemeName,
		                                        string dstTechniqueSchemeName )
		{
			return RemoveShaderBasedTechnique( materialName, ResourceGroupManager.AutoDetectResourceGroupName,
			                                   srcTechniqueSchemeName, dstTechniqueSchemeName );
		}

		/// <summary>
		///   Removes all shader based techniques of the given material.
		/// </summary>
		/// <param name="materialName"> The source material name </param>
		/// <param name="groupName"> The source group name </param>
		/// <returns> True on success </returns>
		public bool RemoveAllShaderBasedTechniques( string materialName, string groupName )
		{
			foreach ( var itMatEntry in materialEntriesMap )
			{
				//Case material not found
				if ( materialEntriesMap.Contains( itMatEntry ) == false )
				{
					return false;
				}

				var matTechniqueEntries = itMatEntry.Item2.TechniqueList;

				while ( matTechniqueEntries.Count > 0 )
				{
					foreach ( var itTechEntry in matTechniqueEntries )
					{
						RemoveShaderBasedTechnique( materialName, itMatEntry.Item1.Item2,
						                            itTechEntry.SourceTechnique.SchemeName,
						                            itTechEntry.DestinationTechniqueSchemeName );
					}
				}
				materialEntriesMap.Remove( itMatEntry );
			}

			return true;
		}

		public bool RemoveAllShaderBasedTechniques( string materialName )
		{
			return RemoveAllShaderBasedTechniques( materialName, ResourceGroupManager.AutoDetectResourceGroupName );
		}

		/// <summary>
		///   Removes all shader based techniques that created by this shader generator.
		/// </summary>
		public void RemoveAllShaderBasedTechniques()
		{
			for ( int i = 0; i < materialEntriesMap.Count; i++ )
			{
				var itMatEntry = materialEntriesMap[ i ];

				RemoveAllShaderBasedTechniques( itMatEntry.Item1.Item1, itMatEntry.Item1.Item2 );
			}
		}

		/// <summary>
		///   Create a scheme.
		/// </summary>
		/// <param name="schemeName"> The scheme name to create. </param>
		public void CreateScheme( string schemeName )
		{
			var schemeEntry = new SGScheme( schemeName );
			schemeEntriesMap.Add( schemeName, schemeEntry );
		}

		/// <summary>
		///   Invalidate a given scheme. This action will lead to shader regeneration of all techniques that belong to the given scheme name.
		/// </summary>
		/// <param name="schemeName"> The scheme to invalidate </param>
		public void InvalidateScheme( string schemeName )
		{
			if ( schemeEntriesMap.ContainsKey( schemeName ) )
			{
				schemeEntriesMap[ schemeName ].Invalidate();
			}
		}

		/// <summary>
		///   Validate a given scheme. This action will generate shader programs for all techniques of the given scheme name.
		/// </summary>
		/// <param name="schemeName"> The scheme to validate </param>
		/// <returns> </returns>
		public bool ValidateScheme( string schemeName )
		{
			if ( schemeEntriesMap.ContainsKey( schemeName ) == false )
			{
				return false;
			}

			schemeEntriesMap[ schemeName ].Validate();
			return true;
		}

		/// <summary>
		///   Invalidates specific material scheme. This action will lead to shader regeneration of the technique beongs to the given scheme name
		/// </summary>
		/// <param name="schemeName"> The scheme to invalidate </param>
		/// <param name="materialName"> The material to invalidate </param>
		public void InvalidateMaterial( string schemeName, string materialName )
		{
			InvalidateMaterial( schemeName, materialName, ResourceGroupManager.AutoDetectResourceGroupName );
		}

		public void InvalidateMaterial( string schemeName, string materialName, string groupName )
		{
			schemeEntriesMap[ schemeName ].Invalidate( materialName, groupName );
		}

		/// <summary>
		///   Validate specific material scheme. This action will generate shader programs for the technique of the given scheme name.
		/// </summary>
		/// <param name="schemeName"> The scheme to validate </param>
		/// <param name="materialName"> The material to validate </param>
		/// <returns> </returns>
		public bool ValidateMaterial( string schemeName, string materialName )
		{
			return ValidateMaterial( schemeName, materialName, ResourceGroupManager.AutoDetectResourceGroupName );
		}

		public bool ValidateMaterial( string schemeName, string materialName, string groupName )
		{
			if ( schemeEntriesMap.ContainsKey( schemeName ) == false )
			{
				return false;
			}
			return schemeEntriesMap[ schemeName ].Validate( materialName, groupName );
		}

		//todo materialSerializerListener

		/// <summary>
		///   Clone all shader based techniques from one material to another. this function can be used in conjunction with the Material.Clone() function to copy both material properties and RTSS state from one material to another.
		/// </summary>
		/// <param name="srcMaterialName"> The source material name </param>
		/// <param name="srcGroupName"> The source group name </param>
		/// <param name="dstMaterialName"> The destination material name </param>
		/// <param name="dstGroupName"> The destination group name </param>
		/// <returns> True if successful </returns>
		public bool CloneShaderBasedTechniques( string srcMaterialName, string srcGroupName, string dstMaterialName,
		                                        string dstGroupName )
		{
			var srcMat = (Material)MaterialManager.Instance.GetByName( srcMaterialName );
			var dstMat = (Material)MaterialManager.Instance.GetByName( dstMaterialName );

			//make sure material exists
			if ( srcMat == null || dstMat == null || srcMat == dstMat )
			{
				return false;
			}

			//update group name in case it is autodetect
			string trueSrcGroupName = srcMat.Group;
			string trueDstGroupName = dstMat.Group;

			//case the requested material belongs to different group and it is not autodetect
			if ( ( trueSrcGroupName != srcGroupName && srcGroupName != ResourceGroupManager.AutoDetectResourceGroupName ) ||
			     trueSrcGroupName != dstGroupName && dstGroupName != ResourceGroupManager.AutoDetectResourceGroupName )
			{
				return false;
			}
			//remove any techniques in the destination material so the new techniques may be copied
			RemoveAllShaderBasedTechniques( dstMaterialName, trueDstGroupName );

			//remove any techniques from the destination material which have RTSS associated schemes from
			//the source material. This code is performed in case the user performed a clone of a material
			//which has already generated RTSS techniques in the source material.
			var schemesToRemove = new List<int>();
			int techCount = (ushort)srcMat.TechniqueCount;
			for ( ushort ti = 0; ti < techCount; techCount++ )
			{
				Technique pSrcTech = srcMat.GetTechnique( ti );
				Pass pSrcPass = null;
				if ( pSrcTech.PassCount > 0 )
				{
					pSrcPass = pSrcTech.GetPass( 0 );
				}

				if ( pSrcPass != null )
				{
					schemesToRemove.Add( pSrcTech.SchemeIndex );
				}
			}
			//remove the techniques from the destination material
			techCount = (ushort)dstMat.TechniqueCount;
			for ( int ti = techCount - 1; ti > 0; ti-- )
			{
				Technique pDstTech = dstMat.GetTechnique( ti );
				if ( schemesToRemove.Contains( ti ) )
				{
					dstMat.RemoveTechnique( dstMat.GetTechnique( ti ) );
				}
			}

			//Clone the render states from source to destination
			//Check if RTSS techniques exist in the source material
			foreach ( var itSrcMatEntry in materialEntriesMap )
			{
				var techniqueEntries = itSrcMatEntry.Item2.TechniqueList;

				foreach ( var itTechEntry in techniqueEntries )
				{
					string srcFromTechniqueScheme = itTechEntry.SourceTechnique.SchemeName;
					string srcToTechniqueScheme = itTechEntry.DestinationTechniqueSchemeName;

					//for every technique in the source material create a shader based technique in the destination material
					if ( CreateShaderBasedTechnique( dstMaterialName, trueDstGroupName, srcFromTechniqueScheme,
					                                 srcToTechniqueScheme, false ) )
					{
						int passCount = itTechEntry.SourceTechnique.PassCount;
						for ( short pi = 0; pi < passCount; pi++ )
						{
							if ( itTechEntry.HasRenderState( pi ) )
							{
								RenderState srcRenderState = itTechEntry.GetRenderState( pi );
								RenderState dstRenderState = GetRenderState( srcToTechniqueScheme, dstMaterialName,
								                                             trueDstGroupName, (ushort)pi );

								List<SubRenderState> srcSubRenderState = srcRenderState.TemplateSubRenderStateList;

								foreach ( var itSubState in srcSubRenderState )
								{
									SubRenderState srcSubState = itSubState;
									SubRenderState dstSubState = CreateSubRenderState( srcSubState.Type );
									dstSubState = srcSubState;
									dstRenderState.AddTemplateSubRenderState( dstSubState );
								}
							}
						}
					}
				}
			}
			return true;
		}

		#endregion

		#region ProtectedMethods

		/// <summary>
		///   Initialize the shader generator instance
		/// </summary>
		/// <returns> </returns>
		protected bool _initialize()
		{
			programWriteManager = new ProgramWriterManager();
			programManager = new ProgramManager();

			ffpRenderStateBuilder = new FFPRenderStateBuilder();
			if ( ffpRenderStateBuilder.Initialize() == false )
			{
				return false;
			}

			//Create extensions factories
			createSubRenderStateExFactories();

			scriptTranslatorManager = new SgScriptTranslatorManager( this );
			ScriptCompilerManager.Instance.TranslatorManagers.Add( scriptTranslatorManager );


			addCustomScriptTranslator( "rtshader_system", coreScriptTranslator );

			//create the default scheme
			CreateScheme( ShaderGenerator.DefaultSchemeName );

			return true;
		}

		/// <summary>
		///   Finalize the shader generatror instance
		/// </summary>
		protected void _finalize()
		{
			isFinalizing = true;
			//Delete technique entries
			techniqueEntriesMap.Clear();

			//Delete material entries
			materialEntriesMap.Clear();

			//Delete scheme entries
			schemeEntriesMap.Clear();

			//destroy extensions factories
			destroySubRenderStateExFactories();

			//Delete FFP Emulator
			if ( ffpRenderStateBuilder != null )
			{
				ffpRenderStateBuilder.Finalize();
				ffpRenderStateBuilder = null;
			}

			//Delete Program manager
			programManager = null;

			//Delete Program writer manager.
			programWriteManager = null;

			removeCustomScriptTranslator( "rtshader_system" );

			//Delete script translator manager
			if ( scriptTranslatorManager != null )
			{
				ScriptCompilerManager.Instance.TranslatorManagers.Remove( scriptTranslatorManager );
				scriptTranslatorManager = null;
			}


			foreach ( var key in sceneManagerMap.Keys )
			{
				RemoveSceneManager( sceneManagerMap[ key ] );
			}
			sceneManagerMap.Clear();
		}

		/// <summary>
		///   Called when a single object is rendered
		/// </summary>
		/// <param name="rend"> </param>
		/// <param name="pass"> </param>
		/// <param name="source"> </param>
		/// <param name="lightList"> </param>
		/// <param name="suppressRenderStateChanges"> </param>
		protected void notifyRenderSingleObject( IRenderable rend, Pass pass, AutoParamDataSource source,
		                                         LightList lightList, bool suppressRenderStateChanges )
		{
			if ( activeViewportValid )
			{
				//TODO userObjectBindings?
			}
		}

		/// <summary>
		///   Called when finding visible object process starts.
		/// </summary>
		/// <param name="source"> </param>
		/// <param name="irs"> </param>
		/// <param name="v"> </param>
		protected void preFindVisibleObjects( SceneManager source, IlluminationRenderStage irs, Viewport v )
		{
			string curMaterialScheme = v.MaterialScheme;

			activeSceneMgr = source;
			activeViewportValid = ValidateScheme( curMaterialScheme );
		}

		/// <summary>
		///   Create sub render state core extensions factories
		/// </summary>
		protected void createSubRenderStateExFactories()
		{
			SubRenderStateFactory curFactory;

			curFactory = new PerPixelLightingFactory();
			AddSubRenderStateFactory( curFactory );
			subRenderStateExFactories.Add( curFactory.Type, curFactory );

			curFactory = new NormalMapLightingFactory();
			AddSubRenderStateFactory( curFactory );
			subRenderStateExFactories.Add( curFactory.Type, curFactory );

			curFactory = new IntegratedPSSM3Factory();
			AddSubRenderStateFactory( curFactory );
			subRenderStateExFactories.Add( curFactory.Type, curFactory );

			curFactory = new LayerBlendingFactory();
			AddSubRenderStateFactory( curFactory );
			subRenderStateExFactories.Add( curFactory.Type, curFactory );

			curFactory = new HardwareSkinningFactory();
			AddSubRenderStateFactory( curFactory );
			subRenderStateExFactories.Add( curFactory.Type, curFactory );

			curFactory = new TextureAtlasSamplerFactory();
			AddSubRenderStateFactory( curFactory );
			subRenderStateExFactories.Add( curFactory.Type, curFactory );
		}

		/// <summary>
		///   Destroys sub render state core extensions factories
		/// </summary>
		protected void destroySubRenderStateExFactories()
		{
			foreach ( var key in subRenderStateExFactories.Keys )
			{
				RemoveSubRenderStateFactory( subRenderStateExFactories[ key ] );
			}
			subRenderStateExFactories.Clear();
		}

		/// <summary>
		///   Create an instance of the SubRenderState based on script properties using the current sub render state factories
		/// </summary>
		/// <param name="compiler"> The compiler instance </param>
		/// <param name="prop"> The abstract property node. </param>
		/// <param name="texState"> The texture unit state that is the parent context of this node. </param>
		/// <param name="translator"> The translator for the specific SubRenderState </param>
		/// <returns> </returns>
		public SubRenderState createSubRenderState( ScriptCompiler compiler, PropertyAbstractNode prop,
		                                            TextureUnitState texState, SGScriptTranslator translator )
		{
			SubRenderState subRenderState = null;
			foreach ( var key in subRenderStateFactories.Keys )
			{
				subRenderState = subRenderStateFactories[ key ].CreateInstance( compiler, prop, texState, translator );
				if ( subRenderState != null )
				{
					break;
				}
			}
			return subRenderState;
		}

		/// <summary>
		///   Create an instance of the SubRenderState based on script properties using the current sub render state factories
		/// </summary>
		/// <param name="compiler"> The compiler instance </param>
		/// <param name="prop"> The abstract property node. </param>
		/// <param name="pass"> The pass that is the parent context of this node. </param>
		/// <param name="translator"> The translator for the specific SubRenderState </param>
		/// <returns> </returns>
		public SubRenderState createSubRenderState( ScriptCompiler compiler, PropertyAbstractNode prop, Pass pass,
		                                            SGScriptTranslator translator )
		{
			SubRenderState subRenderState = null;
			foreach ( var key in subRenderStateFactories.Keys )
			{
				subRenderState = subRenderStateFactories[ key ].CreateInstance( compiler, prop, pass, translator );
				if ( subRenderState != null )
				{
					break;
				}
			}
			return subRenderState;
		}

		/// <summary>
		///   Add custom script translator
		/// </summary>
		/// <param name="key"> The key name of the given translator. </param>
		/// <param name="translator"> The translator to associate with the given key </param>
		/// <returns> true on success </returns>
		internal bool addCustomScriptTranslator( string key, SGScriptTranslator translator )
		{
			if ( scriptTranslatorMap.ContainsKey( key ) )
			{
				return false;
			}

			scriptTranslatorMap.Add( key, translator );
			return true;
		}

		/// <summary>
		///   Remove ccustom script translator.
		/// </summary>
		/// <param name="key"> The key name of the translator to remove </param>
		/// <returns> True on success </returns>
		protected bool removeCustomScriptTranslator( string key )
		{
			if ( scriptTranslatorMap.ContainsKey( key ) == false )
			{
				return false;
			}

			scriptTranslatorMap.Remove( key );

			return true;
		}

		internal SGScriptTranslator GetTranslator( AbstractNode node )
		{
			SGScriptTranslator translator = null;

			if ( node is ObjectAbstractNode )
			{
				var obj = node as ObjectAbstractNode;
				translator = scriptTranslatorMap[ obj.Cls ];
			}

			return translator;
		}

		protected int NumTranslators
		{
			get
			{
				return scriptTranslatorMap.Count;
			}
		}

		/// <summary>
		///   Serializes a given pass entry attributes
		/// </summary>
		/// <param name="ser"> The material serialzier </param>
		/// <param name="passEntry"> The SGPass instance </param>
		internal void serializePassAttributes( MaterialSerializer ser, SGPass passEntry )
		{
			//TODO
			//write section header and begin it
		}

		/// <summary>
		///   Serialize a given textureUnitState entry attributes.
		/// </summary>
		/// <param name="ser"> The material serializer </param>
		/// <param name="passEntry"> The SGPass instance </param>
		/// <param name="srcTextureUnit"> The TextureUnitState being serialized </param>
		internal void serializeTextureUnitStateAttributes( MaterialSerializer ser, SGPass passEntry,
		                                                   TextureUnitState srcTextureUnit )
		{
			//TODO
		}

		internal Tuple<SGScheme, bool> CreateOrRetrieveScheme( string schemeName )
		{
			bool wasCreated = false;
			SGScheme schemeEntry = null;

			//create
			if ( schemeEntriesMap.ContainsKey( schemeName ) == false )
			{
				schemeEntry = new SGScheme( schemeName );
				schemeEntriesMap.Add( schemeName, schemeEntry );
				wasCreated = true;
			}
			else //retrieve
			{
				schemeEntry = schemeEntriesMap[ schemeName ];
			}

			return new Tuple<SGScheme, bool>( schemeEntry, wasCreated );
		}

		internal string GetRTShaderScheme( int index )
		{
			//make sure index isn't out of bounds
			if ( index < 0 || index >= schemeEntriesMap.Count )
			{
				throw new AxiomException( "Index out of bounds", new IndexOutOfRangeException() );
			}

			int i = 0;

			foreach ( var key in schemeEntriesMap.Keys )
			{
				if ( i == index )
				{
					return key;
				}
				i++;
			}

			throw new AxiomException( "No schemes found" );
		}

		#endregion

		#region Properties

		/// <summary>
		///   Gets the current number of generated vertex shader.
		/// </summary>
		public int VertexShaderCount
		{
			get
			{
				return programManager.VertexShaderCount;
			}
		}

		/// <summary>
		///   Gets the current number of generated fragments shaders.
		/// </summary>
		public int FragmentShaderCount
		{
			get
			{
				return programManager.FragmentShaderCount;
			}
		}

		/// <summary>
		///   Returns the number of exisiting factories
		/// </summary>
		public int NumSubRenderStateFactories
		{
			get
			{
				return subRenderStateFactories.Count;
			}
		}

		/// <summary>
		///   Gets/Sets the vertex shader outputs compaction policy
		/// </summary>
		public OutputsCompactPolicy VertexShaderOutputsCompactPolicy { get; set; }

		/// <summary>
		///   Gets/Sets whether shaders are created for passes with shaders. Note that this only refers to when the system parses the materials itself. Not for when calling the CreateShaderBasedTechnique() function directly
		/// </summary>
		public bool CreateShaderOverProgrammablePass { get; set; }

		public int RTShaderSchemeCount
		{
			get
			{
				return schemeEntriesMap.Count;
			}
		}

		/// <summary>
		///   Gets/Set output shader cache path. Generated shader code will be written to this path. In case of empty cache path, shaders will be generated directly from system memory. The default is empty cache path.
		/// </summary>
		public string ShaderChachePath
		{
			get
			{
				return shaderCachePath;
			}
			set
			{
				string stdCachePath = value;
				if ( shaderCachePath != stdCachePath )
				{
					string path = stdCachePath;
					path = path.Replace( "\\", "/" );
					if ( path[ path.Length - 1 ] != '/' )
					{
						path += '/';
					}
					stdCachePath = path;
				}
				if ( shaderCachePath != stdCachePath )
				{
					//Remove previous cache path.
					if ( shaderCachePath != string.Empty )
					{
						ResourceGroupManager.Instance.RemoveResourceLocation( shaderCachePath, GeneratedShadersGroupName );
					}

					shaderCachePath = stdCachePath;

					//Case this is a valid file path -? add as resource location in order to make sure that 
					//generated shaders could be loaded by the file system archieve
					if ( shaderCachePath != string.Empty )
					{
						string outTestFileName = shaderCachePath + "ShaderGenerator.tst";
						System.IO.StreamWriter outfile = null;
						try
						{
							outfile = new System.IO.StreamWriter( outTestFileName );
						}
						catch
						{
							throw;
						}
						//close and remove the test file
						outfile.Close();
						outfile.Dispose();

						ResourceGroupManager.Instance.AddResourceLocation( shaderCachePath, "FileSystem",
						                                                   GeneratedShadersGroupName );
					}
				}
			}
		}

		/// <summary>
		///   Gets the output vertex shader target profiles as a list of strings
		/// </summary>
		public string[] VertexShaderProfilesList
		{
			get
			{
				return vertexShaderProfilesList;
			}
		}

		/// <summary>
		///   Gets/Sets the output vertex shader profiles
		/// </summary>
		public string VertexShaderProfiles
		{
			get
			{
				return vertexShaderProfiles;
			}
			set
			{
				vertexShaderProfiles = value;
				vertexShaderProfilesList = vertexShaderProfiles.Split( ' ' );
			}
		}

		/// <summary>
		///   Gets/Sets output fragment shader target profiles.
		/// </summary>
		public string FragmentShaderProfiles
		{
			get
			{
				return fragmentShaderProfiles;
			}
			set
			{
				fragmentShaderProfiles = value;
				fragmentShaderProfilesList = fragmentShaderProfiles.Split( ' ' );
			}
		}

		/// <summary>
		///   Gets the output fragment shader target profiles as a list of strings.
		/// </summary>
		public string[] FragmentShaderProfilesList
		{
			get
			{
				return fragmentShaderProfilesList;
			}
		}

		/// <summary>
		///   Gets/Sets the target shader langauge
		///   <remarks>
		///     The default shader language is cg.
		///   </remarks>
		/// </summary>
		public string TargetLangauge
		{
			get
			{
				return shaderLanguage;
			}
			set
			{
				if ( ProgramWriterManager.Instance.IsLanguageSupported( value ) == false )
				{
					throw new AxiomException( "The language " + shaderLanguage + " is not supported!" );
				}

				//Case target language changed -> flush the shaders cache
				if ( shaderLanguage != value )
				{
					shaderLanguage = value;
					FlushShaderCache();
				}
			}
		}

		/// <summary>
		///   Gets the active scenen manager that is doing the actual scene rendering. This attribute will be updated on the call to preFindVisibleObjects.
		/// </summary>
		public SceneManager ActiveSceneManager
		{
			get
			{
				return activeSceneMgr;
			}
		}

		public static ShaderGenerator Instance
		{
			get
			{
				return ShaderGenerator.instance;
			}
		}

		#endregion

		#region Nested Types

		internal class SGPass : IDisposable
		{
			protected SGTechnique _parent;
			protected Pass _srcPass;
			protected Pass _dstPass;
			protected RenderState customRenderState;
			protected TargetRenderState targetRenderState;
			public static string UserKey;

			public SGPass( SGTechnique parent, Pass srcPass, Pass dstPass )
			{
				_parent = parent;
				_srcPass = srcPass;
				_dstPass = dstPass;
				customRenderState = null;
				targetRenderState = null;
				//TODO UserObjectBindings?
			}

			/// <summary>
			///   Build the render state
			/// </summary>
			public void BuildRenderTargetRenderState()
			{
				string schemeName = _parent.DestinationTechniqueSchemeName;
				RenderState renderStateGlobal = ShaderGenerator.instance.GetRenderState( schemeName );
				targetRenderState = new TargetRenderState();

				//Set light properties
				var lightCount = new int[3]
				                 {
				                 	0, 0, 0
				                 };

				//Use light count definitions of the custom render state if exists.
				if ( customRenderState != null && customRenderState.LightCountAutoUpdate == false )
				{
					customRenderState.GetLightCount( out lightCount );
				}
					//Use light count definitions of the global render state if exists
				else if ( renderStateGlobal != null )
				{
					renderStateGlobal.GetLightCount( out lightCount );
				}

				targetRenderState.SetLightCount( lightCount );

				//Build the FFP state.
				FFPRenderStateBuilder.Instance.BuildRenderState( this, targetRenderState );

				//Link the target render state with the custom render state of this pass if exists.
				if ( customRenderState != null )
				{
					targetRenderState.Link( customRenderState, _srcPass, _dstPass );
				}

				//Link the target render staet with the scheme render state of the shader generator.
				if ( renderStateGlobal != null )
				{
					targetRenderState.Link( renderStateGlobal, _srcPass, _dstPass );
				}
			}

			/// <summary>
			///   Aquire the CPU/GPU programs for this pass.
			/// </summary>
			public void AquirePrograms()
			{
				ProgramManager.Instance.AcquirePrograms( _dstPass, targetRenderState );
			}

			/// <summary>
			///   Release the CPU/GPU programs of this pass.
			/// </summary>
			public void ReleasePrograms()
			{
				ProgramManager.Instance.ReleasePrograms( _dstPass, targetRenderState );
			}

			/// <summary>
			///   Called when a single object is about to be rendered.
			/// </summary>
			/// <param name="rend"> </param>
			/// <param name="source"> </param>
			/// <param name="lightList"> </param>
			/// <param name="suppressRenderStateChanges"> </param>
			public void NotifyRenderSingleObject( IRenderable rend, AutoParamDataSource source, LightList lightList,
			                                      bool suppressRenderStateChanges )
			{
				if ( targetRenderState != null && suppressRenderStateChanges == false )
				{
					targetRenderState.UpdateGpuProgramsParams( rend, _dstPass, source, lightList );
				}
			}

			/// <summary>
			///   Gets source pass
			/// </summary>
			public Pass SrcPass
			{
				get
				{
					return _srcPass;
				}
			}

			/// <summary>
			///   Gets destintion pass
			/// </summary>
			public Pass DstPass
			{
				get
				{
					return _dstPass;
				}
			}

			/// <summary>
			///   Get custom FPP sub state of this pass
			/// </summary>
			/// <param name="subStateOrder"> </param>
			/// <returns> </returns>
			public SubRenderState GetCustomFFPSubState( int subStateOrder )
			{
				SubRenderState customSubState = null;

				//try to override with custom render state of this pass.
				customRenderState = GetCustomFFPSubState( subStateOrder, customRenderState );

				//Case no custom sub state of this pass found, try to override with global scheme state
				if ( customRenderState == null )
				{
					string schemeName = _parent.DestinationTechniqueSchemeName;
					RenderState renderStateGlobal = ShaderGenerator.instance.GetRenderState( schemeName );
					customRenderState = GetCustomFFPSubState( subStateOrder, renderStateGlobal );
				}

				return customSubState;
			}

			public RenderState GetCustomFFPSubState( int subStateOrder, RenderState renderState )
			{
				if ( renderState != null )
				{
					List<SubRenderState> subRenderStateList = renderState.TemplateSubRenderStateList;
					foreach ( var curSubRenderState in subRenderStateList )
					{
						if ( curSubRenderState.ExecutionOrder == subStateOrder )
						{
							SubRenderState clone;
							clone = ShaderGenerator.instance.CreateSubRenderState( curSubRenderState.Type );
							return clone;
						}
					}
				}
				return null;
			}

			/// <summary>
			///   Gets/Sets custom render state of this pass
			/// </summary>
			public RenderState CustomRenderState
			{
				get
				{
					return customRenderState;
				}
				set
				{
					customRenderState = value;
				}
			}


			public void Dispose()
			{
				if ( targetRenderState != null )
				{
					targetRenderState.Dispose();
					targetRenderState = null;
				}
			}
		}

		internal class SGTechnique : IDisposable
		{
			protected SGMaterial _parent;
			protected Technique _srcTechnique;
			protected string _dstTechniqueSchemeName;
			protected Technique dstTechnique;
			private List<SGPass> passEntries;
			private List<RenderState> customRenderStates;
			public static string UserKey;

			public SGTechnique( SGMaterial parent, Technique srcTechnique, string dstTechniqueSchemeName )
			{
				_parent = parent;
				_srcTechnique = srcTechnique;
				_dstTechniqueSchemeName = dstTechniqueSchemeName;
				dstTechnique = null;
				BuildDestinationTechnique = true;
			}

			/// <summary>
			///   Gets the parent SGMaterial
			/// </summary>
			public SGMaterial Parent
			{
				get
				{
					return _parent;
				}
			}

			/// <summary>
			///   Gets the source technique
			/// </summary>
			public Technique SourceTechnique
			{
				get
				{
					return _srcTechnique;
				}
			}

			/// <summary>
			///   Gets the destination technique
			/// </summary>
			public Technique DestinationTechnique
			{
				get
				{
					return dstTechnique;
				}
			}

			/// <summary>
			///   Gets the destination technique scheme name.
			/// </summary>
			public string DestinationTechniqueSchemeName
			{
				get
				{
					return _dstTechniqueSchemeName;
				}
			}

			/// <summary>
			///   Gets/Sets if the technique needs to generate shader code.
			/// </summary>
			public bool BuildDestinationTechnique { get; set; }

			/// <summary>
			///   Get render state by pass index
			/// </summary>
			/// <param name="passIndex"> </param>
			/// <returns> </returns>
			public RenderState GetRenderState( short passIndex )
			{
				RenderState renderState = null;

				if ( passIndex > customRenderStates.Count )
				{
					customRenderStates.Add( null );
				}

				renderState = customRenderStates[ passIndex ];
				if ( renderState == null )
				{
					renderState = new RenderState();
					customRenderStates[ passIndex ] = renderState;
				}
				return renderState;
			}

			/// <summary>
			///   Builds the render state.
			/// </summary>
			public void BuildTargetRenderState()
			{
				//Remove existing destination technique and passes in order to build it again from scratch
				if ( dstTechnique != null )
				{
					Material mat = _srcTechnique.Parent;
					for ( int i = 0; i < mat.TechniqueCount; i++ )
					{
						if ( mat.GetTechnique( i ) == dstTechnique )
						{
							mat.RemoveTechnique( mat.GetTechnique( i ) );
							break;
						}
					}

					DestroySGPasses();
				}

				//Create the destination technique and passes 
				dstTechnique = _srcTechnique.Parent.CreateTechnique();
				//TODO: Object Bindings?
				dstTechnique = _srcTechnique;
				dstTechnique.SchemeName = _dstTechniqueSchemeName;
				CreateSGPasses();

				//Build render state for each pass.
				for ( int i = 0; i < passEntries.Count; i++ )
				{
					passEntries[ i ].BuildRenderTargetRenderState();
				}
			}

			/// <summary>
			///   Acquire the CPU/GPU programs for this technique.
			/// </summary>
			public void AcquirePrograms()
			{
				for ( int i = 0; i < passEntries.Count; i++ )
				{
					passEntries[ i ].AquirePrograms();
				}
			}

			/// <summary>
			///   Release the GPU/GPU programs of this technique.
			/// </summary>
			public void ReleasePrograms()
			{
				//Remove destination technique.
				if ( dstTechnique != null )
				{
					Material mat = _srcTechnique.Parent;

					for ( int i = 0; i < mat.TechniqueCount; i++ )
					{
						if ( mat.GetTechnique( i ) == dstTechnique )
						{
							mat.RemoveTechnique( mat.GetTechnique( i ) );
							break;
						}
					}

					dstTechnique = null;
				}

				//Release CPU/GPU programs that associated wit this technique passes.
				for ( int i = 0; i < passEntries.Count; i++ )
				{
					passEntries[ i ].ReleasePrograms();
				}

				DestroySGPasses();
			}

			/// <summary>
			///   Tells if a custom render state exists for the given pass.
			/// </summary>
			/// <param name="passIndex"> </param>
			/// <returns> </returns>
			public bool HasRenderState( short passIndex )
			{
				return ( passIndex < customRenderStates.Count ) && ( customRenderStates[ passIndex ] != null );
			}

			/// <summary>
			///   Create the passes entries
			/// </summary>
			protected void CreateSGPasses()
			{
				//Create pass entry for each pass
				for ( int i = 0; i < _srcTechnique.PassCount; i++ )
				{
					Pass srcPass = _srcTechnique.GetPass( i );
					Pass dstPass = dstTechnique.GetPass( i );

					var passEntry = new SGPass( this, srcPass, dstPass );

					if ( i < customRenderStates.Count )
					{
						passEntry.CustomRenderState = customRenderStates[ i ];
					}
					passEntries.Add( passEntry );
				}
			}

			/// <summary>
			///   Destroys the passes entries
			/// </summary>
			protected void DestroySGPasses()
			{
				for ( int i = 0; i < passEntries.Count; i++ )
				{
					passEntries[ i ].Dispose();
				}
				passEntries.Clear();
			}


			public void Dispose()
			{
				string materialName = _parent.MaterialName;
				string groupName = _parent.GroupName;

				if ( MaterialManager.Instance.ResourceExists( materialName ) )
				{
					var mat = (Material)MaterialManager.Instance.GetByName( materialName );

					//Remove the destination technique from parent material
					for ( int i = 0; i < mat.TechniqueCount; i++ )
					{
						if ( dstTechnique == mat.GetTechnique( i ) )
						{
							//Unload the generated technique in order to free reference resources.
							dstTechnique.Unload();

							//Remove the generated technique in order to restore the material to its original state
							mat.RemoveTechnique( mat.GetTechnique( i ) );

							//touch when finalizing -will reload the texture - so no touch if finalizing
							if ( ShaderGenerator.Instance.isFinalizing == false )
							{
								//Make sure the material goes back to its original state
								mat.Touch();
							}
							break;
						}
					}

					//Release CPU/GPU programs that associated with this technique passes
					for ( int i = 0; i < passEntries.Count; i++ )
					{
						passEntries[ i ].ReleasePrograms();
					}

					DestroySGPasses();

					//Delete the custom render states of each pass if exist
					for ( int i = 0; i < customRenderStates.Count; i++ )
					{
						if ( customRenderStates[ i ] != null )
						{
							customRenderStates[ i ].Dispose();
							customRenderStates[ i ] = null;
						}
					}
					customRenderStates.Clear();
				}
			}
		}

		internal class SGMaterial
		{
			protected string name;
			protected string group;
			protected List<SGTechnique> techniqueEntries;

			public SGMaterial( string materialName, string groupName )
			{
				name = materialName;
				@group = groupName;
			}

			/// <summary>
			///   Gets material name
			/// </summary>
			public string MaterialName
			{
				get
				{
					return name;
				}
			}

			/// <summary>
			///   Gets the group name
			/// </summary>
			public string GroupName
			{
				get
				{
					return group;
				}
			}

			/// <summary>
			///   Gets techniques list of this material
			/// </summary>
			public List<SGTechnique> TechniqueList
			{
				get
				{
					return techniqueEntries;
				}
			}
		}

		internal class SGScheme : IDisposable
		{
			protected string name;
			protected List<SGTechnique> techniqueEntries;
			protected bool outOfDate;
			protected RenderState renderState;
			protected FogMode fogMode;

			public SGScheme( string schemeName )
			{
				outOfDate = true;
				renderState = null;
				name = schemeName;
				fogMode = FogMode.None;
			}

			/// <summary>
			///   Gets if this scheme does not contain any techniques.
			/// </summary>
			public bool Empty
			{
				get
				{
					return ( techniqueEntries.Count == 0 );
				}
			}

			/// <summary>
			///   Invalidates the whole scheme.
			/// </summary>
			public void Invalidate()
			{
				//turn on the build destintion technique flag of all techniques
				for ( int i = 0; i < techniqueEntries.Count; i++ )
				{
					SGTechnique curTechEntry = techniqueEntries[ i ];
					curTechEntry.BuildDestinationTechnique = true;
				}
				outOfDate = true;
			}

			/// <summary>
			///   Validates the whole scheme.
			/// </summary>
			public void Validate()
			{
				SynchronizeWithLightSettings();
				SynchronizeWithFogSettings();

				//The target scheme is up to date.
				if ( !outOfDate )
				{
					return;
				}

				//build render state for each technique
				for ( int i = 0; i < techniqueEntries.Count; i++ )
				{
					SGTechnique curTechEntry = techniqueEntries[ i ];

					if ( curTechEntry.BuildDestinationTechnique )
					{
						curTechEntry.BuildTargetRenderState();
					}
				}

				//Acquire GPU programs for each technique
				for ( int i = 0; i < techniqueEntries.Count; i++ )
				{
					SGTechnique curTechEntry = techniqueEntries[ i ];

					if ( curTechEntry.BuildDestinationTechnique )
					{
						curTechEntry.AcquirePrograms();
					}
				}

				//turn off the build destination technique flag
				for ( int i = 0; i < techniqueEntries.Count; i++ )
				{
					SGTechnique curTechEntry = techniqueEntries[ i ];
					curTechEntry.BuildDestinationTechnique = false;
				}

				//mark this scheme as up to date
				outOfDate = false;
			}

			/// <summary>
			///   Invalidate specific material
			/// </summary>
			/// <see cref=">ShaderGenerator.InvalidateMaterial" />
			/// <param name="materialName"> </param>
			public void Invalidate( string materialName )
			{
				Invalidate( materialName, ResourceGroupManager.AutoDetectResourceGroupName );
			}

			/// <summary>
			///   Invalidate specific material
			/// </summary>
			/// <see cref=">ShaderGenerator.InvalidateMaterial" />
			/// <param name="materialName"> </param>
			/// <param name="groupName"> </param>
			public void Invalidate( string materialName, string groupName )
			{
				//Find the desired technique
				bool doAutoDetect = ( groupName == ResourceGroupManager.AutoDetectResourceGroupName );
				for ( int i = 0; i < techniqueEntries.Count; i++ )
				{
					SGTechnique curTechEntry = techniqueEntries[ i ];
					SGMaterial curMaterial = curTechEntry.Parent;
					if ( ( curMaterial.MaterialName == materialName ) &&
					     ( ( doAutoDetect ) || ( curMaterial.GroupName == groupName ) ) )
					{
						//turn on the build destination technique flag
						curTechEntry.BuildDestinationTechnique = true;
						break;
					}
				}

				outOfDate = true;
			}

			/// <summary>
			///   Validate specific material
			/// </summary>
			/// <param name="materialName"> </param>
			/// <returns> </returns>
			public void Validate( string materialName )
			{
				Validate( materialName, ResourceGroupManager.AutoDetectResourceGroupName );
			}

			/// <summary>
			///   Validate specific material
			/// </summary>
			/// <param name="materialName"> </param>
			/// <param name="groupName"> </param>
			/// <returns> </returns>
			public bool Validate( string materialName, string groupName )
			{
				SynchronizeWithFogSettings();
				SynchronizeWithLightSettings();

				bool doAutoDetect = groupName == ResourceGroupManager.AutoDetectResourceGroupName;
				for ( int i = 0; i < techniqueEntries.Count; i++ )
				{
					SGTechnique curTechEntry = techniqueEntries[ i ];
					SGMaterial curMat = curTechEntry.Parent;
					if ( ( curMat.MaterialName == materialName ) &&
					     ( ( doAutoDetect == true ) || ( curMat.GroupName == groupName ) ) &
					     ( curTechEntry.BuildDestinationTechnique ) )
					{
						//build render state for each technique
						curTechEntry.BuildTargetRenderState();

						//Acquire the cpu/gpu programs
						curTechEntry.AcquirePrograms();

						//turn off the build destination technique flag.
						curTechEntry.BuildDestinationTechnique = false;

						return true;
					}
				}
				return false;
			}

			private void SynchronizeWithFogSettings()
			{
				SceneManager sceneManager = ShaderGenerator.instance.ActiveSceneManager;
				if ( sceneManager != null && sceneManager.FogMode != fogMode )
				{
					fogMode = sceneManager.FogMode;
					Invalidate();
				}
			}

			private void SynchronizeWithLightSettings()
			{
				SceneManager sceneManager = ShaderGenerator.instance.ActiveSceneManager;
				var curRenderState = RenderState;

				if ( sceneManager != null && curRenderState.LightCountAutoUpdate )
				{
					//TODO:
					LightList lightList = null; //sceneManager._getLightsAffectingFrustum();

					var sceneLightCount = new int[3]
					                      {
					                      	0, 0, 0
					                      };
					var currLightCount = new int[3]
					                     {
					                     	0, 0, 0
					                     };

					for ( int i = 0; i < lightList.Count; i++ )
					{
						sceneLightCount[ (int)lightList[ i ].Type ]++;
					}

					renderState.GetLightCount( out currLightCount );

					//Case light state has been changed -> invalidate this scheme.
					if ( currLightCount[ 0 ] != sceneLightCount[ 0 ] ||
					     currLightCount[ 1 ] != sceneLightCount[ 1 ] ||
					     currLightCount[ 2 ] != sceneLightCount[ 2 ] )
					{
						curRenderState.SetLightCount( currLightCount );
						Invalidate();
					}
				}
			}

			/// <summary>
			///   Adds a technique to current techniques list
			/// </summary>
			/// <param name="techEntry"> </param>
			public void AddTechniqueEntry( SGTechnique techEntry )
			{
				techniqueEntries.Add( techEntry );

				//mark as out of date
				outOfDate = true;
			}

			/// <summary>
			///   Removes a technique from the current techniques list.
			/// </summary>
			/// <param name="techEntry"> </param>
			public void RemoveTechniqueEntry( SGTechnique techEntry )
			{
				//build render state for each technique
				for ( int i = 0; i < techniqueEntries.Count; i++ )
				{
					SGTechnique curTechEntry = techniqueEntries[ i ];

					if ( curTechEntry == techEntry )
					{
						techniqueEntries.Remove( curTechEntry );
						break;
					}
				}
			}

			public RenderState GetRenderState( string materialName, string groupName, ushort passIndex )
			{
				bool doAutoDetect = ( groupName == ResourceGroupManager.AutoDetectResourceGroupName );
				for ( int i = 0; i < techniqueEntries.Count; i++ )
				{
					SGTechnique curTechEntry = techniqueEntries[ i ];
					Material curMat = curTechEntry.SourceTechnique.Parent;
					if ( ( curMat.Name == materialName ) &&
					     ( ( doAutoDetect == true ) || ( curMat.Group == groupName ) ) )
					{
						return curTechEntry.GetRenderState( (short)passIndex );
					}
				}
				return null;
			}

			public RenderState RenderState
			{
				get
				{
					if ( renderState == null )
					{
						renderState = new RenderState();
					}
					return renderState;
				}

				set
				{
					renderState = value;
				}
			}

			public void Dispose()
			{
				if ( renderState != null )
				{
					renderState.Dispose();
					renderState = null;
				}
			}
		}

		public class SgScriptTranslatorManager : ScriptTranslatorManager
		{
			protected ShaderGenerator _owner;

			public SgScriptTranslatorManager( ShaderGenerator owner )
			{
				_owner = owner;
			}

			public virtual int NumTranslators
			{
				get
				{
					return _owner.NumTranslators;
				}
			}

			public virtual SGScriptTranslator GetTranslator( AbstractNode node )
			{
				return _owner.GetTranslator( node );
			}
		}

		protected class MatGroupPair : Axiom.Math.Tuple<string, string>
		{
			private string materialName;
			private string trueGroupName;

			public MatGroupPair( string materialName, string trueGroupName )
				: base( materialName, trueGroupName )
			{
				this.materialName = materialName;
				this.trueGroupName = trueGroupName;
			}

			public string Item1
			{
				get
				{
					return First;
				}
			}

			public string Item2
			{
				get
				{
					return Second;
				}
			}

			private static bool isEqual( MatGroupPair p1, MatGroupPair p2 )
			{
				bool cmpVal1 = p1.Item1 == p2.Item1;
				bool cmpVal2 = p1.Item2 == p2.Item2;
				return ( cmpVal1 == false && cmpVal2 == false );
			}

			public static bool operator ==( MatGroupPair p1, MatGroupPair p2 )
			{
				return isEqual( p1, p2 );
			}

			public static bool operator !=( MatGroupPair p1, MatGroupPair p2 )
			{
				return !isEqual( p1, p2 );
			}
		}

		#endregion
	}
}