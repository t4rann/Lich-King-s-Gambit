Shader "Unlit/AnimatedRadialGradient2D_PixelArt"
{
    Properties
    {
        _Color1("Color 1", Color) = (1,0,0,1)
        _Color2("Color 2", Color) = (0,1,0,1)
        _Color3("Color 3", Color) = (0,0,1,1)
        _Color4("Color 4", Color) = (1,1,0,1)

        _Speed("Shift Speed", Float) = 0.2
        _NoiseStrength("Noise Strength", Range(0,1)) = 0.15
        _NoiseScale("Noise Scale", Float) = 6.0

        _PixelSize("Pixel Size", Range(0.001,0.1)) = 0.02
        _ColorLevels("Color Levels", Range(2,8)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float4 _Color4;

            float _Speed;
            float _NoiseStrength;
            float _NoiseScale;

            float _PixelSize;
            int _ColorLevels;

            // ----------- HASH / NOISE -----------
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) +
                       (c - a) * u.y * (1.0 - u.x) +
                       (d - b) * u.x * u.y;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.vertex.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ----------- ПИКСЕЛИЗАЦИЯ UV -----------
                float2 uv = floor(i.uv / _PixelSize) * _PixelSize;

                // ----------- ПИКСЕЛИЗАЦИЯ ШУМА -----------
                float nRaw = noise(uv * _NoiseScale + _Time.y * 0.5);
                float n = floor(nRaw * _ColorLevels) / _ColorLevels;
                n = (n - 0.5) * _NoiseStrength;

                // ----------- РАДИАЛЬНОЕ РАССТОЯНИЕ -----------
                float dist = length(uv);

                // ----------- АНИМАЦИЯ ЦВЕТОВ -----------
                float t1 = 0.5 + 0.5 * sin(_Time.y * _Speed + n);
                float t2 = 0.5 + 0.5 * sin(_Time.y * _Speed + 1.5 + n);
                float t3 = 0.5 + 0.5 * sin(_Time.y * _Speed + 3.0 + n);
                float t4 = 0.5 + 0.5 * sin(_Time.y * _Speed + 4.5 + n);

                float4 col = _Color1 * t1 +
                             _Color2 * t2 +
                             _Color3 * t3 +
                             _Color4 * t4;

                col.rgb /= (t1 + t2 + t3 + t4);

                // ----------- КВАНТИЗАЦИЯ ЦВЕТА ДЛЯ ПИКСЕЛЬНОГО СТИЛЯ -----------
                col.rgb = floor(col.rgb * _ColorLevels) / _ColorLevels;

                // ----------- РАДИАЛЬНОЕ ЗАТУХАНИЕ -----------
                float noisyDist = saturate(dist + n);
                fixed4 finalColor = lerp(col, _Color1, noisyDist);

                return finalColor;
            }
            ENDCG
        }
    }
}
