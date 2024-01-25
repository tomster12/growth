
Shader "Tom/Testing/_OutlineSample"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(0, 1)) = 0.1
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

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
            };
            
            uniform fixed4 _OutlineColor;
            uniform float _OutlineWidth;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.position);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float alpha_l = tex2D(_MainTex, i.uv + float2(_OutlineWidth, 0)).a;
                float alpha_r = tex2D(_MainTex, i.uv - float2(_OutlineWidth, 0)).a;
                float alpha_u = tex2D(_MainTex, i.uv + float2(0, _OutlineWidth)).a;
                float alpha_d = tex2D(_MainTex, i.uv - float2(0, _OutlineWidth)).a;

                int is_sampled = (alpha_l + alpha_r + alpha_u + alpha_d) > 0;
                int is_main = col.a > 0;
                int is_border = (is_sampled - is_main) > 0;

                fixed4 col_border = is_border * _OutlineColor;

                return col_border;
            }
            ENDCG
        }
    }
}
