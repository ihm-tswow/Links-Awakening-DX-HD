#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#define PI 3.1415926538

sampler s0;

int width;
int height;
float scale;
float brightness;
float offset;

float offsetWidth;
float offsetHeight;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color0 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	// offset
	float offsetX = sin(offset + coords.y * (height / scale) / offsetHeight * PI + PI / 2) * (offsetWidth / (width / scale));
	return tex2D(s0, float2(coords.x + offsetX, coords.y)) * (1 - brightness) + float4(1, 1, 1, 1) * brightness;
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}
