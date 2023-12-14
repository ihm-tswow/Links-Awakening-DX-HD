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

#define c0 float4(1.000, 0.710, 0.192, 1.000)
#define c1 float4(0.871, 0.000, 0.000, 1.000)
#define c2 float4(0.000, 0.000, 0.000, 1.000)

sampler s0;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color1 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float4 color = tex2D(s0, coords) * color1;
	float sum = 0.333f * color.r + 0.333f * color.g + 0.333f * color.b;

	if(sum < mark0)
		return c0 * color.a * color1.a;	
	if(sum < mark1)
		return c1 * color.a * color1.a;
	
	return c2 * color.a * color1.a;
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}