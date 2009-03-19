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
	output.oPosition = mul(input.position,worldViewProj );

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