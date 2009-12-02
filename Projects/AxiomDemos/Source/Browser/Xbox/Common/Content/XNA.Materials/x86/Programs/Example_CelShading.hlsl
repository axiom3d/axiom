
/* Cel shading vertex program for single-pass rendering
  converted from CG example program
*/
struct VS_INPUT
{
	float4 position : POSITION0;
	float3 normal : NORMAL0;
};

// parameters
float3 lightPosition;
float3 eyePosition;
float4 shininess;
float4 diffuse;
float4 specular;

float4x4 worldViewProj;

struct VS_OUTPUT
{
	float4 oPosition   : POSITION;
	float  diffuse	 : TEXCOORD0;
	float  specular	 : TEXCOORD1;
	float  edge		 : TEXCOORD2;
};

VS_OUTPUT main_vp(VS_INPUT input )	
{
	VS_OUTPUT output = (VS_OUTPUT)0;
	// calculate output position
	output.oPosition = mul(worldViewProj, input.position);

	// calculate light vector
	float3 N = normalize(input.normal);
	float3 L = normalize(lightPosition - input.position.xyz);
	
	// Calculate diffuse component
	output.diffuse = max(dot(N, L) , 0);

	// Calculate specular component
	float3 E = normalize(eyePosition - input.position.xyz);
	float3 H = normalize(L + E);
	output.specular = pow(max(dot(N, H), 0), shininess);
	// Mask off specular if diffuse is 0
	if (output.diffuse == 0) output.specular = 0;

	// Edge detection, dot eye and normal vectors
	output.edge = max(dot(N, E), 0);
	return output;
}

sampler1D diffuseRamp: register(s0);
sampler1D specularRamp:  register(s1);
sampler1D edgeRamp:  register(s2);

float4 main_fp( VS_OUTPUT input ) : COLOR
{
	// Step functions from textures
	input.diffuse= tex1D(diffuseRamp, input.diffuse).x;
	input.specular= tex1D(specularRamp, input.specular).x;
	input.edge = tex1D(edgeRamp, input.edge).x;

	return input.edge * ((diffuse*input.diffuse) + 
					(specular * input.specular ));
}
			 
			 
