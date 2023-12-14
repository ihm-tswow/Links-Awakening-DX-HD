#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler s0;

float pixelY;
float mult0;
float mult1;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color1 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float4 sideColors 
				= tex2D(s0, float2(coords.x, coords.y - pixelY * 0.5)) * mult0;
	sideColors += tex2D(s0, float2(coords.x, coords.y + pixelY * 0.5)) * mult0;
	sideColors += tex2D(s0, float2(coords.x, coords.y - pixelY * 2.5)) * mult1;
	sideColors += tex2D(s0, float2(coords.x, coords.y + pixelY * 2.5)) * mult1;
	
	return sideColors;
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}
