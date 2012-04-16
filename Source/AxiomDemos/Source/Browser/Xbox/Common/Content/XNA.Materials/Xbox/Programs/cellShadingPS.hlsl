
/* Cel shading vertex program for single-pass rendering
  converted from CG example program
*/
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
	