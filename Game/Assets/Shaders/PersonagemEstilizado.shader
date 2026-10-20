// Personagem estilizado (Bets de Rua): luz em faixas suaves (toon), contorno de luz (rim) e cor do time por
// máscara. Leve para placa integrada: uma passada de luz, sombras do URP, sem texturas extras obrigatórias.
//
// Máscara (_MascaraTime, opcional): R = cor principal do time (camisa), G = cor secundária (bermuda / detalhe),
// B = área "sem tinta" (pele, cabelo: não recebe a cor do time). Sem máscara, usa a cor do vértice com os
// mesmos canais; sem nenhum dos dois, nada é tingido.
// A tinta MULTIPLICA o branco/cinza da textura base (as dobras e o tecido aparecem por baixo da cor).
Shader "Bets/PersonagemEstilizado"
{
    Properties
    {
        [MainTexture] _BaseMap ("Textura base", 2D) = "white" {}
        [MainColor] _BaseColor ("Cor base", Color) = (1,1,1,1)
        [Normal] _BumpMap ("Relevo (normal)", 2D) = "bump" {}
        _BumpScale ("Força do relevo", Range(0, 2)) = 0.35

        [Header(Time)]
        _MascaraTime ("Máscara do time (R principal, G secundária)", 2D) = "black" {}
        [Toggle(_USAR_COR_VERTICE)] _UsarCorVertice ("Usar cor do vértice como máscara", Float) = 0
        _CorTime ("Cor principal do time", Color) = (0.15, 0.35, 0.9, 1)
        _CorTime2 ("Cor secundária do time", Color) = (0.08, 0.08, 0.1, 1)
        _ForcaTinta ("Força da tinta do time", Range(0, 1)) = 1

        [Header(Luz estilizada)]
        _Faixas ("Faixas de luz (0 = suave)", Range(0, 6)) = 3
        _Suavidade ("Suavidade entre faixas", Range(0.01, 1)) = 0.35
        _CorSombra ("Cor da sombra (multiplica)", Color) = (0.62, 0.6, 0.72, 1)
        _Ambiente ("Ambiente (céu) na sombra", Range(0, 1)) = 0.55
        _Brilho ("Brilho especular", Range(0, 1)) = 0.03
        _TamanhoBrilho ("Tamanho do brilho", Range(1, 128)) = 64

        [Header(Contorno de luz)]
        _CorRim ("Cor do contorno", Color) = (1, 0.95, 0.85, 1)
        _ForcaRim ("Força do contorno", Range(0, 2)) = 0.45
        _LarguraRim ("Largura do contorno", Range(0.5, 8)) = 3.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            half4 _CorTime;
            half4 _CorTime2;
            half _ForcaTinta;
            half _Faixas;
            half _Suavidade;
            half4 _CorSombra;
            half _Ambiente;
            half _Brilho;
            half _TamanhoBrilho;
            half4 _CorRim;
            half _ForcaRim;
            half _LarguraRim;
            half _UsarCorVertice;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _USAR_COR_VERTICE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);      SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MascaraTime);  SAMPLER(sampler_MascaraTime);

            struct Entrada
            {
                float4 posicao : POSITION;
                float3 normal  : NORMAL;
                float4 tangente : TANGENT;
                float2 uv      : TEXCOORD0;
                half4 cor      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Saida
            {
                float4 posicaoTela : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 posicaoMundo : TEXCOORD1;
                half3 normalMundo  : TEXCOORD2;
                half4 tangenteMundo : TEXCOORD3;
                half4 cor          : TEXCOORD4;
                float fogFactor    : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Saida Vert(Entrada e)
            {
                Saida s = (Saida)0;
                UNITY_SETUP_INSTANCE_ID(e);
                UNITY_TRANSFER_INSTANCE_ID(e, s);
                VertexPositionInputs p = GetVertexPositionInputs(e.posicao.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(e.normal, e.tangente);
                s.posicaoTela = p.positionCS;
                s.posicaoMundo = p.positionWS;
                s.normalMundo = n.normalWS;
                s.tangenteMundo = half4(n.tangentWS, e.tangente.w * GetOddNegativeScale());
                s.uv = TRANSFORM_TEX(e.uv, _BaseMap);
                s.cor = e.cor;
                s.fogFactor = ComputeFogFactor(p.positionCS.z);
                return s;
            }

            // Luz em faixas: quantiza o "quanto a luz bate" em _Faixas degraus com borda suave
            half Faixas(half ndl)
            {
                half x = saturate(ndl);
                if (_Faixas < 0.5h) return x;
                half escala = x * _Faixas;
                half degrau = floor(escala);
                half frac_ = escala - degrau;
                half borda = smoothstep(0.5h - _Suavidade * 0.5h, 0.5h + _Suavidade * 0.5h, frac_);
                return saturate((degrau + borda) / _Faixas);
            }

            half3 LuzEstilizada(Light luz, half3 normal, half3 olhar, half3 albedo)
            {
                half ndl = dot(normal, luz.direction);
                half luzFaixa = Faixas(ndl * 0.5h + 0.5h) * luz.shadowAttenuation * luz.distanceAttenuation;
                half3 cor = lerp(albedo * _CorSombra.rgb, albedo, luzFaixa) * luz.color;
                half3 meio = SafeNormalize(luz.direction + olhar);
                half brilho = smoothstep(0.5h, 0.55h, pow(saturate(dot(normal, meio)), _TamanhoBrilho)) * _Brilho;
                return cor + brilho * luz.color * luzFaixa;
            }

            half4 Frag(Saida s) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(s);
                half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, s.uv) * _BaseColor;

                // Cor do time: pela máscara (ou pela cor do vértice)
                #if defined(_USAR_COR_VERTICE)
                    half3 mascara = s.cor.rgb;
                #else
                    half3 mascara = SAMPLE_TEXTURE2D(_MascaraTime, sampler_MascaraTime, s.uv).rgb;
                #endif
                half cinza = dot(base.rgb, half3(0.299h, 0.587h, 0.114h));
                // Só as dobras do tecido passam para a tinta: o brilho "assado" na textura da IA é cortado
                half dobra = lerp(0.84h, 1.0h, smoothstep(0.08h, 0.45h, cinza));
                half3 tecido1 = _CorTime.rgb * dobra;
                half3 tecido2 = _CorTime2.rgb * dobra;
                half3 albedo = base.rgb;
                albedo = lerp(albedo, tecido1, mascara.r * _ForcaTinta);
                albedo = lerp(albedo, tecido2, mascara.g * _ForcaTinta);

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, s.uv), _BumpScale);
                half3 bitangente = s.tangenteMundo.w * cross(s.normalMundo, s.tangenteMundo.xyz);
                half3 normal = normalize(TransformTangentToWorld(normalTS, half3x3(s.tangenteMundo.xyz, bitangente, s.normalMundo)));
                half3 olhar = SafeNormalize(GetWorldSpaceViewDir(s.posicaoMundo));

                float4 coordSombra = TransformWorldToShadowCoord(s.posicaoMundo);
                Light principal = GetMainLight(coordSombra);
                half3 cor = LuzEstilizada(principal, normal, olhar, albedo);

                #if defined(_ADDITIONAL_LIGHTS)
                uint n = GetAdditionalLightsCount();
                for (uint i = 0u; i < n; ++i)
                {
                    Light extra = GetAdditionalLight(i, s.posicaoMundo);
                    cor += LuzEstilizada(extra, normal, olhar, albedo) * 0.6h;
                }
                #endif

                // Céu na sombra (luz ambiente do URP) e oclusão de tela (SSAO), se ligado
                half3 ambiente = SampleSH(normal) * albedo * _Ambiente;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(s.posicaoTela));
                    ambiente *= ao.indirectAmbientOcclusion;
                    cor *= lerp(1.0h, ao.directAmbientOcclusion, 0.5h);
                #endif
                cor += ambiente;

                // Contorno de luz: separa o boneco do fundo (silhueta limpa)
                half rim = pow(1.0h - saturate(dot(normal, olhar)), _LarguraRim) * _ForcaRim;
                cor += _CorRim.rgb * rim * saturate(principal.shadowAttenuation + 0.3h);

                cor = MixFog(cor, s.fogFactor);
                return half4(cor, 1.0h);
            }
            ENDHLSL
        }

        // Sombra projetada (o personagem faz sombra no chão)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex VertSombra
            #pragma fragment FragSombra
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct E { float4 posicao : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 VertSombra(E e) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(e);
                float3 pw = TransformObjectToWorld(e.posicao.xyz);
                float3 nw = TransformObjectToWorldNormal(e.normal);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 dirLuz = normalize(_LightPosition - pw);
                #else
                    float3 dirLuz = _LightDirection;
                #endif
                float4 pc = TransformWorldToHClip(ApplyShadowBias(pw, nw, dirLuz));
                #if UNITY_REVERSED_Z
                    pc.z = min(pc.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    pc.z = max(pc.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return pc;
            }
            half4 FragSombra() : SV_Target { return 0; }
            ENDHLSL
        }

        // Profundidade (para profundidade de campo e outros efeitos de tela)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex VertProf
            #pragma fragment FragProf
            #pragma multi_compile_instancing
            struct E { float4 posicao : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 VertProf(E e) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(e); return TransformObjectToHClip(e.posicao.xyz); }
            half FragProf() : SV_Target { return 0; }
            ENDHLSL
        }

        // Profundidade + normais (para a oclusão de ambiente SSAO enxergar o personagem)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex VertNorm
            #pragma fragment FragNorm
            #pragma multi_compile_instancing
            struct E { float4 posicao : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct S { float4 pc : SV_POSITION; half3 n : TEXCOORD0; };
            S VertNorm(E e) { UNITY_SETUP_INSTANCE_ID(e); S s; s.pc = TransformObjectToHClip(e.posicao.xyz); s.n = TransformObjectToWorldNormal(e.normal); return s; }
            half4 FragNorm(S s) : SV_Target { return half4(NormalizeNormalPerPixel(s.n), 0); }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
