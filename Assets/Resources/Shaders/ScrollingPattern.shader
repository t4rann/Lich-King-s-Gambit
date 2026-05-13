Shader "Custom/ScrollingPattern2D_UltraSmooth"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SpeedX ("Speed X", Float) = 0.1
        _SpeedY ("Speed Y", Float) = 0.1
        _Tiling ("Tiling", Vector) = (1, 1, 0, 0)
        _FilterSharpness ("Filter Sharpness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _SpeedX;
            float _SpeedY;
            float2 _Tiling;
            float _FilterSharpness;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Высокоточные UV с тилингом
                float2 uv = IN.texcoord * _Tiling;
                
                // Специальная функция для плавного смещения без дрожи
                // Используем синусоидальное время для абсолютной плавности
                float smoothTime = _Time.y;
                
                // Вычисляем смещение с высокой точностью
                float2 scrollOffset;
                scrollOffset.x = smoothTime * _SpeedX;
                scrollOffset.y = smoothTime * _SpeedY;
                
                // Применяем смещение
                uv += scrollOffset;
                
                // Зацикливаем с помощью frac для бесшовного повторения
                uv = frac(uv);
                
                // Умная фильтрация с учетом производных
                // Это ключевой момент для устранения дрожи
                float2 dx = ddx(uv) * _FilterSharpness;
                float2 dy = ddy(uv) * _FilterSharpness;
                
                // Используем tex2Dgrad для контроля фильтрации
                fixed4 c = tex2Dgrad(_MainTex, uv, dx, dy) * IN.color;
                c.rgb *= c.a;
                
                return c;
            }
            ENDCG
        }
    }
}