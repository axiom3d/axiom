void main
(
	float4 uv: TEXCOORD0,
	uniform sampler2D diffuseMap : register(s0),
	out float4 oColor : COLOR0
)
{
	oColor = tex2D(diffuseMap, uv);
}