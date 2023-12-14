#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0
#endif

const float PI = 3.14159265f;

float radius = 2.5f;
float scale = 1;
float centerX, centerY;

int width, height;

sampler sampler0 : register(s0) { };

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TexCoord : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 texColor = tex2D(sampler0, input.TexCoord);

	float posX = input.TexCoord.x * width;
	float posY = input.TexCoord.y * height;

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

	return texColor * input.Color * circle;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};