// General functions
// Expand a range-compressed vector
float3 expand(float3 v)
{
	return (v - 0.5) * 2;
}

struct VS_INPUT
{
	float4 position	: POSITION;
	float3 normal	: NORMAL;
	float2 uv		: TEXCOORD0;
	float3 tangent    : TEXCOORD1;
};

// parameters
float4 lightPosition;
float4x4 worldViewProj;

struct VS_OUTPUT
{
	float4 oPosition    : POSITION;
	float2 oUv		: TEXCOORD0;
	float3 oTSLightDir	: TEXCOORD1;
};

/* Bump mapping vertex program
   In this program, we want to calculate the tangent space light vector
   on a per-vertex level which will get passed to the fragment program,
   or to the fixed function dot3 operation, to produce the per-pixel
   lighting effect. 
*/
VS_OUTPUT main_vp(VS_INPUT input )	
{
	VS_OUTPUT output = (VS_OUTPUT)0;
	// calculate output position
	output.oPosition = mul(worldViewProj, input.position);

	// pass the main uvs straight through unchanged
	output.oUv = input.uv;

	// calculate tangent space light vector
	// Get object space light direction
	float3 lightDir = lightPosition - input.position.xyz;

	// Calculate the binormal (NB we assume both normal and tangent are
	// already normalised)
	// NB looks like nvidia cross params are BACKWARDS to what you'd expect
	// this equates to NxT, not TxN
	float3 binormal = cross(input.tangent, input.normal);
	
	// Form a rotation matrix out of the vectors
	float3x3 rotation = float3x3(input.tangent, binormal, input.normal);
	
	// Transform the light vector according to this matrix
	output.oTSLightDir = normalize(mul(rotation, lightDir));
	return output;
}

float4 	lightDiffuse;
sampler2D   normalMap		:register(s0);
samplerCUBE normalCubeMap	:register(s1);

float4 main_fp( VS_OUTPUT input ) : COLOR	  
{
	// retrieve normalised light vector, expand from range-compressed
	float3 lightVec = expand(texCUBElod(normalCubeMap,float4(input.oTSLightDir,0)).xyz);

	// get bump map vector, again expand from range-compressed
	float3 bumpVec = expand(tex2Dlod(normalMap, float4(input.oUv,0,0)).xyz);

	// Calculate dot product
	return lightDiffuse * dot(bumpVec, lightVec);
	
}
