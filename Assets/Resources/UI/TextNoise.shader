// TextNoise.shader
Shader "UI/TextNoise"
{
    Properties
    {
        [PerRendererData] _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        
        // 噪声参数
        _NoiseScale ("Noise Scale", Float) = 20.0
        _SpeedX ("Speed X", Float) = 0.1
        _SpeedY ("Speed Y", Float) = 0.1
        _ColorA ("Color A", Color) = (0.8, 0.1, 0.1, 1)  // 红色
        _ColorB ("Color B", Color) = (1.0, 0.8, 0.1, 1)  // 黄色
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.5
        _BlendAmount ("Noise Blend", Range(0, 1)) = 0.7
        
        // UI 参数
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Lighting Off
        Cull Off
        ZTest [unity_GUIZTestMode]
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            // 噪声参数
            float _NoiseScale;
            float _SpeedX;
            float _SpeedY;
            float4 _ColorA;
            float4 _ColorB;
            float _NoiseIntensity;
            float _BlendAmount;
            
            // 简单的噪声函数
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            // 柏林噪声函数
            float noise(float2 p)
            {
                float2 ip = floor(p);
                float2 u = frac(p);
                u = u * u * (3.0 - 2.0 * u);
                
                float res = lerp(
                    lerp(random(ip), random(ip + float2(1.0, 0.0)), u.x),
                    lerp(random(ip + float2(0.0, 1.0)), random(ip + float2(1.0, 1.0)), u.x),
                    u.y
                );
                return res;
            }
            
            // 分形布朗运动
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                return value;
            }
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                
                // 计算屏幕坐标用于噪声
                OUT.screenPos = ComputeScreenPos(OUT.vertex).xy;
                
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                // 采样字体纹理
                half4 color = tex2D(_MainTex, IN.texcoord);
                color += _TextureSampleAdd;
                color.a *= IN.color.a;
                
                // 计算基于时间的动画
                float time = _Time.y;
                float2 timeOffset = float2(_SpeedX * time, _SpeedY * time);
                
                // 使用屏幕坐标生成噪声，确保所有字符的噪声一致
                float2 noiseUV = IN.screenPos * _NoiseScale + timeOffset;
                
                // 生成噪声
                float n = fbm(noiseUV);
                
                // 映射到0-1范围
                n = saturate(n);
                
                // 从噪声生成颜色
                half4 noiseColor = lerp(_ColorA, _ColorB, n);
                noiseColor.a = 1.0;
                
                // 混合噪声颜色和字体颜色
                half4 finalColor = color;
                finalColor.rgb = lerp(finalColor.rgb, noiseColor.rgb, _BlendAmount);
                finalColor.rgb *= lerp(1.0, n, _NoiseIntensity);
                
                // 应用顶点颜色
                finalColor.rgb *= IN.color.rgb;
                
                // 应用裁剪
                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif
                
                return finalColor;
            }
            ENDCG
        }
    }
}