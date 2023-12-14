#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_1
#endif

Texture2D sprBlur;

sampler sampler0 : register(s0) {
	Filter = POINT;
};
sampler sampler1  : register(s1) {
	Texture = (sprBlur);
};

float4 blurColor;

float radius = 2.5f;
float scale = 1;

int width, height;
int textureWidth, textureHeight;

float4 PixelShaderFunction(float4 pos : SV_Position, float4 color1 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	#if OPENGL
	pos.y = height - pos.y;
	#endif

	float posX = coords.x * width;
	float posY = coords.y * height;

	float distX = min(posX, abs(posX - width));
	float distY = min(posY, abs(posY - height));

	float circle = 1;

	if (distX < radius && distY < radius)
	{
		float a = radius - distX;
		float b = radius - distY;
		float dist = clamp((radius - sqrt(a * a + b * b)) * scale, 0, 1);
		circle = dist;
	}
	else
	{
		float distEdge = clamp(min(distX, distY) * scale, 0, 1);
		circle = distEdge;
	}

	if (radius == 0)
		circle = 1;

	float4 textureSample = tex2D(sampler0, float2(coords.x, coords.y));
	float4 blurSample = tex2D(sampler1, float2(pos.x / textureWidth, pos.y / textureHeight));

	return ((blurSample * blurColor * (1 - color1.a) + color1) * textureSample.r + textureSample * (1 - textureSample.r) * blurColor) * circle;
}

technique Technique1
{
	pass Pass1
	{
		PixelShader = compile  PS_SHADERMODEL  PixelShaderFunction();
	}
}
