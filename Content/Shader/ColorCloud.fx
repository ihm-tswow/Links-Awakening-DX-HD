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

sampler sampler0 : register(s0) { };

float scale;
float scaleX;
float scaleY;

float4 color0 = float4(1, 0, 0, 1);
float4 color1 = float4(1, 1, 1, 1);

float2 offset;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float4 texColor = tex2D(sampler0, coords);
	
	float4 insideColor = color0;
	if (((int)(((pos.x / scaleX + offset.x / scale)) / 4) +
		 (int)(((pos.y / scaleY + offset.y / scale)) / 4)) % 2 == 0)
		insideColor = color1;

	return insideColor * texColor;
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}