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
float3 eyePosition;

struct VS_OUTPUT
{
	float4 oPosition    	: POSITION;
	float2 oUv			: TEXCOORD0;
	float3 oTSLightDir	: TEXCOORD1;
	float3 oTSHalfAngle 	: TEXCOORD2;
};

/* Vertex program which includes specular component */


VS_OUTPUT specular_vp(VS_INPUT input)
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

	// Calculate half-angle in tangent space
	float3 eyeDir = eyePosition - input.position.xyz;
	float3 halfAngle = normalize(eyeDir + lightDir);
	output.oTSHalfAngle = mul(rotation, halfAngle);
	
	return output;
}

float4 lightDiffuse;
float4 lightSpecular;
sampler2D   normalMap : register(s0);
samplerCUBE normalCubeMap : register(s1);
samplerCUBE normalCubeMap2: register(s2);

/* Fragment program which supports specular component */
float4 specular_fp( VS_OUTPUT input) : COLOR
{
	// retrieve normalised light vector, expand from range-compressed
	float3 lightVec = expand(texCUBElod(normalCubeMap, float4(input.oTSLightDir,0) ).xyz);

	// retrieve half angle and normalise through cube map
	float3 halfAngle = expand(texCUBElod(normalCubeMap2, float4(input.oTSHalfAngle,0)).xyz);

	// get bump map vector, again expand from range-compressed
	float3 bumpVec = expand(tex2Dlod(normalMap, float4(input.oUv,0,0)).xyz);

	// Pre-raise the specular exponent to the eight power
	// Note we have no 'pow' function in basic fragment programs, if we were willing to accept compatibility
	// with ps_2_0 / arbfp1 and above, we could have a variable shininess parameter
	// This is equivalent to 
	float specFactor = dot(bumpVec, halfAngle);
	for (int i = 0; i < 3; ++i)
		specFactor *= specFactor;
	

	// Calculate dot product for diffuse
	return (lightDiffuse * dot(bumpVec, lightVec)) + 
			(lightSpecular * specFactor);
	
}
			 
