/*
  Single-weight-per-vertex hardware skinning, 2 lights
  The trouble with vertex programs is they're not general purpose, but
  fixed function hardware skinning is very poorly supported
*/

struct VS_INPUT
{
	float4 position : POSITION;
	float3 normal   : NORMAL;
	float2 uv       : TEXCOORD0;
	float  blendIdx : BLENDINDICES;
};

float3x4   worldMatrix3x4Array[24];
float4x4 viewProjectionMatrix;
float3   lightPos[2];
float4   lightDiffuseColour[2];
float4   ambient;

struct VS_OUTPUT
{
	float4 oPosition : POSITION;
	float2 oUv       : TEXCOORD0;
	float4 colour    : COLOR;
};

VS_OUTPUT hardwareSkinningOneWeight_vp(VS_INPUT input )
	// Support up to 24 bones of float3x4
	// vs_1_1 only supports 96 params so more than this is not feasible
	
{
	VS_OUTPUT output = (VS_OUTPUT)0;

	// transform by indexed matrix
	float4 blendPos = float4(mul(worldMatrix3x4Array[input.blendIdx], input.position).xyz, 1.0);
	
	// view / projection
	output.oPosition = mul(viewProjectionMatrix,blendPos);
	
	// transform normal
	float3 norm = mul((float3x3)worldMatrix3x4Array[input.blendIdx], input.normal);
	float3 lightDir0 = normalize(lightPos[0] - blendPos);
	float3 lightDir1 = normalize(lightPos[1] - blendPos);

	
	output.oUv = input.uv;
	output.colour = ambient + 
		(saturate(dot(lightDir0, norm)) * lightDiffuseColour[0]) + 
		(saturate(dot(lightDir1, norm)) * lightDiffuseColour[1]);
	return output;
}	