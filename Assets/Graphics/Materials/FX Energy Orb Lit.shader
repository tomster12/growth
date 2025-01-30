Shader "Tom/EnergyOrbLit"
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
        //Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "LightMode" = "UniversalForward" }
		Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#define MAX_ORB_COUNT 1024

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 posCS : SV_POSITION;
				float2 posWS : TEXCOORD1;
				float2 uv : TEXCOORD0;
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
				o.posCS = TransformObjectToHClip(v.vertex.xyz);
				o.posWS = TransformObjectToWorld(v.vertex.xyz).xy;
				o.uv = v.uv;
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
				float2 p = i.posWS.xy;
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

				half4 baseColour;
                
				if (inside)
				{
					baseColour = _InsideColour;
				}
				if (blend < _BlendThreshold)
				{
					discard;
				}
				if (blend < _EdgeThreshold)
				{
					baseColour = _EdgeColour;
				}
				else
				{
					float v = (blend - _EdgeThreshold) / (_InsideThreshold - _EdgeThreshold);
					v = saturate(v);
                
					if (v < 0.2)
						v = 0;
					else if (v < 0.6)
						v = 0.5;
					else
						v = 1;

					baseColour = lerp(_OutsideColourA, _OutsideColourB, v);
				}
                
				//return baseColour;

				// Calculate lighting
				Light mainLight = GetMainLight(); // Get the main directional light.
				half3 lightDir = normalize(mainLight.direction);
				half3 normal = half3(0.0, 0.0, 1.0); // Assuming flat surface normal for 2D.
				half3 lightColor = mainLight.color.rgb;

				// Diffuse lighting
				float ndotl = max(dot(normal, lightDir), 0.0);
				half3 diffuse = lightColor * ndotl;

				// Apply lighting to the base color
				baseColour.rgb *= diffuse;

				return baseColour;
			}
            ENDHLSL
        }
    }

	FallBack "Transparent/Diffuse"
}
