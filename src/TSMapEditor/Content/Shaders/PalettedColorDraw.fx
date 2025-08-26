#pragma enable_d3d11_debug_symbols

#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

// Flexible shader for rendering objects.
// Can render objects in either paletted or RGBA mode,
// takes depth into account, and can also draw shadows.

// Relies on custom data in the vertex shader,
// meaning it is not compatible with SpriteBatch
// and needs a custom batcher instead.

float WorldTextureHeight;
bool ComplexDepth;
bool IncreaseDepthUpwards;
bool DecreaseDepthUpwards;
bool IsShadow;
bool UsePalette;
bool UseRemap;
float Opacity;

Texture2D MainTexture;
SamplerState MainSampler
{
    Texture = <MainTexture>;
    AddressU = clamp;
    AddressV = clamp;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

Texture2D PaletteTexture;
SamplerState PaletteSampler
{
    Texture = <PaletteTexture>;
    AddressU = clamp;
    AddressV = clamp;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

struct VertexShaderInput
{
    float3 Position : POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
    float4 CustomData : TEXCOORD1;
};

matrix WorldViewProj : register(c0);

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
    float4 CustomData : TEXCOORD1;
};

struct PixelShaderOutput
{
    float4 color : SV_Target0;
    float depthTarget : SV_Target1;
    float depthEmbedded : SV_Depth;
};


VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(float4(input.Position, 1.0), WorldViewProj);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    output.CustomData = input.CustomData;
    return output;
}


PixelShaderOutput MainPS(VertexShaderOutput input)
{
    PixelShaderOutput output = (PixelShaderOutput) 0;

    // We need to read from the main texture first,
    // otherwise the output will be black!
    float4 tex = MainTexture.Sample(MainSampler, input.TextureCoordinates);

    // Discard transparent areas
    clip(tex.a == 0.0f ? -1 : 1);

    float totalDepth = input.Position.z;

    if (IsShadow)
    {
        output.color = float4(0, 0, 0, 0.5);

        output.depthTarget = totalDepth;
        output.depthEmbedded = totalDepth;
    }
    else
    {
        if (ComplexDepth || IncreaseDepthUpwards || DecreaseDepthUpwards)
        {
            float imageHeight = input.CustomData.y;

            uint spriteSheetWidth;
            uint spriteSheetHeight;
            MainTexture.GetDimensions(spriteSheetWidth, spriteSheetHeight);

            float depthMultiplier = 0.9;

            float distanceFromTop = (input.TextureCoordinates.y - input.CustomData.w) * spriteSheetHeight;
            float distanceFromBottom = imageHeight - distanceFromTop;

            if (ComplexDepth)
            {
                float imageWidth = input.CustomData.x;

                float centralX = imageWidth * input.Color.a;
                float dx = abs((spriteSheetWidth * (input.TextureCoordinates.x - input.CustomData.z)) - centralX);
                totalDepth = input.Position.z + (((distanceFromBottom - dx) / WorldTextureHeight) * depthMultiplier);
            }
            else if (IncreaseDepthUpwards)
            {
                totalDepth = input.Position.z + ((distanceFromBottom / WorldTextureHeight) * depthMultiplier);
            }
            else if (DecreaseDepthUpwards)
            {
                totalDepth = input.Position.z - ((distanceFromBottom / WorldTextureHeight) * depthMultiplier);
            }
        }

        output.depthTarget = totalDepth;
        output.depthEmbedded = totalDepth;

        if (UsePalette)
        {
            // Get color from palette
            float4 paletteColor = PaletteTexture.Sample(PaletteSampler, float2(tex.a, 0.5));

            // We need to convert the grayscale into remap
            if (UseRemap)
            {
                float brightness = max(paletteColor.r, max(paletteColor.g, paletteColor.b));

                // Brigthen it up a bit
                brightness = brightness * 1.25;

                output.color = float4(brightness * input.Color.r, brightness * input.Color.g, brightness * input.Color.b, paletteColor.a) * Opacity;
            }
            else
            {
                // Multiply the color by 2. This is done because unlike map lighting which can exceed 1.0 and go up to 2.0,
                // the color values passed in the pixel shader input are capped at 1.0.
                // So the multiplication is done to translate pixel shader input color space into in-game color space.
                // We lose a bit of precision from doing this, but we'll have to accept that.
                output.color = float4(paletteColor.r * input.Color.r * 2.0,
                    paletteColor.g * input.Color.g * 2.0,
                    paletteColor.b * input.Color.b * 2.0,
                    paletteColor.a) * Opacity;
            }
        }
        else
        {
            output.color = float4(tex.r * input.Color.r * 2.0,
                tex.g * input.Color.g * 2.0,
                tex.b * input.Color.b * 2.0, tex.a) * Opacity;
        }
    }

    return output;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};