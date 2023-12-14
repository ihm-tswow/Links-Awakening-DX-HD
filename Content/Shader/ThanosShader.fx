#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

Texture2D SpriteTexture;
sampler2D sampler0 : register(s0)
{
};

Texture2D NoiceTexture;
sampler sampler1 : register(s1)
{
	Texture = (NoiceTexture);
    Filter = POINT;
};

float Percentage;
float2 Scale;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color1 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float4 texColor = tex2D(sampler0, coords);
	if ((Percentage > 0 && Percentage >= tex2D(sampler1, coords * Scale).r) || Percentage == 1)
		return float4(0, 0, 0, 0);
	else
		return texColor * color1;
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}