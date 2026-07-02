Shader "Water/WaterSurface"
{
    Properties
    {
        [Header(Wave Mode)]
        [MaterialToggle] _UseGerstner("Use Gerstner Waves (off=Sine)", Float) = 1
        _WaveHeightScale("Wave Height Scale", Range(0, 2)) = 0.3
        _Steepness("Gerstner Steepness", Range(0, 1)) = 0.3
        _WaveAmp1("Wave 1 Amp", Range(0, 2)) = 0.4
        _WaveFreq1("Wave 1 Freq", Range(0.1, 5)) = 0.8
        _WaveSpeed1("Wave 1 Speed", Range(0.1, 3)) = 1.2
        _WaveDir1("Wave 1 Dir", Vector) = (1, 0, 0, 0)
        _WaveAmp2("Wave 2 Amp", Range(0, 2)) = 0.25
        _WaveFreq2("Wave 2 Freq", Range(0.1, 5)) = 1.2
        _WaveSpeed2("Wave 2 Speed", Range(0.1, 3)) = 0.8
        _WaveDir2("Wave 2 Dir", Vector) = (0.7, 0.7, 0, 0)
        _WaveAmp3("Wave 3 Amp", Range(0, 2)) = 0.15
        _WaveFreq3("Wave 3 Freq", Range(0.1, 5)) = 2.0
        _WaveSpeed3("Wave 3 Speed", Range(0.1, 3)) = 2.0
        _WaveDir3("Wave 3 Dir", Vector) = (-0.3, 0.9, 0, 0)
        _WaveAmp4("Wave 4 Amp", Range(0, 2)) = 0.08
        _WaveFreq4("Wave 4 Freq", Range(0.1, 5)) = 3.0
        _WaveSpeed4("Wave 4 Speed", Range(0.1, 3)) = 1.5
        _WaveDir4("Wave 4 Dir", Vector) = (-0.8, 0.6, 0, 0)

        [Header(Normal Maps)]
        _NormalMap1("Normal Map 1", 2D) = "bump" {}
        _NormalMap2("Normal Map 2", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 3)) = 1.2
        _NormalSpeed1("Scroll Speed 1", Vector) = (0.015, 0.008, 0, 0)
        _NormalSpeed2("Scroll Speed 2", Vector) = (-0.012, 0.015, 0, 0)
        _NormalTiling("Normal Tiling", Range(0.5, 10)) = 2.5

        [Header(Turbulence)]
        _TurbulenceTex("Turbulence Noise", 2D) = "black" {}
        _TurbulenceStrength("Strength", Range(0, 0.1)) = 0.03
        _TurbulenceSpeed("Speed", Vector) = (0.01, 0.01, 0, 0)

        [Header(Reflection)]
        [NoScaleOffset] _Cubemap("Reflection Cubemap", Cube) = "_Skybox" {}
        _ReflectionStrength("Strength", Range(0, 1)) = 0.5

        [Header(Fresnel)]
        _FresnelPower("Power", Range(0.5, 8)) = 3.0
        _FresnelOffset("Offset", Range(0, 0.5)) = 0.02

        [Header(Specular)]
        _SpecColor("Color", Color) = (1, 1, 1, 1)
        _Shininess("Shininess", Range(0.1, 128)) = 64

        [Header(Water Color)]
        _ShallowColor("Shallow", Color) = (0.1, 0.3, 0.2, 0.6)
        _DeepColor("Deep", Color) = (0.0, 0.05, 0.1, 0.9)
        _DepthFactor("Depth Factor", Range(0.1, 2)) = 0.5

        [Header(Foam)]
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamTexture("Foam Noise", 2D) = "white" {}
        _MainFoamScale("Scale", Float) = 40
        _MainFoamIntensity("Intensity", Range(0, 10)) = 3.8
        _MainFoamSpeed("Speed", Float) = 0.1
        _MainFoamOpacity("Opacity", Range(0, 1)) = 0.87
        _MainFoamWidth("Width", Range(0.01, 2)) = 0.2

        [Header(Alpha)]
        _Alpha("Alpha", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 worldTangent : TEXCOORD3;
                float3 worldBinormal : TEXCOORD4;
                float4 projPos : TEXCOORD5;
                float3 viewDir : TEXCOORD6;
                UNITY_FOG_COORDS(7)
            };

            float _UseGerstner, _Steepness, _WaveHeightScale;
            float _WaveAmp1, _WaveFreq1, _WaveSpeed1; float4 _WaveDir1;
            float _WaveAmp2, _WaveFreq2, _WaveSpeed2; float4 _WaveDir2;
            float _WaveAmp3, _WaveFreq3, _WaveSpeed3; float4 _WaveDir3;
            float _WaveAmp4, _WaveFreq4, _WaveSpeed4; float4 _WaveDir4;

            sampler2D _NormalMap1, _NormalMap2;
            float _NormalScale;
            float2 _NormalSpeed1, _NormalSpeed2;
            float _NormalTiling;

            sampler2D _TurbulenceTex;
            float _TurbulenceStrength;
            float2 _TurbulenceSpeed;

            samplerCUBE _Cubemap;
            float _ReflectionStrength;
            float _FresnelPower, _FresnelOffset;
            float _Shininess;
            float4 _ShallowColor, _DeepColor;
            float _DepthFactor;

            float4 _FoamColor;
            sampler2D _FoamTexture;
            float _MainFoamScale, _MainFoamIntensity, _MainFoamSpeed, _MainFoamOpacity, _MainFoamWidth;

            float _Alpha;
            sampler2D _CameraDepthTexture;

            // ---- Gerstner Wave ----
            float3 gerstnerOffset(float2 pos, float time, float2 dir, float amp, float freq, float Q)
            {
                float phase = freq * dot(dir, pos) + time;
                float3 o;
                o.x = Q * amp * dir.x * cos(phase);
                o.z = Q * amp * dir.y * cos(phase);
                o.y = amp * sin(phase);
                return o;
            }

            float3 gerstnerNormal(float2 pos, float time, float2 dir, float amp, float freq, float Q)
            {
                float phase = freq * dot(dir, pos) + time;
                float wa = freq * amp;
                float S = sin(phase), C = cos(phase);
                return float3(-dir.x * wa * C, Q * wa * (dir.x * S + dir.y * S), -dir.y * wa * C);
            }

            // Gerstner wave normal reconstruction
            float3 calcGerstner(float2 pos, float time, out float3 worldOffset)
            {
                worldOffset = 0;
                float3 N = float3(0, 0, 0);
                float3 tangent = float3(1,0,0), binormal = float3(0,0,1);

                if (_UseGerstner) {
                    float2 d1 = normalize(_WaveDir1.xy);
                    float Q1 = _Steepness / (_WaveFreq1 * _WaveAmp1 * 4 + 0.001);
                    worldOffset += gerstnerOffset(pos, time * _WaveSpeed1, d1, _WaveAmp1, _WaveFreq1, Q1) * _WaveHeightScale;
                    N += float3(-gerstnerNormal(pos, time * _WaveSpeed1, d1, _WaveAmp1, _WaveFreq1, Q1).x, 1, -gerstnerNormal(pos, time * _WaveSpeed1, d1, _WaveAmp1, _WaveFreq1, Q1).z) * _WaveHeightScale;

                    float2 d2 = normalize(_WaveDir2.xy);
                    float Q2 = _Steepness / (_WaveFreq2 * _WaveAmp2 * 4 + 0.001);
                    worldOffset += gerstnerOffset(pos, time * _WaveSpeed2, d2, _WaveAmp2, _WaveFreq2, Q2) * _WaveHeightScale;
                    N += float3(-gerstnerNormal(pos, time * _WaveSpeed2, d2, _WaveAmp2, _WaveFreq2, Q2).x, 0, -gerstnerNormal(pos, time * _WaveSpeed2, d2, _WaveAmp2, _WaveFreq2, Q2).z) * _WaveHeightScale;

                    float2 d3 = normalize(_WaveDir3.xy);
                    float Q3 = _Steepness / (_WaveFreq3 * _WaveAmp3 * 4 + 0.001);
                    worldOffset += gerstnerOffset(pos, time * _WaveSpeed3, d3, _WaveAmp3, _WaveFreq3, Q3) * _WaveHeightScale;
                    N += float3(-gerstnerNormal(pos, time * _WaveSpeed3, d3, _WaveAmp3, _WaveFreq3, Q3).x, 0, -gerstnerNormal(pos, time * _WaveSpeed3, d3, _WaveAmp3, _WaveFreq3, Q3).z) * _WaveHeightScale;

                    float2 d4 = normalize(_WaveDir4.xy);
                    float Q4 = _Steepness / (_WaveFreq4 * _WaveAmp4 * 4 + 0.001);
                    worldOffset += gerstnerOffset(pos, time * _WaveSpeed4, d4, _WaveAmp4, _WaveFreq4, Q4) * _WaveHeightScale;
                    N += float3(-gerstnerNormal(pos, time * _WaveSpeed4, d4, _WaveAmp4, _WaveFreq4, Q4).x, 0, -gerstnerNormal(pos, time * _WaveSpeed4, d4, _WaveAmp4, _WaveFreq4, Q4).z) * _WaveHeightScale;
                }
                return normalize(N);
            }

            // ---- Sine wave displacement (fallback) ----
            float sineWave(float2 pos, float time)
            {
                float h = 0;
                h += 0.5 * sin(pos.x * 0.3 + time * 1.2);
                h += 0.3 * sin(pos.y * 0.5 + time * 0.8);
                h += 0.15 * sin((pos.x + pos.y) * 1.0 + time * 2.0);
                return h * 0.3;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 normalWS = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 tangentWS = normalize(mul((float3x3)unity_ObjectToWorld, v.tangent.xyz));
                float3 binormalWS = cross(normalWS, tangentWS) * v.tangent.w;

                float h;
                float3 gerstnerN;
                float3 gerstnerOff;
                if (_UseGerstner)
                {
                    gerstnerN = calcGerstner(worldPos.xz, _Time.y, gerstnerOff);
                    worldPos += gerstnerOff;
                    h = gerstnerOff.y;
                    o.worldNormal = gerstnerN;
                }
                else
                {
                    h = sineWave(worldPos.xz, _Time.y);
                    worldPos.y += h;
                    o.worldNormal = normalWS;
                }

                o.worldPos = worldPos;
                o.worldTangent = tangentWS;
                o.worldBinormal = binormalWS;
                o.pos = UnityWorldToClipPos(worldPos);
                o.projPos = ComputeScreenPos(o.pos);
                o.projPos.z = -mul(UNITY_MATRIX_V, float4(worldPos, 1)).z;
                o.uv = v.uv * _NormalTiling;
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float3 blendNormals(float3 n1, float3 n2)
            {
                return normalize(float3(n1.xy + n2.xy, n1.z * n2.z));
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 V = normalize(i.viewDir);
                float3 N = normalize(i.worldNormal);

                // Turbulence UV distortion
                float2 turbUV = i.worldPos.xz * 0.05 + _Time.y * _TurbulenceSpeed;
                float4 turbSample = tex2D(_TurbulenceTex, turbUV);
                float2 turbOffset = (turbSample.rg - 0.5) * 2.0 * _TurbulenceStrength;

                // Dual-layer normal maps
                float2 uv1 = i.uv + _Time.y * _NormalSpeed1 + turbOffset;
                float2 uv2 = i.uv + _Time.y * _NormalSpeed2 + turbOffset * 0.7;
                float3 n1 = UnpackNormal(tex2D(_NormalMap1, uv1));
                float3 n2 = UnpackNormal(tex2D(_NormalMap2, uv2));
                n1.xy *= _NormalScale; n2.xy *= _NormalScale;
                float3 Ndet = blendNormals(n1, n2);

                // Reorient along wave normal (or use world normal for sine waves)
                float3 T = normalize(float3(N.z, 0, -N.x));
                float3 B = cross(N, T);
                N = normalize(Ndet.x * T + Ndet.y * B + Ndet.z * N);

                // Fresnel
                float cosTheta = max(dot(V, N), 0.0);
                float F = _FresnelOffset + (1.0 - _FresnelOffset) * pow(1.0 - cosTheta, _FresnelPower);

                // Cubemap reflection
                float3 reflDir = reflect(-V, N);
                float3 reflection = texCUBE(_Cubemap, reflDir).rgb * _ReflectionStrength;

                // Depth-based underwater fog
                float2 screenUV = i.projPos.xy / i.projPos.w;
                float rawDepth = tex2D(_CameraDepthTexture, screenUV).r;
                float sceneZ = LinearEyeDepth(rawDepth);
                float waterDepth = sceneZ - i.projPos.z;
                float depthFactor = saturate(waterDepth * _DepthFactor);
                float3 refraction = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);

                // Blinn-Phong specular
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 H = normalize(V + L);
                float spec = pow(max(dot(N, H), 0.0), _Shininess);
                float3 specular = _SpecColor.rgb * spec * _LightColor0.rgb;

                // Haimian-style foam
                float2 foamUV = i.worldPos.xz * _MainFoamScale * 0.01 + _Time.y * _MainFoamSpeed * 0.5;
                float4 foamNoise = tex2D(_FoamTexture, foamUV);
                float foamRaw = saturate(waterDepth / (_MainFoamWidth * (foamNoise.r * max(_MainFoamIntensity, 0.01))));
                float foam = (1.0 - saturate(pow(foamRaw, 15.0) / 0.1)) * _MainFoamOpacity;
                float3 foamCol = _FoamColor.rgb * foam;

                // Combine
                float3 color = lerp(refraction, reflection, F);
                color += specular;
                color += foamCol;
                float alpha = lerp(_ShallowColor.a, _DeepColor.a, depthFactor) * _Alpha;
                alpha = max(alpha, foam * _FoamColor.a);

                UNITY_APPLY_FOG(i.fogCoord, color);
                return float4(color, alpha);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
