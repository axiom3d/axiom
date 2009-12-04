//Colored vertex support useful for billboards
void main
(
	float4 iPosition : POSITION,
	float2 iTexCoord0 : TEXCOORD0,
	float4 iColor : COLOR0,
	
	out float4 oPosition : POSITION,
	out float2 oTexCoord0 : TEXCOORD0,
	out float4 oColor : COLOR0,

	uniform float4x4 worldViewProj
)
{
	oPosition = mul(worldViewProj, iPosition) ;
	oTexCoord0 = iTexCoord0;
	oColor = iColor;
}