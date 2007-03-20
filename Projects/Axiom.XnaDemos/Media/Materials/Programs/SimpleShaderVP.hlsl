/////////////////////////////////////////////////////////////////////////////////
//
// SimpleShaderVP.cg
//
// Hamilton Chong
// (c) 2006
//
//
/////////////////////////////////////////////////////////////////////////////////

// Define inputs from application.
struct VertexIn
{
  float4 position       : POSITION;     // vertex position in object space
  float4 normal         : NORMAL;       // vertex normal in object space
  float4 texCoord	: TEXCOORD0;	// texture coordinate for vertex
};

// Define outputs from vertex shader.
struct Vertex
{
  float4 position       : POSITION;     // vertex position in post projective space
  float4 texCoord    	: TEXCOORD0;    // texture coordinate for vertex
  float  diffuse        : TEXCOORD1;    	// diffuse shading value
};

Vertex main(VertexIn         In,
            uniform float4x4 uModelViewProjection,   // model-view-projection matrix
            uniform float4   uLightPosition          // light position in object space
            )
{
    Vertex Out;

    // compute diffuse shading
    float3 lightDirection = normalize(uLightPosition.xyz - In.position.xyz);
    Out.diffuse = dot(In.normal.xyz, lightDirection);

    // store texture coordinates
    Out.texCoord = In.texCoord;

    // compute vertex's homogenous screen-space coordinates
    Out.position = mul(uModelViewProjection, In.position);

    return Out;
}