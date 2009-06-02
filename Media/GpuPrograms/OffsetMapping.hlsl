
/* Bump mapping with Parallax offset vertex program 
   In this program, we want to calculate the tangent space light end eye vectors 
   which will get passed to the fragment program to produce the per-pixel bump map 
   with parallax offset effect. 
*/ 

/* Vertex program that moves light and eye vectors into texture tangent space at vertex */ 

struct VS_INPUT
{
	float4 position   : POSITION;
      float3 normal      : NORMAL;
      float2 uv         : TEXCOORD0;
	float3 tangent     : TEXCOORD1;
};

float3 lightPosition;
float3 eyePosition;
float4x4 worldViewProj;

struct VS_OUTPUT
{
	float4 oPosition    : POSITION;
      float2 oUv          : TEXCOORD0; 
	float3 oLightDir    : TEXCOORD1;
      float3 oEyeDir       : TEXCOORD2;
      float3 oHalfAngle    : TEXCOORD3;
};

VS_OUTPUT main_vp(VS_INPUT input ) 
{  
	VS_OUTPUT output = (VS_OUTPUT)0;
   // calculate output position 
   output.oPosition = mul(worldViewProj, input.position); 

   // pass the main uvs straight through unchanged 
   output.oUv = input.uv; 

   // calculate tangent space light vector 
   // Get object space light direction 
   float3 lightDir = lightPosition - input.position.xyz; 
   float3 eyeDir = eyePosition - input.position.xyz; 
    
   // Calculate the binormal (NB we assume both normal and tangent are 
   // already normalised) 
   // NB looks like nvidia cross params are BACKWARDS to what you'd expect 
   // this equates to NxT, not TxN 
   float3 binormal = cross(input.tangent, input.normal); 
    
   // Form a rotation matrix out of the vectors 
   float3x3 rotation = float3x3(input.tangent, binormal, input.normal); 
    
   // Transform the light vector according to this matrix 
   lightDir = normalize(mul(rotation, lightDir)); 
   eyeDir = normalize(mul(rotation, eyeDir)); 

   output.oLightDir = lightDir;
   output.oEyeDir = eyeDir;
   output.oHalfAngle = normalize(eyeDir + lightDir);
   return output;
}

// General functions

// Expand a range-compressed vector
float3 expand(float3 v)
{
	return (v - 0.5) * 2;
}

float3 lightDiffuse;
float3 lightSpecular;
float4 scaleBias;

sampler2D normalHeightMap:register(s0);
sampler2D diffuseMap:register(s1);

float4  main_fp( VS_OUTPUT input ) : COLOR
{
	// get the height using the tex coords
	float height = tex2D(normalHeightMap, input.oUv).a;

	// scale and bias factors	
	float scale = scaleBias.x;
	float bias = scaleBias.y;

	// calculate displacement	
	float displacement = (height * scale) + bias;
	
	float3 uv2 = float3(input.oUv, 1);
	
	// calculate the new tex coord to use for normal and diffuse
	float2 newTexCoord = ((input.oEyeDir* displacement) + uv2).xy;
	
	// get the new normal and diffuse values
	float3 normal = expand(tex2D(normalHeightMap, newTexCoord).xyz);
	float3 diffuse = tex2D(diffuseMap, newTexCoord).xyz;
	
	float3 specular = pow(saturate(dot(normal, input.oHalfAngle)), 32) * lightSpecular;
	float3 col = diffuse * saturate(dot(normal, input.oLightDir)) * lightDiffuse + specular;
		
	return float4(col, 1);
}