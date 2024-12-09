Texture2D<float4> Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);

struct Input
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

float4 main(Input input) : SV_Target0
{
    float4 sampledColor = Texture.Sample(Sampler, input.TexCoord);

    if (sampledColor.a < 0.02)
    {
        discard;
    }

    return input.Color * sampledColor;
}