//Outputs texture lookup * color input from vertex program
void main_fp
(
	float4 iPosition0: POSITION0,
	float4 iTexCoord0: TEXCOORD0,
	float4 iColor : COLOR0,
	
	uniform sampler2D diffuseMap : register(s0),
	
	out float4 oColor : COLOR0
)
{
	oColor = iColor * tex2D(diffuseMap, iTexCoord0);
}