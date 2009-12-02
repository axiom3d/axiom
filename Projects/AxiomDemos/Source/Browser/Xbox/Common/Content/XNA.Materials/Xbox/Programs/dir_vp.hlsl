void main
(
	float4 iPosition : POSITION, //in object space
	float3 iNormal : NORMAL, //in object space
	float2 iTexCoord0 : TEXCOORD0,

	out float4 oPosition : POSITION, //in projection space
	out float2 oTexCoord0 : TEXCOORD0, 
	out float3 oNormal : TEXCOORD1, //in object space
	out float3 oLightVector : TEXCOORD2,
  
	uniform float4x4 worldViewProj,
	uniform float3 lightPosition //in object space
)
{
	// pass normal	
	oNormal = iNormal;

	oTexCoord0 = iTexCoord0;

	// pass light vector
	oLightVector = normalize(lightPosition);

	// transform position to projection space
	oPosition = mul(iPosition, worldViewProj);
}
