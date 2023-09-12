
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class NoiseData
{
    [SerializeField] public float[] valueRange;
    [SerializeField] public float noiseScale = 1.0f;

    private float currentOffset = 0;


    public NoiseData()
    {
        this.valueRange = new float[] { 0.0f, 1.0f };
        this.noiseScale = 1.0f;
    }

    public NoiseData(float[] valueRange)
    {
        this.valueRange = valueRange;
        this.noiseScale = 1.0f;
    }

    public NoiseData(float[] valueRange, float noiseScale)
    {
        this.valueRange = valueRange;
        this.noiseScale = noiseScale;
    }


    public void RandomizeOffset() => currentOffset = -100000 + UnityEngine.Random.value * 200000;

    public float GetNoise(Vector2 pos) => GetNoise(pos.x, pos.y);

    public float GetNoise(float x, float y)
    {
        return valueRange[0] + Mathf.PerlinNoise(x * noiseScale + currentOffset, y * noiseScale + currentOffset) * (valueRange[1] - valueRange[0]);
    }

    public float GetCyclicNoise(float pct)
    {
        // Get noise on a circle
        float a = pct * (2 * Mathf.PI);
        float x = 0.5f + 0.5f * Mathf.Cos(a);
        float y = 0.5f + 0.5f * Mathf.Sin(a);
        return GetNoise(x, y);
    }


    // Based on shader defined here:
    // - D:\Files\Coding\Unity\Full\Growth\Library\PackageCache\com.aarthificial.pixelgraphics@5d5d2dab89\Runtime\Shaders\SimplexNoise3D.hlsl

    private static Vector4 Vector4Step(Vector4 edge, Vector4 x) => new Vector4(
                                                                    edge.x > x.x ? 0.0f : 1.0f,
                                                                    edge.y > x.y ? 0.0f : 1.0f,
                                                                    edge.z > x.z ? 0.0f : 1.0f,
                                                                    edge.w > x.w ? 0.0f : 1.0f);
    private static Vector4 Vector4Abs(Vector4 a) => new Vector4(
                                                            Mathf.Abs(a.x),
                                                            Mathf.Abs(a.y),
                                                            Mathf.Abs(a.z),
                                                            Mathf.Abs(a.w));
    private static Vector3 Vector3Max(Vector3 p, float val) => new Vector3(Mathf.Max(p.x, val), Mathf.Max(p.y, val), Mathf.Max(p.z, val));
    private static Vector4 Vector4Max(Vector4 p, float val) => new Vector4(Mathf.Max(p.x, val), Mathf.Max(p.y, val), Mathf.Max(p.z, val), Mathf.Max(p.w, val));
    private static Vector3 Vector3Floor(Vector3 p) => new Vector3(Mathf.Floor(p.x), Mathf.Floor(p.y), Mathf.Floor(p.z));
    private static Vector4 Vector4Floor(Vector4 p) => new Vector4(Mathf.Floor(p.x), Mathf.Floor(p.y), Mathf.Floor(p.z), Mathf.Floor(p.w));
    private static Vector3 Vector3Mult(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    private static Vector4 Vector4Mult(Vector4 a, Vector4 b) => new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);

    private static Vector3 mod289_3(Vector3 x)
    {
        // return x - floor(x / 289.0) * 289.0;
        return x - Vector3Floor(x / 289.0f) * 289.0f;
    }

    private static Vector4 mod289_4(Vector4 x)
    {
        // return x - floor(x / 289.0) * 289.0;
        return x - Vector4Floor(x / 289.0f) * 289.0f;
    }

    private static Vector4 permute(Vector4 x)
    {
        // return mod289_4((x * 34.0 + 1.0) * x);
        return mod289_4(Vector4Mult((x * 34.0f + Vector4.one), x));
    }

    private static Vector4 taylorInvSqrt(Vector4 r)
    {
        // return 1.79284291400159 - r * 0.85373472095314;
        return 1.79284291400159f * Vector4.one - r * 0.85373472095314f;
    }

    private static Vector4 snoise_grad(Vector3 v)
    {
        // const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
        Vector3 Cx = Vector3.one * (1.0f / 6.0f);
        Vector3 Cy = Vector3.one * (1.0f / 3.0f);

        // First corner
        // float3 i = floor(v + dot(v, C.yyy));
        // float3 x0 = v - i + dot(i, C.xxx);
        Vector3 i = Vector3Floor(v + Vector3.Dot(v, Cy) * Vector3.one);
        Vector3 x0 = v - i + Vector3.Dot(i, Cx) * Vector3.one;

        // Other corners
        // float3 g = step(x0.yzx, x0.xyz);
        // float3 l = 1.0 - g;
        // float3 i1 = min(g.xyz, l.zxy);
        // float3 i2 = max(g.xyz, l.zxy);
        Vector3 g = new Vector3(
            x0.y > x0.x ? 0 : 1,
            x0.z > x0.y ? 0 : 1,
            x0.x > x0.z ? 0 : 1);
        Vector3 l = Vector3.one - g;
        Vector3 i1 = new Vector3(
            Mathf.Min(g.x, l.z),
            Mathf.Min(g.y, l.x),
            Mathf.Min(g.z, l.y));
        Vector3 i2 = new Vector3(
            Mathf.Max(g.x, l.z),
            Mathf.Max(g.y, l.x),
            Mathf.Max(g.z, l.y));

        // x1 = x0 - i1  + 1.0 * C.xxx;
        // x2 = x0 - i2  + 2.0 * C.xxx;
        // x3 = x0 - 1.0 + 3.0 * C.xxx;
        // float3 x1 = x0 - i1 + C.xxx;
        // float3 x2 = x0 - i2 + C.yyy;
        // float3 x3 = x0 - 0.5;
        Vector3 x1 = x0 - i1 + Cx;
        Vector3 x2 = x0 - i2 + Cy;
        Vector3 x3 = x0 - 0.5f * Vector3.one;

        // Permutations
        // i = mod289_3(i); // Avoid truncation effects in permutation
        // float4 p =
        //      permute(
        //          permute(
        //              permute(i.z + float4(0.0, i1.z, i2.z, 1.0))
        //              + i.y + float4(0.0, i1.y, i2.y, 1.0)
        //          )
        //          + i.x + float4(0.0, i1.x, i2.x, 1.0)
        //      );
        i = mod289_3(i);
        Vector4 p = permute(
                        permute(
                            permute(Vector4.one * i.z + new Vector4(0.0f, i1.z, i2.z, 1.0f))
                            + Vector4.one * i.y + new Vector4(0.0f, i1.y, i2.y, 1.0f)
                        )
                        + Vector4.one * i.x + new Vector4(0.0f, i1.x, i2.x, 1.0f)
                    );

        // Gradients: 7x7 points over a square, mapped onto an octahedron.
        // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
        // float4 j = p - 49.0 * floor(p / 49.0); // mod(p,7*7)
        Vector4 j = p - 49.0f * Vector4Floor(p / 49.0f);

        // float4 x_ = floor(j / 7.0);
        // float4 y_ = floor(j - 7.0 * x_); // mod(j,N)
        Vector4 x_ = Vector4Floor(j / 7.0f);
        Vector4 y_ = Vector4Floor(j - 7.0f * x_);

        // float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
        // float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;
        Vector4 x = (x_ * 2.0f + 0.5f * Vector4.one) / 7.0f - Vector4.one;
        Vector4 y = (y_ * 2.0f + 0.5f * Vector4.one) / 7.0f - Vector4.one;

        // float4 h = 1.0 - abs(x) - abs(y);
        Vector4 h = Vector4.one - Vector4Abs(x) - Vector4Abs(y);

        // float4 b0 = float4(x.xy, y.xy);
        // float4 b1 = float4(x.zw, y.zw);
        Vector4 b0 = new Vector4(x.x, x.y, y.x, y.y);
        Vector4 b1 = new Vector4(x.z, x.w, y.z, y.w);

        // //float4 s0 = float4(lessThan(b0, 0.0)) * 2.0 - 1.0;
        // //float4 s1 = float4(lessThan(b1, 0.0)) * 2.0 - 1.0;
        // float4 s0 = floor(b0) * 2.0 + 1.0;
        // float4 s1 = floor(b1) * 2.0 + 1.0;
        // float4 sh = -step(h, 0.0);
        Vector4 s0 = Vector4Floor(b0) * 2.0f + Vector4.one;
        Vector4 s1 = Vector4Floor(b1) * 2.0f + Vector4.one;
        Vector4 sh = -Vector4Step(h, Vector4.zero);

        // float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
        // float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
        Vector4 a0 = new Vector4(
            b0.x + s0.x * sh.x,
            b0.z + s0.z * sh.x,
            b0.y + s0.y * sh.y,
            b0.w + s0.w * sh.y
        );
        Vector4 a1 = new Vector4(
            b1.x + s1.x * sh.z,
            b1.z + s1.z * sh.z,
            b1.y + s1.y * sh.w,
            b1.w + s1.w * sh.w
        );

        // float3 g0 = float3(a0.xy, h.x);
        // float3 g1 = float3(a0.zw, h.y);
        // float3 g2 = float3(a1.xy, h.z);
        // float3 g3 = float3(a1.zw, h.w);
        Vector3 g0 = new Vector3(a0.x, a0.y, h.x);
        Vector3 g1 = new Vector3(a0.z, a0.w, h.y);
        Vector3 g2 = new Vector3(a1.x, a1.y, h.z);
        Vector3 g3 = new Vector3(a1.z, a1.w, h.w);

        // Normalise gradients
        // float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
        Vector4 norm = taylorInvSqrt(new Vector4(Vector3.Dot(g0, g0), Vector3.Dot(g1, g1), Vector3.Dot(g2, g2), Vector3.Dot(g3, g3)));
        // g0 *= norm.x;
        // g1 *= norm.y;
        // g2 *= norm.z;
        // g3 *= norm.w;
        g0 *= norm.x;
        g1 *= norm.y;
        g2 *= norm.z;
        g3 *= norm.w;

        // Compute noise and gradient at P
        // float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
        // float4 m2 = m * m;
        // float4 m3 = m2 * m;
        // float4 m4 = m2 * m2;
        Vector4 m = Vector4Max(Vector4.one * 0.6f - new Vector4(Vector3.Dot(x0, x0), Vector3.Dot(x1, x1), Vector3.Dot(x2, x2), Vector3.Dot(x3, x3)), 0.0f);
        Vector4 m2 = Vector4Mult(m, m);
        Vector4 m3 = Vector4Mult(m2, m);
        Vector4 m4 = Vector4Mult(m2, m2);
        // float3 grad =
        //     -6.0 * m3.x * x0 * dot(x0, g0) + m4.x * g0 +
        //     -6.0 * m3.y * x1 * dot(x1, g1) + m4.y * g1 +
        //     -6.0 * m3.z * x2 * dot(x2, g2) + m4.z * g2 +
        //     -6.0 * m3.w * x3 * dot(x3, g3) + m4.w * g3;
        // float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
        // return 42.0 * float4(grad, dot(m4, px));
        Vector3 grad =
            -6.0f * m3.x * x0 * Vector3.Dot(x0, g0) + m4.x * g0 +
            -6.0f * m3.y * x1 * Vector3.Dot(x1, g1) + m4.y * g1 +
            -6.0f * m3.z * x2 * Vector3.Dot(x2, g2) + m4.z * g2 +
            -6.0f * m3.w * x3 * Vector3.Dot(x3, g3) + m4.w * g3;
        Vector4 px = new Vector4(Vector3.Dot(x0, g0), Vector3.Dot(x1, g1), Vector3.Dot(x2, g2), Vector3.Dot(x3, g3));
        return 42.0f * new Vector4(grad.x, grad.y, grad.z, Vector4.Dot(m4, px));
    }   

    public static void SimplexNoise3D_float(Vector3 Vertex, float Scale, out float Noise, out Vector3 Gradient)
    {
        Vector4 noise_vector = snoise_grad(Vertex * Scale);
        Noise = noise_vector.w;
        Gradient = new Vector3(noise_vector.x, noise_vector.y, noise_vector.z);
    }
}
