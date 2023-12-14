#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_1
#endif

Texture2D sprBlur;

sampler sampler0 : register(s0) {
	Filter = POINT;
};
sampler sampler1  : register(s1) {
	Texture = (sprBlur);
};

int width, height;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color1 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	#if OPENGL
	pos.y = height - pos.y;
	#endif

	float4 textureSample = tex2D(sampler0, float2(coords.x, coords.y));

	return tex2D(sampler1, float2(pos.x / width, pos.y / height)) * color1 * textureSample.r + textureSample * (1 - textureSample.r) * color1;
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}
