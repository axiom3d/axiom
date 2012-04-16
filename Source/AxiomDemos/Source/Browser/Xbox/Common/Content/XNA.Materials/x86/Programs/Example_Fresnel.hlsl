struct VS_INPUT
{
	float4 pos	: POSITION;
	float4 normal: NORMAL;
	float2 tex	: TEXCOORD0;
};

// parameters
float4x4 worldViewProjMatrix;
float3 eyePosition;
float timeVal;
float scale;
float scroll;
float noise;

struct VS_OUTPUT
{
	float4 oPos		: POSITION;
	float3 noiseCoord : TEXCOORD0;
	float4 projectionCoord : TEXCOORD1;
	float3 oEyeDir : TEXCOORD2;
	float3 oNormal : TEXCOORD3;
};

VS_OUTPUT main_vp(VS_INPUT input )	
{
	VS_OUTPUT output = (VS_OUTPUT)0;
	// Vertex program for fresnel reflections / refractions
	output.oPos = mul(worldViewProjMatrix, input.pos);
	// Projective texture coordinates, adjust for mapping
	float4x4 scalemat = float4x4(	 0.5,   0,   0, 0.5, 
	                                 0,-0.5,   0, 0.5,
						   0,   0, 0.5, 0.5,
						   0,   0,   0,   1);
	output.projectionCoord = mul(scalemat, output.oPos);
	// Noise map coords
	output.noiseCoord.xy = (input.tex + (timeVal * scroll)) * scale;
	output.noiseCoord.z = noise * timeVal;

	output.oEyeDir = normalize(input.pos.xyz - eyePosition);
	output.oNormal = input.normal.rgb;
	return output;
}


float4 tintColour;
float noiseScale;
float fresnelBias;
float fresnelScale;
float fresnelPower;

sampler2D noiseMap : register(s0);
sampler2D reflectMap : register(s1);
sampler2D refractMap : register(s2);

float4 main_fp( VS_OUTPUT input ) : COLOR
{
	// Do the tex projection manually so we can distort _after_
	float2 final = input.projectionCoord.xy / input.projectionCoord.w;

	// Noise
	float3 noiseNormal = (tex2D(noiseMap, (input.noiseCoord.xy / 5)).rgb - 0.5).rbg * noiseScale;
	final += noiseNormal.xz;

	// Fresnel
	//normal = normalize(input.oNormal + noiseNormal.xz);
	float fresnel = fresnelBias + fresnelScale * pow(1 + dot(input.oEyeDir, input.oNormal ), fresnelPower);

	// Reflection / refraction
	float4 reflectionColour = tex2D(reflectMap, final);
	float4 refractionColour = tex2D(refractMap, final) + tintColour;

	// Final colour
	return lerp(refractionColour, reflectionColour, fresnel);


}

/*
// Old version to match ATI PS 1.3 implementation
void main_vp_old(
		float4 pos			: POSITION,
		float4 normal		: NORMAL,
		float2 tex			: TEXCOORD0,
		
		out float4 oPos		: POSITION,
		out float fresnel   : COLOR,
		out float3 noiseCoord : TEXCOORD0,
		out float4 projectionCoord : TEXCOORD1,

		uniform float4x4 worldViewProjMatrix,
		uniform float3 eyePosition, // object space
		uniform float fresnelBias,
		uniform float fresnelScale,
		uniform float fresnelPower,
		uniform float timeVal,
		uniform float scale,  // the amount to scale the noise texture by
		uniform float scroll, // the amount by which to scroll the noise
		uniform float noise  // the noise perturb as a factor of the  time
		)
{
	oPos = mul(worldViewProjMatrix, pos);
	// Projective texture coordinates, adjust for mapping
	float4x4 scalemat = float4x4(0.5,   0,   0, 0.5, 
	                               0,-0.5,   0, 0.5,
								   0,   0, 0.5, 0.5,
								   0,   0,   0,   1);
	projectionCoord = mul(scalemat, oPos);
	// Noise map coords
	noiseCoord.xy = (tex + (timeVal * scroll)) * scale;
	noiseCoord.z = noise * timeVal;

	// calc fresnel factor (reflection coefficient)
	float3 eyeDir = normalize(pos.xyz - eyePosition);
	fresnel = fresnelBias + fresnelScale * pow(1 + dot(eyeDir, normal), fresnelPower);
	
}
*/