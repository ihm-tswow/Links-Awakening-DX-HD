#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float mark0 = 0.0;
float mark1 = 0.0;
float mark2 = 0.0;

#define c0 float4(1.000, 1.000, 0.482, 1.000)
#define c1 float4(0.063, 0.129, 0.192, 1.000)
#define c2 float4(1.000, 1.000, 1.000, 1.000)

sampler s0;

float4 MainPS(float4 pos : SV_Position, float4 color1 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float4 color = tex2D(s0, coords) * color1;
	float sum = 0.4f * color.r + 0.2f * color.g + 0.4f * color.b;

	if(sum <= mark0)
		return color;
	if(sum <= mark1)
		return c0 * color.a * color1.a;
	if(sum <= mark2)
		return c1 * color.a * color1.a;
	
	return c2 * color.a * color1.a;
}

technique BasicColorDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};