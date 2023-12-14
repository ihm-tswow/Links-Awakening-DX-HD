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

sampler s0;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float4 texColor = tex2D(s0, coords);
	float dist = 1.0 - clamp(((0.05 + texColor.a * 0.95) - color.a) / 0.05, 0, 1);
	return float4(0, 0, 0, dist);
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}