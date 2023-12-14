#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

sampler s0;

float3 W = float3(0.3, 0.59, 0.11);
float percentage;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color0 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float4 color = tex2D(s0, float2(coords.x, coords.y));
    float intensity = dot(color.rgb, W);
	float4 grayscale =  float4(intensity, intensity, intensity, color.a);
	return lerp(color, grayscale, percentage) * color0;
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}
