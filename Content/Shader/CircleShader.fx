#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0
#endif

Texture2D SpriteTexture;

float softRad = 30;
float size;
float centerX, centerY;

int width;
int height;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float2 pos = float2(input.TexCoord.x * width - centerX, input.TexCoord.y * height - centerY);
	float white = length(pos);

	white = clamp((size - white) / softRad, 0, 1);
	float black = 1 - white;

	return (input.Color * float4(1, 1, 1, 1)) * (black * black * black);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};