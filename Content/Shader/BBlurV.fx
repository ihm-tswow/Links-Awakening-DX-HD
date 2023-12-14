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
float mult0 = 0.25;
float mult1 = 0.125;
float mult2 = 0.075;

// float mult0 = 0.45;
// float mult1 = 0.25;
// float mult2 = 0.075;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color1 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float valMax = tex2D(s0, float2(coords.x, coords.y)).a;

	valMax = max(tex2D(s0, float2(coords.x, coords.y - pixelY * 1)).a * mult0, valMax);
	valMax = max(tex2D(s0, float2(coords.x, coords.y - pixelY * 2)).a * mult1, valMax);
	valMax = max(tex2D(s0, float2(coords.x, coords.y - pixelY * 3)).a * mult2, valMax);

	valMax = max(tex2D(s0, float2(coords.x, coords.y + pixelY * 1)).a * mult0, valMax);
	valMax = max(tex2D(s0, float2(coords.x, coords.y + pixelY * 2)).a * mult1, valMax);
	valMax = max(tex2D(s0, float2(coords.x, coords.y + pixelY * 3)).a * mult2, valMax);

	return float4(0, 0, 0, valMax);
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}
