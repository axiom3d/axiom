/*
  Basic ambient lighting vertex program
*/
struct VS_OUTPUT
{
	float4 oPosition : POSITION;
	float2 oUv	   : TEXCOORD0;
	float4 colour    : COLOR;
};

float4x4 worldViewProj;
float4 ambient;

VS_OUTPUT ambientOneTexture_vp(float4 position : POSITION, float2 uv	: TEXCOORD0)
{
	VS_OUTPUT output = (VS_OUTPUT)0;
	output.oPosition = mul(worldViewProj, position);
	output.oUv = uv;
	output.colour = ambient;
	return output;
}
