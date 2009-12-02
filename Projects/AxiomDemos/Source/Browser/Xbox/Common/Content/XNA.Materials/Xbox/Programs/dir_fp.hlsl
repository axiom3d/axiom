void main
(
	float2 iTexCoord0 : TEXCOORD0,
	float3 iNormal : TEXCOORD1, //in object space
	float3 iLightVector : TEXCOORD2, //in object space
  
	out float4 oColor : COLOR,

	uniform sampler2D diffuseMap, 
	uniform float4 lightDiffuse
) 
{
	float3 n = normalize(iNormal);
	float nDotL = max(dot(n, iLightVector),0.0);
  
	float4 color = float4(0.0, 0.0, 0.0, 0.0);
  
	if(nDotL > 0.0) 
	{

		//add diffuse light from material and light
		color += lightDiffuse * nDotL;
  	
		//modulate texture
		color *= tex2D(diffuseMap, iTexCoord0);
	}
          
	oColor = color;
}