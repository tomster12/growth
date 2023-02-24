
Shader "Tom/OutlineFill"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
        [HDR]_OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        //_OutlineWidth ("Outline Width", Range(1.0, 3.0)) = 1.2
        _OutlineWidth ("Outline Width", Range(0.0, 20.0)) = 1.2
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+110"
            "RenderType"="Overlay"
        }

        Pass
        {
            Name "Fill"
            Cull Off
            ZTest [_ZTest]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            Stencil
            {
                Ref 1
                Comp NotEqual
            }


            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag
            
            struct appdata
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            uniform fixed4 _OutlineColor;
            uniform float _OutlineWidth;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.position * _OutlineWidth);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = _OutlineColor;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return i.color * (col.a > 0);
            }
            ENDCG
        }
    }
}
