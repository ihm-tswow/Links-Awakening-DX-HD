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

Texture2D sprLight;

sampler sampler0 : register(s0) { };
sampler sampler1  : register(s1)
{
	Texture = (sprLight);
};

float lightState = 0;
int mode = 0;
int width, height;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color1 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float4 texColor = tex2D(sampler0, coords);
	float4 lightColor = tex2D(sampler1, float2(pos.x / width, pos.y / height));// * float4(1.963, 1.0, 5.149, 1);
	
	lightColor.r = clamp(lightColor.r, 0, 1);
	lightColor.b = clamp(lightColor.b, 0, 1);

	float3 lerpTarget = float3(1, 1, 1);
	if (mode == 1)
		lerpTarget = texColor.rgb;

	return float4(lerp(texColor.rgb * lightColor.rgb, lerpTarget, lightState), texColor.a) * color1;
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}