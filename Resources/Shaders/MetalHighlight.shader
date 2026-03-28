Shader "Custom/MetalHighlight_Rotated_Fixed"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _HighlightTex("Highlight Texture", 2D) = "white" {}
        _HighlightColor("Highlight Color", Color) = (1,1,1,1)
        _HighlightSpeed("Highlight Speed", Range(0,5)) = 1
        _HighlightAngle("Highlight Angle (deg)", Range(-180,180)) = 45
        _BlinkInterval("Blink Interval (sec)", Range(0.1,5)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _HighlightTex;
            float4 _MainTex_ST;
            float4 _HighlightColor;
            float _HighlightSpeed;
            float _HighlightAngle;
            float _BlinkInterval;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Вращение UV вокруг центра
            float2 RotateUV(float2 uv, float angle)
            {
                float rad = radians(angle);
                float2 center = float2(0.5, 0.5);
                uv -= center;
                float cosA = cos(rad);
                float sinA = sin(rad);
                float2 rotated;
                rotated.x = uv.x * cosA - uv.y * sinA;
                rotated.y = uv.x * sinA + uv.y * cosA;
                return rotated + center;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Основной спрайт
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb *= col.a;

                // Направление движения (угол из свойств)
                float moveAngle = _HighlightAngle;
                float moveRad = radians(moveAngle);
                float2 moveDir = float2(cos(moveRad), sin(moveRad));
                
                // Угол для поворота текстуры = направление движения + 90 градусов
                // (так как текстура по умолчанию ориентирована вверх)
                float textureAngle = moveAngle - 90;

                // Цикл интервала для плавного движения
                float cycle = frac(_Time.y / _BlinkInterval);
                
                // Смещение UV вдоль направления движения
                float2 highlightUV = i.uv;
                
                // Двигаем текстуру в направлении moveDir
                // Изменяем фазу так, чтобы текстура "выезжала" из-за края
                float offset = cycle * _HighlightSpeed;
                
                // Смещаем UV вдоль направления движения
                highlightUV += moveDir * offset;
                
                // Если вышли за пределы, возвращаем в начало
                // Более простая логика для плавного повторения
                highlightUV = frac(highlightUV);
                
                // Вращаем UV текстуры блика, чтобы она была ориентирована вдоль направления движения
                highlightUV = RotateUV(highlightUV, textureAngle);
                
                // Берем текстуру шлейфа
                fixed4 highlight = tex2D(_HighlightTex, highlightUV);
                highlight.rgb *= _HighlightColor.rgb;
                highlight.a *= _HighlightColor.a;

                // Накладываем блик поверх спрайта
                col.rgb += highlight.rgb * highlight.a;

                return col;
            }

            ENDCG
        }
    }
}