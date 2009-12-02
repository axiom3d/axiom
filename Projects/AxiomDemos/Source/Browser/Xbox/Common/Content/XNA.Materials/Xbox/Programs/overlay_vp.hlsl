void main
(
	float4 iPosition : POSITION,
	float2 iTexCoord0 : TEXCOORD0,
	
	out float4 oPosition : POSITION,
	out float2 oTexCoord0 : TEXCOORD0,
	out float4 oColor : COLOR0,

	uniform float4x4 worldViewProj
)
{
	oPosition = mul(iPosition, worldViewProj);
	
	// Clean up inaccuracies
	//iPosition.xy = sign(iPosition.xy);

	//oPosition = float4(iPosition.xy, -5, 1);
	
	oTexCoord0 = iTexCoord0;
	oColor = float4(1,1,1,1);
}