namespace Axiom.Components.RTShaderSystem
{
	public class FFPRenderState
	{
		public enum FFPVertexShaderStage
		{
			VSPreProcess = 0,
			VSTransform = 100,
			VSColor = 200,
			VSLighting = 300,
			VSTexturing = 400,
			VSFog = 500,
			VSPostProcess = 2000
		}

		public enum FFPFragmentShaderStage
		{
			PSPreProcess = 0,
			PSColorBegin = 100,
			PSSampling = 150,
			PSTexturing = 200,
			PSColorEnd = 300,
			PSFog = 400,
			PSPostProcess = 500
		}

		public enum FFPShaderStage
		{
			PreProcess = 0,
			Transform = 100,
			Color = 200,
			Lighting = 300,
			Texturing = 400,
			Fog = 500,
			PostProcess = 600
		}

		//Fixed Function Library: common functions
		public static string FFPLibCommon = "FFPLib_Common";
		public static string FFPFuncAssign = "FFP_Assign";
		public static string FFPFuncConstruct = "FFP_Construct";
		public static string FFPFuncModulate = "FFP_Modulate";
		public static string FFPFuncAdd = "FFP_Add";
		public static string FFPFuncSubtract = "FFP_Subtract";
		public static string FFPFuncLerp = "FFP_Lerp";
		public static string FFPFuncDotProduct = "FFP_DotProduct";
		public static string FFPFuncNormalize = "FFP_Normalize";

		//Fixed Function Library: Transform Functions
		public static string FFPLibTransform = "FFPLib_Transform";
		public static string FFPFuncTransform = "FFP_Transform";

		//fixed Function Library: Lighting functions
		public static string FFPLibLighting = "FFPLib_Lighting";
		public static string FFPFuncLightDirectionDiffuse = "FFP_Light_Directional_Diffuse";
		public static string FFPFuncLightDirectionDiffuseSpecular = "FFP_Light_Directional_DiffuseSpecular";
		public static string FFPFuncLightPointDiffuse = "FFP_Light_Point_Diffuse";
		public static string FFPFuncLightPointDiffuseSpecular = "FFP_Light_Point_DiffuseSpecular";
		public static string FFPFuncLightSpotDiffuse = "FFP_Light_Spot_Diffuse";
		public static string FFPFuncLightSpotDiffuseSpecular = "FFP_Light_Spot_DffuseSpecular";

		//Texturing Functions
		public static string FFPLibTexturing = "FFPLib_Texturing";
		public static string FFPFuncTransformTexCoord = "FFP_TransformTexCoord";
		public static string FFPFuncGenerateTexcoordEnvNormal = "FFP_GenerateTexCoord_EnvMap_Normal";
		public static string FFPFunGenerateTexcoordEnvSphere = "FFP_GenerateTexCoord_EnvMap_Sphere";
		public static string FFPFuncGenerateTexCoordEnvReflect = "FFP_GenerateTexCoord_EnvMap_Reflect";
		public static string FFPFuncGenerateTexCoordProjection = "FFP_GenerateTexCoord_Projection";
		public static string FFPFuncSampleTexture = "FFP_SampleTexture";
		public static string FFPFuncSamplerTextureProj = "FFP_SampleTextureProj";
		public static string FFPFuncModulateX2 = "FFP_ModulateX2";
		public static string FFPFuncModulateX4 = "FFP_ModulateX4";
		public static string FFPFuncAddSigned = "FFP_AddSigned";
		public static string FFPFuncAddSmooth = "FFP_AddSmooth";

		//Fog Functions
		public static string FFPLibFog = "FFPLib_Fog";
		public static string FFPFuncVertexFogLinear = "FFP_VertexFog_Linear";
		public static string FFPFuncVertexFogExp = "FFP_VertexFog_Exp";
		public static string FFPFuncVertexFogExp2 = "FFP_VertexFog_Exp2";
		public static string FFPFuncPixelFogDepth = "FFP_PixelFog_Depth";
		public static string FFPFuncPixelFogLinear = "FFP_PixelFog_Linear";
		public static string FFPFuncPixelFogExp = "FFP_PixelFog_Exp";
		public static string FFPFuncPixelFogExp2 = "FFP_PixelFog_Exp2";
	}
}