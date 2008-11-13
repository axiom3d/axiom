void main
(
  float4 iPosition : POSITION, //in object space
  float3 iNormal : NORMAL, //in object space
  float2 iTexCoord0 : TEXCOORD0,

  out float4 oPosition : POSITION, //in projection space
  out float2 oTexCoord0 : TEXCOORD0, 
  out float3 oNormal : TEXCOORD1, //in object space
  out float3 oLightVector : TEXCOORD2,
  out float oDist : TEXCOORD3,

  uniform float4x4 worldViewProj,
  uniform float3 lightPosition //in object space
)
{
    // pass normal
    oNormal = iNormal;

    oTexCoord0 = iTexCoord0;

    // compute light vector
    float3 aux = lightPosition - iPosition.xyz;
    oDist = length(aux);

    // pass light vector as a color
    oLightVector = normalize(aux);

    // transform position to projection space
    oPosition = mul(iPosition, worldViewProj);
}
