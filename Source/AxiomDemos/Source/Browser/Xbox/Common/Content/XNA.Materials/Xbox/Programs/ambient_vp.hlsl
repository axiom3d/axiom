void main
(
	float4 iPosition : POSITION,
	float2 iTexCoord0 : TEXCOORD0,
	
	out float4 oPosition : POSITION,
	out float2 oTexCoord0 : TEXCOORD0,
	out float4 oColor : COLOR0,

	uniform float4x4 worldViewProj,
	uniform float4 ambient
)
{
	oPosition = mul(iPosition, worldViewProj);
	oTexCoord0 = iTexCoord0;
	oColor = ambient;
}