float4x4 World;
float4x4 View;
float4x4 Projection;

texture modelTexture : register(t0);
sampler ModelTextureSampler : register(s0);

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoords    : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 TexCoords    : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    
    output.Position = mul(viewPosition, Projection);
	output.TexCoords=input.TexCoords;
    
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    return tex2D(ModelTextureSampler , input.TexCoords);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_1_1 VertexShaderFunction();
        PixelShader = compile ps_1_1 PixelShaderFunction();
    }
}
