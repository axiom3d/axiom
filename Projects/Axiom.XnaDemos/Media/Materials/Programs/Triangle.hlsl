
//application to vertex structure
struct a2v
{ 
    float4 position : POSITION0;
    float4 color : COLOR0;
};

//vertex to pixel shader structure
struct v2p
{
    float4 position : POSITION0;
    float4 color : COLOR0;
};

//pixel shader to screen
struct p2f
{
    float4 color : COLOR0;
};

//VERTEX SHADER
v2p vs( in a2v IN, 
            uniform float4x4 uModelViewProjection   // model-view-projection matrix
 ) 
{
	v2p OUT;
    //getting to position to object space
    OUT.position = mul( uModelViewProjection, IN.position);
    OUT.color = IN.color;
    return OUT;
}

void ps( in v2p IN, out p2f OUT )
{
    //output some color
    OUT.color = IN.color;
}

 

