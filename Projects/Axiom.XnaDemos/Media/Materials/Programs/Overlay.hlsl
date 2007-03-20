sampler2D TextureMap : TEXUNIT0;

//application to vertex structure
struct a2v
{ 
	float4 position		: POSITION0;
	float4 normal		: NORMAL;       // vertex normal in object space
	float4 texCoord		: TEXCOORD0;	// texture coordinate for vertex
};

//vertex to pixel shader structure
struct v2p
{
	float4 position	: POSITION0;
	float4 texCoord	: TEXCOORD0;
	float  diffuse	: TEXCOORD1;    	// diffuse shading value
};

//pixel shader to screen
struct p2f
{
	float4 color	: COLOR0;
};

//VERTEX SHADER
v2p vs( 
        in a2v IN ,            
        uniform float4x4 uModelViewProjection,   // model-view-projection matrix
        uniform float4   uLightPosition          // light position in object space
      ) 
{
	v2p OUT;
    // compute diffuse shading
    float3 lightDirection = normalize(uLightPosition.xyz - IN.position.xyz);
    OUT.diffuse = dot(IN.normal.xyz, lightDirection);

    OUT.texCoord = IN.texCoord;

    //getting to position to object space
    OUT.position = mul( IN.position, uModelViewProjection );
    
    return OUT;
}
 
void ps( in v2p IN, out p2f OUT )
{
    //output some color
    OUT.color = tex2D(TextureMap, IN.texCoord );
}
 

