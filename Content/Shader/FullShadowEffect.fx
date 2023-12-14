#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 xViewProjection;

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

sampler s0;

float pixelWidth;

float rotation = 1.5;

float offsetX;
float height;

struct VertexShaderOutput
{
	float4 Pos : SV_Position;
	float2 TextureCoordinates : TEXCOORD0;
	float2 DrawPosition : TEXCOORD1;
	float2 SourceSize : TEXCOORD2;
	float4 Color: COLOR0;
};

struct PixelShaderInput
{
	float4 Pos : SV_Position;
	float2 TextureCoordinates : TEXCOORD0;
	float2 DrawPosition : TEXCOORD1;
	float2 SourceSize : TEXCOORD2;
	float2 RealCoord : TEXCOORD3;
	float4 Color: COLOR0;
};

PixelShaderInput SpriteVertexShader(VertexShaderOutput input)
{
    PixelShaderInput Output = (PixelShaderInput)0;

	input.Pos.x += ((input.SourceSize.y - input.Pos.y + input.DrawPosition.y) * offsetX);
	input.Pos.y -= ((-(input.DrawPosition.y + input.SourceSize.y) + input.Pos.y) * (1 - height));

	Output.RealCoord = float2(
		(input.Pos.x - input.DrawPosition.x) / input.SourceSize.x, 
		(input.Pos.y - input.DrawPosition.y - (input.SourceSize.y * (1 - height))) / (input.SourceSize.y * height));

    Output.Pos = mul(input.Pos, xViewProjection);
    Output.TextureCoordinates = input.TextureCoordinates;
	Output.Color = input.Color;

    return Output;
}

float4 MainPS(PixelShaderInput input) : COLOR
{
	float4 texColor = tex2D(s0, float2(input.TextureCoordinates.x, input.TextureCoordinates.y));
	return texColor.a * input.Color;
}

technique SpriteDrawing
{
	pass P0
	{
        VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};