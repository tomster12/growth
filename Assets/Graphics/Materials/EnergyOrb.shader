Shader "Tom/EnergyOrb"
{
    Properties
    {
        _InsideColour ("Inside Colour", Color) = (1, 1, 1, 1)
        _OutsideColourA ("Outside Colour A", Color) = (1, 1, 1, 1)
        _OutsideColourB ("Outside Colour B", Color) = (1, 1, 1, 1)
        _EdgeColour ("Edge Colour", Color) = (1, 1, 1, 1)
        _BlendThreshold ("Blend Threshold", Float) = 0.5
        _EdgeThreshold ("Edge Threshold", Float) = 1.0
        _InsideThreshold ("Inside Threshold", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define MAX_ORB_COUNT 1024

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            StructuredBuffer<float4> _OrbData;
            int _OrbCount;
            float4 _InsideColour;
            float4 _OutsideColourA;
            float4 _OutsideColourB;
            float4 _EdgeColour;
            float _BlendThreshold;
            float _EdgeThreshold;
            float _InsideThreshold;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            float metaball(float2 p, float2 center, float size)
            {
                float2 d = p - center;
                float distance = length(d);
                return (size * size) / (distance * distance);
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 p = i.worldPos.xy;
                float blend = 0.0;
                bool inside = false;

                for (int j = 0; j < _OrbCount; j++)
                {
                    float2 orbPos = _OrbData[j].xy;
                    float orbSize = _OrbData[j].z;
                    float orbBlend = metaball(p, orbPos, orbSize);
                    
                    blend += orbBlend;

                    if (orbBlend >= _InsideThreshold)
					{
						inside = true;
					}
                }

                if (inside)
                {
                    return _InsideColour;
                }

                if (blend < _BlendThreshold)
				{
					discard;
				}

                if (blend < _EdgeThreshold)
				{
					return _EdgeColour;
				}

                float v = (blend - _EdgeThreshold) / (_InsideThreshold - _EdgeThreshold);
                v = saturate(v);

                if (v < 0.2) v = 0;
                else if (v < 0.6) v = 0.5;
				else v = 1;

                return lerp(_OutsideColourA, _OutsideColourB, v);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
