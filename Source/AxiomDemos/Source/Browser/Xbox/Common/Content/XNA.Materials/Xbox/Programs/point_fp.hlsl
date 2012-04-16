void main
(
  float2 iTexCoord0 : TEXCOORD0,
  float3 iNormal : TEXCOORD1, //in object space
  float3 iLightVector : TEXCOORD2, //in object space
  float iDist : TEXCOORD3,

  out float4 oColor : COLOR,
  
  uniform sampler2D diffuseMap : register(s0),
  uniform float4 lightDiffuse,
  uniform float4 lightAttenuation
) 
{
  float3 n = normalize(iNormal);
  float nDotL = max(dot(n,normalize(iLightVector)),0.0);
  
  float4 color = float4(0.0, 0.0, 0.0, 0.0);
  
  if(nDotL > 0.0) 
  {
    float att = 1.0 / (lightAttenuation[1] + lightAttenuation[2] * iDist + lightAttenuation[3] * iDist * iDist);
  
    //add diffuse light
    color += att * lightDiffuse * nDotL;
    
    //modulate diffuse texture
    color *= tex2D(diffuseMap, iTexCoord0);
  }
          
  oColor = color;
}
