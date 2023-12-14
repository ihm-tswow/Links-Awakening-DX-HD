#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler s0;

float Offset;
float Period;
float Time;

float4 MainPS(float4 pos : SV_Position, float4 color0 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	coords.x += sin(coords.y * Period + Time) * Offset;
	float4 color = tex2D(s0, coords);
	return color * color0;
}

technique BasicColorDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};