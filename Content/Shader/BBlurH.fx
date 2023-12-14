#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_1
#endif

sampler s0;

float pixelX;
float mult0 = 0.25;
float mult1 = 0.125;
float mult2 = 0.075;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color1 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float val0 = tex2D(s0, float2(coords.x, coords.y)).a;

	if (val0 == 1)
		return float4(0, 0, 0, val0);

	float val1 = tex2D(s0, float2(coords.x - pixelX * 1, coords.y)).a;
	float val2 = tex2D(s0, float2(coords.x - pixelX * 2, coords.y)).a;
	float val3 = tex2D(s0, float2(coords.x - pixelX * 3, coords.y)).a;

	float val4 = tex2D(s0, float2(coords.x + pixelX * 1, coords.y)).a;
	float val5 = tex2D(s0, float2(coords.x + pixelX * 2, coords.y)).a;
	float val6 = tex2D(s0, float2(coords.x + pixelX * 3, coords.y)).a;

	float valOut = val0;

	if (val1 == 1 || val4 == 1)
		valOut = max(max(val1 * mult0, val4 * mult0), valOut);
	else if (val2 == 1 || val5 == 1)
		valOut = max(max(val2 * mult1, val5 * mult1), valOut);
	else if (val3 == 1 || val6 == 1)
		valOut = max(max(val3 * mult2, val6 * mult2), valOut);
	else
	{
		float mult = 1.95f;
		valOut = max(val1 * mult0 * mult, valOut);
		valOut = max(val2 * mult1 * mult, valOut);
		valOut = max(val3 * mult2 * mult, valOut);

		valOut = max(val4 * mult0 * mult, valOut);
		valOut = max(val5 * mult1 * mult, valOut);
		valOut = max(val6 * mult2 * mult, valOut);
	}
	
	return float4(0, 0, 0, valOut);
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}
