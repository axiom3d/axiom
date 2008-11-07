void main
(
	float4 position : POSITION,
	float2 uv : TEXCOORD0,
	out float4 oPosition : POSITION,
	out float2 oUv : TEXCOORD0,

	out float4 oColor : COLOR,


	uniform float4x4 worldViewProj,
	uniform float4 ambient)

{
	
	oPosition = mul(worldViewProj, position);

	oUv = uv;
	
	oColor = ambient;

}
